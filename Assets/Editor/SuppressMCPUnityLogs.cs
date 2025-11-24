#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace CardGame.Editor
{
    /// <summary>
    /// Suppresses MCP Unity package logs using Unity's LogHandler API.
    /// This is needed because the package may still be in Unity's cache
    /// even after removal from manifest.json, until Unity refreshes the package cache.
    /// </summary>
    [InitializeOnLoad]
    public class SuppressMCPUnityLogs
    {
        private static ILogHandler originalLogHandler;
        private static bool isInitialized = false;
        
        static SuppressMCPUnityLogs()
        {
            if (!isInitialized)
            {
                originalLogHandler = Debug.unityLogger.logHandler;
                Debug.unityLogger.logHandler = new MCPUnityLogFilter(originalLogHandler);
                isInitialized = true;
            }
        }
        
        private class MCPUnityLogFilter : ILogHandler
        {
            private ILogHandler originalHandler;
            
            public MCPUnityLogFilter(ILogHandler original)
            {
                originalHandler = original;
            }
            
            public void LogFormat(LogType logType, Object context, string format, params object[] args)
            {
                string message = string.Format(format, args);
                
                // Suppress all MCP Unity related logs
                if (message.Contains("[MCP Unity]") || 
                    message.Contains("McpUnity") ||
                    message.Contains("mcp-unity"))
                {
                    return; // Suppress this log
                }
                
                // Pass all other logs to the original handler
                originalHandler.LogFormat(logType, context, format, args);
            }
            
            public void LogException(System.Exception exception, Object context)
            {
                // Check if exception is from MCP Unity
                if (exception != null && 
                    (exception.StackTrace != null && 
                     (exception.StackTrace.Contains("McpUnity") || 
                      exception.StackTrace.Contains("mcp-unity"))))
                {
                    return; // Suppress this exception
                }
                
                // Pass all other exceptions to the original handler
                originalHandler.LogException(exception, context);
            }
        }
    }
}
#endif

