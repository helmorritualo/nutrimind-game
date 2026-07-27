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
    /// Runtime presenter for the Quiz Result route.
    /// Fetches the server-authoritative result and binds the view.
    /// Never recalculates scores, selected answers, correct answers,
    /// explanations, or per-question points locally.
    /// Shows the canonical server response as-is.
    /// </summary>
    public sealed class QuizResultPresenter : RoutePresenterBase
    {
        private readonly QuizResultPanelView _view;
        private readonly AppRouteContext _ctx;
        private string _resolvedQuizId;
        private string _resolvedSubjectId;
        private string _resolvedTermId;

        public QuizResultPresenter(AppLifetime lifetime, QuizResultPanelView view, AppRouteContext ctx)
            : base(lifetime)
        {
            _view = view;
            _ctx = ctx ?? AppRouteContext.Empty;
            _resolvedQuizId = _ctx.QuizId;
            _resolvedSubjectId = _ctx.SubjectId;
            _resolvedTermId = _ctx.TermId;
            _view.BackToQuizPortalRequested += OnBack;
            _view.ViewHistoryRequested += OnViewHistory;
            _view.RetryRequested += OnTryAgain;
        }

        public void LoadAsync()
        {
            if (Disposed)
            {
                return;
            }

            TaskUtilities.ForgetSafely(
                FetchAsync(RequestToken),
                RequestToken,
                "QuizResult.Load");
        }

        protected override void OnDispose()
        {
            _view.BackToQuizPortalRequested -= OnBack;
            _view.ViewHistoryRequested -= OnViewHistory;
            _view.RetryRequested -= OnTryAgain;
        }

        private async Task FetchAsync(CancellationToken token)
        {
            _view.SetPreviewState(QuizResultPreviewState.Loading);

            // Fetch the result.
            var resultReq = new GetQuizResultRequest { AttemptId = _ctx.AttemptId };
            AppResult<QuizResult> result =
                await Lifetime.Gateway.GetQuizResultAsync(resultReq, token).ConfigureAwait(true);

            if (Disposed || token.IsCancellationRequested)
            {
                return;
            }

            if (!result.IsSuccess)
            {
                if (IsUnauthorized(result.Error))
                {
                    HandleUnauthorized();
                    return;
                }

                _view.SetPreviewState(QuizResultPreviewState.RecoverableError);
                return;
            }

            // Also fetch quiz detail for context.
            var detailReq = new QuizIdRequest { QuizId = _ctx.QuizId ?? result.Value.QuizId };
            AppResult<QuizDetail> detailResult =
                await Lifetime.Gateway.GetQuizDetailAsync(detailReq, token).ConfigureAwait(true);

            if (Disposed || token.IsCancellationRequested)
            {
                return;
            }

            QuizListPreviewItem summary = AppViewMappers.MapQuizSummaryToPreviewItem(new QuizSummary
            {
                Id = result.Value.QuizId,
                Title = detailResult.IsSuccess ? detailResult.Value.Title : result.Value.QuizId,
                SubjectId = detailResult.IsSuccess ? detailResult.Value.SubjectId : string.Empty,
                TermId = detailResult.IsSuccess ? detailResult.Value.TermId : string.Empty,
                Status = detailResult.IsSuccess ? detailResult.Value.Status : "completed"
            });

            QuizDetailPreviewContent detail = detailResult.IsSuccess
                ? AppViewMappers.MapQuizDetail(detailResult.Value)
                : null;

            _resolvedQuizId = result.Value.QuizId ?? _ctx.QuizId;
            if (detailResult.IsSuccess)
            {
                _resolvedSubjectId = detailResult.Value.SubjectId;
                _resolvedTermId = detailResult.Value.TermId;
            }

            QuizResultPreviewContent previewResult = AppViewMappers.MapQuizResult(result.Value);

            _view.SetResultContext(summary, detail, previewResult);
        }

        private void OnBack()
        {
            if (Disposed)
            {
                return;
            }

            AppRouteContext rootContext = AppRouteContext.ForQuiz(
                    null,
                    _resolvedSubjectId,
                    _resolvedTermId)
                .WithReturnToMainOnQuizBack(_ctx.ReturnToMainOnQuizBack);
            TaskUtilities.ForgetSafely(
                Lifetime.Router?.ResetQuizPortalToRootAsync(
                    rootContext,
                    NavigationToken),
                NavigationToken,
                "QuizResult.Back");
        }

        private void OnTryAgain()
        {
            if (Disposed)
            {
                return;
            }

            TaskUtilities.ForgetSafely(
                Lifetime.Router?.NavigateAsync(
                    AppRouteId.QuizAttempt,
                    AppRouteContext.ForQuiz(
                            _resolvedQuizId,
                            _resolvedSubjectId,
                            _resolvedTermId)
                        .WithReturnToMainOnQuizBack(_ctx.ReturnToMainOnQuizBack),
                    NavigationToken),
                NavigationToken,
                "QuizResult.TryAgain");
        }

        private void OnViewHistory()
        {
            if (Disposed)
            {
                return;
            }

            TaskUtilities.ForgetSafely(
                Lifetime.Router?.PushAsync(
                    AppRouteId.QuizHistory,
                    AppRouteContext.ForQuiz(
                            _resolvedQuizId,
                            _resolvedSubjectId,
                            _resolvedTermId)
                        .WithReturnToMainOnQuizBack(_ctx.ReturnToMainOnQuizBack),
                    NavigationToken),
                NavigationToken,
                "QuizResult.History");
        }
    }
}
