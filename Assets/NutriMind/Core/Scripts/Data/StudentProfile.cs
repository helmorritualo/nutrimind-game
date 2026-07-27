namespace NutriMind.Core.Data
{
    /// <summary>
    /// Neutral student profile. Not a transport DTO.
    /// </summary>
    public sealed class StudentProfile
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string LrnMasked { get; set; }
        public string GradeId { get; set; }
        public StudentSection Section { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Classroom section summary attached to a student profile.
    /// </summary>
    public sealed class StudentSection
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string GradeId { get; set; }
    }
}
