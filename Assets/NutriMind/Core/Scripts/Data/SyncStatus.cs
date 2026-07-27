using System;
using System.Collections.Generic;

namespace NutriMind.Core.Data
{
    /// <summary>
    /// Server sync revision and pending-action flags for bootstrap and status routes.
    /// </summary>
    public sealed class SyncStatus
    {
        public bool PendingServerActions { get; set; }
        public int Revision { get; set; }
        public int PendingOutboxCount { get; set; }
        public DateTimeOffset? LastSyncedAt { get; set; }
    }

    /// <summary>
    /// Result of pushing a local outbox batch to the server.
    /// </summary>
    public sealed class SyncPushResult
    {
        public string BatchUuid { get; set; }
        public int ServerRevision { get; set; }
        public int AcceptedCount { get; set; }
        public int DuplicateCount { get; set; }
        public int RejectedCount { get; set; }
        public int DeferredCount { get; set; }
        public IReadOnlyList<SyncPushEventResult> Events { get; set; } = Array.Empty<SyncPushEventResult>();
    }

    /// <summary>
    /// Per-event outcome within a sync push batch.
    /// </summary>
    public sealed class SyncPushEventResult
    {
        public string EventUuid { get; set; }
        public string Status { get; set; }
        public string ErrorCode { get; set; }
    }
}
