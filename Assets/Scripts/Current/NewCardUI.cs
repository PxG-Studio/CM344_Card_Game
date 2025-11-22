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
    public class NewCardUI : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
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
        
        [Header("Flip Settings")]
        [SerializeField] public bool startFaceDown = true;
        [SerializeField] public bool autoFlipOnReveal = true;
        [SerializeField] public float revealDelay = 0.2f;
        [SerializeField] private bool allowClickToFlip = false;
        
        [Header("Drag Settings")]
        [SerializeField] private bool allowDrag = true;
        
        [Header("Captured Colors")]
        [SerializeField] private Color playerCapturedColor = new Color(1f, 0.5f, 0f, 1f); // Orange for player's cards (matches card border orange)
        [SerializeField] private Color opponentCapturedColor = new Color(0f, 0.8f, 0f, 1f); // Green for opponent's captured cards
        
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
            
            // Diagnostic: Check if EventSystem exists (only warn once, HUDSetup should create it)
            if (EventSystem.current == null)
            {
                // Try to create EventSystem automatically if HUDSetup hasn't done it yet
                GameObject eventSystemObj = GameObject.Find("EventSystem");
                if (eventSystemObj == null)
                {
                    eventSystemObj = new GameObject("EventSystem");
                    eventSystemObj.AddComponent<EventSystem>();
                    eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                    Debug.Log($"NewCardUI: Created EventSystem automatically for drag and drop");
                }
            }
            
            // Diagnostic: Check if Canvas has GraphicRaycaster
            if (canvas != null)
            {
                GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
                if (raycaster == null)
                {
                    Debug.LogWarning($"[NewCardUI] Canvas {canvas.name} missing GraphicRaycaster! UI interactions may not work.");
                }
                else
                {
                    Debug.Log($"[NewCardUI] GraphicRaycaster found on Canvas '{canvas.name}' for '{gameObject.name}'. UI raycasting should work.");
                }
            }
            else
            {
                Debug.LogWarning($"[NewCardUI] No Canvas found in parent hierarchy for '{gameObject.name}'! This card may not work as UI.");
            }
            
            // [CardFront] Diagnostic: Log card type and drag readiness
            bool isPlayerCard = IsPlayerCard();
            bool isOpponentCard = IsOpponentCard();
            Debug.Log($"[NewCardUI] Awake complete for '{gameObject.name}'. IsPlayerCard: {isPlayerCard}, IsOpponentCard: {isOpponentCard}, AllowDrag: {allowDrag}, CanvasGroup.interactable: {canvasGroup.interactable}, CanvasGroup.blocksRaycasts: {canvasGroup.blocksRaycasts}");
            
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
            
            Debug.Log($"NewCardUI: Auto-setup containers completed. FrontContainer: {frontContainer != null}, BackContainer: {backContainer != null}", this);
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
            Debug.Log($"NewCardUI on {gameObject.name}: Initialized with card '{cardData.Data.cardName}' (InstanceID: {cardData.InstanceID}). Card field set: {card != null}");
            
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
                StartCoroutine(DelayedFlip());
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
                
            // Set card background color based on ownership
            if (cardBackground != null)
            {
                // Determine if this card belongs to the player or opponent
                bool isPlayerCard = IsPlayerCard();
                
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
            }
        }
        
        private void Start()
        {
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
                        (parent.GetComponent<CardGame.UI.NewHandUI>() != null || 
                         parent.GetComponent<CardGame.UI.NewHandOppUI>() != null);
                    
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
                CardGame.UI.NewHandUI parentHandUI = GetComponentInParent<CardGame.UI.NewHandUI>();
                if (parentHandUI != null)
                {
                    NewCard foundCard = parentHandUI.GetCardForUI(this);
                    if (foundCard != null)
                    {
                        card = foundCard;
                        Debug.Log($"[NewCardUI] Found and set card in Start() from parent NewHandUI: {card.Data.cardName}");
                    }
                }
                
                if (card == null)
                {
                    CardGame.UI.NewHandOppUI parentHandOppUI = GetComponentInParent<CardGame.UI.NewHandOppUI>();
                    if (parentHandOppUI != null)
                    {
                        NewCard foundCard = parentHandOppUI.GetCardForUI(this);
                        if (foundCard != null)
                        {
                            card = foundCard;
                            Debug.Log($"[NewCardUI] Found and set card in Start() from parent NewHandOppUI: {card.Data.cardName}");
                        }
                    }
                }
                
                // [CardFront] Final fallback: Try scene-wide search only if parent search failed
                // This is acceptable in Start() as a recovery mechanism, but prefer parent hierarchy
                if (card == null)
                {
                    #if UNITY_EDITOR
                    // Only in Editor - this is a last resort recovery
                    CardGame.UI.NewHandUI handUI = FindObjectOfType<CardGame.UI.NewHandUI>();
                    if (handUI != null)
                    {
                        NewCard foundCard = handUI.GetCardForUI(this);
                        if (foundCard != null)
                        {
                            card = foundCard;
                            Debug.Log($"[NewCardUI] Found and set card in Start() from scene NewHandUI (fallback): {card.Data.cardName}");
                        }
                    }
                    
                    if (card == null)
                    {
                        CardGame.UI.NewHandOppUI handOppUI = FindObjectOfType<CardGame.UI.NewHandOppUI>();
                        if (handOppUI != null)
                        {
                            NewCard foundCard = handOppUI.GetCardForUI(this);
                            if (foundCard != null)
                            {
                                card = foundCard;
                                Debug.Log($"[NewCardUI] Found and set card in Start() from scene NewHandOppUI (fallback): {card.Data.cardName}");
                            }
                        }
                    }
                    #endif
                }
            }
            else
            {
                Debug.Log($"NewCardUI on {gameObject.name}: Card verified in Start(): {card.Data?.cardName ?? "UNNAMED"}");
            }
        }
        
        private void Update()
        {
            
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
            
            // Assign to SpriteRenderer or Image (whichever exists)
            if (backSpriteRenderer != null && backSprite != null)
            {
                backSpriteRenderer.sprite = backSprite;
            }
            else if (backImage != null && backSprite != null)
            {
                backImage.sprite = backSprite;
            }
            else if (backSprite == null)
            {
                // No sprite assigned - ensure we have a default visual
                // The back container should already have a visual created in AutoSetupContainers
                // If not, create one now
                if (backSpriteRenderer == null && backImage == null && backContainer != null)
                {
                    // Create default visual
                    GameObject cardBackVisual = new GameObject("CardBackVisual");
                    cardBackVisual.transform.SetParent(backContainer.transform);
                    cardBackVisual.transform.localPosition = Vector3.zero;
                    cardBackVisual.transform.localRotation = Quaternion.identity;
                    cardBackVisual.transform.localScale = Vector3.one;
                    
                    RectTransform rectTransform = GetComponent<RectTransform>();
                    if (rectTransform != null)
                    {
                        // UI card - use Image
                        backImage = cardBackVisual.AddComponent<UnityEngine.UI.Image>();
                        if (backImage != null)
                        {
                            backImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);
                            
                            // Image component needs a sprite to render - create a simple white texture
                            Texture2D whiteTexture = new Texture2D(1, 1);
                            whiteTexture.SetPixel(0, 0, Color.white);
                            whiteTexture.Apply();
                            backImage.sprite = Sprite.Create(whiteTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
                            
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
                        // 2D card - use SpriteRenderer
                        backSpriteRenderer = cardBackVisual.AddComponent<SpriteRenderer>();
                        if (backSpriteRenderer != null)
                        {
                            backSpriteRenderer.color = new Color(0.3f, 0.3f, 0.3f, 1f);
                        }
                    }
                }
                // Silently use default colored back - no warning needed as this is expected behavior
                // Debug.LogWarning("NewCardUI: No card back sprite assigned (neither in card data nor default). Using default colored back.", this);
            }
        }
        
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
            if (allowClickToFlip && flipAnimation != null && !flipAnimation.isAnimating)
            {
                flipAnimation.FlipToggle();
            }
        }
        
        public void OnBeginDrag(PointerEventData eventData)
        {
            // [CardFront] CardFront-style logging prefix - ALWAYS log to diagnose missing player card drags
            Debug.Log($"[NewCardUI] OnBeginDrag CALLED for '{gameObject.name}'. allowDrag: {allowDrag}, card bound: {card != null}, Card property: {Card != null}, IsPlayerCard: {IsPlayerCard()}, IsOpponentCard: {IsOpponentCard()}");
            
            // Prevent dragging prefab assets (not instantiated in scene)
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
            
           // CRITICAL: Opponent cards should NEVER be draggable by player
           // Check if this is an opponent card and block dragging immediately
           bool isOpponentCard = IsOpponentCard();
           if (isOpponentCard)
           {
               // [CardFront] This is expected behavior - use Debug.Log instead of LogWarning to reduce spam
               Debug.Log($"[NewCardUI] Opponent card '{gameObject.name}' drag blocked (expected behavior).");
               return;
           }
            
            // [CardFront] CardFront Architecture: Use Hub connections instead of FindObjectOfType()
            // Recover card reference if lost using parent Hub (NewHandUI/NewHandOppUI)
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
                    CardGame.UI.NewHandUI handUI = GetComponentInParent<CardGame.UI.NewHandUI>();
                    CardGame.UI.NewHandOppUI handOppUI = GetComponentInParent<CardGame.UI.NewHandOppUI>();
                    
                    if (handUI != null)
                    {
                        NewCard foundCard = handUI.GetCardForUI(this);
                        if (foundCard != null)
                        {
                            card = foundCard;
                            Debug.Log($"[NewCardUI] Recovered card from parent HandUI Hub: {card.Data.cardName}");
                        }
                    }
                    else if (handOppUI != null)
                    {
                        NewCard foundCard = handOppUI.GetCardForUI(this);
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
                        handUI = parent.GetComponentInParent<CardGame.UI.NewHandUI>();
                        handOppUI = parent.GetComponentInParent<CardGame.UI.NewHandOppUI>();
                        
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
                        else if (handOppUI != null)
                        {
                            int siblingIndex = transform.GetSiblingIndex();
                            if (siblingIndex >= 0 && siblingIndex < handOppUI.GetCardCount())
                            {
                                card = handOppUI.GetCardForUIByIndex(siblingIndex);
                                if (card != null)
                                {
                                    Debug.Log($"[NewCardUI] Recovered card by sibling index from HandOppUI Hub: {card.Data.cardName}");
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
                Debug.LogError($"[NewCardUI] CRITICAL: Card reference lost. GameObject: {gameObject.name}, InstanceID: {GetInstanceID()}. Cannot start drag.");
                Debug.LogError($"[NewCardUI] This indicates Initialize() was not called or card field was cleared. Check CardFactory.");
                return;
            }
            
            // [CardFront] Turn System Rules: Only active side can move cards
            // Check turn state via Hub (FateFlowController)
            if (CardGame.Managers.FateFlowController.Instance != null)
            {
                bool canAct = CardGame.Managers.FateFlowController.Instance.CanAct(CardGame.Managers.FateSide.Player);
                if (!canAct)
                {
                    Debug.LogWarning($"[NewCardUI] Cannot drag - not player's turn. Current fate: {CardGame.Managers.FateFlowController.Instance.CurrentFate}");
                    return; // Turn system blocks drag
                }
            }
            
            // [CardFront] All checks passed - start drag
            Debug.Log($"[NewCardUI] Starting drag for card: {card.Data.cardName}");
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
            // [CardFront] OnEndDrag: Validate drag state
            if (!isDragging)
            {
                // [CardFront] Suppress warnings for opponent cards - OnEndDrag is called even when drag was blocked
                // This is expected behavior for opponent cards that were correctly prevented from dragging
                if (IsOpponentCard())
                {
                    // Silently ignore - opponent cards are expected to have isDragging = false
                    return;
                }
                
                // Only warn if this is a player card that should have been dragging
                if (IsPlayerCard() && allowDrag)
                {
                    Debug.LogWarning($"[NewCardUI] OnEndDrag called but isDragging is false for player card '{gameObject.name}'. Drag may have been interrupted.");
                }
                return;
            }
            
            Debug.Log($"[NewCardUI] OnEndDrag START for '{gameObject.name}'. Card: {(card != null ? card.Data?.cardName : "null")}, Position: {eventData.position}");
            
            isDragging = false;
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            
            // [CardFront] Verify card reference before proceeding
            if (card == null || card.Data == null)
            {
                Debug.LogError($"[NewCardUI] OnEndDrag: Card reference lost. Cannot complete drop. GameObject: {gameObject.name}");
                return;
            }
            
            Debug.Log($"[NewCardUI] OnEndDrag: Card '{card.Data.cardName}' dropped at screen position {eventData.position}");
            
            // [CardFront] Cluster approach: Use UI raycast to find drop area (local system)
            Debug.Log($"[NewCardUI] OnEndDrag: Attempting to find drop area via UI raycast at position {eventData.position}...");
            CardDropArea1 dropArea = FindDropAreaViaRaycast(eventData);
            
            if (dropArea == null)
            {
                Debug.Log($"[NewCardUI] OnEndDrag: UI raycast found no drop area. Checking drop areas in scene...");
                // Diagnostic: Count all CardDropArea1 components in scene
                CardDropArea1[] allDropAreas = FindObjectsOfType<CardDropArea1>(true);
                Debug.Log($"[NewCardUI] OnEndDrag: Found {allDropAreas.Length} CardDropArea1 component(s) in scene.");
                foreach (var area in allDropAreas)
                {
                    Debug.Log($"[NewCardUI] OnEndDrag:   - CardDropArea1 '{area.name}' at {area.transform.position}, IsOccupied: {area.IsOccupied}");
                }
            }
            
            // [CardFront] Fallback: Use Physics2D if UI raycast fails
            if (dropArea == null && Camera.main != null)
            {
                Debug.Log($"[NewCardUI] OnEndDrag: Attempting Physics2D fallback at position {eventData.position}...");
                dropArea = FindDropAreaViaPhysics2D(eventData);
            }
            
            // [CardFront] If drop area found, place card on board
            if (dropArea != null)
            {
                Debug.Log($"[NewCardUI] OnEndDrag: Drop area found! '{dropArea.name}' at {dropArea.transform.position}. Placing card...");
                PlaceCardOnBoard(dropArea);
                return;
            }
            
            Debug.LogWarning($"[NewCardUI] OnEndDrag: No valid drop area found for card '{card.Data.cardName}' at screen position {eventData.position}. Card will return to hand.");
            // Card returns to original position via NewHandUI.ArrangeCards()
        }
        
        /// <summary>
        /// [CardFront] Cluster method: Find drop area via UI raycast (local system)
        /// </summary>
        private CardDropArea1 FindDropAreaViaRaycast(PointerEventData eventData)
        {
            if (EventSystem.current == null)
            {
                Debug.LogWarning($"[NewCardUI] FindDropAreaViaRaycast: EventSystem.current is null!");
                return null;
            }
            
            List<RaycastResult> raycastResults = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, raycastResults);
            
            Debug.Log($"[NewCardUI] FindDropAreaViaRaycast: RaycastAll found {raycastResults.Count} UI object(s) at position {eventData.position}");
            
            foreach (RaycastResult result in raycastResults)
            {
                Debug.Log($"[NewCardUI] FindDropAreaViaRaycast: Checking '{result.gameObject.name}' for CardDropArea1...");
                CardDropArea1 dropArea = result.gameObject.GetComponent<CardDropArea1>();
                if (dropArea != null)
                {
                    Debug.Log($"[NewCardUI] FindDropAreaViaRaycast: Found CardDropArea1 '{dropArea.name}'! IsOccupied: {dropArea.IsOccupied}");
                    if (!dropArea.IsOccupied)
                    {
                        Debug.Log($"[NewCardUI] Found CardDropArea1 via UI raycast: {dropArea.name}");
                        return dropArea;
                    }
                    else
                    {
                        Debug.Log($"[NewCardUI] FindDropAreaViaRaycast: CardDropArea1 '{dropArea.name}' is occupied. Skipping.");
                    }
                }
            }
            
            Debug.Log($"[NewCardUI] FindDropAreaViaRaycast: No unoccupied CardDropArea1 found in raycast results.");
            return null;
        }
        
        /// <summary>
        /// [CardFront] Cluster method: Find drop area via Physics2D (local system)
        /// </summary>
        private CardDropArea1 FindDropAreaViaPhysics2D(PointerEventData eventData)
        {
            if (Camera.main == null)
            {
                Debug.LogWarning($"[NewCardUI] FindDropAreaViaPhysics2D: Camera.main is null! Cannot convert screen to world position.");
                return null;
            }
            
            Vector3 screenPos = new Vector3(eventData.position.x, eventData.position.y, Camera.main.nearClipPlane);
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPos);
            worldPos.z = 0f;
            
            Debug.Log($"[NewCardUI] FindDropAreaViaPhysics2D: Screen position {screenPos}, World position {worldPos}");
            
            Collider2D[] hitColliders = Physics2D.OverlapPointAll(worldPos);
            
            Debug.Log($"[NewCardUI] FindDropAreaViaPhysics2D: Physics2D.OverlapPointAll found {hitColliders.Length} collider(s) at world position {worldPos}");
            
            foreach (Collider2D hitCollider in hitColliders)
            {
                if (hitCollider == null) continue;
                
                Debug.Log($"[NewCardUI] FindDropAreaViaPhysics2D: Checking collider '{hitCollider.gameObject.name}' for CardDropArea1...");
                CardDropArea1 dropArea = hitCollider.GetComponent<CardDropArea1>();
                if (dropArea != null)
                {
                    Debug.Log($"[NewCardUI] FindDropAreaViaPhysics2D: Found CardDropArea1 '{dropArea.name}'! IsOccupied: {dropArea.IsOccupied}");
                    if (!dropArea.IsOccupied)
                    {
                        Debug.Log($"[NewCardUI] Found CardDropArea1 via Physics2D: {dropArea.name}");
                        return dropArea;
                    }
                    else
                    {
                        Debug.Log($"[NewCardUI] FindDropAreaViaPhysics2D: CardDropArea1 '{dropArea.name}' is occupied. Skipping.");
                    }
                }
            }
            
            Debug.Log($"[NewCardUI] FindDropAreaViaPhysics2D: No unoccupied CardDropArea1 found in Physics2D results.");
            return null;
        }
        
        private void PlaceCardOnBoard(CardDropArea1 dropArea)
        {
            Debug.Log($"PlaceCardOnBoard: Attempting to place card on {dropArea?.name}");
            
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
                else
                {
                    Debug.Log($"PlaceCardOnBoard: Turn check passed. Current fate: {CardGame.Managers.FateFlowController.Instance.CurrentFate}");
                }
            }
            else
            {
                Debug.LogWarning("PlaceCardOnBoard: FateFlowController.Instance is null - allowing placement anyway");
            }
            
            // Check if drop area is occupied
            if (dropArea.IsOccupied)
            {
                Debug.LogWarning($"Cannot place card - drop area {dropArea.name} is occupied");
                return;
            }
            
            // [CardFront] Hub connection: Get deck manager via Hub (NewHandUI) instead of FindObjectOfType
            // NewHandUI is the Hub that manages card UI instances and knows about deckManager
            CardGame.Managers.NewDeckManager deckManager = null;
            
            // Use parent Hub connection to get deck manager
            CardGame.UI.NewHandUI handUI = GetComponentInParent<CardGame.UI.NewHandUI>();
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
                Debug.LogError("[NewCardUI] PlaceCardOnBoard: NewHandUI Hub not found in parent hierarchy. Cannot place card.");
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
            
            Debug.Log($"[NewCardUI] PlaceCardOnBoard: All checks passed. Creating board card for '{card.Data.cardName}'...");
            
            // [CardFront] Hub approach: Use CardFactory to create board card
            // CardFactory is the Hub for card creation - use it instead of manual instantiation
            GameObject boardCardPrefab = UnityEngine.Resources.Load<GameObject>("NewCardPrefab");
            
            if (boardCardPrefab == null)
            {
                Debug.LogError("[NewCardUI] PlaceCardOnBoard: NewCardPrefab not found in Resources folder. Cannot create board card.");
                return;
            }
            
            // [CardFront] Use CardFactory Hub for board card creation
            GameObject boardCard = CardGame.Factories.CardFactory.CreateBoardCard(
                card, 
                boardCardPrefab, 
                dropArea.transform.position
            );
            
            if (boardCard == null)
            {
                Debug.LogError("[NewCardUI] PlaceCardOnBoard: CardFactory failed to create board card.");
                return;
            }
            
            // Get CardMover component (should be added by CardFactory)
            CardMover cardMover = boardCard.GetComponent<CardMover>();
            if (cardMover == null)
            {
                cardMover = boardCard.GetComponentInChildren<CardMover>();
            }
            
            if (cardMover == null)
            {
                Debug.LogError("[NewCardUI] PlaceCardOnBoard: Board card prefab missing CardMover component. Cannot drag board card.");
                Destroy(boardCard);
                return;
            }
            
            // [CardFront] Trigger drop through CardDropArea1 (uses event channel)
            // This will handle: playing card, placement, battles via Hub connections
            Debug.Log($"[NewCardUI] PlaceCardOnBoard: Triggering drop for card '{card.Data.cardName}' on {dropArea.name}");
            dropArea.OnCardDrop(cardMover);
            
            // [CardFront] Remove from hand UI via event channel (cluster cleanup)
            // NewHandUI will handle removal when OnCardPlayed event fires
            Debug.Log($"[NewCardUI] PlaceCardOnBoard: Card '{card.Data.cardName}' placement complete!");
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
            if (TryGetComponent<CardMover>(out var mover))
            {
                mover.SetCard(card);
                syncCount++;
                Debug.Log($"[NewCardUI] Synced card reference '{card.Data.cardName}' to CardMover on '{gameObject.name}'");
            }
            
            // Opponent mover on this GameObject (if present)
            if (TryGetComponent<CardMoverOpp>(out var moverOpp))
            {
                moverOpp.SetCard(card);
                syncCount++;
                Debug.Log($"[NewCardUI] Synced card reference '{card.Data.cardName}' to CardMoverOpp on '{gameObject.name}'");
            }
            
            // Some prefabs nest CardMover/CardMoverOpp on children, so update them too
            CardMover[] childMovers = GetComponentsInChildren<CardMover>(true);
            foreach (var childMover in childMovers)
            {
                // Skip if we already synced this one (same component on root GameObject)
                if (childMover == mover) continue;
                
                childMover.SetCard(card);
                syncCount++;
                Debug.Log($"[NewCardUI] Synced card reference '{card.Data.cardName}' to child CardMover on '{childMover.gameObject.name}'");
            }
            
            CardMoverOpp[] childMoverOpps = GetComponentsInChildren<CardMoverOpp>(true);
            foreach (var childMoverOpp in childMoverOpps)
            {
                // Skip if we already synced this one (same component on root GameObject)
                if (childMoverOpp == moverOpp) continue;
                
                childMoverOpp.SetCard(card);
                syncCount++;
                Debug.Log($"[NewCardUI] Synced card reference '{card.Data.cardName}' to child CardMoverOpp on '{childMoverOpp.gameObject.name}'");
            }
            
            if (syncCount == 0)
            {
                Debug.LogWarning($"[NewCardUI] SyncCardReferenceToMovers: No CardMover or CardMoverOpp components found on '{gameObject.name}' or its children. Card will need to find reference via FindCardReference().");
            }
        }

        /// <summary>
        /// Determines if this card belongs to the player (vs opponent)
        /// </summary>
        /// <summary>
        /// Determines if this card belongs to the opponent.
        /// Checks GameObject name, parent hierarchy, and deck manager.
        /// </summary>
        private bool IsOpponentCard()
        {
            // Check GameObject name for "Opp" marker
            if (gameObject.name.Contains("Opp") || gameObject.name.Contains("NewCardPrefabOpp"))
            {
                return true;
            }
            
            // Check parent hierarchy for opponent containers
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
            // Check if card is in opponent hand UI via parent Hub
            CardGame.UI.NewHandOppUI handOppUI = GetComponentInParent<CardGame.UI.NewHandOppUI>();
            if (handOppUI != null)
            {
                NewCard foundCard = handOppUI.GetCardForUI(this);
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
            CardGame.UI.NewHandUI handUI = GetComponentInParent<CardGame.UI.NewHandUI>();
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

            // Check if it's a CardMover (player card) vs CardMoverOpp (opponent card)
            CardMover cardMover = GetComponent<CardMover>();
            if (cardMover != null)
            {
                return true; // Player card
            }

            CardMoverOpp cardMoverOpp = GetComponent<CardMoverOpp>();
            if (cardMoverOpp != null)
            {
                return false; // Opponent card
            }

            // Check in children/parents
            cardMover = GetComponentInChildren<CardMover>();
            if (cardMover != null) return true;

            cardMoverOpp = GetComponentInChildren<CardMoverOpp>();
            if (cardMoverOpp != null) return false;

            cardMover = GetComponentInParent<CardMover>();
            if (cardMover != null) return true;

            cardMoverOpp = GetComponentInParent<CardMoverOpp>();
            if (cardMoverOpp != null) return false;

            // Default: assume player card if we can't determine
            return true;
        }
    }
}