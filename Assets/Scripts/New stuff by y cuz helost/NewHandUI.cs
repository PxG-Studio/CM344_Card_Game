using System.Collections.Generic;
using UnityEngine;
using CardGame.Core;
using CardGame.Managers;

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
        [SerializeField] private float arcHeight = 50f;
        [SerializeField] private float rotationAngle = 5f;
        
        private List<NewCardUI> cardUIList = new List<NewCardUI>();
        private NewDeckManager deckManager;
        
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
            if (cardPrefab == null || cardContainer == null)
            {
                Debug.LogError("NewCardPrefab or CardContainer not assigned!");
                return;
            }
            
            NewCardUI cardUI = Instantiate(cardPrefab, cardContainer);
            cardUI.Initialize(card);
            cardUI.OnCardPlayed += HandleCardUIPlayed;
            
            cardUIList.Add(cardUI);
            ArrangeCards();
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
                Destroy(cardUIToRemove.gameObject);
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
            
            float totalWidth = Mathf.Min((cardCount - 1) * cardSpacing, maxSpread);
            float startX = -totalWidth / 2f;
            
            for (int i = 0; i < cardCount; i++)
            {
                NewCardUI cardUI = cardUIList[i];
                RectTransform rectTransform = cardUI.GetComponent<RectTransform>();
                
                // Calculate position
                float t = cardCount > 1 ? (float)i / (cardCount - 1) : 0.5f;
                float x = startX + (t * totalWidth);
                
                // Calculate arc
                float normalizedPos = (2f * t) - 1f; // -1 to 1
                float y = -Mathf.Abs(normalizedPos) * arcHeight;
                
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