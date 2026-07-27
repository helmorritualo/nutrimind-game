using System;
using System.Collections.Generic;

namespace NutriMind.Core.Data
{
    /// <summary>
    /// Mission list/detail summary with availability and progress rollup.
    /// </summary>
    public sealed class MissionSummary
    {
        public string Id { get; set; }
        public string GradeId { get; set; }
        public string SubjectId { get; set; }
        public string TermId { get; set; }
        public string Title { get; set; }
        public int Order { get; set; }
        public string Status { get; set; }
        public string LockedReason { get; set; }
        public string AvailabilitySource { get; set; }
        public string TeacherPolicy { get; set; }
        public MissionProgressSummary Progress { get; set; }
        public int AreaCount { get; set; } = 3;
    }

    /// <summary>
    /// Mission progress rollup. Required area and collectible counts are always three.
    /// </summary>
    public sealed class MissionProgressSummary
    {
        public string State { get; set; }
        public string ActiveAreaId { get; set; }
        public int CompletedAreaCount { get; set; }
        public int RequiredAreaCount { get; set; } = 3;
        public int CollectibleCount { get; set; }
        public int RequiredCollectibleCount { get; set; } = 3;
        public DateTimeOffset? CompletedAt { get; set; }
        public int Revision { get; set; }
    }

    /// <summary>
    /// Mission detail including per-area progress. Area 3 is the integrated final challenge.
    /// </summary>
    public sealed class MissionDetail
    {
        public MissionSummary Mission { get; set; }
        public IReadOnlyList<AreaProgressSummary> Areas { get; set; } = Array.Empty<AreaProgressSummary>();
        public IReadOnlyList<string> NewlyUnlockedIds { get; set; } = Array.Empty<string>();
    }

    /// <summary>
    /// Per-area progress summary within a mission.
    /// </summary>
    public sealed class AreaProgressSummary
    {
        public string Id { get; set; }
        public int Order { get; set; }
        public string Phase { get; set; }
        public string State { get; set; }
        public bool ReviewRequired { get; set; }
        public string CollectibleId { get; set; }
        public bool CollectibleCollected { get; set; }
        public DateTimeOffset? CompletedAt { get; set; }
    }
}
