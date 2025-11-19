using CardGame.Managers;

namespace CardGame.Effects
{
    public class BloomEffect : ICardEffect
    {
        public void OnPlace(Card card, BoardManager board)
        {
            // After captures, let controller select adjacent open space
            var openSpaces = board.GetAdjacentOpenSpaces(card.Position);
            if (openSpaces.Count == 0) return;

            var selectedSpace = board.PromptPlayerToSelectSpace(openSpaces);
            if (selectedSpace != null)
            {
                var overgrowthCard = board.SpawnCard("Overgrowth", selectedSpace);
                overgrowthCard.Locked = true;
                overgrowthCard.CanScore = false;
                overgrowthCard.CanBeCaptured = false;
            }
        }
    }
}
