using System;
using System.Threading;
using System.Threading.Tasks;
using NutriMind.App.UI;
using NutriMind.Core.Bootstrap;
using NutriMind.Core.Data;
using NutriMind.Core.Networking;
using NutriMind.Core.Persistence;
using NutriMind.Core.Utilities;
using UnityEngine;

namespace NutriMind.App.Features
{
    /// <summary>
    /// Owns application startup state transitions. Presenters observe; views never call this directly for SQL/HTTP.
    /// </summary>
    public sealed class AppStartupCoordinator
    {
        private readonly AppLifetime _lifetime;
        private CancellationTokenSource _runCts;
        private BootstrapPreviewState _state = BootstrapPreviewState.InitializingLocalStorage;
        private AppError _lastError;
        private bool _offlineEligible;

        public AppStartupCoordinator(AppLifetime lifetime)
        {
            _lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
        }

        public event Action<BootstrapPreviewState> StateChanged;

        public BootstrapPreviewState State => _state;

        public AppError LastError => _lastError;

        public bool IsOfflineEligible => _offlineEligible;

        public async Task RunAsync(CancellationToken externalToken = default)
        {
            CancelActiveRun();
            _runCts = CancellationTokenSource.CreateLinkedTokenSource(
                externalToken,
                _lifetime.LifetimeToken);
            CancellationToken token = _runCts.Token;

            try
            {
                await ExecuteStartupAsync(token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                NutriMindLog.Startup("Startup cancelled.");
            }
            catch (Exception exception)
            {
                _lastError = AppError.FromException(exception);
                SetState(BootstrapPreviewState.RecoverableError);
                NutriMindLog.StartupError(
                    "Startup failed: " + exception.GetType().Name + " — " + exception.Message);
            }
        }

        public void Cancel()
        {
            CancelActiveRun();
        }

        public async Task ContinueOfflineAsync(CancellationToken cancellationToken = default)
        {
            if (!_offlineEligible)
            {
                NutriMindLog.StartupWarning("Continue Offline ignored; session is not offline_eligible.");
                return;
            }

            SetState(BootstrapPreviewState.Ready);
            await Task.CompletedTask;
        }

        private async Task ExecuteStartupAsync(CancellationToken cancellationToken)
        {
            _lastError = null;
            _offlineEligible = false;

            if (_lifetime.ConfigurationError != null)
            {
                _lastError = _lifetime.ConfigurationError;
                SetState(BootstrapPreviewState.RecoverableError);
                return;
            }

            if (_lifetime.Composition?.ComposeError != null
                && _lifetime.RuntimeOptions != null
                && _lifetime.RuntimeOptions.Mode != NutriMindRuntimeMode.Mock)
            {
                _lastError = _lifetime.Composition.ComposeError;
                SetState(BootstrapPreviewState.RecoverableError);
                return;
            }

            SetState(BootstrapPreviewState.InitializingLocalStorage);

            // Capture Unity API values while still on the main thread (later awaits use ConfigureAwait(false)).
            string clientVersion = Application.version;

            AppResult open = _lifetime.Database != null
                ? (_lifetime.Database.IsOpen ? AppResult.Success() : _lifetime.Database.Open())
                : AppResult.Failure(AppErrorCodes.ClientInternalError, "Database is not available.");
            if (open.IsFailure)
            {
                _lastError = open.Error;
                SetState(BootstrapPreviewState.RecoverableError);
                return;
            }

            AppResult<string> deviceId = _lifetime.InstallationRepository.GetOrCreateDeviceId();
            if (deviceId.IsFailure)
            {
                _lastError = deviceId.Error;
                SetState(BootstrapPreviewState.RecoverableError);
                return;
            }

            _lifetime.SetInstallationDeviceId(deviceId.Value);

            SetState(BootstrapPreviewState.CheckingSecureToken);
            string token = await _lifetime.TokenStore.ReadAsync(cancellationToken).ConfigureAwait(false);
            bool hasToken = !string.IsNullOrWhiteSpace(token);

            SetState(BootstrapPreviewState.CheckingConnectivity);
            bool isOnline = _lifetime.Connectivity == null || _lifetime.Connectivity.IsOnline;

            if (!isOnline)
            {
                await HandleOfflinePathAsync(hasToken, cancellationToken).ConfigureAwait(false);
                return;
            }

            AppResult<PingStatus> ping = await _lifetime.Gateway.PingAsync(cancellationToken).ConfigureAwait(false);
            if (ping.IsFailure)
            {
                if (ping.Error != null && ping.Error.IsNetworkError)
                {
                    await HandleOfflinePathAsync(hasToken, cancellationToken).ConfigureAwait(false);
                    return;
                }

                _lastError = ping.Error;
                SetState(BootstrapPreviewState.RecoverableError);
                return;
            }

            SetState(BootstrapPreviewState.CheckingClientVersion);
            AppResult<ClientConfiguration> configResult =
                await _lifetime.Gateway.GetConfigAsync(cancellationToken).ConfigureAwait(false);
            if (configResult.IsFailure)
            {
                if (configResult.Error != null
                    && (configResult.Error.Code == AppErrorCodes.ServiceUnavailable
                        || configResult.Error.HttpStatus == 503))
                {
                    SetState(BootstrapPreviewState.Maintenance);
                    _lastError = configResult.Error;
                    return;
                }

                if (configResult.Error != null && configResult.Error.IsNetworkError)
                {
                    await HandleOfflinePathAsync(hasToken, cancellationToken).ConfigureAwait(false);
                    return;
                }

                _lastError = configResult.Error;
                SetState(BootstrapPreviewState.RecoverableError);
                return;
            }

            ClientConfiguration config = configResult.Value;
            _lifetime.SetClientConfiguration(config);

            if (config.MaintenanceMode)
            {
                _lastError = AppError.Api(
                    AppErrorCodes.ServiceUnavailable,
                    string.IsNullOrWhiteSpace(config.MaintenanceMessage)
                        ? "Online services are temporarily unavailable."
                        : config.MaintenanceMessage,
                    503,
                    isRetryable: true);
                SetState(BootstrapPreviewState.Maintenance);
                return;
            }

            if (IsClientVersionTooOld(clientVersion, config.MinimumClientVersion))
            {
                _lastError = AppError.Api(
                    AppErrorCodes.ClientVersionUnsupported,
                    "A newer version of NutriMind is required.",
                    426);
                SetState(BootstrapPreviewState.RequiredUpdate);
                return;
            }

            SetState(BootstrapPreviewState.CheckingManifest);
            // Manifest policy is recorded from config; full content catalog checks arrive in later prompts.
            if (!string.IsNullOrWhiteSpace(config.RequiredManifestVersion)
                && string.Equals(config.RequiredManifestVersion, "unsupported", StringComparison.OrdinalIgnoreCase))
            {
                _lastError = AppError.Api(
                    AppErrorCodes.ManifestVersionUnsupported,
                    "Required gameplay manifest is unsupported.",
                    409);
                SetState(BootstrapPreviewState.RequiredUpdate);
                return;
            }

            if (!hasToken)
            {
                SetState(BootstrapPreviewState.AuthenticationRequired);
                return;
            }

            SetState(BootstrapPreviewState.LoadingBootstrap);
            AppResult<BootstrapSnapshot> bootstrap =
                await _lifetime.Gateway.GetBootstrapAsync(cancellationToken).ConfigureAwait(false);
            if (bootstrap.IsFailure)
            {
                if (IsUnauthorized(bootstrap.Error))
                {
                    await _lifetime.ClearAuthenticationAsync(cancellationToken).ConfigureAwait(false);
                    SetState(BootstrapPreviewState.AuthenticationRequired);
                    return;
                }

                if (bootstrap.Error != null && bootstrap.Error.IsNetworkError)
                {
                    await HandleOfflinePathAsync(hasToken: true, cancellationToken).ConfigureAwait(false);
                    return;
                }

                _lastError = bootstrap.Error;
                SetState(BootstrapPreviewState.RecoverableError);
                return;
            }

            await PersistBootstrapSessionAsync(bootstrap.Value, offlineEligible: true, cancellationToken)
                .ConfigureAwait(false);
            _lifetime.SetBootstrap(bootstrap.Value);
            _lifetime.SetAuthenticated(bootstrap.Value.Profile, authenticated: true);
            SetState(BootstrapPreviewState.Ready);
        }

        private async Task HandleOfflinePathAsync(bool hasToken, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            AppResult<LocalSessionRecord> sessionResult = _lifetime.SessionRepository.GetSession();
            LocalSessionRecord session = sessionResult.IsSuccess ? sessionResult.Value : null;
            bool offlineEligible = session != null && session.OfflineEligible;

            // Cache alone is not eligibility — offline_eligible must be explicitly set on the session.
            AppResult<ResourceCacheRecord> cache = _lifetime.ResourceCacheRepository.Get(ResourceCacheKeys.Bootstrap);
            bool hasBootstrapCache = cache.IsSuccess && cache.Value != null;

            if (hasToken && offlineEligible && hasBootstrapCache)
            {
                _offlineEligible = true;
                if (session != null)
                {
                    _lifetime.SetAuthenticated(
                        new StudentProfile
                        {
                            Id = session.StudentId,
                            DisplayName = session.DisplayName,
                            GradeId = session.GradeId,
                            Section = new StudentSection
                            {
                                Id = session.SectionId,
                                Name = session.SectionName,
                                GradeId = session.GradeId
                            },
                            IsActive = true
                        },
                        authenticated: true);
                }

                SetState(BootstrapPreviewState.OfflineEligible);
                return;
            }

            if (!hasToken)
            {
                SetState(BootstrapPreviewState.AuthenticationRequired);
                return;
            }

            _lastError = AppError.Network(
                AppErrorCodes.NetworkOffline,
                "Offline mode is unavailable until a successful online bootstrap marks this device eligible.");
            SetState(BootstrapPreviewState.RecoverableError);
            await Task.CompletedTask;
        }

        private async Task PersistBootstrapSessionAsync(
            BootstrapSnapshot snapshot,
            bool offlineEligible,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            StudentProfile profile = snapshot?.Profile;
            if (profile == null || string.IsNullOrWhiteSpace(profile.Id))
            {
                return;
            }

            string now = _lifetime.Clock.UtcNow.ToUniversalTime().ToString("o");
            var session = new LocalSessionRecord
            {
                StudentId = profile.Id,
                DisplayName = profile.DisplayName,
                GradeId = profile.GradeId,
                SectionId = profile.Section?.Id,
                SectionName = profile.Section?.Name,
                LastAuthenticatedUtc = now,
                LastBootstrapRevision = snapshot.Sync != null ? snapshot.Sync.Revision : 0,
                LastBootstrapCachedUtc = now,
                OfflineEligible = offlineEligible
            };

            _lifetime.SessionRepository.UpsertSession(session);

            // Marker cache entry — payload is not a transport secret and excludes the access token.
            _lifetime.ResourceCacheRepository.Upsert(new ResourceCacheRecord
            {
                CacheKey = ResourceCacheKeys.Bootstrap,
                PayloadJson = "{\"cached\":true,\"student_id\":\"" + EscapeJson(profile.Id) + "\"}",
                SchemaVersion = 1,
                ServerRevision = snapshot.Sync != null ? snapshot.Sync.Revision : (int?)null,
                CachedUtc = now
            });

            if (CachePolicy.AllowsCache(CachePolicy.Profile))
            {
                _lifetime.ResourceCacheRepository.Upsert(new ResourceCacheRecord
                {
                    CacheKey = ResourceCacheKeys.Profile,
                    PayloadJson =
                        "{\"id\":\"" + EscapeJson(profile.Id) +
                        "\",\"display_name\":\"" + EscapeJson(profile.DisplayName) + "\"}",
                    SchemaVersion = 1,
                    CachedUtc = now
                });
            }

            await Task.CompletedTask;
        }

        public async Task PersistAuthenticatedBootstrapAsync(
            BootstrapSnapshot snapshot,
            CancellationToken cancellationToken = default)
        {
            await PersistBootstrapSessionAsync(snapshot, offlineEligible: true, cancellationToken)
                .ConfigureAwait(false);
            _lifetime.SetBootstrap(snapshot);
            if (snapshot?.Profile != null)
            {
                _lifetime.SetAuthenticated(snapshot.Profile, authenticated: true);
            }
        }

        private void SetState(BootstrapPreviewState state)
        {
            _state = state;
            NutriMindLog.Startup("State -> " + BootstrapPanelView.GetContractStateName(state));

            // Startup awaits use ConfigureAwait(false); UI listeners must run on the main thread.
            BootstrapPreviewState captured = state;
            UnityMainThread.Post(() => StateChanged?.Invoke(captured));
        }

        private void CancelActiveRun()
        {
            if (_runCts == null)
            {
                return;
            }

            try
            {
                _runCts.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // ignore
            }

            _runCts.Dispose();
            _runCts = null;
        }

        private static bool IsUnauthorized(AppError error)
        {
            if (error == null)
            {
                return false;
            }

            return error.Code == AppErrorCodes.AuthTokenMissing
                   || error.Code == AppErrorCodes.AuthTokenInvalid
                   || error.Code == AppErrorCodes.AuthTokenRevoked
                   || error.HttpStatus == 401;
        }

        private static bool IsClientVersionTooOld(string currentVersion, string minimumVersion)
        {
            if (string.IsNullOrWhiteSpace(minimumVersion) || minimumVersion.Trim() == "0.0.0")
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(currentVersion))
            {
                return true;
            }

            return CompareVersions(currentVersion.Trim(), minimumVersion.Trim()) < 0;
        }

        private static int CompareVersions(string left, string right)
        {
            string[] leftParts = left.Split('.', '-', '+');
            string[] rightParts = right.Split('.', '-', '+');
            int length = Math.Max(leftParts.Length, rightParts.Length);
            for (int i = 0; i < length; i++)
            {
                int l = i < leftParts.Length && int.TryParse(leftParts[i], out int lv) ? lv : 0;
                int r = i < rightParts.Length && int.TryParse(rightParts[i], out int rv) ? rv : 0;
                if (l != r)
                {
                    return l.CompareTo(r);
                }
            }

            return 0;
        }

        private static string EscapeJson(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
