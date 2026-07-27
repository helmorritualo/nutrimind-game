using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using NutriMind.Core.Data;
using NutriMind.Core.Networking;
using NutriMind.Tests.TestData;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace NutriMind.Tests.EditMode
{
    public sealed class MockStudentGatewayLoginAndLatencyTests
    {
        [UnityTest]
        public IEnumerator Login_HappyPath_Succeeds()
        {
            MockStudentGateway gateway = MockGatewayTestFactory.CreateGateway();
            Task<AppResult<LoginResult>> task = gateway.LoginAsync(new LoginRequest
            {
                Lrn = MockGatewayTestFactory.ValidLrn,
                Pin = MockGatewayTestFactory.ValidPin,
                DeviceName = "EditModeTestDevice"
            });
            yield return Await(task);

            AppResult<LoginResult> result = task.Result;
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.AccessToken, Is.Not.Null.And.Not.Empty);
            Assert.That(result.Value.Student, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator Login_WrongPin_ReturnsAuthInvalidCredentials()
        {
            MockStudentGateway gateway = MockGatewayTestFactory.CreateGateway();
            Task<AppResult<LoginResult>> task = gateway.LoginAsync(new LoginRequest
            {
                Lrn = MockGatewayTestFactory.ValidLrn,
                Pin = "9999",
                DeviceName = "EditModeTestDevice"
            });
            yield return Await(task);

            AppResult<LoginResult> result = task.Result;
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo(AppErrorCodes.AuthInvalidCredentials));
        }

        [UnityTest]
        public IEnumerator Login_RateLimitedScenario_ReturnsRateLimited()
        {
            MockStudentGateway gateway = MockGatewayTestFactory.CreateGateway(
                MockApiScenario.RateLimitedLogin);
            Task<AppResult<LoginResult>> task = gateway.LoginAsync(new LoginRequest
            {
                Lrn = MockGatewayTestFactory.ValidLrn,
                Pin = MockGatewayTestFactory.ValidPin,
                DeviceName = "EditModeTestDevice"
            });
            yield return Await(task);

            AppResult<LoginResult> result = task.Result;
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo(AppErrorCodes.RateLimited));
            Assert.That(result.Error.HttpStatus, Is.EqualTo(429));
            Assert.That(result.Error.IsRetryable, Is.True);
        }

        [UnityTest]
        public IEnumerator MockLatency_Cancellation_ThrowsOperationCanceled()
        {
            MockStudentGateway gateway = MockGatewayTestFactory.CreateGateway(
                minimumLatencyMs: 500,
                maximumLatencyMs: 800);

            using var cts = new CancellationTokenSource();
            Task<AppResult<PingStatus>> pingTask = gateway.PingAsync(cts.Token);
            cts.CancelAfter(50);

            while (!pingTask.IsCompleted)
            {
                yield return null;
            }

            Assert.That(pingTask.IsCanceled || pingTask.IsFaulted, Is.True);
            if (pingTask.IsFaulted)
            {
                Assert.That(pingTask.Exception.GetBaseException(), Is.TypeOf<OperationCanceledException>());
            }
        }

        [Test]
        public void ResourcesMockFixtureSource_MissingPath_FailsWithPathIdentity()
        {
            var source = new ResourcesMockFixtureSource();
            const string missingName = "definitely_missing_fixture_xyz";
            string expectedPath = MockFixtureNames.ToResourcePath(missingName);
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Fixture missing"));

            AppResult<string> result = source.LoadText(missingName);

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo(AppErrorCodes.FixtureLoadFailed));
            Assert.That(result.Error.Message, Does.Contain(expectedPath));
            Assert.That(expectedPath, Is.EqualTo("NutriMindMock/definitely_missing_fixture_xyz"));
        }

        private static IEnumerator Await(Task task)
        {
            while (!task.IsCompleted)
            {
                yield return null;
            }

            if (task.IsFaulted)
            {
                throw task.Exception ?? new Exception("Task faulted.");
            }
        }
    }
}
