using UnityEngine;

namespace NutriMind.Gameplay.Runtime
{
    public interface IGameplayPlayerInput
    {
        void SetMove(Vector2 value);
        void AddLookDelta(Vector2 value);
        void SetGameplayInputEnabled(bool enabled);
        void ResetInput();
    }
}
