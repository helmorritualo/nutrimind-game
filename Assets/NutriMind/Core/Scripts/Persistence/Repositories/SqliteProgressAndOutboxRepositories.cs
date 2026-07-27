using System;
using System.Collections.Generic;
using System.Linq;
using NutriMind.Core.Data;
using NutriMind.Core.Utilities;
using SQLite;

namespace NutriMind.Core.Persistence
{
    public sealed class SqliteMissionProgressRepository : IMissionProgressRepository
    {
        private readonly NutriMindDatabase _database;

        public SqliteMissionProgressRepository(NutriMindDatabase database)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
        }

        public AppResult UpsertMission(MissionProgressRecord record)
        {
            return Upsert(record, r => r == null || string.IsNullOrWhiteSpace(r.MissionId), "mission_id");
        }

        public AppResult UpsertArea(AreaProgressRecord record)
        {
            return Upsert(record, r => r == null || string.IsNullOrWhiteSpace(r.AreaId), "area_id");
        }

        public AppResult UpsertQuestionOutcome(QuestionOutcomeRecord record)
        {
            if (record == null
                || string.IsNullOrWhiteSpace(record.MissionId)
                || string.IsNullOrWhiteSpace(record.AreaId)
                || string.IsNullOrWhiteSpace(record.QuestionId)
                || record.AttemptNumber < 1)
            {
                return AppResult.Failure(
                    AppErrorCodes.ValidationFailed,
                    "question_outcome composite key is incomplete.");
            }

            try
            {
                _database.ExecuteWithConnection(connection => connection.InsertOrReplace(record));
                return AppResult.Success();
            }
            catch (Exception exception)
            {
                return AppResult.Failure(AppError.FromException(exception));
            }
        }

        public AppResult UpsertCollectible(CollectibleStateRecord record)
        {
            return Upsert(
                record,
                r => r == null || string.IsNullOrWhiteSpace(r.CollectibleId),
                "collectible_id");
        }

        public AppResult UpsertWorldState(WorldStateRecord record)
        {
            return Upsert(record, r => r == null || string.IsNullOrWhiteSpace(r.StateKey), "state_key");
        }

        public AppResult<MissionProgressRecord> GetMission(string missionId)
        {
            if (string.IsNullOrWhiteSpace(missionId))
            {
                return AppResult<MissionProgressRecord>.Failure(
                    AppErrorCodes.ValidationFailed,
                    "mission_id is required.");
            }

            try
            {
                MissionProgressRecord record = _database.ExecuteWithConnection(connection =>
                    connection.Find<MissionProgressRecord>(missionId));
                return AppResult<MissionProgressRecord>.Success(record);
            }
            catch (Exception exception)
            {
                return AppResult<MissionProgressRecord>.Failure(AppError.FromException(exception));
            }
        }

        public AppResult<IReadOnlyList<AreaProgressRecord>> GetAreasForMission(string missionId)
        {
            return QueryList(missionId, connection =>
                connection.Table<AreaProgressRecord>()
                    .Where(a => a.MissionId == missionId)
                    .OrderBy(a => a.AreaOrder)
                    .ToList());
        }

        public AppResult<IReadOnlyList<QuestionOutcomeRecord>> GetQuestionOutcomesForMission(string missionId)
        {
            return QueryList(missionId, connection =>
                connection.Table<QuestionOutcomeRecord>()
                    .Where(q => q.MissionId == missionId)
                    .ToList());
        }

        public AppResult<IReadOnlyList<CollectibleStateRecord>> GetCollectiblesForMission(string missionId)
        {
            return QueryList(missionId, connection =>
                connection.Table<CollectibleStateRecord>()
                    .Where(c => c.MissionId == missionId)
                    .ToList());
        }

        public AppResult<IReadOnlyList<WorldStateRecord>> GetWorldStateForMission(string missionId)
        {
            return QueryList(missionId, connection =>
                connection.Table<WorldStateRecord>()
                    .Where(w => w.MissionId == missionId)
                    .ToList());
        }

        private AppResult Upsert<T>(T record, Func<T, bool> invalid, string fieldName) where T : class
        {
            if (invalid(record))
            {
                return AppResult.Failure(AppErrorCodes.ValidationFailed, fieldName + " is required.");
            }

            try
            {
                _database.ExecuteWithConnection(connection => connection.InsertOrReplace(record));
                return AppResult.Success();
            }
            catch (Exception exception)
            {
                return AppResult.Failure(AppError.FromException(exception));
            }
        }

        private AppResult<IReadOnlyList<T>> QueryList<T>(
            string missionId,
            Func<SQLiteConnection, List<T>> query)
        {
            if (string.IsNullOrWhiteSpace(missionId))
            {
                return AppResult<IReadOnlyList<T>>.Failure(
                    AppErrorCodes.ValidationFailed,
                    "mission_id is required.");
            }

            try
            {
                List<T> list = _database.ExecuteWithConnection(query);
                return AppResult<IReadOnlyList<T>>.Success(list);
            }
            catch (Exception exception)
            {
                return AppResult<IReadOnlyList<T>>.Failure(AppError.FromException(exception));
            }
        }
    }

    public sealed class SqliteOutboxRepository : IOutboxRepository
    {
        private readonly NutriMindDatabase _database;

        public SqliteOutboxRepository(NutriMindDatabase database)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
        }

        public AppResult<SyncOutboxRecord> Enqueue(SyncOutboxRecord record)
        {
            if (record == null || string.IsNullOrWhiteSpace(record.EventUuid))
            {
                return AppResult<SyncOutboxRecord>.Failure(
                    AppErrorCodes.ValidationFailed,
                    "event_uuid is required.");
            }

            try
            {
                SyncOutboxRecord saved = _database.ExecuteWithConnection(connection =>
                {
                    if (record.LocalSequence <= 0)
                    {
                        record.LocalSequence = NextSequence(connection);
                    }

                    if (string.IsNullOrWhiteSpace(record.State))
                    {
                        record.State = OutboxEventState.Pending;
                    }

                    if (!OutboxEventState.IsKnown(record.State))
                    {
                        throw new InvalidOperationException("Unsupported outbox state: " + record.State);
                    }

                    connection.Insert(record);
                    return record;
                });

                return AppResult<SyncOutboxRecord>.Success(saved);
            }
            catch (Exception exception)
            {
                NutriMindLog.SqliteError("Enqueue outbox failed: " + exception.GetType().Name);
                return AppResult<SyncOutboxRecord>.Failure(AppError.FromException(exception));
            }
        }

        public AppResult<long> PeekNextLocalSequence()
        {
            try
            {
                long next = _database.ExecuteWithConnection(NextSequence);
                return AppResult<long>.Success(next);
            }
            catch (Exception exception)
            {
                return AppResult<long>.Failure(AppError.FromException(exception));
            }
        }

        public AppResult<IReadOnlyList<SyncOutboxRecord>> GetPushableAscending(int limit)
        {
            int take = Math.Max(1, limit);
            try
            {
                List<SyncOutboxRecord> rows = _database.ExecuteWithConnection(connection =>
                    connection.Query<SyncOutboxRecord>(
                        @"SELECT * FROM sync_outbox
                          WHERE state = ? OR state = ?
                          ORDER BY local_sequence ASC
                          LIMIT ?",
                        OutboxEventState.Pending,
                        OutboxEventState.Deferred,
                        take));
                return AppResult<IReadOnlyList<SyncOutboxRecord>>.Success(rows);
            }
            catch (Exception exception)
            {
                return AppResult<IReadOnlyList<SyncOutboxRecord>>.Failure(AppError.FromException(exception));
            }
        }

        public AppResult RecoverInterruptedSending()
        {
            try
            {
                int recovered = _database.ExecuteWithConnection(connection =>
                    connection.Execute(
                        "UPDATE sync_outbox SET state = ? WHERE state = ?",
                        OutboxEventState.Pending,
                        OutboxEventState.Sending));
                if (recovered > 0)
                {
                    NutriMindLog.Sqlite("Recovered " + recovered + " interrupted sending outbox rows.");
                }

                return AppResult.Success();
            }
            catch (Exception exception)
            {
                return AppResult.Failure(AppError.FromException(exception));
            }
        }

        public AppResult MarkSending(IReadOnlyList<string> eventUuids, string attemptUtc)
        {
            if (eventUuids == null || eventUuids.Count == 0)
            {
                return AppResult.Success();
            }

            try
            {
                _database.ExecuteWithConnection(connection =>
                {
                    foreach (string eventUuid in eventUuids)
                    {
                        connection.Execute(
                            @"UPDATE sync_outbox
                              SET state = ?, attempt_count = attempt_count + 1, last_attempt_utc = ?
                              WHERE event_uuid = ?",
                            OutboxEventState.Sending,
                            attemptUtc,
                            eventUuid);
                    }
                });
                return AppResult.Success();
            }
            catch (Exception exception)
            {
                return AppResult.Failure(AppError.FromException(exception));
            }
        }

        public AppResult ApplyPushResult(
            string eventUuid,
            string state,
            string errorCode,
            string attemptUtc,
            int? serverRevision)
        {
            if (string.IsNullOrWhiteSpace(eventUuid) || !OutboxEventState.IsKnown(state))
            {
                return AppResult.Failure(AppErrorCodes.ValidationFailed, "Invalid outbox result.");
            }

            try
            {
                _database.ExecuteWithConnection(connection =>
                    connection.Execute(
                        @"UPDATE sync_outbox
                          SET state = ?, last_error_code = ?, last_attempt_utc = ?, server_revision = ?
                          WHERE event_uuid = ?",
                        state,
                        errorCode,
                        attemptUtc,
                        serverRevision,
                        eventUuid));
                return AppResult.Success();
            }
            catch (Exception exception)
            {
                return AppResult.Failure(AppError.FromException(exception));
            }
        }

        public AppResult<int> CountByStates(params string[] states)
        {
            if (states == null || states.Length == 0)
            {
                return AppResult<int>.Success(0);
            }

            try
            {
                int count = _database.ExecuteWithConnection(connection =>
                {
                    int total = 0;
                    foreach (string state in states)
                    {
                        total += connection.ExecuteScalar<int>(
                            "SELECT COUNT(*) FROM sync_outbox WHERE state = ?",
                            state);
                    }

                    return total;
                });
                return AppResult<int>.Success(count);
            }
            catch (Exception exception)
            {
                return AppResult<int>.Failure(AppError.FromException(exception));
            }
        }

        public AppResult<IReadOnlyList<SyncOutboxRecord>> GetAllAscending()
        {
            try
            {
                List<SyncOutboxRecord> rows = _database.ExecuteWithConnection(connection =>
                    connection.Query<SyncOutboxRecord>(
                        "SELECT * FROM sync_outbox ORDER BY local_sequence ASC"));
                return AppResult<IReadOnlyList<SyncOutboxRecord>>.Success(rows);
            }
            catch (Exception exception)
            {
                return AppResult<IReadOnlyList<SyncOutboxRecord>>.Failure(AppError.FromException(exception));
            }
        }

        private static long NextSequence(SQLiteConnection connection)
        {
            return connection.ExecuteScalar<long>(
                "SELECT COALESCE(MAX(local_sequence), 0) + 1 FROM sync_outbox");
        }
    }

    public sealed class SqliteAnnouncementReadRepository : IAnnouncementReadRepository
    {
        private readonly NutriMindDatabase _database;

        public SqliteAnnouncementReadRepository(NutriMindDatabase database)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
        }

        public AppResult MarkRead(string announcementKey, string readUtc)
        {
            if (string.IsNullOrWhiteSpace(announcementKey))
            {
                return AppResult.Failure(AppErrorCodes.ValidationFailed, "announcement_key is required.");
            }

            try
            {
                _database.ExecuteWithConnection(connection =>
                    connection.InsertOrReplace(new AnnouncementReadStateRecord
                    {
                        AnnouncementKey = announcementKey,
                        ReadUtc = readUtc
                    }));
                return AppResult.Success();
            }
            catch (Exception exception)
            {
                return AppResult.Failure(AppError.FromException(exception));
            }
        }

        public AppResult<bool> IsRead(string announcementKey)
        {
            if (string.IsNullOrWhiteSpace(announcementKey))
            {
                return AppResult<bool>.Failure(AppErrorCodes.ValidationFailed, "announcement_key is required.");
            }

            try
            {
                bool read = _database.ExecuteWithConnection(connection =>
                    connection.Find<AnnouncementReadStateRecord>(announcementKey) != null);
                return AppResult<bool>.Success(read);
            }
            catch (Exception exception)
            {
                return AppResult<bool>.Failure(AppError.FromException(exception));
            }
        }

        public AppResult<IReadOnlyList<AnnouncementReadStateRecord>> GetAll()
        {
            try
            {
                List<AnnouncementReadStateRecord> rows = _database.ExecuteWithConnection(connection =>
                    connection.Table<AnnouncementReadStateRecord>().ToList());
                return AppResult<IReadOnlyList<AnnouncementReadStateRecord>>.Success(rows);
            }
            catch (Exception exception)
            {
                return AppResult<IReadOnlyList<AnnouncementReadStateRecord>>.Failure(
                    AppError.FromException(exception));
            }
        }
    }

    public sealed class SqliteIdempotentRequestRepository : IIdempotentRequestRepository
    {
        private readonly NutriMindDatabase _database;

        public SqliteIdempotentRequestRepository(NutriMindDatabase database)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
        }

        public AppResult Upsert(IdempotentRequestRecord record)
        {
            if (record == null || string.IsNullOrWhiteSpace(record.RequestUuid))
            {
                return AppResult.Failure(AppErrorCodes.ValidationFailed, "request_uuid is required.");
            }

            try
            {
                _database.ExecuteWithConnection(connection => connection.InsertOrReplace(record));
                return AppResult.Success();
            }
            catch (Exception exception)
            {
                return AppResult.Failure(AppError.FromException(exception));
            }
        }

        public AppResult<IdempotentRequestRecord> Get(string requestUuid)
        {
            if (string.IsNullOrWhiteSpace(requestUuid))
            {
                return AppResult<IdempotentRequestRecord>.Failure(
                    AppErrorCodes.ValidationFailed,
                    "request_uuid is required.");
            }

            try
            {
                IdempotentRequestRecord record = _database.ExecuteWithConnection(connection =>
                    connection.Find<IdempotentRequestRecord>(requestUuid));
                return AppResult<IdempotentRequestRecord>.Success(record);
            }
            catch (Exception exception)
            {
                return AppResult<IdempotentRequestRecord>.Failure(AppError.FromException(exception));
            }
        }

        public AppResult Delete(string requestUuid)
        {
            if (string.IsNullOrWhiteSpace(requestUuid))
            {
                return AppResult.Failure(AppErrorCodes.ValidationFailed, "request_uuid is required.");
            }

            try
            {
                _database.ExecuteWithConnection(connection =>
                    connection.Execute(
                        "DELETE FROM idempotent_request WHERE request_uuid = ?",
                        requestUuid));
                return AppResult.Success();
            }
            catch (Exception exception)
            {
                return AppResult.Failure(AppError.FromException(exception));
            }
        }
    }
}
