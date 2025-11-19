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
    public class NewCardUI : MonoBehaviour, IPointerClickHandler
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
        
        [Header("Captured Colors")]
        [SerializeField] private Color playerCapturedColor = new Color(1f, 0.5f, 0f, 1f); // Orange for player's cards (matches card border orange)
        [SerializeField] private Color opponentCapturedColor = new Color(0f, 0.8f, 0f, 1f); // Green for opponent's captured cards
        
        public Color PlayerCapturedColor => playerCapturedColor;
        public Color OpponentCapturedColor => opponentCapturedColor;
        
       
        private NewCard card;
       
        
        private Canvas canvas;
        private RectTransform rectTransform;
        
        public NewCard Card => card;
        public System.Action<NewCardUI> OnCardClicked;
        public System.Action<NewCardUI> OnCardPlayed;
        
        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvas = GetComponentInParent<Canvas>();
            
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
                Debug.LogError("NewCardUI: Cannot initialize with null card data!");
                return;
            }
            
            card = cardData;
            
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
                Debug.LogWarning("NewCardUI: No card back sprite assigned (neither in card data nor default). Using default colored back.", this);
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
        
        public void RefreshVisuals()
        {
            UpdateVisuals();
        }

        /// <summary>
        /// Determines if this card belongs to the player (vs opponent)
        /// </summary>
        private bool IsPlayerCard()
        {
            // Check if card is in player's hand
            if (card != null)
            {
                CardGame.Managers.NewDeckManager playerDeckManager = FindObjectOfType<CardGame.Managers.NewDeckManager>();
                if (playerDeckManager != null && playerDeckManager.Hand.Contains(card))
                {
                    return true; // Card is in player's hand
                }
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