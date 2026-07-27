using System;
using System.Threading;
using System.Threading.Tasks;
using NutriMind.Core.Utilities;

namespace NutriMind.App.Routing
{
    /// <summary>
    /// Scene-aware application router with separate Main and QuizPortal stacks.
    /// </summary>
    public sealed class AppRouter : IAppRouter
    {
        private readonly IAppSceneNavigator _sceneNavigator;
        private readonly AppRouteStack _mainStack = new AppRouteStack();
        private readonly AppRouteStack _quizStack = new AppRouteStack();
        private AppSceneId _activeSceneStack = AppSceneId.Main;
        private AppRouteEntry? _mainReturnRoute;
        private int _navigationGate;

        public AppRouter(IAppSceneNavigator sceneNavigator)
        {
            _sceneNavigator = sceneNavigator ?? throw new ArgumentNullException(nameof(sceneNavigator));
            EnsureMainRoot();
        }

        public event Action<AppRouteEntry> RouteChanged;

        public AppRouteEntry CurrentRoute
        {
            get
            {
                AppRouteStack stack = GetActiveStack();
                if (stack.TryGetCurrent(out AppRouteEntry entry))
                {
                    return entry;
                }

                return new AppRouteEntry(AppRouteId.Home);
            }
        }

        public AppSceneId ActiveSceneStack => _activeSceneStack;

        public AppRouteEntry? MainReturnRoute => _mainReturnRoute;

        public async Task NavigateAsync(
            AppRouteId routeId,
            AppRouteContext context = null,
            CancellationToken cancellationToken = default)
        {
            await PushOrReplaceAsync(routeId, context, replace: true, cancellationToken).ConfigureAwait(false);
        }

        public async Task PushAsync(
            AppRouteId routeId,
            AppRouteContext context = null,
            CancellationToken cancellationToken = default)
        {
            await PushOrReplaceAsync(routeId, context, replace: false, cancellationToken).ConfigureAwait(false);
        }

        public async Task ReplaceAsync(
            AppRouteId routeId,
            AppRouteContext context = null,
            CancellationToken cancellationToken = default)
        {
            await PushOrReplaceAsync(routeId, context, replace: true, cancellationToken).ConfigureAwait(false);
        }

        public async Task BackAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!TryEnterNavigationGate())
            {
                return;
            }

            try
            {
                if (_activeSceneStack == AppSceneId.QuizPortal)
                {
                    await BackInQuizPortalAsync(cancellationToken).ConfigureAwait(false);
                    return;
                }

                await BackInMainAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                ExitNavigationGate();
            }
        }

        public async Task EnterQuizPortalAsync(
            AppRouteContext context = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!TryEnterNavigationGate())
            {
                return;
            }

            try
            {
                if (_mainStack.TryGetCurrent(out AppRouteEntry currentMain))
                {
                    _mainReturnRoute = currentMain;
                }
                else
                {
                    _mainReturnRoute = new AppRouteEntry(AppRouteId.Home);
                }

                EnsureQuizPortalRoot();
                _activeSceneStack = AppSceneId.QuizPortal;
                await _sceneNavigator.LoadAsync(AppSceneId.QuizPortal, cancellationToken).ConfigureAwait(false);
                RaiseRouteChanged();
            }
            finally
            {
                ExitNavigationGate();
            }
        }

        public async Task ReturnToMainAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!TryEnterNavigationGate())
            {
                return;
            }

            try
            {
                AppRouteEntry restore = _mainReturnRoute ?? new AppRouteEntry(AppRouteId.Home);
                _mainReturnRoute = null;
                _quizStack.Clear();
                _mainStack.Reset(restore);
                _activeSceneStack = AppSceneId.Main;
                await _sceneNavigator.LoadAsync(AppSceneId.Main, cancellationToken).ConfigureAwait(false);
                RaiseRouteChanged();
            }
            finally
            {
                ExitNavigationGate();
            }
        }

        public async Task HandleUnauthorizedAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ClearStacks();
            await _sceneNavigator.LoadAsync(AppSceneId.Authentication, cancellationToken).ConfigureAwait(false);
            NutriMindLog.Auth("Unauthorized/logout cleared route stacks and loaded Authentication.");
        }

        public void ClearStacks()
        {
            _mainStack.Clear();
            _quizStack.Clear();
            _mainReturnRoute = null;
            _activeSceneStack = AppSceneId.Main;
            EnsureMainRoot();
        }

        public void EnsureMainRoot()
        {
            if (_mainStack.IsEmpty)
            {
                _mainStack.Reset(new AppRouteEntry(AppRouteId.Home));
            }
        }

        public void EnsureQuizPortalRoot()
        {
            if (_quizStack.IsEmpty)
            {
                _quizStack.Reset(new AppRouteEntry(AppRouteId.QuizList));
            }
        }

        private async Task PushOrReplaceAsync(
            AppRouteId routeId,
            AppRouteContext context,
            bool replace,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!TryEnterNavigationGate())
            {
                return;
            }

            try
            {
                bool isQuiz = AppSceneNavigator.IsQuizPortalRoute(routeId);
                bool isMain = AppSceneNavigator.IsMainRoute(routeId);

                if (isQuiz && _activeSceneStack == AppSceneId.Main)
                {
                    throw new InvalidOperationException(
                        "Cannot push QuizPortal route '" + routeId + "' onto the Main stack. Use EnterQuizPortalAsync.");
                }

                var entry = new AppRouteEntry(routeId, context);

                // Bottom-nav / shell chrome may request Main routes while Quiz Portal is active.
                // Leave Quiz Portal and land on the requested Main route instead of throwing.
                if (isMain && _activeSceneStack == AppSceneId.QuizPortal)
                {
                    _quizStack.Clear();
                    AppRouteEntry restore = _mainReturnRoute ?? new AppRouteEntry(AppRouteId.Home);
                    _mainReturnRoute = null;

                    if (replace)
                    {
                        _mainStack.Reset(entry);
                    }
                    else
                    {
                        _mainStack.Reset(restore);
                        _mainStack.Push(entry);
                    }

                    _activeSceneStack = AppSceneId.Main;
                    await _sceneNavigator.LoadAsync(AppSceneId.Main, cancellationToken).ConfigureAwait(false);
                    RaiseRouteChanged();
                    return;
                }

                AppSceneId targetScene = AppSceneNavigator.GetSceneForRoute(routeId);
                AppRouteStack stack = isQuiz ? _quizStack : _mainStack;

                if (stack.IsEmpty)
                {
                    stack.Reset(entry);
                }
                else if (replace)
                {
                    stack.Replace(entry);
                }
                else
                {
                    stack.Push(entry);
                }

                _activeSceneStack = targetScene;
                await _sceneNavigator.LoadAsync(targetScene, cancellationToken).ConfigureAwait(false);
                RaiseRouteChanged();
            }
            finally
            {
                ExitNavigationGate();
            }
        }

        private async Task BackInMainAsync(CancellationToken cancellationToken)
        {
            if (_mainStack.Count <= 1)
            {
                _mainStack.Reset(new AppRouteEntry(AppRouteId.Home));
                await _sceneNavigator.LoadAsync(AppSceneId.Main, cancellationToken).ConfigureAwait(false);
                RaiseRouteChanged();
                return;
            }

            _mainStack.TryPop(out _);
            if (_mainStack.IsEmpty)
            {
                _mainStack.Reset(new AppRouteEntry(AppRouteId.Home));
            }

            await _sceneNavigator.LoadAsync(AppSceneId.Main, cancellationToken).ConfigureAwait(false);
            RaiseRouteChanged();
        }

        private async Task BackInQuizPortalAsync(CancellationToken cancellationToken)
        {
            if (_quizStack.Count <= 1)
            {
                AppRouteContext context = _quizStack.TryGetCurrent(out AppRouteEntry current)
                    ? current.Context
                    : AppRouteContext.Empty;

                if (context != null && context.ReturnToMainOnQuizBack && _mainReturnRoute.HasValue)
                {
                    await ReturnToMainInternalAsync(cancellationToken).ConfigureAwait(false);
                    return;
                }

                _quizStack.Reset(new AppRouteEntry(AppRouteId.QuizList));
                await _sceneNavigator.LoadAsync(AppSceneId.QuizPortal, cancellationToken).ConfigureAwait(false);
                RaiseRouteChanged();
                return;
            }

            _quizStack.TryPop(out _);
            if (_quizStack.IsEmpty)
            {
                _quizStack.Reset(new AppRouteEntry(AppRouteId.QuizList));
            }

            await _sceneNavigator.LoadAsync(AppSceneId.QuizPortal, cancellationToken).ConfigureAwait(false);
            RaiseRouteChanged();
        }

        private async Task ReturnToMainInternalAsync(CancellationToken cancellationToken)
        {
            AppRouteEntry restore = _mainReturnRoute ?? new AppRouteEntry(AppRouteId.Home);
            _mainReturnRoute = null;
            _quizStack.Clear();
            _mainStack.Reset(restore);
            _activeSceneStack = AppSceneId.Main;
            await _sceneNavigator.LoadAsync(AppSceneId.Main, cancellationToken).ConfigureAwait(false);
            RaiseRouteChanged();
        }

        private AppRouteStack GetActiveStack()
        {
            return _activeSceneStack == AppSceneId.QuizPortal ? _quizStack : _mainStack;
        }

        private void RaiseRouteChanged()
        {
            RouteChanged?.Invoke(CurrentRoute);
        }

        private bool TryEnterNavigationGate()
        {
            if (_navigationGate > 0)
            {
                NutriMindLog.RuntimeWarning("Nested navigation ignored.");
                return false;
            }

            _navigationGate++;
            return true;
        }

        private void ExitNavigationGate()
        {
            if (_navigationGate > 0)
            {
                _navigationGate--;
            }
        }
    }
}
