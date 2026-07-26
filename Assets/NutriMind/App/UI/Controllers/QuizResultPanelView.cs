using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace NutriMind.App.UI
{
    /// <summary>
    /// Presentation-only QuizResult route states for static preview.
    /// </summary>
    public enum QuizResultPreviewState
    {
        Content = 0,
        Loading = 1,
        RecoverableError = 2
    }

    /// <summary>
    /// Presentation-only result status labels for static Quiz Portal result preview.
    /// </summary>
    public enum QuizResultPreviewStatus
    {
        Submitted = 0,
        Scored = 1,
        PendingVisibility = 2
    }

    /// <summary>
    /// Immutable presentation-only answer feedback row from the scored-result fixture.
    /// Does not include selected options, correct options, explanations, or possible points.
    /// </summary>
    public readonly struct QuizResultPreviewAnswer
    {
        public QuizResultPreviewAnswer(string questionId, bool correct, float earnedPoints)
        {
            QuestionId = questionId ?? string.Empty;
            Correct = correct;
            EarnedPoints = earnedPoints;
        }

        public string QuestionId { get; }
        public bool Correct { get; }
        public float EarnedPoints { get; }
    }

    /// <summary>
    /// Immutable presentation-only Quiz Portal result content for static preview.
    /// Displays server-provided scored fields only. Not a production DTO.
    /// </summary>
    public sealed class QuizResultPreviewContent
    {
        public QuizResultPreviewContent(
            string attemptId,
            string quizId,
            QuizResultPreviewStatus status,
            float earnedPoints,
            float possiblePoints,
            float percentage,
            bool? passed,
            int correctCount,
            int incorrectCount,
            int unansweredCount,
            DateTimeOffset submittedAtUtc,
            bool feedbackVisible,
            IReadOnlyList<QuizResultPreviewAnswer> answers)
        {
            AttemptId = attemptId ?? string.Empty;
            QuizId = quizId ?? string.Empty;
            Status = status;
            EarnedPoints = earnedPoints;
            PossiblePoints = possiblePoints;
            Percentage = percentage;
            Passed = passed;
            CorrectCount = correctCount;
            IncorrectCount = incorrectCount;
            UnansweredCount = unansweredCount;
            SubmittedAtUtc = submittedAtUtc;
            FeedbackVisible = feedbackVisible;

            if (answers == null || answers.Count == 0)
            {
                Answers = Array.Empty<QuizResultPreviewAnswer>();
            }
            else
            {
                var copy = new QuizResultPreviewAnswer[answers.Count];
                for (int i = 0; i < answers.Count; i++)
                {
                    copy[i] = answers[i];
                }

                Answers = copy;
            }
        }

        public string AttemptId { get; }
        public string QuizId { get; }
        public QuizResultPreviewStatus Status { get; }
        public float EarnedPoints { get; }
        public float PossiblePoints { get; }
        public float Percentage { get; }
        public bool? Passed { get; }
        public int CorrectCount { get; }
        public int IncorrectCount { get; }
        public int UnansweredCount { get; }
        public DateTimeOffset SubmittedAtUtc { get; }
        public bool FeedbackVisible { get; }
        public IReadOnlyList<QuizResultPreviewAnswer> Answers { get; }
    }

    /// <summary>
    /// Deterministic static-preview catalog for Quiz Portal scored-result fixtures.
    /// Returns canonical result only for quiz_fixture_001. Not a production repository.
    /// </summary>
    public static class QuizResultPreviewCatalog
    {
        public const string CanonicalQuizId = "quiz_fixture_001";
        public const string CanonicalAttemptId = "attempt_fixture_001";

        /// <summary>
        /// Returns true only when a repository scored-result fixture exists for the quiz id.
        /// Does not invent scores for synthetic QuizList items.
        /// </summary>
        public static bool TryGetResult(string quizId, out QuizResultPreviewContent content)
        {
            if (string.Equals(quizId, CanonicalQuizId, StringComparison.Ordinal))
            {
                content = CreateCanonicalResult();
                return true;
            }

            content = null;
            return false;
        }

        private static QuizResultPreviewContent CreateCanonicalResult()
        {
            var answers = new QuizResultPreviewAnswer[]
            {
                new("qq_001", true, 1f),
                new("qq_002", true, 1f)
            };

            return new QuizResultPreviewContent(
                CanonicalAttemptId,
                CanonicalQuizId,
                QuizResultPreviewStatus.Scored,
                2f,
                2f,
                100f,
                true,
                2,
                0,
                0,
                new DateTimeOffset(2026, 7, 19, 4, 30, 0, TimeSpan.Zero),
                true,
                answers);
        }
    }

    /// <summary>
    /// Presentation-only Quiz Portal result view. Displays server-provided score fields,
    /// optional question feedback when visible, and shared data states.
    /// Does not call APIs, recalculate scores, invent answers, or persist results.
    /// </summary>
    public sealed class QuizResultPanelView : IAppScreenView
    {
        private const string RootName = "quiz-result-root";
        private const string CompactClass = "quiz-result-panel--compact";
        private const string NarrowClass = "quiz-result-panel--narrow";
        private const string MobileClass = "mobile";
        private const string DataStateHostVisibleClass = "quiz-result-panel__data-state-host--visible";
        private const string ContentShellHiddenClass = "quiz-result-panel__content-shell--hidden";
        private const string FeedbackListHiddenClass = "quiz-result-panel__feedback-list--hidden";
        private const string FeedbackMessageHiddenClass = "quiz-result-panel__feedback-message--hidden";
        private const string FeedbackHiddenNoteHiddenClass = "quiz-result-panel__feedback-hidden-note--hidden";
        private const float CompactBreakpoint = 1100f;
        private const float NarrowBreakpoint = 820f;

        private VisualElement _root;
        private VisualElement _contentShell;
        private ScrollView _scroll;
        private VisualElement _body;
        private Label _eyebrow;
        private Label _title;
        private Label _subjectTerm;
        private Label _statusLabel;
        private Label _passLabel;
        private VisualElement _passChip;
        private Label _submittedAt;
        private Label _percentage;
        private Label _points;
        private Label _correctValue;
        private Label _incorrectValue;
        private Label _unansweredValue;
        private Label _totalPointsValue;
        private Label _feedbackTitle;
        private Label _feedbackMessage;
        private VisualElement _feedbackList;
        private VisualElement _feedbackHiddenNote;
        private Button _backButton;
        private Button _historyButton;
        private VisualElement _dataStateHost;

        private TemplateContainer _ownedDataStateInstance;
        private DataStatePanelView _dataStateView;
        private bool _warnedMissingDataStateAsset;
        private bool _disposed;
        private float _lastWidth = -1f;
        private bool _hasValidContext;
        private bool _isFixtureGap;
        private string _fixtureGapQuizTitle = string.Empty;

        private EventCallback<ClickEvent> _backClicked;
        private EventCallback<ClickEvent> _historyClicked;
        private EventCallback<GeometryChangedEvent> _geometryChanged;

        public QuizResultPanelView(
            VisualElement root,
            VisualTreeAsset dataStatePanelAsset = null)
        {
            ResolveRoot(root);
            if (_root == null)
            {
                Debug.LogWarning(
                    "[QuizResultPanelView] Could not resolve quiz-result-root inside the supplied element.");
                return;
            }

            CacheElements();
            BindDataStatePanel(dataStatePanelAsset);
            RegisterCallbacks();
            ApplyResponsiveClasses(_root.resolvedStyle.width);

            QuizListPreviewItem summary = QuizDetailPreviewCatalog.CreateCanonicalSummary();
            if (QuizDetailPreviewCatalog.TryGetDetail(summary.Id, out QuizDetailPreviewContent detail)
                && QuizResultPreviewCatalog.TryGetResult(summary.Id, out QuizResultPreviewContent result))
            {
                SetResultContext(summary, detail, result);
            }

            SetPreviewState(QuizResultPreviewState.Content);
        }

        public VisualElement Root => _root;

        public bool IsBound => _root != null && !_disposed;

        public QuizResultPreviewState PreviewState { get; private set; } =
            QuizResultPreviewState.Content;

        public QuizListPreviewItem SelectedSummary { get; private set; }

        public QuizDetailPreviewContent DetailContent { get; private set; }

        public QuizResultPreviewContent ResultContent { get; private set; }

        public event Action BackToQuizPortalRequested;
        public event Action ViewHistoryRequested;
        public event Action RetryRequested;

        /// <summary>
        /// Binds summary, detail, and server-provided result for static preview.
        /// Validates consistency only — does not recalculate displayed scores.
        /// </summary>
        public void SetResultContext(
            QuizListPreviewItem summary,
            QuizDetailPreviewContent detail,
            QuizResultPreviewContent result)
        {
            if (!IsBound)
            {
                return;
            }

            SelectedSummary = summary;
            DetailContent = detail;
            ResultContent = result;
            _isFixtureGap = false;
            _fixtureGapQuizTitle = summary.Title ?? string.Empty;

            if (result == null)
            {
                _hasValidContext = false;
                ResultContent = null;
                ShowFixtureGapState(summary.Title);
                return;
            }

            if (!ValidateContext(summary, detail, result, out string errorTitle, out string errorMessage))
            {
                _hasValidContext = false;
                DetailContent = null;
                ResultContent = null;
                ShowRecoverableStaticError(errorTitle, errorMessage);
                return;
            }

            _hasValidContext = true;
            BindContent(summary, detail, result);
            SetPreviewState(QuizResultPreviewState.Content);
        }

        public void SetPreviewState(QuizResultPreviewState state)
        {
            if (!IsBound)
            {
                return;
            }

            if (state == QuizResultPreviewState.Content && !_hasValidContext)
            {
                return;
            }

            PreviewState = state;

            if (state == QuizResultPreviewState.Content)
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

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            UnregisterCallbacks();
            ClearFeedbackCards();
            DisposeOwnedDataState();

            BackToQuizPortalRequested = null;
            ViewHistoryRequested = null;
            RetryRequested = null;

            _root = null;
            _contentShell = null;
            _scroll = null;
            _body = null;
            _eyebrow = null;
            _title = null;
            _subjectTerm = null;
            _statusLabel = null;
            _passLabel = null;
            _passChip = null;
            _submittedAt = null;
            _percentage = null;
            _points = null;
            _correctValue = null;
            _incorrectValue = null;
            _unansweredValue = null;
            _totalPointsValue = null;
            _feedbackTitle = null;
            _feedbackMessage = null;
            _feedbackList = null;
            _feedbackHiddenNote = null;
            _backButton = null;
            _historyButton = null;
            _dataStateHost = null;
            DetailContent = null;
            ResultContent = null;
            _lastWidth = -1f;
            _hasValidContext = false;
            _isFixtureGap = false;
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
            _contentShell = _root.Q<VisualElement>("quiz-result-content-shell");
            _scroll = _root.Q<ScrollView>("quiz-result-scroll");
            _body = _root.Q<VisualElement>("quiz-result-body");
            _eyebrow = _root.Q<Label>("quiz-result-eyebrow");
            _title = _root.Q<Label>("quiz-result-title");
            _subjectTerm = _root.Q<Label>("quiz-result-subject-term");
            _statusLabel = _root.Q<Label>("quiz-result-status-label");
            _passLabel = _root.Q<Label>("quiz-result-pass-label");
            _passChip = _root.Q<VisualElement>("quiz-result-pass-chip");
            _submittedAt = _root.Q<Label>("quiz-result-submitted-at");
            _percentage = _root.Q<Label>("quiz-result-percentage");
            _points = _root.Q<Label>("quiz-result-points");
            _correctValue = _root.Q<Label>("quiz-result-correct-value");
            _incorrectValue = _root.Q<Label>("quiz-result-incorrect-value");
            _unansweredValue = _root.Q<Label>("quiz-result-unanswered-value");
            _totalPointsValue = _root.Q<Label>("quiz-result-total-points-value");
            _feedbackTitle = _root.Q<Label>("quiz-result-feedback-title");
            _feedbackMessage = _root.Q<Label>("quiz-result-feedback-message");
            _feedbackList = _root.Q<VisualElement>("quiz-result-feedback-list");
            _feedbackHiddenNote = _root.Q<VisualElement>("quiz-result-feedback-hidden-note");
            _backButton = _root.Q<Button>("quiz-result-back-button");
            _historyButton = _root.Q<Button>("quiz-result-history-button");
            _dataStateHost = _root.Q<VisualElement>("quiz-result-data-state-host");
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
                        "[QuizResultPanelView] DataStatePanel VisualTreeAsset is missing. " +
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
                    "[QuizResultPanelView] Failed to bind nested DataStatePanelView.");
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
            _backClicked = _ => BackToQuizPortalRequested?.Invoke();
            _historyClicked = _ => ViewHistoryRequested?.Invoke();
            _geometryChanged = OnGeometryChanged;

            _backButton?.RegisterCallback(_backClicked);
            _historyButton?.RegisterCallback(_historyClicked);
            _root?.RegisterCallback(_geometryChanged);

            if (_dataStateView != null && _dataStateView.IsBound)
            {
                _dataStateView.PrimaryActionRequested += OnDataStatePrimaryAction;
                _dataStateView.SecondaryActionRequested += OnDataStateSecondaryAction;
            }
        }

        private void UnregisterCallbacks()
        {
            if (_backButton != null && _backClicked != null)
            {
                _backButton.UnregisterCallback(_backClicked);
            }

            if (_historyButton != null && _historyClicked != null)
            {
                _historyButton.UnregisterCallback(_historyClicked);
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
            QuizResultPreviewContent result,
            out string title,
            out string message)
        {
            title = "Quiz result could not be shown";
            message = "This result preview failed a consistency check.";

            if (string.IsNullOrWhiteSpace(summary.Id))
            {
                message = "Quiz summary is missing.";
                return false;
            }

            if (detail == null)
            {
                message = "Quiz detail content is missing.";
                return false;
            }

            if (result == null)
            {
                message = "Quiz result content is missing.";
                return false;
            }

            if (!string.Equals(summary.Id, detail.QuizId, StringComparison.Ordinal)
                || !string.Equals(summary.Id, result.QuizId, StringComparison.Ordinal))
            {
                message = "Quiz summary, detail, and result identifiers do not match.";
                return false;
            }

            if (detail.QuestionCount <= 0)
            {
                message = "This quiz has no questions to display.";
                return false;
            }

            if (result.PossiblePoints < 0f || result.EarnedPoints < 0f)
            {
                message = "Result point values are invalid.";
                return false;
            }

            if (result.EarnedPoints > result.PossiblePoints)
            {
                message = "Earned points exceed possible points.";
                return false;
            }

            if (result.Percentage < 0f || result.Percentage > 100f)
            {
                message = "Result percentage is outside 0–100.";
                return false;
            }

            if (result.CorrectCount + result.IncorrectCount + result.UnansweredCount
                != detail.QuestionCount)
            {
                message = "Result question counts do not match the quiz detail.";
                return false;
            }

            var seenQuestionIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < result.Answers.Count; i++)
            {
                string questionId = result.Answers[i].QuestionId;
                if (string.IsNullOrWhiteSpace(questionId) || !seenQuestionIds.Add(questionId))
                {
                    message = "Result answer rows contain missing or duplicate question IDs.";
                    return false;
                }

                if (!TryFindDetailQuestion(detail, questionId))
                {
                    message = "A result answer does not map to a quiz detail question.";
                    return false;
                }
            }

            return true;
        }

        private static bool TryFindDetailQuestion(QuizDetailPreviewContent detail, string questionId)
        {
            for (int i = 0; i < detail.QuestionCount; i++)
            {
                if (string.Equals(detail.Questions[i].Id, questionId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private void BindContent(
            QuizListPreviewItem summary,
            QuizDetailPreviewContent detail,
            QuizResultPreviewContent result)
        {
            if (_eyebrow != null)
            {
                _eyebrow.text = "Quiz complete";
            }

            if (_title != null)
            {
                _title.text = summary.Title;
            }

            if (_subjectTerm != null)
            {
                _subjectTerm.text =
                    $"{GetSubjectLabel(summary.Subject)} • Term {(int)summary.Term}";
            }

            if (_statusLabel != null)
            {
                _statusLabel.text = GetStatusLabel(result.Status);
            }

            if (_passLabel != null)
            {
                _passLabel.text = GetPassLabel(result.Passed);
            }

            if (_passChip != null)
            {
                _passChip.style.display = DisplayStyle.Flex;
            }

            if (_submittedAt != null)
            {
                _submittedAt.text = $"Submitted {FormatSubmittedAt(result.SubmittedAtUtc)}";
            }

            if (_percentage != null)
            {
                _percentage.text = FormatPercentage(result.Percentage);
            }

            if (_points != null)
            {
                _points.text =
                    $"{FormatPointsNumber(result.EarnedPoints)} of " +
                    $"{FormatPointsNumber(result.PossiblePoints)} points";
            }

            if (_correctValue != null)
            {
                _correctValue.text = result.CorrectCount.ToString(CultureInfo.InvariantCulture);
            }

            if (_incorrectValue != null)
            {
                _incorrectValue.text = result.IncorrectCount.ToString(CultureInfo.InvariantCulture);
            }

            if (_unansweredValue != null)
            {
                _unansweredValue.text = result.UnansweredCount.ToString(CultureInfo.InvariantCulture);
            }

            if (_totalPointsValue != null)
            {
                _totalPointsValue.text =
                    $"{FormatPointsNumber(result.EarnedPoints)} of " +
                    $"{FormatPointsNumber(result.PossiblePoints)}";
            }

            BindFeedbackSection(detail, result);
        }

        private void BindFeedbackSection(
            QuizDetailPreviewContent detail,
            QuizResultPreviewContent result)
        {
            ClearFeedbackCards();

            if (_feedbackTitle != null)
            {
                _feedbackTitle.text = "Question feedback";
            }

            if (result.FeedbackVisible)
            {
                _feedbackMessage?.RemoveFromClassList(FeedbackMessageHiddenClass);
                _feedbackList?.RemoveFromClassList(FeedbackListHiddenClass);
                _feedbackHiddenNote?.AddToClassList(FeedbackHiddenNoteHiddenClass);

                if (_feedbackMessage != null)
                {
                    _feedbackMessage.text =
                        "Your teacher has made question-level feedback visible for this result.";
                }

                RebuildFeedbackCards(detail, result);
                return;
            }

            _feedbackMessage?.AddToClassList(FeedbackMessageHiddenClass);
            _feedbackList?.AddToClassList(FeedbackListHiddenClass);
            _feedbackHiddenNote?.RemoveFromClassList(FeedbackHiddenNoteHiddenClass);
        }

        private void RebuildFeedbackCards(
            QuizDetailPreviewContent detail,
            QuizResultPreviewContent result)
        {
            if (_feedbackList == null || detail == null || result == null)
            {
                return;
            }

            var answersByQuestionId = new Dictionary<string, QuizResultPreviewAnswer>(
                StringComparer.Ordinal);
            for (int i = 0; i < result.Answers.Count; i++)
            {
                QuizResultPreviewAnswer answer = result.Answers[i];
                answersByQuestionId[answer.QuestionId] = answer;
            }

            int cardIndex = 0;
            for (int i = 0; i < detail.QuestionCount; i++)
            {
                QuizDetailPreviewQuestion question = detail.Questions[i];
                if (!answersByQuestionId.TryGetValue(question.Id, out QuizResultPreviewAnswer answer))
                {
                    continue;
                }

                var card = new VisualElement();
                card.AddToClassList("ds-card");
                card.AddToClassList("quiz-result-panel__feedback-card");

                // Two cards per row at full width; the right-hand card drops its gutter.
                if (cardIndex % 2 == 1)
                {
                    card.AddToClassList("quiz-result-panel__feedback-card--row-end");
                }

                cardIndex++;

                var number = new Label($"Question {i + 1}")
                {
                    pickingMode = PickingMode.Ignore
                };
                number.AddToClassList("quiz-result-panel__feedback-card-number");
                card.Add(number);

                var prompt = new Label(question.Prompt)
                {
                    pickingMode = PickingMode.Ignore
                };
                prompt.AddToClassList("quiz-result-panel__feedback-card-prompt");
                card.Add(prompt);

                var statusRow = new VisualElement
                {
                    pickingMode = PickingMode.Ignore
                };
                statusRow.AddToClassList("quiz-result-panel__feedback-card-status-row");

                var statusIcon = new VisualElement
                {
                    pickingMode = PickingMode.Ignore
                };
                statusIcon.AddToClassList("ds-icon");
                statusIcon.AddToClassList(answer.Correct ? "ds-icon--check" : "ds-icon--close");
                statusIcon.AddToClassList("quiz-result-panel__feedback-card-status-icon");
                if (!answer.Correct)
                {
                    statusIcon.AddToClassList("quiz-result-panel__feedback-card-status-icon--incorrect");
                }

                statusRow.Add(statusIcon);

                var status = new Label(answer.Correct ? "Correct" : "Incorrect")
                {
                    pickingMode = PickingMode.Ignore
                };
                status.AddToClassList("quiz-result-panel__feedback-card-status");
                statusRow.Add(status);
                card.Add(statusRow);

                var points = new Label($"{FormatEarnedPointsLabel(answer.EarnedPoints)}")
                {
                    pickingMode = PickingMode.Ignore
                };
                points.AddToClassList("quiz-result-panel__feedback-card-points");
                card.Add(points);

                _feedbackList.Add(card);
            }
        }

        private void ClearFeedbackCards()
        {
            _feedbackList?.Clear();
        }

        private void ShowFixtureGapState(string quizTitle)
        {
            if (_dataStateView == null || !_dataStateView.IsBound)
            {
                return;
            }

            _isFixtureGap = true;
            _fixtureGapQuizTitle = string.IsNullOrWhiteSpace(quizTitle)
                ? "This assignment"
                : quizTitle;
            PreviewState = QuizResultPreviewState.RecoverableError;
            ShowDataStateHost();
            _dataStateView.SetState(DataStatePanelState.RecoverableError);
            _dataStateView.Configure(new DataStatePanelConfiguration(
                title: "Preview result is not available",
                message: "This assignment does not have a repository result fixture.",
                detail:
                    $"“{_fixtureGapQuizTitle}” — Only quiz_fixture_001 has canonical scored-result content. " +
                    "No score or feedback was invented.",
                iconClass: "ds-icon--error",
                primaryActionLabel: "Back to Quiz Portal",
                secondaryActionLabel: string.Empty,
                showSpinner: false));
        }

        private void ShowRecoverableStaticError(string title, string message)
        {
            if (_dataStateView == null || !_dataStateView.IsBound)
            {
                return;
            }

            _isFixtureGap = false;
            PreviewState = QuizResultPreviewState.RecoverableError;
            ShowDataStateHost();
            _dataStateView.SetState(DataStatePanelState.RecoverableError);
            _dataStateView.Configure(new DataStatePanelConfiguration(
                title: title,
                message: message,
                detail: "Your submitted attempt is not changed by this screen.",
                iconClass: "ds-icon--error",
                primaryActionLabel: "Try Again",
                secondaryActionLabel: "Back to Quiz Portal",
                showSpinner: false));
        }

        private void ApplyNonContentState(QuizResultPreviewState state)
        {
            switch (state)
            {
                case QuizResultPreviewState.Loading:
                    _isFixtureGap = false;
                    _dataStateView.SetState(DataStatePanelState.Loading);
                    _dataStateView.Configure(new DataStatePanelConfiguration(
                        title: "Loading quiz result",
                        message: "Getting your score and result details.",
                        detail: "Your result is scored by the NutriMind server.",
                        iconClass: null,
                        primaryActionLabel: string.Empty,
                        secondaryActionLabel: string.Empty,
                        showSpinner: true));
                    break;

                case QuizResultPreviewState.RecoverableError:
                    if (_isFixtureGap)
                    {
                        ShowFixtureGapState(_fixtureGapQuizTitle);
                        break;
                    }

                    _dataStateView.SetState(DataStatePanelState.RecoverableError);
                    _dataStateView.Configure(new DataStatePanelConfiguration(
                        title: "Quiz result could not be loaded",
                        message: "Check your connection and try again.",
                        detail: "Your submitted attempt is not changed by this screen.",
                        iconClass: "ds-icon--error",
                        primaryActionLabel: "Try Again",
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

        private void OnDataStatePrimaryAction()
        {
            if (PreviewState != QuizResultPreviewState.RecoverableError)
            {
                return;
            }

            if (_isFixtureGap)
            {
                BackToQuizPortalRequested?.Invoke();
                return;
            }

            RetryRequested?.Invoke();
        }

        private void OnDataStateSecondaryAction()
        {
            if (PreviewState == QuizResultPreviewState.RecoverableError && !_isFixtureGap)
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
        }

        private static string FormatPercentage(float percentage)
        {
            if (Mathf.Approximately(percentage, Mathf.Round(percentage)))
            {
                return $"{Mathf.RoundToInt(percentage)}%";
            }

            return percentage.ToString("0.##", CultureInfo.InvariantCulture) + "%";
        }

        private static string FormatPointsNumber(float points)
        {
            if (Mathf.Approximately(points, Mathf.Round(points)))
            {
                return Mathf.RoundToInt(points).ToString(CultureInfo.InvariantCulture);
            }

            return points.ToString("0.##", CultureInfo.InvariantCulture);
        }

        private static string FormatEarnedPointsLabel(float earnedPoints)
        {
            string amount = FormatPointsNumber(earnedPoints);
            return Mathf.Approximately(earnedPoints, 1f)
                ? $"{amount} point earned"
                : $"{amount} points earned";
        }

        private static string FormatSubmittedAt(DateTimeOffset submittedAtUtc)
        {
            DateTime utc = submittedAtUtc.UtcDateTime;
            return utc.ToString("MMM d, yyyy", CultureInfo.InvariantCulture)
                + " • "
                + utc.ToString("h:mm tt", CultureInfo.InvariantCulture)
                + " UTC";
        }

        private static string GetStatusLabel(QuizResultPreviewStatus status) =>
            status switch
            {
                QuizResultPreviewStatus.Submitted => "Submitted",
                QuizResultPreviewStatus.Scored => "Scored",
                QuizResultPreviewStatus.PendingVisibility => "Result pending",
                _ => "Result pending"
            };

        private static string GetPassLabel(bool? passed) =>
            passed switch
            {
                true => "Passed",
                false => "Not passed",
                null => "Result pending"
            };

        private static string GetSubjectLabel(NutriMindSubject subject) =>
            subject switch
            {
                NutriMindSubject.LiteraQuest => "LiteraQuest",
                NutriMindSubject.PeAndHealth => "PE & Health",
                NutriMindSubject.Science => "Science",
                _ => subject.ToString()
            };
    }
}
