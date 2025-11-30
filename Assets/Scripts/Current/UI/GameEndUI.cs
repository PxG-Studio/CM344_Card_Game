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
            else if (newState == GameState.Draw)
            {
                // Draws are handled explicitly with the statistics-aware overload
                // via GameEndManager, but we keep this path for safety so that a
                // direct state change to Draw still shows a tie screen.
                ShowGameEnd(false, true);
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
            int cardsPlayed = CardDropArea.GetCardsPlayed();
            int capturesMade = CardDropArea.GetCapturesMade();
            int longestChain = CardDropArea.GetLongestChain();
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
            
            if (endGamePanel == null)
            {
                return;
            }
            
            endGamePanel.SetActive(true);
            
            // Ensure all child content stays visually contained within the panel
            // bounds regardless of resolution. This is done at runtime so it also
            // works when the panel is created via HUDSetup.
            RectTransform panelRect = endGamePanel.GetComponent<RectTransform>();
            if (panelRect != null)
            {
                // Clamp anchors to stretch inside the parent without overflowing.
                panelRect.anchorMin = new Vector2(0.15f, 0.1f);
                panelRect.anchorMax = new Vector2(0.85f, 0.9f);
                panelRect.pivot = new Vector2(0.5f, 0.5f);
                // Slight nudge to the right to visually center the inner dark box.
                panelRect.anchoredPosition = new Vector2(20f, 0f);
                
                // Ensure all child content, especially the buttons, is visually
                // contained within the dark square and neatly laid out.
                if (winnerText != null)
                {
                    RectTransform wRect = winnerText.GetComponent<RectTransform>();
                    if (wRect != null)
                    {
                        wRect.anchorMin = new Vector2(0.1f, 0.78f);
                        wRect.anchorMax = new Vector2(0.9f, 0.88f);
                        wRect.pivot = new Vector2(0.5f, 0.5f);
                        wRect.anchoredPosition = Vector2.zero;
                    }
                }
                
                if (contextualMessageText != null)
                {
                    RectTransform cRect = contextualMessageText.GetComponent<RectTransform>();
                    if (cRect != null)
                    {
                        cRect.anchorMin = new Vector2(0.1f, 0.66f);
                        cRect.anchorMax = new Vector2(0.9f, 0.76f);
                        cRect.pivot = new Vector2(0.5f, 0.5f);
                        cRect.anchoredPosition = Vector2.zero;
                    }
                }
                
                if (finalScoreText != null)
                {
                    RectTransform fRect = finalScoreText.GetComponent<RectTransform>();
                    if (fRect != null)
                    {
                        fRect.anchorMin = new Vector2(0.1f, 0.52f);
                        fRect.anchorMax = new Vector2(0.9f, 0.64f);
                        fRect.pivot = new Vector2(0.5f, 0.5f);
                        fRect.anchoredPosition = Vector2.zero;
                    }
                }
                
                if (statisticsText != null)
                {
                    RectTransform sRect = statisticsText.GetComponent<RectTransform>();
                    if (sRect != null)
                    {
                        sRect.anchorMin = new Vector2(0.1f, 0.34f);
                        sRect.anchorMax = new Vector2(0.9f, 0.46f);
                        sRect.pivot = new Vector2(0.5f, 0.5f);
                        sRect.anchoredPosition = Vector2.zero;
                    }
                }
                
                if (winLossRecordText != null)
                {
                    RectTransform wlRect = winLossRecordText.GetComponent<RectTransform>();
                    if (wlRect != null)
                    {
                        wlRect.anchorMin = new Vector2(0.1f, 0.24f);
                        wlRect.anchorMax = new Vector2(0.9f, 0.30f);
                        wlRect.pivot = new Vector2(0.5f, 0.5f);
                        // Slight nudge left (~1/2 unit square) so the record text
                        // visually centers under the stats block.
                        wlRect.anchoredPosition = new Vector2(-20f, 0f);
                    }
                }
                
                if (restartButton != null)
                {
                    RectTransform rRect = restartButton.GetComponent<RectTransform>();
                    if (rRect != null)
                    {
                        rRect.anchorMin = new Vector2(0.15f, 0.18f);
                        rRect.anchorMax = new Vector2(0.85f, 0.30f);
                        rRect.pivot = new Vector2(0.5f, 0.5f);
                        rRect.anchoredPosition = Vector2.zero;
                    }
                }
                
                if (quitButton != null)
                {
                    RectTransform qRect = quitButton.GetComponent<RectTransform>();
                    if (qRect != null)
                    {
                        qRect.anchorMin = new Vector2(0.15f, 0.05f);
                        qRect.anchorMax = new Vector2(0.85f, 0.17f);
                        qRect.pivot = new Vector2(0.5f, 0.5f);
                        qRect.anchoredPosition = Vector2.zero;
                    }
                }
                
                // Make sure a Mask exists so any stray content is clipped to the
                // panel's rectangle.
                Mask panelMask = endGamePanel.GetComponent<Mask>();
                if (panelMask == null)
                {
                    panelMask = endGamePanel.AddComponent<Mask>();
                }
                panelMask.showMaskGraphic = true;
            }
            
            // Get final scores
            int p1Score = 0;
            int p2Score = 0;
            if (ScoreManager.Instance != null)
            {
                p1Score = ScoreManager.Instance.P1Score;
                p2Score = ScoreManager.Instance.P2Score;
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
                    winnerText.text = "P1 WINS!";
                    winnerText.color = victoryColor;
                    winnerText.fontSize = 72;
                }
                else
                {
                    winnerText.text = "P2 WINS!";
                    winnerText.color = defeatColor;
                    winnerText.fontSize = 72;
                }
            }
            
            // Update final score text with margin
            string marginText = scoreMargin > 0 ? $"+{scoreMargin}" : scoreMargin < 0 ? $"{scoreMargin}" : "0";
            if (finalScoreText != null)
            {
                finalScoreText.text = $"Final Score\nP1: {p1Score}  |  P2: {p2Score}\nMargin: {marginText}";
            }
            
            // Update statistics text (with null safety)
            if (statisticsText != null)
            {
                statisticsText.text = $"Cards Played: {cardsPlayed}\nCaptures Made: {capturesMade}\nLongest Chain: {longestChain}";
            }
            else
            {
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
                }
            }
            else
            {
            }
            
            // Update contextual message (with null safety)
            if (contextualMessageText != null)
            {
                contextualMessageText.text = GetContextualMessage(playerWon, isTie, scoreMargin);
            }
            else
            {
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
                // Fallback: reload scene
                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
                );
            }
        }
        
        private void OnQuitClicked()
        {
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

