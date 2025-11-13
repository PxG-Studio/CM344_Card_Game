using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NewCardData 
{
    /// <summary>
    /// ScriptableObject representing a single card's data with directional stats
    /// </summary>
    [CreateAssetMenu(fileName = "New Directional Card", menuName = "Card Game/Directional Card Data", order = 1)]
    public class NewCardData : ScriptableObject
    {
        [Header("Card Information")]
        public string cardName;
        [TextArea(3, 5)]
        public string description;
        public Sprite artwork;

        [Header("Card Stats")]
        public int TopStat;
        public int RightStat;
        public int DownStat;
        public int LeftStat;
        public CardType cardType;

        [Header("Card Effects")]
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
        Flame,
        Wind,
        Earth,
        Lightning,
    }

    public enum EffectType
    {
        Cyclone,
        Burn,
        Bloom,
        VoltSwitch
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