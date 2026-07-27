using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NutriMind.App.Routing;
using NutriMind.App.UI;
using NutriMind.Core.Bootstrap;
using NutriMind.Core.Data;
using NutriMind.Core.Networking;
using NutriMind.Core.Utilities;

namespace NutriMind.App.Presentation
{
    /// <summary>
    /// Runtime presenter for Mission List.
    /// Uses SetContext + existing selection events. Preview catalog provides card visuals;
    /// navigation uses stable mission IDs from MissionPreviewSelection.
    /// </summary>
    public sealed class MissionListPresenter : RoutePresenterBase
    {
        private readonly MissionSelectionPanelView _view;
        private readonly AppRouteContext _ctx;
        private IReadOnlyList<MissionSummary> _serverMissions;

        public MissionListPresenter(AppLifetime lifetime, MissionSelectionPanelView view, AppRouteContext ctx)
            : base(lifetime)
        {
            _view = view;
            _ctx = ctx;
            _view.MissionSelected += OnMissionSelected;
            _view.StartMissionRequested += OnOpenMission;
            _view.ContinueMissionRequested += OnOpenMission;
            _view.ReviewMissionRequested += OnOpenMission;
            _view.LockedMissionRequested += OnLockedMission;
            _view.BackRequested += OnBack;
        }

        public void LoadAsync()
        {
            if (Disposed)
            {
                return;
            }

            NutriMindSubject subject = AppViewMappers.MapSubject(_ctx?.SubjectId);
            NutriMindTerm term = AppViewMappers.MapTerm(_ctx?.TermId);
            _view.SetContext(subject, term);
            TaskUtilities.ForgetSafely(FetchAsync(Cts.Token), Cts.Token, "MissionList.Load");
        }

        protected override void OnDispose()
        {
            _view.MissionSelected -= OnMissionSelected;
            _view.StartMissionRequested -= OnOpenMission;
            _view.ContinueMissionRequested -= OnOpenMission;
            _view.ReviewMissionRequested -= OnOpenMission;
            _view.LockedMissionRequested -= OnLockedMission;
            _view.BackRequested -= OnBack;
        }

        private async Task FetchAsync(CancellationToken token)
        {
            var request = new GetMissionsRequest
            {
                SubjectId = _ctx?.SubjectId,
                TermId = _ctx?.TermId
            };

            AppResult<IReadOnlyList<MissionSummary>> result =
                await Lifetime.Gateway.GetMissionsAsync(request, token).ConfigureAwait(true);

            if (Disposed || token.IsCancellationRequested)
            {
                return;
            }

            if (result.IsSuccess)
            {
                _serverMissions = result.Value ?? Array.Empty<MissionSummary>();
                return;
            }

            if (IsUnauthorized(result.Error))
            {
                HandleUnauthorized();
            }
        }

        private void OnMissionSelected(MissionPreviewSelection selection)
        {
            // Selection only highlights; open actions navigate.
        }

        private void OnOpenMission(MissionPreviewSelection selection)
        {
            if (Disposed || string.IsNullOrWhiteSpace(selection.MissionId))
            {
                return;
            }

            if (selection.IsLocked)
            {
                OnLockedMission(selection);
                return;
            }

            TaskUtilities.ForgetSafely(
                Lifetime.Router?.NavigateAsync(
                    AppRouteId.MissionDetail,
                    AppRouteContext.ForMission(selection.MissionId, _ctx?.SubjectId, _ctx?.TermId),
                    Cts.Token),
                Cts.Token,
                "MissionList.Detail");
        }

        private void OnLockedMission(MissionPreviewSelection selection)
        {
            if (Disposed)
            {
                return;
            }

            string reason = !string.IsNullOrWhiteSpace(selection.LockReason)
                ? selection.LockReason
                : "teacher";

            TaskUtilities.ForgetSafely(
                Lifetime.Router?.NavigateAsync(
                    AppRouteId.LockedMission,
                    AppRouteContext.ForLockedMission(selection.MissionId, reason),
                    Cts.Token),
                Cts.Token,
                "MissionList.Locked");
        }

        private void OnBack()
        {
            if (Disposed)
            {
                return;
            }

            TaskUtilities.ForgetSafely(
                Lifetime.Router?.BackAsync(Cts.Token),
                Cts.Token,
                "MissionList.Back");
        }
    }
}
