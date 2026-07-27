using System;

namespace NutriMind.Core.Utilities
{
    public interface IAppClock
    {
        DateTimeOffset UtcNow { get; }
    }

    public sealed class SystemAppClock : IAppClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }

    public sealed class FixedMockClock : IAppClock
    {
        public static readonly DateTimeOffset DefaultFixedUtc =
            new DateTimeOffset(2026, 7, 27, 4, 0, 0, TimeSpan.Zero);

        private DateTimeOffset _utcNow;

        public FixedMockClock()
            : this(DefaultFixedUtc)
        {
        }

        public FixedMockClock(DateTimeOffset utcNow)
        {
            _utcNow = utcNow.ToUniversalTime();
        }

        public DateTimeOffset UtcNow => _utcNow;

        public void Advance(TimeSpan delta)
        {
            _utcNow = _utcNow.Add(delta);
        }

        public void Set(DateTimeOffset utcNow)
        {
            _utcNow = utcNow.ToUniversalTime();
        }
    }
}
