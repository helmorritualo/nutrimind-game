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
        private PendingRewardUseEnvelopeV2 _pendingEnvelope;
        private IdempotentRequestRecord _pendingRecord;
        private RewardSummary _pendingServerResult;
        private bool _finalizationPending;

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
                PersistRewardsCache(items);

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
            else if (IsOffline(result.Error))
            {
                IReadOnlyList<RewardSummary> cached = LoadRewardsCache();
                if (cached != null)
                {
                    _lastFetchedRewards = cached;
                    if (cached.Count == 0)
                    {
                        _view.SetPreviewState(RewardsPreviewState.Empty);
                    }
                    else
                    {
                        _view.SetItems(AppViewMappers.MapRewardSummaries(cached));
                        _view.SetPreviewState(RewardsPreviewState.OfflineCached);
                    }
                }
                else
                {
                    _lastFetchedRewards = Array.Empty<RewardSummary>();
                    _view.SetItems(Array.Empty<RewardsPreviewItem>());
                    _view.SetPreviewState(RewardsPreviewState.OfflineUnavailable);
                }
            }
            else
            {
                _view.SetPreviewState(AppViewMappers.ErrorToRewardsPreviewState(result.Error));
            }
        }

        private void PersistRewardsCache(IReadOnlyList<RewardSummary> items)
        {
            string studentId = Lifetime.AuthenticatedStudentState?.Profile?.Id
                ?? Lifetime.CurrentProfile?.Id;
            if (string.IsNullOrWhiteSpace(studentId) || Lifetime.ResourceCacheRepository == null)
            {
                return;
            }

            LearnerRouteCache.SaveRewards(
                Lifetime.ResourceCacheRepository,
                studentId,
                items ?? Array.Empty<RewardSummary>(),
                DateTimeOffset.UtcNow.ToString("o"));
        }

        private IReadOnlyList<RewardSummary> LoadRewardsCache()
        {
            string studentId = Lifetime.AuthenticatedStudentState?.Profile?.Id
                ?? Lifetime.CurrentProfile?.Id;
            if (string.IsNullOrWhiteSpace(studentId) || Lifetime.ResourceCacheRepository == null)
            {
                return null;
            }

            AppResult<IReadOnlyList<RewardSummary>> cached = LearnerRouteCache.LoadRewards(
                Lifetime.ResourceCacheRepository,
                studentId);
            return cached.IsSuccess ? cached.Value : null;
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
                || reward == null
                || string.IsNullOrWhiteSpace(reward.RewardCode))
            {
                return;
            }

            string currentStudentId = ResolveCurrentStudentId();
            if (string.IsNullOrWhiteSpace(currentStudentId))
            {
                ShowLocalIntegrityFailure();
                return;
            }

            _useRewardInFlight = true;
            _view.SetUseRewardEnabled(false);
            bool dispatched = false;
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
                        currentStudentId,
                        reward.RewardCode);
                if (unresolved.IsFailure)
                {
                    ShowLocalPersistenceFailure();
                    return;
                }

                IdempotentRequestRecord record = unresolved.Value;
                PendingRewardUseEnvelopeV2 envelope;
                if (record != null)
                {
                    if (!TryRestoreRewardEnvelope(
                            record,
                            currentStudentId,
                            reward.RewardCode,
                            out envelope))
                    {
                        ShowLocalIntegrityFailure();
                        return;
                    }
                }
                else
                {
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }

                    envelope = new PendingRewardUseEnvelopeV2
                    {
                        StudentId = currentStudentId,
                        RewardCode = reward.RewardCode,
                        RequestUuid = Guid.NewGuid().ToString()
                    };
                    string payload;
                    try
                    {
                        payload = IdempotentMutationSerializers.SerializeReward(envelope);
                    }
                    catch (Exception)
                    {
                        ShowLocalIntegrityFailure();
                        return;
                    }

                    AppResult<IdempotentRequestRecord> created =
                        IdempotentMutationTransitions.CreatePending(
                            envelope.RequestUuid,
                            IdempotentOperations.UseReward,
                            currentStudentId,
                            reward.RewardCode,
                            payload,
                            GetUtcNow());
                    if (created.IsFailure)
                    {
                        ShowLocalIntegrityFailure();
                        return;
                    }

                    record = created.Value;
                    if (repository.Upsert(record).IsFailure)
                    {
                        ShowLocalPersistenceFailure();
                        return;
                    }
                }

                if (token.IsCancellationRequested)
                {
                    return;
                }

                if (!TryBeginDispatch(repository, record))
                {
                    ShowLocalPersistenceFailure();
                    return;
                }

                _pendingEnvelope = envelope;
                _pendingRecord = record;
                var request = new UseRewardRequest
                {
                    RewardCode = envelope.RewardCode,
                    RequestUuid = envelope.RequestUuid
                };

                dispatched = true;
                AppResult<RewardSummary> result =
                    await Lifetime.Gateway.UseRewardAsync(request, token).ConfigureAwait(true);

                if (token.IsCancellationRequested)
                {
                    HandlePostDispatchCancellation(repository, record, envelope);
                    return;
                }

                if (result.IsSuccess)
                {
                    string resultJson = SerializeRewardResult(result.Value);
                    if (IdempotentMutationTransitions.Transition(
                            repository,
                            record,
                            IdempotentRequestStates.Completed,
                            resultJson,
                            GetUtcNow()).IsFailure)
                    {
                        _pendingServerResult = result.Value;
                        _finalizationPending = true;
                        ShowLocalFinalizationFailure();
                        return;
                    }

                    ClearPendingMutation();
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
                    if (IdempotentMutationTransitions.Transition(
                            repository,
                            record,
                            IdempotentRequestStates.Uncertain,
                            null,
                            GetUtcNow()).IsFailure)
                    {
                        ShowLocalPersistenceFailure();
                        return;
                    }

                    if (!Disposed)
                    {
                        _shellRuntime?.ShowToast(
                            "We could not confirm the result. Retry safely.",
                            AppShellToastTone.Warning);
                    }
                }
                else
                {
                    if (IdempotentMutationTransitions.Transition(
                            repository,
                            record,
                            IdempotentRequestStates.Rejected,
                            SerializeErrorResult(result.Error),
                            GetUtcNow()).IsFailure)
                    {
                        ShowLocalPersistenceFailure();
                        return;
                    }

                    ClearPendingMutation();
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
            catch (OperationCanceledException) when (dispatched)
            {
                HandlePostDispatchCancellation(
                    Lifetime.IdempotentRequestRepository,
                    _pendingRecord,
                    _pendingEnvelope);
            }
            finally
            {
                if (!_finalizationPending)
                {
                    _useRewardInFlight = false;
                    if (!Disposed)
                    {
                        _view.SetUseRewardEnabled(true);
                    }
                }
            }
        }

        private bool TryRestoreRewardEnvelope(
            IdempotentRequestRecord record,
            string currentStudentId,
            string rewardCode,
            out PendingRewardUseEnvelopeV2 envelope)
        {
            envelope = null;
            try
            {
                PendingRewardUseEnvelopeV2 restored =
                    IdempotentMutationSerializers.DeserializeReward(record.NormalizedPayloadJson);
                if (restored == null
                    || !string.Equals(restored.RequestUuid, record.RequestUuid, StringComparison.Ordinal)
                    || !string.Equals(restored.StudentId, currentStudentId, StringComparison.Ordinal)
                    || !string.Equals(restored.RewardCode, rewardCode, StringComparison.Ordinal)
                    || !string.Equals(record.StudentId, currentStudentId, StringComparison.Ordinal)
                    || !string.Equals(record.EntityKey, rewardCode, StringComparison.Ordinal)
                    || !string.Equals(
                        record.NormalizedPayloadJson,
                        IdempotentMutationSerializers.SerializeReward(restored),
                        StringComparison.Ordinal))
                {
                    return false;
                }

                if (IdempotentMutationTransitions.ValidateImmutableIdentity(
                        record,
                        IdempotentOperations.UseReward,
                        currentStudentId,
                        rewardCode,
                        record.NormalizedPayloadJson).IsFailure)
                {
                    return false;
                }

                envelope = restored;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool TryBeginDispatch(
            IIdempotentRequestRepository repository,
            IdempotentRequestRecord record)
        {
            if (record.State == IdempotentRequestStates.Sending
                && IdempotentMutationTransitions.Transition(
                    repository,
                    record,
                    IdempotentRequestStates.Uncertain,
                    null,
                    GetUtcNow()).IsFailure)
            {
                return false;
            }

            return IdempotentMutationTransitions.Transition(
                    repository,
                    record,
                    IdempotentRequestStates.Sending,
                    null,
                    GetUtcNow())
                .IsSuccess;
        }

        private void HandlePostDispatchCancellation(
            IIdempotentRequestRepository repository,
            IdempotentRequestRecord record,
            PendingRewardUseEnvelopeV2 envelope)
        {
            if (record == null || envelope == null)
            {
                ShowLocalPersistenceFailure();
                return;
            }

            _pendingRecord = record;
            _pendingEnvelope = envelope;
            if (record.State == IdempotentRequestStates.Sending
                && IdempotentMutationTransitions.Transition(
                    repository,
                    record,
                    IdempotentRequestStates.Uncertain,
                    null,
                    GetUtcNow()).IsFailure)
            {
                ShowLocalPersistenceFailure();
                return;
            }

            if (!Disposed)
            {
                _shellRuntime?.ShowToast(
                    "We could not confirm the result. Retry safely.",
                    AppShellToastTone.Warning);
            }
        }

        private void RetryFinalization()
        {
            if (!_finalizationPending || _pendingRecord == null)
            {
                return;
            }

            if (IdempotentMutationTransitions.Transition(
                    Lifetime.IdempotentRequestRepository,
                    _pendingRecord,
                    IdempotentRequestStates.Completed,
                    SerializeRewardResult(_pendingServerResult),
                    GetUtcNow()).IsFailure)
            {
                ShowLocalFinalizationFailure();
                return;
            }

            _finalizationPending = false;
            ClearPendingMutation();
            _useRewardInFlight = false;
            if (!Disposed)
            {
                _view.SetUseRewardEnabled(true);
                _shellRuntime?.ShowToast("Reward activated!", AppShellToastTone.Success);
                TaskUtilities.ForgetSafely(FetchAsync(RequestToken), RequestToken, "Rewards.FinalizeRefresh");
            }
        }

        private string ResolveCurrentStudentId()
        {
            return Lifetime.AuthenticatedStudentState?.Profile?.Id
                ?? Lifetime.CurrentProfile?.Id;
        }

        private void ClearPendingMutation()
        {
            _pendingEnvelope = null;
            _pendingRecord = null;
            _pendingServerResult = null;
            _finalizationPending = false;
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

        private void ShowLocalIntegrityFailure()
        {
            if (!Disposed)
            {
                _shellRuntime?.ShowToast(
                    "This request could not be safely recovered. Please refresh and try again.",
                    AppShellToastTone.Danger);
            }
        }

        private void ShowLocalFinalizationFailure()
        {
            if (!Disposed)
            {
                _shellRuntime?.ShowToast(
                    "The reward was confirmed, but local storage could not finalize it. Retry safely.",
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

            if (_finalizationPending)
            {
                RetryFinalization();
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
