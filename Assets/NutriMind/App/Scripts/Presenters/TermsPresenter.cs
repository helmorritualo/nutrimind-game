using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NutriMind.App.Presentation;
using NutriMind.App.Routing;
using NutriMind.App.UI;
using NutriMind.Core.Bootstrap;
using NutriMind.Core.Data;
using NutriMind.Core.Networking;
using NutriMind.Core.Utilities;

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
            _view.TermSelected += OnTermSelected;
            _view.BackRequested += OnBack;
        }

        public void LoadAsync()
        {
            if (Disposed)
            {
                return;
            }

            TaskUtilities.ForgetSafely(FetchAsync(Cts.Token), Cts.Token, "Terms.Load");
        }

        protected override void OnDispose()
        {
            _view.TermSelected -= OnTermSelected;
            _view.BackRequested -= OnBack;
        }

        private async Task FetchAsync(CancellationToken token)
        {
            var request = new GetTermsRequest { SubjectSlug = _ctx.SubjectSlug ?? _ctx.SubjectId };

            AppResult<IReadOnlyList<TermSummary>> result =
                await Lifetime.Gateway.GetTermsAsync(request, token).ConfigureAwait(true);

            if (Disposed || token.IsCancellationRequested)
            {
                return;
            }

            _termMap.Clear();

            if (result.IsSuccess && result.Value != null)
            {
                for (int i = 0; i < result.Value.Count; i++)
                {
                    TermSummary t = result.Value[i];
                    NutriMindTerm mapped = AppViewMappers.MapTerm(t.Id);
                    if (!_termMap.ContainsKey(mapped))
                    {
                        _termMap[mapped] = t;
                    }
                }
            }
            else if (IsUnauthorized(result.Error))
            {
                HandleUnauthorized();
                return;
            }

            FillDefaultTermsIfEmpty();
            NutriMindSubject subject = AppViewMappers.MapSubject(_ctx.SubjectId);
            _view.SetSubject(subject);
        }

        private void FillDefaultTermsIfEmpty()
        {
            if (!_termMap.ContainsKey(NutriMindTerm.Term1))
            {
                _termMap[NutriMindTerm.Term1] = new TermSummary { Id = "t1", Name = "Term 1" };
            }

            if (!_termMap.ContainsKey(NutriMindTerm.Term2))
            {
                _termMap[NutriMindTerm.Term2] = new TermSummary { Id = "t2", Name = "Term 2" };
            }

            if (!_termMap.ContainsKey(NutriMindTerm.Term3))
            {
                _termMap[NutriMindTerm.Term3] = new TermSummary { Id = "t3", Name = "Term 3" };
            }
        }

        private void OnTermSelected(NutriMindTerm term)
        {
            if (Disposed)
            {
                return;
            }

            _termMap.TryGetValue(term, out TermSummary summary);
            string termId = summary?.Id ?? term.ToString().ToLowerInvariant();

            TaskUtilities.ForgetSafely(
                Lifetime.Router?.NavigateAsync(
                    AppRouteId.MissionList,
                    AppRouteContext.ForTerm(_ctx.SubjectId, termId, _ctx.SubjectSlug),
                    Cts.Token),
                Cts.Token,
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
                    Cts.Token),
                Cts.Token,
                "Terms.Back");
        }
    }
}
