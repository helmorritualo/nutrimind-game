namespace NutriMind.Core.Data
{
    /// <summary>
    /// Client/runtime policy from GET /config. Authoritative sync limits live here.
    /// </summary>
    public sealed class ClientConfiguration
    {
        public string ApiVersion { get; set; }
        public string MinimumClientVersion { get; set; }
        public string RequiredManifestVersion { get; set; }
        public bool MaintenanceMode { get; set; }
        public string MaintenanceMessage { get; set; }
        public int SyncMaxEventsPerBatch { get; set; } = 100;
        public int SyncMaxRequestBytes { get; set; } = 512 * 1024;
        public int SyncMaxEventPayloadBytes { get; set; } = 16 * 1024;
        public int SyncMaxEventAgeDays { get; set; } = 90;
    }
}
