using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace CardGame.Editor
{
    /// <summary>
    /// [CardFront] Editor utility to remove prefab assets from scene hierarchy.
    /// Prefab assets should NOT be in the scene - they should only exist as prefab assets in the project.
    /// Automatically runs when scene is opened.
    /// </summary>
    [InitializeOnLoad]
    public class RemovePrefabAssetsFromScene
    {
        static RemovePrefabAssetsFromScene()
        {
            // Automatically run when scene is loaded or changed
            EditorSceneManager.sceneOpened += OnSceneOpened;
        }
        
        private static void OnSceneOpened(UnityEngine.SceneManagement.Scene scene, OpenSceneMode mode)
        {
            // Don't run during PlayMode (tests or runtime) - only in EditMode
            if (Application.isPlaying)
            {
                return;
            }
            
            // Skip test scenes (they're temporary and shouldn't be modified)
            if (scene.name == null || scene.name.StartsWith("InitTestScene") || scene.name.Contains("Test"))
            {
                return;
            }
            
            // Run cleanup automatically when scene is opened (with safety check)
            EditorApplication.delayCall += () => 
            {
                try
                {
                    // Double-check we're still in EditMode
                    if (!Application.isPlaying)
                    {
                        RemovePrefabAssets();
                    }
                }
                catch (System.Exception ex)
                {
                    // Silently fail during test execution to avoid interfering with tests
                    if (!ex.Message.Contains("test"))
                    {
                        Debug.LogWarning($"[RemovePrefabAssets] Error during cleanup: {ex.Message}");
                    }
                }
            };
        }
        
        [MenuItem("CardFront/Tools/Remove Prefab Assets from Scene")]
        public static void RemovePrefabAssets()
        {
            // Don't run during PlayMode
            if (Application.isPlaying)
            {
                return;
            }
            
            UnityEngine.SceneManagement.Scene activeScene = EditorSceneManager.GetActiveScene();
            
            if (!activeScene.IsValid())
            {
                Debug.LogWarning("[RemovePrefabAssets] No active scene found!");
                return;
            }
            
            // Skip test scenes
            if (activeScene.name.StartsWith("InitTestScene") || activeScene.name.Contains("Test"))
            {
                return;
            }
            
            GameObject[] allObjects = Object.FindObjectsOfType<GameObject>(true);
            int removedCount = 0;
            
            foreach (GameObject obj in allObjects)
            {
                if (obj == null) continue;
                
                string objName = obj.name;
                
                // Check for exact prefab asset names (without Clone suffix)
                if ((objName == "NewCardPrefab" || objName == "NewCardPrefabOpp") && 
                    !objName.Contains("(Clone)"))
                {
                    // Verify it has NewCardUI component (indicates it's a card prefab)
                    CardGame.UI.NewCardUI cardUI = obj.GetComponent<CardGame.UI.NewCardUI>();
                    if (cardUI != null)
                    {
                        // Check if this is NOT a prefab instance (it's a scene object)
                        PrefabAssetType prefabType = PrefabUtility.GetPrefabAssetType(obj);
                        PrefabInstanceStatus prefabStatus = PrefabUtility.GetPrefabInstanceStatus(obj);
                        
                        // If it's a scene object (not a prefab instance or variant), remove it
                        if (prefabType == PrefabAssetType.NotAPrefab && 
                            prefabStatus == PrefabInstanceStatus.NotAPrefab)
                        {
                            Debug.LogWarning($"[RemovePrefabAssets] Found prefab asset '{objName}' (InstanceID: {obj.GetInstanceID()}) in scene hierarchy at path '{GetFullPath(obj)}'. Removing...");
                            Object.DestroyImmediate(obj);
                            removedCount++;
                        }
                    }
                }
            }
            
            if (removedCount > 0)
            {
                EditorSceneManager.MarkSceneDirty(activeScene);
                Debug.Log($"[RemovePrefabAssets] ✓ Removed {removedCount} prefab asset(s) from scene '{activeScene.name}'.");
            }
            else
            {
                Debug.Log($"[RemovePrefabAssets] ✓ No prefab assets found in scene '{activeScene.name}'. Scene is clean.");
            }
        }
        
        /// <summary>
        /// Gets the full hierarchy path of a GameObject.
        /// </summary>
        private static string GetFullPath(GameObject obj)
        {
            string path = obj.name;
            Transform parent = obj.transform.parent;
            
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            
            return path;
        }
    }
}
