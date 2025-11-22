using System.Collections.Generic;
using UnityEngine;
using CardGame.Core;
using CardGame.Managers;
using CardGame.Factories;

namespace CardGame.UI
{
    /// <summary>
    /// Manages the visual representation of the player's hand with NewCard
    /// </summary>
    public class NewHandUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private NewCardUI cardPrefab;
        [SerializeField] private Transform cardContainer;
        
        [Header("Layout Settings")]
        [SerializeField] private float cardSpacing = 120f;
        [SerializeField] private float maxSpread = 800f;
        [SerializeField] private float arcHeight = 0f;
        [SerializeField] private float rotationAngle = 5f;
        
        private List<NewCardUI> cardUIList = new List<NewCardUI>();
        private NewDeckManager deckManager;
        
        /// <summary>
        /// [CardFront] Hub property: Exposes deck manager for Hub connections
        /// </summary>
        public NewDeckManager DeckManager => deckManager;
        
        /// <summary>
        /// Gets the card associated with a specific card UI instance.
        /// </summary>
        public NewCard GetCardForUI(NewCardUI cardUI)
        {
            if (cardUI == null) return null;
            
            // First try: check if it's in the list and has a card
            if (cardUIList.Contains(cardUI))
            {
                if (cardUI.Card != null)
                {
                    return cardUI.Card;
                }
            }
            
            // Second try: find by GameObject reference (in case card field is null)
            int index = -1;
            for (int i = 0; i < cardUIList.Count; i++)
            {
                var ui = cardUIList[i];
                if (ui != null && ui.gameObject == cardUI.gameObject)
                {
                    if (ui.Card != null)
                    {
                        return ui.Card;
                    }
                    // Store index for fallback
                    index = i;
                    break;
                }
            }
            
            // Third try: match by index with deck manager hand (if card field is null)
            if (index >= 0 && deckManager != null && deckManager.Hand != null && index < deckManager.Hand.Count)
            {
                NewCard handCard = deckManager.Hand[index];
                if (handCard != null)
                {
                    Debug.Log($"GetCardForUI: Found card by index matching: {handCard.Data?.cardName}");
                    return handCard;
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Gets the card count in the hand UI.
        /// </summary>
        public int GetCardCount()
        {
            return cardUIList.Count;
        }
        
        /// <summary>
        /// Gets the card at a specific index in the hand UI list.
        /// </summary>
        public NewCard GetCardForUIByIndex(int index)
        {
            if (index >= 0 && index < cardUIList.Count)
            {
                var cardUI = cardUIList[index];
                if (cardUI != null)
                {
                    if (cardUI.Card != null)
                    {
                        return cardUI.Card;
                    }
                    // Try to get from deck manager by index
                    if (deckManager != null && deckManager.Hand != null && index < deckManager.Hand.Count)
                    {
                        return deckManager.Hand[index];
                    }
                }
            }
            return null;
        }
        
        private void Start()
        {
            deckManager = FindObjectOfType<NewDeckManager>();
            
            if (deckManager != null)
            {
                deckManager.OnCardDrawn += HandleCardDrawn;
                deckManager.OnCardPlayed += HandleCardPlayed;
                deckManager.OnCardDiscarded += HandleCardDiscarded;
            }
        }
        
        private void OnDestroy()
        {
            if (deckManager != null)
            {
                deckManager.OnCardDrawn -= HandleCardDrawn;
                deckManager.OnCardPlayed -= HandleCardPlayed;
                deckManager.OnCardDiscarded -= HandleCardDiscarded;
            }
        }
        
        private void HandleCardDrawn(NewCard card)
        {
            AddCardToHand(card);
        }
        
        private void HandleCardPlayed(NewCard card)
        {
            RemoveCardFromHand(card);
        }
        
        private void HandleCardDiscarded(NewCard card)
        {
            RemoveCardFromHand(card);
        }
        
        public void AddCardToHand(NewCard card)
        {
            if (card == null)
            {
                Debug.LogError("NewHandUI.AddCardToHand: Cannot add null card to hand!");
                return;
            }
            
            if (cardPrefab == null)
            {
                Debug.LogError("NewHandUI.AddCardToHand: CardPrefab is not assigned!");
                return;
            }
            
            if (cardContainer == null)
            {
                Debug.LogError("NewHandUI.AddCardToHand: CardContainer is not assigned!");
                return;
            }
            
            // Calculate reveal delay BEFORE creating card (for staggered flip animations)
            float revealDelay = 0f;
            if (cardUIList.Count > 0 && cardPrefab.autoFlipOnReveal)
            {
                revealDelay = cardUIList.Count * 0.1f; // 0s, 0.1s, 0.2s, etc.
            }
            
            // CRITICAL: Use CardFactory to ensure Initialize() is called BEFORE Start()
            NewCardUI cardUI = CardFactory.CreateCardUI(card, cardPrefab, cardContainer, revealDelay);
            
            if (cardUI == null)
            {
                Debug.LogError($"NewHandUI.AddCardToHand: Failed to create card UI for '{card.Data?.cardName ?? "UNKNOWN"}'");
                return;
            }
            
            // Verify card is bound (should always be true if CardFactory worked)
            if (cardUI.Card == null)
            {
                Debug.LogError($"NewHandUI.AddCardToHand: Card UI was created but card is null for '{card.Data?.cardName ?? "UNKNOWN"}'. This should never happen with CardFactory.");
                Destroy(cardUI.gameObject);
                return;
            }
            
            // Subscribe to card played event
            cardUI.OnCardPlayed += HandleCardUIPlayed;
            
            // Add to list
            cardUIList.Add(cardUI);
            
            // Arrange cards in hand
            ArrangeCards();
            
            Debug.Log($"NewHandUI.AddCardToHand: Successfully added card '{card.Data.cardName}' to hand. Total cards: {cardUIList.Count}");
        }
        
        private void HandleCardUIPlayed(NewCardUI cardUI)
        {
            if (deckManager != null)
            {
                // Play the card
                deckManager.PlayCard(cardUI.Card);
                
                // Apply card effects if needed
                ApplyCardEffects(cardUI.Card);
            }
        }
        
        private void ApplyCardEffects(NewCard card)
        {
            // Apply effects based on your game logic
            // This is where you'd handle the directional stats and card effects
            if (card.Data.effects != null)
            {
                foreach (var effect in card.Data.effects)
                {
                    // Handle different effect types
                    Debug.Log($"Applying effect: {effect.effectType} with value {effect.effectValue}");
                    // Add your effect handling logic here
                }
            }
        }
        
        public void RemoveCardFromHand(NewCard card)
        {
            NewCardUI cardUIToRemove = cardUIList.Find(c => c.Card.InstanceID == card.InstanceID);
            
            if (cardUIToRemove != null)
            {
                cardUIList.Remove(cardUIToRemove);
                
                // Only destroy if it's a UI card (NewCardUI), not a 2D board card (CardMover)
                // CardMover cards should stay on the board when played
                CardMover cardMover = cardUIToRemove.GetComponent<CardMover>();
                if (cardMover == null)
                {
                    // It's a UI card, safe to destroy
                    Destroy(cardUIToRemove.gameObject);
                }
                else
                {
                    // It's a board card (CardMover), just remove from UI list but keep the GameObject
                    Debug.Log($"Card {card.Data.cardName} played on board - keeping GameObject");
                }
                
                ArrangeCards();
            }
        }
        
        public void ClearHand()
        {
            foreach (NewCardUI cardUI in cardUIList)
            {
                Destroy(cardUI.gameObject);
            }
            cardUIList.Clear();
        }
        
        private void ArrangeCards()
        {
            int cardCount = cardUIList.Count;
            if (cardCount == 0) return;
            
            float totalHeight = Mathf.Min((cardCount - 1) * cardSpacing, maxSpread);
            float startY = -totalHeight / 2f;
            
            for (int i = 0; i < cardCount; i++)
            {
                NewCardUI cardUI = cardUIList[i];
                RectTransform rectTransform = cardUI.GetComponent<RectTransform>();
                
                // Calculate position
                float t = cardCount > 1 ? (float)i / (cardCount - 1) : 0.5f;
                float y = startY + (t * totalHeight);
                
                // Calculate arc
                float normalizedPos = (2f * t) - 1f; // -1 to 1
                float x = -Mathf.Abs(normalizedPos) * arcHeight;
                
                // Calculate rotation
                float rotation = normalizedPos * rotationAngle;
                
                // Apply transform
                rectTransform.anchoredPosition = new Vector2(x, y);
                rectTransform.localRotation = Quaternion.Euler(0, 0, rotation);
                
                // Set sibling index for proper overlap
                cardUI.transform.SetSiblingIndex(i);
            }
        }
    }
}