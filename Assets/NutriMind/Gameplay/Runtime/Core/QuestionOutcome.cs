namespace NutriMind.Gameplay.Runtime
{
    public enum QuestionResult
    {
        Unanswered = 0,
        Correct = 1,
        Incorrect = 2
    }

    public sealed class QuestionOutcome
    {
        public string QuestionId { get; set; } = string.Empty;
        public int AttemptCount { get; set; }
        public QuestionResult Result { get; set; } = QuestionResult.Unanswered;
        public string SelectedOptionId { get; set; } = string.Empty;
        public bool ReviewRequired { get; set; }
        public bool Acknowledged { get; set; }
    }
}
