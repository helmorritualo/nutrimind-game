using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace NutriMind.App.UI
{
    /// <summary>
    /// Visual tone of a system dialog. Does not imply production side effects.
    /// </summary>
    public enum SystemDialogTone
    {
        Information,
        Success,
        Warning,
        Error
    }

    /// <summary>
    /// Immutable presentation configuration for <see cref="SystemDialogView"/>.
    /// Does not include route objects, API payloads, server exceptions, scenes,
    /// learner records, quiz attempts, mission state, or HTTP status handling.
    /// </summary>
    public readonly struct SystemDialogConfiguration
    {
        public SystemDialogConfiguration(
            string title,
            string message,
            string primaryActionLabel,
            string secondaryActionLabel = null,
            string detail = null,
            string referenceText = null,
            string eyebrow = "NutriMind",
            string iconClass = null,
            SystemDialogTone tone = SystemDialogTone.Information,
            bool allowDismiss = false,
            bool dismissOnBackdrop = false,
            bool hideAfterPrimaryAction = true,
            bool hideAfterSecondaryAction = true)
        {
            Title = title;
            Message = message;
            PrimaryActionLabel = primaryActionLabel;
            SecondaryActionLabel = secondaryActionLabel;
            Detail = detail;
            ReferenceText = referenceText;
            Eyebrow = eyebrow;
            IconClass = iconClass;
            Tone = tone;
            AllowDismiss = allowDismiss;
            DismissOnBackdrop = dismissOnBackdrop;
            HideAfterPrimaryAction = hideAfterPrimaryAction;
            HideAfterSecondaryAction = hideAfterSecondaryAction;
        }

        public string Title { get; }
        public string Message { get; }
        public string Detail { get; }
        public string ReferenceText { get; }
        public string Eyebrow { get; }
        public string PrimaryActionLabel { get; }
        public string SecondaryActionLabel { get; }
        public string IconClass { get; }
        public SystemDialogTone Tone { get; }
        public bool AllowDismiss { get; }
        public bool DismissOnBackdrop { get; }
        public bool HideAfterPrimaryAction { get; }
        public bool HideAfterSecondaryAction { get; }
    }

    /// <summary>
    /// Static UI-preview copy presets for essential NutriMind system states.
    /// Presentation only — does not execute auth, networking, navigation, or save retry.
    /// </summary>
    public static class SystemDialogPresets
    {
        public static SystemDialogConfiguration SessionExpired()
        {
            return new SystemDialogConfiguration(
                title: "Your session has expired",
                message: "Sign in again to continue using NutriMind.",
                primaryActionLabel: "Sign In Again",
                secondaryActionLabel: null,
                detail: "Your downloaded learning content and saved local progress remain on this device.",
                referenceText: null,
                eyebrow: "Account",
                iconClass: "ds-icon--lock",
                tone: SystemDialogTone.Information,
                allowDismiss: false,
                dismissOnBackdrop: false,
                hideAfterPrimaryAction: true,
                hideAfterSecondaryAction: true);
        }

        public static SystemDialogConfiguration RequiredUpdate()
        {
            return new SystemDialogConfiguration(
                title: "Update required",
                message: "A newer version of NutriMind is required to continue.",
                primaryActionLabel: "Update App",
                secondaryActionLabel: null,
                detail: "Update the application, then open NutriMind again.",
                referenceText: "Your local learning progress remains on this device.",
                eyebrow: "Application Update",
                iconClass: "ds-icon--refresh",
                tone: SystemDialogTone.Warning,
                allowDismiss: false,
                dismissOnBackdrop: false,
                hideAfterPrimaryAction: false,
                hideAfterSecondaryAction: true);
        }

        public static SystemDialogConfiguration Maintenance()
        {
            return new SystemDialogConfiguration(
                title: "NutriMind is under maintenance",
                message: "Online services are temporarily unavailable.",
                primaryActionLabel: "Try Again",
                secondaryActionLabel: "Back to Login",
                detail: "Try again in a few minutes. Downloaded missions may still be available from the application.",
                referenceText: "Last checked: Just now",
                eyebrow: "Service Notice",
                iconClass: "ds-icon--clock",
                tone: SystemDialogTone.Warning,
                allowDismiss: false,
                dismissOnBackdrop: false,
                hideAfterPrimaryAction: false,
                hideAfterSecondaryAction: true);
        }

        public static SystemDialogConfiguration OfflineUnavailable()
        {
            return new SystemDialogConfiguration(
                title: "Internet connection required",
                message: "This content is not available offline on this device.",
                primaryActionLabel: "Retry",
                secondaryActionLabel: "Go Back",
                detail: "Reconnect to load the latest information.",
                referenceText: "Your existing downloaded progress is safe.",
                eyebrow: "Connection",
                iconClass: "ds-icon--wifi",
                tone: SystemDialogTone.Information,
                allowDismiss: true,
                dismissOnBackdrop: false,
                hideAfterPrimaryAction: false,
                hideAfterSecondaryAction: true);
        }

        public static SystemDialogConfiguration LocalSaveFailure()
        {
            return new SystemDialogConfiguration(
                title: "Progress could not be saved",
                message: "NutriMind could not save the latest change on this device.",
                primaryActionLabel: "Try Again",
                secondaryActionLabel: "Continue Without Saving",
                detail: "Try again before leaving this activity.",
                referenceText: "Reference: NM-SAVE-001",
                eyebrow: "Local Progress",
                iconClass: "ds-icon--error",
                tone: SystemDialogTone.Error,
                allowDismiss: false,
                dismissOnBackdrop: false,
                hideAfterPrimaryAction: false,
                hideAfterSecondaryAction: true);
        }

        public static SystemDialogConfiguration ContentValidationFailure()
        {
            return new SystemDialogConfiguration(
                title: "This mission content could not be opened",
                message: "NutriMind found missing or invalid mission information.",
                primaryActionLabel: "Back to Missions",
                secondaryActionLabel: null,
                detail: "Return to mission selection and choose another available mission.",
                referenceText: "Reference: NM-CONTENT-001",
                eyebrow: "Mission Content",
                iconClass: "ds-icon--error",
                tone: SystemDialogTone.Error,
                allowDismiss: false,
                dismissOnBackdrop: false,
                hideAfterPrimaryAction: true,
                hideAfterSecondaryAction: true);
        }

        public static SystemDialogConfiguration MissionUnavailable()
        {
            return new SystemDialogConfiguration(
                title: "This mission is unavailable",
                message: "The mission cannot be started right now.",
                primaryActionLabel: "Back to Missions",
                secondaryActionLabel: "Retry",
                detail: "It may be locked by your Teacher, waiting for a prerequisite, or unavailable on this device.",
                referenceText: "Check the mission details for the exact availability reason.",
                eyebrow: "Mission Availability",
                iconClass: "ds-icon--lock",
                tone: SystemDialogTone.Information,
                allowDismiss: true,
                dismissOnBackdrop: false,
                hideAfterPrimaryAction: true,
                hideAfterSecondaryAction: false);
        }

        public static SystemDialogConfiguration QuizExpired()
        {
            return new SystemDialogConfiguration(
                title: "This quiz has expired",
                message: "The quiz is no longer accepting attempts.",
                primaryActionLabel: "Return to Quiz Portal",
                secondaryActionLabel: null,
                detail: "Your Teacher may publish another quiz or extend availability later.",
                referenceText: "No new attempt was created.",
                eyebrow: "Quiz Portal",
                iconClass: "ds-icon--clock",
                tone: SystemDialogTone.Warning,
                allowDismiss: false,
                dismissOnBackdrop: false,
                hideAfterPrimaryAction: true,
                hideAfterSecondaryAction: true);
        }

        public static SystemDialogConfiguration QuizUnavailable()
        {
            return new SystemDialogConfiguration(
                title: "This quiz is unavailable",
                message: "You cannot start this quiz right now.",
                primaryActionLabel: "Return to Quiz Portal",
                secondaryActionLabel: "Retry",
                detail: "It may not be published to your class, or the attempt window may not be open.",
                referenceText: "No attempt was started.",
                eyebrow: "Quiz Portal",
                iconClass: "ds-icon--lock",
                tone: SystemDialogTone.Information,
                allowDismiss: true,
                dismissOnBackdrop: false,
                hideAfterPrimaryAction: true,
                hideAfterSecondaryAction: false);
        }

        public static SystemDialogConfiguration ServerValidationError()
        {
            return new SystemDialogConfiguration(
                title: "Some answers need attention",
                message: "The server could not accept the quiz in its current state.",
                primaryActionLabel: "Review Answers",
                secondaryActionLabel: "Return to Quiz Portal",
                detail: "Review the highlighted questions before trying again.",
                referenceText: "No result was recorded.",
                eyebrow: "Quiz Portal",
                iconClass: "ds-icon--warning",
                tone: SystemDialogTone.Error,
                allowDismiss: false,
                dismissOnBackdrop: false,
                hideAfterPrimaryAction: true,
                hideAfterSecondaryAction: true);
        }
    }

    /// <summary>
    /// Reusable UI Toolkit system dialog for App information, interruption,
    /// availability, and recoverable failure states.
    /// Not a MonoBehaviour — construct with an already-instantiated component root,
    /// subscribe to action / dismissal events from the owner, and call
    /// <see cref="Dispose"/> when the host unbinds.
    /// <para>
    /// Future AppShell modal usage (presentation wiring only):
    /// <code>
    /// VisualElement modalLayer = appShellController.GetModalLayer();
    /// TemplateContainer instance = systemDialogAsset.CloneTree();
    /// modalLayer.Add(instance);
    /// var systemDialog = new SystemDialogView(instance);
    /// systemDialog.PrimaryActionRequested += OnPrimaryAction;
    /// systemDialog.SecondaryActionRequested += OnSecondaryAction;
    /// systemDialog.Dismissed += OnDismissed;
    /// systemDialog.Show(SystemDialogPresets.Maintenance());
    /// // later:
    /// systemDialog.PrimaryActionRequested -= OnPrimaryAction;
    /// systemDialog.SecondaryActionRequested -= OnSecondaryAction;
    /// systemDialog.Dismissed -= OnDismissed;
    /// systemDialog.Dispose();
    /// instance.RemoveFromHierarchy();
    /// </code>
    /// </para>
    /// Owns its own <c>ds-backdrop</c>. Do not add a second AppShell backdrop for the same dialog.
    /// Does not perform networking, SQLite, sync, routing, authentication, quiz submit, or gameplay state changes.
    /// Use <see cref="ConfirmDialogView"/> for two-action confirmations (Confirm / Cancel).
    /// </summary>
    public sealed class SystemDialogView : IDisposable
    {
        private const string RootName = "system-dialog-root";

        private const string HiddenClass = "system-dialog--hidden";
        private const string InformationClass = "system-dialog--information";
        private const string SuccessClass = "system-dialog--success";
        private const string WarningClass = "system-dialog--warning";
        private const string ErrorClass = "system-dialog--error";

        private const string CompactClass = "system-dialog--compact";
        private const string NarrowClass = "system-dialog--narrow";
        private const string MobileClass = "mobile";
        private const string SingleActionClass = "system-dialog--single-action";

        private const string CloseHiddenClass = "system-dialog__close--hidden";
        private const string DetailHiddenClass = "system-dialog__detail--hidden";
        private const string ReferenceHiddenClass = "system-dialog__reference--hidden";
        private const string SecondaryHiddenClass = "system-dialog__secondary-action--hidden";

        private const float CompactBreakpoint = 1100f;
        private const float NarrowBreakpoint = 820f;

        private static readonly string[] ToneClasses =
        {
            InformationClass,
            SuccessClass,
            WarningClass,
            ErrorClass
        };

        private static readonly string[] SemanticIconClasses =
        {
            "ds-icon--info",
            "ds-icon--check",
            "ds-icon--warning",
            "ds-icon--error",
            "ds-icon--wifi",
            "ds-icon--lock",
            "ds-icon--refresh",
            "ds-icon--clock",
            "ds-icon--close"
        };

        private VisualElement _root;
        private VisualElement _backdrop;
        private VisualElement _card;
        private Label _eyebrow;
        private Label _title;
        private Button _closeButton;
        private VisualElement _iconBackground;
        private VisualElement _icon;
        private Label _message;
        private Label _detail;
        private VisualElement _reference;
        private Label _referenceLabel;
        private VisualElement _actions;
        private Button _secondaryActionButton;
        private Button _primaryActionButton;

        private SystemDialogConfiguration _configuration;
        private bool _isVisible;
        private bool _disposed;
        private bool _isDismissing;
        private bool _isInvokingPrimary;
        private bool _isInvokingSecondary;
        private float _lastWidth = -1f;

        /// <summary>
        /// Raised when the primary recovery / navigation action is activated.
        /// Owning screens decide meaning.
        /// </summary>
        public event Action PrimaryActionRequested;

        /// <summary>
        /// Raised when the optional secondary action is activated.
        /// Not cancellation — owning screens decide meaning.
        /// </summary>
        public event Action SecondaryActionRequested;

        /// <summary>
        /// Raised only by close button, Escape, or allowed backdrop dismissal.
        /// </summary>
        public event Action Dismissed;

        /// <summary>
        /// Raised when visibility changes. Argument is true when shown, false when hidden.
        /// </summary>
        public event Action<bool> VisibilityChanged;

        /// <summary>
        /// Creates a view bound to an already-instantiated component root,
        /// a TemplateContainer containing the root, or a local modal host that contains it.
        /// Does not search the entire application panel globally.
        /// </summary>
        public SystemDialogView(VisualElement root)
        {
            ResolveRoot(root);
            if (_root == null)
            {
                return;
            }

            CacheElements();
            RegisterCallbacks();
            ApplyResponsiveClasses(_root.resolvedStyle.width);
            SetHiddenInteractionState();
        }

        public VisualElement Root => _root;

        public bool IsBound => _root != null && !_disposed;

        public bool IsVisible => _isVisible && IsBound;

        public SystemDialogTone Tone => _configuration.Tone;

        public SystemDialogConfiguration Configuration => _configuration;

        /// <summary>
        /// Applies configuration and shows the dialog. Focus defaults to the primary action.
        /// </summary>
        public void Show(SystemDialogConfiguration configuration)
        {
            if (!IsBound)
            {
                return;
            }

            _configuration = configuration;
            ApplyTone(configuration.Tone);
            ApplyIcon(configuration.IconClass, configuration.Tone);
            ApplyEyebrow(configuration.Eyebrow);
            ApplyTitle(configuration.Title);
            ApplyMessage(configuration.Message);
            ApplyDetail(configuration.Detail);
            ApplyReferenceText(configuration.ReferenceText);
            SetPrimaryActionLabel(ResolvePrimaryActionLabel(configuration.PrimaryActionLabel));
            ApplySecondaryAction(configuration.SecondaryActionLabel);
            ApplyCloseButton(configuration.AllowDismiss);
            SetPrimaryActionEnabled(true);
            SetSecondaryActionEnabled(true);

            bool wasVisible = _isVisible;
            _root.RemoveFromClassList(HiddenClass);
            _root.pickingMode = PickingMode.Position;
            _isVisible = true;

            if (!wasVisible)
            {
                VisibilityChanged?.Invoke(true);
            }

            _root.schedule.Execute(FocusInitialControlSafely);
        }

        /// <summary>
        /// Hides the dialog without raising PrimaryActionRequested, SecondaryActionRequested, or Dismissed.
        /// </summary>
        public void Hide()
        {
            if (!IsBound || !_isVisible)
            {
                if (IsBound)
                {
                    SetHiddenInteractionState();
                }

                return;
            }

            SetHiddenInteractionState();
            _isVisible = false;
            VisibilityChanged?.Invoke(false);
        }

        /// <summary>
        /// Invokes <see cref="PrimaryActionRequested"/>. Hides afterward only when configured.
        /// </summary>
        public void RequestPrimaryAction()
        {
            if (!IsBound || !_isVisible || _isInvokingPrimary)
            {
                return;
            }

            if (_primaryActionButton != null && !_primaryActionButton.enabledSelf)
            {
                return;
            }

            _isInvokingPrimary = true;
            try
            {
                PrimaryActionRequested?.Invoke();
                if (_configuration.HideAfterPrimaryAction)
                {
                    Hide();
                }
            }
            finally
            {
                _isInvokingPrimary = false;
            }
        }

        /// <summary>
        /// Invokes <see cref="SecondaryActionRequested"/>. Hides afterward only when configured.
        /// No-op when the secondary action is absent or disabled.
        /// </summary>
        public void RequestSecondaryAction()
        {
            if (!IsBound || !_isVisible || _isInvokingSecondary)
            {
                return;
            }

            if (_secondaryActionButton == null
                || _secondaryActionButton.ClassListContains(SecondaryHiddenClass)
                || !_secondaryActionButton.enabledSelf)
            {
                return;
            }

            _isInvokingSecondary = true;
            try
            {
                SecondaryActionRequested?.Invoke();
                if (_configuration.HideAfterSecondaryAction)
                {
                    Hide();
                }
            }
            finally
            {
                _isInvokingSecondary = false;
            }
        }

        /// <summary>
        /// Invokes <see cref="Dismissed"/> then hides. No-op when dismissal is not allowed.
        /// </summary>
        public void Dismiss()
        {
            if (!IsBound || !_isVisible || _isDismissing)
            {
                return;
            }

            if (!_configuration.AllowDismiss)
            {
                return;
            }

            _isDismissing = true;
            try
            {
                Dismissed?.Invoke();
                Hide();
            }
            finally
            {
                _isDismissing = false;
            }
        }

        public void SetPrimaryActionEnabled(bool enabled)
        {
            _primaryActionButton?.SetEnabled(enabled);
        }

        public void SetSecondaryActionEnabled(bool enabled)
        {
            _secondaryActionButton?.SetEnabled(enabled);
        }

        public void SetPrimaryActionLabel(string label)
        {
            if (_primaryActionButton == null)
            {
                return;
            }

            string resolved = ResolvePrimaryActionLabel(label);
            _primaryActionButton.text = resolved;
            _primaryActionButton.tooltip = resolved;
        }

        public void SetSecondaryActionLabel(string label)
        {
            ApplySecondaryAction(label);
        }

        public void SetMessage(string message)
        {
            ApplyMessage(message);
        }

        public void SetDetail(string detail)
        {
            ApplyDetail(detail);
        }

        public void SetReferenceText(string referenceText)
        {
            ApplyReferenceText(referenceText);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            UnregisterCallbacks();
            PrimaryActionRequested = null;
            SecondaryActionRequested = null;
            Dismissed = null;
            VisibilityChanged = null;
            _root = null;
            _backdrop = null;
            _card = null;
            _eyebrow = null;
            _title = null;
            _closeButton = null;
            _iconBackground = null;
            _icon = null;
            _message = null;
            _detail = null;
            _reference = null;
            _referenceLabel = null;
            _actions = null;
            _secondaryActionButton = null;
            _primaryActionButton = null;
            _isVisible = false;
            _lastWidth = -1f;
            _isDismissing = false;
            _isInvokingPrimary = false;
            _isInvokingSecondary = false;
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
            _backdrop = _root.Q<VisualElement>("system-dialog-backdrop");
            _card = _root.Q<VisualElement>("system-dialog-card");
            _eyebrow = _root.Q<Label>("system-dialog-eyebrow");
            _title = _root.Q<Label>("system-dialog-title");
            _closeButton = _root.Q<Button>("system-dialog-close");
            _iconBackground = _root.Q<VisualElement>("system-dialog-icon-background");
            _icon = _root.Q<VisualElement>("system-dialog-icon");
            _message = _root.Q<Label>("system-dialog-message");
            _detail = _root.Q<Label>("system-dialog-detail");
            _reference = _root.Q<VisualElement>("system-dialog-reference");
            _referenceLabel = _root.Q<Label>("system-dialog-reference-label");
            _actions = _root.Q<VisualElement>("system-dialog-actions");
            _secondaryActionButton = _root.Q<Button>("system-dialog-secondary-action");
            _primaryActionButton = _root.Q<Button>("system-dialog-primary-action");
        }

        private void RegisterCallbacks()
        {
            if (_primaryActionButton != null)
            {
                _primaryActionButton.clicked += OnPrimaryActionClicked;
            }

            if (_secondaryActionButton != null)
            {
                _secondaryActionButton.clicked += OnSecondaryActionClicked;
            }

            if (_closeButton != null)
            {
                _closeButton.clicked += OnCloseClicked;
            }

            if (_backdrop != null)
            {
                _backdrop.RegisterCallback<ClickEvent>(OnBackdropClicked);
            }

            _root.RegisterCallback<KeyDownEvent>(OnRootKeyDown);
            _root.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        private void UnregisterCallbacks()
        {
            if (_primaryActionButton != null)
            {
                _primaryActionButton.clicked -= OnPrimaryActionClicked;
            }

            if (_secondaryActionButton != null)
            {
                _secondaryActionButton.clicked -= OnSecondaryActionClicked;
            }

            if (_closeButton != null)
            {
                _closeButton.clicked -= OnCloseClicked;
            }

            if (_backdrop != null)
            {
                _backdrop.UnregisterCallback<ClickEvent>(OnBackdropClicked);
            }

            if (_root != null)
            {
                _root.UnregisterCallback<KeyDownEvent>(OnRootKeyDown);
                _root.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            }
        }

        private void OnPrimaryActionClicked()
        {
            RequestPrimaryAction();
        }

        private void OnSecondaryActionClicked()
        {
            RequestSecondaryAction();
        }

        private void OnCloseClicked()
        {
            Dismiss();
        }

        private void OnBackdropClicked(ClickEvent evt)
        {
            if (!_isVisible || !_configuration.AllowDismiss || !_configuration.DismissOnBackdrop)
            {
                return;
            }

            if (evt.target != _backdrop)
            {
                return;
            }

            evt.StopPropagation();
            Dismiss();
        }

        private void OnRootKeyDown(KeyDownEvent evt)
        {
            if (!_isVisible)
            {
                return;
            }

            if (evt.keyCode != KeyCode.Escape)
            {
                return;
            }

            if (!_configuration.AllowDismiss)
            {
                return;
            }

            evt.StopPropagation();
            Dismiss();
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            ApplyResponsiveClasses(evt.newRect.width);
        }

        private void FocusInitialControlSafely()
        {
            if (!IsBound || !_isVisible)
            {
                return;
            }

            if (_primaryActionButton != null
                && _primaryActionButton.enabledSelf
                && _primaryActionButton.focusable)
            {
                _primaryActionButton.Focus();
                return;
            }

            if (_secondaryActionButton != null
                && !_secondaryActionButton.ClassListContains(SecondaryHiddenClass)
                && _secondaryActionButton.enabledSelf
                && _secondaryActionButton.focusable)
            {
                _secondaryActionButton.Focus();
                return;
            }

            if (_closeButton != null
                && !_closeButton.ClassListContains(CloseHiddenClass)
                && _closeButton.focusable)
            {
                _closeButton.Focus();
            }
        }

        private void SetHiddenInteractionState()
        {
            if (_root == null)
            {
                return;
            }

            _root.AddToClassList(HiddenClass);
            _root.pickingMode = PickingMode.Ignore;
        }

        private void ApplyTone(SystemDialogTone tone)
        {
            for (int i = 0; i < ToneClasses.Length; i++)
            {
                _root.RemoveFromClassList(ToneClasses[i]);
            }

            switch (tone)
            {
                case SystemDialogTone.Success:
                    _root.AddToClassList(SuccessClass);
                    break;
                case SystemDialogTone.Warning:
                    _root.AddToClassList(WarningClass);
                    break;
                case SystemDialogTone.Error:
                    _root.AddToClassList(ErrorClass);
                    break;
                default:
                    _root.AddToClassList(InformationClass);
                    break;
            }
        }

        private void ApplyIcon(string iconClass, SystemDialogTone tone)
        {
            if (_icon == null)
            {
                return;
            }

            string resolved = ResolveIconClass(iconClass, tone);
            for (int i = 0; i < SemanticIconClasses.Length; i++)
            {
                _icon.RemoveFromClassList(SemanticIconClasses[i]);
            }

            _icon.AddToClassList(resolved);
        }

        private string ResolveIconClass(string iconClass, SystemDialogTone tone)
        {
            if (!string.IsNullOrWhiteSpace(iconClass))
            {
                string trimmed = iconClass.Trim();
                if (IsAllowedSemanticIcon(trimmed))
                {
                    return trimmed;
                }

                Debug.LogWarning(
                    $"[SystemDialogView] Ignored unsupported icon class '{trimmed}'. Falling back to tone default.");
            }

            return GetDefaultIconClass(tone);
        }

        private static string GetDefaultIconClass(SystemDialogTone tone)
        {
            switch (tone)
            {
                case SystemDialogTone.Success:
                    return "ds-icon--check";
                case SystemDialogTone.Warning:
                    return "ds-icon--warning";
                case SystemDialogTone.Error:
                    return "ds-icon--error";
                default:
                    return "ds-icon--info";
            }
        }

        private static bool IsAllowedSemanticIcon(string iconClass)
        {
            for (int i = 0; i < SemanticIconClasses.Length; i++)
            {
                if (SemanticIconClasses[i] == iconClass)
                {
                    return true;
                }
            }

            return false;
        }

        private void ApplyEyebrow(string eyebrow)
        {
            if (_eyebrow == null)
            {
                return;
            }

            _eyebrow.text = string.IsNullOrWhiteSpace(eyebrow) ? "NutriMind" : eyebrow.Trim();
        }

        private void ApplyTitle(string title)
        {
            if (_title == null)
            {
                return;
            }

            _title.text = string.IsNullOrWhiteSpace(title) ? "System message" : title.Trim();
        }

        private void ApplyMessage(string message)
        {
            if (_message == null)
            {
                return;
            }

            _message.text = string.IsNullOrWhiteSpace(message)
                ? "Important information from NutriMind."
                : message.Trim();
        }

        private void ApplyDetail(string detail)
        {
            if (_detail == null)
            {
                return;
            }

            bool hasDetail = !string.IsNullOrWhiteSpace(detail);
            _detail.text = hasDetail ? detail.Trim() : string.Empty;
            _detail.EnableInClassList(DetailHiddenClass, !hasDetail);
        }

        private void ApplyReferenceText(string referenceText)
        {
            if (_reference == null)
            {
                return;
            }

            bool hasReference = !string.IsNullOrWhiteSpace(referenceText);
            if (_referenceLabel != null)
            {
                _referenceLabel.text = hasReference ? referenceText.Trim() : string.Empty;
            }

            _reference.EnableInClassList(ReferenceHiddenClass, !hasReference);
        }

        private void ApplySecondaryAction(string label)
        {
            bool hasSecondary = !string.IsNullOrWhiteSpace(label);

            if (_secondaryActionButton != null)
            {
                if (hasSecondary)
                {
                    string resolved = label.Trim();
                    _secondaryActionButton.text = resolved;
                    _secondaryActionButton.tooltip = resolved;
                    _secondaryActionButton.RemoveFromClassList(SecondaryHiddenClass);
                    _secondaryActionButton.focusable = true;
                }
                else
                {
                    _secondaryActionButton.text = string.Empty;
                    _secondaryActionButton.tooltip = string.Empty;
                    _secondaryActionButton.AddToClassList(SecondaryHiddenClass);
                    _secondaryActionButton.focusable = false;
                }
            }

            _root?.EnableInClassList(SingleActionClass, !hasSecondary);
        }

        private void ApplyCloseButton(bool allowDismiss)
        {
            if (_closeButton == null)
            {
                return;
            }

            if (allowDismiss)
            {
                _closeButton.RemoveFromClassList(CloseHiddenClass);
                _closeButton.focusable = true;
                _closeButton.tooltip = "Close";
            }
            else
            {
                _closeButton.AddToClassList(CloseHiddenClass);
                _closeButton.focusable = false;
            }
        }

        private static string ResolvePrimaryActionLabel(string label)
        {
            if (string.IsNullOrWhiteSpace(label))
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning("[SystemDialogView] Empty primary action label; falling back to 'OK'.");
#endif
                return "OK";
            }

            return label.Trim();
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
