using UnityEngine;
using System.Collections;
using System.Linq;
using CardGame.Core;
using CardGame.UI;

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

        /// <summary>
        /// Check if Player 2 (Opponent) can interact based on current turn state.
        /// Uses FateFlowController to determine if it's Player 2's turn.
        /// </summary>
        public bool CanPlayer2Interact()
        {
            if (FateFlowController.Instance != null)
            {
                return FateFlowController.Instance.CanAct(FateSide.Opponent);
            }
            // Fallback: check if game state allows interaction
            return currentState == GameState.PlayerTurn || currentState == GameState.Preparing;
        }
        
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
            
            // Reset coin toss for new game (rematch)
            if (CoinTossManager.Instance != null)
            {
                CoinTossManager.Instance.ResetCoinToss();
            }
            
            // Reset FateFlowController to default (will be set by coin toss)
            if (FateFlowController.Instance != null)
            {
                FateFlowController.Instance.SetFate(FateSide.Player); // Default, will be overridden
            }
            
            // Perform coin toss and wait for result before starting game
            StartCoroutine(PerformCoinTossAndStartGame());
        }
        
        /// <summary>
        /// Performs coin toss and starts the game after result is determined.
        /// </summary>
        private System.Collections.IEnumerator PerformCoinTossAndStartGame()
        {
            // Wait for coin toss manager to be ready
            CoinTossManager coinTossManager = CoinTossManager.Instance;
            if (coinTossManager == null)
            {
                Debug.LogWarning("[GameManager] CoinTossManager not found. Creating...");
                GameObject coinTossObj = new GameObject("CoinTossManager");
                coinTossObj.AddComponent<CoinTossManager>();
                coinTossManager = CoinTossManager.Instance;
                yield return new WaitForEndOfFrame();
            }
            
            // Wait for CoinTossUI to be ready
            // Search inactive objects too (panel starts inactive)
            CoinTossUI coinTossUI = FindObjectOfType<CoinTossUI>(true);
            int retryCount = 0;
            int maxRetries = 10; // Wait up to 5 seconds (10 * 0.5s)
            
            while (coinTossUI == null && retryCount < maxRetries)
            {
                Debug.LogWarning($"[GameManager] CoinTossUI not found (attempt {retryCount + 1}/{maxRetries}). Waiting for HUDSetup to create it...");
                yield return new WaitForSeconds(0.5f);
                coinTossUI = FindObjectOfType<CoinTossUI>(true); // Search inactive objects too
                retryCount++;
            }
            
            if (coinTossUI == null)
            {
                Debug.LogError("[GameManager] CoinTossUI still not found after waiting. HUDSetup may not have created it.");
            }
            
            // Trigger coin toss animation through UI
            if (coinTossUI != null)
            {
                // Start the coin toss from GameManager (always active) to ensure coroutine can start
                StartCoroutine(StartCoinTossFromManager(coinTossUI));
                Debug.Log("[GameManager] Coin toss animation started via CoinTossUI.");
            }
            else
            {
                // Fallback: Perform coin toss without UI
                Debug.LogWarning("[GameManager] CoinTossUI not found. Performing coin toss without animation.");
                FateSide fallbackStartingSide = coinTossManager.PerformCoinToss();
                if (FateFlowController.Instance != null)
                {
                    FateFlowController.Instance.SetFate(fallbackStartingSide);
                }
                StartFirstTurn();
                yield break;
            }
            
            // Wait for coin toss to complete
            float waitTime = 0f;
            float maxWaitTime = 10f; // Maximum wait time for coin toss
            
            while (!coinTossManager.IsComplete && waitTime < maxWaitTime)
            {
                yield return new WaitForSeconds(0.1f);
                waitTime += 0.1f;
            }
            
            if (!coinTossManager.IsComplete)
            {
                Debug.LogWarning("[GameManager] Coin toss did not complete in time. Using default starting player.");
            }
            
            // Get coin toss result and set starting player
            FateSide startingSide = coinTossManager.GetStartingPlayer();
            if (FateFlowController.Instance != null)
            {
                FateFlowController.Instance.SetFate(startingSide);
                Debug.Log($"[GameManager] Coin toss result: {startingSide}. Starting player set in FateFlowController.");
            }
            
            // Wait for coin toss UI animation to complete (additional buffer)
            yield return new WaitForSeconds(1f);
        }
        
        /// <summary>
        /// Starts the coin toss from GameManager (always active) to ensure coroutines can run.
        /// </summary>
        private System.Collections.IEnumerator StartCoinTossFromManager(CoinTossUI coinTossUI)
        {
            if (coinTossUI == null)
            {
                Debug.LogError("[GameManager] CoinTossUI is null! Cannot start coin toss.");
                yield break;
            }
            
            // StartCoinToss() activates the GameObject, but activation isn't immediate
            // We need to wait for Unity to process the activation before starting coroutines
            coinTossUI.StartCoinToss();
            
            // Wait for end of frame to ensure GameObject activation is processed
            yield return new WaitForEndOfFrame();
            
            // Additional wait to ensure GameObject is fully active
            yield return null;
            
            // Verify GameObject is active before starting animation
            if (coinTossUI == null || coinTossUI.gameObject == null)
            {
                Debug.LogError("[GameManager] CoinTossUI or its GameObject became null after activation!");
                yield break;
            }
            
            GameObject coinTossObj = coinTossUI.gameObject;
            bool activeSelf = coinTossObj.activeSelf;
            bool activeInHierarchy = coinTossObj.activeInHierarchy;
            
            Debug.Log($"[GameManager] CoinTossUI GameObject state after activation - activeSelf: {activeSelf}, activeInHierarchy: {activeInHierarchy}, enabled: {coinTossUI.enabled}");
            
            // Now start the animation on the active GameObject
            if (activeSelf && coinTossUI.enabled)
            {
                coinTossUI.StartCoinTossAnimation();
                Debug.Log("[GameManager] Coin toss animation started successfully.");
            }
            else
            {
                // If still not active, try activating again and wait
                if (!activeSelf)
                {
                    Debug.LogWarning("[GameManager] CoinTossUI GameObject is still inactive. Activating again and waiting...");
                    coinTossObj.SetActive(true);
                    yield return new WaitForEndOfFrame();
                    yield return null;
                    
                    if (coinTossObj.activeSelf && coinTossUI.enabled)
                    {
                        coinTossUI.StartCoinTossAnimation();
                        Debug.Log("[GameManager] Coin toss animation started after second activation.");
                    }
                    else
                    {
                        Debug.LogError($"[GameManager] Failed to activate CoinTossUI GameObject! activeSelf: {coinTossObj.activeSelf}, enabled: {coinTossUI.enabled}");
                    }
                }
                else
                {
                    Debug.LogError($"[GameManager] CoinTossUI GameObject activation failed! activeSelf: {activeSelf}, activeInHierarchy: {activeInHierarchy}, enabled: {coinTossUI.enabled}");
                }
            }
        }
        
        private void StartFirstTurn()
        {
            // Use FateFlowController to determine starting player
            if (FateFlowController.Instance != null)
            {
                FateSide startingSide = FateFlowController.Instance.CurrentFate;
                if (startingSide == FateSide.Player)
                {
                    ChangeState(GameState.PlayerTurn);
                }
                else
                {
                    ChangeState(GameState.EnemyTurn);
                }
            }
            else
            {
                // Fallback: default to Player 1
                ChangeState(GameState.PlayerTurn);
            }
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
            
            // Reset coin toss for rematch
            if (CoinTossManager.Instance != null)
            {
                CoinTossManager.Instance.ResetCoinToss();
            }
            
            // Show coin toss UI again for rematch
            CardGame.UI.CoinTossUI coinTossUI = FindObjectOfType<CardGame.UI.CoinTossUI>(true); // Search inactive objects too
            if (coinTossUI != null)
            {
                coinTossUI.Show();
            }
            
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

