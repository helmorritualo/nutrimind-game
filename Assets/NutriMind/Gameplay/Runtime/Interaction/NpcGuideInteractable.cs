namespace NutriMind.Gameplay.Runtime
{
    public sealed class NpcGuideInteractable : WorldInteractableBase
    {
        protected override void OnInteract(WorldInteractionContext context)
        {
            context?.MissionController?.HandleNpcInteraction(this);
        }
    }
}
