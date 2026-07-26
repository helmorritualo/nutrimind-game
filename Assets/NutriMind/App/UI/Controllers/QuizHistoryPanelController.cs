using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NutriMind.App.UI
{
    /// <summary>
    /// Standalone <c>UIDocument</c> preview adapter for <see cref="QuizHistoryPanelView"/>.
    /// Presentation only — applies inspector preview state and logs requests.
    /// Does not call APIs, invent history rows, or store results.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class QuizHistoryPanelController : MonoBehaviour
    {
        [SerializeField]
        private VisualTreeAsset _dataStatePanelAsset;

        [SerializeField]
        private QuizHistoryPreviewState _previewState =
            QuizHistoryPreviewState.Content;

        [SerializeField]
        private QuizHistoryPreviewSubjectFilter _previewSubjectFilter =
            QuizHistoryPreviewSubjectFilter.All;

        [SerializeField]
        private QuizHistoryPreviewTermFilter _previewTermFilter =
            QuizHistoryPreviewTermFilter.All;

        private UIDocument _uiDocument;
        private QuizHistoryPanelView _view;
        private bool _eventsRegistered;
        private QuizHistoryPreviewState? _appliedState;
        private QuizHistoryPreviewSubjectFilter? _appliedSubjectFilter;
        private QuizHistoryPreviewTermFilter? _appliedTermFilter;

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
            VisualElement componentRoot = panelRoot?.Q<VisualElement>("quiz-history-root");
            if (componentRoot == null)
            {
                Invoke(nameof(BindWhenReady), 0.05f);
                return;
            }

            panelRoot.style.flexGrow = 1;
            panelRoot.style.width = Length.Percent(100);
            panelRoot.style.height = Length.Percent(100);

            UnbindView();
            _view = new QuizHistoryPanelView(componentRoot, _dataStatePanelAsset);
            if (!_view.IsBound)
            {
                Debug.LogWarning(
                    "[QuizHistoryPanelController] QuizHistoryPanelView failed to bind quiz-history-root.");
                _view.Dispose();
                _view = null;
                return;
            }

            RegisterEvents();
            _view.SetItems(QuizHistoryPreviewCatalog.CreateCanonicalItems());
            ApplyPreviewValues(force: true);
        }

        private void Unbind()
        {
            UnbindView();
            _uiDocument = null;
            _appliedState = null;
            _appliedSubjectFilter = null;
            _appliedTermFilter = null;
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
            _view.ViewResultRequested += OnViewResultRequested;
            _view.FiltersChanged += OnFiltersChanged;
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
            _view.ViewResultRequested -= OnViewResultRequested;
            _view.FiltersChanged -= OnFiltersChanged;
            _view.RetryRequested -= OnRetryRequested;
            _eventsRegistered = false;
        }

        private void ApplyPreviewValues(bool force)
        {
            if (_view == null || !_view.IsBound)
            {
                return;
            }

            bool filtersChanged = force
                || _appliedSubjectFilter != _previewSubjectFilter
                || _appliedTermFilter != _previewTermFilter;
            if (filtersChanged)
            {
                _view.SetFilters(
                    new QuizHistoryPreviewFilters(_previewSubjectFilter, _previewTermFilter));
                _appliedSubjectFilter = _previewSubjectFilter;
                _appliedTermFilter = _previewTermFilter;
            }

            bool stateChanged = force || _appliedState != _previewState;
            if (stateChanged)
            {
                _view.SetPreviewState(_previewState);
                _appliedState = _previewState;
            }
        }

        private void OnBackToQuizPortalRequested() =>
            Debug.Log("[QuizHistoryPanelController] Back to Quiz Portal requested — preview only.");

        private void OnViewResultRequested(QuizHistoryPreviewSelection selection) =>
            Debug.Log(
                $"[QuizHistoryPanelController] View Result requested: attempt={selection.AttemptId}, " +
                $"quiz={selection.Summary.Id} '{selection.Summary.Title}' — preview only.");

        private void OnFiltersChanged(QuizHistoryPreviewFilters filters) =>
            Debug.Log(
                $"[QuizHistoryPanelController] Filters changed: subject={filters.Subject}, term={filters.Term}.");

        private void OnRetryRequested() =>
            Debug.Log("[QuizHistoryPanelController] Retry requested — preview only.");

#if UNITY_EDITOR
        [ContextMenu("Cycle History State")]
        private void CycleHistoryState()
        {
            _previewState = (QuizHistoryPreviewState)(
                ((int)_previewState + 1) % System.Enum.GetValues(typeof(QuizHistoryPreviewState)).Length);
            ApplyPreviewValues(force: true);
            EditorUtility.SetDirty(this);
        }

        [ContextMenu("Reset History Filters")]
        private void ResetHistoryFilters()
        {
            _previewSubjectFilter = QuizHistoryPreviewSubjectFilter.All;
            _previewTermFilter = QuizHistoryPreviewTermFilter.All;
            ApplyPreviewValues(force: true);
            EditorUtility.SetDirty(this);
        }
#endif
    }
}
