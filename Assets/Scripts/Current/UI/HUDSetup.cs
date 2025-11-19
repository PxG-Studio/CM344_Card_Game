using UnityEngine;
using TMPro;
using CardGame.Managers;

namespace CardGame.UI
{
    /// <summary>
    /// Auto-setup script that ensures HUDManager is properly configured on scene load.
    /// This script finds all HUD elements and wires them up automatically.
    /// </summary>
    [DefaultExecutionOrder(-100)] // Execute early
    public class HUDSetup : MonoBehaviour
    {
        [Header("Auto-Setup Settings")]
        [SerializeField] private bool autoSetupOnAwake = true;
        
        private void Awake()
        {
            if (autoSetupOnAwake)
            {
                SetupHUD();
            }
        }
        
        /// <summary>
        /// Automatically find and wire up the HUD components.
        /// </summary>
        [ContextMenu("Setup HUD")]
        public void SetupHUD()
        {
            // Find the HUDOverlayCanvas
            GameObject hudCanvas = GameObject.Find("HUDOverlayCanvas");
            if (hudCanvas == null)
            {
                Debug.LogError("HUDSetup: Could not find HUDOverlayCanvas!");
                return;
            }
            
            // Get or add HUDManager component
            HUDManager hudManager = hudCanvas.GetComponent<HUDManager>();
            if (hudManager == null)
            {
                hudManager = hudCanvas.AddComponent<HUDManager>();
                Debug.Log("HUDSetup: Added HUDManager component to HUDOverlayCanvas");
            }
            
            // Find and wire up all the text labels using reflection
            WireUpHUDReferences(hudManager, hudCanvas.transform);
            
            Debug.Log("HUDSetup: HUD successfully configured!");
        }
        
        /// <summary>
        /// Wire up all HUD references using reflection to set private serialized fields.
        /// </summary>
        private void WireUpHUDReferences(HUDManager hudManager, Transform hudRoot)
        {
            // Find all the UI elements
            Transform p1Panel = hudRoot.Find("P1Panel");
            Transform p2Panel = hudRoot.Find("P2Panel");
            
            if (p1Panel == null || p2Panel == null)
            {
                Debug.LogError("HUDSetup: Could not find P1Panel or P2Panel!");
                return;
            }
            
            // Find text labels
            TMP_Text p1ScoreLabel = p1Panel.Find("ScoreLabel")?.GetComponent<TMP_Text>();
            TMP_Text p1HandDeckLabel = p1Panel.Find("HandDeckLabel")?.GetComponent<TMP_Text>();
            TMP_Text p1PlayerLabel = p1Panel.Find("PlayerLabel")?.GetComponent<TMP_Text>();
            TMP_Text p2ScoreLabel = p2Panel.Find("ScoreLabel")?.GetComponent<TMP_Text>();
            TMP_Text p2HandDeckLabel = p2Panel.Find("HandDeckLabel")?.GetComponent<TMP_Text>();
            TMP_Text p2PlayerLabel = p2Panel.Find("PlayerLabel")?.GetComponent<TMP_Text>();
            TMP_Text tilesRemainingLabel = hudRoot.Find("TilesRemainingLabel")?.GetComponent<TMP_Text>();
            
            // Find or create turn indicators
            UnityEngine.UI.Image p1TurnIndicator = FindOrCreateTurnIndicator(p1Panel, "TurnIndicator");
            UnityEngine.UI.Image p2TurnIndicator = FindOrCreateTurnIndicator(p2Panel, "TurnIndicator");
            
            // Find deck managers
            NewDeckManager player1DeckManager = FindObjectOfType<NewDeckManager>();
            NewDeckManagerOpp player2DeckManager = FindObjectOfType<NewDeckManagerOpp>();
            
            // Use reflection to set the private serialized fields
            var hudType = typeof(HUDManager);
            
            SetPrivateField(hudManager, hudType, "p1ScoreLabel", p1ScoreLabel);
            SetPrivateField(hudManager, hudType, "p1HandDeckLabel", p1HandDeckLabel);
            SetPrivateField(hudManager, hudType, "p1PlayerLabel", p1PlayerLabel);
            SetPrivateField(hudManager, hudType, "p1TurnIndicator", p1TurnIndicator);
            SetPrivateField(hudManager, hudType, "p2ScoreLabel", p2ScoreLabel);
            SetPrivateField(hudManager, hudType, "p2HandDeckLabel", p2HandDeckLabel);
            SetPrivateField(hudManager, hudType, "p2PlayerLabel", p2PlayerLabel);
            SetPrivateField(hudManager, hudType, "p2TurnIndicator", p2TurnIndicator);
            SetPrivateField(hudManager, hudType, "tilesRemainingLabel", tilesRemainingLabel);
            SetPrivateField(hudManager, hudType, "player1DeckManager", player1DeckManager);
            SetPrivateField(hudManager, hudType, "player2DeckManager", player2DeckManager);
            
            Debug.Log($"HUDSetup: Wired up references - " +
                     $"P1Score: {p1ScoreLabel != null}, " +
                     $"P1HandDeck: {p1HandDeckLabel != null}, " +
                     $"P1Turn: {p1TurnIndicator != null}, " +
                     $"P2Score: {p2ScoreLabel != null}, " +
                     $"P2HandDeck: {p2HandDeckLabel != null}, " +
                     $"P2Turn: {p2TurnIndicator != null}, " +
                     $"TilesRemaining: {tilesRemainingLabel != null}, " +
                     $"DeckMgr1: {player1DeckManager != null}, " +
                     $"DeckMgr2: {player2DeckManager != null}");
        }
        
        /// <summary>
        /// Find or create a turn indicator Image component.
        /// </summary>
        private UnityEngine.UI.Image FindOrCreateTurnIndicator(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                UnityEngine.UI.Image existingImage = existing.GetComponent<UnityEngine.UI.Image>();
                if (existingImage != null)
                {
                    return existingImage;
                }
            }
            
            // Create new turn indicator as child of PlayerLabel if it exists
            Transform playerLabel = parent.Find("PlayerLabel");
            Transform indicatorParent = playerLabel != null ? playerLabel : parent;
            
            GameObject indicator = new GameObject(name);
            indicator.transform.SetParent(indicatorParent, false);
            indicator.layer = 5; // UI layer
            
            // Add RectTransform
            RectTransform rectTransform = indicator.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 0.5f);
            rectTransform.anchorMax = new Vector2(0, 0.5f);
            rectTransform.pivot = new Vector2(0, 0.5f);
            rectTransform.anchoredPosition = new Vector2(-5, 0);
            rectTransform.sizeDelta = new Vector2(8, 20);
            
            // Add Image component
            UnityEngine.UI.Image image = indicator.AddComponent<UnityEngine.UI.Image>();
            image.color = new Color(0.3f, 0.3f, 0.3f, 0.3f); // Default inactive color
            
            Debug.Log($"HUDSetup: Created turn indicator '{name}' under {indicatorParent.name}");
            return image;
        }
        
        /// <summary>
        /// Set a private serialized field using reflection.
        /// </summary>
        private void SetPrivateField(object target, System.Type type, string fieldName, object value)
        {
            var field = type.GetField(fieldName, 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
            
            if (field != null)
            {
                field.SetValue(target, value);
            }
            else
            {
                Debug.LogWarning($"HUDSetup: Could not find field '{fieldName}' in {type.Name}");
            }
        }
    }
}

