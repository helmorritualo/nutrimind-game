using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace NutriMind.App.UI
{
    public enum MissionDetailPreviewState { Content = 0, Loading = 1, Locked = 2, OfflineUnavailable = 3, RecoverableError = 4 }
    public enum MissionDetailProgressState { Locked = 0, Available = 1, Started = 2, InProgress = 3, ReviewRequired = 4, MissionCompleted = 5 }
    public enum MissionDetailAreaState { Locked = 0, Available = 1, Started = 2, InProgress = 3, ReviewRequired = 4, CollectibleUnlocked = 5, CollectibleCollected = 6, Completed = 7 }
    public enum MissionDetailAreaPhase { DiscoverAndConnect = 0, PracticeAndApply = 1, ResolveAndMaster = 2 }
    public enum MissionDetailLocalAvailability { Downloaded = 0, NotDownloaded = 1, OfflineUnavailable = 2, Unknown = 3 }
    public enum MissionDetailPrimaryAction { Start = 0, Continue = 1, Review = 2 }

    public sealed class MissionDetailAreaAuthoredContent
    {
        public MissionDetailAreaAuthoredContent(string areaId, int areaNumber, string title, MissionDetailAreaPhase phase, string gameplayStory)
        {
            AreaId = areaId ?? string.Empty;
            AreaNumber = areaNumber;
            Title = title ?? string.Empty;
            Phase = phase;
            GameplayStory = gameplayStory ?? string.Empty;
        }

        public string AreaId { get; }
        public int AreaNumber { get; }
        public string Title { get; }
        public MissionDetailAreaPhase Phase { get; }
        public string GameplayStory { get; }
    }

    public sealed class MissionDetailAuthoredContent
    {
        public MissionDetailAuthoredContent(string missionId, NutriMindSubject subject, NutriMindTerm term, int missionNumber, string title, string curriculumBlock, string storyPremise, string learningFocus, string collectibleType, string rewardDescription, IReadOnlyList<MissionDetailAreaAuthoredContent> areas)
        {
            MissionId = missionId ?? string.Empty;
            Subject = subject;
            Term = term;
            MissionNumber = missionNumber;
            Title = title ?? string.Empty;
            CurriculumBlock = curriculumBlock ?? string.Empty;
            StoryPremise = storyPremise ?? string.Empty;
            LearningFocus = learningFocus ?? string.Empty;
            CollectibleType = collectibleType ?? string.Empty;
            RewardDescription = rewardDescription ?? string.Empty;
            Areas = CopyList(areas);
        }

        public string MissionId { get; }
        public NutriMindSubject Subject { get; }
        public NutriMindTerm Term { get; }
        public int MissionNumber { get; }
        public string Title { get; }
        public string CurriculumBlock { get; }
        public string StoryPremise { get; }
        public string LearningFocus { get; }
        public string CollectibleType { get; }
        public string RewardDescription { get; }
        public IReadOnlyList<MissionDetailAreaAuthoredContent> Areas { get; }

        private static IReadOnlyList<T> CopyList<T>(IReadOnlyList<T> source)
        {
            if (source == null)
            {
                return Array.Empty<T>();
            }

            var copy = new T[source.Count];
            for (int i = 0; i < source.Count; i++)
            {
                copy[i] = source[i];
            }

            return copy;
        }
    }

    public sealed class MissionDetailAreaProgressContent
    {
        public MissionDetailAreaProgressContent(string areaId, MissionDetailAreaState state, bool reviewRequired, bool collectibleCollected)
        {
            AreaId = areaId ?? string.Empty;
            State = state;
            ReviewRequired = reviewRequired;
            CollectibleCollected = collectibleCollected;
        }

        public string AreaId { get; }
        public MissionDetailAreaState State { get; }
        public bool ReviewRequired { get; }
        public bool CollectibleCollected { get; }
    }

    public sealed class MissionDetailProgressContent
    {
        public MissionDetailProgressContent(MissionDetailProgressState state, string activeAreaId, int completedAreaCount, int requiredAreaCount, int collectibleCount, int requiredCollectibleCount, IReadOnlyList<MissionDetailAreaProgressContent> areas)
        {
            State = state;
            ActiveAreaId = activeAreaId;
            CompletedAreaCount = completedAreaCount;
            RequiredAreaCount = requiredAreaCount;
            CollectibleCount = collectibleCount;
            RequiredCollectibleCount = requiredCollectibleCount;
            Areas = CopyList(areas);
        }

        public MissionDetailProgressState State { get; }
        public string ActiveAreaId { get; }
        public int CompletedAreaCount { get; }
        public int RequiredAreaCount { get; }
        public int CollectibleCount { get; }
        public int RequiredCollectibleCount { get; }
        public IReadOnlyList<MissionDetailAreaProgressContent> Areas { get; }

        private static IReadOnlyList<T> CopyList<T>(IReadOnlyList<T> source)
        {
            if (source == null)
            {
                return Array.Empty<T>();
            }

            var copy = new T[source.Count];
            for (int i = 0; i < source.Count; i++)
            {
                copy[i] = source[i];
            }

            return copy;
        }
    }

    public sealed class MissionDetailPreviewContent
    {
        public MissionDetailPreviewContent(MissionDetailAuthoredContent authored, MissionDetailProgressContent progress, MissionDetailLocalAvailability localAvailability, string classroomAvailabilityText, string prerequisiteText)
        {
            Authored = authored;
            Progress = progress;
            LocalAvailability = localAvailability;
            ClassroomAvailabilityText = classroomAvailabilityText ?? string.Empty;
            PrerequisiteText = prerequisiteText ?? string.Empty;
        }

        public MissionDetailAuthoredContent Authored { get; }
        public MissionDetailProgressContent Progress { get; }
        public MissionDetailLocalAvailability LocalAvailability { get; }
        public string ClassroomAvailabilityText { get; }
        public string PrerequisiteText { get; }
    }

    public readonly struct MissionDetailPreviewActionRequest
    {
        public MissionDetailPreviewActionRequest(string missionId, string missionTitle, MissionDetailPrimaryAction action)
        {
            MissionId = missionId ?? string.Empty;
            MissionTitle = missionTitle ?? string.Empty;
            Action = action;
        }

        public string MissionId { get; }
        public string MissionTitle { get; }
        public MissionDetailPrimaryAction Action { get; }
    }

    public static class MissionDetailPreviewCatalog
    {
        public static bool TryGetContent(MissionPreviewSelection selection, out MissionDetailPreviewContent content)
        {
            content = null;
            if (selection.Subject != NutriMindSubject.LiteraQuest || selection.Term != NutriMindTerm.Term1)
            {
                return false;
            }

            content = selection.MissionNumber switch
            {
                1 when Matches(selection, "g5_lq_t1_m01", 1, "The Festival Storybook Rescue") => CreateMissionOne(),
                2 when Matches(selection, "g5_lq_t1_m02", 2, "The Bell of Seven Moments") => CreateMissionTwo(),
                3 when Matches(selection, "g5_lq_t1_m03", 3, "The Hall of Speaking Sounds") => CreateMissionThree(),
                _ => null
            };
            return content != null;
        }

        public static MissionPreviewSelection CreateCanonicalDefaultSelection() =>
            new("g5_lq_t1_m02", NutriMindSubject.LiteraQuest, NutriMindTerm.Term1, 2, "The Bell of Seven Moments", false, string.Empty);

        private static bool Matches(MissionPreviewSelection selection, string missionId, int missionNumber, string title) =>
            string.Equals(selection.MissionId, missionId, StringComparison.Ordinal)
            && selection.Subject == NutriMindSubject.LiteraQuest
            && selection.Term == NutriMindTerm.Term1
            && selection.MissionNumber == missionNumber
            && string.Equals(selection.MissionTitle, title, StringComparison.Ordinal);

        private static MissionDetailPreviewContent CreateMissionOne()
        {
            var areas = new[]
            {
                new MissionDetailAreaAuthoredContent("g5_lq_t1_m01_a01", 1, "Parade Meadow", MissionDetailAreaPhase.DiscoverAndConnect, "Farmer Lira asks the Pathfinder to repair the opening section of the festival chapter. The learner observes a short illustrated scene, identifies the character, setting, goal, and first event, then repairs banner captions using clear noun and pronoun references. The repaired banner route opens the path deeper into the festival and reveals Story Fragment 1."),
                new MissionDetailAreaAuthoredContent("g5_lq_t1_m01_a02", 2, "Drumbeat Lane", MissionDetailAreaPhase.PracticeAndApply, "The parade has lost its written rhythm and its lantern poster communicates the wrong mood. The learner restores action instructions using appropriate verb forms and complements, compares visual layouts and tone, and chooses evidence that supports the intended meaning. The corrected drum-and-lantern sequence reveals Story Fragment 2."),
                new MissionDetailAreaAuthoredContent("g5_lq_t1_m01_a03", 3, "Freedom Stage", MissionDetailAreaPhase.ResolveAndMaster, "At the final stage, the learner arranges seven illustrated events into a coherent narrative, selects the best title and main idea, and completes a short ending. The integrated final challenge reuses the strongest story-grammar, language, and visual-literacy concepts from the mission. The restored festival chapter reveals Story Fragment 3 and completes the mission.")
            };
            return Create(
                "g5_lq_t1_m01", 1, "The Festival Storybook Rescue", "Weeks 1–2",
                "On the morning of Bayang Haraya’s Freedom and Friendship Festival, the town storybook loses its first chapter. Farmer Lira remembers pieces of the parade story, but the Haze has scattered the events, captions, and illustrations across three connected festival zones. The Pathfinder must restore the chapter in correct order before the opening ceremony.",
                "Story grammar; sequential plot; main idea; collective/concrete/abstract nouns; demonstrative and relative pronouns; verb-forming suffixes; helping/linking/transitive verbs; noun complements; narrative text; layout, tone, and mood.",
                "Story Fragment", "Three Story Fragments form the Festival Chapter and unlock Mission 2.", areas,
                MissionDetailProgressState.MissionCompleted, null, 3, 3,
                MissionDetailAreaState.Completed, MissionDetailAreaState.Completed, MissionDetailAreaState.Completed);
        }

        private static MissionDetailPreviewContent CreateMissionTwo()
        {
            var areas = new[]
            {
                new MissionDetailAreaAuthoredContent("g5_lq_t1_m02_a01", 1, "Clock Gate", MissionDetailAreaPhase.DiscoverAndConnect, "Collect seven time-stamped clues and order them. Progressive verb forms show what was happening at each moment. Read short witness statements, infer feelings and traits, and use adverbs of manner and time to distinguish observation from assumption."),
                new MissionDetailAreaAuthoredContent("g5_lq_t1_m02_a02", 2, "Analogy Alley", MissionDetailAreaPhase.PracticeAndApply, "Unlock a dictionary cabinet, use analogies and context to clarify unfamiliar words, and reject a caption that relies on an age or social stereotype. Write a formal compound-complex statement that explains what happened and predicts the bell keeper’s reasonable next action."),
                new MissionDetailAreaAuthoredContent("g5_lq_t1_m02_a03", 3, "Bell Tower", MissionDetailAreaPhase.ResolveAndMaster, "Summarize the seven-event sequence, draw a conclusion supported by clues, and decide which details could happen in real life. The player presents a concise summary to the town council. The bell rings in the restored order, and the misleading poster is replaced by a fair visual account.")
            };
            return Create(
                "g5_lq_t1_m02", 2, "The Bell of Seven Moments", "Weeks 3–4",
                "The memorial bell rings seven times, but every witness remembers the order differently. A young bell keeper is blamed unfairly because of a misleading poster. The Pathfinder investigates the seven moments, corrects the record, and restores the bell’s true story.",
                "Sequencing at least seven events; main idea and summary; progressive tenses; adverbs of manner and time; character feelings and traits; prediction, conclusion, real-life possibility; analogy and dictionary use; formal tone; compound-complex sentences; visual purpose and stereotypes.",
                "Story Fragment", "Seven-Moment Story Fragment set and the Bell Keeper badge.", areas,
                MissionDetailProgressState.InProgress, "g5_lq_t1_m02_a03", 2, 2,
                MissionDetailAreaState.Completed, MissionDetailAreaState.Completed, MissionDetailAreaState.InProgress);
        }

        private static MissionDetailPreviewContent CreateMissionThree()
        {
            var areas = new[]
            {
                new MissionDetailAreaAuthoredContent("g5_lq_t1_m03_a01", 1, "Echo Arcade", MissionDetailAreaPhase.DiscoverAndConnect, "Match sound words and sound devices to scenes without turning the passage into noise. Repair short lines using alliteration, assonance, and consonance while keeping the meaning clear."),
                new MissionDetailAreaAuthoredContent("g5_lq_t1_m03_a02", 2, "Figure Studio", MissionDetailAreaPhase.PracticeAndApply, "Interpret simile, metaphor, and personification, then choose which comparison best fits a scene. Guide performers to use facial expression, gesture, eye contact, posture, and spacing that support the message."),
                new MissionDetailAreaAuthoredContent("g5_lq_t1_m03_a03", 3, "Cultural Gallery", MissionDetailAreaPhase.ResolveAndMaster, "Rearrange adjective series and visual elements, remove an inappropriate stereotype, and assemble a culturally respectful story panel. The troupe performs the restored scene. The player identifies how sound, figure of speech, gesture, and visual design work together.")
            };
            return Create(
                "g5_lq_t1_m03", 3, "The Hall of Speaking Sounds", "Weeks 5–6",
                "A hall of oral stories has gone silent because the sounds, comparisons, and gestures were separated from their meanings. The Pathfinder helps performers rebuild a respectful community presentation.",
                "Onomatopoeia, alliteration, assonance, consonance; simile, metaphor, and personification; adjective order; non-verbal cues; cultural appropriateness; creation of a visual narrative.",
                "Story Fragment", "Voice-and-Image Story Fragment set.", areas,
                MissionDetailProgressState.Available, null, 0, 0,
                MissionDetailAreaState.Available, MissionDetailAreaState.Locked, MissionDetailAreaState.Locked);
        }

        private static MissionDetailPreviewContent Create(
            string missionId, int missionNumber, string title, string curriculumBlock, string premise, string learningFocus, string collectibleType, string reward, IReadOnlyList<MissionDetailAreaAuthoredContent> authoredAreas,
            MissionDetailProgressState state, string activeAreaId, int completedCount, int collectibleCount,
            MissionDetailAreaState firstState, MissionDetailAreaState secondState, MissionDetailAreaState thirdState)
        {
            var progressAreas = new[]
            {
                new MissionDetailAreaProgressContent(authoredAreas[0].AreaId, firstState, false, firstState == MissionDetailAreaState.Completed),
                new MissionDetailAreaProgressContent(authoredAreas[1].AreaId, secondState, false, secondState == MissionDetailAreaState.Completed),
                new MissionDetailAreaProgressContent(authoredAreas[2].AreaId, thirdState, false, thirdState == MissionDetailAreaState.Completed)
            };
            return new MissionDetailPreviewContent(
                new MissionDetailAuthoredContent(missionId, NutriMindSubject.LiteraQuest, NutriMindTerm.Term1, missionNumber, title, curriculumBlock, premise, learningFocus, collectibleType, reward, authoredAreas),
                new MissionDetailProgressContent(state, activeAreaId, completedCount, 3, collectibleCount, 3, progressAreas),
                MissionDetailLocalAvailability.Downloaded, "Published to your classroom", "No prerequisite");
        }
    }

    public sealed class MissionDetailPanelView : IAppScreenView
    {
        private const string RootName = "mission-detail-root";
        private const string CompactClass = "mission-detail-panel--compact";
        private const string NarrowClass = "mission-detail-panel--narrow";
        private const string MobileClass = "mobile";
        private const string HiddenClass = "mission-detail-panel__hidden";
        private const string DataStateVisibleClass = "mission-detail-panel__data-state-host--visible";
        private const float CompactBreakpoint = 1100f;
        private const float NarrowBreakpoint = 820f;

        private VisualElement _root;
        private VisualElement _contentShell;
        private ScrollView _scroll;
        private VisualElement _body;
        private VisualElement _hero;
        private Label _eyebrow;
        private Label _title;
        private Label _curriculumBlock;
        private VisualElement _statusChip;
        private VisualElement _statusIcon;
        private Label _statusLabel;
        private Label _premise;
        private VisualElement _heroSummary;
        private Label _summaryTitle;
        private Label _areasValue;
        private Label _collectiblesValue;
        private Label _currentArea;
        private Label _offlineStatus;
        private VisualElement _offlineIcon;
        private Label _learningTitle;
        private Label _learningText;
        private VisualElement _areaList;
        private Label _collectibleTitle;
        private Label _collectibleCount;
        private VisualElement _collectibleList;
        private Label _classroomValue;
        private Label _prerequisiteValue;
        private Label _downloadValue;
        private VisualElement _footer;
        private Button _backButton;
        private Button _primaryButton;
        private VisualElement _dataStateHost;
        private TemplateContainer _ownedDataStateInstance;
        private DataStatePanelView _dataStateView;
        private EventCallback<ClickEvent> _backClicked;
        private EventCallback<ClickEvent> _primaryClicked;
        private EventCallback<GeometryChangedEvent> _geometryChanged;
        private bool _warnedMissingDataStateAsset;
        private bool _warnedInvalidContent;
        private bool _disposed;
        private float _lastWidth = -1f;

        public MissionDetailPanelView(VisualElement root, VisualTreeAsset dataStatePanelAsset)
        {
            ResolveRoot(root);
            if (_root == null)
            {
                return;
            }

            CacheElements();
            BindDataStatePanel(dataStatePanelAsset);
            RegisterCallbacks();
            SetPreviewState(MissionDetailPreviewState.Content);
            ApplyResponsiveClasses(_root.resolvedStyle.width);
        }

        public VisualElement Root => _root;
        public bool IsBound => _root != null && !_disposed;
        public MissionDetailPreviewState PreviewState { get; private set; }
        public MissionDetailPreviewContent Content { get; private set; }
        public event Action BackRequested;
        public event Action<MissionDetailPreviewActionRequest> PrimaryActionRequested;
        public event Action RetryRequested;

        public void SetContent(MissionDetailPreviewContent content)
        {
            if (!IsBound)
            {
                return;
            }

            if (!TryValidateContent(content, out string warning))
            {
                if (!_warnedInvalidContent)
                {
                    Debug.LogWarning($"[MissionDetailPanelView] Invalid mission detail content. {warning}");
                    _warnedInvalidContent = true;
                }

                Content = null;
                SetPreviewState(MissionDetailPreviewState.RecoverableError);
                return;
            }

            _warnedInvalidContent = false;
            Content = content;
            ApplyContent(content);
        }

        public void SetPreviewState(MissionDetailPreviewState state)
        {
            if (!IsBound)
            {
                return;
            }

            if (state != MissionDetailPreviewState.Content && (_dataStateView == null || !_dataStateView.IsBound))
            {
                return;
            }

            PreviewState = state;
            if (state == MissionDetailPreviewState.Content)
            {
                ShowContent();
                if (Content != null)
                {
                    ApplyContent(Content);
                }

                _dataStateView?.SetState(DataStatePanelState.Content);
                return;
            }

            HideContent();
            switch (state)
            {
                case MissionDetailPreviewState.Loading:
                    _dataStateView.SetState(DataStatePanelState.Loading);
                    _dataStateView.Configure(new DataStatePanelConfiguration("Loading mission details", "Getting mission availability and progress.", "Your mission story and learning content remain stored with the game.", null, string.Empty, string.Empty, true));
                    break;
                case MissionDetailPreviewState.Locked:
                    _dataStateView.SetState(DataStatePanelState.PermissionOrLocked);
                    _dataStateView.Configure("Mission is locked", "This mission is not available to start yet.", "Return to Missions to view its current requirement.", "ds-icon--lock", "Back to Missions", string.Empty);
                    break;
                case MissionDetailPreviewState.OfflineUnavailable:
                    _dataStateView.SetState(DataStatePanelState.OfflineUnavailable);
                    _dataStateView.Configure("Mission unavailable offline", "Connect to the internet to check this mission or make it available for offline play.", "Your existing mission progress is safe on this device.", "ds-icon--wifi", "Try Again", "Back to Missions");
                    break;
                case MissionDetailPreviewState.RecoverableError:
                    _dataStateView.SetState(DataStatePanelState.RecoverableError);
                    _dataStateView.Configure("Mission details could not be loaded", "Check your connection and try again.", "No mission progress was changed.", "ds-icon--error", "Try Again", "Back to Missions");
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
            DisposeOwnedDataState();
            BackRequested = null;
            PrimaryActionRequested = null;
            RetryRequested = null;
            Content = null;
            _root = null;
            _contentShell = null;
            _scroll = null;
            _body = null;
            _hero = null;
            _eyebrow = null;
            _title = null;
            _curriculumBlock = null;
            _statusChip = null;
            _statusIcon = null;
            _statusLabel = null;
            _premise = null;
            _heroSummary = null;
            _summaryTitle = null;
            _areasValue = null;
            _collectiblesValue = null;
            _currentArea = null;
            _offlineStatus = null;
            _offlineIcon = null;
            _learningTitle = null;
            _learningText = null;
            _areaList = null;
            _collectibleTitle = null;
            _collectibleCount = null;
            _collectibleList = null;
            _classroomValue = null;
            _prerequisiteValue = null;
            _downloadValue = null;
            _footer = null;
            _backButton = null;
            _primaryButton = null;
            _dataStateHost = null;
        }

        private void ResolveRoot(VisualElement root)
        {
            _root = root == null ? null : root.name == RootName ? root : root.Q<VisualElement>(RootName);
        }

        private void CacheElements()
        {
            _contentShell = _root.Q<VisualElement>("mission-detail-content-shell");
            _scroll = _root.Q<ScrollView>("mission-detail-scroll");
            _body = _root.Q<VisualElement>("mission-detail-body");
            _hero = _root.Q<VisualElement>("mission-detail-hero");
            _eyebrow = _root.Q<Label>("mission-detail-eyebrow");
            _title = _root.Q<Label>("mission-detail-title");
            _curriculumBlock = _root.Q<Label>("mission-detail-curriculum-block");
            _statusChip = _root.Q<VisualElement>("mission-detail-status-chip");
            _statusIcon = _root.Q<VisualElement>("mission-detail-status-icon");
            _statusLabel = _root.Q<Label>("mission-detail-status-label");
            _premise = _root.Q<Label>("mission-detail-premise");
            _heroSummary = _root.Q<VisualElement>("mission-detail-hero-summary");
            _summaryTitle = _root.Q<Label>("mission-detail-summary-title");
            _areasValue = _root.Q<Label>("mission-detail-areas-value");
            _collectiblesValue = _root.Q<Label>("mission-detail-collectibles-value");
            _currentArea = _root.Q<Label>("mission-detail-current-area");
            _offlineStatus = _root.Q<Label>("mission-detail-offline-status");
            _offlineIcon = _root.Q<VisualElement>("mission-detail-offline-icon");
            _learningTitle = _root.Q<Label>("mission-detail-learning-title");
            _learningText = _root.Q<Label>("mission-detail-learning-text");
            _areaList = _root.Q<VisualElement>("mission-detail-area-list");
            _collectibleTitle = _root.Q<Label>("mission-detail-collectible-title");
            _collectibleCount = _root.Q<Label>("mission-detail-collectible-count");
            _collectibleList = _root.Q<VisualElement>("mission-detail-collectible-list");
            _classroomValue = _root.Q<Label>("mission-detail-classroom-value");
            _prerequisiteValue = _root.Q<Label>("mission-detail-prerequisite-value");
            _downloadValue = _root.Q<Label>("mission-detail-download-value");
            _footer = _root.Q<VisualElement>("mission-detail-footer");
            _backButton = _root.Q<Button>("mission-detail-back-button");
            _primaryButton = _root.Q<Button>("mission-detail-primary-button");
            _dataStateHost = _root.Q<VisualElement>("mission-detail-data-state-host");
        }

        private void BindDataStatePanel(VisualTreeAsset dataStatePanelAsset)
        {
            if (_dataStateHost == null || dataStatePanelAsset == null)
            {
                if (!_warnedMissingDataStateAsset)
                {
                    Debug.LogWarning("[MissionDetailPanelView] DataStatePanel VisualTreeAsset is missing. Content remains usable; non-content states are no-ops.");
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
                Debug.LogWarning("[MissionDetailPanelView] Failed to bind DataStatePanelView from the assigned asset.");
                DisposeOwnedDataState();
                return;
            }

            _dataStateView.PrimaryActionRequested += OnDataStatePrimaryAction;
            _dataStateView.SecondaryActionRequested += OnDataStateSecondaryAction;
            _dataStateView.SetVisible(false);
        }

        private void RegisterCallbacks()
        {
            _backClicked = _ => BackRequested?.Invoke();
            _primaryClicked = _ => RaisePrimaryAction();
            _geometryChanged = OnGeometryChanged;
            _backButton?.RegisterCallback(_backClicked);
            _primaryButton?.RegisterCallback(_primaryClicked);
            _root?.RegisterCallback(_geometryChanged);
        }

        private void UnregisterCallbacks()
        {
            if (_backButton != null && _backClicked != null) _backButton.UnregisterCallback(_backClicked);
            if (_primaryButton != null && _primaryClicked != null) _primaryButton.UnregisterCallback(_primaryClicked);
            if (_root != null && _geometryChanged != null) _root.UnregisterCallback(_geometryChanged);
            _backClicked = null;
            _primaryClicked = null;
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

        private void ApplyContent(MissionDetailPreviewContent content)
        {
            MissionDetailAuthoredContent authored = content.Authored;
            MissionDetailProgressContent progress = content.Progress;
            if (_eyebrow != null) _eyebrow.text = $"LiteraQuest • Term 1 • Mission {authored.MissionNumber}";
            if (_title != null) _title.text = authored.Title;
            if (_curriculumBlock != null) _curriculumBlock.text = authored.CurriculumBlock;
            if (_premise != null) _premise.text = authored.StoryPremise;
            if (_summaryTitle != null) _summaryTitle.text = "Your progress";
            if (_areasValue != null) _areasValue.text = $"{progress.CompletedAreaCount} of {progress.RequiredAreaCount} areas";
            if (_collectiblesValue != null) _collectiblesValue.text = $"{progress.CollectibleCount} of {progress.RequiredCollectibleCount} {authored.CollectibleType}s";
            if (_learningTitle != null) _learningTitle.text = "Learning focus";
            if (_learningText != null) _learningText.text = authored.LearningFocus;
            if (_collectibleTitle != null) _collectibleTitle.text = $"{authored.CollectibleType}s";
            if (_collectibleCount != null) _collectibleCount.text = $"{progress.CollectibleCount} of {progress.RequiredCollectibleCount} collected";
            if (_classroomValue != null) _classroomValue.text = content.ClassroomAvailabilityText;
            if (_prerequisiteValue != null) _prerequisiteValue.text = content.PrerequisiteText;
            if (_downloadValue != null) _downloadValue.text = GetOfflineStatus(content.LocalAvailability);
            if (_offlineStatus != null) _offlineStatus.text = GetOfflineStatus(content.LocalAvailability);
            ApplyStatus(progress.State);
            ApplyCurrentArea(authored, progress.ActiveAreaId);
            RebuildAreaCards(authored.Areas, progress.Areas);
            RebuildCollectibleSlots(authored.CollectibleType, progress.Areas);
            ApplyPrimaryAction(progress.State);
        }

        private void ApplyStatus(MissionDetailProgressState state)
        {
            if (_statusLabel != null) _statusLabel.text = GetMissionStatusLabel(state);
            SetIconClass(_statusIcon, GetMissionIconClass(state));
            SetIconClass(_offlineIcon, GetOfflineIconClass(Content?.LocalAvailability ?? MissionDetailLocalAvailability.Unknown));
        }

        private void ApplyCurrentArea(MissionDetailAuthoredContent authored, string activeAreaId)
        {
            if (_currentArea == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(activeAreaId))
            {
                _currentArea.text = "Current area: None";
                return;
            }

            for (int i = 0; i < authored.Areas.Count; i++)
            {
                if (string.Equals(authored.Areas[i].AreaId, activeAreaId, StringComparison.Ordinal))
                {
                    _currentArea.text = $"Current area: {authored.Areas[i].Title}";
                    return;
                }
            }

            _currentArea.text = "Current area: —";
        }

        private void RebuildAreaCards(IReadOnlyList<MissionDetailAreaAuthoredContent> authoredAreas, IReadOnlyList<MissionDetailAreaProgressContent> progressAreas)
        {
            _areaList?.Clear();
            if (_areaList == null) return;
            for (int i = 0; i < authoredAreas.Count; i++)
            {
                MissionDetailAreaAuthoredContent authored = authoredAreas[i];
                MissionDetailAreaProgressContent progress = progressAreas[i];
                var card = new VisualElement();
                card.AddToClassList("mission-detail-area-card");
                card.AddToClassList("ds-card");
                card.AddToClassList("ds-card--flush");
                card.AddToClassList(GetAreaModifierClass(progress.State));
                if (i == authoredAreas.Count - 1)
                {
                    card.AddToClassList("mission-detail-area-card--last");
                }
                card.pickingMode = PickingMode.Ignore;
                var eyebrow = new Label($"Area {authored.AreaNumber}");
                eyebrow.AddToClassList("mission-detail-area-card__eyebrow");
                eyebrow.pickingMode = PickingMode.Ignore;
                var phase = new Label(GetPhaseLabel(authored.Phase) + (authored.Phase == MissionDetailAreaPhase.ResolveAndMaster ? " • Integrated mission challenge" : string.Empty));
                phase.AddToClassList("mission-detail-area-card__phase");
                phase.pickingMode = PickingMode.Ignore;
                var title = new Label(authored.Title);
                title.AddToClassList("mission-detail-area-card__title");
                title.pickingMode = PickingMode.Ignore;
                var story = new Label(authored.GameplayStory);
                story.AddToClassList("mission-detail-area-card__story");
                story.pickingMode = PickingMode.Ignore;
                var chip = new Label(GetAreaStatusLabel(progress.State));
                chip.AddToClassList("ds-chip");
                chip.AddToClassList("mission-detail-area-card__state");
                chip.pickingMode = PickingMode.Ignore;
                var collectible = new Label(progress.CollectibleCollected ? "Story Fragment collected" : "Story Fragment not yet collected");
                collectible.AddToClassList("mission-detail-area-card__collectible");
                collectible.pickingMode = PickingMode.Ignore;
                card.Add(eyebrow);
                card.Add(phase);
                card.Add(title);
                card.Add(story);
                card.Add(chip);
                card.Add(collectible);
                _areaList.Add(card);
            }
        }

        private void RebuildCollectibleSlots(string collectibleType, IReadOnlyList<MissionDetailAreaProgressContent> progressAreas)
        {
            _collectibleList?.Clear();
            if (_collectibleList == null) return;
            for (int i = 0; i < 3; i++)
            {
                bool collected = progressAreas[i].CollectibleCollected;
                var slot = new VisualElement();
                slot.AddToClassList("mission-detail-collectible-slot");
                slot.AddToClassList("ds-card");
                slot.AddToClassList("ds-card--flush");
                slot.EnableInClassList("mission-detail-collectible-slot--collected", collected);
                if (i == 2)
                {
                    slot.AddToClassList("mission-detail-collectible-slot--last");
                }
                slot.pickingMode = PickingMode.Ignore;
                var icon = new VisualElement();
                icon.AddToClassList("ds-icon");
                icon.AddToClassList(collected ? "ds-icon--check" : progressAreas[i].State == MissionDetailAreaState.CollectibleUnlocked ? "ds-icon--trophy" : "ds-icon--lock");
                icon.AddToClassList("mission-detail-collectible-slot__icon");
                icon.pickingMode = PickingMode.Ignore;
                var title = new Label($"{collectibleType} {i + 1}");
                title.AddToClassList("mission-detail-collectible-slot__title");
                title.pickingMode = PickingMode.Ignore;
                var status = new Label(collected ? "Collected" : "Not yet collected");
                status.AddToClassList("mission-detail-collectible-slot__status");
                status.pickingMode = PickingMode.Ignore;
                slot.Add(icon);
                slot.Add(title);
                slot.Add(status);
                _collectibleList.Add(slot);
            }
        }

        private void ApplyPrimaryAction(MissionDetailProgressState state)
        {
            if (_primaryButton == null) return;
            if (!TryGetPrimaryAction(state, out MissionDetailPrimaryAction action, out string label))
            {
                _primaryButton.style.display = DisplayStyle.None;
                _primaryButton.SetEnabled(false);
                return;
            }

            _primaryButton.style.display = DisplayStyle.Flex;
            _primaryButton.SetEnabled(true);
            _primaryButton.text = label;
            _primaryButton.userData = action;
        }

        private void RaisePrimaryAction()
        {
            if (Content == null || !TryGetPrimaryAction(Content.Progress.State, out MissionDetailPrimaryAction action, out _))
            {
                return;
            }

            PrimaryActionRequested?.Invoke(new MissionDetailPreviewActionRequest(Content.Authored.MissionId, Content.Authored.Title, action));
        }

        private void OnDataStatePrimaryAction()
        {
            if (PreviewState == MissionDetailPreviewState.Locked)
            {
                BackRequested?.Invoke();
            }
            else if (PreviewState == MissionDetailPreviewState.OfflineUnavailable || PreviewState == MissionDetailPreviewState.RecoverableError)
            {
                RetryRequested?.Invoke();
            }
        }

        private void OnDataStateSecondaryAction()
        {
            if (PreviewState == MissionDetailPreviewState.OfflineUnavailable || PreviewState == MissionDetailPreviewState.RecoverableError)
            {
                BackRequested?.Invoke();
            }
        }

        private void ShowContent()
        {
            _contentShell?.RemoveFromClassList(HiddenClass);
            if (_scroll != null) _scroll.style.display = DisplayStyle.Flex;
            if (_footer != null) _footer.style.display = DisplayStyle.Flex;
            _dataStateHost?.RemoveFromClassList(DataStateVisibleClass);
        }

        private void HideContent()
        {
            _contentShell?.AddToClassList(HiddenClass);
            if (_scroll != null) _scroll.style.display = DisplayStyle.None;
            if (_footer != null) _footer.style.display = DisplayStyle.None;
            _dataStateHost?.AddToClassList(DataStateVisibleClass);
        }

        private void OnGeometryChanged(GeometryChangedEvent evt) => ApplyResponsiveClasses(evt.newRect.width);

        private void ApplyResponsiveClasses(float width)
        {
            if (_root == null || float.IsNaN(width) || width <= 0f || Mathf.Approximately(width, _lastWidth)) return;
            _lastWidth = width;
            bool compact = width < CompactBreakpoint;
            bool narrow = width < NarrowBreakpoint;
            _root.EnableInClassList(CompactClass, compact);
            _root.EnableInClassList(NarrowClass, narrow);
            _root.EnableInClassList(MobileClass, narrow);
        }

        private static bool TryValidateContent(MissionDetailPreviewContent content, out string warning)
        {
            if (content?.Authored == null || content.Progress == null) { warning = "Authored or progress content was null."; return false; }
            MissionDetailAuthoredContent authored = content.Authored;
            MissionDetailProgressContent progress = content.Progress;
            if (string.IsNullOrWhiteSpace(authored.MissionId) || authored.Subject != NutriMindSubject.LiteraQuest || authored.Term != NutriMindTerm.Term1 || authored.MissionNumber < 1 || string.IsNullOrWhiteSpace(authored.Title)) { warning = "Mission identity was invalid."; return false; }
            if (authored.Areas == null || progress.Areas == null || authored.Areas.Count != 3 || progress.Areas.Count != 3) { warning = "Exactly three authored and progress areas are required."; return false; }
            if (progress.RequiredAreaCount != 3 || progress.RequiredCollectibleCount != 3 || progress.CompletedAreaCount < 0 || progress.CompletedAreaCount > 3 || progress.CollectibleCount < 0 || progress.CollectibleCount > 3) { warning = "Required or completed counts were invalid."; return false; }
            var ids = new HashSet<string>(StringComparer.Ordinal);
            int completed = 0;
            int collected = 0;
            bool activeFound = string.IsNullOrWhiteSpace(progress.ActiveAreaId);
            for (int i = 0; i < 3; i++)
            {
                MissionDetailAreaAuthoredContent area = authored.Areas[i];
                MissionDetailAreaProgressContent areaProgress = progress.Areas[i];
                if (area == null || areaProgress == null || area.AreaNumber != i + 1 || area.Phase != (MissionDetailAreaPhase)i || string.IsNullOrWhiteSpace(area.AreaId) || !ids.Add(area.AreaId) || !string.Equals(area.AreaId, areaProgress.AreaId, StringComparison.Ordinal)) { warning = "Area identifiers, order, or phases were invalid."; return false; }
                if (areaProgress.State == MissionDetailAreaState.Completed) completed++;
                if (areaProgress.CollectibleCollected) collected++;
                if (string.Equals(progress.ActiveAreaId, area.AreaId, StringComparison.Ordinal)) activeFound = true;
            }
            if (!activeFound || completed != progress.CompletedAreaCount || collected != progress.CollectibleCount) { warning = "Active area or derived counts did not match area progress."; return false; }
            if (!TryGetPrimaryAction(progress.State, out _, out _) && progress.State != MissionDetailProgressState.Locked) { warning = "Mission state does not have a valid primary action."; return false; }
            warning = null;
            return true;
        }

        private static bool TryGetPrimaryAction(MissionDetailProgressState state, out MissionDetailPrimaryAction action, out string label)
        {
            switch (state)
            {
                case MissionDetailProgressState.Available: action = MissionDetailPrimaryAction.Start; label = "Start Mission"; return true;
                case MissionDetailProgressState.Started:
                case MissionDetailProgressState.InProgress:
                case MissionDetailProgressState.ReviewRequired: action = MissionDetailPrimaryAction.Continue; label = "Continue Mission"; return true;
                case MissionDetailProgressState.MissionCompleted: action = MissionDetailPrimaryAction.Review; label = "Review Mission"; return true;
                default: action = default; label = string.Empty; return false;
            }
        }

        private static void SetIconClass(VisualElement icon, string iconClass)
        {
            if (icon == null) return;
            icon.RemoveFromClassList("ds-icon--check");
            icon.RemoveFromClassList("ds-icon--play");
            icon.RemoveFromClassList("ds-icon--flag");
            icon.RemoveFromClassList("ds-icon--lock");
            icon.RemoveFromClassList("ds-icon--wifi");
            icon.AddToClassList("ds-icon");
            icon.AddToClassList(iconClass);
        }

        private static string GetMissionStatusLabel(MissionDetailProgressState state) => state switch
        {
            MissionDetailProgressState.Locked => "Locked",
            MissionDetailProgressState.Available => "Available",
            MissionDetailProgressState.Started => "Started",
            MissionDetailProgressState.InProgress => "In Progress",
            MissionDetailProgressState.ReviewRequired => "Review Required",
            MissionDetailProgressState.MissionCompleted => "Completed",
            _ => "—"
        };

        private static string GetAreaStatusLabel(MissionDetailAreaState state) => state switch
        {
            MissionDetailAreaState.Locked => "Locked",
            MissionDetailAreaState.Available => "Available",
            MissionDetailAreaState.Started => "Started",
            MissionDetailAreaState.InProgress => "In Progress",
            MissionDetailAreaState.ReviewRequired => "Review Required",
            MissionDetailAreaState.CollectibleUnlocked => "Collectible unlocked",
            MissionDetailAreaState.CollectibleCollected => "Collected",
            MissionDetailAreaState.Completed => "Completed",
            _ => "—"
        };

        private static string GetMissionIconClass(MissionDetailProgressState state) => state switch
        {
            MissionDetailProgressState.Locked => "ds-icon--lock",
            MissionDetailProgressState.Available => "ds-icon--play",
            MissionDetailProgressState.Started or MissionDetailProgressState.InProgress => "ds-icon--flag",
            _ => "ds-icon--check"
        };

        private static string GetAreaModifierClass(MissionDetailAreaState state) => state switch
        {
            MissionDetailAreaState.Completed or MissionDetailAreaState.CollectibleCollected => "mission-detail-area-card--completed",
            MissionDetailAreaState.Started or MissionDetailAreaState.InProgress or MissionDetailAreaState.ReviewRequired or MissionDetailAreaState.CollectibleUnlocked => "mission-detail-area-card--progress",
            MissionDetailAreaState.Available => "mission-detail-area-card--available",
            _ => "mission-detail-area-card--locked"
        };

        private static string GetPhaseLabel(MissionDetailAreaPhase phase) => phase switch
        {
            MissionDetailAreaPhase.DiscoverAndConnect => "Discover and Connect",
            MissionDetailAreaPhase.PracticeAndApply => "Practice and Apply",
            MissionDetailAreaPhase.ResolveAndMaster => "Resolve and Master",
            _ => "—"
        };

        private static string GetOfflineStatus(MissionDetailLocalAvailability availability) => availability switch
        {
            MissionDetailLocalAvailability.Downloaded => "Downloaded • Available offline",
            MissionDetailLocalAvailability.NotDownloaded => "Not downloaded on this device",
            MissionDetailLocalAvailability.OfflineUnavailable => "Unavailable offline",
            _ => "Availability unknown"
        };

        private static string GetOfflineIconClass(MissionDetailLocalAvailability availability) =>
            availability == MissionDetailLocalAvailability.Downloaded ? "ds-icon--check" : "ds-icon--wifi";
    }
}
