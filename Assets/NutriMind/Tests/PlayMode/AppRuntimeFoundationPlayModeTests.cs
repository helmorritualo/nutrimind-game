using System.Collections;
using NutriMind.App.Composition;
using NutriMind.App.Features;
using NutriMind.App.Routing;
using NutriMind.App.UI;
using NutriMind.Core.Bootstrap;
using NutriMind.Core.Data;
using NutriMind.Core.Networking;
using NutriMind.Core.Persistence;
using NutriMind.Tests.TestData;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace NutriMind.Tests.PlayMode
{
    /// <summary>
    /// SCN_App_* PlayMode coverage for Prompt 1.
    /// These tests require App scenes in Build Settings (indices 0–3).
    /// If the Unity Test Runner cannot auto-load scenes in batch/CI, run them from the Editor Test Runner PlayMode tab.
    /// </summary>
    public sealed class AppRuntimeFoundationPlayModeTests
    {
        [UnitySetUp]
        public IEnumerator UnitySetUp()
        {
            yield return PlayModeAppTestHelpers.ResetDefaultDatabaseFiles();
        }

        [UnityTearDown]
        public IEnumerator UnityTearDown()
        {
            yield return PlayModeAppTestHelpers.ResetDefaultDatabaseFiles();
        }

        [UnityTest]
        [Timeout(60000)]
        public IEnumerator Bootstrap_CleanStartup_NavigatesToAuthentication()
        {
            yield return PlayModeAppTestHelpers.LoadBootstrapScene();
            yield return PlayModeAppTestHelpers.WaitForAppLifetime();
            yield return PlayModeAppTestHelpers.ForceZeroMockLatency();

            // Startup lands on Bootstrap AuthenticationRequired; Open Login loads Authentication.
            yield return PlayModeAppTestHelpers.WaitForBootstrapAuthenticationRequiredAndOpenLogin();

            Assert.That(SceneManager.GetActiveScene().name,
                Is.EqualTo(AppSceneNavigator.AuthenticationSceneName));
            Assert.That(
                PlayModeAppTestHelpers.CountActiveSceneRootsOfType<AppAuthenticationSceneRoot>(),
                Is.EqualTo(1));
        }

        [UnityTest]
        [Timeout(90000)]
        public IEnumerator ValidMockLogin_NavigatesToMainHome()
        {
            yield return PlayModeAppTestHelpers.LoadBootstrapScene();
            yield return PlayModeAppTestHelpers.WaitForAppLifetime();
            yield return PlayModeAppTestHelpers.ForceZeroMockLatency();
            yield return PlayModeAppTestHelpers.WaitForBootstrapAuthenticationRequiredAndOpenLogin();

            yield return PlayModeAppTestHelpers.PerformMockLoginViaUseCase();
            yield return PlayModeAppTestHelpers.WaitForScene(AppSceneNavigator.MainSceneName);

            Assert.That(SceneManager.GetActiveScene().name,
                Is.EqualTo(AppSceneNavigator.MainSceneName));
            Assert.That(AppLifetime.Instance.IsAuthenticated, Is.True);
            Assert.That(AppLifetime.Instance.Router.CurrentRoute.RouteId, Is.EqualTo(AppRouteId.Home));
            Assert.That(
                PlayModeAppTestHelpers.CountActiveSceneRootsOfType<AppMainSceneRoot>(),
                Is.EqualTo(1));
        }

        [UnityTest]
        [Timeout(90000)]
        public IEnumerator OfflineCleanInstall_CannotAuthenticate()
        {
            yield return PlayModeAppTestHelpers.LoadBootstrapScene();
            yield return PlayModeAppTestHelpers.WaitForAppLifetime();
            yield return PlayModeAppTestHelpers.ForceZeroMockLatency();

            AppLifetime.Instance.Connectivity.SetState(ConnectivityState.Offline);

            // Fresh install without token: startup should still land on Authentication.
            var startup = new AppStartupCoordinator(AppLifetime.Instance);
            var run = startup.RunAsync();
            while (!run.IsCompleted)
            {
                yield return null;
            }

            Assert.That(run.IsFaulted, Is.False);
            Assert.That(startup.State, Is.EqualTo(BootstrapPreviewState.AuthenticationRequired));

            var useCase = new LoginUseCase(AppLifetime.Instance, startup);
            var loginTask = useCase.ExecuteAsync(new LoginRequestModel
            {
                Lrn = MockGatewayTestFactory.ValidLrn,
                Pin = MockGatewayTestFactory.ValidPin,
                DeviceName = SystemInfo.deviceName
            });
            while (!loginTask.IsCompleted)
            {
                yield return null;
            }

            LoginUseCaseResult login = loginTask.Result;
            Assert.That(login.IsSuccess, Is.False);
            Assert.That(login.IsOfflineUnavailable, Is.True);
        }

        [UnityTest]
        [Timeout(120000)]
        public IEnumerator OnlineCache_ThenOffline_EligiblePath()
        {
            yield return PlayModeAppTestHelpers.LoadBootstrapScene();
            yield return PlayModeAppTestHelpers.WaitForAppLifetime();
            yield return PlayModeAppTestHelpers.ForceZeroMockLatency();
            yield return PlayModeAppTestHelpers.WaitForBootstrapAuthenticationRequiredAndOpenLogin();

            yield return PlayModeAppTestHelpers.PerformMockLoginViaUseCase();
            yield return PlayModeAppTestHelpers.WaitForScene(AppSceneNavigator.MainSceneName);

            // Session + bootstrap cache should be offline_eligible after successful login.
            AppResult<LocalSessionRecord> session =
                AppLifetime.Instance.SessionRepository.GetSession();
            Assert.That(session.IsSuccess, Is.True);
            Assert.That(session.Value, Is.Not.Null);
            Assert.That(session.Value.OfflineEligible, Is.True);
            Assert.That(
                AppLifetime.Instance.ResourceCacheRepository.Get(ResourceCacheKeys.Bootstrap).Value,
                Is.Not.Null);

            // Development Mock token store persists across toggle; keep the lifetime instance offline.
            AppLifetime.Instance.Connectivity.SetState(ConnectivityState.Offline);
            var startup = new AppStartupCoordinator(AppLifetime.Instance);
            var run = startup.RunAsync();
            while (!run.IsCompleted)
            {
                yield return null;
            }

            Assert.That(run.IsFaulted, Is.False);
            Assert.That(startup.State, Is.EqualTo(BootstrapPreviewState.OfflineEligible));
            Assert.That(startup.IsOfflineEligible, Is.True);
            Assert.That(AppLifetime.Instance.LastBootstrap, Is.Not.Null);
            Assert.That(AppLifetime.Instance.LastBootstrap.Profile, Is.Not.Null);
            Assert.That(AppLifetime.Instance.OfflineEligible, Is.True);

            var continueTask = startup.ContinueOfflineAsync();
            while (!continueTask.IsCompleted)
            {
                yield return null;
            }

            Assert.That(startup.State, Is.EqualTo(BootstrapPreviewState.Ready));
        }

        [UnityTest]
        [Timeout(90000)]
        public IEnumerator RecoverableStartupError_RetryRestoresAuthenticationRequired()
        {
            yield return PlayModeAppTestHelpers.LoadBootstrapScene();
            yield return PlayModeAppTestHelpers.WaitForAppLifetime();
            yield return PlayModeAppTestHelpers.ForceZeroMockLatency();

            // Recoverable path: token present, offline, and no offline-eligible session/cache.
            yield return PlayModeAppTestHelpers.WaitForBootstrapAuthenticationRequiredAndOpenLogin();

            // Seed a token without offline-eligible session.
            var write = AppLifetime.Instance.TokenStore.WriteAsync("stale-token-without-session");
            while (!write.IsCompleted)
            {
                yield return null;
            }

            AppLifetime.Instance.SessionRepository.ClearSession();
            AppLifetime.Instance.ResourceCacheRepository.ClearAll();
            AppLifetime.Instance.Connectivity.SetState(ConnectivityState.Offline);

            var startup = new AppStartupCoordinator(AppLifetime.Instance);
            var run = startup.RunAsync();
            while (!run.IsCompleted)
            {
                yield return null;
            }

            Assert.That(startup.State, Is.EqualTo(BootstrapPreviewState.RecoverableError));
            Assert.That(startup.LastError, Is.Not.Null);
            Assert.That(startup.LastError.Code, Is.EqualTo(AppErrorCodes.NetworkOffline));

            // Retry after restoring online + clearing bad token should return to AuthenticationRequired.
            AppLifetime.Instance.Connectivity.SetState(ConnectivityState.Online);
            var clear = AppLifetime.Instance.TokenStore.ClearAsync();
            while (!clear.IsCompleted)
            {
                yield return null;
            }

            var retry = startup.RunAsync();
            while (!retry.IsCompleted)
            {
                yield return null;
            }

            Assert.That(startup.State, Is.EqualTo(BootstrapPreviewState.AuthenticationRequired));
        }

        [UnityTest]
        [Timeout(90000)]
        public IEnumerator OnlyOneApplicationSceneRoot_AndOneRouteSurfaceVisible()
        {
            yield return PlayModeAppTestHelpers.LoadBootstrapScene();
            yield return PlayModeAppTestHelpers.WaitForAppLifetime();
            yield return PlayModeAppTestHelpers.ForceZeroMockLatency();

            Assert.That(
                PlayModeAppTestHelpers.CountActiveSceneRootsOfType<AppBootstrapSceneRoot>(),
                Is.EqualTo(1));

            yield return PlayModeAppTestHelpers.WaitForBootstrapAuthenticationRequiredAndOpenLogin();

            Assert.That(
                PlayModeAppTestHelpers.CountActiveSceneRootsOfType<AppBootstrapSceneRoot>(),
                Is.EqualTo(0));
            Assert.That(
                PlayModeAppTestHelpers.CountActiveSceneRootsOfType<AppAuthenticationSceneRoot>(),
                Is.EqualTo(1));
            Assert.That(
                PlayModeAppTestHelpers.CountActiveSceneRootsOfType<AppMainSceneRoot>(),
                Is.EqualTo(0));
            Assert.That(
                PlayModeAppTestHelpers.CountActiveSceneRootsOfType<AppQuizPortalSceneRoot>(),
                Is.EqualTo(0));

            // One login surface bound.
            UIDocument[] documents = Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None);
            int loginRoots = 0;
            for (int i = 0; i < documents.Length; i++)
            {
                VisualElement root = documents[i].rootVisualElement;
                if (root != null && root.Q<VisualElement>("login-root") != null
                    && documents[i].isActiveAndEnabled)
                {
                    loginRoots++;
                }
            }

            Assert.That(loginRoots, Is.EqualTo(1));
        }

        [UnityTest]
        [Timeout(120000)]
        public IEnumerator ResetLocalDatabase_RealAppLifetimePath_ChangesInstallationUuidAndReturnsBootstrap()
        {
            yield return PlayModeAppTestHelpers.LoadBootstrapScene();
            yield return PlayModeAppTestHelpers.WaitForAppLifetime();
            yield return PlayModeAppTestHelpers.ForceZeroMockLatency();

            AppLifetime lifetime = AppLifetime.Instance;
            Assert.That(lifetime.IsReady, Is.True);

            AppResult<string> beforeResult = lifetime.InstallationRepository.GetOrCreateDeviceId();
            Assert.That(beforeResult.IsSuccess, Is.True);
            string beforeUuid = beforeResult.Value;
            Assert.That(beforeUuid, Is.Not.Null.And.Not.Empty);
            lifetime.SetInstallationDeviceId(beforeUuid);

            var resetTask = lifetime.ResetLocalDatabaseAsync(System.Threading.CancellationToken.None);
            while (!resetTask.IsCompleted)
            {
                yield return null;
            }

            Assert.That(resetTask.IsFaulted, Is.False, "ResetLocalDatabaseAsync faulted.");
            AppResult resetResult = resetTask.Result;
            Assert.That(resetResult.IsSuccess, Is.True,
                resetResult.Error != null
                    ? resetResult.Error.Code + " — " + resetResult.Error.Message
                    : "reset failed");

            yield return PlayModeAppTestHelpers.WaitForAppLifetime();
            lifetime = AppLifetime.Instance;

            Assert.That(lifetime.IsReady, Is.True);
            Assert.That(lifetime.Database, Is.Not.Null);
            Assert.That(lifetime.Database.IsOpen, Is.True);
            Assert.That(lifetime.Database.SchemaVersion, Is.EqualTo(1));
            Assert.That(lifetime.Gateway, Is.Not.Null);
            Assert.That(lifetime.Router, Is.Not.Null);
            Assert.That(lifetime.SceneNavigator, Is.Not.Null);
            Assert.That(lifetime.MissionProgressRepository, Is.Not.Null);
            Assert.That(lifetime.OutboxRepository, Is.Not.Null);
            Assert.That(lifetime.AnnouncementReadRepository, Is.Not.Null);
            Assert.That(lifetime.IdempotentRequestRepository, Is.Not.Null);
            Assert.That(lifetime.LocalProgressWriter, Is.Not.Null);

            string afterUuid = lifetime.InstallationDeviceId;
            if (string.IsNullOrEmpty(afterUuid))
            {
                AppResult<string> afterResult = lifetime.InstallationRepository.GetOrCreateDeviceId();
                Assert.That(afterResult.IsSuccess, Is.True);
                afterUuid = afterResult.Value;
            }

            Assert.That(afterUuid, Is.Not.EqualTo(beforeUuid),
                "Installation UUID must change after ResetLocalDatabaseAsync.");

            yield return PlayModeAppTestHelpers.WaitForScene(
                AppSceneNavigator.BootstrapSceneName,
                PlayModeAppTestHelpers.DefaultTimeoutSeconds);
            Assert.That(SceneManager.GetActiveScene().name,
                Is.EqualTo(AppSceneNavigator.BootstrapSceneName));
        }
    }
}
