using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NutriMind.Core.Data;
using NutriMind.Core.Utilities;

namespace NutriMind.Core.Networking
{
    /// <summary>
    /// Deterministic in-process mock Student API. No HTTP server; no fixture file rewrites.
    /// </summary>
    public sealed class MockStudentGateway : IStudentGateway
    {
        private readonly NutriMindRuntimeOptions _options;
        private readonly IConnectivityService _connectivity;
        private readonly IAuthTokenStore _tokenStore;
        private readonly IMockFixtureSource _fixtures;
        private readonly IAppClock _clock;
        private readonly MockServerState _state;
        private readonly object _seedGate = new object();
        private bool _seedAttempted;
        private AppError _seedError;

        public MockStudentGateway(
            NutriMindRuntimeOptions options,
            IConnectivityService connectivity,
            IAuthTokenStore tokenStore,
            IMockFixtureSource fixtures = null,
            IAppClock clock = null,
            IIdGenerator ids = null,
            MockServerState state = null)
        {
            _options = (options ?? NutriMindRuntimeOptions.CreateDefaults()).Clone();
            _options.Clamp();
            _connectivity = connectivity ?? new MockConnectivityService(!_options.StartOffline);
            _tokenStore = tokenStore ?? new InMemoryMockAuthTokenStore();
            // Preload fixtures on the constructing thread (Awake/Compose = main thread) so
            // later mock latency delays can complete off-thread without Resources.Load.
            _fixtures = fixtures ?? new ResourcesMockFixtureSource(preloadAll: true);
            _clock = clock ?? new FixedMockClock();
            _state = state ?? new MockServerState();

            if (_fixtures is ResourcesMockFixtureSource resourceFixtures
                && resourceFixtures.PreloadError != null)
            {
                NutriMindLog.MockGatewayWarning(
                    "Mock fixture preload reported: " + resourceFixtures.PreloadError.Code);
            }

            if (_options.StartOffline)
            {
                _connectivity.SetState(ConnectivityState.Offline);
            }
        }

        public MockServerState ServerState => _state;

        public NutriMindRuntimeOptions Options => _options;

        public async Task<AppResult<PingStatus>> PingAsync(CancellationToken cancellationToken = default)
        {
            return await ExecuteAsync(
                MockOperationNames.Ping,
                requireAuth: false,
                cancellationToken,
                () => Task.FromResult(AppResult<PingStatus>.Success(new PingStatus
                {
                    Status = "ok",
                    Service = "nutrimind-mock"
                }))).ConfigureAwait(false);
        }

        public async Task<AppResult<ClientConfiguration>> GetConfigAsync(
            CancellationToken cancellationToken = default)
        {
            return await ExecuteAsync(
                MockOperationNames.Config,
                requireAuth: false,
                cancellationToken,
                () =>
                {
                    AppResult<MockConfigFixture> loaded =
                        _fixtures.LoadJson<MockConfigFixture>(MockFixtureNames.Config);
                    if (loaded.IsFailure)
                    {
                        return Task.FromResult(AppResult<ClientConfiguration>.Failure(loaded.Error));
                    }

                    return Task.FromResult(AppResult<ClientConfiguration>.Success(
                        MockFixtureMapper.ToConfig(loaded.Value)));
                }).ConfigureAwait(false);
        }

        public async Task<AppResult<LoginResult>> LoginAsync(
            LoginRequest request,
            CancellationToken cancellationToken = default)
        {
            return await ExecuteAsync(
                MockOperationNames.AuthLogin,
                requireAuth: false,
                cancellationToken,
                async () =>
                {
                    if (_options.MockScenario == MockApiScenario.RateLimitedLogin)
                    {
                        return AppResult<LoginResult>.Failure(AppError.Api(
                            AppErrorCodes.RateLimited,
                            "Too many login attempts. Try again shortly.",
                            429,
                            isRetryable: true,
                            retryAfterSeconds: 10));
                    }

                    string lrn = request?.Lrn?.Trim() ?? string.Empty;
                    string pin = request?.Pin?.Trim() ?? string.Empty;
                    if (!string.Equals(lrn, MockServerState.ValidMockLrn, StringComparison.Ordinal)
                        || !string.Equals(pin, MockServerState.ValidMockPin, StringComparison.Ordinal))
                    {
                        return AppResult<LoginResult>.Failure(AppError.Api(
                            AppErrorCodes.AuthInvalidCredentials,
                            "Invalid LRN or PIN.",
                            401));
                    }

                    AppError seedError = EnsureSeeded();
                    if (seedError != null)
                    {
                        return AppResult<LoginResult>.Failure(seedError);
                    }

                    AppResult<MockLoginSuccessFixture> loaded =
                        _fixtures.LoadJson<MockLoginSuccessFixture>(MockFixtureNames.LoginSuccess);
                    if (loaded.IsFailure)
                    {
                        return AppResult<LoginResult>.Failure(loaded.Error);
                    }

                    LoginResult result = MockFixtureMapper.ToLoginResult(loaded.Value);
                    if (result == null || string.IsNullOrWhiteSpace(result.AccessToken))
                    {
                        return AppResult<LoginResult>.Failure(
                            AppErrorCodes.FixtureLoadFailed,
                            "Login fixture missing access token at Resources path '"
                            + MockFixtureNames.ToResourcePath(MockFixtureNames.LoginSuccess) + "'.");
                    }

                    await _tokenStore.WriteAsync(result.AccessToken, cancellationToken).ConfigureAwait(false);
                    _state.SetIssuedToken(result.AccessToken);
                    if (_options.LogGatewayOperations)
                    {
                        NutriMindLog.MockGateway(
                            "Login success for LRN " + NutriMindLog.MaskLrn(lrn) + ".");
                    }

                    return AppResult<LoginResult>.Success(result);
                }).ConfigureAwait(false);
        }

        public async Task<AppResult> LogoutAsync(CancellationToken cancellationToken = default)
        {
            AppResult<bool> typed = await ExecuteAsync(
                MockOperationNames.AuthLogout,
                requireAuth: true,
                cancellationToken,
                async () =>
                {
                    await _tokenStore.ClearAsync(cancellationToken).ConfigureAwait(false);
                    _state.ClearIssuedToken();
                    return AppResult<bool>.Success(true);
                }).ConfigureAwait(false);

            return typed.IsSuccess ? AppResult.Success() : AppResult.Failure(typed.Error);
        }

        public async Task<AppResult<BootstrapSnapshot>> GetBootstrapAsync(
            CancellationToken cancellationToken = default)
        {
            return await ExecuteAsync(
                MockOperationNames.BootstrapGet,
                requireAuth: true,
                cancellationToken,
                () =>
                {
                    AppError seedError = EnsureSeeded();
                    if (seedError != null)
                    {
                        return Task.FromResult(AppResult<BootstrapSnapshot>.Failure(seedError));
                    }

                    if (_options.MockScenario == MockApiScenario.EmptyData)
                    {
                        return Task.FromResult(AppResult<BootstrapSnapshot>.Success(new BootstrapSnapshot
                        {
                            Profile = _state.Profile,
                            RequiredManifestVersion = "4.0",
                            Subjects = Array.Empty<SubjectSummary>(),
                            Missions = Array.Empty<MissionSummary>(),
                            QuizPortalAvailableCount = 0,
                            AnnouncementsVisibleCount = 0,
                            Sync = _state.SyncStatus
                        }));
                    }

                    AppResult<MockBootstrapFixture> loaded =
                        _fixtures.LoadJson<MockBootstrapFixture>(MockFixtureNames.Bootstrap);
                    if (loaded.IsFailure)
                    {
                        return Task.FromResult(AppResult<BootstrapSnapshot>.Failure(loaded.Error));
                    }

                    BootstrapSnapshot snapshot = MockFixtureMapper.ToBootstrap(loaded.Value);
                    snapshot.Profile = _state.Profile ?? snapshot.Profile;
                    snapshot.Sync = _state.SyncStatus ?? snapshot.Sync;
                    if (_options.MockScenario == MockApiScenario.LockedMission
                        && snapshot.Missions != null
                        && snapshot.Missions.Count > 0)
                    {
                        MissionDetail locked = _state.MissionDetail;
                        if (locked?.Mission != null)
                        {
                            snapshot.Missions = new[] { locked.Mission };
                        }
                    }

                    return Task.FromResult(AppResult<BootstrapSnapshot>.Success(snapshot));
                }).ConfigureAwait(false);
        }

        public async Task<AppResult<StudentProfile>> GetProfileAsync(
            CancellationToken cancellationToken = default)
        {
            return await ExecuteAsync(
                MockOperationNames.ProfileGet,
                requireAuth: true,
                cancellationToken,
                () =>
                {
                    AppError seedError = EnsureSeeded();
                    if (seedError != null)
                    {
                        return Task.FromResult(AppResult<StudentProfile>.Failure(seedError));
                    }

                    return Task.FromResult(AppResult<StudentProfile>.Success(_state.Profile));
                }).ConfigureAwait(false);
        }

        public async Task<AppResult<StudentSettings>> GetSettingsAsync(
            CancellationToken cancellationToken = default)
        {
            return await ExecuteAsync(
                MockOperationNames.SettingsGet,
                requireAuth: true,
                cancellationToken,
                () =>
                {
                    AppError seedError = EnsureSeeded();
                    if (seedError != null)
                    {
                        return Task.FromResult(AppResult<StudentSettings>.Failure(seedError));
                    }

                    return Task.FromResult(AppResult<StudentSettings>.Success(_state.Settings));
                }).ConfigureAwait(false);
        }

        public async Task<AppResult<StudentSettings>> PatchSettingsAsync(
            PatchSettingsRequest request,
            CancellationToken cancellationToken = default)
        {
            return await ExecuteAsync(
                MockOperationNames.SettingsPatch,
                requireAuth: true,
                cancellationToken,
                () =>
                {
                    AppError seedError = EnsureSeeded();
                    if (seedError != null)
                    {
                        return Task.FromResult(AppResult<StudentSettings>.Failure(seedError));
                    }

                    return Task.FromResult(AppResult<StudentSettings>.Success(
                        _state.ApplySettingsPatch(request)));
                }).ConfigureAwait(false);
        }

        public async Task<AppResult<IReadOnlyList<SubjectSummary>>> GetSubjectsAsync(
            CancellationToken cancellationToken = default)
        {
            return await ExecuteAsync(
                MockOperationNames.SubjectsList,
                requireAuth: true,
                cancellationToken,
                () =>
                {
                    if (_options.MockScenario == MockApiScenario.EmptyData)
                    {
                        return Task.FromResult(AppResult<IReadOnlyList<SubjectSummary>>.Success(
                            Array.Empty<SubjectSummary>()));
                    }

                    AppResult<MockSubjectListFixture> loaded =
                        _fixtures.LoadJson<MockSubjectListFixture>(MockFixtureNames.Subjects);
                    if (loaded.IsFailure)
                    {
                        return Task.FromResult(AppResult<IReadOnlyList<SubjectSummary>>.Failure(loaded.Error));
                    }

                    return Task.FromResult(AppResult<IReadOnlyList<SubjectSummary>>.Success(
                        MockFixtureMapper.ToSubjects(loaded.Value.items)));
                }).ConfigureAwait(false);
        }

        public async Task<AppResult<IReadOnlyList<TermSummary>>> GetTermsAsync(
            GetTermsRequest request,
            CancellationToken cancellationToken = default)
        {
            return await ExecuteAsync(
                MockOperationNames.TermsList,
                requireAuth: true,
                cancellationToken,
                () =>
                {
                    if (_options.MockScenario == MockApiScenario.EmptyData)
                    {
                        return Task.FromResult(AppResult<IReadOnlyList<TermSummary>>.Success(
                            Array.Empty<TermSummary>()));
                    }

                    AppResult<MockTermListFixture> loaded =
                        _fixtures.LoadJson<MockTermListFixture>(MockFixtureNames.Terms);
                    if (loaded.IsFailure)
                    {
                        return Task.FromResult(AppResult<IReadOnlyList<TermSummary>>.Failure(loaded.Error));
                    }

                    return Task.FromResult(AppResult<IReadOnlyList<TermSummary>>.Success(
                        MockFixtureMapper.ToTerms(loaded.Value.items)));
                }).ConfigureAwait(false);
        }

        public async Task<AppResult<IReadOnlyList<MissionSummary>>> GetMissionsAsync(
            GetMissionsRequest request,
            CancellationToken cancellationToken = default)
        {
            return await ExecuteAsync(
                MockOperationNames.MissionsList,
                requireAuth: true,
                cancellationToken,
                () =>
                {
                    AppError seedError = EnsureSeeded();
                    if (seedError != null)
                    {
                        return Task.FromResult(AppResult<IReadOnlyList<MissionSummary>>.Failure(seedError));
                    }

                    if (_options.MockScenario == MockApiScenario.EmptyData)
                    {
                        return Task.FromResult(AppResult<IReadOnlyList<MissionSummary>>.Success(
                            Array.Empty<MissionSummary>()));
                    }

                    MissionDetail detail = _state.MissionDetail;
                    if (detail?.Mission != null)
                    {
                        return Task.FromResult(AppResult<IReadOnlyList<MissionSummary>>.Success(
                            new[] { detail.Mission }));
                    }

                    AppResult<MockMissionListFixture> loaded =
                        _fixtures.LoadJson<MockMissionListFixture>(MockFixtureNames.Missions);
                    if (loaded.IsFailure)
                    {
                        return Task.FromResult(AppResult<IReadOnlyList<MissionSummary>>.Failure(loaded.Error));
                    }

                    return Task.FromResult(AppResult<IReadOnlyList<MissionSummary>>.Success(
                        MockFixtureMapper.ToMissionSummaries(loaded.Value.items)));
                }).ConfigureAwait(false);
        }

        public async Task<AppResult<MissionDetail>> GetMissionDetailAsync(
            MissionIdRequest request,
            CancellationToken cancellationToken = default)
        {
            return await ExecuteAsync(
                MockOperationNames.MissionDetail,
                requireAuth: true,
                cancellationToken,
                () => Task.FromResult(ResolveMissionDetail(request))).ConfigureAwait(false);
        }

        public async Task<AppResult<MissionDetail>> GetMissionProgressAsync(
            MissionIdRequest request,
            CancellationToken cancellationToken = default)
        {
            return await ExecuteAsync(
                MockOperationNames.MissionProgress,
                requireAuth: true,
                cancellationToken,
                () => Task.FromResult(ResolveMissionDetail(request))).ConfigureAwait(false);
        }

        public async Task<AppResult<ProgressMutationResult>> StartMissionAsync(
            StartMissionRequest request,
            CancellationToken cancellationToken = default)
        {
            return await ExecuteProgressMutationAsync(
                MockOperationNames.MissionStart,
                request?.EventUuid,
                MockServerState.NormalizeEventPayload(
                    request?.EventUuid,
                    request?.MissionId,
                    null,
                    request?.LocalSequence ?? 0),
                request?.MissionId,
                cancellationToken).ConfigureAwait(false);
        }

        public async Task<AppResult<ProgressMutationResult>> StartAreaAsync(
            StartAreaRequest request,
            CancellationToken cancellationToken = default)
        {
            return await ExecuteProgressMutationAsync(
                MockOperationNames.AreaStart,
                request?.EventUuid,
                MockServerState.NormalizeEventPayload(
                    request?.EventUuid,
                    request?.MissionId,
                    request?.AreaId,
                    request?.LocalSequence ?? 0),
                request?.MissionId,
                cancellationToken).ConfigureAwait(false);
        }

        public async Task<AppResult<ProgressMutationResult>> PostAreaEventAsync(
            AreaEventRequest request,
            CancellationToken cancellationToken = default)
        {
            return await ExecuteProgressMutationAsync(
                MockOperationNames.AreaEvent,
                request?.EventUuid,
                MockServerState.NormalizeEventPayload(
                    request?.EventUuid,
                    request?.MissionId,
                    request?.AreaId,
                    request?.LocalSequence ?? 0) + "|type=" + (request?.EventType ?? string.Empty),
                request?.MissionId,
                cancellationToken).ConfigureAwait(false);
        }

        public async Task<AppResult<ProgressMutationResult>> CollectCollectibleAsync(
            CollectCollectibleRequest request,
            CancellationToken cancellationToken = default)
        {
            return await ExecuteProgressMutationAsync(
                MockOperationNames.AreaCollectible,
                request?.EventUuid,
                MockServerState.NormalizeEventPayload(
                    request?.EventUuid,
                    request?.MissionId,
                    request?.AreaId,
                    request?.LocalSequence ?? 0) + "|collectible=" + (request?.CollectibleId ?? string.Empty),
                request?.MissionId,
                cancellationToken).ConfigureAwait(false);
        }

        public async Task<AppResult<ProgressMutationResult>> CompleteAreaAsync(
            CompleteAreaRequest request,
            CancellationToken cancellationToken = default)
        {
            return await ExecuteProgressMutationAsync(
                MockOperationNames.AreaComplete,
                request?.EventUuid,
                MockServerState.NormalizeEventPayload(
                    request?.EventUuid,
                    request?.MissionId,
                    request?.AreaId,
                    request?.LocalSequence ?? 0),
                request?.MissionId,
                cancellationToken).ConfigureAwait(false);
        }

        public async Task<AppResult<IReadOnlyList<QuizSummary>>> GetQuizzesAsync(
            GetQuizzesRequest request,
            CancellationToken cancellationToken = default)
        {
            return await ExecuteAsync(
                MockOperationNames.QuizzesList,
                requireAuth: true,
                cancellationToken,
                () =>
                {
                    if (_options.MockScenario == MockApiScenario.EmptyData)
                    {
                        return Task.FromResult(AppResult<IReadOnlyList<QuizSummary>>.Success(
                            Array.Empty<QuizSummary>()));
                    }

                    AppResult<MockQuizListFixture> loaded =
                        _fixtures.LoadJson<MockQuizListFixture>(MockFixtureNames.Quizzes);
                    if (loaded.IsFailure)
                    {
                        return Task.FromResult(AppResult<IReadOnlyList<QuizSummary>>.Failure(loaded.Error));
                    }

                    return Task.FromResult(AppResult<IReadOnlyList<QuizSummary>>.Success(
                        MockFixtureMapper.ToQuizSummaries(loaded.Value.items)));
                }).ConfigureAwait(false);
        }

        public async Task<AppResult<QuizDetail>> GetQuizDetailAsync(
            QuizIdRequest request,
            CancellationToken cancellationToken = default)
        {
            return await ExecuteAsync(
                MockOperationNames.QuizDetail,
                requireAuth: true,
                cancellationToken,
                () =>
                {
                    AppResult<MockQuizDetailFixture> loaded =
                        _fixtures.LoadJson<MockQuizDetailFixture>(MockFixtureNames.QuizDetail);
                    if (loaded.IsFailure)
                    {
                        return Task.FromResult(AppResult<QuizDetail>.Failure(loaded.Error));
                    }

                    QuizDetail detail = MockFixtureMapper.ToQuizDetail(loaded.Value);
                    if (!string.IsNullOrWhiteSpace(request?.QuizId)
                        && detail != null
                        && !string.Equals(detail.Id, request.QuizId, StringComparison.Ordinal))
                    {
                        return Task.FromResult(AppResult<QuizDetail>.Failure(AppError.Api(
                            AppErrorCodes.QuizNotFound,
                            "Quiz was not found.",
                            404)));
                    }

                    return Task.FromResult(AppResult<QuizDetail>.Success(detail));
                }).ConfigureAwait(false);
        }

        public async Task<AppResult<QuizResult>> SubmitQuizAttemptAsync(
            SubmitQuizAttemptRequest request,
            CancellationToken cancellationToken = default)
        {
            return await ExecuteAsync(
                MockOperationNames.QuizAttemptSubmit,
                requireAuth: true,
                cancellationToken,
                () =>
                {
                    AppError seedError = EnsureSeeded();
                    if (seedError != null)
                    {
                        return Task.FromResult(AppResult<QuizResult>.Failure(seedError));
                    }

                    string clientUuid = request?.Submission?.ClientAttemptUuid?.Trim();
                    string normalized = MockServerState.NormalizeQuizPayload(request);

                    if (!string.IsNullOrWhiteSpace(clientUuid))
                    {
                        bool found = _state.TryGetIdempotent(
                            MockOperationNames.QuizAttemptSubmit,
                            clientUuid,
                            normalized,
                            out QuizResult prior,
                            out AppError mismatch,
                            out _);
                        if (mismatch != null)
                        {
                            return Task.FromResult(AppResult<QuizResult>.Failure(mismatch));
                        }

                        if (found)
                        {
                            // Retry after commit-then-timeout returns the original committed result.
                            return Task.FromResult(AppResult<QuizResult>.Success(prior));
                        }
                    }

                    AppResult<MockQuizResultFixture> loaded =
                        _fixtures.LoadJson<MockQuizResultFixture>(MockFixtureNames.QuizResult);
                    if (loaded.IsFailure)
                    {
                        return Task.FromResult(AppResult<QuizResult>.Failure(loaded.Error));
                    }

                    QuizResult template = MockFixtureMapper.ToQuizResult(loaded.Value);
                    QuizResult committed = _state.CommitQuizResult(request, template, _clock);
                    _state.StoreIdempotent(
                        MockOperationNames.QuizAttemptSubmit,
                        clientUuid,
                        normalized,
                        committed);

                    if (_options.MockScenario == MockApiScenario.QuizSubmissionTimeout)
                    {
                        return Task.FromResult(AppResult<QuizResult>.Failure(AppError.Network(
                            AppErrorCodes.NetworkTimeout,
                            "Quiz submission timed out after the mock server committed the attempt.",
                            isRetryable: true)));
                    }

                    return Task.FromResult(AppResult<QuizResult>.Success(committed));
                }).ConfigureAwait(false);
        }

        public async Task<AppResult<IReadOnlyList<QuizHistoryEntry>>> GetQuizResultsAsync(
            GetQuizResultsRequest request,
            CancellationToken cancellationToken = default)
        {
            return await ExecuteAsync(
                MockOperationNames.QuizResultsList,
                requireAuth: true,
                cancellationToken,
                () =>
                {
                    AppError seedError = EnsureSeeded();
                    if (seedError != null)
                    {
                        return Task.FromResult(AppResult<IReadOnlyList<QuizHistoryEntry>>.Failure(seedError));
                    }

                    if (_options.MockScenario == MockApiScenario.EmptyData)
                    {
                        return Task.FromResult(AppResult<IReadOnlyList<QuizHistoryEntry>>.Success(
                            Array.Empty<QuizHistoryEntry>()));
                    }

                    IReadOnlyList<QuizHistoryEntry> history = _state.QuizHistory;
                    if (history.Count > 0)
                    {
                        return Task.FromResult(AppResult<IReadOnlyList<QuizHistoryEntry>>.Success(history));
                    }

                    AppResult<MockQuizHistoryListFixture> loaded =
                        _fixtures.LoadJson<MockQuizHistoryListFixture>(MockFixtureNames.QuizHistory);
                    if (loaded.IsFailure)
                    {
                        return Task.FromResult(AppResult<IReadOnlyList<QuizHistoryEntry>>.Failure(loaded.Error));
                    }

                    return Task.FromResult(AppResult<IReadOnlyList<QuizHistoryEntry>>.Success(
                        MockFixtureMapper.ToQuizHistory(loaded.Value.items)));
                }).ConfigureAwait(false);
        }

        public async Task<AppResult<QuizResult>> GetQuizResultAsync(
            GetQuizResultRequest request,
            CancellationToken cancellationToken = default)
        {
            return await ExecuteAsync(
                MockOperationNames.QuizResultGet,
                requireAuth: true,
                cancellationToken,
                () =>
                {
                    AppError seedError = EnsureSeeded();
                    if (seedError != null)
                    {
                        return Task.FromResult(AppResult<QuizResult>.Failure(seedError));
                    }

                    if (_state.TryGetQuizResultByAttemptId(request?.AttemptId, out QuizResult found))
                    {
                        return Task.FromResult(AppResult<QuizResult>.Success(found));
                    }

                    AppResult<MockQuizResultFixture> loaded =
                        _fixtures.LoadJson<MockQuizResultFixture>(MockFixtureNames.QuizResult);
                    if (loaded.IsFailure)
                    {
                        return Task.FromResult(AppResult<QuizResult>.Failure(loaded.Error));
                    }

                    QuizResult result = MockFixtureMapper.ToQuizResult(loaded.Value);
                    if (!string.IsNullOrWhiteSpace(request?.AttemptId)
                        && result != null
                        && !string.Equals(result.AttemptId, request.AttemptId, StringComparison.Ordinal))
                    {
                        return Task.FromResult(AppResult<QuizResult>.Failure(AppError.Api(
                            AppErrorCodes.AttemptNotFound,
                            "Quiz attempt was not found.",
                            404)));
                    }

                    return Task.FromResult(AppResult<QuizResult>.Success(result));
                }).ConfigureAwait(false);
        }

        public async Task<AppResult<ProgressSummary>> GetProgressSummaryAsync(
            CancellationToken cancellationToken = default)
        {
            return await ExecuteAsync(
                MockOperationNames.ProgressSummary,
                requireAuth: true,
                cancellationToken,
                () =>
                {
                    AppError seedError = EnsureSeeded();
                    if (seedError != null)
                    {
                        return Task.FromResult(AppResult<ProgressSummary>.Failure(seedError));
                    }

                    if (_options.MockScenario == MockApiScenario.EmptyData)
                    {
                        return Task.FromResult(AppResult<ProgressSummary>.Success(new ProgressSummary()));
                    }

                    return Task.FromResult(AppResult<ProgressSummary>.Success(_state.ProgressSummary));
                }).ConfigureAwait(false);
        }

        public async Task<AppResult<IReadOnlyList<RewardSummary>>> GetRewardsAsync(
            CancellationToken cancellationToken = default)
        {
            return await ExecuteAsync(
                MockOperationNames.RewardsList,
                requireAuth: true,
                cancellationToken,
                () =>
                {
                    AppError seedError = EnsureSeeded();
                    if (seedError != null)
                    {
                        return Task.FromResult(AppResult<IReadOnlyList<RewardSummary>>.Failure(seedError));
                    }

                    if (_options.MockScenario == MockApiScenario.EmptyData)
                    {
                        return Task.FromResult(AppResult<IReadOnlyList<RewardSummary>>.Success(
                            Array.Empty<RewardSummary>()));
                    }

                    return Task.FromResult(AppResult<IReadOnlyList<RewardSummary>>.Success(_state.Rewards));
                }).ConfigureAwait(false);
        }

        public async Task<AppResult<RewardSummary>> UseRewardAsync(
            UseRewardRequest request,
            CancellationToken cancellationToken = default)
        {
            return await ExecuteAsync(
                MockOperationNames.RewardUse,
                requireAuth: true,
                cancellationToken,
                () =>
                {
                    AppError seedError = EnsureSeeded();
                    if (seedError != null)
                    {
                        return Task.FromResult(AppResult<RewardSummary>.Failure(seedError));
                    }

                    string requestUuid = request?.RequestUuid?.Trim();
                    string normalized = MockServerState.NormalizeRewardPayload(request);

                    if (!string.IsNullOrWhiteSpace(requestUuid))
                    {
                        bool found = _state.TryGetIdempotent(
                            MockOperationNames.RewardUse,
                            requestUuid,
                            normalized,
                            out RewardSummary prior,
                            out AppError mismatch,
                            out _);
                        if (mismatch != null)
                        {
                            return Task.FromResult(AppResult<RewardSummary>.Failure(mismatch));
                        }

                        if (found)
                        {
                            return Task.FromResult(AppResult<RewardSummary>.Success(prior));
                        }
                    }

                    RewardSummary used = _state.UseReward(request?.RewardCode, _clock);
                    if (used == null)
                    {
                        return Task.FromResult(AppResult<RewardSummary>.Failure(AppError.Api(
                            AppErrorCodes.RewardNotAvailable,
                            "Reward is not available.",
                            404)));
                    }

                    _state.StoreIdempotent(
                        MockOperationNames.RewardUse,
                        requestUuid,
                        normalized,
                        used);

                    if (_options.MockScenario == MockApiScenario.RewardUseTimeout)
                    {
                        return Task.FromResult(AppResult<RewardSummary>.Failure(AppError.Network(
                            AppErrorCodes.NetworkTimeout,
                            "Reward use timed out after the mock server committed the change.",
                            isRetryable: true)));
                    }

                    return Task.FromResult(AppResult<RewardSummary>.Success(used));
                }).ConfigureAwait(false);
        }

        public async Task<AppResult<IReadOnlyList<CertificateSummary>>> GetCertificatesAsync(
            CancellationToken cancellationToken = default)
        {
            return await ExecuteAsync(
                MockOperationNames.CertificatesList,
                requireAuth: true,
                cancellationToken,
                () =>
                {
                    if (_options.MockScenario == MockApiScenario.EmptyData)
                    {
                        return Task.FromResult(AppResult<IReadOnlyList<CertificateSummary>>.Success(
                            Array.Empty<CertificateSummary>()));
                    }

                    AppResult<MockCertificateListFixture> loaded =
                        _fixtures.LoadJson<MockCertificateListFixture>(MockFixtureNames.Certificates);
                    if (loaded.IsFailure)
                    {
                        return Task.FromResult(
                            AppResult<IReadOnlyList<CertificateSummary>>.Failure(loaded.Error));
                    }

                    return Task.FromResult(AppResult<IReadOnlyList<CertificateSummary>>.Success(
                        MockFixtureMapper.ToCertificates(loaded.Value.items)));
                }).ConfigureAwait(false);
        }

        public async Task<AppResult<CertificateSummary>> GetCertificateDetailAsync(
            CertificateIdRequest request,
            CancellationToken cancellationToken = default)
        {
            return await ExecuteAsync(
                MockOperationNames.CertificateDetail,
                requireAuth: true,
                cancellationToken,
                () =>
                {
                    AppResult<MockCertificateListFixture> loaded =
                        _fixtures.LoadJson<MockCertificateListFixture>(MockFixtureNames.Certificates);
                    if (loaded.IsFailure)
                    {
                        return Task.FromResult(AppResult<CertificateSummary>.Failure(loaded.Error));
                    }

                    IReadOnlyList<CertificateSummary> list =
                        MockFixtureMapper.ToCertificates(loaded.Value.items);
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (string.Equals(list[i].Id, request?.CertificateId, StringComparison.Ordinal))
                        {
                            return Task.FromResult(AppResult<CertificateSummary>.Success(list[i]));
                        }
                    }

                    if (list.Count > 0 && string.IsNullOrWhiteSpace(request?.CertificateId))
                    {
                        return Task.FromResult(AppResult<CertificateSummary>.Success(list[0]));
                    }

                    return Task.FromResult(AppResult<CertificateSummary>.Failure(AppError.Api(
                        AppErrorCodes.CertificateNotFound,
                        "Certificate was not found.",
                        404)));
                }).ConfigureAwait(false);
        }

        public async Task<AppResult<IReadOnlyList<AnnouncementSummary>>> GetAnnouncementsAsync(
            CancellationToken cancellationToken = default)
        {
            return await ExecuteAsync(
                MockOperationNames.AnnouncementsList,
                requireAuth: true,
                cancellationToken,
                () =>
                {
                    if (_options.MockScenario == MockApiScenario.EmptyData)
                    {
                        return Task.FromResult(AppResult<IReadOnlyList<AnnouncementSummary>>.Success(
                            Array.Empty<AnnouncementSummary>()));
                    }

                    AppResult<MockAnnouncementListFixture> loaded =
                        _fixtures.LoadJson<MockAnnouncementListFixture>(MockFixtureNames.Announcements);
                    if (loaded.IsFailure)
                    {
                        return Task.FromResult(
                            AppResult<IReadOnlyList<AnnouncementSummary>>.Failure(loaded.Error));
                    }

                    return Task.FromResult(AppResult<IReadOnlyList<AnnouncementSummary>>.Success(
                        MockFixtureMapper.ToAnnouncements(loaded.Value.items)));
                }).ConfigureAwait(false);
        }

        public async Task<AppResult<LeaderboardPage>> GetLeaderboardAsync(
            GetLeaderboardRequest request,
            CancellationToken cancellationToken = default)
        {
            return await ExecuteAsync(
                MockOperationNames.LeaderboardGet,
                requireAuth: true,
                cancellationToken,
                () =>
                {
                    if (_options.MockScenario == MockApiScenario.EmptyData)
                    {
                        return Task.FromResult(AppResult<LeaderboardPage>.Success(new LeaderboardPage
                        {
                            Context = new LeaderboardContext
                            {
                                Scope = request?.Scope ?? "section",
                                ScopeLabel = "Section",
                                Metric = "missions_completed",
                                MetricLabel = "Missions completed",
                                PeriodLabel = "This term",
                                ContextLabel = "Grade 5 A"
                            },
                            Entries = Array.Empty<LeaderboardEntry>()
                        }));
                    }

                    AppResult<MockLeaderboardFixture> loaded =
                        _fixtures.LoadJson<MockLeaderboardFixture>(MockFixtureNames.Leaderboard);
                    if (loaded.IsFailure)
                    {
                        return Task.FromResult(AppResult<LeaderboardPage>.Failure(loaded.Error));
                    }

                    return Task.FromResult(AppResult<LeaderboardPage>.Success(
                        MockFixtureMapper.ToLeaderboard(loaded.Value)));
                }).ConfigureAwait(false);
        }

        public async Task<AppResult<SyncStatus>> GetSyncStatusAsync(
            CancellationToken cancellationToken = default)
        {
            return await ExecuteAsync(
                MockOperationNames.SyncStatus,
                requireAuth: true,
                cancellationToken,
                () =>
                {
                    AppError seedError = EnsureSeeded();
                    if (seedError != null)
                    {
                        return Task.FromResult(AppResult<SyncStatus>.Failure(seedError));
                    }

                    return Task.FromResult(AppResult<SyncStatus>.Success(_state.SyncStatus));
                }).ConfigureAwait(false);
        }

        public async Task<AppResult<SyncPushResult>> PushSyncAsync(
            SyncPushRequest request,
            CancellationToken cancellationToken = default)
        {
            return await ExecuteAsync(
                MockOperationNames.SyncPush,
                requireAuth: true,
                cancellationToken,
                () =>
                {
                    AppError seedError = EnsureSeeded();
                    if (seedError != null)
                    {
                        return Task.FromResult(AppResult<SyncPushResult>.Failure(seedError));
                    }

                    string batchUuid = request?.BatchUuid?.Trim();
                    string normalized = MockServerState.NormalizeSyncPayload(request);

                    if (!string.IsNullOrWhiteSpace(batchUuid))
                    {
                        bool found = _state.TryGetIdempotent(
                            MockOperationNames.SyncPush,
                            batchUuid,
                            normalized,
                            out SyncPushResult prior,
                            out AppError mismatch,
                            out _);
                        if (mismatch != null)
                        {
                            return Task.FromResult(AppResult<SyncPushResult>.Failure(mismatch));
                        }

                        if (found)
                        {
                            return Task.FromResult(AppResult<SyncPushResult>.Success(prior));
                        }
                    }

                    if (_options.MockScenario == MockApiScenario.SyncConflict)
                    {
                        return Task.FromResult(AppResult<SyncPushResult>.Failure(AppError.Api(
                            AppErrorCodes.StaleClientRevision,
                            "Client revision is stale relative to the mock server.",
                            409,
                            isRetryable: true)));
                    }

                    SyncStatus current = _state.SyncStatus;
                    if (request != null
                        && current != null
                        && request.LastKnownServerRevision < current.Revision - 1)
                    {
                        return Task.FromResult(AppResult<SyncPushResult>.Failure(AppError.Api(
                            AppErrorCodes.StaleClientRevision,
                            "Client revision is stale relative to the mock server.",
                            409,
                            isRetryable: true)));
                    }

                    SyncPushResult result = _state.ApplySyncPush(request, _clock);
                    _state.StoreIdempotent(
                        MockOperationNames.SyncPush,
                        batchUuid,
                        normalized,
                        result);
                    return Task.FromResult(AppResult<SyncPushResult>.Success(result));
                }).ConfigureAwait(false);
        }

        private async Task<AppResult<ProgressMutationResult>> ExecuteProgressMutationAsync(
            string operation,
            string eventUuid,
            string normalizedPayload,
            string missionId,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync(
                operation,
                requireAuth: true,
                cancellationToken,
                () =>
                {
                    AppError seedError = EnsureSeeded();
                    if (seedError != null)
                    {
                        return Task.FromResult(AppResult<ProgressMutationResult>.Failure(seedError));
                    }

                    if (_options.MockScenario == MockApiScenario.LockedMission)
                    {
                        return Task.FromResult(AppResult<ProgressMutationResult>.Failure(AppError.Api(
                            AppErrorCodes.MissionLocked,
                            "Mission is locked by teacher policy.",
                            403)));
                    }

                    if (!string.IsNullOrWhiteSpace(missionId)
                        && !string.Equals(missionId, MockServerState.CanonicalMissionId, StringComparison.Ordinal))
                    {
                        return Task.FromResult(AppResult<ProgressMutationResult>.Failure(AppError.Api(
                            AppErrorCodes.MissionNotFound,
                            "Mission was not found.",
                            404)));
                    }

                    string uuid = eventUuid?.Trim();
                    if (!string.IsNullOrWhiteSpace(uuid))
                    {
                        bool found = _state.TryGetIdempotent(
                            operation,
                            uuid,
                            normalizedPayload,
                            out ProgressMutationResult prior,
                            out AppError mismatch,
                            out _);
                        if (mismatch != null)
                        {
                            return Task.FromResult(AppResult<ProgressMutationResult>.Failure(mismatch));
                        }

                        if (found)
                        {
                            return Task.FromResult(AppResult<ProgressMutationResult>.Success(prior));
                        }
                    }

                    ProgressMutationResult result = _state.ApplyProgressMutation(uuid, "accepted", _clock);
                    _state.StoreIdempotent(operation, uuid, normalizedPayload, result);
                    return Task.FromResult(AppResult<ProgressMutationResult>.Success(result));
                }).ConfigureAwait(false);
        }

        private AppResult<MissionDetail> ResolveMissionDetail(MissionIdRequest request)
        {
            AppError seedError = EnsureSeeded();
            if (seedError != null)
            {
                return AppResult<MissionDetail>.Failure(seedError);
            }

            MissionDetail detail = _state.MissionDetail;
            if (detail == null)
            {
                return AppResult<MissionDetail>.Failure(
                    AppErrorCodes.FixtureLoadFailed,
                    "Mission detail mock state was not seeded.");
            }

            if (!string.IsNullOrWhiteSpace(request?.MissionId)
                && detail.Mission != null
                && !string.Equals(detail.Mission.Id, request.MissionId, StringComparison.Ordinal))
            {
                return AppResult<MissionDetail>.Failure(AppError.Api(
                    AppErrorCodes.MissionNotFound,
                    "Mission was not found.",
                    404));
            }

            if (_options.MockScenario == MockApiScenario.LockedMission
                && detail.Mission != null
                && string.Equals(detail.Mission.Status, "locked", StringComparison.OrdinalIgnoreCase))
            {
                // Detail still returns; start mutations fail with MISSION_LOCKED.
            }

            return AppResult<MissionDetail>.Success(detail);
        }

        private async Task<AppResult<T>> ExecuteAsync<T>(
            string operation,
            bool requireAuth,
            CancellationToken cancellationToken,
            Func<Task<AppResult<T>>> action)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int delayMs = ResolveLatencyMilliseconds(operation);
            if (delayMs > 0)
            {
                await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (!_connectivity.IsOnline || _options.MockScenario == MockApiScenario.OfflineWithCache)
            {
                return AppResult<T>.Failure(AppError.Network(
                    AppErrorCodes.NetworkOffline,
                    "Device is offline."));
            }

            AppError scenarioError = EvaluateScenarioGate(operation, requireAuth);
            if (scenarioError != null)
            {
                return AppResult<T>.Failure(scenarioError);
            }

            if (requireAuth)
            {
                AppError authError = await EnsureAuthorizedAsync(cancellationToken).ConfigureAwait(false);
                if (authError != null)
                {
                    return AppResult<T>.Failure(authError);
                }
            }

            if (_options.LogGatewayOperations)
            {
                NutriMindLog.MockGateway("operation=" + operation + " scenario=" + _options.MockScenario);
            }

            // Fixture text is preloaded; JsonUtility/domain mapping is safe off the main thread.
            return await action().ConfigureAwait(false);
        }

        private AppError EvaluateScenarioGate(string operation, bool requireAuth)
        {
            switch (_options.MockScenario)
            {
                case MockApiScenario.RecoverableServerErrors:
                    if (!string.Equals(operation, MockOperationNames.AuthLogin, StringComparison.Ordinal)
                        && !string.Equals(operation, MockOperationNames.Ping, StringComparison.Ordinal))
                    {
                        return AppError.Api(
                            AppErrorCodes.ServiceUnavailable,
                            "Mock server is temporarily unavailable.",
                            503,
                            isRetryable: true,
                            retryAfterSeconds: 5);
                    }

                    break;

                case MockApiScenario.UnauthorizedAfterLogin:
                    if (requireAuth)
                    {
                        return AppError.Api(
                            AppErrorCodes.AuthTokenInvalid,
                            "Access token is no longer valid after mock login.",
                            401);
                    }

                    break;
            }

            return null;
        }

        private async Task<AppError> EnsureAuthorizedAsync(CancellationToken cancellationToken)
        {
            string token = await _tokenStore.ReadAsync(cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(token))
            {
                return AppError.Api(
                    AppErrorCodes.AuthTokenMissing,
                    "Authentication token is required.",
                    401);
            }

            return null;
        }

        private int ResolveLatencyMilliseconds(string operation)
        {
            int min = Math.Max(0, _options.MinimumMockLatencyMilliseconds);
            int max = Math.Max(min, _options.MaximumMockLatencyMilliseconds);
            int mid = min + ((max - min) / 2);

            switch (_options.MockScenario)
            {
                case MockApiScenario.QuizSubmissionTimeout
                    when string.Equals(operation, MockOperationNames.QuizAttemptSubmit, StringComparison.Ordinal):
                case MockApiScenario.RewardUseTimeout
                    when string.Equals(operation, MockOperationNames.RewardUse, StringComparison.Ordinal):
                    return Math.Max(mid, max);
                case MockApiScenario.RateLimitedLogin
                    when string.Equals(operation, MockOperationNames.AuthLogin, StringComparison.Ordinal):
                    return min;
                default:
                    return mid;
            }
        }

        private AppError EnsureSeeded()
        {
            lock (_seedGate)
            {
                if (_state.IsSeeded)
                {
                    return null;
                }

                if (_seedAttempted)
                {
                    return _seedError;
                }

                _seedAttempted = true;
                _seedError = SeedStateFromFixtures();
                return _seedError;
            }
        }

        private AppError SeedStateFromFixtures()
        {
            AppResult<MockStudentProfileFixture> profile =
                _fixtures.LoadJson<MockStudentProfileFixture>(MockFixtureNames.Profile);
            if (profile.IsFailure)
            {
                return profile.Error;
            }

            AppResult<MockSettingsFixture> settings =
                _fixtures.LoadJson<MockSettingsFixture>(MockFixtureNames.Settings);
            if (settings.IsFailure)
            {
                return settings.Error;
            }

            AppResult<MockMissionDetailFixture> mission =
                _fixtures.LoadJson<MockMissionDetailFixture>(MockFixtureNames.MissionDetail);
            if (mission.IsFailure)
            {
                return mission.Error;
            }

            AppResult<MockProgressSummaryFixture> progress =
                _fixtures.LoadJson<MockProgressSummaryFixture>(MockFixtureNames.ProgressSummary);
            if (progress.IsFailure)
            {
                return progress.Error;
            }

            AppResult<MockSyncStatusFixture> sync =
                _fixtures.LoadJson<MockSyncStatusFixture>(MockFixtureNames.SyncStatus);
            if (sync.IsFailure)
            {
                return sync.Error;
            }

            AppResult<MockRewardListFixture> rewards =
                _fixtures.LoadJson<MockRewardListFixture>(MockFixtureNames.Rewards);
            if (rewards.IsFailure)
            {
                return rewards.Error;
            }

            AppResult<MockQuizHistoryListFixture> history =
                _fixtures.LoadJson<MockQuizHistoryListFixture>(MockFixtureNames.QuizHistory);
            if (history.IsFailure)
            {
                return history.Error;
            }

            _state.SeedFromFixtures(
                MockFixtureMapper.ToProfile(profile.Value),
                MockFixtureMapper.ToSettings(settings.Value),
                MockFixtureMapper.ToMissionDetail(mission.Value),
                MockFixtureMapper.ToProgressSummary(progress.Value),
                MockFixtureMapper.ToSyncStatus(sync.Value),
                MockFixtureMapper.ToRewards(rewards.Value.items),
                MockFixtureMapper.ToQuizHistory(history.Value.items));

            if (_options.MockScenario == MockApiScenario.LockedMission)
            {
                _state.ApplyLockedMissionScenario();
            }

            return null;
        }
    }
}
