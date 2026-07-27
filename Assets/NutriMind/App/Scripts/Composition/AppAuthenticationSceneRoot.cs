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
    /// Authentication scene root. Owns Login UIDocument and runtime presenter.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class AppAuthenticationSceneRoot : MonoBehaviour
    {
        [SerializeField]
        private UIDocument _uiDocument;

        private LoginPanelView _view;
        private LoginRuntimePresenter _presenter;

        private void Awake()
        {
            if (_uiDocument == null)
            {
                _uiDocument = GetComponent<UIDocument>();
            }
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

        private void BindWhenReady()
        {
            if (!AppLifetime.HasInstance)
            {
                NutriMindLog.AuthWarning(
                    "AppAuthenticationSceneRoot requires AppLifetime. Load SCN_App_Bootstrap first.");
                Invoke(nameof(BindWhenReady), 0.1f);
                return;
            }

            if (_uiDocument == null)
            {
                _uiDocument = GetComponent<UIDocument>();
            }

            VisualElement root = _uiDocument != null ? _uiDocument.rootVisualElement : null;
            if (root == null || root.Q<VisualElement>("login-root") == null)
            {
                Invoke(nameof(BindWhenReady), 0.05f);
                return;
            }

            Teardown();
            StretchDocument(root);
            _view = new LoginPanelView(root, SystemInfo.deviceName);
            var useCase = new LoginUseCase(AppLifetime.Instance, new AppStartupCoordinator(AppLifetime.Instance));
            _presenter = new LoginRuntimePresenter(AppLifetime.Instance, useCase, _view);
            NutriMindLog.Auth("AppAuthenticationSceneRoot bound.");
        }

        private void Teardown()
        {
            _presenter?.Dispose();
            _presenter = null;
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
