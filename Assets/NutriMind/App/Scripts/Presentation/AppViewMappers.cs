using System;
using System.Collections.Generic;
using NutriMind.App.UI;
using NutriMind.Core.Data;
using NutriMind.Core.Networking;
using UnityEngine;

namespace NutriMind.App.Presentation
{
    /// <summary>
    /// Transitional domain-to-presentation mappers.
    /// Produces existing Preview* types used by views so views need no API awareness.
    /// These will be replaced by richer view-data types once views are fully runtime-ready.
    /// </summary>
    public static class AppViewMappers
    {
        // ──────────────────────────── Subject ────────────────────────────

        public static NutriMindSubject MapSubject(string subjectId)
        {
            if (TryMapSubject(subjectId, out NutriMindSubject subject))
            {
                return subject;
            }

            if (!string.IsNullOrWhiteSpace(subjectId))
            {
                Debug.LogWarning($"[AppViewMappers] Unknown subject identifier '{subjectId}'.");
            }

            return NutriMindSubject.LiteraQuest;
        }

        public static bool TryMapSubject(string subjectId, out NutriMindSubject subject)
        {
            subject = default;
            switch (NormalizeIdentifier(subjectId))
            {
                case "lq":
                case "literaquest":
                case "subjectliteraquest":
                    subject = NutriMindSubject.LiteraQuest;
                    return true;
                case "peh":
                case "pehealth":
                case "subjectpehealth":
                    subject = NutriMindSubject.PeAndHealth;
                    return true;
                case "sci":
                case "science":
                case "subjectscience":
                    subject = NutriMindSubject.Science;
                    return true;
                default:
                    return false;
            }
        }

        public static NutriMindTerm MapTerm(string termId)
        {
            if (TryMapTerm(termId, out NutriMindTerm term))
            {
                return term;
            }

            if (!string.IsNullOrWhiteSpace(termId))
            {
                Debug.LogWarning($"[AppViewMappers] Unknown term identifier '{termId}'.");
            }

            return NutriMindTerm.Term1;
        }

        public static bool TryMapTerm(string termId, out NutriMindTerm term)
        {
            term = default;
            switch (NormalizeIdentifier(termId))
            {
                case "t1":
                case "term1":
                    term = NutriMindTerm.Term1;
                    return true;
                case "t2":
                case "term2":
                    term = NutriMindTerm.Term2;
                    return true;
                case "t3":
                case "term3":
                    term = NutriMindTerm.Term3;
                    return true;
                default:
                    return false;
            }
        }

        private static string NormalizeIdentifier(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var normalized = new char[value.Length];
            int length = 0;
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (char.IsLetterOrDigit(c))
                {
                    normalized[length++] = char.ToLowerInvariant(c);
                }
            }

            return new string(normalized, 0, length);
        }

        // ──────────────────────────── Subject/Term reverse ───────────────

        public static string SubjectToId(NutriMindSubject subject)
        {
            switch (subject)
            {
                case NutriMindSubject.PeAndHealth:
                    return "peh";
                case NutriMindSubject.Science:
                    return "sci";
                default:
                    return "lq";
            }
        }

        public static string TermToId(NutriMindTerm term)
        {
            switch (term)
            {
                case NutriMindTerm.Term2:
                    return "t2";
                case NutriMindTerm.Term3:
                    return "t3";
                default:
                    return "t1";
            }
        }

        // ──────────────────────────── Mission list ───────────────────────

        public static MissionPreviewItem MapMissionSummaryToPreviewItem(
            MissionSummary mission,
            NutriMindSubject fallbackSubject,
            NutriMindTerm fallbackTerm)
        {
            if (mission == null)
            {
                return null;
            }

            NutriMindSubject subject = TryMapSubject(mission.SubjectId, out NutriMindSubject mappedSubject)
                ? mappedSubject
                : fallbackSubject;
            NutriMindTerm term = TryMapTerm(mission.TermId, out NutriMindTerm mappedTerm)
                ? mappedTerm
                : fallbackTerm;

            MissionProgressSummary progress = mission.Progress;
            string status = !string.IsNullOrWhiteSpace(progress?.State)
                ? progress.State
                : mission.Status;
            string normalizedStatus = NormalizeIdentifier(status);
            bool isLocked = NormalizeIdentifier(mission.Status) == "locked";
            MissionPreviewPrimaryAction action;
            if (isLocked)
            {
                action = MissionPreviewPrimaryAction.Locked;
            }
            else if (normalizedStatus == "completed" || normalizedStatus == "missioncompleted")
            {
                action = MissionPreviewPrimaryAction.Review;
            }
            else if (normalizedStatus == "inprogress"
                || normalizedStatus == "started"
                || normalizedStatus == "reviewrequired")
            {
                action = MissionPreviewPrimaryAction.Continue;
            }
            else
            {
                action = MissionPreviewPrimaryAction.Start;
            }

            int areasRequired = progress?.RequiredAreaCount ?? mission.AreaCount;
            if (areasRequired <= 0)
            {
                areasRequired = mission.AreaCount > 0 ? mission.AreaCount : 3;
            }

            int collectiblesRequired = progress?.RequiredCollectibleCount ?? 3;
            if (collectiblesRequired <= 0)
            {
                collectiblesRequired = 3;
            }

            return new MissionPreviewItem(
                mission.Id,
                string.IsNullOrWhiteSpace(mission.Title) ? "(Untitled mission)" : mission.Title,
                mission.Order,
                subject,
                term,
                status,
                isLocked,
                mission.LockedReason,
                progress?.CompletedAreaCount ?? 0,
                areasRequired,
                progress?.CollectibleCount ?? 0,
                collectiblesRequired,
                action);
        }

        public static MissionPreviewItem[] MapMissionSummaries(
            IReadOnlyList<MissionSummary> missions,
            NutriMindSubject fallbackSubject,
            NutriMindTerm fallbackTerm)
        {
            if (missions == null || missions.Count == 0)
            {
                return Array.Empty<MissionPreviewItem>();
            }

            var mapped = new List<MissionPreviewItem>(missions.Count);
            for (int i = 0; i < missions.Count; i++)
            {
                MissionPreviewItem item = MapMissionSummaryToPreviewItem(
                    missions[i],
                    fallbackSubject,
                    fallbackTerm);
                if (item != null && !string.IsNullOrWhiteSpace(item.MissionId))
                {
                    mapped.Add(item);
                }
            }

            return mapped.ToArray();
        }

        // ──────────────────────────── Progress ───────────────────────────

        public static ProgressPreviewSummary MapProgressSummary(
            ProgressSummary summary,
            int? pendingOutboxCount = null)
        {
            summary ??= new ProgressSummary();
            return new ProgressPreviewSummary(
                summary.MissionsStarted,
                summary.MissionsCompleted,
                summary.AreasCompleted,
                summary.ReviewRequiredCount,
                summary.QuizAttempts,
                pendingOutboxCount);
        }

        // ──────────────────────────── Announcements ──────────────────────

        public static bool IsAnnouncementEffectivelyUnread(
            AnnouncementSummary announcement,
            bool locallyMarkedRead)
        {
            return announcement != null && announcement.IsUnread && !locallyMarkedRead;
        }

        // ──────────────────────────── Quiz List ──────────────────────────

        public static QuizListPreviewItem MapQuizSummaryToPreviewItem(QuizSummary quiz)
        {
            if (quiz == null)
            {
                return default;
            }

            return new QuizListPreviewItem(
                id: quiz.Id ?? string.Empty,
                title: quiz.Title ?? "(Untitled)",
                subject: MapSubject(quiz.SubjectId),
                term: MapTerm(quiz.TermId),
                status: MapQuizStatus(quiz.Status),
                lockedReason: quiz.LockedReason,
                opensAtUtc: quiz.OpensAt,
                closesAtUtc: quiz.ClosesAt,
                maxAttempts: quiz.MaxAttempts,
                attemptsUsed: quiz.AttemptsUsed,
                resultVisibility: MapResultVisibility(quiz.ResultVisibility),
                subjectId: quiz.SubjectId,
                termId: quiz.TermId);
        }

        public static QuizListPreviewItem[] MapQuizSummaries(IReadOnlyList<QuizSummary> quizzes)
        {
            if (quizzes == null || quizzes.Count == 0)
            {
                return Array.Empty<QuizListPreviewItem>();
            }

            var items = new QuizListPreviewItem[quizzes.Count];
            for (int i = 0; i < quizzes.Count; i++)
            {
                items[i] = MapQuizSummaryToPreviewItem(quizzes[i]);
            }

            return items;
        }

        public static QuizListPreviewStatus MapQuizStatus(string status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return QuizListPreviewStatus.Unavailable;
            }

            switch (status.ToLowerInvariant())
            {
                case "available":
                    return QuizListPreviewStatus.Available;
                case "completed":
                    return QuizListPreviewStatus.Completed;
                case "locked":
                    return QuizListPreviewStatus.Locked;
                default:
                    return QuizListPreviewStatus.Unavailable;
            }
        }

        public static QuizListPreviewResultVisibility MapResultVisibility(string visibility)
        {
            if (string.IsNullOrWhiteSpace(visibility))
            {
                return QuizListPreviewResultVisibility.Hidden;
            }

            switch (visibility.ToLowerInvariant())
            {
                case "immediate":
                    return QuizListPreviewResultVisibility.Immediate;
                case "after_close":
                case "afterclose":
                    return QuizListPreviewResultVisibility.AfterClose;
                case "teacher_release":
                case "teacherrelease":
                    return QuizListPreviewResultVisibility.TeacherRelease;
                default:
                    return QuizListPreviewResultVisibility.Hidden;
            }
        }

        // ──────────────────────────── Profile ────────────────────────────

        /// <summary>
        /// Returns a masked LRN suitable for display. Uses server-provided masked value;
        /// never re-masks or shows the raw LRN.
        /// </summary>
        public static string MaskLrn(StudentProfile profile)
        {
            if (profile == null)
            {
                return string.Empty;
            }

            return string.IsNullOrWhiteSpace(profile.LrnMasked)
                ? "••••••••••••"
                : profile.LrnMasked;
        }

        public static string FormatDisplayName(StudentProfile profile)
        {
            if (profile == null)
            {
                return string.Empty;
            }

            return string.IsNullOrWhiteSpace(profile.DisplayName)
                ? "Student"
                : profile.DisplayName;
        }

        public static string FormatGradeSection(StudentProfile profile)
        {
            if (profile == null)
            {
                return string.Empty;
            }

            string grade = string.IsNullOrWhiteSpace(profile.GradeId) ? string.Empty : profile.GradeId;
            string section = profile.Section != null && !string.IsNullOrWhiteSpace(profile.Section.Name)
                ? profile.Section.Name
                : string.Empty;

            if (string.IsNullOrWhiteSpace(grade) && string.IsNullOrWhiteSpace(section))
            {
                return string.Empty;
            }

            if (string.IsNullOrWhiteSpace(section))
            {
                return grade;
            }

            return string.IsNullOrWhiteSpace(grade) ? section : grade + " — " + section;
        }

        // ──────────────────────────── Announcement badge ─────────────────

        public static int ClampBadgeCount(int count) => Math.Max(0, count);

        // ──────────────────────────── Quiz Detail → PreviewContent ──────────

        public static QuizDetailPreviewQuestionType MapQuestionType(string type)
        {
            if (string.IsNullOrWhiteSpace(type))
            {
                return QuizDetailPreviewQuestionType.MultipleChoiceSingle;
            }

            switch (type.ToLowerInvariant())
            {
                case "multiple_choice_multiple":
                case "multiplechoicemultiple":
                    return QuizDetailPreviewQuestionType.MultipleChoiceMultiple;
                case "true_false":
                case "truefalse":
                    return QuizDetailPreviewQuestionType.TrueFalse;
                default:
                    return QuizDetailPreviewQuestionType.MultipleChoiceSingle;
            }
        }

        public static QuizDetailPreviewContent MapQuizDetail(QuizDetail detail)
        {
            if (detail == null)
            {
                return null;
            }

            List<QuizDetailPreviewQuestion> questions = null;
            if (detail.Questions != null && detail.Questions.Count > 0)
            {
                questions = new List<QuizDetailPreviewQuestion>(detail.Questions.Count);
                for (int i = 0; i < detail.Questions.Count; i++)
                {
                    QuizQuestionDelivery q = detail.Questions[i];
                    List<QuizDetailPreviewOption> options = null;
                    if (q.Options != null && q.Options.Count > 0)
                    {
                        options = new List<QuizDetailPreviewOption>(q.Options.Count);
                        for (int j = 0; j < q.Options.Count; j++)
                        {
                            options.Add(new QuizDetailPreviewOption(q.Options[j].Key, q.Options[j].Text));
                        }
                    }

                    questions.Add(new QuizDetailPreviewQuestion(
                        q.Id,
                        MapQuestionType(q.Type),
                        q.Prompt,
                        options));
                }
            }

            return new QuizDetailPreviewContent(
                detail.Id,
                detail.Title,
                detail.Instructions,
                questions);
        }

        // ──────────────────────────── Quiz Result → PreviewContent ──────────

        public static QuizResultPreviewContent MapQuizResult(QuizResult result)
        {
            if (result == null)
            {
                return null;
            }

            List<QuizResultPreviewAnswer> answers = null;
            if (result.Answers != null && result.Answers.Count > 0)
            {
                answers = new List<QuizResultPreviewAnswer>(result.Answers.Count);
                for (int i = 0; i < result.Answers.Count; i++)
                {
                    QuizResultAnswer a = result.Answers[i];
                    answers.Add(new QuizResultPreviewAnswer(
                        a.QuestionId,
                        a.Correct ?? false,
                        a.EarnedPoints ?? 0f));
                }
            }

            QuizResultPreviewStatus status = result.FeedbackVisible
                ? QuizResultPreviewStatus.Scored
                : QuizResultPreviewStatus.PendingVisibility;

            return new QuizResultPreviewContent(
                result.AttemptId,
                result.QuizId,
                status,
                result.EarnedPoints,
                result.PossiblePoints,
                result.Percentage,
                result.Passed,
                result.CorrectCount,
                result.IncorrectCount,
                result.UnansweredCount,
                result.SubmittedAt,
                result.FeedbackVisible,
                answers);
        }

        // ──────────────────────────── Quiz History → PreviewItem ──────────

        public static QuizHistoryPreviewItem MapQuizHistoryEntry(QuizHistoryEntry entry)
        {
            if (entry == null)
            {
                return null;
            }

            QuizListPreviewItem summary = MapQuizSummaryToPreviewItem(new QuizSummary
            {
                Id = entry.QuizId,
                Title = entry.QuizTitle,
                SubjectId = entry.SubjectId,
                TermId = entry.TermId,
                Status = entry.Status
            });

            QuizResultPreviewStatus status = entry.FeedbackVisible
                ? QuizResultPreviewStatus.Scored
                : QuizResultPreviewStatus.PendingVisibility;

            QuizResultPreviewContent resultContent = new QuizResultPreviewContent(
                entry.AttemptId,
                entry.QuizId,
                status,
                0f,
                0f,
                entry.Percentage ?? 0f,
                entry.Passed,
                0,
                0,
                0,
                entry.SubmittedAt,
                entry.FeedbackVisible,
                null);

            return new QuizHistoryPreviewItem(entry.AttemptId, summary, resultContent);
        }

        public static QuizHistoryPreviewItem[] MapQuizHistoryEntries(IReadOnlyList<QuizHistoryEntry> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return Array.Empty<QuizHistoryPreviewItem>();
            }

            var items = new QuizHistoryPreviewItem[entries.Count];
            for (int i = 0; i < entries.Count; i++)
            {
                items[i] = MapQuizHistoryEntry(entries[i]);
            }

            return items;
        }

        // ──────────────────────────── PreviewState helpers ────────────────

        public static QuizResultPreviewState ToQuizResultPreviewState(bool isError, bool isOffline = false)
        {
            if (isError || isOffline)
            {
                return QuizResultPreviewState.RecoverableError;
            }

            return QuizResultPreviewState.Content;
        }

        public static QuizHistoryPreviewState ToQuizHistoryPreviewState(DataStatePanelState state)
        {
            switch (state)
            {
                case DataStatePanelState.Loading:
                    return QuizHistoryPreviewState.Loading;
                case DataStatePanelState.Empty:
                    return QuizHistoryPreviewState.Empty;
                case DataStatePanelState.OfflineCached:
                    return QuizHistoryPreviewState.OfflineCached;
                default:
                    return QuizHistoryPreviewState.Content;
            }
        }

        // Map submission: preview option IDs are treated as production option keys.
        // TODO: verify preview fixture option IDs align with production API option.key values.
        public static QuizAttemptSubmission MapPreviewSubmission(
            string clientAttemptUuid,
            QuizAttemptPreviewSubmission previewSubmission)
        {
            List<QuizAnswerSelection> answers = null;
            if (previewSubmission.Answers != null && previewSubmission.Answers.Count > 0)
            {
                answers = new List<QuizAnswerSelection>(previewSubmission.Answers.Count);
                for (int i = 0; i < previewSubmission.Answers.Count; i++)
                {
                    QuizAttemptPreviewAnswer a = previewSubmission.Answers[i];
                    List<string> keys = null;
                    if (a.SelectedOptionIds != null && a.SelectedOptionIds.Count > 0)
                    {
                        keys = new List<string>(a.SelectedOptionIds.Count);
                        for (int j = 0; j < a.SelectedOptionIds.Count; j++)
                        {
                            keys.Add(a.SelectedOptionIds[j]);
                        }
                    }

                    answers.Add(new QuizAnswerSelection
                    {
                        QuestionId = a.QuestionId,
                        SelectedOptionKeys = keys ?? (IReadOnlyList<string>)Array.Empty<string>()
                    });
                }
            }

            return new QuizAttemptSubmission
            {
                ClientAttemptUuid = clientAttemptUuid,
                StartedAt = DateTimeOffset.UtcNow,
                SubmittedAt = DateTimeOffset.UtcNow,
                Answers = answers ?? (IReadOnlyList<QuizAnswerSelection>)Array.Empty<QuizAnswerSelection>()
            };
        }

        // ──────────────────────────── Rewards ────────────────────────────

        public static RewardsPreviewItemStatus MapRewardStatus(string status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return RewardsPreviewItemStatus.Locked;
            }

            switch (status.ToLowerInvariant())
            {
                case "owned":
                case "earned":
                    return RewardsPreviewItemStatus.Owned;
                case "available":
                    return RewardsPreviewItemStatus.Available;
                case "used":
                case "redeemed":
                    return RewardsPreviewItemStatus.Used;
                default:
                    return RewardsPreviewItemStatus.Locked;
            }
        }

        public static RewardsPreviewItem MapRewardSummaryToPreviewItem(RewardSummary reward)
        {
            if (reward == null)
            {
                return null;
            }

            RewardsPreviewItemStatus status = MapRewardStatus(reward.Status);
            string supportingText = reward.SupportingText ?? string.Empty;
            if (string.IsNullOrWhiteSpace(supportingText) && reward.EarnedAt.HasValue)
            {
                supportingText = "Earned " + reward.EarnedAt.Value.ToLocalTime().ToString("MMM d, yyyy");
            }

            return new RewardsPreviewItem(
                presentationKey: reward.RewardCode ?? string.Empty,
                title: reward.Title ?? "(Reward)",
                description: reward.Description ?? string.Empty,
                supportingText: supportingText,
                status: status,
                lockedReason: reward.LockedReason ?? string.Empty,
                iconClass: "ds-icon--star");
        }

        public static RewardsPreviewItem[] MapRewardSummaries(IReadOnlyList<RewardSummary> rewards)
        {
            if (rewards == null || rewards.Count == 0)
            {
                return Array.Empty<RewardsPreviewItem>();
            }

            var items = new RewardsPreviewItem[rewards.Count];
            for (int i = 0; i < rewards.Count; i++)
            {
                items[i] = MapRewardSummaryToPreviewItem(rewards[i]);
            }

            return items;
        }

        public static RewardsPreviewState ErrorToRewardsPreviewState(AppError error)
        {
            if (error == null)
            {
                return RewardsPreviewState.RecoverableError;
            }

            if (error.Code == AppErrorCodes.NetworkOffline || error.IsNetworkError)
            {
                return RewardsPreviewState.OfflineCached;
            }

            return RewardsPreviewState.RecoverableError;
        }

        // ──────────────────────────── Certificates ────────────────────────────

        public static CertificatePreviewAvailability MapCertificateAvailability(string status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return CertificatePreviewAvailability.Locked;
            }

            switch (status.ToLowerInvariant())
            {
                case "issued":
                case "earned":
                case "complete":
                    return CertificatePreviewAvailability.Issued;
                case "in_progress":
                case "inprogress":
                case "started":
                    return CertificatePreviewAvailability.InProgress;
                default:
                    return CertificatePreviewAvailability.Locked;
            }
        }

        public static CertificatePreviewItem MapCertificateSummaryToPreviewItem(CertificateSummary cert)
        {
            if (cert == null)
            {
                return null;
            }

            CertificatePreviewAvailability availability = MapCertificateAvailability(cert.Status);
            string issueDateText = cert.IssuedAt.HasValue
                ? cert.IssuedAt.Value.ToLocalTime().ToString("MMMM d, yyyy")
                : string.Empty;
            string awardedToText = string.IsNullOrWhiteSpace(cert.AwardedToDisplayName)
                ? string.Empty
                : "Awarded to " + cert.AwardedToDisplayName;

            return new CertificatePreviewItem(
                presentationId: cert.Id ?? string.Empty,
                title: cert.Title ?? "(Certificate)",
                typeLabel: cert.TypeLabel ?? string.Empty,
                availability: availability,
                issueDateText: issueDateText,
                eligibilityDescription: cert.EligibilityDescription ?? string.Empty,
                recognitionText: cert.RecognitionText ?? string.Empty,
                lockedReason: cert.LockedReason ?? string.Empty,
                documentHeading: cert.Title ?? string.Empty,
                awardedToText: awardedToText,
                iconClass: "ds-icon--certificate");
        }

        public static CertificatePreviewItem[] MapCertificateSummaries(IReadOnlyList<CertificateSummary> certs)
        {
            if (certs == null || certs.Count == 0)
            {
                return Array.Empty<CertificatePreviewItem>();
            }

            var items = new CertificatePreviewItem[certs.Count];
            for (int i = 0; i < certs.Count; i++)
            {
                items[i] = MapCertificateSummaryToPreviewItem(certs[i]);
            }

            return items;
        }

        public static CertificatesPreviewState ErrorToCertificatesPreviewState(AppError error)
        {
            if (error == null)
            {
                return CertificatesPreviewState.RecoverableError;
            }

            if (error.Code == AppErrorCodes.NetworkOffline || error.IsNetworkError)
            {
                return CertificatesPreviewState.OfflineCached;
            }

            return CertificatesPreviewState.RecoverableError;
        }

        // ──────────────────────────── Leaderboard ────────────────────────────

        public static LeaderboardPreviewEntry MapLeaderboardEntry(LeaderboardEntry entry)
        {
            if (entry == null)
            {
                return new LeaderboardPreviewEntry(0, "—", 0, false);
            }

            return new LeaderboardPreviewEntry(
                entry.Rank,
                string.IsNullOrWhiteSpace(entry.PrivacySafeName) ? "—" : entry.PrivacySafeName,
                entry.MissionsCompleted,
                entry.IsCurrentStudent);
        }

        public static LeaderboardPreviewData MapLeaderboardPage(LeaderboardPage page)
        {
            if (page == null)
            {
                return new LeaderboardPreviewData(
                    new LeaderboardPreviewContext(string.Empty, string.Empty, string.Empty, string.Empty),
                    Array.Empty<LeaderboardPreviewEntry>());
            }

            LeaderboardContext ctx = page.Context;
            LeaderboardPreviewContext previewCtx = ctx != null
                ? new LeaderboardPreviewContext(
                    ctx.ScopeLabel ?? string.Empty,
                    ctx.MetricLabel ?? string.Empty,
                    ctx.PeriodLabel ?? string.Empty,
                    ctx.ContextLabel ?? string.Empty)
                : new LeaderboardPreviewContext(string.Empty, string.Empty, string.Empty, string.Empty);

            IReadOnlyList<LeaderboardEntry> entries = page.Entries;
            if (entries == null || entries.Count == 0)
            {
                return new LeaderboardPreviewData(previewCtx, Array.Empty<LeaderboardPreviewEntry>());
            }

            var previewEntries = new LeaderboardPreviewEntry[entries.Count];
            for (int i = 0; i < entries.Count; i++)
            {
                previewEntries[i] = MapLeaderboardEntry(entries[i]);
            }

            return new LeaderboardPreviewData(previewCtx, previewEntries);
        }

        public static LeaderboardPreviewState ErrorToLeaderboardPreviewState(AppError error)
        {
            if (error == null)
            {
                return LeaderboardPreviewState.RecoverableError;
            }

            if (error.Code == AppErrorCodes.NetworkOffline || error.IsNetworkError)
            {
                return LeaderboardPreviewState.OfflineUnavailable;
            }

            return LeaderboardPreviewState.RecoverableError;
        }

        // ──────────────────────────── DataState helpers ───────────────────

        /// <summary>
        /// Returns the presentation DataState appropriate for the given AppError in a context
        /// where a cached fallback is unavailable.
        /// </summary>
        public static DataStatePanelState ErrorToDataState(AppError error, bool hasCachedData = false)
        {
            if (error == null)
            {
                return DataStatePanelState.RecoverableError;
            }

            if (error.Code == AppErrorCodes.NetworkOffline)
            {
                return hasCachedData ? DataStatePanelState.OfflineCached : DataStatePanelState.OfflineUnavailable;
            }

            if (error.IsNetworkError)
            {
                return hasCachedData ? DataStatePanelState.OfflineCached : DataStatePanelState.OfflineUnavailable;
            }

            if (error.HttpStatus == 403)
            {
                return DataStatePanelState.PermissionOrLocked;
            }

            return DataStatePanelState.RecoverableError;
        }
    }
}
