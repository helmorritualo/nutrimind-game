using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace NutriMind.App.UI
{
    /// <summary>
    /// Layout-only subject selection panel wiring for UI Toolkit static preview.
    /// Handles responsive classes, card selection styling, and placeholder
    /// back/nav/Continue actions (logged via Debug.Log only).
    /// Does not perform routing, progress loading, or networking.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class SubjectSelectionPanelController : MonoBehaviour
    {
        private const string CompactClass = "subject-selection--compact";
        private const string NarrowClass = "subject-selection--narrow";
        private const string MobileClass = "mobile";
        private const string CardClass = "subject-selection__card";
        private const string UnavailableCardClass = "subject-selection__card--unavailable";
        private const string NavItemClass = "subject-selection__nav-item";
        private const float CompactBreakpoint = 1100f;
        private const float NarrowBreakpoint = 820f;

        private static readonly string[] ContinueButtonNames =
        {
            "continue-lq-button",
            "continue-peh-button",
            "continue-sci-button",
        };

        private readonly List<Button> _continueButtons = new List<Button>();

        private UIDocument _uiDocument;
        private VisualElement _root;
        private VisualElement _nav;
        private Button _backButton;
        private float _lastWidth = -1f;

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

            _root = _uiDocument.rootVisualElement?.Q<VisualElement>("subject-selection-root");
            if (_root == null)
            {
                Invoke(nameof(BindWhenReady), 0.05f);
                return;
            }

            var panelRoot = _uiDocument.rootVisualElement;
            panelRoot.style.flexGrow = 1;
            panelRoot.style.width = Length.Percent(100);
            panelRoot.style.height = Length.Percent(100);

            _nav = _root.Q<VisualElement>("subject-nav");
            if (_nav != null)
            {
                foreach (var button in _nav.Query<Button>(className: NavItemClass).ToList())
                {
                    button.RegisterCallback<ClickEvent>(OnNavClickEvent);
                }
            }

            foreach (var card in _root.Query<VisualElement>(className: CardClass).ToList())
            {
                if (!card.ClassListContains(UnavailableCardClass))
                {
                    card.RegisterCallback<ClickEvent>(OnCardClickEvent);
                }
            }

            foreach (var buttonName in ContinueButtonNames)
            {
                var button = _root.Q<Button>(buttonName);
                if (button == null)
                {
                    continue;
                }

                button.RegisterCallback<ClickEvent>(OnContinueClickEvent);
                _continueButtons.Add(button);
            }

            _backButton = _root.Q<Button>("back-button");
            if (_backButton != null)
            {
                _backButton.RegisterCallback<ClickEvent>(OnBackClicked);
            }

            _root.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            ApplyResponsiveClasses(_root.resolvedStyle.width);
        }

        private void Unbind()
        {
            if (_nav != null)
            {
                foreach (var button in _nav.Query<Button>(className: NavItemClass).ToList())
                {
                    button.UnregisterCallback<ClickEvent>(OnNavClickEvent);
                }
            }

            if (_root != null)
            {
                foreach (var card in _root.Query<VisualElement>(className: CardClass).ToList())
                {
                    card.UnregisterCallback<ClickEvent>(OnCardClickEvent);
                }

                _root.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            }

            foreach (var button in _continueButtons)
            {
                button.UnregisterCallback<ClickEvent>(OnContinueClickEvent);
            }

            _continueButtons.Clear();

            if (_backButton != null)
            {
                _backButton.UnregisterCallback<ClickEvent>(OnBackClicked);
            }

            _root = null;
            _nav = null;
            _backButton = null;
            _lastWidth = -1f;
        }

        private void OnCardClickEvent(ClickEvent evt)
        {
            if (evt.currentTarget is VisualElement card)
            {
                SetActiveCard(card);
            }
        }

        private void SetActiveCard(VisualElement selected)
        {
            if (_root == null || selected == null)
            {
                return;
            }

            _root.Query<VisualElement>(className: CardClass).ForEach(card =>
            {
                card.EnableInClassList("is-selected", card == selected);
            });
        }

        private void OnContinueClickEvent(ClickEvent evt)
        {
            evt.StopPropagation();

            if (!(evt.currentTarget is Button button))
            {
                return;
            }

            string subjectLabel = GetSubjectLabel(button.name);
            if (IsInUnavailableCard(button))
            {
                Debug.Log($"[Static Preview] {subjectLabel} is unavailable in this classroom.");
                return;
            }

            Debug.Log($"[Static Preview] View Terms: {subjectLabel}");
        }

        private static string GetSubjectLabel(string continueButtonName)
        {
            switch (continueButtonName)
            {
                case "continue-lq-button":
                    return "LiteraQuest";
                case "continue-peh-button":
                    return "PE & Health";
                case "continue-sci-button":
                    return "Science";
                default:
                    return continueButtonName;
            }
        }

        private static bool IsInUnavailableCard(VisualElement element)
        {
            var current = element;
            while (current != null)
            {
                if (current.ClassListContains(UnavailableCardClass))
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private void OnBackClicked(ClickEvent evt)
        {
            Debug.Log("[Static Preview] Back button clicked.");
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

            _nav.Query<Button>(className: NavItemClass).ForEach(button =>
            {
                button.EnableInClassList("is-active", button == selected);
            });
        }
    }
}
