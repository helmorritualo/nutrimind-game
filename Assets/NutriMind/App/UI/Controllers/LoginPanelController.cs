using UnityEngine;
using UnityEngine.UIElements;

namespace NutriMind.App.UI
{
    /// <summary>
    /// Layout-only login panel wiring for UI Toolkit preview.
    /// Handles placeholders, PIN visibility, and responsive layout classes.
    /// Does not perform authentication or networking.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class LoginPanelController : MonoBehaviour
    {
        private const string CompactClass = "login-panel--compact";
        private const string NarrowClass = "login-panel--narrow";
        private const string MobileClass = "mobile";
        private const float CompactBreakpoint = 1100f;
        private const float NarrowBreakpoint = 820f;

        [SerializeField]
        private bool _showErrorState = true;

        private UIDocument _uiDocument;
        private VisualElement _root;
        private TextField _lrnField;
        private TextField _pinField;
        private Button _pinToggle;
        private VisualElement _errorBanner;
        private bool _pinVisible;
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

            _root = _uiDocument.rootVisualElement?.Q<VisualElement>("login-root");
            if (_root == null)
            {
                Invoke(nameof(BindWhenReady), 0.05f);
                return;
            }

            var panelRoot = _uiDocument.rootVisualElement;
            panelRoot.style.flexGrow = 1;
            panelRoot.style.width = Length.Percent(100);
            panelRoot.style.height = Length.Percent(100);

            _lrnField = _root.Q<TextField>("lrn-field");
            _pinField = _root.Q<TextField>("pin-field");
            _pinToggle = _root.Q<Button>("pin-toggle");
            _errorBanner = _root.Q<VisualElement>("login-error");

            ConfigureTextField(_lrnField, "Enter LRN", isPassword: false);
            ConfigureTextField(_pinField, "Enter PIN", isPassword: true);

            if (_errorBanner != null)
            {
                _errorBanner.style.display = _showErrorState ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (_pinToggle != null)
            {
                _pinToggle.clicked += OnPinToggleClicked;
            }

            _root.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            ApplyResponsiveClasses(_root.resolvedStyle.width);
        }

        private void Unbind()
        {
            if (_pinToggle != null)
            {
                _pinToggle.clicked -= OnPinToggleClicked;
            }

            if (_root != null)
            {
                _root.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            }

            _root = null;
            _lrnField = null;
            _pinField = null;
            _pinToggle = null;
            _errorBanner = null;
            _lastWidth = -1f;
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

        private void OnPinToggleClicked()
        {
            if (_pinField == null)
            {
                return;
            }

            _pinVisible = !_pinVisible;
            _pinField.isPasswordField = !_pinVisible;
            _pinToggle.tooltip = _pinVisible ? "Hide PIN" : "Show PIN";
        }

        private static void ConfigureTextField(TextField field, string placeholder, bool isPassword)
        {
            if (field == null)
            {
                return;
            }

            field.isPasswordField = isPassword;
            field.label = string.Empty;

            if (field.textEdition != null)
            {
                field.textEdition.placeholder = placeholder;
            }
        }
    }
}
