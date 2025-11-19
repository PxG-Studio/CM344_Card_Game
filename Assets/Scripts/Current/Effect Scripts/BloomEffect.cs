using CardGame.Core;
using UnityEngine;
namespace CardGame.Effects
{
    public class BloomEffect : ICardEffect
    {
        // card: the card being placed
        // boardIndex: index or slot position on the board
        public void OnPlace(NewCard card, int boardIndex)
        {
            // Find the CardDropArea1 instance
            var dropArea = Object.FindObjectOfType<CardDropArea1>();
            if (dropArea == null) return;

            // Find adjacent open slots (implement GetAdjacentOpenSlots in CardDropArea1)
            var openSlots = dropArea.GetAdjacentOpenSlots(boardIndex);
            if (openSlots.Count == 0) return;

            // Prompt player to select one (implement PromptPlayerToSelectSlot in CardDropArea1)
            int selectedSlot = dropArea.PromptPlayerToSelectSlot(openSlots);
            if (selectedSlot == -1) return;

            // Create and place Overgrowth card (implement CreateOvergrowthCard in CardDropArea1)
            NewCard overgrowthCard = dropArea.CreateOvergrowthCard();
            dropArea.PlaceCardInSlot(overgrowthCard, selectedSlot);

            // Lock the space and set Overgrowth properties
            overgrowthCard.IsPlayable = false;
            overgrowthCard.IsExhausted = true;
            // Optionally set flags for scoring/capture prevention in NewCardData or NewCard
        }
    }
}
