namespace NutriMind.App.Routing
{
    /// <summary>
    /// Typed route payload. Never uses a loosely typed dictionary.
    /// </summary>
    public sealed class AppRouteContext
    {
        public static AppRouteContext Empty { get; } = new AppRouteContext();

        public string SubjectId { get; private set; }
        public string SubjectSlug { get; private set; }
        public string TermId { get; private set; }
        public string MissionId { get; private set; }
        public string QuizId { get; private set; }
        public string AttemptId { get; private set; }
        public string CertificateId { get; private set; }
        public string LockReason { get; private set; }
        public bool ReturnToMainOnQuizBack { get; private set; }

        public static AppRouteContext ForSubject(string subjectId, string subjectSlug = null)
        {
            return new AppRouteContext
            {
                SubjectId = Normalize(subjectId),
                SubjectSlug = Normalize(subjectSlug)
            };
        }

        public static AppRouteContext ForTerm(string subjectId, string termId, string subjectSlug = null)
        {
            return new AppRouteContext
            {
                SubjectId = Normalize(subjectId),
                SubjectSlug = Normalize(subjectSlug),
                TermId = Normalize(termId)
            };
        }

        public static AppRouteContext ForMission(string missionId, string subjectId = null, string termId = null)
        {
            return new AppRouteContext
            {
                MissionId = Normalize(missionId),
                SubjectId = Normalize(subjectId),
                TermId = Normalize(termId)
            };
        }

        public static AppRouteContext ForLockedMission(string missionId, string lockReason)
        {
            return new AppRouteContext
            {
                MissionId = Normalize(missionId),
                LockReason = Normalize(lockReason)
            };
        }

        public static AppRouteContext ForQuiz(string quizId, string subjectId = null, string termId = null)
        {
            return new AppRouteContext
            {
                QuizId = Normalize(quizId),
                SubjectId = Normalize(subjectId),
                TermId = Normalize(termId)
            };
        }

        public static AppRouteContext ForQuizAttempt(string quizId, string attemptId = null)
        {
            return new AppRouteContext
            {
                QuizId = Normalize(quizId),
                AttemptId = Normalize(attemptId)
            };
        }

        public static AppRouteContext ForQuizResult(string attemptId, string quizId = null)
        {
            return new AppRouteContext
            {
                AttemptId = Normalize(attemptId),
                QuizId = Normalize(quizId)
            };
        }

        public static AppRouteContext ForCertificate(string certificateId)
        {
            return new AppRouteContext
            {
                CertificateId = Normalize(certificateId)
            };
        }

        public AppRouteContext WithReturnToMainOnQuizBack(bool value)
        {
            return new AppRouteContext
            {
                SubjectId = SubjectId,
                SubjectSlug = SubjectSlug,
                TermId = TermId,
                MissionId = MissionId,
                QuizId = QuizId,
                AttemptId = AttemptId,
                CertificateId = CertificateId,
                LockReason = LockReason,
                ReturnToMainOnQuizBack = value
            };
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
