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

        [UnityTest]
        [Timeout(120000)]
        public IEnumerator RouteAccess_ProfileSettings_And_RewardsCertificates()
        {
            yield return PlayModeAppTestHelpers.LoadBootstrapScene();
            yield return PlayModeAppTestHelpers.WaitForAppLifetime();
            yield return PlayModeAppTestHelpers.ForceZeroMockLatency();
            yield return PlayModeAppTestHelpers.WaitForBootstrapAuthenticationRequiredAndOpenLogin();
            yield return PlayModeAppTestHelpers.PerformMockLoginViaUseCase();
            yield return PlayModeAppTestHelpers.WaitForScene(AppSceneNavigator.MainSceneName);

            IAppRouter router = AppLifetime.Instance.Router;
            Assert.That(router.CurrentRoute.RouteId, Is.EqualTo(AppRouteId.Home));
            AssertNoPlaceholder();

            yield return NavigateAndAssert(router.PushAsync(AppRouteId.Profile, AppRouteContext.Empty), AppRouteId.Profile);
            yield return NavigateAndAssert(router.PushAsync(AppRouteId.Settings, AppRouteContext.Empty), AppRouteId.Settings);
            yield return NavigateAndAssert(router.BackAsync(), AppRouteId.Profile);

            yield return NavigateAndAssert(router.NavigateAsync(AppRouteId.Rewards, AppRouteContext.Empty), AppRouteId.Rewards);
            yield return NavigateAndAssert(router.PushAsync(AppRouteId.Certificates, AppRouteContext.Empty), AppRouteId.Certificates);
            yield return NavigateAndAssert(router.BackAsync(), AppRouteId.Rewards);
        }

        [UnityTest]
        [Timeout(120000)]
        public IEnumerator MoreHub_SecondaryDestinations_AreReachable()
        {
            yield return PlayModeAppTestHelpers.LoadBootstrapScene();
            yield return PlayModeAppTestHelpers.WaitForAppLifetime();
            yield return PlayModeAppTestHelpers.ForceZeroMockLatency();
            yield return PlayModeAppTestHelpers.WaitForBootstrapAuthenticationRequiredAndOpenLogin();
            yield return PlayModeAppTestHelpers.PerformMockLoginViaUseCase();
            yield return PlayModeAppTestHelpers.WaitForScene(AppSceneNavigator.MainSceneName);

            IAppRouter router = AppLifetime.Instance.Router;
            AppRouteId[] destinations =
            {
                AppRouteId.Profile,
                AppRouteId.Settings,
                AppRouteId.Certificates,
                AppRouteId.Announcements,
                AppRouteId.Leaderboard
            };

            for (int i = 0; i < destinations.Length; i++)
            {
                yield return NavigateAndAssert(
                    router.NavigateAsync(AppRouteId.Home, AppRouteContext.Empty),
                    AppRouteId.Home);
                yield return NavigateAndAssert(
                    router.PushAsync(destinations[i], AppRouteContext.Empty),
                    destinations[i]);
                yield return NavigateAndAssert(router.BackAsync(), AppRouteId.Home);
            }
        }

        [UnityTest]
        [Timeout(120000)]
        public IEnumerator MountedRoutePanels_FillContentRegion()
        {
            yield return PlayModeAppTestHelpers.LoadBootstrapScene();
            yield return PlayModeAppTestHelpers.WaitForAppLifetime();
            yield return PlayModeAppTestHelpers.ForceZeroMockLatency();
            yield return PlayModeAppTestHelpers.WaitForBootstrapAuthenticationRequiredAndOpenLogin();
            yield return PlayModeAppTestHelpers.PerformMockLoginViaUseCase();
            yield return PlayModeAppTestHelpers.WaitForScene(AppSceneNavigator.MainSceneName);

            yield return WaitForContentInstance();
            AssertMountedPanelFillsContent();

            IAppRouter router = AppLifetime.Instance.Router;
            var enterQuiz = router.EnterQuizPortalAsync(
                AppRouteContext.Empty.WithReturnToMainOnQuizBack(true));
            while (!enterQuiz.IsCompleted)
            {
                yield return null;
            }

            yield return PlayModeAppTestHelpers.WaitForScene(AppSceneNavigator.QuizPortalSceneName);
            yield return WaitForContentInstance();
            AssertMountedPanelFillsContent();
        }

        [UnityTest]
        [Timeout(120000)]
        public IEnumerator RealControlPath_ProfileSettings_RewardsCertificates_AndMoreHub()
        {
            yield return PlayModeAppTestHelpers.LoadBootstrapScene();
            yield return PlayModeAppTestHelpers.WaitForAppLifetime();
            yield return PlayModeAppTestHelpers.ForceZeroMockLatency();
            yield return PlayModeAppTestHelpers.WaitForBootstrapAuthenticationRequiredAndOpenLogin();
            yield return PlayModeAppTestHelpers.PerformMockLoginViaUseCase();
            yield return PlayModeAppTestHelpers.WaitForScene(AppSceneNavigator.MainSceneName);
            yield return WaitForContentInstance();

            IAppRouter router = AppLifetime.Instance.Router;
            Assert.That(router.CurrentRoute.RouteId, Is.EqualTo(AppRouteId.Home));

            Button profileButton = FindShellButton("app-shell-profile");
            Assert.That(profileButton, Is.Not.Null, "Shell profile control missing.");
            ClickButton(profileButton);
            yield return WaitForRoute(AppRouteId.Profile);

            Button settingsButton = FindContentButton("settings-button");
            Assert.That(settingsButton, Is.Not.Null, "Profile Settings button missing.");
            ClickButton(settingsButton);
            yield return WaitForRoute(AppRouteId.Settings);

            yield return NavigateAndAssert(router.BackAsync(), AppRouteId.Profile);

            Button rewardsNav = FindShellButton("nav-rewards");
            Assert.That(rewardsNav, Is.Not.Null, "Rewards bottom nav missing.");
            ClickButton(rewardsNav);
            yield return WaitForRoute(AppRouteId.Rewards);

            Button certificatesButton = FindContentButton("rewards-view-certificates-button");
            Assert.That(certificatesButton, Is.Not.Null, "View Certificates button missing.");
            ClickButton(certificatesButton);
            yield return WaitForRoute(AppRouteId.Certificates);
            Assert.That(CountActiveRouteSurfaces(), Is.EqualTo(1));
            AssertNoPlaceholder();

            yield return NavigateAndAssert(router.BackAsync(), AppRouteId.Rewards);

            Button moreNav = FindShellButton("nav-more");
            Assert.That(moreNav, Is.Not.Null, "More bottom nav missing.");
            ClickButton(moreNav);
            yield return null;
            yield return null;

            VisualElement moreHub = FindMoreHub();
            Assert.That(moreHub, Is.Not.Null, "More hub should be visible after More tap.");
            Assert.That(moreHub.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));

            Button destination = moreHub.Q<Button>(className: "app-shell__more-hub-item");
            Assert.That(destination, Is.Not.Null, "More hub destination missing.");
            ClickButton(destination);

            float start = Time.realtimeSinceStartup;
            while (router.CurrentRoute.RouteId == AppRouteId.Rewards
                   && Time.realtimeSinceStartup - start < 10f)
            {
                yield return null;
            }

            Assert.That(router.CurrentRoute.RouteId, Is.Not.EqualTo(AppRouteId.Rewards));
            Assert.That(CountActiveRouteSurfaces(), Is.EqualTo(1));
            AssertNoPlaceholder();

            VisualElement hubAfter = FindMoreHub();
            if (hubAfter != null)
            {
                Assert.That(hubAfter.resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
            }

            VisualElement modalLayer = FindShellRoot()?.Q<VisualElement>("app-shell-modal-layer");
            if (modalLayer != null)
            {
                Assert.That(modalLayer.pickingMode, Is.EqualTo(PickingMode.Ignore));
            }
        }

        private static IEnumerator WaitForRoute(AppRouteId expected, float timeoutSeconds = 10f)
        {
            float start = Time.realtimeSinceStartup;
            while (AppLifetime.Instance.Router.CurrentRoute.RouteId != expected)
            {
                if (Time.realtimeSinceStartup - start > timeoutSeconds)
                {
                    Assert.Fail(
                        "Timed out waiting for route "
                        + expected
                        + "; current="
                        + AppLifetime.Instance.Router.CurrentRoute.RouteId);
                }

                yield return null;
            }

            AssertNoPlaceholder();
            Assert.That(CountActiveRouteSurfaces(), Is.EqualTo(1));
        }

        private static VisualElement FindShellRoot()
        {
            UIDocument[] documents = Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None);
            for (int i = 0; i < documents.Length; i++)
            {
                VisualElement root = documents[i] != null ? documents[i].rootVisualElement : null;
                if (root != null && root.Q<VisualElement>("app-shell-content-region") != null)
                {
                    return root;
                }
            }

            return null;
        }

        private static Button FindShellButton(string name)
        {
            return FindShellRoot()?.Q<Button>(name);
        }

        private static Button FindContentButton(string name)
        {
            VisualElement root = FindShellRoot();
            VisualElement region = root?.Q<VisualElement>("app-shell-content-region");
            return region?.Q<Button>(name);
        }

        private static VisualElement FindMoreHub()
        {
            return FindShellRoot()?.Q<VisualElement>("app-shell-more-hub");
        }

        private static void ClickButton(Button button)
        {
            Assert.That(button, Is.Not.Null);
            using (var evt = ClickEvent.GetPooled())
            {
                evt.target = button;
                button.SendEvent(evt);
            }
        }

        private static IEnumerator NavigateAndAssert(System.Threading.Tasks.Task task, AppRouteId expected)
        {
            while (!task.IsCompleted)
            {
                yield return null;
            }

            Assert.That(task.IsFaulted, Is.False, task.Exception?.ToString());
            Assert.That(AppLifetime.Instance.Router.CurrentRoute.RouteId, Is.EqualTo(expected));
            AssertNoPlaceholder();
            Assert.That(CountActiveRouteSurfaces(), Is.EqualTo(1));
        }

        private static void AssertNoPlaceholder()
        {
            UIDocument[] documents = Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None);
            for (int i = 0; i < documents.Length; i++)
            {
                VisualElement root = documents[i] != null ? documents[i].rootVisualElement : null;
                if (root == null)
                {
                    continue;
                }

                root.Query<Label>().ForEach(label =>
                {
                    if (label != null && !string.IsNullOrEmpty(label.text)
                        && label.text.IndexOf("UXML asset not assigned", System.StringComparison.Ordinal) >= 0)
                    {
                        Assert.Fail("Placeholder visible: " + label.text);
                    }
                });
            }
        }

        private static int CountActiveRouteSurfaces()
        {
            int count = 0;
            UIDocument[] documents = Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None);
            for (int i = 0; i < documents.Length; i++)
            {
                VisualElement root = documents[i] != null ? documents[i].rootVisualElement : null;
                VisualElement region = root?.Q<VisualElement>("app-shell-content-region");
                if (region == null)
                {
                    continue;
                }

                TemplateContainer instance = region.Q<TemplateContainer>(className: "app-shell__content-instance");
                if (instance != null)
                {
                    count++;
                }
            }

            return count;
        }

        private static IEnumerator WaitForContentInstance(float timeoutSeconds = 10f)
        {
            float start = Time.realtimeSinceStartup;
            while (CountActiveRouteSurfaces() < 1)
            {
                if (Time.realtimeSinceStartup - start > timeoutSeconds)
                {
                    Assert.Fail("Timed out waiting for mounted app-shell__content-instance.");
                }

                yield return null;
            }
        }

        private static void AssertMountedPanelFillsContent()
        {
            UIDocument[] documents = Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None);
            bool found = false;
            for (int i = 0; i < documents.Length; i++)
            {
                VisualElement root = documents[i] != null ? documents[i].rootVisualElement : null;
                VisualElement region = root?.Q<VisualElement>("app-shell-content-region");
                if (region == null)
                {
                    continue;
                }

                TemplateContainer instance = region.Q<TemplateContainer>(className: "app-shell__content-instance");
                if (instance == null)
                {
                    continue;
                }

                found = true;
                Assert.That(instance.ClassListContains("app-shell__content-instance"), Is.True);
                Assert.That(instance.resolvedStyle.width, Is.GreaterThan(0f));
                Assert.That(instance.resolvedStyle.height, Is.GreaterThan(0f));

                float regionWidth = region.resolvedStyle.width;
                float regionHeight = region.resolvedStyle.height;
                if (regionWidth > 1f && regionHeight > 1f)
                {
                    Assert.That(instance.resolvedStyle.width, Is.GreaterThan(regionWidth * 0.85f));
                    Assert.That(instance.resolvedStyle.height, Is.GreaterThan(regionHeight * 0.85f));
                }
            }

            Assert.That(found, Is.True, "Expected a mounted app-shell__content-instance.");
        }
    }
}
