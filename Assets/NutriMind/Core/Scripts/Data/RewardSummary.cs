using System;

namespace NutriMind.Core.Data
{
    /// <summary>
    /// Reward inventory item. Schema is domain-owned until OpenAPI item shapes are frozen.
    /// </summary>
    public sealed class RewardSummary
    {
        public string RewardCode { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string SupportingText { get; set; }
        public string Status { get; set; }
        public string LockedReason { get; set; }
        public DateTimeOffset? EarnedAt { get; set; }
        public DateTimeOffset? UsedAt { get; set; }
    }
}
