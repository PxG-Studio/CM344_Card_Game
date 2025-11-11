using System.Collections.Generic;
using UnityEngine;
using CardGame.Core;
using NewCardData;

namespace CardGame.Managers
{
    /// <summary>
    /// Manages the player's deck, hand, and discard pile for NewCard system
    /// </summary>
    public class NewDeckManager : MonoBehaviour
    {
        [Header("Deck Configuration")]
        [SerializeField] private List<NewCardData.NewCardData> startingDeck = new List<NewCardData.NewCardData>();
        
        private NewDeck drawPile = new NewDeck();
        private NewDeck hand = new NewDeck();
        private NewDeck discardPile = new NewDeck();
        
        public IReadOnlyList<NewCard> Hand => hand.Cards;
        public int DrawPileCount => drawPile.Count;
        public int DiscardPileCount => discardPile.Count;
        
        // Events
        public System.Action<NewCard> OnCardDrawn;
        public System.Action<NewCard> OnCardPlayed;
        public System.Action<NewCard> OnCardDiscarded;
        public System.Action OnDeckShuffled;
        
        public void InitializeDeck()
        {
            // Clear all piles
            drawPile.Clear();
            hand.Clear();
            discardPile.Clear();
            
            // Create card instances from NewCardData
            foreach (NewCardData.NewCardData cardData in startingDeck)
            {
                if (cardData != null)
                {
                    NewCard card = new NewCard(cardData);
                    drawPile.AddCard(card);
                }
            }
            
            // Shuffle the deck
            ShuffleDeck();
            
            Debug.Log($"NewDeck initialized: {drawPile.Count} cards in draw pile");
        }
        
        public void ShuffleDeck()
        {
            drawPile.Shuffle();
            OnDeckShuffled?.Invoke();
            Debug.Log("NewDeck shuffled");
        }
        
        public void DrawCard()
        {
            // Check if we need to reshuffle discard pile
            if (drawPile.Count == 0 && discardPile.Count > 0)
            {
                ReshuffleDiscardPile();
            }
            
            if (drawPile.Count == 0)
            {
                Debug.LogWarning("No cards to draw!");
                return;
            }
            
            // Check hand size limit (you may want to make this configurable)
            if (hand.Count >= 5) // Adjust max hand size as needed
            {
                Debug.LogWarning("Hand is full!");
                return;
            }
            
            NewCard card = drawPile.DrawCard();
            hand.AddCard(card);
            OnCardDrawn?.Invoke(card);
            
            Debug.Log($"Drew card: {card.Data.cardName}");
        }
        
        public void DrawCards(int count)
        {
            for (int i = 0; i < count; i++)
            {
                DrawCard();
            }
        }
        
        public void PlayCard(NewCard card)
        {
            if (!hand.Contains(card))
            {
                Debug.LogWarning("Card not in hand!");
                return;
            }
            
            hand.RemoveCard(card);
            OnCardPlayed?.Invoke(card);
            
            // Move to discard pile
            DiscardCard(card);
            
            Debug.Log($"Played card: {card.Data.cardName}");
        }
        
        public void DiscardCard(NewCard card)
        {
            if (hand.Contains(card))
            {
                hand.RemoveCard(card);
            }
            
            discardPile.AddCard(card);
            OnCardDiscarded?.Invoke(card);
        }
        
        public void DiscardHand()
        {
            while (hand.Count > 0)
            {
                NewCard card = hand.Cards[0];
                DiscardCard(card);
            }
        }
        
        private void ReshuffleDiscardPile()
        {
            Debug.Log("Reshuffling discard pile into draw pile");
            
            foreach (NewCard card in discardPile.Cards)
            {
                card.ResetToBaseStats();
            }
            
            drawPile.AddCards(discardPile.Cards);
            discardPile.Clear();
            ShuffleDeck();
        }
        
        public NewCard GetCardInHand(int index)
        {
            return hand.GetCardAt(index);
        }
    }
}