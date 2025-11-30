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
                }
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
            }
            
            // Game ends when both hands are empty AND all 10 cards are on the board
            bool allCardsPlayed = (totalCardsPlayed >= 10) && playerHandEmpty && opponentHandEmpty;
            
            if (allCardsPlayed)
            {
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
            
            
            if (GameManager.Instance == null)
            {
                return;
            }
            
            // Determine winner based on scores
            if (p1Score > p2Score)
            {
                GameManager.Instance.ChangeState(GameState.Victory);
                ShowWinnerUI(true, false, cardsPlayed, capturesMade, longestChain, scoreMargin);
            }
            else if (p2Score > p1Score)
            {
                GameManager.Instance.ChangeState(GameState.Defeat);
                ShowWinnerUI(false, false, cardsPlayed, capturesMade, longestChain, scoreMargin);
            }
            else
            {
                // Tie - you may want to handle this differently
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
        }
    }
}
