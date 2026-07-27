using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NutriMind.Core.Data;

namespace NutriMind.Core.Networking
{
    /// <summary>
    /// Placeholder gateway for DevelopmentServer/ProductionServer modes until HTTP transport exists.
    /// Every call returns <see cref="AppErrorCodes.ClientConfigurationError"/>.
    /// </summary>
    public sealed class UnconfiguredStudentGateway : IStudentGateway
    {
        private const string DefaultMessage =
            "Student HTTP gateway is not configured for this runtime mode.";

        private readonly string _message;

        public UnconfiguredStudentGateway(string message = null)
        {
            _message = string.IsNullOrWhiteSpace(message) ? DefaultMessage : message.Trim();
        }

        public Task<AppResult<PingStatus>> PingAsync(CancellationToken cancellationToken = default)
        {
            return FailAsync<PingStatus>(cancellationToken);
        }

        public Task<AppResult<ClientConfiguration>> GetConfigAsync(CancellationToken cancellationToken = default)
        {
            return FailAsync<ClientConfiguration>(cancellationToken);
        }

        public Task<AppResult<LoginResult>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
        {
            return FailAsync<LoginResult>(cancellationToken);
        }

        public Task<AppResult> LogoutAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(AppResult.Failure(AppError.Configuration(_message)));
        }

        public Task<AppResult<BootstrapSnapshot>> GetBootstrapAsync(CancellationToken cancellationToken = default)
        {
            return FailAsync<BootstrapSnapshot>(cancellationToken);
        }

        public Task<AppResult<StudentProfile>> GetProfileAsync(CancellationToken cancellationToken = default)
        {
            return FailAsync<StudentProfile>(cancellationToken);
        }

        public Task<AppResult<StudentSettings>> GetSettingsAsync(CancellationToken cancellationToken = default)
        {
            return FailAsync<StudentSettings>(cancellationToken);
        }

        public Task<AppResult<StudentSettings>> PatchSettingsAsync(
            PatchSettingsRequest request,
            CancellationToken cancellationToken = default)
        {
            return FailAsync<StudentSettings>(cancellationToken);
        }

        public Task<AppResult<IReadOnlyList<SubjectSummary>>> GetSubjectsAsync(
            CancellationToken cancellationToken = default)
        {
            return FailAsync<IReadOnlyList<SubjectSummary>>(cancellationToken);
        }

        public Task<AppResult<IReadOnlyList<TermSummary>>> GetTermsAsync(
            GetTermsRequest request,
            CancellationToken cancellationToken = default)
        {
            return FailAsync<IReadOnlyList<TermSummary>>(cancellationToken);
        }

        public Task<AppResult<IReadOnlyList<MissionSummary>>> GetMissionsAsync(
            GetMissionsRequest request,
            CancellationToken cancellationToken = default)
        {
            return FailAsync<IReadOnlyList<MissionSummary>>(cancellationToken);
        }

        public Task<AppResult<MissionDetail>> GetMissionDetailAsync(
            MissionIdRequest request,
            CancellationToken cancellationToken = default)
        {
            return FailAsync<MissionDetail>(cancellationToken);
        }

        public Task<AppResult<MissionDetail>> GetMissionProgressAsync(
            MissionIdRequest request,
            CancellationToken cancellationToken = default)
        {
            return FailAsync<MissionDetail>(cancellationToken);
        }

        public Task<AppResult<ProgressMutationResult>> StartMissionAsync(
            StartMissionRequest request,
            CancellationToken cancellationToken = default)
        {
            return FailAsync<ProgressMutationResult>(cancellationToken);
        }

        public Task<AppResult<ProgressMutationResult>> StartAreaAsync(
            StartAreaRequest request,
            CancellationToken cancellationToken = default)
        {
            return FailAsync<ProgressMutationResult>(cancellationToken);
        }

        public Task<AppResult<ProgressMutationResult>> PostAreaEventAsync(
            AreaEventRequest request,
            CancellationToken cancellationToken = default)
        {
            return FailAsync<ProgressMutationResult>(cancellationToken);
        }

        public Task<AppResult<ProgressMutationResult>> CollectCollectibleAsync(
            CollectCollectibleRequest request,
            CancellationToken cancellationToken = default)
        {
            return FailAsync<ProgressMutationResult>(cancellationToken);
        }

        public Task<AppResult<ProgressMutationResult>> CompleteAreaAsync(
            CompleteAreaRequest request,
            CancellationToken cancellationToken = default)
        {
            return FailAsync<ProgressMutationResult>(cancellationToken);
        }

        public Task<AppResult<IReadOnlyList<QuizSummary>>> GetQuizzesAsync(
            GetQuizzesRequest request,
            CancellationToken cancellationToken = default)
        {
            return FailAsync<IReadOnlyList<QuizSummary>>(cancellationToken);
        }

        public Task<AppResult<QuizDetail>> GetQuizDetailAsync(
            QuizIdRequest request,
            CancellationToken cancellationToken = default)
        {
            return FailAsync<QuizDetail>(cancellationToken);
        }

        public Task<AppResult<QuizResult>> SubmitQuizAttemptAsync(
            SubmitQuizAttemptRequest request,
            CancellationToken cancellationToken = default)
        {
            return FailAsync<QuizResult>(cancellationToken);
        }

        public Task<AppResult<IReadOnlyList<QuizHistoryEntry>>> GetQuizResultsAsync(
            GetQuizResultsRequest request,
            CancellationToken cancellationToken = default)
        {
            return FailAsync<IReadOnlyList<QuizHistoryEntry>>(cancellationToken);
        }

        public Task<AppResult<QuizResult>> GetQuizResultAsync(
            GetQuizResultRequest request,
            CancellationToken cancellationToken = default)
        {
            return FailAsync<QuizResult>(cancellationToken);
        }

        public Task<AppResult<ProgressSummary>> GetProgressSummaryAsync(
            CancellationToken cancellationToken = default)
        {
            return FailAsync<ProgressSummary>(cancellationToken);
        }

        public Task<AppResult<IReadOnlyList<RewardSummary>>> GetRewardsAsync(
            CancellationToken cancellationToken = default)
        {
            return FailAsync<IReadOnlyList<RewardSummary>>(cancellationToken);
        }

        public Task<AppResult<RewardSummary>> UseRewardAsync(
            UseRewardRequest request,
            CancellationToken cancellationToken = default)
        {
            return FailAsync<RewardSummary>(cancellationToken);
        }

        public Task<AppResult<IReadOnlyList<CertificateSummary>>> GetCertificatesAsync(
            CancellationToken cancellationToken = default)
        {
            return FailAsync<IReadOnlyList<CertificateSummary>>(cancellationToken);
        }

        public Task<AppResult<CertificateSummary>> GetCertificateDetailAsync(
            CertificateIdRequest request,
            CancellationToken cancellationToken = default)
        {
            return FailAsync<CertificateSummary>(cancellationToken);
        }

        public Task<AppResult<IReadOnlyList<AnnouncementSummary>>> GetAnnouncementsAsync(
            CancellationToken cancellationToken = default)
        {
            return FailAsync<IReadOnlyList<AnnouncementSummary>>(cancellationToken);
        }

        public Task<AppResult<LeaderboardPage>> GetLeaderboardAsync(
            GetLeaderboardRequest request,
            CancellationToken cancellationToken = default)
        {
            return FailAsync<LeaderboardPage>(cancellationToken);
        }

        public Task<AppResult<SyncStatus>> GetSyncStatusAsync(CancellationToken cancellationToken = default)
        {
            return FailAsync<SyncStatus>(cancellationToken);
        }

        public Task<AppResult<SyncPushResult>> PushSyncAsync(
            SyncPushRequest request,
            CancellationToken cancellationToken = default)
        {
            return FailAsync<SyncPushResult>(cancellationToken);
        }

        private Task<AppResult<T>> FailAsync<T>(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(AppResult<T>.Failure(AppError.Configuration(_message)));
        }
    }
}
