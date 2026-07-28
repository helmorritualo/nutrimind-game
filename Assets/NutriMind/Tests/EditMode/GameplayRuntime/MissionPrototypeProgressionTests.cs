using System.Collections.Generic;
using NUnit.Framework;
using NutriMind.Gameplay.Runtime;
using UnityEngine;

namespace NutriMind.Tests.EditMode.GameplayRuntime
{
    [TestFixture]
    public sealed class MissionPrototypeProgressionTests
    {
        private readonly List<GameObject> _created = new List<GameObject>();
        private MissionPrototypeController _controller;
        private MissionSceneBindings _bindings;
        private AreaGateController _gate1;
        private AreaGateController _gate2;
        private StoryFragmentCollectible _fragment1;
        private StoryFragmentCollectible _fragment2;

        [SetUp]
        public void SetUp()
        {
            var root = new GameObject("MissionRoot");
            _created.Add(root);
            _bindings = root.AddComponent<MissionSceneBindings>();
            _controller = root.AddComponent<MissionPrototypeController>();

            _fragment1 = CreateFragment(MissionContentIds.Fragment1);
            _fragment2 = CreateFragment(MissionContentIds.Fragment2);
            _gate1 = CreateGate(MissionContentIds.Gate1);
            _gate2 = CreateGate(MissionContentIds.Gate2);

            SetPrivate(_bindings, "_missionController", _controller);
            SetPrivate(_bindings, "_fragment1", _fragment1);
            SetPrivate(_bindings, "_fragment2", _fragment2);
            SetPrivate(_bindings, "_gate1", _gate1);
            SetPrivate(_bindings, "_gate2", _gate2);
            SetPrivate(_controller, "_bindings", _bindings);

            _fragment1.Initialize(_controller);
            _fragment2.Initialize(_controller);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _created)
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
            }

            _created.Clear();
        }

        [Test]
        public void Fragment1Collection_UnlocksGate1_KeepsArea1()
        {
            _controller.Progress.CurrentStep = MissionObjectiveStep.Area1_CollectFragment;
            _fragment1.SetRevealed(true);
            _fragment1.TryCollect();

            Assert.That(_controller.Progress.CollectedFragmentCount, Is.EqualTo(1));
            Assert.That(_controller.Progress.IsGateUnlocked(MissionContentIds.Gate1), Is.True);
            Assert.That(_gate1.State, Is.EqualTo(AreaGateState.Unlocked));
            Assert.That(_controller.Progress.CurrentStep, Is.EqualTo(MissionObjectiveStep.Area1_Complete));
            Assert.That(_controller.Progress.CurrentAreaId, Is.EqualTo(MissionContentIds.Area1Id));
        }

        [Test]
        public void Fragment1Collection_DoesNotEnterArea2()
        {
            _controller.Progress.CurrentStep = MissionObjectiveStep.Area1_CollectFragment;
            _controller.HandleFragmentCollected(MissionContentIds.Fragment1);

            Assert.That(_controller.Progress.CurrentAreaId, Is.EqualTo(MissionContentIds.Area1Id));
            Assert.That(_controller.Progress.CurrentStep, Is.EqualTo(MissionObjectiveStep.Area1_Complete));
        }

        [Test]
        public void CrossingArea2Entry_AfterArea1Complete_EntersArea2()
        {
            _controller.Progress.CurrentStep = MissionObjectiveStep.Area1_Complete;
            _controller.HandleAreaEntry(MissionContentIds.Area2Id);

            Assert.That(_controller.Progress.CurrentAreaId, Is.EqualTo(MissionContentIds.Area2Id));
            Assert.That(_controller.Progress.CurrentStep, Is.EqualTo(MissionObjectiveStep.Area2_TalkToMina));
        }

        [Test]
        public void RepeatedArea2Entry_IsIgnored()
        {
            _controller.Progress.CurrentStep = MissionObjectiveStep.Area1_Complete;
            _controller.HandleAreaEntry(MissionContentIds.Area2Id);
            _controller.Progress.CurrentStep = MissionObjectiveStep.Area2_FindClues;
            _controller.HandleAreaEntry(MissionContentIds.Area2Id);

            Assert.That(_controller.Progress.CurrentStep, Is.EqualTo(MissionObjectiveStep.Area2_FindClues));
        }

        [Test]
        public void Area2Entry_BeforeArea1Complete_IsIgnored()
        {
            _controller.Progress.CurrentStep = MissionObjectiveStep.Area1_CollectFragment;
            _controller.HandleAreaEntry(MissionContentIds.Area2Id);

            Assert.That(_controller.Progress.CurrentAreaId, Is.EqualTo(MissionContentIds.Area1Id));
        }

        [Test]
        public void UnknownFragmentIds_AreRejected()
        {
            _controller.HandleFragmentCollected("extra_fragment");
            _controller.HandleFragmentCollected(MissionContentIds.Fragment3);

            Assert.That(_controller.Progress.CollectedFragmentCount, Is.EqualTo(0));
        }

        [Test]
        public void Fragment1Collection_IsIdempotent()
        {
            _controller.HandleFragmentCollected(MissionContentIds.Fragment1);
            _controller.HandleFragmentCollected(MissionContentIds.Fragment1);

            Assert.That(_controller.Progress.CollectedFragmentCount, Is.EqualTo(1));
        }

        [Test]
        public void Fragment2Collection_UnlocksGate2()
        {
            _controller.Progress.CurrentAreaId = MissionContentIds.Area2Id;
            _controller.Progress.CurrentStep = MissionObjectiveStep.Area2_CollectFragment;
            _controller.HandleFragmentCollected(MissionContentIds.Fragment2);

            Assert.That(_controller.Progress.IsGateUnlocked(MissionContentIds.Gate2), Is.True);
            Assert.That(_controller.Progress.CurrentStep, Is.EqualTo(MissionObjectiveStep.Area2_Complete));
            Assert.That(_gate2.State, Is.EqualTo(AreaGateState.Unlocked));
        }

        private StoryFragmentCollectible CreateFragment(string id)
        {
            var host = new GameObject(id);
            _created.Add(host);
            var visual = new GameObject("FragmentVisual");
            visual.transform.SetParent(host.transform, false);
            SphereCollider trigger = host.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            StoryFragmentCollectible fragment = host.AddComponent<StoryFragmentCollectible>();
            SetPrivate(fragment, "_collectibleId", id);
            SetPrivate(fragment, "_triggerCollider", trigger);
            SetPrivate(fragment, "_visualRoot", visual);
            fragment.SetRevealed(false);
            return fragment;
        }

        private AreaGateController CreateGate(string gateId)
        {
            var go = new GameObject(gateId);
            _created.Add(go);
            BoxCollider blocker = go.AddComponent<BoxCollider>();
            blocker.isTrigger = false;
            AreaGateController gate = go.AddComponent<AreaGateController>();
            SetPrivate(gate, "_gateId", gateId);
            SetPrivate(gate, "_blockerCollider", blocker);
            gate.Lock();
            return gate;
        }

        private static void SetPrivate(object target, string fieldName, object value)
        {
            System.Reflection.FieldInfo field = target.GetType().GetField(
                fieldName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }
    }
}
