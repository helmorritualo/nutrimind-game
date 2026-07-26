using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace NutriMind.App.UI
{
    /// <summary>
    /// Presentation-only QuizAttempt route states for static preview.
    /// Maps onto shared <see cref="DataStatePanelView"/> presentation.
    /// Future presenter/service layers must map stable server codes
    /// (QUIZ_NOT_AVAILABLE, ATTEMPT_LIMIT_REACHED, IDEMPOTENCY_PAYLOAD_MISMATCH,
    /// SERVER_BUSY, etc.) to these panel states — this UI does not branch on codes.
    /// </summary>
    public enum QuizAttemptPreviewState
    {
        Content = 0,
        Submitting = 1,
        UncertainSubmission = 2,
        RecoverableError = 3
    }

    /// <summary>
    /// Internal content mode for the attempt panel (answering vs review-before-submit).
    /// Not an AppShell route.
    /// </summary>
    public enum QuizAttemptContentMode
    {
        Answering = 0,
        Review = 1
    }

    /// <summary>
    /// Immutable presentation-only answer row for static Quiz Portal attempt preview.
    /// Uses fixture option IDs (<c>option.id</c>), not reconciled production API option keys.
    /// Not a production request DTO and not ready for direct HTTP submission.
    /// </summary>
    public readonly struct QuizAttemptPreviewAnswer
    {
        public QuizAttemptPreviewAnswer(string questionId, IReadOnlyList<string> selectedOptionIds)
        {
            QuestionId = questionId ?? string.Empty;
            if (selectedOptionIds == null || selectedOptionIds.Count == 0)
            {
                SelectedOptionIds = Array.Empty<string>();
            }
            else
            {
                var copy = new string[selectedOptionIds.Count];
                for (int i = 0; i < selectedOptionIds.Count; i++)
                {
                    copy[i] = selectedOptionIds[i];
                }

                SelectedOptionIds = copy;
            }
        }

        public string QuestionId { get; }

        /// <summary>
        /// Preview fixture option IDs. Not production API <c>option.key</c> values.
        /// </summary>
        public IReadOnlyList<string> SelectedOptionIds { get; }
    }

    /// <summary>
    /// Immutable presentation-only submission payload for static Quiz Portal attempt preview.
    /// Presentation-only static preview — not a production request DTO.
    /// Uses fixture option IDs, not reconciled API option keys.
    /// Does not include client attempt UUID, timestamps, scores, or correctness.
    /// </summary>
    public sealed class QuizAttemptPreviewSubmission
    {
        public QuizAttemptPreviewSubmission(
            string quizId,
            IReadOnlyList<QuizAttemptPreviewAnswer> answers,
            int totalQuestions,
            int answeredCount,
            int unansweredCount,
            int markedCount)
        {
            QuizId = quizId ?? string.Empty;
            if (answers == null || answers.Count == 0)
            {
                Answers = Array.Empty<QuizAttemptPreviewAnswer>();
            }
            else
            {
                var copy = new QuizAttemptPreviewAnswer[answers.Count];
                for (int i = 0; i < answers.Count; i++)
                {
                    copy[i] = answers[i];
                }

                Answers = copy;
            }

            TotalQuestions = totalQuestions;
            AnsweredCount = answeredCount;
            UnansweredCount = unansweredCount;
            MarkedCount = markedCount;
        }

        public string QuizId { get; }
        public IReadOnlyList<QuizAttemptPreviewAnswer> Answers { get; }
        public int TotalQuestions { get; }
        public int AnsweredCount { get; }
        public int UnansweredCount { get; }
        public int MarkedCount { get; }
    }

    /// <summary>
    /// Presentation-only Quiz Portal attempt view. Displays one question at a time,
    /// preserves local selections, supports review-before-submit, and raises typed
    /// host intents. Does not call APIs, score answers, generate UUIDs, or persist state.
    /// </summary>
    public sealed class QuizAttemptPanelView : IAppScreenView
    {
        private const string RootName = "quiz-attempt-root";
        private const string CompactClass = "quiz-attempt-panel--compact";
        private const string NarrowClass = "quiz-attempt-panel--narrow";
        private const string MobileClass = "mobile";
        private const string ReviewRootClass = "quiz-attempt-panel--review";
        private const string DataStateHostVisibleClass = "quiz-attempt-panel__data-state-host--visible";
        private const string ContentShellHiddenClass = "quiz-attempt-panel__content-shell--hidden";
        private const string AnswerModeHiddenClass = "quiz-attempt-panel__answer-mode--hidden";
        private const string ReviewModeHiddenClass = "quiz-attempt-panel__review-mode--hidden";
        private const string AnswerFooterHiddenClass = "quiz-attempt-panel__answer-footer--hidden";
        private const string ReviewFooterHiddenClass = "quiz-attempt-panel__review-footer--hidden";
        private const string OptionSelectedClass = "quiz-attempt-panel__option--selected";
        private const string NavCurrentClass = "quiz-attempt-panel__question-nav--current";
        private const string NavAnsweredClass = "quiz-attempt-panel__question-nav--answered";
        private const string NavUnansweredClass = "quiz-attempt-panel__question-nav--unanswered";
        private const string NavMarkedClass = "quiz-attempt-panel__question-nav--marked";
        private const float CompactBreakpoint = 1100f;
        private const float NarrowBreakpoint = 820f;

        private static readonly string[] NextIconClasses =
        {
            "ds-icon--arrow-right",
            "ds-icon--list",
            "ds-icon--check"
        };

        private VisualElement _root;
        private VisualElement _contentShell;
        private Button _exitButton;
        private Label _title;
        private Label _subjectTerm;
        private Label _questionProgressLabel;
        private ProgressBar _progressBar;
        private VisualElement _attemptChip;
        private Label _attemptChipLabel;
        private VisualElement _onlineChip;
        private ScrollView _bodyScroll;
        private VisualElement _body;
        private VisualElement _answerMode;
        private VisualElement _reviewMode;
        private Label _questionNumber;
        private Label _questionType;
        private Label _questionPrompt;
        private Label _selectionInstruction;
        private VisualElement _optionsHost;
        private Label _answeredCountLabel;
        private Label _unansweredCountLabel;
        private Label _markedCountLabel;
        private VisualElement _questionNavHost;
        private Label _reviewSummary;
        private VisualElement _reviewList;
        private VisualElement _answerFooter;
        private VisualElement _reviewFooter;
        private Button _previousButton;
        private VisualElement _reviewToggleRow;
        private Toggle _reviewToggle;
        private Button _nextButton;
        private VisualElement _nextIcon;
        private Label _nextLabel;
        private Button _backToQuestionsButton;
        private Button _submitButton;
        private VisualElement _dataStateHost;
        private VisualElement _localModalHost;

        private TemplateContainer _ownedDataStateInstance;
        private DataStatePanelView _dataStateView;
        private bool _warnedMissingDataStateAsset;
        private bool _disposed;
        private float _lastWidth = -1f;
        private bool _hasValidContext;
        private bool _suppressOptionCallbacks;
        private bool _suppressReviewToggleCallback;

        private readonly Dictionary<string, HashSet<string>> _selectedOptionIdsByQuestionId = new();
        private readonly HashSet<string> _markedQuestionIds = new();
        private readonly List<OptionBinding> _optionBindings = new();
        private readonly List<NavigatorBinding> _navigatorBindings = new();
        private readonly List<ReviewCardBinding> _reviewCardBindings = new();

        private EventCallback<ChangeEvent<bool>> _reviewToggleCallback;
        private EventCallback<ClickEvent> _exitClicked;
        private EventCallback<ClickEvent> _previousClicked;
        private EventCallback<ClickEvent> _nextClicked;
        private EventCallback<ClickEvent> _backToQuestionsClicked;
        private EventCallback<ClickEvent> _submitClicked;
        private EventCallback<GeometryChangedEvent> _geometryChanged;

        public QuizAttemptPanelView(
            VisualElement root,
            VisualTreeAsset dataStatePanelAsset = null)
        {
            ResolveRoot(root);
            if (_root == null)
            {
                Debug.LogWarning(
                    "[QuizAttemptPanelView] Could not resolve quiz-attempt-root inside the supplied element.");
                return;
            }

            CacheElements();
            BindDataStatePanel(dataStatePanelAsset);
            RegisterCallbacks();
            ApplyResponsiveClasses(_root.resolvedStyle.width);

            QuizListPreviewItem summary = QuizDetailPreviewCatalog.CreateCanonicalSummary();
            if (QuizDetailPreviewCatalog.TryGetDetail(summary.Id, out QuizDetailPreviewContent detail))
            {
                SetQuizContext(summary, detail);
            }

            SetPreviewState(QuizAttemptPreviewState.Content);
        }

        public VisualElement Root => _root;

        public bool IsBound => _root != null && !_disposed;

        public QuizAttemptPreviewState PreviewState { get; private set; } =
            QuizAttemptPreviewState.Content;

        public QuizAttemptContentMode ContentMode { get; private set; } =
            QuizAttemptContentMode.Answering;

        public QuizListPreviewItem SelectedSummary { get; private set; }

        public QuizDetailPreviewContent DetailContent { get; private set; }

        public int CurrentQuestionIndex { get; private set; }

        public int AnsweredCount { get; private set; }

        public int UnansweredCount { get; private set; }

        public int MarkedCount { get; private set; }

        /// <summary>Standalone hosts may clone ConfirmDialog here. Embedded AppShell leaves this empty.</summary>
        public VisualElement LocalModalHost => _localModalHost;

        public event Action ExitRequested;
        public event Action<int> QuestionChanged;
        public event Action<QuizAttemptPreviewSubmission> SubmitRequested;
        public event Action CheckSubmissionStatusRequested;
        public event Action ReturnToReviewRequested;
        public event Action BackToQuizPortalRequested;

        /// <summary>
        /// Binds summary + detail for static preview. Resets local answers and review marks.
        /// Invalid context shows a safe unavailable state and disables interactive answering.
        /// </summary>
        public void SetQuizContext(
            QuizListPreviewItem summary,
            QuizDetailPreviewContent detail)
        {
            if (!IsBound)
            {
                return;
            }

            ResetLocalAnswerState();
            SelectedSummary = summary;
            DetailContent = detail;

            if (!ValidateContext(summary, detail, out string unavailableTitle, out string unavailableMessage))
            {
                _hasValidContext = false;
                DetailContent = null;
                ShowUnavailableState(unavailableTitle, unavailableMessage);
                return;
            }

            _hasValidContext = true;
            BindSummaryHeader(summary);
            RebuildNavigator();
            ShowQuestion(0);
            SetPreviewState(QuizAttemptPreviewState.Content);
            SetContentMode(QuizAttemptContentMode.Answering);
            RefreshCounts();
        }

        public void SetPreviewState(QuizAttemptPreviewState state)
        {
            if (!IsBound)
            {
                return;
            }

            if (state == QuizAttemptPreviewState.Content && !_hasValidContext)
            {
                return;
            }

            PreviewState = state;

            if (state == QuizAttemptPreviewState.Content)
            {
                ShowContentShell();
                return;
            }

            if (_dataStateView == null || !_dataStateView.IsBound)
            {
                return;
            }

            ShowDataStateHost();
            ApplyNonContentState(state);
        }

        public void ShowQuestion(int zeroBasedIndex)
        {
            if (!IsBound || !_hasValidContext || DetailContent == null)
            {
                return;
            }

            int count = DetailContent.QuestionCount;
            if (count <= 0)
            {
                return;
            }

            int clamped = Mathf.Clamp(zeroBasedIndex, 0, count - 1);
            bool indexChanged = clamped != CurrentQuestionIndex;
            CurrentQuestionIndex = clamped;

            SetContentMode(QuizAttemptContentMode.Answering);
            BindCurrentQuestion();
            RefreshNavigatorVisuals();
            RefreshFooterForAnswering();
            RefreshHeaderProgress();
            RefreshCounts();

            if (indexChanged)
            {
                QuestionChanged?.Invoke(CurrentQuestionIndex);
            }
        }

        public void ShowReview()
        {
            if (!IsBound || !_hasValidContext || DetailContent == null)
            {
                return;
            }

            SetContentMode(QuizAttemptContentMode.Review);
            RebuildReviewCards();
            RefreshHeaderProgress();
            RefreshCounts();
            RefreshReviewSummary();
        }

        public void ResetPreviewAnswers()
        {
            if (!IsBound)
            {
                return;
            }

            ResetLocalAnswerState();
            if (_hasValidContext && DetailContent != null)
            {
                if (ContentMode == QuizAttemptContentMode.Review)
                {
                    RebuildReviewCards();
                }
                else
                {
                    BindCurrentQuestion();
                }

                RefreshNavigatorVisuals();
                RefreshCounts();
                RefreshReviewSummary();
                RefreshFooterForAnswering();
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            UnregisterCallbacks();
            ClearOptionBindings();
            ClearNavigatorBindings();
            ClearReviewCardBindings();
            DisposeOwnedDataState();

            ExitRequested = null;
            QuestionChanged = null;
            SubmitRequested = null;
            CheckSubmissionStatusRequested = null;
            ReturnToReviewRequested = null;
            BackToQuizPortalRequested = null;

            _root = null;
            _contentShell = null;
            _exitButton = null;
            _title = null;
            _subjectTerm = null;
            _questionProgressLabel = null;
            _progressBar = null;
            _attemptChip = null;
            _attemptChipLabel = null;
            _onlineChip = null;
            _bodyScroll = null;
            _body = null;
            _answerMode = null;
            _reviewMode = null;
            _questionNumber = null;
            _questionType = null;
            _questionPrompt = null;
            _selectionInstruction = null;
            _optionsHost = null;
            _answeredCountLabel = null;
            _unansweredCountLabel = null;
            _markedCountLabel = null;
            _questionNavHost = null;
            _reviewSummary = null;
            _reviewList = null;
            _answerFooter = null;
            _reviewFooter = null;
            _previousButton = null;
            _reviewToggleRow = null;
            _reviewToggle = null;
            _nextButton = null;
            _nextIcon = null;
            _nextLabel = null;
            _backToQuestionsButton = null;
            _submitButton = null;
            _dataStateHost = null;
            _localModalHost = null;
            DetailContent = null;
            _lastWidth = -1f;
            _hasValidContext = false;
        }

        private void ResolveRoot(VisualElement root)
        {
            if (root == null)
            {
                return;
            }

            _root = root.name == RootName ? root : root.Q<VisualElement>(RootName);
        }

        private void CacheElements()
        {
            _contentShell = _root.Q<VisualElement>("quiz-attempt-content-shell");
            _exitButton = _root.Q<Button>("quiz-attempt-exit-button");
            _title = _root.Q<Label>("quiz-attempt-title");
            _subjectTerm = _root.Q<Label>("quiz-attempt-subject-term");
            _questionProgressLabel = _root.Q<Label>("quiz-attempt-question-progress-label");
            _progressBar = _root.Q<ProgressBar>("quiz-attempt-progress-bar");
            _attemptChip = _root.Q<VisualElement>("quiz-attempt-attempt-chip");
            _attemptChipLabel = _attemptChip?.Q<Label>();
            _onlineChip = _root.Q<VisualElement>("quiz-attempt-online-chip");
            _bodyScroll = _root.Q<ScrollView>("quiz-attempt-body-scroll");
            _body = _root.Q<VisualElement>("quiz-attempt-body");
            _answerMode = _root.Q<VisualElement>("quiz-attempt-answer-mode");
            _reviewMode = _root.Q<VisualElement>("quiz-attempt-review-mode");
            _questionNumber = _root.Q<Label>("quiz-attempt-question-number");
            _questionType = _root.Q<Label>("quiz-attempt-question-type");
            _questionPrompt = _root.Q<Label>("quiz-attempt-question-prompt");
            _selectionInstruction = _root.Q<Label>("quiz-attempt-selection-instruction");
            _optionsHost = _root.Q<VisualElement>("quiz-attempt-options-host");
            _answeredCountLabel = _root.Q<Label>("quiz-attempt-answered-count");
            _unansweredCountLabel = _root.Q<Label>("quiz-attempt-unanswered-count");
            _markedCountLabel = _root.Q<Label>("quiz-attempt-marked-count");
            _questionNavHost = _root.Q<VisualElement>("quiz-attempt-question-nav-host");
            _reviewSummary = _root.Q<Label>("quiz-attempt-review-summary");
            _reviewList = _root.Q<VisualElement>("quiz-attempt-review-list");
            _answerFooter = _root.Q<VisualElement>("quiz-attempt-answer-footer");
            _reviewFooter = _root.Q<VisualElement>("quiz-attempt-review-footer");
            _previousButton = _root.Q<Button>("quiz-attempt-previous-button");
            _reviewToggleRow = _root.Q<VisualElement>("quiz-attempt-review-toggle-row");
            _reviewToggle = _root.Q<Toggle>("quiz-attempt-review-toggle");
            _nextButton = _root.Q<Button>("quiz-attempt-next-button");
            _nextIcon = _root.Q<VisualElement>("quiz-attempt-next-icon");
            _nextLabel = _root.Q<Label>("quiz-attempt-next-label");
            _backToQuestionsButton = _root.Q<Button>("quiz-attempt-back-to-questions-button");
            _submitButton = _root.Q<Button>("quiz-attempt-submit-button");
            _dataStateHost = _root.Q<VisualElement>("quiz-attempt-data-state-host");
            _localModalHost = _root.Q<VisualElement>("quiz-attempt-local-modal-host");
        }

        private void BindDataStatePanel(VisualTreeAsset dataStatePanelAsset)
        {
            if (_dataStateHost == null)
            {
                return;
            }

            if (dataStatePanelAsset == null)
            {
                if (!_warnedMissingDataStateAsset)
                {
                    Debug.LogWarning(
                        "[QuizAttemptPanelView] DataStatePanel VisualTreeAsset is missing. " +
                        "Content preview remains usable; non-Content SetPreviewState calls are no-ops.");
                    _warnedMissingDataStateAsset = true;
                }

                return;
            }

            _ownedDataStateInstance = dataStatePanelAsset.CloneTree();
            _ownedDataStateInstance.style.flexGrow = 1;
            _ownedDataStateInstance.style.width = Length.Percent(100);
            _ownedDataStateInstance.style.height = Length.Percent(100);
            _dataStateHost.Add(_ownedDataStateInstance);

            _dataStateView = new DataStatePanelView(_ownedDataStateInstance);
            if (!_dataStateView.IsBound)
            {
                Debug.LogWarning(
                    "[QuizAttemptPanelView] Failed to bind nested DataStatePanelView.");
                DisposeOwnedDataState();
            }
        }

        private void DisposeOwnedDataState()
        {
            if (_dataStateView != null)
            {
                _dataStateView.PrimaryActionRequested -= OnDataStatePrimaryAction;
                _dataStateView.SecondaryActionRequested -= OnDataStateSecondaryAction;
                _dataStateView.Dispose();
                _dataStateView = null;
            }

            if (_ownedDataStateInstance != null)
            {
                _ownedDataStateInstance.RemoveFromHierarchy();
                _ownedDataStateInstance = null;
            }
        }

        private void RegisterCallbacks()
        {
            _exitClicked = _ => ExitRequested?.Invoke();
            _previousClicked = _ => OnPreviousClicked();
            _nextClicked = _ => OnNextClicked();
            _backToQuestionsClicked = _ => ShowQuestion(CurrentQuestionIndex);
            _submitClicked = _ => OnSubmitClicked();
            _reviewToggleCallback = OnReviewToggleChanged;
            _geometryChanged = OnGeometryChanged;

            _exitButton?.RegisterCallback(_exitClicked);
            _previousButton?.RegisterCallback(_previousClicked);
            _nextButton?.RegisterCallback(_nextClicked);
            _backToQuestionsButton?.RegisterCallback(_backToQuestionsClicked);
            _submitButton?.RegisterCallback(_submitClicked);
            _reviewToggle?.RegisterValueChangedCallback(_reviewToggleCallback);
            _root?.RegisterCallback(_geometryChanged);

            if (_dataStateView != null && _dataStateView.IsBound)
            {
                _dataStateView.PrimaryActionRequested += OnDataStatePrimaryAction;
                _dataStateView.SecondaryActionRequested += OnDataStateSecondaryAction;
            }
        }

        private void UnregisterCallbacks()
        {
            if (_exitButton != null && _exitClicked != null)
            {
                _exitButton.UnregisterCallback(_exitClicked);
            }

            if (_previousButton != null && _previousClicked != null)
            {
                _previousButton.UnregisterCallback(_previousClicked);
            }

            if (_nextButton != null && _nextClicked != null)
            {
                _nextButton.UnregisterCallback(_nextClicked);
            }

            if (_backToQuestionsButton != null && _backToQuestionsClicked != null)
            {
                _backToQuestionsButton.UnregisterCallback(_backToQuestionsClicked);
            }

            if (_submitButton != null && _submitClicked != null)
            {
                _submitButton.UnregisterCallback(_submitClicked);
            }

            if (_reviewToggle != null && _reviewToggleCallback != null)
            {
                _reviewToggle.UnregisterValueChangedCallback(_reviewToggleCallback);
            }

            if (_root != null && _geometryChanged != null)
            {
                _root.UnregisterCallback(_geometryChanged);
            }

            if (_dataStateView != null)
            {
                _dataStateView.PrimaryActionRequested -= OnDataStatePrimaryAction;
                _dataStateView.SecondaryActionRequested -= OnDataStateSecondaryAction;
            }
        }

        private static bool ValidateContext(
            QuizListPreviewItem summary,
            QuizDetailPreviewContent detail,
            out string title,
            out string message)
        {
            title = "Quiz unavailable";
            message = "This quiz cannot be opened in the current preview.";

            if (string.IsNullOrWhiteSpace(summary.Id))
            {
                message = "Quiz summary is missing.";
                return false;
            }

            if (summary.Status != QuizListPreviewStatus.Available)
            {
                title = "Quiz not available";
                message = "Only available quizzes can be attempted in this preview.";
                return false;
            }

            if (detail == null)
            {
                message = "Quiz detail content is missing.";
                return false;
            }

            if (!string.Equals(summary.Id, detail.QuizId, StringComparison.Ordinal))
            {
                message = "Quiz summary and detail identifiers do not match.";
                return false;
            }

            if (detail.QuestionCount <= 0)
            {
                message = "This quiz has no questions to display.";
                return false;
            }

            if (summary.AttemptsUsed >= summary.MaxAttempts)
            {
                title = "Attempt limit reached";
                message = "No attempts remain for this quiz in the current preview.";
                return false;
            }

            return true;
        }

        private void ShowUnavailableState(string title, string message)
        {
            if (_dataStateView == null || !_dataStateView.IsBound)
            {
                return;
            }

            PreviewState = QuizAttemptPreviewState.RecoverableError;
            ShowDataStateHost();
            _dataStateView.SetState(DataStatePanelState.PermissionOrLocked);
            _dataStateView.Configure(
                title: title,
                message: message,
                detail: "Interactive answering is disabled until a valid Available quiz context is provided.",
                iconClass: "ds-icon--lock",
                primaryActionLabel: string.Empty,
                secondaryActionLabel: "Back to Quiz Portal");
        }

        private void ApplyNonContentState(QuizAttemptPreviewState state)
        {
            switch (state)
            {
                case QuizAttemptPreviewState.Submitting:
                    _dataStateView.SetState(DataStatePanelState.Loading);
                    _dataStateView.Configure(new DataStatePanelConfiguration(
                        title: "Submitting your quiz...",
                        message: "Please keep NutriMind open while your answers are being sent.",
                        detail: "Do not start another submission while this message is visible.",
                        iconClass: null,
                        primaryActionLabel: string.Empty,
                        secondaryActionLabel: string.Empty,
                        showSpinner: true));
                    break;

                case QuizAttemptPreviewState.UncertainSubmission:
                    // Uncertain response: do not offer Submit Again — avoids unsafe duplicate attempts.
                    _dataStateView.SetState(DataStatePanelState.RecoverableError);
                    _dataStateView.Configure(new DataStatePanelConfiguration(
                        title: "We’re checking your submission",
                        message: "NutriMind did not receive a clear response from the server.",
                        detail: "Do not submit again. Check the submission status first to avoid a duplicate attempt.",
                        iconClass: "ds-icon--warning",
                        primaryActionLabel: "Check Submission Status",
                        secondaryActionLabel: "Back to Quiz Portal",
                        showSpinner: false));
                    break;

                case QuizAttemptPreviewState.RecoverableError:
                    _dataStateView.SetState(DataStatePanelState.RecoverableError);
                    _dataStateView.Configure(new DataStatePanelConfiguration(
                        title: "Quiz submission could not continue",
                        message: "Your answers are still available in this preview.",
                        detail: "Return to review your answers. No request is sent by this static UI.",
                        iconClass: "ds-icon--error",
                        primaryActionLabel: "Return to Review",
                        secondaryActionLabel: "Back to Quiz Portal",
                        showSpinner: false));
                    break;
            }
        }

        private void ShowContentShell()
        {
            _contentShell?.RemoveFromClassList(ContentShellHiddenClass);
            _dataStateHost?.RemoveFromClassList(DataStateHostVisibleClass);
            _dataStateView?.SetState(DataStatePanelState.Content);
        }

        private void ShowDataStateHost()
        {
            _contentShell?.AddToClassList(ContentShellHiddenClass);
            _dataStateHost?.AddToClassList(DataStateHostVisibleClass);
        }

        private void SetContentMode(QuizAttemptContentMode mode)
        {
            ContentMode = mode;
            bool review = mode == QuizAttemptContentMode.Review;

            if (review)
            {
                _root?.AddToClassList(ReviewRootClass);
                _answerMode?.AddToClassList(AnswerModeHiddenClass);
                _reviewMode?.RemoveFromClassList(ReviewModeHiddenClass);
                _answerFooter?.AddToClassList(AnswerFooterHiddenClass);
                _reviewFooter?.RemoveFromClassList(ReviewFooterHiddenClass);
            }
            else
            {
                _root?.RemoveFromClassList(ReviewRootClass);
                _answerMode?.RemoveFromClassList(AnswerModeHiddenClass);
                _reviewMode?.AddToClassList(ReviewModeHiddenClass);
                _answerFooter?.RemoveFromClassList(AnswerFooterHiddenClass);
                _reviewFooter?.AddToClassList(ReviewFooterHiddenClass);
            }
        }

        private void BindSummaryHeader(QuizListPreviewItem summary)
        {
            if (_title != null)
            {
                _title.text = summary.Title;
            }

            if (_subjectTerm != null)
            {
                _subjectTerm.text =
                    $"{GetSubjectLabel(summary.Subject)} • Term {(int)summary.Term}";
            }

            int attemptNumber = Mathf.Clamp(summary.AttemptsUsed + 1, 1, Mathf.Max(1, summary.MaxAttempts));
            if (_attemptChipLabel != null)
            {
                _attemptChipLabel.text = $"Attempt {attemptNumber} of {summary.MaxAttempts}";
            }
        }

        private void BindCurrentQuestion()
        {
            if (DetailContent == null || CurrentQuestionIndex < 0
                || CurrentQuestionIndex >= DetailContent.QuestionCount)
            {
                return;
            }

            QuizDetailPreviewQuestion question = DetailContent.Questions[CurrentQuestionIndex];
            if (_questionNumber != null)
            {
                _questionNumber.text = $"Question {CurrentQuestionIndex + 1}";
            }

            if (_questionType != null)
            {
                _questionType.text = GetQuestionTypeLabel(question.Type);
            }

            if (_questionPrompt != null)
            {
                _questionPrompt.text = question.Prompt;
            }

            if (_selectionInstruction != null)
            {
                _selectionInstruction.text = question.Type == QuizDetailPreviewQuestionType.MultipleChoiceMultiple
                    ? "Select all answers that apply."
                    : "Select one answer.";
            }

            RebuildOptions(question);
            SyncReviewToggle(question.Id);
        }

        private void RebuildOptions(QuizDetailPreviewQuestion question)
        {
            ClearOptionBindings();
            if (_optionsHost == null)
            {
                return;
            }

            _optionsHost.Clear();
            HashSet<string> selected = GetOrCreateSelectionSet(question.Id);
            bool multi = question.Type == QuizDetailPreviewQuestionType.MultipleChoiceMultiple;

            // Add controls directly to the host. Do not wrap in RadioButtonGroup —
            // that control rebuilds from choices and can hide manually added children.
            for (int i = 0; i < question.Options.Count; i++)
            {
                QuizDetailPreviewOption option = question.Options[i];
                string letter = GetOptionLetter(i);
                string label = $"{letter}. {option.Text}";
                string optionId = option.Id;
                bool isSelected = selected.Contains(optionId);

                if (multi)
                {
                    var toggle = new Toggle
                    {
                        text = label,
                        name = $"quiz-attempt-option-{optionId}"
                    };
                    toggle.AddToClassList("ds-check");
                    toggle.AddToClassList("quiz-attempt-panel__option");
                    if (isSelected)
                    {
                        toggle.AddToClassList(OptionSelectedClass);
                    }

                    toggle.SetValueWithoutNotify(isSelected);

                    EventCallback<ChangeEvent<bool>> callback = evt =>
                    {
                        if (_suppressOptionCallbacks)
                        {
                            return;
                        }

                        OnMultiOptionChanged(question.Id, optionId, evt.newValue, toggle);
                    };

                    toggle.RegisterValueChangedCallback(callback);
                    _optionBindings.Add(new OptionBinding(toggle, null, callback, optionId, question.Id));
                    _optionsHost.Add(toggle);
                }
                else
                {
                    var radio = new RadioButton
                    {
                        text = label,
                        name = $"quiz-attempt-option-{optionId}"
                    };
                    radio.AddToClassList("ds-radio");
                    radio.AddToClassList("quiz-attempt-panel__option");
                    if (isSelected)
                    {
                        radio.AddToClassList(OptionSelectedClass);
                    }

                    radio.SetValueWithoutNotify(isSelected);

                    EventCallback<ChangeEvent<bool>> callback = evt =>
                    {
                        if (_suppressOptionCallbacks || !evt.newValue)
                        {
                            return;
                        }

                        OnSingleOptionSelected(question.Id, optionId);
                    };

                    radio.RegisterValueChangedCallback(callback);
                    _optionBindings.Add(new OptionBinding(null, radio, callback, optionId, question.Id));
                    _optionsHost.Add(radio);
                }
            }
        }

        private void OnSingleOptionSelected(string questionId, string optionId)
        {
            HashSet<string> selected = GetOrCreateSelectionSet(questionId);
            selected.Clear();
            selected.Add(optionId);

            _suppressOptionCallbacks = true;
            for (int i = 0; i < _optionBindings.Count; i++)
            {
                OptionBinding binding = _optionBindings[i];
                if (binding.Radio == null)
                {
                    continue;
                }

                bool match = string.Equals(binding.OptionId, optionId, StringComparison.Ordinal);
                binding.Radio.SetValueWithoutNotify(match);
                binding.Radio.EnableInClassList(OptionSelectedClass, match);
            }

            _suppressOptionCallbacks = false;
            RefreshCounts();
            RefreshNavigatorVisuals();
        }

        private void OnMultiOptionChanged(
            string questionId,
            string optionId,
            bool isOn,
            Toggle toggle)
        {
            HashSet<string> selected = GetOrCreateSelectionSet(questionId);
            if (isOn)
            {
                selected.Add(optionId);
            }
            else
            {
                selected.Remove(optionId);
            }

            toggle.EnableInClassList(OptionSelectedClass, isOn);
            RefreshCounts();
            RefreshNavigatorVisuals();
        }

        private void ClearOptionBindings()
        {
            for (int i = 0; i < _optionBindings.Count; i++)
            {
                OptionBinding binding = _optionBindings[i];
                if (binding.Toggle != null && binding.ToggleCallback != null)
                {
                    binding.Toggle.UnregisterValueChangedCallback(binding.ToggleCallback);
                }

                if (binding.Radio != null && binding.ToggleCallback != null)
                {
                    binding.Radio.UnregisterValueChangedCallback(binding.ToggleCallback);
                }
            }

            _optionBindings.Clear();
            _optionsHost?.Clear();
        }

        private void RebuildNavigator()
        {
            ClearNavigatorBindings();
            if (_questionNavHost == null || DetailContent == null)
            {
                return;
            }

            _questionNavHost.Clear();
            for (int i = 0; i < DetailContent.QuestionCount; i++)
            {
                int index = i;
                var button = new Button
                {
                    text = (i + 1).ToString(),
                    name = $"quiz-attempt-nav-{i + 1}"
                };
                button.AddToClassList("ds-btn");
                button.AddToClassList("ds-btn--ghost");
                button.AddToClassList("quiz-attempt-panel__question-nav");

                EventCallback<ClickEvent> callback = _ =>
                {
                    if (index == CurrentQuestionIndex
                        && ContentMode == QuizAttemptContentMode.Answering)
                    {
                        return;
                    }

                    ShowQuestion(index);
                };

                button.RegisterCallback(callback);
                _navigatorBindings.Add(new NavigatorBinding(button, callback, index));
                _questionNavHost.Add(button);
            }

            RefreshNavigatorVisuals();
        }

        private void ClearNavigatorBindings()
        {
            for (int i = 0; i < _navigatorBindings.Count; i++)
            {
                NavigatorBinding binding = _navigatorBindings[i];
                if (binding.Button != null && binding.Callback != null)
                {
                    binding.Button.UnregisterCallback(binding.Callback);
                }
            }

            _navigatorBindings.Clear();
            _questionNavHost?.Clear();
        }

        private void RefreshNavigatorVisuals()
        {
            if (DetailContent == null)
            {
                return;
            }

            for (int i = 0; i < _navigatorBindings.Count; i++)
            {
                NavigatorBinding binding = _navigatorBindings[i];
                if (binding.Button == null || binding.Index >= DetailContent.QuestionCount)
                {
                    continue;
                }

                string questionId = DetailContent.Questions[binding.Index].Id;
                bool answered = IsQuestionAnswered(questionId);
                bool marked = _markedQuestionIds.Contains(questionId);
                bool current = binding.Index == CurrentQuestionIndex
                    && ContentMode == QuizAttemptContentMode.Answering;

                binding.Button.EnableInClassList(NavCurrentClass, current);
                binding.Button.EnableInClassList(NavAnsweredClass, answered);
                binding.Button.EnableInClassList(NavUnansweredClass, !answered);
                binding.Button.EnableInClassList(NavMarkedClass, marked);

                string status = current ? "current" : string.Empty;
                if (answered)
                {
                    status = string.IsNullOrEmpty(status) ? "answered" : status + ", answered";
                }
                else
                {
                    status = string.IsNullOrEmpty(status) ? "unanswered" : status + ", unanswered";
                }

                if (marked)
                {
                    status += ", marked for review";
                }

                binding.Button.tooltip = $"Question {binding.Index + 1} — {status}";
            }
        }

        private void RebuildReviewCards()
        {
            ClearReviewCardBindings();
            if (_reviewList == null || DetailContent == null)
            {
                return;
            }

            _reviewList.Clear();
            for (int i = 0; i < DetailContent.QuestionCount; i++)
            {
                int index = i;
                QuizDetailPreviewQuestion question = DetailContent.Questions[i];
                HashSet<string> selected = GetOrCreateSelectionSet(question.Id);
                bool answered = selected.Count > 0;
                bool marked = _markedQuestionIds.Contains(question.Id);

                var card = new VisualElement();
                card.AddToClassList("ds-card");
                card.AddToClassList("quiz-attempt-panel__review-card");

                var number = new Label($"Question {i + 1}")
                {
                    pickingMode = PickingMode.Ignore
                };
                number.AddToClassList("quiz-attempt-panel__review-card-number");
                card.Add(number);

                var prompt = new Label(question.Prompt)
                {
                    pickingMode = PickingMode.Ignore
                };
                prompt.AddToClassList("quiz-attempt-panel__review-card-prompt");
                card.Add(prompt);

                var answer = new Label(answered
                    ? BuildSelectedAnswerText(question, selected)
                    : "Not answered")
                {
                    pickingMode = PickingMode.Ignore
                };
                answer.AddToClassList("quiz-attempt-panel__review-card-answer");
                card.Add(answer);

                var status = new Label(answered ? "Answered" : "Not answered")
                {
                    pickingMode = PickingMode.Ignore
                };
                status.AddToClassList("quiz-attempt-panel__review-card-status");
                card.Add(status);

                if (marked)
                {
                    var markedLabel = new Label("Marked for review")
                    {
                        pickingMode = PickingMode.Ignore
                    };
                    markedLabel.AddToClassList("quiz-attempt-panel__review-card-marked");
                    card.Add(markedLabel);
                }

                var editButton = new Button { text = "Edit Answer" };
                editButton.AddToClassList("ds-btn");
                editButton.AddToClassList("ds-btn--secondary");
                editButton.AddToClassList("quiz-attempt-panel__review-edit-button");

                EventCallback<ClickEvent> callback = _ => ShowQuestion(index);
                editButton.RegisterCallback(callback);
                _reviewCardBindings.Add(new ReviewCardBinding(editButton, callback));
                card.Add(editButton);
                _reviewList.Add(card);
            }
        }

        private void ClearReviewCardBindings()
        {
            for (int i = 0; i < _reviewCardBindings.Count; i++)
            {
                ReviewCardBinding binding = _reviewCardBindings[i];
                if (binding.Button != null && binding.Callback != null)
                {
                    binding.Button.UnregisterCallback(binding.Callback);
                }
            }

            _reviewCardBindings.Clear();
            _reviewList?.Clear();
        }

        private static string BuildSelectedAnswerText(
            QuizDetailPreviewQuestion question,
            HashSet<string> selectedIds)
        {
            var parts = new List<string>();
            for (int i = 0; i < question.Options.Count; i++)
            {
                QuizDetailPreviewOption option = question.Options[i];
                if (selectedIds.Contains(option.Id))
                {
                    parts.Add($"{GetOptionLetter(i)}. {option.Text}");
                }
            }

            return parts.Count == 0 ? "Not answered" : string.Join(", ", parts);
        }

        private void SyncReviewToggle(string questionId)
        {
            if (_reviewToggle == null)
            {
                return;
            }

            _suppressReviewToggleCallback = true;
            _reviewToggle.SetValueWithoutNotify(_markedQuestionIds.Contains(questionId));
            _suppressReviewToggleCallback = false;
        }

        private void OnReviewToggleChanged(ChangeEvent<bool> evt)
        {
            if (_suppressReviewToggleCallback
                || !_hasValidContext
                || DetailContent == null
                || CurrentQuestionIndex < 0
                || CurrentQuestionIndex >= DetailContent.QuestionCount)
            {
                return;
            }

            string questionId = DetailContent.Questions[CurrentQuestionIndex].Id;
            if (evt.newValue)
            {
                _markedQuestionIds.Add(questionId);
            }
            else
            {
                _markedQuestionIds.Remove(questionId);
            }

            RefreshCounts();
            RefreshNavigatorVisuals();
        }

        private void OnPreviousClicked()
        {
            if (CurrentQuestionIndex <= 0)
            {
                return;
            }

            ShowQuestion(CurrentQuestionIndex - 1);
        }

        private void OnNextClicked()
        {
            if (DetailContent == null)
            {
                return;
            }

            if (CurrentQuestionIndex >= DetailContent.QuestionCount - 1)
            {
                ShowReview();
                return;
            }

            ShowQuestion(CurrentQuestionIndex + 1);
        }

        private void OnSubmitClicked()
        {
            if (PreviewState != QuizAttemptPreviewState.Content
                || ContentMode != QuizAttemptContentMode.Review
                || !_hasValidContext
                || DetailContent == null)
            {
                return;
            }

            SubmitRequested?.Invoke(BuildSubmissionPayload());
        }

        private QuizAttemptPreviewSubmission BuildSubmissionPayload()
        {
            var answers = new List<QuizAttemptPreviewAnswer>(DetailContent.QuestionCount);
            for (int i = 0; i < DetailContent.QuestionCount; i++)
            {
                QuizDetailPreviewQuestion question = DetailContent.Questions[i];
                HashSet<string> selected = GetOrCreateSelectionSet(question.Id);
                var ids = new List<string>();
                for (int optionIndex = 0; optionIndex < question.Options.Count; optionIndex++)
                {
                    string optionId = question.Options[optionIndex].Id;
                    if (selected.Contains(optionId))
                    {
                        ids.Add(optionId);
                    }
                }

                answers.Add(new QuizAttemptPreviewAnswer(question.Id, ids));
            }

            RefreshCounts();
            return new QuizAttemptPreviewSubmission(
                DetailContent.QuizId,
                answers,
                DetailContent.QuestionCount,
                AnsweredCount,
                UnansweredCount,
                MarkedCount);
        }

        private void RefreshFooterForAnswering()
        {
            if (_previousButton != null)
            {
                _previousButton.SetEnabled(CurrentQuestionIndex > 0);
            }

            bool lastQuestion = DetailContent != null
                && CurrentQuestionIndex >= DetailContent.QuestionCount - 1;

            if (_nextLabel != null)
            {
                _nextLabel.text = lastQuestion ? "Review Answers" : "Next Question";
            }

            if (_nextButton != null)
            {
                _nextButton.tooltip = lastQuestion
                    ? "Review your answers before submitting"
                    : "Go to the next question";
            }

            if (_nextIcon != null)
            {
                for (int i = 0; i < NextIconClasses.Length; i++)
                {
                    _nextIcon.RemoveFromClassList(NextIconClasses[i]);
                }

                _nextIcon.AddToClassList(lastQuestion ? "ds-icon--list" : "ds-icon--arrow-right");
            }
        }

        private void RefreshHeaderProgress()
        {
            if (DetailContent == null)
            {
                return;
            }

            int total = DetailContent.QuestionCount;
            if (ContentMode == QuizAttemptContentMode.Review)
            {
                if (_questionProgressLabel != null)
                {
                    _questionProgressLabel.text = "Review answers";
                }

                if (_progressBar != null)
                {
                    _progressBar.value = 100f;
                }

                return;
            }

            int display = CurrentQuestionIndex + 1;
            if (_questionProgressLabel != null)
            {
                _questionProgressLabel.text = $"Question {display} of {total}";
            }

            if (_progressBar != null)
            {
                _progressBar.value = total <= 0 ? 0f : (display / (float)total) * 100f;
            }
        }

        private void RefreshCounts()
        {
            if (DetailContent == null)
            {
                AnsweredCount = 0;
                UnansweredCount = 0;
                MarkedCount = 0;
                return;
            }

            int answered = 0;
            for (int i = 0; i < DetailContent.QuestionCount; i++)
            {
                if (IsQuestionAnswered(DetailContent.Questions[i].Id))
                {
                    answered++;
                }
            }

            AnsweredCount = answered;
            UnansweredCount = DetailContent.QuestionCount - answered;
            MarkedCount = _markedQuestionIds.Count;

            if (_answeredCountLabel != null)
            {
                _answeredCountLabel.text = $"{AnsweredCount} of {DetailContent.QuestionCount}";
            }

            if (_unansweredCountLabel != null)
            {
                _unansweredCountLabel.text = UnansweredCount.ToString();
            }

            if (_markedCountLabel != null)
            {
                _markedCountLabel.text = MarkedCount.ToString();
            }
        }

        private void RefreshReviewSummary()
        {
            if (_reviewSummary == null || DetailContent == null)
            {
                return;
            }

            _reviewSummary.text =
                $"{DetailContent.QuestionCount} questions · {AnsweredCount} answered · " +
                $"{UnansweredCount} unanswered · {MarkedCount} marked";
        }

        private bool IsQuestionAnswered(string questionId)
        {
            return _selectedOptionIdsByQuestionId.TryGetValue(questionId, out HashSet<string> set)
                && set != null
                && set.Count > 0;
        }

        private HashSet<string> GetOrCreateSelectionSet(string questionId)
        {
            if (!_selectedOptionIdsByQuestionId.TryGetValue(questionId, out HashSet<string> set)
                || set == null)
            {
                set = new HashSet<string>();
                _selectedOptionIdsByQuestionId[questionId] = set;
            }

            return set;
        }

        private void ResetLocalAnswerState()
        {
            _selectedOptionIdsByQuestionId.Clear();
            _markedQuestionIds.Clear();
            CurrentQuestionIndex = 0;
            AnsweredCount = 0;
            UnansweredCount = 0;
            MarkedCount = 0;
        }

        private void OnDataStatePrimaryAction()
        {
            switch (PreviewState)
            {
                case QuizAttemptPreviewState.UncertainSubmission:
                    CheckSubmissionStatusRequested?.Invoke();
                    break;

                case QuizAttemptPreviewState.RecoverableError:
                    ReturnToReviewRequested?.Invoke();
                    break;
            }
        }

        private void OnDataStateSecondaryAction()
        {
            if (PreviewState == QuizAttemptPreviewState.UncertainSubmission
                || PreviewState == QuizAttemptPreviewState.RecoverableError)
            {
                BackToQuizPortalRequested?.Invoke();
            }
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            ApplyResponsiveClasses(evt.newRect.width);
        }

        private void ApplyResponsiveClasses(float width)
        {
            if (!IsBound || width <= 0f || Mathf.Approximately(width, _lastWidth))
            {
                return;
            }

            _lastWidth = width;
            bool narrow = width < NarrowBreakpoint;
            _root.EnableInClassList(CompactClass, width < CompactBreakpoint);
            _root.EnableInClassList(NarrowClass, narrow);
            _root.EnableInClassList(MobileClass, narrow);
            _root.EnableInClassList("app-screen-content--compact", width < CompactBreakpoint);
            _root.EnableInClassList("app-screen-content--narrow", narrow);
            // UITK does not support CSS `order`; reorder Mark-for-review for narrow layout in code.
            UpdateAnswerFooterChildOrder(narrow);
        }

        private void UpdateAnswerFooterChildOrder(bool narrow)
        {
            if (_answerFooter == null
                || _reviewToggleRow == null
                || _previousButton == null
                || _nextButton == null)
            {
                return;
            }

            if (narrow)
            {
                _answerFooter.Insert(0, _reviewToggleRow);
                _answerFooter.Insert(1, _previousButton);
                _answerFooter.Insert(2, _nextButton);
                return;
            }

            _answerFooter.Insert(0, _previousButton);
            _answerFooter.Insert(1, _reviewToggleRow);
            _answerFooter.Insert(2, _nextButton);
        }

        private static string GetOptionLetter(int index)
        {
            if (index < 0 || index > 25)
            {
                return (index + 1).ToString();
            }

            return ((char)('A' + index)).ToString();
        }

        private static string GetQuestionTypeLabel(QuizDetailPreviewQuestionType type) =>
            type switch
            {
                QuizDetailPreviewQuestionType.MultipleChoiceSingle => "Single choice",
                QuizDetailPreviewQuestionType.MultipleChoiceMultiple => "Multiple choice",
                QuizDetailPreviewQuestionType.TrueFalse => "True or false",
                _ => "Question"
            };

        private static string GetSubjectLabel(NutriMindSubject subject) =>
            subject switch
            {
                NutriMindSubject.LiteraQuest => "LiteraQuest",
                NutriMindSubject.PeAndHealth => "PE & Health",
                NutriMindSubject.Science => "Science",
                _ => subject.ToString()
            };

        private readonly struct OptionBinding
        {
            public OptionBinding(
                Toggle toggle,
                RadioButton radio,
                EventCallback<ChangeEvent<bool>> toggleCallback,
                string optionId,
                string questionId)
            {
                Toggle = toggle;
                Radio = radio;
                ToggleCallback = toggleCallback;
                OptionId = optionId;
                QuestionId = questionId;
            }

            public Toggle Toggle { get; }
            public RadioButton Radio { get; }
            public EventCallback<ChangeEvent<bool>> ToggleCallback { get; }
            public string OptionId { get; }
            public string QuestionId { get; }
        }

        private readonly struct NavigatorBinding
        {
            public NavigatorBinding(Button button, EventCallback<ClickEvent> callback, int index)
            {
                Button = button;
                Callback = callback;
                Index = index;
            }

            public Button Button { get; }
            public EventCallback<ClickEvent> Callback { get; }
            public int Index { get; }
        }

        private readonly struct ReviewCardBinding
        {
            public ReviewCardBinding(Button button, EventCallback<ClickEvent> callback)
            {
                Button = button;
                Callback = callback;
            }

            public Button Button { get; }
            public EventCallback<ClickEvent> Callback { get; }
        }
    }
}
