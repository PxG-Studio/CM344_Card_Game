using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using NewCardData;

namespace CardGame.Core
{
    /// <summary>
    /// Manages a collection of NewCard instances
    /// </summary>
    public class NewDeck
    {
        private List<NewCard> _cards = new List<NewCard>();
        private System.Random _random = new System.Random();
        
        public int Count => _cards.Count;
        public IReadOnlyList<NewCard> Cards => _cards.AsReadOnly();
        
        public void AddCard(NewCard card)
        {
            _cards.Add(card);
        }
        
        public void AddCards(IEnumerable<NewCard> cards)
        {
            _cards.AddRange(cards);
        }
        
        public void RemoveCard(NewCard card)
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
                NewCard temp = _cards[k];
                _cards[k] = _cards[n];
                _cards[n] = temp;
            }
        }
        
        public NewCard DrawCard()
        {
            if (_cards.Count == 0)
                return null;
                
            NewCard card = _cards[0];
            _cards.RemoveAt(0);
            return card;
        }
        
        public List<NewCard> DrawCards(int count)
        {
            List<NewCard> drawn = new List<NewCard>();
            for (int i = 0; i < count && _cards.Count > 0; i++)
            {
                drawn.Add(DrawCard());
            }
            return drawn;
        }
        
        public NewCard GetCardAt(int index)
        {
            if (index < 0 || index >= _cards.Count)
                return null;
            return _cards[index];
        }
        
        public bool Contains(NewCard card)
        {
            return _cards.Contains(card);
        }
    }
}