using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using NutriMind.App.Composition;
using NutriMind.App.Features;
using NutriMind.Core.Bootstrap;
using NutriMind.Core.Data;
using NutriMind.Core.Networking;
using NutriMind.Core.Persistence;
using NutriMind.Core.Utilities;
using NutriMind.Tests.TestData;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace NutriMind.Tests.EditMode
{
    /// <summary>
    /// Critical Prompt 1 foundation coverage: live scenario, reset/recompose, offline restore, cache round-trip.
    /// </summary>
    public sealed class MockRuntimeFoundationTests
    {
        [UnityTest]
        public IEnumerator LiveScenario_ChangesAffectSameGateway()
        {
            var connectivity = new MockConnectivityService(startOnline: true);
            var serverState = new MockServerState();
            var mockRuntime = new MockRuntimeState(MockApiScenario.HappyPath, connectivity, serverState);
            MockStudentGateway gateway = MockGatewayTestFactory.CreateGateway(
                MockApiScenario.HappyPath,
                connectivity: connectivity,
                state: serverState,
                mockRuntime: mockRuntime);

            Task<AppResult<LoginResult>> happy = gateway.LoginAsync(new LoginRequest
            {
                Lrn = MockGatewayTestFactory.ValidLrn,
                Pin = MockGatewayTestFactory.ValidPin,
                DeviceName = "EditMode"
            });
            yield return Await(happy);
            Assert.That(happy.Result.IsSuccess, Is.True);

            mockRuntime.SetScenario(MockApiScenario.RateLimitedLogin);
            Task<AppResult<LoginResult>> limited = gateway.LoginAsync(new LoginRequest
            {
                Lrn = MockGatewayTestFactory.ValidLrn,
                Pin = MockGatewayTestFactory.ValidPin,
                DeviceName = "EditMode"
            });
            yield return Await(limited);
            Assert.That(limited.Result.IsFailure, Is.True);
            Assert.That(limited.Result.Error.Code, Is.EqualTo(AppErrorCodes.RateLimited));

            mockRuntime.SetScenario(MockApiScenario.HappyPath);
            Task<AppResult<LoginResult>> again = gateway.LoginAsync(new LoginRequest
            {
                Lrn = MockGatewayTestFactory.ValidLrn,
                Pin = MockGatewayTestFactory.ValidPin,
                DeviceName = "EditMode"
            });
            yield return Await(again);
            Assert.That(again.Result.IsSuccess, Is.True);
        }

        [Test]
        public void ResetLocalDatabase_Recompose_LeavesReadyServices()
        {
            string dbPath = Path.Combine(Path.GetTempPath(), "nutrimind-reset-" + Guid.NewGuid().ToString("N") + ".db");
            try
            {
                var options = NutriMindRuntimeOptions.CreateDefaults();
                options.Mode = NutriMindRuntimeMode.Mock;
                options.MinimumMockLatencyMilliseconds = 0;
                options.MaximumMockLatencyMilliseconds = 0;
                options.LogGatewayOperations = false;

                var root = new AppCompositionRoot(options);
                Assert.That(root.Compose().IsSuccess, Is.True);
                Assert.That(root.Database.IsOpen, Is.True);
                Assert.That(root.Database.SchemaVersion, Is.EqualTo(1));
                string firstInstall = root.InstallationRepository.GetOrCreateDeviceId().Value;

                root.Dispose();
                TryDelete(dbPath);
                // Composition uses default path; exercise Recompose via a second root after wipe.
                TryDelete(NutriMindDatabase.GetDefaultDatabasePath());
                TryDelete(NutriMindDatabase.GetDefaultDatabasePath() + "-wal");
                TryDelete(NutriMindDatabase.GetDefaultDatabasePath() + "-shm");

                var recomposed = new AppCompositionRoot(options);
                Assert.That(recomposed.Compose().IsSuccess, Is.True);
                Assert.That(recomposed.Database.IsOpen, Is.True);
                Assert.That(recomposed.Database.SchemaVersion, Is.EqualTo(1));
                Assert.That(recomposed.Gateway, Is.Not.Null);
                Assert.That(recomposed.Router, Is.Not.Null);
                Assert.That(recomposed.SceneNavigator, Is.Not.Null);
                Assert.That(recomposed.MissionProgressRepository, Is.Not.Null);
                Assert.That(recomposed.LocalProgressWriter, Is.Not.Null);
                Assert.That(recomposed.AnnouncementReadRepository, Is.Not.Null);
                Assert.That(recomposed.IdempotentRequestRepository, Is.Not.Null);
                Assert.That(recomposed.MockRuntimeState, Is.Not.Null);

                string firstAfterRecompose =
                    recomposed.InstallationRepository.GetOrCreateDeviceId().Value;
                AppResult<string> regenerated =
                    recomposed.InstallationRepository.RegenerateDeviceIdForFullInstallReset();
                Assert.That(regenerated.IsSuccess, Is.True);
                Assert.That(regenerated.Value, Is.Not.EqualTo(firstInstall));
                Assert.That(regenerated.Value, Is.Not.EqualTo(firstAfterRecompose));
                recomposed.Dispose();
            }
            finally
            {
                TryDelete(dbPath);
            }
        }

        [UnityTest]
        public IEnumerator OfflineColdRestart_RestoresBootstrapFromCache()
        {
            string tokenPath = Path.Combine(
                Path.GetTempPath(),
                "mock-dev-auth-" + Guid.NewGuid().ToString("N") + ".dat");
            string dbPath = Path.Combine(
                Path.GetTempPath(),
                "nutrimind-offline-" + Guid.NewGuid().ToString("N") + ".db");

            try
            {
                var options = NutriMindRuntimeOptions.CreateDefaults();
                options.Mode = NutriMindRuntimeMode.Mock;
                options.MinimumMockLatencyMilliseconds = 0;
                options.MaximumMockLatencyMilliseconds = 0;
                options.LogGatewayOperations = false;

                var clock = new FixedMockClock();
                var ids = new DeterministicMockIdGenerator();
                var database = new NutriMindDatabase(clock, dbPath);
                Assert.That(database.Open().IsSuccess, Is.True);

                var tokenStore = new DevelopmentMockAuthTokenStore(tokenPath);
                Task write = tokenStore.WriteAsync("mock-dev-token-offline");
                yield return Await(write);

                var sessionRepo = new SqliteLocalSessionRepository(database);
                var cacheRepo = new SqliteResourceCacheRepository(database);
                var snapshot = new BootstrapSnapshot
                {
                    RequiredManifestVersion = "v5",
                    Profile = new StudentProfile
                    {
                        Id = "stu-1",
                        DisplayName = "Offline Student",
                        GradeId = "g5",
                        LrnMasked = "****9012",
                        IsActive = true,
                        Section = new StudentSection { Id = "sec-1", Name = "A", GradeId = "g5" }
                    },
                    Subjects = new[]
                    {
                        new SubjectSummary { Id = "lq", Name = "LiteraQuest", Slug = "lq", IsActive = true }
                    },
                    Missions = new[]
                    {
                        new MissionSummary
                        {
                            Id = "g5_lq_t1_m01",
                            Title = "Festival",
                            GradeId = "g5",
                            SubjectId = "lq",
                            TermId = "t1",
                            Status = "available",
                            Progress = new MissionProgressSummary { State = "not_started", RequiredAreaCount = 3 }
                        }
                    },
                    QuizPortalAvailableCount = 2,
                    AnnouncementsVisibleCount = 1,
                    Sync = new SyncStatus { Revision = 7, PendingOutboxCount = 0 }
                };

                string now = clock.UtcNow.ToString("o");
                AppResult<string> json = BootstrapCacheMapper.Serialize(snapshot, now);
                Assert.That(json.IsSuccess, Is.True);
                Assert.That(sessionRepo.UpsertSession(new LocalSessionRecord
                {
                    StudentId = snapshot.Profile.Id,
                    DisplayName = snapshot.Profile.DisplayName,
                    GradeId = snapshot.Profile.GradeId,
                    SectionId = snapshot.Profile.Section.Id,
                    SectionName = snapshot.Profile.Section.Name,
                    LastAuthenticatedUtc = now,
                    LastBootstrapRevision = 7,
                    LastBootstrapCachedUtc = now,
                    OfflineEligible = true
                }).IsSuccess, Is.True);
                Assert.That(cacheRepo.Upsert(new ResourceCacheRecord
                {
                    CacheKey = ResourceCacheKeys.Bootstrap,
                    PayloadJson = json.Value,
                    SchemaVersion = 1,
                    ServerRevision = 7,
                    CachedUtc = now
                }).IsSuccess, Is.True);

                database.Dispose();

                // Recreate composition boundary with same DB + token + offline connectivity.
                var database2 = new NutriMindDatabase(clock, dbPath);
                Assert.That(database2.Open().IsSuccess, Is.True);
                var tokenStore2 = new DevelopmentMockAuthTokenStore(tokenPath);
                var connectivity = new MockConnectivityService(startOnline: false);
                var sessionRepo2 = new SqliteLocalSessionRepository(database2);
                var cacheRepo2 = new SqliteResourceCacheRepository(database2);

                Task<string> read = tokenStore2.ReadAsync();
                yield return Await(read);
                Assert.That(read.Result, Is.EqualTo("mock-dev-token-offline"));

                AppResult<LocalSessionRecord> session = sessionRepo2.GetSession();
                Assert.That(session.Value.OfflineEligible, Is.True);
                AppResult<ResourceCacheRecord> cache = cacheRepo2.Get(ResourceCacheKeys.Bootstrap);
                AppResult<BootstrapSnapshot> restored =
                    BootstrapCacheMapper.Deserialize(cache.Value.PayloadJson, cache.Value.SchemaVersion);
                Assert.That(restored.IsSuccess, Is.True);
                Assert.That(restored.Value.Profile.Id, Is.EqualTo("stu-1"));
                Assert.That(restored.Value.QuizPortalAvailableCount, Is.EqualTo(2));
                Assert.That(restored.Value.Sync.Revision, Is.EqualTo(7));
                Assert.That(connectivity.IsOnline, Is.False);

                database2.Dispose();
            }
            finally
            {
                TryDelete(tokenPath);
                TryDelete(dbPath);
                TryDelete(dbPath + "-wal");
                TryDelete(dbPath + "-shm");
            }
        }

        [Test]
        public void BootstrapCache_RoundTrip_PreservesFields_AndRejectsMalformed()
        {
            var snapshot = new BootstrapSnapshot
            {
                RequiredManifestVersion = "v5",
                Profile = new StudentProfile
                {
                    Id = "stu-round",
                    DisplayName = "Round Trip",
                    GradeId = "g5",
                    LrnMasked = "****1111",
                    IsActive = true,
                    Section = new StudentSection { Id = "s1", Name = "B", GradeId = "g5" }
                },
                Subjects = new[]
                {
                    new SubjectSummary { Id = "sci", Name = "Science", Slug = "sci", IsActive = true }
                },
                Missions = Array.Empty<MissionSummary>(),
                QuizPortalAvailableCount = 3,
                AnnouncementsVisibleCount = 4,
                Sync = new SyncStatus { Revision = 11, PendingServerActions = true }
            };

            AppResult<string> json = BootstrapCacheMapper.Serialize(snapshot, "2026-07-27T04:00:00Z");
            Assert.That(json.IsSuccess, Is.True);
            Assert.That(json.Value.ToLowerInvariant(), Does.Not.Contain("pin"));
            Assert.That(json.Value.ToLowerInvariant(), Does.Not.Contain("access_token"));
            Assert.That(json.Value.ToLowerInvariant(), Does.Not.Contain("bearer"));

            AppResult<BootstrapSnapshot> restored =
                BootstrapCacheMapper.Deserialize(json.Value, 1);
            Assert.That(restored.IsSuccess, Is.True);
            Assert.That(restored.Value.Profile.Id, Is.EqualTo("stu-round"));
            Assert.That(restored.Value.RequiredManifestVersion, Is.EqualTo("v5"));
            Assert.That(restored.Value.QuizPortalAvailableCount, Is.EqualTo(3));
            Assert.That(restored.Value.AnnouncementsVisibleCount, Is.EqualTo(4));
            Assert.That(restored.Value.Sync.Revision, Is.EqualTo(11));
            Assert.That(restored.Value.Subjects[0].Id, Is.EqualTo("sci"));

            AppResult<BootstrapSnapshot> badVersion =
                BootstrapCacheMapper.Deserialize(json.Value, 99);
            Assert.That(badVersion.IsFailure, Is.True);
            Assert.That(badVersion.Error.Code, Is.EqualTo(AppErrorCodes.CacheSchemaUnsupported));

            AppResult<BootstrapSnapshot> malformed =
                BootstrapCacheMapper.Deserialize("{not-json", 1);
            Assert.That(malformed.IsFailure, Is.True);
            Assert.That(malformed.Error.Code, Is.EqualTo(AppErrorCodes.CachePayloadInvalid));
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
    }
}
