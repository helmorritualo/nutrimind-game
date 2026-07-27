using System;
using System.Threading.Tasks;
using NutriMind.App.Routing;
using NutriMind.Core.Data;
using NutriMind.Core.Networking;
using NutriMind.Core.Persistence;
using NutriMind.Core.Sync;
using NutriMind.Core.Utilities;

namespace NutriMind.App.Composition
{
    /// <summary>
    /// Wires Core services for the active runtime mode.
    /// Mock mode resolves MockStudentGateway when present; Development/Production never fall back to Mock.
    /// </summary>
    public sealed class AppCompositionRoot : IDisposable
    {
        private readonly NutriMindRuntimeOptions _options;
        private bool _disposed;

        public AppCompositionRoot(NutriMindRuntimeOptions options)
        {
            _options = (options ?? NutriMindRuntimeOptions.CreateDefaults()).Clone();
            _options.Clamp();
        }

        public NutriMindRuntimeOptions Options => _options;

        public IAppClock Clock { get; private set; }

        public IIdGenerator IdGenerator { get; private set; }

        public NutriMindDatabase Database { get; private set; }

        public IInstallationRepository InstallationRepository { get; private set; }

        public ILocalSessionRepository SessionRepository { get; private set; }

        public IResourceCacheRepository ResourceCacheRepository { get; private set; }

        public IOutboxRepository OutboxRepository { get; private set; }

        public IAuthTokenStore TokenStore { get; private set; }

        public IConnectivityService Connectivity { get; private set; }

        public IStudentGateway Gateway { get; private set; }

        public ISyncPushGateway SyncPushGateway { get; private set; }

        public SyncCoordinator SyncCoordinator { get; private set; }

        public IAppSceneNavigator SceneNavigator { get; private set; }

        public IAppRouter Router { get; private set; }

        public AppError ComposeError { get; private set; }

        public AppResult Compose()
        {
            DisposeOwned();

            Clock = new SystemAppClock();
            IdGenerator = new SystemIdGenerator();
            TokenStore = new InMemoryMockAuthTokenStore();
            Connectivity = new MockConnectivityService(startOnline: !_options.StartOffline);

            Database = new NutriMindDatabase(Clock);
            AppResult open = Database.Open();
            if (open.IsFailure)
            {
                ComposeError = open.Error;
                return open;
            }

            if (_options.ResetMockDatabaseOnStart && _options.Mode == NutriMindRuntimeMode.Mock)
            {
                ResetDatabaseFilesForMock();
                open = Database.Open();
                if (open.IsFailure)
                {
                    ComposeError = open.Error;
                    return open;
                }
            }

            InstallationRepository = new SqliteInstallationRepository(Database, IdGenerator, Clock);
            SessionRepository = new SqliteLocalSessionRepository(Database);
            ResourceCacheRepository = new SqliteResourceCacheRepository(Database);
            OutboxRepository = new SqliteOutboxRepository(Database);

            AppResult gatewayResult = CreateGateway();
            if (gatewayResult.IsFailure)
            {
                ComposeError = gatewayResult.Error;
                return gatewayResult;
            }

            SyncPushGateway = new StudentGatewaySyncPushAdapter(Gateway);
            SyncCoordinator = new SyncCoordinator(OutboxRepository, SyncPushGateway, IdGenerator, Clock);
            SceneNavigator = new AppSceneNavigator();
            Router = new AppRouter(SceneNavigator);
            return AppResult.Success();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            DisposeOwned();
        }

        private AppResult CreateGateway()
        {
            switch (_options.Mode)
            {
                case NutriMindRuntimeMode.Mock:
                {
                    AppResult mock = CreateMockGateway();
                    if (mock.IsFailure)
                    {
                        ComposeError = mock.Error;
                    }
                    else
                    {
                        ComposeError = null;
                    }

                    return mock;
                }

                case NutriMindRuntimeMode.DevelopmentServer:
                    Gateway = new UnconfiguredStudentGateway(
                        "DevelopmentServer HTTP student gateway is not configured. " +
                        "NutriMind does not silently fall back to Mock mode.");
                    NutriMindLog.RuntimeError(
                        "CLIENT_CONFIGURATION_ERROR: DevelopmentServer mode has no HTTP gateway.");
                    ComposeError = AppError.Configuration(
                        "DevelopmentServer mode is not configured for this client build.");
                    // Still wire surrounding services; gateway calls fail with CLIENT_CONFIGURATION_ERROR.
                    return AppResult.Success();

                case NutriMindRuntimeMode.ProductionServer:
                    Gateway = new UnconfiguredStudentGateway(
                        "ProductionServer HTTP student gateway is not configured. " +
                        "NutriMind does not silently fall back to Mock mode.");
                    NutriMindLog.RuntimeError(
                        "CLIENT_CONFIGURATION_ERROR: ProductionServer mode has no HTTP gateway.");
                    ComposeError = AppError.Configuration(
                        "ProductionServer mode is not configured for this client build.");
                    return AppResult.Success();

                default:
                    Gateway = new UnconfiguredStudentGateway("Unknown runtime mode.");
                    return AppResult.Failure(AppError.Configuration("Unknown NutriMindRuntimeMode."));
            }
        }

        private AppResult CreateMockGateway()
        {
            Gateway = new MockStudentGateway(
                _options,
                Connectivity,
                TokenStore,
                fixtures: null,
                clock: Clock,
                ids: IdGenerator);
            NutriMindLog.Runtime("Wired MockStudentGateway.");
            return AppResult.Success();
        }

        private void ResetDatabaseFilesForMock()
        {
            try
            {
                Database?.Dispose();
            }
            catch (Exception)
            {
                // ignore
            }

            string path = NutriMindDatabase.GetDefaultDatabasePath();
            TryDelete(path);
            TryDelete(path + "-shm");
            TryDelete(path + "-wal");
            Database = new NutriMindDatabase(Clock);
            NutriMindLog.Sqlite("Reset mock database files on start.");
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.Delete(path);
                }
            }
            catch (Exception exception)
            {
                NutriMindLog.SqliteWarning("Could not delete " + path + ": " + exception.GetType().Name);
            }
        }

        private void DisposeOwned()
        {
            Router = null;
            SceneNavigator = null;
            SyncCoordinator = null;
            SyncPushGateway = null;
            Gateway = null;
            OutboxRepository = null;
            ResourceCacheRepository = null;
            SessionRepository = null;
            InstallationRepository = null;
            Connectivity = null;
            TokenStore = null;
            IdGenerator = null;
            Clock = null;

            if (Database != null)
            {
                Database.Dispose();
                Database = null;
            }
        }
    }

    /// <summary>
    /// Adapts <see cref="IStudentGateway.PushSyncAsync"/> for <see cref="SyncCoordinator"/>.
    /// </summary>
    public sealed class StudentGatewaySyncPushAdapter : ISyncPushGateway
    {
        private readonly IStudentGateway _gateway;

        public StudentGatewaySyncPushAdapter(IStudentGateway gateway)
        {
            _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
        }

        public Task<AppResult<SyncPushResult>> SyncPushBatchAsync(
            SyncPushBatchRequest request,
            System.Threading.CancellationToken cancellationToken = default)
        {
            var networkRequest = new SyncPushRequest
            {
                BatchUuid = request?.BatchUuid,
                ClientId = "unity-client",
                LastKnownServerRevision = 0,
                Events = MapEvents(request)
            };

            return _gateway.PushSyncAsync(networkRequest, cancellationToken);
        }

        private static System.Collections.Generic.IReadOnlyList<NutriMind.Core.Networking.SyncPushEvent> MapEvents(
            SyncPushBatchRequest request)
        {
            if (request?.Events == null || request.Events.Count == 0)
            {
                return Array.Empty<NutriMind.Core.Networking.SyncPushEvent>();
            }

            var mapped = new NutriMind.Core.Networking.SyncPushEvent[request.Events.Count];
            for (int i = 0; i < request.Events.Count; i++)
            {
                NutriMind.Core.Sync.SyncPushEvent source = request.Events[i];
                DateTimeOffset createdAt = DateTimeOffset.UtcNow;
                if (source != null
                    && !string.IsNullOrWhiteSpace(source.ClientCreatedUtc)
                    && DateTimeOffset.TryParse(source.ClientCreatedUtc, out DateTimeOffset parsed))
                {
                    createdAt = parsed;
                }

                mapped[i] = new NutriMind.Core.Networking.SyncPushEvent
                {
                    EventUuid = source?.EventUuid,
                    EventType = source?.EventType,
                    GradeId = source?.GradeId,
                    SubjectId = source?.SubjectId,
                    TermId = source?.TermId,
                    MissionId = source?.MissionId,
                    AreaId = source?.AreaId,
                    LocalSequence = source != null ? (int)source.LocalSequence : 0,
                    ClientCreatedAt = createdAt
                };
            }

            return mapped;
        }
    }
}
