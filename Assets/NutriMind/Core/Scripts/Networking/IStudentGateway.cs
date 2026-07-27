using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NutriMind.Core.Data;

namespace NutriMind.Core.Networking
{
    /// <summary>
    /// Student API gateway. Implementations return <see cref="AppResult"/> / <see cref="AppResult{T}"/>
    /// for expected failures; cancellation uses <see cref="CancellationToken"/>.
    /// </summary>
    public interface IStudentGateway
    {
        Task<AppResult<PingStatus>> PingAsync(CancellationToken cancellationToken = default);

        Task<AppResult<ClientConfiguration>> GetConfigAsync(CancellationToken cancellationToken = default);

        Task<AppResult<LoginResult>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

        Task<AppResult> LogoutAsync(CancellationToken cancellationToken = default);

        Task<AppResult<BootstrapSnapshot>> GetBootstrapAsync(CancellationToken cancellationToken = default);

        Task<AppResult<StudentProfile>> GetProfileAsync(CancellationToken cancellationToken = default);

        Task<AppResult<StudentSettings>> GetSettingsAsync(CancellationToken cancellationToken = default);

        Task<AppResult<StudentSettings>> PatchSettingsAsync(
            PatchSettingsRequest request,
            CancellationToken cancellationToken = default);

        Task<AppResult<IReadOnlyList<SubjectSummary>>> GetSubjectsAsync(
            CancellationToken cancellationToken = default);

        Task<AppResult<IReadOnlyList<TermSummary>>> GetTermsAsync(
            GetTermsRequest request,
            CancellationToken cancellationToken = default);

        Task<AppResult<IReadOnlyList<MissionSummary>>> GetMissionsAsync(
            GetMissionsRequest request,
            CancellationToken cancellationToken = default);

        Task<AppResult<MissionDetail>> GetMissionDetailAsync(
            MissionIdRequest request,
            CancellationToken cancellationToken = default);

        Task<AppResult<MissionDetail>> GetMissionProgressAsync(
            MissionIdRequest request,
            CancellationToken cancellationToken = default);

        Task<AppResult<ProgressMutationResult>> StartMissionAsync(
            StartMissionRequest request,
            CancellationToken cancellationToken = default);

        Task<AppResult<ProgressMutationResult>> StartAreaAsync(
            StartAreaRequest request,
            CancellationToken cancellationToken = default);

        Task<AppResult<ProgressMutationResult>> PostAreaEventAsync(
            AreaEventRequest request,
            CancellationToken cancellationToken = default);

        Task<AppResult<ProgressMutationResult>> CollectCollectibleAsync(
            CollectCollectibleRequest request,
            CancellationToken cancellationToken = default);

        Task<AppResult<ProgressMutationResult>> CompleteAreaAsync(
            CompleteAreaRequest request,
            CancellationToken cancellationToken = default);

        Task<AppResult<IReadOnlyList<QuizSummary>>> GetQuizzesAsync(
            GetQuizzesRequest request,
            CancellationToken cancellationToken = default);

        Task<AppResult<QuizDetail>> GetQuizDetailAsync(
            QuizIdRequest request,
            CancellationToken cancellationToken = default);

        Task<AppResult<QuizResult>> SubmitQuizAttemptAsync(
            SubmitQuizAttemptRequest request,
            CancellationToken cancellationToken = default);

        Task<AppResult<IReadOnlyList<QuizHistoryEntry>>> GetQuizResultsAsync(
            GetQuizResultsRequest request,
            CancellationToken cancellationToken = default);

        Task<AppResult<QuizResult>> GetQuizResultAsync(
            GetQuizResultRequest request,
            CancellationToken cancellationToken = default);

        Task<AppResult<ProgressSummary>> GetProgressSummaryAsync(
            CancellationToken cancellationToken = default);

        Task<AppResult<IReadOnlyList<RewardSummary>>> GetRewardsAsync(
            CancellationToken cancellationToken = default);

        Task<AppResult<RewardSummary>> UseRewardAsync(
            UseRewardRequest request,
            CancellationToken cancellationToken = default);

        Task<AppResult<IReadOnlyList<CertificateSummary>>> GetCertificatesAsync(
            CancellationToken cancellationToken = default);

        Task<AppResult<CertificateSummary>> GetCertificateDetailAsync(
            CertificateIdRequest request,
            CancellationToken cancellationToken = default);

        Task<AppResult<IReadOnlyList<AnnouncementSummary>>> GetAnnouncementsAsync(
            CancellationToken cancellationToken = default);

        Task<AppResult<LeaderboardPage>> GetLeaderboardAsync(
            GetLeaderboardRequest request,
            CancellationToken cancellationToken = default);

        Task<AppResult<SyncStatus>> GetSyncStatusAsync(CancellationToken cancellationToken = default);

        Task<AppResult<SyncPushResult>> PushSyncAsync(
            SyncPushRequest request,
            CancellationToken cancellationToken = default);
    }
}
