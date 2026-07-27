using SQLite;

namespace NutriMind.Core.Persistence
{
    [Table("installation_state")]
    public sealed class InstallationStateRecord
    {
        public const string SingletonKey = "default";

        [PrimaryKey]
        [Column("singleton_key")]
        public string SingletonKeyValue { get; set; }

        [Column("device_id")]
        public string DeviceId { get; set; }

        [Column("created_utc")]
        public string CreatedUtc { get; set; }
    }

    [Table("local_session")]
    public sealed class LocalSessionRecord
    {
        [PrimaryKey]
        [Column("student_id")]
        public string StudentId { get; set; }

        [Column("display_name")]
        public string DisplayName { get; set; }

        [Column("grade_id")]
        public string GradeId { get; set; }

        [Column("section_id")]
        public string SectionId { get; set; }

        [Column("section_name")]
        public string SectionName { get; set; }

        [Column("last_authenticated_utc")]
        public string LastAuthenticatedUtc { get; set; }

        [Column("last_bootstrap_revision")]
        public int LastBootstrapRevision { get; set; }

        [Column("last_bootstrap_cached_utc")]
        public string LastBootstrapCachedUtc { get; set; }

        [Column("offline_eligible")]
        public bool OfflineEligible { get; set; }
    }

    [Table("resource_cache")]
    public sealed class ResourceCacheRecord
    {
        [PrimaryKey]
        [Column("cache_key")]
        public string CacheKey { get; set; }

        [Column("payload_json")]
        public string PayloadJson { get; set; }

        [Column("schema_version")]
        public int SchemaVersion { get; set; }

        [Column("server_revision")]
        public int? ServerRevision { get; set; }

        [Column("cached_utc")]
        public string CachedUtc { get; set; }
    }

    [Table("mission_progress")]
    public sealed class MissionProgressRecord
    {
        [PrimaryKey]
        [Column("mission_id")]
        public string MissionId { get; set; }

        [Column("state")]
        public string State { get; set; }

        [Column("active_area_id")]
        public string ActiveAreaId { get; set; }

        [Column("completed_area_count")]
        public int CompletedAreaCount { get; set; }

        [Column("required_area_count")]
        public int RequiredAreaCount { get; set; }

        [Column("collectible_count")]
        public int CollectibleCount { get; set; }

        [Column("required_collectible_count")]
        public int RequiredCollectibleCount { get; set; }

        [Column("revision")]
        public int Revision { get; set; }

        [Column("started_utc")]
        public string StartedUtc { get; set; }

        [Column("completed_utc")]
        public string CompletedUtc { get; set; }
    }

    [Table("area_progress")]
    public sealed class AreaProgressRecord
    {
        [PrimaryKey]
        [Column("area_id")]
        public string AreaId { get; set; }

        [Column("mission_id")]
        public string MissionId { get; set; }

        [Column("area_order")]
        public int AreaOrder { get; set; }

        [Column("state")]
        public string State { get; set; }

        [Column("started_utc")]
        public string StartedUtc { get; set; }

        [Column("completed_utc")]
        public string CompletedUtc { get; set; }
    }

    [Table("question_outcome")]
    public sealed class QuestionOutcomeRecord
    {
        [PrimaryKey]
        [Column("mission_id")]
        public string MissionId { get; set; }

        [PrimaryKey]
        [Column("area_id")]
        public string AreaId { get; set; }

        [PrimaryKey]
        [Column("question_id")]
        public string QuestionId { get; set; }

        [PrimaryKey]
        [Column("attempt_number")]
        public int AttemptNumber { get; set; }

        [Column("outcome")]
        public string Outcome { get; set; }

        [Column("review_required")]
        public bool ReviewRequired { get; set; }

        [Column("answered_utc")]
        public string AnsweredUtc { get; set; }
    }

    [Table("collectible_state")]
    public sealed class CollectibleStateRecord
    {
        [PrimaryKey]
        [Column("collectible_id")]
        public string CollectibleId { get; set; }

        [Column("mission_id")]
        public string MissionId { get; set; }

        [Column("area_id")]
        public string AreaId { get; set; }

        [Column("collected")]
        public bool Collected { get; set; }

        [Column("collected_utc")]
        public string CollectedUtc { get; set; }
    }

    [Table("world_state")]
    public sealed class WorldStateRecord
    {
        [PrimaryKey]
        [Column("state_key")]
        public string StateKey { get; set; }

        [Column("mission_id")]
        public string MissionId { get; set; }

        [Column("area_id")]
        public string AreaId { get; set; }

        [Column("value_json")]
        public string ValueJson { get; set; }

        [Column("updated_utc")]
        public string UpdatedUtc { get; set; }
    }

    [Table("sync_outbox")]
    public sealed class SyncOutboxRecord
    {
        [PrimaryKey]
        [Column("event_uuid")]
        public string EventUuid { get; set; }

        [Unique]
        [Column("local_sequence")]
        public long LocalSequence { get; set; }

        [Column("event_type")]
        public string EventType { get; set; }

        [Column("grade_id")]
        public string GradeId { get; set; }

        [Column("subject_id")]
        public string SubjectId { get; set; }

        [Column("term_id")]
        public string TermId { get; set; }

        [Column("mission_id")]
        public string MissionId { get; set; }

        [Column("area_id")]
        public string AreaId { get; set; }

        [Column("payload_json")]
        public string PayloadJson { get; set; }

        [Column("client_created_utc")]
        public string ClientCreatedUtc { get; set; }

        [Column("state")]
        public string State { get; set; }

        [Column("attempt_count")]
        public int AttemptCount { get; set; }

        [Column("last_error_code")]
        public string LastErrorCode { get; set; }

        [Column("last_attempt_utc")]
        public string LastAttemptUtc { get; set; }

        [Column("server_revision")]
        public int? ServerRevision { get; set; }
    }

    [Table("announcement_read_state")]
    public sealed class AnnouncementReadStateRecord
    {
        [PrimaryKey]
        [Column("announcement_key")]
        public string AnnouncementKey { get; set; }

        [Column("read_utc")]
        public string ReadUtc { get; set; }
    }

    [Table("idempotent_request")]
    public sealed class IdempotentRequestRecord
    {
        [PrimaryKey]
        [Column("request_uuid")]
        public string RequestUuid { get; set; }

        [Column("operation")]
        public string Operation { get; set; }

        [Column("student_id")]
        public string StudentId { get; set; }

        [Column("entity_key")]
        public string EntityKey { get; set; }

        [Column("normalized_payload_json")]
        public string NormalizedPayloadJson { get; set; }

        [Column("state")]
        public string State { get; set; }

        [Column("result_json")]
        public string ResultJson { get; set; }

        [Column("created_utc")]
        public string CreatedUtc { get; set; }

        [Column("updated_utc")]
        public string UpdatedUtc { get; set; }
    }
}
