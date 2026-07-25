using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace NutriMind.App.UI
{
    /// <summary>
    /// Static content-screen choices available to the AppShell preview host.
    /// Bootstrap and Login are intentionally excluded because they live outside
    /// the authenticated application shell.
    /// </summary>
    public enum AppShellContentPreviewScreen
    {
        None,
        Home,
        Subjects,
        Terms,
        Missions,
        LockedMission,
        Profile,
        Settings,
        Progress,
        QuizList,
        QuizDetail,
        QuizAttempt,
        QuizResult
    }

    /// <summary>
    /// Serialized presentation metadata for one content-only static preview.
    /// Contains no route history, scene, authentication, server, or learner state.
    /// </summary>
    [System.Serializable]
    public sealed class AppShellContentPreviewEntry
    {
        [SerializeField]
        private AppShellContentPreviewScreen _screen;

        [SerializeField]
        private VisualTreeAsset _contentAsset;

        [SerializeField]
        private AppShellPreviewRoute _activeNavigation = AppShellPreviewRoute.Home;

        [SerializeField]
        private string _pageTitle = "Home";

        [SerializeField]
        private string _pageContext;

        [SerializeField]
        private bool _selectFromBottomNavigation = true;

        public AppShellContentPreviewScreen Screen => _screen;

        public VisualTreeAsset ContentAsset => _contentAsset;

        public AppShellPreviewRoute ActiveNavigation => _activeNavigation;

        public string PageTitle => _pageTitle;

        public string PageContext => _pageContext;

        public bool SelectFromBottomNavigation => _selectFromBottomNavigation;
    }

    /// <summary>
    /// Presentation-only host that clones one serialized content UXML into AppShell.
    /// This component is a static UI preview mechanism, not a production router.
    /// <para>
    /// A migrated panel UXML represents route content only. Its root fills the shell
    /// content region and uses <c>ds-root theme-nutrimind app-screen-content</c> plus
    /// its screen class. It references DesignSystem.uss, NutriMindTheme.uss,
    /// Shared/AppScreenContent.uss, and its own USS. It must not include the AppShell
    /// brand, global connection/profile/notifications controls, bottom application
    /// navigation, global toast/modal/loading hosts, or the offline/sync banner.
    /// </para>
    /// <para>
    /// Route-local headings, breadcrumbs, Back controls, identity, tabs, filters,
    /// cards, lists, detail layouts, local actions, state hosts, and helper text are
    /// allowed. Each later migration supplies one plain <see cref="IAppScreenView"/>
    /// implementation, while the existing MonoBehaviour controller may remain as a
    /// standalone <c>UIDocument</c> preview adapter for that same view.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AppShellController))]
    public sealed class AppShellContentPreviewController : MonoBehaviour
    {
        private const string ContentInstanceClass = "app-shell__content-instance";
        private const string ScreenContentClass = "app-screen-content";
        private const string EmbeddedContentClass = "app-screen-content--embedded";

        [SerializeField]
        [Tooltip("Static screen preview shown inside AppShell. This is not production routing.")]
        private AppShellContentPreviewScreen _previewScreen =
            AppShellContentPreviewScreen.None;

        [SerializeField]
        [Tooltip("Content-only screen UXML assets and shell metadata used for static preview.")]
        private List<AppShellContentPreviewEntry> _previewEntries = new();

        [SerializeField]
        [Tooltip("Shared DataStatePanel UXML used when a preview screen is not assigned.")]
        private VisualTreeAsset _dataStatePanelAsset;

        [SerializeField]
        [Tooltip("When enabled, AppShell bottom-navigation clicks select matching preview entries.")]
        private bool _respondToShellNavigation = true;

        [SerializeField]
        [Tooltip("When enabled, the AppShell profile button selects the Profile preview entry.")]
        private bool _respondToProfileRequest = true;

        private AppShellController _appShell;
        private VisualElement _contentRegion;

        private TemplateContainer _currentContentInstance;
        private VisualElement _currentContentRoot;
        private IAppScreenView _currentScreenView;
        private HomePanelView _currentHomeView;

        private TemplateContainer _fallbackInstance;
        private DataStatePanelView _fallbackView;

        private AppShellContentPreviewScreen? _appliedScreen;
        private VisualTreeAsset _appliedAsset;
        private string _appliedTitle;
        private string _appliedContext;
        private AppShellPreviewRoute? _appliedNavigation;

        private bool _isBound;
        private bool _warnedMissingDataStateAsset;
        private bool _entriesValidated;
        private bool _requestFallbackActive;
        private AppShellContentPreviewScreen _requestFallbackSelection;

        public AppShellContentPreviewScreen PreviewScreen => _previewScreen;

        public VisualElement CurrentContentRoot => _currentContentRoot;

        public bool HasCurrentContent => _currentContentInstance != null;

        private void OnEnable()
        {
            _appShell = GetComponent<AppShellController>();
            BindWhenReady();
        }

        private void OnDisable()
        {
            CancelInvoke(nameof(BindWhenReady));
            Unbind();
        }

        private void OnValidate()
        {
            ValidatePreviewEntries();
            _entriesValidated = true;

            if (!isActiveAndEnabled || !_isBound)
            {
                return;
            }

            _requestFallbackActive = false;
            ApplyPreviewScreen(_previewScreen, force: true);
        }

        private void Update()
        {
            if (!_isBound)
            {
                return;
            }

            if (_requestFallbackActive)
            {
                if (_previewScreen == _requestFallbackSelection)
                {
                    return;
                }

                _requestFallbackActive = false;
            }

            ApplyPreviewScreen(_previewScreen);
        }

        /// <summary>
        /// Selects and immediately applies a static content preview when bound.
        /// Does not perform production navigation.
        /// </summary>
        public void SetPreviewScreen(AppShellContentPreviewScreen screen)
        {
            _previewScreen = screen;
            _requestFallbackActive = false;

            if (_isBound)
            {
                ApplyPreviewScreen(screen);
            }
        }

        private void BindWhenReady()
        {
            if (_isBound)
            {
                return;
            }

            if (_appShell == null)
            {
                _appShell = GetComponent<AppShellController>();
            }

            if (_appShell == null)
            {
                return;
            }

            _contentRegion = _appShell.GetContentRegion();
            if (_contentRegion == null)
            {
                Invoke(nameof(BindWhenReady), 0.05f);
                return;
            }

            _appShell.PreviewRouteRequested += OnPreviewRouteRequested;
            _appShell.ProfileRequested += OnProfileRequested;
            _appShell.NotificationsRequested += OnNotificationsRequested;
            _isBound = true;

            if (!_entriesValidated)
            {
                ValidatePreviewEntries();
                _entriesValidated = true;
            }

            ApplyPreviewScreen(_previewScreen, force: true);
        }

        private void Unbind()
        {
            if (_appShell != null && _isBound)
            {
                _appShell.PreviewRouteRequested -= OnPreviewRouteRequested;
                _appShell.ProfileRequested -= OnProfileRequested;
                _appShell.NotificationsRequested -= OnNotificationsRequested;
            }

            ClearCurrentContent();
            ClearFallback();

            _contentRegion = null;
            _appShell = null;
            _isBound = false;
            _requestFallbackActive = false;
            ResetAppliedTracking();
        }

        private AppShellContentPreviewEntry FindEntry(
            AppShellContentPreviewScreen screen)
        {
            if (screen == AppShellContentPreviewScreen.None || _previewEntries == null)
            {
                return null;
            }

            for (int i = 0; i < _previewEntries.Count; i++)
            {
                AppShellContentPreviewEntry entry = _previewEntries[i];
                if (entry != null && entry.Screen == screen)
                {
                    return entry;
                }
            }

            return null;
        }

        private AppShellContentPreviewEntry FindEntryForNavigation(
            AppShellPreviewRoute route)
        {
            if (_previewEntries == null)
            {
                return null;
            }

            for (int i = 0; i < _previewEntries.Count; i++)
            {
                AppShellContentPreviewEntry entry = _previewEntries[i];
                if (entry != null
                    && entry.SelectFromBottomNavigation
                    && entry.ActiveNavigation == route)
                {
                    return entry;
                }
            }

            return null;
        }

        private void ApplyPreviewScreen(
            AppShellContentPreviewScreen screen,
            bool force = false)
        {
            if (!_isBound || _contentRegion == null)
            {
                return;
            }

            AppShellContentPreviewEntry entry = FindEntry(screen);
            VisualTreeAsset asset = entry?.ContentAsset;
            string title = entry == null
                ? null
                : ResolvePageTitle(entry.PageTitle, screen);
            string context = entry == null
                ? null
                : NormalizeOptionalText(entry.PageContext);
            AppShellPreviewRoute? navigation = entry?.ActiveNavigation;

            if (!force
                && _appliedScreen == screen
                && _appliedAsset == asset
                && _appliedTitle == title
                && _appliedContext == context
                && _appliedNavigation == navigation)
            {
                return;
            }

            _requestFallbackActive = false;

            if (screen == AppShellContentPreviewScreen.None)
            {
                ClearCurrentContent();
                ShowFallback(
                    "AppShell content preview",
                    "Select a migrated application screen in the AppShell Content Preview component.",
                    "Existing panels remain standalone until their migration prompt is completed.");
                SetAppliedTracking(screen, null, null, null, null);
                return;
            }

            if (entry == null)
            {
                ClearCurrentContent();
                ShowFallback(
                    "Preview screen is not configured",
                    $"No AppShell content entry is available for {GetScreenDisplayName(screen)}.",
                    "Add a content-only UXML asset after that panel has been migrated.");
                SetAppliedTracking(screen, null, null, null, null);
                return;
            }

            ApplyShellMetadata(entry, screen);

            if (asset == null)
            {
                ClearCurrentContent();
                ShowFallback(
                    $"{GetScreenDisplayName(screen)} preview is not assigned",
                    "Assign the migrated content-only UXML asset to this AppShell preview entry.",
                    "The existing standalone panel has not been replaced.");
                SetAppliedTracking(screen, null, title, context, navigation);
                return;
            }

            ClearCurrentContent();
            ClearFallback();

            TemplateContainer instance = asset.CloneTree();
            instance.style.width = Length.Percent(100);
            instance.style.height = Length.Percent(100);
            instance.style.flexGrow = 1;
            instance.style.flexShrink = 1;
            instance.AddToClassList(ContentInstanceClass);

            _contentRegion.Add(instance);
            _currentContentInstance = instance;
            _currentContentRoot = ResolveContentRoot(instance);
            if (_currentContentRoot != null)
            {
                _currentContentRoot.AddToClassList(ScreenContentClass);
                _currentContentRoot.AddToClassList(EmbeddedContentClass);
            }

            _currentScreenView = CreateScreenView(screen, _currentContentRoot);
            SetAppliedTracking(screen, asset, title, context, navigation);

            Debug.Log(
                $"[AppShellContentPreview] Showing {GetScreenDisplayName(screen)} " +
                "inside AppShell (preview only).");
        }

        private void ApplyShellMetadata(
            AppShellContentPreviewEntry entry,
            AppShellContentPreviewScreen screen)
        {
            if (_appShell == null || entry == null)
            {
                return;
            }

            _appShell.SetPreviewRoute(entry.ActiveNavigation);
            _appShell.SetPageTitle(
                ResolvePageTitle(entry.PageTitle, screen),
                NormalizeOptionalText(entry.PageContext));
        }

        private static VisualElement ResolveContentRoot(
            TemplateContainer instance)
        {
            if (instance == null)
            {
                return null;
            }

            return instance.childCount == 1
                ? instance.ElementAt(0)
                : instance;
        }

        private IAppScreenView CreateScreenView(
            AppShellContentPreviewScreen screen,
            VisualElement contentRoot)
        {
            switch (screen)
            {
                case AppShellContentPreviewScreen.Home:
                    return CreateHomeView(contentRoot);

                // Remaining screens gain explicit cases as each panel is migrated.
                // Returning null means the UXML is shown as a static visual preview
                // without screen-specific callbacks.
                default:
                    return null;
            }
        }

        private IAppScreenView CreateHomeView(VisualElement contentRoot)
        {
            var homeView = new HomePanelView(contentRoot);
            if (!homeView.IsBound)
            {
                Debug.LogWarning(
                    "[AppShellContentPreview] HomePanelView failed to bind home-root.");
                homeView.Dispose();
                return null;
            }

            homeView.ContinueMissionRequested += OnHomeContinueMissionRequested;
            homeView.QuizPortalRequested += OnHomeQuizPortalRequested;
            _currentHomeView = homeView;
            return homeView;
        }

        private void OnHomeContinueMissionRequested()
        {
            Debug.Log(
                "[AppShellContentPreview] Home Continue Mission requested — preview only.");

            _appShell?.ShowToast(
                "Continue Mission selected. Mission routing will be connected later.",
                AppShellToastTone.Information);
        }

        private void OnHomeQuizPortalRequested()
        {
            Debug.Log(
                "[AppShellContentPreview] Home Quiz Portal requested — preview only.");

            _appShell?.ShowToast(
                "Quiz Portal selected. Quiz routing will be connected later.",
                AppShellToastTone.Information);
        }

        private void ClearCurrentContent()
        {
            if (_currentHomeView != null)
            {
                _currentHomeView.ContinueMissionRequested -=
                    OnHomeContinueMissionRequested;

                _currentHomeView.QuizPortalRequested -=
                    OnHomeQuizPortalRequested;

                _currentHomeView = null;
            }

            _currentScreenView?.Dispose();
            _currentScreenView = null;

            if (_currentContentInstance != null)
            {
                _currentContentInstance.RemoveFromHierarchy();
                _currentContentInstance = null;
            }

            _currentContentRoot = null;
            _appliedAsset = null;
        }

        private void ShowFallback(
            string title,
            string message,
            string detail)
        {
            ClearFallback();

            if (_contentRegion == null)
            {
                return;
            }

            if (_dataStatePanelAsset == null)
            {
                if (!_warnedMissingDataStateAsset)
                {
                    Debug.LogWarning(
                        "[AppShellContentPreview] DataStatePanel VisualTreeAsset is not assigned. " +
                        "Assign Assets/NutriMind/App/UI/UXML/Shared/DataStatePanel.uxml " +
                        "to _dataStatePanelAsset to display preview fallback states.");
                    _warnedMissingDataStateAsset = true;
                }

                return;
            }

            _fallbackInstance = _dataStatePanelAsset.CloneTree();
            _fallbackInstance.style.width = Length.Percent(100);
            _fallbackInstance.style.height = Length.Percent(100);
            _fallbackInstance.style.flexGrow = 1;
            _fallbackInstance.style.flexShrink = 1;
            _fallbackInstance.AddToClassList(ContentInstanceClass);
            _contentRegion.Add(_fallbackInstance);

            _fallbackView = new DataStatePanelView(_fallbackInstance);
            if (!_fallbackView.IsBound)
            {
                Debug.LogWarning(
                    "[AppShellContentPreview] Failed to bind DataStatePanelView from the assigned fallback asset.");
                ClearFallback();
                return;
            }

            _fallbackView.SetState(DataStatePanelState.Empty);
            _fallbackView.Configure(
                title: title,
                message: message,
                detail: detail,
                iconClass: "ds-icon--info",
                primaryActionLabel: string.Empty,
                secondaryActionLabel: string.Empty);
        }

        private void ClearFallback()
        {
            _fallbackView?.Dispose();
            _fallbackView = null;

            if (_fallbackInstance != null)
            {
                _fallbackInstance.RemoveFromHierarchy();
                _fallbackInstance = null;
            }
        }

        private void OnPreviewRouteRequested(AppShellPreviewRoute route)
        {
            if (!_respondToShellNavigation)
            {
                return;
            }

            AppShellContentPreviewEntry entry = FindEntryForNavigation(route);
            if (entry != null)
            {
                SetPreviewScreen(entry.Screen);
                return;
            }

            ClearCurrentContent();
            ShowFallback(
                $"{GetRouteDisplayName(route)} is not migrated yet",
                "This AppShell navigation destination does not have a content-only preview assigned.",
                "Complete its migration or screen-creation prompt before adding it here.");
            HoldRequestFallback();
            Debug.Log(
                $"[AppShellContentPreview] {GetRouteDisplayName(route)} has no matching " +
                "content-only preview entry (preview only).");
        }

        private void OnProfileRequested()
        {
            if (!_respondToProfileRequest)
            {
                return;
            }

            AppShellContentPreviewEntry entry =
                FindEntry(AppShellContentPreviewScreen.Profile);
            if (entry != null)
            {
                SetPreviewScreen(entry.Screen);
                return;
            }

            ClearCurrentContent();
            ShowFallback(
                "Profile is not migrated yet",
                "Assign the content-only Profile panel after its migration prompt is complete.",
                "The existing standalone Profile panel remains unchanged.");
            HoldRequestFallback();
        }

        private void OnNotificationsRequested()
        {
            Debug.Log(
                "[AppShellContentPreview] Notifications requested — " +
                "no Announcements screen is assigned in this milestone preview.");
        }

        private void HoldRequestFallback()
        {
            _requestFallbackActive = true;
            _requestFallbackSelection = _previewScreen;
            ResetAppliedTracking();
        }

        private void SetAppliedTracking(
            AppShellContentPreviewScreen screen,
            VisualTreeAsset asset,
            string title,
            string context,
            AppShellPreviewRoute? navigation)
        {
            _appliedScreen = screen;
            _appliedAsset = asset;
            _appliedTitle = title;
            _appliedContext = context;
            _appliedNavigation = navigation;
        }

        private void ResetAppliedTracking()
        {
            _appliedScreen = null;
            _appliedAsset = null;
            _appliedTitle = null;
            _appliedContext = null;
            _appliedNavigation = null;
        }

        private static string ResolvePageTitle(
            string configuredTitle,
            AppShellContentPreviewScreen screen)
        {
            return string.IsNullOrWhiteSpace(configuredTitle)
                ? GetScreenDisplayName(screen)
                : configuredTitle.Trim();
        }

        private static string NormalizeOptionalText(string text)
        {
            return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        }

        private static string GetScreenDisplayName(
            AppShellContentPreviewScreen screen)
        {
            switch (screen)
            {
                case AppShellContentPreviewScreen.Home:
                    return "Home";
                case AppShellContentPreviewScreen.Subjects:
                    return "Subjects";
                case AppShellContentPreviewScreen.Terms:
                    return "Terms";
                case AppShellContentPreviewScreen.Missions:
                    return "Missions";
                case AppShellContentPreviewScreen.LockedMission:
                    return "Mission Availability";
                case AppShellContentPreviewScreen.Profile:
                    return "Profile";
                case AppShellContentPreviewScreen.Settings:
                    return "Settings";
                case AppShellContentPreviewScreen.Progress:
                    return "Progress";
                case AppShellContentPreviewScreen.QuizList:
                    return "Quiz Portal";
                case AppShellContentPreviewScreen.QuizDetail:
                    return "Quiz Details";
                case AppShellContentPreviewScreen.QuizAttempt:
                    return "Quiz";
                case AppShellContentPreviewScreen.QuizResult:
                    return "Quiz Result";
                default:
                    return "AppShell content preview";
            }
        }

        private static string GetRouteDisplayName(AppShellPreviewRoute route)
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
                    return route.ToString();
            }
        }

        private void ValidatePreviewEntries()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_previewEntries == null)
            {
                return;
            }

            var screens = new HashSet<AppShellContentPreviewScreen>();
            var navigationRoutes = new HashSet<AppShellPreviewRoute>();

            for (int i = 0; i < _previewEntries.Count; i++)
            {
                AppShellContentPreviewEntry entry = _previewEntries[i];
                if (entry == null)
                {
                    continue;
                }

                if (entry.Screen == AppShellContentPreviewScreen.None)
                {
                    Debug.LogWarning(
                        $"[AppShellContentPreview] Preview entry {i} uses None. " +
                        "None is reserved for the unselected fallback.");
                }
                else if (!screens.Add(entry.Screen))
                {
                    Debug.LogWarning(
                        $"[AppShellContentPreview] Multiple preview entries use {entry.Screen}. " +
                        "The first entry will be used.");
                }

                if (entry.SelectFromBottomNavigation
                    && !navigationRoutes.Add(entry.ActiveNavigation))
                {
                    Debug.LogWarning(
                        $"[AppShellContentPreview] Multiple bottom-navigation preview entries use " +
                        $"{entry.ActiveNavigation}. The first entry will be used.");
                }

                if (entry.ContentAsset == null)
                {
                    Debug.LogWarning(
                        $"[AppShellContentPreview] Preview entry {i} ({entry.Screen}) has no " +
                        "content-only VisualTreeAsset assigned.");
                }
            }
#endif
        }
    }
}
