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
    public class NewCardUI : MonoBehaviour
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
           
        }
        
        public void Initialize(NewCard cardData)
        {
            if (cardData == null)
            {
                Debug.LogError("NewCardUI: Cannot initialize with null card data!");
                return;
            }
            
            card = cardData;
          
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