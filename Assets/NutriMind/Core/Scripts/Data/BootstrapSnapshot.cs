using System;
using System.Collections.Generic;

namespace NutriMind.Core.Data
{
    /// <summary>
    /// Authenticated bootstrap payload used for Home and session restore.
    /// </summary>
    public sealed class BootstrapSnapshot
    {
        public StudentProfile Profile { get; set; }
        public string RequiredManifestVersion { get; set; }
        public IReadOnlyList<SubjectSummary> Subjects { get; set; } = Array.Empty<SubjectSummary>();
        public IReadOnlyList<MissionSummary> Missions { get; set; } = Array.Empty<MissionSummary>();
        public int QuizPortalAvailableCount { get; set; }
        public int AnnouncementsVisibleCount { get; set; }
        public SyncStatus Sync { get; set; }
    }
}
