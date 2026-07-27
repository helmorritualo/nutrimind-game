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
    /// Runtime presenter for the Progress route.
    /// Fetches the learner's full progress summary from the server.
    /// Progress panel is mainly static-fixture-driven; the presenter sets the correct
    /// data state and wires navigation events.
    /// </summary>
    public sealed class ProgressPresenter : RoutePresenterBase
    {
        private readonly ProgressPanelView _view;

        public ProgressPresenter(AppLifetime lifetime, ProgressPanelView view)
            : base(lifetime)
        {
            _view = view;
            _view.RetryRequested += OnRetry;
            _view.QuizPortalRequested += OnQuizPortalRequested;
            _view.LeaderboardRequested += OnLeaderboardRequested;
            _view.MissionReviewRequested += OnMissionReviewRequested;
        }

        public void LoadAsync()
        {
            if (Disposed)
            {
                return;
            }

            TaskUtilities.ForgetSafely(FetchAsync(Cts.Token), Cts.Token, "Progress.Load");
        }

        protected override void OnDispose()
        {
            _view.RetryRequested -= OnRetry;
            _view.QuizPortalRequested -= OnQuizPortalRequested;
            _view.LeaderboardRequested -= OnLeaderboardRequested;
            _view.MissionReviewRequested -= OnMissionReviewRequested;
        }

        private async Task FetchAsync(CancellationToken token)
        {
            _view.SetDataState(DataStatePanelState.Loading);

            AppResult<ProgressSummary> result =
                await Lifetime.Gateway.GetProgressSummaryAsync(token).ConfigureAwait(true);

            if (Disposed || token.IsCancellationRequested)
            {
                return;
            }

            if (result.IsSuccess)
            {
                // ProgressPanelView uses static fixtures for visual content; we surface
                // the live data state so non-content overlays (loading, error) work correctly.
                _view.SetDataState(DataStatePanelState.Content);
            }
            else if (IsUnauthorized(result.Error))
            {
                HandleUnauthorized();
            }
            else
            {
                _view.SetDataState(AppViewMappers.ErrorToDataState(result.Error));
            }
        }

        private void OnRetry()
        {
            if (Disposed)
            {
                return;
            }

            TaskUtilities.ForgetSafely(FetchAsync(Cts.Token), Cts.Token, "Progress.Retry");
        }

        private void OnQuizPortalRequested()
        {
            if (Disposed)
            {
                return;
            }

            TaskUtilities.ForgetSafely(
                Lifetime.Router?.EnterQuizPortalAsync(cancellationToken: NavigationToken),
                NavigationToken,
                "Progress.EnterQuizPortal");
        }

        private void OnLeaderboardRequested()
        {
            if (Disposed)
            {
                return;
            }

            TaskUtilities.ForgetSafely(
                Lifetime.Router?.NavigateAsync(AppRouteId.Leaderboard, cancellationToken: Cts.Token),
                Cts.Token,
                "Progress.Leaderboard");
        }

        private void OnMissionReviewRequested(ProgressMissionPreviewSelection selection)
        {
            if (Disposed)
            {
                return;
            }

            // ProgressMissionPreviewSelection carries a presentation MissionNumber (int), not a
            // canonical mission ID, so we navigate to MissionList with the subject/term context
            // and let the learner reach the exact mission from there.
            var ctx = AppRouteContext.ForTerm(
                AppViewMappers.SubjectToId(selection.Subject),
                AppViewMappers.TermToId(selection.Term));

            TaskUtilities.ForgetSafely(
                Lifetime.Router?.NavigateAsync(AppRouteId.MissionList, ctx, Cts.Token),
                Cts.Token,
                "Progress.MissionReview");
        }
    }
}
