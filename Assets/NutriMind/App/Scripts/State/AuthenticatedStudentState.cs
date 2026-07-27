using System;
using System.Collections.Generic;
using NutriMind.Core.Data;

namespace NutriMind.App.State
{
    /// <summary>
    /// Observable authenticated student snapshot owned by composition.
    /// Never stores the bearer token.
    /// </summary>
    public sealed class AuthenticatedStudentState
    {
        private readonly object _gate = new object();

        public event Action Changed;

        public StudentProfile Profile { get; private set; }

        public string RequiredManifestVersion { get; private set; }

        public IReadOnlyList<SubjectSummary> Subjects { get; private set; } = Array.Empty<SubjectSummary>();

        public MissionSummary ActiveMission { get; private set; }

        public int QuizAvailableCount { get; private set; }

        public int AnnouncementVisibleCount { get; private set; }

        public long ServerRevision { get; private set; }

        public DateTimeOffset? LastBootstrapUtc { get; private set; }

        public bool HasProfile => Profile != null;

        public void ApplyBootstrap(BootstrapSnapshot snapshot, DateTimeOffset? cachedUtc = null)
        {
            if (snapshot == null)
            {
                return;
            }

            lock (_gate)
            {
                Profile = snapshot.Profile;
                RequiredManifestVersion = snapshot.RequiredManifestVersion;
                Subjects = snapshot.Subjects ?? Array.Empty<SubjectSummary>();
                ActiveMission = FindActiveMission(snapshot.Missions);
                QuizAvailableCount = snapshot.QuizPortalAvailableCount;
                AnnouncementVisibleCount = snapshot.AnnouncementsVisibleCount;
                ServerRevision = snapshot.Sync?.Revision ?? ServerRevision;
                LastBootstrapUtc = cachedUtc ?? DateTimeOffset.UtcNow;
            }

            RaiseChanged();
        }

        public void ApplyProfile(StudentProfile profile)
        {
            lock (_gate)
            {
                Profile = profile;
            }

            RaiseChanged();
        }

        public void SetAnnouncementVisibleCount(int count)
        {
            lock (_gate)
            {
                AnnouncementVisibleCount = Math.Max(0, count);
            }

            RaiseChanged();
        }

        public void SetQuizAvailableCount(int count)
        {
            lock (_gate)
            {
                QuizAvailableCount = Math.Max(0, count);
            }

            RaiseChanged();
        }

        public void SetServerRevision(long revision)
        {
            lock (_gate)
            {
                ServerRevision = revision;
            }

            RaiseChanged();
        }

        public void SetActiveMission(MissionSummary mission)
        {
            lock (_gate)
            {
                ActiveMission = mission;
            }

            RaiseChanged();
        }

        public void Clear()
        {
            lock (_gate)
            {
                Profile = null;
                RequiredManifestVersion = null;
                Subjects = Array.Empty<SubjectSummary>();
                ActiveMission = null;
                QuizAvailableCount = 0;
                AnnouncementVisibleCount = 0;
                ServerRevision = 0;
                LastBootstrapUtc = null;
            }

            RaiseChanged();
        }

        private static MissionSummary FindActiveMission(IReadOnlyList<MissionSummary> missions)
        {
            if (missions == null || missions.Count == 0)
            {
                return null;
            }

            for (int i = 0; i < missions.Count; i++)
            {
                MissionSummary mission = missions[i];
                string state = mission?.Progress?.State;
                if (string.Equals(state, "in_progress", StringComparison.OrdinalIgnoreCase))
                {
                    return mission;
                }
            }

            for (int i = 0; i < missions.Count; i++)
            {
                MissionSummary mission = missions[i];
                string status = mission?.Status;
                if (string.Equals(status, "available", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(status, "in_progress", StringComparison.OrdinalIgnoreCase))
                {
                    return mission;
                }
            }

            return missions[0];
        }

        private void RaiseChanged()
        {
            try
            {
                Changed?.Invoke();
            }
            catch (Exception)
            {
                // Listeners must not break state updates.
            }
        }
    }
}
