using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using CardGame.UI;

namespace CardGame.Editor
{
    /// <summary>
    /// Editor tool to validate and fix card prefabs.
    /// Scans all card prefabs and reports/fixes missing scripts, null references, and missing components.
    /// </summary>
    public class CardPrefabValidator : EditorWindow
    {
        private Vector2 scrollPosition;
        private List<ValidationResult> validationResults = new List<ValidationResult>();
        
        private class ValidationResult
        {
            public string prefabPath;
            public GameObject prefab;
            public List<string> errors = new List<string>();
            public List<string> warnings = new List<string>();
            public bool isValid => errors.Count == 0;
        }
        
        [MenuItem("Card Game/Validate Card Prefabs")]
        public static void ShowWindow()
        {
            GetWindow<CardPrefabValidator>("Card Prefab Validator");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Card Prefab Validator", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            if (GUILayout.Button("Scan All Card Prefabs", GUILayout.Height(30)))
            {
                ScanAllPrefabs();
            }
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("Fix All Issues", GUILayout.Height(30)))
            {
                FixAllIssues();
            }
            
            GUILayout.Space(10);
            GUILayout.Label("Validation Results:", EditorStyles.boldLabel);
            
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            
            if (validationResults.Count == 0)
            {
                GUILayout.Label("No prefabs scanned yet. Click 'Scan All Card Prefabs' to begin.");
            }
            else
            {
                foreach (var result in validationResults)
                {
                    DrawValidationResult(result);
                }
            }
            
            GUILayout.EndScrollView();
        }
        
        private void DrawValidationResult(ValidationResult result)
        {
            EditorGUILayout.BeginVertical("box");
            
            string statusIcon = result.isValid ? "✓" : "✗";
            string statusColor = result.isValid ? "green" : "red";
            GUILayout.Label($"<color={statusColor}><b>{statusIcon}</b></color> {result.prefabPath}", EditorStyles.boldLabel);
            
            if (result.errors.Count > 0)
            {
                EditorGUILayout.BeginVertical("helpbox");
                GUILayout.Label("<color=red><b>Errors:</b></color>", EditorStyles.boldLabel);
                foreach (var error in result.errors)
                {
                    EditorGUILayout.LabelField("• " + error, EditorStyles.wordWrappedMiniLabel);
                }
                EditorGUILayout.EndVertical();
            }
            
            if (result.warnings.Count > 0)
            {
                EditorGUILayout.BeginVertical("helpbox");
                GUILayout.Label("<color=yellow><b>Warnings:</b></color>", EditorStyles.boldLabel);
                foreach (var warning in result.warnings)
                {
                    EditorGUILayout.LabelField("• " + warning, EditorStyles.wordWrappedMiniLabel);
                }
                EditorGUILayout.EndVertical();
            }
            
            EditorGUILayout.EndVertical();
            GUILayout.Space(5);
        }
        
        private void ScanAllPrefabs()
        {
            validationResults.Clear();
            
            // Find all prefabs in Assets/PreFabs folder
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/PreFabs" });
            
            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                
                if (prefab == null) continue;
                
                // Check if this is a card prefab
                if (prefab.name.Contains("Card") || prefab.GetComponent<NewCardUI>() != null)
                {
                    ValidationResult result = ValidatePrefab(prefab, path);
                    validationResults.Add(result);
                }
            }
            
            Debug.Log($"CardPrefabValidator: Scanned {validationResults.Count} card prefabs. Found {validationResults.Count(r => !r.isValid)} with issues.");
        }
        
        private ValidationResult ValidatePrefab(GameObject prefab, string path)
        {
            ValidationResult result = new ValidationResult
            {
                prefabPath = path,
                prefab = prefab
            };
            
            // Check for missing scripts
            if (HasMissingScripts(prefab))
            {
                result.errors.Add("Has missing MonoBehaviour references");
            }
            
            // Check for NewCardUI component
            NewCardUI cardUI = prefab.GetComponent<NewCardUI>();
            if (cardUI == null)
            {
                result.errors.Add("Missing NewCardUI component");
            }
            else
            {
                // Validate NewCardUI serialized fields
                ValidateNewCardUI(cardUI, result);
                
                // Check for CanvasGroup (required for drag-and-drop)
                if (cardUI.GetComponent<CanvasGroup>() == null)
                {
                    result.warnings.Add("NewCardUI missing CanvasGroup component (required for drag-and-drop)");
                }
            }
            
            // Check for CardMover (optional - only for board cards)
            CardMover cardMover = prefab.GetComponent<CardMover>();
            if (cardMover != null)
            {
                ValidateCardMover(cardMover, result);
            }
            
            // Check for CardBackVisual
            Transform cardBackVisual = prefab.transform.Find("CardBackVisual");
            if (cardBackVisual != null)
            {
                ValidateCardBackVisual(cardBackVisual.gameObject, result);
            }
            
            // Check required child objects
            ValidateChildObjects(prefab, result);
            
            return result;
        }
        
        private bool HasMissingScripts(GameObject obj)
        {
            #if UNITY_EDITOR
            // Check all components for missing scripts
            Component[] components = obj.GetComponents<Component>();
            foreach (Component comp in components)
            {
                if (comp == null)
                {
                    return true;
                }
            }
            
            // Check recursively in children
            foreach (Transform child in obj.transform)
            {
                if (HasMissingScripts(child.gameObject))
                {
                    return true;
                }
            }
            #endif
            return false;
        }
        
        private void ValidateNewCardUI(NewCardUI cardUI, ValidationResult result)
        {
            // Check critical serialized fields using reflection
            var cardNameTextField = typeof(NewCardUI).GetField("cardNameText", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var artworkField = typeof(NewCardUI).GetField("artwork", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (cardNameTextField != null)
            {
                var cardNameText = cardNameTextField.GetValue(cardUI);
                if (cardNameText == null)
                {
                    result.warnings.Add("NewCardUI.cardNameText is not assigned");
                }
            }
            
            if (artworkField != null)
            {
                var artwork = artworkField.GetValue(cardUI);
                if (artwork == null)
                {
                    result.warnings.Add("NewCardUI.artwork is not assigned");
                }
            }
            
            // Check for flip animation setup
            var flipAnimationField = typeof(NewCardUI).GetField("flipAnimation", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (flipAnimationField != null)
            {
                var flipAnimation = flipAnimationField.GetValue(cardUI);
                if (flipAnimation == null)
                {
                    result.warnings.Add("NewCardUI.flipAnimation is not assigned (flip animations won't work)");
                }
            }
        }
        
        private void ValidateCardMover(CardMover cardMover, ValidationResult result)
        {
            // CardMover.card should be null in prefab (will be set at runtime)
            // But we can check if the component is properly set up
            if (cardMover.GetComponent<Collider2D>() == null)
            {
                result.warnings.Add("CardMover requires a Collider2D for mouse interaction");
            }
        }
        
        private void ValidateCardBackVisual(GameObject cardBackVisual, ValidationResult result)
        {
            // Check for missing scripts
            if (HasMissingScripts(cardBackVisual))
            {
                result.errors.Add("CardBackVisual has missing MonoBehaviour references");
            }
            
            // Check if it has Image or SpriteRenderer
            if (cardBackVisual.GetComponent<UnityEngine.UI.Image>() == null && 
                cardBackVisual.GetComponent<SpriteRenderer>() == null)
            {
                result.warnings.Add("CardBackVisual should have Image (UI) or SpriteRenderer (2D) component");
            }
        }
        
        private void ValidateChildObjects(GameObject prefab, ValidationResult result)
        {
            // Check for FrontContainer and BackContainer (created at runtime if missing, but prefab should have them)
            Transform frontContainer = prefab.transform.Find("FrontContainer");
            Transform backContainer = prefab.transform.Find("BackContainer");
            
            if (frontContainer == null)
            {
                result.warnings.Add("FrontContainer child object not found (will be created at runtime)");
            }
            
            if (backContainer == null)
            {
                result.warnings.Add("BackContainer child object not found (will be created at runtime)");
            }
        }
        
        private void FixAllIssues()
        {
            int fixedCount = 0;
            
            foreach (var result in validationResults.Where(r => !r.isValid))
            {
                if (FixPrefab(result))
                {
                    fixedCount++;
                }
            }
            
            if (fixedCount > 0)
            {
                AssetDatabase.SaveAssets();
                Debug.Log($"CardPrefabValidator: Fixed {fixedCount} prefab(s).");
                EditorUtility.DisplayDialog("Fix Complete", $"Fixed {fixedCount} prefab(s).", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("No Fixes Needed", "No issues could be automatically fixed.", "OK");
            }
            
            // Rescan to show updated results
            ScanAllPrefabs();
        }
        
        private bool FixPrefab(ValidationResult result)
        {
            if (result.prefab == null) return false;
            
            bool modified = false;
            
            // Fix missing scripts
            #if UNITY_EDITOR
            if (result.errors.Contains("Has missing MonoBehaviour references"))
            {
                int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(result.prefab);
                if (removed > 0)
                {
                    modified = true;
                    Debug.Log($"Fixed {removed} missing script(s) on {result.prefabPath}");
                }
            }
            
            if (result.errors.Contains("CardBackVisual has missing MonoBehaviour references"))
            {
                Transform cardBackVisual = result.prefab.transform.Find("CardBackVisual");
                if (cardBackVisual != null)
                {
                    int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(cardBackVisual.gameObject);
                    if (removed > 0)
                    {
                        modified = true;
                        Debug.Log($"Fixed {removed} missing script(s) on CardBackVisual in {result.prefabPath}");
                    }
                }
            }
            
            // Auto-add missing CanvasGroup component
            if (result.warnings.Contains("NewCardUI missing CanvasGroup component (required for drag-and-drop)"))
            {
                NewCardUI cardUI = result.prefab.GetComponent<NewCardUI>();
                if (cardUI != null)
                {
                    CanvasGroup canvasGroup = cardUI.GetComponent<CanvasGroup>();
                    if (canvasGroup == null)
                    {
                        canvasGroup = cardUI.gameObject.AddComponent<CanvasGroup>();
                        modified = true;
                        Debug.Log($"Added CanvasGroup component to {result.prefabPath}");
                    }
                }
            }
            
            // Auto-add missing CardBackVisual Image/SpriteRenderer
            if (result.warnings.Contains("CardBackVisual should have Image (UI) or SpriteRenderer (2D) component"))
            {
                Transform cardBackVisual = result.prefab.transform.Find("CardBackVisual");
                if (cardBackVisual != null)
                {
                    GameObject cardBackVisualObj = cardBackVisual.gameObject;
                    
                    // Check if it's a UI card (has RectTransform)
                    if (result.prefab.GetComponent<RectTransform>() != null)
                    {
                        // UI card - add Image if missing
                        if (cardBackVisualObj.GetComponent<UnityEngine.UI.Image>() == null)
                        {
                            UnityEngine.UI.Image image = cardBackVisualObj.AddComponent<UnityEngine.UI.Image>();
                            image.color = new Color(0.3f, 0.3f, 0.3f, 1f); // Dark gray placeholder
                            modified = true;
                            Debug.Log($"Added Image component to CardBackVisual in {result.prefabPath}");
                        }
                    }
                    else
                    {
                        // 2D card - add SpriteRenderer if missing
                        if (cardBackVisualObj.GetComponent<SpriteRenderer>() == null)
                        {
                            SpriteRenderer spriteRenderer = cardBackVisualObj.AddComponent<SpriteRenderer>();
                            spriteRenderer.color = new Color(0.3f, 0.3f, 0.3f, 1f); // Dark gray placeholder
                            modified = true;
                            Debug.Log($"Added SpriteRenderer component to CardBackVisual in {result.prefabPath}");
                        }
                    }
                }
            }
            #endif
            
            if (modified)
            {
                EditorUtility.SetDirty(result.prefab);
                PrefabUtility.SavePrefabAsset(result.prefab);
            }
            
            return modified;
        }
    }
}

