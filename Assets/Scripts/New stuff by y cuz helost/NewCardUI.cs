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
    public class NewCardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IDragHandler
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
        
        [Header("Visual Settings")]
        [SerializeField] private float hoverScale = 1.1f;
        [SerializeField] private float hoverYOffset = 20f;
        [SerializeField] private float animationSpeed = 10f;
        
        private NewCard card;
        private Vector3 originalPosition;
        private Vector3 originalScale;
        private bool isHovering = false;
        private bool isDragging = false;
        private Canvas canvas;
        private RectTransform rectTransform;
        
        public NewCard Card => card;
        public System.Action<NewCardUI> OnCardClicked;
        public System.Action<NewCardUI> OnCardPlayed;
        
        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvas = GetComponentInParent<Canvas>();
            originalScale = transform.localScale;
        }
        
        public void Initialize(NewCard cardData)
        {
            if (cardData == null)
            {
                Debug.LogError("NewCardUI: Cannot initialize with null card data!");
                return;
            }
            
            card = cardData;
            originalPosition = transform.position;
            UpdateVisuals();
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
            if (!isDragging)
            {
                // Smooth animation for hover effect
                Vector3 targetPosition = originalPosition;
                Vector3 targetScale = originalScale;
                
                if (isHovering)
                {
                    targetPosition += Vector3.up * hoverYOffset;
                    targetScale = originalScale * hoverScale;
                }
                
                transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * animationSpeed);
                transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * animationSpeed);
            }
        }
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (card == null || !card.IsPlayable) return;
            
            isHovering = true;
            originalPosition = transform.position;
        }
        
        public void OnPointerExit(PointerEventData eventData)
        {
            isHovering = false;
        }
        
        public void OnPointerDown(PointerEventData eventData)
        {
            if (card == null || !card.IsPlayable) return;
            
            isDragging = true;
            originalPosition = transform.position;
        }
        
        public void OnPointerUp(PointerEventData eventData)
        {
            if (!isDragging) return;
            
            isDragging = false;
            
            // Check if card was dragged to valid play area
            if (IsOverPlayArea(eventData.position))
            {
                PlayCard();
            }
            else
            {
                // Return to original position
                transform.position = originalPosition;
            }
        }
        
        public void OnDrag(PointerEventData eventData)
        {
            if (isDragging && canvas != null)
            {
                Vector2 position;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvas.transform as RectTransform,
                    eventData.position,
                    canvas.worldCamera,
                    out position
                );
                
                transform.position = canvas.transform.TransformPoint(position);
            }
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
                
            canvasGroup.interactable = interactable;
            canvasGroup.alpha = interactable ? 1f : 0.5f;
        }
        
        public void RefreshVisuals()
        {
            UpdateVisuals();
        }
    }
}