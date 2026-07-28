using System;
using System.Threading;
using System.Threading.Tasks;

namespace NutriMind.Core.Utilities
{
    /// <summary>
    /// Safe fire-and-forget helpers for UI/event handlers. Never blocks.
    /// </summary>
    public static class TaskUtilities
    {
        public static void ForgetSafely(
            Task task,
            CancellationToken cancellationToken = default,
            string logPrefix = null)
        {
            if (task == null)
            {
                return;
            }

            _ = ObserveAsync(task, cancellationToken, logPrefix ?? "Task");
        }

        private static async Task ObserveAsync(
            Task task,
            CancellationToken cancellationToken,
            string logPrefix)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected during scene unload / presenter dispose.
            }
            catch (Exception exception)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                NutriMindLog.RuntimeError(
                    logPrefix + " failed: " + exception.GetType().Name + " — " + exception.Message);
            }
        }
    }
}
