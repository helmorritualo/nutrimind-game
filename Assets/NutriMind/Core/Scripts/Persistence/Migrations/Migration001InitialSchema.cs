using SQLite;

namespace NutriMind.Core.Persistence
{
    /// <summary>
    /// Schema version 1 — installation, session, cache, progress, outbox, announcements, idempotency.
    /// </summary>
    public sealed class Migration001InitialSchema : IDatabaseMigration
    {
        public int Version => 1;
        public string Name => "initial_schema";

        public void Apply(SQLiteConnection connection)
        {
            connection.Execute(@"
CREATE TABLE IF NOT EXISTS installation_state (
    singleton_key TEXT NOT NULL PRIMARY KEY,
    device_id TEXT NOT NULL,
    created_utc TEXT NOT NULL
);");

            connection.Execute(@"
CREATE TABLE IF NOT EXISTS local_session (
    student_id TEXT NOT NULL PRIMARY KEY,
    display_name TEXT NOT NULL,
    grade_id TEXT NOT NULL,
    section_id TEXT NOT NULL,
    section_name TEXT NOT NULL,
    last_authenticated_utc TEXT NOT NULL,
    last_bootstrap_revision INTEGER NOT NULL DEFAULT 0,
    last_bootstrap_cached_utc TEXT,
    offline_eligible INTEGER NOT NULL DEFAULT 0
);");

            connection.Execute(@"
CREATE TABLE IF NOT EXISTS resource_cache (
    cache_key TEXT NOT NULL PRIMARY KEY,
    payload_json TEXT NOT NULL,
    schema_version INTEGER NOT NULL,
    server_revision INTEGER,
    cached_utc TEXT NOT NULL
);");

            connection.Execute(@"
CREATE TABLE IF NOT EXISTS mission_progress (
    mission_id TEXT NOT NULL PRIMARY KEY,
    state TEXT NOT NULL,
    active_area_id TEXT,
    completed_area_count INTEGER NOT NULL DEFAULT 0,
    required_area_count INTEGER NOT NULL DEFAULT 3,
    collectible_count INTEGER NOT NULL DEFAULT 0,
    required_collectible_count INTEGER NOT NULL DEFAULT 3,
    revision INTEGER NOT NULL DEFAULT 0,
    started_utc TEXT NOT NULL,
    completed_utc TEXT
);");

            connection.Execute(@"
CREATE TABLE IF NOT EXISTS area_progress (
    area_id TEXT NOT NULL PRIMARY KEY,
    mission_id TEXT NOT NULL,
    area_order INTEGER NOT NULL,
    state TEXT NOT NULL,
    started_utc TEXT NOT NULL,
    completed_utc TEXT
);");

            connection.Execute(@"
CREATE TABLE IF NOT EXISTS question_outcome (
    mission_id TEXT NOT NULL,
    area_id TEXT NOT NULL,
    question_id TEXT NOT NULL,
    attempt_number INTEGER NOT NULL,
    outcome TEXT NOT NULL,
    review_required INTEGER NOT NULL DEFAULT 0,
    answered_utc TEXT NOT NULL,
    PRIMARY KEY (mission_id, area_id, question_id, attempt_number)
);");

            connection.Execute(@"
CREATE TABLE IF NOT EXISTS collectible_state (
    collectible_id TEXT NOT NULL PRIMARY KEY,
    mission_id TEXT NOT NULL,
    area_id TEXT NOT NULL,
    collected INTEGER NOT NULL DEFAULT 0,
    collected_utc TEXT
);");

            connection.Execute(@"
CREATE TABLE IF NOT EXISTS world_state (
    state_key TEXT NOT NULL PRIMARY KEY,
    mission_id TEXT NOT NULL,
    area_id TEXT,
    value_json TEXT NOT NULL,
    updated_utc TEXT NOT NULL
);");

            connection.Execute(@"
CREATE TABLE IF NOT EXISTS sync_outbox (
    event_uuid TEXT NOT NULL PRIMARY KEY,
    local_sequence INTEGER NOT NULL UNIQUE,
    event_type TEXT NOT NULL,
    grade_id TEXT NOT NULL,
    subject_id TEXT NOT NULL,
    term_id TEXT NOT NULL,
    mission_id TEXT NOT NULL,
    area_id TEXT,
    payload_json TEXT NOT NULL,
    client_created_utc TEXT NOT NULL,
    state TEXT NOT NULL,
    attempt_count INTEGER NOT NULL DEFAULT 0,
    last_error_code TEXT,
    last_attempt_utc TEXT,
    server_revision INTEGER
);");

            connection.Execute(@"
CREATE TABLE IF NOT EXISTS announcement_read_state (
    announcement_key TEXT NOT NULL PRIMARY KEY,
    read_utc TEXT NOT NULL
);");

            connection.Execute(@"
CREATE TABLE IF NOT EXISTS idempotent_request (
    request_uuid TEXT NOT NULL PRIMARY KEY,
    operation TEXT NOT NULL,
    normalized_payload_json TEXT NOT NULL,
    state TEXT NOT NULL,
    result_json TEXT,
    created_utc TEXT NOT NULL,
    updated_utc TEXT NOT NULL
);");

            connection.Execute(
                "CREATE INDEX IF NOT EXISTS idx_sync_outbox_state_sequence ON sync_outbox(state, local_sequence);");
            connection.Execute(
                "CREATE INDEX IF NOT EXISTS idx_area_progress_mission ON area_progress(mission_id);");
            connection.Execute(
                "CREATE INDEX IF NOT EXISTS idx_question_outcome_mission ON question_outcome(mission_id, area_id);");
            connection.Execute(
                "CREATE INDEX IF NOT EXISTS idx_collectible_mission ON collectible_state(mission_id);");
            connection.Execute(
                "CREATE INDEX IF NOT EXISTS idx_world_state_mission ON world_state(mission_id);");
        }
    }
}
