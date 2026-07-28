using System.Collections;
using NUnit.Framework;
using NutriMind.Gameplay.Runtime;
using NutriMind.Gameplay.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace NutriMind.Tests.PlayMode.GameplayRuntime
{
    public sealed class MissionPrototypeSmokeTests
    {
        private const string ScenePath =
            "Assets/NutriMind/Missions/Grade5/LiteraQuest/G5_LQ_T1_M01/Scenes/SCN_G5_LQ_T1_M01_TheFestivalStorybookRescue.unity";

        [UnityTest]
        public IEnumerator MissionScene_BootstrapStartsArea1()
        {
            yield return LoadMissionScene();
            yield return null;
            yield return null;

            MissionPrototypeController controller = Object.FindFirstObjectByType<MissionPrototypeController>();
            Assert.That(controller, Is.Not.Null, "MissionPrototypeController should exist in scene.");
            controller.Bootstrap();

            Assert.That(controller.Progress.CurrentStep, Is.EqualTo(MissionObjectiveStep.Area1_TalkToLira));
            Assert.That(controller.Progress.IsFragmentCollected(MissionContentIds.Fragment1), Is.False);
            Assert.That(controller.Progress.IsFragmentCollected(MissionContentIds.Fragment2), Is.False);
            Assert.That(controller.Progress.IsGateUnlocked(MissionContentIds.Gate1), Is.False);
            Assert.That(controller.Progress.IsGateUnlocked(MissionContentIds.Gate2), Is.False);

            GameplayStudentHudRuntimeController hud = Object.FindFirstObjectByType<GameplayStudentHudRuntimeController>();
            Assert.That(hud, Is.Not.Null);
            Assert.That(hud.View, Is.Not.Null);
            Assert.That(hud.View.IsBound, Is.True);

            StoryFragmentCollectible[] fragments =
                Object.FindObjectsByType<StoryFragmentCollectible>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.That(fragments.Length, Is.EqualTo(2));
            foreach (StoryFragmentCollectible fragment in fragments)
            {
                Assert.That(fragment.gameObject.activeSelf, Is.True, fragment.name + " host must stay active.");
                Assert.That(fragment.IsRevealed, Is.False);
            }
        }

        [UnityTest]
        [Timeout(120000)]
        public IEnumerator MissionScene_Areas1And2_CriticalProgressionPath()
        {
            yield return LoadMissionScene();
            yield return null;
            yield return null;

            MissionPrototypeController controller = Object.FindFirstObjectByType<MissionPrototypeController>();
            MissionSceneBindings bindings = Object.FindFirstObjectByType<MissionSceneBindings>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(bindings, Is.Not.Null);
            controller.Bootstrap();

            // Area 1 critical path through real fragment reveal/collection.
            controller.Progress.CurrentStep = MissionObjectiveStep.Area1_CollectFragment;
            bindings.Fragment1.SetRevealed(true);
            Assert.That(bindings.Fragment1.IsRevealed, Is.True);
            Assert.That(bindings.Fragment1.VisualRoot.activeSelf, Is.True);
            Assert.That(bindings.Fragment1.TriggerCollider.enabled, Is.True);
            Assert.That(bindings.Fragment1.TryCollect(), Is.True);
            Assert.That(bindings.Fragment1.TryCollect(), Is.False);

            Assert.That(controller.Progress.CollectedFragmentCount, Is.EqualTo(1));
            Assert.That(controller.Progress.CurrentAreaId, Is.EqualTo(MissionContentIds.Area1Id));
            Assert.That(controller.Progress.CurrentStep, Is.EqualTo(MissionObjectiveStep.Area1_Complete));
            Assert.That(controller.Progress.IsGateUnlocked(MissionContentIds.Gate1), Is.True);
            Assert.That(bindings.Gate1.State, Is.EqualTo(AreaGateState.Unlocked));

            GameplayStudentHudRuntimeController hud = Object.FindFirstObjectByType<GameplayStudentHudRuntimeController>();
            Assert.That(hud.CurrentModel.CollectedFragments, Is.EqualTo(1));
            Assert.That(hud.CurrentModel.ObjectiveText, Is.EqualTo("Continue to Banner Market Lane."));

            // Area 2 starts only after entry trigger path.
            controller.HandleAreaEntry(MissionContentIds.Area2Id);
            Assert.That(controller.Progress.CurrentAreaId, Is.EqualTo(MissionContentIds.Area2Id));
            Assert.That(controller.Progress.CurrentStep, Is.EqualTo(MissionObjectiveStep.Area2_TalkToMina));
            controller.HandleAreaEntry(MissionContentIds.Area2Id);
            Assert.That(controller.Progress.CurrentStep, Is.EqualTo(MissionObjectiveStep.Area2_TalkToMina));

            controller.Progress.CurrentStep = MissionObjectiveStep.Area2_CollectFragment;
            bindings.Fragment2.SetRevealed(true);
            Assert.That(bindings.Fragment2.TryCollect(), Is.True);
            Assert.That(bindings.Fragment2.TryCollect(), Is.False);

            Assert.That(controller.Progress.CollectedFragmentCount, Is.EqualTo(2));
            Assert.That(controller.Progress.IsGateUnlocked(MissionContentIds.Gate2), Is.True);
            Assert.That(bindings.Gate2.State, Is.EqualTo(AreaGateState.Unlocked));
            Assert.That(controller.Progress.CurrentStep, Is.EqualTo(MissionObjectiveStep.Area2_Complete));
            Assert.That(controller.Progress.CurrentAreaId, Is.EqualTo(MissionContentIds.Area2Id));
            Assert.That(hud.CurrentModel.CollectedFragments, Is.EqualTo(2));
            Assert.That(hud.CurrentModel.TotalFragments, Is.EqualTo(3));
            Assert.That(hud.CurrentModel.ObjectiveText, Is.EqualTo("Continue to Chronicle Courtyard."));

            // Overlay input gating smoke.
            GameplayUiCoordinator coordinator = Object.FindFirstObjectByType<GameplayUiCoordinator>();
            Assert.That(coordinator, Is.Not.Null);
            Assert.That(bindings.PlayerInteraction, Is.Not.Null);
            coordinator.SetGameplayInputEnabled(false);
            Assert.That(bindings.PlayerInteraction.InteractionEnabled, Is.False);
            coordinator.SetGameplayInputEnabled(true);
            Assert.That(bindings.PlayerInteraction.InteractionEnabled, Is.True);
        }

        private static IEnumerator LoadMissionScene()
        {
#if UNITY_EDITOR
            EditorSceneManager.LoadSceneInPlayMode(
                ScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
#else
            Assert.Fail("Mission PlayMode smoke tests require the Unity Editor LoadSceneInPlayMode path.");
            yield break;
#endif
        }
    }
}
