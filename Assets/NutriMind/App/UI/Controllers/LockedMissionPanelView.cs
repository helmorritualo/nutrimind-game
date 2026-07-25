using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace NutriMind.App.UI
{
    public enum MissionLockReason
    {
        TeacherRestricted,
        PrerequisiteRequired,
        NotPublished,
        NotDownloaded,
        OfflineUnavailable
    }

    public readonly struct LockedMissionPreviewContext
    {
        public LockedMissionPreviewContext(NutriMindSubject subject, NutriMindTerm term, int missionNumber, string missionTitle, MissionLockReason reason, string requirementText)
        {
            Subject = subject;
            Term = term;
            MissionNumber = missionNumber;
            MissionTitle = missionTitle;
            Reason = reason;
            RequirementText = requirementText;
        }

        public NutriMindSubject Subject { get; }
        public NutriMindTerm Term { get; }
        public int MissionNumber { get; }
        public string MissionTitle { get; }
        public MissionLockReason Reason { get; }
        public string RequirementText { get; }
    }

    /// <summary>Presentation-only locked mission route view.</summary>
    public sealed class LockedMissionPanelView : IAppScreenView
    {
        private const string RootName = "locked-mission-root";
        private const string CompactClass = "locked-mission--compact";
        private const string NarrowClass = "locked-mission--narrow";
        private const string MobileClass = "mobile";
        private const string ActionGhostClass = "ds-btn--ghost";
        private const string ActionPrimaryClass = "ds-btn--primary";
        private const float CompactBreakpoint = 1100f;
        private const float NarrowBreakpoint = 820f;

        private readonly Dictionary<MissionLockReason, LockedMissionPreviewData> _previewData = new()
        {
            [MissionLockReason.TeacherRestricted] = new("Ecosystems and Balance", "Discover how living things depend on each other within an ecosystem.", "Locked by Teacher", "This mission is currently locked by your Teacher. Your existing progress is safe.", "Prerequisite complete — no additional missions required.", "Waiting for classroom release", "Downloaded • Available offline", "Back to Missions", "ds-icon--lock", "ds-icon--arrow-left", true),
            [MissionLockReason.PrerequisiteRequired] = new("Life Cycles", "Follow the stages of life for plants and animals in your community.", "Prerequisite Required", "Complete Mission 3 before starting this mission.", "Requires: Habitats Around Us (Mission 3)", "Published to your classroom", "Downloaded • Available offline", "Continue Required Mission", "ds-icon--lock", "ds-icon--chevron-right", false),
            [MissionLockReason.NotPublished] = new("Producers and Consumers", "Learn how living things get and use energy in food chains.", "Not Yet Published", "This mission has not been published to your classroom yet. Check back once your teacher makes it available.", "Prerequisite complete — no additional missions required.", "Not yet published to your classroom", "Not available until published", "Back to Missions", "ds-icon--warning", "ds-icon--arrow-left", true),
            [MissionLockReason.NotDownloaded] = new("Adaptations for Survival", "Discover how living things adapt to survive in their environment.", "Download Required", "Download this mission while online to play it offline.", "Prerequisite complete — no additional missions required.", "Published to your classroom", "Not downloaded on this device", "Download Mission", "ds-icon--arrow-down", "ds-icon--arrow-down", false),
            [MissionLockReason.OfflineUnavailable] = new("Human Body Systems", "Explore how body systems work together to keep us healthy.", "Offline Unavailable", "This mission is not available offline on this device.", "Prerequisite complete — no additional missions required.", "Published to your classroom", "Downloaded, but offline play is unavailable for this mission", "Retry Connection", "ds-icon--wifi", "ds-icon--refresh", true)
        };

        private VisualElement _root;
        private Button _backButton;
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
        private bool _disposed;
        private float _lastWidth = -1f;

        public LockedMissionPanelView(VisualElement root)
        {
            ResolveRoot(root);
            if (_root == null)
            {
                Debug.LogWarning("[LockedMissionPanelView] Could not resolve locked-mission-root inside the supplied element.");
                return;
            }

            CacheElements();
            RegisterCallbacks();
            SetContext(new LockedMissionPreviewContext(NutriMindSubject.Science, NutriMindTerm.Term2, 5, "Ecosystems and Balance", MissionLockReason.TeacherRestricted, "Prerequisite complete — no additional missions required."));
            ApplyResponsiveClasses(_root.resolvedStyle.width);
        }

        public VisualElement Root => _root;
        public bool IsBound => _root != null && !_disposed;
        public LockedMissionPreviewContext Context { get; private set; }
        public event Action BackRequested;
        public event Action PrimaryActionRequested;
        public event Action SecondaryActionRequested;

        public void SetContext(LockedMissionPreviewContext context)
        {
            if (!IsBound)
            {
                return;
            }

            Context = context;
            LockedMissionPreviewData data = _previewData[context.Reason];
            string title = string.IsNullOrWhiteSpace(context.MissionTitle) ? data.Title : context.MissionTitle;
            string requirement = string.IsNullOrWhiteSpace(context.RequirementText) ? data.PrerequisiteText : context.RequirementText;
            if (_summaryTitle != null) _summaryTitle.text = title;
            if (_summaryDescription != null) _summaryDescription.text = data.Description;
            if (_statusPillLabel != null) _statusPillLabel.text = data.StatusPillLabel;
            if (_lockMessage != null) _lockMessage.text = data.LockMessage;
            if (_detailPrerequisite != null) _detailPrerequisite.text = requirement;
            if (_detailClassroom != null) _detailClassroom.text = data.ClassroomText;
            if (_detailDownload != null) _detailDownload.text = data.DownloadText;
            if (_actionLabel != null) _actionLabel.text = data.ActionLabel;
            SetIconClass(_summaryIcon, data.IconClass);
            SetIconClass(_bannerIcon, data.IconClass);
            SetIconClass(_actionIcon, data.ActionIconClass);
            _actionButton?.EnableInClassList(ActionGhostClass, data.UseGhostAction);
            _actionButton?.EnableInClassList(ActionPrimaryClass, !data.UseGhostAction);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            UnregisterCallbacks();
            BackRequested = null;
            PrimaryActionRequested = null;
            SecondaryActionRequested = null;
            _root = null;
            _backButton = null;
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

        private void ResolveRoot(VisualElement root) => _root = root?.name == RootName ? root : root?.Q<VisualElement>(RootName);
        private void CacheElements()
        {
            _backButton = _root.Q<Button>("back-button");
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
        }

        private void RegisterCallbacks()
        {
            _backButton?.RegisterCallback<ClickEvent>(OnBackClicked);
            _actionButton?.RegisterCallback<ClickEvent>(OnPrimaryActionClicked);
            _root?.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        private void UnregisterCallbacks()
        {
            _backButton?.UnregisterCallback<ClickEvent>(OnBackClicked);
            _actionButton?.UnregisterCallback<ClickEvent>(OnPrimaryActionClicked);
            _root?.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        private void OnBackClicked(ClickEvent evt) => BackRequested?.Invoke();
        private void OnPrimaryActionClicked(ClickEvent evt) => PrimaryActionRequested?.Invoke();
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

        private static void SetIconClass(VisualElement icon, string iconClass)
        {
            if (icon == null) return;
            var classesToRemove = new List<string>();
            foreach (string existingClass in icon.GetClasses())
            {
                if (existingClass.StartsWith("ds-icon--", StringComparison.Ordinal)) classesToRemove.Add(existingClass);
            }
            foreach (string existingClass in classesToRemove) icon.RemoveFromClassList(existingClass);
            icon.AddToClassList(iconClass);
        }

        private readonly struct LockedMissionPreviewData
        {
            public LockedMissionPreviewData(string title, string description, string statusPillLabel, string lockMessage, string prerequisiteText, string classroomText, string downloadText, string actionLabel, string iconClass, string actionIconClass, bool useGhostAction)
            {
                Title = title; Description = description; StatusPillLabel = statusPillLabel; LockMessage = lockMessage; PrerequisiteText = prerequisiteText; ClassroomText = classroomText; DownloadText = downloadText; ActionLabel = actionLabel; IconClass = iconClass; ActionIconClass = actionIconClass; UseGhostAction = useGhostAction;
            }
            public string Title { get; } public string Description { get; } public string StatusPillLabel { get; } public string LockMessage { get; } public string PrerequisiteText { get; } public string ClassroomText { get; } public string DownloadText { get; } public string ActionLabel { get; } public string IconClass { get; } public string ActionIconClass { get; } public bool UseGhostAction { get; }
        }
    }
}
