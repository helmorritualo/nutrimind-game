using System;
using System.Threading;
using System.Threading.Tasks;

namespace NutriMind.Core.Bootstrap
{
    /// <summary>
    /// Application-lifetime single-flight gate for unauthorized/session-clear flows.
    /// Concurrent callers await the same in-flight operation; the gate resets after completion.
    /// </summary>
    public sealed class UnauthorizedSingleFlightGate
    {
        private readonly object _gateLock = new object();
        private Task _inFlight;

        public Task ExecuteAsync(Func<Task> action, CancellationToken callerToken = default)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            Task inFlight;
            lock (_gateLock)
            {
                if (_inFlight != null && !_inFlight.IsCompleted)
                {
                    inFlight = _inFlight;
                }
                else
                {
                    Task run = RunAsync(action);
                    _inFlight = run;
                    inFlight = run;
                }
            }

            return AwaitAsync(inFlight, callerToken);
        }

        private async Task RunAsync(Func<Task> action)
        {
            try
            {
                await action().ConfigureAwait(false);
            }
            finally
            {
                lock (_gateLock)
                {
                    _inFlight = null;
                }
            }
        }

        private static async Task AwaitAsync(Task inFlight, CancellationToken cancellationToken)
        {
            if (inFlight == null)
            {
                return;
            }

            if (!cancellationToken.CanBeCanceled)
            {
                await inFlight.ConfigureAwait(false);
                return;
            }

            var completion = new TaskCompletionSource<object>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            using (cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken)))
            {
                Task finished = await Task.WhenAny(inFlight, completion.Task).ConfigureAwait(false);
                await finished.ConfigureAwait(false);
            }
        }
    }
}
