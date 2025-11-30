using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CardGame.Entities;

namespace CardGame.UI
{
    /// <summary>
    /// Displays entity health, mana, and status information
    /// </summary>
    public class EntityUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Entity targetEntity;
        
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private Slider healthBar;
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private Slider manaBar;
        [SerializeField] private TextMeshProUGUI manaText;
        [SerializeField] private TextMeshProUGUI shieldText;
        [SerializeField] private GameObject shieldIcon;
        
        [Header("Enemy Intent (Enemy Only)")]
        [SerializeField] private GameObject intentPanel;
        [SerializeField] private Image intentIcon;
        [SerializeField] private TextMeshProUGUI intentValueText;
        [SerializeField] private Sprite attackIntentSprite;
        [SerializeField] private Sprite defendIntentSprite;
        [SerializeField] private Sprite specialIntentSprite;
        
        private void Start()
        {
            if (targetEntity == null)
                targetEntity = GetComponentInParent<Entity>();
                
            if (targetEntity != null)
            {
                SubscribeToEvents();
                InitializeUI();
            }
        }
        
        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }
        
        private void SubscribeToEvents()
        {
            targetEntity.OnHealthChanged += UpdateHealth;
            targetEntity.OnManaChanged += UpdateMana;
            targetEntity.OnShieldChanged += UpdateShield;
            
            // Subscribe to enemy-specific events
            if (targetEntity is Enemy enemy)
            {
                enemy.OnIntentChanged += UpdateIntent;
            }
        }
        
        private void UnsubscribeFromEvents()
        {
            if (targetEntity != null)
            {
                targetEntity.OnHealthChanged -= UpdateHealth;
                targetEntity.OnManaChanged -= UpdateMana;
                targetEntity.OnShieldChanged -= UpdateShield;
                
                if (targetEntity is Enemy enemy)
                {
                    enemy.OnIntentChanged -= UpdateIntent;
                }
            }
        }
        
        private void InitializeUI()
        {
            if (nameText != null)
                nameText.text = targetEntity.EntityName;
                
            UpdateHealth(targetEntity.CurrentHealth, targetEntity.MaxHealth);
            UpdateMana(targetEntity.CurrentMana, targetEntity.MaxMana);
            UpdateShield(targetEntity.Shield);
            
            // Hide intent panel for player
            if (intentPanel != null)
                intentPanel.SetActive(targetEntity is Enemy);
        }
        
        private void UpdateHealth(int current, int max)
        {
            if (healthBar != null)
            {
                healthBar.maxValue = max;
                healthBar.value = current;
            }
            
            if (healthText != null)
                healthText.text = $"{current}/{max}";
        }
        
        private void UpdateMana(int current, int max)
        {
            if (manaBar != null)
            {
                manaBar.maxValue = max;
                manaBar.value = current;
            }
            
            if (manaText != null)
                manaText.text = $"{current}/{max}";
        }
        
        private void UpdateShield(int shield)
        {
            if (shieldText != null)
                shieldText.text = shield.ToString();
                
            if (shieldIcon != null)
                shieldIcon.SetActive(shield > 0);
        }
        
        private void UpdateIntent(EnemyIntent intent, int value)
        {
            if (intentPanel == null) return;
            
            intentPanel.SetActive(true);
            
            if (intentValueText != null)
                intentValueText.text = value.ToString();
                
            if (intentIcon != null)
            {
                switch (intent)
                {
                    case EnemyIntent.Attack:
                        intentIcon.sprite = attackIntentSprite;
                        break;
                    case EnemyIntent.Defend:
                        intentIcon.sprite = defendIntentSprite;
                        break;
                    case EnemyIntent.Special:
                        intentIcon.sprite = specialIntentSprite;
                        break;
                }
            }
        }
    }
}

