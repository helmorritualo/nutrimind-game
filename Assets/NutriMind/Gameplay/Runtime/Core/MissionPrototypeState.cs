using System.Collections.Generic;

namespace NutriMind.Gameplay.Runtime
{
    public sealed class MissionPrototypeState
    {
        public string CurrentAreaId { get; set; } = MissionContentIds.Area1Id;
        public MissionObjectiveStep CurrentStep { get; set; } = MissionObjectiveStep.Area1_TalkToLira;
        public HashSet<string> CompletedInteractionIds { get; } = new HashSet<string>();
        public HashSet<string> InspectedClueIds { get; } = new HashSet<string>();
        public Dictionary<string, QuestionOutcome> QuestionOutcomes { get; } =
            new Dictionary<string, QuestionOutcome>();
        public HashSet<string> ReviewRequiredQuestionIds { get; } = new HashSet<string>();
        public bool CaptionRepaired { get; set; }
        public bool SequenceCompleted { get; set; }
        public HashSet<string> CollectedFragmentIds { get; } = new HashSet<string>();
        public HashSet<string> UnlockedGateIds { get; } = new HashSet<string>();
        public string CheckpointId { get; set; } = string.Empty;
        public int InspectedArea2ClueCount { get; set; }
    }
}
