using System;

namespace NutriMind.Core.Persistence
{
    /// <summary>
    /// Allowed sync_outbox.state values. Quiz Portal attempts do not use this outbox.
    /// </summary>
    public static class OutboxEventState
    {
        public const string Pending = "pending";
        public const string Sending = "sending";
        public const string Accepted = "accepted";
        public const string Duplicate = "duplicate";
        public const string Rejected = "rejected";
        public const string Deferred = "deferred";

        public static bool IsKnown(string state)
        {
            if (string.IsNullOrWhiteSpace(state))
            {
                return false;
            }

            switch (state.Trim())
            {
                case Pending:
                case Sending:
                case Accepted:
                case Duplicate:
                case Rejected:
                case Deferred:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsTerminalSuccess(string state)
        {
            return string.Equals(state, Accepted, StringComparison.Ordinal)
                || string.Equals(state, Duplicate, StringComparison.Ordinal);
        }

        public static bool IsPushable(string state)
        {
            return string.Equals(state, Pending, StringComparison.Ordinal)
                || string.Equals(state, Deferred, StringComparison.Ordinal);
        }
    }
}
