using System.Collections.Generic;
using NUnit.Framework;
using NutriMind.Gameplay.Runtime;

namespace NutriMind.Tests.EditMode.GameplayRuntime
{
    [TestFixture]
    public sealed class EventSequenceValidatorTests
    {
        [Test]
        public void CorrectOrder_Succeeds()
        {
            var slots = new List<string>
            {
                MissionContentIds.EventSequenceCardIds[0],
                MissionContentIds.EventSequenceCardIds[1],
                MissionContentIds.EventSequenceCardIds[2]
            };

            Assert.That(EventSequenceValidator.IsCorrectOrder(slots), Is.True);
        }

        [Test]
        public void IncorrectOrder_Fails()
        {
            var slots = new List<string>
            {
                MissionContentIds.EventSequenceCardIds[1],
                MissionContentIds.EventSequenceCardIds[0],
                MissionContentIds.EventSequenceCardIds[2]
            };

            Assert.That(EventSequenceValidator.IsCorrectOrder(slots), Is.False);
        }

        [Test]
        public void MissingSlot_CannotConfirm()
        {
            var slots = new List<string> { MissionContentIds.EventSequenceCardIds[0], string.Empty, string.Empty };
            Assert.That(EventSequenceValidator.CanConfirm(slots), Is.False);
        }

        [Test]
        public void ResetClearsAllSlots_WhenReplaced()
        {
            var slots = new List<string> { "a", "b", "c" };
            slots[0] = string.Empty;
            slots[1] = string.Empty;
            slots[2] = string.Empty;
            Assert.That(EventSequenceValidator.CanConfirm(slots), Is.False);
        }
    }
}
