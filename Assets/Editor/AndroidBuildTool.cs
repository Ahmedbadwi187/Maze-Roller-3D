using UnityEditor;
using UnityEngine;

namespace RollAndEscape.EditorTools
{
    /// <summary>
    /// Produces a debug/development APK for testing on a real Android device - not a signed
    /// release build (no keystore configured), just enough to sideload and try the game.
    /// </summary>
    public static class AndroidBuildTool
    {
        private const string OutputPath = "Builds/Android/RollAndEscape.apk";

        [MenuItem("Roll & Escape/Build Android APK (for device testing)")]
        public static void BuildApk()
        {
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.rollandescape.game");
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel23;

            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = new[]
                {
                    "Assets/Scenes/Splash.unity",
                    "Assets/Scenes/LevelSelect.unity",
                    "Assets/Scenes/Game.unity",
                    "Assets/Scenes/Settings.unity"
                },
                locationPathName = OutputPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            var summary = report.summary;

            Debug.Log($"Android build result: {summary.result} - {summary.totalErrors} errors, " +
                      $"{summary.totalWarnings} warnings, {summary.totalSize} bytes, output: {summary.outputPath}");
        }
    }
}
