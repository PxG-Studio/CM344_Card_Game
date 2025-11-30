using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using CardGame.Core;
using CardGame.Data;

namespace CardGame.UI
{
    /// <summary>
    /// UI representation of a card
    /// </summary>
    public class CardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        [Header("UI References")]
        [SerializeField] private Image cardBackground;
        [SerializeField] private Image artwork;
        [SerializeField] private TextMeshProUGUI cardNameText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI manaCostText;
        [SerializeField] private TextMeshProUGUI attackValueText;
        [SerializeField] private TextMeshProUGUI defenseValueText;
        [SerializeField] private GameObject attackIcon;
        [SerializeField] private GameObject defenseIcon;
        
        [Header("Visual Settings")]
        [SerializeField] private float hoverScale = 1.1f;
        [SerializeField] private float hoverYOffset = 20f;
        [SerializeField] private float animationSpeed = 10f;
        
        private Card card;
        private Vector3 originalPosition;
        private Vector3 originalScale;
        private bool isHovering = false;
        private bool isDragging = false;
        private Canvas canvas;
        private RectTransform rectTransform;
        
        public Card Card => card;
        public System.Action<CardUI> OnCardClicked;
        public System.Action<CardUI> OnCardPlayed;
        
        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvas = GetComponentInParent<Canvas>();
            originalScale = transform.localScale;
        }
        
        public void Initialize(Card cardData)
        {
            card = cardData;
            UpdateVisuals();
        }
        
        private void UpdateVisuals()
        {
            if (card == null) return;
            
            // Update text fields
            if (cardNameText != null)
                cardNameText.text = card.Data.cardName;
                
            if (descriptionText != null)
                descriptionText.text = card.Data.description;
                
            if (manaCostText != null)
                manaCostText.text = card.CurrentManaCost.ToString();
                
            if (attackValueText != null)
            {
                attackValueText.text = card.CurrentAttackValue.ToString();
                attackIcon?.SetActive(card.CurrentAttackValue > 0);
            }
                
            if (defenseValueText != null)
            {
                defenseValueText.text = card.CurrentDefenseValue.ToString();
                defenseIcon?.SetActive(card.CurrentDefenseValue > 0);
            }
            
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
            isHovering = true;
            originalPosition = transform.position;
        }
        
        public void OnPointerExit(PointerEventData eventData)
        {
            isHovering = false;
        }
        
        public void OnPointerDown(PointerEventData eventData)
        {
            isDragging = true;
            originalPosition = transform.position;
        }
        
        public void OnPointerUp(PointerEventData eventData)
        {
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
            OnCardPlayed?.Invoke(this);
        }
        
        public void SetInteractable(bool interactable)
        {
            CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
                
            canvasGroup.interactable = interactable;
            canvasGroup.alpha = interactable ? 1f : 0.5f;
        }
    }
}

