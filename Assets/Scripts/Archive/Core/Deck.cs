using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CardGame.Data;

namespace CardGame.Core
{
    /// <summary>
    /// Manages a collection of cards (deck, hand, discard pile)
    /// </summary>
    public class Deck
    {
        private List<Card> _cards = new List<Card>();
        private System.Random _random = new System.Random();
        
        public int Count => _cards.Count;
        public IReadOnlyList<Card> Cards => _cards.AsReadOnly();
        
        public void AddCard(Card card)
        {
            _cards.Add(card);
        }
        
        public void AddCards(IEnumerable<Card> cards)
        {
            _cards.AddRange(cards);
        }
        
        public void RemoveCard(Card card)
        {
            _cards.Remove(card);
        }
        
        public void Clear()
        {
            _cards.Clear();
        }
        
        public void Shuffle()
        {
            int n = _cards.Count;
            while (n > 1)
            {
                n--;
                int k = _random.Next(n + 1);
                Card temp = _cards[k];
                _cards[k] = _cards[n];
                _cards[n] = temp;
            }
        }
        
        public Card DrawCard()
        {
            if (_cards.Count == 0)
                return null;
                
            Card card = _cards[0];
            _cards.RemoveAt(0);
            return card;
        }
        
        public List<Card> DrawCards(int count)
        {
            List<Card> drawn = new List<Card>();
            for (int i = 0; i < count && _cards.Count > 0; i++)
            {
                drawn.Add(DrawCard());
            }
            return drawn;
        }
        
        public Card GetCardAt(int index)
        {
            if (index < 0 || index >= _cards.Count)
                return null;
            return _cards[index];
        }
        
        public bool Contains(Card card)
        {
            return _cards.Contains(card);
        }
    }
}

