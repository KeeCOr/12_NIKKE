#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

public static class CommandLineEditModeTestRunner {
    private const string ResultPath = "C:/tmp/12_NIKKE_EditMode_api_result.txt";

    public static void Run() {
        File.WriteAllText(ResultPath, "STARTED\n");
        var api = ScriptableObject.CreateInstance<TestRunnerApi>();
        api.RegisterCallbacks(new ResultCallbacks());
        api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode }));
    }

    private sealed class ResultCallbacks : ICallbacks {
        public void RunStarted(ITestAdaptor testsToRun) { }
        public void TestStarted(ITestAdaptor test) { }
        public void TestFinished(ITestResultAdaptor result) { }

        public void RunFinished(ITestResultAdaptor result) {
            using (var writer = new StreamWriter(ResultPath, false)) {
                writer.WriteLine($"PASS={result.PassCount}");
                writer.WriteLine($"FAIL={result.FailCount}");
                writer.WriteLine($"SKIP={result.SkipCount}");
                writer.WriteLine($"INCONCLUSIVE={result.InconclusiveCount}");
                WriteFailures(writer, result, "");
            }
            EditorApplication.Exit(result.FailCount > 0 ? 2 : 0);
        }

        private static void WriteFailures(StreamWriter writer, ITestResultAdaptor result, string indent) {
            if (result.FailCount > 0 && !result.HasChildren) {
                writer.WriteLine($"{indent}FAILED: {result.FullName}");
                writer.WriteLine(result.Message);
            }

            foreach (var child in result.Children)
                WriteFailures(writer, child, indent + "  ");
        }
    }
}
#endif
