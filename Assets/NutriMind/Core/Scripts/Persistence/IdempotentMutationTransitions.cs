using System;
using NutriMind.Core.Data;

namespace NutriMind.Core.Persistence
{
    /// <summary>
    /// Centralized idempotent request transition helper.
    /// Preserves identity fields and only mutates state/result/updated timestamps.
    /// </summary>
    public static class IdempotentMutationTransitions
    {
        public static AppResult<IdempotentRequestRecord> CreatePending(
            string requestUuid,
            string operation,
            string studentId,
            string entityKey,
            string normalizedPayloadJson,
            string createdUtc)
        {
            if (string.IsNullOrWhiteSpace(requestUuid)
                || string.IsNullOrWhiteSpace(operation)
                || string.IsNullOrWhiteSpace(studentId)
                || string.IsNullOrWhiteSpace(entityKey)
                || string.IsNullOrWhiteSpace(normalizedPayloadJson)
                || string.IsNullOrWhiteSpace(createdUtc))
            {
                return AppResult<IdempotentRequestRecord>.Failure(
                    AppErrorCodes.ValidationFailed,
                    "Idempotent pending create requires full identity and payload.");
            }

            return AppResult<IdempotentRequestRecord>.Success(new IdempotentRequestRecord
            {
                RequestUuid = requestUuid.Trim(),
                Operation = operation.Trim(),
                StudentId = studentId.Trim(),
                EntityKey = entityKey.Trim(),
                NormalizedPayloadJson = normalizedPayloadJson,
                State = IdempotentRequestStates.Pending,
                ResultJson = null,
                CreatedUtc = createdUtc,
                UpdatedUtc = createdUtc
            });
        }

        public static AppResult Transition(
            IIdempotentRequestRepository repository,
            IdempotentRequestRecord record,
            string nextState,
            string resultJson,
            string updatedUtc)
        {
            if (repository == null)
            {
                return AppResult.Failure(
                    AppErrorCodes.ClientConfigurationError,
                    "Idempotent request repository is required.");
            }

            if (record == null)
            {
                return AppResult.Failure(
                    AppErrorCodes.ValidationFailed,
                    "Idempotent request record is required.");
            }

            AppResult validate = ValidateIdentity(record);
            if (validate.IsFailure)
            {
                return validate;
            }

            if (!IsLegalTransition(record.State, nextState))
            {
                return AppResult.Failure(
                    AppErrorCodes.ValidationFailed,
                    "Illegal idempotent transition from '"
                    + (record.State ?? string.Empty)
                    + "' to '"
                    + (nextState ?? string.Empty)
                    + "'.");
            }

            string previousState = record.State;
            string previousResult = record.ResultJson;
            string previousUpdated = record.UpdatedUtc;

            record.State = nextState;
            record.ResultJson = resultJson;
            record.UpdatedUtc = string.IsNullOrWhiteSpace(updatedUtc)
                ? DateTimeOffset.UtcNow.ToString("o")
                : updatedUtc;

            AppResult write = repository.Upsert(record);
            if (write.IsFailure)
            {
                record.State = previousState;
                record.ResultJson = previousResult;
                record.UpdatedUtc = previousUpdated;
                return write;
            }

            return AppResult.Success();
        }

        public static AppResult ValidateImmutableIdentity(
            IdempotentRequestRecord existing,
            string operation,
            string studentId,
            string entityKey,
            string normalizedPayloadJson)
        {
            if (existing == null)
            {
                return AppResult.Failure(
                    AppErrorCodes.ValidationFailed,
                    "Existing idempotent record is required.");
            }

            AppResult identity = ValidateIdentity(existing);
            if (identity.IsFailure)
            {
                return identity;
            }

            if (!string.Equals(existing.Operation, operation, StringComparison.Ordinal)
                || !string.Equals(existing.StudentId, studentId, StringComparison.Ordinal)
                || !string.Equals(existing.EntityKey, entityKey, StringComparison.Ordinal))
            {
                return AppResult.Failure(
                    AppErrorCodes.ValidationFailed,
                    "Idempotent identity mismatch for existing request UUID.");
            }

            if (!string.Equals(
                    existing.NormalizedPayloadJson,
                    normalizedPayloadJson,
                    StringComparison.Ordinal))
            {
                return AppResult.Failure(
                    AppErrorCodes.ValidationFailed,
                    "Cannot reuse request UUID with a changed normalized payload.");
            }

            return AppResult.Success();
        }

        public static bool IsLegalTransition(string currentState, string nextState)
        {
            if (string.IsNullOrWhiteSpace(nextState))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(currentState)
                && nextState == IdempotentRequestStates.Pending)
            {
                return true;
            }

            if (currentState == IdempotentRequestStates.Pending
                && nextState == IdempotentRequestStates.Sending)
            {
                return true;
            }

            if (currentState == IdempotentRequestStates.Sending)
            {
                return nextState == IdempotentRequestStates.Uncertain
                    || nextState == IdempotentRequestStates.Completed
                    || nextState == IdempotentRequestStates.Rejected;
            }

            if (currentState == IdempotentRequestStates.Uncertain
                && nextState == IdempotentRequestStates.Sending)
            {
                return true;
            }

            return false;
        }

        public static AppResult ValidateIdentity(IdempotentRequestRecord record)
        {
            if (record == null
                || string.IsNullOrWhiteSpace(record.RequestUuid)
                || string.IsNullOrWhiteSpace(record.Operation)
                || string.IsNullOrWhiteSpace(record.StudentId)
                || string.IsNullOrWhiteSpace(record.EntityKey)
                || string.IsNullOrWhiteSpace(record.NormalizedPayloadJson)
                || string.IsNullOrWhiteSpace(record.CreatedUtc))
            {
                return AppResult.Failure(
                    AppErrorCodes.ValidationFailed,
                    "Idempotent record is missing required identity fields.");
            }

            return AppResult.Success();
        }
    }
}
