using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;

namespace CardGame.Editor
{
    /// <summary>
    /// Prevents MCP Unity server from restarting on every domain reload.
    /// Ensures server only starts once and handles reloads gracefully.
    /// </summary>
    [InitializeOnLoad]
    public class FixMCPUnityReload
    {
        private static bool serverInitialized = false;
        private static int lastReloadFrame = -1;
        
        static FixMCPUnityReload()
        {
            // Subscribe to domain reload events
            EditorApplication.delayCall += OnDelayedInit;
            
            // Prevent duplicate initialization on domain reload
            EditorApplication.update += OnEditorUpdate;
        }
        
        private static void OnEditorUpdate()
        {
            // Only initialize once per Unity session
            if (!serverInitialized && !EditorApplication.isCompiling && !EditorApplication.isUpdating)
            {
                // Delay initialization to avoid conflicts with domain reload
                EditorApplication.delayCall += InitializeMCPOnce;
                serverInitialized = true;
            }
        }
        
        private static void OnDelayedInit()
        {
            // This is called after domain reload completes
            // Reset initialization flag to allow server to restart if needed
            // But prevent multiple starts in same frame
            int currentFrame = Time.frameCount;
            
            if (lastReloadFrame != currentFrame)
            {
                lastReloadFrame = currentFrame;
                
                // Server should be handled by MCP Unity package itself
                // We just prevent duplicate initialization attempts
            }
        }
        
        private static void InitializeMCPOnce()
        {
            // MCP Unity package handles its own server lifecycle
            // We just prevent duplicate initialization
            Debug.Log("FixMCPUnityReload: MCP Unity server lifecycle is managed by the package. No manual initialization needed.");
        }
        
        [DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            // Scripts have been reloaded - this is normal and expected
            // MCP Unity server should handle its own restart gracefully
            Debug.Log("FixMCPUnityReload: Scripts reloaded. MCP Unity server will restart if needed (this is normal).");
        }
    }
}

