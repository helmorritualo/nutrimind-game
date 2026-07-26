using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UIElements;

namespace NutriMind.App.UI
{
    /// <summary>
    /// Presentation-only Leaderboard route states for static preview.
    /// </summary>
    public enum LeaderboardPreviewState
    {
        Content = 0,
        Loading = 1,
        Empty = 2,
        OfflineUnavailable = 3,
        RecoverableError = 4
    }

    /// <summary>
    /// Static UI preview only.
    /// Not the Student API leaderboard DTO.
    /// Replace when the leaderboard response contract is defined.
    /// </summary>
    public sealed class LeaderboardPreviewContext
    {
        public LeaderboardPreviewContext(
            string scopeLabel,
            string metricLabel,
            string periodLabel,
            string contextLabel)
        {
            ScopeLabel = scopeLabel ?? string.Empty;
            MetricLabel = metricLabel ?? string.Empty;
            PeriodLabel = periodLabel ?? string.Empty;
            ContextLabel = contextLabel ?? string.Empty;
        }

        public string ScopeLabel { get; }
        public string MetricLabel { get; }
        public string PeriodLabel { get; }
        public string ContextLabel { get; }
    }

    /// <summary>
    /// Static UI preview only.
    /// Not the Student API leaderboard DTO.
    /// Replace when the leaderboard response contract is defined.
    /// PrivacySafeName is a presentation alias, not a legal identity.
    /// </summary>
    public sealed class LeaderboardPreviewEntry
    {
        public LeaderboardPreviewEntry(
            int rank,
            string privacySafeName,
            int missionsCompleted,
            bool isCurrentStudent)
        {
            Rank = rank;
            PrivacySafeName = privacySafeName ?? string.Empty;
            MissionsCompleted = missionsCompleted;
            IsCurrentStudent = isCurrentStudent;
        }

        public int Rank { get; }

        /// <summary>
        /// PrivacySafeName is a presentation alias, not a legal identity.
        /// </summary>
        public string PrivacySafeName { get; }

        public int MissionsCompleted { get; }
        public bool IsCurrentStudent { get; }
    }

    /// <summary>
    /// Static UI preview only.
    /// Not the Student API leaderboard DTO.
    /// Replace when the leaderboard response contract is defined.
    /// </summary>
    public sealed class LeaderboardPreviewData
    {
        private readonly LeaderboardPreviewEntry[] _entries;

        public LeaderboardPreviewData(
            LeaderboardPreviewContext context,
            IReadOnlyList<LeaderboardPreviewEntry> entries)
        {
            Context = context;
            if (entries == null || entries.Count == 0)
            {
                _entries = Array.Empty<LeaderboardPreviewEntry>();
            }
            else
            {
                _entries = new LeaderboardPreviewEntry[entries.Count];
                for (int i = 0; i < entries.Count; i++)
                {
                    _entries[i] = entries[i];
                }
            }
        }

        public LeaderboardPreviewContext Context { get; }

        public IReadOnlyList<LeaderboardPreviewEntry> Entries => _entries;
    }

    /// <summary>
    /// Deterministic static-preview catalog for Leaderboard.
    /// Static UI preview only. Not the Student API leaderboard DTO.
    /// Replace when the leaderboard response contract is defined.
    /// </summary>
    public static class LeaderboardPreviewCatalog
    {
        /// <summary>
        /// Builds the single canonical Grade 5 section standings preview.
        /// </summary>
        public static LeaderboardPreviewData CreateCanonicalPreview()
        {
            var context = new LeaderboardPreviewContext(
                "Your section",
                "Missions completed",
                "Grade 5 • All terms",
                "All subjects • All terms");

            var entries = new LeaderboardPreviewEntry[]
            {
                new(1, "Bright Sprout", 35, false),
                new(2, "Curious Comet", 33, false),
                new(3, "Story Scout", 32, false),
                new(4, "Pathfinder", 30, true),
                new(5, "Kind Explorer", 29, false),
                new(6, "Blue Falcon", 27, false)
            };

            return new LeaderboardPreviewData(context, entries);
        }
    }

    /// <summary>
    /// Presentation-only Leaderboard route view. Binds local preview standings and
    /// raises user intent for the host to handle.
    /// Does not call networking endpoints, cache standings, or persist ranking data.
    /// </summary>
    public sealed class LeaderboardPanelView : IAppScreenView
    {
        private const string RootName = "leaderboard-root";
        private const string CompactClass = "leaderboard-panel--compact";
        private const string NarrowClass = "leaderboard-panel--narrow";
        private const string MobileClass = "mobile";
        private const string ContentShellHiddenClass = "leaderboard-panel__content-shell--hidden";
        private const string DataStateHostVisibleClass = "leaderboard-panel__data-state-host--visible";
        private const float CompactBreakpoint = 1100f;
        private const float NarrowBreakpoint = 820f;

        private static readonly Regex SixOrMoreDigits =
            new(@"\d{6,}", RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private VisualElement _root;
        private VisualElement _contentShell;
        private ScrollView _scroll;
        private VisualElement _body;
        private Label _overviewContext;
        private Button _backButton;
        private Label _scopeValue;
        private Label _metricValue;
        private Label _periodValue;
        private Label _ownRank;
        private Label _ownName;
        private Label _ownMetric;
        private Label _ownCount;
        private Label _standingsCount;
        private Label _standingsMetric;
        private VisualElement _list;
        private VisualElement _dataStateHost;

        private TemplateContainer _ownedDataStateInstance;
        private DataStatePanelView _dataStateView;
        private bool _warnedMissingDataStateAsset;
        private bool _disposed;
        private float _lastWidth = -1f;
        private LeaderboardPreviewData _data;
        private readonly List<VisualElement> _rowElements = new();

        public LeaderboardPanelView(
            VisualElement root,
            VisualTreeAsset dataStatePanelAsset = null)
        {
            ResolveRoot(root);
            if (_root == null)
            {
                Debug.LogWarning(
                    "[LeaderboardPanelView] Could not resolve leaderboard-root inside the supplied element.");
                return;
            }

            CacheElements();
            BindDataStatePanel(dataStatePanelAsset);
            RegisterCallbacks();
            ApplyResponsiveClasses(_root.resolvedStyle.width);
            SetPreviewState(LeaderboardPreviewState.Content);
        }

        public VisualElement Root => _root;

        public bool IsBound => _root != null && !_disposed;

        public LeaderboardPreviewState PreviewState { get; private set; } =
            LeaderboardPreviewState.Content;

        public LeaderboardPreviewData Data => _data;

        public LeaderboardPreviewEntry CurrentStudentEntry { get; private set; }

        public int LoadedEntryCount { get; private set; }

        public event Action BackToProgressRequested;
        public event Action RetryRequested;

        public void SetData(LeaderboardPreviewData data)
        {
            if (!IsBound)
            {
                return;
            }

            ClearRows();
            CurrentStudentEntry = null;
            LoadedEntryCount = 0;
            _data = null;

            string warning = null;
            if (data == null || !TryValidatePreviewData(data, out warning))
            {
                if (!string.IsNullOrEmpty(warning))
                {
                    Debug.LogWarning($"[LeaderboardPanelView] {warning}");
                }
                else if (data == null)
                {
                    Debug.LogWarning(
                        "[LeaderboardPanelView] SetData received a null payload. LoadedEntryCount = 0.");
                }

                ApplyEmptyBoundLabels();
                return;
            }

            _data = data;
            LoadedEntryCount = data.Entries.Count;
            BindContext(data.Context);
            BindOwnPosition(data);
            RebuildRows(data.Entries);
        }

        public void SetPreviewState(LeaderboardPreviewState state)
        {
            if (!IsBound)
            {
                return;
            }

            if (state != LeaderboardPreviewState.Content
                && (_dataStateView == null || !_dataStateView.IsBound))
            {
                return;
            }

            PreviewState = state;

            switch (state)
            {
                case LeaderboardPreviewState.Content:
                    ShowContent();
                    _dataStateView?.SetState(DataStatePanelState.Content);
                    break;

                case LeaderboardPreviewState.Loading:
                    HideContent();
                    _dataStateView.SetState(DataStatePanelState.Loading);
                    ApplyRouteDataStateCopy(state);
                    break;

                case LeaderboardPreviewState.Empty:
                    HideContent();
                    _dataStateView.SetState(DataStatePanelState.Empty);
                    ApplyRouteDataStateCopy(state);
                    break;

                case LeaderboardPreviewState.OfflineUnavailable:
                    HideContent();
                    _dataStateView.SetState(DataStatePanelState.OfflineUnavailable);
                    ApplyRouteDataStateCopy(state);
                    break;

                case LeaderboardPreviewState.RecoverableError:
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
            ClearRows();
            DisposeOwnedDataState();

            BackToProgressRequested = null;
            RetryRequested = null;

            _root = null;
            _contentShell = null;
            _scroll = null;
            _body = null;
            _overviewContext = null;
            _backButton = null;
            _scopeValue = null;
            _metricValue = null;
            _periodValue = null;
            _ownRank = null;
            _ownName = null;
            _ownMetric = null;
            _ownCount = null;
            _standingsCount = null;
            _standingsMetric = null;
            _list = null;
            _dataStateHost = null;
            _data = null;
            CurrentStudentEntry = null;
            LoadedEntryCount = 0;
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
            _contentShell = _root.Q<VisualElement>("leaderboard-content-shell");
            _scroll = _root.Q<ScrollView>("leaderboard-scroll");
            _body = _root.Q<VisualElement>("leaderboard-body");
            _overviewContext = _root.Q<Label>("leaderboard-overview-context");
            _backButton = _root.Q<Button>("leaderboard-back-progress-button");
            _scopeValue = _root.Q<Label>("leaderboard-scope-value");
            _metricValue = _root.Q<Label>("leaderboard-metric-value");
            _periodValue = _root.Q<Label>("leaderboard-period-value");
            _ownRank = _root.Q<Label>("leaderboard-own-rank");
            _ownName = _root.Q<Label>("leaderboard-own-name");
            _ownMetric = _root.Q<Label>("leaderboard-own-metric");
            _ownCount = _root.Q<Label>("leaderboard-own-count");
            _standingsCount = _root.Q<Label>("leaderboard-standings-count");
            _standingsMetric = _root.Q<Label>("leaderboard-standings-metric");
            _list = _root.Q<VisualElement>("leaderboard-list");
            _dataStateHost = _root.Q<VisualElement>("leaderboard-data-state-host");
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
                        "[LeaderboardPanelView] DataStatePanel VisualTreeAsset is missing. " +
                        "Content remains usable; non-content states are no-ops.");
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
                    "[LeaderboardPanelView] Failed to bind DataStatePanelView from the assigned asset.");
                DisposeOwnedDataState();
                return;
            }

            _dataStateView.PrimaryActionRequested += OnDataStatePrimaryAction;
            _dataStateView.SecondaryActionRequested += OnDataStateSecondaryAction;
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
            _root?.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        private void UnregisterCallbacks()
        {
            _backButton?.UnregisterCallback<ClickEvent>(OnBackClicked);
            _root?.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);

            if (_dataStateView != null)
            {
                _dataStateView.PrimaryActionRequested -= OnDataStatePrimaryAction;
                _dataStateView.SecondaryActionRequested -= OnDataStateSecondaryAction;
            }
        }

        private void BindContext(LeaderboardPreviewContext context)
        {
            if (context == null)
            {
                return;
            }

            SetLabelText(_overviewContext, context.ContextLabel);
            SetLabelText(_scopeValue, context.ScopeLabel);
            SetLabelText(_metricValue, context.MetricLabel);
            SetLabelText(_periodValue, context.PeriodLabel);
            SetLabelText(_standingsMetric, $"Metric: {context.MetricLabel}");
        }

        private void BindOwnPosition(LeaderboardPreviewData data)
        {
            LeaderboardPreviewEntry current = null;
            for (int i = 0; i < data.Entries.Count; i++)
            {
                if (data.Entries[i].IsCurrentStudent)
                {
                    current = data.Entries[i];
                    break;
                }
            }

            CurrentStudentEntry = current;
            if (current == null)
            {
                ApplyEmptyBoundLabels();
                return;
            }

            SetLabelText(_ownRank, current.Rank.ToString());
            SetLabelText(_ownName, current.PrivacySafeName);
            SetLabelText(
                _ownMetric,
                $"{FormatMissionsCompleted(current.MissionsCompleted)} completed");
            SetLabelText(
                _ownCount,
                $"{current.Rank} of {data.Entries.Count} displayed positions");
            SetLabelText(
                _standingsCount,
                $"{data.Entries.Count} displayed positions");
        }

        private void ApplyEmptyBoundLabels()
        {
            SetLabelText(_ownRank, "—");
            SetLabelText(_ownName, string.Empty);
            SetLabelText(_ownMetric, string.Empty);
            SetLabelText(_ownCount, "0 of 0 displayed positions");
            SetLabelText(_standingsCount, "0 displayed positions");
        }

        private void RebuildRows(IReadOnlyList<LeaderboardPreviewEntry> entries)
        {
            ClearRows();
            if (_list == null || entries == null)
            {
                return;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                VisualElement row = CreateRow(entries[i]);
                _list.Add(row);
                _rowElements.Add(row);
            }
        }

        private VisualElement CreateRow(LeaderboardPreviewEntry entry)
        {
            var row = new VisualElement();
            row.AddToClassList("leaderboard-panel__row");
            row.pickingMode = PickingMode.Ignore;

            if (entry.Rank == 1)
            {
                row.AddToClassList("leaderboard-panel__row--first");
            }
            else if (entry.Rank == 2)
            {
                row.AddToClassList("leaderboard-panel__row--second");
            }
            else if (entry.Rank == 3)
            {
                row.AddToClassList("leaderboard-panel__row--third");
            }

            if (entry.IsCurrentStudent)
            {
                row.AddToClassList("leaderboard-panel__row--current");
            }

            var rankHost = new VisualElement();
            rankHost.AddToClassList("leaderboard-panel__row-rank");
            rankHost.pickingMode = PickingMode.Ignore;

            if (entry.Rank == 1)
            {
                var icon = new VisualElement();
                icon.AddToClassList("ds-icon");
                icon.AddToClassList("ds-icon--trophy");
                icon.AddToClassList("leaderboard-panel__row-rank-icon");
                icon.pickingMode = PickingMode.Ignore;
                rankHost.Add(icon);
            }
            else if (entry.Rank == 2 || entry.Rank == 3)
            {
                var icon = new VisualElement();
                icon.AddToClassList("ds-icon");
                icon.AddToClassList("ds-icon--medal");
                icon.AddToClassList("leaderboard-panel__row-rank-icon");
                icon.pickingMode = PickingMode.Ignore;
                rankHost.Add(icon);
            }
            else
            {
                var circle = new VisualElement();
                circle.AddToClassList("leaderboard-panel__row-rank-circle");
                circle.pickingMode = PickingMode.Ignore;
                var numberInCircle = new Label(entry.Rank.ToString());
                numberInCircle.AddToClassList("leaderboard-panel__row-rank-number");
                numberInCircle.pickingMode = PickingMode.Ignore;
                circle.Add(numberInCircle);
                rankHost.Add(circle);
            }

            if (entry.Rank <= 3)
            {
                var number = new Label(entry.Rank.ToString());
                number.AddToClassList("leaderboard-panel__row-rank-number");
                number.pickingMode = PickingMode.Ignore;
                rankHost.Add(number);
            }

            var body = new VisualElement();
            body.AddToClassList("leaderboard-panel__row-body");
            body.pickingMode = PickingMode.Ignore;

            var identity = new VisualElement();
            identity.AddToClassList("leaderboard-panel__row-identity");
            identity.pickingMode = PickingMode.Ignore;

            var name = new Label(entry.PrivacySafeName);
            name.AddToClassList("leaderboard-panel__row-name");
            name.pickingMode = PickingMode.Ignore;
            identity.Add(name);

            if (entry.IsCurrentStudent)
            {
                var youChip = new VisualElement();
                youChip.AddToClassList("ds-chip");
                youChip.AddToClassList("leaderboard-panel__row-you-chip");
                youChip.pickingMode = PickingMode.Ignore;
                var youLabel = new Label("You");
                youLabel.AddToClassList("leaderboard-panel__row-you-label");
                youLabel.pickingMode = PickingMode.Ignore;
                youChip.Add(youLabel);
                identity.Add(youChip);
            }

            body.Add(identity);

            if (entry.IsCurrentStudent)
            {
                var helper = new Label("Your position is highlighted in the section standings.");
                helper.AddToClassList("leaderboard-panel__row-helper");
                helper.pickingMode = PickingMode.Ignore;
                body.Add(helper);
            }

            var metric = new Label(FormatMissionsCompleted(entry.MissionsCompleted));
            metric.AddToClassList("leaderboard-panel__row-metric");
            metric.pickingMode = PickingMode.Ignore;

            row.Add(rankHost);
            row.Add(body);
            row.Add(metric);
            return row;
        }

        private void ClearRows()
        {
            if (_list != null)
            {
                _list.Clear();
            }

            _rowElements.Clear();
        }

        private void ShowContent()
        {
            _contentShell?.RemoveFromClassList(ContentShellHiddenClass);
            if (_scroll != null)
            {
                _scroll.style.display = DisplayStyle.Flex;
            }

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

            _dataStateHost?.AddToClassList(DataStateHostVisibleClass);
            _dataStateView?.SetVisible(true);
        }

        private void ApplyRouteDataStateCopy(LeaderboardPreviewState state)
        {
            if (_dataStateView == null || !_dataStateView.IsBound)
            {
                return;
            }

            switch (state)
            {
                case LeaderboardPreviewState.Loading:
                    _dataStateView.Configure(
                        new DataStatePanelConfiguration(
                            "Loading leaderboard",
                            "Getting the latest section standings.",
                            "Leaderboard positions are provided by the NutriMind server.",
                            null,
                            string.Empty,
                            string.Empty,
                            true));
                    break;

                case LeaderboardPreviewState.Empty:
                    _dataStateView.Configure(
                        "No section standings yet",
                        "There is not enough eligible progress data to show a leaderboard.",
                        "Check again after your section completes more learning activities.",
                        "ds-icon--leaderboard",
                        "Back to Progress",
                        string.Empty);
                    break;

                case LeaderboardPreviewState.OfflineUnavailable:
                    _dataStateView.Configure(
                        "Leaderboard needs a connection",
                        "Section standings are not available offline.",
                        "Connect to the internet and try again.",
                        "ds-icon--wifi",
                        "Back to Progress",
                        string.Empty);
                    break;

                case LeaderboardPreviewState.RecoverableError:
                    _dataStateView.Configure(
                        "Leaderboard could not be loaded",
                        "Check your connection and try again.",
                        "No progress or ranking information was changed.",
                        "ds-icon--error",
                        "Try Again",
                        "Back to Progress");
                    break;
            }
        }

        private void OnBackClicked(ClickEvent evt) => BackToProgressRequested?.Invoke();

        private void OnDataStatePrimaryAction()
        {
            if (PreviewState == LeaderboardPreviewState.Empty
                || PreviewState == LeaderboardPreviewState.OfflineUnavailable)
            {
                BackToProgressRequested?.Invoke();
            }
            else if (PreviewState == LeaderboardPreviewState.RecoverableError)
            {
                RetryRequested?.Invoke();
            }
        }

        private void OnDataStateSecondaryAction()
        {
            if (PreviewState == LeaderboardPreviewState.RecoverableError)
            {
                BackToProgressRequested?.Invoke();
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

        private static void SetLabelText(Label label, string text)
        {
            if (label != null)
            {
                label.text = text ?? string.Empty;
            }
        }

        private static string FormatMissionsCompleted(int count) =>
            count == 1 ? "1 mission" : $"{count} missions";

        private static bool TryValidatePreviewData(
            LeaderboardPreviewData data,
            out string warning)
        {
            warning = null;
            if (data == null)
            {
                warning = "SetData received a null payload. LoadedEntryCount = 0.";
                return false;
            }

            if (data.Context == null)
            {
                warning = "Leaderboard preview context is null. LoadedEntryCount = 0.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(data.Context.ScopeLabel)
                || string.IsNullOrWhiteSpace(data.Context.MetricLabel)
                || string.IsNullOrWhiteSpace(data.Context.PeriodLabel)
                || string.IsNullOrWhiteSpace(data.Context.ContextLabel))
            {
                warning = "Leaderboard preview context labels must be nonempty. LoadedEntryCount = 0.";
                return false;
            }

            if (data.Entries == null || data.Entries.Count == 0)
            {
                warning = "Leaderboard preview entries are empty. LoadedEntryCount = 0.";
                return false;
            }

            var ranks = new HashSet<int>();
            var names = new HashSet<string>(StringComparer.Ordinal);
            int currentCount = 0;
            int lastSeenRank = 0;
            int previousMissions = int.MaxValue;

            for (int i = 0; i < data.Entries.Count; i++)
            {
                LeaderboardPreviewEntry entry = data.Entries[i];
                if (entry == null)
                {
                    warning = "Leaderboard preview contains a null entry. LoadedEntryCount = 0.";
                    return false;
                }

                if (entry.Rank < 1)
                {
                    warning = "Leaderboard preview ranks must be >= 1. LoadedEntryCount = 0.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(entry.PrivacySafeName))
                {
                    warning = "Leaderboard preview aliases must be nonempty. LoadedEntryCount = 0.";
                    return false;
                }

                if (entry.MissionsCompleted < 0)
                {
                    warning = "Leaderboard preview missions completed must be >= 0. LoadedEntryCount = 0.";
                    return false;
                }

                if (!ranks.Add(entry.Rank))
                {
                    warning = "Leaderboard preview contains duplicate ranks. LoadedEntryCount = 0.";
                    return false;
                }

                if (!names.Add(entry.PrivacySafeName))
                {
                    warning = "Leaderboard preview contains duplicate aliases. LoadedEntryCount = 0.";
                    return false;
                }

                if (FailsPrivacyQualityCheck(entry.PrivacySafeName))
                {
                    warning =
                        "Leaderboard preview alias failed privacy-quality checks. LoadedEntryCount = 0.";
                    return false;
                }

                if (entry.IsCurrentStudent)
                {
                    currentCount++;
                }

                if (entry.Rank <= lastSeenRank)
                {
                    warning = "Leaderboard preview rows must be ordered by ascending rank. LoadedEntryCount = 0.";
                    return false;
                }

                if (entry.Rank != lastSeenRank + 1)
                {
                    warning =
                        "Leaderboard preview ranks must be contiguous from 1. LoadedEntryCount = 0.";
                    return false;
                }

                if (entry.MissionsCompleted > previousMissions)
                {
                    warning =
                        "Leaderboard preview metric values must be non-increasing by rank. LoadedEntryCount = 0.";
                    return false;
                }

                lastSeenRank = entry.Rank;
                previousMissions = entry.MissionsCompleted;
            }

            if (currentCount != 1)
            {
                warning =
                    "Leaderboard preview requires exactly one current learner. LoadedEntryCount = 0.";
                return false;
            }

            if (data.Entries[0].Rank != 1 || data.Entries[data.Entries.Count - 1].Rank != data.Entries.Count)
            {
                warning =
                    "Leaderboard preview ranks must run 1 through count. LoadedEntryCount = 0.";
                return false;
            }

            return true;
        }

        private static bool FailsPrivacyQualityCheck(string alias)
        {
            if (string.IsNullOrWhiteSpace(alias))
            {
                return true;
            }

            if (alias.IndexOf('@') >= 0)
            {
                return true;
            }

            if (alias.IndexOf(".com", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            if (alias.IndexOf("LRN", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            return SixOrMoreDigits.IsMatch(alias);
        }
    }
}
