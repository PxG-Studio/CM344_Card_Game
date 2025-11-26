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
        private NewDeckManagerP1 playerDeckManager;
        private NewDeckManagerP2 opponentDeckManager;
        
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
                playerDeckManager = FindObjectOfType<NewDeckManagerP1>();
            }
            if (opponentDeckManager == null)
            {
                opponentDeckManager = FindObjectOfType<NewDeckManagerP2>();
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
        /// Checks if game should end (when all 16 board slots are filled)
        /// </summary>
        public void CheckGameEnd()
        {
            if (isGameEnding)
            {
                Debug.Log("[GameEndManager] CheckGameEnd called but game is already ending. Ignoring duplicate call.");
                return;
            }
            
            // Game ends when all 16 board slots are filled (territory-based scoring)
            // Get board occupancy count from CardDropArea
            (int occupiedSpaces, int totalSpaces) = CardDropArea.GetBoardOccupancy();
            
            bool allSlotsFilled = occupiedSpaces >= totalSpaces && totalSpaces >= 16;
            
            Debug.Log($"[GameEndManager] ===== GAME END CHECK =====");
            Debug.Log($"[GameEndManager] Board occupancy: {occupiedSpaces}/{totalSpaces} slots filled");
            Debug.Log($"[GameEndManager] All slots filled condition: {allSlotsFilled} (need {totalSpaces} slots)");
            Debug.Log($"[GameEndManager] ==========================");
            
            if (allSlotsFilled)
            {
                Debug.Log("[GameEndManager] ✓✓✓ ALL BOARD SLOTS FILLED! ✓✓✓");
                Debug.Log($"[GameEndManager] All {totalSpaces} slots are occupied. Ending game...");
                Debug.Log($"[GameEndManager] Current gameEndUI reference: {(gameEndUI != null ? "Found" : "Null - will search when showing UI")}");
                isGameEnding = true;
                
                // Start coroutine to wait for chains to complete, then end game
                StartCoroutine(WaitForChainsAndEndGame());
            }
            else
            {
                Debug.Log($"[GameEndManager] ❌ Game continues - Waiting for all slots to be filled. Occupied: {occupiedSpaces}/{totalSpaces} (need {totalSpaces})");
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
            
            int p1Score = ScoreManager.Instance.P1Score;
            int p2Score = ScoreManager.Instance.P2Score;
            bool isTie = p1Score == p2Score;
            int scoreMargin = ScoreManager.Instance.GetScoreMargin();
            
            // Get statistics
            int cardsPlayed = CardDropArea.GetCardsPlayed();
            int capturesMade = CardDropArea.GetCapturesMade();
            int longestChain = CardDropArea.GetLongestChain();
            
            bool p1Won = p1Score > p2Score;
            
            // Record statistics in GameStatsTracker
            if (GameStatsTracker.Instance != null)
            {
                GameStatsTracker.Instance.RecordGameResult(p1Won, isTie, cardsPlayed, capturesMade, longestChain, scoreMargin);
            }
            
            Debug.Log($"[GameEndManager] Final Scores - P1: {p1Score}, P2: {p2Score}, Margin: {scoreMargin}");
            Debug.Log($"[GameEndManager] Statistics - Cards Played: {cardsPlayed}, Captures Made: {capturesMade}, Longest Chain: {longestChain}");
            
            if (GameManager.Instance == null)
            {
                Debug.LogError("GameEndManager: GameManager not found!");
                return;
            }
            
            // Determine winner based on scores
            if (p1Score > p2Score)
            {
                Debug.Log("P1 wins!");
                GameManager.Instance.ChangeState(GameState.Victory);
                ShowWinnerUI(true, false, cardsPlayed, capturesMade, longestChain, scoreMargin);
            }
            else if (p2Score > p1Score)
            {
                Debug.Log("Opponent wins!");
                GameManager.Instance.ChangeState(GameState.Defeat);
                ShowWinnerUI(false, false, cardsPlayed, capturesMade, longestChain, scoreMargin);
            }
            else
            {
                // Proper draw handling: neither side wins, but the game ends in a tie.
                Debug.Log("It's a tie!");
                GameManager.Instance.ChangeState(GameState.Draw);
                ShowWinnerUI(false, true, cardsPlayed, capturesMade, longestChain, scoreMargin);
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
        public NewDeckManagerP1 GetPlayerDeckManager()
        {
            return playerDeckManager;
        }
        
        /// <summary>
        /// Gets the opponent deck manager (for external access if needed)
        /// </summary>
        public NewDeckManagerP2 GetOpponentDeckManager()
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
