using UnityEngine;
using System.Collections;
using System.Collections.Generic;
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
        /// Check if P2 can interact based on current turn state.
        /// Uses FateFlowController to determine if it's P2's turn.
        /// </summary>
        public bool CanPlayer2Interact()
        {
            if (FateFlowController.Instance != null)
            {
                return FateFlowController.Instance.CanAct(FateSide.P2);
            }
            // Fallback: check if game state allows interaction
            return currentState == GameState.PlayerTurn || currentState == GameState.Preparing;
        }
        
        // Events
        public System.Action<GameState> OnGameStateChanged;
        public System.Action OnTurnStarted;
        public System.Action OnTurnEnded;
        public System.Action<CardDropArea, NewCard> OnCardPlaced;
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                // Use DestroyImmediate in editor to avoid play mode exit issues
                #if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    DestroyImmediate(gameObject);
                }
                else
                {
                    Destroy(gameObject);
                }
                #else
                Destroy(gameObject);
                #endif
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
            ChangeState(GameState.Menu);
        }
        
        public void ChangeState(GameState newState)
        {
            if (currentState == newState)
                return;
                
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
                case GameState.Draw:
                    // For now, treat Draw similarly to Victory in terms of flow:
                    // the dedicated GameEndUI will display the correct "IT'S A TIE!"
                    // messaging based on the isTie flag passed from GameEndManager.
                    HandleVictory();
                    break;
            }
        }
        
        public void StartGame()
        {
            ChangeState(GameState.Preparing);
        }
        
        private void PrepareGame()
        {
            // Reset statistics for new game
            CardDropArea.ResetGameStatistics();
            
            // Reset managers for new game
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.ResetScores();
            }
            
            if (GameEndManager.Instance != null)
            {
                GameEndManager.Instance.Reset();
            }
            
            // Reset current game statistics (keep session stats - wins/losses/ties are preserved)
            if (GameStatsTracker.Instance != null)
            {
                GameStatsTracker.Instance.ResetCurrentGameStats();
            }
            
            // Reset Battle Front Influence bar UI
            CardGame.UI.CardFrontlineUI frontlineUI = FindObjectOfType<CardGame.UI.CardFrontlineUI>();
            if (frontlineUI != null)
            {
                frontlineUI.ResetFrontline();
            }
            
            // Reset coin toss for new game
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
            // This will trigger card drawing through the normal game flow
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
                yield return new WaitForSeconds(0.5f);
                coinTossUI = FindObjectOfType<CoinTossUI>(true); // Search inactive objects too
                retryCount++;
            }
            
            if (coinTossUI == null)
            {
            }
            
            // Trigger coin toss animation through UI
            if (coinTossUI != null)
            {
                // Start the coin toss from GameManager (always active) to ensure coroutine can start
                StartCoroutine(StartCoinTossFromManager(coinTossUI));
            }
            else
            {
                // Fallback: Perform coin toss without UI
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
                // In practice, the UI coin toss may still be running; this is a normal fallback.
            }
            
            // Get coin toss result and set starting player (uses default if toss not yet performed)
            FateSide startingSide = coinTossManager.GetStartingPlayer();
            
            if (FateFlowController.Instance != null)
            {
                FateFlowController.Instance.SetFate(startingSide);
            }
            else
            {
            }
            
            // Wait for coin toss UI animation to complete (additional buffer)
            yield return new WaitForSeconds(1f);
            
            // Start the first turn once coin toss has resolved and fate has been set.
            StartFirstTurn();
        }
        
        /// <summary>
        /// Starts the coin toss from GameManager (always active) to ensure coroutines can run.
        /// </summary>
        private System.Collections.IEnumerator StartCoinTossFromManager(CoinTossUI coinTossUI)
        {
            if (coinTossUI == null)
            {
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
                yield break;
            }
            
            GameObject coinTossObj = coinTossUI.gameObject;
            bool activeSelf = coinTossObj.activeSelf;
            bool activeInHierarchy = coinTossObj.activeInHierarchy;
            
            
            // Now start the animation on the active GameObject
            if (activeSelf && coinTossUI.enabled)
            {
                coinTossUI.StartCoinTossAnimation();
            }
            else
            {
                // If still not active, try activating again and wait
                if (!activeSelf)
                {
                    // This is a normal recovery path in some initialization orders; log at info level
                    coinTossObj.SetActive(true);
                    yield return new WaitForEndOfFrame();
                    yield return null;
                    
                    if (coinTossObj.activeSelf && coinTossUI.enabled)
                    {
                        coinTossUI.StartCoinTossAnimation();
                    }
                    else
                    {
                    }
                }
                else
                {
                }
            }
        }
        
        private void StartFirstTurn()
        {
            // Use FateFlowController to determine starting player
            if (FateFlowController.Instance != null)
            {
                FateSide startingSide = FateFlowController.Instance.CurrentFate;
                
                // Ensure decks are initialized and opening hands drawn at the very start
                TryInitializeDecksAndDrawOpeningHands();
                
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
            OnTurnStarted?.Invoke();
        }
        
        public void EndPlayerTurn()
        {
            OnTurnEnded?.Invoke();
            ChangeState(GameState.EnemyTurn);
        }
        
    private void StartEnemyTurn()
    {
    }
    
    public void EndEnemyTurn()
        {
            ChangeState(GameState.PlayerTurn);
        }
        
        private void HandleVictory()
        {
        }
        
        private void HandleDefeat()
        {
        }
        
        public void CheckWinCondition()
        {
            // Will be implemented based on specific game rules
        }
        
        public void NotifyCardPlaced(CardDropArea tile, NewCard card)
        {
            OnCardPlaced?.Invoke(tile, card);
        }
        
        /// <summary>
        /// [CardFront] Resets game state for rematch without reloading scene
        /// Reuses the same initialization flow as StartGame() to ensure consistency
        /// </summary>
        public void ResetGameState()
        {
            Debug.Log("[REMATCH] ResetGameState called - reusing initial game setup flow");
            
            // Step 1: Hide game end UI first (before resetting other systems)
            Debug.Log("[REMATCH] Step 1: Hiding game end UI");
            CardGame.UI.GameEndUI gameEndUI = FindObjectOfType<CardGame.UI.GameEndUI>();
            if (gameEndUI != null)
            {
                gameEndUI.HideGameEnd();
            }
            
            // Step 2: Clear board - remove all cards from CardDropArea instances
            Debug.Log("[REMATCH] Step 2: Clearing board (destroying all cards and resetting tiles)");
            ClearBoard();
            
            // Step 3: Clear hands (both UI and deck manager hand lists)
            Debug.Log("[REMATCH] Step 3: Clearing player hands (UI and deck manager lists)");
            ClearHands();
            
            // Step 4: Reinitialize decks (this will clear everything and rebuild from starting deck)
            Debug.Log("[REMATCH] Step 4: Reinitializing deck managers");
            NewDeckManagerP1 playerDeck = FindObjectOfType<NewDeckManagerP1>();
            if (playerDeck != null)
            {
                playerDeck.InitializeDeck();
            }
            
            NewDeckManagerP2 opponentDeck = FindObjectOfType<NewDeckManagerP2>();
            if (opponentDeck != null)
            {
                opponentDeck.InitializeDeck();
            }
            
            // Step 5: Show coin toss UI (same as initial game start)
            Debug.Log("[REMATCH] Step 5: Showing coin toss UI for new game");
            CardGame.UI.CoinTossUI coinTossUI = FindObjectOfType<CardGame.UI.CoinTossUI>(true); // Search inactive objects too
            if (coinTossUI != null)
            {
                coinTossUI.Show();
            }
            
            // Step 6: Change to Preparing state - this will trigger PrepareGame() which handles:
            // - Resetting statistics (current game only, preserves session stats)
            // - Resetting ScoreManager
            // - Resetting GameEndManager
            // - Resetting Battle Front Influence bar
            // - Resetting coin toss
            // - Performing coin toss and starting game (including card drawing)
            // This reuses the exact same flow as StartGame() for consistency
            Debug.Log("[REMATCH] Step 6: Changing to Preparing state (will trigger normal game initialization flow)");
            ChangeState(GameState.Preparing);
            
            Debug.Log("[REMATCH] ResetGameState completed - game will initialize through PrepareGame() just like a fresh start");
        }
        

        /// <summary>
        /// Verifies that deck systems exist at the start of a game session.
        /// 
        /// NOTE:
        /// NewCardSystemP1Tester/NewCardSystemP2 already handle InitializeDeck()
        /// and DrawInitialCards() as part of their own startup flow (after the
        /// coin toss). Re-initializing decks here would clear logical hands while
        /// the hand UI still shows cards, causing mismatches like
        /// "card is not in hand" and preventing turn advancement.
        /// 
        /// To avoid that desync this method now only logs the presence (or
        /// absence) of the tester components and does not modify deck state.
        /// </summary>
        private void TryInitializeDecksAndDrawOpeningHands()
        {
            CardGame.Testing.NewCardSystemP1Tester tester = FindObjectOfType<CardGame.Testing.NewCardSystemP1Tester>();
            CardGame.Testing.NewCardSystemP2 oppTester = FindObjectOfType<CardGame.Testing.NewCardSystemP2>();
            
            if (tester == null || oppTester == null)
            {
            }
            else
            {
            }
        }
        
        /// <summary>
        /// Clears all cards from the board (removes from CardDropArea instances)
        /// </summary>
        private void ClearBoard()
        {
            CardDropArea[] allDropAreas = FindObjectsOfType<CardDropArea>();
            HashSet<GameObject> cardsToDestroy = new HashSet<GameObject>();
            
            // First pass: Collect all cards that are occupying CardDropArea instances
            foreach (CardDropArea dropArea in allDropAreas)
            {
                if (dropArea != null && dropArea.IsOccupied)
                {
                    GameObject occupyingCard = dropArea.GetOccupyingCard();
                    if (occupyingCard != null)
                    {
                        cardsToDestroy.Add(occupyingCard);
                    }
                }
            }
            
            // Second pass: Also find cards on board by position (z ≈ 0) that aren't in hands
            // This catches any cards that might not be properly tracked in occupyingCard
            CardMoverP1[] allCardMovers = FindObjectsOfType<CardMoverP1>();
            CardMoverP2[] allCardMoverP2s = FindObjectsOfType<CardMoverP2>();
            
            foreach (CardMoverP1 mover in allCardMovers)
            {
                if (mover != null && mover.gameObject != null)
                {
                    // Check if card is on board (z ≈ 0) and not in hand
                    bool isOnBoard = Mathf.Abs(mover.transform.position.z) < 1f;
                    bool isInHand = mover.gameObject.transform.parent != null && 
                                   mover.gameObject.transform.parent.GetComponent<CardGame.UI.NewHandP1UI>() != null;
                    
                    if (isOnBoard && !isInHand)
                    {
                        cardsToDestroy.Add(mover.gameObject);
                    }
                }
            }
            
            foreach (CardMoverP2 moverP2 in allCardMoverP2s)
            {
                if (moverP2 != null && moverP2.gameObject != null)
                {
                    // Check if card is on board (z ≈ 0) and not in hand
                    bool isOnBoard = Mathf.Abs(moverP2.transform.position.z) < 1f;
                    bool isInHand = moverP2.gameObject.transform.parent != null && 
                                   moverP2.gameObject.transform.parent.GetComponent<CardGame.UI.NewHandP2UI>() != null;
                    
                    if (isOnBoard && !isInHand)
                    {
                        cardsToDestroy.Add(moverP2.gameObject);
                    }
                }
            }
            
            // First, stop all coroutines in CardDropArea instances to prevent ripple effects from continuing
            foreach (CardDropArea dropArea in allDropAreas)
            {
                if (dropArea != null)
                {
                    dropArea.StopAllCoroutines();
                }
            }
            
            // Destroy all collected cards (use DestroyImmediate to ensure they're gone immediately)
            foreach (GameObject cardToDestroy in cardsToDestroy)
            {
                if (cardToDestroy != null)
                {
                    #if UNITY_EDITOR
                    if (!Application.isPlaying)
                    {
                        DestroyImmediate(cardToDestroy);
                    }
                    else
                    {
                        DestroyImmediate(cardToDestroy);
                    }
                    #else
                    DestroyImmediate(cardToDestroy);
                    #endif
                }
            }
            
            // Also find and destroy any remaining cards on the board (catch any that weren't tracked)
            CardMoverP1[] remainingP1Cards = FindObjectsOfType<CardMoverP1>();
            foreach (CardMoverP1 mover in remainingP1Cards)
            {
                if (mover != null && mover.gameObject != null)
                {
                    bool isOnBoard = Mathf.Abs(mover.transform.position.z) < 1f;
                    bool isInHand = mover.gameObject.transform.parent != null && 
                                   mover.gameObject.transform.parent.GetComponent<CardGame.UI.NewHandP1UI>() != null;
                    
                    if (isOnBoard && !isInHand)
                    {
                        #if UNITY_EDITOR
                        DestroyImmediate(mover.gameObject);
                        #else
                        DestroyImmediate(mover.gameObject);
                        #endif
                    }
                }
            }
            
            CardMoverP2[] remainingP2Cards = FindObjectsOfType<CardMoverP2>();
            foreach (CardMoverP2 mover in remainingP2Cards)
            {
                if (mover != null && mover.gameObject != null)
                {
                    bool isOnBoard = Mathf.Abs(mover.transform.position.z) < 1f;
                    bool isInHand = mover.gameObject.transform.parent != null && 
                                   mover.gameObject.transform.parent.GetComponent<CardGame.UI.NewHandP2UI>() != null;
                    
                    if (isOnBoard && !isInHand)
                    {
                        #if UNITY_EDITOR
                        DestroyImmediate(mover.gameObject);
                        #else
                        DestroyImmediate(mover.gameObject);
                        #endif
                    }
                }
            }
            
            // Finally, reset all CardDropArea instances to clear occupying card references
            // This also resets tile colors to white and stops all coroutines
            foreach (CardDropArea dropArea in allDropAreas)
            {
                if (dropArea != null)
                {
                    dropArea.ResetForNewGame();
                }
            }
            
            // Force update all tile colors to ensure they're white after reset
            // This ensures any tiles that might have retained colors are reset
            CardDropArea.UpdateAllTileColors();
        }
        
        /// <summary>
        /// Clears all cards from hands (removes hand UI cards)
        /// </summary>
        private void ClearHands()
        {
            // First, clear deck manager hand lists (this ensures data is cleared)
            NewDeckManagerP1 playerDeck = FindObjectOfType<NewDeckManagerP1>();
            if (playerDeck != null)
            {
                playerDeck.DiscardHand(); // Move all hand cards to discard
            }
            
            NewDeckManagerP2 opponentDeck = FindObjectOfType<NewDeckManagerP2>();
            if (opponentDeck != null)
            {
                opponentDeck.DiscardHand(); // Move all hand cards to discard
            }
            
            // Then, clear hand UI visual elements
            CardGame.UI.NewHandP1UI playerHand = FindObjectOfType<CardGame.UI.NewHandP1UI>();
            if (playerHand != null)
            {
                playerHand.ClearHand();
            }
            
            CardGame.UI.NewHandP2UI opponentHand = FindObjectOfType<CardGame.UI.NewHandP2UI>();
            if (opponentHand != null)
            {
                opponentHand.ClearHand();
            }
        }
        
        /// <summary>
        /// Resets all CardDropArea instances for a new game
        /// </summary>
        private void ResetAllCardDropAreas()
        {
            CardDropArea[] allDropAreas = FindObjectsOfType<CardDropArea>();
            int resetCount = 0;
            
            foreach (CardDropArea dropArea in allDropAreas)
            {
                if (dropArea != null)
                {
                    dropArea.ResetForNewGame();
                    resetCount++;
                }
            }
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
        Draw,
        Paused
    }
}

