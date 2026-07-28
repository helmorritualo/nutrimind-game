using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace NutriMind.Gameplay.UI
{
    /// <summary>
    /// Presentation-only student gameplay HUD view for <c>GameplayStudentHud.uxml</c>.
    /// Raises movement, look, interaction, and pause intent events for a host to handle.
    /// Does not own mission state, player control, or persistence.
    /// </summary>
    public sealed class GameplayStudentHudView
    {
        private const string RootName = "gameplay-student-hud-root";
        private const string InputDisabledClass = "gameplay-student-hud__input-disabled";
        private const string LookHelperHiddenClass = "gameplay-student-hud__look-helper--hidden";
        private const string CompactClass = "gameplay-student-hud--compact";
        private const string NarrowClass = "gameplay-student-hud--narrow";
        private const string ShortClass = "gameplay-student-hud--short";
        private const string MobileClass = "mobile";
        private const float CompactBreakpoint = 1200f;
        private const float NarrowBreakpoint = 900f;
        private const float ShortBreakpoint = 650f;
        private const float JoystickRadius = 80f;
        private const float JoystickDeadZone = 8f;

        private VisualElement _root;
        private VisualElement _safeArea;
        private VisualElement _lookZone;
        private Label _lookHelper;
        private Label _areaPhaseLabel;
        private Label _missionTitleLabel;
        private Label _objectiveTextLabel;
        private Label _fragmentCountLabel;
        private VisualElement _interactionIcon;
        private Label _interactionLabel;
        private Button _interactionButton;
        private Button _pauseButton;
        private VisualElement _joystickKnob;
        private VirtualJoystickManipulator _joystickManipulator;
        private TouchLookManipulator _lookManipulator;
        private GameplaySafeAreaApplier _safeAreaApplier;
        private bool _disposed;
        private bool _inputEnabled = true;
        private bool _interactionAvailable;
        private bool _pauseAvailable = true;
        private float _lastWidth = -1f;
        private float _lastHeight = -1f;
        private string _currentInteractionIconClass = GameplayStudentHudViewModel.DefaultInteractionIconClass;

        public event Action<Vector2> MoveChanged;
        public event Action<Vector2> LookDeltaChanged;
        public event Action InteractionRequested;
        public event Action PauseRequested;

        public GameplayStudentHudView(VisualElement root)
        {
            ResolveRoot(root);
            if (_root == null)
            {
                Debug.LogWarning(
                    "[GameplayStudentHudView] Could not resolve gameplay-student-hud-root inside the supplied element.");
                return;
            }

            CacheElements();
            SetupInputManipulators();
            RegisterCallbacks();
            ResetTouchControls();
            ApplyResponsiveClasses(_root.resolvedStyle.width, _root.resolvedStyle.height);
            _safeAreaApplier?.ApplyIfChanged();
        }

        public VisualElement Root => _root;

        public bool IsBound => _root != null && !_disposed;

        public void SetViewModel(GameplayStudentHudViewModel model)
        {
            if (_disposed || _root == null || model == null)
            {
                return;
            }

            GameplayStudentHudViewModel sanitized = model.SanitizedCopy();
            SetObjective(sanitized.AreaPhaseLabel, sanitized.MissionTitle, sanitized.ObjectiveText);
            SetFragmentProgress(sanitized.CollectedFragments, sanitized.TotalFragments);
            SetInteraction(sanitized.InteractionLabel, sanitized.InteractionIconClass, sanitized.InteractionAvailable);
            SetLookHelperVisible(sanitized.ShowLookHelper);
            SetInputEnabled(sanitized.InputEnabled);

            if (_pauseButton != null)
            {
                _pauseAvailable = sanitized.PauseAvailable;
                _pauseButton.SetEnabled(sanitized.PauseAvailable);
                _pauseButton.EnableInClassList(
                    "gameplay-student-hud__pause-button--unavailable",
                    !sanitized.PauseAvailable);
            }
        }

        public void SetInputEnabled(bool enabled)
        {
            if (_disposed || _root == null)
            {
                return;
            }

            _inputEnabled = enabled;
            _root.EnableInClassList(InputDisabledClass, !enabled);

            if (_lookZone != null)
            {
                _lookZone.pickingMode = enabled ? PickingMode.Position : PickingMode.Ignore;
            }

            VisualElement joystick = _root.Q<VisualElement>("movement-joystick");
            if (joystick != null)
            {
                joystick.pickingMode = enabled ? PickingMode.Position : PickingMode.Ignore;
            }

            if (_joystickManipulator != null)
            {
                _joystickManipulator.InputEnabled = enabled;
            }

            if (_lookManipulator != null)
            {
                _lookManipulator.InputEnabled = enabled;
            }

            if (!enabled)
            {
                ResetTouchControls();
            }
        }

        public void SetObjective(string areaPhase, string missionTitle, string objective)
        {
            if (_disposed || _root == null)
            {
                return;
            }

            if (_areaPhaseLabel != null)
            {
                _areaPhaseLabel.text = areaPhase ?? string.Empty;
            }

            if (_missionTitleLabel != null)
            {
                _missionTitleLabel.text = missionTitle ?? string.Empty;
            }

            if (_objectiveTextLabel != null)
            {
                _objectiveTextLabel.text = objective ?? string.Empty;
            }
        }

        public void SetFragmentProgress(int collected, int total)
        {
            if (_disposed || _root == null || _fragmentCountLabel == null)
            {
                return;
            }

            int safeTotal = total < 0 ? 0 : total;
            int safeCollected = collected < 0 ? 0 : collected;
            if (safeTotal > 0 && safeCollected > safeTotal)
            {
                safeCollected = safeTotal;
            }

            _fragmentCountLabel.text = safeCollected + " / " + safeTotal;
        }

        public void SetInteraction(string label, string iconClass, bool available)
        {
            if (_disposed || _root == null)
            {
                return;
            }

            if (_interactionLabel != null)
            {
                _interactionLabel.text = label ?? string.Empty;
            }

            ApplyInteractionIconClass(iconClass);

            if (_interactionButton != null)
            {
                _interactionAvailable = available;
                _interactionButton.SetEnabled(available);
                _interactionButton.style.display = available ? DisplayStyle.Flex : DisplayStyle.Flex;
            }
        }

        public void SetLookHelperVisible(bool visible)
        {
            if (_disposed || _lookHelper == null)
            {
                return;
            }

            _lookHelper.EnableInClassList(LookHelperHiddenClass, !visible);
        }

        public void ResetTouchControls()
        {
            _joystickManipulator?.ResetJoystick();
            _lookManipulator?.ResetLook();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            UnregisterCallbacks();
            RemoveInputManipulators();

            MoveChanged = null;
            LookDeltaChanged = null;
            InteractionRequested = null;
            PauseRequested = null;

            _root = null;
            _safeArea = null;
            _lookZone = null;
            _lookHelper = null;
            _areaPhaseLabel = null;
            _missionTitleLabel = null;
            _objectiveTextLabel = null;
            _fragmentCountLabel = null;
            _interactionIcon = null;
            _interactionLabel = null;
            _interactionButton = null;
            _pauseButton = null;
            _joystickKnob = null;
            _safeAreaApplier = null;
            _lastWidth = -1f;
            _lastHeight = -1f;
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
            _safeArea = _root.Q<VisualElement>("gameplay-safe-area");
            _lookZone = _root.Q<VisualElement>("gameplay-look-zone");
            _lookHelper = _root.Q<Label>("look-helper");
            _areaPhaseLabel = _root.Q<Label>("area-phase-label");
            _missionTitleLabel = _root.Q<Label>("mission-title-label");
            _objectiveTextLabel = _root.Q<Label>("objective-text-label");
            _fragmentCountLabel = _root.Q<Label>("fragment-count-label");
            _interactionIcon = _root.Q<VisualElement>("interaction-icon");
            _interactionLabel = _root.Q<Label>("interaction-label");
            _interactionButton = _root.Q<Button>("interaction-button");
            _pauseButton = _root.Q<Button>("pause-button");
            _joystickKnob = _root.Q<VisualElement>("joystick-knob");

            if (_safeArea != null)
            {
                _safeAreaApplier = new GameplaySafeAreaApplier(_safeArea, _root);
            }
        }

        private void SetupInputManipulators()
        {
            VisualElement joystick = _root.Q<VisualElement>("movement-joystick");
            if (joystick != null && _joystickKnob != null)
            {
                _joystickManipulator = new VirtualJoystickManipulator(_joystickKnob, JoystickRadius, JoystickDeadZone);
                _joystickManipulator.MoveChanged += OnMoveChanged;
                joystick.AddManipulator(_joystickManipulator);
            }

            if (_lookZone != null)
            {
                _lookManipulator = new TouchLookManipulator();
                _lookManipulator.LookDeltaChanged += OnLookDeltaChanged;
                _lookZone.AddManipulator(_lookManipulator);
            }
        }

        private void RemoveInputManipulators()
        {
            VisualElement joystick = _root?.Q<VisualElement>("movement-joystick");
            if (_joystickManipulator != null)
            {
                _joystickManipulator.MoveChanged -= OnMoveChanged;
                joystick?.RemoveManipulator(_joystickManipulator);
                _joystickManipulator = null;
            }

            if (_lookManipulator != null)
            {
                _lookManipulator.LookDeltaChanged -= OnLookDeltaChanged;
                _lookZone?.RemoveManipulator(_lookManipulator);
                _lookManipulator = null;
            }
        }

        private void RegisterCallbacks()
        {
            _interactionButton?.RegisterCallback<ClickEvent>(OnInteractionClicked);
            _pauseButton?.RegisterCallback<ClickEvent>(OnPauseClicked);
            _root?.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        private void UnregisterCallbacks()
        {
            _interactionButton?.UnregisterCallback<ClickEvent>(OnInteractionClicked);
            _pauseButton?.UnregisterCallback<ClickEvent>(OnPauseClicked);
            _root?.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        private void OnInteractionClicked(ClickEvent evt)
        {
            RaiseInteractionRequested();
        }

        private void OnPauseClicked(ClickEvent evt)
        {
            RaisePauseRequested();
        }

        internal void RaiseInteractionRequested()
        {
            if (!_inputEnabled || !_interactionAvailable)
            {
                return;
            }

            InteractionRequested?.Invoke();
        }

        internal void RaisePauseRequested()
        {
            if (!_inputEnabled || !_pauseAvailable)
            {
                return;
            }

            PauseRequested?.Invoke();
        }

        private void OnMoveChanged(Vector2 move)
        {
            if (!_inputEnabled)
            {
                return;
            }

            MoveChanged?.Invoke(VirtualJoystickMath.ToGameplayMove(move));
        }

        private void OnLookDeltaChanged(Vector2 delta)
        {
            if (!_inputEnabled)
            {
                return;
            }

            LookDeltaChanged?.Invoke(delta);
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            ApplyResponsiveClasses(evt.newRect.width, evt.newRect.height);
            _safeAreaApplier?.ApplyIfChanged();
        }

        private void ApplyResponsiveClasses(float width, float height)
        {
            if (_root == null || float.IsNaN(width) || width <= 0f)
            {
                return;
            }

            if (Mathf.Approximately(width, _lastWidth) && Mathf.Approximately(height, _lastHeight))
            {
                return;
            }

            _lastWidth = width;
            _lastHeight = height;

            bool compact = width < CompactBreakpoint;
            bool narrow = width < NarrowBreakpoint;
            bool shortHeight = height > 0f && height < ShortBreakpoint;

            _root.EnableInClassList(CompactClass, compact);
            _root.EnableInClassList(NarrowClass, narrow);
            _root.EnableInClassList(ShortClass, shortHeight);
            _root.EnableInClassList(MobileClass, narrow);
        }

        private void ApplyInteractionIconClass(string iconClass)
        {
            if (_interactionIcon == null)
            {
                return;
            }

            string resolved = string.IsNullOrWhiteSpace(iconClass)
                ? GameplayStudentHudViewModel.DefaultInteractionIconClass
                : iconClass.Trim();

            if (!resolved.StartsWith("ds-icon--", StringComparison.Ordinal))
            {
                resolved = "ds-icon--" + resolved.TrimStart('-');
            }

            if (_currentInteractionIconClass == resolved)
            {
                return;
            }

            if (!string.IsNullOrEmpty(_currentInteractionIconClass))
            {
                _interactionIcon.RemoveFromClassList(_currentInteractionIconClass);
            }

            _currentInteractionIconClass = resolved;
            _interactionIcon.AddToClassList(_currentInteractionIconClass);
        }
    }
}
