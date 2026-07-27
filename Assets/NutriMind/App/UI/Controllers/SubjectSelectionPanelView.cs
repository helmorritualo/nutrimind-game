using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace NutriMind.App.UI
{
    /// <summary>
    /// The three NutriMind subjects. Availability is presentation state,
    /// not part of this domain enum.
    /// </summary>
    public enum NutriMindSubject
    {
        LiteraQuest,
        PeAndHealth,
        Science
    }

    /// <summary>
    /// Presentation-only Subject Selection route view for content-only
    /// <c>SubjectSelectionPanel.uxml</c>. Binds the route intro Back control and the
    /// three subject cards inside an already-instantiated root, tracks the selected
    /// available subject, and raises Back / selection / Continue / unavailable
    /// requests for the host to handle.
    /// Does not perform routing, term loading, availability checks, API calls,
    /// SQLite, synchronization, or AppShell chrome ownership.
    /// </summary>
    public sealed class SubjectSelectionPanelView : IAppScreenView
    {
        private const string RootName = "subject-selection-root";
        private const string SelectedClass = "is-selected";
        private const string CompactClass = "subject-selection--compact";
        private const string NarrowClass = "subject-selection--narrow";
        private const string MobileClass = "mobile";
        private const float CompactBreakpoint = 1100f;
        private const float NarrowBreakpoint = 820f;

        private VisualElement _root;

        private Button _backButton;
        private Label _introMessage;
        private Label _countLabel;

        private VisualElement _literaQuestCard;
        private VisualElement _peAndHealthCard;
        private VisualElement _scienceCard;

        private Button _literaQuestContinueButton;
        private Button _peAndHealthContinueButton;
        private Button _scienceContinueButton;

        private Label _literaQuestProgressLabel;
        private Label _literaQuestMissionLabel;
        private Label _peAndHealthProgressLabel;
        private Label _peAndHealthMissionLabel;
        private Label _scienceProgressLabel;
        private Label _scienceMissionLabel;
        private readonly HashSet<NutriMindSubject> _availableSubjects = new();

        private bool _disposed;
        private float _lastWidth = -1f;

        /// <summary>
        /// Raised when the route-local Back control is clicked.
        /// The host decides where to go.
        /// </summary>
        public event Action BackRequested;

        /// <summary>
        /// Raised when an available card becomes the selected subject.
        /// Selection does not open the subject.
        /// </summary>
        public event Action<NutriMindSubject> SubjectSelected;

        /// <summary>
        /// Raised when an available subject's Continue action is clicked.
        /// The host decides whether to show Terms.
        /// </summary>
        public event Action<NutriMindSubject> ContinueSubjectRequested;

        /// <summary>
        /// Raised when the learner clicks the unavailable PE &amp; Health action.
        /// The host decides how to explain classroom availability.
        /// </summary>
        public event Action<NutriMindSubject> UnavailableSubjectRequested;

        /// <summary>
        /// Creates a view bound to an already-instantiated Subject Selection root,
        /// a TemplateContainer containing the root, or a local host that contains it.
        /// Does not search the entire application panel globally.
        /// </summary>
        public SubjectSelectionPanelView(VisualElement root)
        {
            ResolveRoot(root);
            if (_root == null)
            {
                Debug.LogWarning(
                    "[SubjectSelectionPanelView] Could not resolve subject-selection-root " +
                    "inside the supplied element.");
                return;
            }

            CacheElements();
            ApplyStaticPreviewContent();
            _availableSubjects.Add(NutriMindSubject.LiteraQuest);
            _availableSubjects.Add(NutriMindSubject.Science);
            ApplyInitialSelection();
            RegisterCallbacks();
            ApplyResponsiveClasses(_root.resolvedStyle.width);
        }

        public VisualElement Root => _root;

        public bool IsBound => _root != null && !_disposed;
        public DataStatePanelState DataState { get; private set; } = DataStatePanelState.Content;

        /// <summary>
        /// Currently selected available subject. LiteraQuest by default,
        /// matching the static preview fixture.
        /// </summary>
        public NutriMindSubject SelectedSubject { get; private set; } =
            NutriMindSubject.LiteraQuest;

        /// <summary>
        /// Binds the runtime subject list. Subjects not in the list are disabled.
        /// Subjects not in the canonical set (LiteraQuest, PEHealth, Science) are ignored.
        /// </summary>
        public void Bind(IReadOnlyList<NutriMindSubject> availableSubjects)
        {
            if (_disposed || _root == null)
            {
                return;
            }

            _availableSubjects.Clear();
            if (availableSubjects != null)
            {
                for (int i = 0; i < availableSubjects.Count; i++)
                {
                    NutriMindSubject subject = availableSubjects[i];
                    if (Enum.IsDefined(typeof(NutriMindSubject), subject))
                    {
                        _availableSubjects.Add(subject);
                    }
                }
            }

            ApplySubjectAvailability(_literaQuestCard, NutriMindSubject.LiteraQuest);
            ApplySubjectAvailability(_peAndHealthCard, NutriMindSubject.PeAndHealth);
            ApplySubjectAvailability(_scienceCard, NutriMindSubject.Science);

            if (!_availableSubjects.Contains(SelectedSubject))
            {
                if (_availableSubjects.Contains(NutriMindSubject.LiteraQuest))
                {
                    SelectedSubject = NutriMindSubject.LiteraQuest;
                }
                else if (_availableSubjects.Contains(NutriMindSubject.PeAndHealth))
                {
                    SelectedSubject = NutriMindSubject.PeAndHealth;
                }
                else if (_availableSubjects.Contains(NutriMindSubject.Science))
                {
                    SelectedSubject = NutriMindSubject.Science;
                }
            }

            ApplySelectionClasses();
            if (_countLabel != null)
            {
                int count = _availableSubjects.Count;
                _countLabel.text = count == 1 ? "1 Subject" : $"{count} Subjects";
            }
        }

        public void SetDataState(DataStatePanelState state)
        {
            if (!IsBound)
            {
                return;
            }

            DataState = state;
            ApplySubjectAvailability(_literaQuestCard, NutriMindSubject.LiteraQuest);
            ApplySubjectAvailability(_peAndHealthCard, NutriMindSubject.PeAndHealth);
            ApplySubjectAvailability(_scienceCard, NutriMindSubject.Science);
            if (_introMessage == null)
            {
                return;
            }

            _introMessage.text = state switch
            {
                DataStatePanelState.Empty => "No subjects are currently available for this learner.",
                DataStatePanelState.OfflineCached => "Showing subjects saved from the latest classroom update.",
                DataStatePanelState.OfflineUnavailable => "Subjects are unavailable offline on this device.",
                DataStatePanelState.PermissionOrLocked => "Your classroom does not currently allow subject selection.",
                DataStatePanelState.RecoverableError => "Subjects could not be loaded. Try again.",
                DataStatePanelState.Loading => "Loading your available subjects.",
                _ => "Explore your Grade 5 subjects and continue where you left off."
            };
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
            SubjectSelected = null;
            ContinueSubjectRequested = null;
            UnavailableSubjectRequested = null;

            _root = null;
            _backButton = null;
            _introMessage = null;
            _countLabel = null;
            _literaQuestCard = null;
            _peAndHealthCard = null;
            _scienceCard = null;
            _literaQuestContinueButton = null;
            _peAndHealthContinueButton = null;
            _scienceContinueButton = null;
            _literaQuestProgressLabel = null;
            _literaQuestMissionLabel = null;
            _peAndHealthProgressLabel = null;
            _peAndHealthMissionLabel = null;
            _scienceProgressLabel = null;
            _scienceMissionLabel = null;
            _availableSubjects.Clear();
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
            _backButton = _root.Q<Button>("back-button");
            _introMessage = _root.Q<Label>("subject-selection-intro-message");
            _countLabel = _root.Q<Label>(className: "subject-selection__count-label");

            _literaQuestCard = _root.Q<VisualElement>("card-literaquest");
            _peAndHealthCard = _root.Q<VisualElement>("card-pe-health");
            _scienceCard = _root.Q<VisualElement>("card-science");

            _literaQuestContinueButton = _root.Q<Button>("continue-lq-button");
            _peAndHealthContinueButton = _root.Q<Button>("continue-peh-button");
            _scienceContinueButton = _root.Q<Button>("continue-sci-button");

            _literaQuestProgressLabel = _root.Q<Label>("lq-progress-pct");
            _literaQuestMissionLabel = _root.Q<Label>("lq-missions");
            _peAndHealthProgressLabel = _root.Q<Label>("peh-progress-pct");
            _peAndHealthMissionLabel = _root.Q<Label>("peh-missions");
            _scienceProgressLabel = _root.Q<Label>("sci-progress-pct");
            _scienceMissionLabel = _root.Q<Label>("sci-missions");
        }

        private void ApplyStaticPreviewContent()
        {
            if (_literaQuestProgressLabel != null)
            {
                _literaQuestProgressLabel.text = "60%";
            }

            if (_literaQuestMissionLabel != null)
            {
                _literaQuestMissionLabel.text = "9 / 15 Missions";
            }

            if (_peAndHealthProgressLabel != null)
            {
                _peAndHealthProgressLabel.text = "67%";
            }

            if (_peAndHealthMissionLabel != null)
            {
                _peAndHealthMissionLabel.text = "10 / 15 Missions";
            }

            if (_scienceProgressLabel != null)
            {
                _scienceProgressLabel.text = "73%";
            }

            if (_scienceMissionLabel != null)
            {
                _scienceMissionLabel.text = "11 / 15 Missions";
            }
        }

        private void ApplyInitialSelection()
        {
            _literaQuestCard?.EnableInClassList(
                SelectedClass, SelectedSubject == NutriMindSubject.LiteraQuest);
            _peAndHealthCard?.EnableInClassList(SelectedClass, false);
            _scienceCard?.EnableInClassList(
                SelectedClass, SelectedSubject == NutriMindSubject.Science);
        }

        private void RegisterCallbacks()
        {
            _backButton?.RegisterCallback<ClickEvent>(OnBackClicked);

            _literaQuestCard?.RegisterCallback<ClickEvent>(OnLiteraQuestCardClicked);
            _peAndHealthCard?.RegisterCallback<ClickEvent>(OnPeAndHealthCardClicked);
            _scienceCard?.RegisterCallback<ClickEvent>(OnScienceCardClicked);

            _literaQuestCard?.RegisterCallback<KeyDownEvent>(OnLiteraQuestCardKeyDown);
            _peAndHealthCard?.RegisterCallback<KeyDownEvent>(OnPeAndHealthCardKeyDown);
            _scienceCard?.RegisterCallback<KeyDownEvent>(OnScienceCardKeyDown);

            _literaQuestContinueButton?.RegisterCallback<ClickEvent>(
                OnLiteraQuestContinueClicked);
            _peAndHealthContinueButton?.RegisterCallback<ClickEvent>(
                OnPeAndHealthUnavailableClicked);
            _scienceContinueButton?.RegisterCallback<ClickEvent>(
                OnScienceContinueClicked);

            _root?.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        private void UnregisterCallbacks()
        {
            _backButton?.UnregisterCallback<ClickEvent>(OnBackClicked);

            _literaQuestCard?.UnregisterCallback<ClickEvent>(OnLiteraQuestCardClicked);
            _peAndHealthCard?.UnregisterCallback<ClickEvent>(OnPeAndHealthCardClicked);
            _scienceCard?.UnregisterCallback<ClickEvent>(OnScienceCardClicked);

            _literaQuestCard?.UnregisterCallback<KeyDownEvent>(OnLiteraQuestCardKeyDown);
            _peAndHealthCard?.UnregisterCallback<KeyDownEvent>(OnPeAndHealthCardKeyDown);
            _scienceCard?.UnregisterCallback<KeyDownEvent>(OnScienceCardKeyDown);

            _literaQuestContinueButton?.UnregisterCallback<ClickEvent>(
                OnLiteraQuestContinueClicked);
            _peAndHealthContinueButton?.UnregisterCallback<ClickEvent>(
                OnPeAndHealthUnavailableClicked);
            _scienceContinueButton?.UnregisterCallback<ClickEvent>(
                OnScienceContinueClicked);

            _root?.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        private void OnBackClicked(ClickEvent evt)
        {
            BackRequested?.Invoke();
        }

        private void OnLiteraQuestCardClicked(ClickEvent evt)
        {
            SelectSubject(NutriMindSubject.LiteraQuest);
        }

        private void OnScienceCardClicked(ClickEvent evt)
        {
            SelectSubject(NutriMindSubject.Science);
        }

        private void OnPeAndHealthCardClicked(ClickEvent evt)
        {
            SelectSubject(NutriMindSubject.PeAndHealth);
        }

        private void OnLiteraQuestCardKeyDown(KeyDownEvent evt)
        {
            if (IsActivationKey(evt))
            {
                evt.StopPropagation();
                SelectSubject(NutriMindSubject.LiteraQuest);
            }
        }

        private void OnScienceCardKeyDown(KeyDownEvent evt)
        {
            if (IsActivationKey(evt))
            {
                evt.StopPropagation();
                SelectSubject(NutriMindSubject.Science);
            }
        }

        private void OnPeAndHealthCardKeyDown(KeyDownEvent evt)
        {
            if (IsActivationKey(evt))
            {
                evt.StopPropagation();
                SelectSubject(NutriMindSubject.PeAndHealth);
            }
        }

        private static bool IsActivationKey(KeyDownEvent evt)
        {
            return evt.keyCode == KeyCode.Return
                || evt.keyCode == KeyCode.KeypadEnter
                || evt.keyCode == KeyCode.Space;
        }

        private void OnLiteraQuestContinueClicked(ClickEvent evt)
        {
            evt.StopPropagation();
            SelectSubject(NutriMindSubject.LiteraQuest);
            ContinueSubjectRequested?.Invoke(NutriMindSubject.LiteraQuest);
        }

        private void OnScienceContinueClicked(ClickEvent evt)
        {
            evt.StopPropagation();
            SelectSubject(NutriMindSubject.Science);
            ContinueSubjectRequested?.Invoke(NutriMindSubject.Science);
        }

        private void OnPeAndHealthUnavailableClicked(ClickEvent evt)
        {
            evt.StopPropagation();
            if (_availableSubjects.Contains(NutriMindSubject.PeAndHealth))
            {
                SelectSubject(NutriMindSubject.PeAndHealth);
                ContinueSubjectRequested?.Invoke(NutriMindSubject.PeAndHealth);
            }
            else
            {
                UnavailableSubjectRequested?.Invoke(NutriMindSubject.PeAndHealth);
            }
        }

        /// <summary>
        /// Selects an available subject card. Unavailable PE &amp; Health never
        /// becomes selected. Emits <see cref="SubjectSelected"/> only when the
        /// selection actually changes.
        /// </summary>
        private void SelectSubject(NutriMindSubject subject)
        {
            if (!_availableSubjects.Contains(subject))
            {
                return;
            }

            if (SelectedSubject == subject)
            {
                return;
            }

            SelectedSubject = subject;
            ApplySelectionClasses();

            SubjectSelected?.Invoke(subject);
        }

        private void ApplySubjectAvailability(VisualElement card, NutriMindSubject subject)
        {
            if (card == null)
            {
                return;
            }

            bool available = _availableSubjects.Contains(subject);
            bool interactive = DataState == DataStatePanelState.Content
                || DataState == DataStatePanelState.OfflineCached;
            card.SetEnabled(available && interactive);
            card.style.display = available ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void ApplySelectionClasses()
        {
            _literaQuestCard?.EnableInClassList(
                SelectedClass,
                _availableSubjects.Contains(NutriMindSubject.LiteraQuest)
                && SelectedSubject == NutriMindSubject.LiteraQuest);
            _peAndHealthCard?.EnableInClassList(
                SelectedClass,
                _availableSubjects.Contains(NutriMindSubject.PeAndHealth)
                && SelectedSubject == NutriMindSubject.PeAndHealth);
            _scienceCard?.EnableInClassList(
                SelectedClass,
                _availableSubjects.Contains(NutriMindSubject.Science)
                && SelectedSubject == NutriMindSubject.Science);
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
