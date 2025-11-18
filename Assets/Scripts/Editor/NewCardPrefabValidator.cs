using UnityEngine;
using UnityEditor;
using CardGame.UI;

namespace CardGame.Editor
{
    /// <summary>
    /// Editor script to validate NewCardPrefab setup for flip animation
    /// </summary>
    public class NewCardPrefabValidator : EditorWindow
    {
        [MenuItem("Card Game/Validate NewCardPrefab Setup")]
        public static void ShowWindow()
        {
            GetWindow<NewCardPrefabValidator>("Card Prefab Validator");
        }

        private void OnGUI()
        {
            GUILayout.Label("NewCardPrefab Flip Animation Validator", EditorStyles.boldLabel);
            GUILayout.Space(10);

            if (GUILayout.Button("Validate Current Selection", GUILayout.Height(30)))
            {
                ValidateSelection();
            }

            GUILayout.Space(10);
            GUILayout.Label("Instructions:", EditorStyles.boldLabel);
            GUILayout.Label("1. Select NewCardPrefab in Project window");
            GUILayout.Label("2. Click 'Validate Current Selection'");
            GUILayout.Label("3. Check Console for validation results");
        }

        private void ValidateSelection()
        {
            GameObject prefab = Selection.activeGameObject;
            
            if (prefab == null)
            {
                Debug.LogError("No GameObject selected! Please select NewCardPrefab.");
                return;
            }

            Debug.Log($"=== Validating: {prefab.name} ===");

            // Check for NewCardUI component
            NewCardUI cardUI = prefab.GetComponent<NewCardUI>();
            if (cardUI == null)
            {
                Debug.LogError($"❌ {prefab.name}: Missing NewCardUI component!");
                return;
            }
            Debug.Log($"✅ {prefab.name}: Has NewCardUI component");

            // Use reflection to check private fields
            var frontContainerField = typeof(NewCardUI).GetField("frontContainer", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var backContainerField = typeof(NewCardUI).GetField("backContainer", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            GameObject frontContainer = frontContainerField?.GetValue(cardUI) as GameObject;
            GameObject backContainer = backContainerField?.GetValue(cardUI) as GameObject;

            // Check FrontContainer
            if (frontContainer == null)
            {
                Debug.LogWarning($"⚠️ {prefab.name}: FrontContainer not assigned in NewCardUI!");
            }
            else
            {
                Debug.Log($"✅ {prefab.name}: FrontContainer assigned: {frontContainer.name}");
                
                // Check if FrontContainer has children (card elements)
                if (frontContainer.transform.childCount == 0)
                {
                    Debug.LogWarning($"⚠️ {prefab.name}: FrontContainer has no children! Move card elements into it.");
                }
                else
                {
                    Debug.Log($"✅ {prefab.name}: FrontContainer has {frontContainer.transform.childCount} child elements");
                }

                // Check for CanvasGroup
                if (frontContainer.GetComponent<CanvasGroup>() == null)
                {
                    Debug.Log($"ℹ️ {prefab.name}: FrontContainer missing CanvasGroup (will be auto-added)");
                }
                else
                {
                    Debug.Log($"✅ {prefab.name}: FrontContainer has CanvasGroup");
                }
            }

            // Check BackContainer
            if (backContainer == null)
            {
                Debug.LogWarning($"⚠️ {prefab.name}: BackContainer not assigned in NewCardUI!");
            }
            else
            {
                Debug.Log($"✅ {prefab.name}: BackContainer assigned: {backContainer.name}");
                
                // Check if BackContainer has visual element
                bool hasSpriteRenderer = backContainer.GetComponentInChildren<SpriteRenderer>() != null;
                bool hasImage = backContainer.GetComponentInChildren<UnityEngine.UI.Image>() != null;
                
                if (!hasSpriteRenderer && !hasImage)
                {
                    Debug.LogWarning($"⚠️ {prefab.name}: BackContainer has no visual element (SpriteRenderer or Image)! Add one.");
                }
                else
                {
                    Debug.Log($"✅ {prefab.name}: BackContainer has visual element ({(hasSpriteRenderer ? "SpriteRenderer" : "Image")})");
                }

                // Check for CanvasGroup
                if (backContainer.GetComponent<CanvasGroup>() == null)
                {
                    Debug.Log($"ℹ️ {prefab.name}: BackContainer missing CanvasGroup (will be auto-added)");
                }
                else
                {
                    Debug.Log($"✅ {prefab.name}: BackContainer has CanvasGroup");
                }
            }

            // Check CardFlipAnimation component
            CardFlipAnimation flipAnim = prefab.GetComponent<CardFlipAnimation>();
            if (flipAnim == null && frontContainer != null && backContainer != null)
            {
                Debug.Log($"ℹ️ {prefab.name}: CardFlipAnimation not found (will be auto-added when containers are set)");
            }
            else if (flipAnim != null)
            {
                Debug.Log($"✅ {prefab.name}: Has CardFlipAnimation component");
            }

            // Check UI references
            var cardBackgroundField = typeof(NewCardUI).GetField("cardBackground", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var artworkField = typeof(NewCardUI).GetField("artwork", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (cardBackgroundField?.GetValue(cardUI) == null)
            {
                Debug.LogWarning($"⚠️ {prefab.name}: Card Background not assigned in NewCardUI!");
            }
            else
            {
                Debug.Log($"✅ {prefab.name}: Card Background assigned");
            }

            if (artworkField?.GetValue(cardUI) == null)
            {
                Debug.LogWarning($"⚠️ {prefab.name}: Artwork not assigned in NewCardUI!");
            }
            else
            {
                Debug.Log($"✅ {prefab.name}: Artwork assigned");
            }

            Debug.Log($"=== Validation Complete ===");
            
            // Show auto-wire button if containers are missing
            if (frontContainer == null || backContainer == null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("Flip Animation containers are missing. Click below to auto-wire them.", MessageType.Warning);
                if (GUILayout.Button("Auto-Wire Flip Animation References", GUILayout.Height(30)))
                {
                    AutoWireReferences(prefab, cardUI);
                }
            }
        }
        
        private void AutoWireReferences(GameObject prefab, NewCardUI cardUI)
        {
            // Use the NewCardUIEditor's wire-up method via reflection
            var editorType = typeof(NewCardUIEditor);
            var wireUpMethod = editorType.GetMethod("WireUpReferences", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            
            if (wireUpMethod != null)
            {
                // Create an instance of the editor using UnityEditor.Editor.CreateEditor
                var editor = UnityEditor.Editor.CreateEditor(cardUI, typeof(NewCardUIEditor)) as NewCardUIEditor;
                if (editor != null)
                {
                    wireUpMethod.Invoke(editor, new object[] { cardUI });
                    EditorUtility.SetDirty(cardUI);
                    EditorUtility.SetDirty(prefab);
                    Debug.Log($"✅ Auto-wired Flip Animation references for {prefab.name}");
                    
                    // Re-validate to show updated status
                    ValidateSelection();
                }
                else
                {
                    Debug.LogError("Failed to create NewCardUIEditor instance");
                }
            }
            else
            {
                Debug.LogError("Could not find WireUpReferences method in NewCardUIEditor");
            }
        }
    }
}

