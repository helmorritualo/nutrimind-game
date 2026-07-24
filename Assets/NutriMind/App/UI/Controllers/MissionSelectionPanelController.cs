using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace NutriMind.App.UI
{
    /// <summary>
    /// Layout-only mission selection panel wiring for UI Toolkit static preview.
    /// Handles responsive classes, list selection, and detail-region field updates
    /// from local preview data only. Does not perform routing, JSON/SQLite loading,
    /// progress tracking, or networking.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class MissionSelectionPanelController : MonoBehaviour
    {
        private const string CompactClass = "mission-selection--compact";
        private const string NarrowClass = "mission-selection--narrow";
        private const string MobileClass = "mobile";
        private const string SelectedClass = "is-selected";
        private const string StatusModifierPrefix = "mission-selection__detail-status--";
        private const string PrimaryActionLockedClass = "mission-selection__primary-action--locked";
        private const float CompactBreakpoint = 1100f;
        private const float NarrowBreakpoint = 820f;

        private static readonly string[] StatusStates = { "completed", "progress", "available", "locked" };

        private UIDocument _uiDocument;
        private VisualElement _root;
        private VisualElement _nav;
        private VisualElement _missionList;
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
        private float _lastWidth = -1f;

        private readonly Dictionary<string, MissionPreviewData> _missionData = new()
        {
            ["mission-item-1"] = new MissionPreviewData(
                title: "What Is a Living Thing?",
                description: "Learn how to tell living things apart from non-living things.",
                learningGoal: "I can describe characteristics of living things.",
                areasCompleted: 3,
                areasTotal: 3,
                collectiblesCompleted: 3,
                collectiblesTotal: 3,
                statusLabel: "Completed",
                statusState: "completed",
                prerequisiteText: "No prerequisite",
                classroomText: "Published to your classroom",
                downloadedText: "Downloaded • Available offline",
                primaryActionLabel: "Review Mission",
                isLocked: false),
            ["mission-item-2"] = new MissionPreviewData(
                title: "Needs of Living Things",
                description: "Discover what living things need to survive and how they get what they need.",
                learningGoal: "I can identify the basic needs of living things and explain why they are important.",
                areasCompleted: 2,
                areasTotal: 3,
                collectiblesCompleted: 2,
                collectiblesTotal: 3,
                statusLabel: "In Progress",
                statusState: "progress",
                prerequisiteText: "No prerequisite",
                classroomText: "Published to your classroom",
                downloadedText: "Downloaded • Available offline",
                primaryActionLabel: "Continue Mission",
                isLocked: false),
            ["mission-item-3"] = new MissionPreviewData(
                title: "Habitats Around Us",
                description: "Explore different habitats and how living things adapt to them.",
                learningGoal: "I can name common habitats and describe how organisms survive in each one.",
                areasCompleted: 0,
                areasTotal: 3,
                collectiblesCompleted: 0,
                collectiblesTotal: 3,
                statusLabel: "Available",
                statusState: "available",
                prerequisiteText: "No prerequisite",
                classroomText: "Published to your classroom",
                downloadedText: "Downloaded • Available offline",
                primaryActionLabel: "Start Mission",
                isLocked: false),
            ["mission-item-4"] = new MissionPreviewData(
                title: "Life Cycles",
                description: "Follow the stages of life for plants and animals in your community.",
                learningGoal: "I can explain basic life-cycle stages for familiar living things.",
                areasCompleted: 0,
                areasTotal: 3,
                collectiblesCompleted: 0,
                collectiblesTotal: 3,
                statusLabel: "Prerequisite Locked",
                statusState: "locked",
                prerequisiteText: "Requires: Habitats Around Us (Mission 3)",
                classroomText: "Published to your classroom",
                downloadedText: "Downloaded • Available offline",
                primaryActionLabel: "Back to Missions",
                isLocked: true),
            ["mission-item-5"] = new MissionPreviewData(
                title: "Ecosystems and Balance",
                description: "Discover how living things depend on each other within an ecosystem.",
                learningGoal: "I can describe how organisms interact within an ecosystem to survive.",
                areasCompleted: 0,
                areasTotal: 3,
                collectiblesCompleted: 0,
                collectiblesTotal: 3,
                statusLabel: "Teacher Locked",
                statusState: "locked",
                prerequisiteText: "Prerequisite complete — no additional missions required.",
                classroomText: "Waiting for classroom release",
                downloadedText: "Not downloaded on this device",
                primaryActionLabel: "Back to Missions",
                isLocked: true)
        };

        private void OnEnable()
        {
            _uiDocument = GetComponent<UIDocument>();
            BindWhenReady();
        }

        private void OnDisable()
        {
            Unbind();
            CancelInvoke(nameof(BindWhenReady));
        }

        private void Update()
        {
            if (_root == null)
            {
                return;
            }

            float width = _root.resolvedStyle.width;
            if (float.IsNaN(width) || width <= 0f || Mathf.Approximately(width, _lastWidth))
            {
                return;
            }

            _lastWidth = width;
            ApplyResponsiveClasses(width);
        }

        private void BindWhenReady()
        {
            if (_uiDocument == null)
            {
                _uiDocument = GetComponent<UIDocument>();
            }

            if (_uiDocument == null)
            {
                return;
            }

            _root = _uiDocument.rootVisualElement?.Q<VisualElement>("mission-selection-root");
            if (_root == null)
            {
                Invoke(nameof(BindWhenReady), 0.05f);
                return;
            }

            var panelRoot = _uiDocument.rootVisualElement;
            panelRoot.style.flexGrow = 1;
            panelRoot.style.width = Length.Percent(100);
            panelRoot.style.height = Length.Percent(100);

            _nav = _root.Q<VisualElement>("mission-nav");
            _missionList = _root.Q<VisualElement>("mission-list");
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

            if (_nav != null)
            {
                foreach (var button in _nav.Query<Button>(className: "mission-selection__nav-item").ToList())
                {
                    button.RegisterCallback<ClickEvent>(OnNavClickEvent);
                }
            }

            if (_missionList != null)
            {
                foreach (var button in _missionList.Query<Button>(className: "mission-selection__item").ToList())
                {
                    button.RegisterCallback<ClickEvent>(OnMissionClickEvent);
                }
            }

            _root.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            ApplyResponsiveClasses(_root.resolvedStyle.width);

            var defaultSelection = _root.Q<Button>("mission-item-2");
            if (defaultSelection != null)
            {
                SelectMission(defaultSelection);
            }
        }

        private void Unbind()
        {
            if (_nav != null)
            {
                foreach (var button in _nav.Query<Button>(className: "mission-selection__nav-item").ToList())
                {
                    button.UnregisterCallback<ClickEvent>(OnNavClickEvent);
                }
            }

            if (_missionList != null)
            {
                foreach (var button in _missionList.Query<Button>(className: "mission-selection__item").ToList())
                {
                    button.UnregisterCallback<ClickEvent>(OnMissionClickEvent);
                }
            }

            if (_root != null)
            {
                _root.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            }

            _root = null;
            _nav = null;
            _missionList = null;
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

        private void OnNavClickEvent(ClickEvent evt)
        {
            if (evt.currentTarget is Button button)
            {
                OnNavClicked(button);
            }
        }

        private void OnMissionClickEvent(ClickEvent evt)
        {
            if (evt.currentTarget is Button button)
            {
                SelectMission(button);
            }
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            ApplyResponsiveClasses(evt.newRect.width);
        }

        private void ApplyResponsiveClasses(float width)
        {
            if (_root == null || float.IsNaN(width) || width <= 0f)
            {
                return;
            }

            bool compact = width < CompactBreakpoint;
            bool narrow = width < NarrowBreakpoint;

            _root.EnableInClassList(CompactClass, compact);
            _root.EnableInClassList(NarrowClass, narrow);
            _root.EnableInClassList(MobileClass, narrow);
        }

        private void OnNavClicked(Button selected)
        {
            if (_nav == null || selected == null)
            {
                return;
            }

            _nav.Query<Button>(className: "mission-selection__nav-item").ForEach(button =>
            {
                button.EnableInClassList("is-active", button == selected);
            });
        }

        private void SelectMission(Button selected)
        {
            if (_missionList == null || selected == null)
            {
                return;
            }

            _missionList.Query<Button>(className: "mission-selection__item").ForEach(button =>
            {
                button.EnableInClassList(SelectedClass, button == selected);
            });

            if (!_missionData.TryGetValue(selected.name, out MissionPreviewData data))
            {
                return;
            }

            ApplyMissionData(data);
        }

        private void ApplyMissionData(MissionPreviewData data)
        {
            if (_detailTitle != null)
            {
                _detailTitle.text = data.Title;
            }

            if (_detailDescription != null)
            {
                _detailDescription.text = data.Description;
            }

            if (_detailLearningGoal != null)
            {
                _detailLearningGoal.text = data.LearningGoal;
            }

            if (_detailStatusLabel != null)
            {
                _detailStatusLabel.text = data.StatusLabel;
            }

            if (_detailStatus != null)
            {
                foreach (string state in StatusStates)
                {
                    _detailStatus.EnableInClassList(StatusModifierPrefix + state, state == data.StatusState);
                }
            }

            if (_detailAreasProgress != null)
            {
                _detailAreasProgress.text = $"{data.AreasCompleted} / {data.AreasTotal}";
            }

            UpdateStatFill(_detailAreasFill, data.AreasCompleted, data.AreasTotal);

            if (_detailCollectiblesProgress != null)
            {
                _detailCollectiblesProgress.text = $"{data.CollectiblesCompleted} / {data.CollectiblesTotal}";
            }

            UpdateStatFill(_detailCollectiblesFill, data.CollectiblesCompleted, data.CollectiblesTotal);

            if (_detailPrerequisite != null)
            {
                _detailPrerequisite.text = data.PrerequisiteText;
            }

            if (_detailClassroom != null)
            {
                _detailClassroom.text = data.ClassroomText;
            }

            if (_detailDownloaded != null)
            {
                _detailDownloaded.text = data.DownloadedText;
            }

            if (_primaryActionLabel != null)
            {
                _primaryActionLabel.text = data.PrimaryActionLabel;
            }

            if (_primaryActionButton != null)
            {
                _primaryActionButton.EnableInClassList(PrimaryActionLockedClass, data.IsLocked);
            }
        }

        private static void UpdateStatFill(VisualElement fill, int completed, int total)
        {
            if (fill == null)
            {
                return;
            }

            float percent = total > 0 ? Mathf.Clamp01((float)completed / total) * 100f : 0f;
            fill.style.width = Length.Percent(percent);
        }

        private readonly struct MissionPreviewData
        {
            public MissionPreviewData(
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
                bool isLocked)
            {
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
            }

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
        }
    }
}
