namespace NutriMind.Core.Networking
{
    /// <summary>
    /// Resources.Load paths under NutriMindMock/ (no extension).
    /// </summary>
    public static class MockFixtureNames
    {
        public const string ResourceRoot = "NutriMindMock";

        public const string LoginSuccess = "login_success";
        public const string Config = "config";
        public const string Bootstrap = "bootstrap";
        public const string Profile = "profile";
        public const string Settings = "settings";
        public const string Subjects = "subjects";
        public const string Terms = "terms";
        public const string Missions = "missions";
        public const string MissionDetail = "mission_detail";
        public const string ProgressSummary = "progress_summary";
        public const string Quizzes = "quizzes";
        public const string QuizDetail = "quiz_detail";
        public const string QuizResult = "quiz_result";
        public const string QuizHistory = "quiz_history";
        public const string Rewards = "rewards";
        public const string Certificates = "certificates";
        public const string Announcements = "announcements";
        public const string Leaderboard = "leaderboard";
        public const string SyncStatus = "sync_status";

        public static string ToResourcePath(string fixtureName)
        {
            if (string.IsNullOrWhiteSpace(fixtureName))
            {
                return ResourceRoot + "/";
            }

            string trimmed = fixtureName.Trim().Replace('\\', '/');
            if (trimmed.StartsWith(ResourceRoot + "/"))
            {
                return trimmed;
            }

            return ResourceRoot + "/" + trimmed.TrimStart('/');
        }
    }
}
