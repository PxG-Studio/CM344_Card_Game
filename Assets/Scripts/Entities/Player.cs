using UnityEngine;

namespace CardGame.Entities
{
    /// <summary>
    /// Player entity with card-based actions
    /// </summary>
    public class Player : Entity
    {
        protected override void Awake()
        {
            base.Awake();
            entityName = "Player";
        }
        
        private void Start()
        {
            if (Managers.GameManager.Instance != null)
            {
                Managers.GameManager.Instance.OnTurnStarted += OnTurnStart;
            }
        }
        
        private void OnDestroy()
        {
            if (Managers.GameManager.Instance != null)
            {
                Managers.GameManager.Instance.OnTurnStarted -= OnTurnStart;
            }
        }
        
        private void OnTurnStart()
        {
            if (Managers.GameManager.Instance.CurrentState == Managers.GameState.PlayerTurn)
            {
                RefreshMana();
                ApplyStatusEffects();
            }
        }
        
        protected override void Die()
        {
            base.Die();
            if (Managers.GameManager.Instance != null)
            {
                Managers.GameManager.Instance.ChangeState(Managers.GameState.Defeat);
            }
        }
    }
}

