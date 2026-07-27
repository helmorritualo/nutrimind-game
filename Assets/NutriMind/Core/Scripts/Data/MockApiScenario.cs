namespace NutriMind.Core.Data
{
    public enum MockApiScenario
    {
        HappyPath = 0,
        OfflineWithCache = 1,
        EmptyData = 2,
        LockedMission = 3,
        RateLimitedLogin = 4,
        RecoverableServerErrors = 5,
        QuizSubmissionTimeout = 6,
        RewardUseTimeout = 7,
        SyncConflict = 8,
        UnauthorizedAfterLogin = 9
    }
}
