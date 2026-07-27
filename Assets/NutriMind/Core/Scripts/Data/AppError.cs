using System;

namespace NutriMind.Core.Data
{
    /// <summary>
    /// Stable application error. UI branches on <see cref="Code"/>, not message text.
    /// </summary>
    public sealed class AppError
    {
        public AppError(
            string code,
            string message,
            int? httpStatus = null,
            bool isNetworkError = false,
            bool isRetryable = false,
            int? retryAfterSeconds = null,
            string requestId = null)
        {
            Code = string.IsNullOrWhiteSpace(code) ? AppErrorCodes.ClientInternalError : code.Trim();
            Message = string.IsNullOrWhiteSpace(message) ? "An unexpected error occurred." : message.Trim();
            HttpStatus = httpStatus;
            IsNetworkError = isNetworkError;
            IsRetryable = isRetryable;
            RetryAfterSeconds = retryAfterSeconds;
            RequestId = string.IsNullOrWhiteSpace(requestId) ? null : requestId.Trim();
        }

        public string Code { get; }
        public string Message { get; }
        public int? HttpStatus { get; }
        public bool IsNetworkError { get; }
        public bool IsRetryable { get; }
        public int? RetryAfterSeconds { get; }
        public string RequestId { get; }

        public static AppError FromException(Exception exception, string requestId = null)
        {
            if (exception is OperationCanceledException)
            {
                throw new InvalidOperationException(
                    "Cancellation is not a user-facing AppError.",
                    exception);
            }

            return new AppError(
                AppErrorCodes.ClientInternalError,
                "An unexpected client error occurred.",
                httpStatus: null,
                isNetworkError: false,
                isRetryable: true,
                retryAfterSeconds: null,
                requestId: requestId);
        }

        public static AppError Configuration(string message)
        {
            return new AppError(
                AppErrorCodes.ClientConfigurationError,
                message,
                httpStatus: null,
                isNetworkError: false,
                isRetryable: false);
        }

        public static AppError Network(string code, string message, bool isRetryable = true, int? retryAfterSeconds = null, string requestId = null)
        {
            return new AppError(
                code,
                message,
                httpStatus: null,
                isNetworkError: true,
                isRetryable: isRetryable,
                retryAfterSeconds: retryAfterSeconds,
                requestId: requestId);
        }

        public static AppError Api(
            string code,
            string message,
            int httpStatus,
            bool isRetryable = false,
            int? retryAfterSeconds = null,
            string requestId = null)
        {
            return new AppError(
                code,
                message,
                httpStatus,
                isNetworkError: false,
                isRetryable: isRetryable,
                retryAfterSeconds: retryAfterSeconds,
                requestId: requestId);
        }
    }

    public static class AppErrorCodes
    {
        public const string AuthInvalidCredentials = "AUTH_INVALID_CREDENTIALS";
        public const string AuthTokenMissing = "AUTH_TOKEN_MISSING";
        public const string AuthTokenInvalid = "AUTH_TOKEN_INVALID";
        public const string AuthTokenRevoked = "AUTH_TOKEN_REVOKED";
        public const string AccountInactive = "ACCOUNT_INACTIVE";
        public const string RoleNotStudent = "ROLE_NOT_STUDENT";
        public const string GradeContextMismatch = "GRADE_CONTEXT_MISMATCH";
        public const string ResourceNotFound = "RESOURCE_NOT_FOUND";
        public const string MissionNotFound = "MISSION_NOT_FOUND";
        public const string AreaNotFound = "AREA_NOT_FOUND";
        public const string QuizNotFound = "QUIZ_NOT_FOUND";
        public const string AttemptNotFound = "ATTEMPT_NOT_FOUND";
        public const string CertificateNotFound = "CERTIFICATE_NOT_FOUND";
        public const string MissionLocked = "MISSION_LOCKED";
        public const string AreaLocked = "AREA_LOCKED";
        public const string InvalidProgressTransition = "INVALID_PROGRESS_TRANSITION";
        public const string MissionChallengeIncomplete = "MISSION_CHALLENGE_INCOMPLETE";
        public const string IdempotencyPayloadMismatch = "IDEMPOTENCY_PAYLOAD_MISMATCH";
        public const string StaleClientRevision = "STALE_CLIENT_REVISION";
        public const string ManifestAreaCountInvalid = "MANIFEST_AREA_COUNT_INVALID";
        public const string ManifestVersionUnsupported = "MANIFEST_VERSION_UNSUPPORTED";
        public const string ClientVersionUnsupported = "CLIENT_VERSION_UNSUPPORTED";
        public const string QuizNotAvailable = "QUIZ_NOT_AVAILABLE";
        public const string QuizNotOpen = "QUIZ_NOT_OPEN";
        public const string QuizClosed = "QUIZ_CLOSED";
        public const string AttemptLimitReached = "ATTEMPT_LIMIT_REACHED";
        public const string ResultNotVisible = "RESULT_NOT_VISIBLE";
        public const string RewardNotAvailable = "REWARD_NOT_AVAILABLE";
        public const string RewardAlreadyUsed = "REWARD_ALREADY_USED";
        public const string SyncBatchTooLarge = "SYNC_BATCH_TOO_LARGE";
        public const string SyncEventLimitExceeded = "SYNC_EVENT_LIMIT_EXCEEDED";
        public const string SyncEventTooOld = "SYNC_EVENT_TOO_OLD";
        public const string SyncEventTypeUnsupported = "SYNC_EVENT_TYPE_UNSUPPORTED";
        public const string ValidationFailed = "VALIDATION_FAILED";
        public const string RateLimited = "RATE_LIMITED";
        public const string ServerBusy = "SERVER_BUSY";
        public const string ServiceUnavailable = "SERVICE_UNAVAILABLE";
        public const string InternalError = "INTERNAL_ERROR";
        public const string NetworkOffline = "NETWORK_OFFLINE";
        public const string NetworkTimeout = "NETWORK_TIMEOUT";
        public const string ClientInternalError = "CLIENT_INTERNAL_ERROR";
        public const string ClientConfigurationError = "CLIENT_CONFIGURATION_ERROR";
        public const string FixtureLoadFailed = "FIXTURE_LOAD_FAILED";
    }
}
