using UnityEngine;
using UnityEngine.UIElements;

namespace NutriMind.App.UI
{
    /// <summary>
    /// Static preview presets for <see cref="LoadingOverlayPreviewController"/>.
    /// </summary>
    public enum LoadingOverlayPreviewPreset
    {
        PreparingApplication,
        CheckingSession,
        LoadingProgress,
        PreparingQuiz,
        SubmittingQuiz,
        SavingSettings,
        DownloadingMission,
        ValidatingContent,
        Custom
    }

    /// <summary>
    /// Standalone UI Toolkit preview adapter for <see cref="LoadingOverlayView"/>.
    /// Presentation only — shows inspector presets and logs cancel / visibility requests.
    /// Does not perform networking, SQLite, sync, routing, authentication, downloads,
    /// quiz submit, content validation, or gameplay state changes.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class LoadingOverlayPreviewController : MonoBehaviour
    {
        [SerializeField]
        private LoadingOverlayPreviewPreset _previewPreset =
            LoadingOverlayPreviewPreset.PreparingApplication;

        [SerializeField]
        private bool _showOnEnable = true;

        [SerializeField]
        [Range(0f, 1f)]
        private float _previewProgress = 0.42f;

        [SerializeField]
        private LoadingOverlayMode _customMode = LoadingOverlayMode.Indeterminate;

        [SerializeField]
        private string _customTitle = "Loading...";

        [SerializeField]
        [TextArea]
        private string _customMessage = "Please wait.";

        [SerializeField]
        [TextArea]
        private string _customDetail;

        [SerializeField]
        private string _customProgressLabel = "Progress";

        [SerializeField]
        [Range(0f, 1f)]
        private float _customProgress;

        [SerializeField]
        private bool _customAllowCancel;

        [SerializeField]
        private string _customCancelLabel = "Cancel";

        [SerializeField]
        private bool _customAllowEscapeCancel;

        private UIDocument _uiDocument;
        private LoadingOverlayView _view;
        private LoadingOverlayPreviewPreset? _appliedPreset;
        private float? _appliedPreviewProgress;
        private LoadingOverlayMode? _appliedCustomMode;
        private string _appliedCustomTitle;
        private string _appliedCustomMessage;
        private string _appliedCustomDetail;
        private string _appliedCustomProgressLabel;
        private float? _appliedCustomProgress;
        private bool? _appliedCustomAllowCancel;
        private string _appliedCustomCancelLabel;
        private bool? _appliedCustomAllowEscapeCancel;
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
        [ContextMenu("Show Loading Overlay Preview")]
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
        /// Hides the preview overlay without performing any real cancellation.
        /// </summary>
        [ContextMenu("Hide Loading Overlay Preview")]
        public void HidePreview()
        {
            if (_view == null || !_view.IsBound)
            {
                return;
            }

            _view.Hide();
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

            VisualElement componentRoot = panelRoot.name == "loading-overlay-root"
                ? panelRoot
                : panelRoot.Q<VisualElement>("loading-overlay-root");

            if (componentRoot == null)
            {
                Invoke(nameof(BindWhenReady), 0.05f);
                return;
            }

            panelRoot.style.flexGrow = 1;
            panelRoot.style.width = Length.Percent(100);
            panelRoot.style.height = Length.Percent(100);

            UnbindViewOnly();
            _view = new LoadingOverlayView(componentRoot);
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

            _view.CancelRequested += OnCancelRequested;
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

            _view.CancelRequested -= OnCancelRequested;
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
            bool downloadProgressChanged = _previewPreset == LoadingOverlayPreviewPreset.DownloadingMission
                && _appliedPreset == LoadingOverlayPreviewPreset.DownloadingMission
                && _appliedPreviewProgress != _previewProgress;

            // Determinate mission download: update progress in place without reconstructing the view.
            if (!presetChanged
                && !customChanged
                && downloadProgressChanged
                && _view.IsVisible)
            {
                _view.SetProgress(_previewProgress);
                _appliedPreviewProgress = _previewProgress;
                return;
            }

            if (!presetChanged && !customChanged && !downloadProgressChanged)
            {
                return;
            }

            // Inspector edits reapply only while visible; never force-open a hidden overlay.
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

            LoadingOverlayConfiguration configuration = ResolvePresetConfiguration(_previewPreset);
            if (forceOpen || _view.IsVisible)
            {
                _view.Show(configuration);
            }

            CaptureAppliedTracking();
        }

        private LoadingOverlayConfiguration ResolvePresetConfiguration(LoadingOverlayPreviewPreset preset)
        {
            switch (preset)
            {
                case LoadingOverlayPreviewPreset.CheckingSession:
                    return LoadingOverlayPresets.CheckingSession();
                case LoadingOverlayPreviewPreset.LoadingProgress:
                    return LoadingOverlayPresets.LoadingProgress();
                case LoadingOverlayPreviewPreset.PreparingQuiz:
                    return LoadingOverlayPresets.PreparingQuiz();
                case LoadingOverlayPreviewPreset.SubmittingQuiz:
                    return LoadingOverlayPresets.SubmittingQuiz();
                case LoadingOverlayPreviewPreset.SavingSettings:
                    return LoadingOverlayPresets.SavingSettings();
                case LoadingOverlayPreviewPreset.DownloadingMission:
                    return LoadingOverlayPresets.DownloadingMission(_previewProgress);
                case LoadingOverlayPreviewPreset.ValidatingContent:
                    return LoadingOverlayPresets.ValidatingContent();
                case LoadingOverlayPreviewPreset.Custom:
                    return BuildCustomConfiguration();
                default:
                    return LoadingOverlayPresets.PreparingApplication();
            }
        }

        private LoadingOverlayConfiguration BuildCustomConfiguration()
        {
            return new LoadingOverlayConfiguration(
                title: _customTitle,
                message: _customMessage,
                mode: _customMode,
                detail: string.IsNullOrWhiteSpace(_customDetail) ? null : _customDetail,
                progressLabel: _customProgressLabel,
                progress: _customProgress,
                allowCancel: _customAllowCancel,
                cancelLabel: _customCancelLabel,
                allowEscapeCancel: _customAllowEscapeCancel);
        }

        private bool HasCustomFieldsChanged()
        {
            if (_previewPreset != LoadingOverlayPreviewPreset.Custom)
            {
                return false;
            }

            return _appliedCustomMode != _customMode
                || _appliedCustomTitle != _customTitle
                || _appliedCustomMessage != _customMessage
                || _appliedCustomDetail != _customDetail
                || _appliedCustomProgressLabel != _customProgressLabel
                || _appliedCustomProgress != _customProgress
                || _appliedCustomAllowCancel != _customAllowCancel
                || _appliedCustomCancelLabel != _customCancelLabel
                || _appliedCustomAllowEscapeCancel != _customAllowEscapeCancel;
        }

        private void CaptureAppliedTracking()
        {
            _appliedPreset = _previewPreset;
            _appliedPreviewProgress = _previewProgress;
            _appliedCustomMode = _customMode;
            _appliedCustomTitle = _customTitle;
            _appliedCustomMessage = _customMessage;
            _appliedCustomDetail = _customDetail;
            _appliedCustomProgressLabel = _customProgressLabel;
            _appliedCustomProgress = _customProgress;
            _appliedCustomAllowCancel = _customAllowCancel;
            _appliedCustomCancelLabel = _customCancelLabel;
            _appliedCustomAllowEscapeCancel = _customAllowEscapeCancel;
        }

        private void ResetAppliedTracking()
        {
            _appliedPreset = null;
            _appliedPreviewProgress = null;
            _appliedCustomMode = null;
            _appliedCustomTitle = null;
            _appliedCustomMessage = null;
            _appliedCustomDetail = null;
            _appliedCustomProgressLabel = null;
            _appliedCustomProgress = null;
            _appliedCustomAllowCancel = null;
            _appliedCustomCancelLabel = null;
            _appliedCustomAllowEscapeCancel = null;
        }

        private void OnCancelRequested()
        {
            Debug.Log($"[LoadingOverlayPreview] Cancel requested for {_previewPreset}.");
            _view?.Hide();
        }

        private void OnVisibilityChanged(bool visible)
        {
            Debug.Log(
                visible
                    ? "[LoadingOverlayPreview] Visibility changed: visible."
                    : "[LoadingOverlayPreview] Visibility changed: hidden.");
        }
    }
}
