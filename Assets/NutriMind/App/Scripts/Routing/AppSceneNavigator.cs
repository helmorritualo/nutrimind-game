using System;
using System.Threading;
using System.Threading.Tasks;
using NutriMind.Core.Utilities;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NutriMind.App.Routing
{
    /// <summary>
    /// Loads application scenes by build index / scene name. Does not push in-scene routes.
    /// </summary>
    public sealed class AppSceneNavigator : IAppSceneNavigator
    {
        public const string BootstrapSceneName = "SCN_App_Bootstrap";
        public const string AuthenticationSceneName = "SCN_App_Authentication";
        public const string MainSceneName = "SCN_App_Main";
        public const string QuizPortalSceneName = "SCN_App_QuizPortal";

        private AppSceneId _currentScene = AppSceneId.Bootstrap;
        private bool _isLoading;

        public AppSceneId CurrentScene => _currentScene;

        public async Task LoadAsync(AppSceneId sceneId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await UnityMainThread.SwitchToMainAsync(cancellationToken);

            if (_isLoading)
            {
                NutriMindLog.RuntimeWarning("Scene load already in progress; ignoring " + sceneId + ".");
                return;
            }

            if (_currentScene == sceneId && SceneManager.GetActiveScene().name == GetSceneName(sceneId))
            {
                return;
            }

            string sceneName = GetSceneName(sceneId);
            _isLoading = true;
            try
            {
                AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
                if (operation == null)
                {
                    throw new InvalidOperationException(
                        "Failed to start LoadSceneAsync for '" + sceneName + "'.");
                }

                // Once LoadSceneAsync has started, finish it. Cancelling mid-swap leaves
                // _currentScene stale while Unity already activated the new scene (blank UI).
                while (!operation.isDone)
                {
                    await Task.Yield();
                }

                _currentScene = sceneId;
                NutriMindLog.Runtime("Loaded application scene " + sceneName + ".");
            }
            finally
            {
                _isLoading = false;
            }
        }

        public static string GetSceneName(AppSceneId sceneId)
        {
            switch (sceneId)
            {
                case AppSceneId.Bootstrap:
                    return BootstrapSceneName;
                case AppSceneId.Authentication:
                    return AuthenticationSceneName;
                case AppSceneId.Main:
                    return MainSceneName;
                case AppSceneId.QuizPortal:
                    return QuizPortalSceneName;
                default:
                    throw new ArgumentOutOfRangeException(nameof(sceneId), sceneId, null);
            }
        }

        public static AppSceneId GetSceneForRoute(AppRouteId routeId)
        {
            return IsQuizPortalRoute(routeId) ? AppSceneId.QuizPortal : AppSceneId.Main;
        }

        public static bool IsQuizPortalRoute(AppRouteId routeId)
        {
            return routeId == AppRouteId.QuizList
                   || routeId == AppRouteId.QuizDetail
                   || routeId == AppRouteId.QuizAttempt
                   || routeId == AppRouteId.QuizResult
                   || routeId == AppRouteId.QuizHistory;
        }

        public static bool IsMainRoute(AppRouteId routeId)
        {
            return !IsQuizPortalRoute(routeId);
        }
    }
}
