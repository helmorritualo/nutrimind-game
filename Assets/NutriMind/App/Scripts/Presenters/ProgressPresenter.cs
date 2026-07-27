using System.Threading;
using System.Threading.Tasks;
using NutriMind.App.Presentation;
using NutriMind.App.Routing;
using NutriMind.App.UI;
using NutriMind.Core.Bootstrap;
using NutriMind.Core.Data;
using NutriMind.Core.Persistence;
using NutriMind.Core.Utilities;

namespace NutriMind.App.Presentation
{
    /// <summary>
    /// Runtime presenter for the Progress route.
    /// Fetches the learner's full progress summary from the server.
    /// Binds supported aggregate fields and exposes local pending-sync state.
    /// </summary>
    public sealed class ProgressPresenter : RoutePresenterBase
    {
        private readonly ProgressPanelView _view;
        private ProgressSummary _cachedSummary;

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

            TaskUtilities.ForgetSafely(FetchAsync(RequestToken), RequestToken, "Progress.Load");
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
                _cachedSummary = result.Value ?? new ProgressSummary();
                ProgressPreviewSummary summary = AppViewMappers.MapProgressSummary(
                    _cachedSummary,
                    TryGetPendingOutboxCount());
                _view.SetSummary(summary);
                _view.SetDataState(
                    summary.IsEmpty ? DataStatePanelState.Empty : DataStatePanelState.Content);
            }
            else if (IsUnauthorized(result.Error))
            {
                HandleUnauthorized();
            }
            else if (IsOffline(result.Error) && _cachedSummary != null)
            {
                _view.SetSummary(AppViewMappers.MapProgressSummary(
                    _cachedSummary,
                    TryGetPendingOutboxCount()));
                _view.SetDataState(DataStatePanelState.OfflineCached);
            }
            else
            {
                _view.SetDataState(AppViewMappers.ErrorToDataState(
                    result.Error,
                    hasCachedData: _cachedSummary != null));
            }
        }

        private void OnRetry()
        {
            if (Disposed)
            {
                return;
            }

            TaskUtilities.ForgetSafely(FetchAsync(RequestToken), RequestToken, "Progress.Retry");
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
                Lifetime.Router?.NavigateAsync(AppRouteId.Leaderboard, cancellationToken: NavigationToken),
                NavigationToken,
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
                Lifetime.Router?.NavigateAsync(AppRouteId.MissionList, ctx, NavigationToken),
                NavigationToken,
                "Progress.MissionReview");
        }

        private int? TryGetPendingOutboxCount()
        {
            IOutboxRepository outbox = Lifetime.OutboxRepository;
            if (outbox == null)
            {
                return null;
            }

            AppResult<int> count = outbox.CountByStates(
                OutboxEventState.Pending,
                OutboxEventState.Sending,
                OutboxEventState.Deferred);
            return count.IsSuccess ? count.Value : null;
        }
    }
}
