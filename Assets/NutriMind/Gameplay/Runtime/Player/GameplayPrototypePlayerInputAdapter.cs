using UnityEngine;

namespace NutriMind.Gameplay.Runtime
{
    /// <summary>
    /// Forwards HUD/runtime input to an existing <see cref="GameplayPrototypePlayerController"/>
    /// without rewriting or replacing the player controller.
    /// </summary>
    public sealed class GameplayPrototypePlayerInputAdapter : MonoBehaviour, IGameplayPlayerInput
    {
        [SerializeField] private GameplayPrototypePlayerController _controller;

        public GameplayPrototypePlayerController Controller => _controller;

        public void Bind(GameplayPrototypePlayerController controller)
        {
            _controller = controller;
        }

        public void SetMove(Vector2 value)
        {
            _controller?.SetMove(value);
        }

        public void AddLookDelta(Vector2 value)
        {
            _controller?.AddLookDelta(value);
        }

        public void SetGameplayInputEnabled(bool enabled)
        {
            _controller?.SetGameplayInputEnabled(enabled);
        }

        public void ResetInput()
        {
            _controller?.ResetInput();
        }
    }
}
