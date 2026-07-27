using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NutriMind.App.Features;
using NutriMind.App.Routing;
using NutriMind.App.State;
using NutriMind.Core.Data;
using NutriMind.Core.Networking;
using NutriMind.Core.Persistence;
using NutriMind.Core.Sync;
using NutriMind.Core.Utilities;
using NetworkSyncPushEvent = NutriMind.Core.Networking.SyncPushEvent;
using LocalSyncPushEvent = NutriMind.Core.Sync.SyncPushEvent;

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

        /// <summary>
        /// Deterministic in Mock mode for mutation/request UUIDs; system GUIDs otherwise.
        /// </summary>
        public IIdGenerator IdGenerator { get; private set; }

        /// <summary>
        /// Always system-generated. Installation identity must not reuse the Mock mutation sequence.
        /// </summary>
        public IIdGenerator InstallationIdGenerator { get; private set; }

        public NutriMindDatabase Database { get; private set; }

        public IInstallationRepository InstallationRepository { get; private set; }

        public ILocalSessionRepository SessionRepository { get; private set; }

        public IResourceCacheRepository ResourceCacheRepository { get; private set; }

        public IMissionProgressRepository MissionProgressRepository { get; private set; }

        public IOutboxRepository OutboxRepository { get; private set; }

        public IAnnouncementReadRepository AnnouncementReadRepository { get; private set; }

        public IIdempotentRequestRepository IdempotentRequestRepository { get; private set; }

        public ILocalProgressWriter LocalProgressWriter { get; private set; }

        public IOutboxPayloadSerializer OutboxPayloadSerializer { get; private set; }

        public IAuthTokenStore TokenStore { get; private set; }

        public IConnectivityService Connectivity { get; private set; }

        public IMockRuntimeState MockRuntimeState { get; private set; }

        public MockServerState MockServerState { get; private set; }

        public IStudentGateway Gateway { get; private set; }

        public ISyncPushGateway SyncPushGateway { get; private set; }

        public SyncCoordinator SyncCoordinator { get; private set; }

        public IAppSceneNavigator SceneNavigator { get; private set; }

        public IAppRouter Router { get; private set; }

        public AuthenticatedStudentState AuthenticatedStudentState { get; private set; }

        public ILocalSettingsStore LocalSettingsStore { get; private set; }

        public AppError ComposeError { get; private set; }

        public bool IsDisposed => _disposed;

        public AppResult Compose()
        {
            DisposeOwned();
            _disposed = false;

            if (_options.Mode == NutriMindRuntimeMode.Mock)
            {
                Clock = new FixedMockClock();
                IdGenerator = new DeterministicMockIdGenerator();
            }
            else
            {
                Clock = new SystemAppClock();
                IdGenerator = new SystemIdGenerator();
            }

            // Installation UUID stays independent of Mock deterministic mutation IDs.
            InstallationIdGenerator = new SystemIdGenerator();

            AppResult tokenResult = CreateTokenStore();
            if (tokenResult.IsFailure)
            {
                ComposeError = tokenResult.Error;
                return tokenResult;
            }

            Connectivity = new MockConnectivityService(startOnline: !_options.StartOffline);
            if (_options.StartOffline)
            {
                Connectivity.SetState(ConnectivityState.Offline);
            }

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

            InstallationRepository = new SqliteInstallationRepository(
                Database,
                InstallationIdGenerator,
                Clock);
            SessionRepository = new SqliteLocalSessionRepository(Database);
            ResourceCacheRepository = new SqliteResourceCacheRepository(Database);
            MissionProgressRepository = new SqliteMissionProgressRepository(Database);
            OutboxRepository = new SqliteOutboxRepository(Database);
            AnnouncementReadRepository = new SqliteAnnouncementReadRepository(Database);
            IdempotentRequestRepository = new SqliteIdempotentRequestRepository(Database);
            LocalProgressWriter = new LocalProgressWriter(Database, Clock);
            OutboxPayloadSerializer = new OutboxPayloadSerializer();

            AppResult gatewayResult = CreateGateway();
            if (gatewayResult.IsFailure)
            {
                ComposeError = gatewayResult.Error;
                return gatewayResult;
            }

            SyncPushGateway = new StudentGatewaySyncPushAdapter(Gateway, OutboxPayloadSerializer, OutboxRepository);
            SyncCoordinator = new SyncCoordinator(
                OutboxRepository,
                SyncPushGateway,
                IdGenerator,
                Clock,
                OutboxPayloadSerializer);
            SceneNavigator = new AppSceneNavigator();
            Router = new AppRouter(SceneNavigator);
            AuthenticatedStudentState = new AuthenticatedStudentState();
            LocalSettingsStore = new PlayerPrefsLocalSettingsStore();
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

        private AppResult CreateTokenStore()
        {
            switch (_options.Mode)
            {
                case NutriMindRuntimeMode.Mock:
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    TokenStore = new DevelopmentMockAuthTokenStore();
                    NutriMindLog.Runtime("Wired DevelopmentMockAuthTokenStore (Mock Development).");
                    return AppResult.Success();
#else
                    TokenStore = new InMemoryMockAuthTokenStore();
                    NutriMindLog.RuntimeWarning(
                        "Mock non-development build uses in-memory token store; offline cold restart is unavailable.");
                    return AppResult.Success();
#endif

                case NutriMindRuntimeMode.DevelopmentServer:
                case NutriMindRuntimeMode.ProductionServer:
                    TokenStore = new UnconfiguredAuthTokenStore(_options.Mode);
                    ComposeError = AppError.Configuration(
                        _options.Mode + " secure token store is not configured for this client build.");
                    NutriMindLog.RuntimeError(
                        "CLIENT_CONFIGURATION_ERROR: " + _options.Mode + " has no secure token store.");
                    // Surrounding services may still compose; auth calls fail clearly.
                    return AppResult.Success();

                default:
                    TokenStore = new InMemoryMockAuthTokenStore();
                    return AppResult.Failure(AppError.Configuration("Unknown NutriMindRuntimeMode."));
            }
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
            MockServerState = new MockServerState();
            MockRuntimeState = new MockRuntimeState(
                _options.MockScenario,
                Connectivity,
                MockServerState);

            Gateway = new MockStudentGateway(
                _options,
                Connectivity,
                TokenStore,
                fixtures: null,
                clock: Clock,
                ids: IdGenerator,
                state: MockServerState,
                mockRuntime: MockRuntimeState);
            NutriMindLog.Runtime("Wired MockStudentGateway with shared IMockRuntimeState.");
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

        private void DisposeOwned()
        {
            Router = null;
            SceneNavigator = null;
            SyncCoordinator = null;
            SyncPushGateway = null;
            Gateway = null;
            MockRuntimeState = null;
            MockServerState = null;
            LocalProgressWriter = null;
            OutboxPayloadSerializer = null;
            IdempotentRequestRepository = null;
            AnnouncementReadRepository = null;
            OutboxRepository = null;
            MissionProgressRepository = null;
            ResourceCacheRepository = null;
            SessionRepository = null;
            InstallationRepository = null;
            Connectivity = null;
            TokenStore = null;
            LocalSettingsStore = null;
            AuthenticatedStudentState?.Clear();
            AuthenticatedStudentState = null;
            InstallationIdGenerator = null;
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
    /// Explicit non-configured token store for DevelopmentServer/ProductionServer until a
    /// platform-secure store exists. Not an insecure fallback.
    /// </summary>
    public sealed class UnconfiguredAuthTokenStore : IAuthTokenStore
    {
        private readonly NutriMindRuntimeMode _mode;

        public UnconfiguredAuthTokenStore(NutriMindRuntimeMode mode)
        {
            _mode = mode;
        }

        public bool HasToken => false;

        public Task<string> ReadAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<string>(null);
        }

        public Task WriteAsync(string token, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromException(new InvalidOperationException(
                _mode + " secure token store is not configured."));
        }

        public Task ClearAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Adapts <see cref="IStudentGateway.PushSyncAsync"/> for <see cref="SyncCoordinator"/>.
    /// Parses versioned outbox envelopes losslessly; malformed rows are deferred/rejected with
    /// stable codes and preserved for inspection.
    /// </summary>
    public sealed class StudentGatewaySyncPushAdapter : ISyncPushGateway
    {
        private readonly IStudentGateway _gateway;
        private readonly IOutboxPayloadSerializer _serializer;
        private readonly IOutboxRepository _outboxRepository;

        public StudentGatewaySyncPushAdapter(
            IStudentGateway gateway,
            IOutboxPayloadSerializer serializer = null,
            IOutboxRepository outboxRepository = null)
        {
            _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
            _serializer = serializer ?? new OutboxPayloadSerializer();
            _outboxRepository = outboxRepository;
        }

        public async Task<AppResult<SyncPushResult>> SyncPushBatchAsync(
            SyncPushBatchRequest request,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            AppResult<MappedBatch> mapped = MapEvents(request);
            if (mapped.IsFailure)
            {
                return AppResult<SyncPushResult>.Failure(mapped.Error);
            }

            if (mapped.Value.NetworkEvents.Count == 0)
            {
                return AppResult<SyncPushResult>.Success(new SyncPushResult
                {
                    BatchUuid = request?.BatchUuid,
                    ServerRevision = 0,
                    Events = mapped.Value.LocalResults.ToArray()
                });
            }

            var networkRequest = new SyncPushRequest
            {
                BatchUuid = request?.BatchUuid,
                ClientId = "unity-client",
                LastKnownServerRevision = 0,
                Events = mapped.Value.NetworkEvents
            };

            AppResult<SyncPushResult> push =
                await _gateway.PushSyncAsync(networkRequest, cancellationToken).ConfigureAwait(false);
            if (push.IsFailure)
            {
                return push;
            }

            var combined = new List<SyncPushEventResult>();
            if (mapped.Value.LocalResults.Count > 0)
            {
                combined.AddRange(mapped.Value.LocalResults);
            }

            if (push.Value?.Events != null)
            {
                combined.AddRange(push.Value.Events);
            }

            return AppResult<SyncPushResult>.Success(new SyncPushResult
            {
                BatchUuid = push.Value?.BatchUuid ?? request?.BatchUuid,
                ServerRevision = push.Value?.ServerRevision ?? 0,
                AcceptedCount = push.Value?.AcceptedCount ?? 0,
                DuplicateCount = push.Value?.DuplicateCount ?? 0,
                RejectedCount = (push.Value?.RejectedCount ?? 0) + mapped.Value.RejectedCount,
                DeferredCount = (push.Value?.DeferredCount ?? 0) + mapped.Value.DeferredCount,
                Events = combined
            });
        }

        private AppResult<MappedBatch> MapEvents(SyncPushBatchRequest request)
        {
            var batch = new MappedBatch();
            if (request?.Events == null || request.Events.Count == 0)
            {
                return AppResult<MappedBatch>.Success(batch);
            }

            string attemptUtc = DateTimeOffset.UtcNow.ToUniversalTime().ToString("o");
            for (int i = 0; i < request.Events.Count; i++)
            {
                LocalSyncPushEvent source = request.Events[i];
                if (source == null || string.IsNullOrWhiteSpace(source.EventUuid))
                {
                    continue;
                }

                AppResult<OutboxPayloadEnvelopeV1> envelope =
                    _serializer.Deserialize(source.PayloadJson);
                if (envelope.IsFailure)
                {
                    string state = envelope.Error != null
                                   && envelope.Error.Code == AppErrorCodes.SyncPayloadVersionUnsupported
                        ? OutboxEventState.Deferred
                        : OutboxEventState.Rejected;
                    string code = envelope.Error?.Code ?? AppErrorCodes.SyncPayloadInvalid;
                    MarkInvalid(source.EventUuid, state, code, attemptUtc);
                    batch.LocalResults.Add(new SyncPushEventResult
                    {
                        EventUuid = source.EventUuid,
                        Status = state,
                        ErrorCode = code
                    });
                    if (state == OutboxEventState.Deferred)
                    {
                        batch.DeferredCount++;
                    }
                    else
                    {
                        batch.RejectedCount++;
                    }

                    continue;
                }

                AppResult<NetworkSyncPushEvent> mapped =
                    _serializer.MapToNetworkEvent(source, envelope.Value);
                if (mapped.IsFailure)
                {
                    string code = mapped.Error?.Code ?? AppErrorCodes.SyncPayloadInvalid;
                    MarkInvalid(source.EventUuid, OutboxEventState.Deferred, code, attemptUtc);
                    batch.LocalResults.Add(new SyncPushEventResult
                    {
                        EventUuid = source.EventUuid,
                        Status = OutboxEventState.Deferred,
                        ErrorCode = code
                    });
                    batch.DeferredCount++;
                    continue;
                }

                batch.NetworkEvents.Add(mapped.Value);
            }

            return AppResult<MappedBatch>.Success(batch);
        }

        private void MarkInvalid(string eventUuid, string state, string errorCode, string attemptUtc)
        {
            if (_outboxRepository == null)
            {
                return;
            }

            _outboxRepository.ApplyPushResult(
                eventUuid,
                state,
                errorCode,
                attemptUtc,
                serverRevision: null);
            NutriMindLog.SyncWarning(
                "Preserved outbox event " + eventUuid + " as " + state + " (" + errorCode + ").");
        }

        private sealed class MappedBatch
        {
            public List<NetworkSyncPushEvent> NetworkEvents { get; } = new List<NetworkSyncPushEvent>();
            public List<SyncPushEventResult> LocalResults { get; } = new List<SyncPushEventResult>();
            public int DeferredCount { get; set; }
            public int RejectedCount { get; set; }
        }
    }
}
