using System;

namespace NutriMind.Core.Persistence
{
    /// <summary>
    /// JsonUtility-compatible Bootstrap cache envelope (schema_version = 1).
    /// Never includes access token, PIN, answer keys, or leaderboard data.
    /// </summary>
    [Serializable]
    public sealed class CachedBootstrapSnapshotV1
    {
        public int schemaVersion = 1;
        public string requiredManifestVersion;
        public CachedStudentProfileV1 profile;
        public CachedSubjectSummaryV1[] subjects;
        public CachedMissionSummaryV1[] missions;
        public int quizPortalAvailableCount;
        public int announcementsVisibleCount;
        public CachedSyncStatusV1 sync;
        public string cachedUtc;
    }

    [Serializable]
    public sealed class CachedStudentProfileV1
    {
        public string id;
        public string displayName;
        public string lrnMasked;
        public string gradeId;
        public CachedStudentSectionV1 section;
        public bool isActive;
    }

    [Serializable]
    public sealed class CachedStudentSectionV1
    {
        public string id;
        public string name;
        public string gradeId;
    }

    [Serializable]
    public sealed class CachedSubjectSummaryV1
    {
        public string id;
        public string slug;
        public string name;
        public bool isActive;
    }

    [Serializable]
    public sealed class CachedMissionSummaryV1
    {
        public string id;
        public string gradeId;
        public string subjectId;
        public string termId;
        public string title;
        public int order;
        public string status;
        public string lockedReason;
        public string availabilitySource;
        public string teacherPolicy;
        public int areaCount = 3;
        public string progressState;
        public string activeAreaId;
        public int completedAreaCount;
        public int requiredAreaCount = 3;
        public int collectibleCount;
        public int requiredCollectibleCount = 3;
        public string completedAtUtc;
        public int progressRevision;
    }

    [Serializable]
    public sealed class CachedSyncStatusV1
    {
        public bool pendingServerActions;
        public int revision;
        public int pendingOutboxCount;
        public string lastSyncedAtUtc;
    }
}
