using System.Diagnostics;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using Debug = UnityEngine.Debug;

namespace Mrotondo.ComputePlayground.Editor
{
    /// <summary>
    /// A build entry point for timing runs, so "fast enough" can be a number.
    /// <code>
    /// Unity.exe -quit -batchmode -projectPath . -executeMethod Mrotondo.ComputePlayground.Editor.HeadlessBuild.Run -logFile -
    /// </code>
    /// </summary>
    public static class HeadlessBuild
    {
        [MenuItem("Tools/Minimal Project/Timed Build", priority = 40)]
        public static void Run()
        {
            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                Debug.LogError("Timed build: no enabled scenes in EditorBuildSettings.");
                EditorApplication.Exit(1);
                return;
            }

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = "build/Experiment.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            };

            var stopwatch = Stopwatch.StartNew();
            BuildReport report = BuildPipeline.BuildPlayer(options);
            stopwatch.Stop();

            BuildSummary summary = report.summary;
            Debug.Log($"Timed build: {summary.result} in {stopwatch.Elapsed.TotalSeconds:F1}s, " +
                      $"{summary.totalSize / 1024 / 1024}MB, {summary.totalErrors} error(s).");

            // Slowest build steps first; this is where to look when a build feels heavy.
            foreach (BuildStep step in report.steps.OrderByDescending(step => step.duration).Take(10))
                Debug.Log($"  {step.duration.TotalSeconds,6:F1}s  {step.name}");

            if (summary.result != BuildResult.Succeeded)
                EditorApplication.Exit(1);
        }
    }
}
