using System;
using System.Threading;
using NutriMind.Core.Bootstrap;
using NutriMind.Core.Data;
using NutriMind.Core.Utilities;

namespace NutriMind.App.Presentation
{
    /// <summary>
    /// Base class for all runtime route presenters.
    /// Owns a CancellationTokenSource linked to the application lifetime token.
    /// Cancels and disposes the CTS on disposal so in-flight requests are abandoned cleanly.
    /// </summary>
    public abstract class RoutePresenterBase : IDisposable
    {
        protected readonly AppLifetime Lifetime;
        protected readonly CancellationTokenSource Cts;
        protected bool Disposed;

        protected RoutePresenterBase(AppLifetime lifetime)
        {
            Lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
            Cts = CancellationTokenSource.CreateLinkedTokenSource(lifetime.LifetimeToken);
        }

        /// <summary>
        /// Token for in-route work (loads, retries). Cancelled when this presenter is disposed.
        /// Safe to read after dispose — returns a cancelled token instead of throwing.
        /// </summary>
        protected CancellationToken RequestToken
        {
            get
            {
                if (Disposed)
                {
                    return new CancellationToken(canceled: true);
                }

                try
                {
                    return Cts.Token;
                }
                catch (ObjectDisposedException)
                {
                    return new CancellationToken(canceled: true);
                }
            }
        }

        /// <summary>
        /// Token for navigation that must survive presenter teardown (scene changes).
        /// Prefer this for EnterQuizPortal / ReturnToMain / Navigate that unloads the current scene.
        /// </summary>
        protected CancellationToken NavigationToken => Lifetime.LifetimeToken;

        public void Dispose()
        {
            if (Disposed)
            {
                return;
            }

            Disposed = true;
            OnDispose();

            try
            {
                Cts.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // already disposed
            }

            try
            {
                Cts.Dispose();
            }
            catch (ObjectDisposedException)
            {
                // already disposed
            }
        }

        /// <summary>
        /// Override to unsubscribe events and release view references before CTS is cancelled.
        /// </summary>
        protected virtual void OnDispose()
        {
        }

        /// <summary>
        /// Returns true if the error represents a 401 / expired-session condition.
        /// </summary>
        protected static bool IsUnauthorized(AppError error)
        {
            if (error == null)
            {
                return false;
            }

            return error.Code == AppErrorCodes.AuthTokenMissing
                   || error.Code == AppErrorCodes.AuthTokenInvalid
                   || error.Code == AppErrorCodes.AuthTokenRevoked
                   || error.HttpStatus == 401;
        }

        /// <summary>
        /// Clears authentication and loads the Authentication scene on the main thread.
        /// Call from within an async method that has already switched to the main thread.
        /// </summary>
        protected void HandleUnauthorized()
        {
            TaskUtilities.ForgetSafely(
                Lifetime.HandleUnauthorizedAsync(NavigationToken),
                NavigationToken,
                "Presenter.Unauthorized");
        }

        /// <summary>
        /// Returns true if the error represents a network/offline condition.
        /// </summary>
        protected static bool IsOffline(AppError error)
        {
            if (error == null)
            {
                return false;
            }

            return error.IsNetworkError
                   || error.Code == AppErrorCodes.NetworkOffline
                   || error.Code == AppErrorCodes.NetworkTimeout;
        }
    }
}
