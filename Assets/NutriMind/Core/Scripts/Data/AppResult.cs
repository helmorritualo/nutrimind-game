using System;

namespace NutriMind.Core.Data
{
    /// <summary>
    /// Non-generic operation result. Expected API failures use <see cref="Error"/>, not exceptions.
    /// </summary>
    public readonly struct AppResult
    {
        private AppResult(bool isSuccess, AppError error)
        {
            IsSuccess = isSuccess;
            Error = error;
        }

        public bool IsSuccess { get; }
        public bool IsFailure => !IsSuccess;
        public AppError Error { get; }

        public static AppResult Success() => new AppResult(true, null);

        public static AppResult Failure(AppError error)
        {
            if (error == null)
            {
                throw new ArgumentNullException(nameof(error));
            }

            return new AppResult(false, error);
        }

        public static AppResult Failure(
            string code,
            string message,
            int? httpStatus = null,
            bool isNetworkError = false,
            bool isRetryable = false,
            int? retryAfterSeconds = null,
            string requestId = null)
        {
            return Failure(new AppError(
                code,
                message,
                httpStatus,
                isNetworkError,
                isRetryable,
                retryAfterSeconds,
                requestId));
        }
    }

    /// <summary>
    /// Typed operation result. Expected API failures use <see cref="Error"/>, not exceptions.
    /// </summary>
    public readonly struct AppResult<T>
    {
        private readonly T _value;

        private AppResult(bool isSuccess, T value, AppError error)
        {
            IsSuccess = isSuccess;
            _value = value;
            Error = error;
        }

        public bool IsSuccess { get; }
        public bool IsFailure => !IsSuccess;
        public AppError Error { get; }

        public T Value
        {
            get
            {
                if (!IsSuccess)
                {
                    throw new InvalidOperationException(
                        Error != null
                            ? $"Cannot read Value from failed AppResult ({Error.Code})."
                            : "Cannot read Value from failed AppResult.");
                }

                return _value;
            }
        }

        public bool TryGetValue(out T value)
        {
            if (IsSuccess)
            {
                value = _value;
                return true;
            }

            value = default;
            return false;
        }

        public static AppResult<T> Success(T value) => new AppResult<T>(true, value, null);

        public static AppResult<T> Failure(AppError error)
        {
            if (error == null)
            {
                throw new ArgumentNullException(nameof(error));
            }

            return new AppResult<T>(false, default, error);
        }

        public static AppResult<T> Failure(
            string code,
            string message,
            int? httpStatus = null,
            bool isNetworkError = false,
            bool isRetryable = false,
            int? retryAfterSeconds = null,
            string requestId = null)
        {
            return Failure(new AppError(
                code,
                message,
                httpStatus,
                isNetworkError,
                isRetryable,
                retryAfterSeconds,
                requestId));
        }

        public static implicit operator AppResult(AppResult<T> result)
        {
            return result.IsSuccess
                ? AppResult.Success()
                : AppResult.Failure(result.Error);
        }
    }
}
