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
    public class NewHandP1UI : MonoBehaviour
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
        private NewDeckManagerP1 deckManager;
        
        // [CardFront] Static cache for prefab reference (fallback if serialized reference is lost)
        private static NewCardUI staticCardPrefab;
        
        /// <summary>
        /// [CardFront] Hub property: Exposes deck manager for Hub connections
        /// </summary>
        public NewDeckManagerP1 DeckManager => deckManager;
        
        /// <summary>
        /// [CardFront] Hub property: Exposes card prefab for Hub connections (board card creation).
        /// </summary>
        public NewCardUI CardPrefab => cardPrefab;
        
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
        
        private void Awake()
        {
            // [CardFront] Cache prefab reference in static variable as fallback
            if (cardPrefab != null && staticCardPrefab == null)
            {
                staticCardPrefab = cardPrefab;
            }
            
            // [CardFront] Restore prefab reference from static cache if lost
            if (cardPrefab == null && staticCardPrefab != null)
            {
                cardPrefab = staticCardPrefab;
            }
            
            // Ensure cardContainer is assigned
            if (cardContainer == null)
            {
                cardContainer = transform;
            }
        }
        
        private void Start()
        {
            deckManager = FindObjectOfType<NewDeckManagerP1>();
            
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
                // [CardFront] Try to restore from static cache
                if (staticCardPrefab != null)
                {
                    cardPrefab = staticCardPrefab;
                }
                else
                {
                    Debug.LogError("[NewHandUI] AddCardToHand: CardPrefab is null and static cache is also null! Please ensure the prefab is assigned in the Inspector for the NewHandUI component.");
                    return;
                }
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
            
            // Show delta marker when card is drawn (+1 for gaining a card)
            // Delay slightly to allow card to be positioned first
            StartCoroutine(ShowDrawDeltaMarker(cardUI.transform));
        }
        
        private System.Collections.IEnumerator ShowDrawDeltaMarker(Transform cardTransform)
        {
            // Wait a frame to ensure card is positioned
            yield return null;
            
            // Show alert marker (!) at card position for newly drawn card
            if (cardTransform != null)
            {
                DeltaMarkerSystem.ShowAlert(cardTransform, "!");
            }
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
                
                // Only destroy if it's a UI card (NewCardUI), not a 2D board card (CardMoverP1)
                // CardMoverP1 cards should stay on the board when played
                CardMoverP1 cardMover = cardUIToRemove.GetComponent<CardMoverP1>();
                if (cardMover == null)
                {
                    // It's a UI card, safe to destroy
                    Destroy(cardUIToRemove.gameObject);
                }
                else
                {
                    // It's a board card (CardMover), just remove from UI list but keep the GameObject
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
                if (cardUI == null) continue;
                
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