using UnityEngine;
using UnityEngine.UIElements;

namespace NutriMind.App.UI
{
    /// <summary>
    /// Static UI preview states for the Login panel, switchable from the
    /// inspector. Does not represent real authentication outcomes.
    /// </summary>
    public enum LoginPreviewState
    {
        Default,
        ValidationError,
        Checking,
        InvalidCredentials,
        OfflineUnavailable,
        TooManyAttempts
    }

    /// <summary>
    /// Static UI preview only. Wires the login panel's fields, PIN visibility
    /// toggle, Help/Privacy static overlays, responsive layout classes, and
    /// the inspector-driven <see cref="LoginPreviewState"/> visual states.
    /// Does not perform authentication, networking, or credential storage.
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

        private const string ModalOpenClass = "login-panel__modal-backdrop--open";

        private const string StatusDangerClass = "login-panel__status--danger";
        private const string StatusWarningClass = "login-panel__status--warning";
        private const string StatusInfoClass = "login-panel__status--info";

        private const string IconWarning = "ds-icon--warning";
        private const string IconError = "ds-icon--error";
        private const string IconWifi = "ds-icon--wifi";
        private const string IconLock = "ds-icon--lock";

        private static readonly string[] StatusVariantClasses =
        {
            StatusDangerClass, StatusWarningClass, StatusInfoClass
        };

        private static readonly string[] StatusIconClasses =
        {
            IconWarning, IconError, IconWifi, IconLock
        };

        [Tooltip("UI-only preview state. Switches the status banner, login button, and pin field for design review — no real validation occurs.")]
        [SerializeField]
        private LoginPreviewState _previewState = LoginPreviewState.Default;

        private UIDocument _uiDocument;
        private VisualElement _root;

        private TextField _lrnField;
        private TextField _pinField;
        private Button _pinToggle;
        private bool _pinVisible;

        private VisualElement _statusHost;
        private VisualElement _statusIcon;
        private Label _statusText;

        private Button _loginButton;
        private VisualElement _loginButtonMain;
        private VisualElement _loginButtonChevron;
        private VisualElement _loginChecking;

        private Button _forgotPinButton;
        private Button _helpButton;
        private Button _privacyButton;

        private VisualElement _helpModalBackdrop;
        private Button _helpModalClose;
        private Button _helpModalOk;

        private VisualElement _privacyModalBackdrop;
        private Button _privacyModalClose;
        private Button _privacyModalOk;

        private LoginPreviewState? _appliedPreviewState;
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

        private void OnValidate()
        {
            ApplyPreviewState();
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

            _statusHost = _root.Q<VisualElement>("login-status");
            _statusIcon = _root.Q<VisualElement>("login-status-icon");
            _statusText = _root.Q<Label>("login-status-text");

            _loginButton = _root.Q<Button>("login-button");
            _loginButtonMain = _root.Q<VisualElement>("login-button-main");
            _loginButtonChevron = _root.Q<VisualElement>("login-button-chevron");
            _loginChecking = _root.Q<VisualElement>("login-checking");

            _forgotPinButton = _root.Q<Button>("forgot-pin-button");
            _helpButton = _root.Q<Button>("help-button");
            _privacyButton = _root.Q<Button>("privacy-button");

            _helpModalBackdrop = _root.Q<VisualElement>("help-modal-backdrop");
            _helpModalClose = _root.Q<Button>("help-modal-close");
            _helpModalOk = _root.Q<Button>("help-modal-ok");

            _privacyModalBackdrop = _root.Q<VisualElement>("privacy-modal-backdrop");
            _privacyModalClose = _root.Q<Button>("privacy-modal-close");
            _privacyModalOk = _root.Q<Button>("privacy-modal-ok");

            ConfigureTextField(_lrnField, "Enter LRN", isPassword: false);
            ConfigureTextField(_pinField, "Enter PIN", isPassword: true);

            if (_pinToggle != null)
            {
                _pinToggle.clicked += OnPinToggleClicked;
            }

            if (_loginButton != null)
            {
                _loginButton.clicked += OnLoginButtonClicked;
            }

            if (_forgotPinButton != null)
            {
                _forgotPinButton.clicked += OnForgotPinButtonClicked;
            }

            if (_helpButton != null)
            {
                _helpButton.clicked += OnHelpButtonClicked;
            }

            if (_privacyButton != null)
            {
                _privacyButton.clicked += OnPrivacyButtonClicked;
            }

            if (_helpModalClose != null)
            {
                _helpModalClose.clicked += OnHelpModalCloseClicked;
            }

            if (_helpModalOk != null)
            {
                _helpModalOk.clicked += OnHelpModalCloseClicked;
            }

            if (_privacyModalClose != null)
            {
                _privacyModalClose.clicked += OnPrivacyModalCloseClicked;
            }

            if (_privacyModalOk != null)
            {
                _privacyModalOk.clicked += OnPrivacyModalCloseClicked;
            }

            if (_helpModalBackdrop != null)
            {
                _helpModalBackdrop.RegisterCallback<ClickEvent>(OnHelpBackdropClicked);
            }

            if (_privacyModalBackdrop != null)
            {
                _privacyModalBackdrop.RegisterCallback<ClickEvent>(OnPrivacyBackdropClicked);
            }

            _root.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            ApplyResponsiveClasses(_root.resolvedStyle.width);

            _appliedPreviewState = null;
            ApplyPreviewState();
        }

        private void Unbind()
        {
            if (_pinToggle != null)
            {
                _pinToggle.clicked -= OnPinToggleClicked;
            }

            if (_loginButton != null)
            {
                _loginButton.clicked -= OnLoginButtonClicked;
            }

            if (_forgotPinButton != null)
            {
                _forgotPinButton.clicked -= OnForgotPinButtonClicked;
            }

            if (_helpButton != null)
            {
                _helpButton.clicked -= OnHelpButtonClicked;
            }

            if (_privacyButton != null)
            {
                _privacyButton.clicked -= OnPrivacyButtonClicked;
            }

            if (_helpModalClose != null)
            {
                _helpModalClose.clicked -= OnHelpModalCloseClicked;
            }

            if (_helpModalOk != null)
            {
                _helpModalOk.clicked -= OnHelpModalCloseClicked;
            }

            if (_privacyModalClose != null)
            {
                _privacyModalClose.clicked -= OnPrivacyModalCloseClicked;
            }

            if (_privacyModalOk != null)
            {
                _privacyModalOk.clicked -= OnPrivacyModalCloseClicked;
            }

            if (_helpModalBackdrop != null)
            {
                _helpModalBackdrop.UnregisterCallback<ClickEvent>(OnHelpBackdropClicked);
            }

            if (_privacyModalBackdrop != null)
            {
                _privacyModalBackdrop.UnregisterCallback<ClickEvent>(OnPrivacyBackdropClicked);
            }

            if (_root != null)
            {
                _root.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            }

            _root = null;
            _lrnField = null;
            _pinField = null;
            _pinToggle = null;
            _statusHost = null;
            _statusIcon = null;
            _statusText = null;
            _loginButton = null;
            _loginButtonMain = null;
            _loginButtonChevron = null;
            _loginChecking = null;
            _forgotPinButton = null;
            _helpButton = null;
            _privacyButton = null;
            _helpModalBackdrop = null;
            _helpModalClose = null;
            _helpModalOk = null;
            _privacyModalBackdrop = null;
            _privacyModalClose = null;
            _privacyModalOk = null;
            _appliedPreviewState = null;
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
            if (_pinField == null || _pinToggle == null)
            {
                return;
            }

            _pinVisible = !_pinVisible;
            _pinField.isPasswordField = !_pinVisible;
            _pinToggle.tooltip = _pinVisible ? "Hide PIN" : "Show PIN";
        }

        private static void OnLoginButtonClicked()
        {
            Debug.Log("[LoginPanelController] Log In clicked — static UI preview only, no authentication performed.");
        }

        private static void OnForgotPinButtonClicked()
        {
            Debug.Log("[LoginPanelController] Forgot PIN clicked — static UI preview only.");
        }

        private void OnHelpButtonClicked()
        {
            OpenModal(_helpModalBackdrop);
        }

        private void OnPrivacyButtonClicked()
        {
            OpenModal(_privacyModalBackdrop);
        }

        private void OnHelpModalCloseClicked()
        {
            CloseModal(_helpModalBackdrop);
        }

        private void OnPrivacyModalCloseClicked()
        {
            CloseModal(_privacyModalBackdrop);
        }

        private void OnHelpBackdropClicked(ClickEvent evt)
        {
            if (evt.target == _helpModalBackdrop)
            {
                CloseModal(_helpModalBackdrop);
            }
        }

        private void OnPrivacyBackdropClicked(ClickEvent evt)
        {
            if (evt.target == _privacyModalBackdrop)
            {
                CloseModal(_privacyModalBackdrop);
            }
        }

        private static void OpenModal(VisualElement backdrop)
        {
            backdrop?.AddToClassList(ModalOpenClass);
        }

        private static void CloseModal(VisualElement backdrop)
        {
            backdrop?.RemoveFromClassList(ModalOpenClass);
        }

        /// <summary>
        /// Applies the inspector-selected <see cref="LoginPreviewState"/> to the
        /// status banner, login button, and pin field. UI-only — never touches
        /// real credentials or validation logic.
        /// </summary>
        private void ApplyPreviewState()
        {
            if (_root == null)
            {
                return;
            }

            bool stateChanged = _appliedPreviewState != _previewState;
            _appliedPreviewState = _previewState;

            if (stateChanged && _pinField != null)
            {
                _pinField.SetValueWithoutNotify(string.Empty);
            }

            bool isChecking = _previewState == LoginPreviewState.Checking;
            bool isLockedOut = _previewState == LoginPreviewState.TooManyAttempts;

            SetLoginButtonLoading(isChecking);
            _loginButton?.SetEnabled(!isChecking && !isLockedOut);

            switch (_previewState)
            {
                case LoginPreviewState.Default:
                    SetStatus(visible: false);
                    break;
                case LoginPreviewState.ValidationError:
                    SetStatus(true, StatusWarningClass, IconWarning, "Please enter both your LRN and PIN.");
                    break;
                case LoginPreviewState.Checking:
                    SetStatus(visible: false);
                    break;
                case LoginPreviewState.InvalidCredentials:
                    SetStatus(true, StatusDangerClass, IconError, "We could not verify those details. Please try again.");
                    break;
                case LoginPreviewState.OfflineUnavailable:
                    SetStatus(true, StatusInfoClass, IconWifi, "You need an internet connection for your first sign-in.");
                    break;
                case LoginPreviewState.TooManyAttempts:
                    SetStatus(true, StatusDangerClass, IconLock, "Too many attempts. Try again in 04:52.");
                    break;
                default:
                    SetStatus(visible: false);
                    break;
            }
        }

        private void SetLoginButtonLoading(bool loading)
        {
            if (_loginButtonMain != null)
            {
                _loginButtonMain.style.display = loading ? DisplayStyle.None : DisplayStyle.Flex;
            }

            if (_loginButtonChevron != null)
            {
                _loginButtonChevron.style.display = loading ? DisplayStyle.None : DisplayStyle.Flex;
            }

            if (_loginChecking != null)
            {
                _loginChecking.style.display = loading ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private void SetStatus(bool visible, string variantClass = null, string iconClass = null, string text = null)
        {
            if (_statusHost == null)
            {
                return;
            }

            _statusHost.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

            if (!visible)
            {
                return;
            }

            foreach (string variant in StatusVariantClasses)
            {
                _statusHost.RemoveFromClassList(variant);
            }

            if (variantClass != null)
            {
                _statusHost.AddToClassList(variantClass);
            }

            if (_statusIcon != null)
            {
                foreach (string icon in StatusIconClasses)
                {
                    _statusIcon.RemoveFromClassList(icon);
                }

                if (iconClass != null)
                {
                    _statusIcon.AddToClassList(iconClass);
                }
            }

            if (_statusText != null && text != null)
            {
                _statusText.text = text;
            }
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
