using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace NutriMind.Core.Utilities
{
    /// <summary>
    /// Marshals work onto Unity's player/main thread. UI Toolkit and SceneManager require it.
    /// </summary>
    public static class UnityMainThread
    {
        private static SynchronizationContext _context;
        private static int _mainThreadId;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _context = null;
            _mainThreadId = 0;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CaptureContext()
        {
            EnsureCaptured();
        }

        public static void EnsureCaptured()
        {
            if (_context != null)
            {
                return;
            }

            // Never capture a null context from a thread-pool worker.
            SynchronizationContext current = SynchronizationContext.Current;
            if (current == null)
            {
                return;
            }

            _context = current;
            _mainThreadId = Thread.CurrentThread.ManagedThreadId;
        }

        public static bool IsMainThread
        {
            get
            {
                if (_mainThreadId != 0)
                {
                    return Thread.CurrentThread.ManagedThreadId == _mainThreadId;
                }

                return _context != null
                       && SynchronizationContext.Current != null
                       && ReferenceEquals(SynchronizationContext.Current, _context);
            }
        }

        public static void Post(Action action)
        {
            if (action == null)
            {
                return;
            }

            EnsureCaptured();

            if (_context == null || IsMainThread)
            {
                action();
                return;
            }

            _context.Post(_ =>
            {
                try
                {
                    action();
                }
                catch (Exception exception)
                {
                    NutriMindLog.RuntimeError(
                        "Main-thread posted action failed: " + exception.GetType().Name);
                }
            }, null);
        }

        public static Task SwitchToMainAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EnsureCaptured();

            if (_context == null || IsMainThread)
            {
                return Task.CompletedTask;
            }

            var completion = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            CancellationTokenRegistration registration = default;
            if (cancellationToken.CanBeCanceled)
            {
                registration = cancellationToken.Register(() =>
                    completion.TrySetCanceled(cancellationToken));
            }

            _context.Post(_ =>
            {
                try
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        completion.TrySetCanceled(cancellationToken);
                    }
                    else
                    {
                        completion.TrySetResult(true);
                    }
                }
                finally
                {
                    registration.Dispose();
                }
            }, null);

            return completion.Task;
        }
    }
}
