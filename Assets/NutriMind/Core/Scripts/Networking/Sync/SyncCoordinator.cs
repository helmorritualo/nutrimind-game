using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NutriMind.Core.Data;
using NutriMind.Core.Persistence;
using NutriMind.Core.Utilities;

namespace NutriMind.Core.Sync
{
    /// <summary>
    /// Minimal sync-push dependency so SyncCoordinator can run before the full student gateway exists.
    /// Quiz Portal attempts do not use this path.
    /// </summary>
    public interface ISyncPushGateway
    {
        Task<AppResult<SyncPushResult>> SyncPushBatchAsync(
            SyncPushBatchRequest request,
            CancellationToken cancellationToken = default);
    }

    public sealed class SyncPushBatchRequest
    {
        public string BatchUuid { get; set; }
        public IReadOnlyList<SyncPushEvent> Events { get; set; } = Array.Empty<SyncPushEvent>();
    }

    public sealed class SyncPushEvent
    {
        public string EventUuid { get; set; }
        public long LocalSequence { get; set; }
        public string EventType { get; set; }
        public string GradeId { get; set; }
        public string SubjectId { get; set; }
        public string TermId { get; set; }
        public string MissionId { get; set; }
        public string AreaId { get; set; }
        public string PayloadJson { get; set; }
        public string ClientCreatedUtc { get; set; }
    }

    /// <summary>
    /// Gameplay sync outbox coordinator. Recovers interrupted sending rows, batches ascending
    /// local_sequence (max 100), reuses batch UUID, and applies per-event outcomes.
    /// </summary>
    public sealed class SyncCoordinator
    {
        private readonly IOutboxRepository _outboxRepository;
        private readonly ISyncPushGateway _gateway;
        private readonly IIdGenerator _idGenerator;
        private readonly IAppClock _clock;
        private readonly IOutboxPayloadSerializer _payloadSerializer;
        private readonly object _gate = new object();
        private readonly SemaphoreSlim _pushGate = new SemaphoreSlim(1, 1);
        private string _activeBatchUuid;

        public SyncCoordinator(
            IOutboxRepository outboxRepository,
            ISyncPushGateway gateway,
            IIdGenerator idGenerator,
            IAppClock clock = null,
            IOutboxPayloadSerializer payloadSerializer = null)
        {
            _outboxRepository = outboxRepository ?? throw new ArgumentNullException(nameof(outboxRepository));
            _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
            _idGenerator = idGenerator ?? throw new ArgumentNullException(nameof(idGenerator));
            _clock = clock ?? new SystemAppClock();
            _payloadSerializer = payloadSerializer ?? new OutboxPayloadSerializer();
        }

        public string ActiveBatchUuid
        {
            get
            {
                lock (_gate)
                {
                    return _activeBatchUuid;
                }
            }
        }

        public async Task<AppResult<SyncPushResult>> PushPendingAsync(
            ClientConfiguration configuration = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!await _pushGate.WaitAsync(0, cancellationToken).ConfigureAwait(false))
            {
                return AppResult<SyncPushResult>.Failure(
                    AppErrorCodes.SyncInProgress,
                    "A sync push is already in progress.",
                    isRetryable: true);
            }

            try
            {
                return await PushPendingCoreAsync(configuration, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _pushGate.Release();
            }
        }

        private async Task<AppResult<SyncPushResult>> PushPendingCoreAsync(
            ClientConfiguration configuration,
            CancellationToken cancellationToken)
        {
            ClientConfiguration config = configuration ?? new ClientConfiguration();
            int maxEvents = Math.Max(1, config.SyncMaxEventsPerBatch);
            int maxRequestBytes = Math.Max(1024, config.SyncMaxRequestBytes);
            int maxEventPayloadBytes = Math.Max(256, config.SyncMaxEventPayloadBytes);

            AppResult recover = _outboxRepository.RecoverInterruptedSending();
            if (recover.IsFailure)
            {
                return AppResult<SyncPushResult>.Failure(recover.Error);
            }

            AppResult<IReadOnlyList<SyncOutboxRecord>> pending =
                _outboxRepository.GetPushableAscending(maxEvents);
            if (pending.IsFailure)
            {
                return AppResult<SyncPushResult>.Failure(pending.Error);
            }

            if (pending.Value == null || pending.Value.Count == 0)
            {
                NutriMindLog.Sync("No pending/deferred outbox events.");
                return AppResult<SyncPushResult>.Success(new SyncPushResult
                {
                    BatchUuid = null,
                    ServerRevision = 0,
                    Events = Array.Empty<SyncPushEventResult>()
                });
            }

            List<SyncOutboxRecord> batch = BuildBatch(
                pending.Value,
                maxEvents,
                maxRequestBytes,
                maxEventPayloadBytes,
                out List<SyncOutboxRecord> oversized);

            string attemptUtc = _clock.UtcNow.ToUniversalTime().ToString("o");
            foreach (SyncOutboxRecord oversizedEvent in oversized)
            {
                _outboxRepository.ApplyPushResult(
                    oversizedEvent.EventUuid,
                    OutboxEventState.Deferred,
                    AppErrorCodes.SyncBatchTooLarge,
                    attemptUtc,
                    serverRevision: null);
                NutriMindLog.SyncWarning(
                    "Deferred oversized outbox event " + oversizedEvent.EventUuid + ".");
            }

            batch = FilterInvalidPayloads(batch, attemptUtc);

            if (batch.Count == 0)
            {
                return AppResult<SyncPushResult>.Success(new SyncPushResult
                {
                    BatchUuid = null,
                    ServerRevision = 0,
                    Events = Array.Empty<SyncPushEventResult>()
                });
            }

            string batchUuid = GetOrCreateBatchUuid();
            var eventUuids = new List<string>(batch.Count);
            var pushEvents = new List<SyncPushEvent>(batch.Count);
            foreach (SyncOutboxRecord row in batch)
            {
                eventUuids.Add(row.EventUuid);
                pushEvents.Add(ToPushEvent(row));
            }

            AppResult markSending = _outboxRepository.MarkSending(eventUuids, attemptUtc);
            if (markSending.IsFailure)
            {
                return AppResult<SyncPushResult>.Failure(markSending.Error);
            }

            NutriMindLog.Sync(
                "Pushing batch " + batchUuid + " with " + batch.Count + " events.");

            AppResult<SyncPushResult> pushResult;
            try
            {
                pushResult = await _gateway.SyncPushBatchAsync(
                    new SyncPushBatchRequest
                    {
                        BatchUuid = batchUuid,
                        Events = pushEvents
                    },
                    cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                RestorePending(eventUuids, attemptUtc, AppErrorCodes.NetworkTimeout);
                throw;
            }
            catch (Exception exception)
            {
                NutriMindLog.SyncError("Sync push threw: " + exception.GetType().Name);
                RestorePending(eventUuids, attemptUtc, AppErrorCodes.ClientInternalError);
                return AppResult<SyncPushResult>.Failure(AppError.FromException(exception));
            }

            if (pushResult.IsFailure)
            {
                string errorCode = pushResult.Error != null
                    ? pushResult.Error.Code
                    : AppErrorCodes.InternalError;
                if (pushResult.Error != null && pushResult.Error.IsNetworkError)
                {
                    NutriMindLog.SyncWarning("Sync push network failure; leaving events recoverable.");
                    RestorePending(eventUuids, attemptUtc, errorCode);
                }
                else
                {
                    RestorePending(eventUuids, attemptUtc, errorCode);
                }

                return pushResult;
            }

            ApplyEventResults(pushResult.Value, attemptUtc);
            ClearActiveBatchUuid();
            NutriMindLog.Sync(
                "Sync batch " + batchUuid + " applied. revision=" + pushResult.Value.ServerRevision);
            return pushResult;
        }

        private List<SyncOutboxRecord> BuildBatch(
            IReadOnlyList<SyncOutboxRecord> candidates,
            int maxEvents,
            int maxRequestBytes,
            int maxEventPayloadBytes,
            out List<SyncOutboxRecord> oversized)
        {
            var batch = new List<SyncOutboxRecord>();
            oversized = new List<SyncOutboxRecord>();
            int usedBytes = EstimateBatchEnvelopeBytes();

            foreach (SyncOutboxRecord candidate in candidates)
            {
                if (batch.Count >= maxEvents)
                {
                    break;
                }

                int payloadBytes = Encoding.UTF8.GetByteCount(candidate.PayloadJson ?? string.Empty);
                if (payloadBytes > maxEventPayloadBytes)
                {
                    oversized.Add(candidate);
                    continue;
                }

                int eventBytes = EstimateEventBytes(candidate);
                if (usedBytes + eventBytes > maxRequestBytes)
                {
                    if (batch.Count == 0)
                    {
                        oversized.Add(candidate);
                    }

                    break;
                }

                batch.Add(candidate);
                usedBytes += eventBytes;
            }

            return batch;
        }

        private List<SyncOutboxRecord> FilterInvalidPayloads(
            List<SyncOutboxRecord> batch,
            string attemptUtc)
        {
            var valid = new List<SyncOutboxRecord>(batch.Count);
            foreach (SyncOutboxRecord row in batch)
            {
                AppResult<OutboxPayloadEnvelopeV1> parsed =
                    _payloadSerializer.Deserialize(row.PayloadJson);
                if (parsed.IsSuccess)
                {
                    valid.Add(row);
                    continue;
                }

                string code = parsed.Error?.Code ?? AppErrorCodes.SyncPayloadInvalid;
                string state = code == AppErrorCodes.SyncPayloadVersionUnsupported
                    ? OutboxEventState.Deferred
                    : OutboxEventState.Rejected;
                _outboxRepository.ApplyPushResult(
                    row.EventUuid,
                    state,
                    code,
                    attemptUtc,
                    serverRevision: null);
                NutriMindLog.SyncWarning(
                    "Preserved outbox event " + row.EventUuid + " as " + state + " (" + code + ").");
            }

            return valid;
        }

        private void ApplyEventResults(SyncPushResult result, string attemptUtc)
        {
            if (result == null || result.Events == null)
            {
                return;
            }

            foreach (SyncPushEventResult eventResult in result.Events)
            {
                if (eventResult == null || string.IsNullOrWhiteSpace(eventResult.EventUuid))
                {
                    continue;
                }

                string state = NormalizeResultState(eventResult.Status);
                _outboxRepository.ApplyPushResult(
                    eventResult.EventUuid,
                    state,
                    eventResult.ErrorCode,
                    attemptUtc,
                    result.ServerRevision);
            }
        }

        private void RestorePending(IReadOnlyList<string> eventUuids, string attemptUtc, string errorCode)
        {
            foreach (string eventUuid in eventUuids)
            {
                _outboxRepository.ApplyPushResult(
                    eventUuid,
                    OutboxEventState.Pending,
                    errorCode,
                    attemptUtc,
                    serverRevision: null);
            }
        }

        private string GetOrCreateBatchUuid()
        {
            lock (_gate)
            {
                if (string.IsNullOrWhiteSpace(_activeBatchUuid))
                {
                    _activeBatchUuid = _idGenerator.NewUuid();
                }

                return _activeBatchUuid;
            }
        }

        private void ClearActiveBatchUuid()
        {
            lock (_gate)
            {
                _activeBatchUuid = null;
            }
        }

        private static string NormalizeResultState(string status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return OutboxEventState.Deferred;
            }

            string normalized = status.Trim().ToLowerInvariant();
            if (OutboxEventState.IsKnown(normalized))
            {
                return normalized;
            }

            return OutboxEventState.Deferred;
        }

        private static SyncPushEvent ToPushEvent(SyncOutboxRecord row)
        {
            return new SyncPushEvent
            {
                EventUuid = row.EventUuid,
                LocalSequence = row.LocalSequence,
                EventType = row.EventType,
                GradeId = row.GradeId,
                SubjectId = row.SubjectId,
                TermId = row.TermId,
                MissionId = row.MissionId,
                AreaId = row.AreaId,
                PayloadJson = row.PayloadJson,
                ClientCreatedUtc = row.ClientCreatedUtc
            };
        }

        private static int EstimateBatchEnvelopeBytes() => 128;

        private static int EstimateEventBytes(SyncOutboxRecord row)
        {
            int total = 160;
            total += Encoding.UTF8.GetByteCount(row.EventUuid ?? string.Empty);
            total += Encoding.UTF8.GetByteCount(row.EventType ?? string.Empty);
            total += Encoding.UTF8.GetByteCount(row.GradeId ?? string.Empty);
            total += Encoding.UTF8.GetByteCount(row.SubjectId ?? string.Empty);
            total += Encoding.UTF8.GetByteCount(row.TermId ?? string.Empty);
            total += Encoding.UTF8.GetByteCount(row.MissionId ?? string.Empty);
            total += Encoding.UTF8.GetByteCount(row.AreaId ?? string.Empty);
            total += Encoding.UTF8.GetByteCount(row.PayloadJson ?? string.Empty);
            total += Encoding.UTF8.GetByteCount(row.ClientCreatedUtc ?? string.Empty);
            return total;
        }
    }
}
