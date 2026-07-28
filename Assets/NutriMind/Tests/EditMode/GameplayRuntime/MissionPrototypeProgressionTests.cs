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
        public void TalkingToMina_AfterArea1Complete_EntersArea2EvenWithoutEntryTrigger()
        {
            NpcGuideInteractable mina = CreateNpc(MissionContentIds.MinaNpc);
            SetPrivate(_bindings, "_mina", mina);

            _controller.Progress.CurrentStep = MissionObjectiveStep.Area1_Complete;
            _controller.Progress.CurrentAreaId = MissionContentIds.Area1Id;

            // Without overlay content, HandleMinaTalk should still enter Area 2 before dialogue.
            // Dialogue may no-op without content; verify area transition from the failsafe path.
            _controller.HandleNpcInteraction(mina);

            Assert.That(_controller.Progress.CurrentAreaId, Is.EqualTo(MissionContentIds.Area2Id));
            Assert.That(_controller.Progress.CurrentStep, Is.EqualTo(MissionObjectiveStep.Area2_TalkToMina));
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

        [TestCase(true)]
        [TestCase(false)]
        public void Area1ClueProgress_SupportsEitherInspectionOrder_AndDoesNotRegress(bool openingFirst)
        {
            _controller.Progress.CurrentStep = MissionObjectiveStep.Area1_InspectOpeningIllustration;
            _controller.Progress.MarkInteractionCompleted(MissionContentIds.DamagedStorybook);

            if (openingFirst)
            {
                _controller.Progress.MarkClueInspected(MissionContentIds.ClueOpeningIllustration);
                InvokeArea1ClueProgress();
                Assert.That(
                    _controller.Progress.CurrentStep,
                    Is.EqualTo(MissionObjectiveStep.Area1_InspectSurvivingLines));

                _controller.Progress.MarkClueInspected(MissionContentIds.ClueSurvivingLines);
            }
            else
            {
                _controller.Progress.MarkClueInspected(MissionContentIds.ClueSurvivingLines);
                InvokeArea1ClueProgress();
                Assert.That(
                    _controller.Progress.CurrentStep,
                    Is.EqualTo(MissionObjectiveStep.Area1_InspectOpeningIllustration));

                _controller.Progress.MarkClueInspected(MissionContentIds.ClueOpeningIllustration);
            }

            InvokeArea1ClueProgress();
            Assert.That(
                _controller.Progress.CurrentStep,
                Is.EqualTo(MissionObjectiveStep.Area1_ResolveQuestions));

            MissionObjectiveStep[] laterSteps =
            {
                MissionObjectiveStep.Area1_ResolveQuestions,
                MissionObjectiveStep.Area1_RepairCaption,
                MissionObjectiveStep.Area1_CollectFragment,
                MissionObjectiveStep.Area1_Complete
            };

            foreach (MissionObjectiveStep step in laterSteps)
            {
                _controller.Progress.CurrentStep = step;
                InvokeArea1ClueProgress();
                Assert.That(
                    _controller.Progress.CurrentStep,
                    Is.EqualTo(step),
                    "Area 1 clue progress must not regress from " + step);
            }
        }

        [Test]
        public void Validation_ReportsError_WhenArea1CluesShareComponent()
        {
            var sharedHost = new GameObject("SharedClueHost");
            _created.Add(sharedHost);
            EvidenceClueInteractable shared = sharedHost.AddComponent<EvidenceClueInteractable>();
            SetPrivate(shared, "_clueId", MissionContentIds.ClueOpeningIllustration);
            SetPrivate(shared, "_interactionId", MissionContentIds.ClueOpeningIllustration);

            SetPrivate(_bindings, "_openingIllustrationClue", shared);
            SetPrivate(_bindings, "_survivingLinesClue", shared);

            MissionValidationReport report = _bindings.Validate();
            Assert.That(
                report.Errors,
                Has.Some.Contain("Opening Illustration Clue and Surviving Lines Clue reference the same component."));
        }

        [Test]
        public void Validation_ReportsError_WhenArea1CluesShareClueId()
        {
            EvidenceClueInteractable opening = CreateClue(
                "CluePoint01_OpeningIllustration",
                MissionContentIds.ClueOpeningIllustration);
            EvidenceClueInteractable surviving = CreateClue(
                "CluePoint02_SurvivingLines",
                MissionContentIds.ClueOpeningIllustration);

            SetPrivate(_bindings, "_openingIllustrationClue", opening);
            SetPrivate(_bindings, "_survivingLinesClue", surviving);

            MissionValidationReport report = _bindings.Validate();
            Assert.That(report.Errors, Has.Some.Contain("Duplicate clue id"));
        }

        private EvidenceClueInteractable CreateClue(string objectName, string clueId)
        {
            var host = new GameObject(objectName);
            _created.Add(host);
            var focus = new GameObject("InteractionPoint");
            focus.transform.SetParent(host.transform, false);
            SphereCollider trigger = host.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            EvidenceClueInteractable clue = host.AddComponent<EvidenceClueInteractable>();
            SetPrivate(clue, "_clueId", clueId);
            SetPrivate(clue, "_interactionId", clueId);
            SetPrivate(clue, "_focusPoint", focus.transform);
            return clue;
        }

        private NpcGuideInteractable CreateNpc(string interactionId)
        {
            var host = new GameObject(interactionId);
            _created.Add(host);
            var focus = new GameObject("InteractionPoint");
            focus.transform.SetParent(host.transform, false);
            SphereCollider trigger = host.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            NpcGuideInteractable npc = host.AddComponent<NpcGuideInteractable>();
            SetPrivate(npc, "_interactionId", interactionId);
            SetPrivate(npc, "_focusPoint", focus.transform);
            return npc;
        }

        private void InvokeArea1ClueProgress()
        {
            System.Reflection.MethodInfo method = typeof(MissionPrototypeController).GetMethod(
                "HandleArea1ClueProgress",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(_controller, null);
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
            System.Type type = target.GetType();
            while (type != null)
            {
                System.Reflection.FieldInfo field = type.GetField(
                    fieldName,
                    System.Reflection.BindingFlags.Instance
                    | System.Reflection.BindingFlags.NonPublic
                    | System.Reflection.BindingFlags.DeclaredOnly);
                if (field != null)
                {
                    field.SetValue(target, value);
                    return;
                }

                type = type.BaseType;
            }

            Assert.Fail("Missing field: " + fieldName);
        }
    }
}
