using NutriMind.App.Features;
using NutriMind.App.Presentation;
using NutriMind.App.UI;
using NutriMind.Core.Bootstrap;
using NutriMind.Core.Utilities;
using UnityEngine;
using UnityEngine.UIElements;

namespace NutriMind.App.Composition
{
    /// <summary>
    /// Bootstrap scene root. Owns the Bootstrap UIDocument and runtime presenter.
    /// Creates <see cref="AppLifetime"/> when missing. Runtime options are owned by PFB_AppLifetime.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class AppBootstrapSceneRoot : MonoBehaviour
    {
        [SerializeField]
        private UIDocument _uiDocument;

        [SerializeField]
        private AppLifetime _lifetimePrefab;

        private BootstrapPanelView _view;
        private BootstrapRuntimePresenter _presenter;
        private AppStartupCoordinator _startup;

        private void Awake()
        {
            if (_uiDocument == null)
            {
                _uiDocument = GetComponent<UIDocument>();
            }

            EnsureLifetime();
        }

        private void OnEnable()
        {
            BindWhenReady();
        }

        private void OnDisable()
        {
            Teardown();
            CancelInvoke(nameof(BindWhenReady));
        }

        private void EnsureLifetime()
        {
            if (AppLifetime.HasInstance)
            {
                return;
            }

            if (_lifetimePrefab != null)
            {
                Instantiate(_lifetimePrefab);
                return;
            }

            // Prefer inactive construction so Awake/Compose sees any pre-configure call from tests.
            var go = new GameObject(AppLifetime.LifetimeObjectName);
            go.SetActive(false);
            go.AddComponent<AppLifetime>();
            go.SetActive(true);
        }

        private void BindWhenReady()
        {
            if (_uiDocument == null)
            {
                _uiDocument = GetComponent<UIDocument>();
            }

            VisualElement root = _uiDocument != null ? _uiDocument.rootVisualElement : null;
            if (root == null || root.Q<VisualElement>("bootstrap-root") == null)
            {
                Invoke(nameof(BindWhenReady), 0.05f);
                return;
            }

            if (!AppLifetime.HasInstance)
            {
                Invoke(nameof(BindWhenReady), 0.05f);
                return;
            }

            Teardown();
            StretchDocument(root);
            _view = new BootstrapPanelView(root);
            _startup = new AppStartupCoordinator(AppLifetime.Instance);
            _presenter = new BootstrapRuntimePresenter(AppLifetime.Instance, _startup, _view);
            _presenter.Start();
            NutriMindLog.Startup("AppBootstrapSceneRoot bound.");
        }

        private void Teardown()
        {
            _presenter?.Dispose();
            _presenter = null;
            _startup?.Cancel();
            _startup = null;
            _view?.Dispose();
            _view = null;
        }

        private static void StretchDocument(VisualElement root)
        {
            root.style.flexGrow = 1;
            root.style.width = Length.Percent(100);
            root.style.height = Length.Percent(100);
        }
    }
}
