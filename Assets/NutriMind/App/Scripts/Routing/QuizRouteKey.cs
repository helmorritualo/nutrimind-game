using System;

namespace NutriMind.App.Routing
{
    /// <summary>
    /// Stable identity for Quiz Portal remount decisions. Compares full route context,
    /// not RouteId alone, so QuizDetail A→B remounts while exact duplicate events do not.
    /// </summary>
    public readonly struct QuizRouteKey : IEquatable<QuizRouteKey>
    {
        public QuizRouteKey(
            AppRouteId routeId,
            string quizId,
            string attemptId,
            string subjectId,
            string termId,
            bool returnToMainOnQuizBack)
        {
            RouteId = routeId;
            QuizId = Normalize(quizId);
            AttemptId = Normalize(attemptId);
            SubjectId = Normalize(subjectId);
            TermId = Normalize(termId);
            ReturnToMainOnQuizBack = returnToMainOnQuizBack;
        }

        public AppRouteId RouteId { get; }
        public string QuizId { get; }
        public string AttemptId { get; }
        public string SubjectId { get; }
        public string TermId { get; }
        public bool ReturnToMainOnQuizBack { get; }

        public static QuizRouteKey FromEntry(AppRouteEntry entry)
        {
            AppRouteContext ctx = entry.Context ?? AppRouteContext.Empty;
            return new QuizRouteKey(
                entry.RouteId,
                ctx.QuizId,
                ctx.AttemptId,
                ctx.SubjectId,
                ctx.TermId,
                ctx.ReturnToMainOnQuizBack);
        }

        public bool Equals(QuizRouteKey other)
        {
            return RouteId == other.RouteId
                   && string.Equals(QuizId, other.QuizId, StringComparison.Ordinal)
                   && string.Equals(AttemptId, other.AttemptId, StringComparison.Ordinal)
                   && string.Equals(SubjectId, other.SubjectId, StringComparison.Ordinal)
                   && string.Equals(TermId, other.TermId, StringComparison.Ordinal)
                   && ReturnToMainOnQuizBack == other.ReturnToMainOnQuizBack;
        }

        public override bool Equals(object obj)
        {
            return obj is QuizRouteKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)RouteId;
                hash = (hash * 397) ^ (QuizId != null ? StringComparer.Ordinal.GetHashCode(QuizId) : 0);
                hash = (hash * 397) ^ (AttemptId != null ? StringComparer.Ordinal.GetHashCode(AttemptId) : 0);
                hash = (hash * 397) ^ (SubjectId != null ? StringComparer.Ordinal.GetHashCode(SubjectId) : 0);
                hash = (hash * 397) ^ (TermId != null ? StringComparer.Ordinal.GetHashCode(TermId) : 0);
                hash = (hash * 397) ^ ReturnToMainOnQuizBack.GetHashCode();
                return hash;
            }
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
