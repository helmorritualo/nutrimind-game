namespace NutriMind.Gameplay.Runtime
{
    public static class MissionContentIds
    {
        public const string MissionId = "g5_lq_t1_m01";
        public const string Area1Id = "g5_lq_t1_m01_a01";
        public const string Area2Id = "g5_lq_t1_m01_a02";

        public const string FarmerLiraNpc = "g5_lq_t1_m01_a01_npc_farmer_lira";
        public const string DamagedStorybook = "g5_lq_t1_m01_a01_storybook";
        public const string ClueOpeningIllustration = "g5_lq_t1_m01_a01_clue_opening_illustration";
        public const string ClueSurvivingLines = "g5_lq_t1_m01_a01_clue_surviving_lines";
        public const string CaptionRepairBoard = "g5_lq_t1_m01_a01_caption_board";
        public const string Fragment1 = "g5_lq_t1_m01_a01_collectible";
        public const string Gate1 = "g5_lq_t1_m01_gate_a01_a02";
        public const string CheckpointA01 = "g5_lq_t1_m01_checkpoint_a01";

        public const string MinaNpc = "g5_lq_t1_m01_a02_npc_mina";
        public const string ClueChildrenGather = "g5_lq_t1_m01_a02_clue_children_gather";
        public const string ClueStorybookOpened = "g5_lq_t1_m01_a02_clue_storybook_opened";
        public const string ClueCaptionRepaired = "g5_lq_t1_m01_a02_clue_caption_repaired";
        public const string EventSequenceBoard = "g5_lq_t1_m01_a02_sequence_board";
        public const string Fragment2 = "g5_lq_t1_m01_a02_collectible";
        public const string Gate2 = "g5_lq_t1_m01_gate_a02_a03";
        public const string CheckpointA02 = "g5_lq_t1_m01_checkpoint_a02";

        public static readonly string[] Area1QuestionIds =
        {
            "g5_lq_t1_m01_a01_q01",
            "g5_lq_t1_m01_a01_q02",
            "g5_lq_t1_m01_a01_q03"
        };

        public static readonly string[] Area2QuestionIds =
        {
            "g5_lq_t1_m01_a02_q01",
            "g5_lq_t1_m01_a02_q02",
            "g5_lq_t1_m01_a02_q03"
        };

        public static readonly string[] Area2ClueIds =
        {
            ClueChildrenGather,
            ClueStorybookOpened,
            ClueCaptionRepaired
        };

        public static readonly string[] EventSequenceCardIds =
        {
            "event_children_gather",
            "event_storybook_opened",
            "event_caption_repaired"
        };

        public static readonly string[] EventSequenceSlotIds =
        {
            "slot_beginning",
            "slot_middle",
            "slot_end"
        };
    }
}
