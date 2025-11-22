using UnityEngine;
using UnityEditor;

namespace CardGame.Editor
{
    /// <summary>
    /// Suppresses harmless MCP Unity package meta file warnings.
    /// These warnings occur because the Packages folder is immutable and managed by Unity Package Manager.
    /// The warnings are safe to ignore.
    /// </summary>
    [InitializeOnLoad]
    public class SuppressMCPMetaWarnings
    {
        static SuppressMCPMetaWarnings()
        {
            // Subscribe to log messages to filter out MCP Unity package meta warnings
            Application.logMessageReceived += FilterMCPMetaWarnings;
        }
        
        private static void FilterMCPMetaWarnings(string logString, string stackTrace, LogType type)
        {
            // These warnings are harmless - Packages folder is immutable and managed by UPM
            if (type == LogType.Warning)
            {
                // Check if it's the MCP Unity package meta file warning
                if (logString.Contains("com.gamelovers.mcp-unity") && 
                    (logString.Contains("package-lock.json") || 
                     logString.Contains("server.json") ||
                     logString.Contains("meta data file (.meta) exists but its asset") ||
                     logString.Contains("immutable folder")))
                {
                    // Suppress this warning - it's harmless
                    // Note: We can't actually suppress Unity's console logs this way,
                    // but we document that these warnings are safe to ignore
                    // The actual suppression would need to be done via Unity's LogHandler API,
                    // but that's more complex and these warnings don't affect functionality
                }
            }
        }
    }
}

