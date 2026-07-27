using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NutriMind.App.Presentation;
using NutriMind.App.Routing;
using NutriMind.App.UI;
using NutriMind.Core.Bootstrap;
using NutriMind.Core.Data;
using NutriMind.Core.Networking;
using NutriMind.Core.Persistence;
using NutriMind.Core.Utilities;
using UnityEngine;

namespace NutriMind.App.Presentation
{
    /// <summary>
    /// Runtime presenter for the Term Selection route.
    /// Fetches available terms from the server for the selected subject.
    /// Navigates to Mission List on term tap.
    /// </summary>
    public sealed class TermsPresenter : RoutePresenterBase
    {
        private readonly TermSelectionPanelView _view;
        private readonly AppRouteContext _ctx;
        private readonly Dictionary<NutriMindTerm, TermSummary> _termMap =
            new Dictionary<NutriMindTerm, TermSummary>();

        public TermsPresenter(AppLifetime lifetime, TermSelectionPanelView view, AppRouteContext ctx)
            : base(lifetime)
        {
            _view = view;
            _ctx = ctx;
            _view.OpenTermRequested += OnTermSelected;
            _view.BackRequested += OnBack;
        }

        public void LoadAsync()
        {
            if (Disposed)
            {
                return;
            }

            _view.SetSubject(AppViewMappers.MapSubject(_ctx?.SubjectId));
            TaskUtilities.ForgetSafely(FetchAsync(RequestToken), RequestToken, "Terms.Load");
        }

        protected override void OnDispose()
        {
            _view.OpenTermRequested -= OnTermSelected;
            _view.BackRequested -= OnBack;
        }

        private async Task FetchAsync(CancellationToken token)
        {
            _view.SetDataState(DataStatePanelState.Loading);
            var request = new GetTermsRequest
            {
                SubjectSlug = _ctx?.SubjectSlug ?? _ctx?.SubjectId
            };

            AppResult<IReadOnlyList<TermSummary>> result =
                await Lifetime.Gateway.GetTermsAsync(request, token).ConfigureAwait(true);

            if (Disposed || token.IsCancellationRequested)
            {
                return;
            }

            _termMap.Clear();

            if (result.IsSuccess)
            {
                BindTerms(result.Value);
                PersistTermsCache(result.Value);
                _view.SetDataState(
                    _termMap.Count == 0
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
                IReadOnlyList<TermSummary> cached = LoadTermsCache();
                if (cached == null || cached.Count == 0)
                {
                    cached = GetBootstrapTerms();
                }

                if (cached != null && cached.Count > 0)
                {
                    BindTerms(cached);
                    _view.SetDataState(DataStatePanelState.OfflineCached);
                    return;
                }

                BindTerms(Array.Empty<TermSummary>());
                _view.SetDataState(DataStatePanelState.OfflineUnavailable);
                return;
            }

            BindTerms(Array.Empty<TermSummary>());
            _view.SetDataState(AppViewMappers.ErrorToDataState(result.Error));
        }

        private void OnTermSelected(NutriMindTerm term)
        {
            if (Disposed)
            {
                return;
            }

            if (!_termMap.TryGetValue(term, out TermSummary summary)
                || string.IsNullOrWhiteSpace(summary?.Id))
            {
                return;
            }

            TaskUtilities.ForgetSafely(
                Lifetime.Router?.NavigateAsync(
                    AppRouteId.MissionList,
                    AppRouteContext.ForTerm(_ctx?.SubjectId, summary.Id, _ctx?.SubjectSlug),
                    NavigationToken),
                NavigationToken,
                "Terms.Select");
        }

        private void OnBack()
        {
            if (Disposed)
            {
                return;
            }

            TaskUtilities.ForgetSafely(
                Lifetime.Router?.NavigateAsync(
                    AppRouteId.Subjects,
                    AppRouteContext.Empty,
                    NavigationToken),
                NavigationToken,
                "Terms.Back");
        }

        private void BindTerms(IReadOnlyList<TermSummary> terms)
        {
            _termMap.Clear();
            if (terms != null)
            {
                for (int i = 0; i < terms.Count; i++)
                {
                    TermSummary summary = terms[i];
                    if (!TryMapTerm(summary, out NutriMindTerm mapped))
                    {
                        if (summary != null)
                        {
                            Debug.LogWarning(
                                $"[TermsPresenter] Ignoring unknown term '{summary.Id ?? summary.Name}'.");
                        }

                        continue;
                    }

                    if (!_termMap.ContainsKey(mapped))
                    {
                        _termMap[mapped] = summary;
                    }
                }
            }

            _view.SetTerms(new List<NutriMindTerm>(_termMap.Keys));
        }

        private IReadOnlyList<TermSummary> GetBootstrapTerms()
        {
            IReadOnlyList<MissionSummary> missions = Lifetime.LastBootstrap?.Missions;
            if (missions == null || missions.Count == 0)
            {
                return System.Array.Empty<TermSummary>();
            }

            var terms = new Dictionary<NutriMindTerm, TermSummary>();
            for (int i = 0; i < missions.Count; i++)
            {
                MissionSummary mission = missions[i];
                if (mission == null || !MatchesSubject(mission.SubjectId))
                {
                    continue;
                }

                var summary = new TermSummary
                {
                    Id = mission.TermId,
                    Name = mission.TermId,
                    IsActive = true
                };
                if (TryMapTerm(summary, out NutriMindTerm term) && !terms.ContainsKey(term))
                {
                    summary.Order = (int)term;
                    terms[term] = summary;
                }
            }

            return new List<TermSummary>(terms.Values);
        }

        private bool MatchesSubject(string missionSubjectId)
        {
            if (string.IsNullOrWhiteSpace(_ctx?.SubjectId))
            {
                return true;
            }

            return string.Equals(
                       missionSubjectId,
                       _ctx.SubjectId,
                       System.StringComparison.OrdinalIgnoreCase)
                   || (AppViewMappers.TryMapSubject(
                           missionSubjectId,
                           out NutriMindSubject missionSubject)
                       && AppViewMappers.TryMapSubject(
                           _ctx.SubjectId,
                           out NutriMindSubject routeSubject)
                       && missionSubject == routeSubject);
        }

        private static bool TryMapTerm(TermSummary summary, out NutriMindTerm term)
        {
            term = default;
            if (summary == null)
            {
                return false;
            }

            if (summary.Order >= (int)NutriMindTerm.Term1
                && summary.Order <= (int)NutriMindTerm.Term3)
            {
                term = (NutriMindTerm)summary.Order;
                return true;
            }

            return AppViewMappers.TryMapTerm(summary.Id, out term)
                || AppViewMappers.TryMapTerm(summary.Name, out term);
        }

        private void PersistTermsCache(IReadOnlyList<TermSummary> terms)
        {
            string studentId = Lifetime.AuthenticatedStudentState?.Profile?.Id
                ?? Lifetime.CurrentProfile?.Id;
            string subjectId = _ctx?.SubjectId;
            if (string.IsNullOrWhiteSpace(studentId)
                || string.IsNullOrWhiteSpace(subjectId)
                || Lifetime.ResourceCacheRepository == null)
            {
                return;
            }

            LearnerRouteCache.SaveTerms(
                Lifetime.ResourceCacheRepository,
                studentId,
                subjectId,
                terms ?? Array.Empty<TermSummary>(),
                DateTimeOffset.UtcNow.ToString("o"));
        }

        private IReadOnlyList<TermSummary> LoadTermsCache()
        {
            string studentId = Lifetime.AuthenticatedStudentState?.Profile?.Id
                ?? Lifetime.CurrentProfile?.Id;
            string subjectId = _ctx?.SubjectId;
            if (string.IsNullOrWhiteSpace(studentId)
                || string.IsNullOrWhiteSpace(subjectId)
                || Lifetime.ResourceCacheRepository == null)
            {
                return null;
            }

            AppResult<IReadOnlyList<TermSummary>> cached = LearnerRouteCache.LoadTerms(
                Lifetime.ResourceCacheRepository,
                studentId,
                subjectId);
            return cached.IsSuccess ? cached.Value : null;
        }
    }
}
