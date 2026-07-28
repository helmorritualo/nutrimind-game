using UnityEngine;

namespace NutriMind.Gameplay.Runtime
{
    public sealed class WorldStateController : MonoBehaviour
    {
        [SerializeField] private GameObject _beforeStateRoot;
        [SerializeField] private GameObject _afterStateRoot;
        [SerializeField] private GameObject[] _optionalEffects;
        [SerializeField] private bool _startInBeforeState = true;

        private bool _isAfterState;

        private void Awake()
        {
            if (_startInBeforeState)
            {
                ApplyBeforeState();
            }
            else
            {
                ApplyAfterState();
            }
        }

        public void ApplyBeforeState()
        {
            _isAfterState = false;
            SetActiveSafe(_beforeStateRoot, true);
            SetActiveSafe(_afterStateRoot, false);
            SetEffectsActive(false);
        }

        public void ApplyAfterState()
        {
            if (_isAfterState)
            {
                return;
            }

            _isAfterState = true;
            SetActiveSafe(_beforeStateRoot, false);
            SetActiveSafe(_afterStateRoot, true);
            SetEffectsActive(true);
        }

        private void SetEffectsActive(bool active)
        {
            if (_optionalEffects == null)
            {
                return;
            }

            foreach (GameObject effect in _optionalEffects)
            {
                SetActiveSafe(effect, active);
            }
        }

        private static void SetActiveSafe(GameObject target, bool active)
        {
            if (target != null)
            {
                target.SetActive(active);
            }
        }
    }
}
