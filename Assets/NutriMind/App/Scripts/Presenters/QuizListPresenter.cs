using System.Collections.Generic;
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
    /// Runtime presenter for the Quiz List route.
    /// Fetches available quizzes from the server.
    /// Shows per-quiz open/close windows and attempt counts as server-provided.
    /// Never recalculates scores or quiz status locally.
    /// </summary>
    public sealed class QuizListPresenter : RoutePresenterBase
    {
        private readonly QuizListPanelView _view;
        private QuizListPreviewItem[] _cachedItems;

        public QuizListPresenter(AppLifetime lifetime, QuizListPanelView view)
            : base(lifetime)
        {
            _view = view;
            _view.QuizDetailsRequested += OnQuizSelected;
            _view.QuizResultRequested += OnQuizHistoryRequested;
            _view.RetryRequested += OnRetry;
            _view.ReturnToMainRequested += OnReturnToMain;
        }

        public void LoadAsync()
        {
            if (Disposed)
            {
                return;
            }

            TaskUtilities.ForgetSafely(FetchAsync(RequestToken), RequestToken, "QuizList.Load");
        }

        private async Task FetchAsync(CancellationToken token)
        {
            _view.SetDataState(DataStatePanelState.Loading);

            var request = new GetQuizzesRequest();

            AppResult<IReadOnlyList<QuizSummary>> result =
                await Lifetime.Gateway.GetQuizzesAsync(request, token).ConfigureAwait(true);

            if (Disposed || token.IsCancellationRequested)
            {
                return;
            }

            if (result.IsSuccess)
            {
                IReadOnlyList<QuizSummary> quizzes =
                    result.Value ?? (IReadOnlyList<QuizSummary>)System.Array.Empty<QuizSummary>();
                _cachedItems = AppViewMappers.MapQuizSummaries(quizzes);

                if (_cachedItems.Length == 0)
                {
                    _view.SetDataState(DataStatePanelState.Empty);
                }
                else
                {
                    _view.SetItems(_cachedItems);
                    _view.SetDataState(DataStatePanelState.Content);
                }
            }
            else if (IsUnauthorized(result.Error))
            {
                HandleUnauthorized();
            }
            else if (IsOffline(result.Error) && _cachedItems != null && _cachedItems.Length > 0)
            {
                _view.SetItems(_cachedItems);
                _view.SetDataState(DataStatePanelState.OfflineCached);
            }
            else
            {
                _view.SetDataState(AppViewMappers.ErrorToDataState(result.Error));
            }
        }

        private void OnQuizSelected(QuizListPreviewItem item)
        {
            if (Disposed)
            {
                return;
            }

            TaskUtilities.ForgetSafely(
                Lifetime.Router?.PushAsync(
                    AppRouteId.QuizDetail,
                    AppRouteContext.ForQuiz(item.Id, item.Subject.ToString(), item.Term.ToString()),
                    NavigationToken),
                NavigationToken,
                "QuizList.Detail");
        }

        private void OnQuizHistoryRequested(QuizListPreviewItem item)
        {
            if (Disposed)
            {
                return;
            }

            TaskUtilities.ForgetSafely(
                Lifetime.Router?.PushAsync(
                    AppRouteId.QuizHistory,
                    AppRouteContext.ForQuiz(item.Id),
                    NavigationToken),
                NavigationToken,
                "QuizList.History");
        }

        private void OnRetry()
        {
            if (Disposed)
            {
                return;
            }

            TaskUtilities.ForgetSafely(FetchAsync(RequestToken), RequestToken, "QuizList.Retry");
        }

        private void OnReturnToMain()
        {
            if (Disposed)
            {
                return;
            }

            TaskUtilities.ForgetSafely(
                Lifetime.Router?.ReturnToMainAsync(NavigationToken),
                NavigationToken,
                "QuizList.ReturnToMain");
        }
    }
}
