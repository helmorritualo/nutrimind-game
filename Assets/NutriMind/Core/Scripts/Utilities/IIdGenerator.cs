using System;

namespace NutriMind.Core.Utilities
{
    public interface IIdGenerator
    {
        string NewUuid();
    }

    public sealed class SystemIdGenerator : IIdGenerator
    {
        public string NewUuid() => Guid.NewGuid().ToString("D");
    }

    /// <summary>
    /// Deterministic UUID generator for mock/tests. Format remains a valid UUID string.
    /// </summary>
    public sealed class DeterministicMockIdGenerator : IIdGenerator
    {
        private readonly object _gate = new object();
        private int _sequence;

        public DeterministicMockIdGenerator(int startSequence = 1)
        {
            _sequence = Math.Max(1, startSequence);
        }

        public string NewUuid()
        {
            lock (_gate)
            {
                int value = _sequence++;
                return string.Format(
                    "00000000-0000-4000-8000-{0:D12}",
                    value);
            }
        }

        public void Reset(int startSequence = 1)
        {
            lock (_gate)
            {
                _sequence = Math.Max(1, startSequence);
            }
        }
    }
}
