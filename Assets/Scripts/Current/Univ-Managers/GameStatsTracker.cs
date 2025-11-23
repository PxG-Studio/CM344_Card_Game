using UnityEngine;

namespace CardGame.Managers
{
    /// <summary>
    /// [CardFront] Tracks session statistics: wins, losses, and per-game metrics
    /// </summary>
    public class GameStatsTracker : MonoBehaviour
    {
        public static GameStatsTracker Instance { get; private set; }
        
        // Session statistics
        private int totalGames = 0;
        private int wins = 0;
        private int losses = 0;
        private int ties = 0;
        
        // Per-game statistics (for current game)
        private int currentCardsPlayed = 0;
        private int currentCapturesMade = 0;
        private int currentLongestChain = 0;
        private int currentScoreMargin = 0;
        
        // Properties
        public int TotalGames => totalGames;
        public int Wins => wins;
        public int Losses => losses;
        public int Ties => ties;
        
        public int CurrentCardsPlayed => currentCardsPlayed;
        public int CurrentCapturesMade => currentCapturesMade;
        public int CurrentLongestChain => currentLongestChain;
        public int CurrentScoreMargin => currentScoreMargin;
        
        /// <summary>
        /// Gets win rate as a percentage (0-100)
        /// </summary>
        public float WinRate => totalGames > 0 ? (float)wins / totalGames * 100f : 0f;
        
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
        /// Records game result and statistics
        /// </summary>
        public void RecordGameResult(bool playerWon, bool isTie, int cardsPlayed, int capturesMade, int longestChain, int scoreMargin)
        {
            totalGames++;
            
            if (isTie)
            {
                ties++;
            }
            else if (playerWon)
            {
                wins++;
            }
            else
            {
                losses++;
            }
            
            currentCardsPlayed = cardsPlayed;
            currentCapturesMade = capturesMade;
            currentLongestChain = longestChain;
            currentScoreMargin = scoreMargin;
            
            Debug.Log($"[GameStatsTracker] Game recorded - Result: {(isTie ? "Tie" : (playerWon ? "Win" : "Loss"))}, " +
                     $"Stats: Cards={cardsPlayed}, Captures={capturesMade}, Chain={longestChain}, Margin={scoreMargin}. " +
                     $"Session: {wins}W/{losses}L/{ties}T");
        }
        
        /// <summary>
        /// Resets current game statistics (called at start of new game)
        /// </summary>
        public void ResetCurrentGameStats()
        {
            currentCardsPlayed = 0;
            currentCapturesMade = 0;
            currentLongestChain = 0;
            currentScoreMargin = 0;
        }
        
        /// <summary>
        /// Resets entire session statistics (for new session)
        /// </summary>
        public void ResetSession()
        {
            totalGames = 0;
            wins = 0;
            losses = 0;
            ties = 0;
            ResetCurrentGameStats();
            Debug.Log("[GameStatsTracker] Session statistics reset");
        }
        
        /// <summary>
        /// Gets formatted win/loss record string
        /// </summary>
        public string GetWinLossRecord()
        {
            return $"Wins: {wins} | Losses: {losses}";
        }
    }
}

