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

        public QuizResultPresenter(AppLifetime lifetime, QuizResultPanelView view, AppRouteContext ctx)
            : base(lifetime)
        {
            _view = view;
            _ctx = ctx;
            _view.BackToQuizPortalRequested += OnBack;
            _view.RetryRequested += OnTryAgain;
        }

        public void LoadAsync()
        {
            if (Disposed)
            {
                return;
            }

            FetchAsync(Cts.Token);
        }

        protected override void OnDispose()
        {
            _view.BackToQuizPortalRequested -= OnBack;
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

            QuizResultPreviewContent previewResult = AppViewMappers.MapQuizResult(result.Value);

            _view.SetResultContext(summary, detail, previewResult);
        }

        private void OnBack()
        {
            if (Disposed)
            {
                return;
            }

            TaskUtilities.ForgetSafely(
                Lifetime.Router?.NavigateAsync(AppRouteId.QuizList, AppRouteContext.Empty, Cts.Token),
                Cts.Token,
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
                    AppRouteContext.ForQuiz(_ctx.QuizId),
                    Cts.Token),
                Cts.Token,
                "QuizResult.TryAgain");
        }
    }
}
