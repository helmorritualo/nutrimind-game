using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace NutriMind.Gameplay.UI
{
    /// <summary>
    /// UI Toolkit manipulator for camera-look drag input on a touch region.
    /// </summary>
    public sealed class TouchLookManipulator : PointerManipulator
    {
        private bool _enabled = true;
        private int _activePointerId = PointerId.invalidPointerId;
        private Vector2 _lastPosition;

        public event Action<Vector2> LookDeltaChanged;

        public TouchLookManipulator()
        {
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
                    ResetLook();
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

        public void ResetLook()
        {
            if (_activePointerId != PointerId.invalidPointerId && target != null)
            {
                target.ReleasePointer(_activePointerId);
            }

            _activePointerId = PointerId.invalidPointerId;
            LookDeltaChanged?.Invoke(Vector2.zero);
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (!_enabled || _activePointerId != PointerId.invalidPointerId)
            {
                return;
            }

            _activePointerId = evt.pointerId;
            _lastPosition = (Vector2)evt.localPosition;
            target.CapturePointer(_activePointerId);
            evt.StopPropagation();
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!_enabled || evt.pointerId != _activePointerId)
            {
                return;
            }

            Vector2 current = (Vector2)evt.localPosition;
            Vector2 delta = current - _lastPosition;
            _lastPosition = current;
            LookDeltaChanged?.Invoke(delta);
            evt.StopPropagation();
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (evt.pointerId != _activePointerId)
            {
                return;
            }

            ResetLook();
            evt.StopPropagation();
        }

        private void OnPointerCancel(PointerCancelEvent evt)
        {
            if (evt.pointerId != _activePointerId)
            {
                return;
            }

            ResetLook();
            evt.StopPropagation();
        }

        private void OnPointerCaptureOut(PointerCaptureOutEvent evt)
        {
            if (evt.pointerId != _activePointerId)
            {
                return;
            }

            ResetLook();
        }
    }
}
