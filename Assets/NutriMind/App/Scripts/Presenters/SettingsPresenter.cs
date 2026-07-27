using NutriMind.App.Presentation;
using NutriMind.App.Routing;
using NutriMind.App.UI;
using NutriMind.Core.Bootstrap;
using NutriMind.Core.Utilities;

namespace NutriMind.App.Presentation
{
    /// <summary>
    /// Runtime presenter for the Settings route.
    /// Reads local settings from ILocalSettingsStore and binds them to the view.
    /// All settings mutated here are local-only; server-backed settings are deferred.
    /// </summary>
    public sealed class SettingsPresenter : RoutePresenterBase
    {
        private readonly SettingsPanelView _view;

        public SettingsPresenter(AppLifetime lifetime, SettingsPanelView view)
            : base(lifetime)
        {
            _view = view;
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
            _view.BackRequested -= OnBack;
        }

        private void ApplyState()
        {
            // SettingsPanelView loads AppLocalSettings internally on bind.
            // Runtime only needs the view constructed; store is available for future save hooks.
            _ = Lifetime.LocalSettingsStore;
        }

        private void OnBack()
        {
            if (Disposed)
            {
                return;
            }

            TaskUtilities.ForgetSafely(
                Lifetime.Router?.BackAsync(Cts.Token),
                Cts.Token,
                "Settings.Back");
        }
    }
}
