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
                // Safety check: if original handler is null, skip (avoid infinite loop)
                if (originalHandler == null)
                {
                    // Don't call Debug.unityLogger here as it would create infinite recursion
                    // Just silently skip if handler is null
                    return;
                }
                
                string message;
                try
                {
                    message = string.Format(format, args);
                }
                catch
                {
                    // If string formatting fails, just pass through to original handler
                    try
                    {
                        originalHandler.LogFormat(logType, context, format, args);
                    }
                    catch
                    {
                        // If that also fails, silently skip to avoid infinite loops
                    }
                    return;
                }
                
                // Suppress all MCP Unity related logs (comprehensive pattern matching)
                string messageLower = message.ToLowerInvariant();
                if (messageLower.Contains("mcp unity") || 
                    messageLower.Contains("mcpunity") ||
                    messageLower.Contains("mcp-unity") ||
                    messageLower.Contains("gamelovers.mcp") ||
                    messageLower.Contains("package-lock.json.meta") ||
                    messageLower.Contains("server.json") ||
                    message.Contains("com.gamelovers.mcp-unity") ||
                    message.Contains("Packages/com.gamelovers.mcp-unity"))
                {
                    return; // Suppress this log
                }
                
                // Pass all other logs to the original handler
                try
                {
                    originalHandler.LogFormat(logType, context, format, args);
                }
                catch (System.Exception)
                {
                    // If original handler fails, silently skip to avoid infinite loops
                    // Don't call Debug.unityLogger as it would create recursion
                }
            }
            
            public void LogException(System.Exception exception, Object context)
            {
                // Safety check: if original handler is null, skip (avoid infinite loop)
                if (originalHandler == null)
                {
                    // Don't call Debug.unityLogger here as it would create infinite recursion
                    // Just silently skip if handler is null
                    return;
                }
                
                // Check if exception is from MCP Unity (comprehensive pattern matching)
                if (exception != null)
                {
                    string stackTrace = exception.StackTrace ?? "";
                    string stackTraceLower = stackTrace.ToLowerInvariant();
                    string message = exception.Message ?? "";
                    string messageLower = message.ToLowerInvariant();
                    
                    // Suppress MCP Unity related exceptions
                    if (stackTraceLower.Contains("mcpunity") || 
                        stackTraceLower.Contains("mcp-unity") ||
                        stackTraceLower.Contains("gamelovers.mcp") ||
                        stackTrace.Contains("com.gamelovers.mcp-unity") ||
                        messageLower.Contains("mcp unity") ||
                        messageLower.Contains("mcpunity") ||
                        messageLower.Contains("mcp-unity"))
                    {
                        return; // Suppress this exception
                    }
                    
                    // Suppress known Unity test runner bugs (NullReferenceException in PlaymodeLauncher)
                    // This is a Unity internal bug, not related to our code
                    if (stackTrace.Contains("PlaymodeLauncher") && 
                        stackTrace.Contains("BackgroundWatcher") &&
                        stackTrace.Contains("OnPlayModeStateChanged"))
                    {
                        return; // Suppress this known Unity test runner bug
                    }
                }
                
                // Pass all other exceptions to the original handler
                try
                {
                    originalHandler.LogException(exception, context);
                }
                catch (System.Exception)
                {
                    // If original handler fails, silently skip to avoid infinite loops
                    // Don't call Debug.unityLogger as it would create recursion
                }
            }
        }
    }
}
#endif

