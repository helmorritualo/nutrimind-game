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
        private bool _area1QuestionSequenceStarted;
        private bool _area2QuestionSequenceStarted;

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

            MissionValidationReport report = _bindings.Validate();
            if (report.Errors.Count > 0)
            {
                Debug.LogError(
                    "[MissionPrototypeController] Mission bootstrap stopped "
                    + "because the scene contains blocking errors:\n"
                    + report);
                return;
            }

            if (report.Warnings.Count > 0 || report.ManualPlacementRequired.Count > 0)
            {
                Debug.LogWarning("[MissionPrototypeController] Mission scene warnings:\n" + report);
            }

            if (!MissionContentData.TryLoad(_bindings.MissionJson, out _content, out string loadError))
            {
                Debug.LogError("[MissionPrototypeController] " + loadError);
                return;
            }

            IGameplayPlayerInput playerInput = ResolvePlayerInput(_bindings);

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
                _bindings.HudController?.Initialize(playerInput, _bindings.PlayerInteraction);
            }

            _bindings.UiCoordinator?.Initialize(
                _bindings.HudController,
                _bindings.OverlayController,
                playerInput,
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

            if (_progress.CurrentStep != MissionObjectiveStep.Area1_InspectStorybook)
            {
                return;
            }

            _bindings.OverlayController.ShowEvidence(
                "Damaged Town Storybook",
                _content.Area1.Area.story_source,
                () =>
                {
                    _progress.MarkInteractionCompleted(MissionContentIds.DamagedStorybook);
                    _progress.CurrentStep = MissionObjectiveStep.Area1_InspectOpeningIllustration;
                    ApplyInteractionAvailability();
                    RefreshHud();
                });
        }

        public void HandleClueInteraction(EvidenceClueInteractable clue)
        {
            if (clue == null)
            {
                Debug.LogWarning(
                    "[MissionPrototypeController] Ignored null clue at step '"
                    + _progress.CurrentStep + "'.");
                return;
            }

            if (string.IsNullOrWhiteSpace(clue.ClueId))
            {
                Debug.LogWarning(
                    "[MissionPrototypeController] Ignored clue '" + clue.name
                    + "' with missing clue ID at step '" + _progress.CurrentStep
                    + "' in area '" + _progress.CurrentAreaId + "'.");
                return;
            }

            if (!IsRecognizedClueId(clue.ClueId))
            {
                Debug.LogWarning(
                    "[MissionPrototypeController] Ignored clue '" + clue.name
                    + "' with unrecognized ID '" + clue.ClueId
                    + "' at step '" + _progress.CurrentStep
                    + "' in area '" + _progress.CurrentAreaId + "'.");
                return;
            }

            if (_progress.IsClueInspected(clue.ClueId))
            {
                Debug.LogWarning(
                    "[MissionPrototypeController] Ignored already-inspected clue '" + clue.name
                    + "' with ID '" + clue.ClueId
                    + "' at step '" + _progress.CurrentStep
                    + "' in area '" + _progress.CurrentAreaId
                    + "'. Check duplicate clue IDs or incorrect MissionSceneBindings.");
                return;
            }

            if (!IsClueAllowedForCurrentState(clue))
            {
                Debug.LogWarning(
                    "[MissionPrototypeController] Ignored clue '" + clue.name
                    + "' with ID '" + clue.ClueId
                    + "' at step '" + _progress.CurrentStep
                    + "' in area '" + _progress.CurrentAreaId + "'.");
                return;
            }

            string title = clue.EvidenceTitle;
            string body = clue.EvidenceBody;
            if (_content != null
                && _content.TryGetEvidenceClue(clue.ClueId, out string contentTitle, out string contentBody))
            {
                title = contentTitle;
                body = contentBody;
            }

            _bindings.OverlayController.ShowEvidence(
                title,
                body,
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
                    BeginArea2QuestionSequence();
                });
        }

        public void HandleFragmentCollected(string fragmentId)
        {
            if (!string.Equals(fragmentId, MissionContentIds.Fragment1, StringComparison.Ordinal)
                && !string.Equals(fragmentId, MissionContentIds.Fragment2, StringComparison.Ordinal))
            {
                Debug.LogWarning(
                    "[MissionPrototypeController] Ignoring unknown or unimplemented fragment id: " + fragmentId);
                return;
            }

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
                && _progress.CurrentStep == MissionObjectiveStep.Area1_Complete)
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
                ApplyInteractionAvailability();
                RefreshHud();
            });
        }

        private void HandleMinaTalk()
        {
            // Failsafe: if Area 1 is done but the entry trigger was missed, talking to Mina
            // still advances into Area 2.
            if (_progress.CurrentStep == MissionObjectiveStep.Area1_Complete)
            {
                EnterArea2();
            }

            if (_progress.CurrentStep != MissionObjectiveStep.Area2_TalkToMina
                || _progress.IsInteractionCompleted(MissionContentIds.MinaNpc))
            {
                return;
            }

            if (_content?.Area2?.Area == null)
            {
                return;
            }

            ShowDialogueSequence(_content.Area2.Area.opening_dialogue, () =>
            {
                _progress.MarkInteractionCompleted(MissionContentIds.MinaNpc);
                _progress.CurrentStep = MissionObjectiveStep.Area2_FindClues;
                ApplyInteractionAvailability();
                RefreshHud();
            });
        }

        private void HandleClueInspected(EvidenceClueInteractable clue)
        {
            if (IsArea1Clue(clue))
            {
                HandleArea1ClueProgress();
                return;
            }

            if (IsArea2Clue(clue))
            {
                HandleArea2ClueProgress();
            }
        }

        private void HandleArea1ClueProgress()
        {
            if (_progress.CurrentStep >= MissionObjectiveStep.Area1_ResolveQuestions)
            {
                return;
            }

            bool openingDone =
                _progress.IsClueInspected(MissionContentIds.ClueOpeningIllustration);
            bool survivingLinesDone =
                _progress.IsClueInspected(MissionContentIds.ClueSurvivingLines);

            if (openingDone && survivingLinesDone)
            {
                _progress.CurrentStep = MissionObjectiveStep.Area1_ResolveQuestions;
                ApplyInteractionAvailability();
                RefreshHud();
                BeginArea1QuestionSequence();
                return;
            }

            if (openingDone)
            {
                _progress.CurrentStep = MissionObjectiveStep.Area1_InspectSurvivingLines;
                ApplyInteractionAvailability();
                RefreshHud();
                return;
            }

            if (survivingLinesDone)
            {
                _progress.CurrentStep = MissionObjectiveStep.Area1_InspectOpeningIllustration;
                ApplyInteractionAvailability();
                RefreshHud();
            }
        }

        private void HandleArea2ClueProgress()
        {
            if (_progress.CurrentStep != MissionObjectiveStep.Area2_FindClues)
            {
                ApplyInteractionAvailability();
                RefreshHud();
                return;
            }

            int count = 0;
            foreach (string clueId in MissionContentIds.Area2ClueIds)
            {
                if (_progress.IsClueInspected(clueId))
                {
                    count++;
                }
            }

            _progress.InspectedArea2ClueCount = count;

            if (count >= 3)
            {
                _progress.CurrentStep = MissionObjectiveStep.Area2_UseSequenceBoard;
            }

            ApplyInteractionAvailability();
            RefreshHud();
        }

        private void BeginArea1QuestionSequence()
        {
            if (_area1QuestionSequenceStarted)
            {
                return;
            }

            _area1QuestionSequenceStarted = true;
            BeginQuestionSequence(MissionContentIds.Area1QuestionIds);
        }

        private void BeginArea2QuestionSequence()
        {
            if (_area2QuestionSequenceStarted)
            {
                return;
            }

            _area2QuestionSequenceStarted = true;
            BeginQuestionSequence(MissionContentIds.Area2QuestionIds);
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

            if (_bindings.OverlayController == null)
            {
                Debug.LogWarning("[MissionPrototypeController] Overlay controller is missing.");
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
                return;
            }

            Debug.LogWarning(
                "[MissionPrototypeController] Question attempt produced no feedback UI for "
                + question.id + ". Advancing to keep the mission playable.");
            AdvanceQuestionSequence();
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
            }
            else if (_progress.CurrentStep == MissionObjectiveStep.Area2_ResolveQuestions)
            {
                CompleteArea2Questions();
                return;
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
            ApplyInteractionAvailability();
            RefreshHud();
        }

        private void EnterArea2()
        {
            if (_progress.CurrentAreaId == MissionContentIds.Area2Id
                && _progress.CurrentStep >= MissionObjectiveStep.Area2_TalkToMina)
            {
                return;
            }

            _progress.CurrentAreaId = MissionContentIds.Area2Id;
            if (_progress.CurrentStep < MissionObjectiveStep.Area2_TalkToMina)
            {
                _progress.CurrentStep = MissionObjectiveStep.Area2_TalkToMina;
            }

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
                _progress.CurrentStep == MissionObjectiveStep.Area1_InspectStorybook
                    && !_progress.IsInteractionCompleted(MissionContentIds.DamagedStorybook));

            bool inArea1Inspection =
                _progress.CurrentStep == MissionObjectiveStep.Area1_InspectOpeningIllustration
                || _progress.CurrentStep == MissionObjectiveStep.Area1_InspectSurvivingLines;

            bool storybookInspected = _progress.IsInteractionCompleted(MissionContentIds.DamagedStorybook);

            EnableClue(
                _bindings.OpeningIllustrationClue,
                storybookInspected
                    && inArea1Inspection
                    && !_progress.IsClueInspected(MissionContentIds.ClueOpeningIllustration));
            EnableClue(
                _bindings.SurvivingLinesClue,
                storybookInspected
                    && inArea1Inspection
                    && !_progress.IsClueInspected(MissionContentIds.ClueSurvivingLines));

            EnableInteractable(_bindings.CaptionBoard, _progress.CurrentStep == MissionObjectiveStep.Area1_RepairCaption);
            EnableInteractable(
                _bindings.Mina,
                _progress.CurrentStep == MissionObjectiveStep.Area2_TalkToMina
                    || _progress.CurrentStep == MissionObjectiveStep.Area1_Complete);

            bool inArea2FindClues =
                _progress.CurrentAreaId == MissionContentIds.Area2Id
                && _progress.CurrentStep == MissionObjectiveStep.Area2_FindClues;

            EnableClue(
                _bindings.ChildrenGatherClue,
                inArea2FindClues
                    && !_progress.IsClueInspected(MissionContentIds.ClueChildrenGather));
            EnableClue(
                _bindings.StorybookOpenedClue,
                inArea2FindClues
                    && !_progress.IsClueInspected(MissionContentIds.ClueStorybookOpened));
            EnableClue(
                _bindings.CaptionRepairedClue,
                inArea2FindClues
                    && !_progress.IsClueInspected(MissionContentIds.ClueCaptionRepaired));

            EnableInteractable(
                _bindings.SequenceBoard,
                _progress.CurrentStep == MissionObjectiveStep.Area2_UseSequenceBoard);

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
                    return "Walk toward Banner Market Lane and talk to Mina.";
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
            MissionQuestionDto question = FindQuestionInArea(_content?.Area2, questionId);
            if (question != null)
            {
                return question;
            }

            return FindQuestionInArea(_content?.Area1, questionId);
        }

        private static MissionQuestionDto FindQuestionInArea(MissionAreaContent area, string questionId)
        {
            if (area?.Questions == null)
            {
                return null;
            }

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

        private bool IsClueAllowedForCurrentState(EvidenceClueInteractable clue)
        {
            if (IsArea1Clue(clue))
            {
                bool inArea1Inspection =
                    _progress.CurrentStep == MissionObjectiveStep.Area1_InspectOpeningIllustration
                    || _progress.CurrentStep == MissionObjectiveStep.Area1_InspectSurvivingLines;
                return _progress.IsInteractionCompleted(MissionContentIds.DamagedStorybook)
                    && inArea1Inspection;
            }

            if (IsArea2Clue(clue))
            {
                return _progress.CurrentAreaId == MissionContentIds.Area2Id
                    && _progress.CurrentStep == MissionObjectiveStep.Area2_FindClues;
            }

            return false;
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
            return clue != null
                && (string.Equals(clue.ClueId, MissionContentIds.ClueOpeningIllustration, StringComparison.Ordinal)
                    || string.Equals(clue.ClueId, MissionContentIds.ClueSurvivingLines, StringComparison.Ordinal));
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

        private static bool IsRecognizedClueId(string clueId)
        {
            if (string.Equals(clueId, MissionContentIds.ClueOpeningIllustration, StringComparison.Ordinal)
                || string.Equals(clueId, MissionContentIds.ClueSurvivingLines, StringComparison.Ordinal))
            {
                return true;
            }

            foreach (string area2ClueId in MissionContentIds.Area2ClueIds)
            {
                if (string.Equals(clueId, area2ClueId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static IGameplayPlayerInput ResolvePlayerInput(MissionSceneBindings bindings)
        {
            if (bindings == null)
            {
                return null;
            }

            if (bindings.PlayerInputAdapter != null)
            {
                return bindings.PlayerInputAdapter;
            }

            return bindings.Player;
        }
    }
}
