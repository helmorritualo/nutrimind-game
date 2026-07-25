using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NutriMind.App.UI
{
    /// <summary>
    /// Standalone <c>UIDocument</c> preview adapter for
    /// <see cref="SettingsPanelView"/>. Hosts the settings route only for
    /// isolated layout preview and logs requests that production routing owns.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class SettingsPanelController : MonoBehaviour
    {
        private const string DropdownPopupAssetPath =
            "Assets/NutriMind/App/UI/USS/SettingsDropdownPopup.uss";

        [SerializeField]
        private StyleSheet _dropdownPopupStyle;

        private UIDocument _uiDocument;
        private SettingsPanelView _view;

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
            if (_uiDocument == null)
            {
                _uiDocument = GetComponent<UIDocument>();
            }

            if (_uiDocument == null)
            {
                return;
            }

            VisualElement panelRoot = _uiDocument.rootVisualElement;
            VisualElement componentRoot = panelRoot?.Q<VisualElement>("settings-root");
            if (componentRoot == null)
            {
                Invoke(nameof(BindWhenReady), 0.05f);
                return;
            }

            panelRoot.style.flexGrow = 1;
            panelRoot.style.width = Length.Percent(100);
            panelRoot.style.height = Length.Percent(100);

            ResolveDropdownPopupStyle();
            UnbindView();
            _view = new SettingsPanelView(componentRoot, _dropdownPopupStyle);
            if (!_view.IsBound)
            {
                Debug.LogWarning(
                    "[SettingsPanelController] SettingsPanelView failed to bind settings-root.");
                _view.Dispose();
                _view = null;
                return;
            }

            _view.BackRequested += OnBackRequested;
            _view.SaveRequested += OnSaveRequested;
            _view.RestoreDefaultsRequested += OnRestoreDefaultsRequested;
            _view.ResetTutorialRequested += OnResetTutorialRequested;
        }

        private void ResolveDropdownPopupStyle()
        {
            if (_dropdownPopupStyle == null)
            {
#if UNITY_EDITOR
                _dropdownPopupStyle =
                    AssetDatabase.LoadAssetAtPath<StyleSheet>(DropdownPopupAssetPath);
#endif
            }
        }

        private void Unbind()
        {
            UnbindView();
            _uiDocument = null;
        }

        private void UnbindView()
        {
            if (_view == null)
            {
                return;
            }

            _view.BackRequested -= OnBackRequested;
            _view.SaveRequested -= OnSaveRequested;
            _view.RestoreDefaultsRequested -= OnRestoreDefaultsRequested;
            _view.ResetTutorialRequested -= OnResetTutorialRequested;
            _view.Dispose();
            _view = null;
        }

        private void OnBackRequested()
        {
            Debug.Log("[SettingsPanelController] Back to Profile requested — preview only.");
        }

        private void OnSaveRequested()
        {
            Debug.Log("[SettingsPanelController] Save settings requested — preview only.");
            _view?.MarkPreviewSaved();
        }

        private void OnRestoreDefaultsRequested()
        {
            Debug.Log(
                "[SettingsPanelController] Restore defaults requested — would show ConfirmDialog.");
        }

        private void OnResetTutorialRequested()
        {
            Debug.Log(
                "[SettingsPanelController] Reset tutorial requested — would show ConfirmDialog.");
        }
    }
}
