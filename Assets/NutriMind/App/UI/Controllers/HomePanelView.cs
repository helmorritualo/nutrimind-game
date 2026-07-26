using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace NutriMind.App.UI
{
    /// <summary>
    /// Presentation-only Home route view for content-only <c>HomePanel.uxml</c>.
    /// Binds greeting, announcement, and action cards inside an already-instantiated
    /// root. Raises Continue / Quiz Portal / Announcements requests for the host to handle.
    /// Does not perform routing, mission loading, Quiz Portal networking, API calls,
    /// SQLite, synchronization, or AppShell chrome ownership.
    /// </summary>
    public sealed class HomePanelView : IAppScreenView
    {
        private const string RootName = "home-root";
        private const string CompactClass = "home-panel--compact";
        private const string NarrowClass = "home-panel--narrow";
        private const string MobileClass = "mobile";
        private const float CompactBreakpoint = 1100f;
        private const float NarrowBreakpoint = 820f;

        private VisualElement _root;
        private Button _continueButton;
        private Button _quizPortalButton;
        private Button _announcementsButton;
        private Label _areasCompletedLabel;
        private Label _storyFragmentsLabel;
        private bool _disposed;
        private float _lastWidth = -1f;

        /// <summary>
        /// Raised when the Play Adventure Continue control is clicked.
        /// Presentation request only — the host decides how to respond.
        /// </summary>
        public event Action ContinueMissionRequested;

        /// <summary>
        /// Raised when the Quiz Portal Go to Quizzes control is clicked.
        /// Presentation request only — the host decides how to respond.
        /// </summary>
        public event Action QuizPortalRequested;

        /// <summary>
        /// Raised when the Home announcements preview action is clicked.
        /// Presentation request only — the host decides how to respond.
        /// </summary>
        public event Action AnnouncementsRequested;

        /// <summary>
        /// Creates a view bound to an already-instantiated Home root,
        /// a TemplateContainer containing the root, or a local host that contains it.
        /// Does not search the entire application panel globally.
        /// </summary>
        public HomePanelView(VisualElement root)
        {
            ResolveRoot(root);
            if (_root == null)
            {
                Debug.LogWarning(
                    "[HomePanelView] Could not resolve home-root inside the supplied element.");
                return;
            }

            CacheElements();
            ApplyStaticPreviewContent();
            RegisterCallbacks();
            ApplyResponsiveClasses(_root.resolvedStyle.width);
        }

        public VisualElement Root => _root;

        public bool IsBound => _root != null && !_disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            UnregisterCallbacks();
            ContinueMissionRequested = null;
            QuizPortalRequested = null;
            AnnouncementsRequested = null;
            _root = null;
            _continueButton = null;
            _quizPortalButton = null;
            _announcementsButton = null;
            _areasCompletedLabel = null;
            _storyFragmentsLabel = null;
            _lastWidth = -1f;
        }

        private void ResolveRoot(VisualElement root)
        {
            if (root == null)
            {
                return;
            }

            if (root.name == RootName)
            {
                _root = root;
                return;
            }

            _root = root.Q<VisualElement>(RootName);
        }

        private void CacheElements()
        {
            _continueButton = _root.Q<Button>("play-continue-button");
            _quizPortalButton = _root.Q<Button>("quiz-go-button");
            _announcementsButton = _root.Q<Button>("home-announcement-button");
            _areasCompletedLabel = _root.Q<Label>("areas-completed-label");
            _storyFragmentsLabel = _root.Q<Label>("story-fragments-label");
        }

        private void ApplyStaticPreviewContent()
        {
            if (_areasCompletedLabel != null)
            {
                _areasCompletedLabel.text = "2 / 3";
            }

            if (_storyFragmentsLabel != null)
            {
                _storyFragmentsLabel.text = "2 / 3";
            }
        }

        private void RegisterCallbacks()
        {
            _continueButton?.RegisterCallback<ClickEvent>(OnContinueClicked);
            _quizPortalButton?.RegisterCallback<ClickEvent>(OnQuizPortalClicked);
            _announcementsButton?.RegisterCallback<ClickEvent>(OnAnnouncementsClicked);
            _root?.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        private void UnregisterCallbacks()
        {
            _continueButton?.UnregisterCallback<ClickEvent>(OnContinueClicked);
            _quizPortalButton?.UnregisterCallback<ClickEvent>(OnQuizPortalClicked);
            _announcementsButton?.UnregisterCallback<ClickEvent>(OnAnnouncementsClicked);
            _root?.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        private void OnContinueClicked(ClickEvent evt)
        {
            ContinueMissionRequested?.Invoke();
        }

        private void OnQuizPortalClicked(ClickEvent evt)
        {
            QuizPortalRequested?.Invoke();
        }

        private void OnAnnouncementsClicked(ClickEvent evt)
        {
            AnnouncementsRequested?.Invoke();
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

            if (Mathf.Approximately(width, _lastWidth))
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
    }
}
