#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using CardGame.UI;

namespace CardGame.Editor
{
    [CustomEditor(typeof(NewCardUI))]
    [CanEditMultipleObjects]
    public class NewCardUIPreview : UnityEditor.Editor
    {
        private bool showBackPreview = false;
        
        public override void OnInspectorGUI()
        {
            NewCardUI cardUI = (NewCardUI)target;
            
            // Draw default inspector
            DrawDefaultInspector();
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Card Back Preview", EditorStyles.boldLabel);
            
            // Preview toggle
            showBackPreview = EditorGUILayout.Toggle("Show Card Back Preview", showBackPreview);
            
            if (showBackPreview)
            {
                EditorGUILayout.HelpBox(
                    "This will temporarily show the card back in the Scene view. " +
                    "The card will be flipped to show the back side.",
                    MessageType.Info);
                
                if (GUILayout.Button("Preview Card Back"))
                {
                    PreviewCardBack(cardUI);
                }
                
                if (GUILayout.Button("Show Card Front"))
                {
                    ShowCardFront(cardUI);
                }
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Load Default Card Back Sprite"))
            {
                LoadDefaultCardBackSprite(cardUI);
            }
        }
        
        private void PreviewCardBack(NewCardUI cardUI)
        {
            // Get the CardFlipAnimation component
            CardFlipAnimation flipAnim = cardUI.GetComponent<CardFlipAnimation>();
            if (flipAnim == null)
            {
                EditorUtility.DisplayDialog("Error", 
                    "CardFlipAnimation component not found. Please set up the flip animation first.", 
                    "OK");
                return;
            }
            
            // Use reflection to get private fields
            var backContainerField = typeof(NewCardUI).GetField("backContainer",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var frontContainerField = typeof(NewCardUI).GetField("frontContainer",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            GameObject backContainer = backContainerField != null ? 
                (GameObject)backContainerField.GetValue(cardUI) : null;
            GameObject frontContainer = frontContainerField != null ? 
                (GameObject)frontContainerField.GetValue(cardUI) : null;
            
            if (backContainer == null || frontContainer == null)
            {
                EditorUtility.DisplayDialog("Error", 
                    "Back or Front container not found. Please set up the containers first.", 
                    "OK");
                return;
            }
            
            // Temporarily show back and hide front
            Undo.RecordObject(frontContainer, "Preview Card Back");
            Undo.RecordObject(backContainer, "Preview Card Back");
            Undo.RecordObject(cardUI.transform, "Preview Card Back");
            
            frontContainer.SetActive(false);
            backContainer.SetActive(true);
            cardUI.transform.localRotation = Quaternion.Euler(0, 180, 0);
            
            // Ensure the back sprite is visible
            SpriteRenderer backSpriteRenderer = backContainer.GetComponentInChildren<SpriteRenderer>();
            if (backSpriteRenderer != null)
            {
                backSpriteRenderer.color = new Color(1f, 1f, 1f, 1f);
                backSpriteRenderer.enabled = true;
            }
            
            EditorUtility.SetDirty(cardUI);
            SceneView.RepaintAll();
            
            Debug.Log($"[NewCardUIPreview] Showing card back preview for {cardUI.name}");
        }
        
        private void ShowCardFront(NewCardUI cardUI)
        {
            // Get containers using reflection
            var backContainerField = typeof(NewCardUI).GetField("backContainer",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var frontContainerField = typeof(NewCardUI).GetField("frontContainer",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            GameObject backContainer = backContainerField != null ? 
                (GameObject)backContainerField.GetValue(cardUI) : null;
            GameObject frontContainer = frontContainerField != null ? 
                (GameObject)frontContainerField.GetValue(cardUI) : null;
            
            if (backContainer == null || frontContainer == null)
            {
                return;
            }
            
            // Restore front view
            Undo.RecordObject(frontContainer, "Show Card Front");
            Undo.RecordObject(backContainer, "Show Card Front");
            Undo.RecordObject(cardUI.transform, "Show Card Front");
            
            frontContainer.SetActive(true);
            backContainer.SetActive(false);
            cardUI.transform.localRotation = Quaternion.Euler(0, 0, 0);
            
            EditorUtility.SetDirty(cardUI);
            SceneView.RepaintAll();
            
            Debug.Log($"[NewCardUIPreview] Showing card front for {cardUI.name}");
        }
        
        private void LoadDefaultCardBackSprite(NewCardUI cardUI)
        {
            // Try to load the default card back sprite
            Sprite defaultSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprite/CardBack_Default.png");
            
            if (defaultSprite == null)
            {
                // Try alternative paths
                string[] guids = AssetDatabase.FindAssets("CardBack_Default t:Sprite");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    defaultSprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                }
            }
            
            if (defaultSprite != null)
            {
                // Use reflection to set the defaultCardBackSprite field
                var field = typeof(NewCardUI).GetField("defaultCardBackSprite",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    Undo.RecordObject(cardUI, "Load Default Card Back Sprite");
                    field.SetValue(cardUI, defaultSprite);
                    EditorUtility.SetDirty(cardUI);
                    Debug.Log($"[NewCardUIPreview] Loaded default card back sprite: {defaultSprite.name}");
                    EditorUtility.DisplayDialog("Success", 
                        $"Default card back sprite loaded:\n{defaultSprite.name}", 
                        "OK");
                }
            }
            else
            {
                EditorUtility.DisplayDialog("Not Found", 
                    "Could not find CardBack_Default sprite.\n\n" +
                    "Please ensure the sprite exists at:\n" +
                    "Assets/Sprite/CardBack_Default.png\n\n" +
                    "Or use Tools > CardGame > Generate Card Back Sprite to create one.", 
                    "OK");
            }
        }
    }
}
#endif

