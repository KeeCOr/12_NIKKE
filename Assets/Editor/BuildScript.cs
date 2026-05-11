using UnityEditor;
using UnityEngine;

public static class BuildScript {
    [MenuItem("SquadVsMonster/Build Windows EXE")]
    public static void BuildWindows() {
        string[] scenes = {
            "Assets/Scenes/Game.unity",
            "Assets/Scenes/Result.unity",
        };

        string outputPath = "Build/SquadVsMonster.exe";

        var options = new BuildPlayerOptions {
            scenes       = scenes,
            locationPathName = outputPath,
            target       = BuildTarget.StandaloneWindows64,
            options      = BuildOptions.None,
        };

        var report = BuildPipeline.BuildPlayer(options);
        var summary = report.summary;

        if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded) {
            Debug.Log($"[Build] SUCCESS — {summary.totalSize / 1024 / 1024} MB  →  {outputPath}");
        } else {
            Debug.LogError($"[Build] FAILED: {summary.result}  errors={summary.totalErrors}");
        }
    }

    // Called from command line: -executeMethod BuildScript.BuildWindowsCLI
    public static void BuildWindowsCLI() {
        BuildWindows();
    }
}
