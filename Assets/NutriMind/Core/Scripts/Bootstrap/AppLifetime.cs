using System;
using System.Threading;
using System.Threading.Tasks;
using NutriMind.App.Composition;
using NutriMind.App.Routing;
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
        private bool _isInitialized;

        public static AppLifetime Instance => _instance;

        public static bool HasInstance => _instance != null;

        public NutriMindRuntimeOptions RuntimeOptions => _runtimeOptions;

        public AppCompositionRoot Composition => _compositionRoot;

        public NutriMindDatabase Database => _compositionRoot?.Database;

        public IStudentGateway Gateway => _compositionRoot?.Gateway;

        public IAuthTokenStore TokenStore => _compositionRoot?.TokenStore;

        public IConnectivityService Connectivity => _compositionRoot?.Connectivity;

        public SyncCoordinator SyncCoordinator => _compositionRoot?.SyncCoordinator;

        public IAppSceneNavigator SceneNavigator => _compositionRoot?.SceneNavigator;

        public IAppRouter Router => _compositionRoot?.Router;

        public IInstallationRepository InstallationRepository => _compositionRoot?.InstallationRepository;

        public ILocalSessionRepository SessionRepository => _compositionRoot?.SessionRepository;

        public IResourceCacheRepository ResourceCacheRepository => _compositionRoot?.ResourceCacheRepository;

        public IOutboxRepository OutboxRepository => _compositionRoot?.OutboxRepository;

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

        public bool IsReady => _isInitialized && _compositionRoot != null;

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
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }

            DisposeLifetime();
        }

        public void SetRuntimeOptions(NutriMindRuntimeOptions options)
        {
            if (options == null)
            {
                return;
            }

            options.Clamp();
            _runtimeOptions = options;
        }

        public void SetAuthenticated(StudentProfile profile, bool authenticated)
        {
            IsAuthenticated = authenticated;
            CurrentProfile = profile;
        }

        public void SetBootstrap(BootstrapSnapshot snapshot)
        {
            LastBootstrap = snapshot;
        }

        public void SetClientConfiguration(ClientConfiguration configuration)
        {
            LastClientConfiguration = configuration;
        }

        public void SetInstallationDeviceId(string deviceId)
        {
            InstallationDeviceId = string.IsNullOrWhiteSpace(deviceId) ? null : deviceId.Trim();
        }

        public async Task ClearAuthenticationAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IsAuthenticated = false;
            CurrentProfile = null;
            LastBootstrap = null;

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

        private void InitializeComposition()
        {
            try
            {
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
        }
    }
}
