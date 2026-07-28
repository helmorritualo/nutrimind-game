using System.Collections.Generic;
using NUnit.Framework;
using NutriMind.Gameplay.Runtime;
using NutriMind.Gameplay.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace NutriMind.Tests.EditMode.GameplayRuntime
{
    [TestFixture]
    public sealed class StoryFragmentCollectibleTests
    {
        private readonly List<GameObject> _created = new List<GameObject>();

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
        public void HostStaysActiveWhileHidden()
        {
            StoryFragmentCollectible fragment = CreateFragment(MissionContentIds.Fragment1);
            fragment.SetRevealed(false);

            Assert.That(fragment.gameObject.activeSelf, Is.True);
            Assert.That(fragment.VisualRoot.activeSelf, Is.False);
            Assert.That(fragment.TriggerCollider.enabled, Is.False);
            Assert.That(fragment.IsRevealed, Is.False);
        }

        [Test]
        public void SetRevealedTrue_EnablesVisualAndCollider()
        {
            StoryFragmentCollectible fragment = CreateFragment(MissionContentIds.Fragment1);
            fragment.gameObject.SetActive(false);

            fragment.SetRevealed(true);

            Assert.That(fragment.gameObject.activeSelf, Is.True);
            Assert.That(fragment.VisualRoot.activeSelf, Is.True);
            Assert.That(fragment.TriggerCollider.enabled, Is.True);
            Assert.That(fragment.IsRevealed, Is.True);
        }

        [Test]
        public void RevealedFragment_CanTryCollect()
        {
            StoryFragmentCollectible fragment = CreateFragment(MissionContentIds.Fragment1);
            fragment.SetRevealed(true);

            Assert.That(fragment.TryCollect(), Is.True);
            Assert.That(fragment.IsCollected, Is.True);
            Assert.That(fragment.VisualRoot.activeSelf, Is.False);
            Assert.That(fragment.TriggerCollider.enabled, Is.False);
            Assert.That(fragment.gameObject.activeSelf, Is.True);
        }

        [Test]
        public void DuplicateCollection_IsIgnored()
        {
            StoryFragmentCollectible fragment = CreateFragment(MissionContentIds.Fragment1);
            fragment.SetRevealed(true);
            Assert.That(fragment.TryCollect(), Is.True);
            Assert.That(fragment.TryCollect(), Is.False);
        }

        [Test]
        public void CollectFragment1_CallsMissionProgressionOnce()
        {
            MissionPrototypeController controller = CreateControllerWithBindings(out StoryFragmentCollectible fragment1, out _);

            fragment1.Initialize(controller);
            fragment1.SetRevealed(true);
            Assert.That(fragment1.TryCollect(), Is.True);
            Assert.That(fragment1.TryCollect(), Is.False);

            Assert.That(controller.Progress.IsFragmentCollected(MissionContentIds.Fragment1), Is.True);
            Assert.That(controller.Progress.CurrentStep, Is.EqualTo(MissionObjectiveStep.Area1_Complete));
            Assert.That(controller.Progress.CurrentAreaId, Is.EqualTo(MissionContentIds.Area1Id));
            Assert.That(controller.Progress.IsGateUnlocked(MissionContentIds.Gate1), Is.True);
        }

        [Test]
        public void CollectFragment2_CallsMissionProgressionOnce()
        {
            MissionPrototypeController controller = CreateControllerWithBindings(out _, out StoryFragmentCollectible fragment2);
            controller.Progress.CurrentStep = MissionObjectiveStep.Area2_CollectFragment;
            controller.Progress.CurrentAreaId = MissionContentIds.Area2Id;

            int eventCalls = 0;
            fragment2.Initialize(controller);
            fragment2.Collected += _ => eventCalls++;
            fragment2.SetRevealed(true);
            Assert.That(fragment2.TryCollect(), Is.True);
            Assert.That(fragment2.TryCollect(), Is.False);

            Assert.That(eventCalls, Is.EqualTo(1));
            Assert.That(controller.Progress.IsFragmentCollected(MissionContentIds.Fragment2), Is.True);
            Assert.That(controller.Progress.IsGateUnlocked(MissionContentIds.Gate2), Is.True);
        }

        private StoryFragmentCollectible CreateFragment(string id)
        {
            var host = new GameObject("FragmentHost");
            _created.Add(host);
            var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "FragmentVisual";
            visual.transform.SetParent(host.transform, false);
            Object.DestroyImmediate(visual.GetComponent<Collider>());

            SphereCollider trigger = host.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            StoryFragmentCollectible fragment = host.AddComponent<StoryFragmentCollectible>();

            SetPrivate(fragment, "_collectibleId", id);
            SetPrivate(fragment, "_triggerCollider", trigger);
            SetPrivate(fragment, "_visualRoot", visual);
            fragment.SetRevealed(false);
            return fragment;
        }

        private MissionPrototypeController CreateControllerWithBindings(
            out StoryFragmentCollectible fragment1,
            out StoryFragmentCollectible fragment2)
        {
            var root = new GameObject("MissionRoot");
            _created.Add(root);

            fragment1 = CreateFragment(MissionContentIds.Fragment1);
            fragment2 = CreateFragment(MissionContentIds.Fragment2);

            var gate1Go = new GameObject("Gate1");
            _created.Add(gate1Go);
            AreaGateController gate1 = gate1Go.AddComponent<AreaGateController>();
            SetPrivate(gate1, "_gateId", MissionContentIds.Gate1);
            SetPrivate(gate1, "_blockerCollider", gate1Go.AddComponent<BoxCollider>());

            var gate2Go = new GameObject("Gate2");
            _created.Add(gate2Go);
            AreaGateController gate2 = gate2Go.AddComponent<AreaGateController>();
            SetPrivate(gate2, "_gateId", MissionContentIds.Gate2);
            SetPrivate(gate2, "_blockerCollider", gate2Go.AddComponent<BoxCollider>());

            MissionSceneBindings bindings = root.AddComponent<MissionSceneBindings>();
            MissionPrototypeController controller = root.AddComponent<MissionPrototypeController>();
            SetPrivate(bindings, "_missionController", controller);
            SetPrivate(bindings, "_fragment1", fragment1);
            SetPrivate(bindings, "_fragment2", fragment2);
            SetPrivate(bindings, "_gate1", gate1);
            SetPrivate(bindings, "_gate2", gate2);
            SetPrivate(controller, "_bindings", bindings);
            return controller;
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
