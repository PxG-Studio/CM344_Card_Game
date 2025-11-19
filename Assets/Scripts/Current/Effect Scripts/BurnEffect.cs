using CardGame.Managers;

namespace CardGame.Effects
{
    public class BurnEffect : ICardEffect
    {
        public void OnPlace(Card card, BoardManager board)
        {
            // After captures, let controller select adjacent card
            var adjacentCards = board.GetAdjacentCards(card.Position);
            if (adjacentCards.Count == 0) return;

            var selectedCard = board.PromptPlayerToSelectCard(adjacentCards);
            if (selectedCard != null)
            {
                board.DestroyCard(selectedCard);
            }
        }
    }
}
