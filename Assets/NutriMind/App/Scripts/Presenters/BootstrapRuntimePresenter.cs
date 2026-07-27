using System;
using System.Threading;
using NutriMind.App.Features;
using NutriMind.App.Routing;
using NutriMind.App.UI;
using NutriMind.Core.Bootstrap;
using NutriMind.Core.Utilities;
using UnityEngine;

namespace NutriMind.App.Presentation
{
    /// <summary>
    /// Maps startup coordinator states onto <see cref="BootstrapPanelView"/>.
    /// Never calls SQLite or the gateway directly.
    /// </summary>
    public sealed class BootstrapRuntimePresenter : IDisposable
    {
        private readonly AppLifetime _lifetime;
        private readonly AppStartupCoordinator _startup;
        private readonly BootstrapPanelView _view;
        private readonly CancellationTokenSource _cts;
        private bool _disposed;
        private bool _autoContinueScheduled;

        public BootstrapRuntimePresenter(
            AppLifetime lifetime,
            AppStartupCoordinator startup,
            BootstrapPanelView view)
        {
            _lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
            _startup = startup ?? throw new ArgumentNullException(nameof(startup));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _cts = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.LifetimeToken);

            _startup.StateChanged += OnStateChanged;
            _view.RetryRequested += OnRetryRequested;
            _view.ContinueOfflineRequested += OnContinueOfflineRequested;
            _view.OpenLoginRequested += OnOpenLoginRequested;
            _view.ContinueToApplicationRequested += OnContinueToApplicationRequested;
            _view.UpdateApplicationRequested += OnUpdateApplicationRequested;

            _view.SetState(_startup.State);
        }

        public async void Start()
        {
            if (_disposed)
            {
                return;
            }

            await _startup.RunAsync(_cts.Token);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _startup.StateChanged -= OnStateChanged;
            _view.RetryRequested -= OnRetryRequested;
            _view.ContinueOfflineRequested -= OnContinueOfflineRequested;
            _view.OpenLoginRequested -= OnOpenLoginRequested;
            _view.ContinueToApplicationRequested -= OnContinueToApplicationRequested;
            _view.UpdateApplicationRequested -= OnUpdateApplicationRequested;
            _startup.Cancel();

            try
            {
                _cts.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // ignore
            }

            _cts.Dispose();
        }

        private void OnStateChanged(BootstrapPreviewState state)
        {
            // May arrive via posted main-thread callback; still guard binding/disposal.
            UnityMainThread.Post(() =>
            {
                if (_disposed || !_view.IsBound)
                {
                    return;
                }

                _view.SetState(state);
                if (state == BootstrapPreviewState.Ready)
                {
                    ScheduleAutoContinueToMain();
                }
            });
        }

        private async void OnRetryRequested()
        {
            if (_disposed)
            {
                return;
            }

            await _startup.RunAsync(_cts.Token);
        }

        private async void OnContinueOfflineRequested()
        {
            if (_disposed)
            {
                return;
            }

            await _startup.ContinueOfflineAsync(_cts.Token);
            if (_startup.State == BootstrapPreviewState.Ready)
            {
                await NavigateMainAsync();
            }
        }

        private async void OnOpenLoginRequested()
        {
            if (_disposed || _lifetime.SceneNavigator == null)
            {
                return;
            }

            await UnityMainThread.SwitchToMainAsync(_cts.Token);
            if (_disposed)
            {
                return;
            }

            await _lifetime.SceneNavigator.LoadAsync(AppSceneId.Authentication, _cts.Token);
        }

        private async void OnContinueToApplicationRequested()
        {
            await NavigateMainAsync();
        }

        private void OnUpdateApplicationRequested()
        {
            NutriMindLog.StartupWarning("Update App requested — store update flow is not wired in Prompt 1.");
            Application.OpenURL("https://play.google.com/store");
        }

        private void ScheduleAutoContinueToMain()
        {
            if (_autoContinueScheduled || _disposed)
            {
                return;
            }

            _autoContinueScheduled = true;
            _view.Root?.schedule.Execute(async () =>
            {
                if (_disposed)
                {
                    return;
                }

                await NavigateMainAsync();
            }).StartingIn(350);
        }

        private async System.Threading.Tasks.Task NavigateMainAsync()
        {
            if (_disposed || _lifetime.SceneNavigator == null)
            {
                return;
            }

            await UnityMainThread.SwitchToMainAsync(_cts.Token);
            if (_disposed)
            {
                return;
            }

            _lifetime.Router?.EnsureMainRoot();
            await _lifetime.SceneNavigator.LoadAsync(AppSceneId.Main, _cts.Token);
        }
    }
}
