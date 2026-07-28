using NutriMind.Gameplay.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace NutriMind.Gameplay.Runtime
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class GameplayStudentHudRuntimeController : MonoBehaviour
    {
        [SerializeField] private UIDocument _uiDocument;

        private GameplayStudentHudView _view;
        private IGameplayPlayerInput _playerInput;
        private PlayerInteractionController _interactionController;
        private GameplayStudentHudViewModel _currentModel = CreateDefaultModel();
        private bool _hasRuntimeModel;

        public GameplayStudentHudView View => _view;
        public GameplayStudentHudViewModel CurrentModel => _currentModel;

        public void Initialize(IGameplayPlayerInput playerInput, PlayerInteractionController interactionController)
        {
            _playerInput = playerInput;
            _interactionController = interactionController;
            Bind();
        }

        private void OnEnable()
        {
            Bind();
        }

        private void OnDisable()
        {
            Unbind();
        }

        private void Bind()
        {
            if (_view != null)
            {
                return;
            }

            if (_uiDocument == null)
            {
                _uiDocument = GetComponent<UIDocument>();
            }

            if (_uiDocument == null || _uiDocument.rootVisualElement == null)
            {
                return;
            }

            _view = new GameplayStudentHudView(_uiDocument.rootVisualElement);
            _view.MoveChanged += OnMoveChanged;
            _view.LookDeltaChanged += OnLookDeltaChanged;
            _view.InteractionRequested += OnInteractionRequested;
            _view.PauseRequested += OnPauseRequested;

            if (!_hasRuntimeModel)
            {
                _currentModel = CreateDefaultModel();
            }

            _view.SetViewModel(_currentModel);
        }

        private void Unbind()
        {
            if (_view == null)
            {
                return;
            }

            _view.MoveChanged -= OnMoveChanged;
            _view.LookDeltaChanged -= OnLookDeltaChanged;
            _view.InteractionRequested -= OnInteractionRequested;
            _view.PauseRequested -= OnPauseRequested;
            _view.Dispose();
            _view = null;
        }

        public void SetObjective(string areaPhase, string missionTitle, string objective)
        {
            _hasRuntimeModel = true;
            _currentModel.AreaPhaseLabel = areaPhase ?? string.Empty;
            _currentModel.MissionTitle = missionTitle ?? string.Empty;
            _currentModel.ObjectiveText = objective ?? string.Empty;
            ApplyCurrentModel();
        }

        public void SetFragmentProgress(int collected, int total)
        {
            _hasRuntimeModel = true;
            _currentModel.CollectedFragments = collected;
            _currentModel.TotalFragments = total;
            ApplyCurrentModel();
        }

        public void SetInteraction(string label, string iconClass, bool available)
        {
            _hasRuntimeModel = true;
            _currentModel.InteractionLabel = label ?? string.Empty;
            _currentModel.InteractionIconClass = string.IsNullOrEmpty(iconClass)
                ? GameplayStudentHudViewModel.DefaultInteractionIconClass
                : iconClass;
            _currentModel.InteractionAvailable = available;
            ApplyCurrentModel();
        }

        public void SetInputEnabled(bool enabled)
        {
            _hasRuntimeModel = true;
            _currentModel.InputEnabled = enabled;
            ApplyCurrentModel();
        }

        public void SetLookHelperVisible(bool visible)
        {
            _hasRuntimeModel = true;
            _currentModel.ShowLookHelper = visible;
            ApplyCurrentModel();
        }

        public void ResetTouchControls()
        {
            _view?.ResetTouchControls();
        }

        private void ApplyCurrentModel()
        {
            _view?.SetViewModel(_currentModel);
        }

        private static GameplayStudentHudViewModel CreateDefaultModel()
        {
            return new GameplayStudentHudViewModel
            {
                MissionTitle = "The Festival Storybook Rescue",
                AreaPhaseLabel = "Area 1 • Discover",
                ObjectiveText = "Talk to Farmer Lira beside the damaged storybook.",
                CollectedFragments = 0,
                TotalFragments = 3,
                InteractionAvailable = false,
                PauseAvailable = false,
                ShowLookHelper = true,
                InputEnabled = true
            };
        }

        private void OnMoveChanged(Vector2 move)
        {
            _playerInput?.SetMove(move);
        }

        private void OnLookDeltaChanged(Vector2 delta)
        {
            _playerInput?.AddLookDelta(delta);
        }

        private void OnInteractionRequested()
        {
            _interactionController?.InteractWithFocusedTarget();
        }

        private void OnPauseRequested()
        {
            // Pause overlay is out of scope for this prototype.
        }
    }
}
