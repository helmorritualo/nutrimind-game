using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NutriMind.App.UI
{
    /// <summary>
    /// Standalone <c>UIDocument</c> preview adapter for <see cref="QuizAttemptPanelView"/>.
    /// Presentation only — applies inspector preview state, hosts local ConfirmDialog,
    /// and logs requests. Does not call APIs, score answers, or generate attempt UUIDs.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class QuizAttemptPanelController : MonoBehaviour
    {
        [SerializeField]
        private VisualTreeAsset _dataStatePanelAsset;

        [SerializeField]
        private VisualTreeAsset _confirmDialogAsset;

        [SerializeField]
        private QuizAttemptPreviewState _previewState =
            QuizAttemptPreviewState.Content;

        private UIDocument _uiDocument;
        private QuizAttemptPanelView _view;
        private TemplateContainer _confirmDialogInstance;
        private ConfirmDialogView _confirmDialogView;
        private QuizAttemptPreviewSubmission _pendingSubmission;
        private PendingConfirmAction _pendingAction;
        private bool _eventsRegistered;
        private QuizAttemptPreviewState? _appliedState;

        private enum PendingConfirmAction
        {
            None,
            ExitQuiz,
            SubmitQuiz
        }

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
            VisualElement componentRoot = panelRoot?.Q<VisualElement>("quiz-attempt-root");
            if (componentRoot == null)
            {
                Invoke(nameof(BindWhenReady), 0.05f);
                return;
            }

            panelRoot.style.flexGrow = 1;
            panelRoot.style.width = Length.Percent(100);
            panelRoot.style.height = Length.Percent(100);

            UnbindView();
            _view = new QuizAttemptPanelView(componentRoot, _dataStatePanelAsset);
            if (!_view.IsBound)
            {
                Debug.LogWarning(
                    "[QuizAttemptPanelController] QuizAttemptPanelView failed to bind quiz-attempt-root.");
                _view.Dispose();
                _view = null;
                return;
            }

            QuizListPreviewItem summary = QuizDetailPreviewCatalog.CreateCanonicalSummary();
            if (QuizDetailPreviewCatalog.TryGetDetail(summary.Id, out QuizDetailPreviewContent detail))
            {
                _view.SetQuizContext(summary, detail);
            }

            BindLocalConfirmDialog();
            RegisterEvents();
            ApplyPreviewValues(force: true);
        }

        private void BindLocalConfirmDialog()
        {
            UnbindLocalConfirmDialog();
            if (_confirmDialogAsset == null || _view?.LocalModalHost == null)
            {
                Debug.LogWarning(
                    "[QuizAttemptPanelController] ConfirmDialog asset or local modal host is missing.");
                return;
            }

            _confirmDialogInstance = _confirmDialogAsset.CloneTree();
            _view.LocalModalHost.Add(_confirmDialogInstance);
            _confirmDialogView = new ConfirmDialogView(_confirmDialogInstance);
            if (!_confirmDialogView.IsBound)
            {
                Debug.LogWarning(
                    "[QuizAttemptPanelController] ConfirmDialogView failed to bind.");
                UnbindLocalConfirmDialog();
                return;
            }

            _confirmDialogView.Confirmed += OnConfirmDialogConfirmed;
            _confirmDialogView.Cancelled += OnConfirmDialogCancelled;
        }

        private void UnbindLocalConfirmDialog()
        {
            if (_confirmDialogView != null)
            {
                _confirmDialogView.Confirmed -= OnConfirmDialogConfirmed;
                _confirmDialogView.Cancelled -= OnConfirmDialogCancelled;
                _confirmDialogView.Dispose();
                _confirmDialogView = null;
            }

            if (_confirmDialogInstance != null)
            {
                _confirmDialogInstance.RemoveFromHierarchy();
                _confirmDialogInstance = null;
            }

            _pendingAction = PendingConfirmAction.None;
            _pendingSubmission = null;
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
            UnbindLocalConfirmDialog();
            _view.Dispose();
            _view = null;
        }

        private void RegisterEvents()
        {
            if (_view == null || _eventsRegistered)
            {
                return;
            }

            _view.ExitRequested += OnExitRequested;
            _view.QuestionChanged += OnQuestionChanged;
            _view.SubmitRequested += OnSubmitRequested;
            _view.CheckSubmissionStatusRequested += OnCheckSubmissionStatusRequested;
            _view.ReturnToReviewRequested += OnReturnToReviewRequested;
            _view.BackToQuizPortalRequested += OnBackToQuizPortalRequested;
            _eventsRegistered = true;
        }

        private void UnregisterEvents()
        {
            if (_view == null || !_eventsRegistered)
            {
                _eventsRegistered = false;
                return;
            }

            _view.ExitRequested -= OnExitRequested;
            _view.QuestionChanged -= OnQuestionChanged;
            _view.SubmitRequested -= OnSubmitRequested;
            _view.CheckSubmissionStatusRequested -= OnCheckSubmissionStatusRequested;
            _view.ReturnToReviewRequested -= OnReturnToReviewRequested;
            _view.BackToQuizPortalRequested -= OnBackToQuizPortalRequested;
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

        private void OnExitRequested()
        {
            ShowConfirmation(PendingConfirmAction.ExitQuiz, ConfirmDialogPresets.ExitQuiz());
        }

        private void OnQuestionChanged(int index) =>
            Debug.Log(
                $"[QuizAttemptPanelController] Question changed to index {index} — preview only.");

        private void OnSubmitRequested(QuizAttemptPreviewSubmission submission)
        {
            _pendingSubmission = submission;
            if (submission.UnansweredCount == 0)
            {
                ShowConfirmation(PendingConfirmAction.SubmitQuiz, ConfirmDialogPresets.SubmitQuiz());
                return;
            }

            ShowConfirmation(
                PendingConfirmAction.SubmitQuiz,
                new ConfirmDialogConfiguration(
                    title: "Submit your quiz?",
                    message: $"You answered {submission.AnsweredCount} of {submission.TotalQuestions} questions.",
                    confirmLabel: "Submit Quiz",
                    cancelLabel: "Keep Reviewing",
                    detail: "Unanswered questions will remain unanswered. You will not be able to change your answers after submission.",
                    iconClass: "ds-icon--warning",
                    tone: ConfirmDialogTone.Warning,
                    dismissOnBackdrop: false));
        }

        private void OnCheckSubmissionStatusRequested() =>
            Debug.Log(
                "[QuizAttemptPanelController] Check submission status requested — " +
                "server recovery is not connected in this static preview.");

        private void OnReturnToReviewRequested()
        {
            _previewState = QuizAttemptPreviewState.Content;
            _view?.SetPreviewState(QuizAttemptPreviewState.Content);
            _view?.ShowReview();
            _appliedState = QuizAttemptPreviewState.Content;
            Debug.Log(
                "[QuizAttemptPanelController] Return to Review requested — answers preserved.");
        }

        private void OnBackToQuizPortalRequested() =>
            Debug.Log(
                "[QuizAttemptPanelController] Back to Quiz Portal requested — preview only.");

        private void ShowConfirmation(
            PendingConfirmAction action,
            ConfirmDialogConfiguration configuration)
        {
            if (_confirmDialogView == null || !_confirmDialogView.IsBound)
            {
                Debug.LogWarning(
                    "[QuizAttemptPanelController] ConfirmDialog is unavailable.");
                return;
            }

            _pendingAction = action;
            _confirmDialogView.Show(configuration);
        }

        private void OnConfirmDialogConfirmed()
        {
            PendingConfirmAction action = _pendingAction;
            _pendingAction = PendingConfirmAction.None;

            switch (action)
            {
                case PendingConfirmAction.ExitQuiz:
                    Debug.Log(
                        "[QuizAttemptPanelController] Exit confirmed — preview only (no route change).");
                    break;

                case PendingConfirmAction.SubmitQuiz:
                    QuizAttemptPreviewSubmission submission = _pendingSubmission;
                    _pendingSubmission = null;
                    if (submission != null)
                    {
                        Debug.Log(
                            $"[QuizAttemptPanelController] Submit confirmed: quiz={submission.QuizId}, " +
                            $"answered={submission.AnsweredCount}/{submission.TotalQuestions}, " +
                            $"unanswered={submission.UnansweredCount}, marked={submission.MarkedCount} — " +
                            "no request sent.");
                    }

                    _previewState = QuizAttemptPreviewState.Submitting;
                    _view?.SetPreviewState(QuizAttemptPreviewState.Submitting);
                    _appliedState = QuizAttemptPreviewState.Submitting;
                    break;
            }
        }

        private void OnConfirmDialogCancelled()
        {
            _pendingAction = PendingConfirmAction.None;
            _pendingSubmission = null;
            Debug.Log(
                "[QuizAttemptPanelController] Confirmation cancelled — answers preserved.");
        }

#if UNITY_EDITOR
        [ContextMenu("Cycle Attempt State")]
        private void CycleAttemptState()
        {
            _previewState = (QuizAttemptPreviewState)(
                ((int)_previewState + 1) % System.Enum.GetValues(typeof(QuizAttemptPreviewState)).Length);
            ApplyPreviewValues(force: true);
            EditorUtility.SetDirty(this);
        }

        [ContextMenu("Show Review")]
        private void ShowReviewMenu()
        {
            _previewState = QuizAttemptPreviewState.Content;
            _view?.SetPreviewState(QuizAttemptPreviewState.Content);
            _view?.ShowReview();
            _appliedState = QuizAttemptPreviewState.Content;
            EditorUtility.SetDirty(this);
        }

        [ContextMenu("Reset Answers")]
        private void ResetAnswersMenu()
        {
            _view?.ResetPreviewAnswers();
            EditorUtility.SetDirty(this);
        }
#endif
    }
}
