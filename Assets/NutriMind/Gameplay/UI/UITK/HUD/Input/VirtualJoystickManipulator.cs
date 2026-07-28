using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace NutriMind.Gameplay.UI
{
    /// <summary>
    /// UI Toolkit manipulator for a single-pointer virtual movement joystick.
    /// </summary>
    public sealed class VirtualJoystickManipulator : PointerManipulator
    {
        private readonly VisualElement _knob;
        private readonly float _radius;
        private readonly float _deadZone;
        private bool _enabled = true;
        private int _activePointerId = PointerId.invalidPointerId;
        private Vector2 _center;

        public event Action<Vector2> MoveChanged;

        public VirtualJoystickManipulator(VisualElement knob, float radius, float deadZone)
        {
            _knob = knob;
            _radius = radius;
            _deadZone = deadZone;
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
        }

        public bool InputEnabled
        {
            get => _enabled;
            set
            {
                _enabled = value;
                if (!_enabled)
                {
                    ResetJoystick();
                }
            }
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
            target.RegisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
            target.RegisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);
            target.RegisterCallback<PointerCancelEvent>(OnPointerCancel, TrickleDown.TrickleDown);
            target.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut, TrickleDown.TrickleDown);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            target.UnregisterCallback<PointerCancelEvent>(OnPointerCancel);
            target.UnregisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
        }

        public void ResetJoystick()
        {
            if (_activePointerId != PointerId.invalidPointerId && target != null)
            {
                target.ReleasePointer(_activePointerId);
            }

            _activePointerId = PointerId.invalidPointerId;
            ResetKnobToCenter();
            MoveChanged?.Invoke(Vector2.zero);
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (!_enabled || _activePointerId != PointerId.invalidPointerId)
            {
                return;
            }

            _activePointerId = evt.pointerId;
            _center = new Vector2(
                target.layout.width * 0.5f,
                target.layout.height * 0.5f);

            target.CapturePointer(_activePointerId);
            UpdateFromPointer((Vector2)evt.localPosition);
            evt.StopPropagation();
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!_enabled || evt.pointerId != _activePointerId)
            {
                return;
            }

            UpdateFromPointer((Vector2)evt.localPosition);
            evt.StopPropagation();
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (evt.pointerId != _activePointerId)
            {
                return;
            }

            ResetJoystick();
            evt.StopPropagation();
        }

        private void OnPointerCancel(PointerCancelEvent evt)
        {
            if (evt.pointerId != _activePointerId)
            {
                return;
            }

            ResetJoystick();
            evt.StopPropagation();
        }

        private void OnPointerCaptureOut(PointerCaptureOutEvent evt)
        {
            if (evt.pointerId != _activePointerId)
            {
                return;
            }

            ResetJoystick();
        }

        private void UpdateFromPointer(Vector2 localPosition)
        {
            Vector2 offset = localPosition - _center;
            Vector2 clamped = VirtualJoystickMath.ClampToRadius(offset, _radius);
            UpdateKnob(clamped);
            MoveChanged?.Invoke(VirtualJoystickMath.CalculateNormalized(offset, _radius, _deadZone));
        }

        private void UpdateKnob(Vector2 clampedOffset)
        {
            if (_knob == null)
            {
                return;
            }

            float knobHalfWidth = _knob.resolvedStyle.width > 0f
                ? _knob.resolvedStyle.width * 0.5f
                : _knob.layout.width * 0.5f;
            float knobHalfHeight = _knob.resolvedStyle.height > 0f
                ? _knob.resolvedStyle.height * 0.5f
                : _knob.layout.height * 0.5f;

            _knob.style.left = _center.x + clampedOffset.x - knobHalfWidth;
            _knob.style.top = _center.y + clampedOffset.y - knobHalfHeight;
        }

        private void ResetKnobToCenter()
        {
            if (_knob == null)
            {
                return;
            }

            _knob.style.left = Length.Percent(50);
            _knob.style.top = Length.Percent(50);
        }
    }
}
