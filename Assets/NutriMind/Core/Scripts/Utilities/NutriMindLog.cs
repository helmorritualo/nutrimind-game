using System;
using System.Text;
using UnityEngine;

namespace NutriMind.Core.Utilities
{
    public static class NutriMindLog
    {
        public const string RuntimePrefix = "[NutriMind.Runtime]";
        public const string MockGatewayPrefix = "[NutriMind.MockGateway]";
        public const string SqlitePrefix = "[NutriMind.SQLite]";
        public const string SyncPrefix = "[NutriMind.Sync]";
        public const string StartupPrefix = "[NutriMind.Startup]";
        public const string AuthPrefix = "[NutriMind.Auth]";

        public static void Runtime(string message) => Info(RuntimePrefix, message);
        public static void MockGateway(string message) => Info(MockGatewayPrefix, message);
        public static void Sqlite(string message) => Info(SqlitePrefix, message);
        public static void Sync(string message) => Info(SyncPrefix, message);
        public static void Startup(string message) => Info(StartupPrefix, message);
        public static void Auth(string message) => Info(AuthPrefix, message);

        public static void RuntimeWarning(string message) => Warning(RuntimePrefix, message);
        public static void MockGatewayWarning(string message) => Warning(MockGatewayPrefix, message);
        public static void SqliteWarning(string message) => Warning(SqlitePrefix, message);
        public static void SyncWarning(string message) => Warning(SyncPrefix, message);
        public static void StartupWarning(string message) => Warning(StartupPrefix, message);
        public static void AuthWarning(string message) => Warning(AuthPrefix, message);

        public static void RuntimeError(string message) => Error(RuntimePrefix, message);
        public static void MockGatewayError(string message) => Error(MockGatewayPrefix, message);
        public static void SqliteError(string message) => Error(SqlitePrefix, message);
        public static void SyncError(string message) => Error(SyncPrefix, message);
        public static void StartupError(string message) => Error(StartupPrefix, message);
        public static void AuthError(string message) => Error(AuthPrefix, message);

        public static string MaskLrn(string lrn)
        {
            if (string.IsNullOrWhiteSpace(lrn))
            {
                return string.Empty;
            }

            string trimmed = lrn.Trim();
            if (trimmed.Length <= 4)
            {
                return new string('*', trimmed.Length);
            }

            var builder = new StringBuilder(trimmed.Length);
            builder.Append(trimmed, 0, 2);
            builder.Append('*', trimmed.Length - 4);
            builder.Append(trimmed, trimmed.Length - 2, 2);
            return builder.ToString();
        }

        public static void AssertNoSecrets(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            string lower = message.ToLowerInvariant();
            if (lower.Contains("authorization")
                || lower.Contains("bearer ")
                || lower.Contains("\"pin\"")
                || lower.Contains("pin=")
                || lower.Contains("answer_key")
                || lower.Contains("answerkey"))
            {
                throw new InvalidOperationException("Attempted to log a forbidden secret value.");
            }
        }

        private static void Info(string prefix, string message)
        {
            AssertNoSecrets(message);
            Debug.Log(prefix + " " + message);
        }

        private static void Warning(string prefix, string message)
        {
            AssertNoSecrets(message);
            Debug.LogWarning(prefix + " " + message);
        }

        private static void Error(string prefix, string message)
        {
            AssertNoSecrets(message);
            Debug.LogError(prefix + " " + message);
        }
    }
}
