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
    /// Runtime presenter for the Leaderboard route.
    /// Leaderboard is online-only — no local caching of scores.
    /// Shows OfflineUnavailable when offline.
    /// Displays privacy-safe display aliases only; highlights the current learner.
    /// </summary>
    public sealed class LeaderboardPresenter : RoutePresenterBase
    {
        private readonly LeaderboardPanelView _view;

        public LeaderboardPresenter(AppLifetime lifetime, LeaderboardPanelView view)
            : base(lifetime)
        {
            _view = view;
            _view.BackToProgressRequested += OnBack;
            _view.RetryRequested += OnRetry;
        }

        public void LoadAsync()
        {
            if (Disposed)
            {
                return;
            }

            TaskUtilities.ForgetSafely(FetchAsync(Cts.Token), Cts.Token, "Leaderboard.Load");
        }

        protected override void OnDispose()
        {
            _view.BackToProgressRequested -= OnBack;
            _view.RetryRequested -= OnRetry;
        }

        private async Task FetchAsync(CancellationToken token)
        {
            _view.SetPreviewState(LeaderboardPreviewState.Loading);

            var request = new GetLeaderboardRequest
            {
                Scope = "classroom"
            };

            AppResult<LeaderboardPage> result =
                await Lifetime.Gateway.GetLeaderboardAsync(request, token).ConfigureAwait(true);

            if (Disposed || token.IsCancellationRequested)
            {
                return;
            }

            if (result.IsSuccess)
            {
                LeaderboardPage page = result.Value ?? new LeaderboardPage();
                if (page.Entries == null || page.Entries.Count == 0)
                {
                    _view.SetPreviewState(LeaderboardPreviewState.Empty);
                }
                else
                {
                    _view.SetData(AppViewMappers.MapLeaderboardPage(page));
                    _view.SetPreviewState(LeaderboardPreviewState.Content);
                }
            }
            else if (IsUnauthorized(result.Error))
            {
                HandleUnauthorized();
            }
            else if (IsOffline(result.Error))
            {
                // Leaderboard never caches — always show offline unavailable.
                _view.SetPreviewState(LeaderboardPreviewState.OfflineUnavailable);
            }
            else
            {
                _view.SetPreviewState(AppViewMappers.ErrorToLeaderboardPreviewState(result.Error));
            }
        }

        private void OnRetry()
        {
            if (Disposed)
            {
                return;
            }

            TaskUtilities.ForgetSafely(FetchAsync(Cts.Token), Cts.Token, "Leaderboard.Retry");
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
                "Leaderboard.Back");
        }
    }
}
