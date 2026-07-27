using System;
using System.Threading;
using NutriMind.App.Features;
using NutriMind.App.Routing;
using NutriMind.App.UI;
using NutriMind.Core.Bootstrap;
using NutriMind.Core.Utilities;
using UnityEngine;
using UnityEngine.UIElements;

namespace NutriMind.App.Presentation
{
    /// <summary>
    /// Binds <see cref="LoginPanelView"/> to <see cref="LoginUseCase"/>.
    /// Clears PIN after success/failure. Never logs PIN. Masks LRN in logs.
    /// </summary>
    public sealed class LoginRuntimePresenter : IDisposable
    {
        private readonly AppLifetime _lifetime;
        private readonly LoginUseCase _loginUseCase;
        private readonly LoginPanelView _view;
        private readonly CancellationTokenSource _cts;
        private IVisualElementScheduledItem _rateLimitTicker;
        private int _rateLimitRemaining;
        private bool _disposed;

        public LoginRuntimePresenter(
            AppLifetime lifetime,
            LoginUseCase loginUseCase,
            LoginPanelView view)
        {
            _lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
            _loginUseCase = loginUseCase ?? throw new ArgumentNullException(nameof(loginUseCase));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _cts = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.LifetimeToken);

            _view.SubmitRequested += OnSubmitRequested;
            _view.ForgotPinRequested += OnForgotPinRequested;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            StopRateLimitTicker();
            _view.SubmitRequested -= OnSubmitRequested;
            _view.ForgotPinRequested -= OnForgotPinRequested;

            try
            {
                _cts.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // ignore
            }

            _cts.Dispose();
        }

        private async void OnSubmitRequested()
        {
            if (_disposed || _loginUseCase.IsSubmitting || _rateLimitRemaining > 0)
            {
                return;
            }

            _view.ClearStatus();
            _view.SetChecking(true);

            LoginUseCaseResult result = await _loginUseCase.ExecuteAsync(
                new LoginRequestModel
                {
                    Lrn = _view.Lrn,
                    Pin = _view.Pin,
                    DeviceName = _view.DeviceName
                },
                _cts.Token);

            await UnityMainThread.SwitchToMainAsync(_cts.Token);
            if (_disposed)
            {
                return;
            }

            // Always clear PIN after an attempt — success or failure.
            _view.ClearPin();
            _view.SetChecking(false);

            if (result.IsSuccess)
            {
                _view.ClearStatus();
                _lifetime.Router?.EnsureMainRoot();
                await _lifetime.SceneNavigator.LoadAsync(AppSceneId.Main, _cts.Token);
                return;
            }

            if (result.IsValidationError)
            {
                _view.SetStatus(LoginStatusTone.Warning, result.Message);
                return;
            }

            if (result.IsRateLimited)
            {
                _rateLimitRemaining = Math.Max(1, result.RetryAfterSeconds ?? 60);
                _view.SetRateLimitCountdown(_rateLimitRemaining);
                StartRateLimitTicker();
                return;
            }

            if (result.IsOfflineUnavailable)
            {
                _view.SetStatus(LoginStatusTone.Info, result.Message, "ds-icon--wifi");
                return;
            }

            _view.SetStatus(
                LoginStatusTone.Danger,
                string.IsNullOrWhiteSpace(result.Message)
                    ? "We could not verify those details. Please try again."
                    : result.Message);
        }

        private void OnForgotPinRequested()
        {
            NutriMindLog.Auth("Forgot PIN requested — recovery flow not wired in Prompt 1.");
            _view.SetStatus(
                LoginStatusTone.Info,
                "Ask your teacher if you need help recovering your PIN.",
                "ds-icon--info");
        }

        private void StartRateLimitTicker()
        {
            StopRateLimitTicker();
            if (_view.Root == null)
            {
                return;
            }

            _rateLimitTicker = _view.Root.schedule.Execute(() =>
            {
                if (_disposed)
                {
                    return;
                }

                _rateLimitRemaining = Math.Max(0, _rateLimitRemaining - 1);
                if (_rateLimitRemaining <= 0)
                {
                    StopRateLimitTicker();
                    _view.ClearStatus();
                    _view.SetSubmitEnabled(true);
                    return;
                }

                _view.SetRateLimitCountdown(_rateLimitRemaining);
            }).Every(1000);
        }

        private void StopRateLimitTicker()
        {
            _rateLimitTicker?.Pause();
            _rateLimitTicker = null;
        }
    }
}
