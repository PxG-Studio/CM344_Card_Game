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

        AudioManager audioManager;
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            audioManager = GameObject.FindGameObjectWithTag("Audio").GetComponent<AudioManager>();
        }
        
        private void Start()
        {
            // Ensure all dependencies are found
            EnsureDependencies();
        }
        
        /// <summary>
        /// Ensures all required dependencies are found and wired up
        /// </summary>
        private void EnsureDependencies()
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
            
            // Auto-find GameEndUI if not assigned (it might be created dynamically by HUDSetup)
            if (gameEndUI == null)
            {
                gameEndUI = FindObjectOfType<CardGame.UI.GameEndUI>(true); // Search inactive objects too
            }
            
            // Verify critical dependencies exist
            if (ScoreManager.Instance == null)
            {
                // ScoreManager should be created by HUDSetup, but if it's missing, log a warning
            }
            if (GameStatsTracker.Instance == null)
            {
                // GameStatsTracker should be created by HUDSetup, but if it's missing, log a warning
            }
            if (GameManager.Instance == null)
            {
                // GameManager should exist, but if it's missing, log a warning
            }
        }
        
        /// <summary>
        /// [CardFront] Checks if game should end (when all cards have been played - both hands empty and all 10 cards on board)
        /// </summary>
        public void CheckGameEnd()
        {
            if (isGameEnding)
            {
                return;
            }
            
            // Ensure dependencies are found (in case they were created after Start)
            EnsureDependencies();
            
            // [CardFront] Check if all cards have been played
            // Game ends when:
            // 1. Both players' hands are empty (no more cards to play)
            // 2. All 10 cards have been placed on the board (5 player + 5 opponent)
            // Note: Cards are moved to discard pile when played, so IsDeckEmpty() is not reliable
            
            bool playerHandEmpty = false;
            bool opponentHandEmpty = false;
            int totalCardsPlayed = CardDropArea.GetCardsPlayed();
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
            }
            else
            {
                // Try to find it again
                playerDeckManager = FindObjectOfType<NewDeckManagerP1>();
            }
            
            if (opponentDeckManager != null)
            {
                opponentHandEmpty = opponentDeckManager.IsHandEmpty();
                // [CardFront] Get actual hand count for detailed diagnostics
                if (opponentDeckManager.Hand != null)
                {
                    opponentHandCount = opponentDeckManager.Hand.Count;
                }
            }
            else
            {
                // Try to find it again
                opponentDeckManager = FindObjectOfType<NewDeckManagerP2>();
            }
            
            // Check if board is full (16 cards on 4x4 board)
            CardDropArea[] allDropAreas = FindObjectsOfType<CardDropArea>();
            int totalBoardSpaces = allDropAreas.Length;
            int occupiedSpaces = 0;
            foreach (CardDropArea dropArea in allDropAreas)
            {
                if (dropArea != null && dropArea.IsOccupied)
                {
                    occupiedSpaces++;
                }
            }
            bool boardIsFull = (occupiedSpaces >= totalBoardSpaces && totalBoardSpaces > 0);
            
            // Game ends when:
            // 1. Both hands are empty AND at least 10 cards are on the board, OR
            // 2. The board is full (all 16 spaces occupied)
            bool allCardsPlayed = ((totalCardsPlayed >= 10) && playerHandEmpty && opponentHandEmpty) || boardIsFull;
            
            if (allCardsPlayed)
            {
                if (isGameEnding)
                {
                    return;
                }
                
                isGameEnding = true;
                
                // Start coroutine to wait for chains to complete, then end game
                StartCoroutine(WaitForChainsAndEndGame());
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
        /// Checks if chain captures are currently in progress
        /// </summary>
        public bool AreChainsInProgress()
        {
            return areChainsInProgress;
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
        /// Evaluates the winner based on Battle Front Influence bar control (not scores) and changes game state
        /// </summary>
        private void EvaluateWinner()
        {
            // Get current board control values directly from CardDropArea (most up-to-date source)
            (int p1Control, int p2Control) = CardDropArea.GetBoardControl();
            
            // Update the Battle Front Influence UI with current values to ensure it's in sync
            CardGame.UI.CardFrontlineUI frontlineUI = FindObjectOfType<CardGame.UI.CardFrontlineUI>();
            if (frontlineUI != null)
            {
                // Get current board occupancy for remaining fields calculation
                CardDropArea[] allDropAreas = FindObjectsOfType<CardDropArea>();
                int totalSpaces = allDropAreas.Length;
                int occupiedSpaces = 0;
                foreach (CardDropArea dropArea in allDropAreas)
                {
                    if (dropArea != null && dropArea.IsOccupied)
                    {
                        occupiedSpaces++;
                    }
                }
                int remainingFields = totalSpaces - occupiedSpaces;
                
                // Force update the frontline UI with current board state
                frontlineUI.UpdateFrontline(p1Control, p2Control, remainingFields);
            }
            
            bool isTie = p1Control == p2Control;
            int controlMargin = Mathf.Abs(p1Control - p2Control);
            
            // Get statistics (still use scores for statistics display, but winner is determined by control)
            int p1Score = ScoreManager.Instance != null ? ScoreManager.Instance.P1Score : 0;
            int p2Score = ScoreManager.Instance != null ? ScoreManager.Instance.P2Score : 0;
            int scoreMargin = ScoreManager.Instance != null ? ScoreManager.Instance.GetScoreMargin() : 0;
            
            // Get statistics
            int cardsPlayed = CardDropArea.GetCardsPlayed();
            int capturesMade = CardDropArea.GetCapturesMade();
            int longestChain = CardDropArea.GetLongestChain();
            
            bool p1Won = p1Control > p2Control;
            
            // Record statistics in GameStatsTracker (use control-based result)
            if (GameStatsTracker.Instance != null)
            {
                GameStatsTracker.Instance.RecordGameResult(p1Won, isTie, cardsPlayed, capturesMade, longestChain, controlMargin);
            }
            
            if (GameManager.Instance == null)
            {
                return;
            }
            
            // Determine winner based on Battle Front Influence control (not scores)
            if (p1Control > p2Control)
            {
                GameManager.Instance.ChangeState(GameState.Victory);
                ShowWinnerUI(true, false, cardsPlayed, capturesMade, longestChain, controlMargin);
                audioManager.PlaySFX(audioManager.Victory);
                audioManager.PlaySFX(audioManager.FireCaptureCard);
            }
            else if (p2Control > p1Control)
            {
                GameManager.Instance.ChangeState(GameState.Defeat);
                ShowWinnerUI(false, false, cardsPlayed, capturesMade, longestChain, controlMargin);
                audioManager.PlaySFX(audioManager.Victory);
                audioManager.PlaySFX(audioManager.EarthCaptureCard);
            }
            else
            {
                // Tie - both players have equal control
                GameManager.Instance.ChangeState(GameState.Draw);
                ShowWinnerUI(true, true, cardsPlayed, capturesMade, longestChain, controlMargin);
                audioManager.PlaySFX(audioManager.Tie);
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
                    return;
                }
                else
                {
                }
            }
            
            if (gameEndUI != null)
            {
                gameEndUI.ShowGameEnd(playerWon, isTie, cardsPlayed, capturesMade, longestChain, scoreMargin);
            }
            else
            {
            }
        }
        
        /// <summary>
        /// Logs the current status of decks and hands (for debugging)
        /// </summary>
        private void LogDeckStatus()
        {
            if (playerDeckManager != null)
            {
            }
            if (opponentDeckManager != null)
            {
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
            
            // Re-ensure dependencies in case they were destroyed/recreated
            EnsureDependencies();
        }
    }
}
