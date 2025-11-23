using System.Collections;
using UnityEngine;
using CardGame.UI;

namespace CardGame.Managers
{
    /// <summary>
    /// Handles end-game detection and logic
    /// </summary>
    public class GameEndManager : MonoBehaviour
    {
        public static GameEndManager Instance { get; private set; }
        
        [Header("Settings")]
        [SerializeField] private float delayBeforeGameEnd = 0.5f; // Delay after chains complete before ending game
        [SerializeField] private float maxWaitTimeForChains = 10f; // Maximum time to wait for chains to complete
        
        private bool isGameEnding = false;
        private bool areChainsInProgress = false;
        [SerializeField] private CardGame.UI.GameEndUI gameEndUI;
        
        // References to deck managers (auto-found if not assigned)
        private NewDeckManager playerDeckManager;
        private NewDeckManagerOpp opponentDeckManager;
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
        }
        
        private void Start()
        {
            // Auto-find deck managers if not assigned
            if (playerDeckManager == null)
            {
                playerDeckManager = FindObjectOfType<NewDeckManager>();
            }
            if (opponentDeckManager == null)
            {
                opponentDeckManager = FindObjectOfType<NewDeckManagerOpp>();
            }
            
            if (gameEndUI == null)
            {
                gameEndUI = FindObjectOfType<CardGame.UI.GameEndUI>();
                if (gameEndUI == null)
                {
                    Debug.LogWarning("[GameEndManager] GameEndUI not found in Start(). Will search again when game ends. HUDSetup should create it dynamically.");
                }
                else
                {
                    Debug.Log("[GameEndManager] GameEndUI found and cached in Start().");
                }
            }
            else
            {
                Debug.Log("[GameEndManager] GameEndUI already assigned.");
            }
        }
        
        /// <summary>
        /// [CardFront] Checks if game should end (when all cards have been played - both hands empty and all 10 cards on board)
        /// </summary>
        public void CheckGameEnd()
        {
            if (isGameEnding)
            {
                Debug.Log("[GameEndManager] CheckGameEnd called but game is already ending. Ignoring duplicate call.");
                return;
            }
            
            // [CardFront] Check if all cards have been played
            // Game ends when:
            // 1. Both players' hands are empty (no more cards to play)
            // 2. All 10 cards have been placed on the board (5 player + 5 opponent)
            // Note: Cards are moved to discard pile when played, so IsDeckEmpty() is not reliable
            
            bool playerHandEmpty = false;
            bool opponentHandEmpty = false;
            int totalCardsPlayed = CardDropArea1.GetCardsPlayed();
            int playerHandCount = 0;
            int opponentHandCount = 0;
            
            if (playerDeckManager != null)
            {
                playerHandEmpty = playerDeckManager.IsHandEmpty();
                // [CardFront] Get actual hand count for detailed diagnostics
                if (playerDeckManager.Hand != null)
                {
                    playerHandCount = playerDeckManager.Hand.Count;
                }
                Debug.Log($"[GameEndManager] Player hand check - IsHandEmpty: {playerHandEmpty}, Hand.Count: {playerHandCount}, DeckManager: {(playerDeckManager != null ? "Found" : "NULL")}");
            }
            else
            {
                Debug.LogWarning("[GameEndManager] PlayerDeckManager is NULL! Cannot check player hand.");
            }
            
            if (opponentDeckManager != null)
            {
                opponentHandEmpty = opponentDeckManager.IsHandEmpty();
                // [CardFront] Get actual hand count for detailed diagnostics
                if (opponentDeckManager.Hand != null)
                {
                    opponentHandCount = opponentDeckManager.Hand.Count;
                }
                Debug.Log($"[GameEndManager] Opponent hand check - IsHandEmpty: {opponentHandEmpty}, Hand.Count: {opponentHandCount}, DeckManager: {(opponentDeckManager != null ? "Found" : "NULL")}");
            }
            else
            {
                Debug.LogWarning("[GameEndManager] OpponentDeckManager is NULL! Cannot check opponent hand.");
            }
            
            // Game ends when both hands are empty AND all 10 cards are on the board
            bool allCardsPlayed = (totalCardsPlayed >= 10) && playerHandEmpty && opponentHandEmpty;
            
            Debug.Log($"[GameEndManager] ===== GAME END CHECK =====");
            Debug.Log($"[GameEndManager] Cards played: {totalCardsPlayed}/10");
            Debug.Log($"[GameEndManager] Player hand empty: {playerHandEmpty} (hand.Count: {playerHandCount})");
            Debug.Log($"[GameEndManager] Opponent hand empty: {opponentHandEmpty} (hand.Count: {opponentHandCount})");
            Debug.Log($"[GameEndManager] All cards played condition: {allCardsPlayed}");
            Debug.Log($"[GameEndManager] ==========================");
            
            if (allCardsPlayed)
            {
                Debug.Log("[GameEndManager] ✓✓✓ ALL CARDS HAVE BEEN PLAYED! ✓✓✓");
                Debug.Log("[GameEndManager] Both players have no cards left and all 10 cards are on the board. Ending game...");
                Debug.Log($"[GameEndManager] Current gameEndUI reference: {(gameEndUI != null ? "Found" : "Null - will search when showing UI")}");
                isGameEnding = true;
                
                // Start coroutine to wait for chains to complete, then end game
                StartCoroutine(WaitForChainsAndEndGame());
            }
            else
            {
                // Detailed logging for debugging
                if (totalCardsPlayed < 10)
                {
                    Debug.Log($"[GameEndManager] ❌ Game continues - Waiting for all cards to be played. Cards played: {totalCardsPlayed}/10 (need 10)");
                }
                else if (!playerHandEmpty || !opponentHandEmpty)
                {
                    Debug.Log($"[GameEndManager] ❌ Game continues - Waiting for hands to empty.");
                    Debug.Log($"[GameEndManager]   - Player hand empty: {playerHandEmpty} (hand.Count: {playerHandCount})");
                    Debug.Log($"[GameEndManager]   - Opponent hand empty: {opponentHandEmpty} (hand.Count: {opponentHandCount})");
                }
            }
        }
        
        /// <summary>
        /// Notifies that chain captures are in progress
        /// </summary>
        public void SetChainsInProgress(bool inProgress)
        {
            areChainsInProgress = inProgress;
        }
        
        /// <summary>
        /// Waits for all chain captures to complete, then evaluates winner
        /// </summary>
        private IEnumerator WaitForChainsAndEndGame()
        {
            float elapsedTime = 0f;
            
            // Wait for chains to complete (with timeout)
            while (areChainsInProgress && elapsedTime < maxWaitTimeForChains)
            {
                yield return new WaitForSeconds(0.1f);
                elapsedTime += 0.1f;
            }
            
            // Additional delay to ensure all animations complete
            yield return new WaitForSeconds(delayBeforeGameEnd);
            
            // Recalculate final scores (in case any captures happened after initial check)
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.RecalculateScores();
            }
            
            // Log deck/hand status for debugging
            LogDeckStatus();
            
            // Evaluate winner (collects statistics internally)
            EvaluateWinner();
        }
        
        /// <summary>
        /// Evaluates the winner based on final scores and changes game state
        /// </summary>
        private void EvaluateWinner()
        {
            if (ScoreManager.Instance == null)
            {
                Debug.LogError("GameEndManager: ScoreManager not found!");
                return;
            }
            
            int playerScore = ScoreManager.Instance.PlayerScore;
            int opponentScore = ScoreManager.Instance.OpponentScore;
            bool isTie = playerScore == opponentScore;
            int scoreMargin = ScoreManager.Instance.GetScoreMargin();
            
            // Get statistics
            int cardsPlayed = CardDropArea1.GetCardsPlayed();
            int capturesMade = CardDropArea1.GetCapturesMade();
            int longestChain = CardDropArea1.GetLongestChain();
            
            bool playerWon = playerScore > opponentScore;
            
            // Record statistics in GameStatsTracker
            if (GameStatsTracker.Instance != null)
            {
                GameStatsTracker.Instance.RecordGameResult(playerWon, isTie, cardsPlayed, capturesMade, longestChain, scoreMargin);
            }
            
            Debug.Log($"[GameEndManager] Final Scores - Player: {playerScore}, Opponent: {opponentScore}, Margin: {scoreMargin}");
            Debug.Log($"[GameEndManager] Statistics - Cards Played: {cardsPlayed}, Captures Made: {capturesMade}, Longest Chain: {longestChain}");
            
            if (GameManager.Instance == null)
            {
                Debug.LogError("GameEndManager: GameManager not found!");
                return;
            }
            
            // Determine winner based on scores
            if (playerScore > opponentScore)
            {
                Debug.Log("Player wins!");
                GameManager.Instance.ChangeState(GameState.Victory);
                ShowWinnerUI(true, false, cardsPlayed, capturesMade, longestChain, scoreMargin);
            }
            else if (opponentScore > playerScore)
            {
                Debug.Log("Opponent wins!");
                GameManager.Instance.ChangeState(GameState.Defeat);
                ShowWinnerUI(false, false, cardsPlayed, capturesMade, longestChain, scoreMargin);
            }
            else
            {
                // Tie - you may want to handle this differently
                Debug.Log("It's a tie!");
                // Default to player victory for ties, or you could add a Tie state
                GameManager.Instance.ChangeState(GameState.Victory);
                ShowWinnerUI(true, true, cardsPlayed, capturesMade, longestChain, scoreMargin);
            }
        }
        
        private void ShowWinnerUI(bool playerWon, bool isTie, int cardsPlayed, int capturesMade, int longestChain, int scoreMargin)
        {
            // [CardFront] Re-check for GameEndUI if not found (it might be created dynamically by HUDSetup)
            if (gameEndUI == null)
            {
                gameEndUI = FindObjectOfType<CardGame.UI.GameEndUI>();
                if (gameEndUI == null)
                {
                    Debug.LogError("GameEndManager: GameEndUI not found. Winner screen cannot be displayed. Please ensure HUDSetup has created the GameEndUI panel.");
                    return;
                }
                else
                {
                    Debug.Log("GameEndManager: Found GameEndUI after initial search. Proceeding with game end display.");
                }
            }
            
            if (gameEndUI != null)
            {
                Debug.Log($"[GameEndManager] Showing game end UI - Player Won: {playerWon}, Is Tie: {isTie}, Cards Played: {cardsPlayed}, Captures: {capturesMade}, Longest Chain: {longestChain}, Score Margin: {scoreMargin}");
                gameEndUI.ShowGameEnd(playerWon, isTie, cardsPlayed, capturesMade, longestChain, scoreMargin);
            }
            else
            {
                Debug.LogError("GameEndManager: GameEndUI is still null after search. Cannot display winner screen.");
            }
        }
        
        /// <summary>
        /// Logs the current status of decks and hands (for debugging)
        /// </summary>
        private void LogDeckStatus()
        {
            if (playerDeckManager != null)
            {
                Debug.Log($"Player - Hand Empty: {playerDeckManager.IsHandEmpty()}, Deck Empty: {playerDeckManager.IsDeckEmpty()}");
            }
            if (opponentDeckManager != null)
            {
                Debug.Log($"Opponent - Hand Empty: {opponentDeckManager.IsHandEmpty()}, Deck Empty: {opponentDeckManager.IsDeckEmpty()}");
            }
        }
        
        /// <summary>
        /// Gets the player deck manager (for external access if needed)
        /// </summary>
        public NewDeckManager GetPlayerDeckManager()
        {
            return playerDeckManager;
        }
        
        /// <summary>
        /// Gets the opponent deck manager (for external access if needed)
        /// </summary>
        public NewDeckManagerOpp GetOpponentDeckManager()
        {
            return opponentDeckManager;
        }
        
        /// <summary>
        /// Resets the game end manager for a new game
        /// </summary>
        public void Reset()
        {
            isGameEnding = false;
            areChainsInProgress = false;
            StopAllCoroutines();
        }
    }
}
