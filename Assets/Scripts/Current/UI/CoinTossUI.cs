using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;
using CardGame.Managers;

namespace CardGame.UI
{
    /// <summary>
    /// Manages the visual coin toss UI, including animation and result display.
    /// </summary>
    public class CoinTossUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject coinTossPanel;
        [SerializeField] private TextMeshProUGUI resultText;
        [SerializeField] private TextMeshProUGUI headsLabel;
        [SerializeField] private TextMeshProUGUI tailsLabel;
        [SerializeField] private Image coinImage;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button headsButton;
        [SerializeField] private Button tailsButton;
        [SerializeField] private GameObject selectionPanel; // Panel showing heads/tails buttons
        [SerializeField] private TextMeshProUGUI selectionPromptText; // Text prompting player to select

        [Header("Animation Settings")]
        [SerializeField] private float animationDuration = 2f;
        [SerializeField] private int spinCount = 5;
        [SerializeField] private int flipCount = 4; // Number of end-over-end flips (X-axis)
        [SerializeField] private AnimationCurve spinCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Visual Settings")]
        [SerializeField] private Sprite headsSprite;
        [SerializeField] private Sprite tailsSprite;
        [SerializeField] private Color defaultColor = Color.white; // Default color for both labels
        [SerializeField] private Color hoverColor = Color.yellow; // Color when hovering over a label
        [SerializeField] private Color headsColor = Color.yellow; // Legacy - kept for compatibility
        [SerializeField] private Color tailsColor = Color.white; // Legacy - kept for compatibility

        private CoinTossManager coinTossManager;
        private bool isAnimating = false;
        private bool hasShownResult = false;
        private bool hasBeenActivated = false; // Track if panel has been intentionally activated
        private bool waitingForSelection = false; // Whether we're waiting for player to select heads/tails

        private void Awake()
        {
            // Auto-find CoinTossManager
            if (coinTossManager == null)
            {
                coinTossManager = CoinTossManager.Instance;
            }

            // Subscribe to coin toss events
            if (coinTossManager != null)
            {
                coinTossManager.OnCoinTossComplete += HandleCoinTossComplete;
            }

            // Hide panel initially (but don't deactivate if it's this GameObject or if already activated)
            // Since the component is ON the panel, coinTossPanel and gameObject are the same
            // We only set inactive if coinTossPanel is a different GameObject and hasn't been activated yet
            if (coinTossPanel != null && coinTossPanel != gameObject && !hasBeenActivated)
            {
                coinTossPanel.SetActive(false);
            }
            // Note: If coinTossPanel == gameObject, don't set inactive here as it's already inactive from HUDSetup

            // Setup continue button
            if (continueButton != null)
            {
                continueButton.onClick.AddListener(OnContinueButtonClicked);
                continueButton.gameObject.SetActive(false);
            }

            // Setup heads/tails selection buttons
            if (headsButton != null)
            {
                // Remove any existing listeners to avoid duplicates
                headsButton.onClick.RemoveAllListeners();
                headsButton.onClick.AddListener(() => OnSelectionMade(true));
                headsButton.interactable = true; // Ensure button is interactable
                // Ensure button's CanvasGroup (if any) allows interaction
                CanvasGroup cg = headsButton.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    cg.interactable = true;
                    cg.blocksRaycasts = true;
                }
                Debug.Log($"[CoinTossUI] Heads button setup complete: interactable={headsButton.interactable}, enabled={headsButton.enabled}");
            }
            else
            {
                Debug.LogError("[CoinTossUI] headsButton is NULL! Cannot set up click handler. Please assign headsButton in Inspector.");
            }

            if (tailsButton != null)
            {
                // Remove any existing listeners to avoid duplicates
                tailsButton.onClick.RemoveAllListeners();
                tailsButton.onClick.AddListener(() => OnSelectionMade(false));
                tailsButton.interactable = true; // Ensure button is interactable
                // Ensure button's CanvasGroup (if any) allows interaction
                CanvasGroup cg = tailsButton.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    cg.interactable = true;
                    cg.blocksRaycasts = true;
                }
                Debug.Log($"[CoinTossUI] Tails button setup complete: interactable={tailsButton.interactable}, enabled={tailsButton.enabled}");
            }
            else
            {
                Debug.LogError("[CoinTossUI] tailsButton is NULL! Cannot set up click handler. Please assign tailsButton in Inspector.");
            }
            
            // If buttons are null, try to find them by name or add click handlers to labels
            if (headsButton == null && headsLabel != null)
            {
                Debug.LogWarning("[CoinTossUI] headsButton is null but headsLabel exists. Trying to add Button component to label...");
                // Try to find or add Button component to the label's GameObject
                Button btn = headsLabel.GetComponent<Button>();
                if (btn == null)
                {
                    btn = headsLabel.GetComponentInParent<Button>();
                }
                if (btn != null)
                {
                    headsButton = btn;
                    headsButton.onClick.RemoveAllListeners();
                    headsButton.onClick.AddListener(() => OnSelectionMade(true));
                    headsButton.interactable = true;
                    Debug.Log("[CoinTossUI] Found Button component on headsLabel GameObject");
                }
                else
                {
                    // Add EventTrigger to make label clickable
                    try
                    {
                        UnityEngine.EventSystems.EventTrigger trigger = headsLabel.GetComponent<UnityEngine.EventSystems.EventTrigger>();
                        if (trigger == null)
                        {
                            trigger = headsLabel.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
                            Debug.Log("[CoinTossUI] Created EventTrigger component on headsLabel");
                        }
                        // Clear existing triggers to avoid duplicates
                        trigger.triggers.Clear();
                        var entry = new UnityEngine.EventSystems.EventTrigger.Entry();
                        entry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerClick;
                        entry.callback.AddListener((data) => { 
                            Debug.Log("[CoinTossUI] Heads label clicked via EventTrigger!");
                            OnSelectionMade(true); 
                        });
                        trigger.triggers.Add(entry);
                        
                        // CRITICAL: Ensure label can receive raycasts
                        headsLabel.raycastTarget = true;
                        
                        // Ensure the GameObject is active and can receive events
                        headsLabel.gameObject.SetActive(true);
                        
                        Debug.Log($"[CoinTossUI] ✓ Added EventTrigger to headsLabel. raycastTarget={headsLabel.raycastTarget}, active={headsLabel.gameObject.activeInHierarchy}, triggerCount={trigger.triggers.Count}");
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"[CoinTossUI] Failed to add EventTrigger to headsLabel: {e.Message}\n{e.StackTrace}");
                    }
                }
            }
            
            if (tailsButton == null && tailsLabel != null)
            {
                Debug.LogWarning("[CoinTossUI] tailsButton is null but tailsLabel exists. Trying to add Button component to label...");
                // Try to find or add Button component to the label's GameObject
                Button btn = tailsLabel.GetComponent<Button>();
                if (btn == null)
                {
                    btn = tailsLabel.GetComponentInParent<Button>();
                }
                if (btn != null)
                {
                    tailsButton = btn;
                    tailsButton.onClick.RemoveAllListeners();
                    tailsButton.onClick.AddListener(() => OnSelectionMade(false));
                    tailsButton.interactable = true;
                    Debug.Log("[CoinTossUI] Found Button component on tailsLabel GameObject");
                }
                else
                {
                    // Add EventTrigger to make label clickable
                    try
                    {
                        UnityEngine.EventSystems.EventTrigger trigger = tailsLabel.GetComponent<UnityEngine.EventSystems.EventTrigger>();
                        if (trigger == null)
                        {
                            trigger = tailsLabel.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
                            Debug.Log("[CoinTossUI] Created EventTrigger component on tailsLabel");
                        }
                        // Clear existing triggers to avoid duplicates
                        trigger.triggers.Clear();
                        var entry = new UnityEngine.EventSystems.EventTrigger.Entry();
                        entry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerClick;
                        entry.callback.AddListener((data) => { 
                            Debug.Log("[CoinTossUI] Tails label clicked via EventTrigger!");
                            OnSelectionMade(false); 
                        });
                        trigger.triggers.Add(entry);
                        
                        // CRITICAL: Ensure label can receive raycasts
                        tailsLabel.raycastTarget = true;
                        
                        // Ensure the GameObject is active and can receive events
                        tailsLabel.gameObject.SetActive(true);
                        
                        Debug.Log($"[CoinTossUI] ✓ Added EventTrigger to tailsLabel. raycastTarget={tailsLabel.raycastTarget}, active={tailsLabel.gameObject.activeInHierarchy}, triggerCount={trigger.triggers.Count}");
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"[CoinTossUI] Failed to add EventTrigger to tailsLabel: {e.Message}\n{e.StackTrace}");
                    }
                }
            }

            // Setup labels with default color (no yellow by default)
            if (headsLabel != null)
            {
                headsLabel.text = "HEADS";
                headsLabel.color = defaultColor; // Start with default color, not yellow
                headsLabel.raycastTarget = true; // Ensure label can receive clicks
                SetupHoverEffect(headsLabel); // Add hover effect
            }

            if (tailsLabel != null)
            {
                tailsLabel.text = "TAILS";
                tailsLabel.color = defaultColor; // Start with default color
                tailsLabel.raycastTarget = true; // Ensure label can receive clicks
                SetupHoverEffect(tailsLabel); // Add hover effect
            }

            // Setup selection prompt
            if (selectionPromptText != null)
            {
                selectionPromptText.text = "Player 1: Select Heads or Tails";
            }

            // Hide result text initially
            if (resultText != null)
            {
                resultText.gameObject.SetActive(false);
            }
        }
        
        /// <summary>
        /// Sets up hover effects for a label - changes color to yellow on hover, back to default on exit.
        /// </summary>
        /// <param name="label">The TextMeshProUGUI label to add hover effects to</param>
        private void SetupHoverEffect(TextMeshProUGUI label)
        {
            if (label == null) return;
            
            // Get or add EventTrigger component
            EventTrigger trigger = label.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = label.gameObject.AddComponent<EventTrigger>();
            }
            
            // Check if hover effects already exist to avoid duplicates
            bool hasPointerEnter = false;
            bool hasPointerExit = false;
            foreach (var entry in trigger.triggers)
            {
                if (entry.eventID == EventTriggerType.PointerEnter) hasPointerEnter = true;
                if (entry.eventID == EventTriggerType.PointerExit) hasPointerExit = true;
            }
            
            // Store default color
            Color originalColor = defaultColor;
            
            // Add PointerEnter event (hover start) if it doesn't exist
            if (!hasPointerEnter)
            {
                EventTrigger.Entry pointerEnter = new EventTrigger.Entry();
                pointerEnter.eventID = EventTriggerType.PointerEnter;
                pointerEnter.callback.AddListener((data) => {
                    label.color = hoverColor; // Change to yellow on hover
                });
                trigger.triggers.Add(pointerEnter);
            }
            
            // Add PointerExit event (hover end) if it doesn't exist
            if (!hasPointerExit)
            {
                EventTrigger.Entry pointerExit = new EventTrigger.Entry();
                pointerExit.eventID = EventTriggerType.PointerExit;
                pointerExit.callback.AddListener((data) => {
                    label.color = originalColor; // Change back to default color
                });
                trigger.triggers.Add(pointerExit);
            }
            
            Debug.Log($"[CoinTossUI] Added hover effect to '{label.text}' label. Default: {originalColor}, Hover: {hoverColor}");
        }

        private void Start()
        {
            // Don't auto-start coin toss - wait for GameManager to call StartCoinToss()
            // This prevents duplicate execution warnings
            
            // Hide panel initially - will be shown when StartCoinToss() is called
            // Make sure panel is inactive at start
            if (coinTossPanel != null)
            {
                coinTossPanel.SetActive(false);
            }
        }
        
        /// <summary>
        /// Starts the coin toss animation. Called by GameManager when game is ready.
        /// </summary>
        public void StartCoinToss()
        {
            Debug.Log("[CoinTossUI] StartCoinToss() called.");
            
            // Activate this GameObject (the component is on coinTossPanel, so this is the panel itself)
            // Ensure parent hierarchy is active first
            Transform parent = transform.parent;
            while (parent != null && !parent.gameObject.activeInHierarchy)
            {
                Debug.LogWarning($"[CoinTossUI] Parent '{parent.name}' is inactive. Activating...");
                parent.gameObject.SetActive(true);
                parent = parent.parent;
            }
            
            // Activate this GameObject (the panel with CoinTossUI component)
            gameObject.SetActive(true);
            hasBeenActivated = true; // Mark as intentionally activated to prevent Awake from deactivating it
            Debug.Log($"[CoinTossUI] Activated gameObject '{gameObject.name}' (activeSelf: {gameObject.activeSelf}, activeInHierarchy: {gameObject.activeInHierarchy})");
            
            // Update coinTossPanel reference to match gameObject (they should be the same)
            if (coinTossPanel != gameObject)
            {
                Debug.LogWarning($"[CoinTossUI] coinTossPanel field ({coinTossPanel?.name}) != gameObject ({gameObject.name}). Updating reference.");
                coinTossPanel = gameObject;
            }
            
            // Reset state
            hasShownResult = false;
            isAnimating = false;
            waitingForSelection = true;
            
            // Reset UI elements
            if (resultText != null)
            {
                resultText.gameObject.SetActive(false);
            }
            
            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(false);
            }

            // Show selection panel and hide coin initially
            if (selectionPanel != null)
            {
                selectionPanel.SetActive(true);
            }

            // Force enable buttons to ensure they're clickable
            ForceEnableButtons();

            if (coinImage != null)
            {
                coinImage.gameObject.SetActive(false);
            }
            
            // Check if coin toss manager is ready
            if (coinTossManager == null)
            {
                coinTossManager = CoinTossManager.Instance;
            }
            
            // Note: Can't start coroutine here because GameObject might not be active yet
            // GameManager will call StartCoinTossAnimation() after GameObject is confirmed active
            Debug.Log("[CoinTossUI] Panel activated. Waiting for GameManager to start animation after GameObject is confirmed active.");
            
            // Debug: Verify button states
            VerifyButtonStates();
        }
        
        /// <summary>
        /// Verifies that buttons are properly set up and clickable. Logs diagnostic information.
        /// </summary>
        private void VerifyButtonStates()
        {
            Debug.Log("[CoinTossUI] Verifying button states...");
            
            // Check if EventSystem exists (required for button clicks)
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                Debug.LogError("[CoinTossUI] EventSystem is missing! Buttons will not work. Creating EventSystem...");
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<EventSystem>();
                eventSystemObj.AddComponent<StandaloneInputModule>();
            }
            else
            {
                Debug.Log($"[CoinTossUI] EventSystem found: {eventSystem.name}, enabled={eventSystem.enabled}");
            }
            
            if (headsButton == null)
            {
                Debug.LogError("[CoinTossUI] headsButton is NULL! Button must be assigned in Inspector.");
            }
            else
            {
                Debug.Log($"[CoinTossUI] headsButton: activeSelf={headsButton.gameObject.activeSelf}, " +
                    $"activeInHierarchy={headsButton.gameObject.activeInHierarchy}, " +
                    $"interactable={headsButton.interactable}, " +
                    $"enabled={headsButton.enabled}, " +
                    $"listeners={headsButton.onClick.GetPersistentEventCount()}");
                
                // Check for blocking CanvasGroup
                CanvasGroup cg = headsButton.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    Debug.Log($"[CoinTossUI] headsButton CanvasGroup: interactable={cg.interactable}, blocksRaycasts={cg.blocksRaycasts}, alpha={cg.alpha}");
                }
                
                // Check parent CanvasGroups
                CanvasGroup parentCg = headsButton.GetComponentInParent<CanvasGroup>();
                if (parentCg != null && parentCg != cg)
                {
                    Debug.Log($"[CoinTossUI] headsButton parent CanvasGroup: interactable={parentCg.interactable}, blocksRaycasts={parentCg.blocksRaycasts}, alpha={parentCg.alpha}");
                }
            }
            
            if (tailsButton == null)
            {
                Debug.LogError("[CoinTossUI] tailsButton is NULL! Button must be assigned in Inspector.");
            }
            else
            {
                Debug.Log($"[CoinTossUI] tailsButton: activeSelf={tailsButton.gameObject.activeSelf}, " +
                    $"activeInHierarchy={tailsButton.gameObject.activeInHierarchy}, " +
                    $"interactable={tailsButton.interactable}, " +
                    $"enabled={tailsButton.enabled}, " +
                    $"listeners={tailsButton.onClick.GetPersistentEventCount()}");
                
                // Check for blocking CanvasGroup
                CanvasGroup cg = tailsButton.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    Debug.Log($"[CoinTossUI] tailsButton CanvasGroup: interactable={cg.interactable}, blocksRaycasts={cg.blocksRaycasts}, alpha={cg.alpha}");
                }
                
                // Check parent CanvasGroups
                CanvasGroup parentCg = tailsButton.GetComponentInParent<CanvasGroup>();
                if (parentCg != null && parentCg != cg)
                {
                    Debug.Log($"[CoinTossUI] tailsButton parent CanvasGroup: interactable={parentCg.interactable}, blocksRaycasts={parentCg.blocksRaycasts}, alpha={parentCg.alpha}");
                }
            }
        }
        
        /// <summary>
        /// Called when player selects heads or tails. Starts the coin flip animation.
        /// Can be called from button click or directly.
        /// </summary>
        /// <param name="selectHeads">True if heads selected, false if tails</param>
        public void OnSelectionMade(bool selectHeads)
        {
            Debug.Log($"[CoinTossUI] OnSelectionMade called: selectHeads={selectHeads}, waitingForSelection={waitingForSelection}, coinTossManager={(coinTossManager != null ? "exists" : "null")}");
            
            if (!waitingForSelection || coinTossManager == null)
            {
                Debug.LogWarning($"[CoinTossUI] OnSelectionMade ignored: waitingForSelection={waitingForSelection}, coinTossManager={(coinTossManager != null ? "exists" : "null")}");
                return;
            }

            // Determine which player is making the selection (default to Player 1 for now)
            // TODO: In multiplayer, this should be determined by turn order or player input
            FateSide selectingPlayer = FateSide.Player; // Player 1 selects first

            // Set the player's selection
            coinTossManager.SetPlayerSelection(selectHeads, selectingPlayer);

            // Hide selection buttons
            if (headsButton != null)
            {
                headsButton.gameObject.SetActive(false);
            }

            if (tailsButton != null)
            {
                tailsButton.gameObject.SetActive(false);
            }

            if (selectionPanel != null)
            {
                selectionPanel.SetActive(false);
            }

            if (selectionPromptText != null)
            {
                selectionPromptText.gameObject.SetActive(false);
            }

            // Show coin and start animation
            if (coinImage != null)
            {
                coinImage.gameObject.SetActive(true);
            }

            waitingForSelection = false;

            // Start the coin flip animation
            StartCoinTossAnimation();
        }

        /// <summary>
        /// Starts the coin toss animation. Can be called directly or from coroutine.
        /// </summary>
        public void StartCoinTossAnimation()
        {
            // Check if coin toss manager is ready
            if (coinTossManager == null)
            {
                coinTossManager = CoinTossManager.Instance;
            }
            
            // Verify GameObject is active before starting coroutine
            // Check both activeSelf and activeInHierarchy to diagnose issues
            bool activeSelf = gameObject.activeSelf;
            bool activeInHierarchy = gameObject.activeInHierarchy;
            
            Debug.Log($"[CoinTossUI] StartCoinTossAnimation() called - activeSelf: {activeSelf}, activeInHierarchy: {activeInHierarchy}, enabled: {enabled}");
            
            if (!activeSelf)
            {
                Debug.LogWarning($"[CoinTossUI] GameObject is not active (activeSelf: false). Activating now...");
                gameObject.SetActive(true);
                activeSelf = gameObject.activeSelf;
                activeInHierarchy = gameObject.activeInHierarchy;
                Debug.Log($"[CoinTossUI] After activation - activeSelf: {activeSelf}, activeInHierarchy: {activeInHierarchy}");
            }
            
            // If still not active, we can't start the coroutine
            if (!activeSelf)
            {
                Debug.LogError($"[CoinTossUI] GameObject activation failed! Cannot start animation.");
                return;
            }
            
            // Try to start animation even if activeInHierarchy is false (might be parent issue)
            // The coroutine will start if the GameObject itself is active
            if (!enabled)
            {
                Debug.LogError($"[CoinTossUI] Cannot start coin toss animation - component is not enabled!");
                return;
            }
            
            // Check if player has made a selection
            if (coinTossManager != null && !coinTossManager.HasSelection)
            {
                // No selection made yet - show selection UI
                waitingForSelection = true;
                if (selectionPanel != null)
                {
                    selectionPanel.SetActive(true);
                }
                
                // Force enable buttons to ensure they're clickable
                ForceEnableButtons();
                
                Debug.Log("[CoinTossUI] Waiting for player to select heads or tails...");
                return;
            }
            
            // Start coin toss animation
            if (coinTossManager != null && !coinTossManager.IsComplete)
            {
                Debug.Log("[CoinTossUI] Starting coin toss animation...");
                try
                {
                    StartCoroutine(PerformCoinTossAnimation());
                    Debug.Log("[CoinTossUI] Coin toss animation coroutine started successfully.");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[CoinTossUI] Failed to start coin toss animation coroutine: {e.Message}");
                }
            }
            else if (coinTossManager != null && coinTossManager.IsComplete)
            {
                // Coin toss already performed, show result immediately
                Debug.Log("[CoinTossUI] Coin toss already performed. Showing result immediately.");
                HandleCoinTossComplete(coinTossManager.GetStartingPlayer());
            }
            else
            {
                Debug.LogError("[CoinTossUI] CoinTossManager is null! Cannot perform coin toss.");
            }
        }

        private void OnDestroy()
        {
            if (coinTossManager != null)
            {
                coinTossManager.OnCoinTossComplete -= HandleCoinTossComplete;
            }

            if (continueButton != null)
            {
                continueButton.onClick.RemoveListener(OnContinueButtonClicked);
            }
        }

        /// <summary>
        /// Performs the coin toss animation and displays the result.
        /// </summary>
        private IEnumerator PerformCoinTossAnimation()
        {
            if (isAnimating) yield break;

            isAnimating = true;
            hasShownResult = false;

            // CRITICAL: Hide result text at the start of animation
            if (resultText != null)
            {
                resultText.gameObject.SetActive(false);
            }

            // Perform coin toss if not already performed
            if (coinTossManager != null && !coinTossManager.IsComplete)
            {
                // Perform coin toss now (triggers result)
                coinTossManager.PerformCoinToss();
            }
            
            // Perform coin toss if not already performed (this determines starting player based on selection)
            if (coinTossManager != null && !coinTossManager.IsComplete)
            {
                // Perform coin toss now (triggers result based on selection)
                coinTossManager.PerformCoinToss();
            }
            
            // NOTE: We get the result here but DON'T display it until after animation completes
            // Get the coin toss result (starting player)
            FateSide startingPlayer = coinTossManager.GetStartingPlayer();
            
            // Get the actual flip result (heads or tails)
            bool? flipResult = coinTossManager.GetFlipResult();
            bool isHeads = flipResult.HasValue ? flipResult.Value : (startingPlayer == FateSide.Player);

            // Animate coin spinning
            float elapsed = 0f;

            while (elapsed < animationDuration)
            {
                float normalizedTime = elapsed / animationDuration;
                float curveValue = spinCurve.Evaluate(normalizedTime);

                // Alternate between heads and tails during spin based on X-axis rotation
                if (coinImage != null)
                {
                    // Use X-axis rotation to determine which side is facing camera
                    float xRotation = curveValue * 360f * flipCount;
                    // Show heads when X rotation is in range 0-180 or 360-540, etc.
                    // Show tails when X rotation is in range 180-360 or 540-720, etc.
                    bool showHeads = (Mathf.FloorToInt(xRotation / 180f) % 2) == 0;
                    coinImage.sprite = showHeads ? headsSprite : tailsSprite;
                    coinImage.color = showHeads ? headsColor : tailsColor;
                }

                // 3D Rotation: Rotate coin on multiple axes for realistic coin flip
                if (coinImage != null)
                {
                    // X-axis: End-over-end flip (coin flipping through the air)
                    float rotationX = curveValue * 360f * flipCount;
                    
                    // Y-axis: Horizontal spin (coin spinning horizontally)
                    float rotationY = curveValue * 720f * spinCount;
                    
                    // Z-axis: Optional slight tilt for extra realism (±15 degrees oscillation)
                    float rotationZ = Mathf.Sin(curveValue * Mathf.PI * 4) * 15f;
                    
                    // Apply 3D rotation to coin
                    coinImage.transform.rotation = Quaternion.Euler(rotationX, rotationY, rotationZ);
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            // Show final result - smoothly rotate to final orientation
            if (coinImage != null)
            {
                // Get current rotation
                Quaternion startRotation = coinImage.transform.rotation;
                // Calculate final rotation based on result
                // For heads, show heads sprite at 0 rotation
                // For tails, show tails sprite at 180 degrees X rotation
                Quaternion targetRotation = isHeads ? Quaternion.identity : Quaternion.Euler(180f, 0f, 0f);
                
                // Smoothly rotate to final orientation
                float snapDuration = 0.3f;
                float snapElapsed = 0f;
                while (snapElapsed < snapDuration)
                {
                    snapElapsed += Time.deltaTime;
                    float t = Mathf.SmoothStep(0f, 1f, snapElapsed / snapDuration);
                    coinImage.transform.rotation = Quaternion.Lerp(startRotation, targetRotation, t);
                    
                    // Update sprite during snap rotation
                    float xRotation = coinImage.transform.rotation.eulerAngles.x;
                    bool showHeads = (Mathf.FloorToInt(xRotation / 180f) % 2) == 0;
                    coinImage.sprite = showHeads ? headsSprite : tailsSprite;
                    coinImage.color = showHeads ? headsColor : tailsColor;
                    
                    yield return null;
                }
                
                // Ensure final state
                coinImage.transform.rotation = targetRotation;
                coinImage.sprite = isHeads ? headsSprite : tailsSprite;
                coinImage.color = isHeads ? headsColor : tailsColor;
            }

            // CRITICAL: Wait a brief moment after animation completes before showing result
            // This ensures the coin has fully settled into its final position
            yield return new WaitForSeconds(0.2f);

            // NOW display result text - only after animation is completely done
            if (resultText != null)
            {
                string flipResultString = isHeads ? "HEADS" : "TAILS";
                string startingPlayerString = startingPlayer == FateSide.Player ? "Player 1" : "Player 2";
                string resultString = $"{flipResultString}!\n{startingPlayerString} Goes First";
                resultText.text = resultString;
                resultText.gameObject.SetActive(true);
                Debug.Log($"[CoinTossUI] Result text displayed after animation: {resultString}");
            }

            hasShownResult = true;
            isAnimating = false;

            // Show continue button after a brief delay
            yield return new WaitForSeconds(0.5f);
            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Handles the coin toss complete event (called if coin toss was performed before UI was ready).
        /// </summary>
        private void HandleCoinTossComplete(FateSide startingSide)
        {
            // CRITICAL: Don't show result if animation is currently running
            // The animation coroutine will handle showing the result after it completes
            if (isAnimating)
            {
                Debug.Log("[CoinTossUI] Animation is running - HandleCoinTossComplete will not show result text. Animation will show it when complete.");
                return;
            }
            
            if (hasShownResult) return;

            // Get the actual flip result (heads or tails)
            bool? flipResult = coinTossManager?.GetFlipResult();
            bool isHeads = flipResult.HasValue ? flipResult.Value : (startingSide == FateSide.Player);

            if (coinImage != null)
            {
                coinImage.sprite = isHeads ? headsSprite : tailsSprite;
                coinImage.color = isHeads ? headsColor : tailsColor;
                coinImage.transform.rotation = isHeads ? Quaternion.identity : Quaternion.Euler(180f, 0f, 0f);
            }

            // Only show result text if animation is NOT running (fallback for edge cases)
            // Normally, the animation coroutine will handle showing the result
            if (resultText != null && !isAnimating)
            {
                string flipResultString = isHeads ? "HEADS" : "TAILS";
                string startingPlayerString = startingSide == FateSide.Player ? "Player 1" : "Player 2";
                string resultString = $"{flipResultString}!\n{startingPlayerString} Goes First";
                resultText.text = resultString;
                resultText.gameObject.SetActive(true);
                Debug.Log($"[CoinTossUI] Result text displayed (fallback): {resultString}");
            }

            hasShownResult = true;

            // Show continue button
            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Called when continue button is clicked. Hides the coin toss UI and proceeds with game.
        /// </summary>
        private void OnContinueButtonClicked()
        {
            // Hide panel
            if (coinTossPanel != null)
            {
                coinTossPanel.SetActive(false);
            }

            // Notify that coin toss UI is done (GameManager will proceed)
            Debug.Log("[CoinTossUI] Coin toss UI dismissed. Proceeding with game.");
            
            // Notify GameManager that coin toss UI is complete (if needed)
            // GameManager is already waiting for coin toss completion, so this is mainly for UI cleanup
        }

        /// <summary>
        /// Shows the coin toss UI again (for rematch).
        /// </summary>
        public void Show()
        {
            if (coinTossPanel != null)
            {
                coinTossPanel.SetActive(true);
            }

            if (resultText != null)
            {
                resultText.gameObject.SetActive(false);
            }

            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(false);
            }

            // Reset selection UI
            if (selectionPanel != null)
            {
                selectionPanel.SetActive(true);
            }
            
            // Reset label colors to default (not yellow)
            if (headsLabel != null)
            {
                headsLabel.color = defaultColor;
            }
            if (tailsLabel != null)
            {
                tailsLabel.color = defaultColor;
            }

            // CRITICAL: Force enable buttons/labels to ensure they're clickable
            // This is needed because Show() might be called before Awake() completes,
            // or the EventTriggers might not have been set up properly
            ForceEnableButtons();

            if (selectionPromptText != null)
            {
                selectionPromptText.gameObject.SetActive(true);
            }

            hasShownResult = false;
            isAnimating = false;
            waitingForSelection = true;
        }

        /// <summary>
        /// Hides the coin toss UI.
        /// </summary>
        public void Hide()
        {
            if (coinTossPanel != null)
            {
                coinTossPanel.SetActive(false);
            }
        }
        
        /// <summary>
        /// Forces buttons to be interactable. Call this if buttons aren't responding to clicks.
        /// </summary>
        public void ForceEnableButtons()
        {
            Debug.Log("[CoinTossUI] ForceEnableButtons() called");
            
            if (headsButton != null)
            {
                headsButton.interactable = true;
                headsButton.enabled = true;
                headsButton.gameObject.SetActive(true);
                
                // Remove and re-add listener to ensure it's connected
                headsButton.onClick.RemoveAllListeners();
                headsButton.onClick.AddListener(() => OnSelectionMade(true));
                
                // Check for blocking CanvasGroups
                CanvasGroup[] canvasGroups = headsButton.GetComponentsInParent<CanvasGroup>();
                foreach (CanvasGroup cg in canvasGroups)
                {
                    cg.interactable = true;
                    cg.blocksRaycasts = true;
                }
                
                Debug.Log($"[CoinTossUI] Heads button forced enabled: interactable={headsButton.interactable}, enabled={headsButton.enabled}, active={headsButton.gameObject.activeInHierarchy}");
            }
            else if (headsLabel != null)
            {
                // Make label clickable via EventTrigger
                UnityEngine.EventSystems.EventTrigger trigger = headsLabel.GetComponent<UnityEngine.EventSystems.EventTrigger>();
                if (trigger == null)
                {
                    trigger = headsLabel.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
                }
                trigger.triggers.Clear();
                var entry = new UnityEngine.EventSystems.EventTrigger.Entry();
                entry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerClick;
                entry.callback.AddListener((data) => { 
                    Debug.Log("[CoinTossUI] Heads label clicked via EventTrigger!");
                    OnSelectionMade(true); 
                });
                trigger.triggers.Add(entry);
                
                // Re-add hover effects (they were cleared above)
                SetupHoverEffect(headsLabel);
                
                // Reset color to default
                headsLabel.color = defaultColor;
                
                // CRITICAL: Ensure label can receive raycasts
                headsLabel.raycastTarget = true;
                
                // Ensure the GameObject is active and can receive events
                headsLabel.gameObject.SetActive(true);
                
                // Check for blocking CanvasGroups
                CanvasGroup[] canvasGroups = headsLabel.GetComponentsInParent<CanvasGroup>();
                foreach (CanvasGroup cg in canvasGroups)
                {
                    cg.interactable = true;
                    cg.blocksRaycasts = true;
                }
                
                Debug.Log($"[CoinTossUI] Heads label made clickable via EventTrigger. raycastTarget={headsLabel.raycastTarget}, active={headsLabel.gameObject.activeInHierarchy}");
            }
            
            if (tailsButton != null)
            {
                tailsButton.interactable = true;
                tailsButton.enabled = true;
                tailsButton.gameObject.SetActive(true);
                
                // Remove and re-add listener to ensure it's connected
                tailsButton.onClick.RemoveAllListeners();
                tailsButton.onClick.AddListener(() => OnSelectionMade(false));
                
                // Check for blocking CanvasGroups
                CanvasGroup[] canvasGroups = tailsButton.GetComponentsInParent<CanvasGroup>();
                foreach (CanvasGroup cg in canvasGroups)
                {
                    cg.interactable = true;
                    cg.blocksRaycasts = true;
                }
                
                Debug.Log($"[CoinTossUI] Tails button forced enabled: interactable={tailsButton.interactable}, enabled={tailsButton.enabled}, active={tailsButton.gameObject.activeInHierarchy}");
            }
            else if (tailsLabel != null)
            {
                // Make label clickable via EventTrigger
                UnityEngine.EventSystems.EventTrigger trigger = tailsLabel.GetComponent<UnityEngine.EventSystems.EventTrigger>();
                if (trigger == null)
                {
                    trigger = tailsLabel.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
                }
                trigger.triggers.Clear();
                var entry = new UnityEngine.EventSystems.EventTrigger.Entry();
                entry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerClick;
                entry.callback.AddListener((data) => { 
                    Debug.Log("[CoinTossUI] Tails label clicked via EventTrigger!");
                    OnSelectionMade(false); 
                });
                trigger.triggers.Add(entry);
                
                // Re-add hover effects (they were cleared above)
                SetupHoverEffect(tailsLabel);
                
                // Reset color to default
                tailsLabel.color = defaultColor;
                
                // CRITICAL: Ensure label can receive raycasts
                tailsLabel.raycastTarget = true;
                
                // Ensure the GameObject is active and can receive events
                tailsLabel.gameObject.SetActive(true);
                
                // Check for blocking CanvasGroups
                CanvasGroup[] canvasGroups = tailsLabel.GetComponentsInParent<CanvasGroup>();
                foreach (CanvasGroup cg in canvasGroups)
                {
                    cg.interactable = true;
                    cg.blocksRaycasts = true;
                }
                
                Debug.Log($"[CoinTossUI] Tails label made clickable via EventTrigger. raycastTarget={tailsLabel.raycastTarget}, active={tailsLabel.gameObject.activeInHierarchy}");
            }
            
            // Verify EventSystem exists
            if (EventSystem.current == null)
            {
                Debug.LogError("[CoinTossUI] EventSystem is missing! Creating one...");
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<EventSystem>();
                eventSystemObj.AddComponent<StandaloneInputModule>();
            }
        }
    }
}

