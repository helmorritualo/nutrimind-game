using System.Text;
using NutriMind.Gameplay.UI;
using UnityEngine;

namespace NutriMind.Gameplay.Runtime
{
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
            var builder = new StringBuilder();
            RequireReference(_missionJson, "Mission JSON", builder);
            RequireReference(_missionController, "MissionPrototypeController", builder);
            RequireReference(_uiCoordinator, "GameplayUiCoordinator", builder);
            RequireReference(_hudController, "GameplayStudentHudRuntimeController", builder);
            RequireReference(_overlayController, "GameplayLearningOverlayController", builder);
            RequireReference(_player, "Player", builder);
            RequireReference(_playerInteraction, "PlayerInteractionController", builder);
            RequireReference(_playerSpawn, "Player spawn", builder);
            RequireReference(_farmerLira, "Farmer Lira", builder);
            RequireReference(_damagedStorybook, "Damaged storybook", builder);
            RequireReference(_openingIllustrationClue, "Opening illustration clue", builder);
            RequireReference(_survivingLinesClue, "Surviving lines clue", builder);
            RequireReference(_captionBoard, "Caption board", builder);
            RequireReference(_area1WorldState, "Area 1 world state", builder);
            RequireReference(_fragment1, "Fragment 1", builder);
            RequireReference(_gate1, "Gate 1", builder);
            RequireReference(_checkpointA01, "Checkpoint A01", builder);
            RequireReference(_mina, "Mina", builder);
            RequireReference(_childrenGatherClue, "Children gather clue", builder);
            RequireReference(_storybookOpenedClue, "Storybook opened clue", builder);
            RequireReference(_captionRepairedClue, "Caption repaired clue", builder);
            RequireReference(_sequenceBoard, "Sequence board", builder);
            RequireReference(_area2WorldState, "Area 2 world state", builder);
            RequireReference(_fragment2, "Fragment 2", builder);
            RequireReference(_gate2, "Gate 2", builder);
            RequireReference(_checkpointA02, "Checkpoint A02", builder);

            if (_fragment1 != null && _fragment1.gameObject.activeInHierarchy)
            {
                builder.AppendLine("Fragment 1 should start inactive/hidden.");
            }

            if (_fragment2 != null && _fragment2.gameObject.activeInHierarchy)
            {
                builder.AppendLine("Fragment 2 should start inactive/hidden.");
            }

            if (_gate1 != null && _gate1.State == AreaGateState.Unlocked)
            {
                builder.AppendLine("Gate 1 should start locked.");
            }

            if (_gate2 != null && _gate2.State == AreaGateState.Unlocked)
            {
                builder.AppendLine("Gate 2 should start locked.");
            }

            error = builder.ToString().Trim();
            return string.IsNullOrEmpty(error);
        }

        private static void RequireReference(Object target, string label, StringBuilder builder)
        {
            if (target == null)
            {
                builder.AppendLine("Missing reference: " + label);
            }
        }
    }
}
