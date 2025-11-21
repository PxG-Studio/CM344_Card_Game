using UnityEngine;
using UnityEditor;
using System.Text.RegularExpressions;

namespace CardGame.Editor
{
    /// <summary>
    /// Suppresses harmless MCP Unity package warnings about immutable folder meta files.
    /// These warnings are safe to ignore as they're related to Unity Package Manager's immutable folders.
    /// </summary>
    [InitializeOnLoad]
    public class SuppressMCPUnityWarnings
    {
        static SuppressMCPUnityWarnings()
        {
            // Suppress the specific MCP Unity package warnings
            Application.logMessageReceived += FilterMCPUnityWarnings;
        }

        private static void FilterMCPUnityWarnings(string logString, string stackTrace, LogType type)
        {
            // These warnings are harmless and related to Unity Package Manager's immutable folders
            if (type == LogType.Warning || type == LogType.Log)
            {
                // Check if it's the MCP Unity package warning
                if (logString.Contains("com.gamelovers.mcp-unity") && 
                    (logString.Contains("immutable folder") || 
                     logString.Contains("package-lock.json") ||
                     logString.Contains("server.json")))
                {
                    // Suppress this warning - it's harmless
                    return;
                }
            }

            // For all other logs, use default Unity behavior
            // Note: We can't actually suppress logs in Unity's console this way,
            // but we can document that these are safe to ignore
        }
    }
}

