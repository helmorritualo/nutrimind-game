using System;
using System.Collections.Generic;
using NutriMind.Core.Data;
using UnityEngine;

namespace NutriMind.Core.Persistence
{
    /// <summary>
    /// Maps domain BootstrapSnapshot ↔ versioned cache JSON. Malformed/unsupported cache fails closed.
    /// </summary>
    public static class BootstrapCacheMapper
    {
        public const int SupportedSchemaVersion = 1;

        public static AppResult<string> Serialize(BootstrapSnapshot snapshot, string cachedUtc)
        {
            if (snapshot == null)
            {
                return AppResult<string>.Failure(
                    AppErrorCodes.ValidationFailed,
                    "Bootstrap snapshot is required for cache serialization.");
            }

            if (snapshot.Profile == null || string.IsNullOrWhiteSpace(snapshot.Profile.Id))
            {
                return AppResult<string>.Failure(
                    AppErrorCodes.ValidationFailed,
                    "Bootstrap profile is required for cache serialization.");
            }

            var cached = new CachedBootstrapSnapshotV1
            {
                schemaVersion = SupportedSchemaVersion,
                requiredManifestVersion = snapshot.RequiredManifestVersion,
                profile = ToCachedProfile(snapshot.Profile),
                subjects = ToCachedSubjects(snapshot.Subjects),
                missions = ToCachedMissions(snapshot.Missions),
                quizPortalAvailableCount = snapshot.QuizPortalAvailableCount,
                announcementsVisibleCount = snapshot.AnnouncementsVisibleCount,
                sync = ToCachedSync(snapshot.Sync),
                cachedUtc = string.IsNullOrWhiteSpace(cachedUtc)
                    ? DateTimeOffset.UtcNow.ToString("o")
                    : cachedUtc
            };

            try
            {
                string json = JsonUtility.ToJson(cached);
                if (string.IsNullOrWhiteSpace(json) || json == "{}")
                {
                    return AppResult<string>.Failure(
                        AppErrorCodes.ClientInternalError,
                        "Bootstrap cache serialization produced empty JSON.");
                }

                return AppResult<string>.Success(json);
            }
            catch (Exception exception)
            {
                return AppResult<string>.Failure(AppError.FromException(exception));
            }
        }

        public static AppResult<BootstrapSnapshot> Deserialize(string payloadJson, int schemaVersion)
        {
            if (schemaVersion != SupportedSchemaVersion)
            {
                return AppResult<BootstrapSnapshot>.Failure(
                    AppErrorCodes.CacheSchemaUnsupported,
                    "Bootstrap cache schema_version " + schemaVersion + " is unsupported.");
            }

            if (string.IsNullOrWhiteSpace(payloadJson))
            {
                return AppResult<BootstrapSnapshot>.Failure(
                    AppErrorCodes.CachePayloadInvalid,
                    "Bootstrap cache payload is empty.");
            }

            CachedBootstrapSnapshotV1 cached;
            try
            {
                cached = JsonUtility.FromJson<CachedBootstrapSnapshotV1>(payloadJson);
            }
            catch (Exception)
            {
                return AppResult<BootstrapSnapshot>.Failure(
                    AppErrorCodes.CachePayloadInvalid,
                    "Bootstrap cache payload could not be parsed.");
            }

            if (cached == null)
            {
                return AppResult<BootstrapSnapshot>.Failure(
                    AppErrorCodes.CachePayloadInvalid,
                    "Bootstrap cache payload deserialized to null.");
            }

            if (cached.schemaVersion != SupportedSchemaVersion)
            {
                return AppResult<BootstrapSnapshot>.Failure(
                    AppErrorCodes.CacheSchemaUnsupported,
                    "Bootstrap cache schema_version " + cached.schemaVersion + " is unsupported.");
            }

            if (cached.profile == null || string.IsNullOrWhiteSpace(cached.profile.id))
            {
                return AppResult<BootstrapSnapshot>.Failure(
                    AppErrorCodes.CachePayloadInvalid,
                    "Bootstrap cache is missing a valid profile.");
            }

            if (ContainsSensitiveKeys(payloadJson))
            {
                return AppResult<BootstrapSnapshot>.Failure(
                    AppErrorCodes.CachePayloadInvalid,
                    "Bootstrap cache must not contain secrets.");
            }

            return AppResult<BootstrapSnapshot>.Success(ToDomain(cached));
        }

        private static bool ContainsSensitiveKeys(string payloadJson)
        {
            string lower = payloadJson.ToLowerInvariant();
            return lower.Contains("\"accesstoken\"")
                   || lower.Contains("\"access_token\"")
                   || lower.Contains("\"pin\"")
                   || lower.Contains("\"bearer\"")
                   || lower.Contains("\"authorization\"")
                   || lower.Contains("\"answer_key\"")
                   || lower.Contains("\"correct_answer\"");
        }

        private static BootstrapSnapshot ToDomain(CachedBootstrapSnapshotV1 cached)
        {
            return new BootstrapSnapshot
            {
                RequiredManifestVersion = cached.requiredManifestVersion,
                Profile = ToDomainProfile(cached.profile),
                Subjects = ToDomainSubjects(cached.subjects),
                Missions = ToDomainMissions(cached.missions),
                QuizPortalAvailableCount = cached.quizPortalAvailableCount,
                AnnouncementsVisibleCount = cached.announcementsVisibleCount,
                Sync = ToDomainSync(cached.sync)
            };
        }

        private static CachedStudentProfileV1 ToCachedProfile(StudentProfile profile)
        {
            return new CachedStudentProfileV1
            {
                id = profile.Id,
                displayName = profile.DisplayName,
                lrnMasked = profile.LrnMasked,
                gradeId = profile.GradeId,
                isActive = profile.IsActive,
                section = profile.Section == null
                    ? null
                    : new CachedStudentSectionV1
                    {
                        id = profile.Section.Id,
                        name = profile.Section.Name,
                        gradeId = profile.Section.GradeId
                    }
            };
        }

        private static StudentProfile ToDomainProfile(CachedStudentProfileV1 profile)
        {
            return new StudentProfile
            {
                Id = profile.id,
                DisplayName = profile.displayName,
                LrnMasked = profile.lrnMasked,
                GradeId = profile.gradeId,
                IsActive = profile.isActive,
                Section = profile.section == null
                    ? null
                    : new StudentSection
                    {
                        Id = profile.section.id,
                        Name = profile.section.name,
                        GradeId = profile.section.gradeId
                    }
            };
        }

        private static CachedSubjectSummaryV1[] ToCachedSubjects(IReadOnlyList<SubjectSummary> subjects)
        {
            if (subjects == null || subjects.Count == 0)
            {
                return Array.Empty<CachedSubjectSummaryV1>();
            }

            var result = new CachedSubjectSummaryV1[subjects.Count];
            for (int i = 0; i < subjects.Count; i++)
            {
                SubjectSummary subject = subjects[i];
                result[i] = subject == null
                    ? new CachedSubjectSummaryV1()
                    : new CachedSubjectSummaryV1
                    {
                        id = subject.Id,
                        slug = subject.Slug,
                        name = subject.Name,
                        isActive = subject.IsActive
                    };
            }

            return result;
        }

        private static IReadOnlyList<SubjectSummary> ToDomainSubjects(CachedSubjectSummaryV1[] subjects)
        {
            if (subjects == null || subjects.Length == 0)
            {
                return Array.Empty<SubjectSummary>();
            }

            var result = new SubjectSummary[subjects.Length];
            for (int i = 0; i < subjects.Length; i++)
            {
                CachedSubjectSummaryV1 subject = subjects[i];
                result[i] = subject == null
                    ? new SubjectSummary()
                    : new SubjectSummary
                    {
                        Id = subject.id,
                        Slug = subject.slug,
                        Name = subject.name,
                        IsActive = subject.isActive
                    };
            }

            return result;
        }

        private static CachedMissionSummaryV1[] ToCachedMissions(IReadOnlyList<MissionSummary> missions)
        {
            if (missions == null || missions.Count == 0)
            {
                return Array.Empty<CachedMissionSummaryV1>();
            }

            var result = new CachedMissionSummaryV1[missions.Count];
            for (int i = 0; i < missions.Count; i++)
            {
                MissionSummary mission = missions[i];
                if (mission == null)
                {
                    result[i] = new CachedMissionSummaryV1();
                    continue;
                }

                MissionProgressSummary progress = mission.Progress;
                result[i] = new CachedMissionSummaryV1
                {
                    id = mission.Id,
                    gradeId = mission.GradeId,
                    subjectId = mission.SubjectId,
                    termId = mission.TermId,
                    title = mission.Title,
                    order = mission.Order,
                    status = mission.Status,
                    lockedReason = mission.LockedReason,
                    availabilitySource = mission.AvailabilitySource,
                    teacherPolicy = mission.TeacherPolicy,
                    areaCount = mission.AreaCount,
                    progressState = progress?.State,
                    activeAreaId = progress?.ActiveAreaId,
                    completedAreaCount = progress?.CompletedAreaCount ?? 0,
                    requiredAreaCount = progress?.RequiredAreaCount ?? 3,
                    collectibleCount = progress?.CollectibleCount ?? 0,
                    requiredCollectibleCount = progress?.RequiredCollectibleCount ?? 3,
                    completedAtUtc = progress?.CompletedAt?.ToUniversalTime().ToString("o"),
                    progressRevision = progress?.Revision ?? 0
                };
            }

            return result;
        }

        private static IReadOnlyList<MissionSummary> ToDomainMissions(CachedMissionSummaryV1[] missions)
        {
            if (missions == null || missions.Length == 0)
            {
                return Array.Empty<MissionSummary>();
            }

            var result = new MissionSummary[missions.Length];
            for (int i = 0; i < missions.Length; i++)
            {
                CachedMissionSummaryV1 mission = missions[i];
                if (mission == null)
                {
                    result[i] = new MissionSummary();
                    continue;
                }

                DateTimeOffset? completedAt = null;
                if (!string.IsNullOrWhiteSpace(mission.completedAtUtc)
                    && DateTimeOffset.TryParse(mission.completedAtUtc, out DateTimeOffset parsed))
                {
                    completedAt = parsed;
                }

                result[i] = new MissionSummary
                {
                    Id = mission.id,
                    GradeId = mission.gradeId,
                    SubjectId = mission.subjectId,
                    TermId = mission.termId,
                    Title = mission.title,
                    Order = mission.order,
                    Status = mission.status,
                    LockedReason = mission.lockedReason,
                    AvailabilitySource = mission.availabilitySource,
                    TeacherPolicy = mission.teacherPolicy,
                    AreaCount = mission.areaCount <= 0 ? 3 : mission.areaCount,
                    Progress = new MissionProgressSummary
                    {
                        State = mission.progressState,
                        ActiveAreaId = mission.activeAreaId,
                        CompletedAreaCount = mission.completedAreaCount,
                        RequiredAreaCount = mission.requiredAreaCount <= 0 ? 3 : mission.requiredAreaCount,
                        CollectibleCount = mission.collectibleCount,
                        RequiredCollectibleCount = mission.requiredCollectibleCount <= 0
                            ? 3
                            : mission.requiredCollectibleCount,
                        CompletedAt = completedAt,
                        Revision = mission.progressRevision
                    }
                };
            }

            return result;
        }

        private static CachedSyncStatusV1 ToCachedSync(SyncStatus sync)
        {
            if (sync == null)
            {
                return new CachedSyncStatusV1();
            }

            return new CachedSyncStatusV1
            {
                pendingServerActions = sync.PendingServerActions,
                revision = sync.Revision,
                pendingOutboxCount = sync.PendingOutboxCount,
                lastSyncedAtUtc = sync.LastSyncedAt?.ToUniversalTime().ToString("o")
            };
        }

        private static SyncStatus ToDomainSync(CachedSyncStatusV1 sync)
        {
            if (sync == null)
            {
                return new SyncStatus();
            }

            DateTimeOffset? lastSynced = null;
            if (!string.IsNullOrWhiteSpace(sync.lastSyncedAtUtc)
                && DateTimeOffset.TryParse(sync.lastSyncedAtUtc, out DateTimeOffset parsed))
            {
                lastSynced = parsed;
            }

            return new SyncStatus
            {
                PendingServerActions = sync.pendingServerActions,
                Revision = sync.revision,
                PendingOutboxCount = sync.pendingOutboxCount,
                LastSyncedAt = lastSynced
            };
        }
    }
}
