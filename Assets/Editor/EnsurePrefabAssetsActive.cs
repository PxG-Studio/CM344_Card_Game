using UnityEngine;
using UnityEditor;
using System.IO;

namespace CardGame.Editor
{
    /// <summary>
    /// [CardFront] Editor utility to ensure prefab assets are active by default.
    /// Fixes issue where cloned cards are instantiated inactive.
    /// </summary>
    public class EnsurePrefabAssetsActive
    {
        [MenuItem("CardFront/Tools/Ensure Prefab Assets Are Active")]
        public static void EnsureActive()
        {
            EnsurePrefabActive("Assets/PreFabs/NewCardPrefabP1.prefab");
            EnsurePrefabActive("Assets/PreFabs/NewCardPrefabP2.prefab");
            
            Debug.Log("[EnsurePrefabAssetsActive] ✓ Prefab assets checked and activated if needed.");
        }
        
        /// <summary>
        /// Ensures a prefab asset's root GameObject is active.
        /// </summary>
        private static void EnsurePrefabActive(string prefabPath)
        {
            if (!File.Exists(prefabPath))
            {
                Debug.LogWarning($"[EnsurePrefabAssetsActive] Prefab not found: {prefabPath}");
                return;
            }
            
            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            
            if (prefabAsset == null)
            {
                Debug.LogWarning($"[EnsurePrefabAssetsActive] Failed to load prefab: {prefabPath}");
                return;
            }
            
            // Open prefab in Prefab Mode to edit
            string assetPath = AssetDatabase.GetAssetPath(prefabAsset);
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(assetPath);
            
            if (prefabRoot == null)
            {
                Debug.LogWarning($"[EnsurePrefabAssetsActive] Failed to load prefab contents: {prefabPath}");
                return;
            }
            
            bool wasModified = false;
            
            // Ensure root GameObject is active
            if (!prefabRoot.activeSelf)
            {
                prefabRoot.SetActive(true);
                wasModified = true;
                Debug.Log($"[EnsurePrefabAssetsActive] Activated root GameObject in prefab: {Path.GetFileName(prefabPath)}");
            }
            
            // Save prefab if modified
            if (wasModified)
            {
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);
                Debug.Log($"[EnsurePrefabAssetsActive] Saved prefab: {Path.GetFileName(prefabPath)}");
            }
            
            // Unload prefab contents
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }
}

