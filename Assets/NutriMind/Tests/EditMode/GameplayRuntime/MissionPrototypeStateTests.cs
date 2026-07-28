using NUnit.Framework;
using NutriMind.Gameplay.Runtime;

namespace NutriMind.Tests.EditMode.GameplayRuntime
{
    [TestFixture]
    public sealed class MissionPrototypeStateTests
    {
        private InMemoryMissionProgressStore _store;

        [SetUp]
        public void SetUp()
        {
            _store = new InMemoryMissionProgressStore();
        }

        [Test]
        public void InitialState_StartsAtArea1TalkToLira()
        {
            Assert.That(_store.CurrentStep, Is.EqualTo(MissionObjectiveStep.Area1_TalkToLira));
            Assert.That(_store.CurrentAreaId, Is.EqualTo(MissionContentIds.Area1Id));
            Assert.That(_store.CollectedFragmentCount, Is.EqualTo(0));
        }

        [Test]
        public void DuplicateClueInspection_IsIgnoredByStore()
        {
            _store.MarkClueInspected(MissionContentIds.ClueOpeningIllustration);
            _store.MarkClueInspected(MissionContentIds.ClueOpeningIllustration);
            Assert.That(_store.IsClueInspected(MissionContentIds.ClueOpeningIllustration), Is.True);
        }

        [Test]
        public void DuplicateFragmentCollection_IsIgnoredByStore()
        {
            _store.MarkFragmentCollected(MissionContentIds.Fragment1);
            _store.MarkFragmentCollected(MissionContentIds.Fragment1);
            Assert.That(_store.CollectedFragmentCount, Is.EqualTo(1));
        }

        [Test]
        public void Fragment1UnlocksGate1_WhenMarked()
        {
            _store.MarkFragmentCollected(MissionContentIds.Fragment1);
            _store.MarkGateUnlocked(MissionContentIds.Gate1);
            Assert.That(_store.IsGateUnlocked(MissionContentIds.Gate1), Is.True);
        }

        [Test]
        public void FragmentCount_RemainsClampedToThree()
        {
            _store.MarkFragmentCollected(MissionContentIds.Fragment1);
            _store.MarkFragmentCollected(MissionContentIds.Fragment2);
            _store.MarkFragmentCollected("extra_fragment");
            Assert.That(_store.CollectedFragmentCount, Is.LessThanOrEqualTo(3));
        }
    }
}
