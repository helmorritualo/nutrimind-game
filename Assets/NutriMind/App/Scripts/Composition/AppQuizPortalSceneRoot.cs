using NutriMind.App.Routing;
using NutriMind.App.UI;
using NutriMind.Core.Bootstrap;
using NutriMind.Core.Utilities;
using UnityEngine;
using UnityEngine.UIElements;

namespace NutriMind.App.Composition
{
    /// <summary>
    /// Quiz Portal scene root. Hosts AppShell-compatible + QuizList scaffolding (Prompt 1).
    /// Does not use AppShellContentPreviewController as a runtime router.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AppQuizPortalSceneRoot : MonoBehaviour
    {
        [SerializeField]
        private UIDocument _shellDocument;

        [SerializeField]
        private VisualTreeAsset _quizListTreeAsset;

        [SerializeField]
        private AppShellController _shellController;

        private QuizListPanelView _quizListView;
        private TemplateContainer _quizListInstance;

        private void Awake()
        {
            if (_shellDocument == null)
            {
                _shellDocument = GetComponent<UIDocument>();
            }

            if (_shellController == null)
            {
                _shellController = GetComponent<AppShellController>();
            }
        }

        private void OnEnable()
        {
            if (AppLifetime.HasInstance)
            {
                AppLifetime.Instance.Router?.EnsureQuizPortalRoot();
                AppLifetime.Instance.Router.RouteChanged += OnRouteChanged;
            }

            BindWhenReady();
        }

        private void OnDisable()
        {
            if (AppLifetime.HasInstance && AppLifetime.Instance.Router != null)
            {
                AppLifetime.Instance.Router.RouteChanged -= OnRouteChanged;
            }

            TeardownContent();
            CancelInvoke(nameof(BindWhenReady));
        }

        private void OnRouteChanged(AppRouteEntry entry)
        {
            if (!AppSceneNavigator.IsQuizPortalRoute(entry.RouteId))
            {
                return;
            }

            ShowQuizListScaffolding();
            if (entry.RouteId != AppRouteId.QuizList)
            {
                _shellController?.SetPageTitle(entry.RouteId.ToString(), "Prompt 2 wiring pending");
            }
        }

        private void BindWhenReady()
        {
            if (_shellDocument == null)
            {
                _shellDocument = GetComponent<UIDocument>();
            }

            if (_shellController == null)
            {
                _shellController = GetComponent<AppShellController>();
            }

            VisualElement root = _shellDocument != null ? _shellDocument.rootVisualElement : null;
            if (root == null || root.Q<VisualElement>("app-shell-root") == null)
            {
                Invoke(nameof(BindWhenReady), 0.05f);
                return;
            }

            StretchDocument(root);
            _shellController?.SetPageTitle("Quiz Portal", "Quizzes");
            _shellController?.SetLoadingPreview(false);
            ShowQuizListScaffolding();
            NutriMindLog.Runtime("AppQuizPortalSceneRoot bound (QuizList scaffolding).");
        }

        private void ShowQuizListScaffolding()
        {
            TeardownContent();
            VisualElement content = _shellController != null
                ? _shellController.GetContentRegion()
                : _shellDocument?.rootVisualElement?.Q<VisualElement>("app-shell-content-region");
            if (content == null)
            {
                return;
            }

            content.Clear();
            _shellController?.SetLoadingPreview(true);

            if (_quizListTreeAsset == null)
            {
                var placeholder = new Label("QuizList content asset is not assigned.");
                content.Add(placeholder);
                _shellController?.SetLoadingPreview(false);
                return;
            }

            _quizListInstance = _quizListTreeAsset.Instantiate();
            content.Add(_quizListInstance);
            _quizListView = new QuizListPanelView(_quizListInstance);
            _shellController?.SetLoadingPreview(false);
            _shellController?.SetPageTitle("Quiz Portal", "Quizzes");
        }

        private void TeardownContent()
        {
            _quizListView?.Dispose();
            _quizListView = null;
            if (_quizListInstance != null)
            {
                _quizListInstance.RemoveFromHierarchy();
                _quizListInstance = null;
            }
        }

        private static void StretchDocument(VisualElement root)
        {
            root.style.flexGrow = 1;
            root.style.width = Length.Percent(100);
            root.style.height = Length.Percent(100);
        }
    }
}
