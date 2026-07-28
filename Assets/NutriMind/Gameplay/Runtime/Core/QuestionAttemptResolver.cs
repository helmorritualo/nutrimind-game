using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace NutriMind.Gameplay.Runtime
{
    public static class QuestionAttemptResolver
    {
        public sealed class AttemptResult
        {
            public bool IsCorrect { get; set; }
            public bool ShowHint { get; set; }
            public bool ShowExplanation { get; set; }
            public bool ShowCorrectAcknowledgement { get; set; }
            public bool CanAdvance { get; set; }
            public string FeedbackText { get; set; } = string.Empty;
        }

        public static AttemptResult EvaluateAttempt(
            MissionQuestionDto question,
            QuestionOutcome outcome,
            string selectedOptionId)
        {
            if (question == null || outcome == null)
            {
                return new AttemptResult();
            }

            bool isCorrect = Array.Exists(
                question.correct_option_ids ?? Array.Empty<string>(),
                id => string.Equals(id, selectedOptionId, StringComparison.Ordinal));

            outcome.AttemptCount++;
            outcome.SelectedOptionId = selectedOptionId ?? string.Empty;

            if (isCorrect)
            {
                outcome.Result = QuestionResult.Correct;
                outcome.Acknowledged = false;
                return new AttemptResult
                {
                    IsCorrect = true,
                    ShowCorrectAcknowledgement = true,
                    CanAdvance = false,
                    FeedbackText = question.correct_feedback ?? string.Empty
                };
            }

            outcome.Result = QuestionResult.Incorrect;
            if (outcome.AttemptCount < question.attempt_limit)
            {
                return new AttemptResult
                {
                    IsCorrect = false,
                    ShowHint = true,
                    CanAdvance = false,
                    FeedbackText = question.first_wrong_hint ?? string.Empty
                };
            }

            outcome.ReviewRequired = true;
            return new AttemptResult
            {
                IsCorrect = false,
                ShowExplanation = true,
                CanAdvance = false,
                FeedbackText = question.second_wrong_explanation ?? string.Empty
            };
        }
    }
}
