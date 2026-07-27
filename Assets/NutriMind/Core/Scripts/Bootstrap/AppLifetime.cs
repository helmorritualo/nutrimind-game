using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NutriMind.App.Composition;
using NutriMind.App.Features;
using NutriMind.App.Routing;
using NutriMind.App.State;
using NutriMind.Core.Data;
using NutriMind.Core.Networking;
using NutriMind.Core.Persistence;
using NutriMind.Core.Sync;
using NutriMind.Core.Utilities;
using UnityEngine;

namespace NutriMind.Core.Bootstrap
{
    /// <summary>
    /// DontDestroyOnLoad application lifetime owner. Created by SCN_App_Bootstrap.
    /// Prevents duplicates when returning to Bootstrap.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AppLifetime : MonoBehaviour
    {
        public const string LifetimeObjectName = "PFB_AppLifetime";

        private static AppLifetime _instance;

        [SerializeField]
        private NutriMindRuntimeOptions _runtimeOptions = NutriMindRuntimeOptions.CreateDefaults();

        private CancellationTokenSource _lifetimeCts;
        private AppCompositionRoot _compositionRoot;
        private bool _composeStarted;
        private bool _isInitialized;
        private readonly SemaphoreSlim _resetGate = new SemaphoreSlim(1, 1);

        public static AppLifetime Instance => _instance;

        public static bool HasInstance => _instance != null;

        public NutriMindRuntimeOptions RuntimeOptions => _runtimeOptions;

        public AppCompositionRoot Composition => _compositionRoot;

        public NutriMindDatabase Database => _compositionRoot?.Database;

        public IStudentGateway Gateway => _compositionRoot?.Gateway;

        public IAuthTokenStore TokenStore => _compositionRoot?.TokenStore;

        public IConnectivityService Connectivity => _compositionRoot?.Connectivity;

        public IMockRuntimeState MockRuntimeState => _compositionRoot?.MockRuntimeState;

        public SyncCoordinator SyncCoordinator => _compositionRoot?.SyncCoordinator;

        public IAppSceneNavigator SceneNavigator => _compositionRoot?.SceneNavigator;

        public IAppRouter Router => _compositionRoot?.Router;

        public AuthenticatedStudentState AuthenticatedStudentState =>
            _compositionRoot?.AuthenticatedStudentState;

        public ILocalSettingsStore LocalSettingsStore => _compositionRoot?.LocalSettingsStore;

        private IMissionLaunchService _missionLaunchService;

        /// <summary>
        /// Returns the application-mode mission launch service.
        /// Lazily created after composition. In Mock mode returns <see cref="MockMissionLaunchService"/>.
        /// </summary>
        public IMissionLaunchService MissionLaunchService
        {
            get
            {
                if (_missionLaunchService == null && IsReady)
                {
                    _missionLaunchService = new MockMissionLaunchService(this);
                }

                return _missionLaunchService;
            }
        }

        public IInstallationRepository InstallationRepository => _compositionRoot?.InstallationRepository;

        public ILocalSessionRepository SessionRepository => _compositionRoot?.SessionRepository;

        public IResourceCacheRepository ResourceCacheRepository => _compositionRoot?.ResourceCacheRepository;

        public IMissionProgressRepository MissionProgressRepository =>
            _compositionRoot?.MissionProgressRepository;

        public IOutboxRepository OutboxRepository => _compositionRoot?.OutboxRepository;

        public IAnnouncementReadRepository AnnouncementReadRepository =>
            _compositionRoot?.AnnouncementReadRepository;

        public IIdempotentRequestRepository IdempotentRequestRepository =>
            _compositionRoot?.IdempotentRequestRepository;

        public ILocalProgressWriter LocalProgressWriter => _compositionRoot?.LocalProgressWriter;

        public IOutboxPayloadSerializer OutboxPayloadSerializer =>
            _compositionRoot?.OutboxPayloadSerializer;

        public IAppClock Clock => _compositionRoot?.Clock;

        public IIdGenerator IdGenerator => _compositionRoot?.IdGenerator;

        public CancellationToken LifetimeToken =>
            _lifetimeCts != null ? _lifetimeCts.Token : CancellationToken.None;

        public bool IsAuthenticated { get; private set; }

        public StudentProfile CurrentProfile { get; private set; }

        public BootstrapSnapshot LastBootstrap { get; private set; }

        public ClientConfiguration LastClientConfiguration { get; private set; }

        public string InstallationDeviceId { get; private set; }

        public AppError ConfigurationError { get; private set; }

        public bool OfflineEligible { get; private set; }

        public bool IsReady => _isInitialized && _compositionRoot != null && !_compositionRoot.IsDisposed;

        private void Awake()
        {
            UnityMainThread.EnsureCaptured();

            if (_instance != null && _instance != this)
            {
                NutriMindLog.Runtime("Duplicate AppLifetime destroyed on return to Bootstrap.");
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            gameObject.name = LifetimeObjectName;

            if (_runtimeOptions == null)
            {
                _runtimeOptions = NutriMindRuntimeOptions.CreateDefaults();
            }
            else
            {
                _runtimeOptions.Clamp();
            }

            _lifetimeCts = new CancellationTokenSource();
            InitializeComposition();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_runtimeOptions.Mode == NutriMindRuntimeMode.Mock)
            {
                DevelopmentMockRuntimeController.EnsureOn(gameObject);
            }
#endif
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }

            DisposeLifetime();
        }

        /// <summary>
        /// Applies startup options only before composition. Late calls are ignored.
        /// Preferred owner: serialized options on PFB_AppLifetime.
        /// </summary>
        public void ConfigureBeforeCompose(NutriMindRuntimeOptions options)
        {
            if (_composeStarted)
            {
                NutriMindLog.RuntimeWarning(
                    "Ignoring ConfigureBeforeCompose after composition has started.");
                return;
            }

            if (options == null)
            {
                return;
            }

            options.Clamp();
            _runtimeOptions = options.Clone();
        }

        [Obsolete("Use ConfigureBeforeCompose before Awake/Compose. Late option changes are ignored.")]
        public void SetRuntimeOptions(NutriMindRuntimeOptions options)
        {
            ConfigureBeforeCompose(options);
        }

        public void SetBootstrap(BootstrapSnapshot snapshot)
        {
            LastBootstrap = snapshot;
            AuthenticatedStudentState?.ApplyBootstrap(snapshot);
        }

        public void SetAuthenticated(StudentProfile profile, bool authenticated)
        {
            IsAuthenticated = authenticated;
            CurrentProfile = profile;
            if (authenticated && profile != null)
            {
                AuthenticatedStudentState?.ApplyProfile(profile);
            }
            else if (!authenticated)
            {
                AuthenticatedStudentState?.Clear();
            }
        }

        public void SetClientConfiguration(ClientConfiguration configuration)
        {
            LastClientConfiguration = configuration;
        }

        public void SetInstallationDeviceId(string deviceId)
        {
            InstallationDeviceId = string.IsNullOrWhiteSpace(deviceId) ? null : deviceId.Trim();
        }

        public void SetOfflineEligible(bool offlineEligible)
        {
            OfflineEligible = offlineEligible;
        }

        public async Task ClearAuthenticationAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IsAuthenticated = false;
            CurrentProfile = null;
            LastBootstrap = null;
            OfflineEligible = false;
            AuthenticatedStudentState?.Clear();

            if (TokenStore != null)
            {
                await TokenStore.ClearAsync(cancellationToken).ConfigureAwait(false);
            }

            SessionRepository?.ClearSession();
            Router?.ClearStacks();
            NutriMindLog.Auth("Authentication cleared. Local progress/outbox preserved.");
        }

        public async Task HandleUnauthorizedAsync(CancellationToken cancellationToken = default)
        {
            await ClearAuthenticationAsync(cancellationToken).ConfigureAwait(false);
            if (Router != null)
            {
                await Router.HandleUnauthorizedAsync(cancellationToken).ConfigureAwait(false);
            }
            else if (SceneNavigator != null)
            {
                await SceneNavigator.LoadAsync(AppSceneId.Authentication, cancellationToken).ConfigureAwait(false);
            }
        }

        public Task<AppResult> ResetMockServerAsync(CancellationToken cancellationToken = default)
        {
            return RunResetExclusiveAsync(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (MockRuntimeState == null)
                {
                    return Task.FromResult(AppResult.Failure(
                        AppErrorCodes.ClientConfigurationError,
                        "Mock runtime state is not available."));
                }

                MockRuntimeState.ResetServerState();
                NutriMindLog.Runtime("Mock server state reset.");
                return Task.FromResult(AppResult.Success());
            }, cancellationToken);
        }

        public Task<AppResult> ResetLocalDatabaseAsync(CancellationToken cancellationToken = default)
        {
            return RunResetExclusiveAsync(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                await CancelActiveOperationsAsync().ConfigureAwait(false);

                string path = Database != null
                    ? Database.DatabaseFilePath
                    : NutriMindDatabase.GetDefaultDatabasePath();

                DisposeCompositionOnly();
                TryDeleteDatabaseFiles(path);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                DevelopmentMockAuthTokenStore.DeletePersistedFile();
#endif
                IsAuthenticated = false;
                CurrentProfile = null;
                LastBootstrap = null;
                LastClientConfiguration = null;
                InstallationDeviceId = null;
                OfflineEligible = false;
                AuthenticatedStudentState?.Clear();

                RecreateLifetimeCts();
                AppResult recompose = RecomposeInternal();
                if (recompose.IsFailure)
                {
                    return recompose;
                }

                // Intentional DB wipe → new installation UUID.
                if (InstallationRepository != null)
                {
                    AppResult<string> regenerated =
                        InstallationRepository.RegenerateDeviceIdForFullInstallReset();
                    if (regenerated.IsSuccess)
                    {
                        InstallationDeviceId = regenerated.Value;
                    }
                }

                // Caller token may have been LifetimeToken and is now cancelled — use the new lifetime.
                await LoadBootstrapSceneAsync(LifetimeToken).ConfigureAwait(false);
                NutriMindLog.Sqlite("Local database reset and recomposed.");
                return AppResult.Success();
            }, cancellationToken);
        }

        public Task<AppResult> FullInstallationResetAsync(CancellationToken cancellationToken = default)
        {
            return RunResetExclusiveAsync(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                await CancelActiveOperationsAsync().ConfigureAwait(false);

                string path = Database != null
                    ? Database.DatabaseFilePath
                    : NutriMindDatabase.GetDefaultDatabasePath();

                DisposeCompositionOnly();
                TryDeleteDatabaseFiles(path);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                DevelopmentMockAuthTokenStore.DeletePersistedFile();
#endif
                IsAuthenticated = false;
                CurrentProfile = null;
                LastBootstrap = null;
                LastClientConfiguration = null;
                InstallationDeviceId = null;
                OfflineEligible = false;
                ConfigurationError = null;
                AuthenticatedStudentState?.Clear();

                RecreateLifetimeCts();
                AppResult recompose = RecomposeInternal();
                if (recompose.IsFailure)
                {
                    return recompose;
                }

                MockRuntimeState?.ResetServerState();
                Router?.ClearStacks();

                if (InstallationRepository != null)
                {
                    AppResult<string> regenerated =
                        InstallationRepository.RegenerateDeviceIdForFullInstallReset();
                    if (regenerated.IsSuccess)
                    {
                        InstallationDeviceId = regenerated.Value;
                    }
                }

                await LoadBootstrapSceneAsync(LifetimeToken).ConfigureAwait(false);
                NutriMindLog.Runtime("Full installation reset completed.");
                return AppResult.Success();
            }, cancellationToken);
        }

        public Task<AppResult> RecomposeAsync(CancellationToken cancellationToken = default)
        {
            return RunResetExclusiveAsync(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                await CancelActiveOperationsAsync().ConfigureAwait(false);
                DisposeCompositionOnly();
                RecreateLifetimeCts();
                AppResult result = RecomposeInternal();
                await Task.CompletedTask.ConfigureAwait(false);
                return result;
            }, cancellationToken);
        }

        private async Task<AppResult> RunResetExclusiveAsync(
            Func<Task<AppResult>> action,
            CancellationToken cancellationToken)
        {
            await _resetGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                return await action().ConfigureAwait(false);
            }
            finally
            {
                _resetGate.Release();
            }
        }

        private async Task CancelActiveOperationsAsync()
        {
            try
            {
                _lifetimeCts?.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // ignore
            }

            await Task.Yield();
        }

        private void RecreateLifetimeCts()
        {
            try
            {
                _lifetimeCts?.Dispose();
            }
            catch (ObjectDisposedException)
            {
                // ignore
            }

            _lifetimeCts = new CancellationTokenSource();
        }

        private void DisposeCompositionOnly()
        {
            _compositionRoot?.Dispose();
            _compositionRoot = null;
            _isInitialized = false;
            _missionLaunchService = null;
        }

        private AppResult RecomposeInternal()
        {
            try
            {
                _composeStarted = true;
                _compositionRoot = new AppCompositionRoot(_runtimeOptions);
                AppResult compose = _compositionRoot.Compose();
                if (compose.IsFailure)
                {
                    ConfigurationError = compose.Error;
                    NutriMindLog.RuntimeError(
                        "Recompose failed: " + compose.Error.Code + " — " + compose.Error.Message);
                    _isInitialized = true;
                    return compose;
                }

                ConfigurationError = _compositionRoot.ComposeError;
                _isInitialized = true;
                NutriMindLog.Runtime("AppLifetime recomposed. mode=" + _runtimeOptions.Mode + ".");
                return AppResult.Success();
            }
            catch (Exception exception)
            {
                ConfigurationError = AppError.FromException(exception);
                _isInitialized = true;
                NutriMindLog.RuntimeError("AppLifetime recompose threw: " + exception.GetType().Name);
                return AppResult.Failure(ConfigurationError);
            }
        }

        private async Task LoadBootstrapSceneAsync(CancellationToken cancellationToken)
        {
            await UnityMainThread.SwitchToMainAsync(cancellationToken).ConfigureAwait(false);
            if (SceneNavigator != null)
            {
                await SceneNavigator.LoadAsync(AppSceneId.Bootstrap, cancellationToken).ConfigureAwait(false);
            }
        }

        private static void TryDeleteDatabaseFiles(string path)
        {
            TryDelete(path);
            TryDelete(path + "-wal");
            TryDelete(path + "-shm");
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception exception)
            {
                NutriMindLog.SqliteWarning("Could not delete " + path + ": " + exception.GetType().Name);
            }
        }

        private void InitializeComposition()
        {
            try
            {
                _composeStarted = true;
                _compositionRoot = new AppCompositionRoot(_runtimeOptions);
                AppResult compose = _compositionRoot.Compose();
                if (compose.IsFailure)
                {
                    ConfigurationError = compose.Error;
                    NutriMindLog.RuntimeError(
                        "Composition failed: " + compose.Error.Code + " — " + compose.Error.Message);
                    _isInitialized = true;
                    return;
                }

                ConfigurationError = null;
                _isInitialized = true;
                if (_compositionRoot.ComposeError != null)
                {
                    ConfigurationError = _compositionRoot.ComposeError;
                }

                NutriMindLog.Runtime(
                    "AppLifetime ready. mode=" + _runtimeOptions.Mode + ".");
            }
            catch (Exception exception)
            {
                ConfigurationError = AppError.FromException(exception);
                NutriMindLog.RuntimeError("AppLifetime composition threw: " + exception.GetType().Name);
                _isInitialized = true;
            }
        }

        private void DisposeLifetime()
        {
            try
            {
                _lifetimeCts?.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // already disposed
            }

            _lifetimeCts?.Dispose();
            _lifetimeCts = null;
            _compositionRoot?.Dispose();
            _compositionRoot = null;
            _isInitialized = false;
            _composeStarted = false;
            _resetGate.Dispose();
        }
    }
}
