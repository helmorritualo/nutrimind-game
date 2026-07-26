using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace NutriMind.App.UI
{
    /// <summary>
    /// Presentation-only Quiz Portal list status values.
    /// Maps from canonical QuizSummary.status; not a production DTO.
    /// </summary>
    public enum QuizListPreviewStatus
    {
        Locked,
        Available,
        Completed,
        Unavailable
    }

    /// <summary>
    /// Presentation-only result-visibility values for Quiz Portal list rows.
    /// Maps from canonical QuizSummary.result_visibility; not a production DTO.
    /// </summary>
    public enum QuizListPreviewResultVisibility
    {
        Immediate,
        AfterClose,
        TeacherRelease,
        Hidden
    }

    /// <summary>
    /// Presentation-only status filter choices for the Quiz Portal list preview.
    /// </summary>
    public enum QuizListPreviewStatusFilter
    {
        All,
        Available,
        Completed,
        Locked,
        Unavailable
    }

    /// <summary>
    /// Presentation-only subject filter choices for the Quiz Portal list preview.
    /// </summary>
    public enum QuizListPreviewSubjectFilter
    {
        All,
        LiteraQuest,
        PeAndHealth,
        Science
    }

    /// <summary>
    /// Presentation-only term filter choices for the Quiz Portal list preview.
    /// </summary>
    public enum QuizListPreviewTermFilter
    {
        All,
        Term1,
        Term2,
        Term3
    }

    /// <summary>
    /// Immutable presentation fixture for one Quiz Portal list row.
    /// Represents only canonical QuizSummary fields for static preview.
    /// Not a production DTO, domain entity, or attempt model.
    /// </summary>
    public readonly struct QuizListPreviewItem
    {
        public QuizListPreviewItem(
            string id,
            string title,
            NutriMindSubject subject,
            NutriMindTerm term,
            QuizListPreviewStatus status,
            string lockedReason,
            DateTimeOffset? opensAtUtc,
            DateTimeOffset? closesAtUtc,
            int maxAttempts,
            int attemptsUsed,
            QuizListPreviewResultVisibility resultVisibility)
        {
            Id = id;
            Title = title;
            Subject = subject;
            Term = term;
            Status = status;
            LockedReason = lockedReason;
            OpensAtUtc = opensAtUtc;
            ClosesAtUtc = closesAtUtc;
            MaxAttempts = maxAttempts;
            AttemptsUsed = attemptsUsed;
            ResultVisibility = resultVisibility;
        }

        public string Id { get; }
        public string Title { get; }
        public NutriMindSubject Subject { get; }
        public NutriMindTerm Term { get; }
        public QuizListPreviewStatus Status { get; }
        public string LockedReason { get; }
        public DateTimeOffset? OpensAtUtc { get; }
        public DateTimeOffset? ClosesAtUtc { get; }
        public int MaxAttempts { get; }
        public int AttemptsUsed { get; }
        public QuizListPreviewResultVisibility ResultVisibility { get; }
    }

    /// <summary>
    /// Immutable presentation filter payload for the Quiz Portal list preview.
    /// Not a production query DTO.
    /// </summary>
    public readonly struct QuizListPreviewFilters
    {
        public QuizListPreviewFilters(
            QuizListPreviewSubjectFilter subject,
            QuizListPreviewTermFilter term,
            QuizListPreviewStatusFilter status)
        {
            Subject = subject;
            Term = term;
            Status = status;
        }

        public QuizListPreviewSubjectFilter Subject { get; }
        public QuizListPreviewTermFilter Term { get; }
        public QuizListPreviewStatusFilter Status { get; }
    }

    /// <summary>
    /// Presentation-only Quiz Portal list view. Binds deterministic preview fixtures,
    /// reuses shared <see cref="DataStatePanelView"/> for non-content states, and raises
    /// typed user intent for the host. Does not call APIs, score attempts, or load answers.
    /// </summary>
    public sealed class QuizListPanelView : IAppScreenView
    {
        private const string RootName = "quiz-list-root";
        private const string CompactClass = "quiz-list-panel--compact";
        private const string NarrowClass = "quiz-list-panel--narrow";
        private const string MobileClass = "mobile";
        private const string DataStateHostVisibleClass = "quiz-list-panel__data-state-host--visible";
        private const string ItemHiddenClass = "quiz-list-panel__item--hidden";
        private const string FilterEmptyHiddenClass = "quiz-list-panel__filter-empty--hidden";
        private const string PaginationHiddenClass = "quiz-list-panel__pagination--hidden";
        private const string LockReasonHiddenClass = "quiz-list-panel__lock-reason--hidden";
        private const string ItemStatusPrefix = "quiz-list-panel__item--";
        private const string StatusBadgePrefix = "quiz-list-panel__status--";
        private const float CompactBreakpoint = 1100f;
        private const float NarrowBreakpoint = 820f;
        private const int ClosingSoonHours = 72;

        private static readonly DateTimeOffset PreviewReferenceTimeUtc =
            new(2026, 7, 19, 5, 0, 0, TimeSpan.Zero);

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
            "ds-icon--warning",
            "ds-icon--clock",
            "ds-icon--calendar"
        };

        private static readonly string[] ItemModifierClasses =
        {
            "quiz-list-panel__item--available",
            "quiz-list-panel__item--completed",
            "quiz-list-panel__item--locked",
            "quiz-list-panel__item--unavailable",
            "quiz-list-panel__item--closing-soon"
        };

        private static readonly string[] StatusBadgeClasses =
        {
            "quiz-list-panel__status--available",
            "quiz-list-panel__status--completed",
            "quiz-list-panel__status--locked",
            "quiz-list-panel__status--unavailable"
        };

        private static readonly string[] ActionButtonVariantClasses =
        {
            "ds-btn--primary",
            "ds-btn--secondary",
            "ds-btn--ghost"
        };

        private static readonly QuizListPreviewItem[] PreviewFixtures =
        {
            new(
                "quiz_fixture_001",
                "Story Elements Check",
                NutriMindSubject.LiteraQuest,
                NutriMindTerm.Term1,
                QuizListPreviewStatus.Available,
                null,
                null,
                null,
                1,
                0,
                QuizListPreviewResultVisibility.Immediate),
            new(
                "quiz_preview_002",
                "Healthy Choices Check",
                NutriMindSubject.PeAndHealth,
                NutriMindTerm.Term1,
                QuizListPreviewStatus.Available,
                null,
                new DateTimeOffset(2026, 7, 18, 5, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 7, 21, 5, 0, 0, TimeSpan.Zero),
                2,
                1,
                QuizListPreviewResultVisibility.AfterClose),
            new(
                "quiz_preview_003",
                "Living Things Review",
                NutriMindSubject.Science,
                NutriMindTerm.Term1,
                QuizListPreviewStatus.Completed,
                null,
                new DateTimeOffset(2026, 7, 10, 5, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 7, 18, 5, 0, 0, TimeSpan.Zero),
                1,
                1,
                QuizListPreviewResultVisibility.Immediate),
            new(
                "quiz_preview_004",
                "Matter and Materials Check",
                NutriMindSubject.Science,
                NutriMindTerm.Term2,
                QuizListPreviewStatus.Locked,
                "Your Teacher will open this quiz later.",
                new DateTimeOffset(2026, 7, 25, 5, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 8, 1, 5, 0, 0, TimeSpan.Zero),
                1,
                0,
                QuizListPreviewResultVisibility.TeacherRelease),
            new(
                "quiz_preview_005",
                "Reading Strategies Check",
                NutriMindSubject.LiteraQuest,
                NutriMindTerm.Term2,
                QuizListPreviewStatus.Unavailable,
                "This quiz is no longer available.",
                new DateTimeOffset(2026, 7, 10, 5, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 7, 15, 5, 0, 0, TimeSpan.Zero),
                1,
                0,
                QuizListPreviewResultVisibility.Hidden)
        };

        private VisualElement _root;
        private ScrollView _scroll;
        private VisualElement _dataStateHost;
        private Label _summaryAvailableCount;
        private Label _summaryClosingCount;
        private Label _summaryCompletedCount;
        private DropdownField _subjectFilter;
        private DropdownField _termFilter;
        private DropdownField _statusFilter;
        private Button _resetFiltersButton;
        private Button _filterEmptyResetButton;
        private Label _visibleCountLabel;
        private VisualElement _filterEmpty;
        private VisualElement _pagination;
        private Button _previousPageButton;
        private Button _nextPageButton;
        private Label _pageLabel;
        private readonly ItemShellElements[] _itemShells = new ItemShellElements[5];

        private TemplateContainer _ownedDataStateInstance;
        private DataStatePanelView _dataStateView;
        private bool _warnedMissingDataStateAsset;
        private bool _disposed;
        private bool _suppressFilterEvents;
        private bool _hasMore;
        private float _lastWidth = -1f;

        public QuizListPanelView(
            VisualElement root,
            VisualTreeAsset dataStatePanelAsset = null)
        {
            ResolveRoot(root);
            if (_root == null)
            {
                Debug.LogWarning(
                    "[QuizListPanelView] Could not resolve quiz-list-root inside the supplied element.");
                return;
            }

            CacheElements();
            BindDataStatePanel(dataStatePanelAsset);
            ApplyStaticFixtures();
            RegisterCallbacks();
            ApplyResponsiveClasses(_root.resolvedStyle.width);
            SetDataState(DataStatePanelState.Content);
        }

        public VisualElement Root => _root;

        public bool IsBound => _root != null && !_disposed;

        public DataStatePanelState DataState { get; private set; } = DataStatePanelState.Content;

        public QuizListPreviewFilters Filters { get; private set; } =
            new(
                QuizListPreviewSubjectFilter.All,
                QuizListPreviewTermFilter.All,
                QuizListPreviewStatusFilter.All);

        public int CurrentPage { get; private set; } = 1;

        public int LastPage { get; private set; } = 1;

        public event Action<QuizListPreviewItem> QuizDetailsRequested;
        public event Action<QuizListPreviewItem> QuizResultRequested;
        public event Action<QuizListPreviewFilters> FiltersChanged;
        public event Action<int> PageRequested;
        public event Action RetryRequested;
        public event Action ReturnToMainRequested;

        /// <summary>
        /// Restores retained filter values without raising <see cref="FiltersChanged"/>.
        /// </summary>
        public void SetFilters(QuizListPreviewFilters filters)
        {
            if (!IsBound)
            {
                return;
            }

            ApplyFilters(filters, raiseEvent: false, resetPageVisual: false);
        }

        public void ResetFilters()
        {
            if (!IsBound)
            {
                return;
            }

            ApplyFilters(
                new QuizListPreviewFilters(
                    QuizListPreviewSubjectFilter.All,
                    QuizListPreviewTermFilter.All,
                    QuizListPreviewStatusFilter.All),
                raiseEvent: true,
                resetPageVisual: true);
        }

        public void SetPagination(int currentPage, int lastPage, bool hasMore)
        {
            if (!IsBound)
            {
                return;
            }

            LastPage = Mathf.Max(1, lastPage);
            CurrentPage = Mathf.Clamp(currentPage, 1, LastPage);
            _hasMore = hasMore;

            if (_pageLabel != null)
            {
                _pageLabel.text = $"Page {CurrentPage} of {LastPage}";
            }

            _previousPageButton?.SetEnabled(CurrentPage > 1);
            _nextPageButton?.SetEnabled(CurrentPage < LastPage && _hasMore);
            _pagination?.EnableInClassList(PaginationHiddenClass, LastPage <= 1);
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

            DataState = state;

            if (state == DataStatePanelState.Content)
            {
                ShowContent();
                _dataStateView?.SetState(DataStatePanelState.Content);
                return;
            }

            HideContent();
            _dataStateView.SetState(state);
            ApplyQuizDataStateCopy(state);
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

            QuizDetailsRequested = null;
            QuizResultRequested = null;
            FiltersChanged = null;
            PageRequested = null;
            RetryRequested = null;
            ReturnToMainRequested = null;

            _root = null;
            _scroll = null;
            _dataStateHost = null;
            _summaryAvailableCount = null;
            _summaryClosingCount = null;
            _summaryCompletedCount = null;
            _subjectFilter = null;
            _termFilter = null;
            _statusFilter = null;
            _resetFiltersButton = null;
            _filterEmptyResetButton = null;
            _visibleCountLabel = null;
            _filterEmpty = null;
            _pagination = null;
            _previousPageButton = null;
            _nextPageButton = null;
            _pageLabel = null;
            for (int i = 0; i < _itemShells.Length; i++)
            {
                _itemShells[i] = default;
            }

            _lastWidth = -1f;
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
            _scroll = _root.Q<ScrollView>("quiz-list-scroll");
            _dataStateHost = _root.Q<VisualElement>("quiz-list-data-state-host");
            _summaryAvailableCount = _root.Q<Label>("quiz-list-summary-available-count");
            _summaryClosingCount = _root.Q<Label>("quiz-list-summary-closing-count");
            _summaryCompletedCount = _root.Q<Label>("quiz-list-summary-completed-count");
            _subjectFilter = _root.Q<DropdownField>("quiz-list-filter-subject");
            _termFilter = _root.Q<DropdownField>("quiz-list-filter-term");
            _statusFilter = _root.Q<DropdownField>("quiz-list-filter-status");
            _resetFiltersButton = _root.Q<Button>("quiz-list-reset-filters");
            _filterEmptyResetButton = _root.Q<Button>("quiz-list-filter-empty-reset");
            _visibleCountLabel = _root.Q<Label>("quiz-list-visible-count");
            _filterEmpty = _root.Q<VisualElement>("quiz-list-filter-empty");
            _pagination = _root.Q<VisualElement>("quiz-list-pagination");
            _previousPageButton = _root.Q<Button>("quiz-list-page-previous");
            _nextPageButton = _root.Q<Button>("quiz-list-page-next");
            _pageLabel = _root.Q<Label>("quiz-list-page-label");

            for (int i = 0; i < _itemShells.Length; i++)
            {
                int index = i + 1;
                _itemShells[i] = new ItemShellElements(
                    _root.Q<VisualElement>($"quiz-list-item-{index}"),
                    _root.Q<VisualElement>($"quiz-{index}-subject-icon"),
                    _root.Q<Label>($"quiz-{index}-title"),
                    _root.Q<Label>($"quiz-{index}-subject-term"),
                    _root.Q<VisualElement>($"quiz-{index}-schedule-icon"),
                    _root.Q<Label>($"quiz-{index}-schedule"),
                    _root.Q<Label>($"quiz-{index}-attempts"),
                    _root.Q<Label>($"quiz-{index}-result-visibility"),
                    _root.Q<VisualElement>($"quiz-{index}-status"),
                    _root.Q<VisualElement>($"quiz-{index}-status-icon"),
                    _root.Q<Label>($"quiz-{index}-status-label"),
                    _root.Q<Label>($"quiz-{index}-lock-reason"),
                    _root.Q<Button>($"quiz-{index}-action-button"),
                    _root.Q<Label>($"quiz-{index}-action-label"));
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
                        "[QuizListPanelView] DataStatePanel VisualTreeAsset is missing. " +
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
                    "[QuizListPanelView] Failed to bind nested DataStatePanelView.");
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
            if (_subjectFilter != null)
            {
                _subjectFilter.RegisterValueChangedCallback(OnSubjectFilterChanged);
            }

            if (_termFilter != null)
            {
                _termFilter.RegisterValueChangedCallback(OnTermFilterChanged);
            }

            if (_statusFilter != null)
            {
                _statusFilter.RegisterValueChangedCallback(OnStatusFilterChanged);
            }

            _resetFiltersButton?.RegisterCallback<ClickEvent>(OnResetFiltersClicked);
            _filterEmptyResetButton?.RegisterCallback<ClickEvent>(OnResetFiltersClicked);
            _previousPageButton?.RegisterCallback<ClickEvent>(OnPreviousPageClicked);
            _nextPageButton?.RegisterCallback<ClickEvent>(OnNextPageClicked);

            for (int i = 0; i < _itemShells.Length; i++)
            {
                _itemShells[i].ActionButton?.RegisterCallback<ClickEvent>(OnItemActionClicked);
            }

            _root?.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);

            if (_dataStateView != null && _dataStateView.IsBound)
            {
                _dataStateView.PrimaryActionRequested += OnDataStatePrimaryAction;
                _dataStateView.SecondaryActionRequested += OnDataStateSecondaryAction;
            }
        }

        private void UnregisterCallbacks()
        {
            if (_subjectFilter != null)
            {
                _subjectFilter.UnregisterValueChangedCallback(OnSubjectFilterChanged);
            }

            if (_termFilter != null)
            {
                _termFilter.UnregisterValueChangedCallback(OnTermFilterChanged);
            }

            if (_statusFilter != null)
            {
                _statusFilter.UnregisterValueChangedCallback(OnStatusFilterChanged);
            }

            _resetFiltersButton?.UnregisterCallback<ClickEvent>(OnResetFiltersClicked);
            _filterEmptyResetButton?.UnregisterCallback<ClickEvent>(OnResetFiltersClicked);
            _previousPageButton?.UnregisterCallback<ClickEvent>(OnPreviousPageClicked);
            _nextPageButton?.UnregisterCallback<ClickEvent>(OnNextPageClicked);

            for (int i = 0; i < _itemShells.Length; i++)
            {
                _itemShells[i].ActionButton?.UnregisterCallback<ClickEvent>(OnItemActionClicked);
            }

            _root?.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);

            if (_dataStateView != null)
            {
                _dataStateView.PrimaryActionRequested -= OnDataStatePrimaryAction;
                _dataStateView.SecondaryActionRequested -= OnDataStateSecondaryAction;
            }
        }

        private void ApplyStaticFixtures()
        {
            ApplySummaryCounts();
            for (int i = 0; i < PreviewFixtures.Length && i < _itemShells.Length; i++)
            {
                BindItemShell(_itemShells[i], PreviewFixtures[i]);
            }

            ApplyFilters(Filters, raiseEvent: false, resetPageVisual: false);
            SetPagination(1, 2, true);
        }

        private void ApplySummaryCounts()
        {
            int available = 0;
            int closingSoon = 0;
            int completed = 0;

            for (int i = 0; i < PreviewFixtures.Length; i++)
            {
                QuizListPreviewItem item = PreviewFixtures[i];
                if (item.Status == QuizListPreviewStatus.Available)
                {
                    available++;
                    if (IsClosingSoon(item))
                    {
                        closingSoon++;
                    }
                }
                else if (item.Status == QuizListPreviewStatus.Completed)
                {
                    completed++;
                }
            }

            if (_summaryAvailableCount != null)
            {
                _summaryAvailableCount.text = available.ToString(CultureInfo.InvariantCulture);
            }

            if (_summaryClosingCount != null)
            {
                _summaryClosingCount.text = closingSoon.ToString(CultureInfo.InvariantCulture);
            }

            if (_summaryCompletedCount != null)
            {
                _summaryCompletedCount.text = completed.ToString(CultureInfo.InvariantCulture);
            }
        }

        private void BindItemShell(ItemShellElements shell, QuizListPreviewItem item)
        {
            bool closingSoon = IsClosingSoon(item);

            if (shell.Title != null)
            {
                shell.Title.text = item.Title;
            }

            if (shell.SubjectTerm != null)
            {
                shell.SubjectTerm.text = $"{GetSubjectLabel(item.Subject)} • Term {(int)item.Term}";
            }

            if (shell.Schedule != null)
            {
                shell.Schedule.text = BuildScheduleCopy(item);
            }

            if (shell.Attempts != null)
            {
                shell.Attempts.text = BuildAttemptsCopy(item);
            }

            if (shell.ResultVisibility != null)
            {
                shell.ResultVisibility.text = BuildResultVisibilityCopy(item.ResultVisibility);
            }

            bool showLockReason =
                (item.Status == QuizListPreviewStatus.Locked
                 || item.Status == QuizListPreviewStatus.Unavailable)
                && !string.IsNullOrWhiteSpace(item.LockedReason);

            if (shell.LockReason != null)
            {
                shell.LockReason.text = showLockReason ? item.LockedReason : string.Empty;
                shell.LockReason.EnableInClassList(LockReasonHiddenClass, !showLockReason);
            }

            if (shell.StatusLabel != null)
            {
                shell.StatusLabel.text = closingSoon
                    ? "Closing Soon"
                    : GetStatusLabel(item.Status);
            }

            if (shell.ActionLabel != null)
            {
                shell.ActionLabel.text = GetActionLabel(item.Status);
            }

            ClearAndApplySubjectIcon(shell.SubjectIcon, item.Subject);
            ClearAndApplyScheduleIcon(shell.ScheduleIcon, item, closingSoon);
            ClearAndApplyStatusIcon(shell.StatusIcon, item.Status, closingSoon);
            ClearAndApplyItemModifiers(shell.Row, item.Status, closingSoon);
            ClearAndApplyStatusBadge(shell.StatusBadge, item.Status);
            ApplyActionButtonVariant(shell.ActionButton, item.Status);

            if (shell.ActionButton != null)
            {
                shell.ActionButton.tooltip = GetActionLabel(item.Status);
            }
        }

        private void ApplyFilters(
            QuizListPreviewFilters filters,
            bool raiseEvent,
            bool resetPageVisual)
        {
            Filters = filters;
            _suppressFilterEvents = true;
            try
            {
                _subjectFilter?.SetValueWithoutNotify(GetSubjectFilterChoice(filters.Subject));
                _termFilter?.SetValueWithoutNotify(GetTermFilterChoice(filters.Term));
                _statusFilter?.SetValueWithoutNotify(GetStatusFilterChoice(filters.Status));
            }
            finally
            {
                _suppressFilterEvents = false;
            }

            int visibleCount = 0;
            for (int i = 0; i < PreviewFixtures.Length && i < _itemShells.Length; i++)
            {
                bool matches = MatchesFilters(PreviewFixtures[i], filters);
                _itemShells[i].Row?.EnableInClassList(ItemHiddenClass, !matches);
                if (matches)
                {
                    visibleCount++;
                }
            }

            if (_visibleCountLabel != null)
            {
                _visibleCountLabel.text = visibleCount == 1
                    ? "1 quiz shown"
                    : $"{visibleCount} quizzes shown";
            }

            _filterEmpty?.EnableInClassList(FilterEmptyHiddenClass, visibleCount > 0);

            if (resetPageVisual)
            {
                SetPagination(1, LastPage, _hasMore);
            }

            if (raiseEvent)
            {
                FiltersChanged?.Invoke(filters);
            }
        }

        private void OnSubjectFilterChanged(ChangeEvent<string> evt)
        {
            if (_suppressFilterEvents)
            {
                return;
            }

            ApplyFilters(
                new QuizListPreviewFilters(
                    ParseSubjectFilter(evt.newValue),
                    Filters.Term,
                    Filters.Status),
                raiseEvent: true,
                resetPageVisual: true);
        }

        private void OnTermFilterChanged(ChangeEvent<string> evt)
        {
            if (_suppressFilterEvents)
            {
                return;
            }

            ApplyFilters(
                new QuizListPreviewFilters(
                    Filters.Subject,
                    ParseTermFilter(evt.newValue),
                    Filters.Status),
                raiseEvent: true,
                resetPageVisual: true);
        }

        private void OnStatusFilterChanged(ChangeEvent<string> evt)
        {
            if (_suppressFilterEvents)
            {
                return;
            }

            ApplyFilters(
                new QuizListPreviewFilters(
                    Filters.Subject,
                    Filters.Term,
                    ParseStatusFilter(evt.newValue)),
                raiseEvent: true,
                resetPageVisual: true);
        }

        private void OnResetFiltersClicked(ClickEvent evt) => ResetFilters();

        private void OnPreviousPageClicked(ClickEvent evt)
        {
            int target = Mathf.Max(1, CurrentPage - 1);
            if (target == CurrentPage)
            {
                return;
            }

            PageRequested?.Invoke(target);
        }

        private void OnNextPageClicked(ClickEvent evt)
        {
            int target = Mathf.Min(LastPage, CurrentPage + 1);
            if (target == CurrentPage)
            {
                return;
            }

            PageRequested?.Invoke(target);
        }

        private void OnItemActionClicked(ClickEvent evt)
        {
            if (evt.currentTarget is not Button button)
            {
                return;
            }

            int index = ResolveItemIndex(button);
            if (index < 0 || index >= PreviewFixtures.Length)
            {
                return;
            }

            QuizListPreviewItem item = PreviewFixtures[index];
            if (item.Status == QuizListPreviewStatus.Completed)
            {
                QuizResultRequested?.Invoke(item);
                return;
            }

            QuizDetailsRequested?.Invoke(item);
        }

        private void OnDataStatePrimaryAction()
        {
            switch (DataState)
            {
                case DataStatePanelState.Empty:
                case DataStatePanelState.OfflineUnavailable:
                case DataStatePanelState.RecoverableError:
                case DataStatePanelState.OfflineCached:
                    RetryRequested?.Invoke();
                    break;

                case DataStatePanelState.PermissionOrLocked:
                    ReturnToMainRequested?.Invoke();
                    break;
            }
        }

        private void OnDataStateSecondaryAction()
        {
            switch (DataState)
            {
                case DataStatePanelState.Empty:
                case DataStatePanelState.OfflineUnavailable:
                case DataStatePanelState.RecoverableError:
                case DataStatePanelState.OfflineCached:
                    ReturnToMainRequested?.Invoke();
                    break;
            }
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

        private void ApplyQuizDataStateCopy(DataStatePanelState state)
        {
            if (_dataStateView == null || !_dataStateView.IsBound)
            {
                return;
            }

            switch (state)
            {
                case DataStatePanelState.Loading:
                    _dataStateView.Configure(
                        title: "Loading quizzes",
                        message: "Checking your latest Quiz Portal assignments.",
                        detail: string.Empty,
                        iconClass: "ds-icon--book",
                        primaryActionLabel: string.Empty,
                        secondaryActionLabel: string.Empty);
                    break;

                case DataStatePanelState.Empty:
                    _dataStateView.Configure(
                        title: "No quizzes assigned yet",
                        message: "New Quiz Portal assignments from your Teacher will appear here.",
                        detail: "Check again later or refresh when you are online.",
                        iconClass: "ds-icon--book",
                        primaryActionLabel: "Refresh",
                        secondaryActionLabel: "Return Home");
                    break;

                case DataStatePanelState.OfflineUnavailable:
                    _dataStateView.Configure(
                        title: "Quiz Portal needs a connection",
                        message: "Quiz assignments and attempts are managed online.",
                        detail: "Reconnect to load current availability before opening a quiz.",
                        iconClass: "ds-icon--wifi",
                        primaryActionLabel: "Retry Connection",
                        secondaryActionLabel: "Return Home");
                    break;

                case DataStatePanelState.RecoverableError:
                    _dataStateView.Configure(
                        title: "Quizzes could not be loaded",
                        message: "Check your connection and try again.",
                        detail: "No attempt was started, and your existing progress is safe.",
                        iconClass: "ds-icon--error",
                        primaryActionLabel: "Try Again",
                        secondaryActionLabel: "Return Home");
                    break;

                case DataStatePanelState.PermissionOrLocked:
                    _dataStateView.Configure(
                        title: "Quiz Portal is not available",
                        message: "Your account does not currently have access to Quiz Portal assignments.",
                        detail: "Ask your Teacher if you believe this should be available.",
                        iconClass: "ds-icon--lock",
                        primaryActionLabel: "Return Home",
                        secondaryActionLabel: string.Empty);
                    break;

                case DataStatePanelState.OfflineCached:
                    _dataStateView.Configure(
                        title: "Saved quiz information may be outdated",
                        message: "Reconnect before opening or starting a quiz.",
                        detail: "Quiz availability and attempts must be confirmed by the server.",
                        iconClass: "ds-icon--wifi",
                        primaryActionLabel: "Retry Connection",
                        secondaryActionLabel: "Return Home");
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

        private static bool MatchesFilters(QuizListPreviewItem item, QuizListPreviewFilters filters)
        {
            if (filters.Subject != QuizListPreviewSubjectFilter.All
                && MapSubjectFilter(item.Subject) != filters.Subject)
            {
                return false;
            }

            if (filters.Term != QuizListPreviewTermFilter.All
                && MapTermFilter(item.Term) != filters.Term)
            {
                return false;
            }

            if (filters.Status != QuizListPreviewStatusFilter.All
                && MapStatusFilter(item.Status) != filters.Status)
            {
                return false;
            }

            return true;
        }

        private static bool IsClosingSoon(QuizListPreviewItem item)
        {
            if (item.Status != QuizListPreviewStatus.Available || !item.ClosesAtUtc.HasValue)
            {
                return false;
            }

            DateTimeOffset closesAt = item.ClosesAtUtc.Value;
            if (closesAt <= PreviewReferenceTimeUtc)
            {
                return false;
            }

            TimeSpan remaining = closesAt - PreviewReferenceTimeUtc;
            return remaining.TotalHours <= ClosingSoonHours;
        }

        private static string BuildScheduleCopy(QuizListPreviewItem item)
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
                    return item.OpensAtUtc.HasValue
                        ? $"Opens {FormatDate(item.OpensAtUtc.Value)}"
                        : "Locked";

                default:
                    if (item.ClosesAtUtc.HasValue && item.ClosesAtUtc.Value > PreviewReferenceTimeUtc)
                    {
                        return $"Closes {FormatDate(item.ClosesAtUtc.Value)}";
                    }

                    return "Available now";
            }
        }

        private static string BuildAttemptsCopy(QuizListPreviewItem item)
        {
            int maxAttempts = Mathf.Max(0, item.MaxAttempts);
            int attemptsUsed = Mathf.Clamp(item.AttemptsUsed, 0, maxAttempts);
            string attemptWord = maxAttempts == 1 ? "attempt" : "attempts";
            return $"{attemptsUsed} of {maxAttempts} {attemptWord} used";
        }

        private static string BuildResultVisibilityCopy(QuizListPreviewResultVisibility visibility) =>
            visibility switch
            {
                QuizListPreviewResultVisibility.Immediate =>
                    "Results available after submission",
                QuizListPreviewResultVisibility.AfterClose =>
                    "Results available after the quiz closes",
                QuizListPreviewResultVisibility.TeacherRelease =>
                    "Results available after your Teacher releases them",
                _ => "Results are not currently visible"
            };

        private static string GetStatusLabel(QuizListPreviewStatus status) =>
            status switch
            {
                QuizListPreviewStatus.Available => "Available",
                QuizListPreviewStatus.Completed => "Completed",
                QuizListPreviewStatus.Locked => "Locked",
                _ => "Unavailable"
            };

        private static string GetActionLabel(QuizListPreviewStatus status) =>
            status switch
            {
                QuizListPreviewStatus.Available => "View Quiz",
                QuizListPreviewStatus.Completed => "View Result",
                _ => "View Details"
            };

        private static string GetSubjectLabel(NutriMindSubject subject) =>
            subject switch
            {
                NutriMindSubject.LiteraQuest => "LiteraQuest",
                NutriMindSubject.PeAndHealth => "PE & Health",
                _ => "Science"
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

            icon.AddToClassList(subject switch
            {
                NutriMindSubject.LiteraQuest => "ds-icon--book",
                NutriMindSubject.PeAndHealth => "ds-icon--bolt",
                _ => "ds-icon--potion"
            });
        }

        private static void ClearAndApplyScheduleIcon(
            VisualElement icon,
            QuizListPreviewItem item,
            bool closingSoon)
        {
            if (icon == null)
            {
                return;
            }

            foreach (string iconClass in StatusIconClasses)
            {
                icon.RemoveFromClassList(iconClass);
            }

            string className = item.Status switch
            {
                QuizListPreviewStatus.Completed => "ds-icon--check",
                QuizListPreviewStatus.Locked => "ds-icon--lock",
                QuizListPreviewStatus.Unavailable => "ds-icon--warning",
                _ when closingSoon => "ds-icon--clock",
                _ => "ds-icon--calendar"
            };
            icon.AddToClassList(className);
        }

        private static void ClearAndApplyStatusIcon(
            VisualElement icon,
            QuizListPreviewStatus status,
            bool closingSoon)
        {
            if (icon == null)
            {
                return;
            }

            foreach (string iconClass in StatusIconClasses)
            {
                icon.RemoveFromClassList(iconClass);
            }

            icon.AddToClassList(closingSoon
                ? "ds-icon--clock"
                : status switch
                {
                    QuizListPreviewStatus.Available => "ds-icon--play",
                    QuizListPreviewStatus.Completed => "ds-icon--check",
                    QuizListPreviewStatus.Locked => "ds-icon--lock",
                    _ => "ds-icon--warning"
                });
        }

        private static void ClearAndApplyItemModifiers(
            VisualElement row,
            QuizListPreviewStatus status,
            bool closingSoon)
        {
            if (row == null)
            {
                return;
            }

            foreach (string modifier in ItemModifierClasses)
            {
                row.RemoveFromClassList(modifier);
            }

            row.AddToClassList(ItemStatusPrefix + status switch
            {
                QuizListPreviewStatus.Available => "available",
                QuizListPreviewStatus.Completed => "completed",
                QuizListPreviewStatus.Locked => "locked",
                _ => "unavailable"
            });

            if (closingSoon)
            {
                row.AddToClassList("quiz-list-panel__item--closing-soon");
            }
        }

        private static void ClearAndApplyStatusBadge(
            VisualElement badge,
            QuizListPreviewStatus status)
        {
            if (badge == null)
            {
                return;
            }

            foreach (string statusClass in StatusBadgeClasses)
            {
                badge.RemoveFromClassList(statusClass);
            }

            badge.AddToClassList(StatusBadgePrefix + status switch
            {
                QuizListPreviewStatus.Available => "available",
                QuizListPreviewStatus.Completed => "completed",
                QuizListPreviewStatus.Locked => "locked",
                _ => "unavailable"
            });
        }

        private static void ApplyActionButtonVariant(Button button, QuizListPreviewStatus status)
        {
            if (button == null)
            {
                return;
            }

            foreach (string variant in ActionButtonVariantClasses)
            {
                button.RemoveFromClassList(variant);
            }

            button.AddToClassList(status switch
            {
                QuizListPreviewStatus.Available => "ds-btn--primary",
                QuizListPreviewStatus.Completed => "ds-btn--secondary",
                _ => "ds-btn--ghost"
            });
        }

        private static int ResolveItemIndex(Button button)
        {
            if (button == null || string.IsNullOrEmpty(button.name))
            {
                return -1;
            }

            // quiz-N-action-button
            string[] parts = button.name.Split('-');
            if (parts.Length >= 2 && int.TryParse(parts[1], out int number))
            {
                return Mathf.Clamp(number, 1, 5) - 1;
            }

            return -1;
        }

        private static QuizListPreviewSubjectFilter MapSubjectFilter(NutriMindSubject subject) =>
            subject switch
            {
                NutriMindSubject.LiteraQuest => QuizListPreviewSubjectFilter.LiteraQuest,
                NutriMindSubject.PeAndHealth => QuizListPreviewSubjectFilter.PeAndHealth,
                _ => QuizListPreviewSubjectFilter.Science
            };

        private static QuizListPreviewTermFilter MapTermFilter(NutriMindTerm term) =>
            term switch
            {
                NutriMindTerm.Term1 => QuizListPreviewTermFilter.Term1,
                NutriMindTerm.Term2 => QuizListPreviewTermFilter.Term2,
                _ => QuizListPreviewTermFilter.Term3
            };

        private static QuizListPreviewStatusFilter MapStatusFilter(QuizListPreviewStatus status) =>
            status switch
            {
                QuizListPreviewStatus.Available => QuizListPreviewStatusFilter.Available,
                QuizListPreviewStatus.Completed => QuizListPreviewStatusFilter.Completed,
                QuizListPreviewStatus.Locked => QuizListPreviewStatusFilter.Locked,
                _ => QuizListPreviewStatusFilter.Unavailable
            };

        private static string GetSubjectFilterChoice(QuizListPreviewSubjectFilter filter) =>
            filter switch
            {
                QuizListPreviewSubjectFilter.LiteraQuest => "LiteraQuest",
                QuizListPreviewSubjectFilter.PeAndHealth => "PE & Health",
                QuizListPreviewSubjectFilter.Science => "Science",
                _ => "All Subjects"
            };

        private static string GetTermFilterChoice(QuizListPreviewTermFilter filter) =>
            filter switch
            {
                QuizListPreviewTermFilter.Term1 => "Term 1",
                QuizListPreviewTermFilter.Term2 => "Term 2",
                QuizListPreviewTermFilter.Term3 => "Term 3",
                _ => "All Terms"
            };

        private static string GetStatusFilterChoice(QuizListPreviewStatusFilter filter) =>
            filter switch
            {
                QuizListPreviewStatusFilter.Available => "Available",
                QuizListPreviewStatusFilter.Completed => "Completed",
                QuizListPreviewStatusFilter.Locked => "Locked",
                QuizListPreviewStatusFilter.Unavailable => "Unavailable",
                _ => "All Statuses"
            };

        private static QuizListPreviewSubjectFilter ParseSubjectFilter(string value) =>
            value switch
            {
                "LiteraQuest" => QuizListPreviewSubjectFilter.LiteraQuest,
                "PE & Health" => QuizListPreviewSubjectFilter.PeAndHealth,
                "Science" => QuizListPreviewSubjectFilter.Science,
                _ => QuizListPreviewSubjectFilter.All
            };

        private static QuizListPreviewTermFilter ParseTermFilter(string value) =>
            value switch
            {
                "Term 1" => QuizListPreviewTermFilter.Term1,
                "Term 2" => QuizListPreviewTermFilter.Term2,
                "Term 3" => QuizListPreviewTermFilter.Term3,
                _ => QuizListPreviewTermFilter.All
            };

        private static QuizListPreviewStatusFilter ParseStatusFilter(string value) =>
            value switch
            {
                "Available" => QuizListPreviewStatusFilter.Available,
                "Completed" => QuizListPreviewStatusFilter.Completed,
                "Locked" => QuizListPreviewStatusFilter.Locked,
                "Unavailable" => QuizListPreviewStatusFilter.Unavailable,
                _ => QuizListPreviewStatusFilter.All
            };

        private readonly struct ItemShellElements
        {
            public ItemShellElements(
                VisualElement row,
                VisualElement subjectIcon,
                Label title,
                Label subjectTerm,
                VisualElement scheduleIcon,
                Label schedule,
                Label attempts,
                Label resultVisibility,
                VisualElement statusBadge,
                VisualElement statusIcon,
                Label statusLabel,
                Label lockReason,
                Button actionButton,
                Label actionLabel)
            {
                Row = row;
                SubjectIcon = subjectIcon;
                Title = title;
                SubjectTerm = subjectTerm;
                ScheduleIcon = scheduleIcon;
                Schedule = schedule;
                Attempts = attempts;
                ResultVisibility = resultVisibility;
                StatusBadge = statusBadge;
                StatusIcon = statusIcon;
                StatusLabel = statusLabel;
                LockReason = lockReason;
                ActionButton = actionButton;
                ActionLabel = actionLabel;
            }

            public VisualElement Row { get; }
            public VisualElement SubjectIcon { get; }
            public Label Title { get; }
            public Label SubjectTerm { get; }
            public VisualElement ScheduleIcon { get; }
            public Label Schedule { get; }
            public Label Attempts { get; }
            public Label ResultVisibility { get; }
            public VisualElement StatusBadge { get; }
            public VisualElement StatusIcon { get; }
            public Label StatusLabel { get; }
            public Label LockReason { get; }
            public Button ActionButton { get; }
            public Label ActionLabel { get; }
        }
    }
}
