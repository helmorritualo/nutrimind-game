using System;
using NutriMind.App.Routing;
using NutriMind.App.UI;
using NutriMind.Core.Bootstrap;
using NutriMind.Core.Utilities;

namespace NutriMind.App.Presentation
{
    /// <summary>
    /// Runtime presenter for Locked Mission.
    /// </summary>
    public sealed class LockedMissionPresenter : RoutePresenterBase
    {
        private readonly LockedMissionPanelView _view;
        private readonly AppRouteContext _ctx;

        public LockedMissionPresenter(AppLifetime lifetime, LockedMissionPanelView view, AppRouteContext ctx)
            : base(lifetime)
        {
            _view = view;
            _ctx = ctx;
            _view.BackRequested += OnBack;
            _view.PrimaryActionRequested += OnBack;
        }

        public void Load()
        {
            if (Disposed)
            {
                return;
            }

            MissionLockReason reason = MapLockReason(_ctx?.LockReason);
            var context = new LockedMissionPreviewContext(
                AppViewMappers.MapSubject(_ctx?.SubjectId),
                AppViewMappers.MapTerm(_ctx?.TermId),
                1,
                _ctx?.MissionId ?? "Locked Mission",
                reason,
                _ctx?.LockReason ?? string.Empty);
            _view.SetContext(context);
        }

        protected override void OnDispose()
        {
            _view.BackRequested -= OnBack;
            _view.PrimaryActionRequested -= OnBack;
        }

        private void OnBack()
        {
            if (Disposed)
            {
                return;
            }

            TaskUtilities.ForgetSafely(
                Lifetime.Router?.BackAsync(NavigationToken),
                NavigationToken,
                "LockedMission.Back");
        }

        private static MissionLockReason MapLockReason(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                return MissionLockReason.TeacherRestricted;
            }

            if (reason.IndexOf("prereq", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return MissionLockReason.PrerequisiteRequired;
            }

            if (reason.IndexOf("publish", StringComparison.OrdinalIgnoreCase) >= 0
                || reason.IndexOf("server", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return MissionLockReason.NotPublished;
            }

            if (reason.IndexOf("download", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return MissionLockReason.NotDownloaded;
            }

            if (reason.IndexOf("offline", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return MissionLockReason.OfflineUnavailable;
            }

            return MissionLockReason.TeacherRestricted;
        }
    }
}
