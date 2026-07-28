using System.Collections.Generic;
using NUnit.Framework;
using NutriMind.Gameplay.Runtime;
using NutriMind.Gameplay.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace NutriMind.Tests.EditMode.GameplayRuntime
{
    [TestFixture]
    public sealed class GameplayInputAndHudRuntimeTests
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
        public void HorizontalVelocity_IsNotScaledIntoGravityAxis()
        {
            var go = new GameObject("Player");
            _created.Add(go);
            go.transform.rotation = Quaternion.identity;

            Vector3 horizontal = GameplayPrototypePlayerController.ComputeHorizontalVelocity(
                new Vector2(0f, 1f),
                go.transform,
                4.5f);

            Assert.That(horizontal.y, Is.EqualTo(0f));
            Assert.That(horizontal.z, Is.EqualTo(4.5f).Within(0.001f));
        }

        [Test]
        public void InteractionDisabled_BlocksInteractExecution()
        {
            var player = new GameObject("Player");
            _created.Add(player);
            PlayerInteractionController interaction = player.AddComponent<PlayerInteractionController>();
            interaction.SetInteractionEnabled(false);

            var targetGo = new GameObject("Target");
            _created.Add(targetGo);
            var spy = targetGo.AddComponent<SpyInteractable>();
            SetPrivate(interaction, "_focusedTarget", spy);

            interaction.InteractWithFocusedTarget();
            Assert.That(spy.InteractCount, Is.EqualTo(0));

            interaction.SetInteractionEnabled(true);
            SetPrivate(interaction, "_focusedTarget", spy);
            interaction.InteractWithFocusedTarget();
            Assert.That(spy.InteractCount, Is.EqualTo(1));
        }

        [Test]
        public void UiCoordinator_DisablesInteractionWhenOverlayBlocks()
        {
            var root = new GameObject("UiRoot");
            _created.Add(root);
            var playerGo = new GameObject("Player");
            _created.Add(playerGo);
            playerGo.AddComponent<CharacterController>();
            GameplayPrototypePlayerController player = playerGo.AddComponent<GameplayPrototypePlayerController>();
            PlayerInteractionController interaction = playerGo.AddComponent<PlayerInteractionController>();
            GameplayUiCoordinator coordinator = root.AddComponent<GameplayUiCoordinator>();

            coordinator.Initialize(null, null, player, interaction);
            coordinator.SetGameplayInputEnabled(false);

            Assert.That(interaction.InteractionEnabled, Is.False);
        }

        [Test]
        public void HudRuntime_RetainsPendingStateBeforeBind()
        {
            var host = new GameObject("HudHost");
            _created.Add(host);
            UIDocument document = host.AddComponent<UIDocument>();
            GameplayStudentHudRuntimeController hud = host.AddComponent<GameplayStudentHudRuntimeController>();

            hud.SetObjective("Area 1 • Discover", "Mission", "Continue to Banner Market Lane.");
            hud.SetFragmentProgress(1, 3);

            Assert.That(hud.CurrentModel.ObjectiveText, Is.EqualTo("Continue to Banner Market Lane."));
            Assert.That(hud.CurrentModel.CollectedFragments, Is.EqualTo(1));
            Assert.That(hud.View, Is.Null);

            // Binding without a visual tree is a no-op; pending model must still be retained.
            hud.SetFragmentProgress(2, 3);
            Assert.That(hud.CurrentModel.CollectedFragments, Is.EqualTo(2));
            Assert.That(document, Is.Not.Null);
        }

        [Test]
        public void OverlayLifecycle_OpensAndClosesOnce()
        {
            var host = new GameObject("OverlayHost");
            _created.Add(host);
            host.AddComponent<UIDocument>();
            GameplayLearningOverlayController overlay = host.AddComponent<GameplayLearningOverlayController>();

            int opened = 0;
            int closed = 0;
            overlay.OverlayOpened += () => opened++;
            overlay.OverlayClosed += () => closed++;

            // Without a bound visual tree, ApplyAndOpen still tracks open state.
            SetPrivate(overlay, "_isOpen", false);
            InvokePrivate(overlay, "ApplyAndOpen");
            InvokePrivate(overlay, "ApplyAndOpen");
            Assert.That(opened, Is.EqualTo(1));

            overlay.Hide();
            overlay.Hide();
            Assert.That(closed, Is.EqualTo(1));
            Assert.That(overlay.IsOpen, Is.False);
        }

        private static void SetPrivate(object target, string fieldName, object value)
        {
            System.Reflection.FieldInfo field = target.GetType().GetField(
                fieldName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }

        private static void InvokePrivate(object target, string methodName)
        {
            System.Reflection.MethodInfo method = target.GetType().GetMethod(
                methodName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, methodName);
            method.Invoke(target, null);
        }

        private sealed class SpyInteractable : WorldInteractableBase
        {
            public int InteractCount { get; private set; }

            protected override void OnInteract(WorldInteractionContext context)
            {
                InteractCount++;
            }
        }
    }
}
