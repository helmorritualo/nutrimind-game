using System;
using NutriMind.Core.Data;
using NutriMind.Core.Utilities;
using SQLite;

namespace NutriMind.Core.Persistence
{
    /// <summary>
    /// Atomically updates local progress and inserts the matching outbox event,
    /// then publishes <see cref="LocalStateChanged"/>.
    /// </summary>
    public sealed class LocalProgressWriter : ILocalProgressWriter
    {
        private readonly NutriMindDatabase _database;
        private readonly IAppClock _clock;

        public LocalProgressWriter(NutriMindDatabase database, IAppClock clock = null)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _clock = clock ?? new SystemAppClock();
        }

        public event Action LocalStateChanged;

        public AppResult Commit(LocalProgressWriteRequest request)
        {
            if (request == null)
            {
                return AppResult.Failure(AppErrorCodes.ValidationFailed, "Progress write request is required.");
            }

            if (request.OutboxEvent == null || string.IsNullOrWhiteSpace(request.OutboxEvent.EventUuid))
            {
                return AppResult.Failure(AppErrorCodes.ValidationFailed, "Outbox event_uuid is required.");
            }

            if (string.IsNullOrWhiteSpace(request.OutboxEvent.EventType)
                || string.IsNullOrWhiteSpace(request.OutboxEvent.PayloadJson))
            {
                return AppResult.Failure(
                    AppErrorCodes.ValidationFailed,
                    "Outbox event_type and payload_json are required.");
            }

            AppResult result = _database.RunInTransaction(connection =>
            {
                ApplyProgress(connection, request);
                EnqueueOutbox(connection, request.OutboxEvent);
            });

            if (result.IsFailure)
            {
                NutriMindLog.SqliteError("Local progress+outbox transaction rolled back.");
                return result;
            }

            try
            {
                LocalStateChanged?.Invoke();
            }
            catch (Exception exception)
            {
                NutriMindLog.SqliteWarning(
                    "LocalStateChanged listener failed: " + exception.GetType().Name);
            }

            return AppResult.Success();
        }

        private static void ApplyProgress(SQLiteConnection connection, LocalProgressWriteRequest request)
        {
            if (request.MissionProgress != null)
            {
                connection.InsertOrReplace(request.MissionProgress);
            }

            if (request.AreaProgress != null)
            {
                connection.InsertOrReplace(request.AreaProgress);
            }

            if (request.QuestionOutcome != null)
            {
                connection.InsertOrReplace(request.QuestionOutcome);
            }

            if (request.CollectibleState != null)
            {
                connection.InsertOrReplace(request.CollectibleState);
            }

            if (request.WorldState != null)
            {
                connection.InsertOrReplace(request.WorldState);
            }
        }

        private void EnqueueOutbox(SQLiteConnection connection, SyncOutboxRecord outboxEvent)
        {
            if (outboxEvent.LocalSequence <= 0)
            {
                outboxEvent.LocalSequence = connection.ExecuteScalar<long>(
                    "SELECT COALESCE(MAX(local_sequence), 0) + 1 FROM sync_outbox");
            }

            if (string.IsNullOrWhiteSpace(outboxEvent.State))
            {
                outboxEvent.State = OutboxEventState.Pending;
            }

            if (!OutboxEventState.IsKnown(outboxEvent.State))
            {
                throw new InvalidOperationException("Unsupported outbox state: " + outboxEvent.State);
            }

            if (string.IsNullOrWhiteSpace(outboxEvent.ClientCreatedUtc))
            {
                outboxEvent.ClientCreatedUtc = _clock.UtcNow.ToUniversalTime().ToString("o");
            }

            connection.Insert(outboxEvent);
        }
    }
}
