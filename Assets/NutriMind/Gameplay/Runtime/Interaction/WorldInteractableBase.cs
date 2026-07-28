using UnityEngine;

namespace NutriMind.Gameplay.Runtime
{
    public abstract class WorldInteractableBase : MonoBehaviour, IWorldInteractable
    {
        [SerializeField] private string _interactionId;
        [SerializeField] private string _promptLabel = "Interact";
        [SerializeField] private string _iconClass = "ds-icon--search";
        [SerializeField] private int _priority;
        [SerializeField] private Transform _focusPoint;
        [SerializeField] private bool _startDisabled;

        private bool _disabledByMission;

        public string InteractionId => _interactionId;
        public string PromptLabel => _promptLabel;
        public string IconClass => _iconClass;
        public int Priority => _priority;
        public Transform FocusPoint => _focusPoint != null ? _focusPoint : transform;
        public bool CanInteract => isActiveAndEnabled && !_startDisabled && !_disabledByMission && CanInteractInternal();

        protected virtual bool CanInteractInternal() => true;

        public void Interact(WorldInteractionContext context)
        {
            if (!CanInteract)
            {
                return;
            }

            OnInteract(context);
        }

        protected abstract void OnInteract(WorldInteractionContext context);

        public void SetMissionDisabled(bool disabled)
        {
            _disabledByMission = disabled;
        }

        public void SetStartDisabled(bool disabled)
        {
            _startDisabled = disabled;
        }

        protected virtual void Reset()
        {
            if (_focusPoint == null)
            {
                _focusPoint = transform;
            }
        }
    }
}
