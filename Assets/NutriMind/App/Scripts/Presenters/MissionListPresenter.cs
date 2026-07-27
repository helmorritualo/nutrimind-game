using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NutriMind.App.Routing;
using NutriMind.App.UI;
using NutriMind.Core.Bootstrap;
using NutriMind.Core.Data;
using NutriMind.Core.Networking;
using NutriMind.Core.Persistence;
using NutriMind.Core.Utilities;

namespace NutriMind.App.Presentation
{
    /// <summary>
    /// Runtime presenter for Mission List.
    /// Binds gateway or bootstrap-cached missions and routes using stable mission IDs.
    /// </summary>
    public sealed class MissionListPresenter : RoutePresenterBase
    {
        private readonly MissionSelectionPanelView _view;
        private readonly AppRouteContext _ctx;
        private readonly Dictionary<string, MissionSummary> _missionMap =
            new(StringComparer.Ordinal);

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
            TaskUtilities.ForgetSafely(FetchAsync(RequestToken), RequestToken, "MissionList.Load");
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
            _view.SetItems(Array.Empty<MissionPreviewItem>());
            _view.SetDataState(DataStatePanelState.Loading);
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
                IReadOnlyList<MissionSummary> missions =
                    result.Value ?? Array.Empty<MissionSummary>();
                PersistMissionsCache(missions);
                BindMissions(
                    missions,
                    missions.Count == 0
                        ? DataStatePanelState.Empty
                        : DataStatePanelState.Content);
                return;
            }

            if (IsUnauthorized(result.Error))
            {
                HandleUnauthorized();
                return;
            }

            if (IsOffline(result.Error))
            {
                IReadOnlyList<MissionSummary> cached = GetCachedMissions();
                if (cached.Count > 0)
                {
                    BindMissions(cached, DataStatePanelState.OfflineCached);
                }
                else
                {
                    BindMissions(Array.Empty<MissionSummary>(), DataStatePanelState.OfflineUnavailable);
                }

                return;
            }

            BindMissions(
                Array.Empty<MissionSummary>(),
                AppViewMappers.ErrorToDataState(result.Error));
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

            if (!_missionMap.TryGetValue(selection.MissionId, out MissionSummary mission))
            {
                return;
            }

            TaskUtilities.ForgetSafely(
                Lifetime.Router?.NavigateAsync(
                    AppRouteId.MissionDetail,
                    AppRouteContext.ForMission(
                        mission.Id,
                        mission.SubjectId ?? _ctx?.SubjectId,
                        mission.TermId ?? _ctx?.TermId),
                    NavigationToken),
                NavigationToken,
                "MissionList.Detail");
        }

        private void OnLockedMission(MissionPreviewSelection selection)
        {
            if (Disposed)
            {
                return;
            }

            _missionMap.TryGetValue(selection.MissionId, out MissionSummary mission);
            string reason = !string.IsNullOrWhiteSpace(mission?.LockedReason)
                ? mission.LockedReason
                : !string.IsNullOrWhiteSpace(selection.LockReason)
                    ? selection.LockReason
                : "teacher";

            TaskUtilities.ForgetSafely(
                Lifetime.Router?.NavigateAsync(
                    AppRouteId.LockedMission,
                    AppRouteContext.ForLockedMission(
                        selection.MissionId,
                        reason,
                        mission?.SubjectId ?? _ctx?.SubjectId,
                        mission?.TermId ?? _ctx?.TermId),
                    NavigationToken),
                NavigationToken,
                "MissionList.Locked");
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
                "MissionList.Back");
        }

        private void BindMissions(
            IReadOnlyList<MissionSummary> missions,
            DataStatePanelState state)
        {
            _missionMap.Clear();
            var ordered = new List<MissionSummary>();
            if (missions != null)
            {
                for (int i = 0; i < missions.Count; i++)
                {
                    MissionSummary mission = missions[i];
                    if (mission == null || string.IsNullOrWhiteSpace(mission.Id))
                    {
                        continue;
                    }

                    if (_missionMap.ContainsKey(mission.Id))
                    {
                        continue;
                    }

                    _missionMap[mission.Id] = mission;
                    ordered.Add(mission);
                }
            }

            ordered.Sort(CompareMissionOrderThenId);
            MissionPreviewItem[] items = AppViewMappers.MapMissionSummaries(
                ordered,
                AppViewMappers.MapSubject(_ctx?.SubjectId),
                AppViewMappers.MapTerm(_ctx?.TermId));
            _view.SetItems(items);
            _view.SetDataState(items.Length == 0 && state == DataStatePanelState.Content
                ? DataStatePanelState.Empty
                : state);
        }

        private static int CompareMissionOrderThenId(MissionSummary left, MissionSummary right)
        {
            int order = (left?.Order ?? 0).CompareTo(right?.Order ?? 0);
            if (order != 0)
            {
                return order;
            }

            return string.CompareOrdinal(left?.Id ?? string.Empty, right?.Id ?? string.Empty);
        }

        private IReadOnlyList<MissionSummary> GetCachedMissions()
        {
            string studentId = Lifetime.AuthenticatedStudentState?.Profile?.Id
                ?? Lifetime.CurrentProfile?.Id;
            if (!string.IsNullOrWhiteSpace(studentId)
                && !string.IsNullOrWhiteSpace(_ctx?.SubjectId)
                && !string.IsNullOrWhiteSpace(_ctx?.TermId)
                && Lifetime.ResourceCacheRepository != null)
            {
                AppResult<IReadOnlyList<MissionSummary>> cached = LearnerRouteCache.LoadMissions(
                    Lifetime.ResourceCacheRepository,
                    studentId,
                    _ctx.SubjectId,
                    _ctx.TermId);
                if (cached.IsSuccess && cached.Value != null)
                {
                    return cached.Value;
                }
            }

            IReadOnlyList<MissionSummary> bootstrap = Lifetime.LastBootstrap?.Missions;
            if (bootstrap == null || bootstrap.Count == 0)
            {
                return Array.Empty<MissionSummary>();
            }

            var matches = new List<MissionSummary>();
            for (int i = 0; i < bootstrap.Count; i++)
            {
                MissionSummary mission = bootstrap[i];
                if (mission != null && MatchesContext(mission))
                {
                    matches.Add(mission);
                }
            }

            return matches;
        }

        private bool MatchesContext(MissionSummary mission)
        {
            bool subjectMatches = string.IsNullOrWhiteSpace(_ctx?.SubjectId)
                || string.Equals(mission.SubjectId, _ctx.SubjectId, StringComparison.OrdinalIgnoreCase)
                || (AppViewMappers.TryMapSubject(mission.SubjectId, out NutriMindSubject missionSubject)
                    && AppViewMappers.TryMapSubject(_ctx.SubjectId, out NutriMindSubject routeSubject)
                    && missionSubject == routeSubject);
            bool termMatches = string.IsNullOrWhiteSpace(_ctx?.TermId)
                || string.Equals(mission.TermId, _ctx.TermId, StringComparison.OrdinalIgnoreCase)
                || (AppViewMappers.TryMapTerm(mission.TermId, out NutriMindTerm missionTerm)
                    && AppViewMappers.TryMapTerm(_ctx.TermId, out NutriMindTerm routeTerm)
                    && missionTerm == routeTerm);
            return subjectMatches && termMatches;
        }

        private void PersistMissionsCache(IReadOnlyList<MissionSummary> missions)
        {
            string studentId = Lifetime.AuthenticatedStudentState?.Profile?.Id
                ?? Lifetime.CurrentProfile?.Id;
            if (string.IsNullOrWhiteSpace(studentId)
                || string.IsNullOrWhiteSpace(_ctx?.SubjectId)
                || string.IsNullOrWhiteSpace(_ctx?.TermId)
                || Lifetime.ResourceCacheRepository == null)
            {
                return;
            }

            LearnerRouteCache.SaveMissions(
                Lifetime.ResourceCacheRepository,
                studentId,
                _ctx.SubjectId,
                _ctx.TermId,
                missions ?? Array.Empty<MissionSummary>(),
                DateTimeOffset.UtcNow.ToString("o"));
        }
    }
}
