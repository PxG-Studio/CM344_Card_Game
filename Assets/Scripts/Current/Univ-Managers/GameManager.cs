using UnityEngine;
using CardGame.Core;

namespace CardGame.Managers
{
    /// <summary>
    /// Main game manager controlling the overall game flow
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        
        [Header("Game State")]
        [SerializeField] private GameState currentState = GameState.Menu;
        
        [Header("Turn Settings")]
        [SerializeField] private int maxHandSize = 7;
        [SerializeField] private int cardsDrawnPerTurn = 3;
        [SerializeField] private int startingHandSize = 5;
        
        public GameState CurrentState => currentState;
        public int MaxHandSize => maxHandSize;
        public int CardsDrawnPerTurn => cardsDrawnPerTurn;
        public int StartingHandSize => startingHandSize;
        
        // Events
        public System.Action<GameState> OnGameStateChanged;
        public System.Action OnTurnStarted;
        public System.Action OnTurnEnded;
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        
        private void Start()
        {
            Initialize();
        }
        
        private void Initialize()
        {
            Debug.Log("GameManager Initialized");
            ChangeState(GameState.Menu);
        }
        
        public void ChangeState(GameState newState)
        {
            if (currentState == newState)
                return;
                
            Debug.Log($"Game State: {currentState} -> {newState}");
            currentState = newState;
            OnGameStateChanged?.Invoke(newState);
            
            HandleStateChange(newState);
        }
        
        private void HandleStateChange(GameState state)
        {
            switch (state)
            {
                case GameState.Menu:
                    break;
                case GameState.Preparing:
                    PrepareGame();
                    break;
                case GameState.PlayerTurn:
                    StartPlayerTurn();
                    break;
                case GameState.EnemyTurn:
                    StartEnemyTurn();
                    break;
                case GameState.Victory:
                    HandleVictory();
                    break;
                case GameState.Defeat:
                    HandleDefeat();
                    break;
            }
        }
        
        public void StartGame()
        {
            ChangeState(GameState.Preparing);
        }
        
        private void PrepareGame()
        {
            Debug.Log("Preparing game...");
            
            // Reset managers for new game
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.ResetScores();
            }
            if (GameEndManager.Instance != null)
            {
                GameEndManager.Instance.Reset();
            }
            
            // Initialization will be handled by other managers
            Invoke(nameof(StartFirstTurn), 1f);
        }
        
        private void StartFirstTurn()
        {
            ChangeState(GameState.PlayerTurn);
        }
        
        private void StartPlayerTurn()
        {
            Debug.Log("Player Turn Started");
            OnTurnStarted?.Invoke();
        }
        
        public void EndPlayerTurn()
        {
            Debug.Log("Player Turn Ended");
            OnTurnEnded?.Invoke();
            ChangeState(GameState.EnemyTurn);
        }
        
        private void StartEnemyTurn()
        {
            Debug.Log("Enemy Turn Started");
            // Enemy AI will be handled separately
            Invoke(nameof(EndEnemyTurn), 2f); // Placeholder delay
        }
        
        private void EndEnemyTurn()
        {
            Debug.Log("Enemy Turn Ended");
            ChangeState(GameState.PlayerTurn);
        }
        
        private void HandleVictory()
        {
            Debug.Log("Victory!");
        }
        
        private void HandleDefeat()
        {
            Debug.Log("Defeat!");
        }
        
        public void CheckWinCondition()
        {
            // Will be implemented based on specific game rules
        }
    }
    
    public enum GameState
    {
        Menu,
        Preparing,
        PlayerTurn,
        EnemyTurn,
        Victory,
        Defeat,
        Paused
    }
}

