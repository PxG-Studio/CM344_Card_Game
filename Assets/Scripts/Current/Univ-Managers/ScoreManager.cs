using UnityEngine;
using CardGame.UI;
using CardGame.Core;

namespace CardGame.Managers
{
    /// <summary>
    /// Manages scoring for P1 and P2 based on captured cards
    /// </summary>
    public class ScoreManager : MonoBehaviour
    {
        public static ScoreManager Instance { get; private set; }
        
        private int p1Score = 0;
        private int p2Score = 0;
        
        public int P1Score => p1Score; // P1 score
        [System.Obsolete("Use P1Score instead. This property will be removed in a future version.")]
        public int PlayerScore => p1Score; // Legacy property - use P1Score instead (P1 score)
        public int P2Score => p2Score; // P2 score
        [System.Obsolete("Use P2Score instead. This property will be removed in a future version.")]
        public int OpponentScore => p2Score; // Legacy property - use P2Score instead
        
        // Event triggered when score changes
        public System.Action<bool, int> OnScoreChanged; // (isPlayer, newScore)
        
        // Event for UI updates (both scores at once)
        public System.Action<int, int> OnScoreUpdated; // (p1Score, p2Score)
        
        private void Awake()
        {
            // CRITICAL: Ensure only one ScoreManager instance exists
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"[ScoreManager] Duplicate ScoreManager detected on '{gameObject.name}'. Destroying duplicate. Existing instance: '{Instance.gameObject.name}'");
                // Use DestroyImmediate in editor to avoid play mode exit issues
                #if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    DestroyImmediate(gameObject);
                }
                else
                {
                    Destroy(gameObject);
                }
                #else
                Destroy(gameObject);
                #endif
                return;
            }
            
            Instance = this;
        }
        
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
        
        /// <summary>
        /// Adds a point to the specified player's score
        /// </summary>
        /// <param name="isPlayer">True for P1, false for P2</param>
        public void AddScore(bool isPlayer)
        {
            if (isPlayer)
            {
                p1Score++;
                OnScoreChanged?.Invoke(true, p1Score);
            }
            else
            {
                p2Score++;
                OnScoreChanged?.Invoke(false, p2Score);
            }
            
            // Invoke combined score update event
            OnScoreUpdated?.Invoke(p1Score, p2Score);
        }
        
        /// <summary>
        /// Gets the score for the specified player
        /// </summary>
        /// <param name="isPlayer">True for P1, false for P2</param>
        /// <returns>The player's score</returns>
        public int GetScore(bool isPlayer)
        {
            return isPlayer ? p1Score : p2Score;
        }
        
        /// <summary>
        /// Resets both scores to zero
        /// </summary>
        public void ResetScores()
        {
            p1Score = 0;
            p2Score = 0;
            OnScoreChanged?.Invoke(true, p1Score);
            OnScoreChanged?.Invoke(false, p2Score);
            OnScoreUpdated?.Invoke(p1Score, p2Score);
        }
        
        /// <summary>
        /// Gets the score margin (positive = P1 leads, negative = P2 leads)
        /// </summary>
        public int GetScoreMargin()
        {
            return p1Score - p2Score;
        }
        
        /// <summary>
        /// [CardFront] Recalculates scores by counting spaces controlled by each player out of 16 total spaces
        /// </summary>
        public void RecalculateScores()
        {
            p1Score = 0;
            p2Score = 0;
            
            // Find all CardDropArea instances (should be 16 total spaces on the board)
            CardDropArea[] allDropAreas = FindObjectsOfType<CardDropArea>();
            
            if (allDropAreas == null || allDropAreas.Length == 0)
            {
                Debug.LogWarning("[ScoreManager] No CardDropArea instances found! Cannot calculate scores.");
                OnScoreChanged?.Invoke(true, p1Score);
                OnScoreChanged?.Invoke(false, p2Score);
                OnScoreUpdated?.Invoke(p1Score, p2Score);
                return;
            }
            
            // Count spaces controlled by each player
            foreach (CardDropArea dropArea in allDropAreas)
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
                var occupyingCardField = typeof(CardDropArea).GetField("occupyingCard",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (occupyingCardField == null)
                {
                    Debug.LogWarning($"[ScoreManager] Could not access 'occupyingCard' field on CardDropArea '{dropArea.gameObject.name}'. Skipping.");
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
                    p1Score++;
                }
                else
                {
                    p2Score++;
                }
            }
            
            int totalSpaces = allDropAreas.Length;
            int emptySpaces = totalSpaces - p1Score - p2Score;
            
            OnScoreChanged?.Invoke(true, p1Score);
            OnScoreChanged?.Invoke(false, p2Score);
            OnScoreUpdated?.Invoke(p1Score, p2Score);
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
                    Color p2Color = new Color(0f, 0.8f, 0f, 1f); // P2 capture color (green)
                    
                    float colorTolerance = 0.1f;
                    if ((Mathf.Abs(borderColor.r - playerColor.r) < colorTolerance &&
                         Mathf.Abs(borderColor.g - playerColor.g) < colorTolerance &&
                         Mathf.Abs(borderColor.b - playerColor.b) < colorTolerance) ||
                        (Mathf.Abs(borderColor.r - p2Color.r) < colorTolerance &&
                         Mathf.Abs(borderColor.g - p2Color.g) < colorTolerance &&
                         Mathf.Abs(borderColor.b - p2Color.b) < colorTolerance))
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
                CardMoverP1 mover = cardObject.GetComponent<CardMoverP1>();
                if (mover != null) return true;
                CardMoverP2 moverOpp = cardObject.GetComponent<CardMoverP2>();
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
                    Color p2Color = new Color(0f, 0.8f, 0f, 1f); // P2 capture color (green)
                    
                    float colorTolerance = 0.1f;
                    if (Mathf.Abs(borderColor.r - playerColor.r) < colorTolerance &&
                        Mathf.Abs(borderColor.g - playerColor.g) < colorTolerance &&
                        Mathf.Abs(borderColor.b - playerColor.b) < colorTolerance)
                    {
                        return true;
                    }
                    
                    if (Mathf.Abs(borderColor.r - p2Color.r) < colorTolerance &&
                        Mathf.Abs(borderColor.g - p2Color.g) < colorTolerance &&
                        Mathf.Abs(borderColor.b - p2Color.b) < colorTolerance)
                    {
                        return false;
                    }
                }
            }
            
            // Default: check component type
            CardMoverP1 defaultMover = cardObject.GetComponent<CardMoverP1>();
            if (defaultMover != null) return true;
            CardMoverP2 defaultMoverOpp = cardObject.GetComponent<CardMoverP2>();
            if (defaultMoverOpp != null) return false;
            
            return true;
        }
    }
}
