namespace NutriMind.Gameplay.Runtime
{
    public sealed class EvidenceClueInteractable : WorldInteractableBase
    {
        [UnityEngine.SerializeField] private string _clueId;
        [UnityEngine.SerializeField] private string _evidenceTitle;
        [UnityEngine.TextArea(2, 6)]
        [UnityEngine.SerializeField] private string _evidenceBody;

        public string ClueId => _clueId;
        public string EvidenceTitle => _evidenceTitle;
        public string EvidenceBody => _evidenceBody;

        protected override bool CanInteractInternal()
        {
            return !string.IsNullOrEmpty(_clueId);
        }

        protected override void OnInteract(WorldInteractionContext context)
        {
            context?.MissionController?.HandleClueInteraction(this);
        }
    }
}
