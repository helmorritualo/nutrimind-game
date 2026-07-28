using System.Collections;
using NUnit.Framework;
using NutriMind.Gameplay.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace NutriMind.Tests.PlayMode.GameplayRuntime
{
    public sealed class MissionPrototypeSmokeTests
    {
        private const string ScenePath =
            "Assets/NutriMind/Missions/Grade5/LiteraQuest/G5_LQ_T1_M01/Scenes/SCN_G5_LQ_T1_M01_TheFestivalStorybookRescue.unity";

        [UnityTest]
        public IEnumerator MissionScene_BootstrapStartsArea1()
        {
            yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(ScenePath);
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
        }
    }
}
