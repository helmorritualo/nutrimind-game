using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace NutriMind.App.UI
{
    /// <summary>
    /// Static UI preview states for the Bootstrap panel.
    /// Maps 1:1 to the application screen specification snake_case contract values.
    /// Does not represent real startup outcomes.
    /// </summary>
    public enum BootstrapPreviewState
    {
        InitializingLocalStorage,
        CheckingSecureToken,
        CheckingConnectivity,
        CheckingClientVersion,
        CheckingManifest,
        LoadingBootstrap,
        OfflineEligible,
        AuthenticationRequired,
        Maintenance,
        RequiredUpdate,
        RecoverableError,
        Ready
    }

    /// <summary>
    /// Presentation-only Bootstrap startup panel view.
    /// Not a MonoBehaviour — construct with an already-instantiated <c>bootstrap-root</c>,
    /// subscribe to request events from the owner, and call <see cref="Dispose"/> when unbound.
    /// Does not perform startup checks, authentication, networking, SQLite, sync,
    /// application updates, scene loading, or production routing.
    /// </summary>
    public sealed class BootstrapPanelView : IDisposable
    {
        private const string RootName = "bootstrap-root";

        private const string ToneLoadingClass = "bootstrap-panel--loading";
        private const string ToneInformationClass = "bootstrap-panel--information";
        private const string ToneSuccessClass = "bootstrap-panel--success";
        private const string ToneWarningClass = "bootstrap-panel--warning";
        private const string ToneErrorClass = "bootstrap-panel--error";

        private const string CompactClass = "bootstrap-panel--compact";
        private const string NarrowClass = "bootstrap-panel--narrow";
        private const string MobileClass = "mobile";

        private const string StateIconHiddenClass = "bootstrap-panel__state-icon--hidden";
        private const string StateIconBackgroundHiddenClass = "bootstrap-panel__state-icon-background--hidden";
        private const string ActionsHiddenClass = "bootstrap-panel__actions--hidden";
        private const string ActionsSingleClass = "bootstrap-panel__actions--single";
        private const string SecondaryHiddenClass = "bootstrap-panel__secondary-action--hidden";
        private const string ReferenceHiddenClass = "bootstrap-panel__reference--hidden";

        private const string StructuralIconClass = "ds-icon";
        private const string StructuralStateIconClass = "bootstrap-panel__state-icon";

        private const float CompactBreakpoint = 1100f;
        private const float NarrowBreakpoint = 820f;

        private static readonly string[] ToneClasses =
        {
            ToneLoadingClass,
            ToneInformationClass,
            ToneSuccessClass,
            ToneWarningClass,
            ToneErrorClass
        };

        private static readonly string[] SemanticIconWhitelist =
        {
            "ds-icon--leaf",
            "ds-icon--book",
            "ds-icon--check",
            "ds-icon--lock",
            "ds-icon--wifi",
            "ds-icon--refresh",
            "ds-icon--info",
            "ds-icon--warning",
            "ds-icon--error",
            "ds-icon--clock",
            "ds-icon--user"
        };

        private VisualElement _root;
        private VisualElement _stateIconBackground;
        private VisualElement _stateIcon;
        private Label _message;
        private Label _detail;
        private VisualElement _progressRegion;
        private ProgressBar _progressBar;
        private Label _progressPercent;
        private VisualElement _actions;
        private Button _secondaryAction;
        private Button _primaryAction;
        private Label _reference;

        private BootstrapPreviewState _state = BootstrapPreviewState.InitializingLocalStorage;
        private BootstrapPanelStateConfiguration _configuration;
        private BootstrapPrimaryAction _primaryActionType = BootstrapPrimaryAction.None;
        private BootstrapSecondaryAction _secondaryActionType = BootstrapSecondaryAction.None;
        private bool _disposed;
        private bool _isRaisingAction;
        private float _lastWidth = -1f;

        /// <summary>
        /// Raised when Retry / Try Again / Retry Connection is requested.
        /// Does not restart a real startup operation — the owner decides.
        /// </summary>
        public event Action RetryRequested;

        /// <summary>
        /// Raised when Continue Offline is requested.
        /// Does not open AppShell — the owner decides after local eligibility.
        /// </summary>
        public event Action ContinueOfflineRequested;

        /// <summary>
        /// Raised when Continue to Sign In is requested.
        /// Does not open Login — the owner decides.
        /// </summary>
        public event Action OpenLoginRequested;

        /// <summary>
        /// Raised when Update App is requested.
        /// Does not start an update flow — the owner decides.
        /// </summary>
        public event Action UpdateApplicationRequested;

        /// <summary>
        /// Raised when Continue is requested from the Ready state.
        /// Does not open AppShell — the owner decides after ready.
        /// </summary>
        public event Action ContinueToApplicationRequested;

        /// <summary>
        /// Creates a view bound to an already-instantiated bootstrap root,
        /// a TemplateContainer containing the root, or a local host that contains it.
        /// </summary>
        public BootstrapPanelView(VisualElement root)
        {
            ResolveRoot(root);
            if (_root == null)
            {
                return;
            }

            CacheElements();
            RegisterCallbacks();
            ApplyResponsiveClasses(_root.resolvedStyle.width);
            SetState(BootstrapPreviewState.InitializingLocalStorage);
        }

        public VisualElement Root => _root;

        public bool IsBound => _root != null && !_disposed;

        public BootstrapPreviewState State => _state;

        public string ContractStateName => GetContractStateName(_state);

        /// <summary>
        /// Returns the exact snake_case contract value for the preview state.
        /// </summary>
        public static string GetContractStateName(BootstrapPreviewState state)
        {
            switch (state)
            {
                case BootstrapPreviewState.InitializingLocalStorage:
                    return "initializing_local_storage";
                case BootstrapPreviewState.CheckingSecureToken:
                    return "checking_secure_token";
                case BootstrapPreviewState.CheckingConnectivity:
                    return "checking_connectivity";
                case BootstrapPreviewState.CheckingClientVersion:
                    return "checking_client_version";
                case BootstrapPreviewState.CheckingManifest:
                    return "checking_manifest";
                case BootstrapPreviewState.LoadingBootstrap:
                    return "loading_bootstrap";
                case BootstrapPreviewState.OfflineEligible:
                    return "offline_eligible";
                case BootstrapPreviewState.AuthenticationRequired:
                    return "authentication_required";
                case BootstrapPreviewState.Maintenance:
                    return "maintenance";
                case BootstrapPreviewState.RequiredUpdate:
                    return "required_update";
                case BootstrapPreviewState.RecoverableError:
                    return "recoverable_error";
                case BootstrapPreviewState.Ready:
                    return "ready";
                default:
                    return "initializing_local_storage";
            }
        }

        /// <summary>
        /// Applies the immutable presentation configuration for the given preview state.
        /// Does not auto-advance, auto-route, or perform startup work.
        /// </summary>
        public void SetState(BootstrapPreviewState state)
        {
            if (!IsBound)
            {
                return;
            }

            _state = state;
            _configuration = ResolveConfiguration(state);
            ApplyConfiguration(_configuration);
        }

        public void SetReferenceText(string text)
        {
            if (_reference == null)
            {
                return;
            }

            string resolved = string.IsNullOrWhiteSpace(text)
                ? string.Empty
                : text.Trim();
            _reference.text = resolved;
            _reference.tooltip = resolved;
        }

        public void SetReferenceVisible(bool visible)
        {
            if (_reference == null)
            {
                return;
            }

            _reference.EnableInClassList(ReferenceHiddenClass, !visible);
        }

        public void SetPrimaryActionEnabled(bool enabled)
        {
            if (_primaryAction == null)
            {
                return;
            }

            _primaryAction.SetEnabled(enabled);
        }

        public void SetSecondaryActionEnabled(bool enabled)
        {
            if (_secondaryAction == null)
            {
                return;
            }

            _secondaryAction.SetEnabled(enabled);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            UnregisterCallbacks();
            RetryRequested = null;
            ContinueOfflineRequested = null;
            OpenLoginRequested = null;
            UpdateApplicationRequested = null;
            ContinueToApplicationRequested = null;
            _root = null;
            _stateIconBackground = null;
            _stateIcon = null;
            _message = null;
            _detail = null;
            _progressRegion = null;
            _progressBar = null;
            _progressPercent = null;
            _actions = null;
            _secondaryAction = null;
            _primaryAction = null;
            _reference = null;
            _primaryActionType = BootstrapPrimaryAction.None;
            _secondaryActionType = BootstrapSecondaryAction.None;
            _lastWidth = -1f;
            _isRaisingAction = false;
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
            _stateIconBackground = _root.Q<VisualElement>("bootstrap-state-icon-background");
            _stateIcon = _root.Q<VisualElement>("bootstrap-state-icon");
            _message = _root.Q<Label>("bootstrap-message");
            _detail = _root.Q<Label>("bootstrap-detail");
            _progressRegion = _root.Q<VisualElement>("bootstrap-progress-region");
            _progressBar = _root.Q<ProgressBar>("bootstrap-progress");
            _progressPercent = _root.Q<Label>("bootstrap-progress-percent");
            _actions = _root.Q<VisualElement>("bootstrap-actions");
            _secondaryAction = _root.Q<Button>("bootstrap-secondary-action");
            _primaryAction = _root.Q<Button>("bootstrap-primary-action");
            _reference = _root.Q<Label>("bootstrap-reference");

            if (_progressBar != null)
            {
                _progressBar.lowValue = 0f;
                _progressBar.highValue = 100f;
            }
        }

        private void RegisterCallbacks()
        {
            if (_primaryAction != null)
            {
                _primaryAction.clicked += OnPrimaryActionClicked;
            }

            if (_secondaryAction != null)
            {
                _secondaryAction.clicked += OnSecondaryActionClicked;
            }

            _root.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        private void UnregisterCallbacks()
        {
            if (_primaryAction != null)
            {
                _primaryAction.clicked -= OnPrimaryActionClicked;
            }

            if (_secondaryAction != null)
            {
                _secondaryAction.clicked -= OnSecondaryActionClicked;
            }

            if (_root != null)
            {
                _root.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            }
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            ApplyResponsiveClasses(evt.newRect.width);
        }

        private void OnPrimaryActionClicked()
        {
            if (!IsBound || _isRaisingAction || _primaryActionType == BootstrapPrimaryAction.None)
            {
                return;
            }

            if (_primaryAction != null && !_primaryAction.enabledSelf)
            {
                return;
            }

            _isRaisingAction = true;
            try
            {
                switch (_primaryActionType)
                {
                    case BootstrapPrimaryAction.Retry:
                        RetryRequested?.Invoke();
                        break;
                    case BootstrapPrimaryAction.OpenLogin:
                        OpenLoginRequested?.Invoke();
                        break;
                    case BootstrapPrimaryAction.UpdateApplication:
                        UpdateApplicationRequested?.Invoke();
                        break;
                    case BootstrapPrimaryAction.ContinueToApplication:
                        ContinueToApplicationRequested?.Invoke();
                        break;
                }
            }
            finally
            {
                _isRaisingAction = false;
            }
        }

        private void OnSecondaryActionClicked()
        {
            if (!IsBound || _isRaisingAction || _secondaryActionType == BootstrapSecondaryAction.None)
            {
                return;
            }

            if (_secondaryAction != null && !_secondaryAction.enabledSelf)
            {
                return;
            }

            _isRaisingAction = true;
            try
            {
                if (_secondaryActionType == BootstrapSecondaryAction.ContinueOffline)
                {
                    ContinueOfflineRequested?.Invoke();
                }
            }
            finally
            {
                _isRaisingAction = false;
            }
        }

        private void ApplyConfiguration(BootstrapPanelStateConfiguration configuration)
        {
            ApplyTone(configuration.Tone);
            ApplyCopy(configuration.Message, configuration.Detail);
            ApplyIcon(configuration.IconClass, configuration.IsLoadingState);
            ApplyProgress(configuration.NormalizedProgress, configuration.ShowProgress);
            ApplyActions(
                configuration.PrimaryAction,
                configuration.PrimaryActionLabel,
                configuration.SecondaryAction,
                configuration.SecondaryActionLabel);

            if (configuration.PrimaryAction != BootstrapPrimaryAction.None)
            {
                _root.schedule.Execute(FocusPrimaryActionSafely);
            }
        }

        private void ApplyTone(BootstrapPanelTone tone)
        {
            if (_root == null)
            {
                return;
            }

            for (int i = 0; i < ToneClasses.Length; i++)
            {
                _root.RemoveFromClassList(ToneClasses[i]);
            }

            switch (tone)
            {
                case BootstrapPanelTone.Information:
                    _root.AddToClassList(ToneInformationClass);
                    break;
                case BootstrapPanelTone.Success:
                    _root.AddToClassList(ToneSuccessClass);
                    break;
                case BootstrapPanelTone.Warning:
                    _root.AddToClassList(ToneWarningClass);
                    break;
                case BootstrapPanelTone.Error:
                    _root.AddToClassList(ToneErrorClass);
                    break;
                default:
                    _root.AddToClassList(ToneLoadingClass);
                    break;
            }
        }

        private void ApplyCopy(string message, string detail)
        {
            if (_message != null)
            {
                _message.text = message;
                _message.tooltip = message;
            }

            if (_detail != null)
            {
                _detail.text = detail;
                _detail.tooltip = detail;
            }
        }

        private void ApplyIcon(string iconClass, bool isLoadingState)
        {
            bool hasIcon = !isLoadingState && !string.IsNullOrWhiteSpace(iconClass);

            // Loading states have no spinner and no semantic icon: hide the whole
            // icon circle so the scaled title/message become the focal point.
            if (_stateIconBackground != null)
            {
                _stateIconBackground.EnableInClassList(StateIconBackgroundHiddenClass, !hasIcon);
            }

            if (_stateIcon == null)
            {
                return;
            }

            if (!hasIcon)
            {
                _stateIcon.AddToClassList(StateIconHiddenClass);
                return;
            }

            for (int i = 0; i < SemanticIconWhitelist.Length; i++)
            {
                _stateIcon.RemoveFromClassList(SemanticIconWhitelist[i]);
            }

            string resolvedIcon = ResolveWhitelistedIcon(iconClass);
            if (resolvedIcon != null)
            {
                if (!_stateIcon.ClassListContains(StructuralIconClass))
                {
                    _stateIcon.AddToClassList(StructuralIconClass);
                }

                if (!_stateIcon.ClassListContains(StructuralStateIconClass))
                {
                    _stateIcon.AddToClassList(StructuralStateIconClass);
                }

                _stateIcon.AddToClassList(resolvedIcon);
            }

            _stateIcon.RemoveFromClassList(StateIconHiddenClass);
        }

        private static string ResolveWhitelistedIcon(string iconClass)
        {
            if (string.IsNullOrWhiteSpace(iconClass))
            {
                return null;
            }

            string trimmed = iconClass.Trim();
            for (int i = 0; i < SemanticIconWhitelist.Length; i++)
            {
                if (string.Equals(SemanticIconWhitelist[i], trimmed, StringComparison.Ordinal))
                {
                    return SemanticIconWhitelist[i];
                }
            }

            return null;
        }

        private void ApplyProgress(float normalizedProgress, bool showProgress)
        {
            float clamped = Mathf.Clamp01(normalizedProgress);
            float percent = clamped * 100f;

            if (_progressRegion != null)
            {
                _progressRegion.style.display = showProgress ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (!showProgress)
            {
                return;
            }

            if (_progressBar != null)
            {
                _progressBar.value = percent;
            }

            if (_progressPercent != null)
            {
                _progressPercent.text = $"{Mathf.RoundToInt(percent)}%";
            }
        }

        private void ApplyActions(
            BootstrapPrimaryAction primaryAction,
            string primaryLabel,
            BootstrapSecondaryAction secondaryAction,
            string secondaryLabel)
        {
            _primaryActionType = primaryAction;
            _secondaryActionType = secondaryAction;

            bool showPrimary = primaryAction != BootstrapPrimaryAction.None;
            bool showSecondary = secondaryAction != BootstrapSecondaryAction.None;
            bool showActions = showPrimary || showSecondary;

            if (_actions != null)
            {
                _actions.EnableInClassList(ActionsHiddenClass, !showActions);
                _actions.EnableInClassList(ActionsSingleClass, showPrimary && !showSecondary);
            }

            if (_primaryAction != null)
            {
                if (showPrimary)
                {
                    string resolved = string.IsNullOrWhiteSpace(primaryLabel)
                        ? string.Empty
                        : primaryLabel.Trim();
                    _primaryAction.text = resolved;
                    _primaryAction.tooltip = resolved;
                    _primaryAction.style.display = DisplayStyle.Flex;
                    _primaryAction.focusable = true;
                    _primaryAction.SetEnabled(true);
                }
                else
                {
                    _primaryAction.text = string.Empty;
                    _primaryAction.tooltip = string.Empty;
                    _primaryAction.style.display = DisplayStyle.None;
                    _primaryAction.focusable = false;
                }
            }

            if (_secondaryAction != null)
            {
                if (showSecondary)
                {
                    string resolved = string.IsNullOrWhiteSpace(secondaryLabel)
                        ? string.Empty
                        : secondaryLabel.Trim();
                    _secondaryAction.text = resolved;
                    _secondaryAction.tooltip = resolved;
                    _secondaryAction.RemoveFromClassList(SecondaryHiddenClass);
                    _secondaryAction.style.display = DisplayStyle.Flex;
                    _secondaryAction.focusable = true;
                    _secondaryAction.SetEnabled(true);
                }
                else
                {
                    _secondaryAction.text = string.Empty;
                    _secondaryAction.tooltip = string.Empty;
                    _secondaryAction.AddToClassList(SecondaryHiddenClass);
                    _secondaryAction.style.display = DisplayStyle.None;
                    _secondaryAction.focusable = false;
                }
            }
        }

        private void FocusPrimaryActionSafely()
        {
            if (!IsBound
                || _primaryAction == null
                || _primaryActionType == BootstrapPrimaryAction.None
                || !_primaryAction.enabledSelf
                || !_primaryAction.focusable)
            {
                return;
            }

            if (_actions != null && _actions.ClassListContains(ActionsHiddenClass))
            {
                return;
            }

            _primaryAction.Focus();
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

        private static BootstrapPanelStateConfiguration ResolveConfiguration(BootstrapPreviewState state)
        {
            switch (state)
            {
                case BootstrapPreviewState.CheckingSecureToken:
                    return new BootstrapPanelStateConfiguration(
                        message: "Checking your secure session...",
                        detail: "Your PIN is never displayed.",
                        iconClass: null,
                        tone: BootstrapPanelTone.Loading,
                        normalizedProgress: 0.22f,
                        isLoadingState: true,
                        showProgress: true,
                        primaryAction: BootstrapPrimaryAction.None,
                        primaryActionLabel: null,
                        secondaryAction: BootstrapSecondaryAction.None,
                        secondaryActionLabel: null);

                case BootstrapPreviewState.CheckingConnectivity:
                    return new BootstrapPanelStateConfiguration(
                        message: "Checking your connection...",
                        detail: "Offline learning may still be available.",
                        iconClass: null,
                        tone: BootstrapPanelTone.Loading,
                        normalizedProgress: 0.38f,
                        isLoadingState: true,
                        showProgress: true,
                        primaryAction: BootstrapPrimaryAction.None,
                        primaryActionLabel: null,
                        secondaryAction: BootstrapSecondaryAction.None,
                        secondaryActionLabel: null);

                case BootstrapPreviewState.CheckingClientVersion:
                    return new BootstrapPanelStateConfiguration(
                        message: "Checking the application version...",
                        detail: "Please wait.",
                        iconClass: null,
                        tone: BootstrapPanelTone.Loading,
                        normalizedProgress: 0.54f,
                        isLoadingState: true,
                        showProgress: true,
                        primaryAction: BootstrapPrimaryAction.None,
                        primaryActionLabel: null,
                        secondaryAction: BootstrapSecondaryAction.None,
                        secondaryActionLabel: null);

                case BootstrapPreviewState.CheckingManifest:
                    return new BootstrapPanelStateConfiguration(
                        message: "Checking available learning content...",
                        detail: "Please wait.",
                        iconClass: null,
                        tone: BootstrapPanelTone.Loading,
                        normalizedProgress: 0.70f,
                        isLoadingState: true,
                        showProgress: true,
                        primaryAction: BootstrapPrimaryAction.None,
                        primaryActionLabel: null,
                        secondaryAction: BootstrapSecondaryAction.None,
                        secondaryActionLabel: null);

                case BootstrapPreviewState.LoadingBootstrap:
                    return new BootstrapPanelStateConfiguration(
                        message: "Loading your learning experience...",
                        detail: "Just a moment.",
                        iconClass: null,
                        tone: BootstrapPanelTone.Loading,
                        normalizedProgress: 0.88f,
                        isLoadingState: true,
                        showProgress: true,
                        primaryAction: BootstrapPrimaryAction.None,
                        primaryActionLabel: null,
                        secondaryAction: BootstrapSecondaryAction.None,
                        secondaryActionLabel: null);

                case BootstrapPreviewState.OfflineEligible:
                    return new BootstrapPanelStateConfiguration(
                        message: "Downloaded content can be used on this device.",
                        detail: "New progress will sync after reconnecting.",
                        iconClass: "ds-icon--wifi",
                        tone: BootstrapPanelTone.Information,
                        normalizedProgress: 0.70f,
                        isLoadingState: false,
                        showProgress: true,
                        primaryAction: BootstrapPrimaryAction.Retry,
                        primaryActionLabel: "Retry Connection",
                        secondaryAction: BootstrapSecondaryAction.ContinueOffline,
                        secondaryActionLabel: "Continue Offline");

                case BootstrapPreviewState.AuthenticationRequired:
                    return new BootstrapPanelStateConfiguration(
                        message: "NutriMind needs your learner account.",
                        detail: "Use your LRN and PIN on the next screen.",
                        iconClass: "ds-icon--user",
                        tone: BootstrapPanelTone.Information,
                        normalizedProgress: 0.22f,
                        isLoadingState: false,
                        showProgress: true,
                        primaryAction: BootstrapPrimaryAction.OpenLogin,
                        primaryActionLabel: "Continue to Sign In",
                        secondaryAction: BootstrapSecondaryAction.None,
                        secondaryActionLabel: null);

                case BootstrapPreviewState.Maintenance:
                    return new BootstrapPanelStateConfiguration(
                        message: "Online services are temporarily unavailable.",
                        detail: "Please try again in a few minutes.",
                        iconClass: "ds-icon--clock",
                        tone: BootstrapPanelTone.Warning,
                        normalizedProgress: 0.38f,
                        isLoadingState: false,
                        showProgress: true,
                        primaryAction: BootstrapPrimaryAction.Retry,
                        primaryActionLabel: "Try Again",
                        secondaryAction: BootstrapSecondaryAction.None,
                        secondaryActionLabel: null);

                case BootstrapPreviewState.RequiredUpdate:
                    return new BootstrapPanelStateConfiguration(
                        message: "A newer version of NutriMind is needed.",
                        detail: "Update the application before continuing.",
                        iconClass: "ds-icon--refresh",
                        tone: BootstrapPanelTone.Warning,
                        normalizedProgress: 0.54f,
                        isLoadingState: false,
                        showProgress: true,
                        primaryAction: BootstrapPrimaryAction.UpdateApplication,
                        primaryActionLabel: "Update App",
                        secondaryAction: BootstrapSecondaryAction.None,
                        secondaryActionLabel: null);

                case BootstrapPreviewState.RecoverableError:
                    return new BootstrapPanelStateConfiguration(
                        message: "A temporary startup problem occurred.",
                        detail: "Please try again.",
                        iconClass: "ds-icon--error",
                        tone: BootstrapPanelTone.Error,
                        normalizedProgress: 0.50f,
                        isLoadingState: false,
                        showProgress: true,
                        primaryAction: BootstrapPrimaryAction.Retry,
                        primaryActionLabel: "Try Again",
                        secondaryAction: BootstrapSecondaryAction.None,
                        secondaryActionLabel: null);

                case BootstrapPreviewState.Ready:
                    // Ready is presentation-only here. Future production flow should
                    // auto-continue to AppShell when bootstrap completes at 100% —
                    // no manual Continue button on this state.
                    return new BootstrapPanelStateConfiguration(
                        message: "Your learning adventure is prepared.",
                        detail: "You're all set.",
                        iconClass: "ds-icon--check",
                        tone: BootstrapPanelTone.Success,
                        normalizedProgress: 1f,
                        isLoadingState: false,
                        showProgress: true,
                        primaryAction: BootstrapPrimaryAction.None,
                        primaryActionLabel: null,
                        secondaryAction: BootstrapSecondaryAction.None,
                        secondaryActionLabel: null);

                case BootstrapPreviewState.InitializingLocalStorage:
                default:
                    return new BootstrapPanelStateConfiguration(
                        message: "Preparing local learning data...",
                        detail: "Please wait.",
                        iconClass: null,
                        tone: BootstrapPanelTone.Loading,
                        normalizedProgress: 0.08f,
                        isLoadingState: true,
                        showProgress: true,
                        primaryAction: BootstrapPrimaryAction.None,
                        primaryActionLabel: null,
                        secondaryAction: BootstrapSecondaryAction.None,
                        secondaryActionLabel: null);
            }
        }

        private enum BootstrapPanelTone
        {
            Loading,
            Information,
            Success,
            Warning,
            Error
        }

        private enum BootstrapPrimaryAction
        {
            None,
            Retry,
            OpenLogin,
            UpdateApplication,
            ContinueToApplication
        }

        private enum BootstrapSecondaryAction
        {
            None,
            ContinueOffline
        }

        private readonly struct BootstrapPanelStateConfiguration
        {
            public BootstrapPanelStateConfiguration(
                string message,
                string detail,
                string iconClass,
                BootstrapPanelTone tone,
                float normalizedProgress,
                bool isLoadingState,
                bool showProgress,
                BootstrapPrimaryAction primaryAction,
                string primaryActionLabel,
                BootstrapSecondaryAction secondaryAction,
                string secondaryActionLabel)
            {
                Message = message;
                Detail = detail;
                IconClass = iconClass;
                Tone = tone;
                NormalizedProgress = normalizedProgress;
                IsLoadingState = isLoadingState;
                ShowProgress = showProgress;
                PrimaryAction = primaryAction;
                PrimaryActionLabel = primaryActionLabel;
                SecondaryAction = secondaryAction;
                SecondaryActionLabel = secondaryActionLabel;
            }

            public string Message { get; }
            public string Detail { get; }
            public string IconClass { get; }
            public BootstrapPanelTone Tone { get; }
            public float NormalizedProgress { get; }
            public bool IsLoadingState { get; }
            public bool ShowProgress { get; }
            public BootstrapPrimaryAction PrimaryAction { get; }
            public string PrimaryActionLabel { get; }
            public BootstrapSecondaryAction SecondaryAction { get; }
            public string SecondaryActionLabel { get; }
        }
    }
}
