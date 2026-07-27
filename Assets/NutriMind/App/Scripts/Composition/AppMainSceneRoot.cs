using NutriMind.App.Presentation;
using NutriMind.App.Routing;
using NutriMind.App.UI;
using NutriMind.Core.Bootstrap;
using NutriMind.Core.Utilities;
using UnityEngine;
using UnityEngine.UIElements;

namespace NutriMind.App.Composition
{
    /// <summary>
    /// Main application scene root.
    /// Creates <see cref="MainScreenCoordinator"/> and <see cref="AppShellRuntimeController"/>
    /// when the shell document is ready. All route-specific UXML assets are assigned here in
    /// the Inspector and forwarded to the coordinator via <see cref="MainScreenAssets"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AppMainSceneRoot : MonoBehaviour
    {
        [Header("Shell")]
        [SerializeField]
        private UIDocument _shellDocument;

        [SerializeField]
        private AppShellController _shellController;

        [SerializeField]
        private VisualTreeAsset _confirmDialogAsset;

        [SerializeField]
        private VisualTreeAsset _systemDialogAsset;

        [Header("Main Route UXML Assets")]
        [SerializeField]
        private VisualTreeAsset _homePanelAsset;

        [SerializeField]
        private VisualTreeAsset _subjectSelectionAsset;

        [SerializeField]
        private VisualTreeAsset _termSelectionAsset;

        [SerializeField]
        private VisualTreeAsset _missionSelectionAsset;

        [SerializeField]
        private VisualTreeAsset _lockedMissionAsset;

        [SerializeField]
        private VisualTreeAsset _missionDetailAsset;

        [SerializeField]
        private VisualTreeAsset _profileAsset;

        [SerializeField]
        private VisualTreeAsset _settingsAsset;

        [SerializeField]
        private VisualTreeAsset _progressAsset;

        [SerializeField]
        private VisualTreeAsset _rewardsAsset;

        [SerializeField]
        private VisualTreeAsset _certificatesAsset;

        [SerializeField]
        private VisualTreeAsset _announcementsAsset;

        [SerializeField]
        private VisualTreeAsset _leaderboardAsset;

        [SerializeField]
        private VisualTreeAsset _dataStatePanelAsset;

        private MainScreenCoordinator _coordinator;
        private AppShellRuntimeController _shellRuntime;

        private void Awake()
        {
            if (_shellDocument == null)
            {
                _shellDocument = GetComponent<UIDocument>();
            }

            if (_shellController == null)
            {
                _shellController = GetComponent<AppShellController>();
            }
        }

        private void OnEnable()
        {
            if (AppLifetime.HasInstance)
            {
                AppLifetime.Instance.Router?.EnsureMainRoot();
            }

            BindWhenReady();
        }

        private void OnDisable()
        {
            CancelInvoke(nameof(BindWhenReady));
            TeardownCoordinator();
        }

        private void BindWhenReady()
        {
            if (_shellDocument == null)
            {
                _shellDocument = GetComponent<UIDocument>();
            }

            if (_shellController == null)
            {
                _shellController = GetComponent<AppShellController>();
            }

            VisualElement root = _shellDocument != null ? _shellDocument.rootVisualElement : null;
            if (root == null || root.Q<VisualElement>("app-shell-root") == null)
            {
                Invoke(nameof(BindWhenReady), 0.05f);
                return;
            }

            StretchDocument(root);

            if (!AppLifetime.HasInstance || !AppLifetime.Instance.IsReady)
            {
                Invoke(nameof(BindWhenReady), 0.05f);
                return;
            }

            TeardownCoordinator();
            BuildCoordinator();

            NutriMindLog.Runtime("AppMainSceneRoot ready — coordinator active.");
        }

        private void BuildCoordinator()
        {
            AppLifetime lifetime = AppLifetime.Instance;

            AppModalHost modalHost = null;
            VisualElement modalLayer = _shellController?.GetModalLayer();
            if (modalLayer != null)
            {
                modalHost = new AppModalHost(modalLayer, _confirmDialogAsset, _systemDialogAsset);
            }

            _shellRuntime = new AppShellRuntimeController(
                _shellController,
                lifetime.Router,
                lifetime.AuthenticatedStudentState,
                lifetime.Connectivity,
                lifetime.SyncCoordinator,
                modalHost,
                lifetime.LifetimeToken);

            _shellRuntime.SignOutConfirmed += OnSignOutConfirmed;

            var assets = new MainScreenAssets
            {
                HomePanelAsset = _homePanelAsset,
                SubjectSelectionAsset = _subjectSelectionAsset,
                TermSelectionAsset = _termSelectionAsset,
                MissionSelectionAsset = _missionSelectionAsset,
                LockedMissionAsset = _lockedMissionAsset,
                MissionDetailAsset = _missionDetailAsset,
                ProfileAsset = _profileAsset,
                SettingsAsset = _settingsAsset,
                ProgressAsset = _progressAsset,
                RewardsAsset = _rewardsAsset,
                CertificatesAsset = _certificatesAsset,
                AnnouncementsAsset = _announcementsAsset,
                LeaderboardAsset = _leaderboardAsset,
                DataStatePanelAsset = _dataStatePanelAsset
            };

            _coordinator = new MainScreenCoordinator(
                lifetime,
                _shellController,
                _shellRuntime,
                assets);

            _coordinator.ApplyCurrentRoute();
        }

        private void TeardownCoordinator()
        {
            if (_shellRuntime != null)
            {
                _shellRuntime.SignOutConfirmed -= OnSignOutConfirmed;
                _shellRuntime.Dispose();
                _shellRuntime = null;
            }

            _coordinator?.Dispose();
            _coordinator = null;
        }

        private void OnSignOutConfirmed()
        {
            if (!AppLifetime.HasInstance)
            {
                return;
            }

            TaskUtilities.ForgetSafely(
                AppLifetime.Instance.HandleUnauthorizedAsync(AppLifetime.Instance.LifetimeToken),
                AppLifetime.Instance.LifetimeToken,
                "Main.SignOut");
        }

        private static void StretchDocument(VisualElement root)
        {
            root.style.flexGrow = 1;
            root.style.width = Length.Percent(100);
            root.style.height = Length.Percent(100);
        }
    }
}
