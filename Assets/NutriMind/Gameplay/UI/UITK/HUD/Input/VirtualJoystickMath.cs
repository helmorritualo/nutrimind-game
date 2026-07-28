using UnityEngine;

namespace NutriMind.Gameplay.UI
{
    /// <summary>
    /// Pure math for virtual joystick displacement and normalization.
    /// </summary>
    public static class VirtualJoystickMath
    {
        /// <summary>
        /// Clamps a local offset vector to the configured joystick radius.
        /// </summary>
        public static Vector2 ClampToRadius(Vector2 localOffset, float radius)
        {
            if (radius <= 0f)
            {
                return Vector2.zero;
            }

            float sqrRadius = radius * radius;
            float sqrMagnitude = localOffset.sqrMagnitude;
            if (sqrMagnitude <= sqrRadius)
            {
                return localOffset;
            }

            float magnitude = Mathf.Sqrt(sqrMagnitude);
            return localOffset * (radius / magnitude);
        }

        /// <summary>
        /// Converts a clamped local offset into a normalized movement vector in [-1, 1].
        /// Values inside the dead zone return <see cref="Vector2.zero"/>.
        /// </summary>
        public static Vector2 CalculateNormalized(Vector2 localOffset, float radius, float deadZone)
        {
            if (radius <= 0f)
            {
                return Vector2.zero;
            }

            float magnitude = localOffset.magnitude;
            if (magnitude <= deadZone)
            {
                return Vector2.zero;
            }

            Vector2 clamped = ClampToRadius(localOffset, radius);
            return clamped / radius;
        }

        /// <summary>
        /// Converts a UI-normalized joystick vector (Y-down) into gameplay move input (Y-forward).
        /// </summary>
        public static Vector2 ToGameplayMove(Vector2 uiNormalized)
        {
            return new Vector2(uiNormalized.x, -uiNormalized.y);
        }
    }
}
