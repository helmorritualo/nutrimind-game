namespace NutriMind.Gameplay.Runtime
{
    public sealed class CaptionRepairInteractable : WorldInteractableBase
    {
        protected override void OnInteract(WorldInteractionContext context)
        {
            context?.MissionController?.HandleCaptionRepairInteraction(this);
        }
    }
}
