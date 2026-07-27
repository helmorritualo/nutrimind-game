using System;

namespace NutriMind.Core.Networking
{
    /// <summary>
    /// JsonUtility-compatible mock-only fixture DTOs. Not production API DTOs.
    /// </summary>
    [Serializable]
    public sealed class MockSectionFixture
    {
        public string id;
        public string name;
        public string grade_id;
    }

    [Serializable]
    public sealed class MockStudentProfileFixture
    {
        public string id;
        public string display_name;
        public string lrn_masked;
        public string grade_id;
        public bool is_active;
        public MockSectionFixture section;
    }

    [Serializable]
    public sealed class MockLoginSuccessFixture
    {
        public string token_type;
        public string access_token;
        public string expires_at;
        public MockStudentProfileFixture student;
    }

    [Serializable]
    public sealed class MockConfigFixture
    {
        public string api_version;
        public string minimum_client_version;
        public string required_manifest_version;
        public bool maintenance_mode;
        public string maintenance_message;
        public int sync_max_events_per_batch;
        public int sync_max_request_bytes;
        public int sync_max_event_payload_bytes;
        public int sync_max_event_age_days;
    }

    [Serializable]
    public sealed class MockSettingsFixture
    {
        public float audio_volume;
        public float music_volume;
        public string language;
        public bool reduced_motion;
        public bool notifications_enabled;
    }

    [Serializable]
    public sealed class MockSubjectFixture
    {
        public string id;
        public string slug;
        public string name;
        public bool is_active;
    }

    [Serializable]
    public sealed class MockSubjectListFixture
    {
        public MockSubjectFixture[] items;
    }

    [Serializable]
    public sealed class MockTermFixture
    {
        public string id;
        public string name;
        public int order;
        public bool is_active;
    }

    [Serializable]
    public sealed class MockTermListFixture
    {
        public MockTermFixture[] items;
    }

    [Serializable]
    public sealed class MockMissionProgressFixture
    {
        public string state;
        public string active_area_id;
        public int completed_area_count;
        public int required_area_count;
        public int collectible_count;
        public int required_collectible_count;
        public string completed_at;
        public int revision;
    }

    [Serializable]
    public sealed class MockMissionSummaryFixture
    {
        public string id;
        public string grade_id;
        public string subject_id;
        public string term_id;
        public string title;
        public int order;
        public string status;
        public string locked_reason;
        public string availability_source;
        public string teacher_policy;
        public int area_count;
        public MockMissionProgressFixture progress;
    }

    [Serializable]
    public sealed class MockMissionListFixture
    {
        public MockMissionSummaryFixture[] items;
    }

    [Serializable]
    public sealed class MockAreaProgressFixture
    {
        public string id;
        public int order;
        public string phase;
        public string state;
        public bool review_required;
        public string collectible_id;
        public bool collectible_collected;
        public string completed_at;
    }

    [Serializable]
    public sealed class MockMissionDetailFixture
    {
        public MockMissionSummaryFixture mission;
        public MockAreaProgressFixture[] areas;
        public string[] newly_unlocked_ids;
    }

    [Serializable]
    public sealed class MockSyncStatusFixture
    {
        public bool pending_server_actions;
        public int revision;
        public int pending_outbox_count;
        public string last_synced_at;
    }

    [Serializable]
    public sealed class MockBootstrapFixture
    {
        public MockStudentProfileFixture profile;
        public string required_manifest_version;
        public MockSubjectFixture[] subjects;
        public MockMissionSummaryFixture[] missions;
        public int quiz_portal_available_count;
        public int announcements_visible_count;
        public MockSyncStatusFixture sync;
    }

    [Serializable]
    public sealed class MockProgressSummaryFixture
    {
        public int missions_started;
        public int missions_completed;
        public int areas_completed;
        public int review_required_count;
        public int quiz_attempts;
    }

    [Serializable]
    public sealed class MockQuizSummaryFixture
    {
        public string id;
        public string title;
        public string subject_id;
        public string term_id;
        public string status;
        public string locked_reason;
        public string opens_at;
        public string closes_at;
        public int max_attempts;
        public int attempts_used;
        public string result_visibility;
    }

    [Serializable]
    public sealed class MockQuizListFixture
    {
        public MockQuizSummaryFixture[] items;
    }

    [Serializable]
    public sealed class MockQuizOptionFixture
    {
        public string key;
        public string text;
    }

    [Serializable]
    public sealed class MockQuizQuestionFixture
    {
        public string id;
        public string type;
        public string prompt;
        public string hint;
        public int points;
        public MockQuizOptionFixture[] options;
    }

    [Serializable]
    public sealed class MockQuizDetailFixture
    {
        public string id;
        public string title;
        public string instructions;
        public string subject_id;
        public string term_id;
        public string status;
        public string locked_reason;
        public string opens_at;
        public string closes_at;
        public int max_attempts;
        public int attempts_used;
        public string result_visibility;
        public int question_count;
        public MockQuizQuestionFixture[] questions;
    }

    [Serializable]
    public sealed class MockStringListFixture
    {
        public string[] items;
    }

    [Serializable]
    public sealed class MockQuizResultAnswerFixture
    {
        public string question_id;
        public bool correct;
        public float earned_points;
        public string[] selected_option_keys;
    }

    [Serializable]
    public sealed class MockQuizResultFixture
    {
        public string attempt_id;
        public string quiz_id;
        public string client_attempt_uuid;
        public string status;
        public float earned_points;
        public float possible_points;
        public float percentage;
        public bool passed;
        public int correct_count;
        public int incorrect_count;
        public int unanswered_count;
        public string submitted_at;
        public bool feedback_visible;
        public MockQuizResultAnswerFixture[] answers;
    }

    [Serializable]
    public sealed class MockQuizHistoryEntryFixture
    {
        public string attempt_id;
        public string quiz_id;
        public string quiz_title;
        public string subject_id;
        public string term_id;
        public string status;
        public float percentage;
        public bool passed;
        public string submitted_at;
        public bool feedback_visible;
    }

    [Serializable]
    public sealed class MockQuizHistoryListFixture
    {
        public MockQuizHistoryEntryFixture[] items;
    }

    [Serializable]
    public sealed class MockRewardFixture
    {
        public string reward_code;
        public string title;
        public string description;
        public string supporting_text;
        public string status;
        public string locked_reason;
        public string earned_at;
        public string used_at;
    }

    [Serializable]
    public sealed class MockRewardListFixture
    {
        public MockRewardFixture[] items;
    }

    [Serializable]
    public sealed class MockCertificateFixture
    {
        public string id;
        public string title;
        public string type_label;
        public string status;
        public string eligibility_description;
        public string recognition_text;
        public string locked_reason;
        public string issued_at;
        public string awarded_to_display_name;
    }

    [Serializable]
    public sealed class MockCertificateListFixture
    {
        public MockCertificateFixture[] items;
    }

    [Serializable]
    public sealed class MockAnnouncementFixture
    {
        public string id;
        public string title;
        public string summary;
        public string body;
        public string audience_label;
        public string kind;
        public bool is_unread;
        public string published_at;
        public string expires_at;
    }

    [Serializable]
    public sealed class MockAnnouncementListFixture
    {
        public MockAnnouncementFixture[] items;
    }

    [Serializable]
    public sealed class MockLeaderboardContextFixture
    {
        public string scope;
        public string scope_label;
        public string metric;
        public string metric_label;
        public string period_label;
        public string context_label;
    }

    [Serializable]
    public sealed class MockLeaderboardEntryFixture
    {
        public int rank;
        public string privacy_safe_name;
        public int missions_completed;
        public bool is_current_student;
    }

    [Serializable]
    public sealed class MockLeaderboardFixture
    {
        public MockLeaderboardContextFixture context;
        public MockLeaderboardEntryFixture[] entries;
    }
}
