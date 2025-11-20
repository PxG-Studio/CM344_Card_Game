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
        [SerializeField] private TMP_Text p1PlayerLabel;
        [SerializeField] private TurnIndicatorUI p1TurnIndicator;
        
        [Header("Player 2 Panel")]
        [SerializeField] private TMP_Text p2ScoreLabel;
        [SerializeField] private TMP_Text p2HandDeckLabel;
        [SerializeField] private TMP_Text p2PlayerLabel;
        [SerializeField] private TurnIndicatorUI p2TurnIndicator;
        
        [Header("Tiles Remaining")]
        [SerializeField] private TMP_Text tilesRemainingLabel;
        
        [Header("Managers")]
        [SerializeField] private NewDeckManager player1DeckManager;
        [SerializeField] private NewDeckManagerOpp player2DeckManager;
        
        [Header("Turn Indicator Settings")]
        [SerializeField] private Color activeTurnColor = new Color(1f, 0.8f, 0f, 1f); // Gold
        [SerializeField] private Color inactiveTurnColor = new Color(0.3f, 0.3f, 0.3f, 0.3f); // Gray/Transparent
        
        private ScoreManager scoreManager;
        private int totalBoardTiles = 16; // 4x4 board
        private bool isPlayer1Turn = true; // Track whose turn it is
        
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
            
            // Subscribe to GameManager state changes for turn indicators
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
            }
            else
            {
                Debug.LogWarning("HUDManager: GameManager.Instance not found!");
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
            UpdateTurnIndicators();
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
            
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
            }
        }
        
        /// <summary>
        /// Handle game state changes to update turn indicators.
        /// </summary>
        private void HandleGameStateChanged(GameState newState)
        {
            switch (newState)
            {
                case GameState.PlayerTurn:
                    SetTurn(true);
                    break;
                case GameState.EnemyTurn:
                    SetTurn(false);
                    break;
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
        /// Update turn indicator visuals to show whose turn it is.
        /// </summary>
        private void UpdateTurnIndicators()
        {
            // Find and control UI turn indicators
            TurnIndicatorUI p1IndicatorUI = GameObject.Find("TurnIndicator_UI")?.GetComponent<TurnIndicatorUI>();
            TurnIndicatorUI p2IndicatorUI = GameObject.Find("TurnIndicator_UI (1)")?.GetComponent<TurnIndicatorUI>();
            
            // If we can't find by those names, search for all TurnIndicatorUI components
            if (p1IndicatorUI == null || p2IndicatorUI == null)
            {
                TurnIndicatorUI[] allIndicators = FindObjectsOfType<TurnIndicatorUI>();
                if (allIndicators.Length >= 2)
                {
                    // Assume first is P1, second is P2 based on creation order
                    p1IndicatorUI = allIndicators[0];
                    p2IndicatorUI = allIndicators[1];
                }
            }
            
            // Update UI indicators
            if (p1IndicatorUI != null)
            {
                p1IndicatorUI.SetActive(isPlayer1Turn);
            }
            
            if (p2IndicatorUI != null)
            {
                p2IndicatorUI.SetActive(!isPlayer1Turn);
            }
            
            // Also update 2D indicators for fallback (if they exist)
            if (p1TurnIndicator != null)
            {
                p1TurnIndicator.color = isPlayer1Turn ? activeTurnColor : inactiveTurnColor;
            }
            
            if (p2TurnIndicator != null)
            {
                p2TurnIndicator.color = isPlayer1Turn ? inactiveTurnColor : activeTurnColor;
            }
            
            // Update player label styling
            if (p1PlayerLabel != null)
            {
                p1PlayerLabel.fontStyle = isPlayer1Turn ? TMPro.FontStyles.Bold : TMPro.FontStyles.Normal;
            }
            
            if (p2PlayerLabel != null)
            {
                p2PlayerLabel.fontStyle = isPlayer1Turn ? TMPro.FontStyles.Normal : TMPro.FontStyles.Bold;
            }
        }
        
        /// <summary>
        /// Set which player's turn it is.
        /// </summary>
        public void SetTurn(bool isPlayer1)
        {
            isPlayer1Turn = isPlayer1;
            UpdateTurnIndicators();
        }
        
        /// <summary>
        /// Toggle to the next player's turn.
        /// </summary>
        public void NextTurn()
        {
            isPlayer1Turn = !isPlayer1Turn;
            UpdateTurnIndicators();
            Debug.Log($"Turn changed to: {(isPlayer1Turn ? "Player 1" : "Player 2")}");
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
            UpdateTurnIndicators();
        }
    }
}

