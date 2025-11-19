using System.Collections;
using UnityEngine;

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
        }
        
        /// <summary>
        /// Called when the board becomes full (last space filled)
        /// </summary>
        public void CheckGameEnd()
        {
            if (isGameEnding) return;
            
            Debug.Log("Board is full! Checking for game end...");
            isGameEnding = true;
            
            // Start coroutine to wait for chains to complete, then end game
            StartCoroutine(WaitForChainsAndEndGame());
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
            
            // Evaluate winner
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
            
            Debug.Log($"Final Scores - Player: {playerScore}, Opponent: {opponentScore}");
            
            if (GameManager.Instance == null)
            {
                Debug.LogError("GameEndManager: GameManager not found!");
                return;
            }
            
            // Determine winner based on scores
            var scoreUI = GameObject.FindObjectOfType<CardGame.UI.ScoreAndWinnerUI>();
            if (playerScore > opponentScore)
            {
                Debug.Log("Player wins!");
                if (scoreUI != null) scoreUI.ShowWinner("Player Wins!");
                GameManager.Instance.ChangeState(GameState.Victory);
            }
            else if (opponentScore > playerScore)
            {
                Debug.Log("Opponent wins!");
                if (scoreUI != null) scoreUI.ShowWinner("Opponent Wins!");
                GameManager.Instance.ChangeState(GameState.Defeat);
            }
            else
            {
                Debug.Log("It's a tie!");
                if (scoreUI != null) scoreUI.ShowWinner("It's a Tie!");
                GameManager.Instance.ChangeState(GameState.Victory);
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
