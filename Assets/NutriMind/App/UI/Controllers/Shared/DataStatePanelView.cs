using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace NutriMind.App.UI
{
    /// <summary>
    /// Non-content presentation states for data-driven application screens.
    /// When <see cref="Content"/>, the panel hides so the owning screen can show normal content.
    /// </summary>
    public enum DataStatePanelState
    {
        Content,
        Loading,
        Empty,
        OfflineCached,
        OfflineUnavailable,
        RecoverableError,
        PermissionOrLocked
    }

    /// <summary>
    /// Immutable presentation configuration for <see cref="DataStatePanelView"/>.
    /// Does not include route, API, or domain models.
    /// </summary>
    public readonly struct DataStatePanelConfiguration
    {
        public DataStatePanelConfiguration(
            string title,
            string message,
            string detail = null,
            string iconClass = null,
            string primaryActionLabel = null,
            string secondaryActionLabel = null,
            bool? showSpinner = null)
        {
            Title = title;
            Message = message;
            Detail = detail;
            IconClass = iconClass;
            PrimaryActionLabel = primaryActionLabel;
            SecondaryActionLabel = secondaryActionLabel;
            ShowSpinner = showSpinner;
        }

        public string Title { get; }
        public string Message { get; }
        public string Detail { get; }
        public string IconClass { get; }
        public string PrimaryActionLabel { get; }
        public string SecondaryActionLabel { get; }
        public bool? ShowSpinner { get; }
    }

    /// <summary>
    /// Reusable UI Toolkit view for NutriMind non-content data states.
    /// Not a MonoBehaviour — construct with an already-instantiated component root,
    /// subscribe to action events from the owning screen controller, and call
    /// <see cref="Dispose"/> when the host unbinds.
    /// <para>
    /// Future screen usage (presentation wiring only):
    /// <code>
    /// TemplateContainer instance = template.CloneTree();
    /// contentHost.Add(instance);
    /// var stateView = new DataStatePanelView(instance);
    /// stateView.SetState(DataStatePanelState.Loading);
    /// // later:
    /// stateView.SetState(DataStatePanelState.RecoverableError);
    /// stateView.Configure(
    ///     title: "Quizzes could not be loaded",
    ///     message: "Check your connection and try again.",
    ///     detail: "No attempt was started.",
    ///     iconClass: "ds-icon--error",
    ///     primaryActionLabel: "Try Again",
    ///     secondaryActionLabel: "Return Home");
    /// </code>
    /// </para>
    /// Does not perform networking, SQLite, sync, routing, authentication, or permission evaluation.
    /// </summary>
    public sealed class DataStatePanelView : IDisposable
    {
        private const string RootName = "data-state-panel-root";
        private const string CompactClass = "data-state-panel--compact";
        private const string NarrowClass = "data-state-panel--narrow";
        private const string MobileClass = "mobile";
        private const string HiddenClass = "data-state-panel--hidden";
        private const string SpinnerHiddenClass = "data-state-panel__spinner--hidden";
        private const string IconHiddenClass = "data-state-panel__icon--hidden";
        private const string DetailHiddenClass = "data-state-panel__detail--hidden";
        private const string ActionsHiddenClass = "data-state-panel__actions--hidden";
        private const string ActionHiddenClass = "data-state-panel__action--hidden";
        private const float CompactBreakpoint = 1100f;
        private const float NarrowBreakpoint = 820f;

        private static readonly string[] StateModifierClasses =
        {
            "data-state-panel--loading",
            "data-state-panel--empty",
            "data-state-panel--offline-cached",
            "data-state-panel--offline-unavailable",
            "data-state-panel--recoverable-error",
            "data-state-panel--permission-locked",
            HiddenClass
        };

        private static readonly string[] SemanticIconClasses =
        {
            "ds-icon--info",
            "ds-icon--wifi",
            "ds-icon--error",
            "ds-icon--lock",
            "ds-icon--warning",
            "ds-icon--refresh",
            "ds-icon--search",
            "ds-icon--book"
        };

        private VisualElement _root;
        private VisualElement _iconBackground;
        private VisualElement _icon;
        private VisualElement _spinner;
        private Label _title;
        private Label _message;
        private Label _detail;
        private VisualElement _actions;
        private Button _primaryAction;
        private Button _secondaryAction;
        private bool _disposed;
        private float _lastWidth = -1f;

        /// <summary>
        /// Raised when the primary action button is clicked. Owning screens decide meaning.
        /// </summary>
        public event Action PrimaryActionRequested;

        /// <summary>
        /// Raised when the secondary action button is clicked. Owning screens decide meaning.
        /// </summary>
        public event Action SecondaryActionRequested;

        /// <summary>
        /// Creates a view bound to an already-instantiated component root,
        /// a TemplateContainer containing the root, or a local host that contains it.
        /// Does not search the entire application panel globally.
        /// </summary>
        public DataStatePanelView(VisualElement root)
        {
            ResolveRoot(root);
            if (_root == null)
            {
                return;
            }

            CacheElements();
            RegisterCallbacks();
            ApplyResponsiveClasses(_root.resolvedStyle.width);
        }

        public VisualElement Root => _root;

        public DataStatePanelState State { get; private set; } = DataStatePanelState.Empty;

        public bool IsBound => _root != null && !_disposed;

        /// <summary>
        /// Applies a state with its static defaults (copy, icon/spinner, actions visibility).
        /// Content hides the component so the owning screen can show normal content.
        /// </summary>
        public void SetState(DataStatePanelState state)
        {
            if (!IsBound)
            {
                return;
            }

            State = state;
            ClearStateModifierClasses();

            if (state == DataStatePanelState.Content)
            {
                _root.AddToClassList(HiddenClass);
                SetSpinnerVisible(false);
                SetIconVisible(false);
                ApplyActionLabels(null, null);
                return;
            }

            _root.RemoveFromClassList(HiddenClass);
            _root.AddToClassList(GetStateModifierClass(state));

            DataStatePanelConfiguration defaults = GetDefaultConfiguration(state);
            ApplyConfiguration(
                defaults,
                applySpinnerOverride: true,
                updateActions: true,
                updateDetail: true);
        }

        /// <summary>
        /// Overrides presentation copy, icon, and action labels for the current visible state.
        /// Null title/message/icon leave existing values. Null action labels leave existing actions
        /// unless at least one action label is non-null (then both are applied; empty hides).
        /// </summary>
        public void Configure(DataStatePanelConfiguration configuration)
        {
            if (!IsBound || State == DataStatePanelState.Content)
            {
                return;
            }

            bool updateActions = configuration.PrimaryActionLabel != null
                || configuration.SecondaryActionLabel != null;

            ApplyConfiguration(
                configuration,
                applySpinnerOverride: configuration.ShowSpinner.HasValue,
                updateActions: updateActions,
                updateDetail: configuration.Detail != null);
        }

        /// <summary>
        /// Overrides presentation copy, icon, and action labels for the current visible state.
        /// </summary>
        public void Configure(
            string title,
            string message,
            string detail = null,
            string iconClass = null,
            string primaryActionLabel = null,
            string secondaryActionLabel = null)
        {
            Configure(new DataStatePanelConfiguration(
                title,
                message,
                detail,
                iconClass,
                primaryActionLabel,
                secondaryActionLabel));
        }

        public void SetVisible(bool visible)
        {
            if (!IsBound)
            {
                return;
            }

            if (visible)
            {
                if (State == DataStatePanelState.Content)
                {
                    return;
                }

                _root.RemoveFromClassList(HiddenClass);
            }
            else
            {
                _root.AddToClassList(HiddenClass);
            }
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
            PrimaryActionRequested = null;
            SecondaryActionRequested = null;
            _root = null;
            _iconBackground = null;
            _icon = null;
            _spinner = null;
            _title = null;
            _message = null;
            _detail = null;
            _actions = null;
            _primaryAction = null;
            _secondaryAction = null;
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
            _iconBackground = _root.Q<VisualElement>("data-state-panel-icon-background");
            _icon = _root.Q<VisualElement>("data-state-panel-icon");
            _spinner = _root.Q<VisualElement>("data-state-panel-spinner");
            _title = _root.Q<Label>("data-state-panel-title");
            _message = _root.Q<Label>("data-state-panel-message");
            _detail = _root.Q<Label>("data-state-panel-detail");
            _actions = _root.Q<VisualElement>("data-state-panel-actions");
            _primaryAction = _root.Q<Button>("data-state-panel-primary-action");
            _secondaryAction = _root.Q<Button>("data-state-panel-secondary-action");
        }

        private void RegisterCallbacks()
        {
            if (_primaryAction != null)
            {
                _primaryAction.clicked += OnPrimaryClicked;
            }

            if (_secondaryAction != null)
            {
                _secondaryAction.clicked += OnSecondaryClicked;
            }

            _root.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        private void UnregisterCallbacks()
        {
            if (_primaryAction != null)
            {
                _primaryAction.clicked -= OnPrimaryClicked;
            }

            if (_secondaryAction != null)
            {
                _secondaryAction.clicked -= OnSecondaryClicked;
            }

            if (_root != null)
            {
                _root.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            }
        }

        private void OnPrimaryClicked()
        {
            PrimaryActionRequested?.Invoke();
        }

        private void OnSecondaryClicked()
        {
            SecondaryActionRequested?.Invoke();
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

        private void ApplyConfiguration(
            DataStatePanelConfiguration configuration,
            bool applySpinnerOverride,
            bool updateActions,
            bool updateDetail)
        {
            if (_title != null && configuration.Title != null)
            {
                _title.text = configuration.Title;
            }

            if (_message != null && configuration.Message != null)
            {
                _message.text = configuration.Message;
            }

            if (updateDetail)
            {
                ApplyDetail(configuration.Detail);
            }

            if (!string.IsNullOrWhiteSpace(configuration.IconClass))
            {
                SetSemanticIconClass(configuration.IconClass);
            }

            bool showSpinner = applySpinnerOverride && configuration.ShowSpinner.HasValue
                ? configuration.ShowSpinner.Value
                : State == DataStatePanelState.Loading;

            SetSpinnerVisible(showSpinner);
            SetIconVisible(!showSpinner);

            if (updateActions)
            {
                ApplyActionLabels(configuration.PrimaryActionLabel, configuration.SecondaryActionLabel);
            }
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

        private void ApplyActionLabels(string primaryLabel, string secondaryLabel)
        {
            bool showPrimary = !string.IsNullOrWhiteSpace(primaryLabel);
            bool showSecondary = !string.IsNullOrWhiteSpace(secondaryLabel);

            if (_primaryAction != null)
            {
                if (showPrimary)
                {
                    _primaryAction.text = primaryLabel.Trim();
                    _primaryAction.tooltip = primaryLabel.Trim();
                }

                _primaryAction.EnableInClassList(ActionHiddenClass, !showPrimary);
                _primaryAction.focusable = showPrimary;
                if (!showPrimary)
                {
                    _primaryAction.SetEnabled(true);
                }
            }

            if (_secondaryAction != null)
            {
                if (showSecondary)
                {
                    _secondaryAction.text = secondaryLabel.Trim();
                    _secondaryAction.tooltip = secondaryLabel.Trim();
                }

                _secondaryAction.EnableInClassList(ActionHiddenClass, !showSecondary);
                _secondaryAction.focusable = showSecondary;
                if (!showSecondary)
                {
                    _secondaryAction.SetEnabled(true);
                }
            }

            _actions?.EnableInClassList(ActionsHiddenClass, !showPrimary && !showSecondary);
        }

        private void SetSpinnerVisible(bool visible)
        {
            _spinner?.EnableInClassList(SpinnerHiddenClass, !visible);
        }

        private void SetIconVisible(bool visible)
        {
            _icon?.EnableInClassList(IconHiddenClass, !visible);
        }

        private void SetSemanticIconClass(string iconClass)
        {
            if (_icon == null || string.IsNullOrWhiteSpace(iconClass))
            {
                return;
            }

            string trimmed = iconClass.Trim();
            if (!IsAllowedSemanticIcon(trimmed))
            {
                Debug.LogWarning($"[DataStatePanelView] Ignored unsupported icon class '{trimmed}'.");
                return;
            }

            for (int i = 0; i < SemanticIconClasses.Length; i++)
            {
                _icon.RemoveFromClassList(SemanticIconClasses[i]);
            }

            _icon.AddToClassList(trimmed);
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

        private void ClearStateModifierClasses()
        {
            for (int i = 0; i < StateModifierClasses.Length; i++)
            {
                _root.RemoveFromClassList(StateModifierClasses[i]);
            }
        }

        private static string GetStateModifierClass(DataStatePanelState state)
        {
            switch (state)
            {
                case DataStatePanelState.Loading:
                    return "data-state-panel--loading";
                case DataStatePanelState.Empty:
                    return "data-state-panel--empty";
                case DataStatePanelState.OfflineCached:
                    return "data-state-panel--offline-cached";
                case DataStatePanelState.OfflineUnavailable:
                    return "data-state-panel--offline-unavailable";
                case DataStatePanelState.RecoverableError:
                    return "data-state-panel--recoverable-error";
                case DataStatePanelState.PermissionOrLocked:
                    return "data-state-panel--permission-locked";
                default:
                    return HiddenClass;
            }
        }

        private static DataStatePanelConfiguration GetDefaultConfiguration(DataStatePanelState state)
        {
            switch (state)
            {
                case DataStatePanelState.Loading:
                    return new DataStatePanelConfiguration(
                        title: "Loading...",
                        message: "Getting everything ready for you.",
                        detail: null,
                        iconClass: null,
                        primaryActionLabel: null,
                        secondaryActionLabel: null,
                        showSpinner: true);

                case DataStatePanelState.Empty:
                    return new DataStatePanelConfiguration(
                        title: "Nothing here yet",
                        message: "New content will appear here when it becomes available.",
                        detail: null,
                        iconClass: "ds-icon--info",
                        primaryActionLabel: "Refresh",
                        secondaryActionLabel: null,
                        showSpinner: false);

                case DataStatePanelState.OfflineCached:
                    return new DataStatePanelConfiguration(
                        title: "You are offline",
                        message: "Showing saved information from this device.",
                        detail: "Some updates may appear after you reconnect.",
                        iconClass: "ds-icon--wifi",
                        primaryActionLabel: "Continue Offline",
                        secondaryActionLabel: "Retry Connection",
                        showSpinner: false);

                case DataStatePanelState.OfflineUnavailable:
                    return new DataStatePanelConfiguration(
                        title: "Connection required",
                        message: "This information is not available offline on this device.",
                        detail: "Reconnect to load the latest content.",
                        iconClass: "ds-icon--wifi",
                        primaryActionLabel: "Retry",
                        secondaryActionLabel: "Go Back",
                        showSpinner: false);

                case DataStatePanelState.RecoverableError:
                    return new DataStatePanelConfiguration(
                        title: "Something went wrong",
                        message: "We could not load this information.",
                        detail: "Try again. Your existing progress is safe.",
                        iconClass: "ds-icon--error",
                        primaryActionLabel: "Try Again",
                        secondaryActionLabel: "Go Back",
                        showSpinner: false);

                case DataStatePanelState.PermissionOrLocked:
                    return new DataStatePanelConfiguration(
                        title: "This content is locked",
                        message: "You do not have access to this content yet.",
                        detail: "Complete the requirement or ask your Teacher when it will be released.",
                        iconClass: "ds-icon--lock",
                        primaryActionLabel: "Go Back",
                        secondaryActionLabel: null,
                        showSpinner: false);

                default:
                    return new DataStatePanelConfiguration(
                        title: string.Empty,
                        message: string.Empty,
                        showSpinner: false);
            }
        }
    }
}
