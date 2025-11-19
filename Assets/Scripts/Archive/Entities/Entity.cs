using UnityEngine;

namespace CardGame.Entities
{
    /// <summary>
    /// Base class for all combat entities (Player, Enemy)
    /// </summary>
    public abstract class Entity : MonoBehaviour
    {
        [Header("Entity Stats")]
        [SerializeField] protected string entityName;
        [SerializeField] protected int maxHealth = 100;
        [SerializeField] protected int currentHealth;
        [SerializeField] protected int maxMana = 3;
        [SerializeField] protected int currentMana;
        
        [Header("Status Effects")]
        protected int shield = 0;
        protected int poison = 0;
        protected int burn = 0;
        protected bool isStunned = false;
        
        public string EntityName => entityName;
        public int MaxHealth => maxHealth;
        public int CurrentHealth => currentHealth;
        public int MaxMana => maxMana;
        public int CurrentMana => currentMana;
        public int Shield => shield;
        public bool IsAlive => currentHealth > 0;
        
        // Events
        public System.Action<int, int> OnHealthChanged;
        public System.Action<int, int> OnManaChanged;
        public System.Action<int> OnShieldChanged;
        public System.Action OnDeath;
        
        protected virtual void Awake()
        {
            currentHealth = maxHealth;
            currentMana = maxMana;
        }
        
        public virtual void TakeDamage(int damage)
        {
            if (damage <= 0) return;
            
            int actualDamage = damage;
            
            // Shield absorbs damage first
            if (shield > 0)
            {
                int shieldDamage = Mathf.Min(shield, damage);
                shield -= shieldDamage;
                actualDamage -= shieldDamage;
                OnShieldChanged?.Invoke(shield);
            }
            
            // Apply remaining damage to health
            if (actualDamage > 0)
            {
                currentHealth = Mathf.Max(0, currentHealth - actualDamage);
                OnHealthChanged?.Invoke(currentHealth, maxHealth);
                
                Debug.Log($"{entityName} took {actualDamage} damage. Health: {currentHealth}/{maxHealth}");
                
                if (currentHealth <= 0)
                {
                    Die();
                }
            }
        }
        
        public virtual void Heal(int amount)
        {
            if (amount <= 0) return;
            
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            
            Debug.Log($"{entityName} healed {amount}. Health: {currentHealth}/{maxHealth}");
        }
        
        public virtual void AddShield(int amount)
        {
            shield += amount;
            OnShieldChanged?.Invoke(shield);
            
            Debug.Log($"{entityName} gained {amount} shield. Total shield: {shield}");
        }
        
        public virtual void SpendMana(int amount)
        {
            currentMana = Mathf.Max(0, currentMana - amount);
            OnManaChanged?.Invoke(currentMana, maxMana);
        }
        
        public virtual void RestoreMana(int amount)
        {
            currentMana = Mathf.Min(maxMana, currentMana + amount);
            OnManaChanged?.Invoke(currentMana, maxMana);
        }
        
        public virtual void RefreshMana()
        {
            currentMana = maxMana;
            OnManaChanged?.Invoke(currentMana, maxMana);
        }
        
        public virtual void ResetShield()
        {
            shield = 0;
            OnShieldChanged?.Invoke(shield);
        }
        
        protected virtual void Die()
        {
            Debug.Log($"{entityName} has died!");
            OnDeath?.Invoke();
        }
        
        public virtual void ApplyStatusEffects()
        {
            // Apply poison damage
            if (poison > 0)
            {
                TakeDamage(poison);
                poison = Mathf.Max(0, poison - 1);
            }
            
            // Apply burn damage
            if (burn > 0)
            {
                TakeDamage(burn);
                burn--;
            }
            
            // Reset stun
            isStunned = false;
        }
        
        public void AddPoison(int amount)
        {
            poison += amount;
            Debug.Log($"{entityName} poisoned for {poison} damage per turn");
        }
        
        public void AddBurn(int stacks)
        {
            burn += stacks;
            Debug.Log($"{entityName} burned for {burn} damage");
        }
        
        public void Stun()
        {
            isStunned = true;
            Debug.Log($"{entityName} is stunned!");
        }
    }
}

