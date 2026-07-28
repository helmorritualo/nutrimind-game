using System;
using System.Collections.Generic;
using NutriMind.Gameplay.UI;
using UnityEngine;

namespace NutriMind.Gameplay.Runtime
{
    public sealed class MissionPrototypeController : MonoBehaviour
    {
        [SerializeField] private MissionSceneBindings _bindings;

        private readonly IMissionProgressStore _progress = new InMemoryMissionProgressStore();
        private MissionContentData _content;
        private int _currentQuestionIndex;
        private string[] _activeQuestionIds = Array.Empty<string>();
        private Transform _lastCheckpointTransform;
        private bool _bootstrapComplete;

        public IMissionProgressStore Progress => _progress;

        private void Awake()
        {
            if (_bindings == null)
            {
                _bindings = GetComponent<MissionSceneBindings>();
            }
        }

        private void Start()
        {
            Bootstrap();
        }

        public void Bootstrap()
        {
            if (_bootstrapComplete)
            {
                return;
            }

            if (_bindings == null)
            {
                Debug.LogError("[MissionPrototypeController] MissionSceneBindings is missing.");
                return;
            }

            if (!_bindings.TryValidate(out string validationError))
            {
                Debug.LogWarning("[MissionPrototypeController] Scene validation warnings:\n" + validationError);
            }

            if (!MissionContentData.TryLoad(_bindings.MissionJson, out _content, out string loadError))
            {
                Debug.LogError("[MissionPrototypeController] " + loadError);
                return;
            }

            if (_bindings.Player != null && _bindings.PlayerSpawn != null)
            {
                _bindings.Player.TeleportTo(_bindings.PlayerSpawn);
            }

            if (_bindings.Player != null && _bindings.Player.PlayerCamera != null)
            {
                _bindings.PlayerInteraction?.Initialize(
                    this,
                    _bindings.Player.transform,
                    _bindings.Player.PlayerCamera.transform);
                _bindings.HudController?.Initialize(_bindings.Player, _bindings.PlayerInteraction);
            }

            _bindings.UiCoordinator?.Initialize(
                _bindings.HudController,
                _bindings.OverlayController,
                _bindings.Player,
                _bindings.PlayerInteraction);

            _bindings.Fragment1?.Initialize(this);
            _bindings.Fragment2?.Initialize(this);
            _bindings.CheckpointA01?.Initialize(this);
            _bindings.CheckpointA02?.Initialize(this);

            ApplyInitialWorldState();
            ApplyInteractionAvailability();
            RefreshHud();
            _bootstrapComplete = true;
        }

        public void HandleNpcInteraction(NpcGuideInteractable npc)
        {
            if (npc == null)
            {
                return;
            }

            if (npc == _bindings.FarmerLira)
            {
                HandleFarmerLiraTalk();
            }
            else if (npc == _bindings.Mina)
            {
                HandleMinaTalk();
            }
        }

        public void HandleStorybookInteraction(StorybookInteractable storybook)
        {
            if (_progress.CurrentStep != MissionObjectiveStep.Area1_InspectStorybook
                && _progress.IsInteractionCompleted(MissionContentIds.DamagedStorybook))
            {
                return;
            }

            if (_progress.CurrentStep < MissionObjectiveStep.Area1_InspectStorybook)
            {
                return;
            }

            _bindings.OverlayController.ShowEvidence(
                "Damaged Town Storybook",
                _content.Area1.Area.story_source,
                () =>
                {
                    _progress.MarkInteractionCompleted(MissionContentIds.DamagedStorybook);
                    if (_progress.CurrentStep == MissionObjectiveStep.Area1_InspectStorybook)
                    {
                        _progress.CurrentStep = MissionObjectiveStep.Area1_InspectOpeningIllustration;
                    }

                    EnableClue(_bindings.OpeningIllustrationClue, true);
                    EnableClue(_bindings.SurvivingLinesClue, true);
                    ApplyInteractionAvailability();
                    RefreshHud();
                });
        }

        public void HandleClueInteraction(EvidenceClueInteractable clue)
        {
            if (clue == null || _progress.IsClueInspected(clue.ClueId))
            {
                return;
            }

            _bindings.OverlayController.ShowEvidence(
                clue.EvidenceTitle,
                clue.EvidenceBody,
                () =>
                {
                    _progress.MarkClueInspected(clue.ClueId);
                    HandleClueInspected(clue);
                });
        }

        public void HandleCaptionRepairInteraction(CaptionRepairInteractable board)
        {
            if (_progress.CurrentStep != MissionObjectiveStep.Area1_RepairCaption)
            {
                return;
            }

            _bindings.OverlayController.ShowCaptionSelection(
                MissionWorldActionContent.GetArea1CaptionOptions(),
                selectedId =>
                {
                    if (!string.Equals(selectedId, "caption_correct", StringComparison.Ordinal))
                    {
                        _bindings.OverlayController.ShowHint(
                            "Try again",
                            "Choose the caption where “They” clearly refers to the children.",
                            () => HandleCaptionRepairInteraction(board));
                        return;
                    }

                    CompleteCaptionRepair();
                });
        }

        public void HandleEventSequenceInteraction(EventSequenceBoardInteractable board)
        {
            if (_progress.CurrentStep != MissionObjectiveStep.Area2_UseSequenceBoard)
            {
                return;
            }

            _bindings.OverlayController.ShowEventSequence(
                MissionWorldActionContent.GetArea2EventCards(),
                _ =>
                {
                    _progress.SequenceCompleted = true;
                    _progress.CurrentStep = MissionObjectiveStep.Area2_ResolveQuestions;
                    ApplyInteractionAvailability();
                    RefreshHud();
                    BeginQuestionSequence(MissionContentIds.Area2QuestionIds);
                });
        }

        public void HandleFragmentCollected(string fragmentId)
        {
            if (_progress.IsFragmentCollected(fragmentId))
            {
                return;
            }

            _progress.MarkFragmentCollected(fragmentId);
            if (string.Equals(fragmentId, MissionContentIds.Fragment1, StringComparison.Ordinal))
            {
                CompleteArea1Fragment();
            }
            else if (string.Equals(fragmentId, MissionContentIds.Fragment2, StringComparison.Ordinal))
            {
                CompleteArea2Fragment();
            }

            RefreshHud();
        }

        public void HandleCheckpointReached(string checkpointId, Transform respawnPoint)
        {
            if (string.Equals(_progress.CheckpointId, checkpointId, StringComparison.Ordinal))
            {
                return;
            }

            _progress.CheckpointId = checkpointId;
            _lastCheckpointTransform = respawnPoint;
        }

        public void HandleAreaEntry(string areaId)
        {
            if (string.Equals(areaId, MissionContentIds.Area2Id, StringComparison.Ordinal)
                && _progress.CurrentStep >= MissionObjectiveStep.Area1_Complete
                && _progress.CurrentStep < MissionObjectiveStep.Area2_TalkToMina)
            {
                EnterArea2();
            }
        }

        private void HandleFarmerLiraTalk()
        {
            if (_progress.CurrentStep != MissionObjectiveStep.Area1_TalkToLira
                || _progress.IsInteractionCompleted(MissionContentIds.FarmerLiraNpc))
            {
                return;
            }

            ShowDialogueSequence(_content.Area1.Area.opening_dialogue, () =>
            {
                _progress.MarkInteractionCompleted(MissionContentIds.FarmerLiraNpc);
                _progress.CurrentStep = MissionObjectiveStep.Area1_InspectStorybook;
                EnableInteractable(_bindings.DamagedStorybook, true);
                ApplyInteractionAvailability();
                RefreshHud();
            });
        }

        private void HandleMinaTalk()
        {
            if (_progress.CurrentStep != MissionObjectiveStep.Area2_TalkToMina
                || _progress.IsInteractionCompleted(MissionContentIds.MinaNpc))
            {
                return;
            }

            ShowDialogueSequence(_content.Area2.Area.opening_dialogue, () =>
            {
                _progress.MarkInteractionCompleted(MissionContentIds.MinaNpc);
                _progress.CurrentStep = MissionObjectiveStep.Area2_FindClues;
                EnableClue(_bindings.ChildrenGatherClue, true);
                EnableClue(_bindings.StorybookOpenedClue, true);
                EnableClue(_bindings.CaptionRepairedClue, true);
                ApplyInteractionAvailability();
                RefreshHud();
            });
        }

        private void HandleClueInspected(EvidenceClueInteractable clue)
        {
            if (IsArea1Clue(clue))
            {
                HandleArea1ClueProgress();
            }
            else if (IsArea2Clue(clue))
            {
                HandleArea2ClueProgress();
            }

            ApplyInteractionAvailability();
            RefreshHud();
        }

        private void HandleArea1ClueProgress()
        {
            bool openingDone = _progress.IsClueInspected(MissionContentIds.ClueOpeningIllustration);
            bool linesDone = _progress.IsClueInspected(MissionContentIds.ClueSurvivingLines);

            if (openingDone && !linesDone)
            {
                _progress.CurrentStep = MissionObjectiveStep.Area1_InspectSurvivingLines;
            }
            else if (!openingDone && linesDone)
            {
                _progress.CurrentStep = MissionObjectiveStep.Area1_InspectOpeningIllustration;
            }
            else if (openingDone && linesDone && _progress.CurrentStep < MissionObjectiveStep.Area1_ResolveQuestions)
            {
                _progress.CurrentStep = MissionObjectiveStep.Area1_ResolveQuestions;
                BeginQuestionSequence(MissionContentIds.Area1QuestionIds);
            }
        }

        private void HandleArea2ClueProgress()
        {
            int count = 0;
            foreach (string clueId in MissionContentIds.Area2ClueIds)
            {
                if (_progress.IsClueInspected(clueId))
                {
                    count++;
                }
            }

            _progress.InspectedArea2ClueCount = count;
            if (count >= 3 && _progress.CurrentStep < MissionObjectiveStep.Area2_UseSequenceBoard)
            {
                _progress.CurrentStep = MissionObjectiveStep.Area2_UseSequenceBoard;
                EnableInteractable(_bindings.SequenceBoard, true);
            }
        }

        private void BeginQuestionSequence(string[] questionIds)
        {
            _activeQuestionIds = questionIds;
            _currentQuestionIndex = 0;
            ShowQuestionAtCurrentIndex();
        }

        private void ShowQuestionAtCurrentIndex()
        {
            if (_activeQuestionIds == null || _currentQuestionIndex >= _activeQuestionIds.Length)
            {
                OnQuestionSequenceComplete();
                return;
            }

            MissionQuestionDto question = FindQuestion(_activeQuestionIds[_currentQuestionIndex]);
            if (question == null)
            {
                Debug.LogError("[MissionPrototypeController] Missing question " + _activeQuestionIds[_currentQuestionIndex]);
                return;
            }

            _bindings.OverlayController.ShowQuestion(question, selectedOptionId =>
            {
                ProcessQuestionAttempt(question, selectedOptionId);
            });
        }

        private void ProcessQuestionAttempt(MissionQuestionDto question, string selectedOptionId)
        {
            QuestionOutcome outcome = _progress.GetOrCreateQuestionOutcome(question.id);
            if (outcome.Result == QuestionResult.Correct && outcome.Acknowledged)
            {
                AdvanceQuestionSequence();
                return;
            }

            QuestionAttemptResolver.AttemptResult result =
                QuestionAttemptResolver.EvaluateAttempt(question, outcome, selectedOptionId);

            if (result.ShowCorrectAcknowledgement)
            {
                _bindings.OverlayController.ShowCorrectAcknowledgement(result.FeedbackText, () =>
                {
                    outcome.Acknowledged = true;
                    AdvanceQuestionSequence();
                });
                return;
            }

            if (result.ShowHint)
            {
                _bindings.OverlayController.ShowHint("Hint", result.FeedbackText, () => ShowQuestionAtCurrentIndex());
                return;
            }

            if (result.ShowExplanation)
            {
                _progress.MarkReviewRequired(question.id);
                _bindings.OverlayController.ShowExplanation("Review", result.FeedbackText, () =>
                {
                    outcome.Acknowledged = true;
                    AdvanceQuestionSequence();
                });
            }
        }

        private void AdvanceQuestionSequence()
        {
            _currentQuestionIndex++;
            ShowQuestionAtCurrentIndex();
        }

        private void OnQuestionSequenceComplete()
        {
            if (_progress.CurrentStep == MissionObjectiveStep.Area1_ResolveQuestions)
            {
                _progress.CurrentStep = MissionObjectiveStep.Area1_RepairCaption;
                EnableInteractable(_bindings.CaptionBoard, true);
            }
            else if (_progress.CurrentStep == MissionObjectiveStep.Area2_ResolveQuestions)
            {
                CompleteArea2Questions();
            }

            ApplyInteractionAvailability();
            RefreshHud();
        }

        private void CompleteCaptionRepair()
        {
            _progress.CaptionRepaired = true;
            _progress.MarkInteractionCompleted(MissionContentIds.CaptionRepairBoard);
            _progress.CurrentStep = MissionObjectiveStep.Area1_CollectFragment;
            _bindings.Area1WorldState?.ApplyAfterState();
            _bindings.Fragment1?.SetRevealed(true);
            ApplyInteractionAvailability();
            RefreshHud();
        }

        private void CompleteArea1Fragment()
        {
            _progress.CurrentStep = MissionObjectiveStep.Area1_Complete;
            _bindings.CheckpointA01?.SetActivated(true);
            UnlockGate(_bindings.Gate1, MissionContentIds.Gate1);
            EnterArea2();
        }

        private void EnterArea2()
        {
            _progress.CurrentAreaId = MissionContentIds.Area2Id;
            if (_progress.CurrentStep < MissionObjectiveStep.Area2_TalkToMina)
            {
                _progress.CurrentStep = MissionObjectiveStep.Area2_TalkToMina;
            }

            EnableInteractable(_bindings.Mina, true);
            ApplyInteractionAvailability();
            RefreshHud();
        }

        private void CompleteArea2Questions()
        {
            _progress.CurrentStep = MissionObjectiveStep.Area2_CollectFragment;
            _bindings.Area2WorldState?.ApplyAfterState();
            _bindings.Fragment2?.SetRevealed(true);
            ApplyInteractionAvailability();
            RefreshHud();
        }

        private void CompleteArea2Fragment()
        {
            _progress.CurrentStep = MissionObjectiveStep.Area2_Complete;
            _bindings.CheckpointA02?.SetActivated(true);
            UnlockGate(_bindings.Gate2, MissionContentIds.Gate2);
            ApplyInteractionAvailability();
            RefreshHud();
        }

        private void UnlockGate(AreaGateController gate, string gateId)
        {
            if (gate == null || _progress.IsGateUnlocked(gateId))
            {
                return;
            }

            gate.Unlock();
            _progress.MarkGateUnlocked(gateId);
        }

        private void ApplyInitialWorldState()
        {
            _bindings.Area1WorldState?.ApplyBeforeState();
            _bindings.Area2WorldState?.ApplyBeforeState();
            _bindings.Fragment1?.SetRevealed(false);
            _bindings.Fragment2?.SetRevealed(false);
            _bindings.Gate1?.Lock();
            _bindings.Gate2?.Lock();
            _bindings.CheckpointA01?.SetActivated(false);
            _bindings.CheckpointA02?.SetActivated(false);
        }

        private void ApplyInteractionAvailability()
        {
            EnableInteractable(_bindings.FarmerLira, _progress.CurrentStep == MissionObjectiveStep.Area1_TalkToLira);
            EnableInteractable(
                _bindings.DamagedStorybook,
                _progress.CurrentStep >= MissionObjectiveStep.Area1_InspectStorybook
                    && !_progress.IsInteractionCompleted(MissionContentIds.DamagedStorybook));

            EnableClue(
                _bindings.OpeningIllustrationClue,
                _progress.IsInteractionCompleted(MissionContentIds.DamagedStorybook)
                    && !_progress.IsClueInspected(MissionContentIds.ClueOpeningIllustration));
            EnableClue(
                _bindings.SurvivingLinesClue,
                _progress.IsInteractionCompleted(MissionContentIds.DamagedStorybook)
                    && !_progress.IsClueInspected(MissionContentIds.ClueSurvivingLines));

            EnableInteractable(_bindings.CaptionBoard, _progress.CurrentStep == MissionObjectiveStep.Area1_RepairCaption);
            EnableInteractable(_bindings.Mina, _progress.CurrentStep == MissionObjectiveStep.Area2_TalkToMina);

            EnableClue(
                _bindings.ChildrenGatherClue,
                _progress.CurrentStep >= MissionObjectiveStep.Area2_FindClues
                    && !_progress.IsClueInspected(MissionContentIds.ClueChildrenGather));
            EnableClue(
                _bindings.StorybookOpenedClue,
                _progress.CurrentStep >= MissionObjectiveStep.Area2_FindClues
                    && !_progress.IsClueInspected(MissionContentIds.ClueStorybookOpened));
            EnableClue(
                _bindings.CaptionRepairedClue,
                _progress.CurrentStep >= MissionObjectiveStep.Area2_FindClues
                    && !_progress.IsClueInspected(MissionContentIds.ClueCaptionRepaired));

            EnableInteractable(_bindings.SequenceBoard, _progress.CurrentStep == MissionObjectiveStep.Area2_UseSequenceBoard);

            _bindings.UiCoordinator?.RefreshInteractionPrompt();
        }

        private void RefreshHud()
        {
            if (_bindings.UiCoordinator == null)
            {
                return;
            }

            string areaLabel = _progress.CurrentAreaId == MissionContentIds.Area2Id
                ? "Area 2 • Apply"
                : "Area 1 • Discover";

            _bindings.UiCoordinator.SetObjective(
                areaLabel,
                _content?.Raw?.title ?? "The Festival Storybook Rescue",
                GetObjectiveText());

            _bindings.UiCoordinator.SetFragmentProgress(_progress.CollectedFragmentCount, 3);
        }

        private string GetObjectiveText()
        {
            switch (_progress.CurrentStep)
            {
                case MissionObjectiveStep.Area1_TalkToLira:
                    return "Talk to Farmer Lira beside the damaged storybook.";
                case MissionObjectiveStep.Area1_InspectStorybook:
                    return "Inspect the damaged town storybook.";
                case MissionObjectiveStep.Area1_InspectOpeningIllustration:
                    return "Inspect the opening illustration.";
                case MissionObjectiveStep.Area1_InspectSurvivingLines:
                    return "Read the surviving lines.";
                case MissionObjectiveStep.Area1_ResolveQuestions:
                    return "Answer the story questions.";
                case MissionObjectiveStep.Area1_RepairCaption:
                    return "Repair the missing opening caption.";
                case MissionObjectiveStep.Area1_CollectFragment:
                    return "Collect Story Fragment 1.";
                case MissionObjectiveStep.Area1_Complete:
                    return "Continue to Banner Market Lane.";
                case MissionObjectiveStep.Area2_TalkToMina:
                    return "Talk to Mina at the first market stall.";
                case MissionObjectiveStep.Area2_FindClues:
                    return _progress.InspectedArea2ClueCount switch
                    {
                        0 => "Find the three event clues in Market Lane.",
                        1 => "Find the remaining event clues. 1 / 3",
                        2 => "Find the remaining event clues. 2 / 3",
                        _ => "Arrange the events in chronological order."
                    };
                case MissionObjectiveStep.Area2_UseSequenceBoard:
                    return "Arrange the events in chronological order.";
                case MissionObjectiveStep.Area2_ResolveQuestions:
                    return "Answer the sequence questions.";
                case MissionObjectiveStep.Area2_CollectFragment:
                    return "Collect Story Fragment 2.";
                case MissionObjectiveStep.Area2_Complete:
                    return "Continue to Chronicle Courtyard.";
                default:
                    return "Explore Story Square.";
            }
        }

        private MissionQuestionDto FindQuestion(string questionId)
        {
            MissionAreaContent area = _progress.CurrentAreaId == MissionContentIds.Area2Id
                ? _content.Area2
                : _content.Area1;

            foreach (MissionQuestionDto question in area.Questions)
            {
                if (string.Equals(question.id, questionId, StringComparison.Ordinal))
                {
                    return question;
                }
            }

            return null;
        }

        private void ShowDialogueSequence(MissionDialogueLineDto[] lines, Action onComplete)
        {
            if (lines == null || lines.Length == 0)
            {
                onComplete?.Invoke();
                return;
            }

            ShowDialogueAtIndex(lines, 0, onComplete);
        }

        private void ShowDialogueAtIndex(MissionDialogueLineDto[] lines, int index, Action onComplete)
        {
            if (index >= lines.Length)
            {
                onComplete?.Invoke();
                return;
            }

            MissionDialogueLineDto line = lines[index];
            _bindings.OverlayController.ShowDialogue(
                line.speaker,
                line.text,
                () => ShowDialogueAtIndex(lines, index + 1, onComplete));
        }

        private static void EnableInteractable(WorldInteractableBase interactable, bool enabled)
        {
            if (interactable == null)
            {
                return;
            }

            interactable.SetMissionDisabled(!enabled);
            interactable.gameObject.SetActive(true);
        }

        private static void EnableClue(EvidenceClueInteractable clue, bool enabled)
        {
            if (clue == null)
            {
                return;
            }

            clue.SetMissionDisabled(!enabled);
            clue.gameObject.SetActive(true);
        }

        private static bool IsArea1Clue(EvidenceClueInteractable clue)
        {
            return clue == null
                ? false
                : string.Equals(clue.ClueId, MissionContentIds.ClueOpeningIllustration, StringComparison.Ordinal)
                    || string.Equals(clue.ClueId, MissionContentIds.ClueSurvivingLines, StringComparison.Ordinal);
        }

        private static bool IsArea2Clue(EvidenceClueInteractable clue)
        {
            if (clue == null)
            {
                return false;
            }

            foreach (string clueId in MissionContentIds.Area2ClueIds)
            {
                if (string.Equals(clue.ClueId, clueId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
