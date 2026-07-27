using System;
using System.Collections.Generic;
using NutriMind.Core.Data;

namespace NutriMind.App.Presentation
{
    /// <summary>
    /// Mutable session state for one in-progress quiz attempt.
    /// Retained by QuizPortalScreenCoordinator across uncertain-submit timeouts so the
    /// identical clientAttemptUuid and answer payload can be resubmitted idempotently.
    /// Never recalculates scores; quiz results are always server-authoritative.
    /// </summary>
    public sealed class QuizAttemptSession
    {
        private readonly Dictionary<string, List<string>> _answers =
            new Dictionary<string, List<string>>(StringComparer.Ordinal);

        public QuizAttemptSession(string quizId, string clientAttemptUuid, QuizDetail quizDetail)
        {
            QuizId = quizId ?? throw new ArgumentNullException(nameof(quizId));
            ClientAttemptUuid = clientAttemptUuid ?? throw new ArgumentNullException(nameof(clientAttemptUuid));
            QuizDetail = quizDetail ?? throw new ArgumentNullException(nameof(quizDetail));
            StartedAt = DateTimeOffset.UtcNow;
        }

        public string QuizId { get; }

        /// <summary>
        /// Stable UUID generated once per attempt. Reused verbatim on timeout-retry.
        /// </summary>
        public string ClientAttemptUuid { get; }

        public QuizDetail QuizDetail { get; }

        public DateTimeOffset StartedAt { get; }

        /// <summary>Current question index (0-based).</summary>
        public int CurrentIndex { get; private set; }

        public bool IsSubmitted { get; private set; }

        /// <summary>
        /// True while a submit request is in-flight. Prevents double-tap.
        /// </summary>
        public bool IsSubmitting { get; private set; }

        /// <summary>
        /// True when a submit attempt returned a network/timeout error.
        /// The session must be retained so the identical payload can be retried.
        /// </summary>
        public bool HasUncertainSubmit { get; private set; }

        public int TotalQuestions => QuizDetail.Questions?.Count ?? 0;

        public bool CanGoBack => CurrentIndex > 0 && !IsSubmitted && !IsSubmitting;

        public bool CanGoForward => CurrentIndex < TotalQuestions - 1 && !IsSubmitted && !IsSubmitting;

        public bool CanSubmit => CurrentIndex == TotalQuestions - 1 && !IsSubmitted && !IsSubmitting;

        /// <summary>
        /// Records selected option keys for the current question.
        /// Replaces any prior selection for that question.
        /// </summary>
        public void SetAnswer(string questionId, IReadOnlyList<string> selectedOptionKeys)
        {
            if (string.IsNullOrWhiteSpace(questionId) || IsSubmitted)
            {
                return;
            }

            if (!_answers.TryGetValue(questionId, out List<string> list))
            {
                list = new List<string>();
                _answers[questionId] = list;
            }
            else
            {
                list.Clear();
            }

            if (selectedOptionKeys != null)
            {
                for (int i = 0; i < selectedOptionKeys.Count; i++)
                {
                    list.Add(selectedOptionKeys[i]);
                }
            }
        }

        public IReadOnlyList<string> GetAnswer(string questionId)
        {
            if (string.IsNullOrWhiteSpace(questionId))
            {
                return Array.Empty<string>();
            }

            return _answers.TryGetValue(questionId, out List<string> list)
                ? list
                : Array.Empty<string>();
        }

        public void NavigateTo(int index)
        {
            if (IsSubmitted || IsSubmitting)
            {
                return;
            }

            CurrentIndex = Math.Max(0, Math.Min(index, TotalQuestions - 1));
        }

        public void BeginSubmit()
        {
            IsSubmitting = true;
            HasUncertainSubmit = false;
        }

        public void MarkUncertainSubmit()
        {
            IsSubmitting = false;
            HasUncertainSubmit = true;
        }

        public void MarkSubmitFailed()
        {
            IsSubmitting = false;
        }

        public void MarkSubmitted()
        {
            IsSubmitting = false;
            IsSubmitted = true;
            HasUncertainSubmit = false;
        }

        /// <summary>
        /// Builds the normalized submission payload from the current answer state.
        /// Called both on first submit and on idempotent retry after uncertain timeout.
        /// </summary>
        public QuizAttemptSubmission BuildSubmission()
        {
            var answers = new List<QuizAnswerSelection>();
            if (QuizDetail.Questions != null)
            {
                for (int i = 0; i < QuizDetail.Questions.Count; i++)
                {
                    string qId = QuizDetail.Questions[i].Id;
                    IReadOnlyList<string> selected = GetAnswer(qId);
                    var selCopy = new List<string>();
                    for (int k = 0; k < selected.Count; k++)
                    {
                        selCopy.Add(selected[k]);
                    }

                    answers.Add(new QuizAnswerSelection
                    {
                        QuestionId = qId,
                        SelectedOptionKeys = selCopy
                    });
                }
            }

            return new QuizAttemptSubmission
            {
                ClientAttemptUuid = ClientAttemptUuid,
                StartedAt = StartedAt,
                SubmittedAt = DateTimeOffset.UtcNow,
                Answers = answers
            };
        }
    }
}
