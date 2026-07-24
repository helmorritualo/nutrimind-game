using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace NutriMind.App.UI
{
    /// <summary>
    /// The reason a previewed mission is locked, used to drive static UI Toolkit preview content.
    /// </summary>
    public enum LockedMissionPreviewMode
    {
        TeacherLocked,
        PrerequisiteLocked,
        NotPublished,
        NotDownloaded,
        OfflineUnavailable
    }

    /// <summary>
    /// Layout-only locked mission panel wiring for UI Toolkit static preview.
    /// Handles responsive classes, static nav active state, and switching between
    /// lock preview modes from local preview data only. Does not perform routing,
    /// unlock validation, download orchestration, or networking.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class LockedMissionPanelController : MonoBehaviour
    {
        private const string CompactClass = "locked-mission--compact";
        private const string NarrowClass = "locked-mission--narrow";
        private const string MobileClass = "mobile";
        private const float CompactBreakpoint = 1100f;
        private const float NarrowBreakpoint = 820f;
        private const string ActionGhostClass = "ds-btn--ghost";
        private const string ActionPrimaryClass = "ds-btn--primary";

        [SerializeField]
        [Tooltip("Static preview mode. Drives the lock icon, title, message, and action label shown by this panel.")]
        private LockedMissionPreviewMode _previewMode = LockedMissionPreviewMode.TeacherLocked;

        private UIDocument _uiDocument;
        private VisualElement _root;
        private VisualElement _nav;
        private VisualElement _summaryIcon;
        private Label _summaryTitle;
        private Label _summaryDescription;
        private Label _statusPillLabel;
        private VisualElement _bannerIcon;
        private Label _lockMessage;
        private Label _detailPrerequisite;
        private Label _detailClassroom;
        private Label _detailDownload;
        private Button _actionButton;
        private Label _actionLabel;
        private VisualElement _actionIcon;
        private float _lastWidth = -1f;

        private readonly Dictionary<LockedMissionPreviewMode, LockedMissionPreviewData> _previewData = new()
        {
            [LockedMissionPreviewMode.TeacherLocked] = new LockedMissionPreviewData(
                title: "Ecosystems and Balance",
                description: "Discover how living things depend on each other within an ecosystem.",
                statusPillLabel: "Locked by Teacher",
                lockMessage: "This mission is currently locked by your Teacher. Your existing progress is safe.",
                prerequisiteText: "Prerequisite complete — no additional missions required.",
                classroomText: "Waiting for classroom release",
                downloadText: "Downloaded • Available offline",
                actionLabel: "Back to Missions",
                iconClass: "ds-icon--lock",
                actionIconClass: "ds-icon--arrow-left",
                useGhostAction: true),
            [LockedMissionPreviewMode.PrerequisiteLocked] = new LockedMissionPreviewData(
                title: "Life Cycles",
                description: "Follow the stages of life for plants and animals in your community.",
                statusPillLabel: "Prerequisite Required",
                lockMessage: "Complete Mission 3 before starting this mission.",
                prerequisiteText: "Requires: Habitats Around Us (Mission 3)",
                classroomText: "Published to your classroom",
                downloadText: "Downloaded • Available offline",
                actionLabel: "Continue Required Mission",
                iconClass: "ds-icon--lock",
                actionIconClass: "ds-icon--chevron-right",
                useGhostAction: false),
            [LockedMissionPreviewMode.NotPublished] = new LockedMissionPreviewData(
                title: "Producers and Consumers",
                description: "Learn how living things get and use energy in food chains.",
                statusPillLabel: "Not Yet Published",
                lockMessage: "This mission has not been published to your classroom yet. Check back once your teacher makes it available.",
                prerequisiteText: "Prerequisite complete — no additional missions required.",
                classroomText: "Not yet published to your classroom",
                downloadText: "Not available until published",
                actionLabel: "Back to Missions",
                iconClass: "ds-icon--warning",
                actionIconClass: "ds-icon--arrow-left",
                useGhostAction: true),
            [LockedMissionPreviewMode.NotDownloaded] = new LockedMissionPreviewData(
                title: "Adaptations for Survival",
                description: "Discover how living things adapt to survive in their environment.",
                statusPillLabel: "Download Required",
                lockMessage: "Download this mission while online to play it offline.",
                prerequisiteText: "Prerequisite complete — no additional missions required.",
                classroomText: "Published to your classroom",
                downloadText: "Not downloaded on this device",
                actionLabel: "Download Mission",
                iconClass: "ds-icon--arrow-down",
                actionIconClass: "ds-icon--arrow-down",
                useGhostAction: false),
            [LockedMissionPreviewMode.OfflineUnavailable] = new LockedMissionPreviewData(
                title: "Human Body Systems",
                description: "Explore how body systems work together to keep us healthy.",
                statusPillLabel: "Offline Unavailable",
                lockMessage: "This mission is not available offline on this device.",
                prerequisiteText: "Prerequisite complete — no additional missions required.",
                classroomText: "Published to your classroom",
                downloadText: "Downloaded, but offline play is unavailable for this mission",
                actionLabel: "Retry Connection",
                iconClass: "ds-icon--wifi",
                actionIconClass: "ds-icon--refresh",
                useGhostAction: true)
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

            _root = _uiDocument.rootVisualElement?.Q<VisualElement>("locked-mission-root");
            if (_root == null)
            {
                Invoke(nameof(BindWhenReady), 0.05f);
                return;
            }

            var panelRoot = _uiDocument.rootVisualElement;
            panelRoot.style.flexGrow = 1;
            panelRoot.style.width = Length.Percent(100);
            panelRoot.style.height = Length.Percent(100);

            _nav = _root.Q<VisualElement>("locked-nav");
            _summaryIcon = _root.Q<VisualElement>("summary-icon");
            _summaryTitle = _root.Q<Label>("summary-title");
            _summaryDescription = _root.Q<Label>("summary-description");
            _statusPillLabel = _root.Q<Label>("status-pill-label");
            _bannerIcon = _root.Q<VisualElement>("banner-icon");
            _lockMessage = _root.Q<Label>("lock-message");
            _detailPrerequisite = _root.Q<Label>("detail-prerequisite");
            _detailClassroom = _root.Q<Label>("detail-classroom");
            _detailDownload = _root.Q<Label>("detail-download");
            _actionButton = _root.Q<Button>("action-button");
            _actionLabel = _root.Q<Label>("action-label");
            _actionIcon = _root.Q<VisualElement>("action-icon");

            if (_nav != null)
            {
                foreach (var button in _nav.Query<Button>(className: "locked-mission__nav-item").ToList())
                {
                    button.RegisterCallback<ClickEvent>(OnNavClickEvent);
                }
            }

            _root.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            ApplyResponsiveClasses(_root.resolvedStyle.width);

            ApplyPreviewMode(_previewMode);
        }

        private void Unbind()
        {
            if (_nav != null)
            {
                foreach (var button in _nav.Query<Button>(className: "locked-mission__nav-item").ToList())
                {
                    button.UnregisterCallback<ClickEvent>(OnNavClickEvent);
                }
            }

            if (_root != null)
            {
                _root.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            }

            _root = null;
            _nav = null;
            _summaryIcon = null;
            _summaryTitle = null;
            _summaryDescription = null;
            _statusPillLabel = null;
            _bannerIcon = null;
            _lockMessage = null;
            _detailPrerequisite = null;
            _detailClassroom = null;
            _detailDownload = null;
            _actionButton = null;
            _actionLabel = null;
            _actionIcon = null;
            _lastWidth = -1f;
        }

        private void OnNavClickEvent(ClickEvent evt)
        {
            if (evt.currentTarget is Button button)
            {
                OnNavClicked(button);
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

            _nav.Query<Button>(className: "locked-mission__nav-item").ForEach(button =>
            {
                button.EnableInClassList("is-active", button == selected);
            });
        }

        /// <summary>
        /// Switches the static preview to the given lock reason and refreshes all bound fields.
        /// </summary>
        public void SetPreviewMode(LockedMissionPreviewMode mode)
        {
            _previewMode = mode;
            ApplyPreviewMode(mode);
        }

        private void ApplyPreviewMode(LockedMissionPreviewMode mode)
        {
            if (_root == null || !_previewData.TryGetValue(mode, out LockedMissionPreviewData data))
            {
                return;
            }

            if (_summaryTitle != null)
            {
                _summaryTitle.text = data.Title;
            }

            if (_summaryDescription != null)
            {
                _summaryDescription.text = data.Description;
            }

            if (_statusPillLabel != null)
            {
                _statusPillLabel.text = data.StatusPillLabel;
            }

            if (_lockMessage != null)
            {
                _lockMessage.text = data.LockMessage;
            }

            if (_detailPrerequisite != null)
            {
                _detailPrerequisite.text = data.PrerequisiteText;
            }

            if (_detailClassroom != null)
            {
                _detailClassroom.text = data.ClassroomText;
            }

            if (_detailDownload != null)
            {
                _detailDownload.text = data.DownloadText;
            }

            if (_actionLabel != null)
            {
                _actionLabel.text = data.ActionLabel;
            }

            SetIconClass(_summaryIcon, data.IconClass);
            SetIconClass(_bannerIcon, data.IconClass);
            SetIconClass(_actionIcon, data.ActionIconClass);

            if (_actionButton != null)
            {
                _actionButton.EnableInClassList(ActionGhostClass, data.UseGhostAction);
                _actionButton.EnableInClassList(ActionPrimaryClass, !data.UseGhostAction);
            }
        }

        private static void SetIconClass(VisualElement icon, string iconClass)
        {
            if (icon == null)
            {
                return;
            }

            var existingIconClasses = new List<string>();
            foreach (string existingClass in icon.GetClasses())
            {
                if (existingClass.StartsWith("ds-icon--"))
                {
                    existingIconClasses.Add(existingClass);
                }
            }

            foreach (string existingClass in existingIconClasses)
            {
                icon.RemoveFromClassList(existingClass);
            }

            icon.AddToClassList(iconClass);
        }

        private readonly struct LockedMissionPreviewData
        {
            public LockedMissionPreviewData(
                string title,
                string description,
                string statusPillLabel,
                string lockMessage,
                string prerequisiteText,
                string classroomText,
                string downloadText,
                string actionLabel,
                string iconClass,
                string actionIconClass,
                bool useGhostAction)
            {
                Title = title;
                Description = description;
                StatusPillLabel = statusPillLabel;
                LockMessage = lockMessage;
                PrerequisiteText = prerequisiteText;
                ClassroomText = classroomText;
                DownloadText = downloadText;
                ActionLabel = actionLabel;
                IconClass = iconClass;
                ActionIconClass = actionIconClass;
                UseGhostAction = useGhostAction;
            }

            public string Title { get; }
            public string Description { get; }
            public string StatusPillLabel { get; }
            public string LockMessage { get; }
            public string PrerequisiteText { get; }
            public string ClassroomText { get; }
            public string DownloadText { get; }
            public string ActionLabel { get; }
            public string IconClass { get; }
            public string ActionIconClass { get; }
            public bool UseGhostAction { get; }
        }
    }
}
