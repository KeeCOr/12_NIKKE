using System.IO;
using UnityEditor;
using UnityEngine;

public static class BuildScript {
    [MenuItem("SquadVsMonster/Build Windows EXE")]
    public static void BuildWindows() {
        string[] scenes = {
            "Assets/Scenes/Game.unity",
        };

        string outputPath = "Build/SquadVsMonster.exe";

        string outputDir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(outputDir) && Directory.Exists(outputDir)) {
            ClearReadOnlyAttributes(outputDir);
            Directory.Delete(outputDir, true);
        }

        var options = new BuildPlayerOptions {
            scenes       = scenes,
            locationPathName = outputPath,
            target       = BuildTarget.StandaloneWindows64,
            options      = BuildOptions.CleanBuildCache,
        };

        var report = BuildPipeline.BuildPlayer(options);
        var summary = report.summary;

        if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded) {
            Debug.Log($"[Build] SUCCESS — {summary.totalSize / 1024 / 1024} MB  →  {outputPath}");
        } else {
            Debug.LogError($"[Build] FAILED: {summary.result}  errors={summary.totalErrors}");
        }
    }


    private static void ClearReadOnlyAttributes(string directory) {
        foreach (var file in Directory.GetFiles(directory, "*", SearchOption.AllDirectories)) {
            File.SetAttributes(file, FileAttributes.Normal);
        }
        foreach (var dir in Directory.GetDirectories(directory, "*", SearchOption.AllDirectories)) {
            File.SetAttributes(dir, FileAttributes.Normal);
        }
        File.SetAttributes(directory, FileAttributes.Normal);
    }
    // Called from command line: -executeMethod BuildScript.BuildWindowsCLI
    public static void BuildWindowsCLI() {
        BuildWindows();
    }
}
