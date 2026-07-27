namespace NutriMind.Core.Data
{
    /// <summary>
    /// Aggregate learner progress for the Progress screen.
    /// </summary>
    public sealed class ProgressSummary
    {
        public int MissionsStarted { get; set; }
        public int MissionsCompleted { get; set; }
        public int AreasCompleted { get; set; }
        public int ReviewRequiredCount { get; set; }
        public int QuizAttempts { get; set; }
    }
}
