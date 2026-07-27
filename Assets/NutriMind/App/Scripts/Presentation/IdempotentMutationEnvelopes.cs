using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using NutriMind.Core.Data;
using UnityEngine;

namespace NutriMind.App.Presentation
{
    public static class IdempotentOperations
    {
        public const string UseReward = "use_reward";
        public const string QuizSubmit = "quiz_submit";
    }

    [Serializable]
    public sealed class PendingRewardUseEnvelopeV1
    {
        public int Version = 1;
        public string RewardCode;
        public string RequestUuid;
    }

    [Serializable]
    public sealed class PendingRewardUseEnvelopeV2
    {
        public int Version = 2;
        public string StudentId;
        public string RewardCode;
        public string RequestUuid;
    }

    [Serializable]
    public sealed class PendingQuizSubmissionEnvelopeV1
    {
        public int Version = 1;
        public string QuizId;
        public QuizAttemptSubmission Submission;
    }

    [Serializable]
    public sealed class PendingQuizSubmissionEnvelopeV2
    {
        public int Version = 2;
        public string StudentId;
        public string QuizId;
        public QuizAttemptSubmission Submission;
    }

    /// <summary>
    /// Deterministic serializers for versioned idempotent mutation envelopes.
    /// Writes V2; reads V1 and V2 for safe recovery.
    /// </summary>
    public static class IdempotentMutationSerializers
    {
        public static string SerializeReward(PendingRewardUseEnvelopeV2 envelope)
        {
            if (envelope == null)
            {
                throw new ArgumentNullException(nameof(envelope));
            }

            if (envelope.Version != 2)
            {
                throw new InvalidOperationException(
                    "Unsupported reward envelope version: " + envelope.Version);
            }

            if (string.IsNullOrWhiteSpace(envelope.StudentId)
                || string.IsNullOrWhiteSpace(envelope.RewardCode)
                || string.IsNullOrWhiteSpace(envelope.RequestUuid))
            {
                throw new InvalidOperationException(
                    "Reward envelope requires StudentId, RewardCode, and RequestUuid.");
            }

            var dto = new RewardEnvelopeDto
            {
                Version = 2,
                StudentId = envelope.StudentId,
                RewardCode = envelope.RewardCode,
                RequestUuid = envelope.RequestUuid
            };
            return JsonUtility.ToJson(dto);
        }

        public static PendingRewardUseEnvelopeV2 DeserializeReward(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new InvalidOperationException("Reward envelope JSON is empty.");
            }

            RewardEnvelopeDto dto = JsonUtility.FromJson<RewardEnvelopeDto>(json);
            if (dto == null || (dto.Version != 1 && dto.Version != 2))
            {
                throw new InvalidOperationException(
                    "Unsupported or malformed reward envelope version.");
            }

            if (dto.Version == 2 && string.IsNullOrWhiteSpace(dto.StudentId))
            {
                throw new InvalidOperationException(
                    "Reward envelope V2 requires StudentId.");
            }

            return new PendingRewardUseEnvelopeV2
            {
                Version = 2,
                StudentId = dto.StudentId,
                RewardCode = dto.RewardCode,
                RequestUuid = dto.RequestUuid
            };
        }

        public static string SerializeQuiz(PendingQuizSubmissionEnvelopeV2 envelope)
        {
            if (envelope == null)
            {
                throw new ArgumentNullException(nameof(envelope));
            }

            if (envelope.Version != 2)
            {
                throw new InvalidOperationException(
                    "Unsupported quiz envelope version: " + envelope.Version);
            }

            if (string.IsNullOrWhiteSpace(envelope.StudentId)
                || string.IsNullOrWhiteSpace(envelope.QuizId))
            {
                throw new InvalidOperationException(
                    "Quiz envelope requires StudentId and QuizId.");
            }

            QuizAttemptSubmission submission = envelope.Submission
                ?? throw new InvalidOperationException("Quiz envelope requires Submission.");

            var sb = new StringBuilder(512);
            sb.Append("{\"Version\":2,\"StudentId\":");
            AppendJsonString(sb, envelope.StudentId);
            sb.Append(",\"QuizId\":");
            AppendJsonString(sb, envelope.QuizId);
            sb.Append(",\"Submission\":{");
            sb.Append("\"ClientAttemptUuid\":");
            AppendJsonString(sb, submission.ClientAttemptUuid);
            sb.Append(",\"StartedAt\":");
            AppendJsonString(sb, FormatUtc(submission.StartedAt));
            sb.Append(",\"SubmittedAt\":");
            AppendJsonString(sb, FormatUtc(submission.SubmittedAt));
            sb.Append(",\"Answers\":[");

            IReadOnlyList<QuizAnswerSelection> answers =
                submission.Answers ?? Array.Empty<QuizAnswerSelection>();
            for (int i = 0; i < answers.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(',');
                }

                QuizAnswerSelection answer = answers[i] ?? new QuizAnswerSelection();
                sb.Append("{\"QuestionId\":");
                AppendJsonString(sb, answer.QuestionId);
                sb.Append(",\"SelectedOptionKeys\":[");
                IReadOnlyList<string> keys =
                    answer.SelectedOptionKeys ?? Array.Empty<string>();
                for (int k = 0; k < keys.Count; k++)
                {
                    if (k > 0)
                    {
                        sb.Append(',');
                    }

                    AppendJsonString(sb, keys[k]);
                }

                sb.Append("]}");
            }

            sb.Append("]}}");
            return sb.ToString();
        }

        public static PendingQuizSubmissionEnvelopeV2 DeserializeQuiz(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new InvalidOperationException("Quiz envelope JSON is empty.");
            }

            QuizEnvelopeDto dto = JsonUtility.FromJson<QuizEnvelopeDto>(json);
            if (dto == null || (dto.Version != 1 && dto.Version != 2) || dto.Submission == null)
            {
                throw new InvalidOperationException(
                    "Unsupported or malformed quiz envelope version.");
            }

            if (dto.Version == 2 && string.IsNullOrWhiteSpace(dto.StudentId))
            {
                throw new InvalidOperationException(
                    "Quiz envelope V2 requires StudentId.");
            }

            var answers = new List<QuizAnswerSelection>();
            if (dto.Submission.Answers != null)
            {
                for (int i = 0; i < dto.Submission.Answers.Length; i++)
                {
                    QuizAnswerDto answer = dto.Submission.Answers[i];
                    if (answer == null)
                    {
                        continue;
                    }

                    answers.Add(new QuizAnswerSelection
                    {
                        QuestionId = answer.QuestionId,
                        SelectedOptionKeys = answer.SelectedOptionKeys ?? Array.Empty<string>()
                    });
                }
            }

            return new PendingQuizSubmissionEnvelopeV2
            {
                Version = 2,
                StudentId = dto.StudentId,
                QuizId = dto.QuizId,
                Submission = new QuizAttemptSubmission
                {
                    ClientAttemptUuid = dto.Submission.ClientAttemptUuid,
                    StartedAt = ParseUtc(dto.Submission.StartedAt),
                    SubmittedAt = ParseUtc(dto.Submission.SubmittedAt),
                    Answers = answers
                }
            };
        }

        private static string FormatUtc(DateTimeOffset? value)
        {
            return value?.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture) ?? string.Empty;
        }

        private static DateTimeOffset? ParseUtc(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (DateTimeOffset.TryParse(
                    value,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out DateTimeOffset parsed))
            {
                return parsed.ToUniversalTime();
            }

            return null;
        }

        private static void AppendJsonString(StringBuilder sb, string value)
        {
            sb.Append('"');
            string text = value ?? string.Empty;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                switch (c)
                {
                    case '\\':
                        sb.Append("\\\\");
                        break;
                    case '"':
                        sb.Append("\\\"");
                        break;
                    case '\n':
                        sb.Append("\\n");
                        break;
                    case '\r':
                        sb.Append("\\r");
                        break;
                    case '\t':
                        sb.Append("\\t");
                        break;
                    default:
                        if (c < 32)
                        {
                            sb.Append("\\u");
                            sb.Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            sb.Append(c);
                        }

                        break;
                }
            }

            sb.Append('"');
        }

        [Serializable]
        private sealed class RewardEnvelopeDto
        {
            public int Version;
            public string StudentId;
            public string RewardCode;
            public string RequestUuid;
        }

        [Serializable]
        private sealed class QuizEnvelopeDto
        {
            public int Version;
            public string StudentId;
            public string QuizId;
            public QuizSubmissionDto Submission;
        }

        [Serializable]
        private sealed class QuizSubmissionDto
        {
            public string ClientAttemptUuid;
            public string StartedAt;
            public string SubmittedAt;
            public QuizAnswerDto[] Answers;
        }

        [Serializable]
        private sealed class QuizAnswerDto
        {
            public string QuestionId;
            public string[] SelectedOptionKeys;
        }
    }
}
