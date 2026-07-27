using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NutriMind.Core.Data;
using NutriMind.Core.Networking;
using NutriMind.Core.Persistence;
using NutriMind.Core.Utilities;
using NutriMind.Tests.TestData;
using NUnit.Framework;
using SQLite;
using UnityEngine;
using UnityEngine.TestTools;

namespace NutriMind.Tests.EditMode
{
    public sealed class NutriMindPersistenceTests
    {
        private TestDatabaseFactory _factory;
        private NutriMindDatabase _database;
        private DeterministicMockIdGenerator _ids;
        private FixedMockClock _clock;

        [SetUp]
        public void SetUp()
        {
            _clock = new FixedMockClock();
            _ids = new DeterministicMockIdGenerator();
            _factory = new TestDatabaseFactory(_clock);
            _database = _factory.OpenDatabase();
        }

        [TearDown]
        public void TearDown()
        {
            _factory?.Dispose();
            _factory = null;
            _database = null;
        }

        [Test]
        public void Migration_FreshDb_AppliesSchemaVersionOne()
        {
            Assert.That(_database.SchemaVersion, Is.EqualTo(1));
            Assert.That(_database.IsOpen, Is.True);
        }

        [Test]
        public void Migration_Reopen_IsIdempotent()
        {
            Assert.That(_database.SchemaVersion, Is.EqualTo(1));
            _database = _factory.ReopenDatabase();
            Assert.That(_database.SchemaVersion, Is.EqualTo(1));
            Assert.That(_database.IsOpen, Is.True);
        }

        [Test]
        public void ProgressPlusOutbox_WriterFailure_RollsBackBoth()
        {
            var writer = new LocalProgressWriter(_database, _clock);
            var progressRepo = new SqliteMissionProgressRepository(_database);
            var outboxRepo = new SqliteOutboxRepository(_database);

            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(".*Transaction failed.*"));
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(".*rolled back.*"));

            AppResult failed = writer.Commit(new LocalProgressWriteRequest
            {
                MissionProgress = new MissionProgressRecord
                {
                    MissionId = "g5_lq_t1_m01",
                    State = "in_progress",
                    RequiredAreaCount = 3,
                    RequiredCollectibleCount = 3,
                    Revision = 1,
                    StartedUtc = _clock.UtcNow.ToString("o")
                },
                OutboxEvent = new SyncOutboxRecord
                {
                    EventUuid = "evt-rollback-1",
                    EventType = "mission_started",
                    GradeId = "grade_5",
                    SubjectId = "subject_literaquest",
                    TermId = "term_1",
                    MissionId = "g5_lq_t1_m01",
                    PayloadJson = "{\"ok\":true}",
                    ClientCreatedUtc = _clock.UtcNow.ToString("o"),
                    State = "not_a_real_state"
                }
            });

            Assert.That(failed.IsFailure, Is.True);
            Assert.That(progressRepo.GetMission("g5_lq_t1_m01").Value, Is.Null);
            Assert.That(outboxRepo.GetAllAscending().Value.Count, Is.EqualTo(0));
        }

        [Test]
        public void InstallationUuid_PersistsAcrossReopen()
        {
            var repo = new SqliteInstallationRepository(_database, _ids, _clock);
            AppResult<string> created = repo.GetOrCreateDeviceId();
            Assert.That(created.IsSuccess, Is.True);
            string deviceId = created.Value;

            _database = _factory.ReopenDatabase();
            var reopened = new SqliteInstallationRepository(_database, _ids, _clock);
            AppResult<string> again = reopened.GetOrCreateDeviceId();

            Assert.That(again.IsSuccess, Is.True);
            Assert.That(again.Value, Is.EqualTo(deviceId));
        }

        [Test]
        public void ResourceCache_RoundTrip()
        {
            var repo = new SqliteResourceCacheRepository(_database);
            var record = new ResourceCacheRecord
            {
                CacheKey = ResourceCacheKeys.Bootstrap,
                PayloadJson = "{\"cached\":true}",
                SchemaVersion = 1,
                ServerRevision = 3,
                CachedUtc = _clock.UtcNow.ToString("o")
            };

            Assert.That(repo.Upsert(record).IsSuccess, Is.True);
            AppResult<ResourceCacheRecord> loaded = repo.Get(ResourceCacheKeys.Bootstrap);
            Assert.That(loaded.IsSuccess, Is.True);
            Assert.That(loaded.Value.PayloadJson, Is.EqualTo(record.PayloadJson));
            Assert.That(loaded.Value.ServerRevision, Is.EqualTo(3));
        }

        [Test]
        public void Session_RoundTrip_HasNoTokenField()
        {
            var repo = new SqliteLocalSessionRepository(_database);
            var session = new LocalSessionRecord
            {
                StudentId = "student_fixture_001",
                DisplayName = "Pathfinder",
                GradeId = "grade_5",
                SectionId = "section_g5_a",
                SectionName = "Grade 5 A",
                LastAuthenticatedUtc = _clock.UtcNow.ToString("o"),
                LastBootstrapRevision = 2,
                LastBootstrapCachedUtc = _clock.UtcNow.ToString("o"),
                OfflineEligible = true
            };

            Assert.That(repo.UpsertSession(session).IsSuccess, Is.True);
            AppResult<LocalSessionRecord> loaded = repo.GetSession();
            Assert.That(loaded.IsSuccess, Is.True);
            Assert.That(loaded.Value.StudentId, Is.EqualTo(session.StudentId));
            Assert.That(loaded.Value.DisplayName, Is.EqualTo(session.DisplayName));
            Assert.That(loaded.Value.OfflineEligible, Is.True);

            foreach (PropertyInfo property in typeof(LocalSessionRecord).GetProperties(
                         BindingFlags.Instance | BindingFlags.Public))
            {
                string name = property.Name.ToLowerInvariant();
                Assert.That(name.Contains("token") || name.Contains("bearer"), Is.False,
                    "LocalSessionRecord must not expose token fields (" + property.Name + ").");
            }
        }

        [Test]
        public void AnnouncementRead_RoundTrip()
        {
            var repo = new SqliteAnnouncementReadRepository(_database);
            Assert.That(repo.IsRead("ann_001").Value, Is.False);

            Assert.That(repo.MarkRead("ann_001", _clock.UtcNow.ToString("o")).IsSuccess, Is.True);
            Assert.That(repo.IsRead("ann_001").Value, Is.True);
            Assert.That(repo.GetAll().Value.Count, Is.EqualTo(1));
        }

        [Test]
        public void ProgressPlusOutbox_AtomicTransaction_Succeeds()
        {
            var writer = new LocalProgressWriter(_database, _clock);
            var progressRepo = new SqliteMissionProgressRepository(_database);
            var outboxRepo = new SqliteOutboxRepository(_database);
            bool changed = false;
            writer.LocalStateChanged += () => changed = true;

            AppResult result = writer.Commit(new LocalProgressWriteRequest
            {
                MissionProgress = new MissionProgressRecord
                {
                    MissionId = "g5_lq_t1_m01",
                    State = "in_progress",
                    ActiveAreaId = "g5_lq_t1_m01_a01",
                    RequiredAreaCount = 3,
                    RequiredCollectibleCount = 3,
                    Revision = 1,
                    StartedUtc = _clock.UtcNow.ToString("o")
                },
                OutboxEvent = CreateOutbox("evt-atomic-1", "mission_started", "g5_lq_t1_m01")
            });

            Assert.That(result.IsSuccess, Is.True, result.Error?.Code + " — " + result.Error?.Message);
            Assert.That(changed, Is.True);
            Assert.That(progressRepo.GetMission("g5_lq_t1_m01").Value, Is.Not.Null);
            IReadOnlyList<SyncOutboxRecord> outbox = outboxRepo.GetAllAscending().Value;
            Assert.That(outbox.Count, Is.EqualTo(1));
            Assert.That(outbox[0].State, Is.EqualTo(OutboxEventState.Pending));
            Assert.That(outbox[0].LocalSequence, Is.EqualTo(1));
        }

        [Test]
        public void Outbox_OrderingAndSendingRecovery()
        {
            var outbox = new SqliteOutboxRepository(_database);

            Assert.That(outbox.Enqueue(CreateOutbox("evt-1", "a", "g5_lq_t1_m01")).IsSuccess, Is.True);
            Assert.That(outbox.Enqueue(CreateOutbox("evt-2", "b", "g5_lq_t1_m01")).IsSuccess, Is.True);

            IReadOnlyList<SyncOutboxRecord> pushable = outbox.GetPushableAscending(10).Value;
            Assert.That(pushable.Count, Is.EqualTo(2));
            Assert.That(pushable[0].EventUuid, Is.EqualTo("evt-1"));
            Assert.That(pushable[1].EventUuid, Is.EqualTo("evt-2"));

            Assert.That(
                outbox.MarkSending(new[] { "evt-1" }, _clock.UtcNow.ToString("o")).IsSuccess,
                Is.True);
            Assert.That(outbox.GetPushableAscending(10).Value.Count, Is.EqualTo(1));
            Assert.That(outbox.CountByStates(OutboxEventState.Sending).Value, Is.EqualTo(1));

            Assert.That(outbox.RecoverInterruptedSending().IsSuccess, Is.True);
            Assert.That(outbox.CountByStates(OutboxEventState.Sending).Value, Is.EqualTo(0));
            Assert.That(outbox.CountByStates(OutboxEventState.Pending).Value, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator TokenAbsentFromSqlite_AfterLoginMetadataCached()
        {
            var tokenStore = new InMemoryMockAuthTokenStore();
            MockStudentGateway gateway = MockGatewayTestFactory.CreateGateway(tokenStore: tokenStore);

            Task<AppResult<LoginResult>> loginTask = gateway.LoginAsync(new LoginRequest
            {
                Lrn = MockGatewayTestFactory.ValidLrn,
                Pin = MockGatewayTestFactory.ValidPin,
                DeviceName = "TokenAuditDevice"
            });
            while (!loginTask.IsCompleted)
            {
                yield return null;
            }

            if (loginTask.IsFaulted)
            {
                throw loginTask.Exception;
            }

            AppResult<LoginResult> login = loginTask.Result;
            Assert.That(login.IsSuccess, Is.True, login.Error?.Message);
            string accessToken = login.Value.AccessToken;
            Assert.That(accessToken, Is.Not.Null.And.Not.Empty);

            string now = _clock.UtcNow.ToString("o");
            var sessionRepo = new SqliteLocalSessionRepository(_database);
            var cacheRepo = new SqliteResourceCacheRepository(_database);

            Assert.That(sessionRepo.UpsertSession(new LocalSessionRecord
            {
                StudentId = login.Value.Student.Id,
                DisplayName = login.Value.Student.DisplayName,
                GradeId = login.Value.Student.GradeId,
                SectionId = login.Value.Student.Section?.Id ?? "section",
                SectionName = login.Value.Student.Section?.Name ?? "Section",
                LastAuthenticatedUtc = now,
                LastBootstrapRevision = 1,
                LastBootstrapCachedUtc = now,
                OfflineEligible = true
            }).IsSuccess, Is.True);

            Assert.That(cacheRepo.Upsert(new ResourceCacheRecord
            {
                CacheKey = ResourceCacheKeys.Bootstrap,
                PayloadJson = "{\"cached\":true,\"student_id\":\"" + login.Value.Student.Id + "\"}",
                SchemaVersion = 1,
                CachedUtc = now
            }).IsSuccess, Is.True);

            Task<string> readTask = tokenStore.ReadAsync();
            while (!readTask.IsCompleted)
            {
                yield return null;
            }

            Assert.That(readTask.Result, Is.EqualTo(accessToken));

            string dbPath = _factory.DatabaseFilePath;
            _database.Dispose();
            _database = null;

            AssertTokenAbsentFromDatabaseFile(dbPath, accessToken);
            _database = _factory.ReopenDatabase();
        }

        private static void AssertTokenAbsentFromDatabaseFile(string dbPath, string accessToken)
        {
            byte[] bytes = File.ReadAllBytes(dbPath);
            string asText = Encoding.UTF8.GetString(bytes);
            Assert.That(asText, Does.Not.Contain(accessToken));
            Assert.That(asText.IndexOf("Bearer", StringComparison.OrdinalIgnoreCase), Is.LessThan(0));

            using var connection = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadOnly);
            List<SqliteMasterRow> tables = connection.Query<SqliteMasterRow>(
                "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'");

            foreach (SqliteMasterRow tableRow in tables)
            {
                string table = tableRow.name;
                List<PragmaColumnInfo> columns =
                    connection.Query<PragmaColumnInfo>("PRAGMA table_info(\"" + table + "\")");

                foreach (PragmaColumnInfo column in columns)
                {
                    string columnName = column.name.ToLowerInvariant();
                    Assert.That(
                        columnName.Contains("token") || columnName.Contains("bearer"),
                        Is.False,
                        "Unexpected token-like column " + table + "." + column.name);

                    string type = column.type ?? string.Empty;
                    bool isTextish = string.IsNullOrWhiteSpace(type)
                        || type.IndexOf("TEXT", StringComparison.OrdinalIgnoreCase) >= 0
                        || type.IndexOf("CHAR", StringComparison.OrdinalIgnoreCase) >= 0;
                    if (!isTextish)
                    {
                        continue;
                    }

                    List<TextCell> cells = connection.Query<TextCell>(
                        "SELECT \"" + column.name + "\" AS value FROM \"" + table + "\"");
                    foreach (TextCell cell in cells)
                    {
                        if (string.IsNullOrEmpty(cell.value))
                        {
                            continue;
                        }

                        Assert.That(cell.value, Does.Not.Contain(accessToken));
                        Assert.That(
                            cell.value.IndexOf("mock-access-token", StringComparison.OrdinalIgnoreCase),
                            Is.LessThan(0));
                    }
                }
            }
        }

        private SyncOutboxRecord CreateOutbox(string eventUuid, string eventType, string missionId)
        {
            return new SyncOutboxRecord
            {
                EventUuid = eventUuid,
                EventType = eventType,
                GradeId = "grade_5",
                SubjectId = "subject_literaquest",
                TermId = "term_1",
                MissionId = missionId,
                PayloadJson = "{\"mission_id\":\"" + missionId + "\"}",
                ClientCreatedUtc = _clock.UtcNow.ToString("o"),
                State = OutboxEventState.Pending
            };
        }

        private sealed class SqliteMasterRow
        {
            public string name { get; set; }
        }

        private sealed class PragmaColumnInfo
        {
            public int cid { get; set; }
            public string name { get; set; }
            public string type { get; set; }
        }

        private sealed class TextCell
        {
            public string value { get; set; }
        }
    }
}
