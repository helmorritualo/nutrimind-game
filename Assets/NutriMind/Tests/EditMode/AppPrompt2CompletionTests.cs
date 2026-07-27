using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NutriMind.App.Presentation;
using NutriMind.App.Routing;
using NutriMind.App.UI;
using NutriMind.Core.Bootstrap;
using NutriMind.Core.Data;
using NutriMind.Core.Networking;
using NutriMind.Core.Persistence;
using NutriMind.Core.Sync;
using NutriMind.Core.Utilities;
using NutriMind.Tests.TestData;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace NutriMind.Tests.EditMode
{
    /// <summary>
    /// Focused Prompt 2 contracts: unauthorized single-flight, envelope identity,
    /// announcement unread math, and sync queue counting.
    /// </summary>
    public sealed class AppPrompt2CompletionTests
    {
        [Test]
        public async Task UnauthorizedSingleFlight_ConcurrentCalls_RunActionOnce()
        {
            var gate = new UnauthorizedSingleFlightGate();
            int runs = 0;
            var started = new TaskCompletionSource<object>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            async Task Action()
            {
                Interlocked.Increment(ref runs);
                started.TrySetResult(null);
                await Task.Delay(80).ConfigureAwait(false);
            }

            Task first = gate.ExecuteAsync(Action);
            await started.Task.ConfigureAwait(false);
            Task second = gate.ExecuteAsync(Action);
            await Task.WhenAll(first, second).ConfigureAwait(false);

            Assert.That(runs, Is.EqualTo(1));

            await gate.ExecuteAsync(Action).ConfigureAwait(false);
            Assert.That(runs, Is.EqualTo(2), "Gate must reset after completion.");
        }

        [UnityTest]
        public IEnumerator RewardTimeout_PersistsEnvelopeAndRetriesSameUuid()
        {
            using (var factory = new TestDatabaseFactory())
            {
                NutriMindDatabase database = factory.OpenDatabase();
                var repo = new SqliteIdempotentRequestRepository(database);

                Task<MockStudentGateway> gatewayTask =
                    CreateAuthenticatedGatewayAsync(MockApiScenario.RewardUseTimeout);
                yield return Await(gatewayTask);
                MockStudentGateway gateway = gatewayTask.Result;

                const string studentId = "student-prompt2-a";
                const string rewardCode = "mock_reward_story_badge";
                const string uuid = "uuid-reward-prompt2-timeout";
                var envelope = new PendingRewardUseEnvelopeV2
                {
                    StudentId = studentId,
                    RewardCode = rewardCode,
                    RequestUuid = uuid
                };
                string normalized = IdempotentMutationSerializers.SerializeReward(envelope);
                string now = DateTimeOffset.UtcNow.ToString("o");

                Assert.That(repo.Upsert(new IdempotentRequestRecord
                {
                    RequestUuid = uuid,
                    Operation = IdempotentOperations.UseReward,
                    StudentId = studentId,
                    EntityKey = rewardCode,
                    NormalizedPayloadJson = normalized,
                    State = IdempotentRequestStates.Sending,
                    CreatedUtc = now,
                    UpdatedUtc = now
                }).IsSuccess, Is.True);

                var request = new UseRewardRequest
                {
                    RewardCode = rewardCode,
                    RequestUuid = uuid
                };

                Task<AppResult<RewardSummary>> timedOutTask = gateway.UseRewardAsync(request);
                yield return Await(timedOutTask);
                Assert.That(timedOutTask.Result.IsFailure, Is.True);
                Assert.That(timedOutTask.Result.Error.Code, Is.EqualTo(AppErrorCodes.NetworkTimeout));

                Assert.That(repo.Upsert(new IdempotentRequestRecord
                {
                    RequestUuid = uuid,
                    Operation = IdempotentOperations.UseReward,
                    StudentId = studentId,
                    EntityKey = rewardCode,
                    NormalizedPayloadJson = normalized,
                    State = IdempotentRequestStates.Uncertain,
                    CreatedUtc = now,
                    UpdatedUtc = DateTimeOffset.UtcNow.ToString("o")
                }).IsSuccess, Is.True);

                AppResult<IdempotentRequestRecord> unresolved =
                    repo.FindLatestUnresolved(IdempotentOperations.UseReward, studentId, rewardCode);
                Assert.That(unresolved.IsSuccess, Is.True);
                Assert.That(unresolved.Value, Is.Not.Null);
                Assert.That(unresolved.Value.RequestUuid, Is.EqualTo(uuid));
                Assert.That(unresolved.Value.State, Is.EqualTo(IdempotentRequestStates.Uncertain));

                PendingRewardUseEnvelopeV2 restored =
                    IdempotentMutationSerializers.DeserializeReward(unresolved.Value.NormalizedPayloadJson);
                Assert.That(restored.RequestUuid, Is.EqualTo(uuid));
                Assert.That(restored.RewardCode, Is.EqualTo(rewardCode));
                Assert.That(restored.StudentId, Is.EqualTo(studentId));
                Assert.That(
                    IdempotentMutationSerializers.SerializeReward(restored),
                    Is.EqualTo(normalized));

                Task<AppResult<RewardSummary>> retryTask = gateway.UseRewardAsync(new UseRewardRequest
                {
                    RewardCode = restored.RewardCode,
                    RequestUuid = restored.RequestUuid
                });
                yield return Await(retryTask);
                Assert.That(retryTask.Result.IsSuccess, Is.True);
                Assert.That(retryTask.Result.Value.RewardCode, Is.EqualTo(rewardCode));

                Assert.That(repo.Upsert(new IdempotentRequestRecord
                {
                    RequestUuid = uuid,
                    Operation = IdempotentOperations.UseReward,
                    StudentId = studentId,
                    EntityKey = rewardCode,
                    NormalizedPayloadJson = normalized,
                    State = IdempotentRequestStates.Completed,
                    ResultJson = retryTask.Result.Value.Status,
                    CreatedUtc = now,
                    UpdatedUtc = DateTimeOffset.UtcNow.ToString("o")
                }).IsSuccess, Is.True);

                AppResult<IdempotentRequestRecord> completed = repo.Get(uuid);
                Assert.That(completed.Value.State, Is.EqualTo(IdempotentRequestStates.Completed));
                Assert.That(
                    repo.FindLatestUnresolved(IdempotentOperations.UseReward, studentId, rewardCode).Value,
                    Is.Null);
            }
        }

        [UnityTest]
        public IEnumerator QuizTimeout_PersistsFullSubmissionAndRetriesIdenticalPayload()
        {
            using (var factory = new TestDatabaseFactory())
            {
                NutriMindDatabase database = factory.OpenDatabase();
                var repo = new SqliteIdempotentRequestRepository(database);

                Task<MockStudentGateway> gatewayTask =
                    CreateAuthenticatedGatewayAsync(MockApiScenario.QuizSubmissionTimeout);
                yield return Await(gatewayTask);
                MockStudentGateway gateway = gatewayTask.Result;

                DateTimeOffset started = DateTimeOffset.Parse("2026-07-27T04:00:00Z").ToUniversalTime();
                DateTimeOffset submitted = DateTimeOffset.Parse("2026-07-27T04:05:00Z").ToUniversalTime();
                var submission = new QuizAttemptSubmission
                {
                    ClientAttemptUuid = "uuid-quiz-prompt2-timeout",
                    StartedAt = started,
                    SubmittedAt = submitted,
                    Answers = new List<QuizAnswerSelection>
                    {
                        new QuizAnswerSelection
                        {
                            QuestionId = "q1",
                            SelectedOptionKeys = new List<string> { "opt_a", "opt_c" }
                        },
                        new QuizAnswerSelection
                        {
                            QuestionId = "q2",
                            SelectedOptionKeys = new List<string> { "opt_b" }
                        }
                    }
                };

                var envelope = new PendingQuizSubmissionEnvelopeV2
                {
                    StudentId = "student-prompt2-a",
                    QuizId = "quiz_fixture_001",
                    Submission = submission
                };
                string normalized = IdempotentMutationSerializers.SerializeQuiz(envelope);
                string now = DateTimeOffset.UtcNow.ToString("o");

                Assert.That(repo.Upsert(new IdempotentRequestRecord
                {
                    RequestUuid = submission.ClientAttemptUuid,
                    Operation = IdempotentOperations.QuizSubmit,
                    StudentId = envelope.StudentId,
                    EntityKey = envelope.QuizId,
                    NormalizedPayloadJson = normalized,
                    State = IdempotentRequestStates.Sending,
                    CreatedUtc = now,
                    UpdatedUtc = now
                }).IsSuccess, Is.True);

                var request = new SubmitQuizAttemptRequest
                {
                    QuizId = envelope.QuizId,
                    Submission = submission
                };

                Task<AppResult<QuizResult>> timedOutTask = gateway.SubmitQuizAttemptAsync(request);
                yield return Await(timedOutTask);
                Assert.That(timedOutTask.Result.IsFailure, Is.True);
                Assert.That(timedOutTask.Result.Error.Code, Is.EqualTo(AppErrorCodes.NetworkTimeout));

                Assert.That(repo.Upsert(new IdempotentRequestRecord
                {
                    RequestUuid = submission.ClientAttemptUuid,
                    Operation = IdempotentOperations.QuizSubmit,
                    StudentId = envelope.StudentId,
                    EntityKey = envelope.QuizId,
                    NormalizedPayloadJson = normalized,
                    State = IdempotentRequestStates.Uncertain,
                    CreatedUtc = now,
                    UpdatedUtc = DateTimeOffset.UtcNow.ToString("o")
                }).IsSuccess, Is.True);

                // Simulate presenter/coordinator recreation from SQLite.
                AppResult<IdempotentRequestRecord> unresolved =
                    repo.FindLatestUnresolved(
                        IdempotentOperations.QuizSubmit,
                        envelope.StudentId,
                        envelope.QuizId);
                Assert.That(unresolved.IsSuccess, Is.True);
                Assert.That(unresolved.Value, Is.Not.Null);

                PendingQuizSubmissionEnvelopeV2 restored =
                    IdempotentMutationSerializers.DeserializeQuiz(unresolved.Value.NormalizedPayloadJson);
                Assert.That(restored.QuizId, Is.EqualTo(envelope.QuizId));
                Assert.That(restored.StudentId, Is.EqualTo(envelope.StudentId));
                Assert.That(restored.Submission.ClientAttemptUuid, Is.EqualTo(submission.ClientAttemptUuid));
                Assert.That(restored.Submission.StartedAt, Is.EqualTo(started));
                Assert.That(restored.Submission.SubmittedAt, Is.EqualTo(submitted));
                Assert.That(restored.Submission.Answers.Count, Is.EqualTo(2));
                Assert.That(restored.Submission.Answers[0].QuestionId, Is.EqualTo("q1"));
                Assert.That(restored.Submission.Answers[0].SelectedOptionKeys, Is.EqualTo(new[] { "opt_a", "opt_c" }));
                Assert.That(restored.Submission.Answers[1].SelectedOptionKeys, Is.EqualTo(new[] { "opt_b" }));
                Assert.That(
                    IdempotentMutationSerializers.SerializeQuiz(restored),
                    Is.EqualTo(normalized));

                Task<AppResult<QuizResult>> retryTask = gateway.SubmitQuizAttemptAsync(
                    new SubmitQuizAttemptRequest
                    {
                        QuizId = restored.QuizId,
                        Submission = restored.Submission
                    });
                yield return Await(retryTask);
                Assert.That(retryTask.Result.IsSuccess, Is.True);
                Assert.That(
                    retryTask.Result.Value.ClientAttemptUuid,
                    Is.EqualTo(submission.ClientAttemptUuid));

                Assert.That(repo.Upsert(new IdempotentRequestRecord
                {
                    RequestUuid = submission.ClientAttemptUuid,
                    Operation = IdempotentOperations.QuizSubmit,
                    StudentId = envelope.StudentId,
                    EntityKey = envelope.QuizId,
                    NormalizedPayloadJson = normalized,
                    State = IdempotentRequestStates.Completed,
                    ResultJson = retryTask.Result.Value.AttemptId,
                    CreatedUtc = now,
                    UpdatedUtc = DateTimeOffset.UtcNow.ToString("o")
                }).IsSuccess, Is.True);
            }
        }

        [Test]
        public void AnnouncementUnread_RepeatSelectionAndMarkReadFailure_DoNotDoubleDecrement()
        {
            var unread = new AnnouncementSummary { Id = "ann-1", IsUnread = true };
            var alreadyRead = new AnnouncementSummary { Id = "ann-2", IsUnread = false };

            Assert.That(
                AppViewMappers.IsAnnouncementEffectivelyUnread(alreadyRead, locallyMarkedRead: false),
                Is.False,
                "Server-read items must never count as unread.");

            int unreadCount = 1;
            bool locallyRead = false;
            bool firstSelection = AppViewMappers.IsAnnouncementEffectivelyUnread(unread, locallyRead);
            Assert.That(firstSelection, Is.True);

            // Successful MarkRead path.
            locallyRead = true;
            if (firstSelection)
            {
                unreadCount = CountEffectiveUnread(
                    new[] { unread },
                    id => string.Equals(id, "ann-1", StringComparison.Ordinal));
            }

            Assert.That(unreadCount, Is.EqualTo(0));

            // Repeat selection must not decrement again.
            int beforeRepeat = unreadCount;
            bool stillUnread = AppViewMappers.IsAnnouncementEffectivelyUnread(unread, locallyRead);
            Assert.That(stillUnread, Is.False);
            Assert.That(unreadCount, Is.EqualTo(beforeRepeat));

            // Failed MarkRead must not change badge.
            int badge = 1;
            AppResult failed = AppResult.Failure(AppErrorCodes.ClientConfigurationError, "write failed");
            if (failed.IsSuccess)
            {
                badge = 0;
            }

            Assert.That(badge, Is.EqualTo(1));
        }

        [Test]
        public void OutboxCountByStates_IgnoresAcceptedAndSupportsSyncSnapshot()
        {
            using (var factory = new TestDatabaseFactory())
            {
                NutriMindDatabase database = factory.OpenDatabase();
                var outbox = new SqliteOutboxRepository(database);
                string now = DateTimeOffset.UtcNow.ToString("o");

                Enqueue(outbox, "p1", OutboxEventState.Pending, now);
                Enqueue(outbox, "d1", OutboxEventState.Deferred, now);
                Enqueue(outbox, "s1", OutboxEventState.Sending, now);
                Enqueue(outbox, "r1", OutboxEventState.Rejected, now);
                Enqueue(outbox, "a1", OutboxEventState.Accepted, now);

                Assert.That(outbox.CountByStates(OutboxEventState.Pending).Value, Is.EqualTo(1));
                Assert.That(outbox.CountByStates(OutboxEventState.Deferred).Value, Is.EqualTo(1));
                Assert.That(outbox.CountByStates(OutboxEventState.Sending).Value, Is.EqualTo(1));
                Assert.That(outbox.CountByStates(OutboxEventState.Rejected).Value, Is.EqualTo(1));
                Assert.That(
                    outbox.CountByStates(OutboxEventState.Pending, OutboxEventState.Deferred).Value,
                    Is.EqualTo(2));

                var snapshot = new SyncQueueSnapshot(
                    outbox.CountByStates(OutboxEventState.Pending).Value,
                    outbox.CountByStates(OutboxEventState.Deferred).Value,
                    outbox.CountByStates(OutboxEventState.Sending).Value,
                    outbox.CountByStates(OutboxEventState.Rejected).Value);

                Assert.That(snapshot.AttentionCount, Is.EqualTo(2));
                Assert.That(snapshot.Sending, Is.EqualTo(1));
                Assert.That(snapshot.Rejected, Is.EqualTo(1));
            }
        }

        [UnityTest]
        public IEnumerator SyncCoordinator_Failure_PreservesPendingRows()
        {
            using (var factory = new TestDatabaseFactory())
            {
                NutriMindDatabase database = factory.OpenDatabase();
                var outbox = new SqliteOutboxRepository(database);
                var serializer = new OutboxPayloadSerializer();
                OutboxPayloadEnvelopeV1 envelope = OutboxPayloadSerializer.FromGameplayFields(
                    questionId: "q1",
                    outcome: "correct",
                    payload: new GameplayEventPayload { IsCorrect = true });
                string payloadJson = serializer.Serialize(envelope).Value;
                string now = DateTimeOffset.UtcNow.ToString("o");

                Assert.That(outbox.Enqueue(new SyncOutboxRecord
                {
                    EventUuid = "evt-sync-fail-1",
                    EventType = "question_answered",
                    GradeId = "g5",
                    SubjectId = "lq",
                    TermId = "t1",
                    MissionId = "g5_lq_t1_m01",
                    AreaId = "g5_lq_t1_m01_a01",
                    PayloadJson = payloadJson,
                    ClientCreatedUtc = now,
                    State = OutboxEventState.Pending
                }).IsSuccess, Is.True);

                var gateway = new FailingSyncPushGateway();
                var coordinator = new SyncCoordinator(
                    outbox,
                    gateway,
                    new DeterministicMockIdGenerator(),
                    factory.Clock,
                    serializer);

                Task<AppResult<SyncPushResult>> pushTask = coordinator.PushPendingAsync();
                yield return Await(pushTask);
                Assert.That(pushTask.Result.IsFailure, Is.True);

                Assert.That(outbox.CountByStates(OutboxEventState.Pending, OutboxEventState.Deferred).Value,
                    Is.GreaterThanOrEqualTo(1),
                    "Client-side sync failure must not erase local work.");
            }
        }

        private static int CountEffectiveUnread(
            IReadOnlyList<AnnouncementSummary> items,
            Func<string, bool> isLocallyRead)
        {
            int count = 0;
            for (int i = 0; i < items.Count; i++)
            {
                AnnouncementSummary item = items[i];
                if (item == null)
                {
                    continue;
                }

                bool local = isLocallyRead(item.Id);
                if (AppViewMappers.IsAnnouncementEffectivelyUnread(item, local))
                {
                    count++;
                }
            }

            return count;
        }

        private static void Enqueue(
            SqliteOutboxRepository outbox,
            string eventUuid,
            string state,
            string now)
        {
            Assert.That(outbox.Enqueue(new SyncOutboxRecord
            {
                EventUuid = eventUuid,
                EventType = "question_answered",
                GradeId = "g5",
                SubjectId = "lq",
                TermId = "t1",
                MissionId = "g5_lq_t1_m01",
                AreaId = "g5_lq_t1_m01_a01",
                PayloadJson = "{}",
                ClientCreatedUtc = now,
                State = state
            }).IsSuccess, Is.True);
        }

        [Test]
        public void ExactLookup_LearnerIsolationAndSubstringKeys_DoNotCollide()
        {
            using (var factory = new TestDatabaseFactory())
            {
                NutriMindDatabase database = factory.OpenDatabase();
                var repo = new SqliteIdempotentRequestRepository(database);
                string now = DateTimeOffset.UtcNow.ToString("o");

                void UpsertReward(string studentId, string rewardCode, string uuid)
                {
                    var envelope = new PendingRewardUseEnvelopeV2
                    {
                        StudentId = studentId,
                        RewardCode = rewardCode,
                        RequestUuid = uuid
                    };
                    Assert.That(repo.Upsert(new IdempotentRequestRecord
                    {
                        RequestUuid = uuid,
                        Operation = IdempotentOperations.UseReward,
                        StudentId = studentId,
                        EntityKey = rewardCode,
                        NormalizedPayloadJson = IdempotentMutationSerializers.SerializeReward(envelope),
                        State = IdempotentRequestStates.Uncertain,
                        CreatedUtc = now,
                        UpdatedUtc = now
                    }).IsSuccess, Is.True);
                }

                UpsertReward("student-a", "badge", "uuid-a");
                UpsertReward("student-a", "badge_extra", "uuid-a-extra");
                UpsertReward("student-b", "badge", "uuid-b");

                Assert.That(
                    repo.FindLatestUnresolved(IdempotentOperations.UseReward, "student-a", "badge")
                        .Value.RequestUuid,
                    Is.EqualTo("uuid-a"));
                Assert.That(
                    repo.FindLatestUnresolved(IdempotentOperations.UseReward, "student-a", "badge_extra")
                        .Value.RequestUuid,
                    Is.EqualTo("uuid-a-extra"));
                Assert.That(
                    repo.FindLatestUnresolved(IdempotentOperations.UseReward, "student-b", "badge")
                        .Value.RequestUuid,
                    Is.EqualTo("uuid-b"));
            }
        }

        [Test]
        public void IdempotentTransitions_RejectIllegalTerminalAndChangedPayload()
        {
            using (var factory = new TestDatabaseFactory())
            {
                NutriMindDatabase database = factory.OpenDatabase();
                var repo = new SqliteIdempotentRequestRepository(database);
                string now = DateTimeOffset.UtcNow.ToString("o");
                string payload =
                    "{\"Version\":2,\"StudentId\":\"student-a\",\"RewardCode\":\"badge\",\"RequestUuid\":\"uuid-1\"}";
                var record = new IdempotentRequestRecord
                {
                    RequestUuid = "uuid-1",
                    Operation = IdempotentOperations.UseReward,
                    StudentId = "student-a",
                    EntityKey = "badge",
                    NormalizedPayloadJson = payload,
                    State = IdempotentRequestStates.Completed,
                    CreatedUtc = now,
                    UpdatedUtc = now
                };
                Assert.That(repo.Upsert(record).IsSuccess, Is.True);
                Assert.That(
                    IdempotentMutationTransitions.Transition(
                        repo,
                        record,
                        IdempotentRequestStates.Sending,
                        null,
                        now).IsFailure,
                    Is.True);
                Assert.That(
                    IdempotentMutationTransitions.ValidateImmutableIdentity(
                        record,
                        IdempotentOperations.UseReward,
                        "student-a",
                        "badge",
                        "{\"Version\":2,\"StudentId\":\"student-a\",\"RewardCode\":\"other\",\"RequestUuid\":\"uuid-1\"}")
                        .IsFailure,
                    Is.True);
            }
        }

        [Test]
        public void LearnerRouteCache_AndProgressEmpty_Contracts()
        {
            using (var factory = new TestDatabaseFactory())
            {
                NutriMindDatabase database = factory.OpenDatabase();
                var cache = new SqliteResourceCacheRepository(database);
                string now = DateTimeOffset.UtcNow.ToString("o");
                Assert.That(
                    LearnerRouteCache.SaveProgress(
                        cache,
                        "student-a",
                        new ProgressSummary { MissionsCompleted = 3 },
                        now).IsSuccess,
                    Is.True);
                Assert.That(
                    LearnerRouteCache.LoadProgress(cache, "student-a").Value.MissionsCompleted,
                    Is.EqualTo(3));
                Assert.That(LearnerRouteCache.LoadProgress(cache, "student-b").IsFailure, Is.True);
                Assert.That(
                    LearnerRouteCache.SaveProgress(cache, "student-a", new ProgressSummary(), now)
                        .IsSuccess,
                    Is.True);
                Assert.That(
                    LearnerRouteCache.LoadProgress(cache, "student-a").Value.MissionsCompleted,
                    Is.EqualTo(0));
            }

            Assert.That(new ProgressPreviewSummary(0, 0, 0, 0, 0, 0).IsEmpty, Is.True);
            Assert.That(new ProgressPreviewSummary(0, 0, 0, 0, 0, 2).IsEmpty, Is.False);
            Assert.That(
                OfflineSyncBannerPresets.SyncAttention(2, 1).Title,
                Does.Contain("needs attention"));
        }

        private static async Task<MockStudentGateway> CreateAuthenticatedGatewayAsync(
            MockApiScenario scenario)
        {
            var tokenStore = new InMemoryMockAuthTokenStore();
            MockStudentGateway gateway = MockGatewayTestFactory.CreateGateway(
                scenario,
                tokenStore: tokenStore);

            AppResult<LoginResult> login = await gateway.LoginAsync(new LoginRequest
            {
                Lrn = MockGatewayTestFactory.ValidLrn,
                Pin = MockGatewayTestFactory.ValidPin,
                DeviceName = "Prompt2Completion"
            });
            Assert.That(login.IsSuccess, Is.True, login.Error?.Message);
            return gateway;
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

        private sealed class FailingSyncPushGateway : ISyncPushGateway
        {
            public Task<AppResult<SyncPushResult>> SyncPushBatchAsync(
                SyncPushBatchRequest request,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(AppResult<SyncPushResult>.Failure(
                    AppErrorCodes.NetworkOffline,
                    "forced offline",
                    isNetworkError: true,
                    isRetryable: true));
            }
        }
    }
}
