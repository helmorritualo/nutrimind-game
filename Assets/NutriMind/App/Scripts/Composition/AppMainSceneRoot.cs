using NutriMind.App.Routing;
using NutriMind.App.UI;
using NutriMind.Core.Bootstrap;
using NutriMind.Core.Utilities;
using UnityEngine;
using UnityEngine.UIElements;

namespace NutriMind.App.Composition
{
    /// <summary>
    /// Main application scene root. Hosts AppShell + Home scaffolding only (Prompt 1).
    /// Does not use AppShellContentPreviewController as a runtime router.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AppMainSceneRoot : MonoBehaviour
    {
        [SerializeField]
        private UIDocument _shellDocument;

        [SerializeField]
        private VisualTreeAsset _homePanelAsset;

        [SerializeField]
        private AppShellController _shellController;

        private HomePanelView _homeView;
        private TemplateContainer _homeInstance;

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
                AppLifetime.Instance.Router?.EnsureMainRoot();
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
            if (entry.RouteId == AppRouteId.Home)
            {
                ShowHomeScaffolding();
            }
            else
            {
                // Prompt 2 wires remaining Main routes; keep Home scaffolding as the safe placeholder.
                ShowHomeScaffolding();
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
            _shellController?.SetPreviewRoute(AppShellPreviewRoute.Home);
            _shellController?.SetPageTitle("Home", "Main");
            _shellController?.SetLoadingPreview(false);
            ShowHomeScaffolding();
            NutriMindLog.Runtime("AppMainSceneRoot bound (Home scaffolding).");
        }

        private void ShowHomeScaffolding()
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

            if (_homePanelAsset == null)
            {
                var placeholder = new Label("Home content asset is not assigned.");
                placeholder.AddToClassList("app-screen-content");
                content.Add(placeholder);
                _shellController?.SetLoadingPreview(false);
                return;
            }

            _homeInstance = _homePanelAsset.Instantiate();
            content.Add(_homeInstance);
            _homeView = new HomePanelView(_homeInstance);
            if (_homeView.IsBound)
            {
                _homeView.QuizPortalRequested += OnQuizPortalRequested;
            }

            _shellController?.SetLoadingPreview(false);
            _shellController?.SetPageTitle("Home", "Main");
        }

        private void OnQuizPortalRequested()
        {
            if (!AppLifetime.HasInstance || AppLifetime.Instance.Router == null)
            {
                return;
            }

            TaskUtilities.ForgetSafely(
                AppLifetime.Instance.Router.EnterQuizPortalAsync(
                    AppRouteContext.Empty.WithReturnToMainOnQuizBack(true),
                    AppLifetime.Instance.LifetimeToken),
                AppLifetime.Instance.LifetimeToken,
                "Main.EnterQuizPortal");
        }

        private void TeardownContent()
        {
            if (_homeView != null)
            {
                _homeView.QuizPortalRequested -= OnQuizPortalRequested;
                _homeView.Dispose();
                _homeView = null;
            }

            if (_homeInstance != null)
            {
                _homeInstance.RemoveFromHierarchy();
                _homeInstance = null;
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
