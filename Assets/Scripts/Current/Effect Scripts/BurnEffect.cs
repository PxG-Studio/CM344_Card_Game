using CardGame.Core;
using UnityEngine;
namespace CardGame.Effects
{
    public class BurnEffect : ICardEffect
    {
        public void OnPlace(NewCard card, int boardIndex)
        {
            // Find the CardDropArea1 instance
            var dropArea = Object.FindObjectOfType<CardDropArea1>();
            if (dropArea == null) return;

            // Find adjacent cards (implement GetAdjacentCards in CardDropArea1)
            var adjacentCards = dropArea.GetAdjacentCards(boardIndex);
            if (adjacentCards.Count == 0) return;

            // Prompt player to select one (implement PromptPlayerToSelectCard in CardDropArea1)
            int selectedCardIndex = dropArea.PromptPlayerToSelectCard(adjacentCards);
            if (selectedCardIndex == -1) return;

            // Destroy the selected card (implement DestroyCardAt in CardDropArea1)
            dropArea.DestroyCardAt(selectedCardIndex);
        }
    }
}
