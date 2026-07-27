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

namespace NutriMind.App.Presentation
{
    /// <summary>
    /// Runtime presenter for the Rewards route.
    /// Fetches available rewards from the server and maps them to <see cref="RewardsPreviewItem"/>.
    /// UseReward requests are idempotent: a unique UUID is generated per tap and stored
    /// in IdempotentRequestRepository for timeout-retry with the identical payload.
    /// </summary>
    public sealed class RewardsPresenter : RoutePresenterBase
    {
        private readonly RewardsPanelView _view;
        private readonly AppShellRuntimeController _shellRuntime;

        // Keeps the last fetched list so we can look up RewardCode by PresentationKey.
        private IReadOnlyList<RewardSummary> _lastFetchedRewards;

        public RewardsPresenter(AppLifetime lifetime, RewardsPanelView view, AppShellRuntimeController shellRuntime)
            : base(lifetime)
        {
            _view = view;
            _shellRuntime = shellRuntime;
            _view.UseRewardRequested += OnUseRewardSelected;
            _view.ViewCertificatesRequested += OnViewCertificatesRequested;
            _view.BackToHomeRequested += OnBack;
            _view.RetryRequested += OnRetry;
        }

        public void LoadAsync()
        {
            if (Disposed)
            {
                return;
            }

            TaskUtilities.ForgetSafely(FetchAsync(Cts.Token), Cts.Token, "Rewards.Load");
        }

        protected override void OnDispose()
        {
            _view.UseRewardRequested -= OnUseRewardSelected;
            _view.ViewCertificatesRequested -= OnViewCertificatesRequested;
            _view.BackToHomeRequested -= OnBack;
            _view.RetryRequested -= OnRetry;
        }

        private async Task FetchAsync(CancellationToken token)
        {
            _view.SetPreviewState(RewardsPreviewState.Loading);

            AppResult<IReadOnlyList<RewardSummary>> result =
                await Lifetime.Gateway.GetRewardsAsync(token).ConfigureAwait(true);

            if (Disposed || token.IsCancellationRequested)
            {
                return;
            }

            if (result.IsSuccess)
            {
                IReadOnlyList<RewardSummary> items = result.Value
                    ?? (IReadOnlyList<RewardSummary>)System.Array.Empty<RewardSummary>();
                _lastFetchedRewards = items;

                if (items.Count == 0)
                {
                    _view.SetPreviewState(RewardsPreviewState.Empty);
                }
                else
                {
                    _view.SetItems(AppViewMappers.MapRewardSummaries(items));
                    _view.SetPreviewState(RewardsPreviewState.Content);
                }
            }
            else if (IsUnauthorized(result.Error))
            {
                HandleUnauthorized();
            }
            else
            {
                _view.SetPreviewState(AppViewMappers.ErrorToRewardsPreviewState(result.Error));
            }
        }

        private void OnUseRewardSelected(RewardsPreviewSelection selection)
        {
            if (Disposed)
            {
                return;
            }

            RewardSummary reward = FindRewardByPresentationKey(selection.PresentationKey);
            if (reward == null)
            {
                return;
            }

            _shellRuntime?.RequestUseReward(
                reward.Title,
                onConfirm: () => TaskUtilities.ForgetSafely(
                    ExecuteUseRewardAsync(reward, Cts.Token),
                    Cts.Token,
                    "Rewards.UseConfirm"));
        }

        private async Task ExecuteUseRewardAsync(RewardSummary reward, CancellationToken token)
        {
            if (Disposed || token.IsCancellationRequested)
            {
                return;
            }

            string idempotencyUuid = System.Guid.NewGuid().ToString();

            Lifetime.IdempotentRequestRepository?.Upsert(new IdempotentRequestRecord
            {
                RequestUuid = idempotencyUuid,
                Operation = "use_reward",
                NormalizedPayloadJson = reward.RewardCode ?? string.Empty,
                State = "pending",
                CreatedUtc = System.DateTimeOffset.UtcNow.ToString("o")
            });

            var request = new UseRewardRequest
            {
                RewardCode = reward.RewardCode,
                RequestUuid = idempotencyUuid
            };

            AppResult<RewardSummary> result =
                await Lifetime.Gateway.UseRewardAsync(request, token).ConfigureAwait(true);

            if (Disposed || token.IsCancellationRequested)
            {
                return;
            }

            if (result.IsSuccess)
            {
                _shellRuntime?.ShowToast("Reward activated!", AppShellToastTone.Success);
                TaskUtilities.ForgetSafely(FetchAsync(token), token, "Rewards.Refresh");
            }
            else if (IsUnauthorized(result.Error))
            {
                HandleUnauthorized();
            }
            else if (IsOffline(result.Error))
            {
                _shellRuntime?.ShowToast("You're offline. Try again when connected.");
            }
            else
            {
                _shellRuntime?.ShowToast("Could not activate reward. Please try again.");
            }
        }

        private RewardSummary FindRewardByPresentationKey(string key)
        {
            if (string.IsNullOrEmpty(key) || _lastFetchedRewards == null)
            {
                return null;
            }

            for (int i = 0; i < _lastFetchedRewards.Count; i++)
            {
                if (_lastFetchedRewards[i].RewardCode == key)
                {
                    return _lastFetchedRewards[i];
                }
            }

            return null;
        }

        private void OnViewCertificatesRequested()
        {
            if (Disposed)
            {
                return;
            }

            TaskUtilities.ForgetSafely(
                Lifetime.Router?.PushAsync(
                    AppRouteId.Certificates,
                    AppRouteContext.Empty,
                    NavigationToken),
                NavigationToken,
                "Rewards.Certificates");
        }

        private void OnRetry()
        {
            if (Disposed)
            {
                return;
            }

            TaskUtilities.ForgetSafely(FetchAsync(RequestToken), RequestToken, "Rewards.Retry");
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
                "Rewards.Back");
        }
    }
}
