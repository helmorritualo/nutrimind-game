using System;
using System.Threading;
using System.Threading.Tasks;
using NutriMind.App.Composition;
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
    /// Runtime presenter for the Quiz Attempt route.
    /// Loads quiz detail, binds the view, then intercepts the view's SubmitRequested event
    /// to map preview answers to production API format.
    /// A stable clientAttemptUuid and frozen submission are persisted before sending;
    /// unresolved submissions are recovered and retried byte-for-byte.
    /// Quiz results are ALWAYS server-provided; scores are never recalculated locally.
    /// </summary>
    public sealed class QuizAttemptPresenter : RoutePresenterBase
    {
        private readonly QuizAttemptPanelView _view;
        private readonly AppRouteContext _ctx;
        private readonly AppShellRuntimeController _shellRuntime;
        private readonly QuizPortalScreenCoordinator _coordinator;

        private string _resolvedSubjectId;
        private string _resolvedTermId;
        private string _pendingClientUuid;
        private QuizAttemptSubmission _retainedSubmission;
        private IdempotentRequestRecord _pendingRecord;
        private bool _isSubmitting;

        public QuizAttemptPresenter(
            AppLifetime lifetime,
            QuizAttemptPanelView view,
            AppRouteContext ctx,
            AppShellRuntimeController shellRuntime,
            QuizPortalScreenCoordinator coordinator)
            : base(lifetime)
        {
            _view = view;
            _ctx = ctx ?? AppRouteContext.Empty;
            _shellRuntime = shellRuntime;
            _coordinator = coordinator;
            _resolvedSubjectId = _ctx.SubjectId;
            _resolvedTermId = _ctx.TermId;

            _view.SubmitRequested += OnSubmitRequested;
            _view.ExitRequested += OnExitRequested;
            _view.CheckSubmissionStatusRequested += OnRetryPendingSubmission;
            _view.BackToQuizPortalRequested += OnExitRequested;
            _view.ReturnToReviewRequested += OnReturnToReviewRequested;
        }

        public void LoadAsync()
        {
            if (Disposed)
            {
                return;
            }

            if (RestorePendingSubmission())
            {
                return;
            }

            TaskUtilities.ForgetSafely(
                FetchAndBindAsync(RequestToken),
                RequestToken,
                "QuizAttempt.Load");
        }

        protected override void OnDispose()
        {
            _view.SubmitRequested -= OnSubmitRequested;
            _view.ExitRequested -= OnExitRequested;
            _view.CheckSubmissionStatusRequested -= OnRetryPendingSubmission;
            _view.BackToQuizPortalRequested -= OnExitRequested;
            _view.ReturnToReviewRequested -= OnReturnToReviewRequested;
        }

        private async Task FetchAndBindAsync(CancellationToken token)
        {
            _view.SetPreviewState(QuizAttemptPreviewState.RecoverableError);

            var request = new QuizIdRequest { QuizId = _ctx.QuizId };

            AppResult<QuizDetail> result =
                await Lifetime.Gateway.GetQuizDetailAsync(request, token).ConfigureAwait(true);

            if (Disposed || token.IsCancellationRequested)
            {
                return;
            }

            if (result.IsSuccess)
            {
                QuizListPreviewItem summary = AppViewMappers.MapQuizSummaryToPreviewItem(new QuizSummary
                {
                    Id = result.Value.Id,
                    Title = result.Value.Title,
                    SubjectId = result.Value.SubjectId,
                    TermId = result.Value.TermId,
                    Status = result.Value.Status,
                    MaxAttempts = result.Value.MaxAttempts,
                    AttemptsUsed = result.Value.AttemptsUsed,
                    OpensAt = result.Value.OpensAt,
                    ClosesAt = result.Value.ClosesAt,
                    ResultVisibility = result.Value.ResultVisibility
                });

                QuizDetailPreviewContent detail = AppViewMappers.MapQuizDetail(result.Value);

                _resolvedSubjectId = result.Value.SubjectId ?? _ctx.SubjectId;
                _resolvedTermId = result.Value.TermId ?? _ctx.TermId;
                _pendingClientUuid = null;
                _retainedSubmission = null;
                _pendingRecord = null;
                _isSubmitting = false;

                _view.SetQuizContext(summary, detail);
            }
            else if (IsUnauthorized(result.Error))
            {
                HandleUnauthorized();
            }
            else
            {
                _view.SetPreviewState(QuizAttemptPreviewState.RecoverableError);
            }
        }

        private bool RestorePendingSubmission()
        {
            IIdempotentRequestRepository repository = Lifetime.IdempotentRequestRepository;
            if (repository == null)
            {
                _view.SetPreviewState(QuizAttemptPreviewState.RecoverableError);
                return true;
            }

            PendingQuizSubmissionEnvelopeV1 retained =
                _coordinator?.RetainedPendingQuizSubmission;
            if (IsEnvelopeForCurrentQuiz(retained))
            {
                AppResult<IdempotentRequestRecord> stored =
                    repository.Get(retained.Submission.ClientAttemptUuid);
                if (stored.IsFailure)
                {
                    _view.SetPreviewState(QuizAttemptPreviewState.RecoverableError);
                    return true;
                }

                IdempotentRequestRecord record = stored.Value;
                if (record == null)
                {
                    string now = GetUtcNow();
                    record = new IdempotentRequestRecord
                    {
                        RequestUuid = retained.Submission.ClientAttemptUuid,
                        Operation = IdempotentOperations.QuizSubmit,
                        NormalizedPayloadJson =
                            IdempotentMutationSerializers.SerializeQuiz(retained),
                        State = IdempotentRequestStates.Uncertain,
                        CreatedUtc = now,
                        UpdatedUtc = now
                    };
                    if (repository.Upsert(record).IsFailure)
                    {
                        _view.SetPreviewState(QuizAttemptPreviewState.RecoverableError);
                        return true;
                    }
                }

                if (IdempotentRequestStates.IsUnresolved(record.State))
                {
                    ApplyPendingEnvelope(retained, record);
                    return true;
                }

                _coordinator?.ReleasePendingQuizSubmission();
            }

            AppResult<IdempotentRequestRecord> unresolved =
                repository.FindLatestUnresolved(
                    IdempotentOperations.QuizSubmit,
                    _ctx.QuizId);
            if (unresolved.IsFailure)
            {
                _view.SetPreviewState(QuizAttemptPreviewState.RecoverableError);
                return true;
            }

            if (unresolved.Value == null)
            {
                return false;
            }

            try
            {
                PendingQuizSubmissionEnvelopeV1 envelope =
                    IdempotentMutationSerializers.DeserializeQuiz(
                        unresolved.Value.NormalizedPayloadJson);
                if (!IsEnvelopeForCurrentQuiz(envelope)
                    || !string.Equals(
                        envelope.Submission.ClientAttemptUuid,
                        unresolved.Value.RequestUuid,
                        StringComparison.Ordinal))
                {
                    _view.SetPreviewState(QuizAttemptPreviewState.RecoverableError);
                    return true;
                }

                ApplyPendingEnvelope(envelope, unresolved.Value);
                return true;
            }
            catch (Exception exception)
            {
                NutriMindLog.RuntimeWarning(
                    "QuizAttemptPresenter could not restore pending submission: "
                    + exception.GetType().Name);
                _view.SetPreviewState(QuizAttemptPreviewState.RecoverableError);
                return true;
            }
        }

        private void ApplyPendingEnvelope(
            PendingQuizSubmissionEnvelopeV1 envelope,
            IdempotentRequestRecord record)
        {
            _pendingClientUuid = envelope.Submission.ClientAttemptUuid;
            _retainedSubmission = envelope.Submission;
            _pendingRecord = record;
            _coordinator?.RetainPendingQuizSubmission(envelope);
            _view.SetPreviewState(QuizAttemptPreviewState.UncertainSubmission);
        }

        private bool IsEnvelopeForCurrentQuiz(PendingQuizSubmissionEnvelopeV1 envelope)
        {
            return envelope != null
                && envelope.Submission != null
                && !string.IsNullOrWhiteSpace(envelope.Submission.ClientAttemptUuid)
                && string.Equals(envelope.QuizId, _ctx.QuizId, StringComparison.Ordinal);
        }

        private void OnSubmitRequested(QuizAttemptPreviewSubmission previewSubmission)
        {
            if (Disposed || _isSubmitting)
            {
                return;
            }

            _shellRuntime?.RequestSubmitQuiz(() =>
            {
                TaskUtilities.ForgetSafely(
                    SubmitAsync(previewSubmission, RequestToken),
                    RequestToken,
                    "QuizAttempt.Submit");
            });
        }

        private async Task SubmitAsync(QuizAttemptPreviewSubmission previewSubmission, CancellationToken token)
        {
            if (Disposed || _isSubmitting || token.IsCancellationRequested)
            {
                return;
            }

            _isSubmitting = true;
            _view.SetPreviewState(QuizAttemptPreviewState.Submitting);
            try
            {
                IIdempotentRequestRepository repository = Lifetime.IdempotentRequestRepository;
                if (repository == null)
                {
                    _view.SetPreviewState(QuizAttemptPreviewState.RecoverableError);
                    return;
                }

                QuizAttemptSubmission submission = _retainedSubmission;
                PendingQuizSubmissionEnvelopeV1 envelope;
                IdempotentRequestRecord record = _pendingRecord;

                if (submission == null)
                {
                    if (previewSubmission == null
                        || !string.Equals(
                            previewSubmission.QuizId,
                            _ctx.QuizId,
                            StringComparison.Ordinal))
                    {
                        _view.SetPreviewState(QuizAttemptPreviewState.RecoverableError);
                        return;
                    }

                    _pendingClientUuid = Guid.NewGuid().ToString();
                    submission = AppViewMappers.MapPreviewSubmission(
                        _pendingClientUuid,
                        previewSubmission);
                    envelope = new PendingQuizSubmissionEnvelopeV1
                    {
                        QuizId = _ctx.QuizId,
                        Submission = submission
                    };
                    string now = GetUtcNow();
                    record = new IdempotentRequestRecord
                    {
                        RequestUuid = submission.ClientAttemptUuid,
                        Operation = IdempotentOperations.QuizSubmit,
                        NormalizedPayloadJson =
                            IdempotentMutationSerializers.SerializeQuiz(envelope),
                        State = IdempotentRequestStates.Pending,
                        CreatedUtc = now,
                        UpdatedUtc = now
                    };
                    if (repository.Upsert(record).IsFailure)
                    {
                        _view.SetPreviewState(QuizAttemptPreviewState.RecoverableError);
                        return;
                    }

                    _retainedSubmission = submission;
                    _pendingRecord = record;
                    _coordinator?.RetainPendingQuizSubmission(envelope);
                }
                else
                {
                    envelope = new PendingQuizSubmissionEnvelopeV1
                    {
                        QuizId = _ctx.QuizId,
                        Submission = submission
                    };
                    if (record == null)
                    {
                        AppResult<IdempotentRequestRecord> stored =
                            repository.Get(submission.ClientAttemptUuid);
                        record = stored.IsSuccess ? stored.Value : null;
                    }

                    if (record == null)
                    {
                        string now = GetUtcNow();
                        record = new IdempotentRequestRecord
                        {
                            RequestUuid = submission.ClientAttemptUuid,
                            Operation = IdempotentOperations.QuizSubmit,
                            NormalizedPayloadJson =
                                IdempotentMutationSerializers.SerializeQuiz(envelope),
                            State = IdempotentRequestStates.Pending,
                            CreatedUtc = now,
                            UpdatedUtc = now
                        };
                        if (repository.Upsert(record).IsFailure)
                        {
                            _view.SetPreviewState(QuizAttemptPreviewState.RecoverableError);
                            return;
                        }
                    }
                }

                if (SetRequestState(
                        repository,
                        record,
                        IdempotentRequestStates.Sending,
                        null).IsFailure)
                {
                    _view.SetPreviewState(QuizAttemptPreviewState.RecoverableError);
                    return;
                }

                _pendingRecord = record;
                _coordinator?.RetainPendingQuizSubmission(envelope);

                var request = new SubmitQuizAttemptRequest
                {
                    QuizId = envelope.QuizId,
                    Submission = envelope.Submission
                };

                AppResult<QuizResult> result =
                    await Lifetime.Gateway.SubmitQuizAttemptAsync(request, token)
                        .ConfigureAwait(true);

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
                        SerializeQuizResult(result.Value));
                    _retainedSubmission = null;
                    _pendingRecord = null;
                    _coordinator?.ReleaseAttemptSession();
                    _coordinator?.ReleasePendingQuizSubmission();

                    string attemptId = result.Value?.AttemptId;
                    AppRouteContext resultCtx = AppRouteContext.ForQuizResult(
                            attemptId,
                            _ctx.QuizId,
                            _resolvedSubjectId,
                            _resolvedTermId)
                        .WithReturnToMainOnQuizBack(_ctx.ReturnToMainOnQuizBack);
                    TaskUtilities.ForgetSafely(
                        Lifetime.Router?.NavigateAsync(
                            AppRouteId.QuizResult,
                            resultCtx,
                            NavigationToken),
                        NavigationToken,
                        "QuizAttempt.ToResult");
                }
                else if (IsOffline(result.Error))
                {
                    SetRequestState(
                        repository,
                        record,
                        IdempotentRequestStates.Uncertain,
                        null);
                    _retainedSubmission = submission;
                    _pendingRecord = record;
                    _coordinator?.RetainPendingQuizSubmission(envelope);
                    if (!Disposed)
                    {
                        _view.SetPreviewState(QuizAttemptPreviewState.UncertainSubmission);
                    }
                }
                else
                {
                    SetRequestState(
                        repository,
                        record,
                        IdempotentRequestStates.Rejected,
                        SerializeErrorResult(result.Error));
                    _retainedSubmission = null;
                    _pendingRecord = null;
                    _coordinator?.ReleaseAttemptSession();
                    _coordinator?.ReleasePendingQuizSubmission();
                    if (IsUnauthorized(result.Error))
                    {
                        HandleUnauthorized();
                    }
                    else if (!Disposed)
                    {
                        _view.SetPreviewState(QuizAttemptPreviewState.RecoverableError);
                    }
                }
            }
            finally
            {
                _isSubmitting = false;
            }
        }

        private void OnExitRequested()
        {
            if (Disposed)
            {
                return;
            }

            if (_retainedSubmission != null)
            {
                var configuration = new SystemDialogConfiguration(
                    title: "Submission not confirmed",
                    message: "The server may already have received this quiz submission.",
                    primaryActionLabel: "Retry Safely",
                    secondaryActionLabel: "Leave Safely",
                    detail: "Retry uses the same request ID and answers. Leaving keeps the pending submission for recovery.",
                    eyebrow: "Quiz Portal",
                    iconClass: "ds-icon--warning",
                    tone: SystemDialogTone.Warning,
                    allowDismiss: false,
                    dismissOnBackdrop: false);
                _shellRuntime?.ModalHost?.ShowSystem(
                    configuration,
                    onPrimary: OnRetryPendingSubmission,
                    onSecondary: NavigateBackSafely);
                return;
            }

            _shellRuntime?.RequestExitQuiz(NavigateBackSafely);
        }

        private void OnRetryPendingSubmission()
        {
            if (Disposed || _isSubmitting || _retainedSubmission == null)
            {
                return;
            }

            TaskUtilities.ForgetSafely(
                SubmitAsync(null, RequestToken),
                RequestToken,
                "QuizAttempt.RetryPending");
        }

        private void NavigateBackSafely()
        {
            if (Disposed)
            {
                return;
            }

            TaskUtilities.ForgetSafely(
                Lifetime.Router?.BackAsync(NavigationToken),
                NavigationToken,
                "QuizAttempt.Exit");
        }

        private void OnReturnToReviewRequested()
        {
            if (Disposed || _isSubmitting || _retainedSubmission != null)
            {
                return;
            }

            _pendingClientUuid = null;
            _view.SetPreviewState(QuizAttemptPreviewState.Content);
            _view.ShowReview();
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

        private static string SerializeQuizResult(QuizResult result)
        {
            return JsonUtility.ToJson(new QuizMutationResultRecord
            {
                AttemptId = result?.AttemptId,
                QuizId = result?.QuizId,
                ClientAttemptUuid = result?.ClientAttemptUuid,
                Status = result?.Status
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

        [Serializable]
        private sealed class QuizMutationResultRecord
        {
            public string AttemptId;
            public string QuizId;
            public string ClientAttemptUuid;
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
