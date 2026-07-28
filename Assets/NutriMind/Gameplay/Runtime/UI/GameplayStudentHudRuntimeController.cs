using System;
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

        public GameplayStudentHudView View => _view;

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

            var model = new GameplayStudentHudViewModel
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
            _view.SetViewModel(model);
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
            _view?.SetObjective(areaPhase, missionTitle, objective);
        }

        public void SetFragmentProgress(int collected, int total)
        {
            _view?.SetFragmentProgress(collected, total);
        }

        public void SetInteraction(string label, string iconClass, bool available)
        {
            _view?.SetInteraction(label, iconClass, available);
        }

        public void SetInputEnabled(bool enabled)
        {
            _view?.SetInputEnabled(enabled);
        }

        public void SetLookHelperVisible(bool visible)
        {
            _view?.SetLookHelperVisible(visible);
        }

        public void ResetTouchControls()
        {
            _view?.ResetTouchControls();
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
