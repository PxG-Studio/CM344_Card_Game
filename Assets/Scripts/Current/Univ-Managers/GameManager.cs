using UnityEngine;
using System.Collections;
using System.Linq;
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
        public System.Action<CardDropArea1, NewCard> OnCardPlaced;
        
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
            
            // [CardFront] Reset statistics for new game (if not already reset by ResetGameState)
            if (CardDropArea1.GetCardsPlayed() > 0)
            {
                CardDropArea1.ResetGameStatistics();
            }
            
            // Reset managers for new game (if not already reset by ResetGameState)
            if (ScoreManager.Instance != null)
            {
                // Only reset if scores are not already zero (avoid duplicate reset during rematch)
                if (ScoreManager.Instance.PlayerScore > 0 || ScoreManager.Instance.OpponentScore > 0)
                {
                    ScoreManager.Instance.ResetScores();
                }
            }
            if (GameEndManager.Instance != null)
            {
                GameEndManager.Instance.Reset();
            }
            
            // Reset current game statistics (keep session stats)
            if (GameStatsTracker.Instance != null)
            {
                GameStatsTracker.Instance.ResetCurrentGameStats();
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
    }
    
    public void EndEnemyTurn()
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
        
        public void NotifyCardPlaced(CardDropArea1 tile, NewCard card)
        {
            OnCardPlaced?.Invoke(tile, card);
        }
        
        /// <summary>
        /// [CardFront] Resets game state for rematch without reloading scene
        /// </summary>
        public void ResetGameState()
        {
            Debug.Log("[GameManager] Resetting game state for rematch...");
            
            // Hide game end UI first (before resetting other systems)
            CardGame.UI.GameEndUI gameEndUI = FindObjectOfType<CardGame.UI.GameEndUI>();
            if (gameEndUI != null)
            {
                gameEndUI.HideGameEnd();
            }
            
            // Reset statistics tracker (current game stats only, keep session stats)
            if (GameStatsTracker.Instance != null)
            {
                GameStatsTracker.Instance.ResetCurrentGameStats();
            }
            else
            {
                Debug.LogWarning("[GameManager] GameStatsTracker.Instance is null. Statistics may not reset properly.");
            }
            
            // Reset statistics in CardDropArea1
            CardDropArea1.ResetGameStatistics();
            
            // Reset ScoreManager
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.ResetScores();
            }
            
            // Reset GameEndManager
            if (GameEndManager.Instance != null)
            {
                GameEndManager.Instance.Reset();
            }
            
            // Clear board - remove all cards from CardDropArea1 instances
            ClearBoard();
            
            // Reset deck managers
            NewDeckManager playerDeck = FindObjectOfType<NewDeckManager>();
            if (playerDeck != null)
            {
                playerDeck.InitializeDeck();
            }
            
            NewDeckManagerOpp opponentDeck = FindObjectOfType<NewDeckManagerOpp>();
            if (opponentDeck != null)
            {
                opponentDeck.InitializeDeck();
            }
            
            // Clear hands - need to remove hand UI cards
            ClearHands();
            
            // Return to preparing state (will trigger normal game flow including initial card draw)
            // Note: PrepareGame() will be called by ChangeState, which will handle final reset
            ChangeState(GameState.Preparing);
            
            // [CardFront] Trigger initial card draw after a short delay to ensure everything is reset
            // The PrepareGame() method will handle initial setup, but we also need to draw cards
            StartCoroutine(TriggerInitialCardDrawAfterReset());
            
            Debug.Log("[GameManager] Game state reset complete. Ready for rematch.");
        }
        
        /// <summary>
        /// Triggers initial card draw after reset (called after short delay)
        /// </summary>
        private System.Collections.IEnumerator TriggerInitialCardDrawAfterReset()
        {
            // Wait for a short delay to ensure all managers are reset
            yield return new UnityEngine.WaitForSeconds(0.3f);
            
            // Check if NewCardSystemTester exists and draw initial cards
            CardGame.Testing.NewCardSystemTester tester = FindObjectOfType<CardGame.Testing.NewCardSystemTester>();
            if (tester != null)
            {
                tester.DrawInitialCards();
            }
            
            // Also draw for opponent
            CardGame.Testing.NewCardSystemOpposition oppTester = FindObjectOfType<CardGame.Testing.NewCardSystemOpposition>();
            if (oppTester != null)
            {
                oppTester.DrawInitialCards();
            }
            
            Debug.Log("[GameManager] Initial cards drawn for rematch");
        }
        
        /// <summary>
        /// Clears all cards from the board (removes from CardDropArea1 instances)
        /// </summary>
        private void ClearBoard()
        {
            // Find all CardMover and CardMoverOpp GameObjects (these are the cards on the board)
            // Cards in hand are in the UI containers, not on the board
            CardMover[] allCardMovers = FindObjectsOfType<CardMover>();
            CardMoverOpp[] allCardMoverOpps = FindObjectsOfType<CardMoverOpp>();
            
            int removedCount = 0;
            
            // Remove all card movers (board cards)
            foreach (CardMover mover in allCardMovers)
            {
                if (mover != null && mover.gameObject != null)
                {
                    // Check if card is played (on board) vs in hand
                    // Cards in hand are children of hand containers
                    if (mover.gameObject.transform.parent == null || 
                        mover.gameObject.transform.parent.GetComponent<CardGame.UI.NewHandUI>() == null)
                    {
                        Destroy(mover.gameObject);
                        removedCount++;
                    }
                }
            }
            
            foreach (CardMoverOpp moverOpp in allCardMoverOpps)
            {
                if (moverOpp != null && moverOpp.gameObject != null)
                {
                    // Check if card is played (on board) vs in hand
                    if (moverOpp.gameObject.transform.parent == null || 
                        moverOpp.gameObject.transform.parent.GetComponent<CardGame.UI.NewHandOppUI>() == null)
                    {
                        Destroy(moverOpp.gameObject);
                        removedCount++;
                    }
                }
            }
            
            // Clear all CardDropArea1 occupying card references
            CardDropArea1[] allDropAreas = FindObjectsOfType<CardDropArea1>();
            foreach (CardDropArea1 dropArea in allDropAreas)
            {
                if (dropArea != null)
                {
                    // Clear the occupying card reference using reflection
                    var occupyingCardField = typeof(CardDropArea1).GetField("occupyingCard",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (occupyingCardField != null)
                    {
                        occupyingCardField.SetValue(dropArea, null);
                    }
                }
            }
            
            Debug.Log($"[GameManager] Cleared board - removed {removedCount} card(s) from board");
        }
        
        /// <summary>
        /// Clears all cards from hands (removes hand UI cards)
        /// </summary>
        private void ClearHands()
        {
            // Find hand UI managers and use their ClearHand methods
            CardGame.UI.NewHandUI playerHand = FindObjectOfType<CardGame.UI.NewHandUI>();
            if (playerHand != null)
            {
                playerHand.ClearHand();
            }
            
            CardGame.UI.NewHandOppUI opponentHand = FindObjectOfType<CardGame.UI.NewHandOppUI>();
            if (opponentHand != null)
            {
                opponentHand.ClearHand();
            }
            
            Debug.Log("[GameManager] Cleared hands");
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

