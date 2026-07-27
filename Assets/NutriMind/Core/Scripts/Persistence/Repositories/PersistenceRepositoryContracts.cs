using System;
using System.Collections.Generic;
using NutriMind.Core.Data;

namespace NutriMind.Core.Persistence
{
    public interface IInstallationRepository
    {
        /// <summary>
        /// Returns the persisted installation device UUID, creating one on first launch.
        /// Never hardware-derived. Intended for future X-Device-ID.
        /// </summary>
        AppResult<string> GetOrCreateDeviceId();

        AppResult<InstallationStateRecord> GetInstallationState();

        /// <summary>
        /// Regenerates device_id. Allowed only for explicit full-install mock reset.
        /// </summary>
        AppResult<string> RegenerateDeviceIdForFullInstallReset();
    }

    public interface ILocalSessionRepository
    {
        AppResult UpsertSession(LocalSessionRecord session);
        AppResult<LocalSessionRecord> GetSession();
        AppResult ClearSession();
    }

    public interface IResourceCacheRepository
    {
        AppResult Upsert(ResourceCacheRecord record);
        AppResult<ResourceCacheRecord> Get(string cacheKey);
        AppResult Delete(string cacheKey);
        AppResult ClearAll();
    }

    public interface IMissionProgressRepository
    {
        AppResult UpsertMission(MissionProgressRecord record);
        AppResult UpsertArea(AreaProgressRecord record);
        AppResult UpsertQuestionOutcome(QuestionOutcomeRecord record);
        AppResult UpsertCollectible(CollectibleStateRecord record);
        AppResult UpsertWorldState(WorldStateRecord record);

        AppResult<MissionProgressRecord> GetMission(string missionId);
        AppResult<IReadOnlyList<AreaProgressRecord>> GetAreasForMission(string missionId);
        AppResult<IReadOnlyList<QuestionOutcomeRecord>> GetQuestionOutcomesForMission(string missionId);
        AppResult<IReadOnlyList<CollectibleStateRecord>> GetCollectiblesForMission(string missionId);
        AppResult<IReadOnlyList<WorldStateRecord>> GetWorldStateForMission(string missionId);
    }

    public interface IOutboxRepository
    {
        AppResult<SyncOutboxRecord> Enqueue(SyncOutboxRecord record);
        AppResult<long> PeekNextLocalSequence();
        AppResult<IReadOnlyList<SyncOutboxRecord>> GetPushableAscending(int limit);
        AppResult RecoverInterruptedSending();
        AppResult MarkSending(IReadOnlyList<string> eventUuids, string attemptUtc);
        AppResult ApplyPushResult(string eventUuid, string state, string errorCode, string attemptUtc, int? serverRevision);
        AppResult<int> CountByStates(params string[] states);
        AppResult<IReadOnlyList<SyncOutboxRecord>> GetAllAscending();
    }

    public interface IAnnouncementReadRepository
    {
        AppResult MarkRead(string announcementKey, string readUtc);
        AppResult<bool> IsRead(string announcementKey);
        AppResult<IReadOnlyList<AnnouncementReadStateRecord>> GetAll();
    }

    public interface IIdempotentRequestRepository
    {
        AppResult Upsert(IdempotentRequestRecord record);
        AppResult<IdempotentRequestRecord> Get(string requestUuid);
        AppResult Delete(string requestUuid);

        /// <summary>
        /// Returns records matching the operation whose state is in <paramref name="states"/>.
        /// Ordered by updated_utc descending (nulls last), then created_utc descending.
        /// </summary>
        AppResult<IReadOnlyList<IdempotentRequestRecord>> GetByOperationAndStates(
            string operation,
            params string[] states);

        /// <summary>
        /// Finds the newest unresolved request for an operation whose normalized payload
        /// contains <paramref name="entityKey"/> (reward code or quiz id).
        /// </summary>
        AppResult<IdempotentRequestRecord> FindLatestUnresolved(string operation, string entityKey);
    }

    /// <summary>
    /// Commits local progress and the matching outbox event in one SQLite transaction.
    /// </summary>
    public interface ILocalProgressWriter
    {
        event Action LocalStateChanged;

        AppResult Commit(LocalProgressWriteRequest request);
    }

    public sealed class LocalProgressWriteRequest
    {
        public MissionProgressRecord MissionProgress { get; set; }
        public AreaProgressRecord AreaProgress { get; set; }
        public QuestionOutcomeRecord QuestionOutcome { get; set; }
        public CollectibleStateRecord CollectibleState { get; set; }
        public WorldStateRecord WorldState { get; set; }

        /// <summary>
        /// Required. local_sequence and pending state are assigned by the writer when unset.
        /// </summary>
        public SyncOutboxRecord OutboxEvent { get; set; }
    }

}
