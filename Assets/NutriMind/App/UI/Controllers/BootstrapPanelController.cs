using UnityEngine;
using UnityEngine.UIElements;

namespace NutriMind.App.UI
{
    /// <summary>
    /// Standalone UI Toolkit preview adapter for <see cref="BootstrapPanelView"/>.
    /// Presentation only — applies inspector preview states and logs request events.
    /// Does not perform startup checks, authentication, networking, SQLite, sync,
    /// application updates, scene loading, or production routing.
    /// Never enables Login or AppShell.
    /// <para>
    /// Future integration (request events only — not implemented here):
    /// // OpenLoginRequested maps to Login.
    /// // ContinueOfflineRequested maps to AppShell only after local eligibility.
    /// // ContinueToApplicationRequested maps to AppShell after ready
    /// // (future production auto-continues at 100% — Ready shows no Continue button).
    /// // UpdateApplicationRequested maps to the approved update flow.
    /// // RetryRequested restarts the appropriate startup operation.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class BootstrapPanelController : MonoBehaviour
    {
        private static readonly BootstrapPreviewState[] PreviewStateOrder =
        {
            BootstrapPreviewState.InitializingLocalStorage,
            BootstrapPreviewState.CheckingSecureToken,
            BootstrapPreviewState.CheckingConnectivity,
            BootstrapPreviewState.CheckingClientVersion,
            BootstrapPreviewState.CheckingManifest,
            BootstrapPreviewState.LoadingBootstrap,
            BootstrapPreviewState.OfflineEligible,
            BootstrapPreviewState.AuthenticationRequired,
            BootstrapPreviewState.Maintenance,
            BootstrapPreviewState.RequiredUpdate,
            BootstrapPreviewState.RecoverableError,
            BootstrapPreviewState.Ready
        };

        [Tooltip("UI-only preview state. Switches bootstrap copy, progress, spinner/icon, and actions — no real startup checks run.")]
        [SerializeField]
        private BootstrapPreviewState _previewState =
            BootstrapPreviewState.InitializingLocalStorage;

        [SerializeField]
        private bool _showReference = false;

        [SerializeField]
        private string _referenceText =
            "Startup preview • No real checks are running";

        private UIDocument _uiDocument;
        private BootstrapPanelView _view;
        private BootstrapPreviewState? _appliedPreviewState;
        private bool? _appliedShowReference;
        private string _appliedReferenceText;
        private bool _eventsRegistered;

        private void OnEnable()
        {
            _uiDocument = GetComponent<UIDocument>();
            BindWhenReady();
        }

        private void OnDisable()
        {
            Unbind();
            CancelInvoke(nameof(BindWhenReady));
        }

        private void OnValidate()
        {
            if (!isActiveAndEnabled || _view == null || !_view.IsBound)
            {
                return;
            }

            ApplyPreviewState(force: true);
        }

        /// <summary>
        /// Applies a preview state from code or tooling. Presentation only.
        /// </summary>
        public void SetPreviewState(BootstrapPreviewState state)
        {
            _previewState = state;

            if (_view == null || !_view.IsBound)
            {
                BindWhenReady();
                if (_view == null || !_view.IsBound)
                {
                    return;
                }
            }

            ApplyPreviewState(force: true);
        }

        /// <summary>
        /// Advances to the next preview state in contract order. Presentation only.
        /// </summary>
        [ContextMenu("Show Next Preview State")]
        public void ShowNextPreviewState()
        {
            int index = IndexOfPreviewState(_previewState);
            int next = (index + 1) % PreviewStateOrder.Length;
            SetPreviewState(PreviewStateOrder[next]);
        }

        /// <summary>
        /// Moves to the previous preview state in contract order. Presentation only.
        /// </summary>
        [ContextMenu("Show Previous Preview State")]
        public void ShowPreviousPreviewState()
        {
            int index = IndexOfPreviewState(_previewState);
            int previous = (index - 1 + PreviewStateOrder.Length) % PreviewStateOrder.Length;
            SetPreviewState(PreviewStateOrder[previous]);
        }

        private void BindWhenReady()
        {
            if (_uiDocument == null)
            {
                _uiDocument = GetComponent<UIDocument>();
            }

            if (_uiDocument == null)
            {
                return;
            }

            VisualElement panelRoot = _uiDocument.rootVisualElement;
            if (panelRoot == null)
            {
                Invoke(nameof(BindWhenReady), 0.05f);
                return;
            }

            VisualElement componentRoot = panelRoot.name == "bootstrap-root"
                ? panelRoot
                : panelRoot.Q<VisualElement>("bootstrap-root");

            if (componentRoot == null)
            {
                Invoke(nameof(BindWhenReady), 0.05f);
                return;
            }

            panelRoot.style.flexGrow = 1;
            panelRoot.style.width = Length.Percent(100);
            panelRoot.style.height = Length.Percent(100);

            UnbindViewOnly();
            _view = new BootstrapPanelView(componentRoot);
            if (!_view.IsBound)
            {
                _view.Dispose();
                _view = null;
                Invoke(nameof(BindWhenReady), 0.05f);
                return;
            }

            RegisterViewEvents();
            ApplyPreviewState(force: true);
        }

        private void Unbind()
        {
            UnbindViewOnly();
            _uiDocument = null;
            _appliedPreviewState = null;
            _appliedShowReference = null;
            _appliedReferenceText = null;
        }

        private void UnbindViewOnly()
        {
            UnregisterViewEvents();

            if (_view != null)
            {
                _view.Dispose();
                _view = null;
            }
        }

        private void RegisterViewEvents()
        {
            if (_view == null || _eventsRegistered)
            {
                return;
            }

            _view.RetryRequested += OnRetryRequested;
            _view.ContinueOfflineRequested += OnContinueOfflineRequested;
            _view.OpenLoginRequested += OnOpenLoginRequested;
            _view.UpdateApplicationRequested += OnUpdateApplicationRequested;
            _view.ContinueToApplicationRequested += OnContinueToApplicationRequested;
            _eventsRegistered = true;
        }

        private void UnregisterViewEvents()
        {
            if (_view == null || !_eventsRegistered)
            {
                _eventsRegistered = false;
                return;
            }

            _view.RetryRequested -= OnRetryRequested;
            _view.ContinueOfflineRequested -= OnContinueOfflineRequested;
            _view.OpenLoginRequested -= OnOpenLoginRequested;
            _view.UpdateApplicationRequested -= OnUpdateApplicationRequested;
            _view.ContinueToApplicationRequested -= OnContinueToApplicationRequested;
            _eventsRegistered = false;
        }

        private void ApplyPreviewState(bool force = false)
        {
            if (_view == null || !_view.IsBound)
            {
                return;
            }

            bool stateChanged = force
                || !_appliedPreviewState.HasValue
                || _appliedPreviewState.Value != _previewState;

            bool referenceVisibilityChanged = force
                || !_appliedShowReference.HasValue
                || _appliedShowReference.Value != _showReference;

            bool referenceTextChanged = force
                || !string.Equals(_appliedReferenceText, _referenceText, System.StringComparison.Ordinal);

            if (!stateChanged && !referenceVisibilityChanged && !referenceTextChanged)
            {
                return;
            }

            if (stateChanged)
            {
                _view.SetState(_previewState);
                _appliedPreviewState = _previewState;
            }

            if (referenceTextChanged)
            {
                _view.SetReferenceText(_referenceText);
                _appliedReferenceText = _referenceText;
            }

            if (referenceVisibilityChanged)
            {
                _view.SetReferenceVisible(_showReference);
                _appliedShowReference = _showReference;
            }
        }

        private static int IndexOfPreviewState(BootstrapPreviewState state)
        {
            for (int i = 0; i < PreviewStateOrder.Length; i++)
            {
                if (PreviewStateOrder[i] == state)
                {
                    return i;
                }
            }

            return 0;
        }

        private static void OnRetryRequested()
        {
            // RetryRequested restarts the appropriate startup operation.
            Debug.Log("[BootstrapPanelController] RetryRequested — static UI preview only, no startup operation restarted.");
        }

        private static void OnContinueOfflineRequested()
        {
            // ContinueOfflineRequested maps to AppShell only after local eligibility.
            Debug.Log("[BootstrapPanelController] ContinueOfflineRequested — static UI preview only, AppShell not opened.");
        }

        private static void OnOpenLoginRequested()
        {
            // OpenLoginRequested maps to Login.
            Debug.Log("[BootstrapPanelController] OpenLoginRequested — static UI preview only, Login not opened.");
        }

        private static void OnUpdateApplicationRequested()
        {
            // UpdateApplicationRequested maps to the approved update flow.
            Debug.Log("[BootstrapPanelController] UpdateApplicationRequested — static UI preview only, no update flow started.");
        }

        private static void OnContinueToApplicationRequested()
        {
            // ContinueToApplicationRequested maps to AppShell after ready
            // (future production auto-continues at 100% — Ready shows no Continue button).
            Debug.Log("[BootstrapPanelController] ContinueToApplicationRequested — static UI preview only, AppShell not opened.");
        }
    }
}
