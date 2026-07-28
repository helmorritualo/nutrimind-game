using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using NutriMind.Gameplay.UI;

namespace NutriMind.Tests.EditMode.GameplayUI
{
    [TestFixture]
    public sealed class VirtualJoystickMathTests
    {
        [Test]
        public void ClampToRadius_KeepsOffsetInsideRadius()
        {
            Vector2 offset = new Vector2(3f, 4f);
            Vector2 clamped = VirtualJoystickMath.ClampToRadius(offset, 10f);
            Assert.That(clamped, Is.EqualTo(offset));
        }

        [Test]
        public void ClampToRadius_ClipsOutsideRadius()
        {
            Vector2 offset = new Vector2(10f, 0f);
            Vector2 clamped = VirtualJoystickMath.ClampToRadius(offset, 5f);
            Assert.That(clamped.x, Is.EqualTo(5f).Within(0.001f));
            Assert.That(clamped.y, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void CalculateNormalized_ReturnsZeroInsideDeadZone()
        {
            Vector2 normalized = VirtualJoystickMath.CalculateNormalized(new Vector2(2f, 0f), 10f, 5f);
            Assert.That(normalized, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void CalculateNormalized_ReturnsUnitVectorAtEdge()
        {
            Vector2 normalized = VirtualJoystickMath.CalculateNormalized(new Vector2(10f, 0f), 10f, 1f);
            Assert.That(normalized.x, Is.EqualTo(1f).Within(0.001f));
            Assert.That(normalized.y, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void CalculateNormalized_ClampThenNormalizeForDiagonalOverflow()
        {
            Vector2 normalized = VirtualJoystickMath.CalculateNormalized(new Vector2(10f, 10f), 10f, 0f);
            float magnitude = normalized.magnitude;
            Assert.That(magnitude, Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void ToGameplayMove_FlipsVerticalAxisForForwardInput()
        {
            Vector2 gameplayMove = VirtualJoystickMath.ToGameplayMove(new Vector2(0f, -1f));
            Assert.That(gameplayMove.x, Is.EqualTo(0f).Within(0.001f));
            Assert.That(gameplayMove.y, Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void CalculateNormalized_ReturnsZeroWhenRadiusInvalid()
        {
            Assert.That(
                VirtualJoystickMath.CalculateNormalized(new Vector2(4f, 2f), 0f, 0f),
                Is.EqualTo(Vector2.zero));
        }
    }
}
