using System;
using System.Threading;
using NutriMind.App.Routing;
using NutriMind.App.State;
using NutriMind.App.UI;
using NutriMind.Core.Bootstrap;
using NutriMind.Core.Networking;
using NutriMind.Core.Sync;
using NutriMind.Core.Utilities;

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
        private readonly IConnectivityService _connectivity;
        private readonly SyncCoordinator _syncCoordinator;
        private readonly AppModalHost _modalHost;
        private readonly CancellationToken _lifetimeToken;
        private bool _disposed;

        public AppShellRuntimeController(
            AppShellController shell,
            IAppRouter router,
            AuthenticatedStudentState studentState,
            IConnectivityService connectivity,
            SyncCoordinator syncCoordinator,
            AppModalHost modalHost,
            CancellationToken lifetimeToken)
        {
            _shell = shell ?? throw new ArgumentNullException(nameof(shell));
            _router = router ?? throw new ArgumentNullException(nameof(router));
            _studentState = studentState;
            _connectivity = connectivity;
            _syncCoordinator = syncCoordinator;
            _modalHost = modalHost;
            _lifetimeToken = lifetimeToken;

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
            _modalHost?.ShowConfirm(
                ConfirmDialogPresets.SubmitQuiz(),
                onConfirm: onConfirm,
                onCancel: null);
        }

        /// <summary>
        /// Shows the use-reward confirm dialog.
        /// On confirm, raises <paramref name="onConfirm"/>.
        /// </summary>
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
            string title = string.IsNullOrWhiteSpace(rewardTitle) ? "Use Reward" : rewardTitle;
            var config = new ConfirmDialogConfiguration(
                title: "Use Reward",
                message: "Are you sure you want to use \"" + title + "\"? This cannot be undone.",
                confirmLabel: "Use Reward",
                cancelLabel: "Cancel",
                iconClass: "ds-icon--star",
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

            TaskUtilities.ForgetSafely(
                _router.PushAsync(AppRouteId.Announcements, AppRouteContext.Empty, _lifetimeToken),
                _lifetimeToken,
                "Shell.Announcements");
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
                case AppShellPreviewRoute.More:
                    return AppRouteId.Leaderboard;
                default:
                    return AppRouteId.Home;
            }
        }
    }
}
