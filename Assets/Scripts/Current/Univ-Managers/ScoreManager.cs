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
        
        // Event for UI updates (both scores at once)
        public System.Action<int, int> OnScoreUpdated; // (playerScore, opponentScore)
        
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
            
            // Invoke combined score update event
            OnScoreUpdated?.Invoke(playerScore, opponentScore);
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
            OnScoreUpdated?.Invoke(playerScore, opponentScore);
            Debug.Log("Scores reset");
        }
        
        /// <summary>
        /// Gets the score margin (positive = player leads, negative = opponent leads)
        /// </summary>
        public int GetScoreMargin()
        {
            return playerScore - opponentScore;
        }
        
        /// <summary>
        /// [CardFront] Recalculates scores by counting spaces controlled by each player out of 16 total spaces
        /// </summary>
        public void RecalculateScores()
        {
            playerScore = 0;
            opponentScore = 0;
            
            // Find all CardDropArea1 instances (should be 16 total spaces on the board)
            CardDropArea1[] allDropAreas = FindObjectsOfType<CardDropArea1>();
            
            if (allDropAreas == null || allDropAreas.Length == 0)
            {
                Debug.LogWarning("[ScoreManager] No CardDropArea1 instances found! Cannot calculate scores.");
                OnScoreChanged?.Invoke(true, playerScore);
                OnScoreChanged?.Invoke(false, opponentScore);
                OnScoreUpdated?.Invoke(playerScore, opponentScore);
                return;
            }
            
            Debug.Log($"[ScoreManager] Found {allDropAreas.Length} CardDropArea1 instances. Calculating scores based on spaces controlled...");
            
            // Count spaces controlled by each player
            foreach (CardDropArea1 dropArea in allDropAreas)
            {
                if (dropArea == null) continue;
                
                // Check if this space is occupied
                if (!dropArea.IsOccupied)
                {
                    // Empty space - no points for either player
                    continue;
                }
                
                // Get the occupying card
                // Use reflection to access the private 'occupyingCard' field
                var occupyingCardField = typeof(CardDropArea1).GetField("occupyingCard",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (occupyingCardField == null)
                {
                    Debug.LogWarning($"[ScoreManager] Could not access 'occupyingCard' field on CardDropArea1 '{dropArea.gameObject.name}'. Skipping.");
                    continue;
                }
                
                GameObject occupyingCard = occupyingCardField.GetValue(dropArea) as GameObject;
                
                if (occupyingCard == null)
                {
                    // Space is marked as occupied but no card reference - skip
                    continue;
                }
                
                // Determine who controls this space based on the card's capture color/owner
                bool isPlayerControlled = IsPlayerCard(occupyingCard);
                
                if (isPlayerControlled)
                {
                    playerScore++;
                }
                else
                {
                    opponentScore++;
                }
            }
            
            int totalSpaces = allDropAreas.Length;
            int emptySpaces = totalSpaces - playerScore - opponentScore;
            
            OnScoreChanged?.Invoke(true, playerScore);
            OnScoreChanged?.Invoke(false, opponentScore);
            OnScoreUpdated?.Invoke(playerScore, opponentScore);
            
            Debug.Log($"[ScoreManager] Recalculated scores based on {totalSpaces} spaces: Player controls {playerScore}/{totalSpaces}, Opponent controls {opponentScore}/{totalSpaces}, Empty: {emptySpaces}/{totalSpaces}");
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
            
            NewCardUI cardUI = cardObject.GetComponent<NewCardUI>();
            if (cardUI == null)
            {
                cardUI = cardObject.GetComponentInChildren<NewCardUI>();
            }
            if (cardUI == null)
            {
                cardUI = cardObject.GetComponentInParent<NewCardUI>();
            }
            
            if (cardUI == null)
            {
                // Fallback: check component type
                CardMover mover = cardObject.GetComponent<CardMover>();
                if (mover != null) return true;
                CardMoverOpp moverOpp = cardObject.GetComponent<CardMoverOpp>();
                if (moverOpp != null) return false;
                return true;
            }
            
            // Check the card's background color
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
                    
                    Color playerColor = new Color(1f, 0.5f, 0f, 1f); // Orange
                    Color opponentColor = new Color(0f, 0.8f, 0f, 1f); // Green
                    
                    float colorTolerance = 0.1f;
                    if (Mathf.Abs(borderColor.r - playerColor.r) < colorTolerance &&
                        Mathf.Abs(borderColor.g - playerColor.g) < colorTolerance &&
                        Mathf.Abs(borderColor.b - playerColor.b) < colorTolerance)
                    {
                        return true;
                    }
                    
                    if (Mathf.Abs(borderColor.r - opponentColor.r) < colorTolerance &&
                        Mathf.Abs(borderColor.g - opponentColor.g) < colorTolerance &&
                        Mathf.Abs(borderColor.b - opponentColor.b) < colorTolerance)
                    {
                        return false;
                    }
                }
            }
            
            // Default: check component type
            CardMover defaultMover = cardObject.GetComponent<CardMover>();
            if (defaultMover != null) return true;
            CardMoverOpp defaultMoverOpp = cardObject.GetComponent<CardMoverOpp>();
            if (defaultMoverOpp != null) return false;
            
            return true;
        }
    }
}
