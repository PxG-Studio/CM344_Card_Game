using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using CardGame.UI;

namespace CardGame.Editor
{
    /// <summary>
    /// Editor utility to fix missing script references on prefab instances.
    /// This helps resolve "The referenced script on this Behaviour is missing!" errors.
    /// </summary>
    public class FixPrefabScriptReferences : EditorWindow
    {
        [InitializeOnLoadMethod]
        private static void AutoFixMissingScriptsOnLoad()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            
            EditorApplication.delayCall += () =>
            {
                RemoveMissingScriptsFromCardBackVisual(true);
                FixMissingScriptsInScene(true);
            };
        }
        
        [MenuItem("Card Game/Fix Missing Script References")]
        public static void ShowWindow()
        {
            GetWindow<FixPrefabScriptReferences>("Fix Script References");
        }

        private void OnGUI()
        {
            GUILayout.Label("Fix Missing Script References", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            GUILayout.Label("This tool will fix missing script references on prefab instances in the current scene.", EditorStyles.wordWrappedLabel);
            GUILayout.Space(10);
            
            if (GUILayout.Button("Fix All Missing Scripts in Current Scene", GUILayout.Height(30)))
            {
                FixMissingScriptsInScene();
            }
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("Fix NewCardPrefab Instances", GUILayout.Height(30)))
            {
                FixNewCardPrefabInstances();
            }
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("Remove Missing Scripts from CardBackVisual", GUILayout.Height(30)))
            {
                RemoveMissingScriptsFromCardBackVisual();
            }
            
            GUILayout.Space(10);
            GUILayout.Label("Note: After fixing, Unity will automatically reconnect prefab instances.", EditorStyles.wordWrappedMiniLabel);
        }

        private static void FixMissingScriptsInScene(bool silent = false)
        {
            int fixedCount = 0;
            GameObject[] allObjects = FindObjectsOfType<GameObject>(true);
            
            foreach (GameObject obj in allObjects)
            {
                // Skip assets and immutable objects (package contents, prefabs in Project)
                if (obj == null || EditorUtility.IsPersistent(obj))
                {
                    continue;
                }
                
                // Check for missing scripts using SerializedObject
                SerializedObject so = new SerializedObject(obj);
                SerializedProperty prop = so.FindProperty("m_Component");
                
                if (prop != null && prop.isArray)
                {
                    // Work backwards to avoid index issues when deleting
                    for (int i = prop.arraySize - 1; i >= 0; i--)
                    {
                        try
                        {
                            SerializedProperty element = prop.GetArrayElementAtIndex(i);
                            SerializedProperty component = element.FindPropertyRelative("component");
                            
                            if (component != null && component.objectReferenceValue == null)
                            {
                                // Found a missing component reference - remove it
                                prop.DeleteArrayElementAtIndex(i);
                                fixedCount++;
                            }
                        }
                        catch (System.Exception ex)
                        {
                            Debug.LogWarning($"FixMissingScriptsInScene: could not remove missing script from {obj.name} ({ex.Message})");
                        }
                    }
                    
                    if (fixedCount > 0)
                    {
                        so.ApplyModifiedProperties();
                        Debug.Log($"Fixed {fixedCount} missing script(s) on {obj.name}");
                    }
                }
            }
            
            if (silent) return;
            
            if (fixedCount > 0)
            {
                EditorUtility.DisplayDialog("Fix Complete", 
                    $"Fixed missing script references on {fixedCount} GameObject(s) in the scene.", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("No Issues Found", 
                    "No missing script references found in the current scene.", "OK");
            }
        }

        private static void FixNewCardPrefabInstances()
        {
            int fixedCount = 0;
            int checkedCount = 0;
            
            // Find all NewCardPrefab instances
            GameObject[] allObjects = FindObjectsOfType<GameObject>(true);
            
            foreach (GameObject obj in allObjects)
            {
                if (obj.name.Contains("NewCardPrefab") || obj.name.Contains("NewCardPrefabOpp"))
                {
                    checkedCount++;
                    
                    // Check if this is a prefab instance
                    PrefabInstanceStatus prefabStatus = PrefabUtility.GetPrefabInstanceStatus(obj);
                    
                    if (prefabStatus == PrefabInstanceStatus.Connected)
                    {
                        // Try to reconnect to prefab (this refreshes the instance)
                        GameObject prefabAsset = PrefabUtility.GetCorrespondingObjectFromSource(obj);
                        
                        if (prefabAsset != null)
                        {
                            // Revert any overrides that might be causing issues
                            PrefabUtility.RevertPrefabInstance(obj, InteractionMode.AutomatedAction);
                            fixedCount++;
                            Debug.Log($"Refreshed prefab instance: {obj.name}");
                        }
                    }
                    else if (prefabStatus == PrefabInstanceStatus.MissingAsset)
                    {
                        Debug.LogWarning($"{obj.name} is a broken prefab instance. Please reconnect it manually in the Inspector.");
                    }
                    
                    // Check for required components
                    bool hasNewCardUI = obj.GetComponent<NewCardUI>() != null;
                    // CardMover is in global namespace (no namespace)
                    bool hasCardMover = obj.GetComponent("CardMover") != null;
                    bool hasCardFlipAnimation = obj.GetComponent<CardFlipAnimation>() != null;
                    
                    if (!hasNewCardUI)
                    {
                        Debug.LogWarning($"{obj.name} is missing NewCardUI component!");
                    }
                    if (!hasCardMover)
                    {
                        Debug.LogWarning($"{obj.name} is missing CardMover component!");
                    }
                    if (!hasCardFlipAnimation)
                    {
                        Debug.LogWarning($"{obj.name} is missing CardFlipAnimation component!");
                    }
                }
            }
            
            EditorUtility.DisplayDialog("Fix Complete", 
                $"Checked {checkedCount} prefab instance(s), refreshed {fixedCount}. Check Console for component warnings.", "OK");
        }
        
        /// <summary>
        /// Removes missing script references from CardBackVisual GameObjects.
        /// This fixes "The referenced script on this Behaviour (Game Object 'CardBackVisual') is missing!" errors.
        /// </summary>
        private static void RemoveMissingScriptsFromCardBackVisual(bool silent = false)
        {
            int fixedCount = 0;
            GameObject[] allObjects = Object.FindObjectsOfType<GameObject>(true);
            
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
            
            if (silent) return;
            
            if (fixedCount > 0)
            {
                EditorUtility.DisplayDialog("Fix Complete", 
                    $"Removed {fixedCount} missing script reference(s) from CardBackVisual GameObjects.", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("No Issues Found", 
                    "No missing script references found on CardBackVisual GameObjects.", "OK");
            }
        }
    }
}

