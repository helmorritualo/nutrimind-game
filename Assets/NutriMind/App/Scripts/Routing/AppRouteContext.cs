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
        public AppRouteOrigin Origin { get; private set; }

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

        public static AppRouteContext ForLockedMission(
            string missionId,
            string lockReason,
            string subjectId = null,
            string termId = null)
        {
            return new AppRouteContext
            {
                MissionId = Normalize(missionId),
                LockReason = Normalize(lockReason),
                SubjectId = Normalize(subjectId),
                TermId = Normalize(termId)
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

        public static AppRouteContext ForQuizAttempt(
            string quizId,
            string attemptId = null,
            string subjectId = null,
            string termId = null)
        {
            return new AppRouteContext
            {
                QuizId = Normalize(quizId),
                AttemptId = Normalize(attemptId),
                SubjectId = Normalize(subjectId),
                TermId = Normalize(termId)
            };
        }

        public static AppRouteContext ForQuizResult(
            string attemptId,
            string quizId = null,
            string subjectId = null,
            string termId = null)
        {
            return new AppRouteContext
            {
                AttemptId = Normalize(attemptId),
                QuizId = Normalize(quizId),
                SubjectId = Normalize(subjectId),
                TermId = Normalize(termId)
            };
        }

        public static AppRouteContext ForCertificate(
            string certificateId,
            AppRouteOrigin origin = AppRouteOrigin.None)
        {
            return new AppRouteContext
            {
                CertificateId = Normalize(certificateId),
                Origin = origin
            };
        }

        public AppRouteContext WithOrigin(AppRouteOrigin origin)
        {
            return CopyWith(origin: origin);
        }

        public AppRouteContext WithReturnToMainOnQuizBack(bool value)
        {
            return CopyWith(returnToMainOnQuizBack: value);
        }

        public AppRouteContext WithQuizIds(
            string quizId = null,
            string attemptId = null,
            string subjectId = null,
            string termId = null)
        {
            return CopyWith(
                quizId: quizId ?? QuizId,
                attemptId: attemptId ?? AttemptId,
                subjectId: subjectId ?? SubjectId,
                termId: termId ?? TermId);
        }

        private AppRouteContext CopyWith(
            string subjectId = null,
            string subjectSlug = null,
            string termId = null,
            string missionId = null,
            string quizId = null,
            string attemptId = null,
            string certificateId = null,
            string lockReason = null,
            bool? returnToMainOnQuizBack = null,
            AppRouteOrigin? origin = null)
        {
            return new AppRouteContext
            {
                SubjectId = subjectId ?? SubjectId,
                SubjectSlug = subjectSlug ?? SubjectSlug,
                TermId = termId ?? TermId,
                MissionId = missionId ?? MissionId,
                QuizId = quizId ?? QuizId,
                AttemptId = attemptId ?? AttemptId,
                CertificateId = certificateId ?? CertificateId,
                LockReason = lockReason ?? LockReason,
                ReturnToMainOnQuizBack = returnToMainOnQuizBack ?? ReturnToMainOnQuizBack,
                Origin = origin ?? Origin
            };
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
