namespace NutriMind.App.Routing
{
    /// <summary>
    /// In-scene application routes. Scene load is separate from route push/replace.
    /// </summary>
    public enum AppRouteId
    {
        // Main scene routes
        Home = 0,
        Subjects = 1,
        Terms = 2,
        MissionList = 3,
        LockedMission = 4,
        MissionDetail = 5,
        Profile = 6,
        Settings = 7,
        Progress = 8,
        Rewards = 9,
        Certificates = 10,
        Announcements = 11,
        Leaderboard = 12,

        // Quiz Portal scene routes
        QuizList = 100,
        QuizDetail = 101,
        QuizAttempt = 102,
        QuizResult = 103,
        QuizHistory = 104
    }
}
