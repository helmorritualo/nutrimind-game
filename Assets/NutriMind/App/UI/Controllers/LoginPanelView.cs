using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace NutriMind.App.UI
{
    public enum LoginStatusTone
    {
        None = 0,
        Danger = 1,
        Warning = 2,
        Info = 3
    }

    /// <summary>
    /// Runtime login panel view (non-MonoBehaviour). Binds to <c>login-root</c>.
    /// Presentation only — never authenticates, calls SQLite, or stores tokens.
    /// </summary>
    public sealed class LoginPanelView : IDisposable
    {
        private const string RootName = "login-root";
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
        private string _deviceName;
        private bool _disposed;
        private bool _checking;
        private float _lastWidth = -1f;

        public event Action SubmitRequested;
        public event Action ForgotPinRequested;

        public LoginPanelView(VisualElement root, string deviceName = null)
        {
            _deviceName = string.IsNullOrWhiteSpace(deviceName)
                ? SystemInfo.deviceName
                : deviceName.Trim();
            if (string.IsNullOrWhiteSpace(_deviceName))
            {
                _deviceName = "NutriMind Device";
            }

            ResolveRoot(root);
            if (_root == null)
            {
                return;
            }

            CacheElements();
            RegisterCallbacks();
            ConfigureTextField(_lrnField, "Enter LRN", isPassword: false);
            ConfigureTextField(_pinField, "Enter PIN", isPassword: true);
            ClearStatus();
            SetChecking(false);
            ApplyResponsiveClasses(_root.resolvedStyle.width);
        }

        public VisualElement Root => _root;

        public bool IsBound => _root != null && !_disposed;

        public string Lrn => _lrnField != null ? (_lrnField.value ?? string.Empty).Trim() : string.Empty;

        public string Pin => _pinField != null ? (_pinField.value ?? string.Empty) : string.Empty;

        public string DeviceName => _deviceName;

        public void SetDeviceName(string deviceName)
        {
            if (string.IsNullOrWhiteSpace(deviceName))
            {
                return;
            }

            _deviceName = deviceName.Trim();
        }

        public void SetLrn(string lrn)
        {
            _lrnField?.SetValueWithoutNotify(lrn ?? string.Empty);
        }

        public void ClearPin()
        {
            _pinField?.SetValueWithoutNotify(string.Empty);
            if (_pinField != null)
            {
                _pinField.isPasswordField = !_pinVisible;
            }
        }

        public void SetChecking(bool checking)
        {
            _checking = checking;
            if (_loginButtonMain != null)
            {
                _loginButtonMain.style.display = checking ? DisplayStyle.None : DisplayStyle.Flex;
            }

            if (_loginButtonChevron != null)
            {
                _loginButtonChevron.style.display = checking ? DisplayStyle.None : DisplayStyle.Flex;
            }

            if (_loginChecking != null)
            {
                _loginChecking.style.display = checking ? DisplayStyle.Flex : DisplayStyle.None;
            }

            _loginButton?.SetEnabled(!checking);
            _lrnField?.SetEnabled(!checking);
            _pinField?.SetEnabled(!checking);
        }

        public void SetSubmitEnabled(bool enabled)
        {
            _loginButton?.SetEnabled(enabled && !_checking);
        }

        public void ClearStatus()
        {
            SetStatus(visible: false, LoginStatusTone.None, null, null);
        }

        public void SetStatus(LoginStatusTone tone, string message, string iconClass = null)
        {
            string variant = null;
            string icon = iconClass;
            switch (tone)
            {
                case LoginStatusTone.Danger:
                    variant = StatusDangerClass;
                    icon = icon ?? IconError;
                    break;
                case LoginStatusTone.Warning:
                    variant = StatusWarningClass;
                    icon = icon ?? IconWarning;
                    break;
                case LoginStatusTone.Info:
                    variant = StatusInfoClass;
                    icon = icon ?? IconWifi;
                    break;
                default:
                    SetStatus(visible: false, LoginStatusTone.None, null, null);
                    return;
            }

            SetStatus(visible: true, tone, variant, icon, message);
        }

        public void SetRateLimitCountdown(int remainingSeconds)
        {
            int clamped = Math.Max(0, remainingSeconds);
            int minutes = clamped / 60;
            int seconds = clamped % 60;
            string text = "Too many attempts. Try again in "
                          + minutes.ToString("00") + ":" + seconds.ToString("00") + ".";
            SetStatus(LoginStatusTone.Danger, text, IconLock);
            SetSubmitEnabled(clamped <= 0);
            SetChecking(false);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            UnregisterCallbacks();
            SubmitRequested = null;
            ForgotPinRequested = null;
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
        }

        private void RegisterCallbacks()
        {
            if (_pinToggle != null)
            {
                _pinToggle.clicked += OnPinToggleClicked;
            }

            if (_loginButton != null)
            {
                _loginButton.clicked += OnLoginClicked;
            }

            if (_forgotPinButton != null)
            {
                _forgotPinButton.clicked += OnForgotPinClicked;
            }

            if (_helpButton != null)
            {
                _helpButton.clicked += OnHelpClicked;
            }

            if (_privacyButton != null)
            {
                _privacyButton.clicked += OnPrivacyClicked;
            }

            if (_helpModalClose != null)
            {
                _helpModalClose.clicked += OnHelpClose;
            }

            if (_helpModalOk != null)
            {
                _helpModalOk.clicked += OnHelpClose;
            }

            if (_privacyModalClose != null)
            {
                _privacyModalClose.clicked += OnPrivacyClose;
            }

            if (_privacyModalOk != null)
            {
                _privacyModalOk.clicked += OnPrivacyClose;
            }

            _helpModalBackdrop?.RegisterCallback<ClickEvent>(OnHelpBackdrop);
            _privacyModalBackdrop?.RegisterCallback<ClickEvent>(OnPrivacyBackdrop);
            _root.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        private void UnregisterCallbacks()
        {
            if (_pinToggle != null)
            {
                _pinToggle.clicked -= OnPinToggleClicked;
            }

            if (_loginButton != null)
            {
                _loginButton.clicked -= OnLoginClicked;
            }

            if (_forgotPinButton != null)
            {
                _forgotPinButton.clicked -= OnForgotPinClicked;
            }

            if (_helpButton != null)
            {
                _helpButton.clicked -= OnHelpClicked;
            }

            if (_privacyButton != null)
            {
                _privacyButton.clicked -= OnPrivacyClicked;
            }

            if (_helpModalClose != null)
            {
                _helpModalClose.clicked -= OnHelpClose;
            }

            if (_helpModalOk != null)
            {
                _helpModalOk.clicked -= OnHelpClose;
            }

            if (_privacyModalClose != null)
            {
                _privacyModalClose.clicked -= OnPrivacyClose;
            }

            if (_privacyModalOk != null)
            {
                _privacyModalOk.clicked -= OnPrivacyClose;
            }

            _helpModalBackdrop?.UnregisterCallback<ClickEvent>(OnHelpBackdrop);
            _privacyModalBackdrop?.UnregisterCallback<ClickEvent>(OnPrivacyBackdrop);
            _root?.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
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

        private void OnLoginClicked()
        {
            if (_checking)
            {
                return;
            }

            SubmitRequested?.Invoke();
        }

        private void OnForgotPinClicked()
        {
            ForgotPinRequested?.Invoke();
        }

        private void OnHelpClicked() => OpenModal(_helpModalBackdrop);

        private void OnPrivacyClicked() => OpenModal(_privacyModalBackdrop);

        private void OnHelpClose() => CloseModal(_helpModalBackdrop);

        private void OnPrivacyClose() => CloseModal(_privacyModalBackdrop);

        private void OnHelpBackdrop(ClickEvent evt)
        {
            if (evt.target == _helpModalBackdrop)
            {
                CloseModal(_helpModalBackdrop);
            }
        }

        private void OnPrivacyBackdrop(ClickEvent evt)
        {
            if (evt.target == _privacyModalBackdrop)
            {
                CloseModal(_privacyModalBackdrop);
            }
        }

        private static void OpenModal(VisualElement backdrop) => backdrop?.AddToClassList(ModalOpenClass);

        private static void CloseModal(VisualElement backdrop) => backdrop?.RemoveFromClassList(ModalOpenClass);

        private void SetStatus(
            bool visible,
            LoginStatusTone tone,
            string variantClass,
            string iconClass,
            string text = null)
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
