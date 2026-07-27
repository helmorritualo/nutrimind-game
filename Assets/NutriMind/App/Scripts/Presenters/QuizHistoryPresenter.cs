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

namespace NutriMind.App.Presentation
{
    /// <summary>
    /// Runtime presenter for the Quiz History route.
    /// Fetches the learner's attempt history from the server and persists a learner-scoped
    /// offline cache for declared cold-restart fallback.
    /// </summary>
    public sealed class QuizHistoryPresenter : RoutePresenterBase
    {
        private readonly QuizHistoryPanelView _view;
        private readonly AppRouteContext _ctx;

        public QuizHistoryPresenter(AppLifetime lifetime, QuizHistoryPanelView view, AppRouteContext ctx)
            : base(lifetime)
        {
            _view = view;
            _ctx = ctx ?? AppRouteContext.Empty;
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

            TaskUtilities.ForgetSafely(
                FetchAsync(RequestToken),
                RequestToken,
                "QuizHistory.Load");
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
                    result.Value ?? (IReadOnlyList<QuizHistoryEntry>)Array.Empty<QuizHistoryEntry>();
                entries = FilterByQuizId(entries);
                PersistQuizHistoryCache(entries);

                if (entries.Count == 0)
                {
                    _view.SetItems(Array.Empty<QuizHistoryPreviewItem>());
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
            else if (IsOffline(result.Error))
            {
                IReadOnlyList<QuizHistoryEntry> cached = LoadQuizHistoryCache();
                if (cached != null)
                {
                    if (cached.Count == 0)
                    {
                        _view.SetItems(Array.Empty<QuizHistoryPreviewItem>());
                        _view.SetPreviewState(QuizHistoryPreviewState.Empty);
                    }
                    else
                    {
                        _view.SetItems(AppViewMappers.MapQuizHistoryEntries(cached));
                        _view.SetPreviewState(QuizHistoryPreviewState.OfflineCached);
                    }
                }
                else
                {
                    _view.SetItems(Array.Empty<QuizHistoryPreviewItem>());
                    _view.SetPreviewState(QuizHistoryPreviewState.OfflineUnavailable);
                }
            }
            else
            {
                _view.SetPreviewState(QuizHistoryPreviewState.RecoverableError);
            }
        }

        private IReadOnlyList<QuizHistoryEntry> FilterByQuizId(IReadOnlyList<QuizHistoryEntry> entries)
        {
            if (string.IsNullOrEmpty(_ctx.QuizId) || entries == null)
            {
                return entries ?? Array.Empty<QuizHistoryEntry>();
            }

            var filtered = new List<QuizHistoryEntry>();
            for (int i = 0; i < entries.Count; i++)
            {
                if (string.Equals(entries[i].QuizId, _ctx.QuizId, StringComparison.Ordinal))
                {
                    filtered.Add(entries[i]);
                }
            }

            return filtered;
        }

        private string BuildQueryKey()
        {
            return (_ctx.SubjectId ?? string.Empty)
                + "|"
                + (_ctx.TermId ?? string.Empty)
                + "|"
                + (_ctx.QuizId ?? string.Empty);
        }

        private void PersistQuizHistoryCache(IReadOnlyList<QuizHistoryEntry> entries)
        {
            string studentId = Lifetime.AuthenticatedStudentState?.Profile?.Id
                ?? Lifetime.CurrentProfile?.Id;
            if (string.IsNullOrWhiteSpace(studentId) || Lifetime.ResourceCacheRepository == null)
            {
                return;
            }

            LearnerRouteCache.SaveQuizHistory(
                Lifetime.ResourceCacheRepository,
                studentId,
                BuildQueryKey(),
                entries ?? Array.Empty<QuizHistoryEntry>(),
                DateTimeOffset.UtcNow.ToString("o"));
        }

        private IReadOnlyList<QuizHistoryEntry> LoadQuizHistoryCache()
        {
            string studentId = Lifetime.AuthenticatedStudentState?.Profile?.Id
                ?? Lifetime.CurrentProfile?.Id;
            if (string.IsNullOrWhiteSpace(studentId) || Lifetime.ResourceCacheRepository == null)
            {
                return null;
            }

            AppResult<IReadOnlyList<QuizHistoryEntry>> cached = LearnerRouteCache.LoadQuizHistory(
                Lifetime.ResourceCacheRepository,
                studentId,
                BuildQueryKey());
            return cached.IsSuccess ? cached.Value : null;
        }

        private void OnViewResultRequested(QuizHistoryPreviewSelection selection)
        {
            if (Disposed || string.IsNullOrEmpty(selection.AttemptId))
            {
                return;
            }

            TaskUtilities.ForgetSafely(
                Lifetime.Router?.NavigateAsync(
                    AppRouteId.QuizResult,
                    AppRouteContext.ForQuizResult(
                            selection.AttemptId,
                            _ctx.QuizId,
                            _ctx.SubjectId,
                            _ctx.TermId)
                        .WithReturnToMainOnQuizBack(_ctx.ReturnToMainOnQuizBack),
                    NavigationToken),
                NavigationToken,
                "QuizHistory.ViewResult");
        }

        private void OnRetry()
        {
            if (Disposed)
            {
                return;
            }

            TaskUtilities.ForgetSafely(FetchAsync(RequestToken), RequestToken, "QuizHistory.Retry");
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
                "QuizHistory.Back");
        }
    }
}
