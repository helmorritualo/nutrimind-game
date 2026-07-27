using System;
using System.Threading.Tasks;
using NutriMind.App.Routing;
using NutriMind.Tests.TestData;
using NUnit.Framework;

namespace NutriMind.Tests.EditMode
{
    public sealed class AppRouteStackAndRouterTests
    {
        [Test]
        public void AppRouteStack_PushPopReset_Works()
        {
            var stack = new AppRouteStack();
            Assert.That(stack.IsEmpty, Is.True);

            stack.Reset(new AppRouteEntry(AppRouteId.Home));
            Assert.That(stack.Count, Is.EqualTo(1));
            Assert.That(stack.Current.RouteId, Is.EqualTo(AppRouteId.Home));

            stack.Push(new AppRouteEntry(AppRouteId.Subjects));
            stack.Push(new AppRouteEntry(AppRouteId.Terms));
            Assert.That(stack.Count, Is.EqualTo(3));
            Assert.That(stack.Current.RouteId, Is.EqualTo(AppRouteId.Terms));

            Assert.That(stack.TryPop(out AppRouteEntry removed), Is.True);
            Assert.That(removed.RouteId, Is.EqualTo(AppRouteId.Terms));
            Assert.That(stack.Current.RouteId, Is.EqualTo(AppRouteId.Subjects));

            stack.Clear();
            Assert.That(stack.IsEmpty, Is.True);
            Assert.That(stack.TryPop(out _), Is.False);
        }

        [Test]
        public void AppSceneNavigator_RouteHelpers_ClassifyMainAndQuiz()
        {
            Assert.That(AppSceneNavigator.IsMainRoute(AppRouteId.Home), Is.True);
            Assert.That(AppSceneNavigator.IsQuizPortalRoute(AppRouteId.Home), Is.False);
            Assert.That(AppSceneNavigator.GetSceneForRoute(AppRouteId.Home), Is.EqualTo(AppSceneId.Main));

            Assert.That(AppSceneNavigator.IsQuizPortalRoute(AppRouteId.QuizList), Is.True);
            Assert.That(AppSceneNavigator.IsMainRoute(AppRouteId.QuizDetail), Is.False);
            Assert.That(
                AppSceneNavigator.GetSceneForRoute(AppRouteId.QuizAttempt),
                Is.EqualTo(AppSceneId.QuizPortal));
        }

        [Test]
        public async Task AppRouter_PushMainRoutes_UsesMainStackWithoutSceneCrossContamination()
        {
            var navigator = new FakeAppSceneNavigator();
            var router = new AppRouter(navigator);

            Assert.That(router.CurrentRoute.RouteId, Is.EqualTo(AppRouteId.Home));
            Assert.That(router.ActiveSceneStack, Is.EqualTo(AppSceneId.Main));

            await router.PushAsync(AppRouteId.Subjects);
            await router.PushAsync(AppRouteId.Profile);

            Assert.That(router.CurrentRoute.RouteId, Is.EqualTo(AppRouteId.Profile));
            Assert.That(navigator.CurrentScene, Is.EqualTo(AppSceneId.Main));

            await router.BackAsync();
            Assert.That(router.CurrentRoute.RouteId, Is.EqualTo(AppRouteId.Subjects));
        }

        [Test]
        public async Task AppRouter_PushQuizRouteOntoMain_Throws()
        {
            var router = new AppRouter(new FakeAppSceneNavigator());

            InvalidOperationException ex = Assert.ThrowsAsync<InvalidOperationException>(
                async () => await router.PushAsync(AppRouteId.QuizList));
            Assert.That(ex.Message, Does.Contain("EnterQuizPortalAsync"));
        }

        [Test]
        public async Task AppRouter_EnterQuizPortalAndReturn_RestoresMainRoute()
        {
            var navigator = new FakeAppSceneNavigator();
            var router = new AppRouter(navigator);

            await router.NavigateAsync(AppRouteId.Progress);
            await router.EnterQuizPortalAsync();

            Assert.That(router.ActiveSceneStack, Is.EqualTo(AppSceneId.QuizPortal));
            Assert.That(router.CurrentRoute.RouteId, Is.EqualTo(AppRouteId.QuizList));
            Assert.That(navigator.CurrentScene, Is.EqualTo(AppSceneId.QuizPortal));
            Assert.That(router.MainReturnRoute.HasValue, Is.True);
            Assert.That(router.MainReturnRoute.Value.RouteId, Is.EqualTo(AppRouteId.Progress));

            await router.PushAsync(AppRouteId.QuizDetail);
            Assert.That(router.CurrentRoute.RouteId, Is.EqualTo(AppRouteId.QuizDetail));

            await router.ReturnToMainAsync();
            Assert.That(router.ActiveSceneStack, Is.EqualTo(AppSceneId.Main));
            Assert.That(router.CurrentRoute.RouteId, Is.EqualTo(AppRouteId.Progress));
            Assert.That(navigator.CurrentScene, Is.EqualTo(AppSceneId.Main));
            Assert.That(router.MainReturnRoute.HasValue, Is.False);
        }

        [Test]
        public async Task AppRouter_MainRouteFromQuizPortal_LoadsRequestedMainRoute()
        {
            var navigator = new FakeAppSceneNavigator();
            var router = new AppRouter(navigator);

            await router.NavigateAsync(AppRouteId.Progress);
            await router.EnterQuizPortalAsync();
            await router.PushAsync(AppRouteId.QuizDetail);

            await router.PushAsync(AppRouteId.Home);
            Assert.That(router.ActiveSceneStack, Is.EqualTo(AppSceneId.Main));
            Assert.That(router.CurrentRoute.RouteId, Is.EqualTo(AppRouteId.Home));
            Assert.That(navigator.CurrentScene, Is.EqualTo(AppSceneId.Main));
        }

        [Test]
        public async Task AppRouter_ResetQuizPortalToRoot_ClearsStackToSingleQuizList()
        {
            var navigator = new FakeAppSceneNavigator();
            var router = new AppRouter(navigator);

            await router.NavigateAsync(AppRouteId.Home);
            await router.EnterQuizPortalAsync(
                AppRouteContext.Empty.WithReturnToMainOnQuizBack(true));
            await router.PushAsync(
                AppRouteId.QuizDetail,
                AppRouteContext.ForQuiz("quiz-a", "sci", "t1"));
            await router.PushAsync(
                AppRouteId.QuizAttempt,
                AppRouteContext.ForQuizAttempt("quiz-a", null, "sci", "t1"));
            await router.PushAsync(
                AppRouteId.QuizResult,
                AppRouteContext.ForQuizResult("attempt-1", "quiz-a", "sci", "t1"));

            await router.ResetQuizPortalToRootAsync(
                AppRouteContext.Empty.WithReturnToMainOnQuizBack(true));

            Assert.That(router.ActiveSceneStack, Is.EqualTo(AppSceneId.QuizPortal));
            Assert.That(router.CurrentRoute.RouteId, Is.EqualTo(AppRouteId.QuizList));
            Assert.That(router.MainReturnRoute.HasValue, Is.True);
            Assert.That(router.MainReturnRoute.Value.RouteId, Is.EqualTo(AppRouteId.Home));

            await router.BackAsync();
            Assert.That(router.ActiveSceneStack, Is.EqualTo(AppSceneId.Main));
            Assert.That(router.CurrentRoute.RouteId, Is.EqualTo(AppRouteId.Home));
        }

        [Test]
        public void QuizRouteKey_SameRouteDifferentContext_AreNotEqual()
        {
            var a = QuizRouteKey.FromEntry(new AppRouteEntry(
                AppRouteId.QuizDetail,
                AppRouteContext.ForQuiz("quiz-a")));
            var b = QuizRouteKey.FromEntry(new AppRouteEntry(
                AppRouteId.QuizDetail,
                AppRouteContext.ForQuiz("quiz-b")));
            var duplicate = QuizRouteKey.FromEntry(new AppRouteEntry(
                AppRouteId.QuizDetail,
                AppRouteContext.ForQuiz("quiz-a")));

            Assert.That(a.Equals(b), Is.False);
            Assert.That(a.Equals(duplicate), Is.True);
        }

        [Test]
        public void AppRouteContext_CertificateOrigin_IsPreserved()
        {
            AppRouteContext rewards = AppRouteContext.ForCertificate(null, AppRouteOrigin.Rewards);
            AppRouteContext more = AppRouteContext.Empty.WithOrigin(AppRouteOrigin.More);

            Assert.That(rewards.Origin, Is.EqualTo(AppRouteOrigin.Rewards));
            Assert.That(more.Origin, Is.EqualTo(AppRouteOrigin.More));
            Assert.That(rewards.WithReturnToMainOnQuizBack(true).Origin, Is.EqualTo(AppRouteOrigin.Rewards));
        }

        [Test]
        public async Task AppRouter_HandleUnauthorized_LoadsAuthenticationAndResetsStacks()
        {
            var navigator = new FakeAppSceneNavigator();
            var router = new AppRouter(navigator);
            await router.PushAsync(AppRouteId.Settings);
            await router.EnterQuizPortalAsync();

            await router.HandleUnauthorizedAsync();

            Assert.That(navigator.CurrentScene, Is.EqualTo(AppSceneId.Authentication));
            Assert.That(router.ActiveSceneStack, Is.EqualTo(AppSceneId.Main));
            Assert.That(router.CurrentRoute.RouteId, Is.EqualTo(AppRouteId.Home));
        }
    }
}
