using System;
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
    /// Manages the Main scene content region.
    /// Observes <see cref="IAppRouter.RouteChanged"/>, clones the appropriate UXML
    /// into the shell content region, constructs the matching runtime presenter,
    /// and disposes the prior presenter/view before loading the next.
    /// Does not own shell chrome, gateway, SQLite, or scene loading.
    /// </summary>
    public sealed class MainScreenCoordinator : IDisposable
    {
        private readonly AppLifetime _lifetime;
        private readonly AppShellController _shellController;
        private readonly AppShellRuntimeController _shellRuntime;

        // Per-route UXML assets (assigned from AppMainSceneRoot serialized fields).
        private readonly VisualTreeAsset _homePanelAsset;
        private readonly VisualTreeAsset _subjectSelectionAsset;
        private readonly VisualTreeAsset _termSelectionAsset;
        private readonly VisualTreeAsset _missionSelectionAsset;
        private readonly VisualTreeAsset _lockedMissionAsset;
        private readonly VisualTreeAsset _missionDetailAsset;
        private readonly VisualTreeAsset _profileAsset;
        private readonly VisualTreeAsset _settingsAsset;
        private readonly VisualTreeAsset _progressAsset;
        private readonly VisualTreeAsset _rewardsAsset;
        private readonly VisualTreeAsset _certificatesAsset;
        private readonly VisualTreeAsset _announcementsAsset;
        private readonly VisualTreeAsset _leaderboardAsset;
        private readonly VisualTreeAsset _dataStatePanelAsset;

        private TemplateContainer _activeInstance;
        private IDisposable _activePresenter;
        private IAppScreenView _activeView;
        private bool _disposed;

        public MainScreenCoordinator(
            AppLifetime lifetime,
            AppShellController shellController,
            AppShellRuntimeController shellRuntime,
            MainScreenAssets assets)
        {
            _lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
            _shellController = shellController;
            _shellRuntime = shellRuntime;

            if (assets != null)
            {
                _homePanelAsset = assets.HomePanelAsset;
                _subjectSelectionAsset = assets.SubjectSelectionAsset;
                _termSelectionAsset = assets.TermSelectionAsset;
                _missionSelectionAsset = assets.MissionSelectionAsset;
                _lockedMissionAsset = assets.LockedMissionAsset;
                _missionDetailAsset = assets.MissionDetailAsset;
                _profileAsset = assets.ProfileAsset;
                _settingsAsset = assets.SettingsAsset;
                _progressAsset = assets.ProgressAsset;
                _rewardsAsset = assets.RewardsAsset;
                _certificatesAsset = assets.CertificatesAsset;
                _announcementsAsset = assets.AnnouncementsAsset;
                _leaderboardAsset = assets.LeaderboardAsset;
                _dataStatePanelAsset = assets.DataStatePanelAsset;
            }

            if (_lifetime.Router != null)
            {
                _lifetime.Router.RouteChanged += OnRouteChanged;
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            if (_lifetime?.Router != null)
            {
                _lifetime.Router.RouteChanged -= OnRouteChanged;
            }

            TeardownActive();
        }

        /// <summary>
        /// Handles the current route immediately (e.g., on scene load).
        /// </summary>
        public void ApplyCurrentRoute()
        {
            if (_lifetime?.Router != null)
            {
                OnRouteChanged(_lifetime.Router.CurrentRoute);
            }
        }

        private void OnRouteChanged(AppRouteEntry entry)
        {
            if (_disposed)
            {
                return;
            }

            TeardownActive();
            BuildRouteContent(entry);
        }

        private void BuildRouteContent(AppRouteEntry entry)
        {
            VisualElement contentRegion = GetContentRegion();
            if (contentRegion == null)
            {
                NutriMindLog.RuntimeWarning(
                    "MainScreenCoordinator: content region not available for route " + entry.RouteId);
                return;
            }

            contentRegion.Clear();

            switch (entry.RouteId)
            {
                case AppRouteId.Home:
                    BuildHome(contentRegion, entry.Context);
                    UpdateShellChrome("Home", AppShellPreviewRoute.Home);
                    break;

                case AppRouteId.Subjects:
                    BuildSubjects(contentRegion, entry.Context);
                    UpdateShellChrome("Subjects", AppShellPreviewRoute.Subjects);
                    break;

                case AppRouteId.Terms:
                    BuildTerms(contentRegion, entry.Context);
                    UpdateShellChrome("Terms", AppShellPreviewRoute.Subjects);
                    break;

                case AppRouteId.MissionList:
                    BuildMissions(contentRegion, entry.Context);
                    UpdateShellChrome("Missions", AppShellPreviewRoute.Missions);
                    break;

                case AppRouteId.LockedMission:
                    BuildLockedMission(contentRegion, entry.Context);
                    UpdateShellChrome("Locked Mission", AppShellPreviewRoute.Missions);
                    break;

                case AppRouteId.MissionDetail:
                    BuildMissionDetail(contentRegion, entry.Context);
                    UpdateShellChrome("Mission Detail", AppShellPreviewRoute.Missions);
                    break;

                case AppRouteId.Profile:
                    BuildProfile(contentRegion, entry.Context);
                    UpdateShellChrome("Profile", AppShellPreviewRoute.More);
                    break;

                case AppRouteId.Settings:
                    BuildSettings(contentRegion, entry.Context);
                    UpdateShellChrome("Settings", AppShellPreviewRoute.More);
                    break;

                case AppRouteId.Progress:
                    BuildProgress(contentRegion, entry.Context);
                    UpdateShellChrome("Progress", AppShellPreviewRoute.Progress);
                    break;

                case AppRouteId.Rewards:
                    BuildRewards(contentRegion, entry.Context);
                    UpdateShellChrome("Rewards", AppShellPreviewRoute.Rewards);
                    break;

                case AppRouteId.Certificates:
                    BuildCertificates(contentRegion, entry.Context);
                    UpdateShellChrome("Certificates", AppShellPreviewRoute.Rewards);
                    break;

                case AppRouteId.Announcements:
                    BuildAnnouncements(contentRegion, entry.Context);
                    UpdateShellChrome("Announcements", AppShellPreviewRoute.More);
                    break;

                case AppRouteId.Leaderboard:
                    BuildLeaderboard(contentRegion, entry.Context);
                    UpdateShellChrome("Leaderboard", AppShellPreviewRoute.More);
                    break;

                default:
                    BuildPlaceholder(contentRegion, entry.RouteId.ToString());
                    break;
            }
        }

        private void BuildHome(VisualElement region, AppRouteContext ctx)
        {
            if (_homePanelAsset == null)
            {
                BuildPlaceholder(region, "Home");
                return;
            }

            _activeInstance = _homePanelAsset.Instantiate();
            region.Add(_activeInstance);
            var view = new HomePanelView(_activeInstance);
            _activeView = view;
            var presenter = new HomePresenter(_lifetime, view);
            _activePresenter = presenter;
            presenter.LoadAsync();
        }

        private void BuildSubjects(VisualElement region, AppRouteContext ctx)
        {
            if (_subjectSelectionAsset == null)
            {
                BuildPlaceholder(region, "Subjects");
                return;
            }

            _activeInstance = _subjectSelectionAsset.Instantiate();
            region.Add(_activeInstance);
            var view = new SubjectSelectionPanelView(_activeInstance);
            _activeView = view;
            var presenter = new SubjectsPresenter(_lifetime, view);
            _activePresenter = presenter;
            presenter.LoadAsync();
        }

        private void BuildTerms(VisualElement region, AppRouteContext ctx)
        {
            if (_termSelectionAsset == null)
            {
                BuildPlaceholder(region, "Terms");
                return;
            }

            _activeInstance = _termSelectionAsset.Instantiate();
            region.Add(_activeInstance);
            var view = new TermSelectionPanelView(_activeInstance);
            _activeView = view;
            var presenter = new TermsPresenter(_lifetime, view, ctx);
            _activePresenter = presenter;
            presenter.LoadAsync();
        }

        private void BuildMissions(VisualElement region, AppRouteContext ctx)
        {
            if (_missionSelectionAsset == null)
            {
                BuildPlaceholder(region, "Missions");
                return;
            }

            _activeInstance = _missionSelectionAsset.Instantiate();
            region.Add(_activeInstance);
            var view = new MissionSelectionPanelView(_activeInstance);
            _activeView = view;
            var presenter = new MissionListPresenter(_lifetime, view, ctx);
            _activePresenter = presenter;
            presenter.LoadAsync();
        }

        private void BuildLockedMission(VisualElement region, AppRouteContext ctx)
        {
            if (_lockedMissionAsset == null)
            {
                BuildPlaceholder(region, "Locked Mission");
                return;
            }

            _activeInstance = _lockedMissionAsset.Instantiate();
            region.Add(_activeInstance);
            var view = new LockedMissionPanelView(_activeInstance);
            _activeView = view;
            var presenter = new LockedMissionPresenter(_lifetime, view, ctx);
            _activePresenter = presenter;
            presenter.Load();
        }

        private void BuildMissionDetail(VisualElement region, AppRouteContext ctx)
        {
            if (_missionDetailAsset == null)
            {
                BuildPlaceholder(region, "Mission Detail");
                return;
            }

            _activeInstance = _missionDetailAsset.Instantiate();
            region.Add(_activeInstance);
            var view = new MissionDetailPanelView(_activeInstance, _dataStatePanelAsset);
            _activeView = view;
            var presenter = new MissionDetailPresenter(_lifetime, view, ctx, _shellRuntime);
            _activePresenter = presenter;
            presenter.LoadAsync();
        }

        private void BuildProfile(VisualElement region, AppRouteContext ctx)
        {
            if (_profileAsset == null)
            {
                BuildPlaceholder(region, "Profile");
                return;
            }

            _activeInstance = _profileAsset.Instantiate();
            region.Add(_activeInstance);
            var view = new ProfilePanelView(_activeInstance);
            _activeView = view;
            var presenter = new ProfilePresenter(_lifetime, view, _shellRuntime);
            _activePresenter = presenter;
            presenter.Load();
        }

        private void BuildSettings(VisualElement region, AppRouteContext ctx)
        {
            if (_settingsAsset == null)
            {
                BuildPlaceholder(region, "Settings");
                return;
            }

            _activeInstance = _settingsAsset.Instantiate();
            region.Add(_activeInstance);
            var view = new SettingsPanelView(_activeInstance);
            _activeView = view;
            var presenter = new SettingsPresenter(_lifetime, view);
            _activePresenter = presenter;
            presenter.Load();
        }

        private void BuildProgress(VisualElement region, AppRouteContext ctx)
        {
            if (_progressAsset == null)
            {
                BuildPlaceholder(region, "Progress");
                return;
            }

            _activeInstance = _progressAsset.Instantiate();
            region.Add(_activeInstance);
            var view = new ProgressPanelView(_activeInstance, _dataStatePanelAsset);
            _activeView = view;
            var presenter = new ProgressPresenter(_lifetime, view);
            _activePresenter = presenter;
            presenter.LoadAsync();
        }

        private void BuildRewards(VisualElement region, AppRouteContext ctx)
        {
            if (_rewardsAsset == null)
            {
                BuildPlaceholder(region, "Rewards");
                return;
            }

            _activeInstance = _rewardsAsset.Instantiate();
            region.Add(_activeInstance);
            var view = new RewardsPanelView(_activeInstance, _dataStatePanelAsset);
            _activeView = view;
            var presenter = new RewardsPresenter(_lifetime, view, _shellRuntime);
            _activePresenter = presenter;
            presenter.LoadAsync();
        }

        private void BuildCertificates(VisualElement region, AppRouteContext ctx)
        {
            if (_certificatesAsset == null)
            {
                BuildPlaceholder(region, "Certificates");
                return;
            }

            _activeInstance = _certificatesAsset.Instantiate();
            region.Add(_activeInstance);
            var view = new CertificatesPanelView(_activeInstance, _dataStatePanelAsset);
            _activeView = view;
            var presenter = new CertificatesPresenter(_lifetime, view);
            _activePresenter = presenter;
            presenter.LoadAsync();
        }

        private void BuildAnnouncements(VisualElement region, AppRouteContext ctx)
        {
            if (_announcementsAsset == null)
            {
                BuildPlaceholder(region, "Announcements");
                return;
            }

            _activeInstance = _announcementsAsset.Instantiate();
            region.Add(_activeInstance);
            var view = new AnnouncementsPanelView(_activeInstance, _dataStatePanelAsset);
            _activeView = view;
            var presenter = new AnnouncementsPresenter(_lifetime, view, _shellRuntime);
            _activePresenter = presenter;
            presenter.LoadAsync();
        }

        private void BuildLeaderboard(VisualElement region, AppRouteContext ctx)
        {
            if (_leaderboardAsset == null)
            {
                BuildPlaceholder(region, "Leaderboard");
                return;
            }

            _activeInstance = _leaderboardAsset.Instantiate();
            region.Add(_activeInstance);
            var view = new LeaderboardPanelView(_activeInstance, _dataStatePanelAsset);
            _activeView = view;
            var presenter = new LeaderboardPresenter(_lifetime, view);
            _activePresenter = presenter;
            presenter.LoadAsync();
        }

        private void TeardownActive()
        {
            _activePresenter?.Dispose();
            _activePresenter = null;
            _activeView?.Dispose();
            _activeView = null;

            if (_activeInstance != null)
            {
                _activeInstance.RemoveFromHierarchy();
                _activeInstance = null;
            }
        }

        private static void BuildPlaceholder(VisualElement region, string routeName)
        {
            var label = new Label(routeName + " — UXML asset not assigned. Assign in AppMainSceneRoot.");
            label.AddToClassList("app-screen-content");
            region.Add(label);
        }

        private VisualElement GetContentRegion()
        {
            return _shellController?.GetContentRegion();
        }

        private void UpdateShellChrome(string title, AppShellPreviewRoute activeNav)
        {
            _shellController?.SetPageTitle(title);
            _shellController?.SetPreviewRoute(activeNav);
            _shellController?.HideLoadingOverlay();
        }
    }

    /// <summary>
    /// Serializable asset bundle passed from AppMainSceneRoot to MainScreenCoordinator.
    /// All fields are optional; missing assets produce placeholder labels.
    /// </summary>
    [Serializable]
    public sealed class MainScreenAssets
    {
        public VisualTreeAsset HomePanelAsset;
        public VisualTreeAsset SubjectSelectionAsset;
        public VisualTreeAsset TermSelectionAsset;
        public VisualTreeAsset MissionSelectionAsset;
        public VisualTreeAsset LockedMissionAsset;
        public VisualTreeAsset MissionDetailAsset;
        public VisualTreeAsset ProfileAsset;
        public VisualTreeAsset SettingsAsset;
        public VisualTreeAsset ProgressAsset;
        public VisualTreeAsset RewardsAsset;
        public VisualTreeAsset CertificatesAsset;
        public VisualTreeAsset AnnouncementsAsset;
        public VisualTreeAsset LeaderboardAsset;
        public VisualTreeAsset DataStatePanelAsset;
    }
}
