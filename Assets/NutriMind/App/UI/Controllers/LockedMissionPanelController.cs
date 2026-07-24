using UnityEngine;
using UnityEngine.UIElements;

namespace NutriMind.App.UI
{
    /// <summary>
    /// Layout-only locked mission panel wiring for UI Toolkit preview.
    /// Handles responsive classes and static nav active state.
    /// Does not perform routing, unlock validation, or networking.
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

        private UIDocument _uiDocument;
        private VisualElement _root;
        private VisualElement _nav;
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
            if (_nav != null)
            {
                foreach (var button in _nav.Query<Button>(className: "locked-mission__nav-item").ToList())
                {
                    button.RegisterCallback<ClickEvent>(OnNavClickEvent);
                }
            }

            _root.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            ApplyResponsiveClasses(_root.resolvedStyle.width);
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
    }
}
