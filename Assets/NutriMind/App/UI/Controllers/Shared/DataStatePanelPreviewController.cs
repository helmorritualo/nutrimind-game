using UnityEngine;
using UnityEngine.UIElements;

namespace NutriMind.App.UI
{
    /// <summary>
    /// Validated semantic icon choices for <see cref="DataStatePanelPreviewController"/> inspector preview.
    /// Maps only to package icon classes confirmed in DesignSystem Icons.uss.
    /// </summary>
    public enum DataStatePanelPreviewIcon
    {
        Default,
        Info,
        Wifi,
        Error,
        Lock,
        Warning,
        Refresh,
        Search
    }

    /// <summary>
    /// Standalone UI Toolkit preview adapter for <see cref="DataStatePanelView"/>.
    /// Presentation only — cycles inspector preview state and logs action requests.
    /// Does not perform networking, SQLite, sync, routing, authentication, or permission evaluation.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class DataStatePanelPreviewController : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Static preview state shown by DataStatePanel. Content hides the component.")]
        private DataStatePanelState _previewState = DataStatePanelState.Empty;

        [SerializeField]
        [Tooltip("When enabled, applies the custom title/message/detail/action labels below.")]
        private bool _useCustomPreviewCopy;

        [SerializeField]
        private string _customTitle;

        [SerializeField]
        [TextArea]
        private string _customMessage;

        [SerializeField]
        [TextArea]
        private string _customDetail;

        [SerializeField]
        private string _customPrimaryActionLabel;

        [SerializeField]
        private string _customSecondaryActionLabel;

        [SerializeField]
        [Tooltip("Optional icon override used only with custom preview copy. Default keeps the state icon.")]
        private DataStatePanelPreviewIcon _customIcon = DataStatePanelPreviewIcon.Default;

        private UIDocument _uiDocument;
        private DataStatePanelView _view;
        private DataStatePanelState? _appliedState;
        private bool? _appliedCustomCopy;
        private bool _eventsRegistered;

        private void OnEnable()
        {
            _uiDocument = GetComponent<UIDocument>();
            BindWhenReady();
        }

        private void OnDisable()
        {
            Unbind();
            CancelInvoke(nameof(BindWhenReady));
        }

        private void OnValidate()
        {
            if (!isActiveAndEnabled || _view == null || !_view.IsBound)
            {
                return;
            }

            ApplyPreviewState(force: true);
        }

        private void Update()
        {
            if (_view == null || !_view.IsBound)
            {
                return;
            }

            ApplyPreviewState(force: false);
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
            if (panelRoot == null)
            {
                Invoke(nameof(BindWhenReady), 0.05f);
                return;
            }

            VisualElement componentRoot = panelRoot.name == "data-state-panel-root"
                ? panelRoot
                : panelRoot.Q<VisualElement>("data-state-panel-root");

            if (componentRoot == null)
            {
                Invoke(nameof(BindWhenReady), 0.05f);
                return;
            }

            panelRoot.style.flexGrow = 1;
            panelRoot.style.width = Length.Percent(100);
            panelRoot.style.height = Length.Percent(100);

            UnbindViewOnly();
            _view = new DataStatePanelView(componentRoot);
            if (!_view.IsBound)
            {
                _view.Dispose();
                _view = null;
                Invoke(nameof(BindWhenReady), 0.05f);
                return;
            }

            RegisterViewEvents();
            ApplyPreviewState(force: true);
        }

        private void Unbind()
        {
            UnbindViewOnly();
            _uiDocument = null;
            _appliedState = null;
            _appliedCustomCopy = null;
        }

        private void UnbindViewOnly()
        {
            UnregisterViewEvents();

            if (_view != null)
            {
                _view.Dispose();
                _view = null;
            }
        }

        private void RegisterViewEvents()
        {
            if (_view == null || _eventsRegistered)
            {
                return;
            }

            _view.PrimaryActionRequested += OnPrimaryActionRequested;
            _view.SecondaryActionRequested += OnSecondaryActionRequested;
            _eventsRegistered = true;
        }

        private void UnregisterViewEvents()
        {
            if (_view == null || !_eventsRegistered)
            {
                _eventsRegistered = false;
                return;
            }

            _view.PrimaryActionRequested -= OnPrimaryActionRequested;
            _view.SecondaryActionRequested -= OnSecondaryActionRequested;
            _eventsRegistered = false;
        }

        private void ApplyPreviewState(bool force)
        {
            if (_view == null || !_view.IsBound)
            {
                return;
            }

            bool stateChanged = force || _appliedState != _previewState;
            bool customChanged = force || _appliedCustomCopy != _useCustomPreviewCopy;

            if (!stateChanged && !customChanged)
            {
                return;
            }

            if (stateChanged || force || customChanged)
            {
                _view.SetState(_previewState);
                _appliedState = _previewState;
            }

            if (_useCustomPreviewCopy && _previewState != DataStatePanelState.Content)
            {
                ApplyCustomPreviewCopy();
            }

            _appliedCustomCopy = _useCustomPreviewCopy;
        }

        private void ApplyCustomPreviewCopy()
        {
            string primary = string.IsNullOrWhiteSpace(_customPrimaryActionLabel)
                ? null
                : _customPrimaryActionLabel;
            string secondary = string.IsNullOrWhiteSpace(_customSecondaryActionLabel)
                ? null
                : _customSecondaryActionLabel;

            _view.Configure(new DataStatePanelConfiguration(
                title: string.IsNullOrWhiteSpace(_customTitle) ? null : _customTitle,
                message: string.IsNullOrWhiteSpace(_customMessage) ? null : _customMessage,
                detail: string.IsNullOrWhiteSpace(_customDetail) ? null : _customDetail,
                iconClass: ResolveCustomIconClass(_customIcon),
                primaryActionLabel: primary,
                secondaryActionLabel: secondary));
        }

        private void OnPrimaryActionRequested()
        {
            Debug.Log($"[DataStatePanelPreview] Primary action requested for {_previewState}.");
        }

        private void OnSecondaryActionRequested()
        {
            Debug.Log($"[DataStatePanelPreview] Secondary action requested for {_previewState}.");
        }

        private static string ResolveCustomIconClass(DataStatePanelPreviewIcon icon)
        {
            switch (icon)
            {
                case DataStatePanelPreviewIcon.Info:
                    return "ds-icon--info";
                case DataStatePanelPreviewIcon.Wifi:
                    return "ds-icon--wifi";
                case DataStatePanelPreviewIcon.Error:
                    return "ds-icon--error";
                case DataStatePanelPreviewIcon.Lock:
                    return "ds-icon--lock";
                case DataStatePanelPreviewIcon.Warning:
                    return "ds-icon--warning";
                case DataStatePanelPreviewIcon.Refresh:
                    return "ds-icon--refresh";
                case DataStatePanelPreviewIcon.Search:
                    return "ds-icon--search";
                default:
                    return null;
            }
        }
    }
}
