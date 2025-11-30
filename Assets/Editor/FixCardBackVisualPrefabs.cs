using UnityEngine;
using UnityEditor;

namespace CardGame.Editor
{
    /// <summary>
    /// [CardFront] Editor tool to fix missing script references on CardBackVisual in prefabs.
    /// Run this once to clean up prefab assets before runtime.
    /// </summary>
    public static class FixCardBackVisualPrefabs
    {
        [MenuItem("CardFront/Fix Prefabs/Clean Missing Scripts on CardBackVisual Prefabs")]
        public static void CleanCardBackVisualPrefabs()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Cannot Clean During Play",
                    "Please stop the game before cleaning prefab assets.", "OK");
                return;
            }

            int fixedCount = 0;
            int totalPrefabsChecked = 0;
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/PreFabs" });
            
            Debug.Log($"[FixCardBackVisualPrefabs] Searching {prefabGuids.Length} prefab(s) in Assets/PreFabs...");
            
            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                
                if (prefab == null) continue;
                
                totalPrefabsChecked++;
                
                // [CardFront] Load prefab contents to access the prefab for editing
                // We must load prefab contents to properly check and fix missing scripts
                GameObject prefabInstance = PrefabUtility.LoadPrefabContents(path);
                
                // [CardFront] Use recursive search to find CardBackVisual anywhere in prefab hierarchy
                Transform cardBackVisual = FindCardBackVisualRecursive(prefabInstance.transform);
                
                if (cardBackVisual != null)
                {
                    int missingScriptCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(cardBackVisual.gameObject);
                    if (missingScriptCount > 0)
                    {
                        Debug.Log($"[FixCardBackVisualPrefabs] Found {missingScriptCount} missing script(s) on CardBackVisual in {path}");
                        
                        // [CardFront] Remove missing scripts from the loaded prefab instance
                        int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(cardBackVisual.gameObject);
                        if (removed > 0)
                        {
                            // [CardFront] Save the modified prefab back to the asset
                            PrefabUtility.SaveAsPrefabAsset(prefabInstance, path);
                            
                            fixedCount += removed;
                            Debug.Log($"[FixCardBackVisualPrefabs] ✓ Fixed {removed} missing script(s) on CardBackVisual in prefab: {path}");
                        }
                    }
                }
                
                // [CardFront] Always unload prefab contents after processing
                PrefabUtility.UnloadPrefabContents(prefabInstance);
            }
            
            Debug.Log($"[FixCardBackVisualPrefabs] Checked {totalPrefabsChecked} prefab(s), fixed {fixedCount} missing script reference(s)");
            
            if (fixedCount > 0)
            {
                AssetDatabase.SaveAssets();
                EditorUtility.DisplayDialog("Fix Complete",
                    $"Fixed {fixedCount} missing script reference(s) on CardBackVisual in prefab assets.\n\nPrefabs have been saved.", "OK");
                Debug.Log($"[FixCardBackVisualPrefabs] Successfully fixed {fixedCount} missing script reference(s) on CardBackVisual prefabs");
            }
            else
            {
                EditorUtility.DisplayDialog("No Issues Found",
                    "No missing script references found on CardBackVisual in prefab assets.", "OK");
            }
        }
        
        private static Transform FindCardBackVisualRecursive(Transform parent)
        {
            if (parent == null) return null;
            
            if (parent.name == "CardBackVisual")
            {
                return parent;
            }
            
            foreach (Transform child in parent)
            {
                Transform found = FindCardBackVisualRecursive(child);
                if (found != null) return found;
            }
            
            return null;
        }
        
    }
}

