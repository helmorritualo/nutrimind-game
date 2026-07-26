using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NutriMind.App.UI
{
    /// <summary>
    /// Standalone <c>UIDocument</c> preview adapter for <see cref="QuizListPanelView"/>.
    /// Presentation only — applies inspector preview filters/state and logs requests.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class QuizListPanelController : MonoBehaviour
    {
        [SerializeField]
        private VisualTreeAsset _dataStatePanelAsset;

        [SerializeField]
        private DataStatePanelState _previewState = DataStatePanelState.Content;

        [SerializeField]
        private QuizListPreviewSubjectFilter _previewSubjectFilter =
            QuizListPreviewSubjectFilter.All;

        [SerializeField]
        private QuizListPreviewTermFilter _previewTermFilter =
            QuizListPreviewTermFilter.All;

        [SerializeField]
        private QuizListPreviewStatusFilter _previewStatusFilter =
            QuizListPreviewStatusFilter.All;

        [SerializeField]
        private int _previewCurrentPage = 1;

        [SerializeField]
        private int _previewLastPage = 2;

        [SerializeField]
        private bool _previewHasMore = true;

        private UIDocument _uiDocument;
        private QuizListPanelView _view;
        private bool _eventsRegistered;
        private DataStatePanelState? _appliedState;
        private QuizListPreviewSubjectFilter? _appliedSubjectFilter;
        private QuizListPreviewTermFilter? _appliedTermFilter;
        private QuizListPreviewStatusFilter? _appliedStatusFilter;
        private int? _appliedCurrentPage;
        private int? _appliedLastPage;
        private bool? _appliedHasMore;

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
            ClampPreviewPagination();
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
            VisualElement componentRoot = panelRoot?.Q<VisualElement>("quiz-list-root");
            if (componentRoot == null)
            {
                Invoke(nameof(BindWhenReady), 0.05f);
                return;
            }

            panelRoot.style.flexGrow = 1;
            panelRoot.style.width = Length.Percent(100);
            panelRoot.style.height = Length.Percent(100);

            UnbindView();
            ClampPreviewPagination();
            _view = new QuizListPanelView(componentRoot, _dataStatePanelAsset);
            if (!_view.IsBound)
            {
                Debug.LogWarning(
                    "[QuizListPanelController] QuizListPanelView failed to bind quiz-list-root.");
                _view.Dispose();
                _view = null;
                return;
            }

            RegisterEvents();
            ApplyPreviewValues(force: true);
        }

        private void Unbind()
        {
            UnbindView();
            _uiDocument = null;
            _appliedState = null;
            _appliedSubjectFilter = null;
            _appliedTermFilter = null;
            _appliedStatusFilter = null;
            _appliedCurrentPage = null;
            _appliedLastPage = null;
            _appliedHasMore = null;
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

            _view.QuizDetailsRequested += OnQuizDetailsRequested;
            _view.QuizResultRequested += OnQuizResultRequested;
            _view.FiltersChanged += OnFiltersChanged;
            _view.PageRequested += OnPageRequested;
            _view.RetryRequested += OnRetryRequested;
            _view.ReturnToMainRequested += OnReturnToMainRequested;
            _eventsRegistered = true;
        }

        private void UnregisterEvents()
        {
            if (_view == null || !_eventsRegistered)
            {
                _eventsRegistered = false;
                return;
            }

            _view.QuizDetailsRequested -= OnQuizDetailsRequested;
            _view.QuizResultRequested -= OnQuizResultRequested;
            _view.FiltersChanged -= OnFiltersChanged;
            _view.PageRequested -= OnPageRequested;
            _view.RetryRequested -= OnRetryRequested;
            _view.ReturnToMainRequested -= OnReturnToMainRequested;
            _eventsRegistered = false;
        }

        private void ApplyPreviewValues(bool force)
        {
            if (_view == null || !_view.IsBound)
            {
                return;
            }

            ClampPreviewPagination();

            bool filtersChanged = force
                || _appliedSubjectFilter != _previewSubjectFilter
                || _appliedTermFilter != _previewTermFilter
                || _appliedStatusFilter != _previewStatusFilter;
            bool paginationChanged = force
                || _appliedCurrentPage != _previewCurrentPage
                || _appliedLastPage != _previewLastPage
                || _appliedHasMore != _previewHasMore;
            bool stateChanged = force || _appliedState != _previewState;

            if (filtersChanged)
            {
                _view.SetFilters(
                    new QuizListPreviewFilters(
                        _previewSubjectFilter,
                        _previewTermFilter,
                        _previewStatusFilter));
                _appliedSubjectFilter = _previewSubjectFilter;
                _appliedTermFilter = _previewTermFilter;
                _appliedStatusFilter = _previewStatusFilter;
            }

            if (paginationChanged)
            {
                _view.SetPagination(_previewCurrentPage, _previewLastPage, _previewHasMore);
                _appliedCurrentPage = _previewCurrentPage;
                _appliedLastPage = _previewLastPage;
                _appliedHasMore = _previewHasMore;
            }

            if (stateChanged)
            {
                _view.SetDataState(_previewState);
                _appliedState = _previewState;
            }
        }

        private void ClampPreviewPagination()
        {
            _previewLastPage = Mathf.Max(1, _previewLastPage);
            _previewCurrentPage = Mathf.Max(1, _previewCurrentPage);
            if (_previewCurrentPage > _previewLastPage)
            {
                _previewCurrentPage = _previewLastPage;
            }

            if (_previewCurrentPage >= _previewLastPage)
            {
                _previewHasMore = false;
            }
        }

        private void OnQuizDetailsRequested(QuizListPreviewItem item) =>
            Debug.Log(
                $"[QuizListPanelController] Quiz details requested: {item.Id} '{item.Title}' " +
                $"({item.Status}, {GetSubjectLabel(item.Subject)}, Term {(int)item.Term}).");

        private void OnQuizResultRequested(QuizListPreviewItem item) =>
            Debug.Log(
                $"[QuizListPanelController] Quiz result requested: {item.Id} '{item.Title}' " +
                $"({GetSubjectLabel(item.Subject)}, Term {(int)item.Term}).");

        private void OnFiltersChanged(QuizListPreviewFilters filters) =>
            Debug.Log(
                $"[QuizListPanelController] Filters changed: " +
                $"subject={filters.Subject}, term={filters.Term}, status={filters.Status}.");

        private void OnPageRequested(int page) =>
            Debug.Log($"[QuizListPanelController] Page requested: {page} — preview only.");

        private void OnRetryRequested() =>
            Debug.Log("[QuizListPanelController] Retry requested — preview only.");

        private void OnReturnToMainRequested() =>
            Debug.Log("[QuizListPanelController] Return Home requested — preview only.");

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

        private static string GetSubjectLabel(NutriMindSubject subject) =>
            subject switch
            {
                NutriMindSubject.LiteraQuest => "LiteraQuest",
                NutriMindSubject.PeAndHealth => "PE & Health",
                _ => "Science"
            };
    }
}
