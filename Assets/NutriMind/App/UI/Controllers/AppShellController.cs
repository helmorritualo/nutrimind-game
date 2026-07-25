using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace NutriMind.App.UI
{
    /// <summary>
    /// Static preview states for shell navigation. UI presentation only —
    /// does not represent production routing.
    /// </summary>
    public enum AppShellPreviewRoute
    {
        Home,
        Subjects,
        Missions,
        Progress,
        Rewards,
        More
    }

    /// <summary>
    /// Static preview states for connection / sync chrome. UI presentation
    /// only — does not represent real networking or synchronization.
    /// </summary>
    public enum AppShellConnectionPreview
    {
        Online,
        Offline,
        SyncPending,
        SyncError
    }

    /// <summary>
    /// Presentation-only toast tone for the shared AppShell toast layer.
    /// Maps to package <c>ds-toast</c> / <c>ds-icon</c> classes.
    /// </summary>
    public enum AppShellToastTone
    {
        Information,
        Success,
        Warning,
        Danger
    }

    /// <summary>
    /// Presentation-only application shell for UI Toolkit layout preview.
    /// Owns shared chrome regions (top bar, nav, offline banner, overlays)
    /// and static preview toggles. Does not perform routing, authentication,
    /// API calls, SQLite, synchronization, mission loading, or gameplay logic.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class AppShellController : MonoBehaviour
    {
        private const string CompactClass = "app-shell--compact";
        private const string NarrowClass = "app-shell--narrow";
        private const string MobileClass = "mobile";
        private const string ConnectionOnlineClass = "app-shell__connection--online";
        private const string ConnectionOfflineClass = "app-shell__connection--offline";
        private const string ConnectionSyncPendingClass = "app-shell__connection--sync-pending";
        private const string ConnectionSyncErrorClass = "app-shell__connection--sync-error";
        private const string IconWifiClass = "ds-icon--wifi";
        private const string IconSyncClass = "ds-icon--sync";
        private const string IconWarningClass = "ds-icon--warning";
        private const string IconErrorClass = "ds-icon--error";
        private const string ToastHiddenClass = "app-shell__toast--hidden";
        private const float DefaultToastDurationSeconds = 2.5f;
        private const float CompactBreakpoint = 1100f;
        private const float NarrowBreakpoint = 820f;

        private static readonly string[] ConnectionIconClasses =
        {
            IconWifiClass,
            IconSyncClass,
            IconWarningClass,
            IconErrorClass
        };

        private static readonly string[] ConnectionStateClasses =
        {
            ConnectionOnlineClass,
            ConnectionOfflineClass,
            ConnectionSyncPendingClass,
            ConnectionSyncErrorClass
        };

        private static readonly string[] ToastVariantClasses =
        {
            "ds-toast--info",
            "ds-toast--success",
            "ds-toast--warning",
            "ds-toast--danger"
        };

        private static readonly string[] ToastIconClasses =
        {
            "ds-icon--info",
            "ds-icon--check",
            "ds-icon--warning",
            "ds-icon--error"
        };

        [SerializeField]
        [Tooltip("UI-only preview route. Switches active nav styling and page title. Does not load screens or change scenes.")]
        private AppShellPreviewRoute _previewRoute = AppShellPreviewRoute.Home;

        [SerializeField]
        [Tooltip("UI-only connection/sync preview. Updates status text, icon, and banner visibility. Does not network or sync.")]
        private AppShellConnectionPreview _connectionPreview = AppShellConnectionPreview.Online;

        [SerializeField]
        [Tooltip("UI-only loading overlay preview. Shows or hides the shared LoadingOverlay. Does not load data.")]
        private bool _showLoadingPreview;

        [SerializeField]
        [Tooltip("Shared LoadingOverlay UXML used by the shell's global loading host.")]
        private VisualTreeAsset _loadingOverlayAsset;

        [SerializeField]
        [Tooltip("Shared OfflineSyncBanner UXML used by the shell's connectivity and sync status host.")]
        private VisualTreeAsset _offlineSyncBannerAsset;

        private UIDocument _uiDocument;
        private VisualElement _root;
        private VisualElement _navRegion;
        private VisualElement _contentRegion;
        private VisualElement _modalLayer;
        private VisualElement _toastLayer;
        private VisualElement _offlineBanner;
        private VisualElement _loadingLayer;
        private VisualElement _connectionHost;
        private VisualElement _connectionIcon;
        private VisualElement _toast;
        private VisualElement _toastIcon;
        private Label _pageTitle;
        private Label _pageContext;
        private Label _connectionLabel;
        private Label _toastLabel;
        private Button _toastCloseButton;
        private Button _notificationsButton;
        private Button _profileButton;
        private Button _navHome;
        private Button _navSubjects;
        private Button _navMissions;
        private Button _navProgress;
        private Button _navRewards;
        private Button _navMore;
        private TemplateContainer _loadingOverlayInstance;
        private LoadingOverlayView _loadingOverlayView;
        private bool _warnedMissingLoadingOverlayAsset;
        private TemplateContainer _offlineSyncBannerInstance;
        private OfflineSyncBannerView _offlineSyncBannerView;
        private bool _warnedMissingOfflineSyncBannerAsset;
        private float _lastWidth = -1f;
        private AppShellPreviewRoute? _appliedRoute;
        private AppShellConnectionPreview? _appliedConnection;
        private bool? _appliedLoading;
        /// <summary>
        /// Raised once after a bottom-navigation click applies its static preview route.
        /// Presentation-only request; this is not a production routing event.
        /// </summary>
        public event Action<AppShellPreviewRoute> PreviewRouteRequested;

        /// <summary>
        /// Raised once when the shell profile control is clicked in static preview.
        /// Presentation-only request; this is not a production routing event.
        /// </summary>
        public event Action ProfileRequested;

        /// <summary>
        /// Raised once when the shell notifications control is clicked in static preview.
        /// Presentation-only request; this is not a production routing event.
        /// </summary>
        public event Action NotificationsRequested;

        private void OnEnable()
        {
            _uiDocument = GetComponent<UIDocument>();
            BindWhenReady();
        }

        private void OnDisable()
        {
            CancelInvoke(nameof(BindWhenReady));
            CancelInvoke(nameof(HideToastImmediately));
            Unbind();
        }

        private void OnDestroy()
        {
            PreviewRouteRequested = null;
            ProfileRequested = null;
            NotificationsRequested = null;
        }

        private void OnValidate()
        {
            if (!isActiveAndEnabled || _root == null)
            {
                return;
            }

            ApplySerializedPreviewState();
        }

        private void Update()
        {
            if (_root == null)
            {
                return;
            }

            ApplySerializedPreviewState();

            float width = _root.resolvedStyle.width;
            if (float.IsNaN(width) || width <= 0f || Mathf.Approximately(width, _lastWidth))
            {
                return;
            }

            _lastWidth = width;
            ApplyResponsiveClasses(width);
        }

        /// <summary>
        /// Sets the serialized static preview route, active navigation, and default route title.
        /// Does not raise <see cref="PreviewRouteRequested"/>; content-preview metadata uses
        /// this method and emitting a request here would create a feedback loop.
        /// </summary>
        public void SetPreviewRoute(AppShellPreviewRoute route)
        {
            _previewRoute = route;
            ApplyPreviewRoute(route, logSelection: false);
        }

        /// <summary>
        /// Sets the static connection/sync chrome preview.
        /// </summary>
        public void SetConnectionPreview(AppShellConnectionPreview state)
        {
            _connectionPreview = state;
            ApplyConnectionPreview(state);
        }

        /// <summary>
        /// Shows or hides the shared LoadingOverlay with the PreparingApplication preset.
        /// Presentation only — does not load data or start real operations.
        /// </summary>
        public void SetLoadingPreview(bool visible)
        {
            _showLoadingPreview = visible;
            ApplyLoadingPreview(visible);
        }

        /// <summary>
        /// Shows the shared LoadingOverlay with the supplied presentation configuration.
        /// Marks serialized loading preview as shown so Update() does not overwrite with
        /// PreparingApplication solely because <c>_showLoadingPreview</c> is true.
        /// </summary>
        public void ShowLoadingOverlay(LoadingOverlayConfiguration configuration)
        {
            _showLoadingPreview = true;
            _appliedLoading = true;
            _loadingOverlayView?.Show(configuration);
        }

        /// <summary>
        /// Hides the shared LoadingOverlay without raising CancelRequested.
        /// </summary>
        public void HideLoadingOverlay()
        {
            _showLoadingPreview = false;
            _appliedLoading = false;
            _loadingOverlayView?.Hide();
        }

        /// <summary>
        /// Forwards normalized progress (0f–1f) to the shared LoadingOverlay when bound.
        /// Does not create an overlay when none is bound.
        /// </summary>
        public void SetLoadingOverlayProgress(float progress)
        {
            _loadingOverlayView?.SetProgress(progress);
        }

        /// <summary>
        /// Shows the shared OfflineSyncBanner with the supplied presentation configuration.
        /// Does not change the serialized connection preview enum.
        /// Presentation only — does not network or sync.
        /// </summary>
        public void ShowOfflineSyncBanner(OfflineSyncBannerConfiguration configuration)
        {
            _offlineSyncBannerView?.Show(configuration);
        }

        /// <summary>
        /// Hides the shared OfflineSyncBanner without raising Dismissed or ActionRequested.
        /// Does not change the serialized connection preview enum.
        /// </summary>
        public void HideOfflineSyncBanner()
        {
            _offlineSyncBannerView?.Hide();
        }

        /// <summary>
        /// Updates the top-bar page title and optional context label.
        /// </summary>
        public void SetPageTitle(string title, string context = null)
        {
            if (_pageTitle != null)
            {
                _pageTitle.text = string.IsNullOrEmpty(title) ? string.Empty : title;
            }

            if (_pageContext != null)
            {
                bool hasContext = !string.IsNullOrEmpty(context);
                _pageContext.text = hasContext ? context : string.Empty;
                _pageContext.style.display = hasContext ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        /// <summary>
        /// Returns the content host for future screen insertion (presentation only).
        /// </summary>
        public VisualElement GetContentRegion()
        {
            return _contentRegion;
        }

        /// <summary>
        /// Returns the modal overlay host for future shared dialogs.
        /// </summary>
        public VisualElement GetModalLayer()
        {
            return _modalLayer;
        }

        /// <summary>
        /// Returns the toast overlay host for the shared package-styled toast.
        /// </summary>
        public VisualElement GetToastLayer()
        {
            return _toastLayer;
        }

        /// <summary>
        /// Shows the shared AppShell toast with package variant styling.
        /// Presentation only — replaces any currently visible toast message and tone.
        /// </summary>
        /// <param name="message">Toast message. Blank values fall back to "Notification".</param>
        /// <param name="tone">Package toast tone.</param>
        /// <param name="durationSeconds">
        /// Auto-hide delay. Values greater than zero schedule hide; zero or negative keeps
        /// the toast visible until <see cref="HideToast"/> or the close button.
        /// </param>
        public void ShowToast(
            string message,
            AppShellToastTone tone = AppShellToastTone.Information,
            float durationSeconds = DefaultToastDurationSeconds)
        {
            if (_toast == null || _toastLabel == null)
            {
                return;
            }

            _toastLabel.text = string.IsNullOrWhiteSpace(message)
                ? "Notification"
                : message.Trim();

            ClearClassList(_toast, ToastVariantClasses);
            ClearClassList(_toastIcon, ToastIconClasses);
            ApplyToastTone(tone);

            _toast.RemoveFromClassList(ToastHiddenClass);

            CancelInvoke(nameof(HideToastImmediately));
            if (durationSeconds > 0f)
            {
                Invoke(nameof(HideToastImmediately), durationSeconds);
            }
        }

        /// <summary>
        /// Hides the shared AppShell toast and cancels any pending auto-hide.
        /// Safe to call repeatedly.
        /// </summary>
        public void HideToast()
        {
            CancelInvoke(nameof(HideToastImmediately));
            HideToastImmediately();
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

            _root = _uiDocument.rootVisualElement?.Q<VisualElement>("app-shell-root");
            if (_root == null)
            {
                Invoke(nameof(BindWhenReady), 0.05f);
                return;
            }

            VisualElement panelRoot = _uiDocument.rootVisualElement;
            panelRoot.style.flexGrow = 1;
            panelRoot.style.width = Length.Percent(100);
            panelRoot.style.height = Length.Percent(100);

            CacheElements();
            BindLoadingOverlay();
            BindOfflineSyncBanner();
            RegisterCallbacks();
            ApplySerializedPreviewState(force: true);
            _root.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            ApplyResponsiveClasses(_root.resolvedStyle.width);
        }

        private void CacheElements()
        {
            if (_root == null)
            {
                return;
            }

            _pageTitle = _root.Q<Label>("app-shell-page-title");
            _pageContext = _root.Q<Label>("app-shell-page-context");
            _connectionHost = _root.Q<VisualElement>("app-shell-connection");
            _connectionIcon = _root.Q<VisualElement>("app-shell-connection-icon");
            _connectionLabel = _root.Q<Label>("app-shell-connection-label");
            _offlineBanner = _root.Q<VisualElement>("app-shell-offline-banner");
            _contentRegion = _root.Q<VisualElement>("app-shell-content-region");
            _navRegion = _root.Q<VisualElement>("app-shell-navigation-region");
            _toastLayer = _root.Q<VisualElement>("app-shell-toast-layer");
            _toast = _root.Q<VisualElement>("app-shell-toast");
            _toastIcon = _root.Q<VisualElement>("app-shell-toast-icon");
            _toastLabel = _root.Q<Label>("app-shell-toast-label");
            _toastCloseButton = _root.Q<Button>("app-shell-toast-close");
            _modalLayer = _root.Q<VisualElement>("app-shell-modal-layer");
            _loadingLayer = _root.Q<VisualElement>("app-shell-loading-layer");
            _notificationsButton = _root.Q<Button>("app-shell-notifications");
            _profileButton = _root.Q<Button>("app-shell-profile");

            _navHome = _root.Q<Button>("nav-home");
            _navSubjects = _root.Q<Button>("nav-subjects");
            _navMissions = _root.Q<Button>("nav-missions");
            _navProgress = _root.Q<Button>("nav-progress");
            _navRewards = _root.Q<Button>("nav-rewards");
            _navMore = _root.Q<Button>("nav-more");

            HideToastImmediately();
        }

        private void RegisterCallbacks()
        {
            _navHome?.RegisterCallback<ClickEvent>(OnNavHomeClicked);
            _navSubjects?.RegisterCallback<ClickEvent>(OnNavSubjectsClicked);
            _navMissions?.RegisterCallback<ClickEvent>(OnNavMissionsClicked);
            _navProgress?.RegisterCallback<ClickEvent>(OnNavProgressClicked);
            _navRewards?.RegisterCallback<ClickEvent>(OnNavRewardsClicked);
            _navMore?.RegisterCallback<ClickEvent>(OnNavMoreClicked);
            _notificationsButton?.RegisterCallback<ClickEvent>(OnNotificationsClicked);
            _profileButton?.RegisterCallback<ClickEvent>(OnProfileClicked);
            _toastCloseButton?.RegisterCallback<ClickEvent>(OnToastCloseClicked);
        }

        private void Unbind()
        {
            _navHome?.UnregisterCallback<ClickEvent>(OnNavHomeClicked);
            _navSubjects?.UnregisterCallback<ClickEvent>(OnNavSubjectsClicked);
            _navMissions?.UnregisterCallback<ClickEvent>(OnNavMissionsClicked);
            _navProgress?.UnregisterCallback<ClickEvent>(OnNavProgressClicked);
            _navRewards?.UnregisterCallback<ClickEvent>(OnNavRewardsClicked);
            _navMore?.UnregisterCallback<ClickEvent>(OnNavMoreClicked);
            _notificationsButton?.UnregisterCallback<ClickEvent>(OnNotificationsClicked);
            _profileButton?.UnregisterCallback<ClickEvent>(OnProfileClicked);
            _toastCloseButton?.UnregisterCallback<ClickEvent>(OnToastCloseClicked);

            CancelInvoke(nameof(HideToastImmediately));

            if (_root != null)
            {
                _root.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            }

            UnbindLoadingOverlay();
            UnbindOfflineSyncBanner();

            _root = null;
            _navRegion = null;
            _contentRegion = null;
            _modalLayer = null;
            _toastLayer = null;
            _toast = null;
            _toastIcon = null;
            _toastLabel = null;
            _toastCloseButton = null;
            _offlineBanner = null;
            _loadingLayer = null;
            _connectionHost = null;
            _connectionIcon = null;
            _pageTitle = null;
            _pageContext = null;
            _connectionLabel = null;
            _notificationsButton = null;
            _profileButton = null;
            _navHome = null;
            _navSubjects = null;
            _navMissions = null;
            _navProgress = null;
            _navRewards = null;
            _navMore = null;
            _lastWidth = -1f;
            _appliedRoute = null;
            _appliedConnection = null;
            _appliedLoading = null;
        }

        /// <summary>
        /// Clones the shared LoadingOverlay into the shell loading host once per bind cycle.
        /// Missing asset logs once and leaves the host empty/non-blocking.
        /// </summary>
        private void BindLoadingOverlay()
        {
            if (_loadingLayer == null)
            {
                return;
            }

            UnbindLoadingOverlay();

            if (_loadingOverlayAsset == null)
            {
                if (!_warnedMissingLoadingOverlayAsset)
                {
                    Debug.LogWarning(
                        "[AppShellController] LoadingOverlay VisualTreeAsset is not assigned. " +
                        "Assign Assets/NutriMind/App/UI/UXML/Shared/LoadingOverlay.uxml to _loadingOverlayAsset. " +
                        "Loading host remains empty and non-blocking.");
                    _warnedMissingLoadingOverlayAsset = true;
                }

                _loadingLayer.pickingMode = PickingMode.Ignore;
                return;
            }

            _loadingOverlayInstance = _loadingOverlayAsset.CloneTree();

            // A cloned TemplateContainer has no layout of its own, so the absolutely
            // positioned overlay root would collapse against a zero-sized parent.
            _loadingOverlayInstance.style.position = Position.Absolute;
            _loadingOverlayInstance.style.left = 0f;
            _loadingOverlayInstance.style.top = 0f;
            _loadingOverlayInstance.style.right = 0f;
            _loadingOverlayInstance.style.bottom = 0f;

            _loadingLayer.Add(_loadingOverlayInstance);
            _loadingOverlayView = new LoadingOverlayView(_loadingOverlayInstance);
            if (!_loadingOverlayView.IsBound)
            {
                Debug.LogWarning(
                    "[AppShellController] Failed to bind LoadingOverlayView from cloned asset. " +
                    "Loading host remains empty and non-blocking.");
                UnbindLoadingOverlay();
                return;
            }

            _loadingOverlayView.CancelRequested += OnLoadingOverlayCancelRequested;
        }

        private void UnbindLoadingOverlay()
        {
            if (_loadingOverlayView != null)
            {
                _loadingOverlayView.CancelRequested -= OnLoadingOverlayCancelRequested;
                _loadingOverlayView.Dispose();
                _loadingOverlayView = null;
            }

            if (_loadingOverlayInstance != null)
            {
                _loadingOverlayInstance.RemoveFromHierarchy();
                _loadingOverlayInstance = null;
            }
        }

        private void OnLoadingOverlayCancelRequested()
        {
            Debug.Log("[AppShellController] Shared loading overlay cancel requested (preview only).");
        }

        /// <summary>
        /// Clones the shared OfflineSyncBanner into the shell banner host once per bind cycle.
        /// Missing asset logs once and leaves the host empty/non-blocking.
        /// </summary>
        private void BindOfflineSyncBanner()
        {
            if (_offlineBanner == null)
            {
                return;
            }

            UnbindOfflineSyncBanner();

            if (_offlineSyncBannerAsset == null)
            {
                if (!_warnedMissingOfflineSyncBannerAsset)
                {
                    Debug.LogWarning(
                        "[AppShellController] OfflineSyncBanner VisualTreeAsset is not assigned. " +
                        "Assign Assets/NutriMind/App/UI/UXML/Shared/OfflineSyncBanner.uxml " +
                        "to _offlineSyncBannerAsset. Banner host remains empty and non-blocking.");
                    _warnedMissingOfflineSyncBannerAsset = true;
                }

                _offlineBanner.pickingMode = PickingMode.Ignore;
                return;
            }

            _offlineSyncBannerInstance = _offlineSyncBannerAsset.CloneTree();
            _offlineSyncBannerInstance.style.width = Length.Percent(100);
            _offlineSyncBannerInstance.style.flexShrink = 0;

            _offlineBanner.Add(_offlineSyncBannerInstance);
            _offlineSyncBannerView = new OfflineSyncBannerView(_offlineSyncBannerInstance);
            if (!_offlineSyncBannerView.IsBound)
            {
                Debug.LogWarning(
                    "[AppShellController] Failed to bind OfflineSyncBannerView from cloned asset. " +
                    "Banner host remains empty and non-blocking.");
                UnbindOfflineSyncBanner();
                return;
            }

            _offlineSyncBannerView.ActionRequested += OnOfflineSyncBannerActionRequested;
            _offlineSyncBannerView.Dismissed += OnOfflineSyncBannerDismissed;

            // Force connection preview once after banner bind so an already-matching
            // _appliedConnection does not skip the initial shared-banner render.
            _appliedConnection = null;
            ApplyConnectionPreview(_connectionPreview);
        }

        private void UnbindOfflineSyncBanner()
        {
            if (_offlineSyncBannerView != null)
            {
                _offlineSyncBannerView.ActionRequested -= OnOfflineSyncBannerActionRequested;
                _offlineSyncBannerView.Dismissed -= OnOfflineSyncBannerDismissed;
                _offlineSyncBannerView.Dispose();
                _offlineSyncBannerView = null;
            }

            if (_offlineSyncBannerInstance != null)
            {
                _offlineSyncBannerInstance.RemoveFromHierarchy();
                _offlineSyncBannerInstance = null;
            }
        }

        private void OnOfflineSyncBannerActionRequested()
        {
            Debug.Log("[AppShellController] Offline/sync banner action requested (preview only).");
        }

        private void OnOfflineSyncBannerDismissed()
        {
            Debug.Log("[AppShellController] Offline/sync banner dismissed (preview only).");
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            ApplyResponsiveClasses(evt.newRect.width);
        }

        private void ApplyResponsiveClasses(float width)
        {
            if (_root == null || float.IsNaN(width) || width <= 0f)
            {
                return;
            }

            bool compact = width < CompactBreakpoint;
            bool narrow = width < NarrowBreakpoint;

            _root.EnableInClassList(CompactClass, compact);
            _root.EnableInClassList(NarrowClass, narrow);
            _root.EnableInClassList(MobileClass, narrow);
        }

        private void ApplySerializedPreviewState(bool force = false)
        {
            if (force || _appliedRoute != _previewRoute)
            {
                ApplyPreviewRoute(_previewRoute, logSelection: false);
            }

            if (force || _appliedConnection != _connectionPreview)
            {
                ApplyConnectionPreview(_connectionPreview);
            }

            if (force || _appliedLoading != _showLoadingPreview)
            {
                ApplyLoadingPreview(_showLoadingPreview);
            }

        }

        private void OnNavHomeClicked(ClickEvent evt)
        {
            SelectPreviewRoute(AppShellPreviewRoute.Home);
        }

        private void OnNavSubjectsClicked(ClickEvent evt)
        {
            SelectPreviewRoute(AppShellPreviewRoute.Subjects);
        }

        private void OnNavMissionsClicked(ClickEvent evt)
        {
            SelectPreviewRoute(AppShellPreviewRoute.Missions);
        }

        private void OnNavProgressClicked(ClickEvent evt)
        {
            SelectPreviewRoute(AppShellPreviewRoute.Progress);
        }

        private void OnNavRewardsClicked(ClickEvent evt)
        {
            SelectPreviewRoute(AppShellPreviewRoute.Rewards);
        }

        private void OnNavMoreClicked(ClickEvent evt)
        {
            SelectPreviewRoute(AppShellPreviewRoute.More);
        }

        private void OnNotificationsClicked(ClickEvent evt)
        {
            Debug.Log("[AppShellController] Notifications button tapped — preview only.");
            NotificationsRequested?.Invoke();
        }

        private void OnProfileClicked(ClickEvent evt)
        {
            Debug.Log("[AppShellController] Profile button tapped — preview only.");
            ProfileRequested?.Invoke();
        }

        private void OnToastCloseClicked(ClickEvent evt)
        {
            HideToast();
        }

        private void HideToastImmediately()
        {
            _toast?.AddToClassList(ToastHiddenClass);
        }

        private void ApplyToastTone(AppShellToastTone tone)
        {
            switch (tone)
            {
                case AppShellToastTone.Success:
                    _toast?.AddToClassList("ds-toast--success");
                    SetIconClass(_toastIcon, "ds-icon--check");
                    break;
                case AppShellToastTone.Warning:
                    _toast?.AddToClassList("ds-toast--warning");
                    SetIconClass(_toastIcon, "ds-icon--warning");
                    break;
                case AppShellToastTone.Danger:
                    _toast?.AddToClassList("ds-toast--danger");
                    SetIconClass(_toastIcon, "ds-icon--error");
                    break;
                default:
                    _toast?.AddToClassList("ds-toast--info");
                    SetIconClass(_toastIcon, "ds-icon--info");
                    break;
            }
        }

        private void SelectPreviewRoute(AppShellPreviewRoute route)
        {
            _previewRoute = route;
            ApplyPreviewRoute(route, logSelection: true);
            PreviewRouteRequested?.Invoke(route);
        }

        private void ApplyPreviewRoute(AppShellPreviewRoute route, bool logSelection)
        {
            _appliedRoute = route;

            Button active = GetNavButton(route);
            SetActiveNavItem(active);
            SetPageTitle(GetRouteTitle(route), "Preview shell");

            if (logSelection)
            {
                Debug.Log($"[AppShellController] Preview route selected: {route}");
            }
        }

        private void SetActiveNavItem(Button selected)
        {
            SetNavItemActive(_navHome, selected == _navHome);
            SetNavItemActive(_navSubjects, selected == _navSubjects);
            SetNavItemActive(_navMissions, selected == _navMissions);
            SetNavItemActive(_navProgress, selected == _navProgress);
            SetNavItemActive(_navRewards, selected == _navRewards);
            SetNavItemActive(_navMore, selected == _navMore);
        }

        private static void SetNavItemActive(Button button, bool isActive)
        {
            button?.EnableInClassList("is-active", isActive);
        }

        private Button GetNavButton(AppShellPreviewRoute route)
        {
            switch (route)
            {
                case AppShellPreviewRoute.Home:
                    return _navHome;
                case AppShellPreviewRoute.Subjects:
                    return _navSubjects;
                case AppShellPreviewRoute.Missions:
                    return _navMissions;
                case AppShellPreviewRoute.Progress:
                    return _navProgress;
                case AppShellPreviewRoute.Rewards:
                    return _navRewards;
                case AppShellPreviewRoute.More:
                    return _navMore;
                default:
                    return _navHome;
            }
        }

        private static string GetRouteTitle(AppShellPreviewRoute route)
        {
            switch (route)
            {
                case AppShellPreviewRoute.Home:
                    return "Home";
                case AppShellPreviewRoute.Subjects:
                    return "Subjects";
                case AppShellPreviewRoute.Missions:
                    return "Missions";
                case AppShellPreviewRoute.Progress:
                    return "Progress";
                case AppShellPreviewRoute.Rewards:
                    return "Rewards";
                case AppShellPreviewRoute.More:
                    return "More";
                default:
                    return "Home";
            }
        }

        private void ApplyConnectionPreview(AppShellConnectionPreview state)
        {
            _appliedConnection = state;

            ClearClassList(_connectionHost, ConnectionStateClasses);
            ClearClassList(_connectionIcon, ConnectionIconClasses);

            switch (state)
            {
                case AppShellConnectionPreview.Online:
                    _connectionHost?.AddToClassList(ConnectionOnlineClass);
                    SetIconClass(_connectionIcon, IconWifiClass);
                    if (_connectionLabel != null)
                    {
                        _connectionLabel.text = "Synced • Just now";
                    }

                    _offlineSyncBannerView?.Hide();
                    break;

                case AppShellConnectionPreview.Offline:
                    _connectionHost?.AddToClassList(ConnectionOfflineClass);
                    SetIconClass(_connectionIcon, IconWifiClass);
                    if (_connectionLabel != null)
                    {
                        _connectionLabel.text = "Offline";
                    }

                    _offlineSyncBannerView?.Show(OfflineSyncBannerPresets.OfflineCached());
                    break;

                case AppShellConnectionPreview.SyncPending:
                    _connectionHost?.AddToClassList(ConnectionSyncPendingClass);
                    SetIconClass(_connectionIcon, IconSyncClass);
                    if (_connectionLabel != null)
                    {
                        _connectionLabel.text = "3 updates waiting";
                    }

                    _offlineSyncBannerView?.Show(OfflineSyncBannerPresets.SyncPending(3));
                    break;

                case AppShellConnectionPreview.SyncError:
                    _connectionHost?.AddToClassList(ConnectionSyncErrorClass);
                    SetIconClass(_connectionIcon, IconErrorClass);
                    if (_connectionLabel != null)
                    {
                        _connectionLabel.text = "Sync needs attention";
                    }

                    _offlineSyncBannerView?.Show(OfflineSyncBannerPresets.SyncError());
                    break;
            }
        }

        private void ApplyLoadingPreview(bool visible)
        {
            _appliedLoading = visible;

            if (_loadingOverlayView == null || !_loadingOverlayView.IsBound)
            {
                return;
            }

            if (visible)
            {
                _loadingOverlayView.Show(LoadingOverlayPresets.PreparingApplication());
            }
            else
            {
                _loadingOverlayView.Hide();
            }
        }

        private static void SetIconClass(VisualElement icon, string iconClass)
        {
            if (icon == null || string.IsNullOrEmpty(iconClass))
            {
                return;
            }

            icon.AddToClassList(iconClass);
        }

        private static void ClearClassList(VisualElement element, string[] classNames)
        {
            if (element == null || classNames == null)
            {
                return;
            }

            for (int i = 0; i < classNames.Length; i++)
            {
                element.RemoveFromClassList(classNames[i]);
            }
        }
    }
}
