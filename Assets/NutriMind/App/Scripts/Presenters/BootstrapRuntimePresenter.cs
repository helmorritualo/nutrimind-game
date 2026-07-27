using System;
using System.Threading;
using System.Threading.Tasks;
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

        public void Start()
        {
            if (_disposed)
            {
                return;
            }

            TaskUtilities.ForgetSafely(HandleStartAsync(), _cts.Token, "Bootstrap.Start");
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

        private async Task HandleStartAsync()
        {
            if (_disposed)
            {
                return;
            }

            await _startup.RunAsync(_cts.Token).ConfigureAwait(false);
        }

        private void OnStateChanged(BootstrapPreviewState state)
        {
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

        private void OnRetryRequested()
        {
            TaskUtilities.ForgetSafely(HandleRetryAsync(), _cts.Token, "Bootstrap.Retry");
        }

        private async Task HandleRetryAsync()
        {
            if (_disposed)
            {
                return;
            }

            await _startup.RunAsync(_cts.Token).ConfigureAwait(false);
        }

        private void OnContinueOfflineRequested()
        {
            TaskUtilities.ForgetSafely(HandleContinueOfflineAsync(), _cts.Token, "Bootstrap.ContinueOffline");
        }

        private async Task HandleContinueOfflineAsync()
        {
            if (_disposed)
            {
                return;
            }

            await _startup.ContinueOfflineAsync(_cts.Token).ConfigureAwait(false);
            if (_startup.State == BootstrapPreviewState.Ready)
            {
                await NavigateMainAsync().ConfigureAwait(false);
            }
        }

        private void OnOpenLoginRequested()
        {
            TaskUtilities.ForgetSafely(HandleOpenLoginAsync(), _cts.Token, "Bootstrap.OpenLogin");
        }

        private async Task HandleOpenLoginAsync()
        {
            if (_disposed || _lifetime.SceneNavigator == null)
            {
                return;
            }

            await UnityMainThread.SwitchToMainAsync(_cts.Token).ConfigureAwait(false);
            if (_disposed)
            {
                return;
            }

            await _lifetime.SceneNavigator.LoadAsync(AppSceneId.Authentication, _cts.Token)
                .ConfigureAwait(false);
        }

        private void OnContinueToApplicationRequested()
        {
            TaskUtilities.ForgetSafely(NavigateMainAsync(), _cts.Token, "Bootstrap.ContinueMain");
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
            _view.Root?.schedule.Execute(() =>
            {
                if (_disposed)
                {
                    return;
                }

                TaskUtilities.ForgetSafely(NavigateMainAsync(), _cts.Token, "Bootstrap.AutoContinue");
            }).StartingIn(350);
        }

        private async Task NavigateMainAsync()
        {
            if (_disposed || _lifetime.SceneNavigator == null)
            {
                return;
            }

            await UnityMainThread.SwitchToMainAsync(_cts.Token).ConfigureAwait(false);
            if (_disposed)
            {
                return;
            }

            _lifetime.Router?.EnsureMainRoot();
            await _lifetime.SceneNavigator.LoadAsync(AppSceneId.Main, _cts.Token).ConfigureAwait(false);
        }
    }
}
