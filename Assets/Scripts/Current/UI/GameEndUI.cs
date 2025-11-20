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
        /// Shows the game end screen with winner information
        /// </summary>
        /// <param name="playerWon">True if player won, false if opponent won</param>
        /// <param name="isTie">True if the game is a tie</param>
        public void ShowGameEnd(bool playerWon, bool isTie)
        {
            if (endGamePanel == null) return;
            
            endGamePanel.SetActive(true);
            
            // Get final scores
            int playerScore = 0;
            int opponentScore = 0;
            if (ScoreManager.Instance != null)
            {
                playerScore = ScoreManager.Instance.PlayerScore;
                opponentScore = ScoreManager.Instance.OpponentScore;
            }
            
            // Update winner text
            if (winnerText != null)
            {
                if (isTie)
                {
                    winnerText.text = "IT'S A TIE!";
                    winnerText.color = tieColor;
                }
                else if (playerWon)
                {
                    winnerText.text = "PLAYER 1 WINS!";
                    winnerText.color = victoryColor;
                }
                else
                {
                    winnerText.text = "PLAYER 2 WINS!";
                    winnerText.color = defeatColor;
                }
            }
            
            // Update final score text
            if (finalScoreText != null)
            {
                finalScoreText.text = $"Final Score\nPlayer 1: {playerScore}  |  Player 2: {opponentScore}";
            }
            
            TriggerCutIn(playerWon, isTie);
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
            Debug.Log("Restart button clicked");
            // TODO: Implement restart logic
            // You might want to reload the scene or reset the game state
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
            );
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

