using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NutriMind.App.Composition;
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
    /// Critical Prompt 1 sync foundation: lossless payload mapping and push concurrency.
    /// </summary>
    public sealed class SyncPayloadAndConcurrencyTests
    {
        [Test]
        public void OutboxPayload_SerializesQuestionAndCollectible_AndMapsAllFields()
        {
            var serializer = new OutboxPayloadSerializer();

            OutboxPayloadEnvelopeV1 question = OutboxPayloadSerializer.FromGameplayFields(
                manifestVersion: "v5",
                encounterId: "enc-1",
                questionId: "g5_lq_t1_m01_a01_q01",
                attemptNumber: 2,
                outcome: "correct",
                reviewRequired: false,
                payload: new GameplayEventPayload
                {
                    SelectedOptionKeys = new[] { "a", "c" },
                    IsCorrect = true,
                    HintShown = true,
                    ExplanationShown = false,
                    ObservationCode = "obs",
                    PredictionCode = "pred",
                    MaterialIds = new[] { "mat-1" },
                    InvestigationActionId = "inv",
                    ResultCode = "res",
                    ConclusionCode = "con",
                    SolutionActionId = "sol",
                    HealthActionId = "health",
                    WellnessResultId = "well",
                    Value = 12.5f,
                    Unit = "kg",
                    ReviewReason = "none"
                });

            AppResult<string> questionJson = serializer.Serialize(question);
            Assert.That(questionJson.IsSuccess, Is.True);

            OutboxPayloadEnvelopeV1 collectible = OutboxPayloadSerializer.FromGameplayFields(
                manifestVersion: "v5",
                collectibleId: "g5_lq_t1_m01_a01_c01",
                outcome: "collected");
            AppResult<string> collectibleJson = serializer.Serialize(collectible);
            Assert.That(collectibleJson.IsSuccess, Is.True);

            AppResult<OutboxPayloadEnvelopeV1> parsedQuestion =
                serializer.Deserialize(questionJson.Value);
            Assert.That(parsedQuestion.IsSuccess, Is.True);

            var source = new NutriMind.Core.Sync.SyncPushEvent
            {
                EventUuid = "evt-q-1",
                EventType = "question_answered",
                GradeId = "g5",
                SubjectId = "lq",
                TermId = "t1",
                MissionId = "g5_lq_t1_m01",
                AreaId = "g5_lq_t1_m01_a01",
                LocalSequence = 3,
                PayloadJson = questionJson.Value,
                ClientCreatedUtc = "2026-07-27T04:00:00Z"
            };

            AppResult<NutriMind.Core.Networking.SyncPushEvent> mapped =
                serializer.MapToNetworkEvent(source, parsedQuestion.Value);
            Assert.That(mapped.IsSuccess, Is.True);
            Assert.That(mapped.Value.QuestionId, Is.EqualTo("g5_lq_t1_m01_a01_q01"));
            Assert.That(mapped.Value.EncounterId, Is.EqualTo("enc-1"));
            Assert.That(mapped.Value.AttemptNumber, Is.EqualTo(2));
            Assert.That(mapped.Value.Outcome, Is.EqualTo("correct"));
            Assert.That(mapped.Value.ManifestVersion, Is.EqualTo("v5"));
            Assert.That(mapped.Value.Payload.SelectedOptionKeys, Is.EquivalentTo(new[] { "a", "c" }));
            Assert.That(mapped.Value.Payload.IsCorrect, Is.True);
            Assert.That(mapped.Value.Payload.HintShown, Is.True);
            Assert.That(mapped.Value.Payload.Value, Is.EqualTo(12.5f));
            Assert.That(mapped.Value.Payload.Unit, Is.EqualTo("kg"));
            Assert.That(mapped.Value.Payload.HealthActionId, Is.EqualTo("health"));

            AppResult<OutboxPayloadEnvelopeV1> parsedCollectible =
                serializer.Deserialize(collectibleJson.Value);
            Assert.That(parsedCollectible.Value.collectibleId, Is.EqualTo("g5_lq_t1_m01_a01_c01"));
        }

        [UnityTest]
        public IEnumerator OutboxPayload_Malformed_IsDeferredOrRejected_AndPreserved()
        {
            string dbPath = Path.Combine(
                Path.GetTempPath(),
                "nutrimind-sync-payload-" + Guid.NewGuid().ToString("N") + ".db");
            var clock = new FixedMockClock();
            var database = new NutriMindDatabase(clock, dbPath);
            Assert.That(database.Open().IsSuccess, Is.True);

            try
            {
                var outbox = new SqliteOutboxRepository(database);
                var serializer = new OutboxPayloadSerializer();
                var countingGateway = new CountingSyncPushGateway();
                var coordinator = new SyncCoordinator(
                    outbox,
                    countingGateway,
                    new DeterministicMockIdGenerator(),
                    clock,
                    serializer);

                Assert.That(outbox.Enqueue(new SyncOutboxRecord
                {
                    EventUuid = "evt-bad-1",
                    EventType = "question_answered",
                    GradeId = "g5",
                    SubjectId = "lq",
                    TermId = "t1",
                    MissionId = "g5_lq_t1_m01",
                    AreaId = "g5_lq_t1_m01_a01",
                    PayloadJson = "{not-valid-json",
                    ClientCreatedUtc = clock.UtcNow.ToString("o"),
                    State = OutboxEventState.Pending
                }).IsSuccess, Is.True);

                Assert.That(outbox.Enqueue(new SyncOutboxRecord
                {
                    EventUuid = "evt-bad-version",
                    EventType = "collectible_collected",
                    GradeId = "g5",
                    SubjectId = "lq",
                    TermId = "t1",
                    MissionId = "g5_lq_t1_m01",
                    AreaId = "g5_lq_t1_m01_a01",
                    PayloadJson = "{\"schemaVersion\":99}",
                    ClientCreatedUtc = clock.UtcNow.ToString("o"),
                    State = OutboxEventState.Pending
                }).IsSuccess, Is.True);

                Task<AppResult<SyncPushResult>> push = coordinator.PushPendingAsync();
                yield return Await(push);

                Assert.That(push.Result.IsSuccess, Is.True);
                Assert.That(countingGateway.SubmissionCount, Is.EqualTo(0));
                IReadOnlyList<SyncOutboxRecord> rows = outbox.GetAllAscending().Value;
                Assert.That(rows.Count, Is.EqualTo(2));
                Assert.That(rows[0].PayloadJson, Is.EqualTo("{not-valid-json"));
                Assert.That(rows[0].State, Is.EqualTo(OutboxEventState.Rejected));
                Assert.That(rows[0].LastErrorCode, Is.EqualTo(AppErrorCodes.SyncPayloadInvalid));
                Assert.That(rows[1].State, Is.EqualTo(OutboxEventState.Deferred));
                Assert.That(rows[1].LastErrorCode, Is.EqualTo(AppErrorCodes.SyncPayloadVersionUnsupported));
            }
            finally
            {
                database.Dispose();
                TryDelete(dbPath);
                TryDelete(dbPath + "-wal");
                TryDelete(dbPath + "-shm");
            }
        }

        [UnityTest]
        public IEnumerator SyncConcurrency_SecondPushReturnsBusy_WithoutDuplicateSubmission()
        {
            string dbPath = Path.Combine(
                Path.GetTempPath(),
                "nutrimind-sync-busy-" + Guid.NewGuid().ToString("N") + ".db");
            var clock = new FixedMockClock();
            var database = new NutriMindDatabase(clock, dbPath);
            Assert.That(database.Open().IsSuccess, Is.True);

            try
            {
                var outbox = new SqliteOutboxRepository(database);
                var serializer = new OutboxPayloadSerializer();
                OutboxPayloadEnvelopeV1 envelope = OutboxPayloadSerializer.FromGameplayFields(
                    questionId: "q1",
                    outcome: "correct",
                    payload: new GameplayEventPayload { IsCorrect = true });
                string payloadJson = serializer.Serialize(envelope).Value;

                Assert.That(outbox.Enqueue(new SyncOutboxRecord
                {
                    EventUuid = "evt-concurrency-1",
                    EventType = "question_answered",
                    GradeId = "g5",
                    SubjectId = "lq",
                    TermId = "t1",
                    MissionId = "g5_lq_t1_m01",
                    AreaId = "g5_lq_t1_m01_a01",
                    PayloadJson = payloadJson,
                    ClientCreatedUtc = clock.UtcNow.ToString("o"),
                    State = OutboxEventState.Pending
                }).IsSuccess, Is.True);

                var slowGateway = new SlowCountingSyncPushGateway(TimeSpan.FromMilliseconds(250));
                var coordinator = new SyncCoordinator(
                    outbox,
                    slowGateway,
                    new DeterministicMockIdGenerator(),
                    clock,
                    serializer);

                Task<AppResult<SyncPushResult>> first = coordinator.PushPendingAsync();
                Task<AppResult<SyncPushResult>> second = coordinator.PushPendingAsync();
                yield return Await(Task.WhenAll(first, second));

                Assert.That(first.Result.IsSuccess, Is.True);
                Assert.That(second.Result.IsFailure, Is.True);
                Assert.That(second.Result.Error.Code, Is.EqualTo(AppErrorCodes.SyncInProgress));
                Assert.That(slowGateway.SubmissionCount, Is.EqualTo(1));
            }
            finally
            {
                database.Dispose();
                TryDelete(dbPath);
                TryDelete(dbPath + "-wal");
                TryDelete(dbPath + "-shm");
            }
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

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
                // ignore
            }
        }

        private sealed class CountingSyncPushGateway : ISyncPushGateway
        {
            public int SubmissionCount { get; private set; }

            public Task<AppResult<SyncPushResult>> SyncPushBatchAsync(
                SyncPushBatchRequest request,
                CancellationToken cancellationToken = default)
            {
                SubmissionCount++;
                return Task.FromResult(AppResult<SyncPushResult>.Success(new SyncPushResult
                {
                    BatchUuid = request?.BatchUuid,
                    ServerRevision = 1,
                    Events = Array.Empty<SyncPushEventResult>()
                }));
            }
        }

        private sealed class SlowCountingSyncPushGateway : ISyncPushGateway
        {
            private readonly TimeSpan _delay;

            public SlowCountingSyncPushGateway(TimeSpan delay)
            {
                _delay = delay;
            }

            public int SubmissionCount { get; private set; }

            public async Task<AppResult<SyncPushResult>> SyncPushBatchAsync(
                SyncPushBatchRequest request,
                CancellationToken cancellationToken = default)
            {
                SubmissionCount++;
                await Task.Delay(_delay, cancellationToken).ConfigureAwait(false);
                var events = new List<SyncPushEventResult>();
                if (request?.Events != null)
                {
                    for (int i = 0; i < request.Events.Count; i++)
                    {
                        events.Add(new SyncPushEventResult
                        {
                            EventUuid = request.Events[i].EventUuid,
                            Status = OutboxEventState.Accepted
                        });
                    }
                }

                return AppResult<SyncPushResult>.Success(new SyncPushResult
                {
                    BatchUuid = request?.BatchUuid,
                    ServerRevision = 1,
                    AcceptedCount = events.Count,
                    Events = events
                });
            }
        }
    }
}
