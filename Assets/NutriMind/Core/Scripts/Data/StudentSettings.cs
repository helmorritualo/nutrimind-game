namespace NutriMind.Core.Data
{
    /// <summary>
    /// Student-facing settings snapshot returned by the Student API.
    /// </summary>
    public sealed class StudentSettings
    {
        public float AudioVolume { get; set; }
        public float MusicVolume { get; set; }
        public string Language { get; set; }
        public bool ReducedMotion { get; set; }
        public bool NotificationsEnabled { get; set; }
    }
}
