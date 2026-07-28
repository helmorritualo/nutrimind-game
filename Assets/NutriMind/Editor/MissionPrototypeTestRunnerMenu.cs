using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using TestMode = UnityEditor.TestTools.TestRunner.Api.TestMode;

namespace NutriMind.Editor
{
    public static class MissionPrototypeTestRunnerMenu
    {
        [MenuItem("NutriMind/Gameplay/Mission 1/Run Areas 1-2 EditMode Tests")]
        public static void RunEditModeTests()
        {
            Run(TestMode.EditMode, new[]
            {
                "NutriMind.Tests.EditMode.GameplayRuntime",
                "NutriMind.Tests.EditMode.GameplayUI"
            });
        }

        [MenuItem("NutriMind/Gameplay/Mission 1/Run Areas 1-2 PlayMode Tests")]
        public static void RunPlayModeTests()
        {
            Run(TestMode.PlayMode, new[]
            {
                "NutriMind.Tests.PlayMode.GameplayRuntime"
            });
        }

        private static void Run(TestMode mode, string[] groupNames)
        {
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            var callbacks = new Callbacks(mode);
            api.RegisterCallbacks(callbacks);
            api.Execute(new ExecutionSettings(new Filter
            {
                testMode = mode,
                groupNames = groupNames
            }));
            Debug.Log("[MissionPrototypeTestRunnerMenu] Started " + mode + " tests for: " + string.Join(", ", groupNames));
        }

        private sealed class Callbacks : ICallbacks
        {
            private readonly TestMode _mode;
            private readonly StringBuilder _failures = new StringBuilder();

            public Callbacks(TestMode mode)
            {
                _mode = mode;
            }

            public void RunStarted(ITestAdaptor testsToRun)
            {
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                string summary = "[MissionPrototypeTestRunnerMenu] " + _mode
                    + " Passed=" + result.PassCount
                    + " Failed=" + result.FailCount
                    + " Skipped=" + result.SkipCount;
                if (result.FailCount > 0)
                {
                    Debug.LogError(summary + "\n" + _failures);
                }
                else
                {
                    Debug.Log(summary);
                }
            }

            public void TestStarted(ITestAdaptor test)
            {
            }

            public void TestFinished(ITestResultAdaptor result)
            {
                if (result.TestStatus == TestStatus.Failed)
                {
                    _failures.AppendLine("FAIL: " + result.FullName);
                    _failures.AppendLine(result.Message);
                }
            }
        }
    }
}
