using System;
using NutriMind.Core.Data;
using NUnit.Framework;

namespace NutriMind.Tests.EditMode
{
    public sealed class AppResultTests
    {
        [Test]
        public void AppResult_Success_HasNoError()
        {
            AppResult result = AppResult.Success();

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.IsFailure, Is.False);
            Assert.That(result.Error, Is.Null);
        }

        [Test]
        public void AppResult_Failure_PreservesErrorCode()
        {
            AppResult result = AppResult.Failure(
                AppErrorCodes.ValidationFailed,
                "Invalid input.",
                httpStatus: 400,
                isRetryable: false);

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error, Is.Not.Null);
            Assert.That(result.Error.Code, Is.EqualTo(AppErrorCodes.ValidationFailed));
            Assert.That(result.Error.Message, Is.EqualTo("Invalid input."));
            Assert.That(result.Error.HttpStatus, Is.EqualTo(400));
        }

        [Test]
        public void AppResult_Failure_NullError_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => AppResult.Failure(null));
        }

        [Test]
        public void AppResultT_Success_ExposesValue()
        {
            AppResult<int> result = AppResult<int>.Success(42);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.EqualTo(42));
            Assert.That(result.TryGetValue(out int value), Is.True);
            Assert.That(value, Is.EqualTo(42));
        }

        [Test]
        public void AppResultT_Failure_ValueThrows_TryGetValueFalse()
        {
            AppResult<string> result = AppResult<string>.Failure(
                AppErrorCodes.NetworkOffline,
                "Device is offline.",
                isNetworkError: true,
                isRetryable: true);

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo(AppErrorCodes.NetworkOffline));
            Assert.That(result.Error.IsNetworkError, Is.True);
            Assert.Throws<InvalidOperationException>(() => _ = result.Value);
            Assert.That(result.TryGetValue(out string _), Is.False);
        }

        [Test]
        public void AppResultT_ImplicitConversion_ToNonGeneric()
        {
            AppResult typed = AppResult<string>.Success("ok");
            Assert.That(typed.IsSuccess, Is.True);

            AppResult failed = AppResult<string>.Failure(AppErrorCodes.InternalError, "boom");
            Assert.That(failed.IsFailure, Is.True);
            Assert.That(failed.Error.Code, Is.EqualTo(AppErrorCodes.InternalError));
        }

        [Test]
        public void AppError_FromException_MapsClientInternal()
        {
            AppError error = AppError.FromException(new InvalidOperationException("x"));
            Assert.That(error.Code, Is.EqualTo(AppErrorCodes.ClientInternalError));
            Assert.That(error.IsRetryable, Is.True);
        }

        [Test]
        public void AppError_FromException_Cancellation_Throws()
        {
            Assert.Throws<InvalidOperationException>(
                () => AppError.FromException(new OperationCanceledException()));
        }
    }
}
