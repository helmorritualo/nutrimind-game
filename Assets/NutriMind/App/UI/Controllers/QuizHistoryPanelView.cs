using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace NutriMind.App.UI
{
    /// <summary>
    /// Presentation-only QuizHistory route states for static preview.
    /// </summary>
    public enum QuizHistoryPreviewState
    {
        Content = 0,
        Loading = 1,
        Empty = 2,
        OfflineCached = 3,
        RecoverableError = 4
    }

    /// <summary>
    /// Subject filter options backed by the quiz-results list endpoint.
    /// </summary>
    public enum QuizHistoryPreviewSubjectFilter
    {
        All = 0,
        LiteraQuest = 1,
        PeAndHealth = 2,
        Science = 3
    }

    /// <summary>
    /// Term filter options backed by the quiz-results list endpoint.
    /// </summary>
    public enum QuizHistoryPreviewTermFilter
    {
        All = 0,
        Term1 = 1,
        Term2 = 2,
        Term3 = 3
    }

    /// <summary>
    /// Immutable subject and term filter values for Quiz History preview.
    /// </summary>
    public readonly struct QuizHistoryPreviewFilters
    {
        public QuizHistoryPreviewFilters(
            QuizHistoryPreviewSubjectFilter subject,
            QuizHistoryPreviewTermFilter term)
        {
            Subject = subject;
            Term = term;
        }

        public QuizHistoryPreviewSubjectFilter Subject { get; }
        public QuizHistoryPreviewTermFilter Term { get; }
    }

    /// <summary>
    /// Immutable history list item projecting summary + scored result fixtures.
    /// </summary>
    public sealed class QuizHistoryPreviewItem
    {
        public QuizHistoryPreviewItem(
            string attemptId,
            QuizListPreviewItem summary,
            QuizResultPreviewContent result)
        {
            AttemptId = attemptId ?? string.Empty;
            Summary = summary;
            Result = result;
        }

        public string AttemptId { get; }
        public QuizListPreviewItem Summary { get; }
        public QuizResultPreviewContent Result { get; }
    }

    /// <summary>
    /// Typed selection payload when opening a historical result.
    /// </summary>
    public readonly struct QuizHistoryPreviewSelection
    {
        public QuizHistoryPreviewSelection(string attemptId, QuizListPreviewItem summary)
        {
            AttemptId = attemptId ?? string.Empty;
            Summary = summary;
        }

        public string AttemptId { get; }
        public QuizListPreviewItem Summary { get; }
    }

    /// <summary>
    /// Deterministic static-preview catalog for Quiz History.
    /// Projects existing detail/result catalogs into exactly one history item.
    /// </summary>
    public static class QuizHistoryPreviewCatalog
    {
        /// <summary>
        /// Builds the canonical one-item history list from existing preview catalogs.
        /// Returns an empty list when required catalog lookups fail.
        /// </summary>
        public static IReadOnlyList<QuizHistoryPreviewItem> CreateCanonicalItems()
        {
            QuizListPreviewItem summary = QuizDetailPreviewCatalog.CreateCanonicalSummary();

            if (!QuizDetailPreviewCatalog.TryGetDetail(summary.Id, out _))
            {
                Debug.LogWarning(
                    "[QuizHistoryPreviewCatalog] Canonical quiz detail lookup failed. " +
                    "Returning empty history list; no fallback attempt was invented.");
                return Array.Empty<QuizHistoryPreviewItem>();
            }

            if (!QuizResultPreviewCatalog.TryGetResult(summary.Id, out QuizResultPreviewContent result)
                || result == null)
            {
                Debug.LogWarning(
                    "[QuizHistoryPreviewCatalog] Canonical scored-result lookup failed. " +
                    "Returning empty history list; no fallback attempt was invented.");
                return Array.Empty<QuizHistoryPreviewItem>();
            }

            var items = new QuizHistoryPreviewItem[]
            {
                new(result.AttemptId, summary, result)
            };

            return items;
        }
    }

    /// <summary>
    /// Presentation-only Quiz Portal history view. Displays loaded scored-result
    /// projections with subject/term filters and shared route states.
    /// Does not call APIs, invent attempts, paginate, or recalculate scores.
    /// </summary>
    public sealed class QuizHistoryPanelView : IAppScreenView
    {
        private const string RootName = "quiz-history-root";
        private const string CompactClass = "quiz-history-panel--compact";
        private const string NarrowClass = "quiz-history-panel--narrow";
        private const string MobileClass = "mobile";
        private const string DataStateHostVisibleClass = "quiz-history-panel__data-state-host--visible";
        private const string ContentShellHiddenClass = "quiz-history-panel__content-shell--hidden";
        private const string OfflineNoticeHiddenClass = "quiz-history-panel__offline-notice--hidden";
        private const string FilterEmptyHiddenClass = "quiz-history-panel__filter-empty--hidden";
        private const string CompactScreenClass = "app-screen-content--compact";
        private const string NarrowScreenClass = "app-screen-content--narrow";
        private const float CompactBreakpoint = 1100f;
        private const float NarrowBreakpoint = 820f;

        private sealed class ResultCardBinding
        {
            public Button Button;
            public EventCallback<ClickEvent> Callback;
            public string AttemptId;
            public QuizListPreviewItem Summary;
        }

        private VisualElement _root;
        private VisualElement _contentShell;
        private ScrollView _scroll;
        private VisualElement _body;
        private VisualElement _offlineNotice;
        private Label _totalValue;
        private Label _passedValue;
        private Label _latestScoreValue;
        private Button _backButton;
        private DropdownField _subjectFilter;
        private DropdownField _termFilter;
        private Button _resetFiltersButton;
        private Label _visibleCountLabel;
        private VisualElement _resultsList;
        private VisualElement _filterEmpty;
        private Button _filterEmptyResetButton;
        private VisualElement _dataStateHost;

        private TemplateContainer _ownedDataStateInstance;
        private DataStatePanelView _dataStateView;
        private bool _warnedMissingDataStateAsset;
        private bool _warnedInvalidItems;

        private readonly List<QuizHistoryPreviewItem> _loadedItems = new();
        private readonly List<QuizHistoryPreviewItem> _visibleItems = new();
        private readonly List<ResultCardBinding> _cardBindings = new();

        private EventCallback<ClickEvent> _backClicked;
        private EventCallback<ClickEvent> _resetFiltersClicked;
        private EventCallback<ClickEvent> _filterEmptyResetClicked;
        private EventCallback<ChangeEvent<string>> _subjectFilterChanged;
        private EventCallback<ChangeEvent<string>> _termFilterChanged;
        private EventCallback<GeometryChangedEvent> _geometryChanged;
        private bool _suppressFilterEvents;
        private bool _disposed;
        private float _lastWidth = -1f;

        public QuizHistoryPanelView(VisualElement root, VisualTreeAsset dataStatePanelAsset)
        {
            ResolveRoot(root);
            if (_root == null)
            {
                return;
            }

            CacheElements();
            BindDataStatePanel(dataStatePanelAsset);
            RegisterCallbacks();
            Filters = new QuizHistoryPreviewFilters(
                QuizHistoryPreviewSubjectFilter.All,
                QuizHistoryPreviewTermFilter.All);
            ApplyFilterDropdowns(Filters);
            SetPreviewState(QuizHistoryPreviewState.Content);
            ApplyResponsiveClasses(_root.resolvedStyle.width);
        }

        public VisualElement Root => _root;

        public bool IsBound => _root != null && !_disposed;

        public QuizHistoryPreviewState PreviewState { get; private set; }

        public QuizHistoryPreviewFilters Filters { get; private set; }

        public int LoadedResultCount => _loadedItems.Count;

        public int VisibleResultCount => _visibleItems.Count;

        public event Action BackToQuizPortalRequested;
        public event Action<QuizHistoryPreviewSelection> ViewResultRequested;
        public event Action<QuizHistoryPreviewFilters> FiltersChanged;
        public event Action RetryRequested;

        public void SetItems(IReadOnlyList<QuizHistoryPreviewItem> items)
        {
            if (!IsBound)
            {
                return;
            }

            _warnedInvalidItems = false;
            _loadedItems.Clear();

            if (items != null)
            {
                var seenAttemptIds = new HashSet<string>(StringComparer.Ordinal);
                for (int i = 0; i < items.Count; i++)
                {
                    QuizHistoryPreviewItem item = items[i];
                    if (!TryValidateItem(item, seenAttemptIds, out string warning))
                    {
                        if (!_warnedInvalidItems)
                        {
                            Debug.LogWarning(
                                $"[QuizHistoryPanelView] Skipping invalid history item(s). {warning}");
                            _warnedInvalidItems = true;
                        }

                        continue;
                    }

                    seenAttemptIds.Add(item.AttemptId);
                    _loadedItems.Add(item);
                }
            }

            RefreshOverviewMetrics();
            RebuildVisibleResults(raiseFiltersChanged: false);
        }

        public void SetFilters(QuizHistoryPreviewFilters filters)
        {
            if (!IsBound)
            {
                return;
            }

            ApplyFilters(filters, raiseEvent: false);
        }

        public void ResetFilters()
        {
            if (!IsBound)
            {
                return;
            }

            var defaults = new QuizHistoryPreviewFilters(
                QuizHistoryPreviewSubjectFilter.All,
                QuizHistoryPreviewTermFilter.All);

            bool changed = Filters.Subject != defaults.Subject || Filters.Term != defaults.Term;
            ApplyFilters(defaults, raiseEvent: changed);
        }

        public void SetPreviewState(QuizHistoryPreviewState state)
        {
            if (!IsBound)
            {
                return;
            }

            if (state != QuizHistoryPreviewState.Content
                && state != QuizHistoryPreviewState.OfflineCached
                && (_dataStateView == null || !_dataStateView.IsBound))
            {
                return;
            }

            PreviewState = state;

            switch (state)
            {
                case QuizHistoryPreviewState.Content:
                    ShowContent(showOfflineNotice: false);
                    _dataStateView?.SetState(DataStatePanelState.Content);
                    break;

                case QuizHistoryPreviewState.OfflineCached:
                    ShowContent(showOfflineNotice: true);
                    _dataStateView?.SetState(DataStatePanelState.Content);
                    break;

                case QuizHistoryPreviewState.Loading:
                    HideContent();
                    _dataStateView.SetState(DataStatePanelState.Loading);
                    ApplyRouteDataStateCopy(state);
                    break;

                case QuizHistoryPreviewState.Empty:
                    HideContent();
                    _dataStateView.SetState(DataStatePanelState.Empty);
                    ApplyRouteDataStateCopy(state);
                    break;

                case QuizHistoryPreviewState.RecoverableError:
                    HideContent();
                    _dataStateView.SetState(DataStatePanelState.RecoverableError);
                    ApplyRouteDataStateCopy(state);
                    break;
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
            ClearResultCards();
            DisposeOwnedDataState();

            BackToQuizPortalRequested = null;
            ViewResultRequested = null;
            FiltersChanged = null;
            RetryRequested = null;

            _root = null;
            _contentShell = null;
            _scroll = null;
            _body = null;
            _offlineNotice = null;
            _totalValue = null;
            _passedValue = null;
            _latestScoreValue = null;
            _backButton = null;
            _subjectFilter = null;
            _termFilter = null;
            _resetFiltersButton = null;
            _visibleCountLabel = null;
            _resultsList = null;
            _filterEmpty = null;
            _filterEmptyResetButton = null;
            _dataStateHost = null;
            _lastWidth = -1f;
            _loadedItems.Clear();
            _visibleItems.Clear();
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
            _contentShell = _root.Q<VisualElement>("quiz-history-content-shell");
            _scroll = _root.Q<ScrollView>("quiz-history-scroll");
            _body = _root.Q<VisualElement>("quiz-history-body");
            _offlineNotice = _root.Q<VisualElement>("quiz-history-offline-notice");
            _totalValue = _root.Q<Label>("quiz-history-total-value");
            _passedValue = _root.Q<Label>("quiz-history-passed-value");
            _latestScoreValue = _root.Q<Label>("quiz-history-latest-score-value");
            _backButton = _root.Q<Button>("quiz-history-back-button");
            _subjectFilter = _root.Q<DropdownField>("quiz-history-subject-filter");
            _termFilter = _root.Q<DropdownField>("quiz-history-term-filter");
            _resetFiltersButton = _root.Q<Button>("quiz-history-reset-filters");
            _visibleCountLabel = _root.Q<Label>("quiz-history-visible-count");
            _resultsList = _root.Q<VisualElement>("quiz-history-results-list");
            _filterEmpty = _root.Q<VisualElement>("quiz-history-filter-empty");
            _filterEmptyResetButton = _root.Q<Button>("quiz-history-filter-empty-reset");
            _dataStateHost = _root.Q<VisualElement>("quiz-history-data-state-host");
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
                        "[QuizHistoryPanelView] DataStatePanel VisualTreeAsset is missing. " +
                        "Content and OfflineCached remain usable; non-content states are no-ops.");
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
                    "[QuizHistoryPanelView] Failed to bind DataStatePanelView from the assigned asset.");
                DisposeOwnedDataState();
                return;
            }

            _dataStateView.PrimaryActionRequested += OnDataStatePrimaryAction;
            _dataStateView.SecondaryActionRequested += OnDataStateSecondaryAction;
            _dataStateView.SetVisible(false);
        }

        private void RegisterCallbacks()
        {
            _backClicked = _ => BackToQuizPortalRequested?.Invoke();
            _resetFiltersClicked = _ => ResetFilters();
            _filterEmptyResetClicked = _ => ResetFilters();
            _subjectFilterChanged = OnSubjectFilterChanged;
            _termFilterChanged = OnTermFilterChanged;
            _geometryChanged = OnGeometryChanged;

            _backButton?.RegisterCallback(_backClicked);
            _resetFiltersButton?.RegisterCallback(_resetFiltersClicked);
            _filterEmptyResetButton?.RegisterCallback(_filterEmptyResetClicked);
            _subjectFilter?.RegisterValueChangedCallback(_subjectFilterChanged);
            _termFilter?.RegisterValueChangedCallback(_termFilterChanged);
            _root?.RegisterCallback(_geometryChanged);
        }

        private void UnregisterCallbacks()
        {
            if (_backButton != null && _backClicked != null)
            {
                _backButton.UnregisterCallback(_backClicked);
            }

            if (_resetFiltersButton != null && _resetFiltersClicked != null)
            {
                _resetFiltersButton.UnregisterCallback(_resetFiltersClicked);
            }

            if (_filterEmptyResetButton != null && _filterEmptyResetClicked != null)
            {
                _filterEmptyResetButton.UnregisterCallback(_filterEmptyResetClicked);
            }

            if (_subjectFilter != null && _subjectFilterChanged != null)
            {
                _subjectFilter.UnregisterValueChangedCallback(_subjectFilterChanged);
            }

            if (_termFilter != null && _termFilterChanged != null)
            {
                _termFilter.UnregisterValueChangedCallback(_termFilterChanged);
            }

            if (_root != null && _geometryChanged != null)
            {
                _root.UnregisterCallback(_geometryChanged);
            }

            _backClicked = null;
            _resetFiltersClicked = null;
            _filterEmptyResetClicked = null;
            _subjectFilterChanged = null;
            _termFilterChanged = null;
            _geometryChanged = null;
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

        private void ApplyFilters(QuizHistoryPreviewFilters filters, bool raiseEvent)
        {
            Filters = filters;
            ApplyFilterDropdowns(filters);
            RebuildVisibleResults(raiseFiltersChanged: raiseEvent);
        }

        private void ApplyFilterDropdowns(QuizHistoryPreviewFilters filters)
        {
            _suppressFilterEvents = true;
            try
            {
                _subjectFilter?.SetValueWithoutNotify(GetSubjectFilterChoice(filters.Subject));
                _termFilter?.SetValueWithoutNotify(GetTermFilterChoice(filters.Term));
            }
            finally
            {
                _suppressFilterEvents = false;
            }
        }

        private void RebuildVisibleResults(bool raiseFiltersChanged)
        {
            _visibleItems.Clear();
            for (int i = 0; i < _loadedItems.Count; i++)
            {
                QuizHistoryPreviewItem item = _loadedItems[i];
                if (MatchesFilters(item, Filters))
                {
                    _visibleItems.Add(item);
                }
            }

            if (_visibleCountLabel != null)
            {
                _visibleCountLabel.text = _visibleItems.Count == 1
                    ? "1 result"
                    : $"{_visibleItems.Count} results";
            }

            bool showFilterEmpty = _loadedItems.Count > 0 && _visibleItems.Count == 0;
            _filterEmpty?.EnableInClassList(FilterEmptyHiddenClass, !showFilterEmpty);

            RebuildResultCards();

            if (raiseFiltersChanged)
            {
                FiltersChanged?.Invoke(Filters);
            }
        }

        private void RefreshOverviewMetrics()
        {
            int total = _loadedItems.Count;
            int passed = 0;
            float? latestPercentage = null;
            DateTimeOffset? latestSubmitted = null;

            for (int i = 0; i < _loadedItems.Count; i++)
            {
                QuizResultPreviewContent result = _loadedItems[i].Result;
                if (result.Passed == true)
                {
                    passed++;
                }

                if (!latestSubmitted.HasValue || result.SubmittedAtUtc > latestSubmitted.Value)
                {
                    latestSubmitted = result.SubmittedAtUtc;
                    latestPercentage = result.Percentage;
                }
            }

            if (_totalValue != null)
            {
                _totalValue.text = total.ToString(CultureInfo.InvariantCulture);
            }

            if (_passedValue != null)
            {
                _passedValue.text = passed.ToString(CultureInfo.InvariantCulture);
            }

            if (_latestScoreValue != null)
            {
                _latestScoreValue.text = latestPercentage.HasValue
                    ? FormatPercentage(latestPercentage.Value)
                    : "—";
            }
        }

        private void RebuildResultCards()
        {
            ClearResultCards();

            if (_resultsList == null)
            {
                return;
            }

            for (int i = 0; i < _visibleItems.Count; i++)
            {
                QuizHistoryPreviewItem item = _visibleItems[i];
                VisualElement card = CreateResultCard(item);
                _resultsList.Add(card);
            }
        }

        private VisualElement CreateResultCard(QuizHistoryPreviewItem item)
        {
            var card = new VisualElement();
            card.AddToClassList("ds-card");
            card.AddToClassList("quiz-history-panel__result-card");

            var identity = new VisualElement();
            identity.AddToClassList("quiz-history-panel__result-identity");
            identity.pickingMode = PickingMode.Ignore;

            var icon = new VisualElement();
            icon.AddToClassList("ds-icon");
            icon.AddToClassList(GetSubjectIconClass(item.Summary.Subject));
            icon.AddToClassList("quiz-history-panel__result-icon");
            icon.pickingMode = PickingMode.Ignore;
            identity.Add(icon);

            var title = new Label(item.Summary.Title);
            title.AddToClassList("quiz-history-panel__result-title");
            title.pickingMode = PickingMode.Ignore;
            identity.Add(title);

            var subjectTerm = new Label(
                $"{GetSubjectLabel(item.Summary.Subject)} • Term {(int)item.Summary.Term}");
            subjectTerm.AddToClassList("quiz-history-panel__result-subject-term");
            subjectTerm.pickingMode = PickingMode.Ignore;
            identity.Add(subjectTerm);

            var submitted = new Label($"Submitted {FormatSubmittedAt(item.Result.SubmittedAtUtc)}");
            submitted.AddToClassList("quiz-history-panel__result-submitted");
            submitted.pickingMode = PickingMode.Ignore;
            identity.Add(submitted);

            var chips = new VisualElement();
            chips.AddToClassList("quiz-history-panel__result-chips");
            chips.pickingMode = PickingMode.Ignore;
            chips.Add(CreateChip(GetStatusLabel(item.Result.Status), "ds-icon--check", false));
            chips.Add(CreateChip(GetPassLabel(item.Result.Passed), "ds-icon--trophy", true));
            identity.Add(chips);
            card.Add(identity);

            var score = new VisualElement();
            score.AddToClassList("quiz-history-panel__result-score");
            score.pickingMode = PickingMode.Ignore;

            var percentage = new Label(FormatPercentage(item.Result.Percentage));
            percentage.AddToClassList("quiz-history-panel__result-percentage");
            percentage.pickingMode = PickingMode.Ignore;
            score.Add(percentage);

            var points = new Label(FormatPoints(item.Result.EarnedPoints, item.Result.PossiblePoints));
            points.AddToClassList("quiz-history-panel__result-points");
            points.pickingMode = PickingMode.Ignore;
            score.Add(points);

            var counts = new Label(
                $"{item.Result.CorrectCount} correct • {item.Result.IncorrectCount} incorrect • {item.Result.UnansweredCount} unanswered");
            counts.AddToClassList("quiz-history-panel__result-counts");
            counts.pickingMode = PickingMode.Ignore;
            score.Add(counts);
            card.Add(score);

            var action = new VisualElement();
            action.AddToClassList("quiz-history-panel__result-action");

            var viewButton = new Button { text = "View Result", tooltip = "View Result" };
            viewButton.AddToClassList("ds-btn");
            viewButton.AddToClassList("ds-btn--primary");
            viewButton.AddToClassList("quiz-history-panel__view-result-button");

            var binding = new ResultCardBinding
            {
                Button = viewButton,
                AttemptId = item.AttemptId,
                Summary = item.Summary
            };
            binding.Callback = evt => OnViewResultClicked(binding);
            viewButton.RegisterCallback(binding.Callback);
            _cardBindings.Add(binding);

            action.Add(viewButton);
            card.Add(action);
            return card;
        }

        private static VisualElement CreateChip(string label, string iconClass, bool isPass)
        {
            var chip = new VisualElement();
            chip.AddToClassList("ds-chip");
            chip.AddToClassList("quiz-history-panel__result-chip");
            if (isPass)
            {
                chip.AddToClassList("quiz-history-panel__result-chip--pass");
            }

            chip.pickingMode = PickingMode.Ignore;

            var icon = new VisualElement();
            icon.AddToClassList("ds-icon");
            icon.AddToClassList(iconClass);
            icon.AddToClassList("quiz-history-panel__result-chip-icon");
            icon.pickingMode = PickingMode.Ignore;
            chip.Add(icon);

            var text = new Label(label);
            text.AddToClassList("quiz-history-panel__result-chip-label");
            text.pickingMode = PickingMode.Ignore;
            chip.Add(text);
            return chip;
        }

        private void ClearResultCards()
        {
            for (int i = 0; i < _cardBindings.Count; i++)
            {
                ResultCardBinding binding = _cardBindings[i];
                if (binding.Button != null && binding.Callback != null)
                {
                    binding.Button.UnregisterCallback(binding.Callback);
                }
            }

            _cardBindings.Clear();
            _resultsList?.Clear();
        }

        private void OnViewResultClicked(ResultCardBinding binding)
        {
            if (binding == null || string.IsNullOrWhiteSpace(binding.AttemptId))
            {
                return;
            }

            ViewResultRequested?.Invoke(
                new QuizHistoryPreviewSelection(binding.AttemptId, binding.Summary));
        }

        private void OnSubjectFilterChanged(ChangeEvent<string> evt)
        {
            if (_suppressFilterEvents)
            {
                return;
            }

            ApplyFilters(
                new QuizHistoryPreviewFilters(ParseSubjectFilter(evt.newValue), Filters.Term),
                raiseEvent: true);
        }

        private void OnTermFilterChanged(ChangeEvent<string> evt)
        {
            if (_suppressFilterEvents)
            {
                return;
            }

            ApplyFilters(
                new QuizHistoryPreviewFilters(Filters.Subject, ParseTermFilter(evt.newValue)),
                raiseEvent: true);
        }

        private void OnDataStatePrimaryAction()
        {
            switch (PreviewState)
            {
                case QuizHistoryPreviewState.Empty:
                    BackToQuizPortalRequested?.Invoke();
                    break;

                case QuizHistoryPreviewState.RecoverableError:
                    RetryRequested?.Invoke();
                    break;
            }
        }

        private void OnDataStateSecondaryAction()
        {
            if (PreviewState == QuizHistoryPreviewState.RecoverableError)
            {
                BackToQuizPortalRequested?.Invoke();
            }
        }

        private void ShowContent(bool showOfflineNotice)
        {
            _contentShell?.RemoveFromClassList(ContentShellHiddenClass);
            if (_scroll != null)
            {
                _scroll.style.display = DisplayStyle.Flex;
            }

            _dataStateHost?.RemoveFromClassList(DataStateHostVisibleClass);
            _offlineNotice?.EnableInClassList(OfflineNoticeHiddenClass, !showOfflineNotice);
        }

        private void HideContent()
        {
            _contentShell?.AddToClassList(ContentShellHiddenClass);
            if (_scroll != null)
            {
                _scroll.style.display = DisplayStyle.None;
            }

            _dataStateHost?.AddToClassList(DataStateHostVisibleClass);
            _offlineNotice?.EnableInClassList(OfflineNoticeHiddenClass, true);
        }

        private void ApplyRouteDataStateCopy(QuizHistoryPreviewState state)
        {
            if (_dataStateView == null || !_dataStateView.IsBound)
            {
                return;
            }

            switch (state)
            {
                case QuizHistoryPreviewState.Loading:
                    _dataStateView.Configure(
                        title: "Loading quiz history",
                        message: "Getting your completed Quiz Portal results.",
                        detail: "Your scores are provided by the NutriMind server.",
                        iconClass: null,
                        primaryActionLabel: string.Empty,
                        secondaryActionLabel: string.Empty);
                    break;

                case QuizHistoryPreviewState.Empty:
                    // ds-icon--book: DataStatePanel whitelist does not include ds-icon--list.
                    _dataStateView.Configure(
                        title: "No quiz results yet",
                        message: "Completed Quiz Portal results will appear here.",
                        detail: "Return to Quiz Portal to view available assignments.",
                        iconClass: "ds-icon--book",
                        primaryActionLabel: "Back to Quiz Portal",
                        secondaryActionLabel: string.Empty);
                    break;

                case QuizHistoryPreviewState.RecoverableError:
                    _dataStateView.Configure(
                        title: "Quiz history could not be loaded",
                        message: "Check your connection and try again.",
                        detail: "Previously submitted attempts are not changed by this screen.",
                        iconClass: "ds-icon--error",
                        primaryActionLabel: "Try Again",
                        secondaryActionLabel: "Back to Quiz Portal");
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
            _root.EnableInClassList(CompactScreenClass, compact);
            _root.EnableInClassList(NarrowScreenClass, narrow);
        }

        private static bool TryValidateItem(
            QuizHistoryPreviewItem item,
            HashSet<string> seenAttemptIds,
            out string warning)
        {
            if (item == null)
            {
                warning = "Item was null.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(item.AttemptId))
            {
                warning = "Attempt ID was empty.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(item.Summary.Id))
            {
                warning = "Summary quiz ID was empty.";
                return false;
            }

            if (item.Result == null)
            {
                warning = "Result content was null.";
                return false;
            }

            if (!string.Equals(item.Summary.Id, item.Result.QuizId, StringComparison.Ordinal))
            {
                warning = "Summary quiz ID did not match result quiz ID.";
                return false;
            }

            if (!string.Equals(item.Result.AttemptId, item.AttemptId, StringComparison.Ordinal))
            {
                warning = "Result attempt ID did not match history item attempt ID.";
                return false;
            }

            if (seenAttemptIds.Contains(item.AttemptId))
            {
                warning = "Duplicate attempt ID.";
                return false;
            }

            if (item.Summary.Subject != NutriMindSubject.LiteraQuest
                && item.Summary.Subject != NutriMindSubject.PeAndHealth
                && item.Summary.Subject != NutriMindSubject.Science)
            {
                warning = "Unsupported subject.";
                return false;
            }

            if (item.Summary.Term != NutriMindTerm.Term1
                && item.Summary.Term != NutriMindTerm.Term2
                && item.Summary.Term != NutriMindTerm.Term3)
            {
                warning = "Unsupported term.";
                return false;
            }

            if (item.Result.Percentage < 0f || item.Result.Percentage > 100f)
            {
                warning = "Percentage out of range.";
                return false;
            }

            if (item.Result.EarnedPoints < 0f || item.Result.PossiblePoints < 0f)
            {
                warning = "Points were negative.";
                return false;
            }

            if (item.Result.EarnedPoints > item.Result.PossiblePoints)
            {
                warning = "Earned points exceeded possible points.";
                return false;
            }

            if (item.Result.CorrectCount < 0
                || item.Result.IncorrectCount < 0
                || item.Result.UnansweredCount < 0)
            {
                warning = "Counts were negative.";
                return false;
            }

            warning = null;
            return true;
        }

        private static bool MatchesFilters(
            QuizHistoryPreviewItem item,
            QuizHistoryPreviewFilters filters)
        {
            if (filters.Subject != QuizHistoryPreviewSubjectFilter.All
                && MapSubjectFilter(item.Summary.Subject) != filters.Subject)
            {
                return false;
            }

            if (filters.Term != QuizHistoryPreviewTermFilter.All
                && MapTermFilter(item.Summary.Term) != filters.Term)
            {
                return false;
            }

            return true;
        }

        private static QuizHistoryPreviewSubjectFilter MapSubjectFilter(NutriMindSubject subject) =>
            subject switch
            {
                NutriMindSubject.LiteraQuest => QuizHistoryPreviewSubjectFilter.LiteraQuest,
                NutriMindSubject.PeAndHealth => QuizHistoryPreviewSubjectFilter.PeAndHealth,
                _ => QuizHistoryPreviewSubjectFilter.Science
            };

        private static QuizHistoryPreviewTermFilter MapTermFilter(NutriMindTerm term) =>
            term switch
            {
                NutriMindTerm.Term1 => QuizHistoryPreviewTermFilter.Term1,
                NutriMindTerm.Term2 => QuizHistoryPreviewTermFilter.Term2,
                _ => QuizHistoryPreviewTermFilter.Term3
            };

        private static string GetSubjectFilterChoice(QuizHistoryPreviewSubjectFilter filter) =>
            filter switch
            {
                QuizHistoryPreviewSubjectFilter.LiteraQuest => "LiteraQuest",
                QuizHistoryPreviewSubjectFilter.PeAndHealth => "PE & Health",
                QuizHistoryPreviewSubjectFilter.Science => "Science",
                _ => "All subjects"
            };

        private static string GetTermFilterChoice(QuizHistoryPreviewTermFilter filter) =>
            filter switch
            {
                QuizHistoryPreviewTermFilter.Term1 => "Term 1",
                QuizHistoryPreviewTermFilter.Term2 => "Term 2",
                QuizHistoryPreviewTermFilter.Term3 => "Term 3",
                _ => "All terms"
            };

        private static QuizHistoryPreviewSubjectFilter ParseSubjectFilter(string value) =>
            value switch
            {
                "LiteraQuest" => QuizHistoryPreviewSubjectFilter.LiteraQuest,
                "PE & Health" => QuizHistoryPreviewSubjectFilter.PeAndHealth,
                "Science" => QuizHistoryPreviewSubjectFilter.Science,
                _ => QuizHistoryPreviewSubjectFilter.All
            };

        private static QuizHistoryPreviewTermFilter ParseTermFilter(string value) =>
            value switch
            {
                "Term 1" => QuizHistoryPreviewTermFilter.Term1,
                "Term 2" => QuizHistoryPreviewTermFilter.Term2,
                "Term 3" => QuizHistoryPreviewTermFilter.Term3,
                _ => QuizHistoryPreviewTermFilter.All
            };

        private static string GetSubjectLabel(NutriMindSubject subject) =>
            subject switch
            {
                NutriMindSubject.LiteraQuest => "LiteraQuest",
                NutriMindSubject.PeAndHealth => "PE & Health",
                NutriMindSubject.Science => "Science",
                _ => subject.ToString()
            };

        private static string GetSubjectIconClass(NutriMindSubject subject) =>
            subject switch
            {
                NutriMindSubject.LiteraQuest => "ds-icon--book",
                NutriMindSubject.PeAndHealth => "ds-icon--bolt",
                NutriMindSubject.Science => "ds-icon--potion",
                _ => "ds-icon--trophy"
            };

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

        private static string FormatPercentage(float percentage)
        {
            float rounded = Mathf.Round(percentage);
            return $"{rounded.ToString("0", CultureInfo.InvariantCulture)}%";
        }

        private static string FormatPoints(float earned, float possible)
        {
            string earnedText = FormatPointsNumber(earned);
            string possibleText = FormatPointsNumber(possible);
            return $"{earnedText} of {possibleText} points";
        }

        private static string FormatPointsNumber(float value)
        {
            if (Mathf.Approximately(value, Mathf.Round(value)))
            {
                return Mathf.Round(value).ToString("0", CultureInfo.InvariantCulture);
            }

            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        private static string FormatSubmittedAt(DateTimeOffset submittedAtUtc)
        {
            DateTime utc = submittedAtUtc.UtcDateTime;
            return utc.ToString("MMM d, yyyy", CultureInfo.InvariantCulture)
                + " • "
                + utc.ToString("h:mm tt", CultureInfo.InvariantCulture)
                + " UTC";
        }
    }
}
