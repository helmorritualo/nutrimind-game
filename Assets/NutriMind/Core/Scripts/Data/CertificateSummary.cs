using System;

namespace NutriMind.Core.Data
{
    /// <summary>
    /// Certificate list/detail item. Schema is domain-owned until OpenAPI item shapes are frozen.
    /// </summary>
    public sealed class CertificateSummary
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string TypeLabel { get; set; }
        public string Status { get; set; }
        public string EligibilityDescription { get; set; }
        public string RecognitionText { get; set; }
        public string LockedReason { get; set; }
        public DateTimeOffset? IssuedAt { get; set; }
        public string AwardedToDisplayName { get; set; }
    }
}
