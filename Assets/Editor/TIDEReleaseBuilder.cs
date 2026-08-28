using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Builds the macOS app or Windows executable after checking the V2 shared
/// scene route and island configuration. Invoke from TIDE > Build or with the
/// matching command-line method.
/// </summary>
public static class TIDEReleaseBuilder
{
    private const string MacBuildPath = "Builds/macOS/TIDE.app";
    private const string WindowsBuildPath = "Builds/Windows/TIDE.exe";

    [MenuItem("TIDE/Build/Build macOS Player")]
    public static void BuildMacOSPlayer()
    {
        BuildMacOS();
    }

    public static void BuildMacOSFromCommandLine()
    {
        bool succeeded = BuildMacOS();
        EditorApplication.Exit(succeeded ? 0 : 1);
    }

    [MenuItem("TIDE/Build/Build Windows Player")]
    public static void BuildWindowsPlayer()
    {
        BuildWindows();
    }

    public static void BuildWindowsFromCommandLine()
    {
        bool succeeded = BuildWindows();
        EditorApplication.Exit(succeeded ? 0 : 1);
    }

    private static bool BuildMacOS()
    {
        return BuildPlayer(BuildTarget.StandaloneOSX, MacBuildPath, "macOS");
    }

    private static bool BuildWindows()
    {
        return BuildPlayer(BuildTarget.StandaloneWindows64, WindowsBuildPath, "Windows");
    }

    private static bool BuildPlayer(BuildTarget target, string outputPath, string platformName)
    {
        if (!GameAssetValidator.HasCanonicalV2BuildSceneSet(out string sceneReport))
        {
            Debug.LogError($"[TIDEReleaseBuilder] {platformName} build stopped because the V2 release route is incomplete:\n{sceneReport}");
            return false;
        }

        string outputDirectory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene != null && scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = target,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
        {
            Debug.LogError($"[TIDEReleaseBuilder] {platformName} build failed: {report.summary.result}. See the Unity Console for details.");
            return false;
        }

        Debug.Log($"[TIDEReleaseBuilder] {platformName} build succeeded: {outputPath}");
        return true;
    }
}
