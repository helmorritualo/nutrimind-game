namespace NutriMind.Gameplay.UI
{
    public enum GameplayLearningOverlayState
    {
        Hidden = 0,
        Dialogue,
        Evidence,
        Question,
        FirstWrongHint,
        SecondWrongExplanation,
        CorrectAcknowledgement,
        CaptionSelection,
        EventSequence
    }

    public sealed class GameplayLearningOverlayViewModel
    {
        public GameplayLearningOverlayState State { get; set; } = GameplayLearningOverlayState.Hidden;
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string Speaker { get; set; } = string.Empty;
        public string PrimaryActionLabel { get; set; } = "Continue";
        public string SecondaryActionLabel { get; set; } = string.Empty;
        public bool ShowSecondaryAction { get; set; }
        public bool ShowResetAction { get; set; }
        public bool ShowConfirmAction { get; set; }
        public bool ConfirmEnabled { get; set; }
        public string[] OptionLabels { get; set; } = System.Array.Empty<string>();
        public string[] OptionIds { get; set; } = System.Array.Empty<string>();
        public string[] SlotLabels { get; set; } = System.Array.Empty<string>();
        public string[] SlotValues { get; set; } = System.Array.Empty<string>();
        public string SelectedCardLabel { get; set; } = string.Empty;
    }
}
