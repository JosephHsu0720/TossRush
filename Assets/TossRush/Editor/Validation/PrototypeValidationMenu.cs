using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace TossRush.Editor.Validation
{
    [InitializeOnLoad]
    public static class PrototypeValidationMenu
    {
        const string PendingKey = "TossRush.PrototypeChecks.Pending";
        const string ApiKey = "TossRush.PrototypeChecks.Api";
        const string Output = "Docs/Validation/StreetPlayMode";
        static TestRunnerApi api;
        static ResultWriter writer;

        static PrototypeValidationMenu()
        {
            // Reattach the explicitly requested run's result writer after entering/exiting Play Mode.
            EditorApplication.delayCall += () => { if (SessionState.GetBool(PendingKey, false)) Attach(); };
        }

        [MenuItem("TossRush/Run Prototype Checks")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || SessionState.GetBool(PendingKey, false)) return;
            Directory.CreateDirectory("Docs/Validation");
            File.WriteAllText(Output + ".json", "{\"status\":\"running\"}");
            SessionState.SetBool(PendingKey, true);
            Attach();
            try {
                api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.PlayMode, assemblyNames = new[] { "TossRush.PlayModeTests" } }));
            }
            catch (Exception exception) {
                SessionState.SetBool(PendingKey, false);
                File.WriteAllText(Output + ".json", JsonUtility.ToJson(new Summary { status = "error", message = exception.Message }, true));
                Cleanup();
                throw;
            }
        }

        [MenuItem("TossRush/Run Prototype Checks", true)]
        static bool CanRun() => !EditorApplication.isPlayingOrWillChangePlaymode && !SessionState.GetBool(PendingKey, false);

        static void Attach()
        {
            if (api && writer != null) return;
            foreach (var candidate in Resources.FindObjectsOfTypeAll<TestRunnerApi>())
                if (candidate.name == ApiKey) { api = candidate; break; }
            if (!api) {
                api = ScriptableObject.CreateInstance<TestRunnerApi>();
                api.name = ApiKey;
                api.hideFlags = HideFlags.HideAndDontSave;
            }
            writer = new ResultWriter();
            api.RegisterCallbacks(writer);
        }

        static void Cleanup()
        {
            if (api) {
                if (writer != null) api.UnregisterCallbacks(writer);
                UnityEngine.Object.DestroyImmediate(api);
            }
            api = null; writer = null;
        }

        [Serializable]
        sealed class Failure { public string test, message, stackTrace; }
        [Serializable]
        sealed class Summary
        {
            public string status, finishedUtc, message;
            public int passed, failed, skipped;
            public double duration;
            public Failure[] failures;
        }

        sealed class ResultWriter : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }
            public void RunFinished(ITestResultAdaptor result)
            {
                if (!SessionState.GetBool(PendingKey, false)) return;
                var failures = new List<Failure>();
                CollectFailures(result, failures);
                var summary = new Summary {
                    status = result.PassCount + result.FailCount + result.SkipCount == 0 ? "no_tests" : result.ResultState,
                    finishedUtc = DateTime.UtcNow.ToString("o"),
                    passed = result.PassCount, failed = result.FailCount, skipped = result.SkipCount,
                    duration = result.Duration, failures = failures.ToArray()
                };
                TestRunnerApi.SaveResultToFile(result, Output + ".xml");
                File.WriteAllText(Output + ".json", JsonUtility.ToJson(summary, true));
                SessionState.SetBool(PendingKey, false);
                Debug.Log("TossRush prototype checks: " + summary.passed + " passed, " + summary.failed + " failed. Report: " + Output + ".json");
                EditorApplication.delayCall += Cleanup;
            }

            static void CollectFailures(ITestResultAdaptor result, List<Failure> failures)
            {
                if (result.HasChildren) foreach (var child in result.Children) CollectFailures(child, failures);
                else if (result.FailCount > 0) failures.Add(new Failure { test = result.FullName, message = result.Message, stackTrace = result.StackTrace });
            }
        }
    }
}
