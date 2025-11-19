using TMPro;
using UnityEngine;
using CardGame.Managers;
using CardGame.Core;

namespace CardGame.UI
{
    /// <summary>
    /// Manages the HUD overlay display including player panels, scores, and card counts.
    /// </summary>
    public class HUDManager : MonoBehaviour
    {
        [Header("Player 1 Panel")]
        [SerializeField] private TMP_Text p1ScoreLabel;
        [SerializeField] private TMP_Text p1HandDeckLabel;
        
        [Header("Player 2 Panel")]
        [SerializeField] private TMP_Text p2ScoreLabel;
        [SerializeField] private TMP_Text p2HandDeckLabel;
        
        [Header("Tiles Remaining")]
        [SerializeField] private TMP_Text tilesRemainingLabel;
        
        [Header("Managers")]
        [SerializeField] private NewDeckManager player1DeckManager;
        [SerializeField] private NewDeckManagerOpp player2DeckManager;
        
        private ScoreManager scoreManager;
        private int totalBoardTiles = 64; // 8x8 board
        
        private void Start()
        {
            // Find ScoreManager
            scoreManager = FindObjectOfType<ScoreManager>();
            if (scoreManager != null)
            {
                scoreManager.OnScoreUpdated += UpdateScores;
            }
            else
            {
                Debug.LogWarning("HUDManager: ScoreManager not found in scene!");
            }
            
            // Auto-find deck managers if not assigned
            if (player1DeckManager == null)
            {
                player1DeckManager = FindObjectOfType<NewDeckManager>();
            }
            if (player2DeckManager == null)
            {
                player2DeckManager = FindObjectOfType<NewDeckManagerOpp>();
            }
            
            // Initialize displays
            UpdateScores(0, 0);
            UpdateHandDeckCounts();
            UpdateTilesRemaining();
        }
        
        private void Update()
        {
            // Update hand/deck counts every frame (light operation)
            UpdateHandDeckCounts();
            UpdateTilesRemaining();
        }
        
        private void OnDestroy()
        {
            if (scoreManager != null)
            {
                scoreManager.OnScoreUpdated -= UpdateScores;
            }
        }
        
        /// <summary>
        /// Update score displays for both players.
        /// </summary>
        private void UpdateScores(int player1Score, int player2Score)
        {
            if (p1ScoreLabel != null)
            {
                p1ScoreLabel.text = $"Score: {player1Score}";
            }
            
            if (p2ScoreLabel != null)
            {
                p2ScoreLabel.text = $"Score: {player2Score}";
            }
        }
        
        /// <summary>
        /// Update hand and deck count displays for both players.
        /// </summary>
        private void UpdateHandDeckCounts()
        {
            // Player 1
            if (p1HandDeckLabel != null && player1DeckManager != null)
            {
                int handCount = player1DeckManager.Hand.Count;
                int deckCount = player1DeckManager.DrawPileCount;
                p1HandDeckLabel.text = $"Hand: {handCount} | Deck: {deckCount}";
            }
            
            // Player 2
            if (p2HandDeckLabel != null && player2DeckManager != null)
            {
                int handCount = player2DeckManager.Hand.Count;
                int deckCount = player2DeckManager.DrawPileCount;
                p2HandDeckLabel.text = $"Hand: {handCount} | Deck: {deckCount}";
            }
        }
        
        
        /// <summary>
        /// Update tiles remaining display based on occupied board slots.
        /// </summary>
        private void UpdateTilesRemaining()
        {
            if (tilesRemainingLabel == null) return;
            
            // Count placed cards on the board
            int placedCards = 0;
            
            CardMover[] playerCards = FindObjectsOfType<CardMover>();
            foreach (CardMover card in playerCards)
            {
                if (card.IsPlayed)
                {
                    placedCards++;
                }
            }
            
            CardMoverOpp[] opponentCards = FindObjectsOfType<CardMoverOpp>();
            foreach (CardMoverOpp card in opponentCards)
            {
                if (card.IsPlayed)
                {
                    placedCards++;
                }
            }
            
            int remaining = totalBoardTiles - placedCards;
            tilesRemainingLabel.text = $"Tiles: {remaining}";
        }
        
        /// <summary>
        /// Manually refresh all HUD displays.
        /// </summary>
        public void RefreshAll()
        {
            if (scoreManager != null)
            {
                UpdateScores(scoreManager.PlayerScore, scoreManager.OpponentScore);
            }
            UpdateHandDeckCounts();
            UpdateTilesRemaining();
        }
    }
}

