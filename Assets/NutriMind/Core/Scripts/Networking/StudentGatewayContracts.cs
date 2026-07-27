using System;
using System.Collections.Generic;
using NutriMind.Core.Data;

namespace NutriMind.Core.Networking
{
    public sealed class PingStatus
    {
        public string Status { get; set; }
        public string Service { get; set; }
    }

    public sealed class LoginRequest
    {
        public string Lrn { get; set; }
        public string Pin { get; set; }
        public string DeviceName { get; set; }
    }

    public sealed class LoginResult
    {
        public string TokenType { get; set; } = "Bearer";
        public string AccessToken { get; set; }
        public DateTimeOffset? ExpiresAt { get; set; }
        public StudentProfile Student { get; set; }
    }

    public sealed class PatchSettingsRequest
    {
        public float? AudioVolume { get; set; }
        public float? MusicVolume { get; set; }
        public string Language { get; set; }
        public bool? ReducedMotion { get; set; }
        public bool? NotificationsEnabled { get; set; }
    }

    public sealed class GetTermsRequest
    {
        public string SubjectSlug { get; set; }
    }

    public sealed class GetMissionsRequest
    {
        public string SubjectId { get; set; }
        public string TermId { get; set; }
        public string Status { get; set; }
        public int? Page { get; set; }
        public int? PerPage { get; set; }
    }

    public sealed class MissionIdRequest
    {
        public string MissionId { get; set; }
    }

    public sealed class StartMissionRequest
    {
        public string MissionId { get; set; }
        public string EventUuid { get; set; }
        public int LocalSequence { get; set; }
        public DateTimeOffset ClientCreatedAt { get; set; }
    }

    public sealed class AreaIdRequest
    {
        public string MissionId { get; set; }
        public string AreaId { get; set; }
    }

    public sealed class StartAreaRequest
    {
        public string MissionId { get; set; }
        public string AreaId { get; set; }
        public string EventUuid { get; set; }
        public int LocalSequence { get; set; }
        public DateTimeOffset ClientCreatedAt { get; set; }
    }

    public sealed class AreaEventRequest
    {
        public string MissionId { get; set; }
        public string AreaId { get; set; }
        public string EventUuid { get; set; }
        public string EventType { get; set; }
        public int LocalSequence { get; set; }
        public DateTimeOffset ClientCreatedAt { get; set; }
        public string EncounterId { get; set; }
        public string QuestionId { get; set; }
        public int? AttemptNumber { get; set; }
        public string Outcome { get; set; }
        public bool ReviewRequired { get; set; }
        public GameplayEventPayload Payload { get; set; }
    }

    /// <summary>
    /// Optional gameplay event facts. Never used to transport static answer keys to the server.
    /// </summary>
    public sealed class GameplayEventPayload
    {
        public IReadOnlyList<string> SelectedOptionKeys { get; set; }
        public bool? IsCorrect { get; set; }
        public bool? HintShown { get; set; }
        public bool? ExplanationShown { get; set; }
        public string ObservationCode { get; set; }
        public string PredictionCode { get; set; }
        public IReadOnlyList<string> MaterialIds { get; set; }
        public string InvestigationActionId { get; set; }
        public string ResultCode { get; set; }
        public string ConclusionCode { get; set; }
        public string SolutionActionId { get; set; }
        public string HealthActionId { get; set; }
        public string WellnessResultId { get; set; }
        public float? Value { get; set; }
        public string Unit { get; set; }
        public string ReviewReason { get; set; }
    }

    public sealed class CollectCollectibleRequest
    {
        public string MissionId { get; set; }
        public string AreaId { get; set; }
        public string CollectibleId { get; set; }
        public string EventUuid { get; set; }
        public int LocalSequence { get; set; }
        public DateTimeOffset ClientCreatedAt { get; set; }
    }

    public sealed class CompleteAreaRequest
    {
        public string MissionId { get; set; }
        public string AreaId { get; set; }
        public string EventUuid { get; set; }
        public int LocalSequence { get; set; }
        public DateTimeOffset ClientCreatedAt { get; set; }
        public bool ReviewRequired { get; set; }
    }

    public sealed class ProgressMutationResult
    {
        public string EventUuid { get; set; }
        public string Status { get; set; }
        public MissionDetail CanonicalState { get; set; }
        public IReadOnlyDictionary<string, string> CanonicalStateFacts { get; set; }
    }

    public sealed class GetQuizzesRequest
    {
        public string SubjectId { get; set; }
        public string TermId { get; set; }
        public string Status { get; set; }
        public int? Page { get; set; }
        public int? PerPage { get; set; }
    }

    public sealed class QuizIdRequest
    {
        public string QuizId { get; set; }
    }

    public sealed class SubmitQuizAttemptRequest
    {
        public string QuizId { get; set; }
        public QuizAttemptSubmission Submission { get; set; }
    }

    public sealed class GetQuizResultRequest
    {
        public string AttemptId { get; set; }
    }

    public sealed class GetQuizResultsRequest
    {
        public string SubjectId { get; set; }
        public string TermId { get; set; }
        public int? Page { get; set; }
        public int? PerPage { get; set; }
    }

    public sealed class UseRewardRequest
    {
        public string RewardCode { get; set; }
        public string RequestUuid { get; set; }
    }

    public sealed class CertificateIdRequest
    {
        public string CertificateId { get; set; }
    }

    public sealed class GetLeaderboardRequest
    {
        public string Scope { get; set; }
        public string SubjectId { get; set; }
        public string TermId { get; set; }
    }

    public sealed class LeaderboardPage
    {
        public LeaderboardContext Context { get; set; }
        public IReadOnlyList<LeaderboardEntry> Entries { get; set; } = Array.Empty<LeaderboardEntry>();
    }

    public sealed class SyncPushRequest
    {
        public string BatchUuid { get; set; }
        public string ClientId { get; set; }
        public int LastKnownServerRevision { get; set; }
        public IReadOnlyList<SyncPushEvent> Events { get; set; } = Array.Empty<SyncPushEvent>();
    }

    public sealed class SyncPushEvent
    {
        public string EventUuid { get; set; }
        public string EventType { get; set; }
        public string GradeId { get; set; }
        public string SubjectId { get; set; }
        public string TermId { get; set; }
        public string MissionId { get; set; }
        public string AreaId { get; set; }
        public string EncounterId { get; set; }
        public string QuestionId { get; set; }
        public string CollectibleId { get; set; }
        public int? AttemptNumber { get; set; }
        public string Outcome { get; set; }
        public bool ReviewRequired { get; set; }
        public int LocalSequence { get; set; }
        public string ManifestVersion { get; set; }
        public DateTimeOffset ClientCreatedAt { get; set; }
        public GameplayEventPayload Payload { get; set; }
    }
}
