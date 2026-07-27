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
    /// Runtime presenter for the Rewards route.
    /// Fetches available rewards from the server and maps them to <see cref="RewardsPreviewItem"/>.
    /// UseReward requests persist a versioned envelope and reuse unresolved UUIDs so
    /// network-uncertain requests can be retried with the identical payload.
    /// </summary>
    public sealed class RewardsPresenter : RoutePresenterBase
    {
        private readonly RewardsPanelView _view;
        private readonly AppShellRuntimeController _shellRuntime;

        // Keeps the last fetched list so we can look up RewardCode by PresentationKey.
        private IReadOnlyList<RewardSummary> _lastFetchedRewards;
        private bool _useRewardInFlight;

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

            TaskUtilities.ForgetSafely(FetchAsync(RequestToken), RequestToken, "Rewards.Load");
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
            if (Disposed || _useRewardInFlight)
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
                    ExecuteUseRewardAsync(reward, RequestToken),
                    RequestToken,
                    "Rewards.UseConfirm"));
        }

        private async Task ExecuteUseRewardAsync(RewardSummary reward, CancellationToken token)
        {
            if (Disposed
                || _useRewardInFlight
                || token.IsCancellationRequested
                || reward == null
                || string.IsNullOrWhiteSpace(reward.RewardCode))
            {
                return;
            }

            _useRewardInFlight = true;
            _view.SetUseRewardEnabled(false);
            try
            {
                IIdempotentRequestRepository repository = Lifetime.IdempotentRequestRepository;
                if (repository == null)
                {
                    ShowLocalPersistenceFailure();
                    return;
                }

                AppResult<IdempotentRequestRecord> unresolved =
                    repository.FindLatestUnresolved(
                        IdempotentOperations.UseReward,
                        reward.RewardCode);
                if (unresolved.IsFailure)
                {
                    ShowLocalPersistenceFailure();
                    return;
                }

                IdempotentRequestRecord record = unresolved.Value;
                PendingRewardUseEnvelopeV1 envelope;
                if (record != null)
                {
                    envelope = RestoreRewardEnvelope(record, reward.RewardCode);
                    record.NormalizedPayloadJson =
                        IdempotentMutationSerializers.SerializeReward(envelope);
                }
                else
                {
                    string requestUuid = Guid.NewGuid().ToString();
                    envelope = new PendingRewardUseEnvelopeV1
                    {
                        RewardCode = reward.RewardCode,
                        RequestUuid = requestUuid
                    };
                    string now = GetUtcNow();
                    record = new IdempotentRequestRecord
                    {
                        RequestUuid = requestUuid,
                        Operation = IdempotentOperations.UseReward,
                        NormalizedPayloadJson =
                            IdempotentMutationSerializers.SerializeReward(envelope),
                        State = IdempotentRequestStates.Pending,
                        CreatedUtc = now,
                        UpdatedUtc = now
                    };
                    if (repository.Upsert(record).IsFailure)
                    {
                        ShowLocalPersistenceFailure();
                        return;
                    }
                }

                if (SetRequestState(
                        repository,
                        record,
                        IdempotentRequestStates.Sending,
                        null).IsFailure)
                {
                    ShowLocalPersistenceFailure();
                    return;
                }

                var request = new UseRewardRequest
                {
                    RewardCode = envelope.RewardCode,
                    RequestUuid = envelope.RequestUuid
                };

                AppResult<RewardSummary> result =
                    await Lifetime.Gateway.UseRewardAsync(request, token).ConfigureAwait(true);

                if (token.IsCancellationRequested)
                {
                    return;
                }

                if (result.IsSuccess)
                {
                    SetRequestState(
                        repository,
                        record,
                        IdempotentRequestStates.Completed,
                        SerializeRewardResult(result.Value));
                    if (!Disposed)
                    {
                        _shellRuntime?.ShowToast("Reward activated!", AppShellToastTone.Success);
                        TaskUtilities.ForgetSafely(
                            FetchAsync(RequestToken),
                            RequestToken,
                            "Rewards.Refresh");
                    }
                }
                else if (IsOffline(result.Error))
                {
                    SetRequestState(
                        repository,
                        record,
                        IdempotentRequestStates.Uncertain,
                        null);
                    if (!Disposed)
                    {
                        _shellRuntime?.ShowToast(
                            "We could not confirm the result. Retry safely.",
                            AppShellToastTone.Warning);
                    }
                }
                else
                {
                    SetRequestState(
                        repository,
                        record,
                        IdempotentRequestStates.Rejected,
                        SerializeErrorResult(result.Error));
                    if (IsUnauthorized(result.Error))
                    {
                        HandleUnauthorized();
                    }
                    else if (!Disposed)
                    {
                        _shellRuntime?.ShowToast(
                            "Could not activate reward. Please try again.",
                            AppShellToastTone.Danger);
                    }
                }
            }
            finally
            {
                _useRewardInFlight = false;
                if (!Disposed)
                {
                    _view.SetUseRewardEnabled(true);
                }
            }
        }

        private PendingRewardUseEnvelopeV1 RestoreRewardEnvelope(
            IdempotentRequestRecord record,
            string rewardCode)
        {
            try
            {
                PendingRewardUseEnvelopeV1 restored =
                    IdempotentMutationSerializers.DeserializeReward(record.NormalizedPayloadJson);
                if (string.Equals(restored.RewardCode, rewardCode, StringComparison.Ordinal)
                    && string.Equals(restored.RequestUuid, record.RequestUuid, StringComparison.Ordinal))
                {
                    return restored;
                }
            }
            catch (Exception)
            {
                // Legacy records stored only reward_code; rebuild the versioned envelope.
            }

            return new PendingRewardUseEnvelopeV1
            {
                RewardCode = rewardCode,
                RequestUuid = record.RequestUuid
            };
        }

        private AppResult SetRequestState(
            IIdempotentRequestRepository repository,
            IdempotentRequestRecord record,
            string state,
            string resultJson)
        {
            record.State = state;
            record.ResultJson = resultJson;
            record.UpdatedUtc = GetUtcNow();
            return repository.Upsert(record);
        }

        private string GetUtcNow()
        {
            DateTimeOffset now = Lifetime.Clock != null
                ? Lifetime.Clock.UtcNow
                : DateTimeOffset.UtcNow;
            return now.ToUniversalTime().ToString("o");
        }

        private void ShowLocalPersistenceFailure()
        {
            if (!Disposed)
            {
                _shellRuntime?.ShowToast(
                    "Could not save this request. Please try again.",
                    AppShellToastTone.Danger);
            }
        }

        private static string SerializeRewardResult(RewardSummary reward)
        {
            return JsonUtility.ToJson(new RewardUseResultRecord
            {
                RewardCode = reward?.RewardCode,
                Status = reward?.Status
            });
        }

        private static string SerializeErrorResult(AppError error)
        {
            return JsonUtility.ToJson(new MutationErrorRecord
            {
                Code = error?.Code,
                HttpStatus = error?.HttpStatus ?? 0
            });
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
                    AppRouteContext.ForCertificate(null, AppRouteOrigin.Rewards),
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

        [Serializable]
        private sealed class RewardUseResultRecord
        {
            public string RewardCode;
            public string Status;
        }

        [Serializable]
        private sealed class MutationErrorRecord
        {
            public string Code;
            public int HttpStatus;
        }
    }
}
