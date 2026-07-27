using System;
using NutriMind.Core.Data;
using NutriMind.Core.Networking;
using UnityEngine;

namespace NutriMind.Core.Persistence
{
    /// <summary>
    /// Versioned local outbox payload envelope. JsonUtility-compatible.
    /// </summary>
    [Serializable]
    public sealed class OutboxPayloadEnvelopeV1
    {
        public int schemaVersion = 1;
        public string manifestVersion;
        public string encounterId;
        public string questionId;
        public string collectibleId;
        public int attemptNumber;
        public bool hasAttemptNumber;
        public string outcome;
        public bool reviewRequired;
        public GameplayEventPayloadV1 payload;
    }

    [Serializable]
    public sealed class GameplayEventPayloadV1
    {
        public string[] selectedOptionKeys;
        public bool isCorrect;
        public bool hasIsCorrect;
        public bool hintShown;
        public bool hasHintShown;
        public bool explanationShown;
        public bool hasExplanationShown;
        public string observation;
        public string prediction;
        public string[] materials;
        public string investigationAction;
        public string result;
        public string conclusion;
        public string solutionAction;
        public string healthAction;
        public string wellnessResult;
        public float value;
        public bool hasValue;
        public string unit;
        public string reviewReason;
    }

    public interface IOutboxPayloadSerializer
    {
        AppResult<string> Serialize(OutboxPayloadEnvelopeV1 envelope);

        AppResult<OutboxPayloadEnvelopeV1> Deserialize(string payloadJson);

        AppResult<SyncPushEvent> MapToNetworkEvent(
            Sync.SyncPushEvent sourceEvent,
            OutboxPayloadEnvelopeV1 envelope);
    }

    public sealed class OutboxPayloadSerializer : IOutboxPayloadSerializer
    {
        public const int SupportedSchemaVersion = 1;

        public AppResult<string> Serialize(OutboxPayloadEnvelopeV1 envelope)
        {
            if (envelope == null)
            {
                return AppResult<string>.Failure(
                    AppErrorCodes.ValidationFailed,
                    "Outbox payload envelope is required.");
            }

            if (envelope.schemaVersion <= 0)
            {
                envelope.schemaVersion = SupportedSchemaVersion;
            }

            try
            {
                string json = JsonUtility.ToJson(envelope);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return AppResult<string>.Failure(
                        AppErrorCodes.SyncPayloadInvalid,
                        "Outbox payload serialization produced empty JSON.");
                }

                return AppResult<string>.Success(json);
            }
            catch (Exception exception)
            {
                return AppResult<string>.Failure(AppError.FromException(exception));
            }
        }

        public AppResult<OutboxPayloadEnvelopeV1> Deserialize(string payloadJson)
        {
            if (string.IsNullOrWhiteSpace(payloadJson))
            {
                return AppResult<OutboxPayloadEnvelopeV1>.Failure(
                    AppErrorCodes.SyncPayloadInvalid,
                    "Outbox payload JSON is empty.");
            }

            OutboxPayloadEnvelopeV1 envelope;
            try
            {
                envelope = JsonUtility.FromJson<OutboxPayloadEnvelopeV1>(payloadJson);
            }
            catch (Exception)
            {
                return AppResult<OutboxPayloadEnvelopeV1>.Failure(
                    AppErrorCodes.SyncPayloadInvalid,
                    "Outbox payload JSON could not be parsed.");
            }

            if (envelope == null)
            {
                return AppResult<OutboxPayloadEnvelopeV1>.Failure(
                    AppErrorCodes.SyncPayloadInvalid,
                    "Outbox payload deserialized to null.");
            }

            if (envelope.schemaVersion <= 0)
            {
                return AppResult<OutboxPayloadEnvelopeV1>.Failure(
                    AppErrorCodes.SyncPayloadInvalid,
                    "Outbox payload schema_version is missing or invalid.");
            }

            if (envelope.schemaVersion != SupportedSchemaVersion)
            {
                return AppResult<OutboxPayloadEnvelopeV1>.Failure(
                    AppErrorCodes.SyncPayloadVersionUnsupported,
                    "Outbox payload schema_version " + envelope.schemaVersion + " is unsupported.");
            }

            return AppResult<OutboxPayloadEnvelopeV1>.Success(envelope);
        }

        public AppResult<SyncPushEvent> MapToNetworkEvent(
            Sync.SyncPushEvent sourceEvent,
            OutboxPayloadEnvelopeV1 envelope)
        {
            if (sourceEvent == null)
            {
                return AppResult<SyncPushEvent>.Failure(
                    AppErrorCodes.SyncPayloadInvalid,
                    "Sync push source event is required.");
            }

            if (envelope == null)
            {
                return AppResult<SyncPushEvent>.Failure(
                    AppErrorCodes.SyncPayloadInvalid,
                    "Outbox payload envelope is required.");
            }

            DateTimeOffset createdAt = DateTimeOffset.UtcNow;
            if (!string.IsNullOrWhiteSpace(sourceEvent.ClientCreatedUtc)
                && DateTimeOffset.TryParse(sourceEvent.ClientCreatedUtc, out DateTimeOffset parsed))
            {
                createdAt = parsed;
            }

            var mapped = new SyncPushEvent
            {
                EventUuid = sourceEvent.EventUuid,
                EventType = sourceEvent.EventType,
                GradeId = sourceEvent.GradeId,
                SubjectId = sourceEvent.SubjectId,
                TermId = sourceEvent.TermId,
                MissionId = sourceEvent.MissionId,
                AreaId = sourceEvent.AreaId,
                EncounterId = envelope.encounterId,
                QuestionId = envelope.questionId,
                CollectibleId = envelope.collectibleId,
                AttemptNumber = envelope.hasAttemptNumber ? envelope.attemptNumber : (int?)null,
                Outcome = envelope.outcome,
                ReviewRequired = envelope.reviewRequired,
                LocalSequence = (int)sourceEvent.LocalSequence,
                ManifestVersion = envelope.manifestVersion,
                ClientCreatedAt = createdAt,
                Payload = ToGameplayPayload(envelope.payload)
            };

            return AppResult<SyncPushEvent>.Success(mapped);
        }

        public static OutboxPayloadEnvelopeV1 FromGameplayFields(
            string manifestVersion = null,
            string encounterId = null,
            string questionId = null,
            string collectibleId = null,
            int? attemptNumber = null,
            string outcome = null,
            bool reviewRequired = false,
            GameplayEventPayload payload = null)
        {
            return new OutboxPayloadEnvelopeV1
            {
                schemaVersion = SupportedSchemaVersion,
                manifestVersion = manifestVersion,
                encounterId = encounterId,
                questionId = questionId,
                collectibleId = collectibleId,
                attemptNumber = attemptNumber ?? 0,
                hasAttemptNumber = attemptNumber.HasValue,
                outcome = outcome,
                reviewRequired = reviewRequired,
                payload = ToPayloadV1(payload)
            };
        }

        private static GameplayEventPayloadV1 ToPayloadV1(GameplayEventPayload payload)
        {
            if (payload == null)
            {
                return null;
            }

            string[] selected = null;
            if (payload.SelectedOptionKeys != null)
            {
                selected = new string[payload.SelectedOptionKeys.Count];
                for (int i = 0; i < payload.SelectedOptionKeys.Count; i++)
                {
                    selected[i] = payload.SelectedOptionKeys[i];
                }
            }

            string[] materials = null;
            if (payload.MaterialIds != null)
            {
                materials = new string[payload.MaterialIds.Count];
                for (int i = 0; i < payload.MaterialIds.Count; i++)
                {
                    materials[i] = payload.MaterialIds[i];
                }
            }

            return new GameplayEventPayloadV1
            {
                selectedOptionKeys = selected,
                isCorrect = payload.IsCorrect ?? false,
                hasIsCorrect = payload.IsCorrect.HasValue,
                hintShown = payload.HintShown ?? false,
                hasHintShown = payload.HintShown.HasValue,
                explanationShown = payload.ExplanationShown ?? false,
                hasExplanationShown = payload.ExplanationShown.HasValue,
                observation = payload.ObservationCode,
                prediction = payload.PredictionCode,
                materials = materials,
                investigationAction = payload.InvestigationActionId,
                result = payload.ResultCode,
                conclusion = payload.ConclusionCode,
                solutionAction = payload.SolutionActionId,
                healthAction = payload.HealthActionId,
                wellnessResult = payload.WellnessResultId,
                value = payload.Value ?? 0f,
                hasValue = payload.Value.HasValue,
                unit = payload.Unit,
                reviewReason = payload.ReviewReason
            };
        }

        private static GameplayEventPayload ToGameplayPayload(GameplayEventPayloadV1 payload)
        {
            if (payload == null)
            {
                return null;
            }

            return new GameplayEventPayload
            {
                SelectedOptionKeys = payload.selectedOptionKeys,
                IsCorrect = payload.hasIsCorrect ? payload.isCorrect : (bool?)null,
                HintShown = payload.hasHintShown ? payload.hintShown : (bool?)null,
                ExplanationShown = payload.hasExplanationShown ? payload.explanationShown : (bool?)null,
                ObservationCode = payload.observation,
                PredictionCode = payload.prediction,
                MaterialIds = payload.materials,
                InvestigationActionId = payload.investigationAction,
                ResultCode = payload.result,
                ConclusionCode = payload.conclusion,
                SolutionActionId = payload.solutionAction,
                HealthActionId = payload.healthAction,
                WellnessResultId = payload.wellnessResult,
                Value = payload.hasValue ? payload.value : (float?)null,
                Unit = payload.unit,
                ReviewReason = payload.reviewReason
            };
        }
    }
}
