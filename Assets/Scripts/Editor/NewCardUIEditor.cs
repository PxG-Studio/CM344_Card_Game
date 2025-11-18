using UnityEngine;
using UnityEditor;
using CardGame.UI;

namespace CardGame.Editor
{
    [CustomEditor(typeof(NewCardUI))]
    [CanEditMultipleObjects]
    public class NewCardUIEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            NewCardUI cardUI = (NewCardUI)target;
            
            // Draw default inspector
            DrawDefaultInspector();
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Auto-Setup", EditorStyles.boldLabel);
            
            // Button to auto-wire all references
            if (GUILayout.Button("Auto-Wire Flip Animation References"))
            {
                WireUpReferences(cardUI);
            }
            
            EditorGUILayout.Space();
            
            // Show current status
            EditorGUILayout.LabelField("Status:", EditorStyles.boldLabel);
            
            var backSpriteRenderer = GetBackSpriteRenderer(cardUI);
            var backImage = GetBackImage(cardUI);
            var defaultCardBackSprite = GetDefaultCardBackSprite(cardUI);
            
            EditorGUILayout.HelpBox(
                $"Flip Animation: {(GetFlipAnimation(cardUI) != null ? "✓ Assigned" : "✗ Missing")}\n" +
                $"Front Container: {(GetFrontContainer(cardUI) != null ? "✓ Assigned" : "✗ Missing")}\n" +
                $"Back Container: {(GetBackContainer(cardUI) != null ? "✓ Assigned" : "✗ Missing")}\n" +
                $"Back Sprite Renderer: {(backSpriteRenderer != null ? "✓ Assigned" : "✗ Missing")}\n" +
                $"Back Image: {(backImage != null ? "✓ Assigned" : "✗ Missing")}\n" +
                $"Default Card Back Sprite: {(defaultCardBackSprite != null ? "✓ Assigned" : "⚠ Optional")}",
                MessageType.Info);
            
            if (defaultCardBackSprite == null)
            {
                EditorGUILayout.HelpBox("Default Card Back Sprite is optional. Assign a sprite here to use as a fallback when cards don't have their own back sprite.", MessageType.Info);
            }
        }
        
        public void WireUpReferences(NewCardUI cardUI)
        {
            Undo.RecordObject(cardUI, "Wire Up Flip Animation References");
            
            // Find or create CardFlipAnimation
            CardFlipAnimation flipAnim = cardUI.GetComponent<CardFlipAnimation>();
            if (flipAnim == null)
            {
                flipAnim = cardUI.gameObject.AddComponent<CardFlipAnimation>();
                Undo.RegisterCreatedObjectUndo(flipAnim, "Create CardFlipAnimation");
            }
            SetFlipAnimation(cardUI, flipAnim);
            
            // Find or create FrontContainer
            Transform frontContainerTransform = cardUI.transform.Find("FrontContainer");
            GameObject frontContainer = null;
            if (frontContainerTransform != null)
            {
                frontContainer = frontContainerTransform.gameObject;
            }
            else
            {
                frontContainer = new GameObject("FrontContainer");
                frontContainer.transform.SetParent(cardUI.transform);
                frontContainer.transform.localPosition = Vector3.zero;
                frontContainer.transform.localRotation = Quaternion.identity;
                frontContainer.transform.localScale = Vector3.one;
                Undo.RegisterCreatedObjectUndo(frontContainer, "Create FrontContainer");
            }
            SetFrontContainer(cardUI, frontContainer);
            
            // Find or create BackContainer
            Transform backContainerTransform = cardUI.transform.Find("BackContainer");
            GameObject backContainer = null;
            if (backContainerTransform != null)
            {
                backContainer = backContainerTransform.gameObject;
            }
            else
            {
                backContainer = new GameObject("BackContainer");
                backContainer.transform.SetParent(cardUI.transform);
                backContainer.transform.localPosition = Vector3.zero;
                backContainer.transform.localRotation = Quaternion.identity;
                backContainer.transform.localScale = Vector3.one;
                Undo.RegisterCreatedObjectUndo(backContainer, "Create BackContainer");
            }
            SetBackContainer(cardUI, backContainer);
            
            // Find or create back visual
            Transform backVisualTransform = backContainer.transform.Find("CardBackVisual");
            GameObject backVisual = null;
            if (backVisualTransform != null)
            {
                backVisual = backVisualTransform.gameObject;
                
                // Find existing back visual component
                UnityEngine.UI.Image existingImage = backVisual.GetComponent<UnityEngine.UI.Image>();
                SpriteRenderer existingSpriteRenderer = backVisual.GetComponent<SpriteRenderer>();
                
                if (existingImage != null)
                {
                    SetBackImage(cardUI, existingImage);
                    
                    // Try to assign default card back sprite if available and not already set
                    if (existingImage.sprite == null)
                    {
                        var defaultSpriteField = typeof(NewCardUI).GetField("defaultCardBackSprite", 
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        Sprite defaultSprite = defaultSpriteField != null ? (Sprite)defaultSpriteField.GetValue(cardUI) : null;
                        if (defaultSprite != null)
                        {
                            existingImage.sprite = defaultSprite;
                            EditorUtility.SetDirty(existingImage);
                        }
                    }
                }
                if (existingSpriteRenderer != null)
                {
                    SetBackSpriteRenderer(cardUI, existingSpriteRenderer);
                    
                    // Try to assign default card back sprite if available and not already set
                    if (existingSpriteRenderer.sprite == null)
                    {
                        var defaultSpriteField = typeof(NewCardUI).GetField("defaultCardBackSprite", 
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        Sprite defaultSprite = defaultSpriteField != null ? (Sprite)defaultSpriteField.GetValue(cardUI) : null;
                        if (defaultSprite != null)
                        {
                            existingSpriteRenderer.sprite = defaultSprite;
                            EditorUtility.SetDirty(existingSpriteRenderer);
                        }
                    }
                }
            }
            else
            {
                backVisual = new GameObject("CardBackVisual");
                backVisual.transform.SetParent(backContainer.transform);
                backVisual.transform.localPosition = Vector3.zero;
                backVisual.transform.localRotation = Quaternion.identity;
                backVisual.transform.localScale = Vector3.one;
                Undo.RegisterCreatedObjectUndo(backVisual, "Create CardBackVisual");
                
                // Add Image or SpriteRenderer based on card type
                RectTransform rectTransform = cardUI.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    // UI card - use Image
                    UnityEngine.UI.Image backImage = backVisual.AddComponent<UnityEngine.UI.Image>();
                    backImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);
                    
                    // Try to use default card back sprite if available, otherwise create white sprite
                    var defaultSpriteField = typeof(NewCardUI).GetField("defaultCardBackSprite", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    Sprite defaultSprite = defaultSpriteField != null ? (Sprite)defaultSpriteField.GetValue(cardUI) : null;
                    
                    if (defaultSprite != null)
                    {
                        backImage.sprite = defaultSprite;
                    }
                    else
                    {
                        // Create white sprite for Image as fallback
                        Texture2D whiteTexture = new Texture2D(1, 1);
                        whiteTexture.SetPixel(0, 0, Color.white);
                        whiteTexture.Apply();
                        backImage.sprite = Sprite.Create(whiteTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
                    }
                    
                    // Set up RectTransform
                    RectTransform backRect = backVisual.GetComponent<RectTransform>();
                    if (backRect != null)
                    {
                        backRect.anchorMin = Vector2.zero;
                        backRect.anchorMax = Vector2.one;
                        backRect.sizeDelta = Vector2.zero;
                        backRect.anchoredPosition = Vector2.zero;
                    }
                    
                    SetBackImage(cardUI, backImage);
                }
                else
                {
                    // 2D card - use SpriteRenderer
                    SpriteRenderer backSpriteRenderer = backVisual.AddComponent<SpriteRenderer>();
                    backSpriteRenderer.color = new Color(0.3f, 0.3f, 0.3f, 1f);
                    
                    // Try to assign default card back sprite if available
                    var defaultSpriteField = typeof(NewCardUI).GetField("defaultCardBackSprite", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    Sprite defaultSprite = defaultSpriteField != null ? (Sprite)defaultSpriteField.GetValue(cardUI) : null;
                    if (defaultSprite != null)
                    {
                        backSpriteRenderer.sprite = defaultSprite;
                    }
                    
                    SetBackSpriteRenderer(cardUI, backSpriteRenderer);
                }
            }
            
            // Update CardFlipAnimation with containers
            if (flipAnim != null && frontContainer != null && backContainer != null)
            {
                flipAnim.SetContainers(frontContainer, backContainer);
            }
            
            EditorUtility.SetDirty(cardUI);
            if (flipAnim != null) EditorUtility.SetDirty(flipAnim);
            
            Debug.Log($"NewCardUI: Auto-wired all Flip Animation references for {cardUI.name}");
        }
        
        // Use reflection to set private serialized fields
        private void SetFlipAnimation(NewCardUI cardUI, CardFlipAnimation flipAnim)
        {
            var field = typeof(NewCardUI).GetField("flipAnimation", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null) field.SetValue(cardUI, flipAnim);
        }
        
        private void SetFrontContainer(NewCardUI cardUI, GameObject container)
        {
            var field = typeof(NewCardUI).GetField("frontContainer", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null) field.SetValue(cardUI, container);
        }
        
        private void SetBackContainer(NewCardUI cardUI, GameObject container)
        {
            var field = typeof(NewCardUI).GetField("backContainer", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null) field.SetValue(cardUI, container);
        }
        
        private void SetBackImage(NewCardUI cardUI, UnityEngine.UI.Image image)
        {
            var field = typeof(NewCardUI).GetField("backImage", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null) field.SetValue(cardUI, image);
        }
        
        private void SetBackSpriteRenderer(NewCardUI cardUI, SpriteRenderer renderer)
        {
            var field = typeof(NewCardUI).GetField("backSpriteRenderer", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null) field.SetValue(cardUI, renderer);
        }
        
        // Use reflection to get private serialized fields
        private CardFlipAnimation GetFlipAnimation(NewCardUI cardUI)
        {
            var field = typeof(NewCardUI).GetField("flipAnimation", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return field != null ? (CardFlipAnimation)field.GetValue(cardUI) : null;
        }
        
        private GameObject GetFrontContainer(NewCardUI cardUI)
        {
            var field = typeof(NewCardUI).GetField("frontContainer", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return field != null ? (GameObject)field.GetValue(cardUI) : null;
        }
        
        private GameObject GetBackContainer(NewCardUI cardUI)
        {
            var field = typeof(NewCardUI).GetField("backContainer", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return field != null ? (GameObject)field.GetValue(cardUI) : null;
        }
        
        private Component GetBackVisual(NewCardUI cardUI)
        {
            var imageField = typeof(NewCardUI).GetField("backImage", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var spriteField = typeof(NewCardUI).GetField("backSpriteRenderer", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (imageField != null)
            {
                var image = (UnityEngine.UI.Image)imageField.GetValue(cardUI);
                if (image != null) return image;
            }
            if (spriteField != null)
            {
                var sprite = (SpriteRenderer)spriteField.GetValue(cardUI);
                if (sprite != null) return sprite;
            }
            return null;
        }
        
        private SpriteRenderer GetBackSpriteRenderer(NewCardUI cardUI)
        {
            var field = typeof(NewCardUI).GetField("backSpriteRenderer", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return field != null ? (SpriteRenderer)field.GetValue(cardUI) : null;
        }
        
        private UnityEngine.UI.Image GetBackImage(NewCardUI cardUI)
        {
            var field = typeof(NewCardUI).GetField("backImage", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return field != null ? (UnityEngine.UI.Image)field.GetValue(cardUI) : null;
        }
        
        private Sprite GetDefaultCardBackSprite(NewCardUI cardUI)
        {
            var field = typeof(NewCardUI).GetField("defaultCardBackSprite", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return field != null ? (Sprite)field.GetValue(cardUI) : null;
        }
    }
}

