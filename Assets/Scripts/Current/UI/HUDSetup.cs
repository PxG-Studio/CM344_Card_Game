using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using CardGame.UI.Widgets;
using CardGame.UI.CursorSystem;
using CardGame.Managers;
using CardGame.Visuals;
using CardGame.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

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
            }
            
            // Ensure supporting managers & board visuals exist
            EnsureGameManagers();
            EnsureFateFlowController();
            EnsureCoinTossManager();
            EnsureEventSystem();
            CleanupMissingScripts();
            SetupBoardBackdrop();
            
            // Find and wire up all the text labels using reflection
            WireUpHUDReferences(hudManager, hudCanvas.transform);
            
            // Setup Game End UI
            SetupGameEndUI(hudCanvas.transform);
            
            // Setup Coin Toss UI
            SetupCoinTossUI(hudCanvas.transform);
            
            // Setup Delta Marker System
            EnsureDeltaMarkerEmitter();
            
            // Setup Card Frontline UI
            SetupCardFrontlineUI(hudCanvas.transform);
            
            
            // After successful HUD setup, automatically start the game
            if (GameManager.Instance != null)
            {
                // Small delay to ensure all systems are ready
                StartCoroutine(DelayedGameStart());
            }
        }
        
        /// <summary>
        /// Delays game start slightly to ensure all systems are initialized.
        /// </summary>
        private System.Collections.IEnumerator DelayedGameStart()
        {
            yield return new WaitForEndOfFrame(); // Wait for end of frame so all components are registered
            yield return new WaitForSeconds(0.5f); // Wait for all systems to initialize
            
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Menu)
            {
                GameManager.Instance.StartGame();
            }
        }
        
        /// <summary>
        /// Ensure required game managers exist in the scene.
        /// </summary>
        private void EnsureGameManagers()
        {
            // Check for GameManager (singleton, persists across scenes)
            if (GameManager.Instance == null)
            {
                // Also check if one exists but Instance isn't set yet (Awake might not have run)
                GameManager existingGM = FindObjectOfType<GameManager>();
                if (existingGM == null)
                {
                    GameObject managerObj = new GameObject("GameManager");
                    managerObj.AddComponent<GameManager>();
                }
            }
            
            // Check for ScoreManager
            ScoreManager scoreManager = ScoreManager.Instance;
            if (scoreManager == null)
            {
                // Also check if one exists but Instance isn't set yet (Awake might not have run)
                scoreManager = FindObjectOfType<ScoreManager>();
                if (scoreManager == null)
                {
                    GameObject managerObj = new GameObject("ScoreManager");
                    managerObj.AddComponent<ScoreManager>();
                }
            }
            
            // Check for GameEndManager
            var gameEndManager = FindObjectOfType<GameEndManager>();
            if (gameEndManager == null)
            {
                GameObject managerObj = new GameObject("GameEndManager");
                managerObj.AddComponent<GameEndManager>();
            }
            
            // Check for GameStatsTracker
            var gameStatsTracker = FindObjectOfType<GameStatsTracker>();
            if (gameStatsTracker == null)
            {
                GameObject statsObj = new GameObject("GameStatsTracker");
                statsObj.AddComponent<GameStatsTracker>();
            }
        }
        
        /// <summary>
        /// Ensure DeltaMarkerEmitter exists for delta marker popups.
        /// </summary>
        private void EnsureDeltaMarkerEmitter()
        {
            DeltaMarkerEmitter existingEmitter = FindObjectOfType<DeltaMarkerEmitter>();
            if (existingEmitter != null)
            {
                existingEmitter.EnsureReady();
                return;
            }
            
            // Create DeltaMarkerEmitter GameObject
            GameObject emitterObj = new GameObject("DeltaMarkerEmitter");
            DeltaMarkerEmitter emitter = emitterObj.AddComponent<DeltaMarkerEmitter>();
            emitter.EnsureReady();
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
                    EventSystem eventSystem = eventSystemObj.AddComponent<EventSystem>();
                    StandaloneInputModule inputModule = eventSystemObj.AddComponent<StandaloneInputModule>();
                    // Configure pixel drag threshold for better click sensitivity (prevents accidental drags)
                    eventSystem.pixelDragThreshold = 10; // Increased from default 5 to make clicks more reliable
                }
                else                 if (eventSystemObj.GetComponent<EventSystem>() == null)
                {
                    EventSystem eventSystem = eventSystemObj.AddComponent<EventSystem>();
                    if (eventSystemObj.GetComponent<StandaloneInputModule>() == null)
                    {
                        eventSystemObj.AddComponent<StandaloneInputModule>();
                    }
                    // Configure pixel drag threshold for better click sensitivity
                    eventSystem.pixelDragThreshold = 10; // Increased from default 5
                }
                else
                {
                    // Configure existing EventSystem's pixel drag threshold
                    EventSystem eventSystem = eventSystemObj.GetComponent<EventSystem>();
                    if (eventSystem != null)
                    {
                        eventSystem.pixelDragThreshold = 10; // Increased from default 5
                    }
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
            }
        }
        
        private void EnsureCoinTossManager()
        {
            CoinTossManager coinTossManager = FindObjectOfType<CoinTossManager>();
            if (coinTossManager == null)
            {
                GameObject coinTossObj = new GameObject("CoinTossManager");
                coinTossObj.AddComponent<CoinTossManager>();
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
            }
            
            // Add CanvasScaler if missing
            UnityEngine.UI.CanvasScaler scaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
            if (scaler == null)
            {
                scaler = canvasObject.AddComponent<UnityEngine.UI.CanvasScaler>();
                scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
            }
            
            // Add GraphicRaycaster if missing
            UnityEngine.UI.GraphicRaycaster raycaster = canvasObject.GetComponent<UnityEngine.UI.GraphicRaycaster>();
            if (raycaster == null)
            {
                raycaster = canvasObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
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
                GameObject.DestroyImmediate(p1Panel.gameObject);
                p1Panel = null;
            }
            if (p1Panel == null)
            {
                p1Panel = CreatePlayerPanel(hudRoot, "P1Panel", true);
            }
            
            Transform p2Panel = hudRoot.Find("P2Panel");
            if (p2Panel != null)
            {
                GameObject.DestroyImmediate(p2Panel.gameObject);
                p2Panel = null;
            }
            if (p2Panel == null)
            {
                p2Panel = CreatePlayerPanel(hudRoot, "P2Panel", false);
            }
            
            // Find text labels
            TMP_Text p1ScoreLabel = p1Panel.Find("ScoreLabel")?.GetComponent<TMP_Text>();
            TMP_Text p1HandDeckLabel = p1Panel.Find("HandDeckLabel")?.GetComponent<TMP_Text>();
            TMP_Text p1PlayerLabel = p1Panel.Find("PlayerLabel")?.GetComponent<TMP_Text>();
            TMP_Text p2ScoreLabel = p2Panel.Find("ScoreLabel")?.GetComponent<TMP_Text>();
            TMP_Text p2HandDeckLabel = p2Panel.Find("HandDeckLabel")?.GetComponent<TMP_Text>();
            TMP_Text p2PlayerLabel = p2Panel.Find("PlayerLabel")?.GetComponent<TMP_Text>();
            TMP_Text tilesRemainingLabel = hudRoot.Find("TilesRemainingLabel")?.GetComponent<TMP_Text>();
            // Ensure TilesRemainingLabel exists for tests and HUDManager, even if the scene prefab
            // doesn't currently define it. This keeps older tests (TilesRemainingLabel_Exists)
            // and TilesRemaining wiring valid on runtime-only HUDs.
            if (tilesRemainingLabel == null)
            {
                GameObject tilesObj = new GameObject("TilesRemainingLabel");
                tilesObj.transform.SetParent(hudRoot, false);
                tilesObj.layer = 5; // UI layer

                RectTransform tilesRect = tilesObj.AddComponent<RectTransform>();
                tilesRect.anchorMin = new Vector2(0.5f, 1f);
                tilesRect.anchorMax = new Vector2(0.5f, 1f);
                tilesRect.pivot = new Vector2(0.5f, 1f);
                tilesRect.anchoredPosition = new Vector2(0f, -40f);
                tilesRect.sizeDelta = new Vector2(260f, 26f);

                TextMeshProUGUI tilesText = tilesObj.AddComponent<TextMeshProUGUI>();
                tilesText.text = string.Empty;
                tilesText.fontSize = 20;
                tilesText.alignment = TextAlignmentOptions.Center;
                tilesText.color = new Color(0.9f, 0.95f, 1f, 0.95f);
                tilesText.enableAutoSizing = false;

                tilesRemainingLabel = tilesText;
            }
            if (tilesRemainingLabel != null)
            {
                // Clear text but keep the label active so GameObject.Find can locate it
                // for tests like TilesRemainingLabel_Exists.
                tilesRemainingLabel.text = string.Empty;
                tilesRemainingLabel.gameObject.SetActive(true);
            }
            
            // Create turn indicators above each panel (like develop-5)
            TurnIndicatorUI p1TurnIndicator = FindOrCreateTurnIndicator(p1Panel, "TurnIndicator", true);
            TurnIndicatorUI p2TurnIndicator = FindOrCreateTurnIndicator(p2Panel, "TurnIndicator", false);
            
            // Find deck managers
            NewDeckManagerP1 player1DeckManager = FindObjectOfType<NewDeckManagerP1>();
            NewDeckManagerP2 player2DeckManager = FindObjectOfType<NewDeckManagerP2>();
            
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
            
            // Wire up PlayerPanelUI components with their labels
            PlayerPanelUI p1PanelUI = p1Panel.GetComponent<PlayerPanelUI>();
            PlayerPanelUI p2PanelUI = p2Panel.GetComponent<PlayerPanelUI>();
            
            if (p1PanelUI != null)
            {
                System.Type panelUIType = typeof(PlayerPanelUI);
                TMP_Text p1BlurpLabel = p1Panel.Find("BlurpLabel")?.GetComponent<TMP_Text>();
                SetPrivateField(p1PanelUI, panelUIType, "fieldControlLabel", p1ScoreLabel);
                SetPrivateField(p1PanelUI, panelUIType, "blurpLabel", p1BlurpLabel);
            }
            
            if (p2PanelUI != null)
            {
                System.Type panelUIType = typeof(PlayerPanelUI);
                TMP_Text p2BlurpLabel = p2Panel.Find("BlurpLabel")?.GetComponent<TMP_Text>();
                SetPrivateField(p2PanelUI, panelUIType, "fieldControlLabel", p2ScoreLabel);
                SetPrivateField(p2PanelUI, panelUIType, "blurpLabel", p2BlurpLabel);
            }
        }
        
        /// <summary>
        /// Create a complete player panel with all UI elements.
        /// </summary>
        private Transform CreatePlayerPanel(Transform parent, string panelName, bool isPlayer1)
        {
            GameObject panel = new GameObject(panelName);
            panel.transform.SetParent(parent, false);
            panel.layer = 5; // UI layer
            
            // Add RectTransform and position - now anchored near bottom of screen
        RectTransform rectTransform = panel.AddComponent<RectTransform>();
        // Move panels in square-unit increments for fine alignment
        float halfSquareUnit = 50f; // 1/2 square unit in pixels
            
            if (isPlayer1)
            {
                // Left of center, anchored to bottom
                rectTransform.anchorMin = new Vector2(0.5f, 0f);
                rectTransform.anchorMax = new Vector2(0.5f, 0f);
                rectTransform.pivot = new Vector2(1f, 0f); // Right pivot so it grows left from center
                // Positioned near board bottom with slight right shift
                rectTransform.anchoredPosition = new Vector2(-160 + halfSquareUnit, 40f);
            }
            else
            {
                // Right of center, anchored to bottom
                rectTransform.anchorMin = new Vector2(0.5f, 0f);
                rectTransform.anchorMax = new Vector2(0.5f, 0f);
                rectTransform.pivot = new Vector2(0f, 0f); // Left pivot so it grows right from center
                // Symmetric position on bottom right
                float p2Offset = 110f;
                rectTransform.anchoredPosition = new Vector2(p2Offset, 40f);
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
            CreateTextLabel(panel.transform, "ScoreLabel", "Field Control: 0", 15, false, true);
            CreateTextLabel(panel.transform, "HandDeckLabel", "Hand: 0 | Deck: 0", 13, false, true);
            
            // Create BlurpText area (replaces "Battlefield where tiles: 16")
            GameObject blurpObj = new GameObject("BlurpLabel");
            blurpObj.transform.SetParent(panel.transform, false);
            blurpObj.layer = 5; // UI layer
            
            RectTransform blurpRect = blurpObj.AddComponent<RectTransform>();
            blurpRect.sizeDelta = new Vector2(0, 40);
            
            TextMeshProUGUI blurpText = blurpObj.AddComponent<TextMeshProUGUI>();
            blurpText.text = "";
            blurpText.fontSize = 14;
            blurpText.fontStyle = TMPro.FontStyles.Bold;
            blurpText.alignment = TMPro.TextAlignmentOptions.Center;
            blurpText.color = new Color(1f, 0.8f, 0f, 1f); // Gold color for blurps
            
            CanvasGroup blurpCanvasGroup = blurpObj.AddComponent<CanvasGroup>();
            blurpCanvasGroup.alpha = 0f;
            blurpCanvasGroup.blocksRaycasts = false;
            blurpCanvasGroup.interactable = false;
            
            // Add PlayerPanelUI component to the panel (will be wired up later)
            PlayerPanelUI panelUI = panel.AddComponent<PlayerPanelUI>();
            
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
            
            CursorManager cursorManager = FindObjectOfType<CursorManager>();
            if (cursorManager == null)
            {
                GameObject cursorManagerGO = new GameObject("CursorManager");
                cursorManager = cursorManagerGO.AddComponent<CursorManager>();
            }
            cursorManager.RefreshCursor();
            
            // Clean up any old generated cursor sprites that might still be in the scene
            GameObject strayCursorSprite = GameObject.Find("GeneratedCursorSprite");
            if (strayCursorSprite != null)
            {
                Destroy(strayCursorSprite);
            }
            
            // Legacy cursor prefab cleanup (no longer needed for CursorManager, but keep tidy)
            if (cursorGameObject != null)
            {
                DisableAllInputComponents(cursorGameObject, "cleaning up old cursor source");
                DisableInputScripts(cursorGameObject);
                if (ignoreRaycastLayer >= 0)
                {
                    SetLayerRecursive(cursorGameObject, ignoreRaycastLayer);
                }
                Destroy(cursorGameObject);
            }
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
            
            // Input components disabled
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
                // [CardFront] Use helper method to ensure all required elements exist
                EnsureGameEndUIElements(existingUI);
                return;
            }
            
            // Create new Game End Panel from scratch
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
            contentRect.sizeDelta = new Vector2(700, 600); // [CardFront] Larger panel for statistics
            contentRect.anchoredPosition = Vector2.zero;
            
            // Add background to content panel
            UnityEngine.UI.Image contentBg = contentPanel.AddComponent<UnityEngine.UI.Image>();
            contentBg.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);
            
            // Add vertical layout
            UnityEngine.UI.VerticalLayoutGroup layout = contentPanel.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            layout.padding = new RectOffset(40, 40, 70, 40); // extra top padding to center result text lower
            layout.spacing = 24;
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
            winnerText.fontSize = 72; // [CardFront] Larger winner text for better visibility
            winnerText.fontStyle = FontStyles.Bold;
            winnerText.alignment = TextAlignmentOptions.Center;
            winnerText.color = Color.white;
            
            RectTransform winnerRect = winnerTextObj.GetComponent<RectTransform>();
            winnerRect.sizeDelta = new Vector2(0, 100);
            
            // Create Contextual Message Text
            GameObject contextualMsgObj = new GameObject("ContextualMessageText");
            contextualMsgObj.transform.SetParent(contentPanel.transform, false);
            
            TextMeshProUGUI contextualMsgText = contextualMsgObj.AddComponent<TextMeshProUGUI>();
            contextualMsgText.text = "Good Game!";
            contextualMsgText.fontSize = 24;
            contextualMsgText.alignment = TextAlignmentOptions.Center;
            contextualMsgText.color = new Color(0.95f, 0.95f, 0.8f, 1f);
            
            RectTransform contextualMsgRect = contextualMsgObj.GetComponent<RectTransform>();
            contextualMsgRect.sizeDelta = new Vector2(0, 40);
            
            // Create Final Score Text (with margin)
            GameObject scoreTextObj = new GameObject("FinalScoreText");
            scoreTextObj.transform.SetParent(contentPanel.transform, false);
            
            TextMeshProUGUI scoreText = scoreTextObj.AddComponent<TextMeshProUGUI>();
            scoreText.text = "Final Score\nPlayer 1: 0  |  Player 2: 0\nMargin: 0";
            scoreText.fontSize = 28;
            scoreText.alignment = TextAlignmentOptions.Center;
            scoreText.color = new Color(0.9f, 0.9f, 0.9f, 1f);
            
            RectTransform scoreRect = scoreTextObj.GetComponent<RectTransform>();
            scoreRect.sizeDelta = new Vector2(0, 100);
            
            // Create Statistics Text
            GameObject statisticsTextObj = new GameObject("StatisticsText");
            statisticsTextObj.transform.SetParent(contentPanel.transform, false);
            
            TextMeshProUGUI statisticsText = statisticsTextObj.AddComponent<TextMeshProUGUI>();
            statisticsText.text = "Cards Played: 0\nCaptures Made: 0\nLongest Chain: 0";
            statisticsText.fontSize = 22;
            statisticsText.alignment = TextAlignmentOptions.Center;
            statisticsText.color = new Color(0.8f, 0.8f, 0.9f, 1f);
            
            RectTransform statisticsRect = statisticsTextObj.GetComponent<RectTransform>();
            statisticsRect.sizeDelta = new Vector2(0, 80);
            
            // Create Win/Loss Record Text
            GameObject winLossTextObj = new GameObject("WinLossRecordText");
            winLossTextObj.transform.SetParent(contentPanel.transform, false);
            
            TextMeshProUGUI winLossText = winLossTextObj.AddComponent<TextMeshProUGUI>();
            winLossText.text = "Wins: 0 | Losses: 0";
            winLossText.fontSize = 20;
            winLossText.alignment = TextAlignmentOptions.Center;
            winLossText.color = new Color(0.7f, 0.7f, 0.8f, 1f);
            
            RectTransform winLossRect = winLossTextObj.GetComponent<RectTransform>();
            winLossRect.sizeDelta = new Vector2(0, 40);
            
            // Create Rematch Button (changed from "Play Again")
            GameObject restartBtnObj = CreateButton(contentPanel.transform, "RematchButton", "Rematch");
            
            // Create Quit Button
            GameObject quitBtnObj = CreateButton(contentPanel.transform, "QuitButton", "Quit Game");

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
            SetPrivateField(gameEndUI, gameEndUIType, "statisticsText", statisticsText);
            SetPrivateField(gameEndUI, gameEndUIType, "winLossRecordText", winLossText);
            SetPrivateField(gameEndUI, gameEndUIType, "contextualMessageText", contextualMsgText);
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
            
        }
        
        /// <summary>
        /// [CardFront] Ensures an existing GameEndUI has all required UI elements
        /// </summary>
        private void EnsureGameEndUIElements(GameEndUI gameEndUI)
        {
            if (gameEndUI == null) return;
            
            System.Type gameEndUIType = typeof(GameEndUI);
            
            // Get the endGamePanel field value
            var endPanelField = gameEndUIType.GetField("endGamePanel", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            GameObject endPanel = null;
            
            if (endPanelField != null)
            {
                endPanel = endPanelField.GetValue(gameEndUI) as GameObject;
            }
            
            // If endPanel is null, use the GameEndUI GameObject itself
            if (endPanel == null)
            {
                endPanel = gameEndUI.gameObject;
                if (endPanelField != null)
                {
                    endPanelField.SetValue(gameEndUI, endPanel);
                }
            }
            
            // Find or create ContentPanel
            Transform contentPanel = endPanel.transform.Find("ContentPanel");
            if (contentPanel == null)
            {
                // Create content panel structure
                GameObject contentPanelObj = new GameObject("ContentPanel");
                contentPanelObj.transform.SetParent(endPanel.transform, false);
                RectTransform contentRect = contentPanelObj.AddComponent<RectTransform>();
                contentRect.anchorMin = new Vector2(0.5f, 0.5f);
                contentRect.anchorMax = new Vector2(0.5f, 0.5f);
                contentRect.pivot = new Vector2(0.5f, 0.5f);
                contentRect.sizeDelta = new Vector2(700, 600);
                contentRect.anchoredPosition = Vector2.zero;
                
                UnityEngine.UI.Image contentBg = contentPanelObj.AddComponent<UnityEngine.UI.Image>();
                contentBg.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);
                
                UnityEngine.UI.VerticalLayoutGroup layout = contentPanelObj.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
                layout.padding = new RectOffset(40, 40, 40, 40);
                layout.spacing = 30;
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.childControlWidth = true;
                layout.childControlHeight = false;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;
                
                contentPanel = contentPanelObj.transform;
            }
            
            // Check and create missing UI elements using helper method
            TextMeshProUGUI statisticsText = EnsureUIElement(contentPanel, "StatisticsText", gameEndUIType, gameEndUI, "statisticsText",
                "Cards Played: 0\nCaptures Made: 0\nLongest Chain: 0", 22, new Color(0.8f, 0.8f, 0.9f, 1f), new Vector2(0, 80));
                
            TextMeshProUGUI winLossText = EnsureUIElement(contentPanel, "WinLossRecordText", gameEndUIType, gameEndUI, "winLossRecordText",
                "Wins: 0 | Losses: 0", 20, new Color(0.7f, 0.7f, 0.8f, 1f), new Vector2(0, 40));
                
            TextMeshProUGUI contextualMsgText = EnsureUIElement(contentPanel, "ContextualMessageText", gameEndUIType, gameEndUI, "contextualMessageText",
                "Good Game!", 24, new Color(0.95f, 0.95f, 0.8f, 1f), new Vector2(0, 40));
            
            // Also ensure winnerText and finalScoreText have correct sizes if they exist
            var winnerTextField = gameEndUIType.GetField("winnerText", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (winnerTextField != null)
            {
                TextMeshProUGUI winnerText = winnerTextField.GetValue(gameEndUI) as TextMeshProUGUI;
                if (winnerText != null && winnerText.fontSize < 72)
                {
                    winnerText.fontSize = 72;
                    RectTransform winnerRect = winnerText.GetComponent<RectTransform>();
                    if (winnerRect != null)
                    {
                        winnerRect.sizeDelta = new Vector2(0, 100);
                    }
                }
            }
        }
        
        /// <summary>
        /// [CardFront] Ensures a UI element exists and is wired to the GameEndUI component
        /// </summary>
        private TextMeshProUGUI EnsureUIElement(Transform parent, string elementName, System.Type gameEndUIType, 
            GameEndUI gameEndUI, string fieldName, string defaultText, int fontSize, Color textColor, Vector2 sizeDelta)
        {
            // Check if element already exists
            Transform existingElement = parent.Find(elementName);
            TextMeshProUGUI textComponent = null;
            
            if (existingElement != null)
            {
                textComponent = existingElement.GetComponent<TextMeshProUGUI>();
                if (textComponent != null)
                {
                    // Element exists, just wire it up
                    SetPrivateField(gameEndUI, gameEndUIType, fieldName, textComponent);
                    return textComponent;
                }
            }
            
            // Create new element
            GameObject elementObj = new GameObject(elementName);
            elementObj.transform.SetParent(parent, false);
            
            textComponent = elementObj.AddComponent<TextMeshProUGUI>();
            textComponent.text = defaultText;
            textComponent.fontSize = fontSize;
            textComponent.alignment = TextAlignmentOptions.Center;
            textComponent.color = textColor;
            
            RectTransform rect = elementObj.GetComponent<RectTransform>();
            rect.sizeDelta = sizeDelta;
            
            // Wire up to GameEndUI
            SetPrivateField(gameEndUI, gameEndUIType, fieldName, textComponent);
            
            return textComponent;
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
        /// Setup the Coin Toss UI panel for determining starting player.
        /// </summary>
        private void SetupCoinTossUI(Transform hudRoot)
        {
            if (hudRoot == null)
            {
                Debug.LogError("HUDSetup: SetupCoinTossUI called with null hudRoot!");
                return;
            }
            
            // Verify parent is active (inactive parents can hide children in hierarchy)
            if (!hudRoot.gameObject.activeInHierarchy)
            {
                Debug.LogWarning($"HUDSetup: HUD root '{hudRoot.name}' is inactive. Activating to ensure CoinTossPanel is visible.");
                hudRoot.gameObject.SetActive(true);
            }
            
            // Check if CoinTossUI already exists
            CoinTossUI existingUI = hudRoot.GetComponentInChildren<CoinTossUI>(true);
            if (existingUI != null)
            {

                // If the panel already exists in the scene (prefab or hand‑placed),
                // nudge its ContentPanel to the left so the whole popup shifts over.
                RectTransform existingRootRect = existingUI.GetComponent<RectTransform>();
                if (existingRootRect != null)
                {
                    // Ensure the root fills the screen like the runtime‑created version.
                    existingRootRect.anchorMin = Vector2.zero;
                    existingRootRect.anchorMax = Vector2.one;
                    existingRootRect.sizeDelta = Vector2.zero;
                    existingRootRect.anchoredPosition = Vector2.zero;
                }

                Transform existingContent = existingUI.transform.Find("ContentPanel");
                if (existingContent != null)
                {
                    RectTransform contentRectExisting = existingContent.GetComponent<RectTransform>();
                    if (contentRectExisting != null)
                    {
                        contentRectExisting.anchorMin = new Vector2(0.5f, 0.5f);
                        contentRectExisting.anchorMax = new Vector2(0.5f, 0.5f);
                        contentRectExisting.pivot = new Vector2(0.5f, 0.5f);
                        // Slight right offset (~1/33 square unit) for fine centering
                        contentRectExisting.anchoredPosition = new Vector2(0.5f, 0f);
                    }
                }

                return;
            }
            
            
            // Create coin toss panel
            GameObject coinTossPanel = new GameObject("CoinTossPanel");
            coinTossPanel.transform.SetParent(hudRoot, false);
            coinTossPanel.layer = 5; // UI layer
            coinTossPanel.SetActive(false); // Start inactive - will be shown when coin toss starts
            
            // Mark as DontDestroyOnLoad to ensure it persists (if parent does)
            // Note: This only works if parent is in DontDestroyOnLoad, but we'll ensure it exists
            
            RectTransform panelRect = coinTossPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;
            panelRect.anchoredPosition = Vector2.zero;
            
            // Add semi-transparent background
            CanvasGroup panelCanvasGroup = coinTossPanel.AddComponent<CanvasGroup>();
            UnityEngine.UI.Image bgImage = coinTossPanel.AddComponent<UnityEngine.UI.Image>();
            bgImage.color = new Color(0f, 0f, 0f, 0.8f);
            
            // Create content panel (centered)
            GameObject contentPanel = new GameObject("ContentPanel");
            contentPanel.transform.SetParent(coinTossPanel.transform, false);
            
            RectTransform contentRect = contentPanel.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.5f, 0.5f);
            contentRect.anchorMax = new Vector2(0.5f, 0.5f);
            contentRect.pivot = new Vector2(0.5f, 0.5f);
            contentRect.sizeDelta = new Vector2(750f, 650f);
            // Slight right offset (~1/33 square unit ≈ 3px) for better centering relative to board
            contentRect.anchoredPosition = new Vector2(0.5f, 0f);
            
            // Add background to content panel
            UnityEngine.UI.Image contentBg = contentPanel.AddComponent<UnityEngine.UI.Image>();
            contentBg.color = new Color(0.15f, 0.15f, 0.2f, 0.95f);
            
            // Add vertical layout & size fitter
            UnityEngine.UI.VerticalLayoutGroup layout = contentPanel.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            layout.padding = new RectOffset(32, 32, 36, 36);
            layout.spacing = 28;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            
            // Create title text
            GameObject titleTextObj = new GameObject("TitleText");
            titleTextObj.transform.SetParent(contentPanel.transform, false);
            TextMeshProUGUI titleText = titleTextObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "COIN TOSS";
            titleText.fontSize = 48;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = Color.white;
            RectTransform titleRect = titleTextObj.GetComponent<RectTransform>();
            titleRect.sizeDelta = new Vector2(0, 60);
            
            // Create coin image container
            GameObject coinContainer = new GameObject("CoinContainer");
            coinContainer.transform.SetParent(contentPanel.transform, false);
            RectTransform coinRect = coinContainer.AddComponent<RectTransform>();
            coinRect.sizeDelta = new Vector2(280, 280);
            UnityEngine.UI.LayoutElement coinLayout = coinContainer.AddComponent<UnityEngine.UI.LayoutElement>();
            coinLayout.preferredWidth = 280;
            coinLayout.preferredHeight = 280;
            
            // Create coin face image (heads/tails artwork) directly under the container.
            // We intentionally skip the extra UICoinGraphic background so the coin
            // appears as a single, clean sprite without a secondary disk behind it.
            GameObject coinImageObj = new GameObject("CoinImage");
            coinImageObj.transform.SetParent(coinContainer.transform, false);
            RectTransform coinImageRect = coinImageObj.AddComponent<RectTransform>();
            coinImageRect.anchorMin = new Vector2(0.5f, 0.5f);
            coinImageRect.anchorMax = new Vector2(0.5f, 0.5f);
            coinImageRect.pivot = new Vector2(0.5f, 0.5f);
            coinImageRect.sizeDelta = new Vector2(200, 200);
            coinImageRect.anchoredPosition = Vector2.zero;
            
            UnityEngine.UI.Image coinImage = coinImageObj.AddComponent<UnityEngine.UI.Image>();
            // Leave coin image untinted; CoinTossUI drives sprites and uses white so
            // the artwork shows in its original colors.
            coinImage.color = Color.white;
            coinImage.preserveAspect = true;
            
            // Create selection prompt
            GameObject selectionPromptObj = new GameObject("SelectionPrompt");
            selectionPromptObj.transform.SetParent(contentPanel.transform, false);
            TextMeshProUGUI selectionPrompt = selectionPromptObj.AddComponent<TextMeshProUGUI>();
            selectionPrompt.enableAutoSizing = true;
            selectionPrompt.fontSizeMin = 24f;
            selectionPrompt.fontSizeMax = 48f;
            selectionPrompt.text = "Player 1: Select Heads or Tails";
            selectionPrompt.fontStyle = FontStyles.Bold;
            selectionPrompt.alignment = TextAlignmentOptions.Center;
            selectionPrompt.color = new Color(0.95f, 0.95f, 1f, 1f);
            RectTransform selectionPromptRect = selectionPromptObj.GetComponent<RectTransform>();
            selectionPromptRect.sizeDelta = new Vector2(contentRect.sizeDelta.x - 64f, 60f);
            
            // Create selection panel with buttons
            GameObject selectionPanel = new GameObject("ButtonsContainer");
            selectionPanel.transform.SetParent(contentPanel.transform, false);
            RectTransform selectionPanelRect = selectionPanel.AddComponent<RectTransform>();
            selectionPanelRect.sizeDelta = new Vector2(contentRect.sizeDelta.x - 64f, 90f);
            
            UnityEngine.UI.HorizontalLayoutGroup selectionLayout = selectionPanel.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            selectionLayout.spacing = 40;
            selectionLayout.childAlignment = TextAnchor.MiddleCenter;
            selectionLayout.childControlWidth = false;
            selectionLayout.childControlHeight = false;
            selectionLayout.childForceExpandWidth = false;
            selectionLayout.childForceExpandHeight = false;
            UnityEngine.UI.ContentSizeFitter selectionFitter = selectionPanel.AddComponent<UnityEngine.UI.ContentSizeFitter>();
            selectionFitter.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            selectionFitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;
            
            GameObject headsButtonObj = CreateButton(selectionPanel.transform, "HeadsButton", "HEADS");
            RectTransform headsBtnRect = headsButtonObj.GetComponent<RectTransform>();
            headsBtnRect.sizeDelta = new Vector2(220, 70);
            TextMeshProUGUI headsLabel = headsButtonObj.GetComponentInChildren<TextMeshProUGUI>();
            headsLabel.enableAutoSizing = true;
            headsLabel.fontSizeMin = 18f;
            headsLabel.fontSizeMax = 32f;
            headsLabel.color = Color.yellow;
            UnityEngine.UI.Button headsButton = headsButtonObj.GetComponent<UnityEngine.UI.Button>();
            UnityEngine.UI.LayoutElement headsLayout = headsButtonObj.AddComponent<UnityEngine.UI.LayoutElement>();
            headsLayout.minWidth = 200;
            headsLayout.minHeight = 70;
            headsLayout.preferredWidth = 220;
            headsLayout.preferredHeight = 70;
            
            GameObject tailsButtonObj = CreateButton(selectionPanel.transform, "TailsButton", "TAILS");
            RectTransform tailsBtnRect = tailsButtonObj.GetComponent<RectTransform>();
            tailsBtnRect.sizeDelta = new Vector2(220, 70);
            TextMeshProUGUI tailsLabel = tailsButtonObj.GetComponentInChildren<TextMeshProUGUI>();
            tailsLabel.enableAutoSizing = true;
            tailsLabel.fontSizeMin = 18f;
            tailsLabel.fontSizeMax = 32f;
            tailsLabel.color = Color.white;
            UnityEngine.UI.Button tailsButton = tailsButtonObj.GetComponent<UnityEngine.UI.Button>();
            UnityEngine.UI.LayoutElement tailsLayout = tailsButtonObj.AddComponent<UnityEngine.UI.LayoutElement>();
            tailsLayout.minWidth = 200;
            tailsLayout.minHeight = 70;
            tailsLayout.preferredWidth = 220;
            tailsLayout.preferredHeight = 70;
            
            // Create result text
            GameObject resultTextObj = new GameObject("ResultText");
            resultTextObj.transform.SetParent(contentPanel.transform, false);
            TextMeshProUGUI resultText = resultTextObj.AddComponent<TextMeshProUGUI>();
            resultText.text = "";
            resultText.fontSize = 36;
            resultText.fontStyle = FontStyles.Bold;
            resultText.alignment = TextAlignmentOptions.Center;
            resultText.color = Color.white;
            RectTransform resultRect = resultTextObj.GetComponent<RectTransform>();
            resultRect.sizeDelta = new Vector2(0, 80);
            resultTextObj.SetActive(false); // Hidden until result
            
            // Create continue button
            GameObject continueBtnObj = CreateButton(contentPanel.transform, "ContinueButton", "Continue");
            continueBtnObj.SetActive(false); // Hidden until result
            UnityEngine.UI.LayoutElement continueLayout = continueBtnObj.AddComponent<UnityEngine.UI.LayoutElement>();
            continueLayout.preferredWidth = 240;
            continueLayout.preferredHeight = 80;
            
            // Add CoinTossUI component
            CoinTossUI coinTossUI = coinTossPanel.AddComponent<CoinTossUI>();
            CoinTossUIController controller = coinTossPanel.AddComponent<CoinTossUIController>();
            controller.InjectDependencies(panelCanvasGroup, contentRect, coinImage, selectionPrompt);
            
            // Wire up references using reflection
            System.Type coinTossUIType = typeof(CoinTossUI);
            SetPrivateField(coinTossUI, coinTossUIType, "coinTossPanel", coinTossPanel);
            SetPrivateField(coinTossUI, coinTossUIType, "hoverContainer", contentRect);
            SetPrivateField(coinTossUI, coinTossUIType, "resultText", resultText);
            SetPrivateField(coinTossUI, coinTossUIType, "headsLabel", headsLabel);
            SetPrivateField(coinTossUI, coinTossUIType, "tailsLabel", tailsLabel);
            SetPrivateField(coinTossUI, coinTossUIType, "coinImage", coinImage);
            SetPrivateField(coinTossUI, coinTossUIType, "continueButton", continueBtnObj.GetComponent<UnityEngine.UI.Button>());
            SetPrivateField(coinTossUI, coinTossUIType, "selectionPanel", selectionPanel);
            SetPrivateField(coinTossUI, coinTossUIType, "selectionPromptText", selectionPrompt);
            SetPrivateField(coinTossUI, coinTossUIType, "headsButton", headsButton);
            SetPrivateField(coinTossUI, coinTossUIType, "tailsButton", tailsButton);
            
            // Auto-assign coin toss sprites (Heads inventor.png and Sheep tails.png)
            LoadCoinTossSprites(coinTossUI, coinTossUIType);
            controller.Setup("Player 1");
            
            // Verify the panel was created and parented correctly
            if (coinTossPanel.transform.parent != hudRoot)
            {
                Debug.LogError($"HUDSetup: CoinTossPanel parent mismatch! Expected '{hudRoot.name}', got '{coinTossPanel.transform.parent?.name}'");
            }
            
            // Verify the GameObject exists in the scene
            if (coinTossPanel == null || coinTossPanel.GetInstanceID() == 0)
            {
                Debug.LogError("HUDSetup: CoinTossPanel GameObject is invalid after creation!");
            }
            else
            {
            coinTossPanel.transform.SetAsLastSibling(); // ensure overlay renders above other HUD elements
            }
        }
        
        /// <summary>
        /// Loads and assigns coin toss sprites to CoinTossUI component.
        /// </summary>
        private void LoadCoinTossSprites(CoinTossUI coinTossUI, System.Type coinTossUIType)
        {
            #if UNITY_EDITOR
            string headsPath = "Assets/Resources/Coin/Heads.png";
            string tailsPath = "Assets/Resources/Coin/Tails.png";
            Sprite headsSprite = AssetDatabase.LoadAssetAtPath<Sprite>(headsPath);
            Sprite tailsSprite = AssetDatabase.LoadAssetAtPath<Sprite>(tailsPath);
            #else
            Sprite headsSprite = Resources.Load<Sprite>("Coin/Heads");
            Sprite tailsSprite = Resources.Load<Sprite>("Coin/Tails");
            #endif
            
            if (headsSprite != null)
            {
                SetPrivateField(coinTossUI, coinTossUIType, "headsSprite", headsSprite);
            }
            else
            {
                Debug.LogWarning("[HUDSetup] Could not load custom coin heads sprite. Please assign manually.");
            }
            
            if (tailsSprite != null)
            {
                SetPrivateField(coinTossUI, coinTossUIType, "tailsSprite", tailsSprite);
            }
            else
            {
                Debug.LogWarning("[HUDSetup] Could not load custom coin tails sprite. Please assign manually.");
            }
        }
        
        /// <summary>
        /// Ensures the gameplay board has a stylised backdrop for visual depth.
        /// </summary>
        /// <summary>
        /// Creates CardFrontlineUI bar programmatically below the board.
        /// </summary>
        private void SetupCardFrontlineUI(Transform hudRoot)
        {
            // Check if CardFrontlineUI already exists
            CardFrontlineUI existing = FindObjectOfType<CardFrontlineUI>();
            if (existing != null)
            {
                return;
            }
            
            // Find "Drop Areas" to determine board width and position
            GameObject dropAreas = GameObject.Find("Drop Areas");
            if (dropAreas == null)
            {
                Debug.LogWarning("HUDSetup: Could not find 'Drop Areas' for CardFrontlineUI positioning.");
                return;
            }
            
            // Get board width from first CardDropArea or estimate
            RectTransform boardRect = dropAreas.GetComponent<RectTransform>();
            float boardWidth = 800f; // Default estimate
            if (boardRect != null)
            {
                boardWidth = boardRect.rect.width;
            }
            
            // Create root CardFrontlineBar
            GameObject frontlineBar = new GameObject("CardFrontlineBar");
            frontlineBar.transform.SetParent(hudRoot, false);
            frontlineBar.layer = 5; // UI layer
            
            RectTransform barRect = frontlineBar.AddComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0.5f, 1f);
            barRect.anchorMax = new Vector2(0.5f, 1f);
            barRect.pivot = new Vector2(0.5f, 1f);
            barRect.sizeDelta = new Vector2(boardWidth, 80f);
            // Center the bar horizontally so its edges align with the 4x4 board
            // and adjust vertical offset (lower by ~15%)
            barRect.anchoredPosition = new Vector2(0f, -90f);
            
            // Create Title Label
            GameObject titleLabelObj = new GameObject("TitleLabel");
            titleLabelObj.transform.SetParent(frontlineBar.transform, false);
            RectTransform titleRect = titleLabelObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.sizeDelta = new Vector2(boardWidth * 0.9f, 26f);
            titleRect.anchoredPosition = new Vector2(0f, -4f);
            
            TextMeshProUGUI titleLabelText = titleLabelObj.AddComponent<TextMeshProUGUI>();
            titleLabelText.text = "Battle Front Influence";
            titleLabelText.fontSize = 26;
            titleLabelText.fontStyle = TMPro.FontStyles.Bold;
            titleLabelText.alignment = TMPro.TextAlignmentOptions.Center;
            titleLabelText.color = Color.white;
            titleLabelText.enableWordWrapping = false;
            titleLabelText.overflowMode = TMPro.TextOverflowModes.Overflow;
            
            // Create Counter Label (kept for CardFrontlineUI logic/tests but hidden by default)
            GameObject counterLabelObj = new GameObject("CounterLabel");
            counterLabelObj.transform.SetParent(frontlineBar.transform, false);
            RectTransform counterRect = counterLabelObj.AddComponent<RectTransform>();
            counterRect.anchorMin = new Vector2(0.5f, 0f);
            counterRect.anchorMax = new Vector2(0.5f, 0f);
            counterRect.pivot = new Vector2(0.5f, 0f);
            counterRect.sizeDelta = new Vector2(boardWidth * 0.9f, 24f);
            counterRect.anchoredPosition = new Vector2(0f, -28f);
            
            TextMeshProUGUI counterLabelText = counterLabelObj.AddComponent<TextMeshProUGUI>();
            counterLabelText.text = string.Empty; // no default "16" visible
            counterLabelText.fontSize = 24;
            counterLabelText.fontStyle = TMPro.FontStyles.Bold;
            counterLabelText.alignment = TMPro.TextAlignmentOptions.Center;
            // Start fully transparent so the numeric label doesn't appear over the bar;
            // CardFrontlineUI can still drive this label if desired by changing its color.
            counterLabelText.color = new Color(0.87f, 0.94f, 1f, 0f);
            counterLabelText.enableWordWrapping = false;
            counterLabelText.overflowMode = TMPro.TextOverflowModes.Overflow;
            
            // Create BarBackground
            GameObject barBgObj = new GameObject("BarBackground");
            barBgObj.transform.SetParent(frontlineBar.transform, false);
            RectTransform barBgRect = barBgObj.AddComponent<RectTransform>();
            barBgRect.anchorMin = new Vector2(0f, 0f);
            barBgRect.anchorMax = new Vector2(1f, 0.5f);
            barBgRect.sizeDelta = Vector2.zero;
            barBgRect.anchoredPosition = Vector2.zero;
            
            Image barBgImage = barBgObj.AddComponent<Image>();
            barBgImage.color = new Color(0.10f, 0.15f, 0.22f, 1f); // #1A2738 dark navy
            
            // Create P1Fill
            GameObject p1FillObj = new GameObject("P1Fill");
            p1FillObj.transform.SetParent(frontlineBar.transform, false);
            RectTransform p1FillRect = p1FillObj.AddComponent<RectTransform>();
            p1FillRect.anchorMin = new Vector2(0f, 0f);
            p1FillRect.anchorMax = new Vector2(0.5f, 0.5f);
            p1FillRect.sizeDelta = Vector2.zero;
            p1FillRect.anchoredPosition = Vector2.zero;
            
            Image p1FillImage = p1FillObj.AddComponent<Image>();
            p1FillImage.color = new Color(1f, 0.62f, 0.27f, 1f); // #FF9F45 ember
            p1FillImage.type = Image.Type.Filled;
            p1FillImage.fillMethod = Image.FillMethod.Horizontal;
            // Start empty; CardFrontlineUI will drive fill amounts as tiles are captured.
            p1FillImage.fillAmount = 0f;
            
            // Create P2Fill
            GameObject p2FillObj = new GameObject("P2Fill");
            p2FillObj.transform.SetParent(frontlineBar.transform, false);
            RectTransform p2FillRect = p2FillObj.AddComponent<RectTransform>();
            p2FillRect.anchorMin = new Vector2(0.5f, 0f);
            p2FillRect.anchorMax = new Vector2(1f, 0.5f);
            p2FillRect.sizeDelta = Vector2.zero;
            p2FillRect.anchoredPosition = Vector2.zero;
            
            Image p2FillImage = p2FillObj.AddComponent<Image>();
            p2FillImage.color = new Color(0.43f, 1f, 0.55f, 1f); // #6EFF8D jade
            p2FillImage.type = Image.Type.Filled;
            p2FillImage.fillMethod = Image.FillMethod.Horizontal;
            p2FillImage.fillOrigin = 1; // Fill from right
            // Start empty; CardFrontlineUI will drive fill amounts as tiles are captured.
            p2FillImage.fillAmount = 0f;
            
            // Create MidDivider
            GameObject dividerObj = new GameObject("MidDivider");
            dividerObj.transform.SetParent(frontlineBar.transform, false);
            RectTransform dividerRect = dividerObj.AddComponent<RectTransform>();
            dividerRect.anchorMin = new Vector2(0.5f, 0f);
            dividerRect.anchorMax = new Vector2(0.5f, 0.5f);
            dividerRect.sizeDelta = new Vector2(4f, 0f);
            dividerRect.anchoredPosition = Vector2.zero;
            
            Image dividerImage = dividerObj.AddComponent<Image>();
            dividerImage.color = Color.white;

            // Create simple placeholder lagging markers (triangles) that visually match the HUD
            GameObject triangleTopObj = new GameObject("TriangleTop");
            triangleTopObj.transform.SetParent(frontlineBar.transform, false);
            RectTransform triangleTopRect = triangleTopObj.AddComponent<RectTransform>();
            triangleTopRect.anchorMin = new Vector2(0.5f, 0.5f);
            triangleTopRect.anchorMax = new Vector2(0.5f, 0.5f);
            // Centered on the divider line with a small vertical offset so it hugs the bar
            triangleTopRect.pivot = new Vector2(0.5f, 0.5f);
            triangleTopRect.anchoredPosition = new Vector2(0f, 6f);
            triangleTopRect.sizeDelta = new Vector2(24f, 24f);

            TextMeshProUGUI triangleTopLabel = triangleTopObj.AddComponent<TextMeshProUGUI>();
            triangleTopLabel.text = "▼";
            triangleTopLabel.fontSize = 24;
            triangleTopLabel.fontStyle = FontStyles.Bold;
            triangleTopLabel.alignment = TextAlignmentOptions.Center;
            triangleTopLabel.color = new Color(1f, 0.8f, 0f, 1f); // gold, same family as turn indicator
            triangleTopLabel.raycastTarget = false;

            GameObject triangleBottomObj = new GameObject("TriangleBottom");
            triangleBottomObj.transform.SetParent(frontlineBar.transform, false);
            RectTransform triangleBottomRect = triangleBottomObj.AddComponent<RectTransform>();
            triangleBottomRect.anchorMin = new Vector2(0.5f, 0.5f);
            triangleBottomRect.anchorMax = new Vector2(0.5f, 0.5f);
            // Keep X aligned with the top triangle/divider, but push further down so it
            // sits at the lower edge of the bar instead of hugging the center line.
            triangleBottomRect.pivot = new Vector2(0.5f, 0.5f);
            triangleBottomRect.anchoredPosition = new Vector2(0f, -14f);
            triangleBottomRect.sizeDelta = new Vector2(24f, 24f);

            TextMeshProUGUI triangleBottomLabel = triangleBottomObj.AddComponent<TextMeshProUGUI>();
            triangleBottomLabel.text = "▲";
            triangleBottomLabel.fontSize = 24;
            triangleBottomLabel.fontStyle = FontStyles.Bold;
            triangleBottomLabel.alignment = TextAlignmentOptions.Center;
            triangleBottomLabel.color = new Color(1f, 0.8f, 0f, 1f);
            triangleBottomLabel.raycastTarget = false;
            
            // Add CardFrontlineUI component
            CardFrontlineUI frontlineUI = frontlineBar.AddComponent<CardFrontlineUI>();
            
            // Wire up references using reflection
            System.Type uiType = typeof(CardFrontlineUI);
            SetPrivateField(frontlineUI, uiType, "titleLabel", titleLabelText);
            SetPrivateField(frontlineUI, uiType, "remainingLabel", counterLabelText);
            SetPrivateField(frontlineUI, uiType, "p1Fill", p1FillImage);
            SetPrivateField(frontlineUI, uiType, "p2Fill", p2FillImage);
            SetPrivateField(frontlineUI, uiType, "midDivider", dividerRect);
            SetPrivateField(frontlineUI, uiType, "triangleTop", triangleTopRect);
            SetPrivateField(frontlineUI, uiType, "triangleBottom", triangleBottomRect);
            
            
            BringCoinTossPanelToFront(hudRoot);
        }
        
        private void SetupBoardBackdrop()
        {
            GameObject dropAreasRoot = GameObject.Find("Drop Areas");
            if (dropAreasRoot == null)
            {
                Debug.LogWarning("HUDSetup: Cannot create battleground backdrop because 'Drop Areas' was not found.");
                return;
            }

            // Remove any lingering legacy world-space backdrops that live directly
            // under Drop Areas. The new backdrop will live under Play Zone instead.
            Transform oldWorldBackdrop = dropAreasRoot.transform.Find("BattlegroundBackdrop");
            if (oldWorldBackdrop != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(oldWorldBackdrop.gameObject);
                }
                else
                {
                    DestroyImmediate(oldWorldBackdrop.gameObject);
                }
            }
            Transform oldSprite = dropAreasRoot.transform.Find("BattlegroundSprite");
            if (oldSprite != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(oldSprite.gameObject);
                }
                else
                {
                    DestroyImmediate(oldSprite.gameObject);
                }
            }

            Sprite sprite = LoadBattlegroundSprite();
            if (sprite == null)
            {
                Debug.LogWarning("[HUDSetup] Battle_Grounds sprite not found. Background will stay default.");
                return;
            }

            // Prefer to parent under Play Zone so the backdrop tracks the board area,
            // but fall back to Drop Areas if Play Zone is missing.
            Transform playZone = GameObject.Find("Play Zone")?.transform ?? dropAreasRoot.transform;

            Transform existing = playZone.Find("BattlegroundBackdrop");
            GameObject backdropObj = existing != null ? existing.gameObject : new GameObject("BattlegroundBackdrop");
            if (existing == null)
            {
                backdropObj.transform.SetParent(playZone, false);
            }
            else if (backdropObj.transform.parent != playZone)
            {
                backdropObj.transform.SetParent(playZone, false);
            }

            // Configure the background sprite so it always sits behind the board and cards.
            SpriteRenderer renderer = backdropObj.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = backdropObj.AddComponent<SpriteRenderer>();
            }
            renderer.color = Color.white;
            renderer.sortingOrder = -200; // behind ProceduralBoardBackdrop (-100) and cards

            // Scale/position background so the full sprite fits the camera view.
            Camera cam = Camera.main;
            if (cam != null)
            {
                float worldHeight = cam.orthographicSize * 2f;
                float worldWidth = worldHeight * cam.aspect;

                renderer.sprite = sprite;

                float spriteWidth = sprite.bounds.size.x;
                float spriteHeight = sprite.bounds.size.y;
                if (spriteWidth > 0.01f && spriteHeight > 0.01f)
                {
                    float scaleX = worldWidth / spriteWidth;
                    float scaleY = worldHeight / spriteHeight;

                    // Use the larger scale so the entire camera view is filled without gaps
                    float uniformScale = Mathf.Max(scaleX, scaleY);

                    Vector3 parentScale = playZone.lossyScale;
                    float adjustedScale = uniformScale;
                    if (parentScale.x != 0f && parentScale.y != 0f)
                    {
                        adjustedScale = uniformScale / Mathf.Max(parentScale.x, parentScale.y);
                    }

                    // Add a small margin to ensure no blue bars show on the edges
                    adjustedScale *= 1.05f;

                    backdropObj.transform.localScale = new Vector3(adjustedScale, adjustedScale, 1f);
                }
                else
                {
                    backdropObj.transform.localScale = Vector3.one;
                }

                Vector3 cameraWorldPos = cam.transform.position;
                Vector3 localPos = playZone.InverseTransformPoint(new Vector3(cameraWorldPos.x, cameraWorldPos.y, playZone.position.z));
                backdropObj.transform.localPosition = new Vector3(localPos.x, localPos.y, 0f);
            }
            else
            {
                renderer.sprite = sprite;
                backdropObj.transform.localScale = Vector3.one;
                backdropObj.transform.localPosition = Vector3.zero;
            }
        }

        private Sprite LoadBattlegroundSprite()
        {
#if UNITY_EDITOR
            Sprite editorSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Backgrounds/Battle_Grounds.png");
            if (editorSprite != null)
            {
                return editorSprite;
            }
#endif
            return Resources.Load<Sprite>("Backgrounds/Battle_Grounds");
        }

        // (Removed CreateScaledBackgroundSprite helper – not needed when scaling uniformly)

        private void BringCoinTossPanelToFront(Transform hudRoot)
        {
            if (hudRoot == null) return;
            Transform coinPanel = hudRoot.Find("CoinTossPanel");
            if (coinPanel != null)
            {
                coinPanel.SetAsLastSibling();
            }
        }
    }
}

