namespace NutriMind.Gameplay.UI
{
    /// <summary>
    /// Display-ready values for the student gameplay HUD.
    /// Contains no mission domain logic, persistence, or scene references.
    /// </summary>
    public sealed class GameplayStudentHudViewModel
    {
        public const string DefaultInteractionIconClass = "ds-icon--search";

        public string MissionTitle { get; set; } = string.Empty;
        public string AreaPhaseLabel { get; set; } = string.Empty;
        public string ObjectiveText { get; set; } = string.Empty;
        public int CollectedFragments { get; set; }
        public int TotalFragments { get; set; }
        public string InteractionLabel { get; set; } = string.Empty;
        public string InteractionIconClass { get; set; } = DefaultInteractionIconClass;
        public bool InteractionAvailable { get; set; }
        public bool PauseAvailable { get; set; } = true;
        public bool ShowLookHelper { get; set; }
        public bool InputEnabled { get; set; } = true;

        /// <summary>
        /// Default preview model for G5 LiteraQuest T1 M1 Area 1.
        /// </summary>
        public static GameplayStudentHudViewModel CreateDefaultPreview()
        {
            return new GameplayStudentHudViewModel
            {
                MissionTitle = "The Festival Storybook Rescue",
                AreaPhaseLabel = "Area 1 • Discover",
                ObjectiveText = "Inspect the damaged storybook beside Farmer Lira.",
                CollectedFragments = 0,
                TotalFragments = 3,
                InteractionLabel = "Inspect",
                InteractionIconClass = DefaultInteractionIconClass,
                InteractionAvailable = true,
                PauseAvailable = true,
                ShowLookHelper = true,
                InputEnabled = true
            };
        }

        /// <summary>
        /// Returns a sanitized copy safe for display binding.
        /// </summary>
        public GameplayStudentHudViewModel SanitizedCopy()
        {
            int total = TotalFragments < 0 ? 0 : TotalFragments;
            int collected = CollectedFragments;
            if (collected < 0)
            {
                collected = 0;
            }

            if (total > 0 && collected > total)
            {
                collected = total;
            }

            return new GameplayStudentHudViewModel
            {
                MissionTitle = MissionTitle ?? string.Empty,
                AreaPhaseLabel = AreaPhaseLabel ?? string.Empty,
                ObjectiveText = ObjectiveText ?? string.Empty,
                CollectedFragments = collected,
                TotalFragments = total,
                InteractionLabel = InteractionLabel ?? string.Empty,
                InteractionIconClass = string.IsNullOrWhiteSpace(InteractionIconClass)
                    ? DefaultInteractionIconClass
                    : InteractionIconClass,
                InteractionAvailable = InteractionAvailable,
                PauseAvailable = PauseAvailable,
                ShowLookHelper = ShowLookHelper,
                InputEnabled = InputEnabled
            };
        }
    }
}
