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
            
            // Convert to proper Canvas if needed
            ConvertToCanvas(hudCanvas);
            
            // Get or add HUDManager component
            HUDManager hudManager = hudCanvas.GetComponent<HUDManager>();
            if (hudManager == null)
            {
                hudManager = hudCanvas.AddComponent<HUDManager>();
                Debug.Log("HUDSetup: Added HUDManager component to HUDOverlayCanvas");
            }
            
            // Ensure game managers exist
            EnsureGameManagers();
            
            // Find and wire up all the text labels using reflection
            WireUpHUDReferences(hudManager, hudCanvas.transform);
            
            Debug.Log("HUDSetup: HUD successfully configured!");
        }
        
        /// <summary>
        /// Ensure required game managers exist in the scene.
        /// </summary>
        private void EnsureGameManagers()
        {
            // Check for ScoreManager
            ScoreManager scoreManager = FindObjectOfType<ScoreManager>();
            if (scoreManager == null)
            {
                GameObject managerObj = new GameObject("ScoreManager");
                managerObj.AddComponent<ScoreManager>();
                Debug.Log("HUDSetup: Created ScoreManager");
            }
            
            // Check for GameEndManager
            var gameEndManager = FindObjectOfType<GameEndManager>();
            if (gameEndManager == null)
            {
                GameObject managerObj = new GameObject("GameEndManager");
                managerObj.AddComponent<GameEndManager>();
                Debug.Log("HUDSetup: Created GameEndManager");
            }
        }
        
        /// <summary>
        /// Convert GameObject to a proper UI Canvas if it isn't already.
        /// </summary>
        private void ConvertToCanvas(GameObject canvasObject)
        {
            // Check if it already has a Canvas component
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            if (canvas == null)
            {
                // Add Canvas component
                canvas = canvasObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100; // Render on top
                Debug.Log("HUDSetup: Added Canvas component");
            }
            
            // Add CanvasScaler if missing
            UnityEngine.UI.CanvasScaler scaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
            if (scaler == null)
            {
                scaler = canvasObject.AddComponent<UnityEngine.UI.CanvasScaler>();
                scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
                Debug.Log("HUDSetup: Added CanvasScaler component");
            }
            
            // Add GraphicRaycaster if missing
            UnityEngine.UI.GraphicRaycaster raycaster = canvasObject.GetComponent<UnityEngine.UI.GraphicRaycaster>();
            if (raycaster == null)
            {
                raycaster = canvasObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                Debug.Log("HUDSetup: Added GraphicRaycaster component");
            }
            
            // Ensure it has a RectTransform (should be automatic when Canvas is added)
            RectTransform rectTransform = canvasObject.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                // Set to fill parent
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.sizeDelta = Vector2.zero;
                rectTransform.anchoredPosition = Vector2.zero;
            }
        }
        
        /// <summary>
        /// Wire up all HUD references using reflection to set private serialized fields.
        /// </summary>
        private void WireUpHUDReferences(HUDManager hudManager, Transform hudRoot)
        {
            // Find or create panels (destroy and recreate if inactive)
            Transform p1Panel = hudRoot.Find("P1Panel");
            if (p1Panel != null && !p1Panel.gameObject.activeSelf)
            {
                Debug.Log("HUDSetup: Destroying inactive P1Panel");
                GameObject.DestroyImmediate(p1Panel.gameObject);
                p1Panel = null;
            }
            if (p1Panel == null)
            {
                p1Panel = CreatePlayerPanel(hudRoot, "P1Panel", true);
                Debug.Log("HUDSetup: Created P1Panel");
            }
            
            Transform p2Panel = hudRoot.Find("P2Panel");
            if (p2Panel != null && !p2Panel.gameObject.activeSelf)
            {
                Debug.Log("HUDSetup: Destroying inactive P2Panel");
                GameObject.DestroyImmediate(p2Panel.gameObject);
                p2Panel = null;
            }
            if (p2Panel == null)
            {
                p2Panel = CreatePlayerPanel(hudRoot, "P2Panel", false);
                Debug.Log("HUDSetup: Created P2Panel");
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
        /// Create a complete player panel with all UI elements.
        /// </summary>
        private Transform CreatePlayerPanel(Transform parent, string panelName, bool isPlayer1)
        {
            GameObject panel = new GameObject(panelName);
            panel.transform.SetParent(parent, false);
            panel.layer = 5; // UI layer
            
            // Add RectTransform and position - moved towards middle/center
            RectTransform rectTransform = panel.AddComponent<RectTransform>();
            if (isPlayer1)
            {
                // Left side, vertically centered, moved down to match P2
                rectTransform.anchorMin = new Vector2(0, 0.5f);
                rectTransform.anchorMax = new Vector2(0, 0.5f);
                rectTransform.pivot = new Vector2(0, 0.5f);
                rectTransform.anchoredPosition = new Vector2(15, -200); // Match P2 vertical position
            }
            else
            {
                // Right side, middle position moved down to clear test UI
                rectTransform.anchorMin = new Vector2(1, 0.5f);
                rectTransform.anchorMax = new Vector2(1, 0.5f);
                rectTransform.pivot = new Vector2(1, 0.5f);
                rectTransform.anchoredPosition = new Vector2(-15, -200); // Middle-right, moved down 200px
            }
            rectTransform.sizeDelta = new Vector2(200, 105);
            
            // Add Image for background with better styling
            UnityEngine.UI.Image image = panel.AddComponent<UnityEngine.UI.Image>();
            image.color = new Color(0.08f, 0.08f, 0.12f, 0.88f);
            
            // Add VerticalLayoutGroup
            UnityEngine.UI.VerticalLayoutGroup layout = panel.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.spacing = 7;
            layout.childAlignment = isPlayer1 ? TextAnchor.UpperLeft : TextAnchor.UpperRight;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            
            // Create text labels with better sizing
            CreateTextLabel(panel.transform, "PlayerLabel", isPlayer1 ? "Player 1" : "Player 2", 16, true, isPlayer1);
            CreateTextLabel(panel.transform, "ScoreLabel", "Score: 0", 15, false, isPlayer1);
            CreateTextLabel(panel.transform, "HandDeckLabel", "Hand: 0 | Deck: 0", 13, false, isPlayer1);
            
            return panel.transform;
        }
        
        /// <summary>
        /// Create a text label for the panel.
        /// </summary>
        private void CreateTextLabel(Transform parent, string name, string text, float fontSize, bool bold, bool leftAlign)
        {
            GameObject label = new GameObject(name);
            label.transform.SetParent(parent, false);
            label.layer = 5;
            
            RectTransform rectTransform = label.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(0, fontSize + 10);
            
            // Use TextMeshProUGUI instead of TMP_Text (which is abstract)
            TMPro.TextMeshProUGUI tmpText = label.AddComponent<TMPro.TextMeshProUGUI>();
            tmpText.text = text;
            tmpText.fontSize = fontSize;
            tmpText.fontStyle = bold ? TMPro.FontStyles.Bold : TMPro.FontStyles.Normal;
            tmpText.alignment = leftAlign ? TMPro.TextAlignmentOptions.Left : TMPro.TextAlignmentOptions.Right;
            tmpText.color = new Color(1f, 1f, 1f, 0.95f); // Slightly transparent white for softer look
            tmpText.enableAutoSizing = false;
            tmpText.fontStyle |= TMPro.FontStyles.Normal;
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

