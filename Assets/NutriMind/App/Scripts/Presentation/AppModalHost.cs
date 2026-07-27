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
        private VisualElement _focusRestoreTarget;
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
            UpdateModalLayerPicking();
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

            CaptureFocusTarget();
            HideSystemInternal();
            _onConfirm = onConfirm;
            _onCancel = onCancel;
            _busy = false;
            _confirmView.Show(configuration);
            SetConfirmBusy(false);
            UpdateModalLayerPicking();
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

            CaptureFocusTarget();
            HideConfirmInternal();
            _onSystemPrimary = onPrimary;
            _onSystemSecondary = onSecondary;
            _onSystemDismiss = onDismiss;
            _systemView.Show(configuration);
            UpdateModalLayerPicking();
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
            UpdateModalLayerPicking();
            RestoreFocus();
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
            UpdateModalLayerPicking();
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
            StretchOverlayInstance(_confirmInstance);
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
            StretchOverlayInstance(_systemInstance);
            _modalLayer.Add(_systemInstance);
            _systemView = new SystemDialogView(_systemInstance);
            _systemView.PrimaryActionRequested += OnSystemPrimary;
            _systemView.SecondaryActionRequested += OnSystemSecondary;
            _systemView.Dismissed += OnSystemDismiss;
        }

        private static void StretchOverlayInstance(TemplateContainer instance)
        {
            if (instance == null)
            {
                return;
            }

            instance.style.position = Position.Absolute;
            instance.style.left = 0;
            instance.style.top = 0;
            instance.style.right = 0;
            instance.style.bottom = 0;
            instance.style.width = Length.Percent(100);
            instance.style.height = Length.Percent(100);
            // Keep the wrapper non-blocking. Confirm/System roots own picking while visible.
            instance.pickingMode = PickingMode.Ignore;
        }

        private void OnConfirmConfirmed()
        {
            if (_busy)
            {
                return;
            }

            Action handler = _onConfirm;
            HideConfirmInternal();
            UpdateModalLayerPicking();
            RestoreFocus();
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
            UpdateModalLayerPicking();
            RestoreFocus();
            handler?.Invoke();
        }

        private void OnSystemPrimary()
        {
            Action handler = _onSystemPrimary;
            HideSystemInternal();
            UpdateModalLayerPicking();
            RestoreFocus();
            handler?.Invoke();
        }

        private void OnSystemSecondary()
        {
            Action handler = _onSystemSecondary;
            HideSystemInternal();
            UpdateModalLayerPicking();
            RestoreFocus();
            handler?.Invoke();
        }

        private void OnSystemDismiss()
        {
            Action handler = _onSystemDismiss;
            HideSystemInternal();
            UpdateModalLayerPicking();
            RestoreFocus();
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

        private void UpdateModalLayerPicking()
        {
            if (_modalLayer == null)
            {
                return;
            }

            if (IsModalVisible)
            {
                _modalLayer.pickingMode = PickingMode.Position;
                _modalLayer.EnableInClassList("app-shell__modal-layer--empty", false);
            }
            else
            {
                // More hub may still own the layer; leave Position if a more-hub child is visible.
                bool moreHubVisible = false;
                for (int i = 0; i < _modalLayer.childCount; i++)
                {
                    VisualElement child = _modalLayer[i];
                    if (child != null
                        && child.name == "app-shell-more-hub"
                        && child.style.display != DisplayStyle.None)
                    {
                        moreHubVisible = true;
                        break;
                    }
                }

                if (moreHubVisible)
                {
                    _modalLayer.pickingMode = PickingMode.Position;
                    _modalLayer.EnableInClassList("app-shell__modal-layer--empty", false);
                }
                else
                {
                    _modalLayer.pickingMode = PickingMode.Ignore;
                    _modalLayer.EnableInClassList("app-shell__modal-layer--empty", true);
                }
            }
        }

        private void CaptureFocusTarget()
        {
            _focusRestoreTarget = _modalLayer?.panel?.focusController?.focusedElement as VisualElement;
        }

        private void RestoreFocus()
        {
            if (_focusRestoreTarget != null && _focusRestoreTarget.panel != null)
            {
                _focusRestoreTarget.Focus();
            }

            _focusRestoreTarget = null;
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
