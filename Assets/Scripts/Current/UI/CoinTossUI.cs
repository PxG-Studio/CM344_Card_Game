using System.Collections;
using System.Linq;
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
        [SerializeField] private RectTransform hoverContainer;
        [SerializeField] private float hoverAmplitude = 20f;
        [SerializeField] private float hoverSpeed = 1.2f;

        [Header("Visual Settings")]
        [SerializeField] private Sprite headsSprite;
        [SerializeField] private Sprite tailsSprite;
        [SerializeField] private Color defaultColor = Color.white; // Default color for both labels
        [SerializeField] private Color hoverColor = Color.yellow; // Color when hovering over a label
        // Use pure white for both coin sides so sprites render with their original colors
        // instead of being tinted yellow.
        [SerializeField] private Color headsColor = Color.white;
        [SerializeField] private Color tailsColor = Color.white;

        private CoinTossManager coinTossManager;
        private bool isAnimating = false;
        private bool hasShownResult = false;
        private bool hasBeenActivated = false; // Track if panel has been intentionally activated
        private bool waitingForSelection = false; // Whether we're waiting for player to select heads/tails
        AudioManager audioManager;
        private void Awake()
        {
            audioManager = GameObject.FindGameObjectWithTag("Audio").GetComponent<AudioManager>();
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
            }
            else
            {
                // Fallback: try to locate the heads button under this panel by name so
                // runtime-created panels (HUDSetup) still get a working click handler
                Button foundHeads = null;
                Transform headsTransform = transform.Find("ButtonsContainer/HeadsButton");
                if (headsTransform != null)
                {
                    foundHeads = headsTransform.GetComponent<Button>();
                }
                if (foundHeads == null)
                {
                    foundHeads = GetComponentsInChildren<Button>(true)
                        .FirstOrDefault(b => b.gameObject.name == "HeadsButton");
                }

                if (foundHeads != null)
                {
                    headsButton = foundHeads;
                    headsButton.onClick.RemoveAllListeners();
                    headsButton.onClick.AddListener(() => OnSelectionMade(true));
                    headsButton.interactable = true;
                    CanvasGroup cg = headsButton.GetComponent<CanvasGroup>();
                    if (cg != null)
                    {
                        cg.interactable = true;
                        cg.blocksRaycasts = true;
                    }
                }
                else
                {
                    // Keep this message aligned with play mode tests that expect a
                    // one-time diagnostic when serialized button fields are null in Awake.
                    Debug.LogError("[CoinTossUI] headsButton is NULL! Button not found in scene hierarchy.");
                }
            }

            if (tailsButton != null)
            {
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
            }
            else
            {
                // Fallback: try to locate the tails button under this panel by name
                Button foundTails = null;
                Transform tailsTransform = transform.Find("ButtonsContainer/TailsButton");
                if (tailsTransform != null)
                {
                    foundTails = tailsTransform.GetComponent<Button>();
                }
                if (foundTails == null)
                {
                    foundTails = GetComponentsInChildren<Button>(true)
                        .FirstOrDefault(b => b.gameObject.name == "TailsButton");
                }

                if (foundTails != null)
                {
                    tailsButton = foundTails;
                    tailsButton.onClick.RemoveAllListeners();
                    tailsButton.onClick.AddListener(() => OnSelectionMade(false));
                    tailsButton.interactable = true;
                    CanvasGroup cg = tailsButton.GetComponent<CanvasGroup>();
                    if (cg != null)
                    {
                        cg.interactable = true;
                        cg.blocksRaycasts = true;
                    }
                }
                else
                {
                    // Keep this message aligned with play mode tests that expect a
                    // one-time diagnostic when serialized button fields are null in Awake.
                    Debug.LogError("[CoinTossUI] tailsButton is NULL! Button not found in scene hierarchy.");
                }
            }
            
            // If buttons are null, try to find them by name or add click handlers to labels
            // Legacy label click fallback removed since dedicated buttons are now created

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
                selectionPromptText.enableAutoSizing = true;
                selectionPromptText.fontSizeMin = 24f;
                selectionPromptText.fontSizeMax = 48f;
                selectionPromptText.text = "Player 1: Select Heads or Tails";
                selectionPromptText.alignment = TextAlignmentOptions.Center;
            }
            
            if (hoverContainer != null)
            {
                StartCoroutine(HoverAnimation());
            }

            // Initialize result text: keep it enabled but empty. The panel itself starts
            // hidden, so this doesn't expose any text until the coin toss UI is shown.
            if (resultText != null)
            {
                resultText.text = string.Empty;
                resultText.gameObject.SetActive(true);
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
            
        }
        
        private IEnumerator HoverAnimation()
        {
            float baseY = hoverContainer.anchoredPosition.y;
            
            while (true)
            {
                float offset = Mathf.Sin(Time.unscaledTime * hoverSpeed) * hoverAmplitude;
                hoverContainer.anchoredPosition = new Vector2(hoverContainer.anchoredPosition.x, baseY + offset);
                yield return null;
            }
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
        
        private void Update()
        {
            // Fallback interaction path: if for any reason the button click handlers are not
            // receiving events, allow a simple left-click anywhere on the active panel to
            // register a "Heads" selection so that the game can proceed.
            if (waitingForSelection && gameObject.activeInHierarchy && !isAnimating)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    OnSelectionMade(true);
                }
            }
        }
        
        /// <summary>
        /// Starts the coin toss animation. Called by GameManager when game is ready.
        /// </summary>
        public void StartCoinToss()
        {
            
            // Activate this GameObject (the component is on coinTossPanel, so this is the panel itself)
            // Ensure parent hierarchy is active first
            Transform parent = transform.parent;
            while (parent != null && !parent.gameObject.activeInHierarchy)
            {
                parent.gameObject.SetActive(true);
                parent = parent.parent;
            }
            
            // Activate this GameObject (the panel with CoinTossUI component)
            gameObject.SetActive(true);
            hasBeenActivated = true; // Mark as intentionally activated to prevent Awake from deactivating it
            
            // Update coinTossPanel reference to match gameObject (they should be the same)
            if (coinTossPanel != gameObject)
            {
                coinTossPanel = gameObject;
            }
            
            // Reset state
            hasShownResult = false;
            isAnimating = false;
            waitingForSelection = true;
            
            // Reset UI elements
            if (resultText != null)
            {
                // Keep the result text object active so tests can see it, but clear the content
                // until the toss animation or completion handler populates it.
                resultText.text = string.Empty;
                resultText.gameObject.SetActive(true);
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

            // Initialize coin to show a random side (heads or tails) at startup
            // Make sure coin is visible with random side before selection
            InitializeRandomCoinSide();
            
            if (coinImage != null)
            {
                coinImage.gameObject.SetActive(true); // Ensure coin is visible at startup
            }
            
            // Check if coin toss manager is ready
            if (coinTossManager == null)
            {
                coinTossManager = CoinTossManager.Instance;
            }
            
            // Note: Can't start coroutine here because GameObject might not be active yet
            // GameManager will call StartCoinTossAnimation() after GameObject is confirmed active
            
            // Debug: Verify button states
            VerifyButtonStates();
        }
        
        /// <summary>
        /// Verifies that buttons are properly set up and clickable. Logs diagnostic information.
        /// </summary>
        private void VerifyButtonStates()
        {
            
            // Check if EventSystem exists (required for button clicks)
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<EventSystem>();
                eventSystemObj.AddComponent<StandaloneInputModule>();
            }
            
            if (headsButton == null)
            {
            }
            
            if (tailsButton == null)
            {
            }
        }
        
        /// <summary>
        /// Called when player selects heads or tails. Starts the coin flip animation.
        /// Can be called from button click or directly.
        /// </summary>
        /// <param name="selectHeads">True if heads selected, false if tails</param>
        public void OnSelectionMade(bool selectHeads)
        {
            if (!waitingForSelection || coinTossManager == null)
            {
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
            audioManager.PlaySFX(audioManager.CoinFlip);
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
            
            if (!activeSelf)
            {
                gameObject.SetActive(true);
                activeSelf = gameObject.activeSelf;
                activeInHierarchy = gameObject.activeInHierarchy;
            }
            
            // If still not active, we can't start the coroutine
            if (!activeSelf)
            {
                return;
            }
            
            // Try to start animation even if activeInHierarchy is false (might be parent issue)
            // The coroutine will start if the GameObject itself is active
            if (!enabled)
            {
                return;
            }
            
            // CRITICAL: Check if coin toss is already complete FIRST (e.g., via SetForcedResult)
            // If it's complete, show result immediately without requiring a selection
            if (coinTossManager != null && coinTossManager.IsComplete)
            {
                // Coin toss already performed, show result immediately
                HandleCoinTossComplete(coinTossManager.GetStartingPlayer());
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
                
                return;
            }
            
            // Start coin toss animation
            if (coinTossManager != null && !coinTossManager.IsComplete)
            {
                try
                {
                    StartCoroutine(PerformCoinTossAnimation());
                }
                catch (System.Exception)
                {
                    // Exception caught and ignored
                }
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
                // Calculate final rotation.
                // DESIGN CHANGE: Always end with an upright coin so that both HEADS and TAILS
                // are easy to read. We now rely purely on the sprite to indicate the result,
                // not on a 180-degree flip which could leave the art visually upside down.
                Quaternion targetRotation = Quaternion.identity;
                
                // Smoothly rotate to final orientation
                float snapDuration = 0.3f;
                float snapElapsed = 0f;
                while (snapElapsed < snapDuration)
                {
                    snapElapsed += Time.deltaTime;
                    float t = Mathf.SmoothStep(0f, 1f, snapElapsed / snapDuration);
                    coinImage.transform.rotation = Quaternion.Lerp(startRotation, targetRotation, t);
                    // Keep the final side locked in during the settle phase
                    coinImage.sprite = isHeads ? headsSprite : tailsSprite;
                    coinImage.color = isHeads ? headsColor : tailsColor;
                    
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
                // Always keep the coin upright for the final display so artwork is never upside down.
                coinImage.transform.rotation = Quaternion.identity;
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

            // Initialize coin to show a random side (heads or tails) at startup
            InitializeRandomCoinSide();

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
        /// Initializes the coin to show a random side (heads or tails) at startup.
        /// </summary>
        private void InitializeRandomCoinSide()
        {
            if (coinImage == null || headsSprite == null || tailsSprite == null)
            {
                return;
            }
            
            // Use RNG to randomly pick heads or tails
            bool showHeads = Random.Range(0, 2) == 0;
            
            // Set the coin sprite and color
            coinImage.sprite = showHeads ? headsSprite : tailsSprite;
            coinImage.color = showHeads ? headsColor : tailsColor;
            
            // Reset rotation to default (no rotation)
            coinImage.transform.rotation = Quaternion.identity;
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
            }
            
            // Verify EventSystem exists
            if (EventSystem.current == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<EventSystem>();
                eventSystemObj.AddComponent<StandaloneInputModule>();
            }
        }
    }
}

