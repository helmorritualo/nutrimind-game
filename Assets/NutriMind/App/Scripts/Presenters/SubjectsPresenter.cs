using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NutriMind.App.Presentation;
using NutriMind.App.Routing;
using NutriMind.App.UI;
using NutriMind.Core.Bootstrap;
using NutriMind.Core.Data;
using NutriMind.Core.Utilities;

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
            _view.SubjectSelected += OnSubjectSelected;
        }

        public void LoadAsync()
        {
            if (Disposed)
            {
                return;
            }

            FetchAsync(Cts.Token);
        }

        protected override void OnDispose()
        {
            _view.SubjectSelected -= OnSubjectSelected;
        }

        private async Task FetchAsync(CancellationToken token)
        {
            AppResult<System.Collections.Generic.IReadOnlyList<SubjectSummary>> result =
                await Lifetime.Gateway.GetSubjectsAsync(token).ConfigureAwait(true);

            if (Disposed || token.IsCancellationRequested)
            {
                return;
            }

            _subjectMap.Clear();

            if (result.IsSuccess && result.Value != null)
            {
                for (int i = 0; i < result.Value.Count; i++)
                {
                    SubjectSummary s = result.Value[i];
                    NutriMindSubject mapped = AppViewMappers.MapSubject(s.Id);
                    if (!_subjectMap.ContainsKey(mapped))
                    {
                        _subjectMap[mapped] = s;
                    }
                }
            }
            else if (IsUnauthorized(result.Error))
            {
                HandleUnauthorized();
                return;
            }

            // Fall back to canonical three subjects from bootstrap subjects if server call fails.
            FillDefaultSubjectsIfEmpty();
            _view.Bind(new List<NutriMindSubject>(_subjectMap.Keys));
        }

        private void FillDefaultSubjectsIfEmpty()
        {
            if (!_subjectMap.ContainsKey(NutriMindSubject.LiteraQuest))
            {
                _subjectMap[NutriMindSubject.LiteraQuest] = new SubjectSummary
                {
                    Id = "lq", Slug = "lq", Name = "LiteraQuest"
                };
            }

            if (!_subjectMap.ContainsKey(NutriMindSubject.PeAndHealth))
            {
                _subjectMap[NutriMindSubject.PeAndHealth] = new SubjectSummary
                {
                    Id = "peh", Slug = "peh", Name = "PE & Health"
                };
            }

            if (!_subjectMap.ContainsKey(NutriMindSubject.Science))
            {
                _subjectMap[NutriMindSubject.Science] = new SubjectSummary
                {
                    Id = "sci", Slug = "sci", Name = "Science"
                };
            }
        }

        private void OnSubjectSelected(NutriMindSubject subject)
        {
            if (Disposed)
            {
                return;
            }

            _subjectMap.TryGetValue(subject, out SubjectSummary summary);
            string subjectId = summary?.Id ?? subject.ToString().ToLowerInvariant();
            string subjectSlug = summary?.Slug ?? subjectId;

            TaskUtilities.ForgetSafely(
                Lifetime.Router?.NavigateAsync(
                    AppRouteId.Terms,
                    AppRouteContext.ForSubject(subjectId, subjectSlug),
                    Cts.Token),
                Cts.Token,
                "Subjects.Select");
        }
    }
}
