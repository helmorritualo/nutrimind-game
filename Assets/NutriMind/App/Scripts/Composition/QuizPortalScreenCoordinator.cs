using System;
using NutriMind.App.Presentation;
using NutriMind.App.Routing;
using NutriMind.App.UI;
using NutriMind.Core.Bootstrap;
using NutriMind.Core.Utilities;
using UnityEngine;
using UnityEngine.UIElements;

namespace NutriMind.App.Composition
{
    /// <summary>
    /// Manages the Quiz Portal scene content region.
    /// Observes <see cref="IAppRouter.RouteChanged"/> for quiz-portal routes, swaps UXML content,
    /// and constructs runtime presenters. Retains <see cref="QuizAttemptSession"/> across
    /// uncertain-submit timeouts so the identical payload is retried.
    /// Does not own shell chrome, gateway, SQLite, or scene loading.
    /// </summary>
    public sealed class QuizPortalScreenCoordinator : IDisposable
    {
        private readonly AppLifetime _lifetime;
        private readonly AppShellController _shellController;
        private readonly AppShellRuntimeController _shellRuntime;

        private readonly VisualTreeAsset _quizListAsset;
        private readonly VisualTreeAsset _quizDetailAsset;
        private readonly VisualTreeAsset _quizAttemptAsset;
        private readonly VisualTreeAsset _quizResultAsset;
        private readonly VisualTreeAsset _quizHistoryAsset;
        private readonly VisualTreeAsset _dataStatePanelAsset;

        private TemplateContainer _activeInstance;
        private IDisposable _activePresenter;
        private IAppScreenView _activeView;

        // Retained across uncertain-submit so retry can use identical UUID + payload.
        private QuizAttemptSession _retainedAttemptSession;

        /// <summary>
        /// Session retained after an uncertain-submit timeout so the presenter can retry
        /// with the identical clientAttemptUuid and payload.
        /// </summary>
        public QuizAttemptSession RetainedAttemptSession => _retainedAttemptSession;

        private bool _disposed;

        public QuizPortalScreenCoordinator(
            AppLifetime lifetime,
            AppShellController shellController,
            AppShellRuntimeController shellRuntime,
            QuizPortalScreenAssets assets)
        {
            _lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
            _shellController = shellController;
            _shellRuntime = shellRuntime;

            if (assets != null)
            {
                _quizListAsset = assets.QuizListAsset;
                _quizDetailAsset = assets.QuizDetailAsset;
                _quizAttemptAsset = assets.QuizAttemptAsset;
                _quizResultAsset = assets.QuizResultAsset;
                _quizHistoryAsset = assets.QuizHistoryAsset;
                _dataStatePanelAsset = assets.DataStatePanelAsset;
            }

            if (_lifetime.Router != null)
            {
                _lifetime.Router.RouteChanged += OnRouteChanged;
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            if (_lifetime?.Router != null)
            {
                _lifetime.Router.RouteChanged -= OnRouteChanged;
            }

            TeardownActive();
            _retainedAttemptSession = null;
        }

        public void ApplyCurrentRoute()
        {
            if (_lifetime?.Router != null)
            {
                OnRouteChanged(_lifetime.Router.CurrentRoute);
            }
        }

        /// <summary>
        /// Called by QuizAttemptPresenter when an attempt enters an uncertain-submit state.
        /// The session is kept so the presenter can retry with the identical clientAttemptUuid.
        /// </summary>
        public void RetainAttemptSession(QuizAttemptSession session)
        {
            _retainedAttemptSession = session;
        }

        /// <summary>
        /// Called by QuizAttemptPresenter after a successful or clearly-failed submit.
        /// </summary>
        public void ReleaseAttemptSession()
        {
            _retainedAttemptSession = null;
        }

        private void OnRouteChanged(AppRouteEntry entry)
        {
            if (_disposed)
            {
                return;
            }

            if (!AppSceneNavigator.IsQuizPortalRoute(entry.RouteId))
            {
                return;
            }

            TeardownActive();
            BuildRouteContent(entry);
        }

        private void BuildRouteContent(AppRouteEntry entry)
        {
            VisualElement contentRegion = _shellController?.GetContentRegion();
            if (contentRegion == null)
            {
                NutriMindLog.RuntimeWarning(
                    "QuizPortalScreenCoordinator: content region not available for route " + entry.RouteId);
                return;
            }

            contentRegion.Clear();

            switch (entry.RouteId)
            {
                case AppRouteId.QuizList:
                    BuildQuizList(contentRegion, entry.Context);
                    UpdateShellChrome("Quiz Portal", "Quizzes");
                    break;

                case AppRouteId.QuizDetail:
                    BuildQuizDetail(contentRegion, entry.Context);
                    UpdateShellChrome("Quiz Details", null);
                    break;

                case AppRouteId.QuizAttempt:
                    BuildQuizAttempt(contentRegion, entry.Context);
                    UpdateShellChrome("Quiz", null);
                    break;

                case AppRouteId.QuizResult:
                    BuildQuizResult(contentRegion, entry.Context);
                    UpdateShellChrome("Quiz Result", null);
                    break;

                case AppRouteId.QuizHistory:
                    BuildQuizHistory(contentRegion, entry.Context);
                    UpdateShellChrome("Quiz History", null);
                    break;

                default:
                    BuildPlaceholder(contentRegion, entry.RouteId.ToString());
                    break;
            }
        }

        private void BuildQuizList(VisualElement region, AppRouteContext ctx)
        {
            if (_quizListAsset == null)
            {
                BuildPlaceholder(region, "QuizList");
                return;
            }

            _activeInstance = _quizListAsset.Instantiate();
            region.Add(_activeInstance);
            var view = new QuizListPanelView(_activeInstance, _dataStatePanelAsset);
            _activeView = view;
            var presenter = new QuizListPresenter(_lifetime, view);
            _activePresenter = presenter;
            presenter.LoadAsync();
        }

        private void BuildQuizDetail(VisualElement region, AppRouteContext ctx)
        {
            if (_quizDetailAsset == null)
            {
                BuildPlaceholder(region, "QuizDetail");
                return;
            }

            _activeInstance = _quizDetailAsset.Instantiate();
            region.Add(_activeInstance);
            var view = new QuizDetailPanelView(_activeInstance);
            _activeView = view;
            var presenter = new QuizDetailPresenter(_lifetime, view, ctx);
            _activePresenter = presenter;
            presenter.LoadAsync();
        }

        private void BuildQuizAttempt(VisualElement region, AppRouteContext ctx)
        {
            if (_quizAttemptAsset == null)
            {
                BuildPlaceholder(region, "QuizAttempt");
                return;
            }

            _activeInstance = _quizAttemptAsset.Instantiate();
            region.Add(_activeInstance);
            var view = new QuizAttemptPanelView(_activeInstance);
            _activeView = view;
            var presenter = new QuizAttemptPresenter(_lifetime, view, ctx, _shellRuntime, this);
            _activePresenter = presenter;
            presenter.LoadAsync();
        }

        private void BuildQuizResult(VisualElement region, AppRouteContext ctx)
        {
            if (_quizResultAsset == null)
            {
                BuildPlaceholder(region, "QuizResult");
                return;
            }

            _activeInstance = _quizResultAsset.Instantiate();
            region.Add(_activeInstance);
            var view = new QuizResultPanelView(_activeInstance);
            _activeView = view;
            var presenter = new QuizResultPresenter(_lifetime, view, ctx);
            _activePresenter = presenter;
            presenter.LoadAsync();
        }

        private void BuildQuizHistory(VisualElement region, AppRouteContext ctx)
        {
            if (_quizHistoryAsset == null)
            {
                BuildPlaceholder(region, "QuizHistory");
                return;
            }

            _activeInstance = _quizHistoryAsset.Instantiate();
            region.Add(_activeInstance);
            var view = new QuizHistoryPanelView(_activeInstance, _dataStatePanelAsset);
            _activeView = view;
            var presenter = new QuizHistoryPresenter(_lifetime, view, ctx);
            _activePresenter = presenter;
            presenter.LoadAsync();
        }

        private void TeardownActive()
        {
            _activePresenter?.Dispose();
            _activePresenter = null;
            _activeView?.Dispose();
            _activeView = null;

            if (_activeInstance != null)
            {
                _activeInstance.RemoveFromHierarchy();
                _activeInstance = null;
            }
        }

        private static void BuildPlaceholder(VisualElement region, string routeName)
        {
            var label = new Label(routeName + " — UXML asset not assigned. Assign in AppQuizPortalSceneRoot.");
            label.AddToClassList("app-screen-content");
            region.Add(label);
        }

        private void UpdateShellChrome(string title, string context)
        {
            _shellController?.SetPageTitle(title, context);
            _shellController?.HideLoadingOverlay();
        }
    }

    /// <summary>
    /// Serializable asset bundle passed from AppQuizPortalSceneRoot to QuizPortalScreenCoordinator.
    /// </summary>
    [Serializable]
    public sealed class QuizPortalScreenAssets
    {
        public VisualTreeAsset QuizListAsset;
        public VisualTreeAsset QuizDetailAsset;
        public VisualTreeAsset QuizAttemptAsset;
        public VisualTreeAsset QuizResultAsset;
        public VisualTreeAsset QuizHistoryAsset;
        public VisualTreeAsset DataStatePanelAsset;
    }
}
