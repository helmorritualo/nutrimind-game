using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace NutriMind.App.UI
{
    /// <summary>
    /// Display mode for <see cref="LoadingOverlayView"/>.
    /// Indeterminate uses the package spinner; Determinate uses the package progress bar.
    /// </summary>
    public enum LoadingOverlayMode
    {
        Indeterminate,
        Determinate
    }

    /// <summary>
    /// Immutable presentation configuration for <see cref="LoadingOverlayView"/>.
    /// Progress is normalized from 0f to 1f and converted to ProgressBar 0–100 internally.
    /// Does not include tasks, coroutines, cancellation tokens, network requests,
    /// scene names, API payloads, quiz attempts, download jobs, or database handles.
    /// </summary>
    public readonly struct LoadingOverlayConfiguration
    {
        /// <summary>
        /// Creates a presentation-only loading overlay configuration.
        /// </summary>
        /// <param name="title">Primary title. Blank values fall back to "Loading...".</param>
        /// <param name="message">Supporting message. Blank values fall back to "Please wait.".</param>
        /// <param name="mode">Indeterminate spinner or Determinate progress bar.</param>
        /// <param name="detail">Optional supporting detail. Blank values hide the detail label.</param>
        /// <param name="progressLabel">Determinate progress label. Blank values fall back to "Progress".</param>
        /// <param name="progress">Normalized progress from 0f to 1f (clamped by the view).</param>
        /// <param name="allowCancel">When false, cancel is hidden and Escape is ignored.</param>
        /// <param name="cancelLabel">Cancel button label. Blank values fall back to "Cancel".</param>
        /// <param name="allowEscapeCancel">Escape requests cancel only when cancel is also allowed.</param>
        public LoadingOverlayConfiguration(
            string title,
            string message,
            LoadingOverlayMode mode = LoadingOverlayMode.Indeterminate,
            string detail = null,
            string progressLabel = null,
            float progress = 0f,
            bool allowCancel = false,
            string cancelLabel = "Cancel",
            bool allowEscapeCancel = false)
        {
            Title = title;
            Message = message;
            Mode = mode;
            Detail = detail;
            ProgressLabel = progressLabel;
            Progress = progress;
            AllowCancel = allowCancel;
            CancelLabel = cancelLabel;
            AllowEscapeCancel = allowEscapeCancel;
        }

        public string Title { get; }
        public string Message { get; }
        public string Detail { get; }
        public string ProgressLabel { get; }
        public float Progress { get; }
        public LoadingOverlayMode Mode { get; }
        public bool AllowCancel { get; }
        public string CancelLabel { get; }
        public bool AllowEscapeCancel { get; }
    }

    /// <summary>
    /// Static UI-preview copy presets for common NutriMind blocking load states.
    /// Presentation only — does not perform auth, networking, downloads, quiz submit, or validation.
    /// </summary>
    public static class LoadingOverlayPresets
    {
        public static LoadingOverlayConfiguration PreparingApplication()
        {
            return new LoadingOverlayConfiguration(
                title: "Loading NutriMind...",
                message: "Getting everything ready for you.",
                mode: LoadingOverlayMode.Indeterminate,
                detail: null,
                allowCancel: false);
        }

        public static LoadingOverlayConfiguration CheckingSession()
        {
            return new LoadingOverlayConfiguration(
                title: "Checking your session",
                message: "Please wait while NutriMind prepares your account.",
                mode: LoadingOverlayMode.Indeterminate,
                detail: "Your PIN is not displayed or stored by this screen.",
                allowCancel: false);
        }

        public static LoadingOverlayConfiguration LoadingProgress()
        {
            return new LoadingOverlayConfiguration(
                title: "Loading your progress",
                message: "Preparing your learning summary.",
                mode: LoadingOverlayMode.Indeterminate,
                detail: null,
                allowCancel: true,
                cancelLabel: "Go Back",
                allowEscapeCancel: true);
        }

        public static LoadingOverlayConfiguration PreparingQuiz()
        {
            return new LoadingOverlayConfiguration(
                title: "Preparing your quiz",
                message: "Loading the quiz instructions and questions.",
                mode: LoadingOverlayMode.Indeterminate,
                detail: "No attempt has started in this static preview.",
                allowCancel: true,
                cancelLabel: "Back to Quiz Portal",
                allowEscapeCancel: true);
        }

        public static LoadingOverlayConfiguration SubmittingQuiz()
        {
            return new LoadingOverlayConfiguration(
                title: "Submitting your quiz...",
                message: "Please keep NutriMind open while your answers are being sent.",
                mode: LoadingOverlayMode.Indeterminate,
                detail: "Do not start another submission while this message is visible.",
                allowCancel: false);
        }

        public static LoadingOverlayConfiguration SavingSettings()
        {
            return new LoadingOverlayConfiguration(
                title: "Saving settings",
                message: "Applying your changes on this device.",
                mode: LoadingOverlayMode.Indeterminate,
                detail: "This preview does not contact a server.",
                allowCancel: false);
        }

        public static LoadingOverlayConfiguration DownloadingMission(float progress = 0f)
        {
            return new LoadingOverlayConfiguration(
                title: "Downloading mission",
                message: "Preparing this mission for offline play.",
                mode: LoadingOverlayMode.Determinate,
                detail: "Keep NutriMind open until the download is complete.",
                progressLabel: "Mission content",
                progress: progress,
                allowCancel: true,
                cancelLabel: "Cancel Download",
                allowEscapeCancel: false);
        }

        public static LoadingOverlayConfiguration ValidatingContent()
        {
            return new LoadingOverlayConfiguration(
                title: "Checking mission content",
                message: "Making sure this mission is ready to open.",
                mode: LoadingOverlayMode.Indeterminate,
                detail: "This may take a moment.",
                allowCancel: false);
        }
    }

    /// <summary>
    /// Reusable UI Toolkit blocking loading overlay for App temporary operations.
    /// Not a MonoBehaviour — construct with an already-instantiated component root,
    /// subscribe to <see cref="CancelRequested"/> from the owner, and call
    /// <see cref="Dispose"/> when the host unbinds.
    /// <para>
    /// Future AppShell usage (presentation wiring only):
    /// <code>
    /// appShellController.ShowLoadingOverlay(LoadingOverlayPresets.PreparingQuiz());
    /// appShellController.ShowLoadingOverlay(LoadingOverlayPresets.DownloadingMission(0f));
    /// appShellController.SetLoadingOverlayProgress(0.45f);
    /// appShellController.HideLoadingOverlay();
    /// </code>
    /// Direct local usage is also valid:
    /// <code>
    /// TemplateContainer instance = loadingOverlayAsset.CloneTree();
    /// host.Add(instance);
    /// var loadingOverlay = new LoadingOverlayView(instance);
    /// loadingOverlay.Show(LoadingOverlayPresets.SubmittingQuiz());
    /// // later:
    /// loadingOverlay.Dispose();
    /// instance.RemoveFromHierarchy();
    /// </code>
    /// </para>
    /// Owns its own <c>ds-backdrop</c>. Do not add a second AppShell backdrop for the same overlay.
    /// Does not perform networking, SQLite, sync, routing, authentication, downloads,
    /// quiz submit, scene loading, or gameplay state changes.
    /// Use <see cref="DataStatePanelView"/> loading for in-content loading where chrome stays usable.
    /// Do not use <see cref="SystemDialogView"/> or <see cref="ConfirmDialogView"/> as loaders.
    /// </summary>
    public sealed class LoadingOverlayView : IDisposable
    {
        private const string RootName = "loading-overlay-root";
        private const string HiddenClass = "loading-overlay--hidden";
        private const string IndeterminateClass = "loading-overlay--indeterminate";
        private const string DeterminateClass = "loading-overlay--determinate";
        private const string CompactClass = "loading-overlay--compact";
        private const string NarrowClass = "loading-overlay--narrow";
        private const string MobileClass = "mobile";

        private const string IndeterminateHiddenClass = "loading-overlay__indeterminate--hidden";
        private const string DeterminateHiddenClass = "loading-overlay__determinate--hidden";
        private const string DetailHiddenClass = "loading-overlay__detail--hidden";
        private const string CancelHiddenClass = "loading-overlay__cancel--hidden";

        private const float CompactBreakpoint = 1100f;
        private const float NarrowBreakpoint = 820f;

        private VisualElement _root;
        private VisualElement _backdrop;
        private VisualElement _card;
        private VisualElement _indeterminateRegion;
        private VisualElement _spinner;
        private VisualElement _determinateRegion;
        private Label _progressLabel;
        private Label _progressPercent;
        private ProgressBar _progressBar;
        private Label _title;
        private Label _message;
        private Label _detail;
        private Button _cancelButton;

        private LoadingOverlayConfiguration _configuration;
        private LoadingOverlayMode _mode;
        private bool _isVisible;
        private bool _disposed;
        private bool _isCancelling;
        private float _progress;
        private float _lastWidth = -1f;

        /// <summary>
        /// Raised only when cancellation is enabled and Cancel or allowed Escape is used.
        /// Does not hide the overlay or cancel a real operation — the owner decides.
        /// </summary>
        public event Action CancelRequested;

        /// <summary>
        /// Raised when visibility changes. Argument is true when shown, false when hidden.
        /// </summary>
        public event Action<bool> VisibilityChanged;

        /// <summary>
        /// Creates a view bound to an already-instantiated component root,
        /// a TemplateContainer containing the root, or a local loading host that contains it.
        /// Does not search the entire application panel globally.
        /// </summary>
        public LoadingOverlayView(VisualElement root)
        {
            ResolveRoot(root);
            if (_root == null)
            {
                return;
            }

            CacheElements();
            RegisterCallbacks();
            ApplyResponsiveClasses(_root.resolvedStyle.width);
            SetHiddenInteractionState();
        }

        public VisualElement Root => _root;

        public bool IsBound => _root != null && !_disposed;

        public bool IsVisible => _isVisible && IsBound;

        public LoadingOverlayMode Mode => _mode;

        /// <summary>
        /// Normalized progress from 0f to 1f.
        /// </summary>
        public float Progress => _progress;

        public bool CanCancel => IsBound && _configuration.AllowCancel && _isVisible;

        /// <summary>
        /// Applies configuration and shows the overlay.
        /// Does not auto-hide and does not pretend an operation completed.
        /// </summary>
        public void Show(LoadingOverlayConfiguration configuration)
        {
            if (!IsBound)
            {
                return;
            }

            _configuration = configuration;
            ApplyMode(configuration.Mode);
            ApplyTitle(configuration.Title);
            ApplyMessage(configuration.Message);
            ApplyDetail(configuration.Detail);
            ApplyProgressLabel(configuration.ProgressLabel);
            SetProgressInternal(configuration.Progress, updateVisibleUi: configuration.Mode == LoadingOverlayMode.Determinate);
            ApplyCancel(configuration.AllowCancel, configuration.CancelLabel);
            SetCancelEnabled(configuration.AllowCancel);

            bool wasVisible = _isVisible;
            _root.RemoveFromClassList(HiddenClass);
            _root.pickingMode = PickingMode.Position;
            _isVisible = true;

            if (!wasVisible)
            {
                VisibilityChanged?.Invoke(true);
            }

            if (configuration.AllowCancel)
            {
                _root.schedule.Execute(FocusCancelSafely);
            }
        }

        public void ShowIndeterminate(
            string title,
            string message,
            string detail = null,
            bool allowCancel = false,
            string cancelLabel = "Cancel",
            bool allowEscapeCancel = false)
        {
            Show(new LoadingOverlayConfiguration(
                title: title,
                message: message,
                mode: LoadingOverlayMode.Indeterminate,
                detail: detail,
                allowCancel: allowCancel,
                cancelLabel: cancelLabel,
                allowEscapeCancel: allowEscapeCancel));
        }

        public void ShowDeterminate(
            string title,
            string message,
            float progress,
            string progressLabel = "Progress",
            string detail = null,
            bool allowCancel = false,
            string cancelLabel = "Cancel",
            bool allowEscapeCancel = false)
        {
            Show(new LoadingOverlayConfiguration(
                title: title,
                message: message,
                mode: LoadingOverlayMode.Determinate,
                detail: detail,
                progressLabel: progressLabel,
                progress: progress,
                allowCancel: allowCancel,
                cancelLabel: cancelLabel,
                allowEscapeCancel: allowEscapeCancel));
        }

        /// <summary>
        /// Updates stored normalized progress. Visible progress UI updates only in Determinate mode.
        /// Does not switch modes and does not auto-hide at 100%.
        /// </summary>
        public void SetProgress(float progress)
        {
            if (!IsBound)
            {
                return;
            }

            SetProgressInternal(progress, updateVisibleUi: _mode == LoadingOverlayMode.Determinate);
        }

        /// <summary>
        /// Updates stored normalized progress and the determinate progress label.
        /// Visible progress UI updates only in Determinate mode.
        /// </summary>
        public void SetProgress(float progress, string progressLabel)
        {
            if (!IsBound)
            {
                return;
            }

            ApplyProgressLabel(progressLabel);
            SetProgressInternal(progress, updateVisibleUi: _mode == LoadingOverlayMode.Determinate);
        }

        public void SetTitle(string title)
        {
            if (!IsBound)
            {
                return;
            }

            ApplyTitle(title);
        }

        public void SetMessage(string message)
        {
            if (!IsBound)
            {
                return;
            }

            ApplyMessage(message);
        }

        public void SetDetail(string detail)
        {
            if (!IsBound)
            {
                return;
            }

            ApplyDetail(detail);
        }

        public void SetCancelEnabled(bool enabled)
        {
            if (_cancelButton == null)
            {
                return;
            }

            _cancelButton.SetEnabled(enabled);
        }

        /// <summary>
        /// Hides the overlay without raising <see cref="CancelRequested"/>.
        /// Does not reset progress. Harmless when repeated.
        /// </summary>
        public void Hide()
        {
            if (!IsBound || !_isVisible)
            {
                if (IsBound)
                {
                    SetHiddenInteractionState();
                }

                return;
            }

            SetHiddenInteractionState();
            _isVisible = false;
            VisibilityChanged?.Invoke(false);
        }

        /// <summary>
        /// Raises <see cref="CancelRequested"/> when cancellation is allowed.
        /// Does not automatically hide — the owner may need to acknowledge cancellation.
        /// </summary>
        public void RequestCancel()
        {
            if (!IsBound || !_isVisible || _isCancelling)
            {
                return;
            }

            if (!_configuration.AllowCancel)
            {
                return;
            }

            if (_cancelButton != null && !_cancelButton.enabledSelf)
            {
                return;
            }

            _isCancelling = true;
            try
            {
                CancelRequested?.Invoke();
            }
            finally
            {
                _isCancelling = false;
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            UnregisterCallbacks();
            CancelRequested = null;
            VisibilityChanged = null;
            _root = null;
            _backdrop = null;
            _card = null;
            _indeterminateRegion = null;
            _spinner = null;
            _determinateRegion = null;
            _progressLabel = null;
            _progressPercent = null;
            _progressBar = null;
            _title = null;
            _message = null;
            _detail = null;
            _cancelButton = null;
            _isVisible = false;
            _lastWidth = -1f;
            _isCancelling = false;
            _progress = 0f;
        }

        private void ResolveRoot(VisualElement root)
        {
            if (root == null)
            {
                return;
            }

            if (root.name == RootName)
            {
                _root = root;
                return;
            }

            _root = root.Q<VisualElement>(RootName);
        }

        private void CacheElements()
        {
            _backdrop = _root.Q<VisualElement>("loading-overlay-backdrop");
            _card = _root.Q<VisualElement>("loading-overlay-card");
            _indeterminateRegion = _root.Q<VisualElement>("loading-overlay-indeterminate");
            _spinner = _root.Q<VisualElement>("loading-overlay-spinner");
            _determinateRegion = _root.Q<VisualElement>("loading-overlay-determinate");
            _progressLabel = _root.Q<Label>("loading-overlay-progress-label");
            _progressPercent = _root.Q<Label>("loading-overlay-progress-percent");
            _progressBar = _root.Q<ProgressBar>("loading-overlay-progress");
            _title = _root.Q<Label>("loading-overlay-title");
            _message = _root.Q<Label>("loading-overlay-message");
            _detail = _root.Q<Label>("loading-overlay-detail");
            _cancelButton = _root.Q<Button>("loading-overlay-cancel");

            if (_progressBar != null)
            {
                _progressBar.lowValue = 0f;
                _progressBar.highValue = 100f;
            }
        }

        private void RegisterCallbacks()
        {
            if (_cancelButton != null)
            {
                _cancelButton.clicked += OnCancelClicked;
            }

            _root.RegisterCallback<KeyDownEvent>(OnRootKeyDown);
            _root.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        private void UnregisterCallbacks()
        {
            if (_cancelButton != null)
            {
                _cancelButton.clicked -= OnCancelClicked;
            }

            if (_root != null)
            {
                _root.UnregisterCallback<KeyDownEvent>(OnRootKeyDown);
                _root.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            }
        }

        private void OnCancelClicked()
        {
            RequestCancel();
        }

        private void OnRootKeyDown(KeyDownEvent evt)
        {
            if (!_isVisible)
            {
                return;
            }

            if (evt.keyCode != KeyCode.Escape)
            {
                return;
            }

            if (!_configuration.AllowCancel || !_configuration.AllowEscapeCancel)
            {
                return;
            }

            evt.StopPropagation();
            RequestCancel();
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            ApplyResponsiveClasses(evt.newRect.width);
        }

        private void FocusCancelSafely()
        {
            if (!IsBound || !_isVisible || _cancelButton == null)
            {
                return;
            }

            if (_cancelButton.ClassListContains(CancelHiddenClass)
                || !_cancelButton.enabledSelf
                || !_cancelButton.focusable)
            {
                return;
            }

            _cancelButton.Focus();
        }

        private void SetHiddenInteractionState()
        {
            if (_root == null)
            {
                return;
            }

            _root.AddToClassList(HiddenClass);
            _root.pickingMode = PickingMode.Ignore;
        }

        private void ApplyMode(LoadingOverlayMode mode)
        {
            _mode = mode;

            _root.RemoveFromClassList(IndeterminateClass);
            _root.RemoveFromClassList(DeterminateClass);

            if (mode == LoadingOverlayMode.Determinate)
            {
                _root.AddToClassList(DeterminateClass);
                _indeterminateRegion?.AddToClassList(IndeterminateHiddenClass);
                _determinateRegion?.RemoveFromClassList(DeterminateHiddenClass);
            }
            else
            {
                _root.AddToClassList(IndeterminateClass);
                _indeterminateRegion?.RemoveFromClassList(IndeterminateHiddenClass);
                _determinateRegion?.AddToClassList(DeterminateHiddenClass);
            }
        }

        private void SetProgressInternal(float progress, bool updateVisibleUi)
        {
            _progress = Mathf.Clamp01(progress);

            if (!updateVisibleUi)
            {
                return;
            }

            float percent = _progress * 100f;

            if (_progressBar != null)
            {
                _progressBar.value = percent;
            }

            if (_progressPercent != null)
            {
                _progressPercent.text = $"{Mathf.RoundToInt(percent)}%";
            }
        }

        private void ApplyTitle(string title)
        {
            if (_title == null)
            {
                return;
            }

            string resolved = string.IsNullOrWhiteSpace(title) ? "Loading..." : title.Trim();
            _title.text = resolved;
            _title.tooltip = resolved;
        }

        private void ApplyMessage(string message)
        {
            if (_message == null)
            {
                return;
            }

            string resolved = string.IsNullOrWhiteSpace(message) ? "Please wait." : message.Trim();
            _message.text = resolved;
            _message.tooltip = resolved;
        }

        private void ApplyDetail(string detail)
        {
            if (_detail == null)
            {
                return;
            }

            bool hasDetail = !string.IsNullOrWhiteSpace(detail);
            _detail.text = hasDetail ? detail.Trim() : string.Empty;
            _detail.tooltip = hasDetail ? detail.Trim() : string.Empty;
            _detail.EnableInClassList(DetailHiddenClass, !hasDetail);
        }

        private void ApplyProgressLabel(string progressLabel)
        {
            if (_progressLabel == null)
            {
                return;
            }

            string resolved = string.IsNullOrWhiteSpace(progressLabel) ? "Progress" : progressLabel.Trim();
            _progressLabel.text = resolved;
            _progressLabel.tooltip = resolved;
        }

        private void ApplyCancel(bool allowCancel, string cancelLabel)
        {
            if (_cancelButton == null)
            {
                return;
            }

            if (allowCancel)
            {
                string resolved = ResolveCancelLabel(cancelLabel);
                _cancelButton.text = resolved;
                _cancelButton.tooltip = resolved;
                _cancelButton.RemoveFromClassList(CancelHiddenClass);
                _cancelButton.focusable = true;
            }
            else
            {
                _cancelButton.text = string.Empty;
                _cancelButton.tooltip = string.Empty;
                _cancelButton.AddToClassList(CancelHiddenClass);
                _cancelButton.focusable = false;
            }
        }

        private static string ResolveCancelLabel(string label)
        {
            if (string.IsNullOrWhiteSpace(label))
            {
                return "Cancel";
            }

            return label.Trim();
        }

        private void ApplyResponsiveClasses(float width)
        {
            if (_root == null || float.IsNaN(width) || width <= 0f)
            {
                return;
            }

            if (Mathf.Approximately(width, _lastWidth))
            {
                return;
            }

            _lastWidth = width;

            bool compact = width < CompactBreakpoint;
            bool narrow = width < NarrowBreakpoint;

            _root.EnableInClassList(CompactClass, compact);
            _root.EnableInClassList(NarrowClass, narrow);
            _root.EnableInClassList(MobileClass, narrow);
        }
    }
}
