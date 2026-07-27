using System;
using System.Collections.Generic;
using System.Text;
using NutriMind.Core.Data;
using NutriMind.Core.Utilities;

namespace NutriMind.Core.Networking
{
    /// <summary>
    /// In-memory mock server mutations. Never rewrites fixture JSON files.
    /// </summary>
    public sealed class MockServerState
    {
        public const string ValidMockLrn = "123456789012";
        public const string ValidMockPin = "1234";
        public const string CanonicalMissionId = "g5_lq_t1_m01";

        private readonly object _gate = new object();
        private readonly Dictionary<string, IdempotencyEntry> _idempotency =
            new Dictionary<string, IdempotencyEntry>(StringComparer.Ordinal);
        private readonly Dictionary<string, QuizResult> _quizResultsByAttemptId =
            new Dictionary<string, QuizResult>(StringComparer.Ordinal);
        private readonly Dictionary<string, QuizResult> _quizResultsByClientUuid =
            new Dictionary<string, QuizResult>(StringComparer.Ordinal);
        private readonly List<QuizHistoryEntry> _quizHistory = new List<QuizHistoryEntry>();
        private readonly List<RewardSummary> _rewards = new List<RewardSummary>();
        private readonly HashSet<string> _timeoutCommittedKeys =
            new HashSet<string>(StringComparer.Ordinal);

        private StudentProfile _profile;
        private StudentSettings _settings;
        private MissionDetail _missionDetail;
        private ProgressSummary _progressSummary;
        private SyncStatus _syncStatus;
        private int _quizAttemptCounter = 1;
        private bool _seeded;

        public bool HasIssuedToken { get; private set; }

        public string IssuedAccessToken { get; private set; }

        public StudentProfile Profile
        {
            get
            {
                lock (_gate)
                {
                    return CloneProfile(_profile);
                }
            }
        }

        public StudentSettings Settings
        {
            get
            {
                lock (_gate)
                {
                    return CloneSettings(_settings);
                }
            }
        }

        public MissionDetail MissionDetail
        {
            get
            {
                lock (_gate)
                {
                    return CloneMissionDetail(_missionDetail);
                }
            }
        }

        public ProgressSummary ProgressSummary
        {
            get
            {
                lock (_gate)
                {
                    return CloneProgress(_progressSummary);
                }
            }
        }

        public SyncStatus SyncStatus
        {
            get
            {
                lock (_gate)
                {
                    return CloneSync(_syncStatus);
                }
            }
        }

        public IReadOnlyList<RewardSummary> Rewards
        {
            get
            {
                lock (_gate)
                {
                    return CloneRewards(_rewards);
                }
            }
        }

        public IReadOnlyList<QuizHistoryEntry> QuizHistory
        {
            get
            {
                lock (_gate)
                {
                    return CloneHistory(_quizHistory);
                }
            }
        }

        public void SeedFromFixtures(
            StudentProfile profile,
            StudentSettings settings,
            MissionDetail missionDetail,
            ProgressSummary progressSummary,
            SyncStatus syncStatus,
            IReadOnlyList<RewardSummary> rewards,
            IReadOnlyList<QuizHistoryEntry> quizHistory)
        {
            lock (_gate)
            {
                _profile = CloneProfile(profile);
                _settings = CloneSettings(settings);
                _missionDetail = CloneMissionDetail(missionDetail);
                _progressSummary = CloneProgress(progressSummary) ?? new ProgressSummary();
                _syncStatus = CloneSync(syncStatus) ?? new SyncStatus { Revision = 4 };
                _rewards.Clear();
                if (rewards != null)
                {
                    for (int i = 0; i < rewards.Count; i++)
                    {
                        _rewards.Add(CloneReward(rewards[i]));
                    }
                }

                _quizHistory.Clear();
                if (quizHistory != null)
                {
                    for (int i = 0; i < quizHistory.Count; i++)
                    {
                        _quizHistory.Add(CloneHistoryEntry(quizHistory[i]));
                    }
                }

                _quizResultsByAttemptId.Clear();
                _quizResultsByClientUuid.Clear();
                _idempotency.Clear();
                _timeoutCommittedKeys.Clear();
                _quizAttemptCounter = 1;
                HasIssuedToken = false;
                IssuedAccessToken = null;
                _seeded = true;
            }
        }

        public void ResetMutations()
        {
            lock (_gate)
            {
                _idempotency.Clear();
                _timeoutCommittedKeys.Clear();
                _quizResultsByAttemptId.Clear();
                _quizResultsByClientUuid.Clear();
                HasIssuedToken = false;
                IssuedAccessToken = null;
                _seeded = false;
            }
        }

        public bool IsSeeded
        {
            get
            {
                lock (_gate)
                {
                    return _seeded;
                }
            }
        }

        public void SetIssuedToken(string accessToken)
        {
            lock (_gate)
            {
                HasIssuedToken = !string.IsNullOrWhiteSpace(accessToken);
                IssuedAccessToken = HasIssuedToken ? accessToken.Trim() : null;
            }
        }

        public void ClearIssuedToken()
        {
            lock (_gate)
            {
                HasIssuedToken = false;
                IssuedAccessToken = null;
            }
        }

        public StudentSettings ApplySettingsPatch(PatchSettingsRequest request)
        {
            lock (_gate)
            {
                if (_settings == null)
                {
                    _settings = new StudentSettings();
                }

                if (request == null)
                {
                    return CloneSettings(_settings);
                }

                if (request.AudioVolume.HasValue)
                {
                    _settings.AudioVolume = request.AudioVolume.Value;
                }

                if (request.MusicVolume.HasValue)
                {
                    _settings.MusicVolume = request.MusicVolume.Value;
                }

                if (request.Language != null)
                {
                    _settings.Language = request.Language;
                }

                if (request.ReducedMotion.HasValue)
                {
                    _settings.ReducedMotion = request.ReducedMotion.Value;
                }

                if (request.NotificationsEnabled.HasValue)
                {
                    _settings.NotificationsEnabled = request.NotificationsEnabled.Value;
                }

                return CloneSettings(_settings);
            }
        }

        public void ApplyLockedMissionScenario()
        {
            lock (_gate)
            {
                if (_missionDetail?.Mission == null)
                {
                    return;
                }

                _missionDetail.Mission.Status = "locked";
                _missionDetail.Mission.LockedReason = "teacher_lock";
                _missionDetail.Mission.TeacherPolicy = "locked";
                if (_missionDetail.Mission.Progress != null)
                {
                    _missionDetail.Mission.Progress.State = "not_started";
                    _missionDetail.Mission.Progress.ActiveAreaId = null;
                    _missionDetail.Mission.Progress.CompletedAreaCount = 0;
                    _missionDetail.Mission.Progress.CollectibleCount = 0;
                }

                if (_missionDetail.Areas != null)
                {
                    var areas = new List<AreaProgressSummary>(_missionDetail.Areas.Count);
                    for (int i = 0; i < _missionDetail.Areas.Count; i++)
                    {
                        AreaProgressSummary area = CloneArea(_missionDetail.Areas[i]);
                        area.State = "locked";
                        area.CollectibleCollected = false;
                        area.CompletedAt = null;
                        areas.Add(area);
                    }

                    _missionDetail.Areas = areas;
                }

                _missionDetail.NewlyUnlockedIds = Array.Empty<string>();
            }
        }

        public ProgressMutationResult ApplyProgressMutation(
            string eventUuid,
            string status,
            IAppClock clock)
        {
            lock (_gate)
            {
                if (_missionDetail?.Mission?.Progress != null)
                {
                    _missionDetail.Mission.Progress.Revision += 1;
                    if (_syncStatus != null)
                    {
                        _syncStatus.Revision = _missionDetail.Mission.Progress.Revision;
                        _syncStatus.LastSyncedAt = clock != null ? clock.UtcNow : DateTimeOffset.UtcNow;
                    }
                }

                var facts = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["mission_id"] = _missionDetail?.Mission?.Id ?? CanonicalMissionId,
                    ["revision"] = (_missionDetail?.Mission?.Progress?.Revision ?? 0).ToString(),
                    ["mission_state"] = _missionDetail?.Mission?.Progress?.State ?? "unknown",
                    ["active_area_id"] = _missionDetail?.Mission?.Progress?.ActiveAreaId ?? string.Empty
                };

                return new ProgressMutationResult
                {
                    EventUuid = eventUuid,
                    Status = string.IsNullOrWhiteSpace(status) ? "accepted" : status,
                    CanonicalState = CloneMissionDetail(_missionDetail),
                    CanonicalStateFacts = facts
                };
            }
        }

        public QuizResult CommitQuizResult(
            SubmitQuizAttemptRequest request,
            QuizResult template,
            IAppClock clock)
        {
            lock (_gate)
            {
                string clientUuid = request?.Submission?.ClientAttemptUuid?.Trim();
                string attemptId = "attempt_mock_" + _quizAttemptCounter.ToString("D3");
                _quizAttemptCounter++;

                QuizResult result = CloneQuizResult(template) ?? new QuizResult();
                result.AttemptId = attemptId;
                result.QuizId = request?.QuizId;
                result.ClientAttemptUuid = clientUuid;
                result.SubmittedAt = clock != null ? clock.UtcNow : DateTimeOffset.UtcNow;
                if (string.IsNullOrWhiteSpace(result.Status))
                {
                    result.Status = "scored";
                }

                if (!string.IsNullOrWhiteSpace(clientUuid))
                {
                    _quizResultsByClientUuid[clientUuid] = CloneQuizResult(result);
                }

                _quizResultsByAttemptId[attemptId] = CloneQuizResult(result);

                _quizHistory.Insert(0, new QuizHistoryEntry
                {
                    AttemptId = result.AttemptId,
                    QuizId = result.QuizId,
                    QuizTitle = "Story Elements Check",
                    SubjectId = "subject_literaquest",
                    TermId = "term_1",
                    Status = result.Status,
                    Percentage = result.Percentage,
                    Passed = result.Passed,
                    SubmittedAt = result.SubmittedAt,
                    FeedbackVisible = result.FeedbackVisible
                });

                if (_progressSummary != null)
                {
                    _progressSummary.QuizAttempts += 1;
                }

                return CloneQuizResult(result);
            }
        }

        public bool TryGetQuizResultByAttemptId(string attemptId, out QuizResult result)
        {
            lock (_gate)
            {
                if (!string.IsNullOrWhiteSpace(attemptId)
                    && _quizResultsByAttemptId.TryGetValue(attemptId.Trim(), out QuizResult found))
                {
                    result = CloneQuizResult(found);
                    return true;
                }

                result = null;
                return false;
            }
        }

        public bool TryGetQuizResultByClientUuid(string clientUuid, out QuizResult result)
        {
            lock (_gate)
            {
                if (!string.IsNullOrWhiteSpace(clientUuid)
                    && _quizResultsByClientUuid.TryGetValue(clientUuid.Trim(), out QuizResult found))
                {
                    result = CloneQuizResult(found);
                    return true;
                }

                result = null;
                return false;
            }
        }

        public RewardSummary UseReward(string rewardCode, IAppClock clock)
        {
            lock (_gate)
            {
                for (int i = 0; i < _rewards.Count; i++)
                {
                    if (!string.Equals(_rewards[i].RewardCode, rewardCode, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    RewardSummary reward = CloneReward(_rewards[i]);
                    if (string.Equals(reward.Status, "used", StringComparison.OrdinalIgnoreCase))
                    {
                        return reward;
                    }

                    reward.Status = "used";
                    reward.UsedAt = clock != null ? clock.UtcNow : DateTimeOffset.UtcNow;
                    _rewards[i] = reward;
                    return CloneReward(reward);
                }

                return null;
            }
        }

        public SyncPushResult ApplySyncPush(SyncPushRequest request, IAppClock clock)
        {
            lock (_gate)
            {
                if (_syncStatus == null)
                {
                    _syncStatus = new SyncStatus();
                }

                int accepted = 0;
                int duplicate = 0;
                var events = new List<SyncPushEventResult>();
                if (request?.Events != null)
                {
                    for (int i = 0; i < request.Events.Count; i++)
                    {
                        SyncPushEvent evt = request.Events[i];
                        string status = "accepted";
                        accepted++;
                        events.Add(new SyncPushEventResult
                        {
                            EventUuid = evt?.EventUuid,
                            Status = status,
                            ErrorCode = null
                        });
                    }
                }

                _syncStatus.Revision += 1;
                _syncStatus.PendingServerActions = false;
                _syncStatus.LastSyncedAt = clock != null ? clock.UtcNow : DateTimeOffset.UtcNow;
                if (_missionDetail?.Mission?.Progress != null)
                {
                    _missionDetail.Mission.Progress.Revision = _syncStatus.Revision;
                }

                return new SyncPushResult
                {
                    BatchUuid = request?.BatchUuid,
                    ServerRevision = _syncStatus.Revision,
                    AcceptedCount = accepted,
                    DuplicateCount = duplicate,
                    RejectedCount = 0,
                    DeferredCount = 0,
                    Events = events
                };
            }
        }

        public bool TryGetIdempotent<T>(
            string operation,
            string uuid,
            string normalizedPayload,
            out T value,
            out AppError mismatchError,
            out bool pendingTimeoutReplay)
        {
            value = default;
            mismatchError = null;
            pendingTimeoutReplay = false;

            if (string.IsNullOrWhiteSpace(uuid))
            {
                return false;
            }

            string key = BuildIdempotencyKey(operation, uuid);
            lock (_gate)
            {
                if (!_idempotency.TryGetValue(key, out IdempotencyEntry entry))
                {
                    return false;
                }

                if (!string.Equals(entry.NormalizedPayload, normalizedPayload, StringComparison.Ordinal))
                {
                    mismatchError = AppError.Api(
                        AppErrorCodes.IdempotencyPayloadMismatch,
                        "Idempotency key was reused with a different payload.",
                        409);
                    return false;
                }

                pendingTimeoutReplay = entry.ReturnTimeoutOnceMore;
                if (entry.ReturnTimeoutOnceMore)
                {
                    entry.ReturnTimeoutOnceMore = false;
                }

                if (entry.Value is T typed)
                {
                    value = typed;
                    return true;
                }

                return false;
            }
        }

        public void StoreIdempotent<T>(
            string operation,
            string uuid,
            string normalizedPayload,
            T value,
            bool returnTimeoutOnNextRead = false)
        {
            if (string.IsNullOrWhiteSpace(uuid))
            {
                return;
            }

            string key = BuildIdempotencyKey(operation, uuid);
            lock (_gate)
            {
                _idempotency[key] = new IdempotencyEntry
                {
                    NormalizedPayload = normalizedPayload ?? string.Empty,
                    Value = value,
                    ReturnTimeoutOnceMore = returnTimeoutOnNextRead
                };

                if (returnTimeoutOnNextRead)
                {
                    _timeoutCommittedKeys.Add(key);
                }
            }
        }

        public bool WasTimeoutCommitted(string operation, string uuid)
        {
            if (string.IsNullOrWhiteSpace(uuid))
            {
                return false;
            }

            string key = BuildIdempotencyKey(operation, uuid);
            lock (_gate)
            {
                return _timeoutCommittedKeys.Contains(key);
            }
        }

        public static string NormalizeQuizPayload(SubmitQuizAttemptRequest request)
        {
            var builder = new StringBuilder();
            builder.Append("quiz=").Append(request?.QuizId ?? string.Empty);
            builder.Append("|uuid=").Append(request?.Submission?.ClientAttemptUuid ?? string.Empty);
            builder.Append("|answers=");
            if (request?.Submission?.Answers != null)
            {
                for (int i = 0; i < request.Submission.Answers.Count; i++)
                {
                    QuizAnswerSelection answer = request.Submission.Answers[i];
                    builder.Append(answer?.QuestionId ?? string.Empty).Append(':');
                    if (answer?.SelectedOptionKeys != null)
                    {
                        var keys = new List<string>(answer.SelectedOptionKeys);
                        keys.Sort(StringComparer.Ordinal);
                        builder.Append(string.Join(",", keys));
                    }

                    builder.Append(';');
                }
            }

            return builder.ToString();
        }

        public static string NormalizeRewardPayload(UseRewardRequest request)
        {
            return "reward=" + (request?.RewardCode ?? string.Empty)
                + "|uuid=" + (request?.RequestUuid ?? string.Empty);
        }

        public static string NormalizeSyncPayload(SyncPushRequest request)
        {
            var builder = new StringBuilder();
            builder.Append("batch=").Append(request?.BatchUuid ?? string.Empty);
            builder.Append("|rev=").Append(request?.LastKnownServerRevision ?? 0);
            builder.Append("|events=");
            if (request?.Events != null)
            {
                for (int i = 0; i < request.Events.Count; i++)
                {
                    SyncPushEvent evt = request.Events[i];
                    builder.Append(evt?.EventUuid ?? string.Empty)
                        .Append(':')
                        .Append(evt?.EventType ?? string.Empty)
                        .Append(':')
                        .Append(evt?.LocalSequence ?? 0)
                        .Append(';');
                }
            }

            return builder.ToString();
        }

        public static string NormalizeEventPayload(string eventUuid, string missionId, string areaId, int localSequence)
        {
            return "event=" + (eventUuid ?? string.Empty)
                + "|mission=" + (missionId ?? string.Empty)
                + "|area=" + (areaId ?? string.Empty)
                + "|seq=" + localSequence;
        }

        private static string BuildIdempotencyKey(string operation, string uuid)
        {
            return (operation ?? string.Empty) + "|" + uuid.Trim();
        }

        private sealed class IdempotencyEntry
        {
            public string NormalizedPayload;
            public object Value;
            public bool ReturnTimeoutOnceMore;
        }

        private static StudentProfile CloneProfile(StudentProfile source)
        {
            if (source == null)
            {
                return null;
            }

            return new StudentProfile
            {
                Id = source.Id,
                DisplayName = source.DisplayName,
                LrnMasked = source.LrnMasked,
                GradeId = source.GradeId,
                IsActive = source.IsActive,
                Section = source.Section == null
                    ? null
                    : new StudentSection
                    {
                        Id = source.Section.Id,
                        Name = source.Section.Name,
                        GradeId = source.Section.GradeId
                    }
            };
        }

        private static StudentSettings CloneSettings(StudentSettings source)
        {
            if (source == null)
            {
                return null;
            }

            return new StudentSettings
            {
                AudioVolume = source.AudioVolume,
                MusicVolume = source.MusicVolume,
                Language = source.Language,
                ReducedMotion = source.ReducedMotion,
                NotificationsEnabled = source.NotificationsEnabled
            };
        }

        private static ProgressSummary CloneProgress(ProgressSummary source)
        {
            if (source == null)
            {
                return null;
            }

            return new ProgressSummary
            {
                MissionsStarted = source.MissionsStarted,
                MissionsCompleted = source.MissionsCompleted,
                AreasCompleted = source.AreasCompleted,
                ReviewRequiredCount = source.ReviewRequiredCount,
                QuizAttempts = source.QuizAttempts
            };
        }

        private static SyncStatus CloneSync(SyncStatus source)
        {
            if (source == null)
            {
                return null;
            }

            return new SyncStatus
            {
                PendingServerActions = source.PendingServerActions,
                Revision = source.Revision,
                PendingOutboxCount = source.PendingOutboxCount,
                LastSyncedAt = source.LastSyncedAt
            };
        }

        private static MissionDetail CloneMissionDetail(MissionDetail source)
        {
            if (source == null)
            {
                return null;
            }

            var areas = new List<AreaProgressSummary>();
            if (source.Areas != null)
            {
                for (int i = 0; i < source.Areas.Count; i++)
                {
                    areas.Add(CloneArea(source.Areas[i]));
                }
            }

            string[] unlocked = Array.Empty<string>();
            if (source.NewlyUnlockedIds != null && source.NewlyUnlockedIds.Count > 0)
            {
                unlocked = new string[source.NewlyUnlockedIds.Count];
                for (int i = 0; i < source.NewlyUnlockedIds.Count; i++)
                {
                    unlocked[i] = source.NewlyUnlockedIds[i];
                }
            }

            return new MissionDetail
            {
                Mission = CloneMissionSummary(source.Mission),
                Areas = areas,
                NewlyUnlockedIds = unlocked
            };
        }

        private static MissionSummary CloneMissionSummary(MissionSummary source)
        {
            if (source == null)
            {
                return null;
            }

            return new MissionSummary
            {
                Id = source.Id,
                GradeId = source.GradeId,
                SubjectId = source.SubjectId,
                TermId = source.TermId,
                Title = source.Title,
                Order = source.Order,
                Status = source.Status,
                LockedReason = source.LockedReason,
                AvailabilitySource = source.AvailabilitySource,
                TeacherPolicy = source.TeacherPolicy,
                AreaCount = source.AreaCount,
                Progress = source.Progress == null
                    ? null
                    : new MissionProgressSummary
                    {
                        State = source.Progress.State,
                        ActiveAreaId = source.Progress.ActiveAreaId,
                        CompletedAreaCount = source.Progress.CompletedAreaCount,
                        RequiredAreaCount = source.Progress.RequiredAreaCount,
                        CollectibleCount = source.Progress.CollectibleCount,
                        RequiredCollectibleCount = source.Progress.RequiredCollectibleCount,
                        CompletedAt = source.Progress.CompletedAt,
                        Revision = source.Progress.Revision
                    }
            };
        }

        private static AreaProgressSummary CloneArea(AreaProgressSummary source)
        {
            if (source == null)
            {
                return null;
            }

            return new AreaProgressSummary
            {
                Id = source.Id,
                Order = source.Order,
                Phase = source.Phase,
                State = source.State,
                ReviewRequired = source.ReviewRequired,
                CollectibleId = source.CollectibleId,
                CollectibleCollected = source.CollectibleCollected,
                CompletedAt = source.CompletedAt
            };
        }

        private static RewardSummary CloneReward(RewardSummary source)
        {
            if (source == null)
            {
                return null;
            }

            return new RewardSummary
            {
                RewardCode = source.RewardCode,
                Title = source.Title,
                Description = source.Description,
                SupportingText = source.SupportingText,
                Status = source.Status,
                LockedReason = source.LockedReason,
                EarnedAt = source.EarnedAt,
                UsedAt = source.UsedAt
            };
        }

        private static List<RewardSummary> CloneRewards(List<RewardSummary> source)
        {
            var list = new List<RewardSummary>(source.Count);
            for (int i = 0; i < source.Count; i++)
            {
                list.Add(CloneReward(source[i]));
            }

            return list;
        }

        private static QuizHistoryEntry CloneHistoryEntry(QuizHistoryEntry source)
        {
            if (source == null)
            {
                return null;
            }

            return new QuizHistoryEntry
            {
                AttemptId = source.AttemptId,
                QuizId = source.QuizId,
                QuizTitle = source.QuizTitle,
                SubjectId = source.SubjectId,
                TermId = source.TermId,
                Status = source.Status,
                Percentage = source.Percentage,
                Passed = source.Passed,
                SubmittedAt = source.SubmittedAt,
                FeedbackVisible = source.FeedbackVisible
            };
        }

        private static List<QuizHistoryEntry> CloneHistory(List<QuizHistoryEntry> source)
        {
            var list = new List<QuizHistoryEntry>(source.Count);
            for (int i = 0; i < source.Count; i++)
            {
                list.Add(CloneHistoryEntry(source[i]));
            }

            return list;
        }

        private static QuizResult CloneQuizResult(QuizResult source)
        {
            if (source == null)
            {
                return null;
            }

            var answers = new List<QuizResultAnswer>();
            if (source.Answers != null)
            {
                for (int i = 0; i < source.Answers.Count; i++)
                {
                    QuizResultAnswer a = source.Answers[i];
                    IReadOnlyList<string> keys = Array.Empty<string>();
                    if (a?.SelectedOptionKeys != null && a.SelectedOptionKeys.Count > 0)
                    {
                        var copy = new string[a.SelectedOptionKeys.Count];
                        for (int k = 0; k < a.SelectedOptionKeys.Count; k++)
                        {
                            copy[k] = a.SelectedOptionKeys[k];
                        }

                        keys = copy;
                    }

                    answers.Add(new QuizResultAnswer
                    {
                        QuestionId = a?.QuestionId,
                        Correct = a?.Correct,
                        EarnedPoints = a?.EarnedPoints,
                        SelectedOptionKeys = keys
                    });
                }
            }

            return new QuizResult
            {
                AttemptId = source.AttemptId,
                QuizId = source.QuizId,
                ClientAttemptUuid = source.ClientAttemptUuid,
                Status = source.Status,
                EarnedPoints = source.EarnedPoints,
                PossiblePoints = source.PossiblePoints,
                Percentage = source.Percentage,
                Passed = source.Passed,
                CorrectCount = source.CorrectCount,
                IncorrectCount = source.IncorrectCount,
                UnansweredCount = source.UnansweredCount,
                SubmittedAt = source.SubmittedAt,
                FeedbackVisible = source.FeedbackVisible,
                Answers = answers
            };
        }
    }
}
