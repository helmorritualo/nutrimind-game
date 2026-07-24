using UnityEngine;
using UnityEngine.UIElements;

namespace NutriMind.App.UI
{
    /// <summary>
    /// Layout-only term selection panel wiring for UI Toolkit static preview.
    /// Handles responsive classes, term card selection styling, and a
    /// placeholder Continue/View Missions action (logged via Debug.Log only).
    /// Locked term cards are not selectable. Does not perform routing,
    /// progress loading, or networking.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class TermSelectionPanelController : MonoBehaviour
    {
        private const string CompactClass = "term-selection--compact";
        private const string NarrowClass = "term-selection--narrow";
        private const string MobileClass = "mobile";
        private const string CardClass = "term-selection__card";
        private const string LockedCardClass = "term-selection__card--locked";
        private const string NavItemClass = "term-selection__nav-item";
        private const float CompactBreakpoint = 1100f;
        private const float NarrowBreakpoint = 820f;

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

            _root = _uiDocument.rootVisualElement?.Q<VisualElement>("term-selection-root");
            if (_root == null)
            {
                Invoke(nameof(BindWhenReady), 0.05f);
                return;
            }

            var panelRoot = _uiDocument.rootVisualElement;
            panelRoot.style.flexGrow = 1;
            panelRoot.style.width = Length.Percent(100);
            panelRoot.style.height = Length.Percent(100);

            _nav = _root.Q<VisualElement>("term-nav");
            if (_nav != null)
            {
                foreach (var button in _nav.Query<Button>(className: NavItemClass).ToList())
                {
                    button.RegisterCallback<ClickEvent>(OnNavClickEvent);
                }
            }

            foreach (var card in _root.Query<Button>(className: CardClass).ToList())
            {
                card.RegisterCallback<ClickEvent>(OnCardClickEvent);
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
                foreach (var card in _root.Query<Button>(className: CardClass).ToList())
                {
                    card.UnregisterCallback<ClickEvent>(OnCardClickEvent);
                }

                _root.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            }

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
            if (!(evt.currentTarget is Button card))
            {
                return;
            }

            string termLabel = GetTermLabel(card);

            if (card.ClassListContains(LockedCardClass))
            {
                Debug.Log($"[Static Preview] {termLabel} is locked and cannot be opened.");
                return;
            }

            SetActiveCard(card);
            Debug.Log($"[Static Preview] View Missions: {termLabel}");
        }

        private static string GetTermLabel(VisualElement card)
        {
            var tabLabel = card.Q<Label>(className: "term-selection__card-tab-label");
            return tabLabel != null ? tabLabel.text : card.name;
        }

        private void SetActiveCard(VisualElement selected)
        {
            if (_root == null || selected == null)
            {
                return;
            }

            _root.Query<Button>(className: CardClass).ForEach(card =>
            {
                card.EnableInClassList("is-selected", card == selected);
            });
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
