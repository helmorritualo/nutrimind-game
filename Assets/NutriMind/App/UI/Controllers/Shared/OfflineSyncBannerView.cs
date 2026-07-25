using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace NutriMind.App.UI
{
    /// <summary>
    /// Presentation states for <see cref="OfflineSyncBannerView"/>.
    /// UI-only — does not represent real connectivity or synchronization.
    /// </summary>
    public enum OfflineSyncBannerState
    {
        Hidden,
        OfflineCached,
        SyncPending,
        Syncing,
        SyncError,
        BackOnline
    }

    /// <summary>
    /// Immutable presentation configuration for <see cref="OfflineSyncBannerView"/>.
    /// Does not include network status, sync queues, HTTP results, retry delegates,
    /// API payloads, learner records, SQLite handles, or cancellation tokens.
    /// </summary>
    public readonly struct OfflineSyncBannerConfiguration
    {
        /// <summary>
        /// Creates a presentation-only offline/sync banner configuration.
        /// </summary>
        /// <param name="state">Visible banner state. Hidden is applied via <see cref="OfflineSyncBannerView.Hide"/>.</param>
        /// <param name="title">Primary status title.</param>
        /// <param name="message">Concise supporting message.</param>
        /// <param name="detail">Optional weaker detail. Blank values hide the detail label.</param>
        /// <param name="iconClass">Optional semantic package icon class. Invalid values fall back by state.</param>
        /// <param name="actionLabel">Optional action label. Blank values hide the action button.</param>
        /// <param name="allowDismiss">When true, shows the dismiss button.</param>
        /// <param name="showSpinner">When true, shows the package spinner and hides the semantic icon.</param>
        public OfflineSyncBannerConfiguration(
            OfflineSyncBannerState state,
            string title,
            string message,
            string detail = null,
            string iconClass = null,
            string actionLabel = null,
            bool allowDismiss = false,
            bool showSpinner = false)
        {
            State = state;
            Title = title;
            Message = message;
            Detail = detail;
            IconClass = iconClass;
            ActionLabel = actionLabel;
            AllowDismiss = allowDismiss;
            ShowSpinner = showSpinner;
        }

        public OfflineSyncBannerState State { get; }
        public string Title { get; }
        public string Message { get; }
        public string Detail { get; }
        public string IconClass { get; }
        public string ActionLabel { get; }
        public bool AllowDismiss { get; }
        public bool ShowSpinner { get; }
    }

    /// <summary>
    /// Static UI-preview copy presets for common NutriMind offline and sync banner states.
    /// Presentation only — does not detect connectivity or perform synchronization.
    /// </summary>
    public static class OfflineSyncBannerPresets
    {
        public static OfflineSyncBannerConfiguration OfflineCached()
        {
            return new OfflineSyncBannerConfiguration(
                state: OfflineSyncBannerState.OfflineCached,
                title: "You are offline",
                message: "Showing downloaded progress from this device.",
                detail: "New progress will wait on this device until you reconnect.",
                iconClass: "ds-icon--wifi",
                actionLabel: "Retry Connection",
                allowDismiss: false,
                showSpinner: false);
        }

        public static OfflineSyncBannerConfiguration SyncPending(int pendingCount = 3)
        {
            string title;
            if (pendingCount < 0)
            {
                title = "Updates waiting to sync";
            }
            else if (pendingCount == 1)
            {
                title = "1 update waiting to sync";
            }
            else
            {
                title = $"{pendingCount} updates waiting to sync";
            }

            return new OfflineSyncBannerConfiguration(
                state: OfflineSyncBannerState.SyncPending,
                title: title,
                message: "Your progress is saved on this device.",
                detail: "It will sync when an internet connection is available.",
                iconClass: "ds-icon--sync",
                actionLabel: "Sync Now",
                allowDismiss: true,
                showSpinner: false);
        }

        public static OfflineSyncBannerConfiguration Syncing()
        {
            return new OfflineSyncBannerConfiguration(
                state: OfflineSyncBannerState.Syncing,
                title: "Syncing your progress",
                message: "Sending saved updates to NutriMind.",
                detail: "Keep the application open until this status changes.",
                iconClass: null,
                actionLabel: null,
                allowDismiss: false,
                showSpinner: true);
        }

        public static OfflineSyncBannerConfiguration SyncError()
        {
            return new OfflineSyncBannerConfiguration(
                state: OfflineSyncBannerState.SyncError,
                title: "Progress needs attention",
                message: "Some saved updates could not sync.",
                detail: "Your local progress remains on this device. Try again when your connection is stable.",
                iconClass: "ds-icon--error",
                actionLabel: "Retry Sync",
                allowDismiss: true,
                showSpinner: false);
        }

        public static OfflineSyncBannerConfiguration BackOnline()
        {
            return new OfflineSyncBannerConfiguration(
                state: OfflineSyncBannerState.BackOnline,
                title: "You are back online",
                message: "NutriMind can connect again.",
                detail: "Saved updates can now be synchronized.",
                iconClass: "ds-icon--check",
                actionLabel: null,
                allowDismiss: true,
                showSpinner: false);
        }
    }

    /// <summary>
    /// Reusable UI Toolkit non-modal offline / sync status banner for App shell chrome.
    /// Not a MonoBehaviour — construct with an already-instantiated component root,
    /// subscribe to <see cref="ActionRequested"/> / <see cref="Dismissed"/> from the owner,
    /// and call <see cref="Dispose"/> when the host unbinds.
    /// <para>
    /// Future AppShell usage (presentation wiring only):
    /// <code>
    /// appShellController.SetConnectionPreview(AppShellConnectionPreview.Offline);
    /// appShellController.ShowOfflineSyncBanner(OfflineSyncBannerPresets.Syncing());
    /// appShellController.HideOfflineSyncBanner();
    /// </code>
    /// Direct local usage is also valid:
    /// <code>
    /// TemplateContainer instance = bannerAsset.CloneTree();
    /// host.Add(instance);
    /// var banner = new OfflineSyncBannerView(instance);
    /// banner.ActionRequested += OnActionRequested;
    /// banner.Show(OfflineSyncBannerPresets.SyncError());
    /// // later:
    /// banner.ActionRequested -= OnActionRequested;
    /// banner.Dispose();
    /// instance.RemoveFromHierarchy();
    /// </code>
    /// </para>
    /// Does not perform connectivity detection, networking, SQLite, sync, routing,
    /// authentication, or gameplay state changes.
    /// Use <see cref="DataStatePanelView"/> for in-content data states.
    /// Use <see cref="SystemDialogView"/> for blocking system interruptions.
    /// Use <see cref="LoadingOverlayView"/> for blocking operations.
    /// </summary>
    public sealed class OfflineSyncBannerView : IDisposable
    {
        private const string RootName = "offline-sync-banner-root";

        private const string HiddenClass = "offline-sync-banner--hidden";
        private const string OfflineCachedClass = "offline-sync-banner--offline-cached";
        private const string SyncPendingClass = "offline-sync-banner--sync-pending";
        private const string SyncingClass = "offline-sync-banner--syncing";
        private const string SyncErrorClass = "offline-sync-banner--sync-error";
        private const string BackOnlineClass = "offline-sync-banner--back-online";

        private const string CompactClass = "offline-sync-banner--compact";
        private const string NarrowClass = "offline-sync-banner--narrow";
        private const string MobileClass = "mobile";

        private const string IconHiddenClass = "offline-sync-banner__icon--hidden";
        private const string SpinnerHiddenClass = "offline-sync-banner__spinner--hidden";
        private const string DetailHiddenClass = "offline-sync-banner__detail--hidden";
        private const string ActionHiddenClass = "offline-sync-banner__action--hidden";
        private const string DismissHiddenClass = "offline-sync-banner__dismiss--hidden";

        private const float CompactBreakpoint = 1100f;
        private const float NarrowBreakpoint = 820f;

        private static readonly string[] StateClasses =
        {
            OfflineCachedClass,
            SyncPendingClass,
            SyncingClass,
            SyncErrorClass,
            BackOnlineClass
        };

        private static readonly string[] SemanticIconClasses =
        {
            "ds-icon--wifi",
            "ds-icon--sync",
            "ds-icon--error",
            "ds-icon--check",
            "ds-icon--warning",
            "ds-icon--info",
            "ds-icon--refresh",
            "ds-icon--close"
        };

        private VisualElement _root;
        private VisualElement _surface;
        private VisualElement _iconBackground;
        private VisualElement _icon;
        private VisualElement _spinner;
        private Label _title;
        private Label _message;
        private Label _detail;
        private VisualElement _actions;
        private Button _actionButton;
        private Button _dismissButton;

        private OfflineSyncBannerConfiguration _configuration;
        private OfflineSyncBannerState _state;
        private bool _isVisible;
        private bool _disposed;
        private bool _isRequestingAction;
        private bool _isDismissing;
        private float _lastWidth = -1f;

        /// <summary>
        /// Raised when the optional action button is clicked.
        /// Does not sync, retry networking, hide, or change state — the owner decides.
        /// </summary>
        public event Action ActionRequested;

        /// <summary>
        /// Raised only when dismissal is allowed and the close button is used.
        /// The view hides after raising this event.
        /// </summary>
        public event Action Dismissed;

        /// <summary>
        /// Raised when visibility changes. Argument is true when shown, false when hidden.
        /// </summary>
        public event Action<bool> VisibilityChanged;

        /// <summary>
        /// Creates a view bound to an already-instantiated component root,
        /// a TemplateContainer containing the root, or an AppShell banner host that contains it.
        /// Does not search the entire application panel globally.
        /// </summary>
        public OfflineSyncBannerView(VisualElement root)
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
            _state = OfflineSyncBannerState.Hidden;
        }

        public VisualElement Root => _root;

        public bool IsBound => _root != null && !_disposed;

        public bool IsVisible => _isVisible && IsBound;

        public OfflineSyncBannerState State => _state;

        public bool HasAction =>
            IsBound
            && _isVisible
            && _actionButton != null
            && !_actionButton.ClassListContains(ActionHiddenClass);

        public bool CanDismiss =>
            IsBound
            && _isVisible
            && _configuration.AllowDismiss
            && _dismissButton != null
            && !_dismissButton.ClassListContains(DismissHiddenClass);

        /// <summary>
        /// Applies the state's default preset. Hidden hides the banner.
        /// Does not start timers.
        /// </summary>
        public void SetState(OfflineSyncBannerState state)
        {
            if (!IsBound)
            {
                return;
            }

            if (state == OfflineSyncBannerState.Hidden)
            {
                Hide();
                return;
            }

            Show(GetDefaultConfiguration(state));
        }

        /// <summary>
        /// Applies configuration and shows the banner.
        /// Does not auto-hide and does not perform networking or sync.
        /// </summary>
        public void Show(OfflineSyncBannerConfiguration configuration)
        {
            if (!IsBound)
            {
                return;
            }

            if (configuration.State == OfflineSyncBannerState.Hidden)
            {
                Hide();
                return;
            }

            _configuration = configuration;
            _state = configuration.State;

            ApplyStateClass(configuration.State);
            ApplyTitle(configuration.Title);
            ApplyMessage(configuration.Message);
            ApplyDetail(configuration.Detail);
            ApplySpinnerAndIcon(configuration.ShowSpinner, configuration.IconClass, configuration.State);
            ApplyAction(configuration.ActionLabel);
            ApplyDismiss(configuration.AllowDismiss);
            SetActionEnabled(true);

            bool wasVisible = _isVisible;
            _root.RemoveFromClassList(HiddenClass);
            _root.style.display = DisplayStyle.Flex;
            _root.visible = true;
            _root.pickingMode = PickingMode.Ignore;
            if (_surface != null)
            {
                _surface.pickingMode = PickingMode.Ignore;
            }

            _isVisible = true;

            if (!wasVisible)
            {
                VisibilityChanged?.Invoke(true);
            }
        }

        /// <summary>
        /// Updates copy and controls while preserving the current visible state.
        /// No-op when hidden or unbound.
        /// </summary>
        public void Configure(
            string title,
            string message,
            string detail = null,
            string iconClass = null,
            string actionLabel = null,
            bool allowDismiss = false,
            bool showSpinner = false)
        {
            if (!IsBound || !_isVisible || _state == OfflineSyncBannerState.Hidden)
            {
                return;
            }

            Show(new OfflineSyncBannerConfiguration(
                state: _state,
                title: title,
                message: message,
                detail: detail,
                iconClass: iconClass,
                actionLabel: actionLabel,
                allowDismiss: allowDismiss,
                showSpinner: showSpinner));
        }

        public void SetActionEnabled(bool enabled)
        {
            if (_actionButton == null)
            {
                return;
            }

            _actionButton.SetEnabled(enabled);
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

        /// <summary>
        /// Hides the banner without raising <see cref="Dismissed"/> or <see cref="ActionRequested"/>.
        /// Harmless when repeated.
        /// </summary>
        public void Hide()
        {
            if (!IsBound)
            {
                return;
            }

            if (!_isVisible)
            {
                SetHiddenInteractionState();
                _state = OfflineSyncBannerState.Hidden;
                return;
            }

            SetHiddenInteractionState();
            _state = OfflineSyncBannerState.Hidden;
            _isVisible = false;
            VisibilityChanged?.Invoke(false);
        }

        /// <summary>
        /// Raises <see cref="ActionRequested"/> when an action is present and enabled.
        /// Keeps the banner visible. Does not sync or switch to Syncing.
        /// </summary>
        public void RequestAction()
        {
            if (!IsBound || !_isVisible || _isRequestingAction)
            {
                return;
            }

            if (_actionButton == null || _actionButton.ClassListContains(ActionHiddenClass))
            {
                return;
            }

            if (!_actionButton.enabledSelf)
            {
                return;
            }

            _isRequestingAction = true;
            try
            {
                ActionRequested?.Invoke();
            }
            finally
            {
                _isRequestingAction = false;
            }
        }

        /// <summary>
        /// Raises <see cref="Dismissed"/> when dismissal is allowed, then hides.
        /// </summary>
        public void Dismiss()
        {
            if (!IsBound || !_isVisible || _isDismissing)
            {
                return;
            }

            if (!_configuration.AllowDismiss)
            {
                return;
            }

            _isDismissing = true;
            try
            {
                Dismissed?.Invoke();
                Hide();
            }
            finally
            {
                _isDismissing = false;
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
            ActionRequested = null;
            Dismissed = null;
            VisibilityChanged = null;
            _root = null;
            _surface = null;
            _iconBackground = null;
            _icon = null;
            _spinner = null;
            _title = null;
            _message = null;
            _detail = null;
            _actions = null;
            _actionButton = null;
            _dismissButton = null;
            _isVisible = false;
            _state = OfflineSyncBannerState.Hidden;
            _lastWidth = -1f;
            _isRequestingAction = false;
            _isDismissing = false;
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
            _surface = _root.Q<VisualElement>("offline-sync-banner-surface");
            _iconBackground = _root.Q<VisualElement>("offline-sync-banner-icon-background");
            _icon = _root.Q<VisualElement>("offline-sync-banner-icon");
            _spinner = _root.Q<VisualElement>("offline-sync-banner-spinner");
            _title = _root.Q<Label>("offline-sync-banner-title");
            _message = _root.Q<Label>("offline-sync-banner-message");
            _detail = _root.Q<Label>("offline-sync-banner-detail");
            _actions = _root.Q<VisualElement>("offline-sync-banner-actions");
            _actionButton = _root.Q<Button>("offline-sync-banner-action");
            _dismissButton = _root.Q<Button>("offline-sync-banner-dismiss");
        }

        private void RegisterCallbacks()
        {
            if (_actionButton != null)
            {
                _actionButton.clicked += OnActionClicked;
            }

            if (_dismissButton != null)
            {
                _dismissButton.clicked += OnDismissClicked;
            }

            _root.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        private void UnregisterCallbacks()
        {
            if (_actionButton != null)
            {
                _actionButton.clicked -= OnActionClicked;
            }

            if (_dismissButton != null)
            {
                _dismissButton.clicked -= OnDismissClicked;
            }

            if (_root != null)
            {
                _root.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            }
        }

        private void OnActionClicked()
        {
            RequestAction();
        }

        private void OnDismissClicked()
        {
            Dismiss();
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            ApplyResponsiveClasses(evt.newRect.width);
        }

        private void SetHiddenInteractionState()
        {
            if (_root == null)
            {
                return;
            }

            ClearStateClasses();
            _root.AddToClassList(HiddenClass);
            _root.style.display = DisplayStyle.None;
            _root.visible = false;
            _root.pickingMode = PickingMode.Ignore;

            if (_actionButton != null)
            {
                _actionButton.focusable = false;
            }

            if (_dismissButton != null)
            {
                _dismissButton.focusable = false;
            }
        }

        private void ApplyStateClass(OfflineSyncBannerState state)
        {
            ClearStateClasses();
            _root.RemoveFromClassList(HiddenClass);

            string stateClass = GetStateClass(state);
            if (!string.IsNullOrEmpty(stateClass))
            {
                _root.AddToClassList(stateClass);
            }
        }

        private void ClearStateClasses()
        {
            for (int i = 0; i < StateClasses.Length; i++)
            {
                _root.RemoveFromClassList(StateClasses[i]);
            }
        }

        private static string GetStateClass(OfflineSyncBannerState state)
        {
            switch (state)
            {
                case OfflineSyncBannerState.OfflineCached:
                    return OfflineCachedClass;
                case OfflineSyncBannerState.SyncPending:
                    return SyncPendingClass;
                case OfflineSyncBannerState.Syncing:
                    return SyncingClass;
                case OfflineSyncBannerState.SyncError:
                    return SyncErrorClass;
                case OfflineSyncBannerState.BackOnline:
                    return BackOnlineClass;
                default:
                    return null;
            }
        }

        private static OfflineSyncBannerConfiguration GetDefaultConfiguration(OfflineSyncBannerState state)
        {
            switch (state)
            {
                case OfflineSyncBannerState.OfflineCached:
                    return OfflineSyncBannerPresets.OfflineCached();
                case OfflineSyncBannerState.SyncPending:
                    return OfflineSyncBannerPresets.SyncPending();
                case OfflineSyncBannerState.Syncing:
                    return OfflineSyncBannerPresets.Syncing();
                case OfflineSyncBannerState.SyncError:
                    return OfflineSyncBannerPresets.SyncError();
                case OfflineSyncBannerState.BackOnline:
                    return OfflineSyncBannerPresets.BackOnline();
                default:
                    return OfflineSyncBannerPresets.OfflineCached();
            }
        }

        private void ApplyTitle(string title)
        {
            if (_title == null)
            {
                return;
            }

            string resolved = string.IsNullOrWhiteSpace(title) ? string.Empty : title.Trim();
            _title.text = resolved;
            _title.tooltip = resolved;
        }

        private void ApplyMessage(string message)
        {
            if (_message == null)
            {
                return;
            }

            string resolved = string.IsNullOrWhiteSpace(message) ? string.Empty : message.Trim();
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
            string resolved = hasDetail ? detail.Trim() : string.Empty;
            _detail.text = resolved;
            _detail.tooltip = resolved;
            _detail.EnableInClassList(DetailHiddenClass, !hasDetail);
        }

        private void ApplySpinnerAndIcon(bool showSpinner, string iconClass, OfflineSyncBannerState state)
        {
            if (showSpinner)
            {
                _spinner?.RemoveFromClassList(SpinnerHiddenClass);
                _icon?.AddToClassList(IconHiddenClass);
                return;
            }

            _spinner?.AddToClassList(SpinnerHiddenClass);
            _icon?.RemoveFromClassList(IconHiddenClass);
            ApplySemanticIcon(iconClass, state);
        }

        private void ApplySemanticIcon(string iconClass, OfflineSyncBannerState state)
        {
            if (_icon == null)
            {
                return;
            }

            string resolved = ResolveIconClass(iconClass, state);
            for (int i = 0; i < SemanticIconClasses.Length; i++)
            {
                _icon.RemoveFromClassList(SemanticIconClasses[i]);
            }

            if (!string.IsNullOrEmpty(resolved))
            {
                _icon.AddToClassList(resolved);
            }
        }

        private string ResolveIconClass(string iconClass, OfflineSyncBannerState state)
        {
            if (!string.IsNullOrWhiteSpace(iconClass))
            {
                string trimmed = iconClass.Trim();
                if (IsAllowedSemanticIcon(trimmed))
                {
                    return trimmed;
                }

                Debug.LogWarning(
                    $"[OfflineSyncBannerView] Ignored unsupported icon class '{trimmed}'. " +
                    $"Using fallback for {state}.");
            }

            return GetFallbackIconClass(state);
        }

        private static string GetFallbackIconClass(OfflineSyncBannerState state)
        {
            switch (state)
            {
                case OfflineSyncBannerState.OfflineCached:
                    return "ds-icon--wifi";
                case OfflineSyncBannerState.SyncPending:
                    return "ds-icon--sync";
                case OfflineSyncBannerState.SyncError:
                    return "ds-icon--error";
                case OfflineSyncBannerState.BackOnline:
                    return "ds-icon--check";
                case OfflineSyncBannerState.Syncing:
                    return null;
                default:
                    return "ds-icon--info";
            }
        }

        private static bool IsAllowedSemanticIcon(string iconClass)
        {
            for (int i = 0; i < SemanticIconClasses.Length; i++)
            {
                if (SemanticIconClasses[i] == iconClass)
                {
                    return true;
                }
            }

            return false;
        }

        private void ApplyAction(string actionLabel)
        {
            if (_actionButton == null)
            {
                return;
            }

            bool hasAction = !string.IsNullOrWhiteSpace(actionLabel);
            if (hasAction)
            {
                string resolved = actionLabel.Trim();
                _actionButton.text = resolved;
                _actionButton.tooltip = resolved;
                _actionButton.RemoveFromClassList(ActionHiddenClass);
                _actionButton.focusable = true;
                _actionButton.pickingMode = PickingMode.Position;
            }
            else
            {
                _actionButton.text = string.Empty;
                _actionButton.tooltip = string.Empty;
                _actionButton.AddToClassList(ActionHiddenClass);
                _actionButton.focusable = false;
            }
        }

        private void ApplyDismiss(bool allowDismiss)
        {
            if (_dismissButton == null)
            {
                return;
            }

            if (allowDismiss)
            {
                _dismissButton.tooltip = "Dismiss status";
                _dismissButton.RemoveFromClassList(DismissHiddenClass);
                _dismissButton.focusable = true;
                _dismissButton.pickingMode = PickingMode.Position;
            }
            else
            {
                _dismissButton.AddToClassList(DismissHiddenClass);
                _dismissButton.focusable = false;
            }
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
