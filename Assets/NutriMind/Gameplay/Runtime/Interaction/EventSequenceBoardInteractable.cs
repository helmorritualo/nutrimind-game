namespace NutriMind.Gameplay.Runtime
{
    public sealed class EventSequenceBoardInteractable : WorldInteractableBase
    {
        protected override void OnInteract(WorldInteractionContext context)
        {
            context?.MissionController?.HandleEventSequenceInteraction(this);
        }
    }
}
