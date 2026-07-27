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

namespace NutriMind.App.Presentation
{
    /// <summary>
    /// Runtime presenter for the Quiz Attempt route.
    /// Loads quiz detail, binds the view, then intercepts the view's SubmitRequested event
    /// to map preview answers to production API format.
    /// A stable clientAttemptUuid is generated once and persisted via IdempotentRequestRepository;
    /// on timeout the submission is retained and reused on the next SubmitRequested.
    /// Quiz results are ALWAYS server-provided; scores are never recalculated locally.
    /// </summary>
    public sealed class QuizAttemptPresenter : RoutePresenterBase
    {
        private readonly QuizAttemptPanelView _view;
        private readonly AppRouteContext _ctx;
        private readonly AppShellRuntimeController _shellRuntime;
        private readonly QuizPortalScreenCoordinator _coordinator;

        private string _pendingClientUuid;
        private QuizAttemptSubmission _retainedSubmission;
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
            _ctx = ctx;
            _shellRuntime = shellRuntime;
            _coordinator = coordinator;

            _view.SubmitRequested += OnSubmitRequested;
            _view.ExitRequested += OnExitRequested;
        }

        public void LoadAsync()
        {
            if (Disposed)
            {
                return;
            }

            // If an uncertain session exists for this quiz, use its retained submission.
            QuizAttemptSession existing = _coordinator?.RetainedAttemptSession;
            if (existing != null
                && existing.QuizId == _ctx.QuizId
                && !existing.IsSubmitted
                && existing.HasUncertainSubmit)
            {
                _pendingClientUuid = existing.ClientAttemptUuid;
                _retainedSubmission = existing.BuildSubmission();
                SetViewStateFromRetainedSession();
                return;
            }

            FetchAndBindAsync(Cts.Token);
        }

        protected override void OnDispose()
        {
            _view.SubmitRequested -= OnSubmitRequested;
            _view.ExitRequested -= OnExitRequested;
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

                _pendingClientUuid = Guid.NewGuid().ToString();
                _retainedSubmission = null;
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

        private void SetViewStateFromRetainedSession()
        {
            _view.SetPreviewState(QuizAttemptPreviewState.UncertainSubmission);
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
                    SubmitAsync(previewSubmission, Cts.Token),
                    Cts.Token,
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

            // Use retained submission on retry to ensure the same UUID and payload.
            QuizAttemptSubmission submission = _retainedSubmission
                ?? AppViewMappers.MapPreviewSubmission(_pendingClientUuid, previewSubmission);

            if (Lifetime.IdempotentRequestRepository != null)
            {
                string now = System.DateTimeOffset.UtcNow.ToString("o");
                Lifetime.IdempotentRequestRepository.Upsert(new IdempotentRequestRecord
                {
                    RequestUuid = submission.ClientAttemptUuid,
                    Operation = "quiz_submit",
                    NormalizedPayloadJson = _ctx.QuizId ?? string.Empty,
                    State = "pending",
                    CreatedUtc = now
                });
            }

            var request = new SubmitQuizAttemptRequest
            {
                QuizId = _ctx.QuizId,
                Submission = submission
            };

            AppResult<QuizResult> result =
                await Lifetime.Gateway.SubmitQuizAttemptAsync(request, token).ConfigureAwait(true);

            if (Disposed || token.IsCancellationRequested)
            {
                return;
            }

            _isSubmitting = false;

            if (result.IsSuccess)
            {
                _retainedSubmission = null;
                _coordinator?.ReleaseAttemptSession();

                string attemptId = result.Value?.AttemptId;
                AppRouteContext resultCtx = AppRouteContext.ForQuizResult(attemptId, _ctx.QuizId);
                TaskUtilities.ForgetSafely(
                    Lifetime.Router?.NavigateAsync(AppRouteId.QuizResult, resultCtx, token),
                    token,
                    "QuizAttempt.ToResult");
            }
            else if (result.Error != null && result.Error.IsNetworkError)
            {
                // Keep the submission for retry.
                _retainedSubmission = submission;
                QuizAttemptSession session = new QuizAttemptSession(_ctx.QuizId, submission.ClientAttemptUuid,
                    new QuizDetail { Id = _ctx.QuizId });
                session.BeginSubmit();
                session.MarkUncertainSubmit();
                _coordinator?.RetainAttemptSession(session);
                _view.SetPreviewState(QuizAttemptPreviewState.UncertainSubmission);
            }
            else if (IsUnauthorized(result.Error))
            {
                _coordinator?.ReleaseAttemptSession();
                HandleUnauthorized();
            }
            else
            {
                _view.SetPreviewState(QuizAttemptPreviewState.RecoverableError);
            }
        }

        private void OnExitRequested()
        {
            if (Disposed)
            {
                return;
            }

            _shellRuntime?.RequestExitQuiz(() =>
            {
                _coordinator?.ReleaseAttemptSession();

                TaskUtilities.ForgetSafely(
                    Lifetime.Router?.BackAsync(Cts.Token),
                    Cts.Token,
                    "QuizAttempt.Exit");
            });
        }
    }
}
