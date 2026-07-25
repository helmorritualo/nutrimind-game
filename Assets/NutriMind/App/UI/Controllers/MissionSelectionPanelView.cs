using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace NutriMind.App.UI
{
    public readonly struct MissionPreviewSelection
    {
        public MissionPreviewSelection(int missionNumber, string missionTitle, bool isLocked, string lockReason)
        {
            MissionNumber = missionNumber;
            MissionTitle = missionTitle;
            IsLocked = isLocked;
            LockReason = lockReason;
        }

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

        private static readonly string[] StatusStates = { "completed", "progress", "available", "locked" };

        private readonly Dictionary<string, MissionPreviewData> _missionData = new()
        {
            ["mission-item-1"] = new MissionPreviewData(1, "What Is a Living Thing?", "Learn how to tell living things apart from non-living things.", "I can describe characteristics of living things.", 3, 3, 3, 3, "Completed", "completed", "No prerequisite", "Published to your classroom", "Downloaded • Available offline", "Review Mission", false, string.Empty),
            ["mission-item-2"] = new MissionPreviewData(2, "Needs of Living Things", "Discover what living things need to survive and how they get what they need.", "I can identify the basic needs of living things and explain why they are important.", 2, 3, 2, 3, "In Progress", "progress", "No prerequisite", "Published to your classroom", "Downloaded • Available offline", "Continue Mission", false, string.Empty),
            ["mission-item-3"] = new MissionPreviewData(3, "Habitats Around Us", "Explore different habitats and how living things adapt to them.", "I can name common habitats and describe how organisms survive in each one.", 0, 3, 0, 3, "Available", "available", "No prerequisite", "Published to your classroom", "Downloaded • Available offline", "Start Mission", false, string.Empty),
            ["mission-item-4"] = new MissionPreviewData(4, "Life Cycles", "Follow the stages of life for plants and animals in your community.", "I can explain basic life-cycle stages for familiar living things.", 0, 3, 0, 3, "Prerequisite Locked", "locked", "Requires: Habitats Around Us (Mission 3)", "Published to your classroom", "Downloaded • Available offline", "Back to Missions", true, "Prerequisite Locked"),
            ["mission-item-5"] = new MissionPreviewData(5, "Ecosystems and Balance", "Discover how living things depend on each other within an ecosystem.", "I can describe how organisms interact within an ecosystem to survive.", 0, 3, 0, 3, "Teacher Locked", "locked", "Prerequisite complete — no additional missions required.", "Waiting for classroom release", "Not downloaded on this device", "Back to Missions", true, "Teacher Locked")
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
        public NutriMindSubject Subject { get; private set; } = NutriMindSubject.Science;
        public NutriMindTerm Term { get; private set; } = NutriMindTerm.Term2;
        public int SelectedMissionNumber { get; private set; } = 2;

        public event Action BackRequested;
        public event Action<MissionPreviewSelection> MissionSelected;
        public event Action<MissionPreviewSelection> StartMissionRequested;
        public event Action<MissionPreviewSelection> ContinueMissionRequested;
        public event Action<MissionPreviewSelection> ReviewMissionRequested;
        public event Action<MissionPreviewSelection> LockedMissionRequested;

        /// <summary>
        /// Updates only the contextual heading for this static preview route.
        /// </summary>
        public void SetContext(NutriMindSubject subject, NutriMindTerm term)
        {
            if (!IsBound)
            {
                return;
            }

            Subject = subject;
            Term = term;
            if (_termHeading != null)
            {
                _termHeading.text = $"Term {(int)term}: {GetTermTitle(subject, term)}";
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

        private static MissionPreviewSelection CreateSelection(MissionPreviewData data) =>
            new(data.MissionNumber, data.Title, data.IsLocked, data.LockReason);

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
            public MissionPreviewData(int missionNumber, string title, string description, string learningGoal, int areasCompleted, int areasTotal, int collectiblesCompleted, int collectiblesTotal, string statusLabel, string statusState, string prerequisiteText, string classroomText, string downloadedText, string primaryActionLabel, bool isLocked, string lockReason)
            {
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
