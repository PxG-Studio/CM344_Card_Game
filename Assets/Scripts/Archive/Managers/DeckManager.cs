using System.Collections.Generic;
using UnityEngine;
using CardGame.Core;
using CardGame.Data;

namespace CardGame.Managers
{
    /// <summary>
    /// Manages the player's deck, hand, and discard pile
    /// </summary>
    public class DeckManager : MonoBehaviour
    {
        [Header("Deck Configuration")]
        [SerializeField] private List<CardData> startingDeck = new List<CardData>();
        
        private Deck drawPile = new Deck();
        private Deck hand = new Deck();
        private Deck discardPile = new Deck();
        
        public IReadOnlyList<Card> Hand => hand.Cards;
        public int DrawPileCount => drawPile.Count;
        public int DiscardPileCount => discardPile.Count;
        
        // Events
        public System.Action<Card> OnCardDrawn;
        public System.Action<Card> OnCardPlayed;
        public System.Action<Card> OnCardDiscarded;
        public System.Action OnDeckShuffled;
        
        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
            }
        }
        
        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
            }
        }
        
        private void HandleGameStateChanged(GameState newState)
        {
            if (newState == GameState.Preparing)
            {
                InitializeDeck();
            }
            else if (newState == GameState.PlayerTurn)
            {
                DrawCards(GameManager.Instance.CardsDrawnPerTurn);
            }
        }
        
        public void InitializeDeck()
        {
            // Clear all piles
            drawPile.Clear();
            hand.Clear();
            discardPile.Clear();
            
            // Create card instances from CardData
            foreach (CardData cardData in startingDeck)
            {
                Card card = new Card(cardData);
                drawPile.AddCard(card);
            }
            
            // Shuffle the deck
            ShuffleDeck();
            
            // Draw starting hand
            DrawCards(GameManager.Instance.StartingHandSize);
            
            Debug.Log($"Deck initialized: {drawPile.Count} cards in draw pile, {hand.Count} in hand");
        }
        
        public void ShuffleDeck()
        {
            drawPile.Shuffle();
            OnDeckShuffled?.Invoke();
            Debug.Log("Deck shuffled");
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
            
            // Check hand size limit
            if (hand.Count >= GameManager.Instance.MaxHandSize)
            {
                Debug.LogWarning("Hand is full!");
                return;
            }
            
            Card card = drawPile.DrawCard();
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
        
        public void PlayCard(Card card)
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
        
        public void DiscardCard(Card card)
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
                Card card = hand.Cards[0];
                DiscardCard(card);
            }
        }
        
        private void ReshuffleDiscardPile()
        {
            Debug.Log("Reshuffling discard pile into draw pile");
            
            foreach (Card card in discardPile.Cards)
            {
                card.ResetToBaseStats();
            }
            
            drawPile.AddCards(discardPile.Cards);
            discardPile.Clear();
            ShuffleDeck();
        }
        
        public Card GetCardInHand(int index)
        {
            return hand.GetCardAt(index);
        }
    }
}

