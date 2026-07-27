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
    /// Runtime presenter for the Quiz History route.
    /// Fetches the learner's attempt history from the server.
    /// Maps server QuizHistoryEntry records to QuizHistoryPreviewItems for the view.
    /// Never caches results locally; history is always server-authoritative.
    /// </summary>
    public sealed class QuizHistoryPresenter : RoutePresenterBase
    {
        private readonly QuizHistoryPanelView _view;
        private readonly AppRouteContext _ctx;

        public QuizHistoryPresenter(AppLifetime lifetime, QuizHistoryPanelView view, AppRouteContext ctx)
            : base(lifetime)
        {
            _view = view;
            _ctx = ctx;
            _view.ViewResultRequested += OnViewResultRequested;
            _view.BackToQuizPortalRequested += OnBack;
            _view.RetryRequested += OnRetry;
        }

        public void LoadAsync()
        {
            if (Disposed)
            {
                return;
            }

            TaskUtilities.ForgetSafely(FetchAsync(Cts.Token), Cts.Token, "QuizHistory.Load");
        }

        protected override void OnDispose()
        {
            _view.ViewResultRequested -= OnViewResultRequested;
            _view.BackToQuizPortalRequested -= OnBack;
            _view.RetryRequested -= OnRetry;
        }

        private async Task FetchAsync(CancellationToken token)
        {
            _view.SetPreviewState(QuizHistoryPreviewState.Loading);

            var request = new GetQuizResultsRequest
            {
                SubjectId = _ctx.SubjectId,
                TermId = _ctx.TermId
            };

            AppResult<IReadOnlyList<QuizHistoryEntry>> result =
                await Lifetime.Gateway.GetQuizResultsAsync(request, token).ConfigureAwait(true);

            if (Disposed || token.IsCancellationRequested)
            {
                return;
            }

            if (result.IsSuccess)
            {
                IReadOnlyList<QuizHistoryEntry> entries =
                    result.Value ?? (IReadOnlyList<QuizHistoryEntry>)System.Array.Empty<QuizHistoryEntry>();

                // Filter to the selected quiz if a QuizId is in context.
                if (!string.IsNullOrEmpty(_ctx.QuizId))
                {
                    var filtered = new List<QuizHistoryEntry>();
                    for (int i = 0; i < entries.Count; i++)
                    {
                        if (string.Equals(entries[i].QuizId, _ctx.QuizId, System.StringComparison.Ordinal))
                        {
                            filtered.Add(entries[i]);
                        }
                    }

                    entries = filtered;
                }

                if (entries.Count == 0)
                {
                    _view.SetPreviewState(QuizHistoryPreviewState.Empty);
                }
                else
                {
                    QuizHistoryPreviewItem[] items = AppViewMappers.MapQuizHistoryEntries(entries);
                    _view.SetItems(items);
                    _view.SetPreviewState(QuizHistoryPreviewState.Content);
                }
            }
            else if (IsUnauthorized(result.Error))
            {
                HandleUnauthorized();
            }
            else
            {
                _view.SetPreviewState(QuizHistoryPreviewState.RecoverableError);
            }
        }

        private void OnViewResultRequested(QuizHistoryPreviewSelection selection)
        {
            if (Disposed || string.IsNullOrEmpty(selection.AttemptId))
            {
                return;
            }

            string quizId = selection.Summary.Id;

            TaskUtilities.ForgetSafely(
                Lifetime.Router?.PushAsync(
                    AppRouteId.QuizResult,
                    AppRouteContext.ForQuizResult(selection.AttemptId, quizId),
                    Cts.Token),
                Cts.Token,
                "QuizHistory.ViewResult");
        }

        private void OnBack()
        {
            if (Disposed)
            {
                return;
            }

            TaskUtilities.ForgetSafely(
                Lifetime.Router?.BackAsync(Cts.Token),
                Cts.Token,
                "QuizHistory.Back");
        }

        private void OnRetry()
        {
            if (Disposed)
            {
                return;
            }

            TaskUtilities.ForgetSafely(FetchAsync(Cts.Token), Cts.Token, "QuizHistory.Retry");
        }
    }
}
