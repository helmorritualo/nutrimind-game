using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace NutriMind.App.UI
{
    /// <summary>
    /// Visual severity of a confirmation. Does not imply production side effects.
    /// </summary>
    public enum ConfirmDialogTone
    {
        Neutral,
        Warning,
        Danger
    }

    /// <summary>
    /// Immutable presentation configuration for <see cref="ConfirmDialogView"/>.
    /// Does not include route names, API models, quiz payloads, or gameplay state.
    /// </summary>
    public readonly struct ConfirmDialogConfiguration
    {
        public ConfirmDialogConfiguration(
            string title,
            string message,
            string confirmLabel,
            string cancelLabel = "Cancel",
            string detail = null,
            string iconClass = null,
            ConfirmDialogTone tone = ConfirmDialogTone.Neutral,
            bool dismissOnBackdrop = false)
        {
            Title = title;
            Message = message;
            ConfirmLabel = confirmLabel;
            CancelLabel = cancelLabel;
            Detail = detail;
            IconClass = iconClass;
            Tone = tone;
            DismissOnBackdrop = dismissOnBackdrop;
        }

        public string Title { get; }
        public string Message { get; }
        public string Detail { get; }
        public string ConfirmLabel { get; }
        public string CancelLabel { get; }
        public string IconClass { get; }
        public ConfirmDialogTone Tone { get; }
        public bool DismissOnBackdrop { get; }
    }

    /// <summary>
    /// Static UI-preview copy presets for common NutriMind confirmations.
    /// Presentation only — does not execute quiz, auth, settings, or gameplay actions.
    /// </summary>
    public static class ConfirmDialogPresets
    {
        public static ConfirmDialogConfiguration SubmitQuiz()
        {
            return new ConfirmDialogConfiguration(
                title: "Submit your quiz?",
                message: "You will not be able to change your answers after submission.",
                confirmLabel: "Submit Quiz",
                cancelLabel: "Keep Reviewing",
                detail: "Review any marked questions before continuing.",
                iconClass: "ds-icon--check",
                tone: ConfirmDialogTone.Neutral,
                dismissOnBackdrop: false);
        }

        public static ConfirmDialogConfiguration ExitQuiz()
        {
            return new ConfirmDialogConfiguration(
                title: "Leave this quiz?",
                message: "Your current answers may not be submitted.",
                confirmLabel: "Leave Quiz",
                cancelLabel: "Stay",
                detail: "Return to the quiz before it closes to continue when recovery is supported.",
                iconClass: "ds-icon--warning",
                tone: ConfirmDialogTone.Warning,
                dismissOnBackdrop: false);
        }

        public static ConfirmDialogConfiguration SignOut()
        {
            return new ConfirmDialogConfiguration(
                title: "Sign out?",
                message: "You will return to the NutriMind login screen.",
                confirmLabel: "Sign Out",
                cancelLabel: "Stay Signed In",
                detail: "Downloaded learning content stays on this device unless removed separately.",
                iconClass: "ds-icon--error",
                tone: ConfirmDialogTone.Danger,
                dismissOnBackdrop: false);
        }

        public static ConfirmDialogConfiguration RestoreDefaults()
        {
            return new ConfirmDialogConfiguration(
                title: "Restore default settings?",
                message: "Audio, display, accessibility, and input settings will return to their defaults.",
                confirmLabel: "Restore Defaults",
                cancelLabel: "Keep Settings",
                detail: "You can adjust them again at any time.",
                iconClass: "ds-icon--refresh",
                tone: ConfirmDialogTone.Warning,
                dismissOnBackdrop: false);
        }

        public static ConfirmDialogConfiguration ResetTutorial()
        {
            return new ConfirmDialogConfiguration(
                title: "Reset the tutorial?",
                message: "The Getting Started guide will appear again the next time you open a mission.",
                confirmLabel: "Reset Tutorial",
                cancelLabel: "Keep Current Setup",
                detail: "Your mission progress will not be removed.",
                iconClass: "ds-icon--refresh",
                tone: ConfirmDialogTone.Warning,
                dismissOnBackdrop: false);
        }

        public static ConfirmDialogConfiguration LeaveMission()
        {
            return new ConfirmDialogConfiguration(
                title: "Leave this mission?",
                message: "You will return to mission selection.",
                confirmLabel: "Leave Mission",
                cancelLabel: "Keep Playing",
                detail: "Completed checkpoints remain saved when the progress system is connected.",
                iconClass: "ds-icon--warning",
                tone: ConfirmDialogTone.Warning,
                dismissOnBackdrop: false);
        }

        public static ConfirmDialogConfiguration RestartCheckpoint()
        {
            return new ConfirmDialogConfiguration(
                title: "Restart from the checkpoint?",
                message: "Progress after your latest checkpoint will be reset.",
                confirmLabel: "Restart Checkpoint",
                cancelLabel: "Keep Playing",
                detail: "Completed earlier areas remain unchanged.",
                iconClass: "ds-icon--refresh",
                tone: ConfirmDialogTone.Warning,
                dismissOnBackdrop: false);
        }
    }

    /// <summary>
    /// Reusable UI Toolkit confirmation dialog for App and gameplay two-action prompts.
    /// Not a MonoBehaviour — construct with an already-instantiated component root,
    /// subscribe to <see cref="Confirmed"/> / <see cref="Cancelled"/> from the owner,
    /// and call <see cref="Dispose"/> when the host unbinds.
    /// <para>
    /// Future AppShell modal usage (presentation wiring only):
    /// <code>
    /// VisualElement modalLayer = appShellController.GetModalLayer();
    /// TemplateContainer instance = confirmDialogAsset.CloneTree();
    /// modalLayer.Add(instance);
    /// var confirmDialog = new ConfirmDialogView(instance);
    /// confirmDialog.Confirmed += OnConfirmed;
    /// confirmDialog.Cancelled += OnCancelled;
    /// confirmDialog.Show(ConfirmDialogPresets.SubmitQuiz());
    /// // later:
    /// confirmDialog.Confirmed -= OnConfirmed;
    /// confirmDialog.Cancelled -= OnCancelled;
    /// confirmDialog.Dispose();
    /// instance.RemoveFromHierarchy();
    /// </code>
    /// </para>
    /// Owns its own <c>ds-backdrop</c>. Do not add a second AppShell backdrop for the same dialog.
    /// Does not perform networking, SQLite, sync, routing, authentication, quiz submit, or gameplay state changes.
    /// </summary>
    public sealed class ConfirmDialogView : IDisposable
    {
        private const string RootName = "confirm-dialog-root";
        private const string HiddenClass = "confirm-dialog--hidden";
        private const string NeutralClass = "confirm-dialog--neutral";
        private const string WarningClass = "confirm-dialog--warning";
        private const string DangerClass = "confirm-dialog--danger";
        private const string CompactClass = "confirm-dialog--compact";
        private const string NarrowClass = "confirm-dialog--narrow";
        private const string MobileClass = "mobile";
        private const string DetailHiddenClass = "confirm-dialog__detail--hidden";
        private const string PrimaryButtonClass = "ds-btn--primary";
        private const string DangerButtonClass = "ds-btn--danger";
        private const float CompactBreakpoint = 1100f;
        private const float NarrowBreakpoint = 820f;

        private static readonly string[] ToneClasses =
        {
            NeutralClass,
            WarningClass,
            DangerClass
        };

        private static readonly string[] ConfirmButtonVariantClasses =
        {
            PrimaryButtonClass,
            DangerButtonClass
        };

        private static readonly string[] SemanticIconClasses =
        {
            "ds-icon--help",
            "ds-icon--check",
            "ds-icon--warning",
            "ds-icon--error",
            "ds-icon--info",
            "ds-icon--lock",
            "ds-icon--refresh",
            "ds-icon--close",
            "ds-icon--gift"
        };

        private VisualElement _root;
        private VisualElement _backdrop;
        private VisualElement _card;
        private VisualElement _iconBackground;
        private VisualElement _icon;
        private Label _title;
        private Label _message;
        private Label _detail;
        private Button _cancelButton;
        private Button _confirmButton;
        private ConfirmDialogConfiguration _configuration;
        private bool _isVisible;
        private bool _disposed;
        private float _lastWidth = -1f;
        private bool _isCancelling;

        /// <summary>
        /// Raised when Confirm is activated. Owning screens decide meaning.
        /// </summary>
        public event Action Confirmed;

        /// <summary>
        /// Raised when Cancel, Escape, or allowed backdrop dismissal occurs.
        /// Owning screens decide meaning.
        /// </summary>
        public event Action Cancelled;

        /// <summary>
        /// Raised when visibility changes. Argument is true when shown, false when hidden.
        /// </summary>
        public event Action<bool> VisibilityChanged;

        /// <summary>
        /// Creates a view bound to an already-instantiated component root,
        /// a TemplateContainer containing the root, or a local modal host that contains it.
        /// Does not search the entire application panel globally.
        /// </summary>
        public ConfirmDialogView(VisualElement root)
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

        public ConfirmDialogTone Tone => _configuration.Tone;

        /// <summary>
        /// Applies configuration and shows the dialog. Focus defaults to Cancel.
        /// </summary>
        public void Show(ConfirmDialogConfiguration configuration)
        {
            if (!IsBound)
            {
                return;
            }

            _configuration = configuration;
            ApplyTone(configuration.Tone);
            ApplyIcon(configuration.IconClass, configuration.Tone);
            ApplyTitle(configuration.Title);
            ApplyMessage(configuration.Message);
            ApplyDetail(configuration.Detail);
            SetConfirmLabel(ResolveConfirmLabel(configuration.ConfirmLabel));
            SetCancelLabel(ResolveCancelLabel(configuration.CancelLabel));
            SetConfirmEnabled(true);
            SetCancelEnabled(true);

            bool wasVisible = _isVisible;
            _root.RemoveFromClassList(HiddenClass);
            _root.pickingMode = PickingMode.Position;
            _isVisible = true;

            if (!wasVisible)
            {
                VisibilityChanged?.Invoke(true);
            }

            _root.schedule.Execute(FocusCancelSafely);
        }

        /// <summary>
        /// Hides the dialog without raising Confirmed or Cancelled.
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
        /// Invokes <see cref="Confirmed"/> then hides. No-op when hidden or Confirm disabled.
        /// </summary>
        public void Confirm()
        {
            if (!IsBound || !_isVisible)
            {
                return;
            }

            if (_confirmButton != null && !_confirmButton.enabledSelf)
            {
                return;
            }

            Confirmed?.Invoke();
            Hide();
        }

        /// <summary>
        /// Invokes <see cref="Cancelled"/> then hides. No-op when hidden or Cancel disabled.
        /// </summary>
        public void Cancel()
        {
            if (!IsBound || !_isVisible || _isCancelling)
            {
                return;
            }

            if (_cancelButton != null && !_cancelButton.enabledSelf)
            {
                return;
            }

            _isCancelling = true;
            try
            {
                Cancelled?.Invoke();
                Hide();
            }
            finally
            {
                _isCancelling = false;
            }
        }

        public void SetConfirmEnabled(bool enabled)
        {
            _confirmButton?.SetEnabled(enabled);
        }

        public void SetCancelEnabled(bool enabled)
        {
            _cancelButton?.SetEnabled(enabled);
        }

        public void SetConfirmLabel(string label)
        {
            if (_confirmButton == null)
            {
                return;
            }

            string resolved = ResolveConfirmLabel(label);
            _confirmButton.text = resolved;
            _confirmButton.tooltip = resolved;
        }

        public void SetCancelLabel(string label)
        {
            if (_cancelButton == null)
            {
                return;
            }

            string resolved = ResolveCancelLabel(label);
            _cancelButton.text = resolved;
            _cancelButton.tooltip = resolved;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            UnregisterCallbacks();
            Confirmed = null;
            Cancelled = null;
            VisibilityChanged = null;
            _root = null;
            _backdrop = null;
            _card = null;
            _iconBackground = null;
            _icon = null;
            _title = null;
            _message = null;
            _detail = null;
            _cancelButton = null;
            _confirmButton = null;
            _isVisible = false;
            _lastWidth = -1f;
            _isCancelling = false;
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
            _backdrop = _root.Q<VisualElement>("confirm-dialog-backdrop");
            _card = _root.Q<VisualElement>("confirm-dialog-card");
            _iconBackground = _root.Q<VisualElement>("confirm-dialog-icon-background");
            _icon = _root.Q<VisualElement>("confirm-dialog-icon");
            _title = _root.Q<Label>("confirm-dialog-title");
            _message = _root.Q<Label>("confirm-dialog-message");
            _detail = _root.Q<Label>("confirm-dialog-detail");
            _cancelButton = _root.Q<Button>("confirm-dialog-cancel");
            _confirmButton = _root.Q<Button>("confirm-dialog-confirm");
        }

        private void RegisterCallbacks()
        {
            if (_confirmButton != null)
            {
                _confirmButton.clicked += OnConfirmClicked;
            }

            if (_cancelButton != null)
            {
                _cancelButton.clicked += OnCancelClicked;
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
            if (_confirmButton != null)
            {
                _confirmButton.clicked -= OnConfirmClicked;
            }

            if (_cancelButton != null)
            {
                _cancelButton.clicked -= OnCancelClicked;
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

        private void OnConfirmClicked()
        {
            Confirm();
        }

        private void OnCancelClicked()
        {
            Cancel();
        }

        private void OnBackdropClicked(ClickEvent evt)
        {
            if (!_isVisible || !_configuration.DismissOnBackdrop)
            {
                return;
            }

            if (evt.target != _backdrop)
            {
                return;
            }

            evt.StopPropagation();
            Cancel();
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

            evt.StopPropagation();
            Cancel();
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            ApplyResponsiveClasses(evt.newRect.width);
        }

        private void FocusCancelSafely()
        {
            if (!IsBound || !_isVisible || _cancelButton == null)
            {
                return;
            }

            _cancelButton.Focus();
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

        private void ApplyTone(ConfirmDialogTone tone)
        {
            for (int i = 0; i < ToneClasses.Length; i++)
            {
                _root.RemoveFromClassList(ToneClasses[i]);
            }

            switch (tone)
            {
                case ConfirmDialogTone.Warning:
                    _root.AddToClassList(WarningClass);
                    ApplyConfirmButtonVariant(PrimaryButtonClass);
                    break;
                case ConfirmDialogTone.Danger:
                    _root.AddToClassList(DangerClass);
                    ApplyConfirmButtonVariant(DangerButtonClass);
                    break;
                default:
                    _root.AddToClassList(NeutralClass);
                    ApplyConfirmButtonVariant(PrimaryButtonClass);
                    break;
            }
        }

        private void ApplyConfirmButtonVariant(string variantClass)
        {
            if (_confirmButton == null)
            {
                return;
            }

            for (int i = 0; i < ConfirmButtonVariantClasses.Length; i++)
            {
                _confirmButton.RemoveFromClassList(ConfirmButtonVariantClasses[i]);
            }

            _confirmButton.AddToClassList(variantClass);
        }

        private void ApplyIcon(string iconClass, ConfirmDialogTone tone)
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

        private string ResolveIconClass(string iconClass, ConfirmDialogTone tone)
        {
            if (!string.IsNullOrWhiteSpace(iconClass))
            {
                string trimmed = iconClass.Trim();
                if (IsAllowedSemanticIcon(trimmed))
                {
                    return trimmed;
                }

                Debug.LogWarning(
                    $"[ConfirmDialogView] Ignored unsupported icon class '{trimmed}'. Falling back to tone default.");
            }

            return GetDefaultIconClass(tone);
        }

        private static string GetDefaultIconClass(ConfirmDialogTone tone)
        {
            switch (tone)
            {
                case ConfirmDialogTone.Warning:
                    return "ds-icon--warning";
                case ConfirmDialogTone.Danger:
                    return "ds-icon--error";
                default:
                    return "ds-icon--help";
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

        private void ApplyTitle(string title)
        {
            if (_title == null)
            {
                return;
            }

            _title.text = string.IsNullOrWhiteSpace(title) ? "Are you sure?" : title.Trim();
        }

        private void ApplyMessage(string message)
        {
            if (_message == null)
            {
                return;
            }

            _message.text = string.IsNullOrWhiteSpace(message)
                ? "Please confirm before continuing."
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

        private static string ResolveConfirmLabel(string label)
        {
            if (string.IsNullOrWhiteSpace(label))
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning("[ConfirmDialogView] Empty confirm label; falling back to 'Confirm'.");
#endif
                return "Confirm";
            }

            return label.Trim();
        }

        private static string ResolveCancelLabel(string label)
        {
            if (string.IsNullOrWhiteSpace(label))
            {
                return "Cancel";
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
