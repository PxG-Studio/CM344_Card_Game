using UnityEngine;

namespace CardGame.Entities
{
    /// <summary>
    /// Enemy entity with AI-controlled actions
    /// </summary>
    public class Enemy : Entity
    {
        [Header("Enemy AI")]
        [SerializeField] private EnemyIntent nextIntent = EnemyIntent.Attack;
        [SerializeField] private int intentValue = 0;
        
        public EnemyIntent NextIntent => nextIntent;
        public int IntentValue => intentValue;
        
        public System.Action<EnemyIntent, int> OnIntentChanged;
        
        protected override void Awake()
        {
            base.Awake();
            if (string.IsNullOrEmpty(entityName))
                entityName = "Enemy";
        }
        
        private void Start()
        {
            if (Managers.GameManager.Instance != null)
            {
                Managers.GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
            }
        }
        
        private void OnDestroy()
        {
            if (Managers.GameManager.Instance != null)
            {
                Managers.GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
            }
        }
        
        private void HandleGameStateChanged(Managers.GameState newState)
        {
            if (newState == Managers.GameState.EnemyTurn)
            {
                ApplyStatusEffects();
                ExecuteIntent();
            }
            else if (newState == Managers.GameState.PlayerTurn)
            {
                DetermineNextIntent();
            }
        }
        
        public void DetermineNextIntent()
        {
            // Simple AI: randomly choose an action
            int random = Random.Range(0, 100);
            
            if (random < 60)
            {
                nextIntent = EnemyIntent.Attack;
                intentValue = Random.Range(5, 15);
            }
            else if (random < 80)
            {
                nextIntent = EnemyIntent.Defend;
                intentValue = Random.Range(3, 8);
            }
            else
            {
                nextIntent = EnemyIntent.Special;
                intentValue = Random.Range(1, 5);
            }
            
            OnIntentChanged?.Invoke(nextIntent, intentValue);
            Debug.Log($"{entityName} intends to: {nextIntent} ({intentValue})");
        }
        
        public void ExecuteIntent()
        {
            if (isStunned)
            {
                Debug.Log($"{entityName} is stunned and cannot act!");
                return;
            }
            
            Player player = FindObjectOfType<Player>();
            if (player == null) return;
            
            switch (nextIntent)
            {
                case EnemyIntent.Attack:
                    player.TakeDamage(intentValue);
                    break;
                    
                case EnemyIntent.Defend:
                    AddShield(intentValue);
                    break;
                    
                case EnemyIntent.Special:
                    player.AddPoison(intentValue);
                    break;
            }
        }
        
        protected override void Die()
        {
            base.Die();
            // Check if all enemies are dead
            Enemy[] enemies = FindObjectsOfType<Enemy>();
            bool allDead = true;
            foreach (Enemy enemy in enemies)
            {
                if (enemy.IsAlive)
                {
                    allDead = false;
                    break;
                }
            }
            
            if (allDead && Managers.GameManager.Instance != null)
            {
                Managers.GameManager.Instance.ChangeState(Managers.GameState.Victory);
            }
        }
    }
    
    public enum EnemyIntent
    {
        Attack,
        Defend,
        Special,
        Unknown
    }
}

