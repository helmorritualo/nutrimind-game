using NUnit.Framework;
using UnityEditor;
using UnityEngine.UIElements;
using NutriMind.Gameplay.UI;

namespace NutriMind.Tests.EditMode.GameplayUI
{
    [TestFixture]
    public sealed class GameplayStudentHudViewTests
    {
        private const string UxmlPath = "Assets/NutriMind/Gameplay/UI/UITK/HUD/UXML/GameplayStudentHud.uxml";

        [Test]
        public void Constructor_ResolvesRootAndBindsRequiredElements()
        {
            TemplateContainer root = LoadHudTree();
            var view = new GameplayStudentHudView(root);

            Assert.That(view.IsBound, Is.True);
            Assert.That(view.Root, Is.Not.Null);
            Assert.That(view.Root.name, Is.EqualTo("gameplay-student-hud-root"));
            Assert.That(root.Q<Label>("area-phase-label"), Is.Not.Null);
            Assert.That(root.Q<Label>("objective-text-label"), Is.Not.Null);
            Assert.That(root.Q<Button>("interaction-button"), Is.Not.Null);
            Assert.That(root.Q<Button>("pause-button"), Is.Not.Null);
            Assert.That(root.Q<VisualElement>("movement-joystick"), Is.Not.Null);
            Assert.That(root.Q<VisualElement>("gameplay-look-zone"), Is.Not.Null);

            view.Dispose();
        }

        [Test]
        public void SetViewModel_AppliesDefaultPreviewCopy()
        {
            TemplateContainer root = LoadHudTree();
            var view = new GameplayStudentHudView(root);
            view.SetViewModel(GameplayStudentHudViewModel.CreateDefaultPreview());

            Assert.That(root.Q<Label>("mission-title-label").text,
                Is.EqualTo("The Festival Storybook Rescue"));
            Assert.That(root.Q<Label>("area-phase-label").text,
                Is.EqualTo("Area 1 • Discover"));
            Assert.That(root.Q<Label>("objective-text-label").text,
                Does.Contain("Inspect the damaged storybook"));
            Assert.That(root.Q<Label>("fragment-count-label").text, Is.EqualTo("0 / 3"));
            Assert.That(root.Q<Label>("interaction-label").text, Is.EqualTo("Inspect"));
            Assert.That(root.Q<Button>("interaction-button").enabledSelf, Is.True);
            Assert.That(root.Q<Label>("look-helper").ClassListContains("gameplay-student-hud__look-helper--hidden"),
                Is.False);

            view.Dispose();
        }

        [Test]
        public void SetFragmentProgress_ClampsCollectedToTotal()
        {
            TemplateContainer root = LoadHudTree();
            var view = new GameplayStudentHudView(root);

            view.SetFragmentProgress(9, 3);
            Assert.That(root.Q<Label>("fragment-count-label").text, Is.EqualTo("3 / 3"));

            view.SetFragmentProgress(-2, 3);
            Assert.That(root.Q<Label>("fragment-count-label").text, Is.EqualTo("0 / 3"));

            view.Dispose();
        }

        [Test]
        public void SetInteraction_UpdatesLabelAndDisabledState()
        {
            TemplateContainer root = LoadHudTree();
            var view = new GameplayStudentHudView(root);

            view.SetInteraction("Talk", "ds-icon--speak", false);
            Assert.That(root.Q<Label>("interaction-label").text, Is.EqualTo("Talk"));
            Assert.That(root.Q<Button>("interaction-button").enabledSelf, Is.False);

            view.Dispose();
        }

        [Test]
        public void SetLookHelperVisible_TogglesHelperClass()
        {
            TemplateContainer root = LoadHudTree();
            var view = new GameplayStudentHudView(root);
            Label helper = root.Q<Label>("look-helper");

            view.SetLookHelperVisible(false);
            Assert.That(helper.ClassListContains("gameplay-student-hud__look-helper--hidden"), Is.True);

            view.SetLookHelperVisible(true);
            Assert.That(helper.ClassListContains("gameplay-student-hud__look-helper--hidden"), Is.False);

            view.Dispose();
        }

        [Test]
        public void SetInputEnabled_TogglesInputDisabledClass()
        {
            TemplateContainer root = LoadHudTree();
            var view = new GameplayStudentHudView(root);

            view.SetInputEnabled(false);
            Assert.That(view.Root.ClassListContains("gameplay-student-hud__input-disabled"), Is.True);

            view.SetInputEnabled(true);
            Assert.That(view.Root.ClassListContains("gameplay-student-hud__input-disabled"), Is.False);

            view.Dispose();
        }

        [Test]
        public void ViewModel_SanitizedCopy_PreventsNullAndNegativeValues()
        {
            var model = new GameplayStudentHudViewModel
            {
                MissionTitle = null,
                AreaPhaseLabel = null,
                ObjectiveText = null,
                CollectedFragments = -4,
                TotalFragments = -1,
                InteractionLabel = null,
                InteractionIconClass = null
            };

            GameplayStudentHudViewModel sanitized = model.SanitizedCopy();
            Assert.That(sanitized.MissionTitle, Is.EqualTo(string.Empty));
            Assert.That(sanitized.CollectedFragments, Is.EqualTo(0));
            Assert.That(sanitized.TotalFragments, Is.EqualTo(0));
            Assert.That(sanitized.InteractionIconClass,
                Is.EqualTo(GameplayStudentHudViewModel.DefaultInteractionIconClass));
        }

        [Test]
        public void InteractionRequested_RaisesPresentationIntent()
        {
            TemplateContainer root = LoadHudTree();
            var view = new GameplayStudentHudView(root);
            int count = 0;
            view.InteractionRequested += () => count++;

            Button button = root.Q<Button>("interaction-button");
            using ClickEvent click = ClickEvent.GetPooled();
            click.target = button;
            button.SendEvent(click);
            Assert.That(count, Is.EqualTo(1));

            view.Dispose();
        }

        [Test]
        public void PauseRequested_RaisesPresentationIntent()
        {
            TemplateContainer root = LoadHudTree();
            var view = new GameplayStudentHudView(root);
            int count = 0;
            view.PauseRequested += () => count++;

            Button button = root.Q<Button>("pause-button");
            using ClickEvent click = ClickEvent.GetPooled();
            click.target = button;
            button.SendEvent(click);
            Assert.That(count, Is.EqualTo(1));

            view.Dispose();
        }

        private static TemplateContainer LoadHudTree()
        {
            VisualTreeAsset asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
            Assert.That(asset, Is.Not.Null, "GameplayStudentHud.uxml must exist for EditMode tests.");
            return asset.CloneTree();
        }
    }
}
