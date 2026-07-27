using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace NutriMind.Editor.MockRuntime
{
    /// <summary>
    /// Editor-only helper to run NutriMind EditMode tests and log a compact summary.
    /// </summary>
    public static class NutriMindEditModeTestRunner
    {
        private static TestRunnerApi _api;
        private static SummaryCallbacks _callbacks;

        [MenuItem("NutriMind/Mock Runtime/Run EditMode Foundation Tests")]
        public static void RunEditModeFoundationTests()
        {
            if (_api != null)
            {
                Debug.LogWarning("[NutriMind.Runtime] EditMode test run already in progress.");
                return;
            }

            _api = ScriptableObject.CreateInstance<TestRunnerApi>();
            _callbacks = new SummaryCallbacks();
            _api.RegisterCallbacks(_callbacks);
            _api.Execute(new ExecutionSettings(new Filter
            {
                testMode = TestMode.EditMode,
                assemblyNames = new[] { "NutriMind.Tests.EditMode" }
            }));
            EditorApplication.update += Poll;
            Debug.Log("[NutriMind.Runtime] EditMode foundation tests started.");
        }

        private static void Poll()
        {
            if (_callbacks == null || !_callbacks.Finished)
            {
                return;
            }

            EditorApplication.update -= Poll;
            if (_api != null && _callbacks != null)
            {
                _api.UnregisterCallbacks(_callbacks);
            }

            string summary =
                "Passed="
                + _callbacks.Passed
                + " Failed="
                + _callbacks.Failed
                + " Skipped="
                + _callbacks.Skipped
                + " Status="
                + _callbacks.Status;

            Debug.Log("[NutriMind.Runtime] EditMode results " + summary);

            string resultsPath = System.IO.Path.Combine(
                UnityEngine.Application.dataPath,
                "..",
                "Temp",
                "NutriMindEditModeResults.txt");
            try
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(resultsPath));
                System.IO.File.WriteAllText(
                    resultsPath,
                    summary
                    + System.Environment.NewLine
                    + (_callbacks.Failures ?? string.Empty));
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning(
                    "[NutriMind.Runtime] Could not write EditMode results file: "
                    + exception.GetType().Name);
            }

            if (!string.IsNullOrEmpty(_callbacks.Failures))
            {
                Debug.LogError("[NutriMind.Runtime] EditMode failures:\n" + _callbacks.Failures);
            }

            _api = null;
            _callbacks = null;
        }

        private sealed class SummaryCallbacks : ICallbacks
        {
            public bool Finished;
            public int Passed;
            public int Failed;
            public int Skipped;
            public string Status = string.Empty;
            public string Failures = string.Empty;

            public void RunStarted(ITestAdaptor testsToRun)
            {
            }

            public void TestStarted(ITestAdaptor test)
            {
            }

            public void TestFinished(ITestResultAdaptor result)
            {
                if (result.HasChildren)
                {
                    return;
                }

                switch (result.TestStatus)
                {
                    case TestStatus.Passed:
                        Passed++;
                        break;
                    case TestStatus.Failed:
                        Failed++;
                        Failures += result.FullName + " :: " + result.Message + "\n\n";
                        break;
                    case TestStatus.Skipped:
                        Skipped++;
                        break;
                }
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                Status = result.TestStatus.ToString();
                Finished = true;
            }
        }
    }
}
