using UnityEngine;
using UnityEngine.UIElements;

namespace NutriMind.App.UI
{
    /// <summary>
    /// Static preview presets for <see cref="SystemDialogPreviewController"/>.
    /// </summary>
    public enum SystemDialogPreviewPreset
    {
        SessionExpired,
        RequiredUpdate,
        Maintenance,
        OfflineUnavailable,
        LocalSaveFailure,
        ContentValidationFailure,
        MissionUnavailable,
        QuizExpired,
        QuizUnavailable,
        ServerValidationError,
        Custom
    }

    /// <summary>
    /// Validated semantic icon choices for custom SystemDialog preview.
    /// Maps only to package icon classes confirmed in DesignSystem Icons.uss.
    /// </summary>
    public enum SystemDialogPreviewIcon
    {
        Default,
        Info,
        Check,
        Warning,
        Error,
        Wifi,
        Lock,
        Refresh,
        Clock,
        Close
    }

    /// <summary>
    /// Standalone UI Toolkit preview adapter for <see cref="SystemDialogView"/>.
    /// Presentation only — shows inspector presets and logs action / dismissal requests.
    /// Does not perform networking, SQLite, sync, routing, authentication, app-store
    /// navigation, content reload, or gameplay state changes.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class SystemDialogPreviewController : MonoBehaviour
    {
        [SerializeField]
        private SystemDialogPreviewPreset _previewPreset = SystemDialogPreviewPreset.SessionExpired;

        [SerializeField]
        private bool _showOnEnable = true;

        [SerializeField]
        private SystemDialogTone _customTone = SystemDialogTone.Information;

        [SerializeField]
        private string _customEyebrow = "NutriMind";

        [SerializeField]
        private string _customTitle = "System message";

        [SerializeField]
        [TextArea]
        private string _customMessage = "Important information from NutriMind.";

        [SerializeField]
        [TextArea]
        private string _customDetail;

        [SerializeField]
        private string _customReferenceText;

        [SerializeField]
        private string _customPrimaryActionLabel = "OK";

        [SerializeField]
        private string _customSecondaryActionLabel;

        [SerializeField]
        private bool _customAllowDismiss;

        [SerializeField]
        private bool _customDismissOnBackdrop;

        [SerializeField]
        private bool _customHideAfterPrimaryAction = true;

        [SerializeField]
        private bool _customHideAfterSecondaryAction = true;

        [SerializeField]
        [Tooltip("Optional icon override for Custom preset. Default uses the tone icon.")]
        private SystemDialogPreviewIcon _customIcon = SystemDialogPreviewIcon.Default;

        private UIDocument _uiDocument;
        private SystemDialogView _view;
        private SystemDialogPreviewPreset? _appliedPreset;
        private SystemDialogTone? _appliedCustomTone;
        private SystemDialogPreviewIcon? _appliedCustomIcon;
        private string _appliedCustomEyebrow;
        private string _appliedCustomTitle;
        private string _appliedCustomMessage;
        private string _appliedCustomDetail;
        private string _appliedCustomReferenceText;
        private string _appliedCustomPrimaryActionLabel;
        private string _appliedCustomSecondaryActionLabel;
        private bool? _appliedCustomAllowDismiss;
        private bool? _appliedCustomDismissOnBackdrop;
        private bool? _appliedCustomHideAfterPrimaryAction;
        private bool? _appliedCustomHideAfterSecondaryAction;
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
        [ContextMenu("Show System Dialog Preview")]
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

            VisualElement componentRoot = panelRoot.name == "system-dialog-root"
                ? panelRoot
                : panelRoot.Q<VisualElement>("system-dialog-root");

            if (componentRoot == null)
            {
                Invoke(nameof(BindWhenReady), 0.05f);
                return;
            }

            panelRoot.style.flexGrow = 1;
            panelRoot.style.width = Length.Percent(100);
            panelRoot.style.height = Length.Percent(100);

            UnbindViewOnly();
            _view = new SystemDialogView(componentRoot);
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

            _view.PrimaryActionRequested += OnPrimaryActionRequested;
            _view.SecondaryActionRequested += OnSecondaryActionRequested;
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

            _view.PrimaryActionRequested -= OnPrimaryActionRequested;
            _view.SecondaryActionRequested -= OnSecondaryActionRequested;
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
            bool customChanged = force || HasCustomFieldsChanged();

            if (!presetChanged && !customChanged)
            {
                return;
            }

            // Inspector edits reapply only while visible; never force-open a hidden dialog.
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

            SystemDialogConfiguration configuration = ResolvePresetConfiguration(_previewPreset);
            if (forceOpen || _view.IsVisible)
            {
                _view.Show(configuration);
            }

            CaptureAppliedTracking();
        }

        private SystemDialogConfiguration ResolvePresetConfiguration(SystemDialogPreviewPreset preset)
        {
            switch (preset)
            {
                case SystemDialogPreviewPreset.RequiredUpdate:
                    return SystemDialogPresets.RequiredUpdate();
                case SystemDialogPreviewPreset.Maintenance:
                    return SystemDialogPresets.Maintenance();
                case SystemDialogPreviewPreset.OfflineUnavailable:
                    return SystemDialogPresets.OfflineUnavailable();
                case SystemDialogPreviewPreset.LocalSaveFailure:
                    return SystemDialogPresets.LocalSaveFailure();
                case SystemDialogPreviewPreset.ContentValidationFailure:
                    return SystemDialogPresets.ContentValidationFailure();
                case SystemDialogPreviewPreset.MissionUnavailable:
                    return SystemDialogPresets.MissionUnavailable();
                case SystemDialogPreviewPreset.QuizExpired:
                    return SystemDialogPresets.QuizExpired();
                case SystemDialogPreviewPreset.QuizUnavailable:
                    return SystemDialogPresets.QuizUnavailable();
                case SystemDialogPreviewPreset.ServerValidationError:
                    return SystemDialogPresets.ServerValidationError();
                case SystemDialogPreviewPreset.Custom:
                    return BuildCustomConfiguration();
                default:
                    return SystemDialogPresets.SessionExpired();
            }
        }

        private SystemDialogConfiguration BuildCustomConfiguration()
        {
            return new SystemDialogConfiguration(
                title: _customTitle,
                message: _customMessage,
                primaryActionLabel: _customPrimaryActionLabel,
                secondaryActionLabel: string.IsNullOrWhiteSpace(_customSecondaryActionLabel)
                    ? null
                    : _customSecondaryActionLabel,
                detail: string.IsNullOrWhiteSpace(_customDetail) ? null : _customDetail,
                referenceText: string.IsNullOrWhiteSpace(_customReferenceText) ? null : _customReferenceText,
                eyebrow: _customEyebrow,
                iconClass: ResolveCustomIconClass(_customIcon),
                tone: _customTone,
                allowDismiss: _customAllowDismiss,
                dismissOnBackdrop: _customDismissOnBackdrop,
                hideAfterPrimaryAction: _customHideAfterPrimaryAction,
                hideAfterSecondaryAction: _customHideAfterSecondaryAction);
        }

        private bool HasCustomFieldsChanged()
        {
            if (_previewPreset != SystemDialogPreviewPreset.Custom)
            {
                return false;
            }

            return _appliedCustomTone != _customTone
                || _appliedCustomIcon != _customIcon
                || _appliedCustomEyebrow != _customEyebrow
                || _appliedCustomTitle != _customTitle
                || _appliedCustomMessage != _customMessage
                || _appliedCustomDetail != _customDetail
                || _appliedCustomReferenceText != _customReferenceText
                || _appliedCustomPrimaryActionLabel != _customPrimaryActionLabel
                || _appliedCustomSecondaryActionLabel != _customSecondaryActionLabel
                || _appliedCustomAllowDismiss != _customAllowDismiss
                || _appliedCustomDismissOnBackdrop != _customDismissOnBackdrop
                || _appliedCustomHideAfterPrimaryAction != _customHideAfterPrimaryAction
                || _appliedCustomHideAfterSecondaryAction != _customHideAfterSecondaryAction;
        }

        private void CaptureAppliedTracking()
        {
            _appliedPreset = _previewPreset;
            _appliedCustomTone = _customTone;
            _appliedCustomIcon = _customIcon;
            _appliedCustomEyebrow = _customEyebrow;
            _appliedCustomTitle = _customTitle;
            _appliedCustomMessage = _customMessage;
            _appliedCustomDetail = _customDetail;
            _appliedCustomReferenceText = _customReferenceText;
            _appliedCustomPrimaryActionLabel = _customPrimaryActionLabel;
            _appliedCustomSecondaryActionLabel = _customSecondaryActionLabel;
            _appliedCustomAllowDismiss = _customAllowDismiss;
            _appliedCustomDismissOnBackdrop = _customDismissOnBackdrop;
            _appliedCustomHideAfterPrimaryAction = _customHideAfterPrimaryAction;
            _appliedCustomHideAfterSecondaryAction = _customHideAfterSecondaryAction;
        }

        private void ResetAppliedTracking()
        {
            _appliedPreset = null;
            _appliedCustomTone = null;
            _appliedCustomIcon = null;
            _appliedCustomEyebrow = null;
            _appliedCustomTitle = null;
            _appliedCustomMessage = null;
            _appliedCustomDetail = null;
            _appliedCustomReferenceText = null;
            _appliedCustomPrimaryActionLabel = null;
            _appliedCustomSecondaryActionLabel = null;
            _appliedCustomAllowDismiss = null;
            _appliedCustomDismissOnBackdrop = null;
            _appliedCustomHideAfterPrimaryAction = null;
            _appliedCustomHideAfterSecondaryAction = null;
        }

        private void OnPrimaryActionRequested()
        {
            Debug.Log($"[SystemDialogPreview] Primary action requested for {_previewPreset}.");
        }

        private void OnSecondaryActionRequested()
        {
            Debug.Log($"[SystemDialogPreview] Secondary action requested for {_previewPreset}.");
        }

        private void OnDismissed()
        {
            Debug.Log($"[SystemDialogPreview] Dismissed preset: {_previewPreset}.");
        }

        private void OnVisibilityChanged(bool visible)
        {
            Debug.Log(
                visible
                    ? "[SystemDialogPreview] Visibility changed: visible."
                    : "[SystemDialogPreview] Visibility changed: hidden.");
        }

        private static string ResolveCustomIconClass(SystemDialogPreviewIcon icon)
        {
            switch (icon)
            {
                case SystemDialogPreviewIcon.Info:
                    return "ds-icon--info";
                case SystemDialogPreviewIcon.Check:
                    return "ds-icon--check";
                case SystemDialogPreviewIcon.Warning:
                    return "ds-icon--warning";
                case SystemDialogPreviewIcon.Error:
                    return "ds-icon--error";
                case SystemDialogPreviewIcon.Wifi:
                    return "ds-icon--wifi";
                case SystemDialogPreviewIcon.Lock:
                    return "ds-icon--lock";
                case SystemDialogPreviewIcon.Refresh:
                    return "ds-icon--refresh";
                case SystemDialogPreviewIcon.Clock:
                    return "ds-icon--clock";
                case SystemDialogPreviewIcon.Close:
                    return "ds-icon--close";
                default:
                    return null;
            }
        }
    }
}
