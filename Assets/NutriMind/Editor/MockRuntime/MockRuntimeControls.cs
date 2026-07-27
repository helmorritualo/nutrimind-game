#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.IO;
using NutriMind.Core.Bootstrap;
using NutriMind.Core.Data;
using NutriMind.Core.Networking;
using NutriMind.Core.Persistence;
using NutriMind.Core.Utilities;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NutriMind.Editor.MockRuntime
{
    /// <summary>
    /// Editor / Development-build mock runtime controls. Must not ship in production UI.
    /// </summary>
    public static class MockRuntimeControls
    {
        public static MockApiScenario SelectedScenario
        {
            get
            {
                if (AppLifetime.HasInstance && AppLifetime.Instance.RuntimeOptions != null)
                {
                    return AppLifetime.Instance.RuntimeOptions.MockScenario;
                }

                return MockApiScenario.HappyPath;
            }
            set
            {
                if (!AppLifetime.HasInstance || AppLifetime.Instance.RuntimeOptions == null)
                {
                    return;
                }

                AppLifetime.Instance.RuntimeOptions.MockScenario = value;
                NutriMindLog.Runtime("Mock scenario set to " + value + ".");
            }
        }

        public static bool IsOnline
        {
            get
            {
                return AppLifetime.HasInstance
                       && AppLifetime.Instance.Connectivity != null
                       && AppLifetime.Instance.Connectivity.IsOnline;
            }
            set
            {
                if (!AppLifetime.HasInstance || AppLifetime.Instance.Connectivity == null)
                {
                    return;
                }

                AppLifetime.Instance.Connectivity.SetState(
                    value ? ConnectivityState.Online : ConnectivityState.Offline);
                NutriMindLog.Runtime(value ? "Connectivity set Online." : "Connectivity set Offline.");
            }
        }

        public static string DatabasePath =>
            AppLifetime.HasInstance && AppLifetime.Instance.Database != null
                ? AppLifetime.Instance.Database.DatabaseFilePath
                : NutriMindDatabase.GetDefaultDatabasePath();

        public static int GetOutboxCount()
        {
            if (!AppLifetime.HasInstance || AppLifetime.Instance.OutboxRepository == null)
            {
                return -1;
            }

            AppResult<int> count = AppLifetime.Instance.OutboxRepository.CountByStates(
                OutboxEventState.Pending,
                OutboxEventState.Sending,
                OutboxEventState.Deferred);
            return count.IsSuccess ? count.Value : -1;
        }

        public static string[] GetKnownCacheKeys()
        {
            return new[]
            {
                ResourceCacheKeys.Bootstrap,
                ResourceCacheKeys.Profile,
                ResourceCacheKeys.Subjects,
                ResourceCacheKeys.ProgressSummary,
                ResourceCacheKeys.Rewards,
                ResourceCacheKeys.Certificates,
                ResourceCacheKeys.Announcements
            };
        }

        public static void ResetDatabase(bool requireConfirmation = true)
        {
#if UNITY_EDITOR
            if (requireConfirmation
                && !EditorUtility.DisplayDialog(
                    "Reset NutriMind Database",
                    "Delete the local SQLite database (progress + outbox)? This cannot be undone.",
                    "Reset Database",
                    "Cancel"))
            {
                return;
            }
#endif
            string path = DatabasePath;
            try
            {
                if (AppLifetime.HasInstance)
                {
                    AppLifetime.Instance.Composition?.Dispose();
                }

                TryDelete(path);
                TryDelete(path + "-shm");
                TryDelete(path + "-wal");
                NutriMindLog.Sqlite("Database reset at " + path);
            }
            catch (Exception exception)
            {
                NutriMindLog.SqliteError("Database reset failed: " + exception.GetType().Name);
            }
        }

        public static void FullInstallationReset(bool requireConfirmation = true)
        {
#if UNITY_EDITOR
            if (requireConfirmation
                && !EditorUtility.DisplayDialog(
                    "Full Installation Reset",
                    "Delete the database and clear in-memory auth/session mock state?",
                    "Full Reset",
                    "Cancel"))
            {
                return;
            }
#endif
            ResetDatabase(requireConfirmation: false);
            if (AppLifetime.HasInstance)
            {
                _ = AppLifetime.Instance.ClearAuthenticationAsync();
                if (AppLifetime.Instance.InstallationRepository != null)
                {
                    AppLifetime.Instance.InstallationRepository.RegenerateDeviceIdForFullInstallReset();
                }
            }

            NutriMindLog.Runtime("Full installation reset requested.");
        }

        private static void TryDelete(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
#endif
