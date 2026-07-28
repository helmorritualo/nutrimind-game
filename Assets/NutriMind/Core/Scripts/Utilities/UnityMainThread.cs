using System;
using System.Runtime.CompilerServices;
using System.Threading;
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

        internal static SynchronizationContext CapturedContext
        {
            get
            {
                EnsureCaptured();
                return _context;
            }
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
                        "Main-thread posted action failed: "
                        + exception.GetType().Name
                        + " — "
                        + exception.Message);
                }
            }, null);
        }

        /// <summary>
        /// Await to resume on the Unity main thread. Uses a custom awaiter that posts to the
        /// captured Unity <see cref="SynchronizationContext"/> — do not replace with a
        /// <see cref="Task"/>-based switch; after <c>ConfigureAwait(false)</c> a Task
        /// continuation captures no context and returns to the thread pool.
        /// </summary>
        public static MainThreadSwitchAwaitable SwitchToMainAsync(
            CancellationToken cancellationToken = default)
        {
            return new MainThreadSwitchAwaitable(cancellationToken);
        }
    }

    public readonly struct MainThreadSwitchAwaitable
    {
        private readonly CancellationToken _cancellationToken;

        public MainThreadSwitchAwaitable(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
        }

        public MainThreadSwitchAwaiter GetAwaiter()
        {
            return new MainThreadSwitchAwaiter(_cancellationToken);
        }
    }

    public readonly struct MainThreadSwitchAwaiter : ICriticalNotifyCompletion
    {
        private readonly CancellationToken _cancellationToken;

        public MainThreadSwitchAwaiter(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
        }

        public bool IsCompleted => UnityMainThread.IsMainThread;

        public void GetResult()
        {
            _cancellationToken.ThrowIfCancellationRequested();
        }

        public void OnCompleted(Action continuation)
        {
            UnsafeOnCompleted(continuation);
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            if (continuation == null)
            {
                throw new ArgumentNullException(nameof(continuation));
            }

            _cancellationToken.ThrowIfCancellationRequested();

            if (UnityMainThread.IsMainThread)
            {
                continuation();
                return;
            }

            SynchronizationContext context = UnityMainThread.CapturedContext;
            if (context == null)
            {
                throw new InvalidOperationException(
                    "Unity main-thread SynchronizationContext was not captured. Cannot marshal to main thread.");
            }

            // Always post the async state-machine continuation to Unity's main context.
            // GetResult() surfaces cancellation once the continuation resumes.
            context.Post(_ => continuation(), null);
        }
    }
}
