using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace NutriMind.App.UI
{
    /// <summary>
    /// Presentation-only preview payload for a Progress mission review request.
    /// Not a production or domain model.
    /// </summary>
    public readonly struct ProgressMissionPreviewSelection
    {
        public ProgressMissionPreviewSelection(
            NutriMindSubject subject,
            NutriMindTerm term,
            int missionNumber,
            string missionTitle,
            bool reviewRequired)
        {
            Subject = subject;
            Term = term;
            MissionNumber = missionNumber;
            MissionTitle = missionTitle;
            ReviewRequired = reviewRequired;
        }

        public NutriMindSubject Subject { get; }
        public NutriMindTerm Term { get; }
        public int MissionNumber { get; }
        public string MissionTitle { get; }
        public bool ReviewRequired { get; }
    }

    /// <summary>
    /// Presentation-only Progress route view. Binds static Grade 5 progress fixtures,
    /// reuses shared <see cref="DataStatePanelView"/> for non-content states, and raises
    /// user intent for the host to handle.
    /// </summary>
    public sealed class ProgressPanelView : IAppScreenView
    {
        private const string RootName = "progress-root";
        private const string SelectedClass = "is-selected";
        private const string CompactClass = "progress-panel--compact";
        private const string NarrowClass = "progress-panel--narrow";
        private const string MobileClass = "mobile";
        private const string DataStateHostVisibleClass = "progress-panel__data-state-host--visible";
        private const string ReviewBadgeHiddenClass = "progress-panel__review-badge--hidden";
        private const string ReviewButtonHiddenClass = "progress-panel__review-button--hidden";
        private const string MissionRowStatusPrefix = "progress-panel__mission-row--";
        private const float CompactBreakpoint = 1100f;
        private const float NarrowBreakpoint = 820f;

        private static readonly string[] MissionRowStatusClasses =
        {
            "progress-panel__mission-row--completed",
            "progress-panel__mission-row--in-progress",
            "progress-panel__mission-row--not-started",
            "progress-panel__mission-row--review-required"
        };

        private static readonly string[] StatusIconClasses =
        {
            "ds-icon--check",
            "ds-icon--refresh",
            "ds-icon--clock",
            "ds-icon--warning"
        };

        private static readonly SubjectFixture[] SubjectFixtures =
        {
            CreateLiteraQuestFixture(),
            CreatePeAndHealthFixture(),
            CreateScienceFixture()
        };

        private VisualElement _root;
        private ScrollView _scroll;
        private VisualElement _dataStateHost;
        private ProgressBar _overallProgress;
        private Button _subjectLiteraQuest;
        private Button _subjectPeAndHealth;
        private Button _subjectScience;
        private Button _term1Button;
        private Button _term2Button;
        private Button _term3Button;
        private Label _term1Percent;
        private Label _term1Missions;
        private Label _term1Reviews;
        private Label _term2Percent;
        private Label _term2Missions;
        private Label _term2Reviews;
        private Label _term3Percent;
        private Label _term3Missions;
        private Label _term3Reviews;
        private Button _openQuizPortalButton;
        private Button _viewLeaderboardButton;
        private readonly SubjectCardElements[] _subjectCards = new SubjectCardElements[3];
        private readonly MissionRowElements[] _missionRows = new MissionRowElements[5];

        private TemplateContainer _ownedDataStateInstance;
        private DataStatePanelView _dataStateView;
        private bool _warnedMissingDataStateAsset;
        private bool _disposed;
        private float _lastWidth = -1f;

        public ProgressPanelView(
            VisualElement root,
            VisualTreeAsset dataStatePanelAsset = null)
        {
            ResolveRoot(root);
            if (_root == null)
            {
                Debug.LogWarning(
                    "[ProgressPanelView] Could not resolve progress-root inside the supplied element.");
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

        public NutriMindSubject SelectedSubject { get; private set; } = NutriMindSubject.LiteraQuest;

        public NutriMindTerm SelectedTerm { get; private set; } = NutriMindTerm.Term1;

        public DataStatePanelState DataState { get; private set; } = DataStatePanelState.Content;

        public event Action<NutriMindSubject> SubjectSelected;
        public event Action<NutriMindTerm> TermSelected;
        public event Action<ProgressMissionPreviewSelection> MissionReviewRequested;
        public event Action QuizPortalRequested;
        public event Action LeaderboardRequested;
        public event Action RetryRequested;

        /// <summary>
        /// Restores retained subject/term selection without raising selection events.
        /// </summary>
        public void SetSelection(NutriMindSubject subject, NutriMindTerm term)
        {
            if (!IsBound)
            {
                return;
            }

            SelectedSubject = subject;
            SelectedTerm = term;
            ApplySelectionVisuals();
            ApplyTermFixtures();
            ApplyMissionFixtures();
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
            ApplyProgressDataStateCopy(state);
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

            SubjectSelected = null;
            TermSelected = null;
            MissionReviewRequested = null;
            QuizPortalRequested = null;
            LeaderboardRequested = null;
            RetryRequested = null;

            _root = null;
            _scroll = null;
            _dataStateHost = null;
            _overallProgress = null;
            _subjectLiteraQuest = null;
            _subjectPeAndHealth = null;
            _subjectScience = null;
            _term1Button = null;
            _term2Button = null;
            _term3Button = null;
            _term1Percent = null;
            _term1Missions = null;
            _term1Reviews = null;
            _term2Percent = null;
            _term2Missions = null;
            _term2Reviews = null;
            _term3Percent = null;
            _term3Missions = null;
            _term3Reviews = null;
            _openQuizPortalButton = null;
            _viewLeaderboardButton = null;
            for (int i = 0; i < _subjectCards.Length; i++)
            {
                _subjectCards[i] = default;
            }

            for (int i = 0; i < _missionRows.Length; i++)
            {
                _missionRows[i] = default;
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
            _scroll = _root.Q<ScrollView>("progress-scroll");
            _dataStateHost = _root.Q<VisualElement>("progress-data-state-host");
            _overallProgress = _root.Q<ProgressBar>("overall-progress-bar");

            _subjectLiteraQuest = _root.Q<Button>("progress-subject-lq");
            _subjectPeAndHealth = _root.Q<Button>("progress-subject-peh");
            _subjectScience = _root.Q<Button>("progress-subject-sci");
            _subjectCards[0] = CacheSubjectCard("lq");
            _subjectCards[1] = CacheSubjectCard("peh");
            _subjectCards[2] = CacheSubjectCard("sci");

            _term1Button = _root.Q<Button>("progress-term-1");
            _term2Button = _root.Q<Button>("progress-term-2");
            _term3Button = _root.Q<Button>("progress-term-3");
            _term1Percent = _root.Q<Label>("term-1-percent");
            _term1Missions = _root.Q<Label>("term-1-missions");
            _term1Reviews = _root.Q<Label>("term-1-reviews");
            _term2Percent = _root.Q<Label>("term-2-percent");
            _term2Missions = _root.Q<Label>("term-2-missions");
            _term2Reviews = _root.Q<Label>("term-2-reviews");
            _term3Percent = _root.Q<Label>("term-3-percent");
            _term3Missions = _root.Q<Label>("term-3-missions");
            _term3Reviews = _root.Q<Label>("term-3-reviews");

            _openQuizPortalButton = _root.Q<Button>("open-quiz-portal-button");
            _viewLeaderboardButton = _root.Q<Button>("progress-view-leaderboard-button");

            for (int i = 0; i < _missionRows.Length; i++)
            {
                int missionNumber = i + 1;
                VisualElement row = _root.Q<VisualElement>($"progress-mission-row-{missionNumber}");
                _missionRows[i] = new MissionRowElements(
                    row,
                    _root.Q<Label>($"mission-{missionNumber}-number"),
                    _root.Q<Label>($"mission-{missionNumber}-title"),
                    _root.Q<VisualElement>($"mission-{missionNumber}-status-icon"),
                    _root.Q<Label>($"mission-{missionNumber}-status-label"),
                    _root.Q<Label>($"mission-{missionNumber}-areas"),
                    _root.Q<Label>($"mission-{missionNumber}-collectibles"),
                    _root.Q<VisualElement>($"mission-{missionNumber}-review-badge"),
                    _root.Q<Button>($"mission-{missionNumber}-review-button"));
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
                        "[ProgressPanelView] DataStatePanel VisualTreeAsset is missing. " +
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
                    "[ProgressPanelView] Failed to bind nested DataStatePanelView.");
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
            _subjectLiteraQuest?.RegisterCallback<ClickEvent>(OnSubjectClicked);
            _subjectPeAndHealth?.RegisterCallback<ClickEvent>(OnSubjectClicked);
            _subjectScience?.RegisterCallback<ClickEvent>(OnSubjectClicked);

            _term1Button?.RegisterCallback<ClickEvent>(OnTermClicked);
            _term2Button?.RegisterCallback<ClickEvent>(OnTermClicked);
            _term3Button?.RegisterCallback<ClickEvent>(OnTermClicked);

            for (int i = 0; i < _missionRows.Length; i++)
            {
                _missionRows[i].ReviewButton?.RegisterCallback<ClickEvent>(OnReviewClicked);
            }

            _openQuizPortalButton?.RegisterCallback<ClickEvent>(OnQuizPortalClicked);
            _viewLeaderboardButton?.RegisterCallback<ClickEvent>(OnLeaderboardClicked);
            _root?.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);

            if (_dataStateView != null && _dataStateView.IsBound)
            {
                _dataStateView.PrimaryActionRequested += OnDataStatePrimaryAction;
                _dataStateView.SecondaryActionRequested += OnDataStateSecondaryAction;
            }
        }

        private void UnregisterCallbacks()
        {
            _subjectLiteraQuest?.UnregisterCallback<ClickEvent>(OnSubjectClicked);
            _subjectPeAndHealth?.UnregisterCallback<ClickEvent>(OnSubjectClicked);
            _subjectScience?.UnregisterCallback<ClickEvent>(OnSubjectClicked);

            _term1Button?.UnregisterCallback<ClickEvent>(OnTermClicked);
            _term2Button?.UnregisterCallback<ClickEvent>(OnTermClicked);
            _term3Button?.UnregisterCallback<ClickEvent>(OnTermClicked);

            for (int i = 0; i < _missionRows.Length; i++)
            {
                _missionRows[i].ReviewButton?.UnregisterCallback<ClickEvent>(OnReviewClicked);
            }

            _openQuizPortalButton?.UnregisterCallback<ClickEvent>(OnQuizPortalClicked);
            _viewLeaderboardButton?.UnregisterCallback<ClickEvent>(OnLeaderboardClicked);
            _root?.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);

            if (_dataStateView != null)
            {
                _dataStateView.PrimaryActionRequested -= OnDataStatePrimaryAction;
                _dataStateView.SecondaryActionRequested -= OnDataStateSecondaryAction;
            }
        }

        private void ApplyStaticFixtures()
        {
            if (_overallProgress != null)
            {
                _overallProgress.value = 67f;
            }

            ApplySubjectFixtures();
            ApplySelectionVisuals();
            ApplyTermFixtures();
            ApplyMissionFixtures();
        }

        private SubjectCardElements CacheSubjectCard(string suffix) =>
            new(
                _root.Q<Label>($"subject-{suffix}-percent"),
                _root.Q<ProgressBar>($"subject-{suffix}-progress"),
                _root.Q<Label>($"subject-{suffix}-missions"),
                _root.Q<Label>($"subject-{suffix}-reviews"),
                _root.Q<Label>($"subject-{suffix}-status"));

        private void ApplySubjectFixtures()
        {
            for (int i = 0; i < SubjectFixtures.Length && i < _subjectCards.Length; i++)
            {
                SubjectFixture fixture = SubjectFixtures[i];
                SubjectCardElements card = _subjectCards[i];

                if (card.Percentage != null)
                {
                    card.Percentage.text = $"{fixture.Percentage}%";
                }

                if (card.Progress != null)
                {
                    card.Progress.value = fixture.Percentage;
                }

                if (card.Missions != null)
                {
                    card.Missions.text = $"{fixture.MissionsCompleted} / 15 missions";
                }

                if (card.Reviews != null)
                {
                    card.Reviews.text = FormatReviewCount(fixture.ReviewCount);
                }

                if (card.Status != null)
                {
                    card.Status.text = fixture.AvailabilityText;
                }
            }
        }

        private void OnSubjectClicked(ClickEvent evt)
        {
            if (evt.currentTarget is not Button button)
            {
                return;
            }

            NutriMindSubject subject = ResolveSubject(button);
            if (subject == SelectedSubject)
            {
                return;
            }

            SelectedSubject = subject;
            NutriMindTerm previousTerm = SelectedTerm;
            SelectedTerm = NutriMindTerm.Term1;

            ApplySelectionVisuals();
            ApplyTermFixtures();
            ApplyMissionFixtures();

            SubjectSelected?.Invoke(subject);
            if (previousTerm != SelectedTerm)
            {
                TermSelected?.Invoke(SelectedTerm);
            }
        }

        private void OnTermClicked(ClickEvent evt)
        {
            if (evt.currentTarget is not Button button)
            {
                return;
            }

            NutriMindTerm term = ResolveTerm(button);
            if (term == SelectedTerm)
            {
                return;
            }

            SelectedTerm = term;
            ApplySelectionVisuals();
            ApplyMissionFixtures();
            TermSelected?.Invoke(term);
        }

        private void OnReviewClicked(ClickEvent evt)
        {
            if (evt.currentTarget is not Button button)
            {
                return;
            }

            int missionNumber = ResolveMissionNumber(button);
            MissionFixture mission = GetSelectedTermFixture().Missions[missionNumber - 1];
            if (!mission.ReviewRequired)
            {
                return;
            }

            MissionReviewRequested?.Invoke(
                new ProgressMissionPreviewSelection(
                    SelectedSubject,
                    SelectedTerm,
                    mission.MissionNumber,
                    mission.Title,
                    mission.ReviewRequired));
        }

        private void OnQuizPortalClicked(ClickEvent evt) => QuizPortalRequested?.Invoke();

        private void OnLeaderboardClicked(ClickEvent evt) => LeaderboardRequested?.Invoke();

        private void OnDataStatePrimaryAction()
        {
            switch (DataState)
            {
                case DataStatePanelState.OfflineCached:
                    SetDataState(DataStatePanelState.Content);
                    break;

                case DataStatePanelState.Empty:
                case DataStatePanelState.OfflineUnavailable:
                case DataStatePanelState.RecoverableError:
                    RetryRequested?.Invoke();
                    break;
            }
        }

        private void OnDataStateSecondaryAction()
        {
            if (DataState == DataStatePanelState.OfflineCached)
            {
                RetryRequested?.Invoke();
            }
        }

        private void ApplySelectionVisuals()
        {
            _subjectLiteraQuest?.EnableInClassList(
                SelectedClass,
                SelectedSubject == NutriMindSubject.LiteraQuest);
            _subjectPeAndHealth?.EnableInClassList(
                SelectedClass,
                SelectedSubject == NutriMindSubject.PeAndHealth);
            _subjectScience?.EnableInClassList(
                SelectedClass,
                SelectedSubject == NutriMindSubject.Science);

            _term1Button?.EnableInClassList(SelectedClass, SelectedTerm == NutriMindTerm.Term1);
            _term2Button?.EnableInClassList(SelectedClass, SelectedTerm == NutriMindTerm.Term2);
            _term3Button?.EnableInClassList(SelectedClass, SelectedTerm == NutriMindTerm.Term3);
        }

        private void ApplyTermFixtures()
        {
            SubjectFixture subject = GetSelectedSubjectFixture();
            ApplyTermCard(subject.Terms[0], _term1Percent, _term1Missions, _term1Reviews);
            ApplyTermCard(subject.Terms[1], _term2Percent, _term2Missions, _term2Reviews);
            ApplyTermCard(subject.Terms[2], _term3Percent, _term3Missions, _term3Reviews);
        }

        private static void ApplyTermCard(
            TermFixture term,
            Label percentLabel,
            Label missionsLabel,
            Label reviewsLabel)
        {
            if (percentLabel != null)
            {
                percentLabel.text = $"{term.Percentage}%";
            }

            if (missionsLabel != null)
            {
                missionsLabel.text = $"{term.MissionsCompleted} / 5 missions";
            }

            if (reviewsLabel != null)
            {
                reviewsLabel.text = FormatReviewCount(term.ReviewCount);
            }
        }

        private void ApplyMissionFixtures()
        {
            TermFixture term = GetSelectedTermFixture();
            for (int i = 0; i < _missionRows.Length; i++)
            {
                ApplyMissionRow(_missionRows[i], term.Missions[i]);
            }
        }

        private static void ApplyMissionRow(MissionRowElements row, MissionFixture mission)
        {
            if (row.Number != null)
            {
                row.Number.text = mission.MissionNumber.ToString();
            }

            if (row.Title != null)
            {
                row.Title.text = mission.Title;
            }

            if (row.StatusLabel != null)
            {
                row.StatusLabel.text = GetStatusLabel(mission.Status);
            }

            if (row.Areas != null)
            {
                row.Areas.text = $"{mission.AreasCompleted} / 3 areas";
            }

            if (row.Collectibles != null)
            {
                row.Collectibles.text = $"{mission.CollectiblesCompleted} / 3 collectibles";
            }

            if (row.Row != null)
            {
                foreach (string statusClass in MissionRowStatusClasses)
                {
                    row.Row.RemoveFromClassList(statusClass);
                }

                row.Row.AddToClassList(MissionRowStatusPrefix + GetStatusClassSuffix(mission));
            }

            if (row.StatusIcon != null)
            {
                foreach (string iconClass in StatusIconClasses)
                {
                    row.StatusIcon.RemoveFromClassList(iconClass);
                }

                row.StatusIcon.AddToClassList(GetStatusIconClass(mission.Status));
            }

            bool showReview = mission.ReviewRequired;
            row.ReviewBadge?.EnableInClassList(ReviewBadgeHiddenClass, !showReview);
            row.ReviewButton?.EnableInClassList(ReviewButtonHiddenClass, !showReview);
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

        private void ApplyProgressDataStateCopy(DataStatePanelState state)
        {
            if (_dataStateView == null || !_dataStateView.IsBound)
            {
                return;
            }

            switch (state)
            {
                case DataStatePanelState.Loading:
                    _dataStateView.Configure(
                        title: "Loading your progress",
                        message: "Gathering your mission and Quiz Portal progress.",
                        detail: string.Empty,
                        iconClass: "ds-icon--refresh",
                        primaryActionLabel: string.Empty,
                        secondaryActionLabel: string.Empty);
                    break;

                case DataStatePanelState.Empty:
                    _dataStateView.Configure(
                        title: "No progress yet",
                        message: "Complete a mission to begin tracking your learning journey.",
                        detail: "Your progress will appear here after your first saved activity.",
                        iconClass: "ds-icon--search",
                        primaryActionLabel: "Refresh",
                        secondaryActionLabel: string.Empty);
                    break;

                case DataStatePanelState.OfflineCached:
                    _dataStateView.Configure(
                        title: "Saved progress is available",
                        message: "This progress was saved on this device.",
                        detail: "Some classroom and Quiz Portal updates may appear after you reconnect.",
                        iconClass: "ds-icon--wifi",
                        primaryActionLabel: "View Saved Progress",
                        secondaryActionLabel: "Retry Connection");
                    break;

                case DataStatePanelState.OfflineUnavailable:
                    _dataStateView.Configure(
                        title: "Progress is unavailable offline",
                        message: "This device does not have saved progress for the learner.",
                        detail: "Reconnect to load the latest progress.",
                        iconClass: "ds-icon--wifi",
                        primaryActionLabel: "Try Again",
                        secondaryActionLabel: string.Empty);
                    break;

                case DataStatePanelState.RecoverableError:
                    _dataStateView.Configure(
                        title: "Progress could not be loaded",
                        message: "Something went wrong while preparing this progress view.",
                        detail: "Existing learner progress is safe. Try again.",
                        iconClass: "ds-icon--error",
                        primaryActionLabel: "Try Again",
                        secondaryActionLabel: string.Empty);
                    break;

                case DataStatePanelState.PermissionOrLocked:
                    _dataStateView.Configure(
                        title: "Progress is unavailable",
                        message: "Your classroom access does not currently allow this progress view.",
                        detail: "Ask your teacher if you think this is unexpected.",
                        iconClass: "ds-icon--lock",
                        primaryActionLabel: string.Empty,
                        secondaryActionLabel: string.Empty);
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

        private SubjectFixture GetSelectedSubjectFixture()
        {
            for (int i = 0; i < SubjectFixtures.Length; i++)
            {
                if (SubjectFixtures[i].Subject == SelectedSubject)
                {
                    return SubjectFixtures[i];
                }
            }

            return SubjectFixtures[0];
        }

        private TermFixture GetSelectedTermFixture()
        {
            SubjectFixture subject = GetSelectedSubjectFixture();
            int index = Mathf.Clamp((int)SelectedTerm - 1, 0, subject.Terms.Length - 1);
            return subject.Terms[index];
        }

        private static NutriMindSubject ResolveSubject(Button button)
        {
            return button.name switch
            {
                "progress-subject-lq" => NutriMindSubject.LiteraQuest,
                "progress-subject-peh" => NutriMindSubject.PeAndHealth,
                _ => NutriMindSubject.Science
            };
        }

        private static NutriMindTerm ResolveTerm(Button button)
        {
            return button.name switch
            {
                "progress-term-1" => NutriMindTerm.Term1,
                "progress-term-2" => NutriMindTerm.Term2,
                _ => NutriMindTerm.Term3
            };
        }

        private static int ResolveMissionNumber(Button button)
        {
            if (button == null || string.IsNullOrEmpty(button.name))
            {
                return 1;
            }

            // mission-N-review-button
            string[] parts = button.name.Split('-');
            if (parts.Length >= 2 && int.TryParse(parts[1], out int number))
            {
                return Mathf.Clamp(number, 1, 5);
            }

            return 1;
        }

        private static string FormatReviewCount(int count) =>
            count == 1 ? "1 review item" : $"{count} review items";

        private static string GetStatusLabel(MissionProgressStatus status) =>
            status switch
            {
                MissionProgressStatus.Completed => "Completed",
                MissionProgressStatus.InProgress => "In Progress",
                MissionProgressStatus.ReviewRequired => "Review Required",
                _ => "Not Started"
            };

        private static string GetStatusClassSuffix(MissionFixture mission)
        {
            if (mission.ReviewRequired)
            {
                return "review-required";
            }

            return mission.Status switch
            {
                MissionProgressStatus.Completed => "completed",
                MissionProgressStatus.InProgress => "in-progress",
                _ => "not-started"
            };
        }

        private static string GetStatusIconClass(MissionProgressStatus status) =>
            status switch
            {
                MissionProgressStatus.Completed => "ds-icon--check",
                MissionProgressStatus.InProgress => "ds-icon--refresh",
                MissionProgressStatus.ReviewRequired => "ds-icon--warning",
                _ => "ds-icon--clock"
            };

        private static SubjectFixture CreateLiteraQuestFixture() =>
            new(
                NutriMindSubject.LiteraQuest,
                60,
                9,
                2,
                "Available",
                new[]
                {
                    new TermFixture(
                        NutriMindTerm.Term1,
                        100,
                        5,
                        1,
                        new[]
                        {
                            Mission("The Festival Storybook Rescue", MissionProgressStatus.Completed, 3, 3, true),
                            Mission("The Bell of Seven Moments", MissionProgressStatus.Completed, 3, 3, false),
                            Mission("The Hall of Speaking Sounds", MissionProgressStatus.Completed, 3, 3, false),
                            Mission("The Newsroom of True Pages", MissionProgressStatus.Completed, 3, 3, false),
                            Mission("The Grand Holiday Chronicle", MissionProgressStatus.Completed, 3, 3, false)
                        }),
                    new TermFixture(
                        NutriMindTerm.Term2,
                        60,
                        3,
                        1,
                        new[]
                        {
                            Mission("The Moonlit Invitation", MissionProgressStatus.Completed, 3, 3, false),
                            Mission("The Lantern Street Chronicle", MissionProgressStatus.Completed, 3, 3, true),
                            Mission("The Two Neighborhood News Desk", MissionProgressStatus.Completed, 3, 3, false),
                            Mission("The Respectful Reporter", MissionProgressStatus.InProgress, 2, 1, false),
                            Mission("The Community Calendar", MissionProgressStatus.NotStarted, 0, 0, false)
                        }),
                    new TermFixture(
                        NutriMindTerm.Term3,
                        20,
                        1,
                        0,
                        new[]
                        {
                            Mission("The Weaver’s Permission", MissionProgressStatus.Completed, 3, 3, false),
                            Mission("The River Celebration Story", MissionProgressStatus.NotStarted, 0, 0, false),
                            Mission("The Mountain Memory Map", MissionProgressStatus.NotStarted, 0, 0, false),
                            Mission("The Festival Poster Problem", MissionProgressStatus.NotStarted, 0, 0, false),
                            Mission("The Living Heritage Exhibit", MissionProgressStatus.NotStarted, 0, 0, false)
                        })
                });

        private static SubjectFixture CreatePeAndHealthFixture() =>
            new(
                NutriMindSubject.PeAndHealth,
                67,
                10,
                1,
                "Not available in your classroom yet.",
                new[]
                {
                    new TermFixture(
                        NutriMindTerm.Term1,
                        100,
                        5,
                        0,
                        new[]
                        {
                            Mission("The Storm Inside", MissionProgressStatus.Completed, 3, 3, false),
                            Mission("The Brave Voice Bridge", MissionProgressStatus.Completed, 3, 3, false),
                            Mission("The Change Garden", MissionProgressStatus.Completed, 3, 3, false),
                            Mission("The Family Circle", MissionProgressStatus.Completed, 3, 3, false),
                            Mission("The Active Court", MissionProgressStatus.Completed, 3, 3, false)
                        }),
                    new TermFixture(
                        NutriMindTerm.Term2,
                        60,
                        3,
                        1,
                        new[]
                        {
                            Mission("The Label Lantern", MissionProgressStatus.Completed, 3, 3, false),
                            Mission("The Safe Dose Trail", MissionProgressStatus.Completed, 3, 3, true),
                            Mission("The Gateway Fog", MissionProgressStatus.Completed, 3, 3, false),
                            Mission("The Fitness Battery", MissionProgressStatus.InProgress, 1, 1, false),
                            Mission("The Rhythm Remedy", MissionProgressStatus.NotStarted, 0, 0, false)
                        }),
                    new TermFixture(
                        NutriMindTerm.Term3,
                        40,
                        2,
                        0,
                        new[]
                        {
                            Mission("Safe Home Workshop", MissionProgressStatus.Completed, 3, 3, false),
                            Mission("School and Community Watch", MissionProgressStatus.Completed, 3, 3, false),
                            Mission("Roadwise Crossing", MissionProgressStatus.InProgress, 2, 2, false),
                            Mission("Injury-Free Arena", MissionProgressStatus.NotStarted, 0, 0, false),
                            Mission("Festival of Safe Movement", MissionProgressStatus.NotStarted, 0, 0, false)
                        })
                });

        private static SubjectFixture CreateScienceFixture() =>
            new(
                NutriMindSubject.Science,
                73,
                11,
                2,
                "Available",
                new[]
                {
                    new TermFixture(
                        NutriMindTerm.Term1,
                        100,
                        5,
                        0,
                        new[]
                        {
                            Mission("The Vanishing Supply Cart", MissionProgressStatus.Completed, 3, 3, false),
                            Mission("The Shape-and-Volume Harbor", MissionProgressStatus.Completed, 3, 3, false),
                            Mission("The Young Investigator’s Kit", MissionProgressStatus.Completed, 3, 3, false),
                            Mission("The Living Things Sorting Forest", MissionProgressStatus.Completed, 3, 3, false),
                            Mission("The Survival Garden", MissionProgressStatus.Completed, 3, 3, false)
                        }),
                    new TermFixture(
                        NutriMindTerm.Term2,
                        80,
                        4,
                        1,
                        new[]
                        {
                            Mission("The Two Body Pathways", MissionProgressStatus.Completed, 3, 3, false),
                            Mission("The Growing Life Gallery", MissionProgressStatus.Completed, 3, 3, true),
                            Mission("Camouflage Creek", MissionProgressStatus.Completed, 3, 3, false),
                            Mission("Push–Pull Workshop", MissionProgressStatus.Completed, 3, 3, false),
                            Mission("Friction Hill and Gravity Drop", MissionProgressStatus.NotStarted, 0, 0, false)
                        }),
                    new TermFixture(
                        NutriMindTerm.Term3,
                        40,
                        2,
                        1,
                        new[]
                        {
                            Mission("The Sparkless Workshop", MissionProgressStatus.Completed, 3, 3, true),
                            Mission("The Rock and Landform Trail", MissionProgressStatus.Completed, 3, 3, false),
                            Mission("The Erosion River and Water Cycle", MissionProgressStatus.InProgress, 2, 1, false),
                            Mission("The Storm Signal Station", MissionProgressStatus.NotStarted, 0, 0, false),
                            Mission("Moonlight Solar Harbor", MissionProgressStatus.NotStarted, 0, 0, false)
                        })
                });

        private static MissionFixture Mission(
            string title,
            MissionProgressStatus status,
            int areasCompleted,
            int collectiblesCompleted,
            bool reviewRequired) =>
            new(0, title, status, areasCompleted, collectiblesCompleted, reviewRequired);

        private enum MissionProgressStatus
        {
            Completed,
            InProgress,
            NotStarted,
            ReviewRequired
        }

        private readonly struct SubjectFixture
        {
            public SubjectFixture(
                NutriMindSubject subject,
                int percentage,
                int missionsCompleted,
                int reviewCount,
                string availabilityText,
                TermFixture[] terms)
            {
                Subject = subject;
                Percentage = percentage;
                MissionsCompleted = missionsCompleted;
                ReviewCount = reviewCount;
                AvailabilityText = availabilityText;
                Terms = terms;
            }

            public NutriMindSubject Subject { get; }
            public int Percentage { get; }
            public int MissionsCompleted { get; }
            public int ReviewCount { get; }
            public string AvailabilityText { get; }
            public TermFixture[] Terms { get; }
        }

        private readonly struct TermFixture
        {
            public TermFixture(
                NutriMindTerm term,
                int percentage,
                int missionsCompleted,
                int reviewCount,
                MissionFixture[] missions)
            {
                Term = term;
                Percentage = percentage;
                MissionsCompleted = missionsCompleted;
                ReviewCount = reviewCount;
                Missions = NormalizeMissionNumbers(missions);
            }

            public NutriMindTerm Term { get; }
            public int Percentage { get; }
            public int MissionsCompleted { get; }
            public int ReviewCount { get; }
            public MissionFixture[] Missions { get; }

            private static MissionFixture[] NormalizeMissionNumbers(MissionFixture[] missions)
            {
                var normalized = new MissionFixture[missions.Length];
                for (int i = 0; i < missions.Length; i++)
                {
                    MissionFixture source = missions[i];
                    normalized[i] = new MissionFixture(
                        i + 1,
                        source.Title,
                        source.Status,
                        source.AreasCompleted,
                        source.CollectiblesCompleted,
                        source.ReviewRequired);
                }

                return normalized;
            }
        }

        private readonly struct MissionFixture
        {
            public MissionFixture(
                int missionNumber,
                string title,
                MissionProgressStatus status,
                int areasCompleted,
                int collectiblesCompleted,
                bool reviewRequired)
            {
                MissionNumber = missionNumber;
                Title = title;
                Status = status;
                AreasCompleted = areasCompleted;
                CollectiblesCompleted = collectiblesCompleted;
                ReviewRequired = reviewRequired;
            }

            public int MissionNumber { get; }
            public string Title { get; }
            public MissionProgressStatus Status { get; }
            public int AreasCompleted { get; }
            public int CollectiblesCompleted { get; }
            public bool ReviewRequired { get; }
        }

        private readonly struct MissionRowElements
        {
            public MissionRowElements(
                VisualElement row,
                Label number,
                Label title,
                VisualElement statusIcon,
                Label statusLabel,
                Label areas,
                Label collectibles,
                VisualElement reviewBadge,
                Button reviewButton)
            {
                Row = row;
                Number = number;
                Title = title;
                StatusIcon = statusIcon;
                StatusLabel = statusLabel;
                Areas = areas;
                Collectibles = collectibles;
                ReviewBadge = reviewBadge;
                ReviewButton = reviewButton;
            }

            public VisualElement Row { get; }
            public Label Number { get; }
            public Label Title { get; }
            public VisualElement StatusIcon { get; }
            public Label StatusLabel { get; }
            public Label Areas { get; }
            public Label Collectibles { get; }
            public VisualElement ReviewBadge { get; }
            public Button ReviewButton { get; }
        }

        private readonly struct SubjectCardElements
        {
            public SubjectCardElements(
                Label percentage,
                ProgressBar progress,
                Label missions,
                Label reviews,
                Label status)
            {
                Percentage = percentage;
                Progress = progress;
                Missions = missions;
                Reviews = reviews;
                Status = status;
            }

            public Label Percentage { get; }
            public ProgressBar Progress { get; }
            public Label Missions { get; }
            public Label Reviews { get; }
            public Label Status { get; }
        }
    }
}
