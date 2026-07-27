using System;
using System.Collections.Generic;
using System.Threading;
using NutriMind.App.Routing;
using NutriMind.App.State;
using NutriMind.App.UI;
using NutriMind.Core.Bootstrap;
using NutriMind.Core.Data;
using NutriMind.Core.Networking;
using NutriMind.Core.Sync;
using NutriMind.Core.Utilities;
using UnityEngine.UIElements;

namespace NutriMind.App.Presentation
{
    /// <summary>
    /// Runtime wrapper for <see cref="AppShellController"/>.
    /// Bridges shell navigation events to <see cref="IAppRouter"/>, observes
    /// <see cref="AuthenticatedStudentState"/> for badge updates, drives the
    /// <see cref="AppModalHost"/> for sign-out / quiz-exit / quiz-submit / reward-use confirms,
    /// and updates the offline/sync banner from <see cref="IConnectivityService"/> state.
    /// Presentation layer only — never owns SQL, gateway calls, or routing decisions.
    /// </summary>
    public sealed class AppShellRuntimeController : IDisposable
    {
        private readonly AppShellController _shell;
        private readonly IAppRouter _router;
        private readonly AuthenticatedStudentState _studentState;
        private readonly AppLifetime _lifetime;
        private readonly IConnectivityService _connectivity;
        private readonly SyncCoordinator _syncCoordinator;
        private readonly AppModalHost _modalHost;
        private readonly CancellationToken _lifetimeToken;

        private VisualElement _moreHubRoot;
        private bool _disposed;

        public AppShellRuntimeController(
            AppShellController shell,
            IAppRouter router,
            AuthenticatedStudentState studentState,
            IConnectivityService connectivity,
            SyncCoordinator syncCoordinator,
            AppModalHost modalHost,
            CancellationToken lifetimeToken,
            AppLifetime lifetime = null)
        {
            _shell = shell ?? throw new ArgumentNullException(nameof(shell));
            _router = router ?? throw new ArgumentNullException(nameof(router));
            _studentState = studentState;
            _connectivity = connectivity;
            _syncCoordinator = syncCoordinator;
            _modalHost = modalHost;
            _lifetimeToken = lifetimeToken;
            _lifetime = lifetime;

            Subscribe();
            RefreshBadges();
            RefreshConnection();
        }

        /// <summary>
        /// Raised when the sign-out confirm is accepted by the learner.
        /// Owner is responsible for clearing authentication and navigating.
        /// </summary>
        public event Action SignOutConfirmed;

        /// <summary>
        /// Raised when the manual sync banner action is tapped.
        /// Owner triggers <see cref="SyncCoordinator.PushPendingAsync"/>.
        /// </summary>
        public event Action ManualSyncRequested;

        public AppModalHost ModalHost => _modalHost;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            HideMoreHub();
            Unsubscribe();
            _modalHost?.Dispose();
        }

        /// <summary>
        /// Updates announcement badge count from the live state.
        /// Call after reading announcements or when AuthenticatedStudentState raises Changed.
        /// </summary>
        public void RefreshBadges()
        {
            if (_shell == null || _studentState == null)
            {
                return;
            }

            int unread = AppViewMappers.ClampBadgeCount(_studentState.AnnouncementVisibleCount);
            _shell.SetAnnouncementUnreadCount(unread);
        }

        /// <summary>
        /// Updates the connection status chrome from the current connectivity state.
        /// </summary>
        public void RefreshConnection()
        {
            if (_shell == null || _connectivity == null)
            {
                return;
            }

            AppShellConnectionPreview preview = _connectivity.IsOnline
                ? AppShellConnectionPreview.Online
                : AppShellConnectionPreview.Offline;

            _shell.SetConnectionPreview(preview);
        }

        /// <summary>
        /// Shows the sync-pending banner with the current pending outbox count.
        /// </summary>
        public void ShowSyncPendingBanner(int pendingCount)
        {
            if (_shell == null)
            {
                return;
            }

            if (pendingCount > 0)
            {
                _shell.ShowOfflineSyncBanner(OfflineSyncBannerPresets.SyncPending(pendingCount));
                _shell.SetConnectionPreview(AppShellConnectionPreview.SyncPending);
            }
            else
            {
                _shell.HideOfflineSyncBanner();
                RefreshConnection();
            }
        }

        /// <summary>
        /// Shows the sync-error banner.
        /// </summary>
        public void ShowSyncErrorBanner()
        {
            _shell?.ShowOfflineSyncBanner(OfflineSyncBannerPresets.SyncError());
            _shell?.SetConnectionPreview(AppShellConnectionPreview.SyncError);
        }

        /// <summary>
        /// Shows the sign-out confirm dialog.
        /// On confirm, raises <see cref="SignOutConfirmed"/>.
        /// </summary>
        public void RequestSignOut()
        {
            HideMoreHub();
            _modalHost?.ShowConfirm(
                ConfirmDialogPresets.SignOut(),
                onConfirm: () => SignOutConfirmed?.Invoke(),
                onCancel: null);
        }

        /// <summary>
        /// Shows the exit-quiz confirm dialog.
        /// On confirm, raises <paramref name="onConfirm"/>.
        /// </summary>
        public void RequestExitQuiz(Action onConfirm)
        {
            HideMoreHub();
            _modalHost?.ShowConfirm(
                ConfirmDialogPresets.ExitQuiz(),
                onConfirm: onConfirm,
                onCancel: null);
        }

        /// <summary>
        /// Shows the submit-quiz confirm dialog.
        /// On confirm, raises <paramref name="onConfirm"/>.
        /// </summary>
        public void RequestSubmitQuiz(Action onConfirm)
        {
            HideMoreHub();
            _modalHost?.ShowConfirm(
                ConfirmDialogPresets.SubmitQuiz(),
                onConfirm: onConfirm,
                onCancel: null);
        }

        /// <summary>
        /// Shows a transient toast in the AppShell. Safe to call while disposed (no-ops).
        /// </summary>
        public void ShowToast(
            string message,
            AppShellToastTone tone = AppShellToastTone.Information,
            float durationSeconds = 3f)
        {
            if (_disposed)
            {
                return;
            }

            _shell?.ShowToast(message, tone, durationSeconds);
        }

        public void ShowGlobalLoading(string message = null)
        {
            if (_disposed || _shell == null)
            {
                return;
            }

            _shell.ShowLoadingOverlay(LoadingOverlayPresets.PreparingApplication());
        }

        public void HideGlobalLoading()
        {
            if (_disposed)
            {
                return;
            }

            _shell?.HideLoadingOverlay();
        }

        public void RequestUseReward(string rewardTitle, Action onConfirm)
        {
            HideMoreHub();
            string title = string.IsNullOrWhiteSpace(rewardTitle) ? "Use Reward" : rewardTitle;
            var config = new ConfirmDialogConfiguration(
                title: "Use Reward",
                message: "Are you sure you want to use \"" + title + "\"? This cannot be undone.",
                confirmLabel: "Use Reward",
                cancelLabel: "Cancel",
                iconClass: "ds-icon--gift",
                tone: ConfirmDialogTone.Warning);
            _modalHost?.ShowConfirm(config, onConfirm: onConfirm, onCancel: null);
        }

        private void Subscribe()
        {
            if (_shell != null)
            {
                _shell.PreviewRouteRequested += OnPreviewRouteRequested;
                _shell.ProfileRequested += OnProfileRequested;
                _shell.NotificationsRequested += OnNotificationsRequested;
            }

            if (_studentState != null)
            {
                _studentState.Changed += OnStudentStateChanged;
            }
        }

        private void Unsubscribe()
        {
            if (_shell != null)
            {
                _shell.PreviewRouteRequested -= OnPreviewRouteRequested;
                _shell.ProfileRequested -= OnProfileRequested;
                _shell.NotificationsRequested -= OnNotificationsRequested;
            }

            if (_studentState != null)
            {
                _studentState.Changed -= OnStudentStateChanged;
            }
        }

        private void OnStudentStateChanged()
        {
            RefreshBadges();
        }

        private void OnPreviewRouteRequested(AppShellPreviewRoute route)
        {
            if (_router == null)
            {
                return;
            }

            if (route == AppShellPreviewRoute.More)
            {
                ShowMoreHub();
                return;
            }

            HideMoreHub();

            if (route == AppShellPreviewRoute.Missions)
            {
                NavigateMissionsBottomNav();
                return;
            }

            AppRouteId targetRoute = MapPreviewRouteToRouteId(route);
            TaskUtilities.ForgetSafely(
                _router.NavigateAsync(targetRoute, AppRouteContext.Empty, _lifetimeToken),
                _lifetimeToken,
                "Shell.NavRoute");
        }

        private void OnProfileRequested()
        {
            if (_router == null)
            {
                return;
            }

            HideMoreHub();
            TaskUtilities.ForgetSafely(
                _router.PushAsync(AppRouteId.Profile, AppRouteContext.Empty, _lifetimeToken),
                _lifetimeToken,
                "Shell.Profile");
        }

        private void OnNotificationsRequested()
        {
            if (_router == null)
            {
                return;
            }

            HideMoreHub();
            TaskUtilities.ForgetSafely(
                _router.PushAsync(AppRouteId.Announcements, AppRouteContext.Empty, _lifetimeToken),
                _lifetimeToken,
                "Shell.Announcements");
        }

        private void NavigateMissionsBottomNav()
        {
            MissionSummary active = _studentState?.ActiveMission;
            if (HasSubjectAndTerm(active))
            {
                TaskUtilities.ForgetSafely(
                    _router.NavigateAsync(
                        AppRouteId.MissionList,
                        AppRouteContext.ForTerm(active.SubjectId, active.TermId),
                        _lifetimeToken),
                    _lifetimeToken,
                    "Shell.MissionsActive");
                return;
            }

            MissionSummary bootstrapMission = FindFirstUsableBootstrapMission();
            if (HasSubjectAndTerm(bootstrapMission))
            {
                TaskUtilities.ForgetSafely(
                    _router.NavigateAsync(
                        AppRouteId.MissionList,
                        AppRouteContext.ForTerm(bootstrapMission.SubjectId, bootstrapMission.TermId),
                        _lifetimeToken),
                    _lifetimeToken,
                    "Shell.MissionsBootstrap");
                return;
            }

            TaskUtilities.ForgetSafely(
                _router.NavigateAsync(AppRouteId.Subjects, AppRouteContext.Empty, _lifetimeToken),
                _lifetimeToken,
                "Shell.MissionsSubjects");
        }

        private MissionSummary FindFirstUsableBootstrapMission()
        {
            IReadOnlyList<MissionSummary> missions = _lifetime?.LastBootstrap?.Missions;
            if (missions == null || missions.Count == 0)
            {
                return null;
            }

            for (int i = 0; i < missions.Count; i++)
            {
                if (HasSubjectAndTerm(missions[i]))
                {
                    return missions[i];
                }
            }

            return null;
        }

        private static bool HasSubjectAndTerm(MissionSummary mission)
        {
            return mission != null
                && !string.IsNullOrWhiteSpace(mission.SubjectId)
                && !string.IsNullOrWhiteSpace(mission.TermId);
        }

        private void ShowMoreHub()
        {
            if (_disposed || _shell == null)
            {
                return;
            }

            VisualElement modalLayer = _shell.GetModalLayer();
            if (modalLayer == null)
            {
                return;
            }

            _shell.SetPreviewRoute(AppShellPreviewRoute.More);
            EnsureMoreHub(modalLayer);
            _moreHubRoot.pickingMode = PickingMode.Position;
            _moreHubRoot.style.display = DisplayStyle.Flex;
            modalLayer.pickingMode = PickingMode.Position;
            modalLayer.EnableInClassList("app-shell__modal-layer--empty", false);
        }

        private void HideMoreHub()
        {
            if (_moreHubRoot != null)
            {
                _moreHubRoot.style.display = DisplayStyle.None;
                _moreHubRoot.pickingMode = PickingMode.Ignore;
            }

            if (_modalHost != null && _modalHost.IsModalVisible)
            {
                return;
            }

            VisualElement modalLayer = _shell?.GetModalLayer();
            if (modalLayer == null)
            {
                return;
            }

            modalLayer.pickingMode = PickingMode.Ignore;
            modalLayer.EnableInClassList("app-shell__modal-layer--empty", true);
        }

        private void EnsureMoreHub(VisualElement modalLayer)
        {
            if (_moreHubRoot != null)
            {
                return;
            }

            _moreHubRoot = new VisualElement();
            _moreHubRoot.name = "app-shell-more-hub";
            _moreHubRoot.AddToClassList("app-shell__more-hub");
            _moreHubRoot.style.position = Position.Absolute;
            _moreHubRoot.style.left = 0;
            _moreHubRoot.style.top = 0;
            _moreHubRoot.style.right = 0;
            _moreHubRoot.style.bottom = 0;
            _moreHubRoot.style.width = Length.Percent(100);
            _moreHubRoot.style.height = Length.Percent(100);
            _moreHubRoot.style.display = DisplayStyle.None;
            _moreHubRoot.pickingMode = PickingMode.Ignore;

            var backdrop = new VisualElement();
            backdrop.AddToClassList("app-shell__more-hub-backdrop");
            backdrop.RegisterCallback<ClickEvent>(_ => HideMoreHub());
            _moreHubRoot.Add(backdrop);

            var card = new VisualElement();
            card.AddToClassList("app-shell__more-hub-card");

            var header = new VisualElement();
            header.AddToClassList("app-shell__more-hub-header");
            header.pickingMode = PickingMode.Ignore;

            var headerIconBg = new VisualElement();
            headerIconBg.AddToClassList("app-shell__more-hub-header-icon-bg");
            headerIconBg.pickingMode = PickingMode.Ignore;

            var headerIcon = new VisualElement();
            headerIcon.AddToClassList("ds-icon");
            headerIcon.AddToClassList("ds-icon--more-horizontal");
            headerIcon.AddToClassList("app-shell__more-hub-header-icon");
            headerIcon.pickingMode = PickingMode.Ignore;
            headerIconBg.Add(headerIcon);
            header.Add(headerIconBg);

            var headerCopy = new VisualElement();
            headerCopy.AddToClassList("app-shell__more-hub-header-copy");
            headerCopy.pickingMode = PickingMode.Ignore;

            var title = new Label("More");
            title.AddToClassList("app-shell__more-hub-title");
            title.pickingMode = PickingMode.Ignore;
            headerCopy.Add(title);

            var subtitle = new Label("Choose a secondary destination.");
            subtitle.AddToClassList("app-shell__more-hub-subtitle");
            subtitle.pickingMode = PickingMode.Ignore;
            headerCopy.Add(subtitle);
            header.Add(headerCopy);
            card.Add(header);

            var list = new VisualElement();
            list.AddToClassList("app-shell__more-hub-list");
            AddMoreDestination(list, "Profile", "ds-icon--user", AppRouteId.Profile);
            AddMoreDestination(list, "Settings", "ds-icon--settings", AppRouteId.Settings);
            AddMoreDestination(list, "Certificates", "ds-icon--medal", AppRouteId.Certificates);
            AddMoreDestination(list, "Announcements", "ds-icon--bell", AppRouteId.Announcements);
            AddMoreDestination(list, "Leaderboard", "ds-icon--leaderboard", AppRouteId.Leaderboard);
            card.Add(list);

            var closeButton = new Button(() => HideMoreHub()) { text = "Close" };
            closeButton.AddToClassList("ds-btn");
            closeButton.AddToClassList("ds-btn--secondary");
            closeButton.AddToClassList("app-shell__more-hub-close");
            card.Add(closeButton);

            _moreHubRoot.Add(card);
            modalLayer.Add(_moreHubRoot);
        }

        private void AddMoreDestination(
            VisualElement list,
            string label,
            string iconClass,
            AppRouteId routeId)
        {
            var button = new Button(() => OnMoreDestinationSelected(routeId));
            button.AddToClassList("app-shell__more-hub-item");

            var iconBg = new VisualElement();
            iconBg.AddToClassList("app-shell__more-hub-item-icon-bg");
            iconBg.pickingMode = PickingMode.Ignore;

            var icon = new VisualElement();
            icon.AddToClassList("ds-icon");
            icon.AddToClassList(iconClass);
            icon.AddToClassList("app-shell__more-hub-item-icon");
            icon.pickingMode = PickingMode.Ignore;
            iconBg.Add(icon);
            button.Add(iconBg);

            var labelElement = new Label(label);
            labelElement.AddToClassList("app-shell__more-hub-item-label");
            labelElement.pickingMode = PickingMode.Ignore;
            button.Add(labelElement);

            var chevron = new VisualElement();
            chevron.AddToClassList("ds-icon");
            chevron.AddToClassList("ds-icon--chevron-right");
            chevron.AddToClassList("app-shell__more-hub-item-chevron");
            chevron.pickingMode = PickingMode.Ignore;
            button.Add(chevron);

            list.Add(button);
        }

        private void OnMoreDestinationSelected(AppRouteId routeId)
        {
            HideMoreHub();
            _shell?.SetPreviewRoute(AppShellPreviewRoute.More);
            TaskUtilities.ForgetSafely(
                _router.PushAsync(routeId, AppRouteContext.Empty, _lifetimeToken),
                _lifetimeToken,
                "Shell.More." + routeId);
        }

        private static AppRouteId MapPreviewRouteToRouteId(AppShellPreviewRoute route)
        {
            switch (route)
            {
                case AppShellPreviewRoute.Subjects:
                    return AppRouteId.Subjects;
                case AppShellPreviewRoute.Missions:
                    return AppRouteId.MissionList;
                case AppShellPreviewRoute.Progress:
                    return AppRouteId.Progress;
                case AppShellPreviewRoute.Rewards:
                    return AppRouteId.Rewards;
                default:
                    return AppRouteId.Home;
            }
        }
    }
}
