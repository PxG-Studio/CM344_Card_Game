using CardGame.Managers;

namespace CardGame.Effects
{
    public interface ICardEffect
    {
        void OnPlace(Card card, BoardManager board);
    }
}
