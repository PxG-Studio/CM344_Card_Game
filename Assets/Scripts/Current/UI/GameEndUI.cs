using TMPro;
using UnityEngine;
using UnityEngine.UI;
using CardGame.Managers;

namespace CardGame.UI
{
    /// <summary>
    /// Displays the game end screen showing the winner
    /// </summary>
    public class GameEndUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject endGamePanel;
        [SerializeField] private TMP_Text winnerText;
        [SerializeField] private TMP_Text finalScoreText;
        [SerializeField] private TMP_Text statisticsText;
        [SerializeField] private TMP_Text winLossRecordText;
        [SerializeField] private TMP_Text contextualMessageText;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button quitButton;
        
        [Header("Settings")]
        [SerializeField] private Color victoryColor = new Color(0.2f, 0.8f, 0.2f, 1f); // Green
        [SerializeField] private Color defeatColor = new Color(0.8f, 0.2f, 0.2f, 1f); // Red
        [SerializeField] private Color tieColor = new Color(0.8f, 0.8f, 0.2f, 1f); // Yellow
        
        [Header("Cut-In")]
        [SerializeField] private VictoryCutInController victoryCutIn;
        [SerializeField] private Color playerAccentColor = new Color(0.95f, 0.42f, 0.19f, 1f);
        [SerializeField] private Color opponentAccentColor = new Color(0.08f, 0.78f, 0.2f, 1f);
        [SerializeField] private Color tieAccentColor = new Color(1f, 0.9f, 0.35f, 1f);
        
        private void Start()
        {
            // Hide the panel initially
            if (endGamePanel != null)
            {
                endGamePanel.SetActive(false);
            }
            
            // Subscribe to game state changes
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
            }
            
            // Setup button listeners
            if (restartButton != null)
            {
                restartButton.onClick.AddListener(OnRestartClicked);
            }
            if (quitButton != null)
            {
                quitButton.onClick.AddListener(OnQuitClicked);
            }
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from events
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
            }
        }
        
        private void OnGameStateChanged(GameState newState)
        {
            if (newState == GameState.Victory)
            {
                ShowGameEnd(true, false);
            }
            else if (newState == GameState.Defeat)
            {
                ShowGameEnd(false, false);
            }
        }
        
        /// <summary>
        /// Shows the game end screen with winner information and statistics
        /// </summary>
        /// <param name="playerWon">True if player won, false if opponent won</param>
        /// <param name="isTie">True if the game is a tie</param>
        public void ShowGameEnd(bool playerWon, bool isTie)
        {
            // Legacy overload - collect statistics from trackers
            int cardsPlayed = CardDropArea1.GetCardsPlayed();
            int capturesMade = CardDropArea1.GetCapturesMade();
            int longestChain = CardDropArea1.GetLongestChain();
            int scoreMargin = 0;
            if (ScoreManager.Instance != null)
            {
                scoreMargin = ScoreManager.Instance.GetScoreMargin();
            }
            
            ShowGameEnd(playerWon, isTie, cardsPlayed, capturesMade, longestChain, scoreMargin);
        }
        
        /// <summary>
        /// Shows the game end screen with winner information and statistics
        /// </summary>
        /// <param name="playerWon">True if player won, false if opponent won</param>
        /// <param name="isTie">True if the game is a tie</param>
        /// <param name="cardsPlayed">Number of cards played this game</param>
        /// <param name="capturesMade">Number of captures made this game</param>
        /// <param name="longestChain">Longest chain capture length</param>
        /// <param name="scoreMargin">Score margin (positive = player leads)</param>
        public void ShowGameEnd(bool playerWon, bool isTie, int cardsPlayed, int capturesMade, int longestChain, int scoreMargin)
        {
            Debug.Log($"[GameEndUI] ShowGameEnd called - Player Won: {playerWon}, Is Tie: {isTie}, Cards Played: {cardsPlayed}, Captures: {capturesMade}, Longest Chain: {longestChain}, Margin: {scoreMargin}");
            
            if (endGamePanel == null)
            {
                Debug.LogError("[GameEndUI] endGamePanel is null! Cannot show game end screen.");
                return;
            }
            
            endGamePanel.SetActive(true);
            
            // Get final scores
            int playerScore = 0;
            int opponentScore = 0;
            if (ScoreManager.Instance != null)
            {
                playerScore = ScoreManager.Instance.PlayerScore;
                opponentScore = ScoreManager.Instance.OpponentScore;
            }
            
            // Update winner text (larger size)
            if (winnerText != null)
            {
                if (isTie)
                {
                    winnerText.text = "IT'S A TIE!";
                    winnerText.color = tieColor;
                    winnerText.fontSize = 72;
                }
                else if (playerWon)
                {
                    winnerText.text = "PLAYER 1 WINS!";
                    winnerText.color = victoryColor;
                    winnerText.fontSize = 72;
                }
                else
                {
                    winnerText.text = "PLAYER 2 WINS!";
                    winnerText.color = defeatColor;
                    winnerText.fontSize = 72;
                }
            }
            
            // Update final score text with margin
            string marginText = scoreMargin > 0 ? $"+{scoreMargin}" : scoreMargin < 0 ? $"{scoreMargin}" : "0";
            if (finalScoreText != null)
            {
                finalScoreText.text = $"Final Score\nPlayer 1: {playerScore}  |  Player 2: {opponentScore}\nMargin: {marginText}";
            }
            
            // Update statistics text (with null safety)
            if (statisticsText != null)
            {
                statisticsText.text = $"Cards Played: {cardsPlayed}\nCaptures Made: {capturesMade}\nLongest Chain: {longestChain}";
            }
            else
            {
                Debug.LogWarning("[GameEndUI] Statistics text is null. Statistics will not be displayed.");
            }
            
            // Update win/loss record (with null safety)
            if (winLossRecordText != null)
            {
                if (GameStatsTracker.Instance != null)
                {
                    winLossRecordText.text = GameStatsTracker.Instance.GetWinLossRecord();
                }
                else
                {
                    winLossRecordText.text = "Wins: 0 | Losses: 0";
                    Debug.LogWarning("[GameEndUI] GameStatsTracker.Instance is null. Win/loss record will show default values.");
                }
            }
            else
            {
                Debug.LogWarning("[GameEndUI] Win/loss record text is null. Win/loss record will not be displayed.");
            }
            
            // Update contextual message (with null safety)
            if (contextualMessageText != null)
            {
                contextualMessageText.text = GetContextualMessage(playerWon, isTie, scoreMargin);
            }
            else
            {
                Debug.LogWarning("[GameEndUI] Contextual message text is null. Contextual message will not be displayed.");
            }
            
            TriggerCutIn(playerWon, isTie);
        }
        
        /// <summary>
        /// Gets contextual message based on game closeness
        /// </summary>
        private string GetContextualMessage(bool playerWon, bool isTie, int scoreMargin)
        {
            if (isTie)
            {
                return "Well Played!";
            }
            
            int absMargin = Mathf.Abs(scoreMargin);
            
            // Close game (≤2 point difference)
            if (absMargin <= 2)
            {
                return "Good Game! That was close!";
            }
            // Dominant victory (≥5 points)
            else if (absMargin >= 5)
            {
                return playerWon ? "Dominant Victory!" : "Tough Loss - Better Luck Next Time!";
            }
            // Normal game
            else
            {
                return playerWon ? "You Win!" : "You Lose!";
            }
        }
        
        /// <summary>
        /// Hides the game end screen
        /// </summary>
        public void HideGameEnd()
        {
            if (endGamePanel != null)
            {
                endGamePanel.SetActive(false);
            }
        }
        
        private void OnRestartClicked()
        {
            Debug.Log("[GameEndUI] Rematch button clicked");
            Rematch();
        }
        
        /// <summary>
        /// Resets game state for rematch without reloading scene
        /// </summary>
        private void Rematch()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ResetGameState();
            }
            else
            {
                Debug.LogError("[GameEndUI] GameManager.Instance is null! Cannot rematch.");
                // Fallback: reload scene
                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
                );
            }
        }
        
        private void OnQuitClicked()
        {
            Debug.Log("Quit button clicked");
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
        
        private void TriggerCutIn(bool playerWon, bool isTie)
        {
            if (victoryCutIn == null)
            {
                return;
            }
            
            Color accent = isTie
                ? tieAccentColor
                : (playerWon ? playerAccentColor : opponentAccentColor);
            
            string message = isTie
                ? "IT'S A TIE!"
                : (playerWon ? "PLAYER 1 WINS!" : "PLAYER 2 WINS!");
            
            victoryCutIn.Play(message, accent);
        }
    }
}

