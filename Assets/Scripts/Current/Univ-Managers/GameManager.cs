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
            // Debug logging removed for capture mechanics isolation
            ChangeState(GameState.Menu);
        }
        
        public void ChangeState(GameState newState)
        {
            if (currentState == newState)
                return;
                
            // Debug logging removed for capture mechanics isolation
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
            // Debug logging removed for capture mechanics isolation
            
            // [CardFront] Reset statistics for new game (if not already reset by ResetGameState)
            if (CardDropArea.GetCardsPlayed() > 0)
            {
                CardDropArea.ResetGameStatistics();
            }
            
            // Reset managers for new game (if not already reset by ResetGameState)
            if (ScoreManager.Instance != null)
            {
                // Only reset if scores are not already zero (avoid duplicate reset during rematch)
                if (ScoreManager.Instance.P1Score > 0 || ScoreManager.Instance.P2Score > 0)
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
            
            // CRITICAL: Reset scores again after a short delay to ensure they stay reset
            // This prevents RecalculateScores() from being called during initialization
            // and overwriting the reset (e.g., if GameEndManager checks board state)
            StartCoroutine(DelayedScoreReset());
        }
        
        /// <summary>
        /// Ensures scores remain reset after game initialization, in case RecalculateScores()
        /// was called during the initialization process. Checks multiple times with increasing delays.
        /// </summary>
        private System.Collections.IEnumerator DelayedScoreReset()
        {
            // Check and reset scores multiple times to catch any RecalculateScores() calls
            // Extended to 1.5s to match test wait times
            float[] checkDelays = { 0.1f, 0.5f, 1.0f, 1.5f };
            
            foreach (float delay in checkDelays)
            {
                yield return new WaitForSeconds(delay);
                
                if (ScoreManager.Instance != null)
                {
                    // Check if board is actually empty
                    CardDropArea[] allDropAreas = FindObjectsOfType<CardDropArea>();
                    bool boardIsEmpty = true;
                    int occupiedCount = 0;
                    foreach (CardDropArea area in allDropAreas)
                    {
                        if (area != null && area.IsOccupied)
                        {
                            boardIsEmpty = false;
                            occupiedCount++;
                        }
                    }
                    
                    // If board is empty but scores are non-zero, reset them
                    // This handles the case where RecalculateScores() was called on an empty board
                    if (boardIsEmpty && (ScoreManager.Instance.P1Score != 0 || ScoreManager.Instance.P2Score != 0))
                    {
                        // Debug logging removed for capture mechanics isolation
                        ScoreManager.Instance.ResetScores();
                    }
                    else if (!boardIsEmpty)
                    {
                        // Debug logging removed for capture mechanics isolation
                    }
                }
            }
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
                // Debug logging removed for capture mechanics isolation
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
            // Debug logging removed for capture mechanics isolation
            
            if (FateFlowController.Instance != null)
            {
                FateFlowController.Instance.SetFate(startingSide);
                // Debug logging removed for capture mechanics isolation
            }
            else
            {
                Debug.LogError("[GameManager] FateFlowController.Instance is null! Cannot set starting player.");
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
            
            // Debug logging removed for capture mechanics isolation
            
            // Now start the animation on the active GameObject
            if (activeSelf && coinTossUI.enabled)
            {
                coinTossUI.StartCoinTossAnimation();
                // Debug logging removed for capture mechanics isolation
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
                        // Debug logging removed for capture mechanics isolation
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
            // Debug logging removed for capture mechanics isolation
            OnTurnStarted?.Invoke();
        }
        
        public void EndPlayerTurn()
        {
            // Debug logging removed for capture mechanics isolation
            OnTurnEnded?.Invoke();
            ChangeState(GameState.EnemyTurn);
        }
        
    private void StartEnemyTurn()
    {
        // Debug logging removed for capture mechanics isolation
    }
    
    public void EndEnemyTurn()
        {
            // Debug logging removed for capture mechanics isolation
            ChangeState(GameState.PlayerTurn);
        }
        
        private void HandleVictory()
        {
            // Debug logging removed for capture mechanics isolation
        }
        
        private void HandleDefeat()
        {
            // Debug logging removed for capture mechanics isolation
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
        /// </summary>
        public void ResetGameState()
        {
            StartCoroutine(ResetGameStateCoroutine());
        }
        
        /// <summary>
        /// Coroutine to reset game state - ensures proper cleanup and timing
        /// </summary>
        private IEnumerator ResetGameStateCoroutine()
        {
            // Debug logging removed for capture mechanics isolation
            
            // Step 1: Hide game end UI first (before resetting other systems)
            // Debug logging removed for capture mechanics isolation
            CardGame.UI.GameEndUI gameEndUI = FindObjectOfType<CardGame.UI.GameEndUI>();
            if (gameEndUI != null)
            {
                gameEndUI.HideGameEnd();
            }
            
            // Step 2: Clear board - remove all cards from CardDropArea instances
            // This also resets all CardDropArea instances (clears occupying card references and turn tracking)
            // Debug logging removed for capture mechanics isolation
            ClearBoard();
            
            // CRITICAL: Wait a frame to ensure Destroy() calls complete in PlayMode
            // Destroy() doesn't destroy immediately - it destroys at end of frame
            yield return null;
            
            // Verify board is actually empty after destruction
            CardDropArea[] allDropAreas = FindObjectsOfType<CardDropArea>();
            int stillOccupied = 0;
            foreach (CardDropArea area in allDropAreas)
            {
                if (area != null && area.IsOccupied)
                {
                    stillOccupied++;
                    // Force clear if still occupied
                    area.ResetForNewGame();
                }
            }
            
            if (stillOccupied > 0)
            {
                Debug.LogWarning($"[REMATCH] {stillOccupied} areas still occupied after board clear. Force cleared.");
                yield return null; // Wait another frame
            }
            
            // Step 3: Clear hands (both UI and deck manager hand lists)
            // Debug logging removed for capture mechanics isolation
            ClearHands();
            yield return null; // Wait for hand clearing to complete
            
            // Step 4: Reinitialize decks (this will clear everything and rebuild from starting deck)
            // Debug logging removed for capture mechanics isolation
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
            yield return null; // Wait for deck initialization
            
            // Step 5: Reset scores immediately (before showing coin toss UI)
            // This ensures scores are reset right away, before PrepareGame() runs
            // Debug logging removed for capture mechanics isolation
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.ResetScores();
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
            
            // Reset statistics in CardDropArea
            CardDropArea.ResetGameStatistics();

            // Reset Battle Front Influence bar UI
            CardGame.UI.CardFrontlineUI frontlineUI = FindObjectOfType<CardGame.UI.CardFrontlineUI>();
            if (frontlineUI != null)
            {
                frontlineUI.ResetFrontline();
            }
            
            // Reset GameEndManager
            if (GameEndManager.Instance != null)
            {
                GameEndManager.Instance.Reset();
            }
            
            // Step 6: Show coin toss UI (same as initial game start)
            // Debug logging removed for capture mechanics isolation
            // Reset coin toss for rematch
            if (CoinTossManager.Instance != null)
            {
                CoinTossManager.Instance.ResetCoinToss();
            }
            
            CardGame.UI.CoinTossUI coinTossUI = FindObjectOfType<CardGame.UI.CoinTossUI>(true); // Search inactive objects too
            if (coinTossUI != null)
            {
                coinTossUI.Show();
            }
            yield return null; // Wait for UI to show
            
            // Step 7: Change to Preparing state - this will trigger PrepareGame() which handles:
            // - Resetting statistics (current game only, preserves session stats)
            // - Resetting ScoreManager (again, as safeguard)
            // - Resetting GameEndManager
            // - Resetting Battle Front Influence bar
            // - Resetting coin toss
            // - Performing coin toss and starting game (including card drawing)
            // This reuses the exact same flow as StartGame() for consistency
            // Debug logging removed for capture mechanics isolation
            ChangeState(GameState.Preparing);
            yield return null; // Allow state change to process
            
            // [CardFront] Trigger initial card draw after a short delay to ensure everything is reset
            // The PrepareGame() method will handle initial setup, but we also need to draw cards
            StartCoroutine(TriggerInitialCardDrawAfterReset());
            
            // Debug logging removed for capture mechanics isolation
        }
        
        /// <summary>
        /// Triggers initial card draw after reset (called after short delay)
        /// </summary>
        private System.Collections.IEnumerator TriggerInitialCardDrawAfterReset()
        {
            // Wait for a short delay to ensure all managers are reset
            yield return new UnityEngine.WaitForSeconds(0.3f);
            
            // Check if NewCardSystemP1Tester exists and draw initial cards
            CardGame.Testing.NewCardSystemP1Tester tester = FindObjectOfType<CardGame.Testing.NewCardSystemP1Tester>();
            if (tester != null)
            {
                tester.DrawInitialCards();
            }
            
            // Also draw for P2
            CardGame.Testing.NewCardSystemP2 oppTester = FindObjectOfType<CardGame.Testing.NewCardSystemP2>();
            if (oppTester != null)
            {
                oppTester.DrawInitialCards();
            }
            
            // Debug logging removed for capture mechanics isolation
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
                Debug.LogWarning("[GameManager] Deck tester components not found. Decks may not be initialized correctly.");
            }
            else
            {
                // Debug logging removed for capture mechanics isolation
            }
        }
        
        /// <summary>
        /// Clears all cards from the board (removes from CardDropArea instances)
        /// </summary>
        private void ClearBoard()
        {
            CardDropArea[] allDropAreas = FindObjectsOfType<CardDropArea>();
            HashSet<GameObject> cardsToDestroy = new HashSet<GameObject>();
            int removedCount = 0;
            
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
            
            // CRITICAL: First, reset all CardDropArea instances BEFORE destroying cards
            // This clears occupying card references immediately so IsOccupied returns false
            // This must happen BEFORE destroying cards to prevent stale references
            // Debug logging removed for capture mechanics isolation
            foreach (CardDropArea dropArea in allDropAreas)
            {
                if (dropArea != null)
                {
                    // Stop all coroutines first
                    dropArea.StopAllCoroutines();
                    // Reset the drop area (clears occupyingCard reference, resets tile color, etc.)
                    dropArea.ResetForNewGame();
                }
            }
            
            // Now destroy all collected cards
            // Use Destroy() in PlayMode (proper cleanup) and DestroyImmediate in EditMode
            // Debug logging removed for capture mechanics isolation
            foreach (GameObject cardToDestroy in cardsToDestroy)
            {
                if (cardToDestroy != null)
                {
                    #if UNITY_EDITOR
                    if (!Application.isPlaying)
                    {
                        // EditMode: use DestroyImmediate
                        DestroyImmediate(cardToDestroy);
                    }
                    else
                    {
                        // PlayMode: use Destroy() for proper cleanup
                        Destroy(cardToDestroy);
                    }
                    #else
                    // Build: use Destroy() for proper cleanup
                    Destroy(cardToDestroy);
                    #endif
                    removedCount++;
                }
            }
            
            // Also find and destroy any remaining cards on the board (catch any that weren't tracked)
            // This is a safety net for cards that might not have been in occupyingCard references
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
                        if (!Application.isPlaying)
                        {
                            DestroyImmediate(mover.gameObject);
                        }
                        else
                        {
                            Destroy(mover.gameObject);
                        }
                        #else
                        Destroy(mover.gameObject);
                        #endif
                        removedCount++;
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
                        if (!Application.isPlaying)
                        {
                            DestroyImmediate(mover.gameObject);
                        }
                        else
                        {
                            Destroy(mover.gameObject);
                        }
                        #else
                        Destroy(mover.gameObject);
                        #endif
                        removedCount++;
                    }
                }
            }
            
            // Force update all tile colors to ensure they're white after reset
            // This ensures any tiles that might have retained colors are reset
            CardDropArea.UpdateAllTileColors();
            
            // CRITICAL: Verify board is actually empty after reset
            // This helps catch any cards that weren't properly destroyed
            int remainingOccupied = 0;
            foreach (CardDropArea dropArea in allDropAreas)
            {
                if (dropArea != null && dropArea.IsOccupied)
                {
                    remainingOccupied++;
                    GameObject remainingCard = dropArea.GetOccupyingCard();
                    if (remainingCard != null)
                    {
                        Debug.LogWarning($"[BOARD RESET] CardDropArea at {dropArea.transform.position} still has occupying card: {remainingCard.name}. Force clearing...");
                        // Force clear the reference if card still exists
                        dropArea.ResetForNewGame();
                    }
                }
            }
            
            if (remainingOccupied > 0)
            {
                Debug.LogWarning($"[BOARD RESET] Warning: {remainingOccupied} CardDropArea instances still report as occupied after reset. This may indicate cards weren't properly destroyed.");
            }
            else
            {
                // Debug logging removed for capture mechanics isolation
            }
        }
        
        /// <summary>
        /// Clears all cards from hands (removes hand UI cards)
        /// </summary>
        private void ClearHands()
        {
            // Find hand UI managers and use their ClearHand methods
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
            
            // Debug logging removed for capture mechanics isolation
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
            
            // Debug logging removed for capture mechanics isolation
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

