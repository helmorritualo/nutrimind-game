#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Threading.Tasks;
using NutriMind.App.Composition;
using NutriMind.Core.Bootstrap;
using NutriMind.Core.Data;
using NutriMind.Core.Utilities;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NutriMind.Editor.MockRuntime
{
    /// <summary>
    /// Editor wrapper around <see cref="DevelopmentMockRuntimeController"/>.
    /// </summary>
    public static class MockRuntimeControls
    {
        private static DevelopmentMockRuntimeController Controller =>
            AppLifetime.HasInstance
                ? AppLifetime.Instance.GetComponent<DevelopmentMockRuntimeController>()
                : null;

        public static MockApiScenario SelectedScenario
        {
            get => Controller != null ? Controller.Scenario : MockApiScenario.HappyPath;
            set
            {
                if (Controller != null)
                {
                    Controller.Scenario = value;
                }
            }
        }

        public static bool IsOnline
        {
            get => Controller != null && Controller.IsOnline;
            set
            {
                if (Controller != null)
                {
                    Controller.IsOnline = value;
                }
            }
        }

        public static string DatabasePath =>
            Controller != null ? Controller.DatabasePath : "AppLifetime not ready";

        public static int GetOutboxCount() => Controller != null ? Controller.GetOutboxCount() : -1;

        public static string[] GetKnownCacheKeys() =>
            Controller != null
                ? Controller.GetKnownCacheKeys()
                : System.Array.Empty<string>();

        public static void ResetMockServer()
        {
            if (Controller == null)
            {
                return;
            }

            TaskUtilities.ForgetSafely(
                Controller.ResetMockServerAsync(),
                logPrefix: "ResetMockServer");
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
            if (Controller == null)
            {
                NutriMindLog.SqliteWarning("ResetDatabase ignored; DevelopmentMockRuntimeController missing.");
                return;
            }

            TaskUtilities.ForgetSafely(
                Controller.ResetLocalDatabaseAsync(),
                logPrefix: "ResetLocalDatabase");
        }

        public static void FullInstallationReset(bool requireConfirmation = true)
        {
#if UNITY_EDITOR
            if (requireConfirmation
                && !EditorUtility.DisplayDialog(
                    "Full Installation Reset",
                    "Delete the database, clear mock auth token, and recompose?",
                    "Full Reset",
                    "Cancel"))
            {
                return;
            }
#endif
            if (Controller == null)
            {
                NutriMindLog.RuntimeWarning("FullInstallationReset ignored; controller missing.");
                return;
            }

            TaskUtilities.ForgetSafely(
                Controller.FullInstallationResetAsync(),
                logPrefix: "FullInstallationReset");
        }
    }
}
#endif
