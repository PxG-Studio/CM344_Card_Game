using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

namespace CardGame.Editor
{
    /// <summary>
    /// Editor tool to find and fix orphaned .meta files.
    /// Safely removes .meta files that don't have corresponding assets.
    /// </summary>
    public class FixMetaFiles : EditorWindow
    {
        private Vector2 scrollPosition;
        private System.Collections.Generic.List<string> orphanedMetaFiles = new System.Collections.Generic.List<string>();
        
        [MenuItem("Card Game/Fix Orphaned Meta Files")]
        public static void ShowWindow()
        {
            GetWindow<FixMetaFiles>("Fix Orphaned Meta Files");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Fix Orphaned Meta Files", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            GUILayout.Label("This tool finds .meta files that don't have corresponding assets.", EditorStyles.wordWrappedLabel);
            GUILayout.Label("These can cause 'meta data file exists but asset can't be found' errors.", EditorStyles.wordWrappedLabel);
            GUILayout.Space(10);
            
            if (GUILayout.Button("Scan for Orphaned Meta Files", GUILayout.Height(30)))
            {
                ScanForOrphanedMetaFiles();
            }
            
            GUILayout.Space(10);
            
            if (orphanedMetaFiles.Count > 0)
            {
                GUILayout.Label($"Found {orphanedMetaFiles.Count} orphaned .meta file(s):", EditorStyles.boldLabel);
                
                scrollPosition = GUILayout.BeginScrollView(scrollPosition);
                
                foreach (var metaPath in orphanedMetaFiles)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(metaPath, EditorStyles.wordWrappedMiniLabel);
                    if (GUILayout.Button("Delete", GUILayout.Width(60)))
                    {
                        DeleteMetaFile(metaPath);
                    }
                    EditorGUILayout.EndHorizontal();
                }
                
                GUILayout.EndScrollView();
                
                GUILayout.Space(10);
                
                if (GUILayout.Button("Delete All Orphaned Meta Files", GUILayout.Height(30)))
                {
                    DeleteAllOrphanedMetaFiles();
                }
            }
            else
            {
                GUILayout.Label("No orphaned .meta files found.", EditorStyles.wordWrappedLabel);
            }
            
            GUILayout.Space(10);
            GUILayout.Label("Note: Always backup your project before deleting .meta files!", EditorStyles.wordWrappedMiniLabel);
            GUILayout.Label("Note: This tool only scans Assets folder. Library and Packages are excluded.", EditorStyles.wordWrappedMiniLabel);
            GUILayout.Label("Warning: MCP Unity package meta file warnings are harmless - Packages folder is immutable and managed by Unity Package Manager.", EditorStyles.wordWrappedMiniLabel);
        }
        
        private void ScanForOrphanedMetaFiles()
        {
            orphanedMetaFiles.Clear();
            
            // Get all .meta files in Assets folder (NOT Packages - those are immutable)
            string assetsPath = Application.dataPath;
            string[] allMetaFiles = Directory.GetFiles(assetsPath, "*.meta", SearchOption.AllDirectories);
            
            foreach (string metaFilePath in allMetaFiles)
            {
                // Skip Packages folder (immutable Unity Package Manager folder)
                if (metaFilePath.Contains("Packages\\") || metaFilePath.Contains("Packages/"))
                {
                    continue; // Skip - Packages folder is immutable, we can't modify it
                }
                
                // Check if corresponding asset exists
                string assetPath = metaFilePath.Substring(0, metaFilePath.Length - 5); // Remove .meta extension
                
                if (!File.Exists(assetPath) && !Directory.Exists(assetPath))
                {
                    // Convert to Unity asset path (Assets/...)
                    string relativePath = "Assets" + assetPath.Substring(assetsPath.Length);
                    orphanedMetaFiles.Add(relativePath + ".meta");
                }
            }
            
            Debug.Log($"FixMetaFiles: Found {orphanedMetaFiles.Count} orphaned .meta file(s) in Assets folder (Packages folder excluded - it's immutable).");
        }
        
        private void DeleteMetaFile(string metaPath)
        {
            if (EditorUtility.DisplayDialog("Delete Meta File", 
                $"Are you sure you want to delete '{metaPath}'?", 
                "Delete", "Cancel"))
            {
                string fullPath = Path.Combine(Application.dataPath, metaPath.Substring(7)); // Remove "Assets/"
                
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    AssetDatabase.Refresh();
                    orphanedMetaFiles.Remove(metaPath);
                    Debug.Log($"FixMetaFiles: Deleted orphaned .meta file: {metaPath}");
                }
            }
        }
        
        private void DeleteAllOrphanedMetaFiles()
        {
            if (orphanedMetaFiles.Count == 0)
            {
                EditorUtility.DisplayDialog("No Files", "No orphaned .meta files to delete.", "OK");
                return;
            }
            
            if (EditorUtility.DisplayDialog("Delete All Orphaned Meta Files", 
                $"Are you sure you want to delete all {orphanedMetaFiles.Count} orphaned .meta files?\n\n" +
                "This action cannot be undone. Make sure you have a backup!", 
                "Delete All", "Cancel"))
            {
                int deletedCount = 0;
                string assetsPath = Application.dataPath;
                
                foreach (var metaPath in orphanedMetaFiles.ToList())
                {
                    string fullPath = Path.Combine(assetsPath, metaPath.Substring(7)); // Remove "Assets/"
                    
                    if (File.Exists(fullPath))
                    {
                        File.Delete(fullPath);
                        deletedCount++;
                    }
                }
                
                AssetDatabase.Refresh();
                orphanedMetaFiles.Clear();
                
                EditorUtility.DisplayDialog("Delete Complete", 
                    $"Deleted {deletedCount} orphaned .meta file(s).", "OK");
                
                Debug.Log($"FixMetaFiles: Deleted {deletedCount} orphaned .meta file(s).");
            }
        }
    }
}

