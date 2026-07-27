namespace NutriMind.Core.Persistence
{
    /// <summary>
    /// Suggested resource_cache.cache_key patterns. Leaderboard is never cached.
    /// </summary>
    public static class ResourceCacheKeys
    {
        public const string Bootstrap = "bootstrap";
        public const string Profile = "profile";
        public const string Subjects = "subjects";
        public const string ProgressSummary = "progress-summary";
        public const string Rewards = "rewards";
        public const string Certificates = "certificates";
        public const string Announcements = "announcements";

        public static string Terms(string subjectId) => "terms:" + subjectId;
        public static string Missions(string subjectId, string termId) => "missions:" + subjectId + ":" + termId;
        public static string MissionDetail(string missionId) => "mission-detail:" + missionId;
        public static string Quizzes(string queryKey) => "quizzes:" + queryKey;
        public static string QuizResults(string queryKey) => "quiz-results:" + queryKey;
    }
}
