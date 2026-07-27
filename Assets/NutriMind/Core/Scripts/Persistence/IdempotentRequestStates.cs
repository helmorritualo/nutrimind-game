namespace NutriMind.Core.Persistence
{
    /// <summary>
    /// Shared state contract for the idempotent_request table.
    /// </summary>
    public static class IdempotentRequestStates
    {
        public const string Pending = "pending";
        public const string Sending = "sending";
        public const string Uncertain = "uncertain";
        public const string Completed = "completed";
        public const string Rejected = "rejected";

        public static readonly string[] Unresolved =
        {
            Pending,
            Sending,
            Uncertain
        };

        public static bool IsUnresolved(string state)
        {
            return state == Pending || state == Sending || state == Uncertain;
        }
    }
}
