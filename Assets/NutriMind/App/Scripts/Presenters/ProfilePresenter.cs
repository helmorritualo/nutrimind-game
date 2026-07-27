using NutriMind.App.Presentation;
using NutriMind.App.Routing;
using NutriMind.App.UI;
using NutriMind.Core.Bootstrap;
using NutriMind.Core.Data;
using NutriMind.Core.Utilities;

namespace NutriMind.App.Presentation
{
    /// <summary>
    /// Runtime presenter for the Profile route.
    /// Binds profile data from AuthenticatedStudentState.
    /// LRN is always displayed masked — never exposed in full.
    /// Raises sign-out via shell confirm.
    /// </summary>
    public sealed class ProfilePresenter : RoutePresenterBase
    {
        private readonly ProfilePanelView _view;
        private readonly AppShellRuntimeController _shellRuntime;

        public ProfilePresenter(AppLifetime lifetime, ProfilePanelView view, AppShellRuntimeController shellRuntime)
            : base(lifetime)
        {
            _view = view;
            _shellRuntime = shellRuntime;
            _view.SignOutRequested += OnSignOutRequested;
            _view.SettingsRequested += OnSettingsRequested;
            _view.BackRequested += OnBack;
        }

        public void Load()
        {
            if (Disposed)
            {
                return;
            }

            ApplyState();
        }

        protected override void OnDispose()
        {
            _view.SignOutRequested -= OnSignOutRequested;
            _view.SettingsRequested -= OnSettingsRequested;
            _view.BackRequested -= OnBack;
        }

        private void ApplyState()
        {
            StudentProfile profile = Lifetime.AuthenticatedStudentState?.Profile;
            if (profile == null)
            {
                return;
            }

            _view.Bind(
                AppViewMappers.FormatDisplayName(profile),
                AppViewMappers.FormatGradeSection(profile),
                AppViewMappers.MaskLrn(profile),
                avatarKey: null);
        }

        private void OnSignOutRequested()
        {
            if (Disposed)
            {
                return;
            }

            _shellRuntime?.RequestSignOut();
        }

        private void OnSettingsRequested()
        {
            if (Disposed)
            {
                return;
            }

            TaskUtilities.ForgetSafely(
                Lifetime.Router?.PushAsync(
                    AppRouteId.Settings,
                    AppRouteContext.Empty,
                    NavigationToken),
                NavigationToken,
                "Profile.Settings");
        }

        private void OnBack()
        {
            if (Disposed)
            {
                return;
            }

            TaskUtilities.ForgetSafely(
                Lifetime.Router?.BackAsync(NavigationToken),
                NavigationToken,
                "Profile.Back");
        }
    }
}
