namespace NutriMind.Core.Data
{
    /// <summary>
    /// Subject catalog entry for selection screens.
    /// </summary>
    public sealed class SubjectSummary
    {
        public string Id { get; set; }
        public string Slug { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
    }
}
