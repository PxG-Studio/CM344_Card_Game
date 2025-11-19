using System.Collections.Generic;
using UnityEngine;
using CardGame.Core;
using CardGame.Managers;

namespace CardGame.UI
{
    /// <summary>
    /// Manages the visual representation of the player's hand
    /// </summary>
    public class HandUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CardUI cardPrefab;
        [SerializeField] private Transform cardContainer;
        
        [Header("Layout Settings")]
        [SerializeField] private float cardSpacing = 120f;
        [SerializeField] private float maxSpread = 800f;
        [SerializeField] private float arcHeight = 50f;
        [SerializeField] private float rotationAngle = 5f;
        
        private List<CardUI> cardUIList = new List<CardUI>();
        private DeckManager deckManager;
        
        private void Start()
        {
            deckManager = FindObjectOfType<DeckManager>();
            
            if (deckManager != null)
            {
                deckManager.OnCardDrawn += HandleCardDrawn;
                deckManager.OnCardPlayed += HandleCardPlayed;
                deckManager.OnCardDiscarded += HandleCardDiscarded;
            }
        }
        
        private void OnDestroy()
        {
            if (deckManager != null)
            {
                deckManager.OnCardDrawn -= HandleCardDrawn;
                deckManager.OnCardPlayed -= HandleCardPlayed;
                deckManager.OnCardDiscarded -= HandleCardDiscarded;
            }
        }
        
        private void HandleCardDrawn(Card card)
        {
            AddCardToHand(card);
        }
        
        private void HandleCardPlayed(Card card)
        {
            RemoveCardFromHand(card);
        }
        
        private void HandleCardDiscarded(Card card)
        {
            RemoveCardFromHand(card);
        }
        
        public void AddCardToHand(Card card)
        {
            if (cardPrefab == null || cardContainer == null)
            {
                Debug.LogError("CardPrefab or CardContainer not assigned!");
                return;
            }
            
            CardUI cardUI = Instantiate(cardPrefab, cardContainer);
            cardUI.Initialize(card);
            cardUI.OnCardPlayed += HandleCardUIPlayed;
            
            cardUIList.Add(cardUI);
            ArrangeCards();
        }
        
        private void HandleCardUIPlayed(CardUI cardUI)
        {
            if (deckManager != null)
            {
                // Check if player has enough mana
                Entities.Player player = FindObjectOfType<Entities.Player>();
                if (player != null && player.CurrentMana >= cardUI.Card.CurrentManaCost)
                {
                    player.SpendMana(cardUI.Card.CurrentManaCost);
                    deckManager.PlayCard(cardUI.Card);
                    
                    // Apply card effects
                    ApplyCardEffects(cardUI.Card);
                }
                else
                {
                    Debug.Log("Not enough mana!");
                }
            }
        }
        
        private void ApplyCardEffects(Card card)
        {
            Entities.Player player = FindObjectOfType<Entities.Player>();
            Entities.Enemy enemy = FindObjectOfType<Entities.Enemy>();
            
            if (player == null) return;
            
            // Apply basic effects
            if (card.CurrentAttackValue > 0 && enemy != null)
            {
                enemy.TakeDamage(card.CurrentAttackValue);
            }
            
            if (card.CurrentDefenseValue > 0)
            {
                player.AddShield(card.CurrentDefenseValue);
            }
            
            if (card.CurrentHealValue > 0)
            {
                player.Heal(card.CurrentHealValue);
            }
            
            // Apply additional effects from CardData
            if (card.Data.effects != null)
            {
                foreach (var effect in card.Data.effects)
                {
                    ApplyEffect(effect, player, enemy);
                }
            }
        }
        
        private void ApplyEffect(Data.CardEffect effect, Entities.Player player, Entities.Enemy enemy)
        {
            switch (effect.effectType)
            {
                case Data.EffectType.Damage:
                    if (enemy != null) enemy.TakeDamage(effect.effectValue);
                    break;
                    
                case Data.EffectType.Heal:
                    player.Heal(effect.effectValue);
                    break;
                    
                case Data.EffectType.Shield:
                    player.AddShield(effect.effectValue);
                    break;
                    
                case Data.EffectType.Poison:
                    if (enemy != null) enemy.AddPoison(effect.effectValue);
                    break;
                    
                case Data.EffectType.Burn:
                    if (enemy != null) enemy.AddBurn(effect.effectValue);
                    break;
                    
                case Data.EffectType.Stun:
                    if (enemy != null) enemy.Stun();
                    break;
                    
                case Data.EffectType.DrawCard:
                    if (deckManager != null) deckManager.DrawCards(effect.effectValue);
                    break;
            }
        }
        
        public void RemoveCardFromHand(Card card)
        {
            CardUI cardUIToRemove = cardUIList.Find(c => c.Card.InstanceID == card.InstanceID);
            
            if (cardUIToRemove != null)
            {
                cardUIList.Remove(cardUIToRemove);
                Destroy(cardUIToRemove.gameObject);
                ArrangeCards();
            }
        }
        
        public void ClearHand()
        {
            foreach (CardUI cardUI in cardUIList)
            {
                Destroy(cardUI.gameObject);
            }
            cardUIList.Clear();
        }
        
        private void ArrangeCards()
        {
            int cardCount = cardUIList.Count;
            if (cardCount == 0) return;
            
            float totalWidth = Mathf.Min((cardCount - 1) * cardSpacing, maxSpread);
            float startX = -totalWidth / 2f;
            
            for (int i = 0; i < cardCount; i++)
            {
                CardUI cardUI = cardUIList[i];
                RectTransform rectTransform = cardUI.GetComponent<RectTransform>();
                
                // Calculate position
                float t = cardCount > 1 ? (float)i / (cardCount - 1) : 0.5f;
                float x = startX + (t * totalWidth);
                
                // Calculate arc
                float normalizedPos = (2f * t) - 1f; // -1 to 1
                float y = -Mathf.Abs(normalizedPos) * arcHeight;
                
                // Calculate rotation
                float rotation = normalizedPos * rotationAngle;
                
                // Apply transform
                rectTransform.anchoredPosition = new Vector2(x, y);
                rectTransform.localRotation = Quaternion.Euler(0, 0, rotation);
                
                // Set sibling index for proper overlap
                cardUI.transform.SetSiblingIndex(i);
            }
        }
    }
}

