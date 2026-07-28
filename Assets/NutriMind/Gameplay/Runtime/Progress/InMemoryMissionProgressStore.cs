using System.Collections.Generic;

namespace NutriMind.Gameplay.Runtime
{
    public interface IMissionProgressStore
    {
        string CurrentAreaId { get; set; }
        MissionObjectiveStep CurrentStep { get; set; }
        string CheckpointId { get; set; }
        bool CaptionRepaired { get; set; }
        bool SequenceCompleted { get; set; }
        int InspectedArea2ClueCount { get; set; }

        bool IsInteractionCompleted(string interactionId);
        void MarkInteractionCompleted(string interactionId);
        bool IsClueInspected(string clueId);
        void MarkClueInspected(string clueId);
        bool IsFragmentCollected(string fragmentId);
        int CollectedFragmentCount { get; }
        void MarkFragmentCollected(string fragmentId);
        bool IsGateUnlocked(string gateId);
        void MarkGateUnlocked(string gateId);
        QuestionOutcome GetOrCreateQuestionOutcome(string questionId);
        bool IsReviewRequired(string questionId);
        void MarkReviewRequired(string questionId);
        MissionPrototypeState CreateSnapshot();
        void RestoreFromSnapshot(MissionPrototypeState snapshot);
    }

    public sealed class InMemoryMissionProgressStore : IMissionProgressStore
    {
        private readonly HashSet<string> _completedInteractions = new HashSet<string>();
        private readonly HashSet<string> _inspectedClues = new HashSet<string>();
        private readonly HashSet<string> _collectedFragments = new HashSet<string>();
        private readonly HashSet<string> _unlockedGates = new HashSet<string>();
        private readonly HashSet<string> _reviewRequired = new HashSet<string>();
        private readonly Dictionary<string, QuestionOutcome> _questionOutcomes = new Dictionary<string, QuestionOutcome>();

        public string CurrentAreaId { get; set; } = MissionContentIds.Area1Id;
        public MissionObjectiveStep CurrentStep { get; set; } = MissionObjectiveStep.Area1_TalkToLira;
        public string CheckpointId { get; set; } = string.Empty;
        public bool CaptionRepaired { get; set; }
        public bool SequenceCompleted { get; set; }
        public int InspectedArea2ClueCount { get; set; }

        public bool IsInteractionCompleted(string interactionId)
        {
            return !string.IsNullOrEmpty(interactionId) && _completedInteractions.Contains(interactionId);
        }

        public void MarkInteractionCompleted(string interactionId)
        {
            if (!string.IsNullOrEmpty(interactionId))
            {
                _completedInteractions.Add(interactionId);
            }
        }

        public bool IsClueInspected(string clueId)
        {
            return !string.IsNullOrEmpty(clueId) && _inspectedClues.Contains(clueId);
        }

        public void MarkClueInspected(string clueId)
        {
            if (!string.IsNullOrEmpty(clueId))
            {
                _inspectedClues.Add(clueId);
            }
        }

        public bool IsFragmentCollected(string fragmentId)
        {
            return !string.IsNullOrEmpty(fragmentId) && _collectedFragments.Contains(fragmentId);
        }

        public int CollectedFragmentCount => _collectedFragments.Count;

        public void MarkFragmentCollected(string fragmentId)
        {
            if (!string.IsNullOrEmpty(fragmentId))
            {
                _collectedFragments.Add(fragmentId);
            }
        }

        public bool IsGateUnlocked(string gateId)
        {
            return !string.IsNullOrEmpty(gateId) && _unlockedGates.Contains(gateId);
        }

        public void MarkGateUnlocked(string gateId)
        {
            if (!string.IsNullOrEmpty(gateId))
            {
                _unlockedGates.Add(gateId);
            }
        }

        public QuestionOutcome GetOrCreateQuestionOutcome(string questionId)
        {
            if (!_questionOutcomes.TryGetValue(questionId, out QuestionOutcome outcome))
            {
                outcome = new QuestionOutcome { QuestionId = questionId };
                _questionOutcomes[questionId] = outcome;
            }

            return outcome;
        }

        public bool IsReviewRequired(string questionId)
        {
            return !string.IsNullOrEmpty(questionId) && _reviewRequired.Contains(questionId);
        }

        public void MarkReviewRequired(string questionId)
        {
            if (!string.IsNullOrEmpty(questionId))
            {
                _reviewRequired.Add(questionId);
            }
        }

        public MissionPrototypeState CreateSnapshot()
        {
            var snapshot = new MissionPrototypeState
            {
                CurrentAreaId = CurrentAreaId,
                CurrentStep = CurrentStep,
                CheckpointId = CheckpointId,
                CaptionRepaired = CaptionRepaired,
                SequenceCompleted = SequenceCompleted,
                InspectedArea2ClueCount = InspectedArea2ClueCount
            };

            foreach (string id in _completedInteractions)
            {
                snapshot.CompletedInteractionIds.Add(id);
            }

            foreach (string id in _inspectedClues)
            {
                snapshot.InspectedClueIds.Add(id);
            }

            foreach (string id in _collectedFragments)
            {
                snapshot.CollectedFragmentIds.Add(id);
            }

            foreach (string id in _unlockedGates)
            {
                snapshot.UnlockedGateIds.Add(id);
            }

            foreach (string id in _reviewRequired)
            {
                snapshot.ReviewRequiredQuestionIds.Add(id);
            }

            foreach (KeyValuePair<string, QuestionOutcome> pair in _questionOutcomes)
            {
                snapshot.QuestionOutcomes[pair.Key] = pair.Value;
            }

            return snapshot;
        }

        public void RestoreFromSnapshot(MissionPrototypeState snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            CurrentAreaId = snapshot.CurrentAreaId;
            CurrentStep = snapshot.CurrentStep;
            CheckpointId = snapshot.CheckpointId;
            CaptionRepaired = snapshot.CaptionRepaired;
            SequenceCompleted = snapshot.SequenceCompleted;
            InspectedArea2ClueCount = snapshot.InspectedArea2ClueCount;

            _completedInteractions.Clear();
            foreach (string id in snapshot.CompletedInteractionIds)
            {
                _completedInteractions.Add(id);
            }

            _inspectedClues.Clear();
            foreach (string id in snapshot.InspectedClueIds)
            {
                _inspectedClues.Add(id);
            }

            _collectedFragments.Clear();
            foreach (string id in snapshot.CollectedFragmentIds)
            {
                _collectedFragments.Add(id);
            }

            _unlockedGates.Clear();
            foreach (string id in snapshot.UnlockedGateIds)
            {
                _unlockedGates.Add(id);
            }

            _reviewRequired.Clear();
            foreach (string id in snapshot.ReviewRequiredQuestionIds)
            {
                _reviewRequired.Add(id);
            }

            _questionOutcomes.Clear();
            foreach (KeyValuePair<string, QuestionOutcome> pair in snapshot.QuestionOutcomes)
            {
                _questionOutcomes[pair.Key] = pair.Value;
            }
        }
    }
}
