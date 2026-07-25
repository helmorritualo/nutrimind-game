using UnityEngine;
using UnityEngine.UIElements;

namespace NutriMind.App.UI
{
    /// <summary>
    /// Static preview presets for <see cref="ConfirmDialogPreviewController"/>.
    /// </summary>
    public enum ConfirmDialogPreviewPreset
    {
        SubmitQuiz,
        ExitQuiz,
        SignOut,
        RestoreDefaults,
        ResetTutorial,
        LeaveMission,
        RestartCheckpoint,
        Custom
    }

    /// <summary>
    /// Validated semantic icon choices for custom ConfirmDialog preview.
    /// Maps only to package icon classes confirmed in DesignSystem Icons.uss.
    /// </summary>
    public enum ConfirmDialogPreviewIcon
    {
        Default,
        Help,
        Check,
        Warning,
        Error,
        Info,
        Lock,
        Refresh,
        Close
    }

    /// <summary>
    /// Standalone UI Toolkit preview adapter for <see cref="ConfirmDialogView"/>.
    /// Presentation only — shows inspector presets and logs Confirm / Cancel requests.
    /// Does not perform networking, SQLite, sync, routing, authentication, quiz submit,
    /// settings reset, or gameplay state changes.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class ConfirmDialogPreviewController : MonoBehaviour
    {
        [SerializeField]
        private ConfirmDialogPreviewPreset _previewPreset = ConfirmDialogPreviewPreset.SubmitQuiz;

        [SerializeField]
        private bool _showOnEnable = true;

        [SerializeField]
        private bool _dismissOnBackdrop;

        [SerializeField]
        private ConfirmDialogTone _customTone = ConfirmDialogTone.Neutral;

        [SerializeField]
        private string _customTitle = "Confirm action?";

        [SerializeField]
        [TextArea]
        private string _customMessage = "Please confirm before continuing.";

        [SerializeField]
        [TextArea]
        private string _customDetail;

        [SerializeField]
        private string _customConfirmLabel = "Confirm";

        [SerializeField]
        private string _customCancelLabel = "Cancel";

        [SerializeField]
        [Tooltip("Optional icon override for Custom preset. Default uses the tone icon.")]
        private ConfirmDialogPreviewIcon _customIcon = ConfirmDialogPreviewIcon.Default;

        private UIDocument _uiDocument;
        private ConfirmDialogView _view;
        private ConfirmDialogPreviewPreset? _appliedPreset;
        private bool? _appliedDismissOnBackdrop;
        private ConfirmDialogTone? _appliedCustomTone;
        private ConfirmDialogPreviewIcon? _appliedCustomIcon;
        private string _appliedCustomTitle;
        private string _appliedCustomMessage;
        private string _appliedCustomDetail;
        private string _appliedCustomConfirmLabel;
        private string _appliedCustomCancelLabel;
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
        [ContextMenu("Show Confirm Dialog Preview")]
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

            VisualElement componentRoot = panelRoot.name == "confirm-dialog-root"
                ? panelRoot
                : panelRoot.Q<VisualElement>("confirm-dialog-root");

            if (componentRoot == null)
            {
                Invoke(nameof(BindWhenReady), 0.05f);
                return;
            }

            panelRoot.style.flexGrow = 1;
            panelRoot.style.width = Length.Percent(100);
            panelRoot.style.height = Length.Percent(100);

            UnbindViewOnly();
            _view = new ConfirmDialogView(componentRoot);
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

            _view.Confirmed += OnConfirmed;
            _view.Cancelled += OnCancelled;
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

            _view.Confirmed -= OnConfirmed;
            _view.Cancelled -= OnCancelled;
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
            bool dismissChanged = force || _appliedDismissOnBackdrop != _dismissOnBackdrop;
            bool customChanged = force || HasCustomFieldsChanged();

            if (!presetChanged && !dismissChanged && !customChanged)
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

            ConfirmDialogConfiguration configuration = BuildConfiguration();
            if (forceOpen || _view.IsVisible)
            {
                _view.Show(configuration);
            }

            CaptureAppliedTracking();
        }

        private ConfirmDialogConfiguration BuildConfiguration()
        {
            ConfirmDialogConfiguration baseConfiguration = ResolvePresetConfiguration(_previewPreset);
            if (!_dismissOnBackdrop)
            {
                return baseConfiguration;
            }

            return new ConfirmDialogConfiguration(
                title: baseConfiguration.Title,
                message: baseConfiguration.Message,
                confirmLabel: baseConfiguration.ConfirmLabel,
                cancelLabel: baseConfiguration.CancelLabel,
                detail: baseConfiguration.Detail,
                iconClass: baseConfiguration.IconClass,
                tone: baseConfiguration.Tone,
                dismissOnBackdrop: true);
        }

        private ConfirmDialogConfiguration ResolvePresetConfiguration(ConfirmDialogPreviewPreset preset)
        {
            switch (preset)
            {
                case ConfirmDialogPreviewPreset.ExitQuiz:
                    return ConfirmDialogPresets.ExitQuiz();
                case ConfirmDialogPreviewPreset.SignOut:
                    return ConfirmDialogPresets.SignOut();
                case ConfirmDialogPreviewPreset.RestoreDefaults:
                    return ConfirmDialogPresets.RestoreDefaults();
                case ConfirmDialogPreviewPreset.ResetTutorial:
                    return ConfirmDialogPresets.ResetTutorial();
                case ConfirmDialogPreviewPreset.LeaveMission:
                    return ConfirmDialogPresets.LeaveMission();
                case ConfirmDialogPreviewPreset.RestartCheckpoint:
                    return ConfirmDialogPresets.RestartCheckpoint();
                case ConfirmDialogPreviewPreset.Custom:
                    return BuildCustomConfiguration();
                default:
                    return ConfirmDialogPresets.SubmitQuiz();
            }
        }

        private ConfirmDialogConfiguration BuildCustomConfiguration()
        {
            return new ConfirmDialogConfiguration(
                title: _customTitle,
                message: _customMessage,
                confirmLabel: _customConfirmLabel,
                cancelLabel: _customCancelLabel,
                detail: string.IsNullOrWhiteSpace(_customDetail) ? null : _customDetail,
                iconClass: ResolveCustomIconClass(_customIcon),
                tone: _customTone,
                dismissOnBackdrop: _dismissOnBackdrop);
        }

        private bool HasCustomFieldsChanged()
        {
            if (_previewPreset != ConfirmDialogPreviewPreset.Custom)
            {
                return false;
            }

            return _appliedCustomTone != _customTone
                || _appliedCustomIcon != _customIcon
                || _appliedCustomTitle != _customTitle
                || _appliedCustomMessage != _customMessage
                || _appliedCustomDetail != _customDetail
                || _appliedCustomConfirmLabel != _customConfirmLabel
                || _appliedCustomCancelLabel != _customCancelLabel;
        }

        private void CaptureAppliedTracking()
        {
            _appliedPreset = _previewPreset;
            _appliedDismissOnBackdrop = _dismissOnBackdrop;
            _appliedCustomTone = _customTone;
            _appliedCustomIcon = _customIcon;
            _appliedCustomTitle = _customTitle;
            _appliedCustomMessage = _customMessage;
            _appliedCustomDetail = _customDetail;
            _appliedCustomConfirmLabel = _customConfirmLabel;
            _appliedCustomCancelLabel = _customCancelLabel;
        }

        private void ResetAppliedTracking()
        {
            _appliedPreset = null;
            _appliedDismissOnBackdrop = null;
            _appliedCustomTone = null;
            _appliedCustomIcon = null;
            _appliedCustomTitle = null;
            _appliedCustomMessage = null;
            _appliedCustomDetail = null;
            _appliedCustomConfirmLabel = null;
            _appliedCustomCancelLabel = null;
        }

        private void OnConfirmed()
        {
            Debug.Log($"[ConfirmDialogPreview] Confirmed preset: {_previewPreset}.");
        }

        private void OnCancelled()
        {
            Debug.Log($"[ConfirmDialogPreview] Cancelled preset: {_previewPreset}.");
        }

        private void OnVisibilityChanged(bool visible)
        {
            Debug.Log(
                visible
                    ? "[ConfirmDialogPreview] Visibility changed: visible."
                    : "[ConfirmDialogPreview] Visibility changed: hidden.");
        }

        private static string ResolveCustomIconClass(ConfirmDialogPreviewIcon icon)
        {
            switch (icon)
            {
                case ConfirmDialogPreviewIcon.Help:
                    return "ds-icon--help";
                case ConfirmDialogPreviewIcon.Check:
                    return "ds-icon--check";
                case ConfirmDialogPreviewIcon.Warning:
                    return "ds-icon--warning";
                case ConfirmDialogPreviewIcon.Error:
                    return "ds-icon--error";
                case ConfirmDialogPreviewIcon.Info:
                    return "ds-icon--info";
                case ConfirmDialogPreviewIcon.Lock:
                    return "ds-icon--lock";
                case ConfirmDialogPreviewIcon.Refresh:
                    return "ds-icon--refresh";
                case ConfirmDialogPreviewIcon.Close:
                    return "ds-icon--close";
                default:
                    return null;
            }
        }
    }
}
