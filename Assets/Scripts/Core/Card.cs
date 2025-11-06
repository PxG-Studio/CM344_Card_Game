using UnityEngine;
using CardGame.Data;

namespace CardGame.Core
{
    /// <summary>
    /// Runtime representation of a card instance
    /// </summary>
    public class Card
    {
        public CardData Data { get; private set; }
        public int InstanceID { get; private set; }
        
        // Runtime modifiable stats
        public int CurrentManaCost { get; set; }
        public int CurrentAttackValue { get; set; }
        public int CurrentDefenseValue { get; set; }
        public int CurrentHealValue { get; set; }
        
        public bool IsPlayable { get; set; }
        public bool IsExhausted { get; set; }
        
        private static int _nextInstanceID = 0;
        
        public Card(CardData data)
        {
            Data = data;
            InstanceID = _nextInstanceID++;
            
            // Initialize runtime stats from data
            CurrentManaCost = data.manaCost;
            CurrentAttackValue = data.attackValue;
            CurrentDefenseValue = data.defenseValue;
            CurrentHealValue = data.healValue;
            
            IsPlayable = true;
            IsExhausted = false;
        }
        
        public void ResetToBaseStats()
        {
            CurrentManaCost = Data.manaCost;
            CurrentAttackValue = Data.attackValue;
            CurrentDefenseValue = Data.defenseValue;
            CurrentHealValue = Data.healValue;
            IsExhausted = false;
        }
        
        public void ModifyStats(int manaCostDelta = 0, int attackDelta = 0, int defenseDelta = 0, int healDelta = 0)
        {
            CurrentManaCost = Mathf.Max(0, CurrentManaCost + manaCostDelta);
            CurrentAttackValue = Mathf.Max(0, CurrentAttackValue + attackDelta);
            CurrentDefenseValue = Mathf.Max(0, CurrentDefenseValue + defenseDelta);
            CurrentHealValue = Mathf.Max(0, CurrentHealValue + healDelta);
        }
    }
}

