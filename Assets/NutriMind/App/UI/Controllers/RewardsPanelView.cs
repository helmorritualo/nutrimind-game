using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace NutriMind.App.UI
{
    /// <summary>
    /// Presentation-only Rewards route states for static preview.
    /// </summary>
    public enum RewardsPreviewState
    {
        Content = 0,
        Loading = 1,
        Empty = 2,
        OfflineCached = 3,
        RecoverableError = 4
    }

    /// <summary>
    /// Presentation-only reward status categories for the Rewards static preview.
    /// Not a Student API enum.
    /// </summary>
    public enum RewardsPreviewItemStatus
    {
        Owned = 0,
        Available = 1,
        Used = 2,
        Locked = 3
    }

    /// <summary>
    /// Local status filter for the Rewards static preview.
    /// </summary>
    public enum RewardsPreviewFilter
    {
        All = 0,
        Owned = 1,
        Available = 2,
        Used = 3,
        Locked = 4
    }

    /// <summary>
    /// UI preview only. Not an API DTO.
    /// PresentationKey is not a canonical rewardCode.
    /// Static UI preview examples only.
    /// Not the Student API reward DTO.
    /// Replace when the reward item contract is defined.
    /// </summary>
    public sealed class RewardsPreviewItem
    {
        public RewardsPreviewItem(
            string presentationKey,
            string title,
            string description,
            string supportingText,
            RewardsPreviewItemStatus status,
            string lockedReason,
            string iconClass)
        {
            PresentationKey = presentationKey ?? string.Empty;
            Title = title ?? string.Empty;
            Description = description ?? string.Empty;
            SupportingText = supportingText ?? string.Empty;
            Status = status;
            LockedReason = lockedReason ?? string.Empty;
            IconClass = iconClass ?? string.Empty;
        }

        public string PresentationKey { get; }
        public string Title { get; }
        public string Description { get; }
        public string SupportingText { get; }
        public RewardsPreviewItemStatus Status { get; }
        public string LockedReason { get; }
        public string IconClass { get; }
    }

    /// <summary>
    /// UI preview only. Not an API DTO.
    /// PresentationKey is not a canonical rewardCode.
    /// </summary>
    public readonly struct RewardsPreviewSelection
    {
        public RewardsPreviewSelection(string presentationKey, string title)
        {
            PresentationKey = presentationKey ?? string.Empty;
            Title = title ?? string.Empty;
        }

        public string PresentationKey { get; }
        public string Title { get; }
    }

    /// <summary>
    /// Deterministic static-preview catalog for Rewards.
    /// Static UI preview examples only. Not the Student API reward DTO.
    /// Replace when the reward item contract is defined.
    /// </summary>
    public static class RewardsPreviewCatalog
    {
        /// <summary>
        /// Builds exactly four presentation-only reward examples.
        /// Source-derived descriptions are hardcoded for static presentation.
        /// </summary>
        public static IReadOnlyList<RewardsPreviewItem> CreateItems()
        {
            return new RewardsPreviewItem[]
            {
                new(
                    "preview_festival_chapter",
                    "Festival Chapter",
                    "Three Story Fragments form the Festival Chapter and unlock Mission 2.",
                    "Collected from The Festival Storybook Rescue.",
                    RewardsPreviewItemStatus.Owned,
                    string.Empty,
                    "ds-icon--book"),
                new(
                    "preview_bell_keeper_badge",
                    "Bell Keeper Badge",
                    "Seven-Moment Story Fragment set and the Bell Keeper badge.",
                    "Ready to use in this static preview.",
                    RewardsPreviewItemStatus.Available,
                    string.Empty,
                    "ds-icon--medal"),
                new(
                    "preview_voice_image_fragment_set",
                    "Voice-and-Image Story Fragment Set",
                    "Voice-and-Image Story Fragment set.",
                    "Already used.",
                    RewardsPreviewItemStatus.Used,
                    string.Empty,
                    "ds-icon--sparkle"),
                new(
                    "preview_term1_chronicle_emblem",
                    "Term 1 Chronicle Emblem",
                    "Term 1 Chronicle Emblem and access to Term 2.",
                    "Complete The Grand Holiday Chronicle.",
                    RewardsPreviewItemStatus.Locked,
                    "Complete The Grand Holiday Chronicle.",
                    "ds-icon--trophy")
            };
        }
    }

    /// <summary>
    /// Presentation-only Rewards route view. Binds local preview fixtures and
    /// raises user intent for the host to handle.
    /// Does not call APIs, generate request UUIDs, mutate reward state, or persist.
    /// </summary>
    public sealed class RewardsPanelView : IAppScreenView
    {
        private const string RootName = "rewards-root";
        private const string CompactClass = "rewards-panel--compact";
        private const string NarrowClass = "rewards-panel--narrow";
        private const string MobileClass = "mobile";
        private const string DataStateHostVisibleClass = "rewards-panel__data-state-host--visible";
        private const string ContentShellHiddenClass = "rewards-panel__content-shell--hidden";
        private const string OfflineNoticeHiddenClass = "rewards-panel__offline-notice--hidden";
        private const string FilterEmptyHiddenClass = "rewards-panel__filter-empty--hidden";
        private const string FilterSelectedClass = "rewards-panel__filter-button--selected";
        private const string CompactScreenClass = "app-screen-content--compact";
        private const string NarrowScreenClass = "app-screen-content--narrow";
        private const float CompactBreakpoint = 1100f;
        private const float NarrowBreakpoint = 820f;

        private sealed class RewardCardBinding
        {
            public Button UseButton { get; set; }
            public EventCallback<ClickEvent> Callback { get; set; }
            public RewardsPreviewItem Item { get; set; }
        }

        private VisualElement _root;
        private VisualElement _contentShell;
        private ScrollView _scroll;
        private VisualElement _body;
        private VisualElement _offlineNotice;
        private Label _ownedValue;
        private Label _availableValue;
        private Label _usedValue;
        private Label _lockedValue;
        private Button _filterAll;
        private Button _filterOwned;
        private Button _filterAvailable;
        private Button _filterUsed;
        private Button _filterLocked;
        private Label _visibleCountLabel;
        private VisualElement _grid;
        private VisualElement _filterEmpty;
        private Button _filterEmptyReset;
        private VisualElement _dataStateHost;

        private TemplateContainer _ownedDataStateInstance;
        private DataStatePanelView _dataStateView;
        private bool _warnedMissingDataStateAsset;
        private bool _warnedInvalidItems;

        private readonly List<RewardsPreviewItem> _loadedItems = new();
        private readonly List<RewardsPreviewItem> _visibleItems = new();
        private readonly List<RewardCardBinding> _cardBindings = new();

        private EventCallback<ClickEvent> _filterAllClicked;
        private EventCallback<ClickEvent> _filterOwnedClicked;
        private EventCallback<ClickEvent> _filterAvailableClicked;
        private EventCallback<ClickEvent> _filterUsedClicked;
        private EventCallback<ClickEvent> _filterLockedClicked;
        private EventCallback<ClickEvent> _filterEmptyResetClicked;
        private EventCallback<GeometryChangedEvent> _geometryChanged;
        private bool _disposed;
        private float _lastWidth = -1f;

        public RewardsPanelView(VisualElement root, VisualTreeAsset dataStatePanelAsset)
        {
            ResolveRoot(root);
            if (_root == null)
            {
                return;
            }

            CacheElements();
            BindDataStatePanel(dataStatePanelAsset);
            RegisterCallbacks();
            Filter = RewardsPreviewFilter.All;
            ApplyFilterButtonSelection(Filter);
            SetPreviewState(RewardsPreviewState.Content);
            ApplyResponsiveClasses(_root.resolvedStyle.width);
        }

        public VisualElement Root => _root;
        public bool IsBound => _root != null && !_disposed;
        public RewardsPreviewState PreviewState { get; private set; }
        public RewardsPreviewFilter Filter { get; private set; }
        public int LoadedRewardCount => _loadedItems.Count;
        public int VisibleRewardCount => _visibleItems.Count;

        public event Action BackToHomeRequested;
        public event Action<RewardsPreviewSelection> UseRewardRequested;
        public event Action<RewardsPreviewFilter> FilterChanged;
        public event Action RetryRequested;

        public void SetItems(IReadOnlyList<RewardsPreviewItem> items)
        {
            if (!IsBound)
            {
                return;
            }

            _warnedInvalidItems = false;
            _loadedItems.Clear();

            if (items != null)
            {
                var seenKeys = new HashSet<string>(StringComparer.Ordinal);
                for (int i = 0; i < items.Count; i++)
                {
                    RewardsPreviewItem item = items[i];
                    if (!TryValidateItem(item, seenKeys, out string warning))
                    {
                        if (!_warnedInvalidItems)
                        {
                            Debug.LogWarning(
                                $"[RewardsPanelView] Skipping invalid reward item(s). {warning}");
                            _warnedInvalidItems = true;
                        }

                        continue;
                    }

                    seenKeys.Add(item.PresentationKey);
                    _loadedItems.Add(item);
                }
            }

            RefreshOverviewMetrics();
            RebuildVisibleRewards(raiseFilterChanged: false);
        }

        public void SetFilter(RewardsPreviewFilter filter)
        {
            if (!IsBound)
            {
                return;
            }

            ApplyFilter(filter, raiseEvent: false);
        }

        public void ResetFilter()
        {
            if (!IsBound)
            {
                return;
            }

            bool changed = Filter != RewardsPreviewFilter.All;
            ApplyFilter(RewardsPreviewFilter.All, raiseEvent: changed);
        }

        public void SetPreviewState(RewardsPreviewState state)
        {
            if (!IsBound)
            {
                return;
            }

            if (state != RewardsPreviewState.Content
                && state != RewardsPreviewState.OfflineCached
                && (_dataStateView == null || !_dataStateView.IsBound))
            {
                return;
            }

            PreviewState = state;

            switch (state)
            {
                case RewardsPreviewState.Content:
                    ShowContent(showOfflineNotice: false);
                    _dataStateView?.SetState(DataStatePanelState.Content);
                    RebuildRewardCards();
                    break;

                case RewardsPreviewState.OfflineCached:
                    ShowContent(showOfflineNotice: true);
                    _dataStateView?.SetState(DataStatePanelState.Content);
                    RebuildRewardCards();
                    break;

                case RewardsPreviewState.Loading:
                    HideContent();
                    _dataStateView.SetState(DataStatePanelState.Loading);
                    ApplyRouteDataStateCopy(state);
                    break;

                case RewardsPreviewState.Empty:
                    HideContent();
                    _dataStateView.SetState(DataStatePanelState.Empty);
                    ApplyRouteDataStateCopy(state);
                    break;

                case RewardsPreviewState.RecoverableError:
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
            ClearRewardCards();
            DisposeOwnedDataState();

            BackToHomeRequested = null;
            UseRewardRequested = null;
            FilterChanged = null;
            RetryRequested = null;

            _root = null;
            _contentShell = null;
            _scroll = null;
            _body = null;
            _offlineNotice = null;
            _ownedValue = null;
            _availableValue = null;
            _usedValue = null;
            _lockedValue = null;
            _filterAll = null;
            _filterOwned = null;
            _filterAvailable = null;
            _filterUsed = null;
            _filterLocked = null;
            _visibleCountLabel = null;
            _grid = null;
            _filterEmpty = null;
            _filterEmptyReset = null;
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
            _contentShell = _root.Q<VisualElement>("rewards-content-shell");
            _scroll = _root.Q<ScrollView>("rewards-scroll");
            _body = _root.Q<VisualElement>("rewards-body");
            _offlineNotice = _root.Q<VisualElement>("rewards-offline-notice");
            _ownedValue = _root.Q<Label>("rewards-owned-value");
            _availableValue = _root.Q<Label>("rewards-available-value");
            _usedValue = _root.Q<Label>("rewards-used-value");
            _lockedValue = _root.Q<Label>("rewards-locked-value");
            _filterAll = _root.Q<Button>("rewards-filter-all");
            _filterOwned = _root.Q<Button>("rewards-filter-owned");
            _filterAvailable = _root.Q<Button>("rewards-filter-available");
            _filterUsed = _root.Q<Button>("rewards-filter-used");
            _filterLocked = _root.Q<Button>("rewards-filter-locked");
            _visibleCountLabel = _root.Q<Label>("rewards-visible-count");
            _grid = _root.Q<VisualElement>("rewards-grid");
            _filterEmpty = _root.Q<VisualElement>("rewards-filter-empty");
            _filterEmptyReset = _root.Q<Button>("rewards-filter-empty-reset");
            _dataStateHost = _root.Q<VisualElement>("rewards-data-state-host");
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
                        "[RewardsPanelView] DataStatePanel VisualTreeAsset is missing. " +
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
                    "[RewardsPanelView] Failed to bind DataStatePanelView from the assigned asset.");
                DisposeOwnedDataState();
                return;
            }

            _dataStateView.PrimaryActionRequested += OnDataStatePrimaryAction;
            _dataStateView.SecondaryActionRequested += OnDataStateSecondaryAction;
            _dataStateView.SetVisible(false);
        }

        private void RegisterCallbacks()
        {
            _filterAllClicked = _ => ApplyFilter(RewardsPreviewFilter.All, raiseEvent: true);
            _filterOwnedClicked = _ => ApplyFilter(RewardsPreviewFilter.Owned, raiseEvent: true);
            _filterAvailableClicked = _ => ApplyFilter(RewardsPreviewFilter.Available, raiseEvent: true);
            _filterUsedClicked = _ => ApplyFilter(RewardsPreviewFilter.Used, raiseEvent: true);
            _filterLockedClicked = _ => ApplyFilter(RewardsPreviewFilter.Locked, raiseEvent: true);
            _filterEmptyResetClicked = _ => ResetFilter();
            _geometryChanged = OnGeometryChanged;

            _filterAll?.RegisterCallback(_filterAllClicked);
            _filterOwned?.RegisterCallback(_filterOwnedClicked);
            _filterAvailable?.RegisterCallback(_filterAvailableClicked);
            _filterUsed?.RegisterCallback(_filterUsedClicked);
            _filterLocked?.RegisterCallback(_filterLockedClicked);
            _filterEmptyReset?.RegisterCallback(_filterEmptyResetClicked);
            _root?.RegisterCallback(_geometryChanged);
        }

        private void UnregisterCallbacks()
        {
            if (_filterAll != null && _filterAllClicked != null)
            {
                _filterAll.UnregisterCallback(_filterAllClicked);
            }

            if (_filterOwned != null && _filterOwnedClicked != null)
            {
                _filterOwned.UnregisterCallback(_filterOwnedClicked);
            }

            if (_filterAvailable != null && _filterAvailableClicked != null)
            {
                _filterAvailable.UnregisterCallback(_filterAvailableClicked);
            }

            if (_filterUsed != null && _filterUsedClicked != null)
            {
                _filterUsed.UnregisterCallback(_filterUsedClicked);
            }

            if (_filterLocked != null && _filterLockedClicked != null)
            {
                _filterLocked.UnregisterCallback(_filterLockedClicked);
            }

            if (_filterEmptyReset != null && _filterEmptyResetClicked != null)
            {
                _filterEmptyReset.UnregisterCallback(_filterEmptyResetClicked);
            }

            if (_root != null && _geometryChanged != null)
            {
                _root.UnregisterCallback(_geometryChanged);
            }

            _filterAllClicked = null;
            _filterOwnedClicked = null;
            _filterAvailableClicked = null;
            _filterUsedClicked = null;
            _filterLockedClicked = null;
            _filterEmptyResetClicked = null;
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

        private void ApplyFilter(RewardsPreviewFilter filter, bool raiseEvent)
        {
            bool changed = Filter != filter;
            Filter = filter;
            ApplyFilterButtonSelection(filter);
            RebuildVisibleRewards(raiseFilterChanged: raiseEvent && changed);
        }

        private void ApplyFilterButtonSelection(RewardsPreviewFilter filter)
        {
            SetFilterButtonSelected(_filterAll, filter == RewardsPreviewFilter.All);
            SetFilterButtonSelected(_filterOwned, filter == RewardsPreviewFilter.Owned);
            SetFilterButtonSelected(_filterAvailable, filter == RewardsPreviewFilter.Available);
            SetFilterButtonSelected(_filterUsed, filter == RewardsPreviewFilter.Used);
            SetFilterButtonSelected(_filterLocked, filter == RewardsPreviewFilter.Locked);
        }

        private static void SetFilterButtonSelected(Button button, bool selected)
        {
            if (button == null)
            {
                return;
            }

            button.EnableInClassList(FilterSelectedClass, selected);
            button.EnableInClassList("ds-btn--secondary", selected);
            button.EnableInClassList("ds-btn--ghost", !selected);
        }

        private void RebuildVisibleRewards(bool raiseFilterChanged)
        {
            _visibleItems.Clear();
            for (int i = 0; i < _loadedItems.Count; i++)
            {
                RewardsPreviewItem item = _loadedItems[i];
                if (MatchesFilter(item, Filter))
                {
                    _visibleItems.Add(item);
                }
            }

            if (_visibleCountLabel != null)
            {
                _visibleCountLabel.text = _visibleItems.Count == 1
                    ? "1 reward"
                    : $"{_visibleItems.Count} rewards";
            }

            bool showFilterEmpty = _loadedItems.Count > 0 && _visibleItems.Count == 0;
            _filterEmpty?.EnableInClassList(FilterEmptyHiddenClass, !showFilterEmpty);

            RebuildRewardCards();

            if (raiseFilterChanged)
            {
                FilterChanged?.Invoke(Filter);
            }
        }

        private void RefreshOverviewMetrics()
        {
            int owned = 0;
            int available = 0;
            int used = 0;
            int locked = 0;

            for (int i = 0; i < _loadedItems.Count; i++)
            {
                switch (_loadedItems[i].Status)
                {
                    case RewardsPreviewItemStatus.Owned:
                        owned++;
                        break;
                    case RewardsPreviewItemStatus.Available:
                        available++;
                        break;
                    case RewardsPreviewItemStatus.Used:
                        used++;
                        break;
                    case RewardsPreviewItemStatus.Locked:
                        locked++;
                        break;
                }
            }

            if (_ownedValue != null) _ownedValue.text = owned.ToString();
            if (_availableValue != null) _availableValue.text = available.ToString();
            if (_usedValue != null) _usedValue.text = used.ToString();
            if (_lockedValue != null) _lockedValue.text = locked.ToString();
        }

        private void RebuildRewardCards()
        {
            ClearRewardCards();
            if (_grid == null)
            {
                return;
            }

            bool offline = PreviewState == RewardsPreviewState.OfflineCached;
            for (int i = 0; i < _visibleItems.Count; i++)
            {
                RewardsPreviewItem item = _visibleItems[i];
                bool lastInRow = i % 2 == 1 || i == _visibleItems.Count - 1;
                _grid.Add(CreateRewardCard(item, offline, lastInRow));
            }
        }

        private VisualElement CreateRewardCard(RewardsPreviewItem item, bool offline, bool lastInRow)
        {
            var card = new VisualElement();
            card.AddToClassList("ds-card");
            card.AddToClassList("rewards-panel__card");
            card.AddToClassList(GetCardStatusClass(item.Status));
            if (lastInRow)
            {
                card.AddToClassList("rewards-panel__card--last-in-row");
            }

            var header = new VisualElement();
            header.AddToClassList("rewards-panel__card-header");
            header.pickingMode = PickingMode.Ignore;

            var icon = new VisualElement();
            icon.AddToClassList("ds-icon");
            icon.AddToClassList(item.IconClass);
            icon.AddToClassList("rewards-panel__card-icon");
            icon.pickingMode = PickingMode.Ignore;

            var chip = new VisualElement();
            chip.AddToClassList("ds-chip");
            chip.AddToClassList("rewards-panel__status-chip");
            chip.AddToClassList(GetStatusChipClass(item.Status));
            chip.pickingMode = PickingMode.Ignore;

            var chipIcon = new VisualElement();
            chipIcon.AddToClassList("ds-icon");
            chipIcon.AddToClassList(GetStatusIconClass(item.Status));
            chipIcon.AddToClassList("rewards-panel__status-chip-icon");
            chipIcon.pickingMode = PickingMode.Ignore;

            var chipLabel = new Label(GetStatusLabel(item.Status));
            chipLabel.AddToClassList("rewards-panel__status-chip-label");
            chipLabel.pickingMode = PickingMode.Ignore;

            chip.Add(chipIcon);
            chip.Add(chipLabel);
            header.Add(icon);
            header.Add(chip);

            var title = new Label(item.Title);
            title.AddToClassList("rewards-panel__card-title");
            title.pickingMode = PickingMode.Ignore;

            var description = new Label(item.Description);
            description.AddToClassList("rewards-panel__card-description");
            description.pickingMode = PickingMode.Ignore;

            card.Add(header);
            card.Add(title);
            card.Add(description);

            if (item.Status == RewardsPreviewItemStatus.Locked)
            {
                var reason = new Label(item.LockedReason);
                reason.AddToClassList("rewards-panel__card-locked-reason");
                reason.pickingMode = PickingMode.Ignore;
                card.Add(reason);
            }
            else
            {
                string supportText = item.Status == RewardsPreviewItemStatus.Available && offline
                    ? "Connect to use this reward."
                    : item.SupportingText;
                var support = new Label(supportText);
                support.AddToClassList("rewards-panel__card-support");
                support.pickingMode = PickingMode.Ignore;
                card.Add(support);
            }

            if (item.Status == RewardsPreviewItemStatus.Available)
            {
                var useButton = new Button { text = "Use Reward" };
                useButton.AddToClassList("ds-btn");
                useButton.AddToClassList("ds-btn--primary");
                useButton.AddToClassList("rewards-panel__card-action");
                useButton.tooltip = offline
                    ? "Connect to use this reward."
                    : "Use this reward (preview intent only)";
                useButton.SetEnabled(!offline);

                var binding = new RewardCardBinding
                {
                    UseButton = useButton,
                    Item = item
                };
                binding.Callback = evt => OnUseRewardClicked(binding);
                useButton.RegisterCallback(binding.Callback);
                _cardBindings.Add(binding);
                card.Add(useButton);
            }

            return card;
        }

        private void OnUseRewardClicked(RewardCardBinding binding)
        {
            if (_disposed
                || binding?.Item == null
                || PreviewState != RewardsPreviewState.Content
                || binding.Item.Status != RewardsPreviewItemStatus.Available)
            {
                return;
            }

            UseRewardRequested?.Invoke(
                new RewardsPreviewSelection(binding.Item.PresentationKey, binding.Item.Title));
        }

        private void ClearRewardCards()
        {
            for (int i = 0; i < _cardBindings.Count; i++)
            {
                RewardCardBinding binding = _cardBindings[i];
                if (binding.UseButton != null && binding.Callback != null)
                {
                    binding.UseButton.UnregisterCallback(binding.Callback);
                }
            }

            _cardBindings.Clear();
            _grid?.Clear();
        }

        private void ShowContent(bool showOfflineNotice)
        {
            _contentShell?.RemoveFromClassList(ContentShellHiddenClass);
            if (_scroll != null)
            {
                _scroll.style.display = DisplayStyle.Flex;
            }

            _offlineNotice?.EnableInClassList(OfflineNoticeHiddenClass, !showOfflineNotice);
            _dataStateHost?.RemoveFromClassList(DataStateHostVisibleClass);
            _dataStateView?.SetVisible(false);
        }

        private void HideContent()
        {
            _contentShell?.AddToClassList(ContentShellHiddenClass);
            if (_scroll != null)
            {
                _scroll.style.display = DisplayStyle.None;
            }

            _offlineNotice?.AddToClassList(OfflineNoticeHiddenClass);
            _dataStateHost?.AddToClassList(DataStateHostVisibleClass);
            _dataStateView?.SetVisible(true);
        }

        private void ApplyRouteDataStateCopy(RewardsPreviewState state)
        {
            if (_dataStateView == null || !_dataStateView.IsBound)
            {
                return;
            }

            switch (state)
            {
                case RewardsPreviewState.Loading:
                    _dataStateView.Configure(
                        new DataStatePanelConfiguration(
                            "Loading rewards",
                            "Getting your latest reward collection.",
                            "Your reward status is provided by the NutriMind server.",
                            null,
                            string.Empty,
                            string.Empty,
                            true));
                    break;

                case RewardsPreviewState.Empty:
                    _dataStateView.Configure(
                        "No rewards yet",
                        "Rewards you earn through NutriMind will appear here.",
                        "Complete available learning activities and check again later.",
                        "ds-icon--gift",
                        "Back to Home",
                        string.Empty);
                    break;

                case RewardsPreviewState.RecoverableError:
                    _dataStateView.Configure(
                        "Rewards could not be loaded",
                        "Check your connection and try again.",
                        "No reward state was changed.",
                        "ds-icon--error",
                        "Try Again",
                        "Back to Home");
                    break;
            }
        }

        private void OnDataStatePrimaryAction()
        {
            if (PreviewState == RewardsPreviewState.Empty)
            {
                BackToHomeRequested?.Invoke();
            }
            else if (PreviewState == RewardsPreviewState.RecoverableError)
            {
                RetryRequested?.Invoke();
            }
        }

        private void OnDataStateSecondaryAction()
        {
            if (PreviewState == RewardsPreviewState.RecoverableError)
            {
                BackToHomeRequested?.Invoke();
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

        private static bool MatchesFilter(RewardsPreviewItem item, RewardsPreviewFilter filter)
        {
            return filter switch
            {
                RewardsPreviewFilter.All => true,
                RewardsPreviewFilter.Owned => item.Status == RewardsPreviewItemStatus.Owned,
                RewardsPreviewFilter.Available => item.Status == RewardsPreviewItemStatus.Available,
                RewardsPreviewFilter.Used => item.Status == RewardsPreviewItemStatus.Used,
                RewardsPreviewFilter.Locked => item.Status == RewardsPreviewItemStatus.Locked,
                _ => true
            };
        }

        private static bool TryValidateItem(
            RewardsPreviewItem item,
            HashSet<string> seenKeys,
            out string warning)
        {
            if (item == null)
            {
                warning = "Item was null.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(item.PresentationKey)
                || string.IsNullOrWhiteSpace(item.Title)
                || string.IsNullOrWhiteSpace(item.Description)
                || string.IsNullOrWhiteSpace(item.SupportingText)
                || string.IsNullOrWhiteSpace(item.IconClass)
                || !item.IconClass.StartsWith("ds-icon--", StringComparison.Ordinal)
                || !Enum.IsDefined(typeof(RewardsPreviewItemStatus), item.Status))
            {
                warning = "Required presentation fields were missing or invalid.";
                return false;
            }

            if (seenKeys.Contains(item.PresentationKey))
            {
                warning = $"Duplicate PresentationKey '{item.PresentationKey}'.";
                return false;
            }

            if (item.Status == RewardsPreviewItemStatus.Locked)
            {
                if (string.IsNullOrWhiteSpace(item.LockedReason))
                {
                    warning = "Locked items require a locked reason.";
                    return false;
                }
            }
            else if (!string.IsNullOrWhiteSpace(item.LockedReason))
            {
                warning = "Non-locked items must have an empty locked reason.";
                return false;
            }

            warning = null;
            return true;
        }

        private static string GetStatusLabel(RewardsPreviewItemStatus status) => status switch
        {
            RewardsPreviewItemStatus.Owned => "Owned",
            RewardsPreviewItemStatus.Available => "Available",
            RewardsPreviewItemStatus.Used => "Used",
            RewardsPreviewItemStatus.Locked => "Locked",
            _ => "—"
        };

        private static string GetStatusIconClass(RewardsPreviewItemStatus status) => status switch
        {
            RewardsPreviewItemStatus.Owned => "ds-icon--check",
            RewardsPreviewItemStatus.Available => "ds-icon--medal",
            RewardsPreviewItemStatus.Used => "ds-icon--info",
            RewardsPreviewItemStatus.Locked => "ds-icon--lock",
            _ => "ds-icon--info"
        };

        private static string GetCardStatusClass(RewardsPreviewItemStatus status) => status switch
        {
            RewardsPreviewItemStatus.Owned => "rewards-panel__card--owned",
            RewardsPreviewItemStatus.Available => "rewards-panel__card--available",
            RewardsPreviewItemStatus.Used => "rewards-panel__card--used",
            RewardsPreviewItemStatus.Locked => "rewards-panel__card--locked",
            _ => "rewards-panel__card--locked"
        };

        private static string GetStatusChipClass(RewardsPreviewItemStatus status) => status switch
        {
            RewardsPreviewItemStatus.Owned => "rewards-panel__status-chip--owned",
            RewardsPreviewItemStatus.Available => "rewards-panel__status-chip--available",
            RewardsPreviewItemStatus.Used => "rewards-panel__status-chip--used",
            RewardsPreviewItemStatus.Locked => "rewards-panel__status-chip--locked",
            _ => "rewards-panel__status-chip--locked"
        };
    }
}
