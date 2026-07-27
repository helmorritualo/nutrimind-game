namespace NutriMind.Core.Data
{
    /// <summary>
    /// Leaderboard filter/context labels. Never cached offline.
    /// </summary>
    public sealed class LeaderboardContext
    {
        public string Scope { get; set; }
        public string ScopeLabel { get; set; }
        public string Metric { get; set; }
        public string MetricLabel { get; set; }
        public string PeriodLabel { get; set; }
        public string ContextLabel { get; set; }
    }

    /// <summary>
    /// One privacy-safe leaderboard row.
    /// </summary>
    public sealed class LeaderboardEntry
    {
        public int Rank { get; set; }
        public string PrivacySafeName { get; set; }
        public int MissionsCompleted { get; set; }
        public bool IsCurrentStudent { get; set; }
    }
}
