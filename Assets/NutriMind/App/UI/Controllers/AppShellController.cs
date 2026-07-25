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
        private const string BannerHiddenClass = "app-shell__offline-banner--hidden";
        private const string BannerOfflineClass = "app-shell__offline-banner--offline";
        private const string BannerSyncPendingClass = "app-shell__offline-banner--sync-pending";
        private const string BannerSyncErrorClass = "app-shell__offline-banner--sync-error";
        private const string LoadingHiddenClass = "app-shell__loading-layer--hidden";
        private const string PreviewHiddenClass = "app-shell__preview-content--hidden";
        private const string ConnectionOnlineClass = "app-shell__connection--online";
        private const string ConnectionOfflineClass = "app-shell__connection--offline";
        private const string ConnectionSyncPendingClass = "app-shell__connection--sync-pending";
        private const string ConnectionSyncErrorClass = "app-shell__connection--sync-error";
        private const string IconWifiClass = "ds-icon--wifi";
        private const string IconSyncClass = "ds-icon--sync";
        private const string IconWarningClass = "ds-icon--warning";
        private const string IconErrorClass = "ds-icon--error";
        private const float CompactBreakpoint = 1100f;
        private const float NarrowBreakpoint = 820f;

        private static readonly string[] ConnectionIconClasses =
        {
            IconWifiClass,
            IconSyncClass,
            IconWarningClass,
            IconErrorClass
        };

        private static readonly string[] BannerStateClasses =
        {
            BannerOfflineClass,
            BannerSyncPendingClass,
            BannerSyncErrorClass
        };

        private static readonly string[] ConnectionStateClasses =
        {
            ConnectionOnlineClass,
            ConnectionOfflineClass,
            ConnectionSyncPendingClass,
            ConnectionSyncErrorClass
        };

        [SerializeField]
        [Tooltip("UI-only preview route. Switches active nav styling and page title. Does not load screens or change scenes.")]
        private AppShellPreviewRoute _previewRoute = AppShellPreviewRoute.Home;

        [SerializeField]
        [Tooltip("UI-only connection/sync preview. Updates status text, icon, and banner visibility. Does not network or sync.")]
        private AppShellConnectionPreview _connectionPreview = AppShellConnectionPreview.Online;

        [SerializeField]
        [Tooltip("UI-only loading overlay preview. Shows or hides the shell loading layer. Does not load data.")]
        private bool _showLoadingPreview;

        [SerializeField]
        [Tooltip("UI-only content placeholder toggle. Shows or hides the temporary App Shell Preview card.")]
        private bool _showPreviewContent = true;

        private UIDocument _uiDocument;
        private VisualElement _root;
        private VisualElement _navRegion;
        private VisualElement _contentRegion;
        private VisualElement _modalLayer;
        private VisualElement _toastLayer;
        private VisualElement _offlineBanner;
        private VisualElement _loadingLayer;
        private VisualElement _previewContent;
        private VisualElement _connectionHost;
        private VisualElement _connectionIcon;
        private VisualElement _bannerIcon;
        private Label _pageTitle;
        private Label _pageContext;
        private Label _connectionLabel;
        private Label _bannerLabel;
        private Button _notificationsButton;
        private Button _profileButton;
        private Button _navHome;
        private Button _navSubjects;
        private Button _navMissions;
        private Button _navProgress;
        private Button _navRewards;
        private Button _navMore;
        private float _lastWidth = -1f;
        private AppShellPreviewRoute? _appliedRoute;
        private AppShellConnectionPreview? _appliedConnection;
        private bool? _appliedLoading;
        private bool? _appliedPreviewContent;

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
        /// Sets the static preview route (nav + title only).
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
        /// Shows or hides the shell loading overlay preview.
        /// </summary>
        public void SetLoadingPreview(bool visible)
        {
            _showLoadingPreview = visible;
            ApplyLoadingPreview(visible);
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
        /// Returns the toast overlay host for future package-styled toasts.
        /// </summary>
        public VisualElement GetToastLayer()
        {
            return _toastLayer;
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
            _bannerIcon = _root.Q<VisualElement>("app-shell-banner-icon");
            _bannerLabel = _root.Q<Label>("app-shell-banner-label");
            _contentRegion = _root.Q<VisualElement>("app-shell-content-region");
            _previewContent = _root.Q<VisualElement>("app-shell-preview-content");
            _navRegion = _root.Q<VisualElement>("app-shell-navigation-region");
            _toastLayer = _root.Q<VisualElement>("app-shell-toast-layer");
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

            if (_root != null)
            {
                _root.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            }

            _root = null;
            _navRegion = null;
            _contentRegion = null;
            _modalLayer = null;
            _toastLayer = null;
            _offlineBanner = null;
            _loadingLayer = null;
            _previewContent = null;
            _connectionHost = null;
            _connectionIcon = null;
            _bannerIcon = null;
            _pageTitle = null;
            _pageContext = null;
            _connectionLabel = null;
            _bannerLabel = null;
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
            _appliedPreviewContent = null;
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

            if (force || _appliedPreviewContent != _showPreviewContent)
            {
                ApplyPreviewContentVisibility(_showPreviewContent);
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
        }

        private void OnProfileClicked(ClickEvent evt)
        {
            Debug.Log("[AppShellController] Profile button tapped — preview only.");
        }

        private void SelectPreviewRoute(AppShellPreviewRoute route)
        {
            _previewRoute = route;
            ApplyPreviewRoute(route, logSelection: true);
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
            ClearClassList(_offlineBanner, BannerStateClasses);
            ClearClassList(_connectionIcon, ConnectionIconClasses);
            ClearClassList(_bannerIcon, ConnectionIconClasses);

            switch (state)
            {
                case AppShellConnectionPreview.Online:
                    _connectionHost?.AddToClassList(ConnectionOnlineClass);
                    SetIconClass(_connectionIcon, IconWifiClass);
                    if (_connectionLabel != null)
                    {
                        _connectionLabel.text = "Synced • Just now";
                    }

                    _offlineBanner?.AddToClassList(BannerHiddenClass);
                    break;

                case AppShellConnectionPreview.Offline:
                    _connectionHost?.AddToClassList(ConnectionOfflineClass);
                    SetIconClass(_connectionIcon, IconWifiClass);
                    if (_connectionLabel != null)
                    {
                        _connectionLabel.text = "Offline";
                    }

                    ShowBanner(
                        BannerOfflineClass,
                        IconWarningClass,
                        "You are offline. Showing downloaded progress.");
                    break;

                case AppShellConnectionPreview.SyncPending:
                    _connectionHost?.AddToClassList(ConnectionSyncPendingClass);
                    SetIconClass(_connectionIcon, IconSyncClass);
                    if (_connectionLabel != null)
                    {
                        _connectionLabel.text = "3 updates waiting";
                    }

                    ShowBanner(
                        BannerSyncPendingClass,
                        IconSyncClass,
                        "Your progress is saved on this device and will sync when online.");
                    break;

                case AppShellConnectionPreview.SyncError:
                    _connectionHost?.AddToClassList(ConnectionSyncErrorClass);
                    SetIconClass(_connectionIcon, IconErrorClass);
                    if (_connectionLabel != null)
                    {
                        _connectionLabel.text = "Sync needs attention";
                    }

                    ShowBanner(
                        BannerSyncErrorClass,
                        IconErrorClass,
                        "Some progress could not sync. Retry when your connection is stable.");
                    break;
            }
        }

        private void ShowBanner(string bannerClass, string iconClass, string message)
        {
            if (_offlineBanner != null)
            {
                _offlineBanner.RemoveFromClassList(BannerHiddenClass);
                _offlineBanner.AddToClassList(bannerClass);
            }

            SetIconClass(_bannerIcon, iconClass);

            if (_bannerLabel != null)
            {
                _bannerLabel.text = message;
            }
        }

        private void ApplyLoadingPreview(bool visible)
        {
            _appliedLoading = visible;

            if (_loadingLayer == null)
            {
                return;
            }

            _loadingLayer.EnableInClassList(LoadingHiddenClass, !visible);
        }

        private void ApplyPreviewContentVisibility(bool visible)
        {
            _appliedPreviewContent = visible;
            _previewContent?.EnableInClassList(PreviewHiddenClass, !visible);
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
