using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor utility to clean up missing script references in the current scene.
/// This fixes "The referenced script on this Behaviour is missing!" warnings.
/// </summary>
public class CleanupMissingScripts : EditorWindow
{
    [MenuItem("Card Game/Cleanup Missing Script References")]
    public static void ShowWindow()
    {
        GetWindow<CleanupMissingScripts>("Clean Missing Scripts");
    }

    private void OnGUI()
    {
        GUILayout.Label("Cleanup Missing Script References", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        GUILayout.Label("This tool will remove missing script references from all GameObjects in the current scene.", EditorStyles.wordWrappedLabel);
        GUILayout.Space(10);
        
        if (GUILayout.Button("Clean Missing Scripts in Current Scene", GUILayout.Height(30)))
        {
            CleanCurrentScene();
        }
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Clean Missing Scripts on CardBackVisual Only", GUILayout.Height(30)))
        {
            CleanCardBackVisual();
        }
        
        GUILayout.Space(10);
        GUILayout.Label("Note: This only affects the current scene. Prefabs need to be fixed separately.", EditorStyles.wordWrappedMiniLabel);
    }

    private static void CleanCurrentScene()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Cannot Clean During Play", 
                "Please stop the game before cleaning missing scripts.", "OK");
            return;
        }

        int fixedCount = 0;
        GameObject[] allObjects = FindObjectsOfType<GameObject>(true);
        
        foreach (GameObject obj in allObjects)
        {
            // Skip prefab assets
            if (obj == null || EditorUtility.IsPersistent(obj))
            {
                continue;
            }
            
            int removedCount = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(obj);
            if (removedCount > 0)
            {
                fixedCount += removedCount;
                Debug.Log($"Removed {removedCount} missing script(s) from {obj.name}");
            }
        }
        
        if (fixedCount > 0)
        {
            EditorUtility.DisplayDialog("Cleanup Complete", 
                $"Removed {fixedCount} missing script reference(s) from GameObjects in the current scene.", "OK");
            // Mark scene as dirty so changes are saved
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        }
        else
        {
            EditorUtility.DisplayDialog("No Issues Found", 
                "No missing script references found in the current scene.", "OK");
        }
    }

    private static void CleanCardBackVisual()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Cannot Clean During Play", 
                "Please stop the game before cleaning missing scripts.", "OK");
            return;
        }

        int fixedCount = 0;
        GameObject[] allObjects = FindObjectsOfType<GameObject>(true);
        
        foreach (GameObject obj in allObjects)
        {
            if (obj != null && obj.name == "CardBackVisual")
            {
                int removedCount = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(obj);
                if (removedCount > 0)
                {
                    fixedCount += removedCount;
                    Debug.Log($"Removed {removedCount} missing script(s) from CardBackVisual on {obj.transform.root?.name ?? "Unknown Root"}");
                }
            }
        }
        
        if (fixedCount > 0)
        {
            EditorUtility.DisplayDialog("Cleanup Complete", 
                $"Removed {fixedCount} missing script reference(s) from CardBackVisual GameObjects.", "OK");
            // Mark scene as dirty so changes are saved
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        }
        else
        {
            EditorUtility.DisplayDialog("No Issues Found", 
                "No missing script references found on CardBackVisual GameObjects.", "OK");
        }
    }
}

