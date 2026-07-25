using UnityEngine;
using UnityEngine.UIElements;

namespace NutriMind.App.UI
{
    /// <summary>Standalone preview adapter for <see cref="ProfilePanelView"/>.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class ProfilePanelController : MonoBehaviour
    {
        private UIDocument _uiDocument;
        private ProfilePanelView _view;
        private bool _eventsRegistered;

        private void OnEnable()
        {
            _uiDocument = GetComponent<UIDocument>();
            BindWhenReady();
        }

        private void OnDisable()
        {
            CancelInvoke(nameof(BindWhenReady));
            Unbind();
        }

        private void BindWhenReady()
        {
            _uiDocument ??= GetComponent<UIDocument>();
            if (_uiDocument == null) return;

            VisualElement panelRoot = _uiDocument.rootVisualElement;
            VisualElement componentRoot = panelRoot?.Q<VisualElement>("profile-root");
            if (componentRoot == null)
            {
                Invoke(nameof(BindWhenReady), 0.05f);
                return;
            }

            panelRoot.style.flexGrow = 1;
            panelRoot.style.width = Length.Percent(100);
            panelRoot.style.height = Length.Percent(100);
            UnbindView();

            _view = new ProfilePanelView(componentRoot);
            if (!_view.IsBound)
            {
                Debug.LogWarning("[ProfilePanelController] ProfilePanelView failed to bind profile-root.");
                _view.Dispose();
                _view = null;
                return;
            }

            _view.BackRequested += OnBackRequested;
            _view.SettingsRequested += OnSettingsRequested;
            _view.SignOutRequested += OnSignOutRequested;
            _eventsRegistered = true;
        }

        private void Unbind()
        {
            UnbindView();
            _uiDocument = null;
        }

        private void UnbindView()
        {
            if (_view == null) return;
            if (_eventsRegistered)
            {
                _view.BackRequested -= OnBackRequested;
                _view.SettingsRequested -= OnSettingsRequested;
                _view.SignOutRequested -= OnSignOutRequested;
                _eventsRegistered = false;
            }
            _view.Dispose();
            _view = null;
        }

        private void OnBackRequested() => Debug.Log("[ProfilePanelController] Back requested — preview only.");
        private void OnSettingsRequested() => Debug.Log("[ProfilePanelController] Settings requested — preview only.");
        private void OnSignOutRequested() => Debug.Log("[ProfilePanelController] Sign out requested — preview only.");
    }
}
