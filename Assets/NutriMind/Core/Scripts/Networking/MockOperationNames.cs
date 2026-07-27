namespace NutriMind.Core.Networking
{
    /// <summary>
    /// Stable mock/scenario operation names used by gateway instrumentation and MockApiScenario rules.
    /// </summary>
    public static class MockOperationNames
    {
        public const string Ping = "ping.get";
        public const string Config = "config.get";
        public const string AuthLogin = "auth.login";
        public const string AuthLogout = "auth.logout";
        public const string BootstrapGet = "bootstrap.get";
        public const string ProfileGet = "profile.get";
        public const string SettingsGet = "settings.get";
        public const string SettingsPatch = "settings.patch";
        public const string SubjectsList = "subjects.list";
        public const string TermsList = "terms.list";
        public const string MissionsList = "missions.list";
        public const string MissionDetail = "mission.detail";
        public const string MissionProgress = "mission.progress";
        public const string MissionStart = "mission.start";
        public const string AreaStart = "area.start";
        public const string AreaEvent = "area.event";
        public const string AreaCollectible = "area.collectible";
        public const string AreaComplete = "area.complete";
        public const string QuizzesList = "quizzes.list";
        public const string QuizDetail = "quiz.detail";
        public const string QuizAttemptSubmit = "quiz.attempt.submit";
        public const string QuizResultsList = "quiz.results.list";
        public const string QuizResultGet = "quiz.result.get";
        public const string ProgressSummary = "progress.summary";
        public const string RewardsList = "rewards.list";
        public const string RewardUse = "reward.use";
        public const string CertificatesList = "certificates.list";
        public const string CertificateDetail = "certificate.detail";
        public const string AnnouncementsList = "announcements.list";
        public const string LeaderboardGet = "leaderboard.get";
        public const string SyncStatus = "sync.status";
        public const string SyncPush = "sync.push";
    }
}
