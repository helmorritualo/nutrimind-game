using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NutriMind.App.UI
{
    /// <summary>
    /// Standalone <c>UIDocument</c> preview adapter for <see cref="QuizResultPanelView"/>.
    /// Presentation only — applies inspector preview state and logs requests.
    /// Does not call APIs, calculate scores, or store results.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class QuizResultPanelController : MonoBehaviour
    {
        [SerializeField]
        private VisualTreeAsset _dataStatePanelAsset;

        [SerializeField]
        private QuizResultPreviewState _previewState =
            QuizResultPreviewState.Content;

        private UIDocument _uiDocument;
        private QuizResultPanelView _view;
        private bool _eventsRegistered;
        private QuizResultPreviewState? _appliedState;

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
            VisualElement componentRoot = panelRoot?.Q<VisualElement>("quiz-result-root");
            if (componentRoot == null)
            {
                Invoke(nameof(BindWhenReady), 0.05f);
                return;
            }

            panelRoot.style.flexGrow = 1;
            panelRoot.style.width = Length.Percent(100);
            panelRoot.style.height = Length.Percent(100);

            UnbindView();
            _view = new QuizResultPanelView(componentRoot, _dataStatePanelAsset);
            if (!_view.IsBound)
            {
                Debug.LogWarning(
                    "[QuizResultPanelController] QuizResultPanelView failed to bind quiz-result-root.");
                _view.Dispose();
                _view = null;
                return;
            }

            RegisterEvents();

            QuizListPreviewItem summary = QuizDetailPreviewCatalog.CreateCanonicalSummary();
            if (QuizDetailPreviewCatalog.TryGetDetail(summary.Id, out QuizDetailPreviewContent detail)
                && QuizResultPreviewCatalog.TryGetResult(summary.Id, out QuizResultPreviewContent result))
            {
                _view.SetResultContext(summary, detail, result);
            }

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

            _view.BackToQuizPortalRequested += OnBackToQuizPortalRequested;
            _view.ViewHistoryRequested += OnViewHistoryRequested;
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

            _view.BackToQuizPortalRequested -= OnBackToQuizPortalRequested;
            _view.ViewHistoryRequested -= OnViewHistoryRequested;
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

            _view.SetPreviewState(_previewState);
            _appliedState = _previewState;
        }

        private void OnBackToQuizPortalRequested() =>
            Debug.Log("[QuizResultPanelController] Back to Quiz Portal requested — preview only.");

        private void OnViewHistoryRequested() =>
            Debug.Log("[QuizResultPanelController] View Quiz History requested — preview only.");

        private void OnRetryRequested() =>
            Debug.Log("[QuizResultPanelController] Retry requested — preview only.");

#if UNITY_EDITOR
        [ContextMenu("Cycle Result State")]
        private void CycleResultState()
        {
            _previewState = (QuizResultPreviewState)(
                ((int)_previewState + 1) % System.Enum.GetValues(typeof(QuizResultPreviewState)).Length);
            ApplyPreviewValues(force: true);
            EditorUtility.SetDirty(this);
        }
#endif
    }
}
