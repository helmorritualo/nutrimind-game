using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace NutriMind.App.UI
{
    public readonly struct MissionPreviewSelection
    {
        public MissionPreviewSelection(
            string missionId,
            NutriMindSubject subject,
            NutriMindTerm term,
            int missionNumber,
            string missionTitle,
            bool isLocked,
            string lockReason)
        {
            MissionId = missionId;
            Subject = subject;
            Term = term;
            MissionNumber = missionNumber;
            MissionTitle = missionTitle;
            IsLocked = isLocked;
            LockReason = lockReason;
        }

        public string MissionId { get; }
        public NutriMindSubject Subject { get; }
        public NutriMindTerm Term { get; }
        public int MissionNumber { get; }
        public string MissionTitle { get; }
        public bool IsLocked { get; }
        public string LockReason { get; }
    }

    /// <summary>
    /// Presentation-only Mission Selection route view. Binds local preview fixtures
    /// and raises user intent for the host to handle.
    /// </summary>
    public sealed class MissionSelectionPanelView : IAppScreenView
    {
        private const string RootName = "mission-selection-root";
        private const string SelectedClass = "is-selected";
        private const string StatusModifierPrefix = "mission-selection__detail-status--";
        private const string PrimaryActionLockedClass = "mission-selection__primary-action--locked";
        private const string CompactClass = "mission-selection--compact";
        private const string NarrowClass = "mission-selection--narrow";
        private const string MobileClass = "mobile";
        private const float CompactBreakpoint = 1100f;
        private const float NarrowBreakpoint = 820f;

        /// <summary>
        /// Canonical subject for the five-item LiteraQuest Term 1 static preview catalog.
        /// </summary>
        private const NutriMindSubject CatalogSubject = NutriMindSubject.LiteraQuest;

        /// <summary>
        /// Canonical term for the five-item LiteraQuest Term 1 static preview catalog.
        /// </summary>
        private const NutriMindTerm CatalogTerm = NutriMindTerm.Term1;

        private static readonly string[] StatusStates = { "completed", "progress", "available", "locked" };

        private readonly Dictionary<string, MissionPreviewData> _missionData = new()
        {
            ["mission-item-1"] = new MissionPreviewData(
                "g5_lq_t1_m01",
                1,
                "The Festival Storybook Rescue",
                "On the morning of Bayang Haraya’s Freedom and Friendship Festival, the town storybook loses its first chapter. Farmer Lira remembers pieces of the parade story, but the Haze has scattered the events, captions, and illustrations across three connected festival zones. The Pathfinder must restore the chapter in correct order before the opening ceremony.",
                "Story grammar; sequential plot; main idea; collective/concrete/abstract nouns; demonstrative and relative pronouns; verb-forming suffixes; helping/linking/transitive verbs; noun complements; narrative text; layout, tone, and mood.",
                3,
                3,
                3,
                3,
                "Completed",
                "completed",
                "No prerequisite",
                "Published to your classroom",
                "Downloaded • Available offline",
                "Review Mission",
                false,
                string.Empty),
            ["mission-item-2"] = new MissionPreviewData(
                "g5_lq_t1_m02",
                2,
                "The Bell of Seven Moments",
                "The memorial bell rings seven times, but every witness remembers the order differently. A young bell keeper is blamed unfairly because of a misleading poster. The Pathfinder investigates the seven moments, corrects the record, and restores the bell’s true story.",
                "Sequencing at least seven events; main idea and summary; progressive tenses; adverbs of manner and time; character feelings and traits; prediction, conclusion, real-life possibility; analogy and dictionary use; formal tone; compound-complex sentences; visual purpose and stereotypes.",
                2,
                3,
                2,
                3,
                "In Progress",
                "progress",
                "No prerequisite",
                "Published to your classroom",
                "Downloaded • Available offline",
                "Continue Mission",
                false,
                string.Empty),
            ["mission-item-3"] = new MissionPreviewData(
                "g5_lq_t1_m03",
                3,
                "The Hall of Speaking Sounds",
                "A hall of oral stories has gone silent because the sounds, comparisons, and gestures were separated from their meanings. The Pathfinder helps performers rebuild a respectful community presentation.",
                "Onomatopoeia, alliteration, assonance, consonance; simile, metaphor, and personification; adjective order; non-verbal cues; cultural appropriateness; creation of a visual narrative.",
                0,
                3,
                0,
                3,
                "Available",
                "available",
                "No prerequisite",
                "Published to your classroom",
                "Downloaded • Available offline",
                "Start Mission",
                false,
                string.Empty),
            ["mission-item-4"] = new MissionPreviewData(
                "g5_lq_t1_m04",
                4,
                "The Newsroom of True Pages",
                "Festival reports have been mixed with rumors. The town newsroom cannot publish until the Pathfinder separates evidence from opinion and rebuilds the report.",
                "Informational text; topic, main idea, and supporting details; explanation and news report; author’s purpose; fact, opinion, and fact-based opinion; formal/informal tone; visual and multimedia meaning.",
                0,
                3,
                0,
                3,
                "Prerequisite Locked",
                "locked",
                "Requires: The Hall of Speaking Sounds (Mission 3)",
                "Published to your classroom",
                "Downloaded • Available offline",
                "Back to Missions",
                true,
                "Prerequisite Locked"),
            ["mission-item-5"] = new MissionPreviewData(
                "g5_lq_t1_m05",
                5,
                "The Grand Holiday Chronicle",
                "The restored pages must be bound into the Grand Holiday Chronicle, but each district wants its part placed first. The Pathfinder must create one fair, coherent record for the whole community.",
                "Term synthesis: narrative and expository organization, story elements, vocabulary and grammar, audience awareness, visual composition, summary, and cultural appropriateness.",
                0,
                3,
                0,
                3,
                "Teacher Locked",
                "locked",
                "Prerequisite complete — no additional missions required.",
                "Waiting for classroom release",
                "Not downloaded on this device",
                "Back to Missions",
                true,
                "Teacher Locked")
        };

        private VisualElement _root;
        private Button _backButton;
        private VisualElement _missionList;
        private Label _termHeading;
        private Label _detailTitle;
        private Label _detailDescription;
        private Label _detailLearningGoal;
        private VisualElement _detailStatus;
        private Label _detailStatusLabel;
        private Label _detailAreasProgress;
        private VisualElement _detailAreasFill;
        private Label _detailCollectiblesProgress;
        private VisualElement _detailCollectiblesFill;
        private Label _detailPrerequisite;
        private Label _detailClassroom;
        private Label _detailDownloaded;
        private Button _primaryActionButton;
        private Label _primaryActionLabel;
        private bool _warnedNonCanonicalContext;
        private bool _disposed;
        private float _lastWidth = -1f;

        public MissionSelectionPanelView(VisualElement root)
        {
            ResolveRoot(root);
            if (_root == null)
            {
                Debug.LogWarning("[MissionSelectionPanelView] Could not resolve mission-selection-root inside the supplied element.");
                return;
            }

            CacheElements();
            RegisterCallbacks();
            SelectMission(_root.Q<Button>("mission-item-2"), false);
            ApplyResponsiveClasses(_root.resolvedStyle.width);
        }

        public VisualElement Root => _root;
        public bool IsBound => _root != null && !_disposed;
        public NutriMindSubject Subject { get; private set; } = CatalogSubject;
        public NutriMindTerm Term { get; private set; } = CatalogTerm;
        public int SelectedMissionNumber { get; private set; } = 2;

        public event Action BackRequested;
        public event Action<MissionPreviewSelection> MissionSelected;
        public event Action<MissionPreviewSelection> StartMissionRequested;
        public event Action<MissionPreviewSelection> ContinueMissionRequested;
        public event Action<MissionPreviewSelection> ReviewMissionRequested;
        public event Action<MissionPreviewSelection> LockedMissionRequested;

        /// <summary>
        /// Keeps the LiteraQuest Term 1 static preview catalog. Non-canonical contexts
        /// log one warning per view instance and do not relabel or replace mission cards.
        /// </summary>
        public void SetContext(NutriMindSubject subject, NutriMindTerm term)
        {
            if (!IsBound)
            {
                return;
            }

            Subject = CatalogSubject;
            Term = CatalogTerm;

            if ((subject != CatalogSubject || term != CatalogTerm) && !_warnedNonCanonicalContext)
            {
                Debug.LogWarning(
                    $"[MissionSelectionPanelView] Requested {GetSubjectLabel(subject)} Term {(int)term}, " +
                    "but this static preview contains only LiteraQuest Term 1. " +
                    "Using the canonical preview catalog.");
                _warnedNonCanonicalContext = true;
            }

            if (_termHeading != null)
            {
                _termHeading.text = $"Term {(int)CatalogTerm}: {GetTermTitle(CatalogSubject, CatalogTerm)}";
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
            BackRequested = null;
            MissionSelected = null;
            StartMissionRequested = null;
            ContinueMissionRequested = null;
            ReviewMissionRequested = null;
            LockedMissionRequested = null;
            _root = null;
            _backButton = null;
            _missionList = null;
            _termHeading = null;
            _detailTitle = null;
            _detailDescription = null;
            _detailLearningGoal = null;
            _detailStatus = null;
            _detailStatusLabel = null;
            _detailAreasProgress = null;
            _detailAreasFill = null;
            _detailCollectiblesProgress = null;
            _detailCollectiblesFill = null;
            _detailPrerequisite = null;
            _detailClassroom = null;
            _detailDownloaded = null;
            _primaryActionButton = null;
            _primaryActionLabel = null;
            _warnedNonCanonicalContext = false;
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
            _backButton = _root.Q<Button>("back-button");
            _missionList = _root.Q<VisualElement>("mission-list");
            _termHeading = _root.Q<Label>("term-heading");
            _detailTitle = _root.Q<Label>("detail-title");
            _detailDescription = _root.Q<Label>("detail-description");
            _detailLearningGoal = _root.Q<Label>("detail-learning-goal");
            _detailStatus = _root.Q<VisualElement>("detail-status");
            _detailStatusLabel = _root.Q<Label>("detail-status-label");
            _detailAreasProgress = _root.Q<Label>("detail-areas-progress");
            _detailAreasFill = _root.Q<VisualElement>("detail-areas-fill");
            _detailCollectiblesProgress = _root.Q<Label>("detail-collectibles-progress");
            _detailCollectiblesFill = _root.Q<VisualElement>("detail-collectibles-fill");
            _detailPrerequisite = _root.Q<Label>("detail-prerequisite");
            _detailClassroom = _root.Q<Label>("detail-classroom");
            _detailDownloaded = _root.Q<Label>("detail-downloaded");
            _primaryActionButton = _root.Q<Button>("primary-action-button");
            _primaryActionLabel = _root.Q<Label>("primary-action-label");
        }

        private void RegisterCallbacks()
        {
            _backButton?.RegisterCallback<ClickEvent>(OnBackClicked);
            _primaryActionButton?.RegisterCallback<ClickEvent>(OnPrimaryActionClicked);
            if (_missionList != null)
            {
                foreach (Button button in _missionList.Query<Button>(className: "mission-selection__item").ToList())
                {
                    button.RegisterCallback<ClickEvent>(OnMissionClicked);
                }
            }

            _root?.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        private void UnregisterCallbacks()
        {
            _backButton?.UnregisterCallback<ClickEvent>(OnBackClicked);
            _primaryActionButton?.UnregisterCallback<ClickEvent>(OnPrimaryActionClicked);
            if (_missionList != null)
            {
                foreach (Button button in _missionList.Query<Button>(className: "mission-selection__item").ToList())
                {
                    button.UnregisterCallback<ClickEvent>(OnMissionClicked);
                }
            }

            _root?.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        private void OnBackClicked(ClickEvent evt) => BackRequested?.Invoke();

        private void OnMissionClicked(ClickEvent evt)
        {
            if (evt.currentTarget is Button button)
            {
                SelectMission(button, true);
            }
        }

        private void OnPrimaryActionClicked(ClickEvent evt)
        {
            MissionPreviewSelection selection = GetCurrentSelection();
            if (selection.IsLocked)
            {
                LockedMissionRequested?.Invoke(selection);
                return;
            }

            MissionPreviewData data = GetSelectedData();
            switch (data.PrimaryActionLabel)
            {
                case "Start Mission":
                    StartMissionRequested?.Invoke(selection);
                    break;
                case "Continue Mission":
                    ContinueMissionRequested?.Invoke(selection);
                    break;
                case "Review Mission":
                    ReviewMissionRequested?.Invoke(selection);
                    break;
                default:
                    LockedMissionRequested?.Invoke(selection);
                    break;
            }
        }

        private void SelectMission(Button selected, bool notify)
        {
            if (selected == null || !_missionData.TryGetValue(selected.name, out MissionPreviewData data))
            {
                return;
            }

            _missionList?.Query<Button>(className: "mission-selection__item").ForEach(button =>
                button.EnableInClassList(SelectedClass, button == selected));

            bool changed = SelectedMissionNumber != data.MissionNumber;
            SelectedMissionNumber = data.MissionNumber;
            ApplyMissionData(data);
            if (notify && changed)
            {
                MissionSelected?.Invoke(CreateSelection(data));
            }
        }

        private MissionPreviewSelection GetCurrentSelection() => CreateSelection(GetSelectedData());

        private MissionPreviewData GetSelectedData()
        {
            foreach (MissionPreviewData data in _missionData.Values)
            {
                if (data.MissionNumber == SelectedMissionNumber)
                {
                    return data;
                }
            }

            return _missionData["mission-item-2"];
        }

        private MissionPreviewSelection CreateSelection(MissionPreviewData data) =>
            new(
                data.MissionId,
                CatalogSubject,
                CatalogTerm,
                data.MissionNumber,
                data.Title,
                data.IsLocked,
                data.LockReason);

        private void ApplyMissionData(MissionPreviewData data)
        {
            if (_detailTitle != null) _detailTitle.text = data.Title;
            if (_detailDescription != null) _detailDescription.text = data.Description;
            if (_detailLearningGoal != null) _detailLearningGoal.text = data.LearningGoal;
            if (_detailStatusLabel != null) _detailStatusLabel.text = data.StatusLabel;
            if (_detailStatus != null)
            {
                foreach (string state in StatusStates)
                {
                    _detailStatus.EnableInClassList(StatusModifierPrefix + state, state == data.StatusState);
                }
            }

            if (_detailAreasProgress != null) _detailAreasProgress.text = $"{data.AreasCompleted} / {data.AreasTotal}";
            if (_detailCollectiblesProgress != null) _detailCollectiblesProgress.text = $"{data.CollectiblesCompleted} / {data.CollectiblesTotal}";
            UpdateStatFill(_detailAreasFill, data.AreasCompleted, data.AreasTotal);
            UpdateStatFill(_detailCollectiblesFill, data.CollectiblesCompleted, data.CollectiblesTotal);
            if (_detailPrerequisite != null) _detailPrerequisite.text = data.PrerequisiteText;
            if (_detailClassroom != null) _detailClassroom.text = data.ClassroomText;
            if (_detailDownloaded != null) _detailDownloaded.text = data.DownloadedText;
            if (_primaryActionLabel != null) _primaryActionLabel.text = data.PrimaryActionLabel;
            _primaryActionButton?.EnableInClassList(PrimaryActionLockedClass, data.IsLocked);
        }

        private static void UpdateStatFill(VisualElement fill, int completed, int total)
        {
            if (fill != null)
            {
                fill.style.width = Length.Percent(total > 0 ? Mathf.Clamp01((float)completed / total) * 100f : 0f);
            }
        }

        private void OnGeometryChanged(GeometryChangedEvent evt) => ApplyResponsiveClasses(evt.newRect.width);

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

        private static string GetTermTitle(NutriMindSubject subject, NutriMindTerm term)
        {
            if (subject == NutriMindSubject.LiteraQuest && term == NutriMindTerm.Term1)
            {
                return "Pages of the Nation";
            }

            if (subject == NutriMindSubject.Science && term == NutriMindTerm.Term2)
            {
                return "Life and Living Things";
            }

            return $"{GetSubjectLabel(subject)} Adventures";
        }

        private static string GetSubjectLabel(NutriMindSubject subject)
        {
            return subject switch
            {
                NutriMindSubject.LiteraQuest => "LiteraQuest",
                NutriMindSubject.PeAndHealth => "PE & Health",
                _ => "Science"
            };
        }

        private readonly struct MissionPreviewData
        {
            public MissionPreviewData(
                string missionId,
                int missionNumber,
                string title,
                string description,
                string learningGoal,
                int areasCompleted,
                int areasTotal,
                int collectiblesCompleted,
                int collectiblesTotal,
                string statusLabel,
                string statusState,
                string prerequisiteText,
                string classroomText,
                string downloadedText,
                string primaryActionLabel,
                bool isLocked,
                string lockReason)
            {
                MissionId = missionId;
                MissionNumber = missionNumber;
                Title = title;
                Description = description;
                LearningGoal = learningGoal;
                AreasCompleted = areasCompleted;
                AreasTotal = areasTotal;
                CollectiblesCompleted = collectiblesCompleted;
                CollectiblesTotal = collectiblesTotal;
                StatusLabel = statusLabel;
                StatusState = statusState;
                PrerequisiteText = prerequisiteText;
                ClassroomText = classroomText;
                DownloadedText = downloadedText;
                PrimaryActionLabel = primaryActionLabel;
                IsLocked = isLocked;
                LockReason = lockReason;
            }

            public string MissionId { get; }
            public int MissionNumber { get; }
            public string Title { get; }
            public string Description { get; }
            public string LearningGoal { get; }
            public int AreasCompleted { get; }
            public int AreasTotal { get; }
            public int CollectiblesCompleted { get; }
            public int CollectiblesTotal { get; }
            public string StatusLabel { get; }
            public string StatusState { get; }
            public string PrerequisiteText { get; }
            public string ClassroomText { get; }
            public string DownloadedText { get; }
            public string PrimaryActionLabel { get; }
            public bool IsLocked { get; }
            public string LockReason { get; }
        }
    }
}
