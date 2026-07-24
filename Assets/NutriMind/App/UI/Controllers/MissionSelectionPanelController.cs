using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace NutriMind.App.UI
{
    /// <summary>
    /// Layout-only mission selection panel wiring for UI Toolkit preview.
    /// Handles responsive classes, list selection, and static nav active state.
    /// Does not perform routing, progress loading, or networking.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class MissionSelectionPanelController : MonoBehaviour
    {
        private const string CompactClass = "mission-selection--compact";
        private const string NarrowClass = "mission-selection--narrow";
        private const string MobileClass = "mobile";
        private const string SelectedClass = "is-selected";
        private const float CompactBreakpoint = 1100f;
        private const float NarrowBreakpoint = 820f;

        private UIDocument _uiDocument;
        private VisualElement _root;
        private VisualElement _nav;
        private VisualElement _missionList;
        private Label _detailTitle;
        private Label _detailDescription;
        private Label _detailLearningGoal;
        private Label _detailProgress;
        private Label _detailRewardStars;
        private Label _detailRewardXp;
        private VisualElement _progressStars;
        private float _lastWidth = -1f;

        private readonly Dictionary<string, MissionPreviewData> _missionData = new()
        {
            ["mission-item-1"] = new MissionPreviewData(
                "What Is a Living Thing?",
                "Learn how to tell living things apart from non-living things.",
                "I can describe characteristics of living things.",
                5,
                5,
                "50",
                "100"),
            ["mission-item-2"] = new MissionPreviewData(
                "Needs of Living Things",
                "Discover what living things need to survive and how they get what they need.",
                "I can identify the basic needs of living things and explain why they are important.",
                3,
                5,
                "50",
                "100"),
            ["mission-item-3"] = new MissionPreviewData(
                "Habitats Around Us",
                "Explore different habitats and how living things adapt to them.",
                "I can name common habitats and describe how organisms survive in each one.",
                0,
                5,
                "50",
                "100"),
            ["mission-item-4"] = new MissionPreviewData(
                "Life Cycles",
                "Follow the stages of life for plants and animals in your community.",
                "I can explain basic life-cycle stages for familiar living things.",
                0,
                5,
                "50",
                "100")
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
            _detailProgress = _root.Q<Label>("detail-progress");
            _detailRewardStars = _root.Q<Label>("detail-reward-stars");
            _detailRewardXp = _root.Q<Label>("detail-reward-xp");
            _progressStars = _root.Q<VisualElement>("progress-stars");

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
            _detailProgress = null;
            _detailRewardStars = null;
            _detailRewardXp = null;
            _progressStars = null;
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

            if (_detailProgress != null)
            {
                _detailProgress.text = $"{data.ProgressFilled} / {data.ProgressTotal}";
            }

            if (_detailRewardStars != null)
            {
                _detailRewardStars.text = data.RewardStars;
            }

            if (_detailRewardXp != null)
            {
                _detailRewardXp.text = data.RewardXp;
            }

            UpdateProgressStars(data.ProgressFilled, data.ProgressTotal);
        }

        private void UpdateProgressStars(int filled, int total)
        {
            if (_progressStars == null)
            {
                return;
            }

            var stars = _progressStars.Query(className: "mission-selection__star").ToList();
            for (int i = 0; i < stars.Count; i++)
            {
                bool isFilled = i < filled;
                stars[i].EnableInClassList("mission-selection__star--filled", isFilled);
                stars[i].EnableInClassList("mission-selection__star--empty", !isFilled);
                stars[i].EnableInClassList("ds-icon--gold", isFilled);
            }
        }

        private readonly struct MissionPreviewData
        {
            public MissionPreviewData(
                string title,
                string description,
                string learningGoal,
                int progressFilled,
                int progressTotal,
                string rewardStars,
                string rewardXp)
            {
                Title = title;
                Description = description;
                LearningGoal = learningGoal;
                ProgressFilled = progressFilled;
                ProgressTotal = progressTotal;
                RewardStars = rewardStars;
                RewardXp = rewardXp;
            }

            public string Title { get; }
            public string Description { get; }
            public string LearningGoal { get; }
            public int ProgressFilled { get; }
            public int ProgressTotal { get; }
            public string RewardStars { get; }
            public string RewardXp { get; }
        }
    }
}
