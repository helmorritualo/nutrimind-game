using System;
using System.Collections.Generic;
using NutriMind.Core.Data;
using UnityEngine;

namespace NutriMind.Core.Persistence
{
    /// <summary>
    /// Versioned learner-scoped route cache for declared offline-fallback routes.
    /// Leaderboard must never be written through this helper.
    /// </summary>
    public static class LearnerRouteCache
    {
        public const int SupportedSchemaVersion = 1;

        public static AppResult SaveProgress(
            IResourceCacheRepository cache,
            string studentId,
            ProgressSummary summary,
            string savedUtc,
            int? serverRevision = null)
        {
            var envelope = new CachedProgressEnvelopeV1
            {
                version = SupportedSchemaVersion,
                studentId = RequireStudent(studentId),
                resourceIdentity = ResourceCacheKeys.ProgressSummary,
                savedUtc = NormalizeUtc(savedUtc),
                item = new CachedProgressSummaryV1
                {
                    missionsStarted = summary?.MissionsStarted ?? 0,
                    missionsCompleted = summary?.MissionsCompleted ?? 0,
                    areasCompleted = summary?.AreasCompleted ?? 0,
                    reviewRequiredCount = summary?.ReviewRequiredCount ?? 0,
                    quizAttempts = summary?.QuizAttempts ?? 0
                }
            };

            return Upsert(
                cache,
                ResourceCacheKeys.StudentProgressSummary(studentId),
                JsonUtility.ToJson(envelope),
                envelope.savedUtc,
                serverRevision);
        }

        public static AppResult<ProgressSummary> LoadProgress(
            IResourceCacheRepository cache,
            string expectedStudentId)
        {
            AppResult<CachedProgressEnvelopeV1> loaded = LoadEnvelope<CachedProgressEnvelopeV1>(
                cache,
                ResourceCacheKeys.StudentProgressSummary(expectedStudentId),
                expectedStudentId,
                ResourceCacheKeys.ProgressSummary);
            if (loaded.IsFailure)
            {
                return AppResult<ProgressSummary>.Failure(loaded.Error);
            }

            CachedProgressSummaryV1 dto = loaded.Value.item;
            return AppResult<ProgressSummary>.Success(new ProgressSummary
            {
                MissionsStarted = dto?.missionsStarted ?? 0,
                MissionsCompleted = dto?.missionsCompleted ?? 0,
                AreasCompleted = dto?.areasCompleted ?? 0,
                ReviewRequiredCount = dto?.reviewRequiredCount ?? 0,
                QuizAttempts = dto?.quizAttempts ?? 0
            });
        }

        public static AppResult SaveSubjects(
            IResourceCacheRepository cache,
            string studentId,
            IReadOnlyList<SubjectSummary> subjects,
            string savedUtc)
        {
            var envelope = new CachedSubjectsEnvelopeV1
            {
                version = SupportedSchemaVersion,
                studentId = RequireStudent(studentId),
                resourceIdentity = ResourceCacheKeys.Subjects,
                savedUtc = NormalizeUtc(savedUtc),
                items = FromSubjects(subjects)
            };
            return Upsert(
                cache,
                ResourceCacheKeys.StudentSubjects(studentId),
                JsonUtility.ToJson(envelope),
                envelope.savedUtc,
                null);
        }

        public static AppResult<IReadOnlyList<SubjectSummary>> LoadSubjects(
            IResourceCacheRepository cache,
            string expectedStudentId)
        {
            AppResult<CachedSubjectsEnvelopeV1> loaded = LoadEnvelope<CachedSubjectsEnvelopeV1>(
                cache,
                ResourceCacheKeys.StudentSubjects(expectedStudentId),
                expectedStudentId,
                ResourceCacheKeys.Subjects);
            if (loaded.IsFailure)
            {
                return AppResult<IReadOnlyList<SubjectSummary>>.Failure(loaded.Error);
            }

            return AppResult<IReadOnlyList<SubjectSummary>>.Success(ToSubjects(loaded.Value.items));
        }

        public static AppResult SaveTerms(
            IResourceCacheRepository cache,
            string studentId,
            string subjectId,
            IReadOnlyList<TermSummary> terms,
            string savedUtc)
        {
            string resourceIdentity = ResourceCacheKeys.Terms(subjectId);
            var envelope = new CachedTermsEnvelopeV1
            {
                version = SupportedSchemaVersion,
                studentId = RequireStudent(studentId),
                resourceIdentity = resourceIdentity,
                savedUtc = NormalizeUtc(savedUtc),
                items = FromTerms(terms)
            };
            return Upsert(
                cache,
                ResourceCacheKeys.StudentTerms(studentId, subjectId),
                JsonUtility.ToJson(envelope),
                envelope.savedUtc,
                null);
        }

        public static AppResult<IReadOnlyList<TermSummary>> LoadTerms(
            IResourceCacheRepository cache,
            string expectedStudentId,
            string subjectId)
        {
            string resourceIdentity = ResourceCacheKeys.Terms(subjectId);
            AppResult<CachedTermsEnvelopeV1> loaded = LoadEnvelope<CachedTermsEnvelopeV1>(
                cache,
                ResourceCacheKeys.StudentTerms(expectedStudentId, subjectId),
                expectedStudentId,
                resourceIdentity);
            if (loaded.IsFailure)
            {
                return AppResult<IReadOnlyList<TermSummary>>.Failure(loaded.Error);
            }

            return AppResult<IReadOnlyList<TermSummary>>.Success(ToTerms(loaded.Value.items));
        }

        public static AppResult SaveMissions(
            IResourceCacheRepository cache,
            string studentId,
            string subjectId,
            string termId,
            IReadOnlyList<MissionSummary> missions,
            string savedUtc)
        {
            string resourceIdentity = ResourceCacheKeys.Missions(subjectId, termId);
            var envelope = new CachedMissionsEnvelopeV1
            {
                version = SupportedSchemaVersion,
                studentId = RequireStudent(studentId),
                resourceIdentity = resourceIdentity,
                savedUtc = NormalizeUtc(savedUtc),
                items = BootstrapCacheMapper.ToCachedMissions(missions)
            };
            return Upsert(
                cache,
                ResourceCacheKeys.StudentMissions(studentId, subjectId, termId),
                JsonUtility.ToJson(envelope),
                envelope.savedUtc,
                null);
        }

        public static AppResult<IReadOnlyList<MissionSummary>> LoadMissions(
            IResourceCacheRepository cache,
            string expectedStudentId,
            string subjectId,
            string termId)
        {
            string resourceIdentity = ResourceCacheKeys.Missions(subjectId, termId);
            AppResult<CachedMissionsEnvelopeV1> loaded = LoadEnvelope<CachedMissionsEnvelopeV1>(
                cache,
                ResourceCacheKeys.StudentMissions(expectedStudentId, subjectId, termId),
                expectedStudentId,
                resourceIdentity);
            if (loaded.IsFailure)
            {
                return AppResult<IReadOnlyList<MissionSummary>>.Failure(loaded.Error);
            }

            return AppResult<IReadOnlyList<MissionSummary>>.Success(
                BootstrapCacheMapper.ToDomainMissions(loaded.Value.items));
        }

        public static AppResult SaveRewards(
            IResourceCacheRepository cache,
            string studentId,
            IReadOnlyList<RewardSummary> rewards,
            string savedUtc)
        {
            var envelope = new CachedRewardsEnvelopeV1
            {
                version = SupportedSchemaVersion,
                studentId = RequireStudent(studentId),
                resourceIdentity = ResourceCacheKeys.Rewards,
                savedUtc = NormalizeUtc(savedUtc),
                items = FromRewards(rewards)
            };
            return Upsert(
                cache,
                ResourceCacheKeys.StudentRewards(studentId),
                JsonUtility.ToJson(envelope),
                envelope.savedUtc,
                null);
        }

        public static AppResult<IReadOnlyList<RewardSummary>> LoadRewards(
            IResourceCacheRepository cache,
            string expectedStudentId)
        {
            AppResult<CachedRewardsEnvelopeV1> loaded = LoadEnvelope<CachedRewardsEnvelopeV1>(
                cache,
                ResourceCacheKeys.StudentRewards(expectedStudentId),
                expectedStudentId,
                ResourceCacheKeys.Rewards);
            if (loaded.IsFailure)
            {
                return AppResult<IReadOnlyList<RewardSummary>>.Failure(loaded.Error);
            }

            return AppResult<IReadOnlyList<RewardSummary>>.Success(ToRewards(loaded.Value.items));
        }

        public static AppResult SaveCertificates(
            IResourceCacheRepository cache,
            string studentId,
            IReadOnlyList<CertificateSummary> certificates,
            string savedUtc)
        {
            var envelope = new CachedCertificatesEnvelopeV1
            {
                version = SupportedSchemaVersion,
                studentId = RequireStudent(studentId),
                resourceIdentity = ResourceCacheKeys.Certificates,
                savedUtc = NormalizeUtc(savedUtc),
                items = FromCertificates(certificates)
            };
            return Upsert(
                cache,
                ResourceCacheKeys.StudentCertificates(studentId),
                JsonUtility.ToJson(envelope),
                envelope.savedUtc,
                null);
        }

        public static AppResult<IReadOnlyList<CertificateSummary>> LoadCertificates(
            IResourceCacheRepository cache,
            string expectedStudentId)
        {
            AppResult<CachedCertificatesEnvelopeV1> loaded =
                LoadEnvelope<CachedCertificatesEnvelopeV1>(
                    cache,
                    ResourceCacheKeys.StudentCertificates(expectedStudentId),
                    expectedStudentId,
                    ResourceCacheKeys.Certificates);
            if (loaded.IsFailure)
            {
                return AppResult<IReadOnlyList<CertificateSummary>>.Failure(loaded.Error);
            }

            return AppResult<IReadOnlyList<CertificateSummary>>.Success(
                ToCertificates(loaded.Value.items));
        }

        public static AppResult SaveAnnouncements(
            IResourceCacheRepository cache,
            string studentId,
            IReadOnlyList<AnnouncementSummary> announcements,
            string savedUtc)
        {
            var envelope = new CachedAnnouncementsEnvelopeV1
            {
                version = SupportedSchemaVersion,
                studentId = RequireStudent(studentId),
                resourceIdentity = ResourceCacheKeys.Announcements,
                savedUtc = NormalizeUtc(savedUtc),
                items = FromAnnouncements(announcements)
            };
            return Upsert(
                cache,
                ResourceCacheKeys.StudentAnnouncements(studentId),
                JsonUtility.ToJson(envelope),
                envelope.savedUtc,
                null);
        }

        public static AppResult<IReadOnlyList<AnnouncementSummary>> LoadAnnouncements(
            IResourceCacheRepository cache,
            string expectedStudentId)
        {
            AppResult<CachedAnnouncementsEnvelopeV1> loaded =
                LoadEnvelope<CachedAnnouncementsEnvelopeV1>(
                    cache,
                    ResourceCacheKeys.StudentAnnouncements(expectedStudentId),
                    expectedStudentId,
                    ResourceCacheKeys.Announcements);
            if (loaded.IsFailure)
            {
                return AppResult<IReadOnlyList<AnnouncementSummary>>.Failure(loaded.Error);
            }

            return AppResult<IReadOnlyList<AnnouncementSummary>>.Success(
                ToAnnouncements(loaded.Value.items));
        }

        public static AppResult SaveQuizHistory(
            IResourceCacheRepository cache,
            string studentId,
            string queryKey,
            IReadOnlyList<QuizHistoryEntry> entries,
            string savedUtc)
        {
            string resourceIdentity = ResourceCacheKeys.QuizResults(queryKey);
            var envelope = new CachedQuizHistoryEnvelopeV1
            {
                version = SupportedSchemaVersion,
                studentId = RequireStudent(studentId),
                resourceIdentity = resourceIdentity,
                savedUtc = NormalizeUtc(savedUtc),
                items = FromQuizHistory(entries)
            };
            return Upsert(
                cache,
                ResourceCacheKeys.StudentQuizResults(studentId, queryKey),
                JsonUtility.ToJson(envelope),
                envelope.savedUtc,
                null);
        }

        public static AppResult<IReadOnlyList<QuizHistoryEntry>> LoadQuizHistory(
            IResourceCacheRepository cache,
            string expectedStudentId,
            string queryKey)
        {
            string resourceIdentity = ResourceCacheKeys.QuizResults(queryKey);
            AppResult<CachedQuizHistoryEnvelopeV1> loaded =
                LoadEnvelope<CachedQuizHistoryEnvelopeV1>(
                    cache,
                    ResourceCacheKeys.StudentQuizResults(expectedStudentId, queryKey),
                    expectedStudentId,
                    resourceIdentity);
            if (loaded.IsFailure)
            {
                return AppResult<IReadOnlyList<QuizHistoryEntry>>.Failure(loaded.Error);
            }

            return AppResult<IReadOnlyList<QuizHistoryEntry>>.Success(
                ToQuizHistory(loaded.Value.items));
        }

        private static AppResult Upsert(
            IResourceCacheRepository cache,
            string cacheKey,
            string payloadJson,
            string savedUtc,
            int? serverRevision)
        {
            if (cache == null)
            {
                return AppResult.Failure(
                    AppErrorCodes.ClientConfigurationError,
                    "Resource cache repository is required.");
            }

            if (IsLeaderboardKey(cacheKey))
            {
                return AppResult.Failure(
                    AppErrorCodes.ValidationFailed,
                    "Leaderboard must never be cached.");
            }

            try
            {
                return cache.Upsert(new ResourceCacheRecord
                {
                    CacheKey = cacheKey,
                    PayloadJson = payloadJson,
                    SchemaVersion = SupportedSchemaVersion,
                    ServerRevision = serverRevision,
                    CachedUtc = savedUtc
                });
            }
            catch (Exception exception)
            {
                return AppResult.Failure(AppError.FromException(exception));
            }
        }

        private static AppResult<TEnvelope> LoadEnvelope<TEnvelope>(
            IResourceCacheRepository cache,
            string cacheKey,
            string expectedStudentId,
            string expectedResourceIdentity)
            where TEnvelope : class, ILearnerCacheEnvelope
        {
            if (cache == null)
            {
                return AppResult<TEnvelope>.Failure(
                    AppErrorCodes.ClientConfigurationError,
                    "Resource cache repository is required.");
            }

            if (IsLeaderboardKey(cacheKey))
            {
                return AppResult<TEnvelope>.Failure(
                    AppErrorCodes.ValidationFailed,
                    "Leaderboard must never be cached.");
            }

            AppResult<ResourceCacheRecord> record = cache.Get(cacheKey);
            if (record.IsFailure)
            {
                return AppResult<TEnvelope>.Failure(record.Error);
            }

            if (record.Value == null)
            {
                return AppResult<TEnvelope>.Failure(
                    AppErrorCodes.CachePayloadInvalid,
                    "Learner cache entry was not found.");
            }

            if (record.Value.SchemaVersion != SupportedSchemaVersion)
            {
                return AppResult<TEnvelope>.Failure(
                    AppErrorCodes.CacheSchemaUnsupported,
                    "Learner cache schema_version "
                    + record.Value.SchemaVersion
                    + " is unsupported.");
            }

            TEnvelope envelope;
            try
            {
                envelope = JsonUtility.FromJson<TEnvelope>(record.Value.PayloadJson);
            }
            catch (Exception exception)
            {
                return AppResult<TEnvelope>.Failure(AppError.FromException(exception));
            }

            if (envelope == null || envelope.Version != SupportedSchemaVersion)
            {
                return AppResult<TEnvelope>.Failure(
                    AppErrorCodes.CachePayloadInvalid,
                    "Learner cache envelope is malformed.");
            }

            if (!string.Equals(envelope.StudentId, expectedStudentId, StringComparison.Ordinal)
                || !string.Equals(
                    envelope.ResourceIdentity,
                    expectedResourceIdentity,
                    StringComparison.Ordinal))
            {
                return AppResult<TEnvelope>.Failure(
                    AppErrorCodes.CachePayloadInvalid,
                    "Learner cache ownership mismatch.");
            }

            return AppResult<TEnvelope>.Success(envelope);
        }

        private static bool IsLeaderboardKey(string cacheKey)
        {
            return !string.IsNullOrWhiteSpace(cacheKey)
                && cacheKey.IndexOf("leaderboard", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string RequireStudent(string studentId)
        {
            if (string.IsNullOrWhiteSpace(studentId))
            {
                throw new ArgumentException("studentId is required.", nameof(studentId));
            }

            return studentId.Trim();
        }

        private static string NormalizeUtc(string savedUtc)
        {
            return string.IsNullOrWhiteSpace(savedUtc)
                ? DateTimeOffset.UtcNow.ToString("o")
                : savedUtc;
        }

        private interface ILearnerCacheEnvelope
        {
            int Version { get; }
            string StudentId { get; }
            string ResourceIdentity { get; }
        }

        [Serializable]
        public sealed class CachedProgressSummaryV1
        {
            public int missionsStarted;
            public int missionsCompleted;
            public int areasCompleted;
            public int reviewRequiredCount;
            public int quizAttempts;
        }

        [Serializable]
        private sealed class CachedProgressEnvelopeV1 : ILearnerCacheEnvelope
        {
            public int version = SupportedSchemaVersion;
            public string studentId;
            public string resourceIdentity;
            public string savedUtc;
            public CachedProgressSummaryV1 item;

            public int Version => version;
            public string StudentId => studentId;
            public string ResourceIdentity => resourceIdentity;
        }

        [Serializable]
        public sealed class CachedSubjectV1
        {
            public string id;
            public string slug;
            public string name;
            public bool isActive;
        }

        [Serializable]
        private sealed class CachedSubjectsEnvelopeV1 : ILearnerCacheEnvelope
        {
            public int version = SupportedSchemaVersion;
            public string studentId;
            public string resourceIdentity;
            public string savedUtc;
            public CachedSubjectV1[] items;

            public int Version => version;
            public string StudentId => studentId;
            public string ResourceIdentity => resourceIdentity;
        }

        [Serializable]
        public sealed class CachedTermV1
        {
            public string id;
            public string name;
            public int order;
            public bool isActive;
        }

        [Serializable]
        private sealed class CachedTermsEnvelopeV1 : ILearnerCacheEnvelope
        {
            public int version = SupportedSchemaVersion;
            public string studentId;
            public string resourceIdentity;
            public string savedUtc;
            public CachedTermV1[] items;

            public int Version => version;
            public string StudentId => studentId;
            public string ResourceIdentity => resourceIdentity;
        }

        [Serializable]
        private sealed class CachedMissionsEnvelopeV1 : ILearnerCacheEnvelope
        {
            public int version = SupportedSchemaVersion;
            public string studentId;
            public string resourceIdentity;
            public string savedUtc;
            public CachedMissionSummaryV1[] items;

            public int Version => version;
            public string StudentId => studentId;
            public string ResourceIdentity => resourceIdentity;
        }

        [Serializable]
        public sealed class CachedRewardV1
        {
            public string rewardCode;
            public string title;
            public string description;
            public string supportingText;
            public string status;
            public string lockedReason;
            public string earnedAtUtc;
            public string usedAtUtc;
        }

        [Serializable]
        private sealed class CachedRewardsEnvelopeV1 : ILearnerCacheEnvelope
        {
            public int version = SupportedSchemaVersion;
            public string studentId;
            public string resourceIdentity;
            public string savedUtc;
            public CachedRewardV1[] items;

            public int Version => version;
            public string StudentId => studentId;
            public string ResourceIdentity => resourceIdentity;
        }

        [Serializable]
        public sealed class CachedCertificateV1
        {
            public string id;
            public string title;
            public string typeLabel;
            public string status;
            public string eligibilityDescription;
            public string recognitionText;
            public string lockedReason;
            public string issuedAtUtc;
            public string awardedToDisplayName;
        }

        [Serializable]
        private sealed class CachedCertificatesEnvelopeV1 : ILearnerCacheEnvelope
        {
            public int version = SupportedSchemaVersion;
            public string studentId;
            public string resourceIdentity;
            public string savedUtc;
            public CachedCertificateV1[] items;

            public int Version => version;
            public string StudentId => studentId;
            public string ResourceIdentity => resourceIdentity;
        }

        [Serializable]
        public sealed class CachedAnnouncementV1
        {
            public string id;
            public string title;
            public string summary;
            public string body;
            public string audienceLabel;
            public string kind;
            public bool isUnread;
            public string publishedAtUtc;
            public string expiresAtUtc;
        }

        [Serializable]
        private sealed class CachedAnnouncementsEnvelopeV1 : ILearnerCacheEnvelope
        {
            public int version = SupportedSchemaVersion;
            public string studentId;
            public string resourceIdentity;
            public string savedUtc;
            public CachedAnnouncementV1[] items;

            public int Version => version;
            public string StudentId => studentId;
            public string ResourceIdentity => resourceIdentity;
        }

        [Serializable]
        public sealed class CachedQuizHistoryV1
        {
            public string attemptId;
            public string quizId;
            public string quizTitle;
            public string subjectId;
            public string termId;
            public string status;
            public float percentage;
            public bool hasPercentage;
            public bool passed;
            public bool hasPassed;
            public string submittedAtUtc;
            public bool feedbackVisible;
        }

        [Serializable]
        private sealed class CachedQuizHistoryEnvelopeV1 : ILearnerCacheEnvelope
        {
            public int version = SupportedSchemaVersion;
            public string studentId;
            public string resourceIdentity;
            public string savedUtc;
            public CachedQuizHistoryV1[] items;

            public int Version => version;
            public string StudentId => studentId;
            public string ResourceIdentity => resourceIdentity;
        }

        private static CachedSubjectV1[] FromSubjects(IReadOnlyList<SubjectSummary> subjects)
        {
            if (subjects == null || subjects.Count == 0)
            {
                return Array.Empty<CachedSubjectV1>();
            }

            var items = new CachedSubjectV1[subjects.Count];
            for (int i = 0; i < subjects.Count; i++)
            {
                SubjectSummary subject = subjects[i] ?? new SubjectSummary();
                items[i] = new CachedSubjectV1
                {
                    id = subject.Id,
                    slug = subject.Slug,
                    name = subject.Name,
                    isActive = subject.IsActive
                };
            }

            return items;
        }

        private static IReadOnlyList<SubjectSummary> ToSubjects(CachedSubjectV1[] items)
        {
            if (items == null || items.Length == 0)
            {
                return Array.Empty<SubjectSummary>();
            }

            var result = new SubjectSummary[items.Length];
            for (int i = 0; i < items.Length; i++)
            {
                CachedSubjectV1 item = items[i] ?? new CachedSubjectV1();
                result[i] = new SubjectSummary
                {
                    Id = item.id,
                    Slug = item.slug,
                    Name = item.name,
                    IsActive = item.isActive
                };
            }

            return result;
        }

        private static CachedTermV1[] FromTerms(IReadOnlyList<TermSummary> terms)
        {
            if (terms == null || terms.Count == 0)
            {
                return Array.Empty<CachedTermV1>();
            }

            var items = new CachedTermV1[terms.Count];
            for (int i = 0; i < terms.Count; i++)
            {
                TermSummary term = terms[i] ?? new TermSummary();
                items[i] = new CachedTermV1
                {
                    id = term.Id,
                    name = term.Name,
                    order = term.Order,
                    isActive = term.IsActive
                };
            }

            return items;
        }

        private static IReadOnlyList<TermSummary> ToTerms(CachedTermV1[] items)
        {
            if (items == null || items.Length == 0)
            {
                return Array.Empty<TermSummary>();
            }

            var result = new TermSummary[items.Length];
            for (int i = 0; i < items.Length; i++)
            {
                CachedTermV1 item = items[i] ?? new CachedTermV1();
                result[i] = new TermSummary
                {
                    Id = item.id,
                    Name = item.name,
                    Order = item.order,
                    IsActive = item.isActive
                };
            }

            return result;
        }

        private static CachedRewardV1[] FromRewards(IReadOnlyList<RewardSummary> rewards)
        {
            if (rewards == null || rewards.Count == 0)
            {
                return Array.Empty<CachedRewardV1>();
            }

            var items = new CachedRewardV1[rewards.Count];
            for (int i = 0; i < rewards.Count; i++)
            {
                RewardSummary reward = rewards[i] ?? new RewardSummary();
                items[i] = new CachedRewardV1
                {
                    rewardCode = reward.RewardCode,
                    title = reward.Title,
                    description = reward.Description,
                    supportingText = reward.SupportingText,
                    status = reward.Status,
                    lockedReason = reward.LockedReason,
                    earnedAtUtc = FormatUtc(reward.EarnedAt),
                    usedAtUtc = FormatUtc(reward.UsedAt)
                };
            }

            return items;
        }

        private static IReadOnlyList<RewardSummary> ToRewards(CachedRewardV1[] items)
        {
            if (items == null || items.Length == 0)
            {
                return Array.Empty<RewardSummary>();
            }

            var result = new RewardSummary[items.Length];
            for (int i = 0; i < items.Length; i++)
            {
                CachedRewardV1 item = items[i] ?? new CachedRewardV1();
                result[i] = new RewardSummary
                {
                    RewardCode = item.rewardCode,
                    Title = item.title,
                    Description = item.description,
                    SupportingText = item.supportingText,
                    Status = item.status,
                    LockedReason = item.lockedReason,
                    EarnedAt = ParseUtc(item.earnedAtUtc),
                    UsedAt = ParseUtc(item.usedAtUtc)
                };
            }

            return result;
        }

        private static CachedCertificateV1[] FromCertificates(
            IReadOnlyList<CertificateSummary> certificates)
        {
            if (certificates == null || certificates.Count == 0)
            {
                return Array.Empty<CachedCertificateV1>();
            }

            var items = new CachedCertificateV1[certificates.Count];
            for (int i = 0; i < certificates.Count; i++)
            {
                CertificateSummary certificate = certificates[i] ?? new CertificateSummary();
                items[i] = new CachedCertificateV1
                {
                    id = certificate.Id,
                    title = certificate.Title,
                    typeLabel = certificate.TypeLabel,
                    status = certificate.Status,
                    eligibilityDescription = certificate.EligibilityDescription,
                    recognitionText = certificate.RecognitionText,
                    lockedReason = certificate.LockedReason,
                    issuedAtUtc = FormatUtc(certificate.IssuedAt),
                    awardedToDisplayName = certificate.AwardedToDisplayName
                };
            }

            return items;
        }

        private static IReadOnlyList<CertificateSummary> ToCertificates(CachedCertificateV1[] items)
        {
            if (items == null || items.Length == 0)
            {
                return Array.Empty<CertificateSummary>();
            }

            var result = new CertificateSummary[items.Length];
            for (int i = 0; i < items.Length; i++)
            {
                CachedCertificateV1 item = items[i] ?? new CachedCertificateV1();
                result[i] = new CertificateSummary
                {
                    Id = item.id,
                    Title = item.title,
                    TypeLabel = item.typeLabel,
                    Status = item.status,
                    EligibilityDescription = item.eligibilityDescription,
                    RecognitionText = item.recognitionText,
                    LockedReason = item.lockedReason,
                    IssuedAt = ParseUtc(item.issuedAtUtc),
                    AwardedToDisplayName = item.awardedToDisplayName
                };
            }

            return result;
        }

        private static CachedAnnouncementV1[] FromAnnouncements(
            IReadOnlyList<AnnouncementSummary> announcements)
        {
            if (announcements == null || announcements.Count == 0)
            {
                return Array.Empty<CachedAnnouncementV1>();
            }

            var items = new CachedAnnouncementV1[announcements.Count];
            for (int i = 0; i < announcements.Count; i++)
            {
                AnnouncementSummary announcement = announcements[i] ?? new AnnouncementSummary();
                items[i] = new CachedAnnouncementV1
                {
                    id = announcement.Id,
                    title = announcement.Title,
                    summary = announcement.Summary,
                    body = announcement.Body,
                    audienceLabel = announcement.AudienceLabel,
                    kind = announcement.Kind,
                    isUnread = announcement.IsUnread,
                    publishedAtUtc = FormatUtc(announcement.PublishedAt),
                    expiresAtUtc = FormatUtc(announcement.ExpiresAt)
                };
            }

            return items;
        }

        private static IReadOnlyList<AnnouncementSummary> ToAnnouncements(
            CachedAnnouncementV1[] items)
        {
            if (items == null || items.Length == 0)
            {
                return Array.Empty<AnnouncementSummary>();
            }

            var result = new AnnouncementSummary[items.Length];
            for (int i = 0; i < items.Length; i++)
            {
                CachedAnnouncementV1 item = items[i] ?? new CachedAnnouncementV1();
                result[i] = new AnnouncementSummary
                {
                    Id = item.id,
                    Title = item.title,
                    Summary = item.summary,
                    Body = item.body,
                    AudienceLabel = item.audienceLabel,
                    Kind = item.kind,
                    IsUnread = item.isUnread,
                    PublishedAt = ParseUtc(item.publishedAtUtc),
                    ExpiresAt = ParseUtc(item.expiresAtUtc)
                };
            }

            return result;
        }

        private static CachedQuizHistoryV1[] FromQuizHistory(IReadOnlyList<QuizHistoryEntry> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return Array.Empty<CachedQuizHistoryV1>();
            }

            var items = new CachedQuizHistoryV1[entries.Count];
            for (int i = 0; i < entries.Count; i++)
            {
                QuizHistoryEntry entry = entries[i] ?? new QuizHistoryEntry();
                items[i] = new CachedQuizHistoryV1
                {
                    attemptId = entry.AttemptId,
                    quizId = entry.QuizId,
                    quizTitle = entry.QuizTitle,
                    subjectId = entry.SubjectId,
                    termId = entry.TermId,
                    status = entry.Status,
                    percentage = entry.Percentage ?? 0f,
                    hasPercentage = entry.Percentage.HasValue,
                    passed = entry.Passed ?? false,
                    hasPassed = entry.Passed.HasValue,
                    submittedAtUtc = entry.SubmittedAt.ToUniversalTime().ToString("o"),
                    feedbackVisible = entry.FeedbackVisible
                };
            }

            return items;
        }

        private static IReadOnlyList<QuizHistoryEntry> ToQuizHistory(CachedQuizHistoryV1[] items)
        {
            if (items == null || items.Length == 0)
            {
                return Array.Empty<QuizHistoryEntry>();
            }

            var result = new QuizHistoryEntry[items.Length];
            for (int i = 0; i < items.Length; i++)
            {
                CachedQuizHistoryV1 item = items[i] ?? new CachedQuizHistoryV1();
                result[i] = new QuizHistoryEntry
                {
                    AttemptId = item.attemptId,
                    QuizId = item.quizId,
                    QuizTitle = item.quizTitle,
                    SubjectId = item.subjectId,
                    TermId = item.termId,
                    Status = item.status,
                    Percentage = item.hasPercentage ? item.percentage : (float?)null,
                    Passed = item.hasPassed ? item.passed : (bool?)null,
                    SubmittedAt = ParseUtc(item.submittedAtUtc) ?? DateTimeOffset.MinValue,
                    FeedbackVisible = item.feedbackVisible
                };
            }

            return result;
        }

        private static string FormatUtc(DateTimeOffset? value)
        {
            return value?.ToUniversalTime().ToString("o") ?? string.Empty;
        }

        private static DateTimeOffset? ParseUtc(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (DateTimeOffset.TryParse(value, out DateTimeOffset parsed))
            {
                return parsed.ToUniversalTime();
            }

            return null;
        }
    }
}
