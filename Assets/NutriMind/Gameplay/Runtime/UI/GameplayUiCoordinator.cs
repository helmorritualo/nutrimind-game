using System;
using NutriMind.Gameplay.UI;
using UnityEngine;

namespace NutriMind.Gameplay.Runtime
{
    public sealed class GameplayUiCoordinator : MonoBehaviour
    {
        private GameplayStudentHudRuntimeController _hud;
        private GameplayLearningOverlayController _overlay;
        private IGameplayPlayerInput _playerInput;
        private PlayerInteractionController _interactionController;
        private bool _overlayBlocking;

        public bool IsOverlayBlocking => _overlayBlocking;

        public void Initialize(
            GameplayStudentHudRuntimeController hud,
            GameplayLearningOverlayController overlay,
            IGameplayPlayerInput playerInput,
            PlayerInteractionController interactionController)
        {
            _hud = hud;
            _overlay = overlay;
            _playerInput = playerInput;
            _interactionController = interactionController;

            if (_overlay != null)
            {
                _overlay.OverlayOpened += OnOverlayOpened;
                _overlay.OverlayClosed += OnOverlayClosed;
            }

            if (_interactionController != null)
            {
                _interactionController.FocusChanged += OnFocusChanged;
            }
        }

        private void OnDestroy()
        {
            if (_overlay != null)
            {
                _overlay.OverlayOpened -= OnOverlayOpened;
                _overlay.OverlayClosed -= OnOverlayClosed;
            }

            if (_interactionController != null)
            {
                _interactionController.FocusChanged -= OnFocusChanged;
            }
        }

        public void SetObjective(string areaPhase, string missionTitle, string objective)
        {
            _hud?.SetObjective(areaPhase, missionTitle, objective);
        }

        public void SetFragmentProgress(int collected, int total)
        {
            _hud?.SetFragmentProgress(collected, total);
        }

        public void SetInteractionPrompt(string label, string iconClass, bool available)
        {
            if (_overlayBlocking)
            {
                _hud?.SetInteraction(string.Empty, iconClass, false);
                return;
            }

            _hud?.SetInteraction(label, iconClass, available);
        }

        public void SetGameplayInputEnabled(bool enabled)
        {
            _hud?.SetInputEnabled(enabled);
            _playerInput?.SetGameplayInputEnabled(enabled);
            if (!enabled)
            {
                _hud?.ResetTouchControls();
                _playerInput?.ResetInput();
            }
        }

        private void OnOverlayOpened()
        {
            _overlayBlocking = true;
            SetGameplayInputEnabled(false);
            SetInteractionPrompt(string.Empty, GameplayStudentHudViewModel.DefaultInteractionIconClass, false);
        }

        private void OnOverlayClosed()
        {
            _overlayBlocking = false;
            SetGameplayInputEnabled(true);
            RefreshInteractionPrompt();
        }

        private void OnFocusChanged(IWorldInteractable target)
        {
            RefreshInteractionPrompt();
        }

        public void RefreshInteractionPrompt()
        {
            if (_overlayBlocking)
            {
                SetInteractionPrompt(string.Empty, GameplayStudentHudViewModel.DefaultInteractionIconClass, false);
                return;
            }

            IWorldInteractable target = _interactionController?.FocusedTarget;
            if (target == null || !target.CanInteract)
            {
                SetInteractionPrompt(string.Empty, GameplayStudentHudViewModel.DefaultInteractionIconClass, false);
                return;
            }

            SetInteractionPrompt(target.PromptLabel, target.IconClass, true);
        }
    }
}
