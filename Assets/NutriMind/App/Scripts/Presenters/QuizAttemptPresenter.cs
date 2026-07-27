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
        private PendingQuizSubmissionEnvelopeV2 _pendingEnvelope;
        private QuizResult _pendingServerResult;
        private bool _finalizationPending;
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
            if (string.IsNullOrWhiteSpace(ResolveCurrentStudentId()))
            {
                ShowLocalIntegrityError();
                return true;
            }

            IIdempotentRequestRepository repository = Lifetime.IdempotentRequestRepository;
            if (repository == null)
            {
                _view.SetPreviewState(QuizAttemptPreviewState.RecoverableError);
                return true;
            }

            PendingQuizSubmissionEnvelopeV2 retained =
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
                if (record == null
                    || !IsExactEnvelopeRecord(retained, record, ResolveCurrentStudentId()))
                {
                    ShowLocalIntegrityError();
                    return true;
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
                    ResolveCurrentStudentId(),
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
                PendingQuizSubmissionEnvelopeV2 envelope =
                    IdempotentMutationSerializers.DeserializeQuiz(
                        unresolved.Value.NormalizedPayloadJson);
                if (!IsExactEnvelopeRecord(
                        envelope,
                        unresolved.Value,
                        ResolveCurrentStudentId()))
                {
                    ShowLocalIntegrityError();
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
            PendingQuizSubmissionEnvelopeV2 envelope,
            IdempotentRequestRecord record)
        {
            _pendingClientUuid = envelope.Submission.ClientAttemptUuid;
            _retainedSubmission = envelope.Submission;
            _pendingRecord = record;
            _pendingEnvelope = envelope;
            _coordinator?.RetainPendingQuizSubmission(envelope);
            _view.SetPreviewState(QuizAttemptPreviewState.UncertainSubmission);
        }

        private bool IsEnvelopeForCurrentQuiz(PendingQuizSubmissionEnvelopeV2 envelope)
        {
            string currentStudentId = ResolveCurrentStudentId();
            return envelope != null
                && envelope.Submission != null
                && !string.IsNullOrWhiteSpace(envelope.Submission.ClientAttemptUuid)
                && !string.IsNullOrWhiteSpace(currentStudentId)
                && string.Equals(envelope.StudentId, currentStudentId, StringComparison.Ordinal)
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

            string currentStudentId = ResolveCurrentStudentId();
            if (string.IsNullOrWhiteSpace(currentStudentId))
            {
                ShowLocalIntegrityError();
                return;
            }

            _isSubmitting = true;
            _view.SetPreviewState(QuizAttemptPreviewState.Submitting);
            bool dispatched = false;
            try
            {
                IIdempotentRequestRepository repository = Lifetime.IdempotentRequestRepository;
                if (repository == null)
                {
                    _view.SetPreviewState(QuizAttemptPreviewState.RecoverableError);
                    return;
                }

                QuizAttemptSubmission submission = _retainedSubmission;
                PendingQuizSubmissionEnvelopeV2 envelope;
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
                    envelope = new PendingQuizSubmissionEnvelopeV2
                    {
                        StudentId = currentStudentId,
                        QuizId = _ctx.QuizId,
                        Submission = submission
                    };
                    string payload;
                    try
                    {
                        payload = IdempotentMutationSerializers.SerializeQuiz(envelope);
                    }
                    catch (Exception)
                    {
                        _view.SetPreviewState(QuizAttemptPreviewState.RecoverableError);
                        return;
                    }

                    AppResult<IdempotentRequestRecord> created =
                        IdempotentMutationTransitions.CreatePending(
                            submission.ClientAttemptUuid,
                            IdempotentOperations.QuizSubmit,
                            currentStudentId,
                            _ctx.QuizId,
                            payload,
                            GetUtcNow());
                    if (created.IsFailure)
                    {
                        _view.SetPreviewState(QuizAttemptPreviewState.RecoverableError);
                        return;
                    }

                    record = created.Value;
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
                    envelope = _pendingEnvelope;
                    if (record == null)
                    {
                        AppResult<IdempotentRequestRecord> unresolved =
                            repository.FindLatestUnresolved(
                                IdempotentOperations.QuizSubmit,
                                currentStudentId,
                                _ctx.QuizId);
                        if (unresolved.IsFailure)
                        {
                            _view.SetPreviewState(QuizAttemptPreviewState.RecoverableError);
                            return;
                        }

                        record = unresolved.Value;
                    }

                    if (envelope == null
                        || record == null
                        || !IsExactEnvelopeRecord(envelope, record, currentStudentId))
                    {
                        ShowLocalIntegrityError();
                        return;
                    }
                }

                if (token.IsCancellationRequested || !TryBeginDispatch(repository, record))
                {
                    _view.SetPreviewState(QuizAttemptPreviewState.RecoverableError);
                    return;
                }

                _pendingRecord = record;
                _pendingEnvelope = envelope;
                _coordinator?.RetainPendingQuizSubmission(envelope);

                var request = new SubmitQuizAttemptRequest
                {
                    QuizId = envelope.QuizId,
                    Submission = envelope.Submission
                };

                dispatched = true;
                AppResult<QuizResult> result =
                    await Lifetime.Gateway.SubmitQuizAttemptAsync(request, token)
                        .ConfigureAwait(true);

                if (token.IsCancellationRequested)
                {
                    HandlePostDispatchCancellation(repository, record, envelope);
                    return;
                }

                if (result.IsSuccess)
                {
                    if (IdempotentMutationTransitions.Transition(
                            repository,
                            record,
                            IdempotentRequestStates.Completed,
                            SerializeQuizResult(result.Value),
                            GetUtcNow()).IsFailure)
                    {
                        _pendingServerResult = result.Value;
                        _finalizationPending = true;
                        _view.SetPreviewState(QuizAttemptPreviewState.RecoverableError);
                        return;
                    }

                    CompleteSuccess(result.Value);
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
                        _view.SetPreviewState(QuizAttemptPreviewState.RecoverableError);
                        return;
                    }

                    _retainedSubmission = submission;
                    _pendingRecord = record;
                    _pendingEnvelope = envelope;
                    _coordinator?.RetainPendingQuizSubmission(envelope);
                    if (!Disposed)
                    {
                        _view.SetPreviewState(QuizAttemptPreviewState.UncertainSubmission);
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
                        _view.SetPreviewState(QuizAttemptPreviewState.RecoverableError);
                        return;
                    }

                    _retainedSubmission = null;
                    _pendingRecord = null;
                    _pendingEnvelope = null;
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
                    _isSubmitting = false;
                }
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
            if (Disposed)
            {
                return;
            }

            if (_finalizationPending)
            {
                RetryFinalization();
                return;
            }

            if (_isSubmitting || _retainedSubmission == null)
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

        private bool IsExactEnvelopeRecord(
            PendingQuizSubmissionEnvelopeV2 envelope,
            IdempotentRequestRecord record,
            string currentStudentId)
        {
            if (!IsEnvelopeForCurrentQuiz(envelope)
                || record == null
                || !string.Equals(record.RequestUuid, envelope.Submission.ClientAttemptUuid, StringComparison.Ordinal)
                || !string.Equals(record.StudentId, currentStudentId, StringComparison.Ordinal)
                || !string.Equals(record.EntityKey, _ctx.QuizId, StringComparison.Ordinal))
            {
                return false;
            }

            try
            {
                string payload = IdempotentMutationSerializers.SerializeQuiz(envelope);
                return string.Equals(record.NormalizedPayloadJson, payload, StringComparison.Ordinal)
                    && IdempotentMutationTransitions.ValidateImmutableIdentity(
                        record,
                        IdempotentOperations.QuizSubmit,
                        currentStudentId,
                        _ctx.QuizId,
                        payload).IsSuccess;
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
            PendingQuizSubmissionEnvelopeV2 envelope)
        {
            if (record == null || envelope == null)
            {
                _view.SetPreviewState(QuizAttemptPreviewState.RecoverableError);
                return;
            }

            _pendingRecord = record;
            _pendingEnvelope = envelope;
            _retainedSubmission = envelope.Submission;
            _coordinator?.RetainPendingQuizSubmission(envelope);
            if (record.State == IdempotentRequestStates.Sending
                && IdempotentMutationTransitions.Transition(
                    repository,
                    record,
                    IdempotentRequestStates.Uncertain,
                    null,
                    GetUtcNow()).IsFailure)
            {
                _view.SetPreviewState(QuizAttemptPreviewState.RecoverableError);
                return;
            }

            if (!Disposed)
            {
                _view.SetPreviewState(QuizAttemptPreviewState.UncertainSubmission);
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
                    SerializeQuizResult(_pendingServerResult),
                    GetUtcNow()).IsFailure)
            {
                _view.SetPreviewState(QuizAttemptPreviewState.RecoverableError);
                return;
            }

            _finalizationPending = false;
            _isSubmitting = false;
            CompleteSuccess(_pendingServerResult);
        }

        private void CompleteSuccess(QuizResult result)
        {
            _retainedSubmission = null;
            _pendingRecord = null;
            _pendingEnvelope = null;
            _pendingServerResult = null;
            _finalizationPending = false;
            _coordinator?.ReleaseAttemptSession();
            _coordinator?.ReleasePendingQuizSubmission();

            string attemptId = result?.AttemptId;
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

        private string ResolveCurrentStudentId()
        {
            return Lifetime.AuthenticatedStudentState?.Profile?.Id
                ?? Lifetime.CurrentProfile?.Id;
        }

        private void ShowLocalIntegrityError()
        {
            _view.SetPreviewState(QuizAttemptPreviewState.RecoverableError);
            if (!Disposed)
            {
                _shellRuntime?.ShowToast(
                    "This submission could not be safely recovered. Please refresh and try again.",
                    AppShellToastTone.Danger);
            }
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
