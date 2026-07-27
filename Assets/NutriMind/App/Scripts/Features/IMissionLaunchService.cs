using System;
using System.Threading;
using System.Threading.Tasks;
using NutriMind.Core.Bootstrap;
using NutriMind.Core.Data;
using NutriMind.Core.Persistence;
using NutriMind.Core.Utilities;

namespace NutriMind.App.Features
{
    public enum MissionLaunchKind
    {
        SimulatedStart,
        SimulatedContinue,
        SimulatedReview
    }

    public sealed class MissionLaunchResult
    {
        public bool Succeeded { get; set; }
        public MissionLaunchKind Kind { get; set; }
        public string MissionId { get; set; }
        public string EventUuid { get; set; }
        public string Message { get; set; }
        public AppError Error { get; set; }
        public bool GameplaySceneOpened { get; set; }
    }

    public interface IMissionLaunchService
    {
        Task<MissionLaunchResult> LaunchAsync(
            string missionId,
            MissionLaunchKind kind,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Mock launch: updates local progress + outbox atomically and shows a simulated result.
    /// Does not claim a gameplay scene opened unless one is observed.
    /// </summary>
    public sealed class MockMissionLaunchService : IMissionLaunchService
    {
        private readonly AppLifetime _lifetime;

        public MockMissionLaunchService(AppLifetime lifetime)
        {
            _lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
        }

        public Task<MissionLaunchResult> LaunchAsync(
            string missionId,
            MissionLaunchKind kind,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(missionId))
            {
                return Task.FromResult(new MissionLaunchResult
                {
                    Succeeded = false,
                    Kind = kind,
                    Error = new AppError(AppErrorCodes.ValidationFailed, "Mission id is required.")
                });
            }

            if (_lifetime.LocalProgressWriter == null || _lifetime.OutboxPayloadSerializer == null)
            {
                return Task.FromResult(new MissionLaunchResult
                {
                    Succeeded = false,
                    Kind = kind,
                    MissionId = missionId.Trim(),
                    Error = AppError.Configuration("Local progress writer is not composed.")
                });
            }

            string normalizedMissionId = missionId.Trim();
            string eventUuid = _lifetime.IdGenerator?.NewUuid()
                               ?? Guid.NewGuid().ToString("D");
            string now = (_lifetime.Clock?.UtcNow ?? DateTimeOffset.UtcNow).ToUniversalTime().ToString("o");

            string progressState = kind == MissionLaunchKind.SimulatedReview
                ? "completed"
                : "in_progress";

            var progress = new MissionProgressRecord
            {
                MissionId = normalizedMissionId,
                State = progressState,
                RequiredAreaCount = 3,
                StartedUtc = kind == MissionLaunchKind.SimulatedReview ? null : now,
                CompletedUtc = kind == MissionLaunchKind.SimulatedReview ? now : null
            };

            OutboxPayloadEnvelopeV1 envelope = OutboxPayloadSerializer.FromGameplayFields(
                manifestVersion: _lifetime.LastBootstrap?.RequiredManifestVersion ?? "v5",
                encounterId: normalizedMissionId,
                outcome: kind == MissionLaunchKind.SimulatedReview
                    ? "mission_reviewed"
                    : "mission_started");

            AppResult<string> payload = _lifetime.OutboxPayloadSerializer.Serialize(envelope);
            if (payload.IsFailure)
            {
                return Task.FromResult(new MissionLaunchResult
                {
                    Succeeded = false,
                    Kind = kind,
                    MissionId = normalizedMissionId,
                    EventUuid = eventUuid,
                    Error = payload.Error
                });
            }

            var write = new LocalProgressWriteRequest
            {
                MissionProgress = progress,
                OutboxEvent = new SyncOutboxRecord
                {
                    EventUuid = eventUuid,
                    EventType = "mission.started",
                    PayloadJson = payload.Value,
                    State = OutboxEventState.Pending,
                    ClientCreatedUtc = now
                }
            };

            AppResult commit = _lifetime.LocalProgressWriter.Commit(write);
            if (commit.IsFailure)
            {
                return Task.FromResult(new MissionLaunchResult
                {
                    Succeeded = false,
                    Kind = kind,
                    MissionId = normalizedMissionId,
                    EventUuid = eventUuid,
                    Error = commit.Error
                });
            }

            string label = kind == MissionLaunchKind.SimulatedContinue
                ? "Continue"
                : kind == MissionLaunchKind.SimulatedReview
                    ? "Review"
                    : "Start";

            return Task.FromResult(new MissionLaunchResult
            {
                Succeeded = true,
                Kind = kind,
                MissionId = normalizedMissionId,
                EventUuid = eventUuid,
                GameplaySceneOpened = false,
                Message = "Simulated mission " + label
                          + " recorded locally. The playable mission scene is not launched in this Mock runtime."
            });
        }
    }
}
