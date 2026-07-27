using NutriMind.Core.Data;

namespace NutriMind.App.Presentation
{
    /// <summary>
    /// Maps stable API/client error codes to learner-facing copy.
    /// Never surfaces raw exception messages.
    /// </summary>
    public static class LearnerFacingErrorMapper
    {
        public static string Map(AppError error)
        {
            if (error == null)
            {
                return "Something went wrong. Please try again.";
            }

            return Map(error.Code, error.RetryAfterSeconds);
        }

        public static string Map(string code, int? retryAfterSeconds = null)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return "Something went wrong. Please try again.";
            }

            switch (code.Trim())
            {
                case AppErrorCodes.AuthInvalidCredentials:
                    return "Those sign-in details did not match. Check your LRN and PIN, then try again.";
                case AppErrorCodes.AuthTokenInvalid:
                case AppErrorCodes.AuthTokenRevoked:
                case AppErrorCodes.AuthTokenMissing:
                    return "Your session ended. Please sign in again.";
                case AppErrorCodes.AccountInactive:
                    return "This learner account is inactive. Ask your teacher for help.";
                case AppErrorCodes.RoleNotStudent:
                    return "This account cannot use the student app.";
                case AppErrorCodes.GradeContextMismatch:
                    return "This content does not match your grade. Ask your teacher for help.";
                case AppErrorCodes.MissionLocked:
                    return "This mission is locked right now.";
                case AppErrorCodes.AreaLocked:
                    return "This area is locked until earlier steps are finished.";
                case AppErrorCodes.QuizNotAvailable:
                    return "This quiz is not available right now.";
                case AppErrorCodes.QuizNotOpen:
                    return "This quiz is not open yet.";
                case AppErrorCodes.QuizClosed:
                    return "This quiz is closed.";
                case AppErrorCodes.AttemptLimitReached:
                    return "You have used all attempts for this quiz.";
                case AppErrorCodes.ResultNotVisible:
                    return "Results for this quiz are not visible yet.";
                case AppErrorCodes.RewardNotAvailable:
                    return "This reward is not available right now.";
                case AppErrorCodes.RewardAlreadyUsed:
                    return "This reward was already used.";
                case AppErrorCodes.IdempotencyPayloadMismatch:
                    return "That request does not match a pending action. Please start again.";
                case AppErrorCodes.RateLimited:
                    return retryAfterSeconds.HasValue && retryAfterSeconds.Value > 0
                        ? "Too many tries. Wait " + retryAfterSeconds.Value + " seconds, then try again."
                        : "Too many tries. Please wait a moment and try again.";
                case AppErrorCodes.ServerBusy:
                    return "The server is busy. Please try again in a moment.";
                case AppErrorCodes.ServiceUnavailable:
                    return "The service is temporarily unavailable. Please try again later.";
                case AppErrorCodes.ValidationFailed:
                    return "Please check your answers and try again.";
                case AppErrorCodes.NetworkOffline:
                    return "You are offline. Connect to continue this action.";
                case AppErrorCodes.NetworkTimeout:
                    return "The request timed out. You can retry safely.";
                case AppErrorCodes.SyncInProgress:
                    return "Sync is already running.";
                default:
                    return "Something went wrong. Please try again.";
            }
        }
    }
}
