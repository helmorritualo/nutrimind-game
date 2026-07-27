using System;
using System.Collections;
using System.IO;
using NutriMind.App.Features;
using NutriMind.App.Routing;
using NutriMind.Core.Bootstrap;
using NutriMind.Core.Data;
using NutriMind.Core.Networking;
using NutriMind.Core.Persistence;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace NutriMind.Tests.TestData
{
    /// <summary>
    /// Shared PlayMode helpers for SCN_App_* scene flows.
    /// Note: AppLifetime uses the default persistent DB path; tests reset those files when possible.
    /// In-memory token store does not survive AppLifetime destruction — offline-eligible checks
    /// re-run startup on the live instance after toggling connectivity.
    /// </summary>
    public static class PlayModeAppTestHelpers
    {
        public const float DefaultTimeoutSeconds = 45f;

        public static IEnumerator ResetDefaultDatabaseFiles()
        {
            if (AppLifetime.HasInstance)
            {
                UnityEngine.Object.Destroy(AppLifetime.Instance.gameObject);
                yield return null;
                yield return null;
            }

            string path = NutriMindDatabase.GetDefaultDatabasePath();
            TryDelete(path);
            TryDelete(path + "-shm");
            TryDelete(path + "-wal");
            yield return null;
        }

        public static IEnumerator LoadBootstrapScene()
        {
            AsyncOperation op = SceneManager.LoadSceneAsync(
                AppSceneNavigator.BootstrapSceneName,
                LoadSceneMode.Single);
            AssertNotNull(op, "LoadSceneAsync Bootstrap");
            while (op != null && !op.isDone)
            {
                yield return null;
            }

            yield return null;
        }

        public static IEnumerator WaitUntil(
            Func<bool> condition,
            float timeoutSeconds,
            string failureMessage)
        {
            float start = Time.realtimeSinceStartup;
            while (!condition())
            {
                if (Time.realtimeSinceStartup - start > timeoutSeconds)
                {
                    throw new TimeoutException(failureMessage);
                }

                yield return null;
            }
        }

        public static IEnumerator WaitForScene(
            string sceneName,
            float timeoutSeconds = DefaultTimeoutSeconds)
        {
            yield return WaitUntil(
                () => SceneManager.GetActiveScene().name == sceneName,
                timeoutSeconds,
                "Timed out waiting for scene '" + sceneName + "'. Active='"
                + SceneManager.GetActiveScene().name + "'.");
        }

        public static IEnumerator WaitForAppLifetime(float timeoutSeconds = DefaultTimeoutSeconds)
        {
            yield return WaitUntil(
                () => AppLifetime.HasInstance && AppLifetime.Instance.IsReady,
                timeoutSeconds,
                "Timed out waiting for AppLifetime.");
        }

        public static IEnumerator ForceZeroMockLatency()
        {
            yield return WaitForAppLifetime();
            NutriMindRuntimeOptions options = AppLifetime.Instance.RuntimeOptions;
            if (options == null)
            {
                yield break;
            }

            options.MinimumMockLatencyMilliseconds = 0;
            options.MaximumMockLatencyMilliseconds = 0;
            options.Clamp();
            // Gateway already cloned options at compose time; mutate gateway options when available.
            if (AppLifetime.Instance.Gateway is MockStudentGateway mock)
            {
                mock.Options.MinimumMockLatencyMilliseconds = 0;
                mock.Options.MaximumMockLatencyMilliseconds = 0;
                mock.Options.Clamp();
            }
        }

        public static int CountActiveSceneRootsOfType<T>() where T : MonoBehaviour
        {
            T[] found = UnityEngine.Object.FindObjectsByType<T>(FindObjectsSortMode.None);
            int count = 0;
            for (int i = 0; i < found.Length; i++)
            {
                if (found[i] != null && found[i].isActiveAndEnabled)
                {
                    count++;
                }
            }

            return count;
        }

        public static IEnumerator WaitForBootstrapAuthenticationRequiredAndOpenLogin(
            float timeoutSeconds = DefaultTimeoutSeconds)
        {
            yield return WaitUntil(
                () =>
                {
                    Button button = FindBootstrapPrimaryActionButton();
                    return button != null
                           && button.enabledInHierarchy
                           && button.style.display != DisplayStyle.None
                           && !string.IsNullOrEmpty(button.text)
                           && button.text.IndexOf("Sign In", System.StringComparison.OrdinalIgnoreCase) >= 0;
                },
                timeoutSeconds,
                "Timed out waiting for Bootstrap 'Continue to Sign In' action.");

            Button primary = FindBootstrapPrimaryActionButton();
            using (var evt = ClickEvent.GetPooled())
            {
                evt.target = primary;
                primary.SendEvent(evt);
            }

            // Fallback if UITK click routing differs across versions.
            if (SceneManager.GetActiveScene().name != AppSceneNavigator.AuthenticationSceneName)
            {
                if (AppLifetime.HasInstance && AppLifetime.Instance.SceneNavigator != null)
                {
                    var nav = AppLifetime.Instance.SceneNavigator.LoadAsync(AppSceneId.Authentication);
                    while (!nav.IsCompleted)
                    {
                        yield return null;
                    }
                }
            }

            yield return WaitForScene(AppSceneNavigator.AuthenticationSceneName, timeoutSeconds);
        }

        public static Button FindBootstrapPrimaryActionButton()
        {
            UIDocument[] documents = UnityEngine.Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None);
            for (int i = 0; i < documents.Length; i++)
            {
                VisualElement root = documents[i] != null ? documents[i].rootVisualElement : null;
                if (root == null)
                {
                    continue;
                }

                Button button = root.Q<Button>("bootstrap-primary-action");
                if (button != null)
                {
                    return button;
                }
            }

            return null;
        }

        public static IEnumerator PerformMockLoginViaUseCase()
        {
            yield return WaitForAppLifetime();
            yield return ForceZeroMockLatency();

            var useCase = new LoginUseCase(
                AppLifetime.Instance,
                new AppStartupCoordinator(AppLifetime.Instance));

            var task = useCase.ExecuteAsync(new LoginRequestModel
            {
                Lrn = MockGatewayTestFactory.ValidLrn,
                Pin = MockGatewayTestFactory.ValidPin,
                DeviceName = SystemInfo.deviceName
            });

            while (!task.IsCompleted)
            {
                yield return null;
            }

            if (task.IsFaulted)
            {
                throw task.Exception ?? new Exception("Login use case faulted.");
            }

            LoginUseCaseResult result = task.Result;
            if (!result.IsSuccess)
            {
                throw new InvalidOperationException(
                    "Mock login failed: "
                    + (result.Error != null ? result.Error.Code + " — " + result.Error.Message : result.Message));
            }

            AppLifetime.Instance.Router?.EnsureMainRoot();
            var nav = AppLifetime.Instance.SceneNavigator.LoadAsync(AppSceneId.Main);
            while (!nav.IsCompleted)
            {
                yield return null;
            }

            if (nav.IsFaulted)
            {
                throw nav.Exception ?? new Exception("Main scene navigation faulted.");
            }
        }

        public static IEnumerator ReRunStartupCoordinator()
        {
            yield return WaitForAppLifetime();
            var coordinator = new AppStartupCoordinator(AppLifetime.Instance);
            var task = coordinator.RunAsync();
            while (!task.IsCompleted)
            {
                yield return null;
            }

            if (task.IsFaulted)
            {
                throw task.Exception ?? new Exception("Startup coordinator faulted.");
            }

            // Expose last state through a short wait for OfflineEligible/Ready/Auth.
            yield return null;
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception)
            {
                // best effort
            }
        }

        private static void AssertNotNull(object value, string label)
        {
            if (value == null)
            {
                throw new InvalidOperationException(label + " returned null.");
            }
        }
    }
}
