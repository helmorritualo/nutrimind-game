using System;
using System.Threading;
using System.Threading.Tasks;

namespace NutriMind.App.Routing
{
    public interface IAppRouter
    {
        event Action<AppRouteEntry> RouteChanged;

        AppRouteEntry CurrentRoute { get; }
        AppSceneId ActiveSceneStack { get; }
        AppRouteEntry? MainReturnRoute { get; }

        Task NavigateAsync(AppRouteId routeId, AppRouteContext context = null, CancellationToken cancellationToken = default);

        Task PushAsync(AppRouteId routeId, AppRouteContext context = null, CancellationToken cancellationToken = default);

        Task ReplaceAsync(AppRouteId routeId, AppRouteContext context = null, CancellationToken cancellationToken = default);

        Task BackAsync(CancellationToken cancellationToken = default);

        Task EnterQuizPortalAsync(AppRouteContext context = null, CancellationToken cancellationToken = default);

        Task ReturnToMainAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Clears the Quiz Portal stack to a single QuizList entry, remains in QuizPortal,
        /// and raises one RouteChanged. Preserves MainReturnRoute / ReturnToMainOnQuizBack policy.
        /// </summary>
        Task ResetQuizPortalToRootAsync(
            AppRouteContext context = null,
            CancellationToken cancellationToken = default);

        Task HandleUnauthorizedAsync(CancellationToken cancellationToken = default);

        void ClearStacks();

        void EnsureMainRoot();

        void EnsureQuizPortalRoot();
    }
}
