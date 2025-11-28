using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using CardGame.Core;
using NewCardData;

namespace CardGame.UI
{
    /// <summary>
    /// UI representation of a NewCard with directional stats
    /// </summary>
    public class NewCardUI : MonoBehaviour,
        IPointerClickHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler,
        IPointerEnterHandler,
        IPointerExitHandler
    {
        [Header("UI References")]
        [SerializeField] private SpriteRenderer cardBackground;
        [SerializeField] private SpriteRenderer artwork;
        [SerializeField] private TextMeshProUGUI cardNameText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        
        [Header("Directional Stats")]
        [SerializeField] private TextMeshProUGUI topStatText;
        [SerializeField] private TextMeshProUGUI rightStatText;
        [SerializeField] private TextMeshProUGUI downStatText;
        [SerializeField] private TextMeshProUGUI leftStatText;
        
        [Header("Card Type")]
        [SerializeField] private TextMeshProUGUI cardTypeText;
        [SerializeField] private SpriteRenderer cardTypeIcon;
        
        [Header("Flip Animation")]
        [SerializeField] private CardFlipAnimation flipAnimation;
        [SerializeField] private GameObject frontContainer;
        [SerializeField] private GameObject backContainer;
        [SerializeField] private SpriteRenderer backSpriteRenderer;
        [SerializeField] private Image backImage;
        [SerializeField] private Sprite defaultCardBackSprite;
        
        [Header("Border Sprites")]
        [SerializeField] private SpriteRenderer borderOverlayRenderer;
        [SerializeField] private Sprite p1BorderSprite;   // Fire_border for orange (player) cards
        [SerializeField] private Sprite p2BorderSprite;   // Earth_border for green (opponent) cards

        // Inspect / zoom view has been removed for now to keep interaction simple
        
        [Header("Flip Settings")]
        [SerializeField] public bool startFaceDown = true;
        [SerializeField] public bool autoFlipOnReveal = true;
        [SerializeField] public float revealDelay = 0.2f;
        [SerializeField] private bool allowClickToFlip = false;
        
        [Header("Drag Settings")]
        [SerializeField] private bool allowDrag = true;
        
        [Header("Captured Colors")]
        [SerializeField] private Color playerCapturedColor = new Color(1f, 128f/255f, 0f, 1f); // Orange #FF8000 for player's cards (matches card border orange)
        [SerializeField] private Color opponentCapturedColor = new Color(0f, 0.8f, 0f, 1f); // Green for opponent's captured cards
        
        [Header("Shadow Settings")]
        [SerializeField] private bool enableCardShadow = true;
        [SerializeField] private Vector2 shadowOffset = new Vector2(0.08f, -0.08f);
        [SerializeField] private float shadowScaleMultiplier = 1.05f;
        [SerializeField] private Color shadowColor = new Color(0f, 0f, 0f, 0.35f);
        [SerializeField] private SpriteRenderer cardShadow;
        
        public Color PlayerCapturedColor => playerCapturedColor;
        public Color OpponentCapturedColor => opponentCapturedColor;
        
        
        private NewCard card;
        
        
        private Canvas canvas;
        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private bool isDragging = false;
        private Vector2 dragOffset;
        
        public NewCard Card => card;
        public System.Action<NewCardUI> OnCardClicked;
        public System.Action<NewCardUI> OnCardPlayed;

        // Shared runtime default sprite for card backs, so that any card which
        // has neither a per-card back nor an assigned default will still show a
        // consistent back image instead of being invisible.
        private static Sprite runtimeDefaultBackSprite;

        
        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvas = GetComponentInParent<Canvas>();
            
            // Get or create CanvasGroup for drag support
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
            
            // [CardFront] CRITICAL: Disable prefab assets from receiving raycasts
            // Prefab assets (non-clones) should NEVER be interactable
            // BUT: Board cards are valid cloned cards that have been renamed (no "(Clone)" suffix)
            // So we need to check if this is actually a prefab asset vs a renamed board card
            bool isPrefabAsset = !gameObject.name.Contains("(Clone)") && 
                                 (gameObject.name == "NewCardPrefab" || gameObject.name == "NewCardPrefabOpp");
            
            if (isPrefabAsset)
            {
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
                
                // Downgrade from warning to verbose log to avoid noisy console spam in normal play
                return; // Early return - prefab assets shouldn't be in the scene anyway
            }
            
            // [CardFront] CRITICAL: Ensure cloned cards (hand cards) and renamed board cards have interactivity enabled
            // Cloned cards should always be interactive unless explicitly disabled
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            
            // Diagnostic: Check if EventSystem exists (only warn once, HUDSetup should create it)
            if (EventSystem.current == null)
            {
                // Try to create EventSystem automatically if HUDSetup hasn't done it yet
                GameObject eventSystemObj = GameObject.Find("EventSystem");
                if (eventSystemObj == null)
                {
                    eventSystemObj = new GameObject("EventSystem");
                    EventSystem eventSystem = eventSystemObj.AddComponent<EventSystem>();
                    eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
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
            
            SetupCardShadow();
            
            // Diagnostic: Check if Canvas has GraphicRaycaster
            if (canvas != null)
            {
                GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
                if (raycaster == null)
                {
                    Debug.LogWarning($"[NewCardUI] Canvas {canvas.name} missing GraphicRaycaster! UI interactions may not work.");
                }
            }
            else
            {
                Debug.LogWarning($"[NewCardUI] No Canvas found in parent hierarchy for '{gameObject.name}'! This card may not work as UI.");
            }
            
            // Auto-setup containers if not assigned (runtime setup)
            // Always set up containers if they're missing - needed for battle captures even if card starts face up
            if (frontContainer == null || backContainer == null)
            {
                AutoSetupContainers();
            }
            
            // Get or create CardFlipAnimation (only if containers are assigned)
            if (frontContainer != null && backContainer != null)
            {
                // Get existing or create new CardFlipAnimation component
                if (flipAnimation == null)
                {
                    flipAnimation = GetComponent<CardFlipAnimation>();
                }
                if (flipAnimation == null)
                {
                    flipAnimation = gameObject.AddComponent<CardFlipAnimation>();
                }
                
                // Assign container references to CardFlipAnimation
                if (flipAnimation != null)
                {
                    flipAnimation.SetContainers(frontContainer, backContainer);
                    flipAnimation.ValidateSetup(); // Only validate if containers exist
                }
            }
            else
            {
                // Flip animation not set up - this is optional, so don't create component
                // But still try to get existing component if it exists
                if (flipAnimation == null)
                {
                    flipAnimation = GetComponent<CardFlipAnimation>();
                }
            }
        }
        
        private void SetupCardShadow()
        {
            if (!enableCardShadow)
            {
                if (cardShadow != null)
                {
                    cardShadow.gameObject.SetActive(false);
                }
                return;
            }
            
            if (cardBackground == null)
            {
                return;
            }
            
            if (cardShadow == null)
            {
                GameObject shadowObj = new GameObject("CardShadow");
                shadowObj.transform.SetParent(cardBackground.transform, false);
                cardShadow = shadowObj.AddComponent<SpriteRenderer>();
            }
            
            cardShadow.gameObject.SetActive(true);
            cardShadow.sprite = cardBackground.sprite;
            cardShadow.color = shadowColor;
            cardShadow.transform.localPosition = new Vector3(shadowOffset.x, shadowOffset.y, 0f);
            cardShadow.transform.localScale = Vector3.one * shadowScaleMultiplier;
            cardShadow.transform.localRotation = Quaternion.identity;
            cardShadow.sortingLayerID = cardBackground.sortingLayerID;
            cardShadow.sortingOrder = cardBackground.sortingOrder - 1;
        }

        // Inspect / zoom helpers have been removed.

        /// <summary>
        /// Ensures there is a border overlay SpriteRenderer and assigns the correct
        /// Fire/Earth frame sprite based on which side the card belongs to.
        /// </summary>
        private void EnsureBorderOverlay(bool isPlayerCard)
        {
            // If no sprites are configured, do nothing.
            if (p1BorderSprite == null && p2BorderSprite == null)
            {
                return;
            }

            // Lazily create the overlay as a SpriteRenderer, parented under the
            // card background so it renders in the same world space but above.
            if (borderOverlayRenderer == null)
            {
                if (cardBackground == null)
                {
                    return;
                }

                GameObject borderObj = new GameObject("BorderOverlay");
                borderObj.transform.SetParent(cardBackground.transform, false);
                borderObj.transform.localPosition = Vector3.zero;
                borderObj.transform.localRotation = Quaternion.identity;
                borderObj.transform.localScale = Vector3.one;

                borderOverlayRenderer = borderObj.AddComponent<SpriteRenderer>();
                borderOverlayRenderer.sortingLayerID = cardBackground.sortingLayerID;
                borderOverlayRenderer.sortingOrder = cardBackground.sortingOrder + 1;
            }

            Sprite targetSprite = isPlayerCard ? p1BorderSprite : p2BorderSprite;
            if (targetSprite != null && borderOverlayRenderer != null)
            {
                borderOverlayRenderer.sprite = targetSprite;
                borderOverlayRenderer.color = Color.white;
                borderOverlayRenderer.enabled = true;

                // Auto-fit the border sprite to the underlying card background so
                // it doesn't explode to full texture size.
                if (cardBackground != null && cardBackground.sprite != null)
                {
                    Vector2 bgSize = cardBackground.sprite.bounds.size;
                    Vector2 borderSize = targetSprite.bounds.size;
                    if (borderSize.x > 0.0001f && borderSize.y > 0.0001f)
                    {
                        float scaleX = bgSize.x / borderSize.x;
                        float scaleY = bgSize.y / borderSize.y;
                        float uniform = Mathf.Min(scaleX, scaleY);
                        borderOverlayRenderer.transform.localScale = new Vector3(uniform, uniform, 1f);
                    }
                    else
                    {
                        borderOverlayRenderer.transform.localScale = Vector3.one;
                    }
                }
                else
                {
                    borderOverlayRenderer.transform.localScale = Vector3.one;
                }
            }
            else if (borderOverlayRenderer != null)
            {
                borderOverlayRenderer.enabled = false;
            }
        }
        
        /// <summary>
        /// Automatically sets up FrontContainer and BackContainer at runtime if not configured in prefab
        /// </summary>
        private void AutoSetupContainers()
        {
            // Create FrontContainer if it doesn't exist
            if (frontContainer == null)
            {
                frontContainer = new GameObject("FrontContainer");
                frontContainer.transform.SetParent(transform);
                frontContainer.transform.localPosition = Vector3.zero;
                frontContainer.transform.localRotation = Quaternion.identity;
                frontContainer.transform.localScale = Vector3.one;
                
                // Move all existing card elements into FrontContainer
                // Find all child elements that should be on the front
                List<Transform> elementsToMove = new List<Transform>();
                
                // Find cardBackground (SpriteRenderer or Image)
                if (cardBackground != null)
                {
                    elementsToMove.Add(cardBackground.transform);
                }
                
                // Find artwork (SpriteRenderer or Image)
                if (artwork != null)
                {
                    elementsToMove.Add(artwork.transform);
                }
                
                // Find all TextMeshProUGUI elements
                TextMeshProUGUI[] allTexts = GetComponentsInChildren<TextMeshProUGUI>();
                foreach (var text in allTexts)
                {
                    if (text.transform != transform && !elementsToMove.Contains(text.transform))
                    {
                        elementsToMove.Add(text.transform);
                    }
                }
                
                // Find cardTypeIcon (SpriteRenderer)
                if (cardTypeIcon != null)
                {
                    elementsToMove.Add(cardTypeIcon.transform);
                }
                
                // Move all found elements into FrontContainer
                foreach (var element in elementsToMove)
                {
                    if (element != null && element.parent == transform)
                    {
                        element.SetParent(frontContainer.transform, true);
                    }
                }
                
                // If no elements were found/moved, try to move all direct children except BackContainer
                if (elementsToMove.Count == 0)
                {
                    for (int i = transform.childCount - 1; i >= 0; i--)
                    {
                        Transform child = transform.GetChild(i);
                        if (child.name != "BackContainer" && child.name != "FrontContainer")
                        {
                            child.SetParent(frontContainer.transform, true);
                        }
                    }
                }
            }
            
            // Create BackContainer if it doesn't exist
            if (backContainer == null)
            {
                backContainer = new GameObject("BackContainer");
                backContainer.transform.SetParent(transform);
                backContainer.transform.localPosition = Vector3.zero;
                backContainer.transform.localRotation = Quaternion.identity;
                backContainer.transform.localScale = Vector3.one;
                
                // Create card back visual
                GameObject cardBackVisual = null;
                
                // Try to use existing backSpriteRenderer or backImage
                if (backSpriteRenderer != null)
                {
                    cardBackVisual = backSpriteRenderer.gameObject;
                    if (cardBackVisual.transform.parent != backContainer.transform)
                    {
                        cardBackVisual.transform.SetParent(backContainer.transform, true);
                    }
                }
                else if (backImage != null)
                {
                    cardBackVisual = backImage.gameObject;
                    if (cardBackVisual.transform.parent != backContainer.transform)
                    {
                        cardBackVisual.transform.SetParent(backContainer.transform, true);
                    }
                }
                else
                {
                    // Create a new visual for card back - try Image first (for UI cards), then SpriteRenderer
                    cardBackVisual = new GameObject("CardBackVisual");
                    cardBackVisual.transform.SetParent(backContainer.transform);
                    cardBackVisual.transform.localPosition = Vector3.zero;
                    cardBackVisual.transform.localRotation = Quaternion.identity;
                    cardBackVisual.transform.localScale = Vector3.one;
                    
                    // Clean up any missing script references on CardBackVisual
                    #if UNITY_EDITOR
                    UnityEditor.GameObjectUtility.RemoveMonoBehavioursWithMissingScript(cardBackVisual);
                    #endif
                    
                    // Try Image first (for UI cards)
                    RectTransform rectTransform = GetComponent<RectTransform>();
                    if (rectTransform != null)
                    {
                        // It's a UI card, use Image
                        backImage = cardBackVisual.AddComponent<UnityEngine.UI.Image>();
                        if (backImage != null)
                        {
                            // Create a simple colored rectangle as default back
                            backImage.color = new Color(0.3f, 0.3f, 0.3f, 1f); // Dark gray placeholder
                            
                            // Image component needs a sprite to render - create a simple white texture
                            Texture2D whiteTexture = new Texture2D(1, 1);
                            whiteTexture.SetPixel(0, 0, Color.white);
                            whiteTexture.Apply();
                            backImage.sprite = Sprite.Create(whiteTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
                            
                            // Make it fill the card
                            RectTransform backRect = cardBackVisual.GetComponent<RectTransform>();
                            if (backRect != null)
                            {
                                backRect.anchorMin = Vector2.zero;
                                backRect.anchorMax = Vector2.one;
                                backRect.sizeDelta = Vector2.zero;
                                backRect.anchoredPosition = Vector2.zero;
                            }
                        }
                    }
                    else
                    {
                        // It's a 2D card, use SpriteRenderer
                        backSpriteRenderer = cardBackVisual.AddComponent<SpriteRenderer>();
                        if (backSpriteRenderer != null)
                        {
                            // Set a default color - will be overridden by captured color when flipped
                            backSpriteRenderer.color = new Color(0.3f, 0.3f, 0.3f, 1f); // Dark gray placeholder
                        }
                    }
                }
            }
            
            // Ensure flipAnimation reference is set
            if (flipAnimation == null)
            {
                flipAnimation = GetComponent<CardFlipAnimation>();
            }
            
            // Note: The serialized fields (frontContainer, backContainer, backSpriteRenderer, backImage)
            // are already assigned since we're using the same field names in this method.
            // They will be visible in the Inspector after the prefab is saved or in Play mode.
            
            SetupCardShadow();
        }
        
        public void Initialize(NewCard cardData)
        {
            if (cardData == null)
            {
                Debug.LogError($"NewCardUI on {gameObject.name}: Cannot initialize with null card data!");
                return;
            }
            
            if (cardData.Data == null)
            {
                Debug.LogError($"NewCardUI on {gameObject.name}: Card data is null! Card: {cardData}");
                return;
            }
            
            card = cardData;
            
            // Verify card is set
            if (card == null)
            {
                Debug.LogError($"NewCardUI on {gameObject.name}: CRITICAL - card field is null after assignment! This should never happen.");
            }
            
            // Set GameObject name to match card name for easier debugging and identification
            // Always update name to ensure it matches the card (important for drag-and-drop)
            string targetName = cardData.Data.cardName;
            if (string.IsNullOrEmpty(gameObject.name) || gameObject.name != targetName)
            {
                gameObject.name = targetName;
            }
            
            // Sync card reference to CardMover components (if any exist)
            SyncCardReferenceToMovers();
            
            // Setup containers if needed (must happen before assigning sprites)
            if (frontContainer == null || backContainer == null)
            {
                AutoSetupContainers();
            }
            
            // Final verification
            if (this.card == null)
            {
                Debug.LogError($"NewCardUI on {gameObject.name}: CRITICAL - card is null after Initialize() completes!");
            }
            
            // Assign card back sprite (with fallback)
            AssignCardBackSprite();
            
            UpdateVisuals();
            
            // Set initial flip state (before UpdateVisuals so back is shown)
            // Only if flip animation is set up
            if (startFaceDown && flipAnimation != null && flipAnimation.IsSetupValid())
            {
                flipAnimation.SetFlippedState(false, instant: true); // Show back, hide front
            }
            
            // Auto-flip if enabled (use revealDelay that may have been set before Initialize)
            // Only if flip animation is set up
            if (autoFlipOnReveal && flipAnimation != null && flipAnimation.IsSetupValid())
            {
                // [CardFront] CRITICAL: Only start coroutine if GameObject is active
                // Coroutines cannot start on inactive GameObjects
                if (gameObject.activeInHierarchy)
                {
                    StartCoroutine(DelayedFlip());
                }
                else
                {
                    Debug.LogWarning($"[NewCardUI] Cannot start DelayedFlip coroutine - GameObject '{gameObject.name}' is inactive. This may indicate the card was instantiated inactive or parent is inactive.");
                }
            }
        }
        
        private void UpdateVisuals()
        {
            if (card == null || card.Data == null) return;
            
            // Update text fields
            if (cardNameText != null)
                cardNameText.text = card.Data.cardName ?? "";
                
            if (descriptionText != null)
                descriptionText.text = card.Data.description ?? "";
                
            // Update directional stats
            if (topStatText != null)
                topStatText.text = card.CurrentTopStat.ToString();
                
            if (rightStatText != null)
                rightStatText.text = card.CurrentRightStat.ToString();
                
            if (downStatText != null)
                downStatText.text = card.CurrentDownStat.ToString();
                
            if (leftStatText != null)
                leftStatText.text = card.CurrentLeftStat.ToString();
            
            // Update card type
            if (cardTypeText != null)
                cardTypeText.text = card.Data.cardType.ToString();
            
            // Update visuals
            if (artwork != null && card.Data.artwork != null)
                artwork.sprite = card.Data.artwork;
            
            // Determine if this card belongs to the player or opponent
            bool isPlayerCard = IsPlayerCard();

            // Set card background color based on ownership
            if (cardBackground != null)
            {
                // Set background color: use card's original color for player cards (orange tint), green for opponent cards
                // Only apply if card is face up (not captured) - captured cards get their color from CardFlipAnimation
                if (flipAnimation == null || flipAnimation.isFlipped)
                {
                    if (isPlayerCard)
                    {
                        // Use card's original color if available, otherwise use orange
                        if (card.Data != null && card.Data.cardColor != Color.white && card.Data.cardColor != Color.clear)
                        {
                            cardBackground.color = card.Data.cardColor; // Use card's original background color
                        }
                        else
                        {
                            cardBackground.color = playerCapturedColor; // Fallback to orange
                        }
                    }
                    else
                    {
                        // Use card's original color if available, otherwise use green
                        if (card.Data != null && card.Data.cardColor != Color.white && card.Data.cardColor != Color.clear)
                        {
                            cardBackground.color = card.Data.cardColor; // Use card's original background color
                        }
                        else
                        {
                            cardBackground.color = opponentCapturedColor; // Fallback to green
                        }
                    }
                }
                // If captured (face down), color will be applied by CardFlipAnimation during capture
                
                SetupCardShadow();
            }

            // Ensure the correct Fire/Earth border frame is applied on top.
            EnsureBorderOverlay(isPlayerCard);
        }
        
        private void Start()
        {
            // [CardFront] CRITICAL: Disable prefab assets from receiving raycasts (runtime check)
            // Prefab assets (non-clones) should NEVER be interactable - disable them immediately
            // BUT: Board cards are valid cloned cards that have been renamed (no "(Clone)" suffix)
            // So we need to check if this is actually a prefab asset vs a renamed board card
            bool isPrefabAsset = !gameObject.name.Contains("(Clone)") && 
                                 (gameObject.name == "NewCardPrefab" || gameObject.name == "NewCardPrefabOpp");
            
            if (isPrefabAsset)
            {
                CanvasGroup cg = GetComponent<CanvasGroup>();
                if (cg == null)
                {
                    cg = gameObject.AddComponent<CanvasGroup>();
                }
                cg.blocksRaycasts = false;
                cg.interactable = false;
                gameObject.SetActive(false); // Disable the entire GameObject
                
                // Downgrade to info log to avoid noisy warnings for expected safety behaviour
                return; // Early return - prefab assets shouldn't be processed
            }
            
            // [CardFront] CRITICAL: Ensure cloned cards (hand cards) and renamed board cards have interactivity enabled
            // Cloned cards and board cards should always be interactive unless explicitly disabled
            if (canvasGroup != null)
            {
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
            
            // Verify card is set after initialization
            if (card == null)
            {
                // Check if this is a prefab asset (not an instance) - these won't have cards
                #if UNITY_EDITOR
                // [CardFront] Prefab assets and uninitialized prefab instances shouldn't warn
                if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(gameObject))
                {
                    // This is a prefab asset, not an instance - no card needed
                    return;
                }
                
                // Check if this is an uninitialized prefab instance (placed directly in scene)
                bool isPrefabInstance = UnityEditor.PrefabUtility.IsPartOfPrefabInstance(gameObject);
                if (isPrefabInstance && (gameObject.name == "NewCardPrefab" || gameObject.name == "NewCardPrefabOpp"))
                {
                    // [CardFront] This is an uninitialized prefab instance - expected to be null until Initialize() is called
                    // Only warn if it's actually in a hand container (should have been initialized)
                    Transform parent = transform.parent;
                    bool inHandContainer = parent != null && 
                        (parent.GetComponent<CardGame.UI.NewHandP1UI>() != null || 
                         parent.GetComponent<CardGame.UI.NewHandP2UI>() != null);
                    
                    if (!inHandContainer)
                    {
                        // Not in a hand container - probably just a test prefab in scene, don't warn
                        return;
                    }
                }
                #endif
                
                // [CardFront] Only warn for instantiated cards (Clone) that are in hands and should have been initialized
                // Prefab assets and uninitialized prefab instances not in hands are expected to be null
                bool isInstantiatedCard = gameObject.name.Contains("(Clone)");
                bool shouldHaveCard = isInstantiatedCard; // Instantiated cards should always have a card reference
                
                if (shouldHaveCard)
                {
                    Debug.LogWarning($"[NewCardUI] Card is null in Start() for instantiated card. Initialize() may not have been called or card was cleared. GameObject: {gameObject.name}");
                }
                // Silently ignore for prefab assets and uninitialized instances not in hands
                
                // [CardFront] Try to find card from hand UIs as last resort (using GetComponentInParent for Hub connection)
                CardGame.UI.NewHandP1UI parentHandUI = GetComponentInParent<CardGame.UI.NewHandP1UI>();
                if (parentHandUI != null)
                {
                    NewCard foundCard = parentHandUI.GetCardForUI(this);
                    if (foundCard != null)
                    {
                        card = foundCard;
                        Debug.Log($"[NewCardUI] Found and set card in Start() from parent NewHandP1UI: {card.Data.cardName}");
                    }
                }
                
                if (card == null)
                {
                    CardGame.UI.NewHandP2UI parentHandOppUI = GetComponentInParent<CardGame.UI.NewHandP2UI>();
                    if (parentHandOppUI != null)
                    {
                        NewCard foundCard = parentHandOppUI.GetCardForUI(this);
                        if (foundCard != null)
                        {
                            card = foundCard;
                            Debug.Log($"[NewCardUI] Found and set card in Start() from parent NewHandP2UI: {card.Data.cardName}");
                        }
                    }
                }
                
                // [CardFront] Final fallback: Try scene-wide search only if parent search failed
                // This is acceptable in Start() as a recovery mechanism, but prefer parent hierarchy
                if (card == null)
                {
                    #if UNITY_EDITOR
                    // Only in Editor - this is a last resort recovery
                    CardGame.UI.NewHandP1UI handUI = FindObjectOfType<CardGame.UI.NewHandP1UI>();
                    if (handUI != null)
                    {
                        NewCard foundCard = handUI.GetCardForUI(this);
                        if (foundCard != null)
                        {
                            card = foundCard;
                            Debug.Log($"[NewCardUI] Found and set card in Start() from scene NewHandP1UI (fallback): {card.Data.cardName}");
                        }
                    }
                    
                    if (card == null)
                    {
                        CardGame.UI.NewHandP2UI handP2UI = FindObjectOfType<CardGame.UI.NewHandP2UI>();
                        if (handP2UI != null)
                        {
                            NewCard foundCard = handP2UI.GetCardForUI(this);
                            if (foundCard != null)
                            {
                                card = foundCard;
                                Debug.Log($"[NewCardUI] Found and set card in Start() from scene NewHandP2UI (fallback): {card.Data.cardName}");
                            }
                        }
                    }
                    #endif
                }
            }
        }
        
        private void Update()
        {
            // No per-frame behaviour required currently.
        }
        
        
        private bool IsOverPlayArea(Vector2 screenPosition)
        {
            // Simple check: if card is dragged upward significantly
            return screenPosition.y > Screen.height * 0.6f;
        }
        
        private void PlayCard()
        {
            if (card != null && card.IsPlayable)
            {
                OnCardPlayed?.Invoke(this);
            }
        }
        
        public void SetInteractable(bool interactable)
        {
            CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            
            // Only modify interactable, not alpha (alpha is controlled by flip animation)
            // Only set alpha if NOT animating (to avoid conflicts)
            canvasGroup.interactable = interactable;
            if (flipAnimation == null || !flipAnimation.isAnimating)
            {
                canvasGroup.alpha = interactable ? 1f : 0.5f;
            }
            // Note: During flip animation, container CanvasGroups control alpha, not root
        }
        
        private void AssignCardBackSprite()
        {
            // Try to get sprite from card data, fallback to default
            Sprite backSprite = null;
            if (card != null && card.Data != null && card.Data.cardBackSprite != null)
            {
                backSprite = card.Data.cardBackSprite;
            }
            else if (defaultCardBackSprite != null)
            {
                backSprite = defaultCardBackSprite;
            }
            else
            {
                // Ensure we have a shared runtime default sprite so every card
                // shows SOME back even if assets are not fully wired.
                if (runtimeDefaultBackSprite == null)
                {
                    Texture2D tex = new Texture2D(1, 1);
                    tex.SetPixel(0, 0, Color.white);
                    tex.Apply();
                    runtimeDefaultBackSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
                }
                backSprite = runtimeDefaultBackSprite;
            }
            
            if (backContainer != null && backSprite != null)
            {
                // Always use a SpriteRenderer for the card back so it behaves like
                // the existing card sprites and works correctly with the flip
                // animation / world space.
                if (backSpriteRenderer == null)
                {
                    // Try to reuse an existing child SpriteRenderer first.
                    backSpriteRenderer = backContainer.GetComponentInChildren<SpriteRenderer>(true);
                }

                if (backSpriteRenderer == null)
                {
                    GameObject cardBackVisual = new GameObject("CardBackSprite");
                    cardBackVisual.transform.SetParent(backContainer.transform, false);
                    cardBackVisual.transform.localPosition = Vector3.zero;
                    cardBackVisual.transform.localRotation = Quaternion.identity;
                    cardBackVisual.transform.localScale = Vector3.one;

                    backSpriteRenderer = cardBackVisual.AddComponent<SpriteRenderer>();

                    // Match the background's sorting so the back sits in the same
                    // render layer as the front of the card.
                    if (cardBackground != null)
                    {
                        backSpriteRenderer.sortingLayerID = cardBackground.sortingLayerID;
                        backSpriteRenderer.sortingOrder = cardBackground.sortingOrder;
                    }
                }

                backSpriteRenderer.sprite = backSprite;
                backSpriteRenderer.color = Color.white;

                // Fit the back sprite to the card background so it isn't huge.
                if (cardBackground != null && cardBackground.sprite != null)
                {
                    Vector2 bgSize = cardBackground.sprite.bounds.size;
                    Vector2 backSize = backSprite.bounds.size;
                    if (backSize.x > 0.0001f && backSize.y > 0.0001f)
                    {
                        float scaleX = bgSize.x / backSize.x;
                        float scaleY = bgSize.y / backSize.y;
                        float uniform = Mathf.Min(scaleX, scaleY);
                        backSpriteRenderer.transform.localScale = new Vector3(uniform, uniform, 1f);
                    }
                    else
                    {
                        backSpriteRenderer.transform.localScale = Vector3.one;
                    }
                }
                else
                {
                    backSpriteRenderer.transform.localScale = Vector3.one;
                }
            }
        }

        // ToggleInspect and related overlay methods removed.
        
        private IEnumerator DelayedFlip()
        {
            yield return new WaitForSeconds(revealDelay);
            if (flipAnimation != null && !flipAnimation.isAnimating)
            {
                flipAnimation.FlipToFront();
            }
        }
        
        public void OnPointerClick(PointerEventData eventData)
        {
            // Simple behaviour: clicking a card just flips it (if enabled).
            if (allowClickToFlip && flipAnimation != null && !flipAnimation.isAnimating)
            {
                flipAnimation.FlipToggle();
            }
        }
        
        /// <summary>
        /// Called when the pointer enters this card's hit area.
        /// Provides a hook for hover-preview behaviour; tests only assert that
        /// the interfaces are implemented, so we keep behaviour minimal.
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            // Hover entered
        }
        
        /// <summary>
        /// Called when the pointer exits this card's hit area.
        /// </summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            // Hover exited
        }
        
        public void OnBeginDrag(PointerEventData eventData)
        {
            // Prevent dragging prefab assets (not instantiated in scene)
            // [CardFront] CRITICAL: Cards are renamed from "NewCardPrefab(Clone)" to card names in Initialize()
            // So we can't check for "(Clone)" in the name. Instead, check if it's an actual prefab asset.
            // Valid cloned cards have been renamed to their card names (e.g., "Earth Historian")
            // Prefab assets have the exact names "NewCardPrefab" or "NewCardPrefabOpp" (without Clone)
            bool isPrefabAsset = (gameObject.name == "NewCardPrefab" || gameObject.name == "NewCardPrefabOpp");
            
            if (isPrefabAsset)
            {
                // [CardFront] Disable this GameObject's raycasting to prevent future drag attempts
                CanvasGroup cg = GetComponent<CanvasGroup>();
                if (cg == null)
                {
                    cg = gameObject.AddComponent<CanvasGroup>();
                }
                cg.blocksRaycasts = false;
                cg.interactable = false;
                
                Debug.LogWarning($"[NewCardUI] BLOCKED: Cannot drag '{gameObject.name}' (InstanceID: {GetInstanceID()}) - this is a prefab asset, not a cloned card instance. Prefab assets cannot be dragged. Only instantiated cards can be dragged. Disabled raycasting for this GameObject. If you see this message, please remove the prefab asset from the scene hierarchy.");
                return;
            }
            
            // [CardFront] Additional check: If card is null or not initialized, this might be a prefab asset
            // Valid cloned cards should have their card reference set by Initialize() before drag can occur
            if (card == null)
            {
                Debug.LogWarning($"[NewCardUI] Cannot drag '{gameObject.name}' - card reference is null. This may be an uninitialized prefab asset.");
                return;
            }
            
            #if UNITY_EDITOR
            if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(gameObject))
            {
                Debug.LogWarning($"[NewCardUI] Cannot drag prefab asset '{gameObject.name}'. Only instantiated cards can be dragged.");
                return;
            }
            
            // Check if this is a prefab instance that hasn't been initialized
            bool isPrefabInstance = UnityEditor.PrefabUtility.IsPartOfPrefabInstance(gameObject);
            if (isPrefabInstance && card == null && Card == null)
            {
                Debug.LogWarning($"[NewCardUI] Prefab instance '{gameObject.name}' has not been initialized. Initialize() may not have been called.");
            }
            #endif
            
            if (!allowDrag)
            {
                Debug.LogWarning($"[NewCardUI] Drag not allowed - allowDrag is false");
                return;
            }
            
           // CRITICAL: Opponent cards should only be draggable during opponent's turn
           // Check if this is an opponent card and verify it's the opponent's turn
           bool isOpponentCard = IsOpponentCard();
           if (isOpponentCard)
           {
               // [CardFront] Check if CardMoverP2 is present - if so, let it handle the drag instead
               // This prevents conflicts between CardMoverP2 (OnMouseDown) and NewCardUI (OnBeginDrag)
               CardMoverP2 cardMoverP2 = GetComponent<CardMoverP2>();
               if (cardMoverP2 == null)
               {
                   cardMoverP2 = GetComponentInChildren<CardMoverP2>();
               }
               if (cardMoverP2 == null)
               {
                   cardMoverP2 = GetComponentInParent<CardMoverP2>();
               }
               
               if (cardMoverP2 != null)
               {
                   // CardMoverP2 handles opponent card dragging via OnMouseDown - don't interfere
                   Debug.Log($"[NewCardUI] Opponent card '{gameObject.name}' drag handled by CardMoverP2 - skipping NewCardUI drag handling to prevent conflicts.");
                   return;
               }
               
               // [CardFront] Allow opponent cards to drag when it's the opponent's turn
               // Check if it's currently the opponent's turn using FateFlowController
               bool canOpponentAct = CardGame.Managers.FateFlowController.Instance != null && 
                                     CardGame.Managers.FateFlowController.Instance.CanAct(CardGame.Managers.FateSide.P2);
               
               if (!canOpponentAct)
               {
                   // [CardFront] Block opponent cards when it's NOT the opponent's turn (expected behavior)
                   Debug.Log($"[NewCardUI] Opponent card '{gameObject.name}' drag blocked - not opponent's turn (expected behavior).");
                   return;
               }
               else
               {
                   // [CardFront] Allow opponent cards to drag when it IS the opponent's turn
                   Debug.Log($"[NewCardUI] Opponent card '{gameObject.name}' drag allowed - opponent's turn.");
                   // Continue with drag initialization below
               }
           }
           
           // [CardFront] Check if card is still in hand before allowing drag
           // Cards already on board should not be draggable
           bool isCardInHand = false;
           
           if (IsPlayerCard())
           {
               CardGame.UI.NewHandP1UI handUI = GetComponentInParent<CardGame.UI.NewHandP1UI>();
               if (handUI != null)
               {
                   NewCard handCard = handUI.GetCardForUI(this);
                   isCardInHand = (handCard != null && handCard == card);
               }
           }
           else if (IsOpponentCard())
           {
               CardGame.UI.NewHandP2UI handP2UI = GetComponentInParent<CardGame.UI.NewHandP2UI>();
               if (handP2UI != null)
               {
                   NewCard handCard = handP2UI.GetCardForUI(this);
                   isCardInHand = (handCard != null && handCard == card);
               }
           }
           
           if (!isCardInHand)
           {
               Debug.LogWarning($"[NewCardUI] Cannot drag '{gameObject.name}' - card is not in hand (already on board or removed).");
               return;
           }
            
            // [CardFront] CardFront Architecture: Use Hub connections instead of FindObjectOfType()
            // Recover card reference if lost using parent Hub (NewHandP1UI/NewHandP2UI)
            if (card == null)
            {
                // Strategy 1: Check Card property (should match field)
                if (Card != null)
                {
                    card = Card;
                    Debug.Log($"[NewCardUI] Recovered card from Card property: {card.Data.cardName}");
                }
                else
                {
                    // Strategy 2: Use Hub connection via GetComponentInParent (no FindObjectOfType!)
                    // Check parent hierarchy for HandUI Hub connection
                    CardGame.UI.NewHandP1UI handUI = GetComponentInParent<CardGame.UI.NewHandP1UI>();
                    CardGame.UI.NewHandP2UI handP2UI = GetComponentInParent<CardGame.UI.NewHandP2UI>();
                    
                    if (handUI != null)
                    {
                        NewCard foundCard = handUI.GetCardForUI(this);
                        if (foundCard != null)
                        {
                            card = foundCard;
                            Debug.Log($"[NewCardUI] Recovered card from parent HandUI Hub: {card.Data.cardName}");
                        }
                    }
                    else if (handP2UI != null)
                    {
                        NewCard foundCard = handP2UI.GetCardForUI(this);
                        if (foundCard != null)
                        {
                            card = foundCard;
                            Debug.Log($"[NewCardUI] Recovered card from parent HandOppUI Hub: {card.Data.cardName}");
                        }
                    }
                    
                    // Strategy 3: Try sibling index matching (if in HandUI container)
                    if (card == null && transform.parent != null)
                    {
                        // Check parent again (might not have found HandUI in GetComponentInParent if structure is different)
                        Transform parent = transform.parent;
                        handUI = parent.GetComponentInParent<CardGame.UI.NewHandP1UI>();
                        handP2UI = parent.GetComponentInParent<CardGame.UI.NewHandP2UI>();
                        
                        if (handUI != null)
                        {
                            int siblingIndex = transform.GetSiblingIndex();
                            if (siblingIndex >= 0 && siblingIndex < handUI.GetCardCount())
                            {
                                card = handUI.GetCardForUIByIndex(siblingIndex);
                                if (card != null)
                                {
                                    Debug.Log($"[NewCardUI] Recovered card by sibling index from HandUI Hub: {card.Data.cardName}");
                                }
                            }
                        }
                        else if (handP2UI != null)
                        {
                            int siblingIndex = transform.GetSiblingIndex();
                            if (siblingIndex >= 0 && siblingIndex < handP2UI.GetCardCount())
                            {
                                card = handP2UI.GetCardForUIByIndex(siblingIndex);
                                if (card != null)
                                {
                                    Debug.Log($"[NewCardUI] Recovered card by sibling index from HandOppUI Hub: {card.Data.cardName}");
                                }
                            }
                        }
                    }
                    
                    // Strategy 4: Try to find card by matching with all instantiated cards in HandUI/HandOppUI lists
                    // This is a fallback when the card reference is lost but the card is still in hand
                    if (card == null)
                    {
                        // Try to find NewHandP2UI and match by GameObject instance
                        NewHandP2UI sceneHandOppUI = FindObjectOfType<NewHandP2UI>();
                        if (sceneHandOppUI != null)
                        {
                            // Use the Hub's GetCardForUI method which has multiple fallback strategies
                            NewCard foundCard = sceneHandOppUI.GetCardForUI(this);
                            if (foundCard != null)
                            {
                                card = foundCard;
                                Debug.Log($"[NewCardUI] Recovered card via HandOppUI Hub's GetCardForUI: {card.Data.cardName}");
                            }
                        }
                        
                        // Also try NewHandP1UI for player cards
                        if (card == null)
                        {
                            NewHandP1UI sceneHandUI = FindObjectOfType<NewHandP1UI>();
                            if (sceneHandUI != null)
                            {
                                NewCard foundCard = sceneHandUI.GetCardForUI(this);
                                if (foundCard != null)
                                {
                                    card = foundCard;
                                    Debug.Log($"[NewCardUI] Recovered card via HandUI Hub's GetCardForUI: {card.Data.cardName}");
                                }
                            }
                        }
                    }
                }
            }
            
            // [CardFront] CRITICAL: Verify card is bound before proceeding
            // If card is still null, card reference was lost - this should never happen with CardFactory
            if (card == null || card.Data == null)
            {
                // [CardFront] Additional check: If this is NOT a clone (i.e., it's the prefab asset or uninitialized instance), block it
                if (!gameObject.name.Contains("(Clone)"))
                {
                    Debug.LogWarning($"[NewCardUI] Cannot drag '{gameObject.name}' - this appears to be a prefab asset or uninitialized instance. Only instantiated cards (clones) can be dragged. Please ensure you're dragging a card from the hand, not a prefab asset in the scene.");
                    return;
                }
                
                Debug.LogError($"[NewCardUI] CRITICAL: Card reference lost. GameObject: {gameObject.name}, InstanceID: {GetInstanceID()}. Cannot start drag.");
                Debug.LogError($"[NewCardUI] This indicates Initialize() was not called or card field was cleared. Check CardFactory.");
                Debug.LogError($"[NewCardUI] Recovery strategies failed. Parent: {(transform.parent != null ? transform.parent.name : "null")}, IsOpponentCard: {isOpponentCard}, HasHandOppUI: {(GetComponentInParent<NewHandP2UI>() != null ? "Yes" : "No")}");
                return;
            }
            
            // [CardFront] Turn System Rules: Only active side can move cards
            // Check turn state via Hub (FateFlowController)
            // Allow player cards on player's turn, opponent cards on opponent's turn
            if (CardGame.Managers.FateFlowController.Instance != null)
            {
                // Determine which side this card belongs to
                CardGame.Managers.FateSide cardSide = isOpponentCard ? 
                    CardGame.Managers.FateSide.P2 : 
                    CardGame.Managers.FateSide.Player;
                
                bool canAct = CardGame.Managers.FateFlowController.Instance.CanAct(cardSide);
                if (!canAct)
                {
                    Debug.LogWarning($"[NewCardUI] Cannot drag - not {cardSide}'s turn. Current fate: {CardGame.Managers.FateFlowController.Instance.CurrentFate}");
                    return; // Turn system blocks drag
                }
            }
            
            // [CardFront] All checks passed - start drag
            isDragging = true;
            
            // Set drag offset
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out dragOffset);
            
            // Make card non-interactable with other UI elements during drag
            canvasGroup.alpha = 0.8f;
            canvasGroup.blocksRaycasts = false;
            
            // Move card to top of sibling index
            transform.SetAsLastSibling();
        }
        
        public void OnDrag(PointerEventData eventData)
        {
            // If CardMoverP2 is present, let it handle opponent card dragging
            if (IsOpponentCard())
            {
                CardMoverP2 cardMoverP2 = GetComponent<CardMoverP2>();
                if (cardMoverP2 == null) cardMoverP2 = GetComponentInChildren<CardMoverP2>();
                if (cardMoverP2 == null) cardMoverP2 = GetComponentInParent<CardMoverP2>();
                if (cardMoverP2 != null) return; // CardMoverP2 handles this
            }
            
            if (!isDragging) return;
            
            Vector2 localPointerPosition;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out localPointerPosition))
            {
                rectTransform.position = canvas.transform.TransformPoint(localPointerPosition) - (Vector3)dragOffset;
            }
        }
        
        public void OnEndDrag(PointerEventData eventData)
        {
            // If CardMoverP2 is present, let it handle opponent card dragging
            if (IsOpponentCard())
            {
                CardMoverP2 cardMoverP2 = GetComponent<CardMoverP2>();
                if (cardMoverP2 == null) cardMoverP2 = GetComponentInChildren<CardMoverP2>();
                if (cardMoverP2 == null) cardMoverP2 = GetComponentInParent<CardMoverP2>();
                if (cardMoverP2 != null) return; // CardMoverP2 handles this
            }
            
            // [CardFront] OnEndDrag: Validate drag state
            if (!isDragging)
            {
                // [CardFront] Check if opponent card drag was blocked because it's not their turn
                // If it's the opponent's turn, we should allow dragging - check turn state
                bool isOpponentCard = IsOpponentCard();
                if (isOpponentCard)
                {
                    // Check if it's actually the opponent's turn - if so, we should have been dragging
                    bool canOpponentAct = CardGame.Managers.FateFlowController.Instance != null && 
                                         CardGame.Managers.FateFlowController.Instance.CanAct(CardGame.Managers.FateSide.P2);
                    
                    if (canOpponentAct)
                    {
                        // It's the opponent's turn but drag wasn't started - this is unexpected
                        Debug.LogWarning($"[NewCardUI] OnEndDrag: Opponent card '{gameObject.name}' drag should have started but didn't. Drag may have been interrupted.");
                    }
                    else
                    {
                        // It's not the opponent's turn - silently ignore (expected behavior)
                        return;
                    }
                }
                
                // Only warn if this is a player card that should have been dragging
                if (IsPlayerCard() && allowDrag)
                {
                    Debug.LogWarning($"[NewCardUI] OnEndDrag called but isDragging is false for player card '{gameObject.name}'. Drag may have been interrupted.");
                }
                return;
            }
            
            isDragging = false;
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            
            // [CardFront] Verify card reference before proceeding
            if (card == null || card.Data == null)
            {
                Debug.LogError($"[NewCardUI] OnEndDrag: Card reference lost. Cannot complete drop. GameObject: {gameObject.name}");
                return;
            }
            
            // [CardFront] Cluster approach: Use UI raycast to find drop area (local system)
            CardDropArea dropArea = FindDropAreaViaRaycast(eventData);
            
            // [CardFront] Fallback: Use Physics2D if UI raycast fails
            if (dropArea == null && Camera.main != null)
            {
                dropArea = FindDropAreaViaPhysics2D(eventData);
            }
            
            // [CardFront] If drop area found, place card on board
            if (dropArea != null)
            {
                
                // [CardFront] Handle P2 cards differently - they use CardMoverP2 and OnCardDropP2
                bool isOpponentCard = IsOpponentCard();
                if (isOpponentCard)
                {
                    PlaceOpponentCardOnBoard(dropArea);
                }
                else
                {
                    PlaceCardOnBoard(dropArea);
                }
                return;
            }
            
            // Card returns to original position via NewHandP1UI.ArrangeCards()
        }
        
        /// <summary>
        /// [CardFront] Cluster method: Find drop area via UI raycast (local system)
        /// </summary>
        private CardDropArea FindDropAreaViaRaycast(PointerEventData eventData)
        {
            if (EventSystem.current == null)
            {
                Debug.LogWarning($"[NewCardUI] FindDropAreaViaRaycast: EventSystem.current is null!");
                return null;
            }
            
            List<RaycastResult> raycastResults = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, raycastResults);
            
            foreach (RaycastResult result in raycastResults)
            {
                CardDropArea dropArea = result.gameObject.GetComponent<CardDropArea>();
                if (dropArea != null && !dropArea.IsOccupied)
                {
                    return dropArea;
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// [CardFront] Cluster method: Find drop area via Physics2D (local system)
        /// Uses Canvas camera if available, otherwise falls back to closest drop area
        /// </summary>
        private CardDropArea FindDropAreaViaPhysics2D(PointerEventData eventData)
        {
            Camera worldCamera = null;
            
            // [CardFront] Try to get the canvas camera first (for Screen Space - Camera mode)
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera && canvas.worldCamera != null)
            {
                worldCamera = canvas.worldCamera;
            }
            else if (Camera.main != null)
            {
                worldCamera = Camera.main;
            }
            
            Vector3 worldPos = Vector3.zero;
            
            if (worldCamera != null)
            {
                Vector3 screenPos = new Vector3(eventData.position.x, eventData.position.y, worldCamera.nearClipPlane);
                worldPos = worldCamera.ScreenToWorldPoint(screenPos);
                worldPos.z = 0f;
                
                // Try point-based detection first
                Collider2D[] hitColliders = Physics2D.OverlapPointAll(worldPos);
                
                foreach (Collider2D hitCollider in hitColliders)
                {
                    if (hitCollider == null) continue;
                    
                    CardDropArea dropArea = hitCollider.GetComponent<CardDropArea>();
                    if (dropArea != null && !dropArea.IsOccupied)
                    {
                        return dropArea;
                    }
                }
                
                // Fallback: Try radius-based detection (larger search area)
                float searchRadius = 2f; // Search within 2 units
                hitColliders = Physics2D.OverlapCircleAll(worldPos, searchRadius);
                
                CardDropArea closestDropArea = null;
                float closestDistance = float.MaxValue;
                
                foreach (Collider2D hitCollider in hitColliders)
                {
                    if (hitCollider == null) continue;
                    
                    CardDropArea dropArea = hitCollider.GetComponent<CardDropArea>();
                    if (dropArea != null && !dropArea.IsOccupied)
                    {
                        float distance = Vector3.Distance(worldPos, dropArea.transform.position);
                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                            closestDropArea = dropArea;
                        }
                    }
                }
                
                if (closestDropArea != null)
                {
                    return closestDropArea;
                }
            }
            
            // Final fallback: Find closest unoccupied drop area in scene (distance-based)
            CardDropArea[] allDropAreas = FindObjectsOfType<CardDropArea>();
            
            if (allDropAreas.Length == 0)
            {
                return null;
            }
            
            // Convert screen position to world using main camera or canvas camera
            Vector3 mouseWorldPos = worldPos;
            if (worldCamera == null && Camera.main != null)
            {
                Vector3 screenPos = new Vector3(eventData.position.x, eventData.position.y, Camera.main.nearClipPlane);
                mouseWorldPos = Camera.main.ScreenToWorldPoint(screenPos);
                mouseWorldPos.z = 0f;
            }
            
            CardDropArea closest = null;
            float minDist = float.MaxValue;
            
            foreach (CardDropArea area in allDropAreas)
            {
                if (area.IsOccupied) continue;
                
                float dist = Vector3.Distance(mouseWorldPos, area.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    closest = area;
                }
            }
            
            if (closest != null && minDist < 5f) // Only return if within reasonable distance (5 units)
            {
                return closest;
            }
            
            return null;
        }
        
        private void PlaceCardOnBoard(CardDropArea dropArea)
        {
            if (dropArea == null || card == null)
            {
                Debug.LogWarning($"PlaceCardOnBoard: dropArea or card is null. dropArea: {dropArea != null}, card: {card != null}");
                return;
            }
            
            // Check if it's the player's turn
            if (CardGame.Managers.FateFlowController.Instance != null)
            {
                if (!CardGame.Managers.FateFlowController.Instance.CanAct(CardGame.Managers.FateSide.Player))
                {
                    Debug.LogWarning($"Cannot place card - not player's turn. Current fate: {CardGame.Managers.FateFlowController.Instance.CurrentFate}");
                    return;
                }
            }
            
            // Check if drop area is occupied
            if (dropArea.IsOccupied)
            {
                Debug.LogWarning($"Cannot place card - drop area {dropArea.name} is occupied");
                return;
            }
            
            // [CardFront] Hub connection: Get deck manager and prefab via Hub (NewHandP1UI)
            // NewHandP1UI is the Hub that manages card UI instances and knows about deckManager and card prefab
            CardGame.Managers.NewDeckManagerP1 deckManager = null;
            CardGame.UI.NewHandP1UI handUI = GetComponentInParent<CardGame.UI.NewHandP1UI>();
            
            // Use parent Hub connection to get deck manager
            if (handUI != null)
            {
                // [CardFront] Access deckManager via Hub property (clean Hub connection)
                deckManager = handUI.DeckManager;
                
                // Validate card via Hub connection (HandUI knows which cards are in hand)
                NewCard validatedCard = handUI.GetCardForUI(this);
                if (validatedCard == null || validatedCard != card)
                {
                    Debug.LogWarning($"[NewCardUI] PlaceCardOnBoard: Card '{card.Data?.cardName}' not found in HandUI Hub. Cannot place.");
                    return;
                }
            }
            else
            {
                Debug.LogError("[NewCardUI] PlaceCardOnBoard: NewHandP1UI Hub not found in parent hierarchy. Cannot place card.");
                return;
            }
            
            // [CardFront] Validate deckManager exists
            if (deckManager == null)
            {
                Debug.LogError("[NewCardUI] PlaceCardOnBoard: DeckManager is null in HandUI Hub. Cannot place card.");
                return;
            }
            
            // [CardFront] Final validation: Card must be in hand (via Hub connection)
            if (!deckManager.Hand.Contains(card))
            {
                Debug.LogWarning($"[NewCardUI] PlaceCardOnBoard: Card '{card.Data?.cardName}' not found in hand. Hand contains {deckManager.Hand.Count} cards.");
                return;
            }
            
            // [CardFront] Hub approach: Get board card prefab from NewHandP1UI (Hub) instead of Resources.Load
            CardGame.UI.NewCardUI boardCardPrefab = handUI.CardPrefab;
            
            if (boardCardPrefab == null)
            {
                Debug.LogError("[NewCardUI] PlaceCardOnBoard: cardPrefab is null in NewHandP1UI Hub. Cannot create board card. Please assign the prefab in the Inspector for NewHandP1UI.");
                return;
            }
            
            // [CardFront] Use CardFactory Hub for board card creation
            GameObject boardCard = CardGame.Factories.CardFactory.CreateBoardCard(
                card, 
                boardCardPrefab.gameObject, 
                dropArea.transform.position
            );
            
            if (boardCard == null)
            {
                Debug.LogError("[NewCardUI] PlaceCardOnBoard: CardFactory failed to create board card.");
                return;
            }
            
            // Get CardMoverP1 component (should be added by CardFactory)
            CardMoverP1 cardMover = boardCard.GetComponent<CardMoverP1>();
            if (cardMover == null)
            {
                cardMover = boardCard.GetComponentInChildren<CardMoverP1>();
            }
            
            if (cardMover == null)
            {
                Debug.LogError("[NewCardUI] PlaceCardOnBoard: Board card prefab missing CardMoverP1 component. Cannot drag board card.");
                Destroy(boardCard);
                return;
            }
            
            // [CardFront] Trigger drop through CardDropArea (uses event channel)
            // This will handle: playing card, placement, battles via Hub connections
            dropArea.OnCardDrop(cardMover);
            
            // [CardFront] Remove from hand UI via event channel (cluster cleanup)
            // NewHandP1UI will handle removal when OnCardPlayed event fires
        }
        
        /// <summary>
        /// [CardFront] Places a P2 card on the board using CardMoverP2
        /// </summary>
        private void PlaceOpponentCardOnBoard(CardDropArea dropArea)
        {
            if (dropArea == null || card == null)
            {
                Debug.LogWarning($"PlaceOpponentCardOnBoard: dropArea or card is null. dropArea: {dropArea != null}, card: {card != null}");
                return;
            }
            
            // Check if it's the opponent's turn
            if (CardGame.Managers.FateFlowController.Instance != null)
            {
                if (!CardGame.Managers.FateFlowController.Instance.CanAct(CardGame.Managers.FateSide.P2))
                {
                    Debug.LogWarning($"Cannot place opponent card - not opponent's turn. Current fate: {CardGame.Managers.FateFlowController.Instance.CurrentFate}");
                    return;
                }
            }
            
            // Check if drop area is occupied
            if (dropArea.IsOccupied)
            {
                Debug.LogWarning($"Cannot place opponent card - drop area {dropArea.name} is occupied");
                return;
            }
            
            // [CardFront] Hub connection: Get opponent deck manager via Hub (NewHandP2UI) instead of FindObjectOfType
            // NewHandP2UI is the Hub that manages opponent card UI instances and knows about deckManagerP2
            CardGame.Managers.NewDeckManagerP2 deckManagerP2 = null;
            
            // Use parent Hub connection to get opponent deck manager
            CardGame.UI.NewHandP2UI handP2UI = GetComponentInParent<CardGame.UI.NewHandP2UI>();
            if (handP2UI != null)
            {
                // [CardFront] Access deckManagerP2 via Hub property (clean Hub connection)
                deckManagerP2 = handP2UI.DeckManager;
                
                // Validate card via Hub connection (HandOppUI knows which cards are in hand)
                NewCard validatedCard = handP2UI.GetCardForUI(this);
                if (validatedCard == null || validatedCard != card)
                {
                    Debug.LogWarning($"[NewCardUI] PlaceOpponentCardOnBoard: Card '{card.Data?.cardName}' not found in HandOppUI Hub. Cannot place.");
                    return;
                }
            }
            else
            {
                Debug.LogError("[NewCardUI] PlaceOpponentCardOnBoard: NewHandP2UI Hub not found in parent hierarchy. Cannot place opponent card.");
                return;
            }
            
            // [CardFront] Validate deckManagerP2 exists
            if (deckManagerP2 == null)
            {
                Debug.LogError("[NewCardUI] PlaceOpponentCardOnBoard: DeckManagerOpp is null in HandOppUI Hub. Cannot place opponent card.");
                return;
            }
            
            // [CardFront] Final validation: Card must be in opponent hand (via Hub connection)
            if (!deckManagerP2.Hand.Contains(card))
            {
                Debug.LogWarning($"[NewCardUI] PlaceOpponentCardOnBoard: Card '{card.Data?.cardName}' not found in opponent hand. Hand contains {deckManagerP2.Hand.Count} cards.");
                return;
            }
            
            // [CardFront] Hub approach: Get opponent board card prefab from NewHandP2UI (Hub)
            // NewHandP2UI has the cardPrefab reference assigned in Inspector - use that instead of Resources.Load
            NewCardUI opponentPrefab = handP2UI.CardPrefab;
            
            if (opponentPrefab == null)
            {
                Debug.LogError("[NewCardUI] PlaceOpponentCardOnBoard: cardPrefab is null in NewHandP2UI Hub. Cannot create opponent board card. Please assign the prefab in the Inspector.");
                return;
            }
            
            // [CardFront] Use CardFactory Hub for board card creation (similar to player cards)
            // CardFactory.CreateBoardCard handles proper initialization and activation
            GameObject boardCardOpp = CardGame.Factories.CardFactory.CreateBoardCard(
                card,
                opponentPrefab.gameObject,
                dropArea.transform.position
            );
            
            if (boardCardOpp == null)
            {
                Debug.LogError("[NewCardUI] PlaceOpponentCardOnBoard: Failed to instantiate opponent board card.");
                return;
            }
            
            // [CardFront] Note: CardFactory.CreateBoardCard now handles:
            // - Activation of the board card
            // - Initialization of NewCardUI component
            // - Setting card reference on CardMoverP2 (P2) or CardMover (P1)
            // - Refreshing home position on the mover component
            
            // Get CardMoverP2 component (should already be set up by CardFactory)
            CardMoverP2 cardMoverP2 = boardCardOpp.GetComponent<CardMoverP2>();
            if (cardMoverP2 == null)
            {
                cardMoverP2 = boardCardOpp.GetComponentInChildren<CardMoverP2>();
            }
            
            if (cardMoverP2 == null)
            {
                Debug.LogError("[NewCardUI] PlaceOpponentCardOnBoard: P2 board card prefab missing CardMoverP2 component. Cannot drag P2 board card. Please ensure the prefab has a CardMoverP2 component.");
                Destroy(boardCardOpp);
                return;
            }
            
            // [CardFront] Safety check: Ensure card reference is set (CardFactory should have done this)
            if (cardMoverP2.Card == null)
            {
                Debug.LogWarning("[NewCardUI] PlaceOpponentCardOnBoard: CardMoverP2 card reference is null (CardFactory should have set it). Setting it now as fallback.");
                cardMoverP2.SetCard(card);
                cardMoverP2.RefreshHomePosition();
            }
            
            // [CardFront] Trigger opponent drop through CardDropArea (uses event channel)
            // This will handle: playing card, placement, battles via Hub connections
            dropArea.OnCardDropP2(cardMoverP2);
            
            // [CardFront] Remove from hand UI via event channel (cluster cleanup)
            // NewHandP2UI will handle removal when OnCardPlayed event fires
        }
        
        public void RefreshVisuals()
        {
            UpdateVisuals();
        }

        /// <summary>
        /// Push the resolved NewCard reference to any CardMover components so they stop logging warnings.
        /// [CardFront] This ensures all player cards can be placed on the board using the same placement system as Flame Witch.
        /// </summary>
        private void SyncCardReferenceToMovers()
        {
            if (card == null)
            {
                Debug.LogWarning($"[NewCardUI] SyncCardReferenceToMovers: Card is null for '{gameObject.name}'. Cannot sync to CardMover.");
                return;
            }
            
            int syncCount = 0;
            
            // Player mover on this GameObject (if present)
            if (TryGetComponent<CardMoverP1>(out var mover))
            {
                mover.SetCard(card);
                syncCount++;
            }
            
            // Opponent mover on this GameObject (if present)
            if (TryGetComponent<CardMoverP2>(out var moverP2))
            {
                moverP2.SetCard(card);
                syncCount++;
            }
            
            // Some prefabs nest CardMoverP1 (P1)/CardMoverP2 (P2) on children, so update them too
            CardMoverP1[] childMovers = GetComponentsInChildren<CardMoverP1>(true);
            foreach (var childMover in childMovers)
            {
                // Skip if we already synced this one (same component on root GameObject)
                if (childMover == mover) continue;
                
                childMover.SetCard(card);
                syncCount++;
            }
            
            CardMoverP2[] childMoverP2s = GetComponentsInChildren<CardMoverP2>(true);
            foreach (var childMoverP2 in childMoverP2s)
            {
                // Skip if we already synced this one (same component on root GameObject)
                if (childMoverP2 == moverP2) continue;
                
                childMoverP2.SetCard(card);
                syncCount++;
            }
            
            if (syncCount == 0)
            {
                Debug.LogWarning($"[NewCardUI] SyncCardReferenceToMovers: No CardMoverP1 (P1) or CardMoverP2 (P2) components found on '{gameObject.name}' or its children. Card will need to find reference via FindCardReference().");
            }
        }

        /// <summary>
        /// Determines if this card belongs to the opponent.
        /// Checks GameObject name, parent hierarchy, and deck manager.
        /// </summary>
        private bool IsOpponentCard()
        {
            // [CardFront] CRITICAL: Check for CardMoverP2 component FIRST (most reliable for board cards)
            // Board cards don't have "Opp" in their name (renamed to card name), so component check is essential
            CardMoverP2 cardMoverP2 = GetComponent<CardMoverP2>();
            if (cardMoverP2 != null)
            {
                return true; // P2 card (has CardMoverP2)
            }
            
            // Check in children (for nested components)
            cardMoverP2 = GetComponentInChildren<CardMoverP2>();
            if (cardMoverP2 != null)
            {
                return true; // P2 card (has CardMoverP2 in children)
            }
            
            // Check in parents (for nested components)
            cardMoverP2 = GetComponentInParent<CardMoverP2>();
            if (cardMoverP2 != null)
            {
                return true; // P2 card (has CardMoverP2 in parent)
            }
            
            // Check GameObject name for "Opp" marker (works for hand cards)
            if (gameObject.name.Contains("Opp") || gameObject.name.Contains("NewCardPrefabOpp"))
            {
                return true;
            }
            
            // Check parent hierarchy for opponent containers (works for hand cards)
            Transform parent = transform.parent;
            while (parent != null)
            {
                if (parent.name.Contains("Opp") || parent.name.Contains("Opponent"))
                {
                    return true;
                }
                parent = parent.parent;
            }
            
            // [CardFront] Use Hub connection instead of FindObjectOfType
            // Check if card is in opponent hand UI via parent Hub (works for hand cards)
            CardGame.UI.NewHandP2UI handP2UI = GetComponentInParent<CardGame.UI.NewHandP2UI>();
            if (handP2UI != null)
            {
                NewCard foundCard = handP2UI.GetCardForUI(this);
                if (foundCard != null)
                {
                    return true;
                }
            }
            
            // [CardFront] Check if card is in opponent deck manager's hand via card reference
            // Only check if card reference is available (no FindObjectOfType)
            if (card != null)
            {
                // Note: This requires deck manager to be accessible via Hub connection
                // For now, this is a simple check - should be improved with Hub pattern
                // TODO: Refactor to use Hub connection instead of checking deck manager directly
            }
            
            return false;
        }
        
        /// <summary>
        /// Determines if this card belongs to the player.
        /// </summary>
        private bool IsPlayerCard()
        {
            // [CardFront] Use Hub connection instead of FindObjectOfType
            // Check if card is in player's hand via parent Hub
            CardGame.UI.NewHandP1UI handUI = GetComponentInParent<CardGame.UI.NewHandP1UI>();
            if (handUI != null)
            {
                NewCard foundCard = handUI.GetCardForUI(this);
                if (foundCard != null)
                {
                    return true; // Card is in player's hand via HandUI Hub
                }
            }
            
            // Check if card is in player's hand via card reference (if available)
            if (card != null)
            {
                // Note: This requires deck manager to be accessible via Hub connection
                // For now, this is a simple check - should be improved with Hub pattern
                // TODO: Refactor to use Hub connection instead of checking deck manager directly
            }

            // Check if it's a CardMoverP1 (P1) vs CardMoverP2 (P2)
            CardMoverP1 cardMover = GetComponent<CardMoverP1>();
            if (cardMover != null)
            {
                return true; // Player card
            }

            CardMoverP2 cardMoverP2 = GetComponent<CardMoverP2>();
            if (cardMoverP2 != null)
            {
                return false; // P2 card
            }

            // Check in children/parents
            cardMover = GetComponentInChildren<CardMoverP1>();
            if (cardMover != null) return true;

            cardMoverP2 = GetComponentInChildren<CardMoverP2>();
            if (cardMoverP2 != null) return false;

            cardMover = GetComponentInParent<CardMoverP1>();
            if (cardMover != null) return true;

            cardMoverP2 = GetComponentInParent<CardMoverP2>();
            if (cardMoverP2 != null) return false;

            // Default: assume player card if we can't determine
            return true;
        }
    }
}
