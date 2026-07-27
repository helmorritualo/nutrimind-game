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
    /// Runtime presenter for the Quiz Detail route.
    /// Fetches quiz detail from the server (availability, attempt info).
    /// Navigates to QuizAttempt on Start, to QuizHistory on View Results.
    /// Never caches quiz detail or recalculates availability locally.
    /// </summary>
    public sealed class QuizDetailPresenter : RoutePresenterBase
    {
        private readonly QuizDetailPanelView _view;
        private readonly AppRouteContext _ctx;
        private AppRouteContext _resolvedContext;

        public QuizDetailPresenter(AppLifetime lifetime, QuizDetailPanelView view, AppRouteContext ctx)
            : base(lifetime)
        {
            _view = view;
            _ctx = ctx ?? AppRouteContext.Empty;
            _resolvedContext = _ctx;
            _view.StartRequested += OnStartAttempt;
            _view.ViewResultRequested += OnViewHistory;
            _view.BackRequested += OnBack;
            _view.RetryRequested += OnRetry;
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
                "QuizDetail.Load");
        }

        protected override void OnDispose()
        {
            _view.StartRequested -= OnStartAttempt;
            _view.ViewResultRequested -= OnViewHistory;
            _view.BackRequested -= OnBack;
            _view.RetryRequested -= OnRetry;
        }

        private async Task FetchAsync(CancellationToken token)
        {
            _view.SetDataState(DataStatePanelState.Loading);

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
                _resolvedContext = AppRouteContext.ForQuiz(
                        result.Value.Id,
                        result.Value.SubjectId,
                        result.Value.TermId)
                    .WithReturnToMainOnQuizBack(_ctx.ReturnToMainOnQuizBack);
                _view.SetQuizContext(summary);
                _view.SetDataState(DataStatePanelState.Content);
            }
            else if (IsUnauthorized(result.Error))
            {
                HandleUnauthorized();
            }
            else if (IsOffline(result.Error))
            {
                _view.SetDataState(DataStatePanelState.OfflineUnavailable);
            }
            else
            {
                _view.SetDataState(AppViewMappers.ErrorToDataState(result.Error));
            }
        }

        private void OnStartAttempt(QuizDetailPreviewSelection selection)
        {
            if (Disposed)
            {
                return;
            }

            TaskUtilities.ForgetSafely(
                Lifetime.Router?.PushAsync(
                    AppRouteId.QuizAttempt,
                    _resolvedContext,
                    NavigationToken),
                NavigationToken,
                "QuizDetail.Start");
        }

        private void OnViewHistory(QuizDetailPreviewSelection selection)
        {
            if (Disposed)
            {
                return;
            }

            TaskUtilities.ForgetSafely(
                Lifetime.Router?.PushAsync(
                    AppRouteId.QuizHistory,
                    _resolvedContext,
                    NavigationToken),
                NavigationToken,
                "QuizDetail.History");
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
                "QuizDetail.Back");
        }

        private void OnRetry()
        {
            if (Disposed)
            {
                return;
            }

            TaskUtilities.ForgetSafely(
                FetchAsync(RequestToken),
                RequestToken,
                "QuizDetail.Retry");
        }
    }
}
