using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using NutriMind.Core.Data;
using NutriMind.Core.Networking;
using NutriMind.Tests.TestData;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace NutriMind.Tests.EditMode
{
    // Prompt 1 EditMode suite — mock idempotency
    public sealed class MockStudentGatewayIdempotencyTests
    {
        [UnityTest]
        public IEnumerator QuizSubmit_SameUuidAndPayload_ReturnsSameAttempt()
        {
            Task<MockStudentGateway> gatewayTask = CreateAuthenticatedGatewayAsync();
            yield return Await(gatewayTask);
            MockStudentGateway gateway = gatewayTask.Result;
            SubmitQuizAttemptRequest request = CreateQuizRequest("uuid-quiz-001", "opt_a");

            Task<AppResult<QuizResult>> firstTask = gateway.SubmitQuizAttemptAsync(request);
            yield return Await(firstTask);
            Task<AppResult<QuizResult>> secondTask = gateway.SubmitQuizAttemptAsync(request);
            yield return Await(secondTask);

            AppResult<QuizResult> first = firstTask.Result;
            AppResult<QuizResult> second = secondTask.Result;
            Assert.That(first.IsSuccess, Is.True);
            Assert.That(second.IsSuccess, Is.True);
            Assert.That(second.Value.AttemptId, Is.EqualTo(first.Value.AttemptId));
            Assert.That(second.Value.ClientAttemptUuid, Is.EqualTo(first.Value.ClientAttemptUuid));
        }

        [UnityTest]
        public IEnumerator QuizSubmit_SameUuidDifferentPayload_ReturnsIdempotencyMismatch()
        {
            Task<MockStudentGateway> gatewayTask = CreateAuthenticatedGatewayAsync();
            yield return Await(gatewayTask);
            MockStudentGateway gateway = gatewayTask.Result;

            Task<AppResult<QuizResult>> firstTask =
                gateway.SubmitQuizAttemptAsync(CreateQuizRequest("uuid-quiz-002", "opt_a"));
            yield return Await(firstTask);
            Task<AppResult<QuizResult>> secondTask =
                gateway.SubmitQuizAttemptAsync(CreateQuizRequest("uuid-quiz-002", "opt_b"));
            yield return Await(secondTask);

            Assert.That(firstTask.Result.IsSuccess, Is.True);
            Assert.That(secondTask.Result.IsFailure, Is.True);
            Assert.That(secondTask.Result.Error.Code, Is.EqualTo(AppErrorCodes.IdempotencyPayloadMismatch));
        }

        [UnityTest]
        public IEnumerator QuizSubmit_TimeoutScenario_CommitsThenRetrySucceeds()
        {
            Task<MockStudentGateway> gatewayTask =
                CreateAuthenticatedGatewayAsync(MockApiScenario.QuizSubmissionTimeout);
            yield return Await(gatewayTask);
            MockStudentGateway gateway = gatewayTask.Result;
            SubmitQuizAttemptRequest request = CreateQuizRequest("uuid-quiz-timeout", "opt_a");

            Task<AppResult<QuizResult>> timedOutTask = gateway.SubmitQuizAttemptAsync(request);
            yield return Await(timedOutTask);
            Assert.That(timedOutTask.Result.IsFailure, Is.True);
            Assert.That(timedOutTask.Result.Error.Code, Is.EqualTo(AppErrorCodes.NetworkTimeout));
            Assert.That(
                gateway.ServerState.TryGetQuizResultByClientUuid("uuid-quiz-timeout", out _),
                Is.True);

            Task<AppResult<QuizResult>> retryTask = gateway.SubmitQuizAttemptAsync(request);
            yield return Await(retryTask);
            Assert.That(retryTask.Result.IsSuccess, Is.True);
            Assert.That(retryTask.Result.Value.ClientAttemptUuid, Is.EqualTo("uuid-quiz-timeout"));
        }

        [UnityTest]
        public IEnumerator RewardUse_SameUuidAndPayload_IsIdempotent()
        {
            Task<MockStudentGateway> gatewayTask = CreateAuthenticatedGatewayAsync();
            yield return Await(gatewayTask);
            MockStudentGateway gateway = gatewayTask.Result;
            var request = new UseRewardRequest
            {
                RewardCode = "mock_reward_story_badge",
                RequestUuid = "uuid-reward-001"
            };

            Task<AppResult<RewardSummary>> firstTask = gateway.UseRewardAsync(request);
            yield return Await(firstTask);
            Task<AppResult<RewardSummary>> secondTask = gateway.UseRewardAsync(request);
            yield return Await(secondTask);

            Assert.That(firstTask.Result.IsSuccess, Is.True);
            Assert.That(secondTask.Result.IsSuccess, Is.True);
            Assert.That(secondTask.Result.Value.Status, Is.EqualTo(firstTask.Result.Value.Status));
            Assert.That(secondTask.Result.Value.RewardCode, Is.EqualTo(firstTask.Result.Value.RewardCode));
        }

        [UnityTest]
        public IEnumerator RewardUse_SameUuidDifferentPayload_ReturnsMismatch()
        {
            Task<MockStudentGateway> gatewayTask = CreateAuthenticatedGatewayAsync();
            yield return Await(gatewayTask);
            MockStudentGateway gateway = gatewayTask.Result;

            Task<AppResult<RewardSummary>> firstTask = gateway.UseRewardAsync(new UseRewardRequest
            {
                RewardCode = "mock_reward_story_badge",
                RequestUuid = "uuid-reward-002"
            });
            yield return Await(firstTask);
            Task<AppResult<RewardSummary>> secondTask = gateway.UseRewardAsync(new UseRewardRequest
            {
                RewardCode = "some_other_reward",
                RequestUuid = "uuid-reward-002"
            });
            yield return Await(secondTask);

            Assert.That(firstTask.Result.IsSuccess, Is.True);
            Assert.That(secondTask.Result.IsFailure, Is.True);
            Assert.That(secondTask.Result.Error.Code, Is.EqualTo(AppErrorCodes.IdempotencyPayloadMismatch));
        }

        [UnityTest]
        public IEnumerator RewardUse_TimeoutScenario_CommitsThenRetrySucceeds()
        {
            Task<MockStudentGateway> gatewayTask =
                CreateAuthenticatedGatewayAsync(MockApiScenario.RewardUseTimeout);
            yield return Await(gatewayTask);
            MockStudentGateway gateway = gatewayTask.Result;
            var request = new UseRewardRequest
            {
                RewardCode = "mock_reward_story_badge",
                RequestUuid = "uuid-reward-timeout"
            };

            Task<AppResult<RewardSummary>> timedOutTask = gateway.UseRewardAsync(request);
            yield return Await(timedOutTask);
            Assert.That(timedOutTask.Result.IsFailure, Is.True);
            Assert.That(timedOutTask.Result.Error.Code, Is.EqualTo(AppErrorCodes.NetworkTimeout));

            Task<AppResult<RewardSummary>> retryTask = gateway.UseRewardAsync(request);
            yield return Await(retryTask);
            Assert.That(retryTask.Result.IsSuccess, Is.True);
            Assert.That(retryTask.Result.Value.Status, Is.EqualTo("used").IgnoreCase);
        }

        [UnityTest]
        public IEnumerator SyncPush_SameUuidAndPayload_IsIdempotent()
        {
            Task<MockStudentGateway> gatewayTask = CreateAuthenticatedGatewayAsync();
            yield return Await(gatewayTask);
            MockStudentGateway gateway = gatewayTask.Result;
            SyncPushRequest request = CreateSyncRequest("batch-001", "event-001", 1);

            Task<AppResult<SyncPushResult>> firstTask = gateway.PushSyncAsync(request);
            yield return Await(firstTask);
            Task<AppResult<SyncPushResult>> secondTask = gateway.PushSyncAsync(request);
            yield return Await(secondTask);

            Assert.That(firstTask.Result.IsSuccess, Is.True,
                firstTask.Result.Error?.Code + " — " + firstTask.Result.Error?.Message);
            Assert.That(secondTask.Result.IsSuccess, Is.True);
            Assert.That(secondTask.Result.IsSuccess, Is.True);
            Assert.That(secondTask.Result.Value.ServerRevision, Is.EqualTo(firstTask.Result.Value.ServerRevision));
            Assert.That(secondTask.Result.Value.AcceptedCount, Is.EqualTo(firstTask.Result.Value.AcceptedCount));
        }

        [UnityTest]
        public IEnumerator SyncPush_SameUuidDifferentPayload_ReturnsMismatch()
        {
            Task<MockStudentGateway> gatewayTask = CreateAuthenticatedGatewayAsync();
            yield return Await(gatewayTask);
            MockStudentGateway gateway = gatewayTask.Result;

            Task<AppResult<SyncPushResult>> firstTask =
                gateway.PushSyncAsync(CreateSyncRequest("batch-002", "event-a", 1));
            yield return Await(firstTask);
            Task<AppResult<SyncPushResult>> secondTask =
                gateway.PushSyncAsync(CreateSyncRequest("batch-002", "event-b", 2));
            yield return Await(secondTask);

            Assert.That(firstTask.Result.IsSuccess, Is.True);
            Assert.That(secondTask.Result.IsFailure, Is.True);
            Assert.That(secondTask.Result.Error.Code, Is.EqualTo(AppErrorCodes.IdempotencyPayloadMismatch));
        }

        private static async Task<MockStudentGateway> CreateAuthenticatedGatewayAsync(
            MockApiScenario scenario = MockApiScenario.HappyPath)
        {
            var tokenStore = new InMemoryMockAuthTokenStore();
            MockStudentGateway gateway = MockGatewayTestFactory.CreateGateway(
                scenario,
                tokenStore: tokenStore);

            AppResult<LoginResult> login = await gateway.LoginAsync(new LoginRequest
            {
                Lrn = MockGatewayTestFactory.ValidLrn,
                Pin = MockGatewayTestFactory.ValidPin,
                DeviceName = "EditModeIdempotency"
            });
            Assert.That(login.IsSuccess, Is.True, login.Error?.Message);
            return gateway;
        }

        private static SubmitQuizAttemptRequest CreateQuizRequest(string clientUuid, string optionKey)
        {
            return new SubmitQuizAttemptRequest
            {
                QuizId = "quiz_fixture_001",
                Submission = new QuizAttemptSubmission
                {
                    ClientAttemptUuid = clientUuid,
                    Answers = new List<QuizAnswerSelection>
                    {
                        new QuizAnswerSelection
                        {
                            QuestionId = "qq_001",
                            SelectedOptionKeys = new[] { optionKey }
                        }
                    }
                }
            };
        }

        private static SyncPushRequest CreateSyncRequest(string batchUuid, string eventUuid, int sequence)
        {
            return new SyncPushRequest
            {
                BatchUuid = batchUuid,
                ClientId = "editmode-test",
                // Mock sync_status fixture seeds revision=4; keep client within one revision.
                LastKnownServerRevision = 4,
                Events = new List<SyncPushEvent>
                {
                    new SyncPushEvent
                    {
                        EventUuid = eventUuid,
                        EventType = "mission_started",
                        MissionId = MockServerState.CanonicalMissionId,
                        LocalSequence = sequence
                    }
                }
            };
        }

        private static IEnumerator Await(Task task)
        {
            while (!task.IsCompleted)
            {
                yield return null;
            }

            if (task.IsFaulted)
            {
                throw task.Exception;
            }
        }
    }
}
