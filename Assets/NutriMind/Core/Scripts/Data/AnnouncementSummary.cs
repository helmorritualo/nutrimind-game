using System;

namespace NutriMind.Core.Data
{
    /// <summary>
    /// Classroom announcement summary. Schema is domain-owned until OpenAPI item shapes are frozen.
    /// </summary>
    public sealed class AnnouncementSummary
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Summary { get; set; }
        public string Body { get; set; }
        public string AudienceLabel { get; set; }
        public string Kind { get; set; }
        public bool IsUnread { get; set; }
        public DateTimeOffset? PublishedAt { get; set; }
        public DateTimeOffset? ExpiresAt { get; set; }
    }
}
