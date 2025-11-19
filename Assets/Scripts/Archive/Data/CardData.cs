using UnityEngine;

namespace CardGame.Data
{
    /// <summary>
    /// ScriptableObject representing a single card's data
    /// </summary>
    [CreateAssetMenu(fileName = "New Card", menuName = "Card Game/Card Data")]
    public class CardData : ScriptableObject
    {
        [Header("Card Information")]
        public string cardName;
        [TextArea(3, 5)]
        public string description;
        public Sprite artwork;
        
        [Header("Card Stats")]
        public int manaCost;
        public CardType cardType;
        public CardRarity rarity;
        
        [Header("Card Effects")]
        public int attackValue;
        public int defenseValue;
        public int healValue;
        public CardEffect[] effects;
        
        [Header("Visual")]
        public Color cardColor = Color.white;
    }
    
    [System.Serializable]
    public class CardEffect
    {
        public EffectType effectType;
        public int effectValue;
        public EffectTarget effectTarget;
    }
    
    public enum CardType
    {
        Attack,
        Defense,
        Spell,
        Heal,
        Buff,
        Debuff
    }
    
    public enum CardRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }
    
    public enum EffectType
    {
        Damage,
        Heal,
        Shield,
        DrawCard,
        DiscardCard,
        Stun,
        Poison,
        Burn,
        Freeze
    }
    
    public enum EffectTarget
    {
        Enemy,
        Self,
        AllEnemies,
        AllAllies,
        Random
    }
}

