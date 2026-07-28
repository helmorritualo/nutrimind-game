namespace NutriMind.Gameplay.Runtime
{
    public sealed class StorybookInteractable : WorldInteractableBase
    {
        protected override void OnInteract(WorldInteractionContext context)
        {
            context?.MissionController?.HandleStorybookInteraction(this);
        }
    }
}
