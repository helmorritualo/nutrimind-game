using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace NutriMind.App.UI
{
    /// <summary>
    /// Presentation-only Announcements route states for static preview.
    /// </summary>
    public enum AnnouncementsPreviewState
    {
        Content = 0,
        Loading = 1,
        Empty = 2,
        OfflineCached = 3,
        RecoverableError = 4
    }

    /// <summary>
    /// Presentation-only filter for the Announcements static preview.
    /// </summary>
    public enum AnnouncementsPreviewFilter
    {
        All = 0,
        Unread = 1
    }

    /// <summary>
    /// Presentation-only announcement kind for the Announcements static preview.
    /// Not a Student API enum.
    /// </summary>
    public enum AnnouncementPreviewKind
    {
        Learning = 0,
        Schedule = 1,
        OfflineReminder = 2
    }

    /// <summary>
    /// Static UI preview only.
    /// Not the Student API announcement DTO.
    /// Replace when the announcement item contract is defined.
    /// PresentationId is not a canonical announcement ID.
    /// </summary>
    public sealed class AnnouncementPreviewItem
    {
        public AnnouncementPreviewItem(
            string presentationId,
            string title,
            string summary,
            string bodyPlainText,
            string audienceLabel,
            string publishedDateText,
            string publicationWindowText,
            bool initiallyUnread,
            AnnouncementPreviewKind kind,
            string iconClass)
        {
            PresentationId = presentationId ?? string.Empty;
            Title = title ?? string.Empty;
            Summary = summary ?? string.Empty;
            BodyPlainText = bodyPlainText ?? string.Empty;
            AudienceLabel = audienceLabel ?? string.Empty;
            PublishedDateText = publishedDateText ?? string.Empty;
            PublicationWindowText = publicationWindowText ?? string.Empty;
            InitiallyUnread = initiallyUnread;
            Kind = kind;
            IconClass = iconClass ?? string.Empty;
        }

        /// <summary>
        /// Presentation-only id. Not a canonical announcement ID.
        /// </summary>
        public string PresentationId { get; }

        public string Title { get; }
        public string Summary { get; }
        public string BodyPlainText { get; }
        public string AudienceLabel { get; }
        public string PublishedDateText { get; }
        public string PublicationWindowText { get; }
        public bool InitiallyUnread { get; }
        public AnnouncementPreviewKind Kind { get; }
        public string IconClass { get; }
    }

    /// <summary>
    /// Static UI preview only.
    /// Not the Student API announcement DTO.
    /// Replace when the announcement item contract is defined.
    /// PresentationId is not a canonical announcement ID.
    /// </summary>
    public readonly struct AnnouncementPreviewSelection
    {
        public AnnouncementPreviewSelection(string presentationId, string title)
        {
            PresentationId = presentationId ?? string.Empty;
            Title = title ?? string.Empty;
        }

        /// <summary>
        /// Presentation-only id. Not a canonical announcement ID.
        /// </summary>
        public string PresentationId { get; }

        public string Title { get; }
    }

    /// <summary>
    /// Static UI preview only.
    /// Not the Student API announcement DTO.
    /// Replace when the announcement item contract is defined.
    /// PresentationId is not a canonical announcement ID.
    /// </summary>
    public sealed class AnnouncementsPreviewReadState
    {
        private readonly string[] _readPresentationIds;

        public AnnouncementsPreviewReadState(IEnumerable<string> readPresentationIds, int unreadCount)
        {
            if (readPresentationIds == null)
            {
                _readPresentationIds = Array.Empty<string>();
            }
            else
            {
                var copied = new List<string>();
                foreach (string id in readPresentationIds)
                {
                    if (!string.IsNullOrWhiteSpace(id))
                    {
                        copied.Add(id);
                    }
                }

                _readPresentationIds = copied.ToArray();
            }

            UnreadCount = unreadCount;
        }

        public IReadOnlyList<string> ReadPresentationIds => _readPresentationIds;

        public int UnreadCount { get; }
    }

    /// <summary>
    /// Deterministic static-preview catalog for Announcements.
    /// Static UI preview only. Not the Student API announcement DTO.
    /// Replace when the announcement item contract is defined.
    /// </summary>
    public static class AnnouncementsPreviewCatalog
    {
        /// <summary>
        /// Builds exactly three presentation-only announcement examples.
        /// </summary>
        public static IReadOnlyList<AnnouncementPreviewItem> CreateItems()
        {
            return new AnnouncementPreviewItem[]
            {
                new(
                    "preview_literaquest_term1_reminder",
                    "LiteraQuest Term 1 learning reminder",
                    "Review The Bell of Seven Moments before the next classroom activity.",
                    "Continue from Bell Tower and review the sequence of seven moments before your next LiteraQuest classroom activity.",
                    "Grade 5 • Section Emerald",
                    "July 24, 2026",
                    "Visible July 24–August 2, 2026",
                    initiallyUnread: true,
                    AnnouncementPreviewKind.Learning,
                    "ds-icon--book"),
                new(
                    "preview_quiz_portal_availability",
                    "Quiz Portal availability reminder",
                    "Check the Story Elements Check assignment while it is available.",
                    "Open the Quiz Portal when you are online and review the assignment details before starting.",
                    "Grade 5 • Section Emerald",
                    "July 22, 2026",
                    "Visible July 22–July 31, 2026",
                    initiallyUnread: true,
                    AnnouncementPreviewKind.Schedule,
                    "ds-icon--calendar"),
                new(
                    "preview_offline_learning_reminder",
                    "Prepare for offline learning",
                    "Confirm that the mission you plan to continue is available offline.",
                    "Before leaving a stable connection, open the mission and check its offline availability. Saved progress remains on this device until synchronization is available.",
                    "Grade 5",
                    "July 18, 2026",
                    "Visible July 18–August 15, 2026",
                    initiallyUnread: false,
                    AnnouncementPreviewKind.OfflineReminder,
                    "ds-icon--wifi")
            };
        }

        public static int CountUnread(
            IReadOnlyList<AnnouncementPreviewItem> items,
            IReadOnlyCollection<string> readPresentationIds)
        {
            if (items == null || items.Count == 0)
            {
                return 0;
            }

            var readIds = readPresentationIds == null
                ? new HashSet<string>(StringComparer.Ordinal)
                : new HashSet<string>(readPresentationIds, StringComparer.Ordinal);

            int count = 0;
            for (int i = 0; i < items.Count; i++)
            {
                AnnouncementPreviewItem item = items[i];
                if (item.InitiallyUnread && !readIds.Contains(item.PresentationId))
                {
                    count++;
                }
            }

            return count;
        }
    }

    /// <summary>
    /// Presentation-only Announcements route view. Binds local preview fixtures and
    /// raises user intent for the host to handle.
    /// Does not call APIs, persist read state, or open external content.
    /// </summary>
    public sealed class AnnouncementsPanelView : IAppScreenView
    {
        private const string RootName = "announcements-root";
        private const string CompactClass = "announcements-panel--compact";
        private const string NarrowClass = "announcements-panel--narrow";
        private const string MobileClass = "mobile";
        private const string DataStateHostVisibleClass = "announcements-panel__data-state-host--visible";
        private const string ContentShellHiddenClass = "announcements-panel__content-shell--hidden";
        private const string OfflineNoticeHiddenClass = "announcements-panel__offline-notice--hidden";
        private const string MainLayoutHiddenClass = "announcements-panel__main-layout--hidden";
        private const string FilterEmptyHiddenClass = "announcements-panel__filter-empty--hidden";
        private const string FilterBtnSelectedClass = "announcements-panel__filter-btn--selected";
        private const string ListItemSelectedClass = "announcements-panel__list-item--selected";
        private const string ListItemUnreadClass = "announcements-panel__list-item--unread";
        private const string ListItemReadClass = "announcements-panel__list-item--read";
        private const string ListItemUnreadDotHiddenClass = "announcements-panel__list-item-unread-dot--hidden";
        private const string ListItemChipUnreadClass = "announcements-panel__list-item-chip--unread";
        private const string ListItemChipReadClass = "announcements-panel__list-item-chip--read";
        private const string DetailReadChipReadClass = "announcements-panel__detail-read-chip--read";
        private const string MarkReadHiddenClass = "announcements-panel__mark-read--hidden";
        private const string ReadHelperHiddenClass = "announcements-panel__read-helper--hidden";
        private const string CompactScreenClass = "app-screen-content--compact";
        private const string NarrowScreenClass = "app-screen-content--narrow";
        private const float CompactBreakpoint = 1100f;
        private const float NarrowBreakpoint = 820f;

        private sealed class AnnouncementListBinding
        {
            public Button Button { get; set; }
            public EventCallback<ClickEvent> Callback { get; set; }
            public AnnouncementPreviewItem Item { get; set; }
        }

        private VisualElement _root;
        private VisualElement _contentShell;
        private ScrollView _scroll;
        private VisualElement _body;
        private VisualElement _offlineNotice;
        private Label _visibleValue;
        private Label _unreadValue;
        private Button _markAllReadButton;
        private Button _filterAllButton;
        private Button _filterUnreadButton;
        private VisualElement _mainLayout;
        private Label _listCount;
        private VisualElement _list;
        private VisualElement _detailSection;
        private VisualElement _detailKindIcon;
        private Label _detailKindLabel;
        private VisualElement _detailReadChip;
        private VisualElement _detailReadIcon;
        private Label _detailReadLabel;
        private Label _detailTitle;
        private Label _detailSummary;
        private Label _publishedValue;
        private Label _audienceValue;
        private Label _windowValue;
        private Label _detailBody;
        private Button _markReadButton;
        private Label _readHelper;
        private VisualElement _filterEmpty;
        private Button _filterEmptyResetButton;
        private VisualElement _dataStateHost;

        private TemplateContainer _ownedDataStateInstance;
        private DataStatePanelView _dataStateView;
        private bool _warnedMissingDataStateAsset;
        private bool _warnedInvalidItems;

        private readonly HashSet<string> _readPresentationIds =
            new HashSet<string>(StringComparer.Ordinal);
        private readonly List<AnnouncementPreviewItem> _loadedItems = new();
        private readonly List<AnnouncementPreviewItem> _visibleItems = new();
        private readonly List<AnnouncementListBinding> _listBindings = new();

        private EventCallback<ClickEvent> _markAllReadClicked;
        private EventCallback<ClickEvent> _markReadClicked;
        private EventCallback<ClickEvent> _filterAllClicked;
        private EventCallback<ClickEvent> _filterUnreadClicked;
        private EventCallback<ClickEvent> _filterEmptyResetClicked;
        private EventCallback<GeometryChangedEvent> _geometryChanged;
        private bool _disposed;
        private float _lastWidth = -1f;
        private string _selectedPresentationId = string.Empty;

        public AnnouncementsPanelView(VisualElement root, VisualTreeAsset dataStatePanelAsset)
        {
            ResolveRoot(root);
            if (_root == null)
            {
                return;
            }

            CacheElements();
            BindDataStatePanel(dataStatePanelAsset);
            RegisterCallbacks();
            SetPreviewState(AnnouncementsPreviewState.Content);
            ApplyResponsiveClasses(_root.resolvedStyle.width);
        }

        public VisualElement Root => _root;
        public bool IsBound => _root != null && !_disposed;
        public AnnouncementsPreviewState PreviewState { get; private set; }
        public AnnouncementsPreviewFilter Filter { get; private set; }
        public string SelectedPresentationId => _selectedPresentationId;
        public int LoadedAnnouncementCount => _loadedItems.Count;
        public int VisibleAnnouncementCount => _visibleItems.Count;

        public int UnreadCount =>
            AnnouncementsPreviewCatalog.CountUnread(_loadedItems, _readPresentationIds);

        public AnnouncementPreviewItem SelectedItem
        {
            get
            {
                for (int i = 0; i < _loadedItems.Count; i++)
                {
                    if (string.Equals(
                            _loadedItems[i].PresentationId,
                            _selectedPresentationId,
                            StringComparison.Ordinal))
                    {
                        return _loadedItems[i];
                    }
                }

                return null;
            }
        }

        public event Action BackRequested;
        public event Action<AnnouncementPreviewSelection> SelectionChanged;
        public event Action<AnnouncementsPreviewReadState> ReadStateChanged;
        public event Action<AnnouncementsPreviewFilter> FilterChanged;
        public event Action RetryRequested;

        public void SetItems(IReadOnlyList<AnnouncementPreviewItem> items)
        {
            if (!IsBound)
            {
                return;
            }

            _warnedInvalidItems = false;
            string previousSelection = _selectedPresentationId;
            _loadedItems.Clear();

            if (items != null)
            {
                var seenIds = new HashSet<string>(StringComparer.Ordinal);
                for (int i = 0; i < items.Count; i++)
                {
                    AnnouncementPreviewItem item = items[i];
                    if (!TryValidateItem(item, seenIds, out string warning))
                    {
                        if (!_warnedInvalidItems)
                        {
                            Debug.LogWarning(
                                $"[AnnouncementsPanelView] Skipping invalid announcement item(s). {warning}");
                            _warnedInvalidItems = true;
                        }

                        continue;
                    }

                    seenIds.Add(item.PresentationId);
                    _loadedItems.Add(item);
                }
            }

            SanitizeReadPresentationIds();
            RebuildVisibleItems();
            RefreshOverviewMetrics();
            RebuildList();
            ApplyFilterEmptyState();
            PreserveSelection(previousSelection);
            ApplyDetail(SelectedItem);
            UpdateMarkAllReadButton();
        }

        public void SetReadPresentationIds(IEnumerable<string> readPresentationIds)
        {
            if (!IsBound)
            {
                return;
            }

            _readPresentationIds.Clear();

            var validUnreadIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < _loadedItems.Count; i++)
            {
                AnnouncementPreviewItem item = _loadedItems[i];
                if (item.InitiallyUnread)
                {
                    validUnreadIds.Add(item.PresentationId);
                }
            }

            if (readPresentationIds != null)
            {
                foreach (string id in readPresentationIds)
                {
                    if (!string.IsNullOrWhiteSpace(id) && validUnreadIds.Contains(id))
                    {
                        _readPresentationIds.Add(id);
                    }
                }
            }

            string previousSelection = _selectedPresentationId;
            RebuildVisibleItems();
            RefreshOverviewMetrics();
            RebuildList();
            ApplyFilterEmptyState();
            PreserveSelection(previousSelection);
            ApplyDetail(SelectedItem);
            UpdateMarkAllReadButton();
        }

        public void SelectByPresentationId(string presentationId)
        {
            if (!IsBound || string.IsNullOrWhiteSpace(presentationId))
            {
                return;
            }

            AnnouncementPreviewItem item = FindVisibleByPresentationId(presentationId);
            if (item == null)
            {
                return;
            }

            _selectedPresentationId = item.PresentationId;
            UpdateListSelectionClasses();
            ApplyDetail(item);
        }

        public void SetFilter(AnnouncementsPreviewFilter filter)
        {
            if (!IsBound || !Enum.IsDefined(typeof(AnnouncementsPreviewFilter), filter))
            {
                return;
            }

            if (Filter == filter)
            {
                UpdateFilterButtonStates();
                return;
            }

            Filter = filter;
            UpdateFilterButtonStates();
            ApplyFilter(restoreSelectionId: _selectedPresentationId, raiseFilterChanged: false);
        }

        public void ResetFilter() => SetFilter(AnnouncementsPreviewFilter.All);

        public void SetPreviewState(AnnouncementsPreviewState state)
        {
            if (!IsBound)
            {
                return;
            }

            if (state != AnnouncementsPreviewState.Content
                && state != AnnouncementsPreviewState.OfflineCached
                && (_dataStateView == null || !_dataStateView.IsBound))
            {
                return;
            }

            PreviewState = state;

            switch (state)
            {
                case AnnouncementsPreviewState.Content:
                    ShowContent(showOfflineNotice: false);
                    _dataStateView?.SetState(DataStatePanelState.Content);
                    ApplyFilterEmptyState();
                    ApplyDetail(SelectedItem);
                    UpdateMarkAllReadButton();
                    break;

                case AnnouncementsPreviewState.OfflineCached:
                    ShowContent(showOfflineNotice: true);
                    _dataStateView?.SetState(DataStatePanelState.Content);
                    ApplyFilterEmptyState();
                    ApplyDetail(SelectedItem);
                    UpdateMarkAllReadButton();
                    break;

                case AnnouncementsPreviewState.Loading:
                    HideContent();
                    _dataStateView.SetState(DataStatePanelState.Loading);
                    ApplyRouteDataStateCopy(state);
                    break;

                case AnnouncementsPreviewState.Empty:
                    HideContent();
                    _dataStateView.SetState(DataStatePanelState.Empty);
                    ApplyRouteDataStateCopy(state);
                    break;

                case AnnouncementsPreviewState.RecoverableError:
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
            ClearListBindings();
            DisposeOwnedDataState();

            BackRequested = null;
            SelectionChanged = null;
            ReadStateChanged = null;
            FilterChanged = null;
            RetryRequested = null;

            _root = null;
            _contentShell = null;
            _scroll = null;
            _body = null;
            _offlineNotice = null;
            _visibleValue = null;
            _unreadValue = null;
            _markAllReadButton = null;
            _filterAllButton = null;
            _filterUnreadButton = null;
            _mainLayout = null;
            _listCount = null;
            _list = null;
            _detailSection = null;
            _detailKindIcon = null;
            _detailKindLabel = null;
            _detailReadChip = null;
            _detailReadIcon = null;
            _detailReadLabel = null;
            _detailTitle = null;
            _detailSummary = null;
            _publishedValue = null;
            _audienceValue = null;
            _windowValue = null;
            _detailBody = null;
            _markReadButton = null;
            _readHelper = null;
            _filterEmpty = null;
            _filterEmptyResetButton = null;
            _dataStateHost = null;
            _lastWidth = -1f;
            _selectedPresentationId = string.Empty;
            _readPresentationIds.Clear();
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
            _contentShell = _root.Q<VisualElement>("announcements-content-shell");
            _scroll = _root.Q<ScrollView>("announcements-scroll");
            _body = _root.Q<VisualElement>("announcements-body");
            _offlineNotice = _root.Q<VisualElement>("announcements-offline-notice");
            _visibleValue = _root.Q<Label>("announcements-visible-value");
            _unreadValue = _root.Q<Label>("announcements-unread-value");
            _markAllReadButton = _root.Q<Button>("announcements-mark-all-read");
            _filterAllButton = _root.Q<Button>("announcements-filter-all");
            _filterUnreadButton = _root.Q<Button>("announcements-filter-unread");
            _mainLayout = _root.Q<VisualElement>("announcements-main-layout");
            _listCount = _root.Q<Label>("announcements-list-count");
            _list = _root.Q<VisualElement>("announcements-list");
            _detailSection = _root.Q<VisualElement>("announcements-detail-section");
            _detailKindIcon = _root.Q<VisualElement>("announcements-detail-kind-icon");
            _detailKindLabel = _root.Q<Label>("announcements-detail-kind-label");
            _detailReadChip = _root.Q<VisualElement>("announcements-detail-read-chip");
            _detailReadIcon = _root.Q<VisualElement>("announcements-detail-read-icon");
            _detailReadLabel = _root.Q<Label>("announcements-detail-read-label");
            _detailTitle = _root.Q<Label>("announcements-detail-title");
            _detailSummary = _root.Q<Label>("announcements-detail-summary");
            _publishedValue = _root.Q<Label>("announcements-published-value");
            _audienceValue = _root.Q<Label>("announcements-audience-value");
            _windowValue = _root.Q<Label>("announcements-window-value");
            _detailBody = _root.Q<Label>("announcements-detail-body");
            _markReadButton = _root.Q<Button>("announcements-mark-read");
            _readHelper = _root.Q<Label>("announcements-read-helper");
            _filterEmpty = _root.Q<VisualElement>("announcements-filter-empty");
            _filterEmptyResetButton = _root.Q<Button>("announcements-filter-empty-reset");
            _dataStateHost = _root.Q<VisualElement>("announcements-data-state-host");

            ConfigurePlainTextLabel(_detailTitle);
            ConfigurePlainTextLabel(_detailSummary);
            ConfigurePlainTextLabel(_publishedValue);
            ConfigurePlainTextLabel(_audienceValue);
            ConfigurePlainTextLabel(_windowValue);
            ConfigurePlainTextLabel(_detailBody);
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
                        "[AnnouncementsPanelView] DataStatePanel VisualTreeAsset is missing. " +
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
                    "[AnnouncementsPanelView] Failed to bind DataStatePanelView from the assigned asset.");
                DisposeOwnedDataState();
                return;
            }

            _dataStateView.PrimaryActionRequested += OnDataStatePrimaryAction;
            _dataStateView.SecondaryActionRequested += OnDataStateSecondaryAction;
            _dataStateView.SetVisible(false);
        }

        private void RegisterCallbacks()
        {
            _markAllReadClicked = _ => OnMarkAllReadClicked();
            _markReadClicked = _ => OnMarkReadClicked();
            _filterAllClicked = _ => OnFilterAllClicked();
            _filterUnreadClicked = _ => OnFilterUnreadClicked();
            _filterEmptyResetClicked = _ => OnFilterEmptyResetClicked();
            _geometryChanged = OnGeometryChanged;

            _markAllReadButton?.RegisterCallback(_markAllReadClicked);
            _markReadButton?.RegisterCallback(_markReadClicked);
            _filterAllButton?.RegisterCallback(_filterAllClicked);
            _filterUnreadButton?.RegisterCallback(_filterUnreadClicked);
            _filterEmptyResetButton?.RegisterCallback(_filterEmptyResetClicked);
            _root?.RegisterCallback(_geometryChanged);
        }

        private void UnregisterCallbacks()
        {
            if (_markAllReadButton != null && _markAllReadClicked != null)
            {
                _markAllReadButton.UnregisterCallback(_markAllReadClicked);
            }

            if (_markReadButton != null && _markReadClicked != null)
            {
                _markReadButton.UnregisterCallback(_markReadClicked);
            }

            if (_filterAllButton != null && _filterAllClicked != null)
            {
                _filterAllButton.UnregisterCallback(_filterAllClicked);
            }

            if (_filterUnreadButton != null && _filterUnreadClicked != null)
            {
                _filterUnreadButton.UnregisterCallback(_filterUnreadClicked);
            }

            if (_filterEmptyResetButton != null && _filterEmptyResetClicked != null)
            {
                _filterEmptyResetButton.UnregisterCallback(_filterEmptyResetClicked);
            }

            if (_root != null && _geometryChanged != null)
            {
                _root.UnregisterCallback(_geometryChanged);
            }

            _markAllReadClicked = null;
            _markReadClicked = null;
            _filterAllClicked = null;
            _filterUnreadClicked = null;
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

        private void SanitizeReadPresentationIds()
        {
            if (_readPresentationIds.Count == 0)
            {
                return;
            }

            var validUnreadIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < _loadedItems.Count; i++)
            {
                AnnouncementPreviewItem item = _loadedItems[i];
                if (item.InitiallyUnread)
                {
                    validUnreadIds.Add(item.PresentationId);
                }
            }

            _readPresentationIds.RemoveWhere(id => !validUnreadIds.Contains(id));
        }

        private void RebuildVisibleItems()
        {
            _visibleItems.Clear();

            for (int i = 0; i < _loadedItems.Count; i++)
            {
                AnnouncementPreviewItem item = _loadedItems[i];
                if (Filter == AnnouncementsPreviewFilter.Unread && !IsUnread(item))
                {
                    continue;
                }

                _visibleItems.Add(item);
            }
        }

        private void RefreshOverviewMetrics()
        {
            if (_visibleValue != null)
            {
                _visibleValue.text = _loadedItems.Count.ToString();
            }

            if (_unreadValue != null)
            {
                _unreadValue.text = UnreadCount.ToString();
            }

            if (_listCount != null)
            {
                int count = _visibleItems.Count;
                _listCount.text = count == 1
                    ? "1 announcement"
                    : $"{count} announcements";
            }
        }

        private void RebuildList()
        {
            ClearListBindings();
            if (_list == null)
            {
                return;
            }

            for (int i = 0; i < _visibleItems.Count; i++)
            {
                AnnouncementPreviewItem item = _visibleItems[i];
                bool last = i == _visibleItems.Count - 1;
                _list.Add(CreateListItem(item, last));
            }
        }

        private VisualElement CreateListItem(AnnouncementPreviewItem item, bool last)
        {
            bool unread = IsUnread(item);

            var button = new Button();
            button.AddToClassList("announcements-panel__list-item");
            button.AddToClassList(GetListItemKindClass(item.Kind));
            button.EnableInClassList(ListItemUnreadClass, unread);
            button.EnableInClassList(ListItemReadClass, !unread);
            if (last)
            {
                button.AddToClassList("announcements-panel__list-item--last");
            }

            button.tooltip = item.Title;
            button.focusable = true;

            var icon = new VisualElement();
            icon.AddToClassList("ds-icon");
            icon.AddToClassList(item.IconClass);
            icon.AddToClassList("announcements-panel__list-item-icon");
            icon.pickingMode = PickingMode.Ignore;

            var copy = new VisualElement();
            copy.AddToClassList("announcements-panel__list-item-copy");
            copy.pickingMode = PickingMode.Ignore;

            var chipRow = new VisualElement();
            chipRow.AddToClassList("announcements-panel__list-item-chip-row");
            chipRow.pickingMode = PickingMode.Ignore;

            var unreadDot = new VisualElement();
            unreadDot.AddToClassList("announcements-panel__list-item-unread-dot");
            unreadDot.EnableInClassList(ListItemUnreadDotHiddenClass, !unread);
            unreadDot.pickingMode = PickingMode.Ignore;

            var chip = new VisualElement();
            chip.AddToClassList("ds-chip");
            chip.AddToClassList("announcements-panel__list-item-chip");
            chip.EnableInClassList(ListItemChipUnreadClass, unread);
            chip.EnableInClassList(ListItemChipReadClass, !unread);
            chip.pickingMode = PickingMode.Ignore;

            var chipLabel = new Label(unread ? "Unread" : "Read");
            chipLabel.AddToClassList("announcements-panel__list-item-chip-label");
            chipLabel.pickingMode = PickingMode.Ignore;
            ConfigurePlainTextLabel(chipLabel);

            chip.Add(chipLabel);
            chipRow.Add(unreadDot);
            chipRow.Add(chip);

            var title = new Label(item.Title);
            title.AddToClassList("announcements-panel__list-item-title");
            title.pickingMode = PickingMode.Ignore;
            ConfigurePlainTextLabel(title);

            var summary = new Label(item.Summary);
            summary.AddToClassList("announcements-panel__list-item-summary");
            summary.pickingMode = PickingMode.Ignore;
            ConfigurePlainTextLabel(summary);

            var meta = new Label($"{item.PublishedDateText} • {item.PublicationWindowText}");
            meta.AddToClassList("announcements-panel__list-item-meta");
            meta.pickingMode = PickingMode.Ignore;
            ConfigurePlainTextLabel(meta);

            copy.Add(chipRow);
            copy.Add(title);
            copy.Add(summary);
            copy.Add(meta);

            var chevron = new VisualElement();
            chevron.AddToClassList("ds-icon");
            chevron.AddToClassList("ds-icon--chevron-right");
            chevron.AddToClassList("announcements-panel__list-item-chevron");
            chevron.pickingMode = PickingMode.Ignore;

            button.Add(icon);
            button.Add(copy);
            button.Add(chevron);

            var binding = new AnnouncementListBinding
            {
                Button = button,
                Item = item
            };
            binding.Callback = evt => OnListItemClicked(binding);
            button.RegisterCallback(binding.Callback);
            _listBindings.Add(binding);

            return button;
        }

        private void OnListItemClicked(AnnouncementListBinding binding)
        {
            if (_disposed || binding?.Item == null)
            {
                return;
            }

            if (string.Equals(
                    binding.Item.PresentationId,
                    _selectedPresentationId,
                    StringComparison.Ordinal))
            {
                return;
            }

            _selectedPresentationId = binding.Item.PresentationId;
            UpdateListSelectionClasses();
            ApplyDetail(binding.Item);
            SelectionChanged?.Invoke(
                new AnnouncementPreviewSelection(binding.Item.PresentationId, binding.Item.Title));
        }

        private void OnMarkReadClicked()
        {
            if (_disposed
                || (PreviewState != AnnouncementsPreviewState.Content
                    && PreviewState != AnnouncementsPreviewState.OfflineCached))
            {
                return;
            }

            AnnouncementPreviewItem item = SelectedItem;
            if (item == null || !IsUnread(item))
            {
                return;
            }

            if (_readPresentationIds.Add(item.PresentationId))
            {
                RefreshAfterReadStateChange(emitEvent: true);
            }
        }

        private void OnMarkAllReadClicked()
        {
            if (_disposed
                || (PreviewState != AnnouncementsPreviewState.Content
                    && PreviewState != AnnouncementsPreviewState.OfflineCached))
            {
                return;
            }

            bool changed = false;
            for (int i = 0; i < _loadedItems.Count; i++)
            {
                AnnouncementPreviewItem item = _loadedItems[i];
                if (item.InitiallyUnread && _readPresentationIds.Add(item.PresentationId))
                {
                    changed = true;
                }
            }

            if (!changed)
            {
                return;
            }

            RefreshAfterReadStateChange(emitEvent: true);
        }

        private void OnFilterAllClicked()
        {
            if (_disposed || Filter == AnnouncementsPreviewFilter.All)
            {
                return;
            }

            Filter = AnnouncementsPreviewFilter.All;
            UpdateFilterButtonStates();
            ApplyFilter(restoreSelectionId: _selectedPresentationId, raiseFilterChanged: true);
        }

        private void OnFilterUnreadClicked()
        {
            if (_disposed || Filter == AnnouncementsPreviewFilter.Unread)
            {
                return;
            }

            Filter = AnnouncementsPreviewFilter.Unread;
            UpdateFilterButtonStates();
            ApplyFilter(restoreSelectionId: _selectedPresentationId, raiseFilterChanged: true);
        }

        private void OnFilterEmptyResetClicked()
        {
            if (_disposed || Filter == AnnouncementsPreviewFilter.All)
            {
                return;
            }

            Filter = AnnouncementsPreviewFilter.All;
            UpdateFilterButtonStates();
            ApplyFilter(restoreSelectionId: _selectedPresentationId, raiseFilterChanged: true);
        }

        private void ApplyFilter(string restoreSelectionId, bool raiseFilterChanged)
        {
            RebuildVisibleItems();
            RefreshOverviewMetrics();
            RebuildList();
            ApplyFilterEmptyState();
            PreserveSelection(restoreSelectionId);
            ApplyDetail(SelectedItem);
            UpdateMarkAllReadButton();

            if (raiseFilterChanged)
            {
                FilterChanged?.Invoke(Filter);
            }
        }

        private void RefreshAfterReadStateChange(bool emitEvent)
        {
            string previousSelection = _selectedPresentationId;
            RebuildVisibleItems();
            RefreshOverviewMetrics();
            RebuildList();
            ApplyFilterEmptyState();
            PreserveSelection(previousSelection);
            ApplyDetail(SelectedItem);
            UpdateMarkAllReadButton();

            if (emitEvent)
            {
                ReadStateChanged?.Invoke(CreateReadStateSnapshot());
            }
        }

        private AnnouncementsPreviewReadState CreateReadStateSnapshot()
        {
            return new AnnouncementsPreviewReadState(_readPresentationIds, UnreadCount);
        }

        private void PreserveSelection(string previousSelectionId)
        {
            if (_visibleItems.Count == 0)
            {
                _selectedPresentationId = string.Empty;
                return;
            }

            AnnouncementPreviewItem preserved = FindVisibleByPresentationId(previousSelectionId);
            if (preserved != null)
            {
                _selectedPresentationId = preserved.PresentationId;
            }
            else
            {
                _selectedPresentationId = _visibleItems[0].PresentationId;
            }

            UpdateListSelectionClasses();
        }

        private void ApplyFilterEmptyState()
        {
            bool showFilterEmpty = Filter == AnnouncementsPreviewFilter.Unread
                && _visibleItems.Count == 0
                && _loadedItems.Count > 0
                && (PreviewState == AnnouncementsPreviewState.Content
                    || PreviewState == AnnouncementsPreviewState.OfflineCached);

            _mainLayout?.EnableInClassList(MainLayoutHiddenClass, showFilterEmpty);
            _filterEmpty?.EnableInClassList(FilterEmptyHiddenClass, !showFilterEmpty);
        }

        private void UpdateFilterButtonStates()
        {
            _filterAllButton?.EnableInClassList(
                FilterBtnSelectedClass,
                Filter == AnnouncementsPreviewFilter.All);
            _filterUnreadButton?.EnableInClassList(
                FilterBtnSelectedClass,
                Filter == AnnouncementsPreviewFilter.Unread);
        }

        private void UpdateMarkAllReadButton()
        {
            if (_markAllReadButton == null)
            {
                return;
            }

            bool canMarkRead = PreviewState == AnnouncementsPreviewState.Content
                || PreviewState == AnnouncementsPreviewState.OfflineCached;
            _markAllReadButton.SetEnabled(canMarkRead && UnreadCount > 0);
        }

        private void ClearListBindings()
        {
            for (int i = 0; i < _listBindings.Count; i++)
            {
                AnnouncementListBinding binding = _listBindings[i];
                if (binding.Button != null && binding.Callback != null)
                {
                    binding.Button.UnregisterCallback(binding.Callback);
                }
            }

            _listBindings.Clear();
            _list?.Clear();
        }

        private void UpdateListSelectionClasses()
        {
            for (int i = 0; i < _listBindings.Count; i++)
            {
                AnnouncementListBinding binding = _listBindings[i];
                if (binding.Button == null || binding.Item == null)
                {
                    continue;
                }

                bool selected = string.Equals(
                    binding.Item.PresentationId,
                    _selectedPresentationId,
                    StringComparison.Ordinal);
                binding.Button.EnableInClassList(ListItemSelectedClass, selected);
            }
        }

        private void ApplyDetail(AnnouncementPreviewItem item)
        {
            if (item == null)
            {
                ApplyEmptyDetailPlaceholder();
                return;
            }

            ApplyDetailSectionKind(item.Kind);
            SetDetailKindIcon(item.IconClass);

            if (_detailKindLabel != null)
            {
                _detailKindLabel.text = GetKindLabel(item.Kind);
            }

            bool unread = IsUnread(item);
            if (_detailReadChip != null)
            {
                _detailReadChip.EnableInClassList(DetailReadChipReadClass, !unread);
            }

            if (_detailReadIcon != null)
            {
                RemoveIconClasses(_detailReadIcon);
                _detailReadIcon.AddToClassList("ds-icon");
                _detailReadIcon.AddToClassList(unread ? "ds-icon--info" : "ds-icon--check");
                _detailReadIcon.AddToClassList("announcements-panel__detail-read-icon");
            }

            if (_detailReadLabel != null)
            {
                _detailReadLabel.text = unread ? "Unread" : "Read";
            }

            SetPlainText(_detailTitle, item.Title);
            SetPlainText(_detailSummary, item.Summary);
            SetPlainText(_publishedValue, item.PublishedDateText);
            SetPlainText(_audienceValue, item.AudienceLabel);
            SetPlainText(_windowValue, item.PublicationWindowText);
            SetPlainText(_detailBody, item.BodyPlainText);
            ApplyReadAction(item);
        }

        private void ApplyDetailSectionKind(AnnouncementPreviewKind kind)
        {
            if (_detailSection == null)
            {
                return;
            }

            _detailSection.RemoveFromClassList("announcements-panel__detail-section--learning");
            _detailSection.RemoveFromClassList("announcements-panel__detail-section--schedule");
            _detailSection.RemoveFromClassList("announcements-panel__detail-section--offline");

            switch (kind)
            {
                case AnnouncementPreviewKind.Learning:
                    _detailSection.AddToClassList("announcements-panel__detail-section--learning");
                    break;
                case AnnouncementPreviewKind.Schedule:
                    _detailSection.AddToClassList("announcements-panel__detail-section--schedule");
                    break;
                case AnnouncementPreviewKind.OfflineReminder:
                    _detailSection.AddToClassList("announcements-panel__detail-section--offline");
                    break;
                default:
                    _detailSection.AddToClassList("announcements-panel__detail-section--learning");
                    break;
            }
        }

        private void ApplyEmptyDetailPlaceholder()
        {
            ApplyDetailSectionKind(AnnouncementPreviewKind.Learning);
            SetDetailKindIcon("ds-icon--bell");

            if (_detailKindLabel != null) _detailKindLabel.text = "Announcement";
            if (_detailReadChip != null) _detailReadChip.EnableInClassList(DetailReadChipReadClass, true);
            if (_detailReadIcon != null)
            {
                RemoveIconClasses(_detailReadIcon);
                _detailReadIcon.AddToClassList("ds-icon");
                _detailReadIcon.AddToClassList("ds-icon--info");
                _detailReadIcon.AddToClassList("announcements-panel__detail-read-icon");
            }

            if (_detailReadLabel != null) _detailReadLabel.text = "—";
            SetPlainText(_detailTitle, "No announcement selected");
            SetPlainText(_detailSummary, "Select an announcement from the list.");
            SetPlainText(_publishedValue, "—");
            SetPlainText(_audienceValue, "—");
            SetPlainText(_windowValue, "—");
            SetPlainText(_detailBody, "Visible announcements will appear in the list.");

            _markReadButton?.AddToClassList(MarkReadHiddenClass);
            _readHelper?.AddToClassList(ReadHelperHiddenClass);
        }

        private void ApplyReadAction(AnnouncementPreviewItem item)
        {
            if (_markReadButton == null || _readHelper == null)
            {
                return;
            }

            bool unread = IsUnread(item);
            if (unread)
            {
                _markReadButton.RemoveFromClassList(MarkReadHiddenClass);
                _readHelper.text = string.Empty;
                _readHelper.AddToClassList(ReadHelperHiddenClass);
            }
            else
            {
                _markReadButton.AddToClassList(MarkReadHiddenClass);
                SetPlainText(_readHelper, "Read on this device");
                _readHelper.RemoveFromClassList(ReadHelperHiddenClass);
            }
        }

        private void SetDetailKindIcon(string iconClass)
        {
            if (_detailKindIcon == null)
            {
                return;
            }

            RemoveIconClasses(_detailKindIcon);
            _detailKindIcon.AddToClassList("ds-icon");
            _detailKindIcon.AddToClassList(iconClass);
            _detailKindIcon.AddToClassList("announcements-panel__detail-kind-icon");
        }

        private static void RemoveIconClasses(VisualElement element)
        {
            if (element == null)
            {
                return;
            }

            var toRemove = new List<string>();
            foreach (string className in element.GetClasses())
            {
                if (className.StartsWith("ds-icon", StringComparison.Ordinal))
                {
                    toRemove.Add(className);
                }
            }

            for (int i = 0; i < toRemove.Count; i++)
            {
                element.RemoveFromClassList(toRemove[i]);
            }
        }

        private bool IsUnread(AnnouncementPreviewItem item) =>
            item != null
            && item.InitiallyUnread
            && !_readPresentationIds.Contains(item.PresentationId);

        private AnnouncementPreviewItem FindVisibleByPresentationId(string presentationId)
        {
            if (string.IsNullOrWhiteSpace(presentationId))
            {
                return null;
            }

            for (int i = 0; i < _visibleItems.Count; i++)
            {
                if (string.Equals(
                        _visibleItems[i].PresentationId,
                        presentationId,
                        StringComparison.Ordinal))
                {
                    return _visibleItems[i];
                }
            }

            return null;
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

        private void ApplyRouteDataStateCopy(AnnouncementsPreviewState state)
        {
            if (_dataStateView == null || !_dataStateView.IsBound)
            {
                return;
            }

            switch (state)
            {
                case AnnouncementsPreviewState.Loading:
                    _dataStateView.Configure(
                        new DataStatePanelConfiguration(
                            "Loading announcements",
                            "Getting visible classroom and learning updates.",
                            "Only announcements inside their publication window are shown.",
                            null,
                            string.Empty,
                            string.Empty,
                            true));
                    break;

                case AnnouncementsPreviewState.Empty:
                    _dataStateView.Configure(
                        new DataStatePanelConfiguration(
                            "No announcements",
                            "There are no visible announcements right now.",
                            "New classroom and learning updates will appear here when published.",
                            "ds-icon--bell",
                            "Back",
                            string.Empty));
                    break;

                case AnnouncementsPreviewState.RecoverableError:
                    _dataStateView.Configure(
                        new DataStatePanelConfiguration(
                            "Announcements could not be loaded",
                            "Check your connection and try again.",
                            "Your local read state was not changed.",
                            "ds-icon--error",
                            "Try Again",
                            "Back"));
                    break;
            }
        }

        private void OnDataStatePrimaryAction()
        {
            if (PreviewState == AnnouncementsPreviewState.Empty)
            {
                BackRequested?.Invoke();
            }
            else if (PreviewState == AnnouncementsPreviewState.RecoverableError)
            {
                RetryRequested?.Invoke();
            }
        }

        private void OnDataStateSecondaryAction()
        {
            if (PreviewState == AnnouncementsPreviewState.RecoverableError)
            {
                BackRequested?.Invoke();
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
            AnnouncementPreviewItem item,
            HashSet<string> seenIds,
            out string warning)
        {
            if (item == null)
            {
                warning = "Item was null.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(item.PresentationId)
                || string.IsNullOrWhiteSpace(item.Title)
                || string.IsNullOrWhiteSpace(item.Summary)
                || string.IsNullOrWhiteSpace(item.BodyPlainText)
                || string.IsNullOrWhiteSpace(item.AudienceLabel)
                || string.IsNullOrWhiteSpace(item.PublishedDateText)
                || string.IsNullOrWhiteSpace(item.PublicationWindowText)
                || string.IsNullOrWhiteSpace(item.IconClass)
                || !item.IconClass.StartsWith("ds-icon--", StringComparison.Ordinal)
                || !Enum.IsDefined(typeof(AnnouncementPreviewKind), item.Kind))
            {
                warning = "Required presentation fields were missing or invalid.";
                return false;
            }

            if (seenIds.Contains(item.PresentationId))
            {
                warning = $"Duplicate PresentationId '{item.PresentationId}'.";
                return false;
            }

            if (ContainsDangerousMarkup(item.PresentationId)
                || ContainsDangerousMarkup(item.Title)
                || ContainsDangerousMarkup(item.Summary)
                || ContainsDangerousMarkup(item.BodyPlainText)
                || ContainsDangerousMarkup(item.AudienceLabel)
                || ContainsDangerousMarkup(item.PublishedDateText)
                || ContainsDangerousMarkup(item.PublicationWindowText)
                || ContainsDangerousMarkup(item.IconClass))
            {
                warning = "Item contained disallowed markup or script content.";
                return false;
            }

            warning = null;
            return true;
        }

        private static bool ContainsDangerousMarkup(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            return value.IndexOf("<script", StringComparison.OrdinalIgnoreCase) >= 0
                || value.IndexOf("<iframe", StringComparison.OrdinalIgnoreCase) >= 0
                || value.IndexOf("javascript:", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void ConfigurePlainTextLabel(Label label)
        {
            if (label == null)
            {
                return;
            }

            label.enableRichText = false;
        }

        private static void SetPlainText(Label label, string value)
        {
            if (label == null)
            {
                return;
            }

            label.enableRichText = false;
            label.text = value ?? string.Empty;
        }

        private static string GetKindLabel(AnnouncementPreviewKind kind) =>
            kind switch
            {
                AnnouncementPreviewKind.Learning => "Learning update",
                AnnouncementPreviewKind.Schedule => "Schedule reminder",
                AnnouncementPreviewKind.OfflineReminder => "Offline reminder",
                _ => "Announcement"
            };

        private static string GetListItemKindClass(AnnouncementPreviewKind kind) =>
            kind switch
            {
                AnnouncementPreviewKind.Learning => "announcements-panel__list-item--learning",
                AnnouncementPreviewKind.Schedule => "announcements-panel__list-item--schedule",
                AnnouncementPreviewKind.OfflineReminder => "announcements-panel__list-item--offline",
                _ => "announcements-panel__list-item--learning"
            };
    }
}
