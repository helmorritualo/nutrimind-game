using NUnit.Framework;
using NutriMind.Gameplay.Runtime;

namespace NutriMind.Tests.EditMode.GameplayRuntime
{
    [TestFixture]
    public sealed class QuestionAttemptResolverTests
    {
        [Test]
        public void CorrectFirstAttempt_ShowsCorrectAcknowledgement()
        {
            MissionQuestionDto question = CreateQuestion();
            var outcome = new QuestionOutcome();

            QuestionAttemptResolver.AttemptResult result =
                QuestionAttemptResolver.EvaluateAttempt(question, outcome, "a");

            Assert.That(result.IsCorrect, Is.True);
            Assert.That(result.ShowCorrectAcknowledgement, Is.True);
            Assert.That(outcome.AttemptCount, Is.EqualTo(1));
        }

        [Test]
        public void WrongFirstAttempt_ReturnsHintState()
        {
            MissionQuestionDto question = CreateQuestion();
            var outcome = new QuestionOutcome();

            QuestionAttemptResolver.AttemptResult result =
                QuestionAttemptResolver.EvaluateAttempt(question, outcome, "b");

            Assert.That(result.ShowHint, Is.True);
            Assert.That(outcome.AttemptCount, Is.EqualTo(1));
        }

        [Test]
        public void CorrectSecondAttempt_ShowsCorrectAcknowledgement()
        {
            MissionQuestionDto question = CreateQuestion();
            var outcome = new QuestionOutcome();
            QuestionAttemptResolver.EvaluateAttempt(question, outcome, "b");

            QuestionAttemptResolver.AttemptResult result =
                QuestionAttemptResolver.EvaluateAttempt(question, outcome, "a");

            Assert.That(result.IsCorrect, Is.True);
            Assert.That(outcome.AttemptCount, Is.EqualTo(2));
        }

        [Test]
        public void WrongSecondAttempt_MarksReviewRequired()
        {
            MissionQuestionDto question = CreateQuestion();
            var outcome = new QuestionOutcome();
            QuestionAttemptResolver.EvaluateAttempt(question, outcome, "b");

            QuestionAttemptResolver.AttemptResult result =
                QuestionAttemptResolver.EvaluateAttempt(question, outcome, "c");

            Assert.That(result.ShowExplanation, Is.True);
            Assert.That(outcome.ReviewRequired, Is.True);
            Assert.That(outcome.AttemptCount, Is.EqualTo(2));
        }

        [Test]
        public void AttemptsNeverExceedTwo_ForPrototypeRule()
        {
            MissionQuestionDto question = CreateQuestion();
            var outcome = new QuestionOutcome();
            QuestionAttemptResolver.EvaluateAttempt(question, outcome, "b");
            QuestionAttemptResolver.EvaluateAttempt(question, outcome, "c");
            QuestionAttemptResolver.EvaluateAttempt(question, outcome, "d");

            Assert.That(outcome.AttemptCount, Is.EqualTo(2));
        }

        private static MissionQuestionDto CreateQuestion()
        {
            return new MissionQuestionDto
            {
                id = "test_q01",
                attempt_limit = 2,
                correct_option_ids = new[] { "a" },
                first_wrong_hint = "hint",
                second_wrong_explanation = "explanation",
                correct_feedback = "correct"
            };
        }
    }
}
