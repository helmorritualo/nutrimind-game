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
        public void FragmentCount_AcceptsOnlyAuthoredFragmentIds()
        {
            _store.MarkFragmentCollected(MissionContentIds.Fragment1);
            _store.MarkFragmentCollected(MissionContentIds.Fragment2);
            _store.MarkFragmentCollected(MissionContentIds.Fragment3);
            _store.MarkFragmentCollected("extra_fragment");
            _store.MarkFragmentCollected("another_unknown");

            Assert.That(_store.CollectedFragmentCount, Is.EqualTo(3));
            Assert.That(_store.IsFragmentCollected("extra_fragment"), Is.False);
            Assert.That(_store.IsFragmentCollected(MissionContentIds.Fragment3), Is.True);
        }
    }
}
