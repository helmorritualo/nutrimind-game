using System;
using System.Threading;
using System.Threading.Tasks;
using NutriMind.Core.Bootstrap;
using NutriMind.Core.Data;
using NutriMind.Core.Networking;
using NutriMind.Core.Utilities;

namespace NutriMind.App.Features
{
    public sealed class LoginRequestModel
    {
        public string Lrn { get; set; }
        public string Pin { get; set; }
        public string DeviceName { get; set; }
    }

    public sealed class LoginUseCaseResult
    {
        public bool IsSuccess { get; set; }
        public bool IsValidationError { get; set; }
        public bool IsRateLimited { get; set; }
        public bool IsOfflineUnavailable { get; set; }
        public int? RetryAfterSeconds { get; set; }
        public string Message { get; set; }
        public AppError Error { get; set; }
        public LoginResult Login { get; set; }
        public BootstrapSnapshot Bootstrap { get; set; }
    }

    /// <summary>
    /// Validates credentials and performs gateway login + bootstrap. Never logs PIN.
    /// </summary>
    public sealed class LoginUseCase
    {
        public const int MinLrnLength = 6;
        public const int MaxLrnLength = 32;
        public const int MinPinLength = 4;
        public const int MaxPinLength = 12;

        private readonly AppLifetime _lifetime;
        private readonly AppStartupCoordinator _startupCoordinator;
        private int _submitGate;

        public LoginUseCase(AppLifetime lifetime, AppStartupCoordinator startupCoordinator = null)
        {
            _lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
            _startupCoordinator = startupCoordinator;
        }

        public bool IsSubmitting => _submitGate > 0;

        public async Task<LoginUseCaseResult> ExecuteAsync(
            LoginRequestModel request,
            CancellationToken cancellationToken = default)
        {
            if (Interlocked.CompareExchange(ref _submitGate, 1, 0) != 0)
            {
                return new LoginUseCaseResult
                {
                    IsSuccess = false,
                    Message = "Sign-in is already in progress."
                };
            }

            try
            {
                return await ExecuteCoreAsync(request, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                Interlocked.Exchange(ref _submitGate, 0);
            }
        }

        private async Task<LoginUseCaseResult> ExecuteCoreAsync(
            LoginRequestModel request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string lrn = request?.Lrn?.Trim() ?? string.Empty;
            string pin = request?.Pin?.Trim() ?? string.Empty;
            string deviceName = request?.DeviceName?.Trim() ?? string.Empty;

            if (!Validate(lrn, pin, deviceName, out string validationMessage))
            {
                return new LoginUseCaseResult
                {
                    IsSuccess = false,
                    IsValidationError = true,
                    Message = validationMessage
                };
            }

            if (_lifetime.Connectivity != null && !_lifetime.Connectivity.IsOnline)
            {
                return new LoginUseCaseResult
                {
                    IsSuccess = false,
                    IsOfflineUnavailable = true,
                    Message = "You need an internet connection for your first sign-in.",
                    Error = AppError.Network(AppErrorCodes.NetworkOffline, "Sign-in requires connectivity.")
                };
            }

            NutriMindLog.Auth("Attempting login for LRN " + NutriMindLog.MaskLrn(lrn) + ".");

            AppResult<LoginResult> loginResult = await _lifetime.Gateway.LoginAsync(
                new LoginRequest
                {
                    Lrn = lrn,
                    Pin = pin,
                    DeviceName = deviceName
                },
                cancellationToken).ConfigureAwait(false);

            if (loginResult.IsFailure)
            {
                return MapFailure(loginResult.Error);
            }

            LoginResult login = loginResult.Value;
            if (login == null || string.IsNullOrWhiteSpace(login.AccessToken))
            {
                return new LoginUseCaseResult
                {
                    IsSuccess = false,
                    Message = "Sign-in response was incomplete.",
                    Error = AppError.Configuration("Login result did not include an access token.")
                };
            }

            // Token only to the token store — never SQLite / resource cache.
            await _lifetime.TokenStore.WriteAsync(login.AccessToken, cancellationToken).ConfigureAwait(false);

            AppResult<BootstrapSnapshot> bootstrapResult =
                await _lifetime.Gateway.GetBootstrapAsync(cancellationToken).ConfigureAwait(false);
            if (bootstrapResult.IsFailure)
            {
                await _lifetime.TokenStore.ClearAsync(cancellationToken).ConfigureAwait(false);
                return MapFailure(bootstrapResult.Error);
            }

            BootstrapSnapshot snapshot = bootstrapResult.Value;
            if (_startupCoordinator != null)
            {
                await _startupCoordinator.PersistAuthenticatedBootstrapAsync(snapshot, cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                _lifetime.SetBootstrap(snapshot);
                _lifetime.SetAuthenticated(snapshot?.Profile ?? login.Student, authenticated: true);
            }

            NutriMindLog.Auth("Login+bootstrap succeeded for LRN " + NutriMindLog.MaskLrn(lrn) + ".");
            return new LoginUseCaseResult
            {
                IsSuccess = true,
                Login = login,
                Bootstrap = snapshot,
                Message = "Signed in."
            };
        }

        public static bool Validate(string lrn, string pin, string deviceName, out string message)
        {
            if (string.IsNullOrWhiteSpace(lrn) || string.IsNullOrWhiteSpace(pin))
            {
                message = "Please enter both your LRN and PIN.";
                return false;
            }

            if (lrn.Length < MinLrnLength || lrn.Length > MaxLrnLength)
            {
                message = "LRN must be between 6 and 32 characters.";
                return false;
            }

            if (pin.Length < MinPinLength || pin.Length > MaxPinLength)
            {
                message = "PIN must be between 4 and 12 characters.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(deviceName))
            {
                message = "Device name is required.";
                return false;
            }

            message = null;
            return true;
        }

        private static LoginUseCaseResult MapFailure(AppError error)
        {
            if (error == null)
            {
                return new LoginUseCaseResult
                {
                    IsSuccess = false,
                    Message = "Sign-in failed."
                };
            }

            if (error.Code == AppErrorCodes.RateLimited)
            {
                return new LoginUseCaseResult
                {
                    IsSuccess = false,
                    IsRateLimited = true,
                    RetryAfterSeconds = error.RetryAfterSeconds ?? 60,
                    Message = "Too many attempts. Try again later.",
                    Error = error
                };
            }

            if (error.IsNetworkError || error.Code == AppErrorCodes.NetworkOffline)
            {
                return new LoginUseCaseResult
                {
                    IsSuccess = false,
                    IsOfflineUnavailable = true,
                    Message = "You need an internet connection for your first sign-in.",
                    Error = error
                };
            }

            if (error.Code == AppErrorCodes.AuthInvalidCredentials)
            {
                return new LoginUseCaseResult
                {
                    IsSuccess = false,
                    Message = "We could not verify those details. Please try again.",
                    Error = error
                };
            }

            return new LoginUseCaseResult
            {
                IsSuccess = false,
                Message = string.IsNullOrWhiteSpace(error.Message)
                    ? "Sign-in failed."
                    : error.Message,
                Error = error
            };
        }
    }
}
