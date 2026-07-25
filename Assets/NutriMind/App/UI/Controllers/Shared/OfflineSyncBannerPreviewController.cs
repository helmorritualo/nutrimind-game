using UnityEngine;
using UnityEngine.UIElements;

namespace NutriMind.App.UI
{
    /// <summary>
    /// Static preview presets for <see cref="OfflineSyncBannerPreviewController"/>.
    /// </summary>
    public enum OfflineSyncBannerPreviewPreset
    {
        Hidden,
        OfflineCached,
        SyncPending,
        Syncing,
        SyncError,
        BackOnline,
        Custom
    }

    /// <summary>
    /// Validated semantic icon choices for custom OfflineSyncBanner preview.
    /// Maps only to package icon classes confirmed in DesignSystem Icons.uss.
    /// </summary>
    public enum OfflineSyncBannerPreviewIcon
    {
        Default,
        Wifi,
        Sync,
        Error,
        Check,
        Warning,
        Info,
        Refresh
    }

    /// <summary>
    /// Standalone UI Toolkit preview adapter for <see cref="OfflineSyncBannerView"/>.
    /// Presentation only — shows inspector presets and logs action / dismissal / visibility.
    /// Does not perform connectivity detection, networking, SQLite, sync, routing,
    /// authentication, or gameplay state changes.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class OfflineSyncBannerPreviewController : MonoBehaviour
    {
        [SerializeField]
        private OfflineSyncBannerPreviewPreset _previewPreset =
            OfflineSyncBannerPreviewPreset.OfflineCached;

        [SerializeField]
        private bool _showOnEnable = true;

        [SerializeField]
        [Min(0)]
        private int _pendingCount = 3;

        [SerializeField]
        private OfflineSyncBannerState _customState =
            OfflineSyncBannerState.OfflineCached;

        [SerializeField]
        private string _customTitle = "Status update";

        [SerializeField]
        [TextArea]
        private string _customMessage = "NutriMind status information.";

        [SerializeField]
        [TextArea]
        private string _customDetail;

        [SerializeField]
        private string _customActionLabel;

        [SerializeField]
        private bool _customAllowDismiss;

        [SerializeField]
        private bool _customShowSpinner;

        [SerializeField]
        [Tooltip("Optional icon override for Custom preset. Default uses the state fallback.")]
        private OfflineSyncBannerPreviewIcon _customIcon = OfflineSyncBannerPreviewIcon.Default;

        private UIDocument _uiDocument;
        private OfflineSyncBannerView _view;
        private OfflineSyncBannerPreviewPreset? _appliedPreset;
        private int? _appliedPendingCount;
        private OfflineSyncBannerState? _appliedCustomState;
        private OfflineSyncBannerPreviewIcon? _appliedCustomIcon;
        private string _appliedCustomTitle;
        private string _appliedCustomMessage;
        private string _appliedCustomDetail;
        private string _appliedCustomActionLabel;
        private bool? _appliedCustomAllowDismiss;
        private bool? _appliedCustomShowSpinner;
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

            ApplyPreviewIfNeeded(force: true);
        }

        private void Update()
        {
            if (_view == null || !_view.IsBound)
            {
                return;
            }

            ApplyPreviewIfNeeded(force: false);
        }

        /// <summary>
        /// Shows the currently selected preview configuration.
        /// </summary>
        [ContextMenu("Show Offline Sync Banner Preview")]
        public void ShowPreview()
        {
            if (_view == null || !_view.IsBound)
            {
                BindWhenReady();
                if (_view == null || !_view.IsBound)
                {
                    return;
                }
            }

            ShowSelectedConfiguration(forceOpen: true);
        }

        /// <summary>
        /// Hides the preview banner without performing any real sync or connectivity work.
        /// </summary>
        [ContextMenu("Hide Offline Sync Banner Preview")]
        public void HidePreview()
        {
            if (_view == null || !_view.IsBound)
            {
                return;
            }

            _view.Hide();
            CaptureAppliedTracking();
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

            VisualElement componentRoot = panelRoot.name == "offline-sync-banner-root"
                ? panelRoot
                : panelRoot.Q<VisualElement>("offline-sync-banner-root");

            if (componentRoot == null)
            {
                Invoke(nameof(BindWhenReady), 0.05f);
                return;
            }

            panelRoot.style.flexGrow = 1;
            panelRoot.style.width = Length.Percent(100);
            panelRoot.style.height = Length.Percent(100);

            UnbindViewOnly();
            _view = new OfflineSyncBannerView(componentRoot);
            if (!_view.IsBound)
            {
                _view.Dispose();
                _view = null;
                Invoke(nameof(BindWhenReady), 0.05f);
                return;
            }

            RegisterViewEvents();

            if (_showOnEnable)
            {
                ShowSelectedConfiguration(forceOpen: true);
            }
            else
            {
                ResetAppliedTracking();
            }
        }

        private void Unbind()
        {
            UnbindViewOnly();
            _uiDocument = null;
            ResetAppliedTracking();
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

            _view.ActionRequested += OnActionRequested;
            _view.Dismissed += OnDismissed;
            _view.VisibilityChanged += OnVisibilityChanged;
            _eventsRegistered = true;
        }

        private void UnregisterViewEvents()
        {
            if (_view == null || !_eventsRegistered)
            {
                _eventsRegistered = false;
                return;
            }

            _view.ActionRequested -= OnActionRequested;
            _view.Dismissed -= OnDismissed;
            _view.VisibilityChanged -= OnVisibilityChanged;
            _eventsRegistered = false;
        }

        private void ApplyPreviewIfNeeded(bool force)
        {
            if (_view == null || !_view.IsBound)
            {
                return;
            }

            bool presetChanged = force || _appliedPreset != _previewPreset;
            bool pendingChanged = _previewPreset == OfflineSyncBannerPreviewPreset.SyncPending
                && _appliedPendingCount != _pendingCount;
            bool customChanged = force || HasCustomFieldsChanged();

            if (!presetChanged && !pendingChanged && !customChanged)
            {
                return;
            }

            if (_previewPreset == OfflineSyncBannerPreviewPreset.Hidden)
            {
                _view.Hide();
                CaptureAppliedTracking();
                return;
            }

            // Inspector edits reapply only while visible; never force-open a hidden banner.
            if (!_view.IsVisible)
            {
                CaptureAppliedTracking();
                return;
            }

            ShowSelectedConfiguration(forceOpen: false);
        }

        private void ShowSelectedConfiguration(bool forceOpen)
        {
            if (_view == null || !_view.IsBound)
            {
                return;
            }

            if (_previewPreset == OfflineSyncBannerPreviewPreset.Hidden)
            {
                _view.Hide();
                CaptureAppliedTracking();
                return;
            }

            OfflineSyncBannerConfiguration configuration = ResolvePresetConfiguration(_previewPreset);
            if (forceOpen || _view.IsVisible)
            {
                _view.Show(configuration);
            }

            CaptureAppliedTracking();
        }

        private OfflineSyncBannerConfiguration ResolvePresetConfiguration(
            OfflineSyncBannerPreviewPreset preset)
        {
            switch (preset)
            {
                case OfflineSyncBannerPreviewPreset.OfflineCached:
                    return OfflineSyncBannerPresets.OfflineCached();
                case OfflineSyncBannerPreviewPreset.SyncPending:
                    return OfflineSyncBannerPresets.SyncPending(_pendingCount);
                case OfflineSyncBannerPreviewPreset.Syncing:
                    return OfflineSyncBannerPresets.Syncing();
                case OfflineSyncBannerPreviewPreset.SyncError:
                    return OfflineSyncBannerPresets.SyncError();
                case OfflineSyncBannerPreviewPreset.BackOnline:
                    return OfflineSyncBannerPresets.BackOnline();
                case OfflineSyncBannerPreviewPreset.Custom:
                    return BuildCustomConfiguration();
                default:
                    return OfflineSyncBannerPresets.OfflineCached();
            }
        }

        private OfflineSyncBannerConfiguration BuildCustomConfiguration()
        {
            OfflineSyncBannerState state = _customState == OfflineSyncBannerState.Hidden
                ? OfflineSyncBannerState.OfflineCached
                : _customState;

            return new OfflineSyncBannerConfiguration(
                state: state,
                title: _customTitle,
                message: _customMessage,
                detail: string.IsNullOrWhiteSpace(_customDetail) ? null : _customDetail,
                iconClass: ResolveCustomIconClass(_customIcon),
                actionLabel: string.IsNullOrWhiteSpace(_customActionLabel) ? null : _customActionLabel,
                allowDismiss: _customAllowDismiss,
                showSpinner: _customShowSpinner);
        }

        private static string ResolveCustomIconClass(OfflineSyncBannerPreviewIcon icon)
        {
            switch (icon)
            {
                case OfflineSyncBannerPreviewIcon.Wifi:
                    return "ds-icon--wifi";
                case OfflineSyncBannerPreviewIcon.Sync:
                    return "ds-icon--sync";
                case OfflineSyncBannerPreviewIcon.Error:
                    return "ds-icon--error";
                case OfflineSyncBannerPreviewIcon.Check:
                    return "ds-icon--check";
                case OfflineSyncBannerPreviewIcon.Warning:
                    return "ds-icon--warning";
                case OfflineSyncBannerPreviewIcon.Info:
                    return "ds-icon--info";
                case OfflineSyncBannerPreviewIcon.Refresh:
                    return "ds-icon--refresh";
                default:
                    return null;
            }
        }

        private bool HasCustomFieldsChanged()
        {
            if (_previewPreset != OfflineSyncBannerPreviewPreset.Custom)
            {
                return false;
            }

            return _appliedCustomState != _customState
                || _appliedCustomIcon != _customIcon
                || _appliedCustomTitle != _customTitle
                || _appliedCustomMessage != _customMessage
                || _appliedCustomDetail != _customDetail
                || _appliedCustomActionLabel != _customActionLabel
                || _appliedCustomAllowDismiss != _customAllowDismiss
                || _appliedCustomShowSpinner != _customShowSpinner;
        }

        private void CaptureAppliedTracking()
        {
            _appliedPreset = _previewPreset;
            _appliedPendingCount = _pendingCount;
            _appliedCustomState = _customState;
            _appliedCustomIcon = _customIcon;
            _appliedCustomTitle = _customTitle;
            _appliedCustomMessage = _customMessage;
            _appliedCustomDetail = _customDetail;
            _appliedCustomActionLabel = _customActionLabel;
            _appliedCustomAllowDismiss = _customAllowDismiss;
            _appliedCustomShowSpinner = _customShowSpinner;
        }

        private void ResetAppliedTracking()
        {
            _appliedPreset = null;
            _appliedPendingCount = null;
            _appliedCustomState = null;
            _appliedCustomIcon = null;
            _appliedCustomTitle = null;
            _appliedCustomMessage = null;
            _appliedCustomDetail = null;
            _appliedCustomActionLabel = null;
            _appliedCustomAllowDismiss = null;
            _appliedCustomShowSpinner = null;
        }

        private void OnActionRequested()
        {
            Debug.Log($"[OfflineSyncBannerPreview] Action requested for {_previewPreset}.");
        }

        private void OnDismissed()
        {
            Debug.Log($"[OfflineSyncBannerPreview] Dismissed preset: {_previewPreset}.");
        }

        private void OnVisibilityChanged(bool visible)
        {
            Debug.Log(
                visible
                    ? "[OfflineSyncBannerPreview] Visibility changed: visible."
                    : "[OfflineSyncBannerPreview] Visibility changed: hidden.");
        }
    }
}
