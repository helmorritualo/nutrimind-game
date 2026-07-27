using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NutriMind.App.Presentation;
using NutriMind.App.Routing;
using NutriMind.App.UI;
using NutriMind.Core.Bootstrap;
using NutriMind.Core.Data;
using NutriMind.Core.Utilities;
using UnityEngine;

namespace NutriMind.App.Presentation
{
    /// <summary>
    /// Runtime presenter for the Subjects route.
    /// Fetches available subjects from the server and binds the view.
    /// Navigates to Term Selection on subject tap, passing the selected subject's server ID.
    /// </summary>
    public sealed class SubjectsPresenter : RoutePresenterBase
    {
        private readonly SubjectSelectionPanelView _view;

        // Maps NutriMindSubject enum → server SubjectSummary so routing gets the real ID.
        private readonly Dictionary<NutriMindSubject, SubjectSummary> _subjectMap =
            new Dictionary<NutriMindSubject, SubjectSummary>();

        public SubjectsPresenter(AppLifetime lifetime, SubjectSelectionPanelView view)
            : base(lifetime)
        {
            _view = view;
            _view.ContinueSubjectRequested += OnSubjectSelected;
            _view.BackRequested += OnBack;
        }

        public void LoadAsync()
        {
            if (Disposed)
            {
                return;
            }

            TaskUtilities.ForgetSafely(FetchAsync(RequestToken), RequestToken, "Subjects.Load");
        }

        protected override void OnDispose()
        {
            _view.ContinueSubjectRequested -= OnSubjectSelected;
            _view.BackRequested -= OnBack;
        }

        private async Task FetchAsync(CancellationToken token)
        {
            _view.SetDataState(DataStatePanelState.Loading);
            AppResult<System.Collections.Generic.IReadOnlyList<SubjectSummary>> result =
                await Lifetime.Gateway.GetSubjectsAsync(token).ConfigureAwait(true);

            if (Disposed || token.IsCancellationRequested)
            {
                return;
            }

            _subjectMap.Clear();

            if (result.IsSuccess)
            {
                BindSubjects(result.Value);
                _view.SetDataState(
                    _subjectMap.Count == 0
                        ? DataStatePanelState.Empty
                        : DataStatePanelState.Content);
                return;
            }

            if (IsUnauthorized(result.Error))
            {
                HandleUnauthorized();
                return;
            }

            IReadOnlyList<SubjectSummary> cached = Lifetime.LastBootstrap?.Subjects;
            if (cached != null && cached.Count > 0)
            {
                BindSubjects(cached);
                if (_subjectMap.Count > 0)
                {
                    _view.SetDataState(DataStatePanelState.OfflineCached);
                    return;
                }
            }

            _view.Bind(System.Array.Empty<NutriMindSubject>());
            _view.SetDataState(AppViewMappers.ErrorToDataState(result.Error));
        }

        private void OnSubjectSelected(NutriMindSubject subject)
        {
            if (Disposed)
            {
                return;
            }

            if (!_subjectMap.TryGetValue(subject, out SubjectSummary summary)
                || string.IsNullOrWhiteSpace(summary?.Id))
            {
                return;
            }

            string subjectId = summary.Id;
            string subjectSlug = string.IsNullOrWhiteSpace(summary.Slug)
                ? subjectId
                : summary.Slug;

            TaskUtilities.ForgetSafely(
                Lifetime.Router?.NavigateAsync(
                    AppRouteId.Terms,
                    AppRouteContext.ForSubject(subjectId, subjectSlug),
                    NavigationToken),
                NavigationToken,
                "Subjects.Select");
        }

        private void OnBack()
        {
            if (Disposed)
            {
                return;
            }

            TaskUtilities.ForgetSafely(
                Lifetime.Router?.NavigateAsync(AppRouteId.Home, AppRouteContext.Empty, NavigationToken),
                NavigationToken,
                "Subjects.Back");
        }

        private void BindSubjects(IReadOnlyList<SubjectSummary> subjects)
        {
            _subjectMap.Clear();
            if (subjects != null)
            {
                for (int i = 0; i < subjects.Count; i++)
                {
                    SubjectSummary summary = subjects[i];
                    if (!TryMapSubject(summary, out NutriMindSubject mapped))
                    {
                        if (summary != null)
                        {
                            Debug.LogWarning(
                                $"[SubjectsPresenter] Ignoring unknown subject '{summary.Id ?? summary.Slug ?? summary.Name}'.");
                        }

                        continue;
                    }

                    if (!_subjectMap.ContainsKey(mapped))
                    {
                        _subjectMap[mapped] = summary;
                    }
                }
            }

            _view.Bind(new List<NutriMindSubject>(_subjectMap.Keys));
        }

        private static bool TryMapSubject(
            SubjectSummary summary,
            out NutriMindSubject subject)
        {
            subject = default;
            return summary != null
                && (AppViewMappers.TryMapSubject(summary.Id, out subject)
                    || AppViewMappers.TryMapSubject(summary.Slug, out subject)
                    || AppViewMappers.TryMapSubject(summary.Name, out subject));
        }
    }
}
