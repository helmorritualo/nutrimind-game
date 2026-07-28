using UnityEngine;

namespace NutriMind.Gameplay.Runtime
{
    public enum AreaGateState
    {
        Locked = 0,
        Unlocking,
        Unlocked
    }

    public sealed class AreaGateController : MonoBehaviour
    {
        [SerializeField] private string _gateId;
        [SerializeField] private Collider _blockerCollider;
        [SerializeField] private GameObject _lockedVisual;
        [SerializeField] private GameObject _unlockedVisual;

        private AreaGateState _state = AreaGateState.Locked;

        public string GateId => _gateId;
        public AreaGateState State => _state;

        private void Awake()
        {
            ApplyLockedState();
        }

        public void Lock()
        {
            _state = AreaGateState.Locked;
            ApplyLockedState();
        }

        public void Unlock()
        {
            if (_state == AreaGateState.Unlocked)
            {
                return;
            }

            _state = AreaGateState.Unlocked;
            if (_blockerCollider != null)
            {
                _blockerCollider.enabled = false;
            }

            if (_lockedVisual != null)
            {
                _lockedVisual.SetActive(false);
            }

            if (_unlockedVisual != null)
            {
                _unlockedVisual.SetActive(true);
            }
        }

        private void ApplyLockedState()
        {
            if (_blockerCollider != null)
            {
                _blockerCollider.enabled = true;
            }

            if (_lockedVisual != null)
            {
                _lockedVisual.SetActive(true);
            }

            if (_unlockedVisual != null)
            {
                _unlockedVisual.SetActive(false);
            }
        }
    }
}
