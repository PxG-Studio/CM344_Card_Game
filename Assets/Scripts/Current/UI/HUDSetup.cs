using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using CardGame.Managers;
using CardGame.Visuals;

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
        
        private static bool hasBeenSetup = false;
        private static int setupFrame = -1;
        
        private void Awake()
        {
            // Prevent duplicate setup on domain reload or multiple HUDSetup instances
            // Only setup once per session, or if this is a new frame (scene reload)
            int currentFrame = Time.frameCount;
            
            if (hasBeenSetup && setupFrame == currentFrame)
            {
                Debug.Log("HUDSetup: Already setup this frame. Skipping duplicate setup.");
                return;
            }
            
            if (autoSetupOnAwake)
            {
                SetupHUD();
                hasBeenSetup = true;
                setupFrame = currentFrame;
            }
        }
        
        private void OnDestroy()
        {
            // Reset flag when HUDSetup is destroyed (scene unload)
            // This allows setup to happen again if scene is reloaded
            if (!Application.isPlaying)
            {
                hasBeenSetup = false;
                setupFrame = -1;
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
            
            // Ensure supporting managers & board visuals exist
            EnsureGameManagers();
            EnsureFateFlowController();
            EnsureEventSystem();
            CleanupMissingScripts();
            SetupBoardBackdrop();
            
            // Find and wire up all the text labels using reflection
            WireUpHUDReferences(hudManager, hudCanvas.transform);
            
            // Setup Game End UI
            SetupGameEndUI(hudCanvas.transform);
            
            Debug.Log("HUDSetup: HUD successfully configured!");
        }
        
        /// <summary>
        /// Ensure required game managers exist in the scene.
        /// </summary>
        private void EnsureGameManagers()
        {
            // Check for GameManager (singleton, persists across scenes)
            if (GameManager.Instance == null)
            {
                GameObject managerObj = new GameObject("GameManager");
                managerObj.AddComponent<GameManager>();
                Debug.Log("HUDSetup: Created GameManager");
            }
            
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
        /// Ensure EventSystem exists for UI interactions (drag and drop).
        /// </summary>
        private void EnsureEventSystem()
        {
            if (EventSystem.current == null)
            {
                GameObject eventSystemObj = GameObject.Find("EventSystem");
                if (eventSystemObj == null)
                {
                    eventSystemObj = new GameObject("EventSystem");
                    eventSystemObj.AddComponent<EventSystem>();
                    eventSystemObj.AddComponent<StandaloneInputModule>();
                    Debug.Log("HUDSetup: Created EventSystem for UI interactions");
                }
                else if (eventSystemObj.GetComponent<EventSystem>() == null)
                {
                    eventSystemObj.AddComponent<EventSystem>();
                    if (eventSystemObj.GetComponent<StandaloneInputModule>() == null)
                    {
                        eventSystemObj.AddComponent<StandaloneInputModule>();
                    }
                    Debug.Log("HUDSetup: Added EventSystem components to existing GameObject");
                }
            }
        }
        
        /// <summary>
        /// [CardFront] Clean up missing script references on CardBackVisual GameObjects in scene.
        /// Note: Prefab assets should be cleaned using Editor tools (CardPrefabValidator or CleanupMissingScripts).
        /// </summary>
        private void CleanupMissingScripts()
        {
            #if UNITY_EDITOR
            if (!Application.isPlaying) return; // Only clean scene instances at runtime
            
            int fixedCount = 0;
            
            // [CardFront] Cluster approach: Clean scene instances only
            // Prefab assets must be cleaned in Editor before runtime
            GameObject[] allObjects = FindObjectsOfType<GameObject>(true);
            
            foreach (GameObject obj in allObjects)
            {
                if (obj != null && obj.name == "CardBackVisual")
                {
                    int removedCount = UnityEditor.GameObjectUtility.RemoveMonoBehavioursWithMissingScript(obj);
                    if (removedCount > 0)
                    {
                        fixedCount += removedCount;
                    }
                }
            }
            
            if (fixedCount > 0)
            {
                Debug.Log($"[HUDSetup] Cleaned up {fixedCount} missing script reference(s) from CardBackVisual scene instances");
            }
            #endif
        }
        
        private void EnsureFateFlowController()
        {
            FateFlowController controller = FindObjectOfType<FateFlowController>();
            if (controller == null)
            {
                GameObject fateObj = new GameObject("FateFlowController");
                fateObj.AddComponent<FateFlowController>();
                Debug.Log("HUDSetup: Created FateFlowController");
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
            if (p2Panel != null)
            {
                Debug.Log("HUDSetup: Destroying existing P2Panel to recreate with new position");
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
            
            // Create turn indicators above each panel (like develop-5)
            TurnIndicatorUI p1TurnIndicator = FindOrCreateTurnIndicator(p1Panel, "TurnIndicator", true);
            TurnIndicatorUI p2TurnIndicator = FindOrCreateTurnIndicator(p2Panel, "TurnIndicator", false);
            
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
                // Left of center
                rectTransform.anchorMin = new Vector2(0.5f, 1);
                rectTransform.anchorMax = new Vector2(0.5f, 1);
                rectTransform.pivot = new Vector2(1, 1); // Right pivot so it grows left from center
                rectTransform.anchoredPosition = new Vector2(-120, -80); // 120px left of center
            }
            else
            {
                // Right of center
                rectTransform.anchorMin = new Vector2(0.5f, 1);
                rectTransform.anchorMax = new Vector2(0.5f, 1);
                rectTransform.pivot = new Vector2(0, 1); // Left pivot so it grows right from center
                rectTransform.anchoredPosition = new Vector2(120, -80); // 120px right of center
            }
            rectTransform.sizeDelta = new Vector2(200, 105);
            
            // Add Image for background with better styling
            UnityEngine.UI.Image image = panel.AddComponent<UnityEngine.UI.Image>();
            image.color = new Color(0.08f, 0.08f, 0.12f, 0.88f);
            
            // Add VerticalLayoutGroup
            UnityEngine.UI.VerticalLayoutGroup layout = panel.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.spacing = 7;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            
            // Create text labels with better sizing
            CreateTextLabel(panel.transform, "PlayerLabel", isPlayer1 ? "Player 1" : "Player 2", 16, true, true);
            CreateTextLabel(panel.transform, "ScoreLabel", "Score: 0", 15, false, true);
            CreateTextLabel(panel.transform, "HandDeckLabel", "Hand: 0 | Deck: 0", 13, false, true);
            
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
        /// Find or create a rotating triangle UI indicator that hovers above the panel.
        /// </summary>
        private TurnIndicatorUI FindOrCreateTurnIndicator(Transform parent, string name, bool isPlayer1)
        {
            // Check if indicator already exists under parent
            Transform existing = parent.Find($"{name}_UI");
            if (existing != null && existing.GetComponent<TurnIndicatorUI>() != null)
            {
                return existing.GetComponent<TurnIndicatorUI>();
            }
            
            // Create UI diamond indicator as a child of the player panel
            GameObject indicatorUI = new GameObject($"{name}_UI");
            indicatorUI.layer = 5; // UI layer
            indicatorUI.transform.SetParent(parent, false);
            
            // Add RectTransform for UI positioning relative to the panel
            RectTransform rectUI = indicatorUI.AddComponent<RectTransform>();
            rectUI.anchorMin = new Vector2(0.5f, 1f);
            rectUI.anchorMax = new Vector2(0.5f, 1f);
            rectUI.pivot = new Vector2(0.5f, 0f); // Sit just above the top edge
            rectUI.anchoredPosition = new Vector2(0f, 20f); // Raised one unit higher (was 10f, now 20f)
            rectUI.sizeDelta = new Vector2(30f, 30f);

            // Prevent panel layout group from moving this indicator
            UnityEngine.UI.LayoutElement layoutElement = indicatorUI.AddComponent<UnityEngine.UI.LayoutElement>();
            layoutElement.ignoreLayout = true;
            
            // Add TextMeshPro component for the triangle indicator
            TMPro.TextMeshProUGUI textIndicator = indicatorUI.AddComponent<TMPro.TextMeshProUGUI>();
            textIndicator.text = "▼"; // Down-pointing triangle (inverted pyramid)
            textIndicator.fontSize = 48;
            textIndicator.color = new Color(1f, 0.8f, 0f, 1f); // Gold color
            textIndicator.alignment = TMPro.TextAlignmentOptions.Center;
            textIndicator.fontStyle = TMPro.FontStyles.Bold;
            textIndicator.raycastTarget = false;
            
            // Add the UI indicator component
            TurnIndicatorUI indicatorScript = indicatorUI.AddComponent<TurnIndicatorUI>();
            indicatorScript.SetActive(false); // Start inactive
            
            string position = isPlayer1 ? "above Player 1 panel" : "above Player 2 panel";
            Debug.Log($"HUDSetup: Created UI triangle indicator '{name}_UI' {position}");
            return indicatorScript;
        }
        
        /// <summary>
        /// Find or create a single moving turn indicator that travels between P1 and P2 panels.
        /// </summary>
        private TurnIndicatorMoving FindOrCreateMovingTurnIndicator(Transform hudRoot, Transform p1Panel, Transform p2Panel)
        {
            // Check if moving indicator already exists
            TurnIndicatorMoving existing = hudRoot.GetComponentInChildren<TurnIndicatorMoving>();
            if (existing != null)
            {
                // Update panel references
                existing.SetPanels(p1Panel.GetComponent<RectTransform>(), p2Panel.GetComponent<RectTransform>());
                return existing;
            }
            
            // Create moving indicator as a child of HUD root
            GameObject movingIndicatorObj = new GameObject("MovingTurnIndicator");
            movingIndicatorObj.layer = 5; // UI layer
            movingIndicatorObj.transform.SetParent(hudRoot, false);
            
            // Add RectTransform - positioned above panels like in develop-5
            RectTransform rectTransform = movingIndicatorObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 1f); // Top center anchor
            rectTransform.anchorMax = new Vector2(0.5f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 0f); // Pivot at bottom (sits above panels)
            rectTransform.sizeDelta = new Vector2(40f, 40f);
            // Start position: above P1 panel (will be updated by SetPanels)
            // P1 panel is at -120 from center, so indicator starts there
            rectTransform.anchoredPosition = new Vector2(-120f, 10f); // Above P1 panel, 10px offset
            
            // Add TextMeshPro component for the triangle indicator
            TMPro.TextMeshProUGUI textIndicator = movingIndicatorObj.AddComponent<TMPro.TextMeshProUGUI>();
            textIndicator.text = "▼"; // Down-pointing triangle (inverted pyramid)
            textIndicator.fontSize = 48;
            textIndicator.color = new Color(1f, 0.8f, 0f, 1f); // Gold color
            textIndicator.alignment = TMPro.TextAlignmentOptions.Center;
            textIndicator.fontStyle = TMPro.FontStyles.Bold;
            
            // Add the moving indicator component
            TurnIndicatorMoving movingIndicator = movingIndicatorObj.AddComponent<TurnIndicatorMoving>();
            
            // Set panel references
            movingIndicator.SetPanels(p1Panel.GetComponent<RectTransform>(), p2Panel.GetComponent<RectTransform>());
            
            Debug.Log("HUDSetup: Created moving turn indicator with figure-eight pattern");
            return movingIndicator;
        }
        
        /// <summary>
        /// Setup custom cursor from Deck Slot or other cursor GameObject.
        /// </summary>
        private void SetupCustomCursor()
        {
            // Check if CustomCursor already exists
            CustomCursor existingCursor = FindObjectOfType<CustomCursor>();
            if (existingCursor != null)
            {
                Debug.Log("HUDSetup: CustomCursor already exists in scene");
                return;
            }
            
            // Try to find the cursor GameObject (Deck Slot or renamed version)
            GameObject cursorGameObject = null;
            string[] possibleNames = { "CustomCursor", "GameCursor", "Cursor", "Pointer", "InteractivePointer", "UICursor", "Deck Slot" };
            
            foreach (string name in possibleNames)
            {
                cursorGameObject = GameObject.Find(name);
                if (cursorGameObject != null)
                {
                    // Rename to CustomCursor if it's still "Deck Slot"
                    if (cursorGameObject.name == "Deck Slot")
                    {
                        cursorGameObject.name = "CustomCursor";
                        Debug.Log("HUDSetup: Renamed 'Deck Slot' to 'CustomCursor'");
                    }
                    break;
                }
            }
            
            int ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
            
            // Always reset cursor manager for a clean state
            GameObject existingManager = GameObject.Find("CursorManager");
            if (existingManager != null)
            {
                Destroy(existingManager);
            }
            
            GameObject cursorManager = new GameObject("CursorManager");
            cursorManager.transform.SetParent(null);
            DontDestroyOnLoad(cursorManager);
            if (ignoreRaycastLayer >= 0)
            {
                cursorManager.layer = ignoreRaycastLayer;
            }
            
            // Clean up any old generated cursor sprites that might still be in the scene
            GameObject strayCursorSprite = GameObject.Find("GeneratedCursorSprite");
            if (strayCursorSprite != null)
            {
                Destroy(strayCursorSprite);
            }
            
            CustomCursor customCursor = cursorManager.AddComponent<CustomCursor>();
            var cursorType = typeof(CustomCursor);
            
            // Configure cursor UI visual to match turn indicator
            SetPrivateField(customCursor, cursorType, "useUICursorVisual", true);
            SetPrivateField(customCursor, cursorType, "uiCursorGlyph", "▲");
            SetPrivateField(customCursor, cursorType, "uiCursorSize", 48f);
            SetPrivateField(customCursor, cursorType, "uiCursorColor", new Color(1f, 0.8f, 0f, 1f));
            SetPrivateField(customCursor, cursorType, "tintGreen", false);
            
            if (cursorGameObject != null)
            {
                // Disable anything that could interfere
                DisableAllInputComponents(cursorGameObject, "setting up custom cursor");
                DisableInputScripts(cursorGameObject);
                if (ignoreRaycastLayer >= 0)
                {
                    SetLayerRecursive(cursorGameObject, ignoreRaycastLayer);
                }
                
                Sprite cursorSprite = ExtractSpriteFromGameObject(cursorGameObject);
                if (cursorSprite == null)
                {
                    Debug.LogWarning("HUDSetup: Cursor GameObject found but no sprite found. Using fallback cursor sprite.");
                    SetPrivateField(customCursor, cursorType, "cursorSprite", CreateFallbackCursorSprite());
                }
                else
                {
                    SetPrivateField(customCursor, cursorType, "cursorSprite", cursorSprite);
                }
                
                cursorGameObject.SetActive(false);
                Debug.Log("HUDSetup: Deactivated cursor GameObject to prevent all input interference");
                
                // Permanently remove the scene copy so it never renders on the board again
                Destroy(cursorGameObject);
                Debug.Log("HUDSetup: Destroyed source cursor GameObject after extracting its sprite");
            }
            else
            {
                Debug.Log("HUDSetup: No cursor GameObject found. Using fallback cursor sprite.");
                SetPrivateField(customCursor, cursorType, "cursorSprite", CreateFallbackCursorSprite());
            }
            Debug.Log("HUDSetup: Created CustomCursor component (fully isolated from input system)");
        }
        
        /// <summary>
        /// Extracts sprite from a GameObject, checking multiple sources.
        /// </summary>
        private Sprite ExtractSpriteFromGameObject(GameObject obj)
        {
            if (obj == null) return null;
            
            // Try SpriteRenderer first
            SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                return sr.sprite;
            }
            
            // Try UI Image
            UnityEngine.UI.Image img = obj.GetComponent<UnityEngine.UI.Image>();
            if (img != null && img.sprite != null)
            {
                return img.sprite;
            }
            
            // Try children SpriteRenderer
            sr = obj.GetComponentInChildren<SpriteRenderer>(true);
            if (sr != null && sr.sprite != null)
            {
                return sr.sprite;
            }
            
            // Try children UI Image
            img = obj.GetComponentInChildren<UnityEngine.UI.Image>(true);
            if (img != null && img.sprite != null)
            {
                return img.sprite;
            }
            
            return null;
        }
        
        /// <summary>
        /// Disables all input-related components on a GameObject and its children.
        /// </summary>
        private void DisableAllInputComponents(GameObject obj, string reason = "")
        {
            if (obj == null) return;
            
            // Disable all colliders
            Collider2D[] colliders2D = obj.GetComponentsInChildren<Collider2D>(true);
            foreach (var col in colliders2D) col.enabled = false;
            
            Collider[] colliders3D = obj.GetComponentsInChildren<Collider>(true);
            foreach (var col in colliders3D) col.enabled = false;
            
            // Disable all UI raycast targets
            UnityEngine.UI.Graphic[] graphics = obj.GetComponentsInChildren<UnityEngine.UI.Graphic>(true);
            foreach (var graphic in graphics)
            {
                if (graphic is UnityEngine.UI.Image img) img.raycastTarget = false;
                else if (graphic is UnityEngine.UI.Text txt) txt.raycastTarget = false;
                else if (graphic is UnityEngine.UI.RawImage rawImg) rawImg.raycastTarget = false;
            }
            
            // Disable all CanvasGroups
            CanvasGroup[] canvasGroups = obj.GetComponentsInChildren<CanvasGroup>(true);
            foreach (var cg in canvasGroups)
            {
                cg.blocksRaycasts = false;
                cg.interactable = false;
            }
            
            // Move to Ignore Raycast layer
            int ignoreLayer = LayerMask.NameToLayer("Ignore Raycast");
            if (ignoreLayer >= 0)
            {
                SetLayerRecursive(obj, ignoreLayer);
            }
            
            if (!string.IsNullOrEmpty(reason))
            {
                Debug.Log($"HUDSetup: Disabled all input components on '{obj.name}' ({reason})");
            }
        }

        /// <summary>
        /// Disables common input scripts (CardMover, NewCardUI, etc.) on the cursor source.
        /// </summary>
        private void DisableInputScripts(GameObject obj)
        {
            if (obj == null) return;
            
            MonoBehaviour[] behaviours = obj.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var behaviour in behaviours)
            {
                if (behaviour is CustomCursor) continue;
                
                string typeName = behaviour.GetType().Name;
                if (typeName.Contains("Input") ||
                    typeName.Contains("Mouse") ||
                    typeName.Contains("Pointer") ||
                    typeName.Contains("Click") ||
                    typeName.Contains("CardMover") ||
                    typeName.Contains("NewCardUI"))
                {
                    behaviour.enabled = false;
                }
            }
        }

        /// <summary>
        /// Sets the layer recursively on a GameObject and all its children.
        /// </summary>
        private void SetLayerRecursive(GameObject obj, int layer)
        {
            if (obj == null) return;
            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursive(child.gameObject, layer);
            }
        }

        /// <summary>
        /// Creates a fallback triangle cursor sprite (pointer) if no sprite is available.
        /// </summary>
        private Sprite CreateFallbackCursorSprite()
        {
            int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

            Color color = new Color(1f, 0.8f, 0f, 1f);
            int center = size / 2;

            for (int y = 0; y < size; y++)
            {
            float normalizedY = (float)y / (size - 1); // 0 at top, 1 at bottom
                int halfWidth = Mathf.RoundToInt(normalizedY * center);
                for (int x = 0; x < size; x++)
                {
                    if (Mathf.Abs(x - center) <= halfWidth)
                    {
                        pixels[y * size + x] = color;
                    }
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            texture.filterMode = FilterMode.Bilinear;

            // Pivot at bottom center so hotspot aligns with the upward point
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0f), 100f);
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
        
        /// <summary>
        /// Setup the Game End UI panel
        /// </summary>
        private void SetupGameEndUI(Transform hudRoot)
        {
            // Check if GameEndUI already exists
            GameEndUI existingUI = hudRoot.GetComponentInChildren<GameEndUI>(true);
            if (existingUI != null)
            {
                Debug.Log("HUDSetup: GameEndUI already exists");
                return;
            }
            
            // Create Game End Panel
            GameObject endPanel = new GameObject("GameEndPanel");
            endPanel.transform.SetParent(hudRoot, false);
            endPanel.layer = 5; // UI layer
            
            RectTransform endPanelRect = endPanel.AddComponent<RectTransform>();
            endPanelRect.anchorMin = Vector2.zero;
            endPanelRect.anchorMax = Vector2.one;
            endPanelRect.sizeDelta = Vector2.zero;
            endPanelRect.anchoredPosition = Vector2.zero;
            
            // Add semi-transparent background
            UnityEngine.UI.Image bgImage = endPanel.AddComponent<UnityEngine.UI.Image>();
            bgImage.color = new Color(0f, 0f, 0f, 0.85f);
            
            // Create content panel (centered)
            GameObject contentPanel = new GameObject("ContentPanel");
            contentPanel.transform.SetParent(endPanel.transform, false);
            
            RectTransform contentRect = contentPanel.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.5f, 0.5f);
            contentRect.anchorMax = new Vector2(0.5f, 0.5f);
            contentRect.pivot = new Vector2(0.5f, 0.5f);
            contentRect.sizeDelta = new Vector2(600, 400);
            contentRect.anchoredPosition = Vector2.zero;
            
            // Add background to content panel
            UnityEngine.UI.Image contentBg = contentPanel.AddComponent<UnityEngine.UI.Image>();
            contentBg.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);
            
            // Add vertical layout
            UnityEngine.UI.VerticalLayoutGroup layout = contentPanel.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            layout.padding = new RectOffset(40, 40, 40, 40);
            layout.spacing = 30;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            
            // Create Winner Text
            GameObject winnerTextObj = new GameObject("WinnerText");
            winnerTextObj.transform.SetParent(contentPanel.transform, false);
            
            TextMeshProUGUI winnerText = winnerTextObj.AddComponent<TextMeshProUGUI>();
            winnerText.text = "PLAYER WINS!";
            winnerText.fontSize = 48;
            winnerText.fontStyle = FontStyles.Bold;
            winnerText.alignment = TextAlignmentOptions.Center;
            winnerText.color = Color.white;
            
            RectTransform winnerRect = winnerTextObj.GetComponent<RectTransform>();
            winnerRect.sizeDelta = new Vector2(0, 80);
            
            // Create Final Score Text
            GameObject scoreTextObj = new GameObject("FinalScoreText");
            scoreTextObj.transform.SetParent(contentPanel.transform, false);
            
            TextMeshProUGUI scoreText = scoreTextObj.AddComponent<TextMeshProUGUI>();
            scoreText.text = "Final Score\nPlayer 1: 0  |  Player 2: 0";
            scoreText.fontSize = 28;
            scoreText.alignment = TextAlignmentOptions.Center;
            scoreText.color = new Color(0.9f, 0.9f, 0.9f, 1f);
            
            RectTransform scoreRect = scoreTextObj.GetComponent<RectTransform>();
            scoreRect.sizeDelta = new Vector2(0, 80);
            
            // Create Restart Button
            GameObject restartBtnObj = CreateButton(contentPanel.transform, "RestartButton", "Play Again");
            
            // Create Quit Button
            GameObject quitBtnObj = CreateButton(contentPanel.transform, "QuitButton", "Quit");

            // Create Persona-style Victory Cut-In overlay
            GameObject cutInObj = new GameObject("VictoryCutIn");
            cutInObj.transform.SetParent(hudRoot, false);
            
            RectTransform cutInRect = cutInObj.AddComponent<RectTransform>();
            cutInRect.anchorMin = new Vector2(0.5f, 0.5f);
            cutInRect.anchorMax = new Vector2(0.5f, 0.5f);
            cutInRect.pivot = new Vector2(0.5f, 0.5f);
            cutInRect.sizeDelta = new Vector2(1200f, 260f);
            cutInRect.anchoredPosition = new Vector2(0f, 140f);
            cutInRect.localRotation = Quaternion.Euler(0f, 0f, -8f);
            
            CanvasGroup cutInCanvasGroup = cutInObj.AddComponent<CanvasGroup>();
            cutInCanvasGroup.alpha = 0f;
            
            Image cutInBackground = cutInObj.AddComponent<Image>();
            cutInBackground.color = new Color(0.05f, 0.05f, 0.05f, 0.95f);
            cutInBackground.raycastTarget = false;
            
            GameObject pulseObj = new GameObject("AccentPulse");
            pulseObj.transform.SetParent(cutInObj.transform, false);
            pulseObj.transform.SetSiblingIndex(0);
            RectTransform pulseRect = pulseObj.AddComponent<RectTransform>();
            pulseRect.anchorMin = new Vector2(0f, 0f);
            pulseRect.anchorMax = new Vector2(1f, 1f);
            pulseRect.sizeDelta = Vector2.zero;
            Image pulseImage = pulseObj.AddComponent<Image>();
            pulseImage.color = new Color(1f, 0.55f, 0.2f, 0f);
            pulseImage.raycastTarget = false;
            
            GameObject shadowTextObj = new GameObject("CutInShadowText");
            shadowTextObj.transform.SetParent(cutInObj.transform, false);
            RectTransform shadowRect = shadowTextObj.AddComponent<RectTransform>();
            shadowRect.anchorMin = new Vector2(0.5f, 0.5f);
            shadowRect.anchorMax = new Vector2(0.5f, 0.5f);
            shadowRect.sizeDelta = new Vector2(1100f, 200f);
            shadowRect.anchoredPosition = new Vector2(8f, -8f);
            TextMeshProUGUI shadowTMP = shadowTextObj.AddComponent<TextMeshProUGUI>();
            shadowTMP.text = "PLAYER 1 WINS!";
            shadowTMP.fontSize = 96;
            shadowTMP.fontStyle = FontStyles.Bold;
            shadowTMP.alignment = TextAlignmentOptions.Center;
            shadowTMP.color = new Color(0f, 0f, 0f, 0.7f);
            shadowTMP.raycastTarget = false;
            
            GameObject mainTextObj = new GameObject("CutInMainText");
            mainTextObj.transform.SetParent(cutInObj.transform, false);
            RectTransform mainRect = mainTextObj.AddComponent<RectTransform>();
            mainRect.anchorMin = new Vector2(0.5f, 0.5f);
            mainRect.anchorMax = new Vector2(0.5f, 0.5f);
            mainRect.sizeDelta = new Vector2(1100f, 200f);
            mainRect.anchoredPosition = Vector2.zero;
            TextMeshProUGUI mainTMP = mainTextObj.AddComponent<TextMeshProUGUI>();
            mainTMP.text = "PLAYER 1 WINS!";
            mainTMP.fontSize = 96;
            mainTMP.fontStyle = FontStyles.Bold;
            mainTMP.alignment = TextAlignmentOptions.Center;
            mainTMP.color = new Color(0.99f, 0.95f, 0.87f, 1f);
            mainTMP.raycastTarget = false;
            
            AudioSource cutInAudioSource = cutInObj.AddComponent<AudioSource>();
            cutInAudioSource.playOnAwake = false;
            cutInAudioSource.loop = false;
            
            // Add GameEndUI component
            GameEndUI gameEndUI = endPanel.AddComponent<GameEndUI>();
            VictoryCutInController victoryCutIn = cutInObj.AddComponent<VictoryCutInController>();
            
            // Wire up references using reflection
            System.Type gameEndUIType = typeof(GameEndUI);
            SetPrivateField(gameEndUI, gameEndUIType, "endGamePanel", endPanel);
            SetPrivateField(gameEndUI, gameEndUIType, "winnerText", winnerText);
            SetPrivateField(gameEndUI, gameEndUIType, "finalScoreText", scoreText);
            SetPrivateField(gameEndUI, gameEndUIType, "restartButton", restartBtnObj.GetComponent<UnityEngine.UI.Button>());
            SetPrivateField(gameEndUI, gameEndUIType, "quitButton", quitBtnObj.GetComponent<UnityEngine.UI.Button>());
            SetPrivateField(gameEndUI, gameEndUIType, "victoryCutIn", victoryCutIn);

            System.Type cutInType = typeof(VictoryCutInController);
            SetPrivateField(victoryCutIn, cutInType, "cutInRoot", cutInRect);
            SetPrivateField(victoryCutIn, cutInType, "canvasGroup", cutInCanvasGroup);
            SetPrivateField(victoryCutIn, cutInType, "mainText", mainTMP);
            SetPrivateField(victoryCutIn, cutInType, "shadowText", shadowTMP);
            SetPrivateField(victoryCutIn, cutInType, "backgroundImage", cutInBackground);
            SetPrivateField(victoryCutIn, cutInType, "accentPulseImage", pulseImage);
            SetPrivateField(victoryCutIn, cutInType, "audioSource", cutInAudioSource);
            
            Debug.Log("HUDSetup: Created GameEndUI panel");
        }
        
        /// <summary>
        /// Create a UI button
        /// </summary>
        private GameObject CreateButton(Transform parent, string name, string text)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);
            
            RectTransform btnRect = btnObj.AddComponent<RectTransform>();
            btnRect.sizeDelta = new Vector2(300, 60);
            
            UnityEngine.UI.Image btnImage = btnObj.AddComponent<UnityEngine.UI.Image>();
            btnImage.color = new Color(0.2f, 0.4f, 0.8f, 1f);
            
            UnityEngine.UI.Button button = btnObj.AddComponent<UnityEngine.UI.Button>();
            
            // Create button text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            
            TextMeshProUGUI btnText = textObj.AddComponent<TextMeshProUGUI>();
            btnText.text = text;
            btnText.fontSize = 24;
            btnText.fontStyle = FontStyles.Bold;
            btnText.alignment = TextAlignmentOptions.Center;
            btnText.color = Color.white;
            
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;
            
            return btnObj;
        }
        
        /// <summary>
        /// Ensures the gameplay board has a stylised backdrop for visual depth.
        /// </summary>
        private void SetupBoardBackdrop()
        {
            GameObject dropAreas = GameObject.Find("Drop Areas");
            if (dropAreas == null)
            {
                Debug.LogWarning("HUDSetup: Could not find 'Drop Areas' to generate board backdrop.");
                return;
            }

            ProceduralBoardBackdrop existing = dropAreas.GetComponentInChildren<ProceduralBoardBackdrop>(true);
            if (existing != null)
            {
                existing.RefreshNow();
                return;
            }

            GameObject backdrop = new GameObject("ProceduralBoardBackdrop");
            backdrop.transform.SetParent(dropAreas.transform, false);
            ProceduralBoardBackdrop generator = backdrop.AddComponent<ProceduralBoardBackdrop>();
            generator.RefreshNow();
        }
    }
}

