using UnityEngine;
using CardGame.UI;
using CardGame.Core;

namespace CardGame.Managers
{
    /// <summary>
    /// Manages scoring for player and opponent based on captured cards
    /// </summary>
    public class ScoreManager : MonoBehaviour
    {
        public static ScoreManager Instance { get; private set; }
        
        private int playerScore = 0;
        private int opponentScore = 0;
        
        public int PlayerScore => playerScore;
        public int OpponentScore => opponentScore;
        
        // Event triggered when score changes
        public System.Action<bool, int> OnScoreChanged; // (isPlayer, newScore)
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
        }
        
        /// <summary>
        /// Adds a point to the specified player's score
        /// </summary>
        /// <param name="isPlayer">True for player, false for opponent</param>
        public void AddScore(bool isPlayer)
        {
            if (isPlayer)
            {
                playerScore++;
                OnScoreChanged?.Invoke(true, playerScore);
                Debug.Log($"Player score: {playerScore}");
            }
            else
            {
                opponentScore++;
                OnScoreChanged?.Invoke(false, opponentScore);
                Debug.Log($"Opponent score: {opponentScore}");
            }
            // Removed UpdateScoreUI calls. UI updates are now handled by ScoreAndWinnerUI via OnScoreChanged event
        }
        
        /// <summary>
        /// Gets the score for the specified player
        /// </summary>
        /// <param name="isPlayer">True for player, false for opponent</param>
        /// <returns>The player's score</returns>
        public int GetScore(bool isPlayer)
        {
            return isPlayer ? playerScore : opponentScore;
        }
        
        /// <summary>
        /// Resets both scores to zero
        /// </summary>
        public void ResetScores()
        {
            playerScore = 0;
            opponentScore = 0;
            OnScoreChanged?.Invoke(true, playerScore);
            OnScoreChanged?.Invoke(false, opponentScore);
            Debug.Log("Scores reset");
        }
        
        /// <summary>
        /// Recalculates scores by counting captured cards on the board
        /// </summary>
        public void RecalculateScores()
        {
            playerScore = 0;
            opponentScore = 0;
            
            // Find all cards on the board
            CardMover[] allCardMovers = FindObjectsOfType<CardMover>();
            CardMoverOpp[] allCardMoverOpps = FindObjectsOfType<CardMoverOpp>();
            
            // Check player cards (CardMover)
            foreach (CardMover cardMover in allCardMovers)
            {
                if (cardMover.Card != null && IsCardCaptured(cardMover.gameObject))
                {
                    bool isPlayerCard = IsPlayerCard(cardMover.gameObject);
                    if (isPlayerCard)
                    {
                        playerScore++;
                    }
                    else
                    {
                        opponentScore++;
                    }
                }
            }
            
            // Check opponent cards (CardMoverOpp)
            foreach (CardMoverOpp cardMoverOpp in allCardMoverOpps)
            {
                if (cardMoverOpp.Card != null && IsCardCaptured(cardMoverOpp.gameObject))
                {
                    bool isPlayerCard = IsPlayerCard(cardMoverOpp.gameObject);
                    if (isPlayerCard)
                    {
                        playerScore++;
                    }
                    else
                    {
                        opponentScore++;
                    }
                }
            }
            
            OnScoreChanged?.Invoke(true, playerScore);
            OnScoreChanged?.Invoke(false, opponentScore);
            
            Debug.Log($"Recalculated scores - Player: {playerScore}, Opponent: {opponentScore}");
            // Trigger UI updates after recalculating scores
            UpdateScoreUI();
        }
        
        /// <summary>
        /// Checks if a card GameObject is captured (has capture color)
        /// </summary>
        private bool IsCardCaptured(GameObject cardObject)
        {
            if (cardObject == null) return false;
            
            NewCardUI cardUI = cardObject.GetComponent<NewCardUI>();
            if (cardUI == null)
            {
                cardUI = cardObject.GetComponentInChildren<NewCardUI>();
            }
            if (cardUI == null)
            {
                cardUI = cardObject.GetComponentInParent<NewCardUI>();
            }
            
            if (cardUI == null) return false;
            
            // Check the card's background color to determine if it's captured
            var cardBackgroundField = typeof(NewCardUI).GetField("cardBackground",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (cardBackgroundField != null)
            {
                var cardBackground = cardBackgroundField.GetValue(cardUI);
                if (cardBackground != null)
                {
                    Color borderColor = Color.white;
                    
                    SpriteRenderer bgSR = cardBackground as SpriteRenderer;
                    if (bgSR != null)
                    {
                        borderColor = bgSR.color;
                    }
                    else
                    {
                        UnityEngine.UI.Image bgImg = cardBackground as UnityEngine.UI.Image;
                        if (bgImg != null)
                        {
                            borderColor = bgImg.color;
                        }
                    }
                    
                    // Check if it's a capture color (not default white/transparent)
                    Color playerColor = new Color(1f, 0.5f, 0f, 1f); // Orange
                    Color opponentColor = new Color(0f, 0.8f, 0f, 1f); // Green
                    
                    float colorTolerance = 0.1f;
                    if ((Mathf.Abs(borderColor.r - playerColor.r) < colorTolerance &&
                         Mathf.Abs(borderColor.g - playerColor.g) < colorTolerance &&
                         Mathf.Abs(borderColor.b - playerColor.b) < colorTolerance) ||
                        (Mathf.Abs(borderColor.r - opponentColor.r) < colorTolerance &&
                         Mathf.Abs(borderColor.g - opponentColor.g) < colorTolerance &&
                         Mathf.Abs(borderColor.b - opponentColor.b) < colorTolerance))
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Determines if a card belongs to the player based on its capture color
        /// </summary>
        private bool IsPlayerCard(GameObject cardObject)
        {
            if (cardObject == null) return true;
            // Use tag to determine ownership
            if (cardObject.CompareTag("p1")) return true;
            if (cardObject.CompareTag("p2")) return false;
            // Fallback: assume player card
            return true;
        }
        
        /// <summary>
        /// Updates the score display UI for both players
        /// </summary>
        private void UpdateScoreUI()
        {
            // Find PlayerScoreText and OpponentScoreText in the scene
            var playerScoreText = GameObject.Find("PlayerScoreText");
            var opponentScoreText = GameObject.Find("OpponentScoreText");

            if (playerScoreText != null)
            {
                var tmp = playerScoreText.GetComponent<TMPro.TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.text = $"Player: {playerScore}";
                }
            }

            if (opponentScoreText != null)
            {
                var tmp = opponentScoreText.GetComponent<TMPro.TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.text = $"Opponent: {opponentScore}";
                }
            }
        }
    }
}
