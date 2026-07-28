using UnityEngine;

namespace NutriMind.Gameplay.Runtime
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class GameplayPrototypePlayerController : MonoBehaviour, IGameplayPlayerInput
    {
        [SerializeField] private Transform _cameraPivot;
        [SerializeField] private Camera _playerCamera;
        [SerializeField] private float _moveSpeed = 4.5f;
        [SerializeField] private float _gravity = -20f;
        [SerializeField] private float _lookSensitivity = 0.15f;
        [SerializeField] private float _minPitch = -35f;
        [SerializeField] private float _maxPitch = 55f;

        private CharacterController _controller;
        private Vector2 _moveInput;
        private float _pitch;
        private float _verticalVelocity;
        private bool _inputEnabled = true;

        public Transform CameraPivot => _cameraPivot;
        public Camera PlayerCamera => _playerCamera;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            if (_cameraPivot == null)
            {
                _cameraPivot = transform;
            }

            if (_playerCamera == null)
            {
                _playerCamera = Camera.main;
            }
        }

        private void Update()
        {
            if (!_inputEnabled)
            {
                return;
            }

            ApplyMovement();
        }

        public void SetMove(Vector2 value)
        {
            _moveInput = Vector2.ClampMagnitude(value, 1f);
        }

        public void AddLookDelta(Vector2 value)
        {
            if (!_inputEnabled)
            {
                return;
            }

            transform.Rotate(Vector3.up, value.x * _lookSensitivity, Space.World);
            _pitch = Mathf.Clamp(_pitch - value.y * _lookSensitivity, _minPitch, _maxPitch);
            if (_cameraPivot != null)
            {
                _cameraPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
            }
        }

        public void SetGameplayInputEnabled(bool enabled)
        {
            _inputEnabled = enabled;
            if (!enabled)
            {
                ResetInput();
            }
        }

        public void ResetInput()
        {
            _moveInput = Vector2.zero;
        }

        public void TeleportTo(Transform target)
        {
            if (target == null || _controller == null)
            {
                return;
            }

            _controller.enabled = false;
            transform.SetPositionAndRotation(target.position, target.rotation);
            _controller.enabled = true;
            ResetInput();
        }

        private void ApplyMovement()
        {
            Vector3 move = transform.right * _moveInput.x + transform.forward * _moveInput.y;
            if (_controller.isGrounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = -2f;
            }

            _verticalVelocity += _gravity * Time.deltaTime;
            move.y = _verticalVelocity;
            _controller.Move(move * _moveSpeed * Time.deltaTime);
        }
    }
}
