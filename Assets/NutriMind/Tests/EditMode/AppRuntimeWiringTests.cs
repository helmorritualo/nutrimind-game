using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NutriMind.App.Presentation;
using NutriMind.App.UI;
using NutriMind.Core.Data;
using NutriMind.Core.Networking;
using NutriMind.Core.Persistence;
using NutriMind.Core.Utilities;
using NutriMind.Tests.TestData;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace NutriMind.Tests.EditMode
{
    /// <summary>
    /// Prompt 2 EditMode coverage for runtime wiring.
    /// Tests QuizAttemptSession lifecycle, ErrorToDataState 401 mapping,
    /// AnnouncementReadRepository idempotent marking, and leaderboard offline (no-cache) behaviour.
    /// </summary>
    public sealed class AppRuntimeWiringTests
    {
        // ──────────────────────── QuizAttemptSession ────────────────────────

        [Test]
        public void QuizAttemptSession_SetAnswer_RecordsAndOverwrites()
        {
            var detail = new QuizDetail
            {
                Id = "quiz-001",
                Questions = new List<QuizQuestionDelivery>
                {
                    new QuizQuestionDelivery { Id = "q1", Type = "multiple_choice_single" },
                    new QuizQuestionDelivery { Id = "q2", Type = "multiple_choice_single" }
                }
            };

            var session = new QuizAttemptSession("quiz-001", "uuid-wiring-001", detail);
            session.SetAnswer("q1", new[] { "opt_a" });
            session.SetAnswer("q2", new[] { "opt_b" });

            IReadOnlyList<string> a1 = session.GetAnswer("q1");
            IReadOnlyList<string> a2 = session.GetAnswer("q2");

            Assert.That(a1.Count, Is.EqualTo(1));
            Assert.That(a1[0], Is.EqualTo("opt_a"));
            Assert.That(a2.Count, Is.EqualTo(1));
            Assert.That(a2[0], Is.EqualTo("opt_b"));

            // Overwrite q1
            session.SetAnswer("q1", new[] { "opt_c" });
            Assert.That(session.GetAnswer("q1")[0], Is.EqualTo("opt_c"));
        }

        [Test]
        public void QuizAttemptSession_BeginSubmit_SetsIsSubmitting()
        {
            var session = BuildMinimalSession("quiz-002", "uuid-wiring-002");
            Assert.That(session.IsSubmitting, Is.False);

            session.BeginSubmit();
            Assert.That(session.IsSubmitting, Is.True);
            Assert.That(session.HasUncertainSubmit, Is.False);
        }

        [Test]
        public void QuizAttemptSession_MarkUncertainSubmit_SetsHasUncertainSubmit()
        {
            var session = BuildMinimalSession("quiz-003", "uuid-wiring-003");
            session.BeginSubmit();
            session.MarkUncertainSubmit();

            Assert.That(session.IsSubmitting, Is.False);
            Assert.That(session.HasUncertainSubmit, Is.True);
            Assert.That(session.IsSubmitted, Is.False);
        }

        [Test]
        public void QuizAttemptSession_MarkSubmitted_LocksSession()
        {
            var session = BuildMinimalSession("quiz-004", "uuid-wiring-004");
            session.BeginSubmit();
            session.MarkSubmitted();

            Assert.That(session.IsSubmitted, Is.True);
            Assert.That(session.IsSubmitting, Is.False);
            Assert.That(session.HasUncertainSubmit, Is.False);

            // After submitted: SetAnswer and NavigateTo must be no-ops
            session.SetAnswer("q1", new[] { "opt_x" });
            Assert.That(session.GetAnswer("q1").Count, Is.EqualTo(0));
        }

        [Test]
        public void QuizAttemptSession_BuildSubmission_UsesStableClientUuid()
        {
            string expectedUuid = "uuid-stable-001";
            var detail = new QuizDetail
            {
                Id = "quiz-005",
                Questions = new List<QuizQuestionDelivery>
                {
                    new QuizQuestionDelivery { Id = "q1", Type = "multiple_choice_single" }
                }
            };

            var session = new QuizAttemptSession("quiz-005", expectedUuid, detail);
            session.SetAnswer("q1", new[] { "opt_a" });

            QuizAttemptSubmission first = session.BuildSubmission();
            QuizAttemptSubmission second = session.BuildSubmission();

            Assert.That(first.ClientAttemptUuid, Is.EqualTo(expectedUuid));
            Assert.That(second.ClientAttemptUuid, Is.EqualTo(expectedUuid));
            Assert.That(first.Answers.Count, Is.EqualTo(1));
            Assert.That(first.Answers[0].QuestionId, Is.EqualTo("q1"));
        }

        // ──────────────────────── ErrorToDataState / 401 ────────────────────

        [Test]
        public void ErrorToDataState_Http403_ReturnsPermissionOrLocked()
        {
            var error = new AppError(AppErrorCodes.AuthTokenInvalid, "Forbidden", httpStatus: 403);
            DataStatePanelState state = AppViewMappers.ErrorToDataState(error);
            Assert.That(state, Is.EqualTo(DataStatePanelState.PermissionOrLocked));
        }

        [Test]
        public void ErrorToDataState_Http401_ReturnsRecoverableError()
        {
            // 401 is handled by the presenter's IsUnauthorized check; ErrorToDataState
            // only maps 403 to PermissionOrLocked — 401 falls through to RecoverableError.
            var error = new AppError(AppErrorCodes.AuthTokenInvalid, "Unauthorized", httpStatus: 401);
            DataStatePanelState state = AppViewMappers.ErrorToDataState(error);
            Assert.That(state, Is.EqualTo(DataStatePanelState.RecoverableError));
        }

        [Test]
        public void ErrorToDataState_Offline_ReturnsOfflineUnavailableWhenNoCache()
        {
            var error = new AppError(AppErrorCodes.NetworkOffline, "Offline");
            DataStatePanelState state = AppViewMappers.ErrorToDataState(error, hasCachedData: false);
            Assert.That(state, Is.EqualTo(DataStatePanelState.OfflineUnavailable));
        }

        [Test]
        public void ErrorToDataState_Offline_ReturnsOfflineCachedWhenCacheAvailable()
        {
            var error = new AppError(AppErrorCodes.NetworkOffline, "Offline");
            DataStatePanelState state = AppViewMappers.ErrorToDataState(error, hasCachedData: true);
            Assert.That(state, Is.EqualTo(DataStatePanelState.OfflineCached));
        }

        [Test]
        public void ErrorToDataState_ServerError_ReturnsRecoverableError()
        {
            var error = new AppError(AppErrorCodes.ServiceUnavailable, "Server error", httpStatus: 503);
            DataStatePanelState state = AppViewMappers.ErrorToDataState(error);
            Assert.That(state, Is.EqualTo(DataStatePanelState.RecoverableError));
        }

        [Test]
        public void ErrorToDataState_NullError_ReturnsRecoverableError()
        {
            DataStatePanelState state = AppViewMappers.ErrorToDataState(null);
            Assert.That(state, Is.EqualTo(DataStatePanelState.RecoverableError));
        }

        // ──────────────────────── 401 via gateway ────────────────────────────

        [UnityTest]
        public IEnumerator Gateway_UnauthorizedScenario_ProtectedEndpointsReturn401()
        {
            Task<MockStudentGateway> loginTask = CreateAuthenticatedGatewayAsync();
            yield return Await(loginTask);
            MockStudentGateway gateway = loginTask.Result;
            Assert.That(gateway, Is.Not.Null);

            // Switch to unauthorized-after-login scenario.
            gateway.MockRuntime.SetScenario(MockApiScenario.UnauthorizedAfterLogin);

            Task<AppResult<LeaderboardPage>> task = gateway.GetLeaderboardAsync(
                new GetLeaderboardRequest { Scope = "section" });
            yield return Await(task);

            // UnauthorizedAfterLogin scenario returns AuthTokenInvalid with HTTP 401.
            Assert.That(task.Result.IsFailure, Is.True);
            Assert.That(
                task.Result.Error.Code,
                Is.EqualTo(AppErrorCodes.AuthTokenInvalid)
                .Or.EqualTo(AppErrorCodes.AuthTokenMissing)
                .Or.EqualTo(AppErrorCodes.AuthTokenRevoked));
            Assert.That(task.Result.Error.HttpStatus, Is.EqualTo(401));
        }

        // ──────────────────────── AnnouncementReadRepository ─────────────────

        [Test]
        public void AnnouncementReadRepository_MarkReadIdempotent_DoesNotDuplicate()
        {
            string dbPath = Path.Combine(
                Path.GetTempPath(),
                "nm-ann-test-" + Guid.NewGuid().ToString("N") + ".db");

            try
            {
                var database = new NutriMindDatabase(new FixedMockClock(), dbPath);
                Assert.That(database.Open().IsSuccess, Is.True);

                var repo = new SqliteAnnouncementReadRepository(database);
                string readUtc = DateTimeOffset.UtcNow.ToString("o");

                // First mark
                AppResult first = repo.MarkRead("ann-001", readUtc);
                Assert.That(first.IsSuccess, Is.True);

                // Idempotent second mark — should not fail
                AppResult second = repo.MarkRead("ann-001", readUtc);
                Assert.That(second.IsSuccess, Is.True);

                // IsRead
                AppResult<bool> isRead = repo.IsRead("ann-001");
                Assert.That(isRead.IsSuccess, Is.True);
                Assert.That(isRead.Value, Is.True);

                // Different key not read
                AppResult<bool> isOtherRead = repo.IsRead("ann-002");
                Assert.That(isOtherRead.IsSuccess, Is.True);
                Assert.That(isOtherRead.Value, Is.False);

                // GetAll returns exactly one
                AppResult<IReadOnlyList<AnnouncementReadStateRecord>> all = repo.GetAll();
                Assert.That(all.IsSuccess, Is.True);
                Assert.That(all.Value.Count, Is.EqualTo(1));
                Assert.That(all.Value[0].AnnouncementKey, Is.EqualTo("ann-001"));

                database.Dispose();
            }
            finally
            {
                TryDelete(dbPath);
                TryDelete(dbPath + "-wal");
                TryDelete(dbPath + "-shm");
            }
        }

        [Test]
        public void AnnouncementReadRepository_MarkMultiple_BatchTracksAll()
        {
            string dbPath = Path.Combine(
                Path.GetTempPath(),
                "nm-ann-batch-" + Guid.NewGuid().ToString("N") + ".db");

            try
            {
                var database = new NutriMindDatabase(new FixedMockClock(), dbPath);
                Assert.That(database.Open().IsSuccess, Is.True);

                var repo = new SqliteAnnouncementReadRepository(database);
                string readUtc = DateTimeOffset.UtcNow.ToString("o");

                repo.MarkRead("ann-a", readUtc);
                repo.MarkRead("ann-b", readUtc);
                repo.MarkRead("ann-c", readUtc);

                AppResult<IReadOnlyList<AnnouncementReadStateRecord>> all = repo.GetAll();
                Assert.That(all.IsSuccess, Is.True);
                Assert.That(all.Value.Count, Is.EqualTo(3));

                database.Dispose();
            }
            finally
            {
                TryDelete(dbPath);
                TryDelete(dbPath + "-wal");
                TryDelete(dbPath + "-shm");
            }
        }

        // ──────────────────────── Leaderboard no-cache ───────────────────────

        [UnityTest]
        public IEnumerator Leaderboard_WhileOffline_ReturnsNetworkError_NoFallback()
        {
            var connectivity = new MockConnectivityService(startOnline: true);
            MockStudentGateway gateway = MockGatewayTestFactory.CreateGateway(
                MockApiScenario.HappyPath,
                connectivity: connectivity);

            Task<AppResult<LoginResult>> loginTask = gateway.LoginAsync(new LoginRequest
            {
                Lrn = MockGatewayTestFactory.ValidLrn,
                Pin = MockGatewayTestFactory.ValidPin,
                DeviceName = "EditModeLeaderboard"
            });
            yield return Await(loginTask);
            Assert.That(loginTask.Result.IsSuccess, Is.True);

            // Go offline. Leaderboard is online-only — no cached fallback exists.
            connectivity.SetOnline(false);

            Task<AppResult<LeaderboardPage>> task = gateway.GetLeaderboardAsync(
                new GetLeaderboardRequest { Scope = "section" });
            yield return Await(task);

            Assert.That(task.Result.IsFailure, Is.True);
            Assert.That(
                task.Result.Error.Code,
                Is.EqualTo(AppErrorCodes.NetworkOffline)
                .Or.EqualTo(AppErrorCodes.NetworkTimeout));
        }

        // ──────────────────────── AppViewMappers unit coverage ───────────────

        [Test]
        public void MapQuizSummaryToPreviewItem_NullInput_ReturnsDefault()
        {
            QuizListPreviewItem result = AppViewMappers.MapQuizSummaryToPreviewItem(null);
            Assert.That(result.Id, Is.Null.Or.Empty);
        }

        [Test]
        public void MapQuizSummaryToPreviewItem_ValidInput_MapsSubjectAndTerm()
        {
            var quiz = new QuizSummary
            {
                Id = "q-001",
                Title = "Test Quiz",
                SubjectId = "peh",
                TermId = "t2",
                Status = "available",
                MaxAttempts = 3,
                AttemptsUsed = 1
            };

            QuizListPreviewItem item = AppViewMappers.MapQuizSummaryToPreviewItem(quiz);
            Assert.That(item.Id, Is.EqualTo("q-001"));
            Assert.That(item.Title, Is.EqualTo("Test Quiz"));
            Assert.That(item.SubjectId, Is.EqualTo("peh"));
            Assert.That(item.TermId, Is.EqualTo("t2"));
            Assert.That(item.Subject, Is.EqualTo(NutriMindSubject.PeAndHealth));
            Assert.That(item.Term, Is.EqualTo(NutriMindTerm.Term2));
            Assert.That(item.Status, Is.EqualTo(QuizListPreviewStatus.Available));
            Assert.That(item.MaxAttempts, Is.EqualTo(3));
            Assert.That(item.AttemptsUsed, Is.EqualTo(1));
        }

        [Test]
        public void MapQuizStatus_KnownValues_ReturnCorrectEnum()
        {
            Assert.That(AppViewMappers.MapQuizStatus("available"), Is.EqualTo(QuizListPreviewStatus.Available));
            Assert.That(AppViewMappers.MapQuizStatus("completed"), Is.EqualTo(QuizListPreviewStatus.Completed));
            Assert.That(AppViewMappers.MapQuizStatus("locked"), Is.EqualTo(QuizListPreviewStatus.Locked));
            Assert.That(AppViewMappers.MapQuizStatus("unknown"), Is.EqualTo(QuizListPreviewStatus.Unavailable));
            Assert.That(AppViewMappers.MapQuizStatus(null), Is.EqualTo(QuizListPreviewStatus.Unavailable));
        }

        [Test]
        public void MapQuizDetail_NullInput_ReturnsNull()
        {
            Assert.That(AppViewMappers.MapQuizDetail(null), Is.Null);
        }

        [Test]
        public void MapQuizDetail_ValidInput_PreservesQuestionCount()
        {
            var detail = new QuizDetail
            {
                Id = "quiz-001",
                Title = "A Quiz",
                Questions = new List<QuizQuestionDelivery>
                {
                    new QuizQuestionDelivery
                    {
                        Id = "q1",
                        Type = "multiple_choice_single",
                        Prompt = "What is 1+1?",
                        Options = new List<QuizOptionDelivery>
                        {
                            new QuizOptionDelivery { Key = "opt_a", Text = "1" },
                            new QuizOptionDelivery { Key = "opt_b", Text = "2" }
                        }
                    }
                }
            };

            NutriMind.App.UI.QuizDetailPreviewContent mapped = AppViewMappers.MapQuizDetail(detail);

            Assert.That(mapped, Is.Not.Null);
            Assert.That(mapped.QuizId, Is.EqualTo("quiz-001"));
            Assert.That(mapped.QuestionCount, Is.EqualTo(1));
            Assert.That(mapped.Questions[0].Id, Is.EqualTo("q1"));
            Assert.That(mapped.Questions[0].Options.Count, Is.EqualTo(2));
        }

        [Test]
        public void MapQuizResult_NullInput_ReturnsNull()
        {
            Assert.That(AppViewMappers.MapQuizResult(null), Is.Null);
        }

        [Test]
        public void MapQuizResult_ValidInput_PreservesScoreFields()
        {
            var result = new QuizResult
            {
                AttemptId = "att-001",
                QuizId = "quiz-001",
                EarnedPoints = 8f,
                PossiblePoints = 10f,
                Percentage = 80f,
                Passed = true,
                CorrectCount = 4,
                IncorrectCount = 1,
                UnansweredCount = 0,
                FeedbackVisible = true,
                Answers = new List<QuizResultAnswer>
                {
                    new QuizResultAnswer { QuestionId = "q1", Correct = true, EarnedPoints = 2f }
                }
            };

            NutriMind.App.UI.QuizResultPreviewContent mapped = AppViewMappers.MapQuizResult(result);

            Assert.That(mapped, Is.Not.Null);
            Assert.That(mapped.AttemptId, Is.EqualTo("att-001"));
            Assert.That(mapped.EarnedPoints, Is.EqualTo(8f));
            Assert.That(mapped.Percentage, Is.EqualTo(80f));
            Assert.That(mapped.Passed, Is.True);
            Assert.That(mapped.CorrectCount, Is.EqualTo(4));
            Assert.That(mapped.FeedbackVisible, Is.True);
            Assert.That(mapped.Answers.Count, Is.EqualTo(1));
        }

        [Test]
        public void TryMapSubject_UnknownIdentifier_DoesNotCollapseIntoKnownSubject()
        {
            bool mapped = AppViewMappers.TryMapSubject(
                "subject_unknown_curriculum",
                out NutriMindSubject subject);

            Assert.That(mapped, Is.False);
            Assert.That(subject, Is.EqualTo(default(NutriMindSubject)));
        }

        [Test]
        public void MapMissionSummaryToPreviewItem_PreservesRuntimeIdentityAndProgress()
        {
            var summary = new MissionSummary
            {
                Id = "g5_lq_t1_m07",
                SubjectId = "subject_literaquest",
                TermId = "term_1",
                Title = "Runtime Mission",
                Order = 7,
                Status = "in_progress",
                Progress = new MissionProgressSummary
                {
                    State = "in_progress",
                    CompletedAreaCount = 2,
                    RequiredAreaCount = 3,
                    CollectibleCount = 1,
                    RequiredCollectibleCount = 3
                }
            };

            MissionPreviewItem item = AppViewMappers.MapMissionSummaryToPreviewItem(
                summary,
                NutriMindSubject.Science,
                NutriMindTerm.Term3);

            Assert.That(item.MissionId, Is.EqualTo("g5_lq_t1_m07"));
            Assert.That(item.MissionNumber, Is.EqualTo(7));
            Assert.That(item.Subject, Is.EqualTo(NutriMindSubject.LiteraQuest));
            Assert.That(item.Term, Is.EqualTo(NutriMindTerm.Term1));
            Assert.That(item.AreasCompleted, Is.EqualTo(2));
            Assert.That(item.AreasRequired, Is.EqualTo(3));
            Assert.That(item.CollectiblesCompleted, Is.EqualTo(1));
            Assert.That(item.PrimaryAction, Is.EqualTo(MissionPreviewPrimaryAction.Continue));
        }

        [Test]
        public void MissionSelectionSetItems_ReplacesAndClearsPreviewCards()
        {
            VisualTreeAsset asset = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/NutriMind/App/UI/UXML/MissionSelectionPanel.uxml");
            Assert.That(asset, Is.Not.Null);

            TemplateContainer root = asset.CloneTree();
            var view = new MissionSelectionPanelView(root);
            view.SetItems(new[]
            {
                new MissionPreviewItem(
                    "runtime_mission",
                    "Runtime Mission",
                    9,
                    NutriMindSubject.Science,
                    NutriMindTerm.Term2,
                    "available",
                    false,
                    string.Empty,
                    0,
                    3,
                    0,
                    3,
                    MissionPreviewPrimaryAction.Start)
            });

            Assert.That(view.LoadedMissionCount, Is.EqualTo(1));
            Assert.That(
                root.Q<VisualElement>("mission-list")
                    .Query<Button>(className: "mission-selection__item").ToList().Count,
                Is.EqualTo(1));
            Assert.That(view.SelectedMissionId, Is.EqualTo("runtime_mission"));

            view.SetItems(Array.Empty<MissionPreviewItem>());

            Assert.That(view.LoadedMissionCount, Is.Zero);
            Assert.That(
                root.Q<VisualElement>("mission-list")
                    .Query<Button>(className: "mission-selection__item").ToList().Count,
                Is.Zero);
            Assert.That(view.SelectedMissionId, Is.Empty);
            view.Dispose();
        }

        [Test]
        public void ProgressSummaryMapperAndView_BindOnlySupportedAggregateFields()
        {
            var summary = new ProgressSummary
            {
                MissionsStarted = 8,
                MissionsCompleted = 5,
                AreasCompleted = 12,
                ReviewRequiredCount = 2,
                QuizAttempts = 4
            };
            ProgressPreviewSummary preview = AppViewMappers.MapProgressSummary(summary, 3);

            Assert.That(preview.MissionsStarted, Is.EqualTo(8));
            Assert.That(preview.MissionsCompleted, Is.EqualTo(5));
            Assert.That(preview.AreasCompleted, Is.EqualTo(12));
            Assert.That(preview.ReviewRequiredCount, Is.EqualTo(2));
            Assert.That(preview.QuizAttempts, Is.EqualTo(4));
            Assert.That(preview.PendingOutboxCount, Is.EqualTo(3));

            VisualTreeAsset asset = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/NutriMind/App/UI/UXML/ProgressPanel.uxml");
            TemplateContainer root = asset.CloneTree();
            var view = new ProgressPanelView(root);
            view.SetSummary(preview);

            Assert.That(root.Q<Label>("overall-missions").text, Does.Contain("5"));
            Assert.That(root.Q<Label>("overall-missions").text, Does.Contain("8"));
            Assert.That(root.Q<Label>("overall-reviews").text, Does.Contain("2"));
            Assert.That(root.Q<Label>("overall-subjects").text, Does.Contain("12"));
            view.Dispose();
        }

        [Test]
        public void SubjectAndTermViews_RepresentSuccessfulEmptyData()
        {
            VisualTreeAsset subjectsAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/NutriMind/App/UI/UXML/SubjectSelectionPanel.uxml");
            var subjectsView = new SubjectSelectionPanelView(subjectsAsset.CloneTree());
            subjectsView.Bind(Array.Empty<NutriMindSubject>());
            subjectsView.SetDataState(DataStatePanelState.Empty);
            Assert.That(subjectsView.DataState, Is.EqualTo(DataStatePanelState.Empty));

            VisualTreeAsset termsAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/NutriMind/App/UI/UXML/TermSelectionPanel.uxml");
            TemplateContainer termRoot = termsAsset.CloneTree();
            var termsView = new TermSelectionPanelView(termRoot);
            termsView.SetTerms(Array.Empty<NutriMindTerm>());
            termsView.SetDataState(DataStatePanelState.Empty);
            Assert.That(termsView.DataState, Is.EqualTo(DataStatePanelState.Empty));
            Assert.That(termRoot.Q<Button>("card-term-1").resolvedStyle.display, Is.EqualTo(DisplayStyle.None));

            subjectsView.Dispose();
            termsView.Dispose();
        }

        [Test]
        public void EffectiveAnnouncementUnread_RequiresServerUnreadAndNoLocalOverride()
        {
            var serverRead = new AnnouncementSummary { Id = "server-read", IsUnread = false };
            var serverUnread = new AnnouncementSummary { Id = "server-unread", IsUnread = true };

            Assert.That(
                AppViewMappers.IsAnnouncementEffectivelyUnread(serverRead, locallyMarkedRead: false),
                Is.False);
            Assert.That(
                AppViewMappers.IsAnnouncementEffectivelyUnread(serverUnread, locallyMarkedRead: true),
                Is.False);
            Assert.That(
                AppViewMappers.IsAnnouncementEffectivelyUnread(serverUnread, locallyMarkedRead: false),
                Is.True);
        }

        // ──────────────────────── Helpers ────────────────────────────────────

        private static QuizAttemptSession BuildMinimalSession(string quizId, string uuid)
        {
            var detail = new QuizDetail
            {
                Id = quizId,
                Questions = new List<QuizQuestionDelivery>
                {
                    new QuizQuestionDelivery { Id = "q1", Type = "multiple_choice_single" }
                }
            };
            return new QuizAttemptSession(quizId, uuid, detail);
        }

        private static async Task<MockStudentGateway> CreateAuthenticatedGatewayAsync()
        {
            var tokenStore = new InMemoryMockAuthTokenStore();
            MockStudentGateway gateway = MockGatewayTestFactory.CreateGateway(
                MockApiScenario.HappyPath,
                tokenStore: tokenStore);

            AppResult<LoginResult> login = await gateway.LoginAsync(new LoginRequest
            {
                Lrn = MockGatewayTestFactory.ValidLrn,
                Pin = MockGatewayTestFactory.ValidPin,
                DeviceName = "EditModeWiring"
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

        // ──────────────────────── Modal stretch / picking ────────────────────

        [Test]
        public void AppModalHost_ShowConfirm_StretchesWrapperAndBlocksPicking()
        {
            VisualTreeAsset confirmAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/NutriMind/App/UI/UXML/Shared/ConfirmDialog.uxml");
            Assert.That(confirmAsset, Is.Not.Null, "ConfirmDialog.uxml must exist.");

            var modalLayer = new VisualElement();
            modalLayer.pickingMode = PickingMode.Ignore;

            var host = new AppModalHost(modalLayer, confirmAsset, null);
            Assert.That(modalLayer.pickingMode, Is.EqualTo(PickingMode.Ignore));

            host.ShowConfirm(ConfirmDialogPresets.SignOut(), onConfirm: () => { }, onCancel: null);

            Assert.That(host.IsModalVisible, Is.True);
            Assert.That(modalLayer.pickingMode, Is.EqualTo(PickingMode.Position));

            TemplateContainer wrapper = null;
            for (int i = 0; i < modalLayer.childCount; i++)
            {
                if (modalLayer[i] is TemplateContainer container)
                {
                    wrapper = container;
                    break;
                }
            }

            Assert.That(wrapper, Is.Not.Null);
            Assert.That(wrapper.style.position.value, Is.EqualTo(Position.Absolute));
            Assert.That(wrapper.style.left.value.value, Is.EqualTo(0f));
            Assert.That(wrapper.style.top.value.value, Is.EqualTo(0f));
            Assert.That(wrapper.style.right.value.value, Is.EqualTo(0f));
            Assert.That(wrapper.style.bottom.value.value, Is.EqualTo(0f));

            host.Hide();
            Assert.That(host.IsModalVisible, Is.False);
            Assert.That(modalLayer.pickingMode, Is.EqualTo(PickingMode.Ignore));
            host.Dispose();
        }

        [Test]
        public void MoreBottomNav_DoesNotMapDirectlyToLeaderboard()
        {
            // Regression guard: More must open a hub, not NavigateAsync(Leaderboard).
            string runtimePath = Path.Combine(
                "Assets", "NutriMind", "App", "Scripts", "Presentation", "AppShellRuntimeController.cs");
            string source = File.ReadAllText(runtimePath);
            Assert.That(source, Does.Contain("ShowMoreHub"));
            Assert.That(source, Does.Contain("AppRouteId.Profile"));
            Assert.That(source, Does.Contain("AppRouteId.Settings"));
            Assert.That(source, Does.Contain("AppRouteId.Certificates"));
            Assert.That(source, Does.Contain("AppRouteId.Announcements"));
            Assert.That(source, Does.Contain("AppRouteId.Leaderboard"));
            Assert.That(source, Does.Not.Contain("case AppShellPreviewRoute.More:\r\n                    return AppRouteId.Leaderboard"));
            Assert.That(source, Does.Not.Contain("case AppShellPreviewRoute.More:\n                    return AppRouteId.Leaderboard"));
        }

        [Test]
        public void Coordinators_DeclareMountPanelHelper()
        {
            string main = File.ReadAllText(Path.Combine(
                "Assets", "NutriMind", "App", "Scripts", "Composition", "MainScreenCoordinator.cs"));
            string quiz = File.ReadAllText(Path.Combine(
                "Assets", "NutriMind", "App", "Scripts", "Composition", "QuizPortalScreenCoordinator.cs"));

            Assert.That(main, Does.Contain("app-shell__content-instance"));
            Assert.That(main, Does.Contain("private static TemplateContainer MountPanel"));
            Assert.That(quiz, Does.Contain("app-shell__content-instance"));
            Assert.That(quiz, Does.Contain("private static TemplateContainer MountPanel"));
            Assert.That(
                File.ReadAllText(Path.Combine(
                    "Assets", "NutriMind", "App", "Scripts", "Presenters", "ProfilePresenter.cs")),
                Does.Contain("SettingsRequested"));
            Assert.That(
                File.ReadAllText(Path.Combine(
                    "Assets", "NutriMind", "App", "Scripts", "Presenters", "RewardsPresenter.cs")),
                Does.Contain("ViewCertificatesRequested"));
        }

        [Test]
        public void PartDPresenters_DeclarePersistentIdempotencyAndNavigationContracts()
        {
            string presenters = Path.Combine(
                "Assets", "NutriMind", "App", "Scripts", "Presenters");
            string composition = Path.Combine(
                "Assets", "NutriMind", "App", "Scripts", "Composition");

            string rewards = File.ReadAllText(Path.Combine(presenters, "RewardsPresenter.cs"));
            Assert.That(rewards, Does.Contain("FindLatestUnresolved"));
            Assert.That(rewards, Does.Contain("IdempotentOperations.UseReward"));
            Assert.That(rewards, Does.Contain("IdempotentRequestStates.Sending"));
            Assert.That(rewards, Does.Contain("IdempotentRequestStates.Uncertain"));
            Assert.That(rewards, Does.Contain("IdempotentRequestStates.Completed"));
            Assert.That(rewards, Does.Contain("IdempotentRequestStates.Rejected"));
            Assert.That(rewards, Does.Contain("AppRouteOrigin.Rewards"));

            string attempt = File.ReadAllText(Path.Combine(presenters, "QuizAttemptPresenter.cs"));
            Assert.That(attempt, Does.Contain("SerializeQuiz"));
            Assert.That(attempt, Does.Contain("DeserializeQuiz"));
            Assert.That(attempt, Does.Contain("RetainPendingQuizSubmission"));
            Assert.That(attempt, Does.Contain("ReleasePendingQuizSubmission"));
            Assert.That(attempt, Does.Contain("IdempotentRequestStates.Sending"));
            Assert.That(attempt, Does.Contain("IdempotentRequestStates.Uncertain"));
            Assert.That(attempt, Does.Not.Contain("new QuizDetail { Id = _ctx.QuizId }"));

            string coordinator = File.ReadAllText(
                Path.Combine(composition, "QuizPortalScreenCoordinator.cs"));
            Assert.That(coordinator, Does.Contain("QuizRouteKey? _mountedRouteKey"));
            Assert.That(coordinator, Does.Contain("QuizRouteKey.FromEntry(entry)"));
            Assert.That(coordinator, Does.Contain("ClearActiveNavigation"));

            string result = File.ReadAllText(Path.Combine(presenters, "QuizResultPresenter.cs"));
            Assert.That(result, Does.Contain("ResetQuizPortalToRootAsync"));

            string main = File.ReadAllText(Path.Combine(composition, "MainScreenCoordinator.cs"));
            Assert.That(main, Does.Contain("entry.Context.Origin"));
            Assert.That(main, Does.Contain("AppRouteOrigin.More"));
        }
    }
}
