using System.Collections;
using UnityEngine;
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
        [SerializeField] private Color headsColor = Color.yellow;
        [SerializeField] private Color tailsColor = Color.white;

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
                headsButton.onClick.AddListener(() => OnSelectionMade(true));
            }

            if (tailsButton != null)
            {
                tailsButton.onClick.AddListener(() => OnSelectionMade(false));
            }

            // Setup labels
            if (headsLabel != null)
            {
                headsLabel.text = "HEADS";
                headsLabel.color = headsColor;
            }

            if (tailsLabel != null)
            {
                tailsLabel.text = "TAILS";
                tailsLabel.color = tailsColor;
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

            if (headsButton != null)
            {
                headsButton.gameObject.SetActive(true);
            }

            if (tailsButton != null)
            {
                tailsButton.gameObject.SetActive(true);
            }

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
        }
        
        /// <summary>
        /// Called when player selects heads or tails. Starts the coin flip animation.
        /// </summary>
        /// <param name="selectHeads">True if heads selected, false if tails</param>
        private void OnSelectionMade(bool selectHeads)
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
                if (headsButton != null)
                {
                    headsButton.gameObject.SetActive(true);
                }
                if (tailsButton != null)
                {
                    tailsButton.gameObject.SetActive(true);
                }
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

            // Display result text
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

            if (resultText != null)
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

            if (headsButton != null)
            {
                headsButton.gameObject.SetActive(true);
            }

            if (tailsButton != null)
            {
                tailsButton.gameObject.SetActive(true);
            }

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
    }
}

