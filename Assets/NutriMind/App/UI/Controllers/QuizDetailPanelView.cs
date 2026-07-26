using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace NutriMind.App.UI
{
    /// <summary>
    /// Presentation-only question type labels for Quiz Portal detail preview.
    /// Not a production DTO.
    /// </summary>
    public enum QuizDetailPreviewQuestionType
    {
        MultipleChoiceSingle,
        MultipleChoiceMultiple,
        TrueFalse
    }

    /// <summary>
    /// Immutable presentation-only option row for static Quiz Portal detail preview.
    /// Not a production DTO.
    /// </summary>
    public readonly struct QuizDetailPreviewOption
    {
        public QuizDetailPreviewOption(string id, string text)
        {
            Id = id;
            Text = text;
        }

        public string Id { get; }
        public string Text { get; }
    }

    /// <summary>
    /// Immutable presentation-only question for static Quiz Portal detail preview.
    /// Not a production DTO.
    /// </summary>
    public readonly struct QuizDetailPreviewQuestion
    {
        public QuizDetailPreviewQuestion(
            string id,
            QuizDetailPreviewQuestionType type,
            string prompt,
            IReadOnlyList<QuizDetailPreviewOption> options)
        {
            Id = id;
            Type = type;
            Prompt = prompt;
            Options = options ?? Array.Empty<QuizDetailPreviewOption>();
        }

        public string Id { get; }
        public QuizDetailPreviewQuestionType Type { get; }
        public string Prompt { get; }
        public IReadOnlyList<QuizDetailPreviewOption> Options { get; }
    }

    /// <summary>
    /// Presentation-only Quiz Portal detail content for static preview.
    /// Matches the canonical quiz-detail-success fixture shape. Not a production DTO.
    /// </summary>
    public sealed class QuizDetailPreviewContent
    {
        public QuizDetailPreviewContent(
            string quizId,
            string title,
            string instructions,
            IReadOnlyList<QuizDetailPreviewQuestion> questions)
        {
            QuizId = quizId;
            Title = title;
            Instructions = instructions;
            Questions = questions ?? Array.Empty<QuizDetailPreviewQuestion>();
        }

        public string QuizId { get; }
        public string Title { get; }
        public string Instructions { get; }
        public IReadOnlyList<QuizDetailPreviewQuestion> Questions { get; }
        public int QuestionCount => Questions?.Count ?? 0;
    }

    /// <summary>
    /// Presentation-only Start/View Result intent payload for static Quiz Portal detail preview.
    /// Not a production DTO or attempt model.
    /// </summary>
    public readonly struct QuizDetailPreviewSelection
    {
        public QuizDetailPreviewSelection(QuizListPreviewItem summary, int questionCount)
        {
            Summary = summary;
            QuestionCount = questionCount;
        }

        public QuizListPreviewItem Summary { get; }
        public int QuestionCount { get; }
    }

    /// <summary>
    /// Deterministic static-preview catalog for Quiz Portal detail fixtures.
    /// Returns canonical detail only for quiz_fixture_001. Not a production repository.
    /// </summary>
    public static class QuizDetailPreviewCatalog
    {
        public const string CanonicalQuizId = "quiz_fixture_001";

        /// <summary>
        /// Creates the exact canonical QuizList summary from quiz-list-success.json.
        /// Presentation-only static preview fixture.
        /// </summary>
        public static QuizListPreviewItem CreateCanonicalSummary() =>
            new(
                CanonicalQuizId,
                "Story Elements Check",
                NutriMindSubject.LiteraQuest,
                NutriMindTerm.Term1,
                QuizListPreviewStatus.Available,
                null,
                null,
                null,
                1,
                0,
                QuizListPreviewResultVisibility.Immediate);

        /// <summary>
        /// Returns true only when a repository quiz-detail fixture exists for the quiz id.
        /// Does not invent questions for noncanonical preview-only list ids.
        /// </summary>
        public static bool TryGetDetail(string quizId, out QuizDetailPreviewContent content)
        {
            if (string.Equals(quizId, CanonicalQuizId, StringComparison.Ordinal))
            {
                content = CreateCanonicalDetail();
                return true;
            }

            content = null;
            return false;
        }

        private static QuizDetailPreviewContent CreateCanonicalDetail()
        {
            var question1Options = new QuizDetailPreviewOption[]
            {
                new("opt_a", "Farmer Lira"),
                new("opt_b", "The bridge")
            };

            var question2Options = new QuizDetailPreviewOption[]
            {
                new("true", "True"),
                new("false", "False")
            };

            var questions = new QuizDetailPreviewQuestion[]
            {
                new(
                    "qq_001",
                    QuizDetailPreviewQuestionType.MultipleChoiceSingle,
                    "Who is the main character?",
                    question1Options),
                new(
                    "qq_002",
                    QuizDetailPreviewQuestionType.TrueFalse,
                    "The story happens during a festival.",
                    question2Options)
            };

            return new QuizDetailPreviewContent(
                CanonicalQuizId,
                "Story Elements Check",
                "Choose the best answer.",
                questions);
        }
    }

    /// <summary>
    /// Presentation-only Quiz Portal detail view. Binds deterministic preview fixtures,
    /// reuses shared <see cref="DataStatePanelView"/> for non-content states, and raises
    /// typed user intent for the host. Does not call APIs, score attempts, or store answers.
    /// </summary>
    public sealed class QuizDetailPanelView : IAppScreenView
    {
        private const string RootName = "quiz-detail-root";
        private const string CompactClass = "quiz-detail-panel--compact";
        private const string NarrowClass = "quiz-detail-panel--narrow";
        private const string MobileClass = "mobile";
        private const string DataStateHostVisibleClass = "quiz-detail-panel__data-state-host--visible";
        private const string QuestionHiddenClass = "quiz-detail-panel__question-card--hidden";
        private const float CompactBreakpoint = 1100f;
        private const float NarrowBreakpoint = 820f;

        private static readonly string[] SubjectIconClasses =
        {
            "ds-icon--book",
            "ds-icon--bolt",
            "ds-icon--potion"
        };

        private static readonly string[] StatusIconClasses =
        {
            "ds-icon--play",
            "ds-icon--check",
            "ds-icon--lock",
            "ds-icon--warning"
        };

        private static readonly string[] StatusModifierClasses =
        {
            "quiz-detail-panel__status--available",
            "quiz-detail-panel__status--completed",
            "quiz-detail-panel__status--locked",
            "quiz-detail-panel__status--unavailable"
        };

        private static readonly string[] ActionButtonVariantClasses =
        {
            "ds-btn--primary",
            "ds-btn--secondary",
            "ds-btn--ghost"
        };

        private static readonly string[] ActionIconClasses =
        {
            "ds-icon--play",
            "ds-icon--eye"
        };

        private VisualElement _root;
        private ScrollView _scroll;
        private VisualElement _dataStateHost;
        private Button _backButton;
        private VisualElement _subjectIcon;
        private Label _subjectTerm;
        private VisualElement _statusChip;
        private VisualElement _statusIcon;
        private Label _statusLabel;
        private Label _title;
        private Label _instructions;
        private Label _questionCount;
        private Label _maxAttempts;
        private Label _attemptsUsed;
        private Label _attemptsRemaining;
        private Label _availability;
        private Label _resultVisibility;
        private Button _startButton;
        private VisualElement _startIcon;
        private Label _startLabel;
        private readonly QuestionShellElements[] _questionShells = new QuestionShellElements[2];

        private TemplateContainer _ownedDataStateInstance;
        private DataStatePanelView _dataStateView;
        private bool _warnedMissingDataStateAsset;
        private bool _warnedQuestionShellOverflow;
        private bool _disposed;
        private float _lastWidth = -1f;
        private DataStatePanelState _requestedDataState = DataStatePanelState.Content;
        private bool _hasSummary;

        public QuizDetailPanelView(
            VisualElement root,
            VisualTreeAsset dataStatePanelAsset = null)
        {
            ResolveRoot(root);
            if (_root == null)
            {
                Debug.LogWarning(
                    "[QuizDetailPanelView] Could not resolve quiz-detail-root inside the supplied element.");
                return;
            }

            CacheElements();
            BindDataStatePanel(dataStatePanelAsset);
            RegisterCallbacks();
            ApplyResponsiveClasses(_root.resolvedStyle.width);
            SetQuizContext(QuizDetailPreviewCatalog.CreateCanonicalSummary());
            SetDataState(DataStatePanelState.Content);
        }

        public VisualElement Root => _root;

        public bool IsBound => _root != null && !_disposed;

        public DataStatePanelState DataState { get; private set; } = DataStatePanelState.Content;

        public QuizListPreviewItem SelectedSummary { get; private set; }

        public QuizDetailPreviewContent DetailContent { get; private set; }

        public event Action BackRequested;
        public event Action<QuizDetailPreviewSelection> StartRequested;
        public event Action<QuizDetailPreviewSelection> ViewResultRequested;
        public event Action RetryRequested;

        /// <summary>
        /// Applies selected QuizList summary context for static preview resolution.
        /// Does not invent detail questions for noncanonical preview ids.
        /// </summary>
        public void SetQuizContext(QuizListPreviewItem summary)
        {
            if (!IsBound)
            {
                return;
            }

            SelectedSummary = summary;
            _hasSummary = true;
            BindSummaryVisuals(summary);

            if (_requestedDataState == DataStatePanelState.Content)
            {
                ResolveContentOrFallbackState();
            }
        }

        public void SetDataState(DataStatePanelState state)
        {
            if (!IsBound)
            {
                return;
            }

            if (state != DataStatePanelState.Content
                && (_dataStateView == null || !_dataStateView.IsBound))
            {
                return;
            }

            _requestedDataState = state;

            if (state == DataStatePanelState.Content)
            {
                ResolveContentOrFallbackState();
                return;
            }

            ApplyHostRequestedNonContentState(state);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            UnregisterCallbacks();
            DisposeOwnedDataState();

            BackRequested = null;
            StartRequested = null;
            ViewResultRequested = null;
            RetryRequested = null;

            _root = null;
            _scroll = null;
            _dataStateHost = null;
            _backButton = null;
            _subjectIcon = null;
            _subjectTerm = null;
            _statusChip = null;
            _statusIcon = null;
            _statusLabel = null;
            _title = null;
            _instructions = null;
            _questionCount = null;
            _maxAttempts = null;
            _attemptsUsed = null;
            _attemptsRemaining = null;
            _availability = null;
            _resultVisibility = null;
            _startButton = null;
            _startIcon = null;
            _startLabel = null;
            for (int i = 0; i < _questionShells.Length; i++)
            {
                _questionShells[i] = default;
            }

            DetailContent = null;
            _lastWidth = -1f;
            _hasSummary = false;
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
            _scroll = _root.Q<ScrollView>("quiz-detail-scroll");
            _dataStateHost = _root.Q<VisualElement>("quiz-detail-data-state-host");
            _backButton = _root.Q<Button>("quiz-detail-back-button");
            _subjectIcon = _root.Q<VisualElement>("quiz-detail-subject-icon");
            _subjectTerm = _root.Q<Label>("quiz-detail-subject-term");
            _statusChip = _root.Q<VisualElement>("quiz-detail-status");
            _statusIcon = _root.Q<VisualElement>("quiz-detail-status-icon");
            _statusLabel = _root.Q<Label>("quiz-detail-status-label");
            _title = _root.Q<Label>("quiz-detail-title");
            _instructions = _root.Q<Label>("quiz-detail-instructions");
            _questionCount = _root.Q<Label>("quiz-detail-question-count");
            _maxAttempts = _root.Q<Label>("quiz-detail-max-attempts");
            _attemptsUsed = _root.Q<Label>("quiz-detail-attempts-used");
            _attemptsRemaining = _root.Q<Label>("quiz-detail-attempts-remaining");
            _availability = _root.Q<Label>("quiz-detail-availability");
            _resultVisibility = _root.Q<Label>("quiz-detail-result-visibility");
            _startButton = _root.Q<Button>("quiz-detail-start-button");
            _startIcon = _root.Q<VisualElement>("quiz-detail-start-icon");
            _startLabel = _root.Q<Label>("quiz-detail-start-label");

            for (int i = 0; i < _questionShells.Length; i++)
            {
                int index = i + 1;
                _questionShells[i] = new QuestionShellElements(
                    _root.Q<VisualElement>($"quiz-detail-question-{index}"),
                    _root.Q<Label>($"quiz-detail-question-{index}-number"),
                    _root.Q<Label>($"quiz-detail-question-{index}-type"),
                    _root.Q<Label>($"quiz-detail-question-{index}-prompt"),
                    _root.Q<Label>($"quiz-detail-question-{index}-option-1"),
                    _root.Q<Label>($"quiz-detail-question-{index}-option-2"));
            }
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
                        "[QuizDetailPanelView] DataStatePanel VisualTreeAsset is missing. " +
                        "Content preview remains usable; non-Content SetDataState calls are no-ops.");
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
                    "[QuizDetailPanelView] Failed to bind nested DataStatePanelView.");
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
            _backButton?.RegisterCallback<ClickEvent>(OnBackClicked);
            _startButton?.RegisterCallback<ClickEvent>(OnStartClicked);
            if (_dataStateView != null && _dataStateView.IsBound)
            {
                _dataStateView.PrimaryActionRequested += OnDataStatePrimaryAction;
                _dataStateView.SecondaryActionRequested += OnDataStateSecondaryAction;
            }

            _root?.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        private void UnregisterCallbacks()
        {
            _backButton?.UnregisterCallback<ClickEvent>(OnBackClicked);
            _startButton?.UnregisterCallback<ClickEvent>(OnStartClicked);
            if (_dataStateView != null)
            {
                _dataStateView.PrimaryActionRequested -= OnDataStatePrimaryAction;
                _dataStateView.SecondaryActionRequested -= OnDataStateSecondaryAction;
            }

            _root?.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        private void ResolveContentOrFallbackState()
        {
            if (!_hasSummary)
            {
                SelectedSummary = QuizDetailPreviewCatalog.CreateCanonicalSummary();
                _hasSummary = true;
                BindSummaryVisuals(SelectedSummary);
            }

            QuizListPreviewItem summary = SelectedSummary;

            if (summary.Status == QuizListPreviewStatus.Locked
                || summary.Status == QuizListPreviewStatus.Unavailable)
            {
                DetailContent = null;
                ShowLockedUnavailableState(summary);
                return;
            }

            if (summary.Status == QuizListPreviewStatus.Available
                && summary.AttemptsUsed >= summary.MaxAttempts)
            {
                DetailContent = null;
                ShowNoAttemptsState(summary);
                return;
            }

            if (!QuizDetailPreviewCatalog.TryGetDetail(summary.Id, out QuizDetailPreviewContent detail))
            {
                DetailContent = null;
                ShowFixtureGapState(summary);
                return;
            }

            DetailContent = detail;
            BindDetailContent(detail);
            BindActionVisuals(summary);
            ShowContent();
            DataState = DataStatePanelState.Content;
            _dataStateView?.SetState(DataStatePanelState.Content);
        }

        private void ApplyHostRequestedNonContentState(DataStatePanelState state)
        {
            DetailContent = null;
            HideContent();
            DataState = state;
            _dataStateView.SetState(state);
            ApplyQuizDetailDataStateCopy(state);
        }

        private void ShowLockedUnavailableState(QuizListPreviewItem summary)
        {
            HideContent();
            DataState = DataStatePanelState.PermissionOrLocked;
            if (_dataStateView == null || !_dataStateView.IsBound)
            {
                return;
            }

            _dataStateView.SetState(DataStatePanelState.PermissionOrLocked);
            string title = string.IsNullOrWhiteSpace(summary.Title)
                ? "This quiz is not available"
                : $"{summary.Title} is not available";
            string message = !string.IsNullOrWhiteSpace(summary.LockedReason)
                ? summary.LockedReason
                : summary.Status == QuizListPreviewStatus.Locked
                    ? "This quiz is locked for your classroom."
                    : "This quiz is no longer available.";

            if (summary.Status == QuizListPreviewStatus.Locked
                && summary.OpensAtUtc.HasValue
                && string.IsNullOrWhiteSpace(summary.LockedReason))
            {
                message = $"This quiz opens {FormatDate(summary.OpensAtUtc.Value)}.";
            }

            _dataStateView.Configure(
                title: title,
                message: message,
                detail: "Return to Quiz Portal to choose another assignment.",
                iconClass: "ds-icon--lock",
                primaryActionLabel: "Back to Quiz Portal",
                secondaryActionLabel: string.Empty);
        }

        private void ShowNoAttemptsState(QuizListPreviewItem summary)
        {
            HideContent();
            DataState = DataStatePanelState.PermissionOrLocked;
            if (_dataStateView == null || !_dataStateView.IsBound)
            {
                return;
            }

            _dataStateView.SetState(DataStatePanelState.PermissionOrLocked);
            string title = string.IsNullOrWhiteSpace(summary.Title)
                ? "No attempts remaining"
                : $"{summary.Title} — no attempts remaining";

            _dataStateView.Configure(
                title: title,
                message: "You have used all allowed attempts for this quiz.",
                detail: "Return to Quiz Portal to choose another assignment.",
                iconClass: "ds-icon--lock",
                primaryActionLabel: "Back to Quiz Portal",
                secondaryActionLabel: string.Empty);
        }

        private void ShowFixtureGapState(QuizListPreviewItem summary)
        {
            HideContent();
            DataState = DataStatePanelState.Empty;
            if (_dataStateView == null || !_dataStateView.IsBound)
            {
                return;
            }

            _dataStateView.SetState(DataStatePanelState.Empty);
            _dataStateView.Configure(
                title: "Preview detail is not available",
                message: "This preview assignment does not have a repository quiz-detail fixture.",
                detail:
                    "Only quiz_fixture_001 has canonical question content. No questions were invented.",
                iconClass: "ds-icon--info",
                primaryActionLabel: "Back to Quiz Portal",
                secondaryActionLabel: string.Empty);
        }

        private void BindSummaryVisuals(QuizListPreviewItem summary)
        {
            if (_subjectTerm != null)
            {
                _subjectTerm.text =
                    $"{GetSubjectLabel(summary.Subject)} • Term {(int)summary.Term}";
            }

            ClearAndApplySubjectIcon(_subjectIcon, summary.Subject);
            ApplyStatusVisuals(summary.Status);

            if (_title != null)
            {
                _title.text = summary.Title ?? string.Empty;
            }

            int remaining = Mathf.Max(0, summary.MaxAttempts - summary.AttemptsUsed);
            if (_maxAttempts != null)
            {
                _maxAttempts.text = summary.MaxAttempts == 1
                    ? "1 allowed"
                    : $"{summary.MaxAttempts} allowed";
            }

            if (_attemptsUsed != null)
            {
                _attemptsUsed.text = summary.AttemptsUsed.ToString(CultureInfo.InvariantCulture);
            }

            if (_attemptsRemaining != null)
            {
                _attemptsRemaining.text = remaining.ToString(CultureInfo.InvariantCulture);
            }

            if (_availability != null)
            {
                _availability.text = BuildAvailabilityCopy(summary);
            }

            if (_resultVisibility != null)
            {
                _resultVisibility.text = BuildResultVisibilityShortCopy(summary.ResultVisibility);
            }

            BindActionVisuals(summary);
        }

        private void BindDetailContent(QuizDetailPreviewContent detail)
        {
            if (_title != null)
            {
                _title.text = detail.Title ?? string.Empty;
            }

            if (_instructions != null)
            {
                _instructions.text = detail.Instructions ?? string.Empty;
            }

            if (_questionCount != null)
            {
                _questionCount.text = detail.QuestionCount.ToString(CultureInfo.InvariantCulture);
            }

            if (detail.QuestionCount > _questionShells.Length && !_warnedQuestionShellOverflow)
            {
                Debug.LogWarning(
                    "[QuizDetailPanelView] Detail fixture has more questions than available " +
                    "UXML shells. Extra questions are not shown in this static preview.");
                _warnedQuestionShellOverflow = true;
            }

            for (int i = 0; i < _questionShells.Length; i++)
            {
                QuestionShellElements shell = _questionShells[i];
                if (shell.Card == null)
                {
                    continue;
                }

                if (i >= detail.QuestionCount)
                {
                    shell.Card.AddToClassList(QuestionHiddenClass);
                    continue;
                }

                shell.Card.RemoveFromClassList(QuestionHiddenClass);
                QuizDetailPreviewQuestion question = detail.Questions[i];
                if (shell.Number != null)
                {
                    shell.Number.text = $"Question {i + 1}";
                }

                if (shell.Type != null)
                {
                    shell.Type.text = GetQuestionTypeLabel(question.Type);
                }

                if (shell.Prompt != null)
                {
                    shell.Prompt.text = question.Prompt ?? string.Empty;
                }

                BindOptionLabel(shell.Option1, question.Options, 0);
                BindOptionLabel(shell.Option2, question.Options, 1);
            }
        }

        private static void BindOptionLabel(
            Label label,
            IReadOnlyList<QuizDetailPreviewOption> options,
            int index)
        {
            if (label == null)
            {
                return;
            }

            if (options == null || index < 0 || index >= options.Count)
            {
                label.text = string.Empty;
                label.style.display = DisplayStyle.None;
                return;
            }

            label.style.display = DisplayStyle.Flex;
            label.text = options[index].Text ?? string.Empty;
        }

        private void BindActionVisuals(QuizListPreviewItem summary)
        {
            if (_startButton == null)
            {
                return;
            }

            bool completed = summary.Status == QuizListPreviewStatus.Completed;
            bool canStart = summary.Status == QuizListPreviewStatus.Available
                && summary.AttemptsUsed < summary.MaxAttempts;

            foreach (string variant in ActionButtonVariantClasses)
            {
                _startButton.RemoveFromClassList(variant);
            }

            if (completed)
            {
                _startButton.AddToClassList("ds-btn--secondary");
                _startButton.SetEnabled(true);
                if (_startLabel != null)
                {
                    _startLabel.text = "View Result";
                }

                ClearAndApplyActionIcon(_startIcon, "ds-icon--eye");
                _startButton.tooltip = "View Result";
                return;
            }

            _startButton.AddToClassList("ds-btn--primary");
            _startButton.SetEnabled(canStart);
            if (_startLabel != null)
            {
                _startLabel.text = "Start Quiz";
            }

            ClearAndApplyActionIcon(_startIcon, "ds-icon--play");
            _startButton.tooltip = "Start Quiz";
        }

        private void ApplyStatusVisuals(QuizListPreviewStatus status)
        {
            if (_statusChip != null)
            {
                foreach (string modifier in StatusModifierClasses)
                {
                    _statusChip.RemoveFromClassList(modifier);
                }

                _statusChip.AddToClassList(GetStatusModifierClass(status));
            }

            if (_statusLabel != null)
            {
                _statusLabel.text = GetStatusLabel(status);
            }

            ClearAndApplyStatusIcon(_statusIcon, status);
        }

        private void ShowContent()
        {
            if (_scroll != null)
            {
                _scroll.style.display = DisplayStyle.Flex;
            }

            _dataStateHost?.RemoveFromClassList(DataStateHostVisibleClass);
        }

        private void HideContent()
        {
            if (_scroll != null)
            {
                _scroll.style.display = DisplayStyle.None;
            }

            _dataStateHost?.AddToClassList(DataStateHostVisibleClass);
        }

        private void ApplyQuizDetailDataStateCopy(DataStatePanelState state)
        {
            if (_dataStateView == null || !_dataStateView.IsBound)
            {
                return;
            }

            switch (state)
            {
                case DataStatePanelState.Loading:
                    _dataStateView.Configure(
                        title: "Loading quiz details",
                        message: "Getting the latest questions and attempt rules.",
                        detail: string.Empty,
                        iconClass: "ds-icon--book",
                        primaryActionLabel: string.Empty,
                        secondaryActionLabel: string.Empty);
                    break;

                case DataStatePanelState.Empty:
                    _dataStateView.Configure(
                        title: "Preview detail is not available",
                        message: "This preview assignment does not have a repository quiz-detail fixture.",
                        detail:
                            "Only quiz_fixture_001 has canonical question content. No questions were invented.",
                        iconClass: "ds-icon--info",
                        primaryActionLabel: "Back to Quiz Portal",
                        secondaryActionLabel: string.Empty);
                    break;

                case DataStatePanelState.OfflineUnavailable:
                    _dataStateView.Configure(
                        title: "Connection required",
                        message: "Quiz details and attempt availability must be confirmed online.",
                        detail: "Reconnect before starting this quiz.",
                        iconClass: "ds-icon--wifi",
                        primaryActionLabel: "Retry Connection",
                        secondaryActionLabel: "Back to Quiz Portal");
                    break;

                case DataStatePanelState.RecoverableError:
                    _dataStateView.Configure(
                        title: "Quiz details could not be loaded",
                        message: "Check your connection and try again.",
                        detail: "No attempt was started.",
                        iconClass: "ds-icon--error",
                        primaryActionLabel: "Try Again",
                        secondaryActionLabel: "Back to Quiz Portal");
                    break;

                case DataStatePanelState.PermissionOrLocked:
                    string lockedTitle = _hasSummary && !string.IsNullOrWhiteSpace(SelectedSummary.Title)
                        ? $"{SelectedSummary.Title} is not available"
                        : "This quiz is not available";
                    string lockedMessage = _hasSummary && !string.IsNullOrWhiteSpace(SelectedSummary.LockedReason)
                        ? SelectedSummary.LockedReason
                        : "Return to Quiz Portal to choose another assignment.";
                    _dataStateView.Configure(
                        title: lockedTitle,
                        message: lockedMessage,
                        detail: "Return to Quiz Portal to choose another assignment.",
                        iconClass: "ds-icon--lock",
                        primaryActionLabel: "Back to Quiz Portal",
                        secondaryActionLabel: string.Empty);
                    break;

                case DataStatePanelState.OfflineCached:
                    _dataStateView.Configure(
                        title: "Saved quiz information cannot start an attempt",
                        message: "Reconnect to confirm current questions and availability.",
                        detail: "Quiz details are not playable from offline cache in this preview.",
                        iconClass: "ds-icon--wifi",
                        primaryActionLabel: "Retry Connection",
                        secondaryActionLabel: "Back to Quiz Portal");
                    break;
            }
        }

        private void OnBackClicked(ClickEvent evt) => BackRequested?.Invoke();

        private void OnStartClicked(ClickEvent evt)
        {
            if (DataState != DataStatePanelState.Content || !_hasSummary)
            {
                Debug.LogWarning(
                    "[QuizDetailPanelView] Ignoring Start/View Result click outside content state.");
                return;
            }

            QuizListPreviewItem summary = SelectedSummary;
            var selection = new QuizDetailPreviewSelection(
                summary,
                DetailContent?.QuestionCount ?? 0);

            if (summary.Status == QuizListPreviewStatus.Completed)
            {
                ViewResultRequested?.Invoke(selection);
                return;
            }

            bool canStart = summary.Status == QuizListPreviewStatus.Available
                && DetailContent != null
                && summary.AttemptsUsed < summary.MaxAttempts;

            if (!canStart)
            {
                Debug.LogWarning(
                    "[QuizDetailPanelView] Ignoring Start click — Start is unavailable for this context.");
                return;
            }

            StartRequested?.Invoke(selection);
        }

        private void OnDataStatePrimaryAction()
        {
            switch (DataState)
            {
                case DataStatePanelState.Empty:
                case DataStatePanelState.PermissionOrLocked:
                    BackRequested?.Invoke();
                    break;

                case DataStatePanelState.OfflineUnavailable:
                case DataStatePanelState.RecoverableError:
                case DataStatePanelState.OfflineCached:
                    RetryRequested?.Invoke();
                    break;
            }
        }

        private void OnDataStateSecondaryAction()
        {
            switch (DataState)
            {
                case DataStatePanelState.OfflineUnavailable:
                case DataStatePanelState.RecoverableError:
                case DataStatePanelState.OfflineCached:
                    BackRequested?.Invoke();
                    break;
            }
        }

        private void OnGeometryChanged(GeometryChangedEvent evt) =>
            ApplyResponsiveClasses(evt.newRect.width);

        private void ApplyResponsiveClasses(float width)
        {
            if (_root == null || float.IsNaN(width) || width <= 0f || Mathf.Approximately(width, _lastWidth))
            {
                return;
            }

            _lastWidth = width;
            bool compact = width < CompactBreakpoint;
            bool narrow = width < NarrowBreakpoint;
            _root.EnableInClassList(CompactClass, compact);
            _root.EnableInClassList(NarrowClass, narrow);
            _root.EnableInClassList(MobileClass, narrow);
        }

        private static string BuildAvailabilityCopy(QuizListPreviewItem item)
        {
            switch (item.Status)
            {
                case QuizListPreviewStatus.Completed:
                    return "Completed";

                case QuizListPreviewStatus.Unavailable:
                    return item.ClosesAtUtc.HasValue
                        ? $"Closed {FormatDate(item.ClosesAtUtc.Value)}"
                        : "Unavailable";

                case QuizListPreviewStatus.Locked:
                    if (item.OpensAtUtc.HasValue)
                    {
                        return $"Opens {FormatDate(item.OpensAtUtc.Value)}";
                    }

                    return "Locked";

                default:
                    if (item.ClosesAtUtc.HasValue)
                    {
                        return $"Closes {FormatDate(item.ClosesAtUtc.Value)}";
                    }

                    return "Available now";
            }
        }

        private static string BuildResultVisibilityShortCopy(
            QuizListPreviewResultVisibility visibility) =>
            visibility switch
            {
                QuizListPreviewResultVisibility.Immediate => "After submission",
                QuizListPreviewResultVisibility.AfterClose => "After the quiz closes",
                QuizListPreviewResultVisibility.TeacherRelease => "After Teacher release",
                _ => "Not currently visible"
            };

        private static string GetStatusLabel(QuizListPreviewStatus status) =>
            status switch
            {
                QuizListPreviewStatus.Available => "Available",
                QuizListPreviewStatus.Completed => "Completed",
                QuizListPreviewStatus.Locked => "Locked",
                _ => "Unavailable"
            };

        private static string GetStatusModifierClass(QuizListPreviewStatus status) =>
            status switch
            {
                QuizListPreviewStatus.Available => "quiz-detail-panel__status--available",
                QuizListPreviewStatus.Completed => "quiz-detail-panel__status--completed",
                QuizListPreviewStatus.Locked => "quiz-detail-panel__status--locked",
                _ => "quiz-detail-panel__status--unavailable"
            };

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
                _ => "Science"
            };

        private static string GetSubjectIconClass(NutriMindSubject subject) =>
            subject switch
            {
                NutriMindSubject.LiteraQuest => "ds-icon--book",
                NutriMindSubject.PeAndHealth => "ds-icon--bolt",
                _ => "ds-icon--potion"
            };

        private static string GetStatusIconClass(QuizListPreviewStatus status) =>
            status switch
            {
                QuizListPreviewStatus.Available => "ds-icon--play",
                QuizListPreviewStatus.Completed => "ds-icon--check",
                QuizListPreviewStatus.Locked => "ds-icon--lock",
                _ => "ds-icon--warning"
            };

        private static string FormatDate(DateTimeOffset value) =>
            value.UtcDateTime.ToString("MMM d, yyyy", CultureInfo.InvariantCulture);

        private static void ClearAndApplySubjectIcon(VisualElement icon, NutriMindSubject subject)
        {
            if (icon == null)
            {
                return;
            }

            foreach (string iconClass in SubjectIconClasses)
            {
                icon.RemoveFromClassList(iconClass);
            }

            icon.AddToClassList(GetSubjectIconClass(subject));
        }

        private static void ClearAndApplyStatusIcon(
            VisualElement icon,
            QuizListPreviewStatus status)
        {
            if (icon == null)
            {
                return;
            }

            foreach (string iconClass in StatusIconClasses)
            {
                icon.RemoveFromClassList(iconClass);
            }

            icon.AddToClassList(GetStatusIconClass(status));
        }

        private static void ClearAndApplyActionIcon(VisualElement icon, string iconClass)
        {
            if (icon == null)
            {
                return;
            }

            foreach (string existing in ActionIconClasses)
            {
                icon.RemoveFromClassList(existing);
            }

            icon.AddToClassList(iconClass);
        }

        private readonly struct QuestionShellElements
        {
            public QuestionShellElements(
                VisualElement card,
                Label number,
                Label type,
                Label prompt,
                Label option1,
                Label option2)
            {
                Card = card;
                Number = number;
                Type = type;
                Prompt = prompt;
                Option1 = option1;
                Option2 = option2;
            }

            public VisualElement Card { get; }
            public Label Number { get; }
            public Label Type { get; }
            public Label Prompt { get; }
            public Label Option1 { get; }
            public Label Option2 { get; }
        }
    }
}
