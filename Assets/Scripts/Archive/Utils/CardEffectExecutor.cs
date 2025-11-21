using UnityEngine;
using CardGame.Core;
using CardGame.Data;
using CardGame.Entities;

namespace CardGame.Utils
{
    /// <summary>
    /// Utility class for executing card effects
    /// </summary>
    public static class CardEffectExecutor
    {
        public static void ExecuteCard(Card card, Player player, Enemy target)
        {
            if (card == null || player == null)
                return;
                
            Debug.Log($"Executing card: {card.Data.cardName}");
            
            // Execute basic card type effects
            switch (card.Data.cardType)
            {
                case CardType.Attack:
                    if (target != null)
                        target.TakeDamage(card.CurrentAttackValue);
                    break;
                    
                case CardType.Defense:
                    player.AddShield(card.CurrentDefenseValue);
                    break;
                    
                case CardType.Heal:
                    player.Heal(card.CurrentHealValue);
                    break;
                    
                case CardType.Spell:
                    ExecuteSpellEffects(card, player, target);
                    break;
                    
                case CardType.Buff:
                    ExecuteBuffEffects(card, player);
                    break;
                    
                case CardType.Debuff:
                    if (target != null)
                        ExecuteDebuffEffects(card, target);
                    break;
            }
            
            // Execute additional effects
            if (card.Data.effects != null)
            {
                foreach (var effect in card.Data.effects)
                {
                    ExecuteEffect(effect, player, target);
                }
            }
        }
        
        private static void ExecuteEffect(CardEffect effect, Player player, Enemy target)
        {
            switch (effect.effectType)
            {
                case EffectType.Damage:
                    ApplyDamageEffect(effect, target);
                    break;
                    
                case EffectType.Heal:
                    player.Heal(effect.effectValue);
                    break;
                    
                case EffectType.Shield:
                    player.AddShield(effect.effectValue);
                    break;
                    
                case EffectType.DrawCard:
                    var deckManager = Object.FindObjectOfType<Managers.DeckManager>();
                    if (deckManager != null)
                        deckManager.DrawCards(effect.effectValue);
                    break;
                    
                case EffectType.Poison:
                    if (target != null)
                        target.AddPoison(effect.effectValue);
                    break;
                    
                case EffectType.Burn:
                    if (target != null)
                        target.AddBurn(effect.effectValue);
                    break;
                    
                case EffectType.Stun:
                    if (target != null)
                        target.Stun();
                    break;
            }
        }
        
        private static void ApplyDamageEffect(CardEffect effect, Enemy target)
        {
            if (target == null) return;
            
            switch (effect.effectTarget)
            {
                case EffectTarget.Enemy:
                    target.TakeDamage(effect.effectValue);
                    break;
                    
                case EffectTarget.AllEnemies:
                    Enemy[] enemies = Object.FindObjectsOfType<Enemy>();
                    foreach (var enemy in enemies)
                    {
                        if (enemy.IsAlive)
                            enemy.TakeDamage(effect.effectValue);
                    }
                    break;
            }
        }
        
        private static void ExecuteSpellEffects(Card card, Player player, Enemy target)
        {
            // Spells typically have their effects defined in the effects array
            // This is a placeholder for spell-specific logic
        }
        
        private static void ExecuteBuffEffects(Card card, Player player)
        {
            // Buff effects on player
        }
        
        private static void ExecuteDebuffEffects(Card card, Enemy target)
        {
            // Debuff effects on enemy
        }
    }
}

