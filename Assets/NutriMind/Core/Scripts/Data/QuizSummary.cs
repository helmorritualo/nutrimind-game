using System;
using System.Collections.Generic;

namespace NutriMind.Core.Data
{
    /// <summary>
    /// Quiz Portal list card summary.
    /// </summary>
    public sealed class QuizSummary
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string SubjectId { get; set; }
        public string TermId { get; set; }
        public string Status { get; set; }
        public string LockedReason { get; set; }
        public DateTimeOffset? OpensAt { get; set; }
        public DateTimeOffset? ClosesAt { get; set; }
        public int MaxAttempts { get; set; }
        public int AttemptsUsed { get; set; }
        public string ResultVisibility { get; set; }
    }

    /// <summary>
    /// Quiz detail for attempt entry. Questions never include answer keys.
    /// </summary>
    public sealed class QuizDetail
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Instructions { get; set; }
        public string SubjectId { get; set; }
        public string TermId { get; set; }
        public string Status { get; set; }
        public string LockedReason { get; set; }
        public DateTimeOffset? OpensAt { get; set; }
        public DateTimeOffset? ClosesAt { get; set; }
        public int MaxAttempts { get; set; }
        public int AttemptsUsed { get; set; }
        public string ResultVisibility { get; set; }
        public int QuestionCount { get; set; }
        public IReadOnlyList<QuizQuestionDelivery> Questions { get; set; } = Array.Empty<QuizQuestionDelivery>();
    }

    /// <summary>
    /// Question payload delivered to the client. Never includes correct answers or scoring keys.
    /// </summary>
    public sealed class QuizQuestionDelivery
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public string Prompt { get; set; }
        public IReadOnlyList<QuizOptionDelivery> Options { get; set; } = Array.Empty<QuizOptionDelivery>();
        public string Hint { get; set; }
        public int Points { get; set; }
    }

    /// <summary>
    /// Visible answer option for a delivered quiz question.
    /// </summary>
    public sealed class QuizOptionDelivery
    {
        public string Key { get; set; }
        public string Text { get; set; }
    }

    /// <summary>
    /// Learner answer selection for one question within an attempt submission.
    /// </summary>
    public sealed class QuizAnswerSelection
    {
        public string QuestionId { get; set; }
        public IReadOnlyList<string> SelectedOptionKeys { get; set; } = Array.Empty<string>();
    }

    /// <summary>
    /// Client-authored quiz attempt payload submitted to the server for scoring.
    /// </summary>
    public sealed class QuizAttemptSubmission
    {
        public string ClientAttemptUuid { get; set; }
        public DateTimeOffset? StartedAt { get; set; }
        public DateTimeOffset? SubmittedAt { get; set; }
        public DateTimeOffset? OfflineCreatedAt { get; set; }
        public IReadOnlyList<QuizAnswerSelection> Answers { get; set; } = Array.Empty<QuizAnswerSelection>();
    }

    /// <summary>
    /// Immediate receipt after a quiz attempt is accepted.
    /// </summary>
    public sealed class QuizAttemptReceipt
    {
        public string AttemptId { get; set; }
        public string QuizId { get; set; }
        public string ClientAttemptUuid { get; set; }
        public string Status { get; set; }
        public DateTimeOffset SubmittedAt { get; set; }
        public bool FeedbackVisible { get; set; }
    }

    /// <summary>
    /// Server-authoritative quiz result. Scores are never recalculated by the client.
    /// </summary>
    public sealed class QuizResult
    {
        public string AttemptId { get; set; }
        public string QuizId { get; set; }
        public string ClientAttemptUuid { get; set; }
        public string Status { get; set; }
        public float EarnedPoints { get; set; }
        public float PossiblePoints { get; set; }
        public float Percentage { get; set; }
        public bool? Passed { get; set; }
        public int CorrectCount { get; set; }
        public int IncorrectCount { get; set; }
        public int UnansweredCount { get; set; }
        public DateTimeOffset SubmittedAt { get; set; }
        public bool FeedbackVisible { get; set; }
        public IReadOnlyList<QuizResultAnswer> Answers { get; set; } = Array.Empty<QuizResultAnswer>();
    }

    /// <summary>
    /// Per-question outcome when feedback is visible. Does not include answer keys when hidden.
    /// </summary>
    public sealed class QuizResultAnswer
    {
        public string QuestionId { get; set; }
        public bool? Correct { get; set; }
        public float? EarnedPoints { get; set; }
        public IReadOnlyList<string> SelectedOptionKeys { get; set; } = Array.Empty<string>();
    }

    /// <summary>
    /// Compact history row for prior quiz attempts.
    /// </summary>
    public sealed class QuizHistoryEntry
    {
        public string AttemptId { get; set; }
        public string QuizId { get; set; }
        public string QuizTitle { get; set; }
        public string SubjectId { get; set; }
        public string TermId { get; set; }
        public string Status { get; set; }
        public float? Percentage { get; set; }
        public bool? Passed { get; set; }
        public DateTimeOffset SubmittedAt { get; set; }
        public bool FeedbackVisible { get; set; }
    }
}
