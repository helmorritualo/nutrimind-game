using System;
using System.Collections.Generic;
using System.Text;
using NutriMind.Gameplay.UI;
using UnityEngine;

namespace NutriMind.Gameplay.Runtime
{
    public sealed class MissionValidationReport
    {
        public readonly List<string> Errors = new List<string>();
        public readonly List<string> Warnings = new List<string>();
        public readonly List<string> ManualPlacementRequired = new List<string>();
        public readonly List<string> Informational = new List<string>();

        public bool IsValid => Errors.Count == 0 && ManualPlacementRequired.Count == 0;

        public override string ToString()
        {
            var builder = new StringBuilder();
            AppendSection(builder, "Errors", Errors);
            AppendSection(builder, "Warnings", Warnings);
            AppendSection(builder, "Manual placement required", ManualPlacementRequired);
            AppendSection(builder, "Informational checks", Informational);
            return builder.ToString().Trim();
        }

        private static void AppendSection(StringBuilder builder, string title, List<string> items)
        {
            builder.AppendLine(title + ":");
            if (items.Count == 0)
            {
                builder.AppendLine("  (none)");
                return;
            }

            foreach (string item in items)
            {
                builder.AppendLine("  - " + item);
            }
        }
    }

    public sealed class MissionSceneBindings : MonoBehaviour
    {
        [Header("Runtime")]
        [SerializeField] private TextAsset _missionJson;
        [SerializeField] private MissionPrototypeController _missionController;
        [SerializeField] private GameplayUiCoordinator _uiCoordinator;
        [SerializeField] private GameplayStudentHudRuntimeController _hudController;
        [SerializeField] private GameplayLearningOverlayController _overlayController;

        [Header("Player")]
        [SerializeField] private GameplayPrototypePlayerController _player;
        [SerializeField] private GameplayPrototypePlayerInputAdapter _playerInputAdapter;
        [SerializeField] private PlayerInteractionController _playerInteraction;
        [SerializeField] private Transform _playerSpawn;

        [Header("Area 1")]
        [SerializeField] private NpcGuideInteractable _farmerLira;
        [SerializeField] private StorybookInteractable _damagedStorybook;
        [SerializeField] private EvidenceClueInteractable _openingIllustrationClue;
        [SerializeField] private EvidenceClueInteractable _survivingLinesClue;
        [SerializeField] private CaptionRepairInteractable _captionBoard;
        [SerializeField] private WorldStateController _area1WorldState;
        [SerializeField] private StoryFragmentCollectible _fragment1;
        [SerializeField] private AreaGateController _gate1;
        [SerializeField] private CheckpointTrigger _checkpointA01;
        [SerializeField] private AreaEntryTrigger _area2Entry;

        [Header("Area 2")]
        [SerializeField] private NpcGuideInteractable _mina;
        [SerializeField] private EvidenceClueInteractable _childrenGatherClue;
        [SerializeField] private EvidenceClueInteractable _storybookOpenedClue;
        [SerializeField] private EvidenceClueInteractable _captionRepairedClue;
        [SerializeField] private EventSequenceBoardInteractable _sequenceBoard;
        [SerializeField] private WorldStateController _area2WorldState;
        [SerializeField] private StoryFragmentCollectible _fragment2;
        [SerializeField] private AreaGateController _gate2;
        [SerializeField] private CheckpointTrigger _checkpointA02;

        public TextAsset MissionJson => _missionJson;
        public MissionPrototypeController MissionController => _missionController;
        public GameplayUiCoordinator UiCoordinator => _uiCoordinator;
        public GameplayStudentHudRuntimeController HudController => _hudController;
        public GameplayLearningOverlayController OverlayController => _overlayController;
        public GameplayPrototypePlayerController Player => _player;
        public GameplayPrototypePlayerInputAdapter PlayerInputAdapter => _playerInputAdapter;
        public PlayerInteractionController PlayerInteraction => _playerInteraction;
        public Transform PlayerSpawn => _playerSpawn;
        public NpcGuideInteractable FarmerLira => _farmerLira;
        public StorybookInteractable DamagedStorybook => _damagedStorybook;
        public EvidenceClueInteractable OpeningIllustrationClue => _openingIllustrationClue;
        public EvidenceClueInteractable SurvivingLinesClue => _survivingLinesClue;
        public CaptionRepairInteractable CaptionBoard => _captionBoard;
        public WorldStateController Area1WorldState => _area1WorldState;
        public StoryFragmentCollectible Fragment1 => _fragment1;
        public AreaGateController Gate1 => _gate1;
        public CheckpointTrigger CheckpointA01 => _checkpointA01;
        public AreaEntryTrigger Area2Entry => _area2Entry;
        public NpcGuideInteractable Mina => _mina;
        public EvidenceClueInteractable ChildrenGatherClue => _childrenGatherClue;
        public EvidenceClueInteractable StorybookOpenedClue => _storybookOpenedClue;
        public EvidenceClueInteractable CaptionRepairedClue => _captionRepairedClue;
        public EventSequenceBoardInteractable SequenceBoard => _sequenceBoard;
        public WorldStateController Area2WorldState => _area2WorldState;
        public StoryFragmentCollectible Fragment2 => _fragment2;
        public AreaGateController Gate2 => _gate2;
        public CheckpointTrigger CheckpointA02 => _checkpointA02;

        public bool TryValidate(out string error)
        {
            MissionValidationReport report = Validate();
            error = report.ToString();
            return report.IsValid;
        }

        public MissionValidationReport Validate()
        {
            var report = new MissionValidationReport();
            ValidateRequiredReferences(report);
            ValidateStableIds(report);
            ValidateColliders(report);
            ValidateStartingStates(report);
            ValidatePlacementSanity(report);
            ValidatePlacementMarkers(report);
            return report;
        }

        private void ValidateRequiredReferences(MissionValidationReport report)
        {
            Require(_missionJson, "Mission JSON", report);
            Require(_missionController, "MissionPrototypeController", report);
            Require(_uiCoordinator, "GameplayUiCoordinator", report);
            Require(_hudController, "GameplayStudentHudRuntimeController", report);
            Require(_overlayController, "GameplayLearningOverlayController", report);
            Require(_player, "GameplayPrototypePlayerController", report);
            if (_player == null && _playerInputAdapter == null)
            {
                report.Errors.Add("Missing player adapter or GameplayPrototypePlayerController.");
            }

            Require(_playerInteraction, "PlayerInteractionController", report);
            Require(_playerSpawn, "Player spawn", report);
            Require(_farmerLira, "Farmer Lira", report);
            Require(_damagedStorybook, "Damaged storybook", report);
            Require(_openingIllustrationClue, "Opening illustration clue", report);
            Require(_survivingLinesClue, "Surviving lines clue", report);
            Require(_captionBoard, "Caption board", report);
            Require(_area1WorldState, "Area 1 world state", report);
            Require(_fragment1, "Fragment 1", report);
            Require(_gate1, "Gate 1", report);
            Require(_checkpointA01, "Checkpoint A01", report);
            Require(_area2Entry, "Area 2 entry", report);
            Require(_mina, "Mina", report);
            Require(_childrenGatherClue, "Children gather clue", report);
            Require(_storybookOpenedClue, "Storybook opened clue", report);
            Require(_captionRepairedClue, "Caption repaired clue", report);
            Require(_sequenceBoard, "Sequence board", report);
            Require(_area2WorldState, "Area 2 world state", report);
            Require(_fragment2, "Fragment 2", report);
            Require(_gate2, "Gate 2", report);
            Require(_checkpointA02, "Checkpoint A02", report);
        }

        private void ValidateStableIds(MissionValidationReport report)
        {
            var interactionIds = new Dictionary<string, string>(StringComparer.Ordinal);
            ValidateInteractableId(_farmerLira, "Farmer Lira", MissionContentIds.FarmerLiraNpc, interactionIds, report);
            ValidateInteractableId(_damagedStorybook, "Damaged storybook", MissionContentIds.DamagedStorybook, interactionIds, report);
            ValidateInteractableId(_captionBoard, "Caption board", MissionContentIds.CaptionRepairBoard, interactionIds, report);
            ValidateInteractableId(_mina, "Mina", MissionContentIds.MinaNpc, interactionIds, report);
            ValidateInteractableId(_sequenceBoard, "Sequence board", MissionContentIds.EventSequenceBoard, interactionIds, report);

            if (_openingIllustrationClue != null && _survivingLinesClue != null
                && ReferenceEquals(_openingIllustrationClue, _survivingLinesClue))
            {
                report.Errors.Add(
                    "Opening Illustration Clue and Surviving Lines Clue reference the same component.");
            }

            if (_childrenGatherClue != null && _storybookOpenedClue != null
                && ReferenceEquals(_childrenGatherClue, _storybookOpenedClue))
            {
                report.Errors.Add(
                    "Children Gather Clue and Storybook Opened Clue reference the same component.");
            }

            if (_childrenGatherClue != null && _captionRepairedClue != null
                && ReferenceEquals(_childrenGatherClue, _captionRepairedClue))
            {
                report.Errors.Add(
                    "Children Gather Clue and Caption Repaired Clue reference the same component.");
            }

            if (_storybookOpenedClue != null && _captionRepairedClue != null
                && ReferenceEquals(_storybookOpenedClue, _captionRepairedClue))
            {
                report.Errors.Add(
                    "Storybook Opened Clue and Caption Repaired Clue reference the same component.");
            }

            var clueIds = new Dictionary<string, string>(StringComparer.Ordinal);
            ValidateClueId(
                _openingIllustrationClue,
                "Opening illustration clue",
                MissionContentIds.ClueOpeningIllustration,
                "CluePoint01_OpeningIllustration",
                clueIds,
                report);
            ValidateClueId(
                _survivingLinesClue,
                "Surviving lines clue",
                MissionContentIds.ClueSurvivingLines,
                "CluePoint02_SurvivingLines",
                clueIds,
                report);
            ValidateClueId(
                _childrenGatherClue,
                "Children gather clue",
                MissionContentIds.ClueChildrenGather,
                "CluePoint01_ChildrenGather",
                clueIds,
                report);
            ValidateClueId(
                _storybookOpenedClue,
                "Storybook opened clue",
                MissionContentIds.ClueStorybookOpened,
                "CluePoint02_StorybookOpened",
                clueIds,
                report);
            ValidateClueId(
                _captionRepairedClue,
                "Caption repaired clue",
                MissionContentIds.ClueCaptionRepaired,
                "CluePoint03_CaptionRepaired",
                clueIds,
                report);

            if (_fragment1 != null && _fragment1.CollectibleId != MissionContentIds.Fragment1)
            {
                report.Errors.Add("Fragment 1 collectible id must be " + MissionContentIds.Fragment1);
            }

            if (_fragment2 != null && _fragment2.CollectibleId != MissionContentIds.Fragment2)
            {
                report.Errors.Add("Fragment 2 collectible id must be " + MissionContentIds.Fragment2);
            }

            if (_gate1 != null && _gate1.GateId != MissionContentIds.Gate1)
            {
                report.Errors.Add("Gate 1 id must be " + MissionContentIds.Gate1);
            }

            if (_gate2 != null && _gate2.GateId != MissionContentIds.Gate2)
            {
                report.Errors.Add("Gate 2 id must be " + MissionContentIds.Gate2);
            }

            if (_checkpointA01 != null && _checkpointA01.CheckpointId != MissionContentIds.CheckpointA01)
            {
                report.Errors.Add("Checkpoint A01 id must be " + MissionContentIds.CheckpointA01);
            }

            if (_checkpointA02 != null && _checkpointA02.CheckpointId != MissionContentIds.CheckpointA02)
            {
                report.Errors.Add("Checkpoint A02 id must be " + MissionContentIds.CheckpointA02);
            }
        }

        private void ValidateColliders(MissionValidationReport report)
        {
            ValidateInteractableTrigger(_farmerLira, "Farmer Lira", report);
            ValidateInteractableTrigger(_damagedStorybook, "Damaged storybook", report);
            ValidateInteractableTrigger(_openingIllustrationClue, "Opening illustration clue", report);
            ValidateInteractableTrigger(_survivingLinesClue, "Surviving lines clue", report);
            ValidateInteractableTrigger(_captionBoard, "Caption board", report);
            ValidateInteractableTrigger(_mina, "Mina", report);
            ValidateInteractableTrigger(_childrenGatherClue, "Children gather clue", report);
            ValidateInteractableTrigger(_storybookOpenedClue, "Storybook opened clue", report);
            ValidateInteractableTrigger(_captionRepairedClue, "Caption repaired clue", report);
            ValidateInteractableTrigger(_sequenceBoard, "Sequence board", report);

            ValidateFragmentTrigger(_fragment1, "Fragment 1", report);
            ValidateFragmentTrigger(_fragment2, "Fragment 2", report);
            ValidateTriggerCollider(_checkpointA01 != null ? _checkpointA01.GetComponent<Collider>() : null, "Checkpoint A01", report);
            ValidateTriggerCollider(_checkpointA02 != null ? _checkpointA02.GetComponent<Collider>() : null, "Checkpoint A02", report);
            ValidateTriggerCollider(_area2Entry != null ? _area2Entry.GetComponent<Collider>() : null, "Area 2 entry", report);

            ValidateGateBlocker(_gate1, "Gate 1", report);
            ValidateGateBlocker(_gate2, "Gate 2", report);
        }

        private void ValidateStartingStates(MissionValidationReport report)
        {
            ValidateFragmentStartState(_fragment1, "Fragment 1", report);
            ValidateFragmentStartState(_fragment2, "Fragment 2", report);

            if (_gate1 != null && _gate1.State == AreaGateState.Unlocked)
            {
                report.Errors.Add("Gate 1 should start locked.");
            }

            if (_gate2 != null && _gate2.State == AreaGateState.Unlocked)
            {
                report.Errors.Add("Gate 2 should start locked.");
            }

            if (_overlayController != null && _overlayController.IsOpen)
            {
                report.Warnings.Add("Learning overlay should start hidden.");
            }

            report.Informational.Add("Starting-state checks completed for fragments, gates, and overlay.");
        }

        private void ValidatePlacementSanity(MissionValidationReport report)
        {
            WarnIfAtParentOrigin(_farmerLira != null ? _farmerLira.transform : null, "Farmer Lira", report);
            WarnIfAtParentOrigin(_mina != null ? _mina.transform : null, "Mina", report);
            WarnIfAtParentOrigin(_storybookOpenedClue != null ? _storybookOpenedClue.transform : null, "Storybook opened clue", report);
            WarnIfAtParentOrigin(_captionRepairedClue != null ? _captionRepairedClue.transform : null, "Caption repaired clue", report);
            WarnIfAtParentOrigin(_sequenceBoard != null ? _sequenceBoard.transform : null, "Sequence board", report);
            WarnIfAtParentOrigin(_checkpointA01 != null ? _checkpointA01.transform : null, "Checkpoint A01", report);
            WarnIfAtParentOrigin(_checkpointA02 != null ? _checkpointA02.transform : null, "Checkpoint A02", report);

            DetectIdenticalPositions(
                new[]
                {
                    _childrenGatherClue != null ? _childrenGatherClue.transform : null,
                    _storybookOpenedClue != null ? _storybookOpenedClue.transform : null,
                    _captionRepairedClue != null ? _captionRepairedClue.transform : null
                },
                "Area 2 clues",
                report);

            if (_fragment1 != null && _damagedStorybook != null)
            {
                float distance = Vector3.Distance(_fragment1.transform.position, _damagedStorybook.transform.position);
                if (distance > 8f)
                {
                    report.Warnings.Add("Fragment 1 is farther than 8 units from the storybook.");
                }
            }

            if (_fragment2 != null && _sequenceBoard != null)
            {
                float distance = Vector3.Distance(_fragment2.transform.position, _sequenceBoard.transform.position);
                if (distance > 10f)
                {
                    report.Warnings.Add("Fragment 2 is farther than 10 units from the sequence board.");
                }
            }

            if (_area2Entry != null && _gate1 != null)
            {
                Vector3 entry = _area2Entry.transform.position;
                Vector3 gate = _gate1.transform.position;
                if (Vector3.Distance(entry, gate) < 0.5f)
                {
                    report.Warnings.Add("Area 2 entry trigger overlaps Gate 1.");
                }
            }
        }

        private void ValidatePlacementMarkers(MissionValidationReport report)
        {
            MissionPlacementRequired[] markers = FindObjectsByType<MissionPlacementRequired>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (MissionPlacementRequired marker in markers)
            {
                if (marker == null || marker.IsConfirmed)
                {
                    continue;
                }

                string message = string.IsNullOrWhiteSpace(marker.Instruction)
                    ? marker.gameObject.name + " requires manual placement."
                    : marker.Instruction;
                report.ManualPlacementRequired.Add(message);
            }
        }

        private static void Require(UnityEngine.Object target, string label, MissionValidationReport report)
        {
            if (target == null)
            {
                report.Errors.Add("Missing reference: " + label);
            }
        }

        private static void ValidateInteractableId(
            WorldInteractableBase interactable,
            string label,
            string expectedId,
            Dictionary<string, string> seen,
            MissionValidationReport report)
        {
            if (interactable == null)
            {
                return;
            }

            string id = interactable.InteractionId;
            if (string.IsNullOrWhiteSpace(id))
            {
                report.Errors.Add(label + " has an empty interaction id.");
                return;
            }

            if (!string.Equals(id, expectedId, StringComparison.Ordinal))
            {
                report.Errors.Add(label + " interaction id must be " + expectedId);
            }

            if (seen.TryGetValue(id, out string existing))
            {
                report.Errors.Add("Duplicate interaction id '" + id + "' on " + label + " and " + existing);
            }
            else
            {
                seen.Add(id, label);
            }
        }

        private static void ValidateClueId(
            EvidenceClueInteractable clue,
            string label,
            string expectedId,
            string expectedObjectName,
            Dictionary<string, string> seen,
            MissionValidationReport report)
        {
            if (clue == null)
            {
                return;
            }

            EvidenceClueInteractable[] cluesOnObject = clue.GetComponents<EvidenceClueInteractable>();
            if (cluesOnObject != null && cluesOnObject.Length > 1)
            {
                report.Errors.Add(clue.gameObject.name + " has more than one EvidenceClueInteractable.");
            }

            if (!string.IsNullOrEmpty(expectedObjectName)
                && !string.Equals(clue.gameObject.name, expectedObjectName, StringComparison.Ordinal))
            {
                report.Warnings.Add(
                    label + " is on '" + clue.gameObject.name
                    + "' but expected object name '" + expectedObjectName + "'.");
            }

            string id = clue.ClueId;
            if (string.IsNullOrWhiteSpace(id))
            {
                report.Errors.Add(label + " has an empty clue id.");
                return;
            }

            if (!string.Equals(id, expectedId, StringComparison.Ordinal))
            {
                report.Errors.Add(
                    clue.gameObject.name + " has clue ID " + id
                    + " but expected " + expectedId + ".");
            }

            if (string.IsNullOrWhiteSpace(clue.InteractionId))
            {
                report.Errors.Add(label + " has an empty interaction id.");
            }
            else if (!string.Equals(clue.InteractionId, expectedId, StringComparison.Ordinal))
            {
                report.Errors.Add(
                    clue.gameObject.name + " has interaction ID " + clue.InteractionId
                    + " but expected " + expectedId + ".");
            }

            if (clue.FocusPoint == null)
            {
                report.Errors.Add(label + " is missing a focus or interaction point.");
            }

            if (seen.TryGetValue(id, out string existing))
            {
                report.Errors.Add("Duplicate clue id '" + id + "' on " + label + " and " + existing);
            }
            else
            {
                seen.Add(id, label);
            }
        }

        private static void ValidateInteractableTrigger(
            WorldInteractableBase interactable,
            string label,
            MissionValidationReport report)
        {
            if (interactable == null)
            {
                return;
            }

            Collider[] colliders = interactable.GetComponentsInChildren<Collider>(true);
            Collider trigger = null;
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null && colliders[i].isTrigger)
                {
                    trigger = colliders[i];
                    break;
                }
            }

            ValidateTriggerCollider(trigger, label, report);

            if (interactable is EvidenceClueInteractable clue
                && (string.Equals(clue.ClueId, MissionContentIds.ClueOpeningIllustration, System.StringComparison.Ordinal)
                    || string.Equals(clue.ClueId, MissionContentIds.ClueSurvivingLines, System.StringComparison.Ordinal)))
            {
                ValidateArea1ClueTriggerSize(interactable, label, report);
            }
        }

        private static void ValidateArea1ClueTriggerSize(
            WorldInteractableBase interactable,
            string label,
            MissionValidationReport report)
        {
            SphereCollider sphere = interactable.GetComponent<SphereCollider>();
            if (sphere == null || !sphere.isTrigger)
            {
                return;
            }

            float worldRadius = sphere.radius * MaxAbsAxis(interactable.transform.lossyScale);
            if (worldRadius < 0.3f || worldRadius > 0.75f)
            {
                report.Warnings.Add(
                    label + " trigger world radius is " + worldRadius.ToString("0.00")
                    + " m; recommended range is 0.35–0.65 m for book-page clues.");
            }
        }

        private static float MaxAbsAxis(Vector3 scale)
        {
            return Mathf.Max(Mathf.Abs(scale.x), Mathf.Max(Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
        }

        private static void ValidateFragmentTrigger(
            StoryFragmentCollectible fragment,
            string label,
            MissionValidationReport report)
        {
            if (fragment == null)
            {
                return;
            }

            ValidateTriggerCollider(fragment.TriggerCollider, label, report);
        }

        private static void ValidateTriggerCollider(Collider collider, string label, MissionValidationReport report)
        {
            if (collider == null)
            {
                report.Errors.Add(label + " is missing a trigger collider.");
                return;
            }

            if (!collider.isTrigger)
            {
                report.Errors.Add(label + " collider must use isTrigger = true.");
            }
        }

        private static void ValidateGateBlocker(AreaGateController gate, string label, MissionValidationReport report)
        {
            if (gate == null)
            {
                return;
            }

            Collider blocker = gate.GetComponentInChildren<Collider>();
            if (blocker == null)
            {
                report.Errors.Add(label + " is missing a blocker collider.");
                return;
            }

            if (blocker.isTrigger)
            {
                report.Errors.Add(label + " blocker must not be a trigger.");
            }

            if (blocker is BoxCollider box && (box.size.x <= 0.01f || box.size.y <= 0.01f || box.size.z <= 0.01f))
            {
                report.Errors.Add(label + " blocker dimensions are zero or near-zero.");
            }
        }

        private static void ValidateFragmentStartState(
            StoryFragmentCollectible fragment,
            string label,
            MissionValidationReport report)
        {
            if (fragment == null)
            {
                return;
            }

            if (!fragment.gameObject.activeSelf)
            {
                report.Errors.Add(label + " host must remain active while hidden.");
            }

            if (fragment.IsRevealed)
            {
                report.Errors.Add(label + " must start unrevealed.");
            }

            if (fragment.IsCollected)
            {
                report.Errors.Add(label + " must start uncollected.");
            }

            if (fragment.VisualRoot != null && fragment.VisualRoot.activeSelf)
            {
                report.Errors.Add(label + " visual root must start hidden.");
            }

            if (fragment.TriggerCollider != null && fragment.TriggerCollider.enabled)
            {
                report.Errors.Add(label + " trigger collider must start disabled.");
            }
        }

        private static void WarnIfAtParentOrigin(Transform target, string label, MissionValidationReport report)
        {
            if (target == null || target.parent == null)
            {
                return;
            }

            if (target.localPosition.sqrMagnitude < 0.0001f)
            {
                report.Warnings.Add(label + " is at its parent origin and may need manual placement.");
            }
        }

        private static void DetectIdenticalPositions(Transform[] targets, string groupLabel, MissionValidationReport report)
        {
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] == null)
                {
                    continue;
                }

                for (int j = i + 1; j < targets.Length; j++)
                {
                    if (targets[j] == null)
                    {
                        continue;
                    }

                    if (Vector3.Distance(targets[i].position, targets[j].position) < 0.05f)
                    {
                        report.Warnings.Add(groupLabel + " have identical or nearly identical positions.");
                        return;
                    }
                }
            }
        }
    }
}
