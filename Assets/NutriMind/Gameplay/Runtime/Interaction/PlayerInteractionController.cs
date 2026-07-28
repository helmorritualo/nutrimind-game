using System;
using UnityEngine;

namespace NutriMind.Gameplay.Runtime
{
    public sealed class PlayerInteractionController : MonoBehaviour
    {
        [SerializeField] private Transform _playerTransform;
        [SerializeField] private Transform _cameraTransform;
        [SerializeField] private float _maxDistance = 2.75f;
        [SerializeField] private LayerMask _interactionMask = ~0;

        private static readonly Collider[] OverlapBuffer = new Collider[32];
        private IWorldInteractable _focusedTarget;
        private MissionPrototypeController _missionController;
        private bool _interactionEnabled = true;

        public event Action<IWorldInteractable> FocusChanged;

        public IWorldInteractable FocusedTarget => _focusedTarget;
        public bool InteractionEnabled => _interactionEnabled;

        public void Initialize(MissionPrototypeController missionController, Transform player, Transform camera)
        {
            _missionController = missionController;
            if (player != null)
            {
                _playerTransform = player;
            }

            if (camera != null)
            {
                _cameraTransform = camera;
            }
        }

        public void SetInteractionEnabled(bool enabled)
        {
            _interactionEnabled = enabled;
            if (!enabled)
            {
                SetFocusedTarget(null);
            }
        }

        private void Update()
        {
            UpdateFocusedTarget();
        }

        public void InteractWithFocusedTarget()
        {
            if (!_interactionEnabled)
            {
                return;
            }

            if (_focusedTarget == null || !_focusedTarget.CanInteract)
            {
                return;
            }

            var context = new WorldInteractionContext
            {
                MissionController = _missionController,
                PlayerTransform = _playerTransform
            };
            _focusedTarget.Interact(context);
        }

        private void UpdateFocusedTarget()
        {
            if (!_interactionEnabled || _playerTransform == null)
            {
                SetFocusedTarget(null);
                return;
            }

            int hitCount = Physics.OverlapSphereNonAlloc(
                _playerTransform.position,
                _maxDistance,
                OverlapBuffer,
                _interactionMask,
                QueryTriggerInteraction.Collide);

            IWorldInteractable best = null;
            float bestScore = float.MinValue;

            Vector3 forward = _cameraTransform != null ? _cameraTransform.forward : _playerTransform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = _playerTransform.forward;
            }
            else
            {
                forward.Normalize();
            }

            for (int i = 0; i < hitCount; i++)
            {
                Collider collider = OverlapBuffer[i];
                if (collider == null)
                {
                    continue;
                }

                IWorldInteractable interactable = ResolveInteractable(collider);
                if (interactable == null || !interactable.CanInteract)
                {
                    continue;
                }

                Transform focus = interactable.FocusPoint != null ? interactable.FocusPoint : collider.transform;
                Vector3 toTarget = focus.position - _playerTransform.position;
                float distance = toTarget.magnitude;
                if (distance > _maxDistance)
                {
                    continue;
                }

                Vector3 flat = toTarget;
                flat.y = 0f;
                if (flat.sqrMagnitude < 0.0001f)
                {
                    flat = forward;
                }
                else
                {
                    flat.Normalize();
                }

                float facing = Vector3.Dot(forward, flat);
                // Prefer closer, camera-facing targets; priority still breaks ties.
                float score = interactable.Priority * 8f + facing * 6f - distance * 2.5f;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = interactable;
                }
            }

            SetFocusedTarget(best);
        }

        private void SetFocusedTarget(IWorldInteractable target)
        {
            if (ReferenceEquals(_focusedTarget, target))
            {
                return;
            }

            _focusedTarget = target;
            FocusChanged?.Invoke(_focusedTarget);
        }

        private static IWorldInteractable ResolveInteractable(Collider collider)
        {
            if (collider == null)
            {
                return null;
            }

            // Prefer the interactable on the collider's own object so parent
            // storybook/NPC hosts do not steal child clue targets.
            IWorldInteractable local = collider.GetComponent<IWorldInteractable>();
            if (local != null)
            {
                return local;
            }

            return collider.GetComponentInParent<IWorldInteractable>();
        }
    }
}
