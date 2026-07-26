using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NutriMind.App.UI
{
    /// <summary>
    /// Standalone <c>UIDocument</c> preview adapter for <see cref="ProgressPanelView"/>.
    /// Presentation only — applies inspector preview selection/state and logs requests.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class ProgressPanelController : MonoBehaviour
    {
        [SerializeField]
        private VisualTreeAsset _dataStatePanelAsset;

        [SerializeField]
        private DataStatePanelState _previewState = DataStatePanelState.Content;

        [SerializeField]
        private NutriMindSubject _previewSubject = NutriMindSubject.LiteraQuest;

        [SerializeField]
        private NutriMindTerm _previewTerm = NutriMindTerm.Term1;

        private UIDocument _uiDocument;
        private ProgressPanelView _view;
        private bool _eventsRegistered;
        private DataStatePanelState? _appliedState;
        private NutriMindSubject? _appliedSubject;
        private NutriMindTerm? _appliedTerm;

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
            VisualElement componentRoot = panelRoot?.Q<VisualElement>("progress-root");
            if (componentRoot == null)
            {
                Invoke(nameof(BindWhenReady), 0.05f);
                return;
            }

            panelRoot.style.flexGrow = 1;
            panelRoot.style.width = Length.Percent(100);
            panelRoot.style.height = Length.Percent(100);

            UnbindView();
            _view = new ProgressPanelView(componentRoot, _dataStatePanelAsset);
            if (!_view.IsBound)
            {
                Debug.LogWarning(
                    "[ProgressPanelController] ProgressPanelView failed to bind progress-root.");
                _view.Dispose();
                _view = null;
                return;
            }

            RegisterEvents();
            _view.SetSelection(_previewSubject, _previewTerm);
            _view.SetDataState(_previewState);
            _appliedSubject = _previewSubject;
            _appliedTerm = _previewTerm;
            _appliedState = _previewState;
        }

        private void Unbind()
        {
            UnbindView();
            _uiDocument = null;
            _appliedState = null;
            _appliedSubject = null;
            _appliedTerm = null;
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

            _view.SubjectSelected += OnSubjectSelected;
            _view.TermSelected += OnTermSelected;
            _view.MissionReviewRequested += OnMissionReviewRequested;
            _view.QuizPortalRequested += OnQuizPortalRequested;
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

            _view.SubjectSelected -= OnSubjectSelected;
            _view.TermSelected -= OnTermSelected;
            _view.MissionReviewRequested -= OnMissionReviewRequested;
            _view.QuizPortalRequested -= OnQuizPortalRequested;
            _view.RetryRequested -= OnRetryRequested;
            _eventsRegistered = false;
        }

        private void ApplyPreviewValues(bool force)
        {
            if (_view == null || !_view.IsBound)
            {
                return;
            }

            bool selectionChanged = force
                || _appliedSubject != _previewSubject
                || _appliedTerm != _previewTerm;
            bool stateChanged = force || _appliedState != _previewState;

            if (selectionChanged)
            {
                _view.SetSelection(_previewSubject, _previewTerm);
                _appliedSubject = _previewSubject;
                _appliedTerm = _previewTerm;
            }

            if (stateChanged)
            {
                _view.SetDataState(_previewState);
                _appliedState = _previewState;
            }
        }

        private void OnSubjectSelected(NutriMindSubject subject)
        {
            _previewSubject = subject;
            _previewTerm = NutriMindTerm.Term1;
            _appliedSubject = subject;
            _appliedTerm = NutriMindTerm.Term1;
            Debug.Log(
                $"[ProgressPanelController] Subject selected: {GetSubjectLabel(subject)}.");
        }

        private void OnTermSelected(NutriMindTerm term)
        {
            _previewTerm = term;
            _appliedTerm = term;
            Debug.Log($"[ProgressPanelController] Term selected: Term {(int)term}.");
        }

        private void OnMissionReviewRequested(ProgressMissionPreviewSelection selection)
        {
            Debug.Log(
                $"[ProgressPanelController] Review requested for Mission {selection.MissionNumber} " +
                $"'{selection.MissionTitle}' ({GetSubjectLabel(selection.Subject)} • Term {(int)selection.Term}).");
        }

        private void OnQuizPortalRequested() =>
            Debug.Log("[ProgressPanelController] Quiz Portal requested — preview only.");

        private void OnRetryRequested() =>
            Debug.Log("[ProgressPanelController] Retry requested — preview only.");

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
