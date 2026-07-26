using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NutriMind.App.UI
{
    /// <summary>
    /// Standalone <c>UIDocument</c> preview adapter for <see cref="QuizDetailPanelView"/>.
    /// Presentation only — applies inspector preview state and logs requests.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class QuizDetailPanelController : MonoBehaviour
    {
        [SerializeField]
        private VisualTreeAsset _dataStatePanelAsset;

        [SerializeField]
        private DataStatePanelState _previewState = DataStatePanelState.Content;

        private UIDocument _uiDocument;
        private QuizDetailPanelView _view;
        private bool _eventsRegistered;
        private DataStatePanelState? _appliedState;

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
            VisualElement componentRoot = panelRoot?.Q<VisualElement>("quiz-detail-root");
            if (componentRoot == null)
            {
                Invoke(nameof(BindWhenReady), 0.05f);
                return;
            }

            panelRoot.style.flexGrow = 1;
            panelRoot.style.width = Length.Percent(100);
            panelRoot.style.height = Length.Percent(100);

            UnbindView();
            _view = new QuizDetailPanelView(componentRoot, _dataStatePanelAsset);
            if (!_view.IsBound)
            {
                Debug.LogWarning(
                    "[QuizDetailPanelController] QuizDetailPanelView failed to bind quiz-detail-root.");
                _view.Dispose();
                _view = null;
                return;
            }

            RegisterEvents();
            _view.SetQuizContext(QuizDetailPreviewCatalog.CreateCanonicalSummary());
            ApplyPreviewValues(force: true);
        }

        private void Unbind()
        {
            UnbindView();
            _uiDocument = null;
            _appliedState = null;
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

            _view.BackRequested += OnBackRequested;
            _view.StartRequested += OnStartRequested;
            _view.ViewResultRequested += OnViewResultRequested;
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

            _view.BackRequested -= OnBackRequested;
            _view.StartRequested -= OnStartRequested;
            _view.ViewResultRequested -= OnViewResultRequested;
            _view.RetryRequested -= OnRetryRequested;
            _eventsRegistered = false;
        }

        private void ApplyPreviewValues(bool force)
        {
            if (_view == null || !_view.IsBound)
            {
                return;
            }

            bool stateChanged = force || _appliedState != _previewState;
            if (!stateChanged)
            {
                return;
            }

            _view.SetDataState(_previewState);
            _appliedState = _previewState;
        }

        private void OnBackRequested() =>
            Debug.Log("[QuizDetailPanelController] Back to Quiz Portal requested — preview only.");

        private void OnStartRequested(QuizDetailPreviewSelection selection) =>
            Debug.Log(
                $"[QuizDetailPanelController] Start requested: {selection.Summary.Id} " +
                $"'{selection.Summary.Title}' ({selection.QuestionCount} questions) — preview only.");

        private void OnViewResultRequested(QuizDetailPreviewSelection selection) =>
            Debug.Log(
                $"[QuizDetailPanelController] View Result requested: {selection.Summary.Id} " +
                $"'{selection.Summary.Title}' — preview only.");

        private void OnRetryRequested() =>
            Debug.Log("[QuizDetailPanelController] Retry requested — preview only.");

#if UNITY_EDITOR
        [ContextMenu("Cycle Data State")]
        private void CycleDataState()
        {
            _previewState = (DataStatePanelState)(
                ((int)_previewState + 1) % System.Enum.GetValues(typeof(DataStatePanelState)).Length);
            ApplyPreviewValues(force: true);
            EditorUtility.SetDirty(this);
        }
#endif
    }
}
