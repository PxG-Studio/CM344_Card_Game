using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor script to configure WebGL build settings
/// </summary>
public class WebGLBuildSettings : EditorWindow
{
    [MenuItem("Build/Configure WebGL Settings")]
    public static void ConfigureWebGLSettings()
    {
        // Set WebGL as the active build target
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
        
        // Set compression format to Uncompressed
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        
        // Optional: Set other WebGL settings
        PlayerSettings.WebGL.memorySize = 512; // Memory size in MB
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.FullWithStacktrace;
        PlayerSettings.WebGL.linkerTarget = WebGLLinkerTarget.Wasm;
        
        // Set template to default
        PlayerSettings.WebGL.template = "APPLICATION:Default";
        
        Debug.Log("WebGL Build Settings Configured:");
        Debug.Log("- Compression: Disabled (Uncompressed)");
        Debug.Log("- Memory Size: 512 MB");
        Debug.Log("- Exception Support: Full with Stacktrace");
        Debug.Log("- Linker Target: Wasm");
        
        // Save the settings
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        EditorUtility.DisplayDialog("WebGL Settings", 
            "WebGL build settings configured successfully!\n\n" +
            "Compression: Uncompressed\n" +
            "Memory: 512 MB\n" +
            "Linker: WebAssembly\n\n" +
            "You can now build for WebGL from File > Build Settings", 
            "OK");
    }
    
    [MenuItem("Build/Quick WebGL Build")]
    public static void QuickWebGLBuild()
    {
        // Configure settings first
        ConfigureWebGLSettings();
        
        // Set up build path
        string buildPath = EditorUtility.SaveFolderPanel("Choose WebGL Build Location", "", "WebGLBuild");
        
        if (string.IsNullOrEmpty(buildPath))
        {
            Debug.Log("Build cancelled by user");
            return;
        }
        
        // Get all scenes in build settings
        string[] scenes = new string[EditorBuildSettings.scenes.Length];
        for (int i = 0; i < scenes.Length; i++)
        {
            scenes[i] = EditorBuildSettings.scenes[i].path;
        }
        
        // Build
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = buildPath,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };
        
        Debug.Log($"Starting WebGL build to: {buildPath}");
        
        var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        
        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log($"Build succeeded! Size: {report.summary.totalSize} bytes");
            EditorUtility.DisplayDialog("Build Complete", 
                $"WebGL build completed successfully!\n\nLocation: {buildPath}\n\nSize: {report.summary.totalSize / (1024 * 1024)} MB", 
                "OK");
        }
        else
        {
            Debug.LogError($"Build failed: {report.summary.result}");
            EditorUtility.DisplayDialog("Build Failed", 
                $"WebGL build failed!\n\nCheck the console for details.", 
                "OK");
        }
    }
}

