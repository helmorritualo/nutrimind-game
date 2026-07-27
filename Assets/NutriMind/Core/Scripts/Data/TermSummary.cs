namespace NutriMind.Core.Data
{
    /// <summary>
    /// Term catalog entry under a subject.
    /// </summary>
    public sealed class TermSummary
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int Order { get; set; }
        public bool IsActive { get; set; }
    }
}
