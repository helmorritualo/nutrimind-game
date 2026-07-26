using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NutriMind.App.UI
{
    /// <summary>
    /// Standalone <c>UIDocument</c> preview adapter for <see cref="CertificatesPanelView"/>.
    /// Presentation only — applies inspector preview state and logs requests.
    /// Does not call APIs, create files, download certificates, or persist.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class CertificatesPanelController : MonoBehaviour
    {
        [SerializeField]
        private VisualTreeAsset _dataStatePanelAsset;

        [SerializeField]
        private CertificatesPreviewState _previewState =
            CertificatesPreviewState.Content;

        [SerializeField]
        private int _previewCertificateIndex;

        private UIDocument _uiDocument;
        private CertificatesPanelView _view;
        private bool _eventsRegistered;
        private CertificatesPreviewState? _appliedState;
        private int? _appliedCertificateIndex;

        private void OnEnable()
        {
            _uiDocument = GetComponent<UIDocument>();
            BindWhenReady();
        }

        private void OnDisable()
        {
            CancelInvoke(nameof(BindWhenReady));
            Unbind();
        }

        private void OnValidate()
        {
            _previewCertificateIndex = Mathf.Clamp(_previewCertificateIndex, 0, 2);

            if (!isActiveAndEnabled || _view == null || !_view.IsBound)
            {
                return;
            }

            ApplyPreviewValues(force: true);
        }

        private void Update()
        {
            if (_view == null || !_view.IsBound)
            {
                return;
            }

            ApplyPreviewValues(force: false);
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
            VisualElement componentRoot = panelRoot?.Q<VisualElement>("certificates-root");
            if (componentRoot == null)
            {
                Invoke(nameof(BindWhenReady), 0.05f);
                return;
            }

            panelRoot.style.flexGrow = 1;
            panelRoot.style.width = Length.Percent(100);
            panelRoot.style.height = Length.Percent(100);

            UnbindView();
            _view = new CertificatesPanelView(componentRoot, _dataStatePanelAsset);
            if (!_view.IsBound)
            {
                Debug.LogWarning(
                    "[CertificatesPanelController] CertificatesPanelView failed to bind certificates-root.");
                _view.Dispose();
                _view = null;
                return;
            }

            RegisterEvents();
            _view.SetItems(CertificatesPreviewCatalog.CreateItems());
            ApplyPreviewValues(force: true);
        }

        private void Unbind()
        {
            UnbindView();
            _uiDocument = null;
            _appliedState = null;
            _appliedCertificateIndex = null;
        }

        private void UnbindView()
        {
            if (_view == null)
            {
                return;
            }

            UnregisterEvents();
            _view.Dispose();
            _view = null;
        }

        private void RegisterEvents()
        {
            if (_view == null || _eventsRegistered)
            {
                return;
            }

            _view.BackToRewardsRequested += OnBackToRewardsRequested;
            _view.SelectionChanged += OnSelectionChanged;
            _view.DownloadRequested += OnDownloadRequested;
            _view.RetryRequested += OnRetryRequested;
            _eventsRegistered = true;
        }

        private void UnregisterEvents()
        {
            if (_view == null || !_eventsRegistered)
            {
                _eventsRegistered = false;
                return;
            }

            _view.BackToRewardsRequested -= OnBackToRewardsRequested;
            _view.SelectionChanged -= OnSelectionChanged;
            _view.DownloadRequested -= OnDownloadRequested;
            _view.RetryRequested -= OnRetryRequested;
            _eventsRegistered = false;
        }

        private void ApplyPreviewValues(bool force)
        {
            if (_view == null || !_view.IsBound)
            {
                return;
            }

            int clampedIndex = Mathf.Clamp(_previewCertificateIndex, 0, 2);
            bool indexChanged = force || _appliedCertificateIndex != clampedIndex;
            if (indexChanged)
            {
                var items = CertificatesPreviewCatalog.CreateItems();
                if (items.Count > 0)
                {
                    int safeIndex = Mathf.Clamp(clampedIndex, 0, items.Count - 1);
                    _view.SelectByPresentationId(items[safeIndex].PresentationId);
                    _previewCertificateIndex = safeIndex;
                }

                _appliedCertificateIndex = _previewCertificateIndex;
            }

            bool stateChanged = force || _appliedState != _previewState;
            if (stateChanged)
            {
                _view.SetPreviewState(_previewState);
                _appliedState = _previewState;
            }
        }

        private void OnBackToRewardsRequested() =>
            Debug.Log("[CertificatesPanelController] Back to Rewards requested — preview only.");

        private void OnSelectionChanged(CertificatePreviewSelection selection)
        {
            var items = CertificatesPreviewCatalog.CreateItems();
            for (int i = 0; i < items.Count; i++)
            {
                if (string.Equals(
                        items[i].PresentationId,
                        selection.PresentationId,
                        System.StringComparison.Ordinal))
                {
                    _previewCertificateIndex = i;
                    _appliedCertificateIndex = i;
                    break;
                }
            }

            Debug.Log(
                $"[CertificatesPanelController] Selection changed: id={selection.PresentationId}, " +
                $"title='{selection.Title}' — preview only.");
        }

        private void OnDownloadRequested(CertificatePreviewSelection selection) =>
            Debug.Log(
                $"[CertificatesPanelController] Download requested: id={selection.PresentationId}, " +
                $"title='{selection.Title}' — preview only. No file created.");

        private void OnRetryRequested() =>
            Debug.Log("[CertificatesPanelController] Retry requested — preview only.");

#if UNITY_EDITOR
        [ContextMenu("Cycle Certificates State")]
        private void CycleCertificatesState()
        {
            _previewState = (CertificatesPreviewState)(
                ((int)_previewState + 1) % System.Enum.GetValues(typeof(CertificatesPreviewState)).Length);
            ApplyPreviewValues(force: true);
            EditorUtility.SetDirty(this);
        }

        [ContextMenu("Cycle Selected Certificate")]
        private void CycleSelectedCertificate()
        {
            _previewCertificateIndex = (_previewCertificateIndex + 1) % 3;
            ApplyPreviewValues(force: true);
            EditorUtility.SetDirty(this);
        }
#endif
    }
}
