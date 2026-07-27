using System;
using NutriMind.App.UI;
using NutriMind.Core.Utilities;
using UnityEngine.UIElements;

namespace NutriMind.App.Presentation
{
    /// <summary>
    /// Single active modal host for ConfirmDialog and SystemDialog.
    /// Does not own gateway, SQLite, or routing logic.
    /// </summary>
    public sealed class AppModalHost : IDisposable
    {
        private readonly VisualElement _modalLayer;
        private readonly VisualTreeAsset _confirmAsset;
        private readonly VisualTreeAsset _systemAsset;

        private TemplateContainer _confirmInstance;
        private TemplateContainer _systemInstance;
        private ConfirmDialogView _confirmView;
        private SystemDialogView _systemView;
        private Action _onConfirm;
        private Action _onCancel;
        private Action _onSystemPrimary;
        private Action _onSystemSecondary;
        private Action _onSystemDismiss;
        private bool _busy;
        private bool _disposed;

        public AppModalHost(
            VisualElement modalLayer,
            VisualTreeAsset confirmAsset = null,
            VisualTreeAsset systemAsset = null)
        {
            _modalLayer = modalLayer ?? throw new ArgumentNullException(nameof(modalLayer));
            _confirmAsset = confirmAsset;
            _systemAsset = systemAsset;
        }

        public bool IsModalVisible =>
            (_confirmView != null && _confirmView.IsVisible)
            || (_systemView != null && _systemView.IsVisible);

        public void ShowConfirm(
            ConfirmDialogConfiguration configuration,
            Action onConfirm,
            Action onCancel = null)
        {
            if (_disposed)
            {
                return;
            }

            EnsureConfirmView();
            if (_confirmView == null)
            {
                NutriMindLog.RuntimeWarning("Confirm dialog asset is not available on AppModalHost.");
                return;
            }

            HideSystemInternal();
            _onConfirm = onConfirm;
            _onCancel = onCancel;
            _busy = false;
            _confirmView.Show(configuration);
            SetConfirmBusy(false);
        }

        public void ShowSystem(
            SystemDialogConfiguration configuration,
            Action onPrimary = null,
            Action onSecondary = null,
            Action onDismiss = null)
        {
            if (_disposed)
            {
                return;
            }

            EnsureSystemView();
            if (_systemView == null)
            {
                NutriMindLog.RuntimeWarning("System dialog asset is not available on AppModalHost.");
                return;
            }

            HideConfirmInternal();
            _onSystemPrimary = onPrimary;
            _onSystemSecondary = onSecondary;
            _onSystemDismiss = onDismiss;
            _systemView.Show(configuration);
        }

        public void SetConfirmBusy(bool busy)
        {
            _busy = busy;
            _confirmView?.SetConfirmEnabled(!busy);
            if (busy)
            {
                NutriMindLog.Runtime("Modal confirm is busy.");
            }
        }

        public void Hide()
        {
            HideConfirmInternal();
            HideSystemInternal();
            _busy = false;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            Hide();
            DetachConfirm();
            DetachSystem();
        }

        private void EnsureConfirmView()
        {
            if (_confirmView != null)
            {
                return;
            }

            if (_confirmAsset == null)
            {
                return;
            }

            _confirmInstance = _confirmAsset.Instantiate();
            _modalLayer.Add(_confirmInstance);
            _confirmView = new ConfirmDialogView(_confirmInstance);
            _confirmView.Confirmed += OnConfirmConfirmed;
            _confirmView.Cancelled += OnConfirmCancelled;
        }

        private void EnsureSystemView()
        {
            if (_systemView != null)
            {
                return;
            }

            if (_systemAsset == null)
            {
                return;
            }

            _systemInstance = _systemAsset.Instantiate();
            _modalLayer.Add(_systemInstance);
            _systemView = new SystemDialogView(_systemInstance);
            _systemView.PrimaryActionRequested += OnSystemPrimary;
            _systemView.SecondaryActionRequested += OnSystemSecondary;
            _systemView.Dismissed += OnSystemDismiss;
        }

        private void OnConfirmConfirmed()
        {
            if (_busy)
            {
                return;
            }

            Action handler = _onConfirm;
            HideConfirmInternal();
            handler?.Invoke();
        }

        private void OnConfirmCancelled()
        {
            if (_busy)
            {
                return;
            }

            Action handler = _onCancel;
            HideConfirmInternal();
            handler?.Invoke();
        }

        private void OnSystemPrimary()
        {
            Action handler = _onSystemPrimary;
            HideSystemInternal();
            handler?.Invoke();
        }

        private void OnSystemSecondary()
        {
            Action handler = _onSystemSecondary;
            HideSystemInternal();
            handler?.Invoke();
        }

        private void OnSystemDismiss()
        {
            Action handler = _onSystemDismiss;
            HideSystemInternal();
            handler?.Invoke();
        }

        private void HideConfirmInternal()
        {
            _onConfirm = null;
            _onCancel = null;
            _confirmView?.Hide();
        }

        private void HideSystemInternal()
        {
            _onSystemPrimary = null;
            _onSystemSecondary = null;
            _onSystemDismiss = null;
            _systemView?.Hide();
        }

        private void DetachConfirm()
        {
            if (_confirmView != null)
            {
                _confirmView.Confirmed -= OnConfirmConfirmed;
                _confirmView.Cancelled -= OnConfirmCancelled;
                _confirmView.Dispose();
                _confirmView = null;
            }

            if (_confirmInstance != null)
            {
                _confirmInstance.RemoveFromHierarchy();
                _confirmInstance = null;
            }
        }

        private void DetachSystem()
        {
            if (_systemView != null)
            {
                _systemView.PrimaryActionRequested -= OnSystemPrimary;
                _systemView.SecondaryActionRequested -= OnSystemSecondary;
                _systemView.Dismissed -= OnSystemDismiss;
                _systemView.Dispose();
                _systemView = null;
            }

            if (_systemInstance != null)
            {
                _systemInstance.RemoveFromHierarchy();
                _systemInstance = null;
            }
        }
    }
}
