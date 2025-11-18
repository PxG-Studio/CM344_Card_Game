using System.Collections;
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
            
            // Get or create CardFlipAnimation
            flipAnimation = GetComponent<CardFlipAnimation>();
            if (flipAnimation == null)
            {
                flipAnimation = gameObject.AddComponent<CardFlipAnimation>();
            }
            
            // Assign container references to CardFlipAnimation if not already set
            if (flipAnimation != null && frontContainer != null && backContainer != null)
            {
                flipAnimation.SetContainers(frontContainer, backContainer);
            }
            
            // Validate setup (logs warnings if containers missing)
            if (flipAnimation != null)
            {
                flipAnimation.ValidateSetup();
            }
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
            
            // Set initial flip state (before UpdateVisuals so back is shown)
            if (startFaceDown && flipAnimation != null)
            {
                flipAnimation.SetFlippedState(false, instant: true); // Show back, hide front
            }
            
            UpdateVisuals();
            
            // Auto-flip if enabled (use revealDelay that may have been set before Initialize)
            if (autoFlipOnReveal && flipAnimation != null)
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
                
            if (cardBackground != null)
                cardBackground.color = card.Data.cardColor;
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
                Debug.LogWarning("NewCardUI: No card back sprite assigned (neither in card data nor default). Card back will be blank.", this);
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
    }
}