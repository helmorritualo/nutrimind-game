using System;
using System.Collections.Generic;

namespace NutriMind.Core.Data
{
    /// <summary>
    /// Offline cache eligibility for Student App routes.
    /// Leaderboard is never cached.
    /// </summary>
    public static class CachePolicy
    {
        public const string HomeComposite = "home.composite";
        public const string MissionList = "missions.list";
        public const string ProgressSummary = "progress.summary";
        public const string Profile = "profile.get";
        public const string Rewards = "rewards.list";
        public const string Certificates = "certificates.list";
        public const string Announcements = "announcements.list";
        public const string QuizHistory = "quiz.history";
        public const string Subjects = "subjects.list";
        public const string Terms = "terms.list";
        public const string Bootstrap = "bootstrap.get";

        public const string Login = "auth.login";
        public const string MissionDetail = "mission.detail";
        public const string QuizList = "quizzes.list";
        public const string QuizDetail = "quiz.detail";
        public const string QuizAttempt = "quiz.attempt.submit";
        public const string Leaderboard = "leaderboard.get";

        private static readonly HashSet<string> OfflineFallbackRoutes = new HashSet<string>(StringComparer.Ordinal)
        {
            HomeComposite,
            MissionList,
            ProgressSummary,
            Profile,
            Rewards,
            Certificates,
            Announcements,
            QuizHistory,
            Subjects,
            Terms,
            Bootstrap
        };

        private static readonly HashSet<string> OfflineUnavailableRoutes = new HashSet<string>(StringComparer.Ordinal)
        {
            Login,
            MissionDetail,
            QuizList,
            QuizDetail,
            QuizAttempt,
            Leaderboard
        };

        private static readonly HashSet<string> NeverCacheRoutes = new HashSet<string>(StringComparer.Ordinal)
        {
            Leaderboard
        };

        public static bool AllowsOfflineFallback(string routeKey)
        {
            if (string.IsNullOrWhiteSpace(routeKey))
            {
                return false;
            }

            return OfflineFallbackRoutes.Contains(routeKey.Trim());
        }

        public static bool IsOfflineUnavailable(string routeKey)
        {
            if (string.IsNullOrWhiteSpace(routeKey))
            {
                return true;
            }

            return OfflineUnavailableRoutes.Contains(routeKey.Trim());
        }

        public static bool AllowsCache(string routeKey)
        {
            if (string.IsNullOrWhiteSpace(routeKey))
            {
                return false;
            }

            string key = routeKey.Trim();
            if (NeverCacheRoutes.Contains(key))
            {
                return false;
            }

            return OfflineFallbackRoutes.Contains(key);
        }

        public static IReadOnlyCollection<string> GetOfflineFallbackRoutes() => OfflineFallbackRoutes;

        public static IReadOnlyCollection<string> GetOfflineUnavailableRoutes() => OfflineUnavailableRoutes;
    }
}
