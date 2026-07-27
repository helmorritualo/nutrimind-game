using System;
using System.Collections.Generic;
using SQLite;
using UnityEngine;

namespace NutriMind.Core.Persistence
{
    /// <summary>
    /// Schema version 2 — typed ownership columns on idempotent_request plus exact lookup index.
    /// Legacy unresolved rows without safe ownership are quarantined as rejected.
    /// </summary>
    public sealed class Migration002IdempotentIdentity : IDatabaseMigration
    {
        public const string QuarantineResultJson =
            "{\"code\":\"legacy_idempotent_unowned\",\"message\":\"Legacy unresolved request lacks safe learner ownership.\"}";

        public int Version => 2;
        public string Name => "idempotent_identity";

        public void Apply(SQLiteConnection connection)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            EnsureColumn(connection, "idempotent_request", "student_id", "TEXT NOT NULL DEFAULT ''");
            EnsureColumn(connection, "idempotent_request", "entity_key", "TEXT NOT NULL DEFAULT ''");

            connection.Execute(@"
CREATE INDEX IF NOT EXISTS idx_idempotent_unresolved_identity
ON idempotent_request (operation, student_id, entity_key, state, updated_utc);");

            List<LegacyIdempotentRow> rows = connection.Query<LegacyIdempotentRow>(
                "SELECT request_uuid AS RequestUuid, operation AS Operation, "
                + "normalized_payload_json AS NormalizedPayloadJson, state AS State "
                + "FROM idempotent_request");

            if (rows == null || rows.Count == 0)
            {
                return;
            }

            string now = DateTimeOffset.UtcNow.ToString("o");
            for (int i = 0; i < rows.Count; i++)
            {
                LegacyIdempotentRow row = rows[i];
                if (row == null || string.IsNullOrWhiteSpace(row.RequestUuid))
                {
                    continue;
                }

                string entityKey;
                bool envelopeMatched = TryExtractEntityKey(
                    row.Operation,
                    row.NormalizedPayloadJson,
                    row.RequestUuid,
                    out entityKey);

                string studentId = string.Empty;
                string nextState = row.State;
                string resultJson = null;
                bool quarantine = false;

                if (!envelopeMatched || string.IsNullOrWhiteSpace(entityKey))
                {
                    entityKey = string.Empty;
                    if (IdempotentRequestStates.IsUnresolved(row.State))
                    {
                        quarantine = true;
                    }
                }

                if (quarantine)
                {
                    nextState = IdempotentRequestStates.Rejected;
                    resultJson = QuarantineResultJson;
                }

                if (resultJson != null)
                {
                    connection.Execute(
                        "UPDATE idempotent_request SET student_id = ?, entity_key = ?, state = ?, "
                        + "result_json = ?, updated_utc = ? WHERE request_uuid = ?",
                        studentId,
                        entityKey ?? string.Empty,
                        nextState,
                        resultJson,
                        now,
                        row.RequestUuid);
                }
                else
                {
                    connection.Execute(
                        "UPDATE idempotent_request SET student_id = ?, entity_key = ? WHERE request_uuid = ?",
                        studentId,
                        entityKey ?? string.Empty,
                        row.RequestUuid);
                }
            }
        }

        private static void EnsureColumn(
            SQLiteConnection connection,
            string tableName,
            string columnName,
            string columnTypeSql)
        {
            List<SqliteTableInfoRow> columns = connection.Query<SqliteTableInfoRow>(
                "PRAGMA table_info(" + tableName + ")");
            if (columns != null)
            {
                for (int i = 0; i < columns.Count; i++)
                {
                    if (string.Equals(columns[i]?.name, columnName, StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }
                }
            }

            connection.Execute(
                "ALTER TABLE " + tableName + " ADD COLUMN " + columnName + " " + columnTypeSql);
        }

        private static bool TryExtractEntityKey(
            string operation,
            string payloadJson,
            string requestUuid,
            out string entityKey)
        {
            entityKey = null;
            if (string.IsNullOrWhiteSpace(payloadJson))
            {
                return false;
            }

            try
            {
                if (string.Equals(operation, "use_reward", StringComparison.Ordinal))
                {
                    LegacyRewardEnvelopeDto dto =
                        JsonUtility.FromJson<LegacyRewardEnvelopeDto>(payloadJson);
                    if (dto == null
                        || (dto.Version != 0 && dto.Version != 1 && dto.Version != 2)
                        || string.IsNullOrWhiteSpace(dto.RewardCode)
                        || !string.Equals(dto.RequestUuid, requestUuid, StringComparison.Ordinal))
                    {
                        return false;
                    }

                    entityKey = dto.RewardCode.Trim();
                    return true;
                }

                if (string.Equals(operation, "quiz_submit", StringComparison.Ordinal))
                {
                    LegacyQuizEnvelopeDto dto =
                        JsonUtility.FromJson<LegacyQuizEnvelopeDto>(payloadJson);
                    if (dto == null
                        || (dto.Version != 0 && dto.Version != 1 && dto.Version != 2)
                        || string.IsNullOrWhiteSpace(dto.QuizId))
                    {
                        return false;
                    }

                    // V1 quiz envelopes do not embed RequestUuid; accept entity extraction only.
                    entityKey = dto.QuizId.Trim();
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }

            return false;
        }

        private sealed class LegacyIdempotentRow
        {
            public string RequestUuid { get; set; }
            public string Operation { get; set; }
            public string NormalizedPayloadJson { get; set; }
            public string State { get; set; }
        }

        private sealed class SqliteTableInfoRow
        {
            public int cid { get; set; }
            public string name { get; set; }
            public string type { get; set; }
            public int notnull { get; set; }
            public string dflt_value { get; set; }
            public int pk { get; set; }
        }

        [Serializable]
        private sealed class LegacyRewardEnvelopeDto
        {
            public int Version;
            public string StudentId;
            public string RewardCode;
            public string RequestUuid;
        }

        [Serializable]
        private sealed class LegacyQuizEnvelopeDto
        {
            public int Version;
            public string StudentId;
            public string QuizId;
        }
    }
}
