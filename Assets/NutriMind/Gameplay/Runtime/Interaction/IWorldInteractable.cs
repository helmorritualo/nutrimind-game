using UnityEngine;

namespace NutriMind.Gameplay.Runtime
{
    public sealed class WorldInteractionContext
    {
        public MissionPrototypeController MissionController { get; set; }
        public Transform PlayerTransform { get; set; }
    }

    public interface IWorldInteractable
    {
        string InteractionId { get; }
        string PromptLabel { get; }
        string IconClass { get; }
        int Priority { get; }
        bool CanInteract { get; }
        Transform FocusPoint { get; }

        void Interact(WorldInteractionContext context);
    }
}
