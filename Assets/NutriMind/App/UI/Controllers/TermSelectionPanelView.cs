using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace NutriMind.App.UI
{
    /// <summary>
    /// Exactly three terms per subject. Presentation enum only.
    /// </summary>
    public enum NutriMindTerm
    {
        Term1 = 1,
        Term2 = 2,
        Term3 = 3
    }

    /// <summary>
    /// Presentation-only Term Selection route view for content-only
    /// <c>TermSelectionPanel.uxml</c>. Binds the route intro Back control, subject
    /// identity, and exactly three term cards. Raises Back / selection / open /
    /// unavailable requests for the host to handle.
    /// Does not perform routing, mission loading, API calls, SQLite,
    /// synchronization, or AppShell chrome ownership.
    /// </summary>
    public sealed class TermSelectionPanelView : IAppScreenView
    {
        private const string RootName = "term-selection-root";
        private const string SelectedClass = "is-selected";
        private const string LockedCardClass = "term-selection__card--locked";
        private const string CompactClass = "term-selection--compact";
        private const string NarrowClass = "term-selection--narrow";
        private const string MobileClass = "mobile";
        private const string HexLiteraQuestClass = "term-selection__subject-hex--literaquest";
        private const string HexPeHealthClass = "term-selection__subject-hex--pe-health";
        private const string HexScienceClass = "term-selection__subject-hex--science";
        private const float CompactBreakpoint = 1100f;
        private const float NarrowBreakpoint = 820f;
        private const string UnavailableReason = "Previous Term Incomplete";

        private VisualElement _root;
        private Button _backButton;
        private VisualElement _subjectHex;
        private VisualElement _subjectHexIcon;
        private Label _subjectTitle;
        private Label _subjectSubtitle;
        private Label _countLabel;
        private Button _term1Card;
        private Button _term2Card;
        private Button _term3Card;
        private Label _term3Status;
        private readonly HashSet<NutriMindTerm> _availableTerms = new();
        private bool _disposed;
        private float _lastWidth = -1f;

        public event Action BackRequested;
        public event Action<NutriMindTerm> TermSelected;
        public event Action<NutriMindTerm> OpenTermRequested;
        public event Action<NutriMindTerm> UnavailableTermRequested;

        public TermSelectionPanelView(VisualElement root)
        {
            ResolveRoot(root);
            if (_root == null)
            {
                Debug.LogWarning(
                    "[TermSelectionPanelView] Could not resolve term-selection-root " +
                    "inside the supplied element.");
                return;
            }

            CacheElements();
            _availableTerms.Add(NutriMindTerm.Term1);
            _availableTerms.Add(NutriMindTerm.Term2);
            ApplyInitialSelection();
            RegisterCallbacks();
            ApplyResponsiveClasses(_root.resolvedStyle.width);
        }

        public VisualElement Root => _root;

        public bool IsBound => _root != null && !_disposed;
        public DataStatePanelState DataState { get; private set; } = DataStatePanelState.Content;

        public NutriMindSubject Subject { get; private set; } = NutriMindSubject.Science;

        public NutriMindTerm SelectedTerm { get; private set; } = NutriMindTerm.Term2;

        /// <summary>
        /// Updates subject identity presentation only. Does not regenerate cards.
        /// </summary>
        public void SetSubject(NutriMindSubject subject)
        {
            if (!IsBound)
            {
                return;
            }

            Subject = subject;

            if (_subjectTitle != null)
            {
                _subjectTitle.text = GetSubjectLabel(subject);
            }

            if (_subjectSubtitle != null)
            {
                _subjectSubtitle.text = "Choose a term to continue your adventure.";
            }

            ApplySubjectHex(subject);
            ApplySubjectIcon(subject);
        }

        /// <summary>
        /// Shows only terms returned by the runtime source. An empty list clears all cards.
        /// </summary>
        public void SetTerms(IReadOnlyList<NutriMindTerm> terms)
        {
            if (!IsBound)
            {
                return;
            }

            _availableTerms.Clear();
            if (terms != null)
            {
                for (int i = 0; i < terms.Count; i++)
                {
                    NutriMindTerm term = terms[i];
                    if (Enum.IsDefined(typeof(NutriMindTerm), term))
                    {
                        _availableTerms.Add(term);
                    }
                }
            }

            ApplyTermAvailability(_term1Card, NutriMindTerm.Term1);
            ApplyTermAvailability(_term2Card, NutriMindTerm.Term2);
            ApplyTermAvailability(_term3Card, NutriMindTerm.Term3);

            if (!_availableTerms.Contains(SelectedTerm))
            {
                if (_availableTerms.Contains(NutriMindTerm.Term1))
                {
                    SelectedTerm = NutriMindTerm.Term1;
                }
                else if (_availableTerms.Contains(NutriMindTerm.Term2))
                {
                    SelectedTerm = NutriMindTerm.Term2;
                }
                else if (_availableTerms.Contains(NutriMindTerm.Term3))
                {
                    SelectedTerm = NutriMindTerm.Term3;
                }
            }

            ApplySelectionClasses(SelectedTerm);
            if (_countLabel != null)
            {
                int count = _availableTerms.Count;
                _countLabel.text = count == 1 ? "1 Term" : $"{count} Terms";
            }
        }

        public void SetDataState(DataStatePanelState state)
        {
            if (!IsBound)
            {
                return;
            }

            DataState = state;
            ApplyTermAvailability(_term1Card, NutriMindTerm.Term1);
            ApplyTermAvailability(_term2Card, NutriMindTerm.Term2);
            ApplyTermAvailability(_term3Card, NutriMindTerm.Term3);
            if (_subjectSubtitle == null)
            {
                return;
            }

            _subjectSubtitle.text = state switch
            {
                DataStatePanelState.Empty => "No terms are currently available for this subject.",
                DataStatePanelState.OfflineCached => "Showing terms derived from saved classroom missions.",
                DataStatePanelState.OfflineUnavailable => "Terms are unavailable offline on this device.",
                DataStatePanelState.PermissionOrLocked => "Your classroom does not currently allow term selection.",
                DataStatePanelState.RecoverableError => "Terms could not be loaded. Try again.",
                DataStatePanelState.Loading => "Loading available terms.",
                _ => "Choose a term to continue your adventure."
            };
        }

        /// <summary>
        /// Learner-facing availability reason for the locked Term 3 fixture.
        /// </summary>
        public string GetUnavailableReason(NutriMindTerm term)
        {
            if (term == NutriMindTerm.Term3)
            {
                return _term3Status != null && !string.IsNullOrWhiteSpace(_term3Status.text)
                    ? _term3Status.text
                    : UnavailableReason;
            }

            return UnavailableReason;
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
            TermSelected = null;
            OpenTermRequested = null;
            UnavailableTermRequested = null;

            _root = null;
            _backButton = null;
            _subjectHex = null;
            _subjectHexIcon = null;
            _subjectTitle = null;
            _subjectSubtitle = null;
            _countLabel = null;
            _term1Card = null;
            _term2Card = null;
            _term3Card = null;
            _term3Status = null;
            _availableTerms.Clear();
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
            _subjectHex = _root.Q<VisualElement>("subject-hex");
            _subjectHexIcon = _root.Q<VisualElement>("subject-hex-icon");
            _subjectTitle = _root.Q<Label>("subject-title");
            _subjectSubtitle = _root.Q<Label>("subject-subtitle");
            _countLabel = _root.Q<Label>(className: "term-selection__count-label");
            _term1Card = _root.Q<Button>("card-term-1");
            _term2Card = _root.Q<Button>("card-term-2");
            _term3Card = _root.Q<Button>("card-term-3");
            _term3Status = _root.Q<Label>("term-3-status");
        }

        private void ApplyInitialSelection()
        {
            ApplySelectionClasses(SelectedTerm);
        }

        private void RegisterCallbacks()
        {
            _backButton?.RegisterCallback<ClickEvent>(OnBackClicked);
            _term1Card?.RegisterCallback<ClickEvent>(OnTerm1Clicked);
            _term2Card?.RegisterCallback<ClickEvent>(OnTerm2Clicked);
            _term3Card?.RegisterCallback<ClickEvent>(OnTerm3Clicked);
            _root?.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        private void UnregisterCallbacks()
        {
            _backButton?.UnregisterCallback<ClickEvent>(OnBackClicked);
            _term1Card?.UnregisterCallback<ClickEvent>(OnTerm1Clicked);
            _term2Card?.UnregisterCallback<ClickEvent>(OnTerm2Clicked);
            _term3Card?.UnregisterCallback<ClickEvent>(OnTerm3Clicked);
            _root?.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        private void OnBackClicked(ClickEvent evt)
        {
            BackRequested?.Invoke();
        }

        private void OnTerm1Clicked(ClickEvent evt)
        {
            HandleAvailableTerm(NutriMindTerm.Term1);
        }

        private void OnTerm2Clicked(ClickEvent evt)
        {
            HandleAvailableTerm(NutriMindTerm.Term2);
        }

        private void OnTerm3Clicked(ClickEvent evt)
        {
            HandleTerm(NutriMindTerm.Term3);
        }

        private void HandleAvailableTerm(NutriMindTerm term)
        {
            HandleTerm(term);
        }

        private void HandleTerm(NutriMindTerm term)
        {
            if (!_availableTerms.Contains(term))
            {
                UnavailableTermRequested?.Invoke(term);
                return;
            }

            SelectTerm(term);
            OpenTermRequested?.Invoke(term);
        }

        private void SelectTerm(NutriMindTerm term)
        {
            if (!_availableTerms.Contains(term))
            {
                return;
            }

            if (SelectedTerm == term)
            {
                return;
            }

            SelectedTerm = term;
            ApplySelectionClasses(term);
            TermSelected?.Invoke(term);
        }

        private void ApplySelectionClasses(NutriMindTerm term)
        {
            _term1Card?.EnableInClassList(SelectedClass, term == NutriMindTerm.Term1);
            _term2Card?.EnableInClassList(SelectedClass, term == NutriMindTerm.Term2);
            _term3Card?.EnableInClassList(
                SelectedClass,
                _availableTerms.Contains(NutriMindTerm.Term3) && term == NutriMindTerm.Term3);
            _ = LockedCardClass;
        }

        private void ApplyTermAvailability(VisualElement card, NutriMindTerm term)
        {
            if (card == null)
            {
                return;
            }

            bool available = _availableTerms.Contains(term);
            bool interactive = DataState == DataStatePanelState.Content
                || DataState == DataStatePanelState.OfflineCached;
            card.SetEnabled(available && interactive);
            card.style.display = available ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void ApplySubjectHex(NutriMindSubject subject)
        {
            if (_subjectHex == null)
            {
                return;
            }

            _subjectHex.EnableInClassList(
                HexLiteraQuestClass, subject == NutriMindSubject.LiteraQuest);
            _subjectHex.EnableInClassList(
                HexPeHealthClass, subject == NutriMindSubject.PeAndHealth);
            _subjectHex.EnableInClassList(
                HexScienceClass, subject == NutriMindSubject.Science);
        }

        private void ApplySubjectIcon(NutriMindSubject subject)
        {
            if (_subjectHexIcon == null)
            {
                return;
            }

            RemoveDsIconModifiers(_subjectHexIcon);

            switch (subject)
            {
                case NutriMindSubject.LiteraQuest:
                    _subjectHexIcon.AddToClassList("ds-icon--book");
                    break;
                case NutriMindSubject.PeAndHealth:
                    _subjectHexIcon.AddToClassList("ds-icon--heart");
                    break;
                case NutriMindSubject.Science:
                default:
                    _subjectHexIcon.AddToClassList("ds-icon--potion");
                    break;
            }
        }

        private static void RemoveDsIconModifiers(VisualElement icon)
        {
            var toRemove = new System.Collections.Generic.List<string>();
            foreach (string existingClass in icon.GetClasses())
            {
                if (existingClass.StartsWith("ds-icon--", StringComparison.Ordinal))
                {
                    toRemove.Add(existingClass);
                }
            }

            for (int i = 0; i < toRemove.Count; i++)
            {
                icon.RemoveFromClassList(toRemove[i]);
            }
        }

        private static string GetSubjectLabel(NutriMindSubject subject)
        {
            switch (subject)
            {
                case NutriMindSubject.LiteraQuest:
                    return "LiteraQuest";
                case NutriMindSubject.PeAndHealth:
                    return "PE & Health";
                case NutriMindSubject.Science:
                    return "Science";
                default:
                    return subject.ToString();
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
