namespace NutriMind.Core.Persistence
{
    /// <summary>
    /// Suggested resource_cache.cache_key patterns. Leaderboard is never cached.
    /// Learner-dependent resources must be scoped by StudentProfile.Id.
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

        public static string ForStudent(string studentId, string resourceKey)
        {
            string student = string.IsNullOrWhiteSpace(studentId) ? string.Empty : studentId.Trim();
            string resource = string.IsNullOrWhiteSpace(resourceKey) ? string.Empty : resourceKey.Trim();
            return "student:" + student + ":" + resource;
        }

        public static string StudentSubjects(string studentId) =>
            ForStudent(studentId, Subjects);

        public static string StudentProgressSummary(string studentId) =>
            ForStudent(studentId, ProgressSummary);

        public static string StudentRewards(string studentId) =>
            ForStudent(studentId, Rewards);

        public static string StudentCertificates(string studentId) =>
            ForStudent(studentId, Certificates);

        public static string StudentAnnouncements(string studentId) =>
            ForStudent(studentId, Announcements);

        public static string StudentTerms(string studentId, string subjectId) =>
            ForStudent(studentId, Terms(subjectId));

        public static string StudentMissions(string studentId, string subjectId, string termId) =>
            ForStudent(studentId, Missions(subjectId, termId));

        public static string StudentQuizResults(string studentId, string queryKey) =>
            ForStudent(studentId, QuizResults(queryKey));
    }
}
