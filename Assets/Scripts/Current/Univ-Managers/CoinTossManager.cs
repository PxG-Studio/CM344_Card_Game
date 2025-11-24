using System;
using UnityEngine;
using CardGame.Managers;

namespace CardGame.Managers
{
    /// <summary>
    /// Manages the visual coin toss to determine which player goes first.
    /// Players select heads or tails, then the coin is flipped.
    /// If the result matches the selection, that player goes first; otherwise, the other player goes first.
    /// </summary>
    public class CoinTossManager : MonoBehaviour
    {
        public static CoinTossManager Instance { get; private set; }

        private FateSide? coinTossResult = null;
        private bool isCoinTossComplete = false;
        private bool? playerSelection = null; // null = not selected, true = heads, false = tails
        private FateSide? selectedByPlayer = null; // Which player made the selection (Player 1 or Player 2)

        /// <summary>
        /// Event fired when coin toss is complete. Parameter is the starting player side.
        /// </summary>
        public event Action<FateSide> OnCoinTossComplete;

        /// <summary>
        /// Gets the coin toss result (heads or tails). Returns null if coin toss has not been performed yet.
        /// </summary>
        public FateSide? Result => coinTossResult;

        /// <summary>
        /// Gets whether a player has selected heads or tails.
        /// </summary>
        public bool HasSelection => playerSelection.HasValue;

        /// <summary>
        /// Gets the player's selection (true = heads, false = tails). Returns null if not selected yet.
        /// </summary>
        public bool? PlayerSelection => playerSelection;

        /// <summary>
        /// Gets which player made the selection.
        /// </summary>
        public FateSide? SelectedByPlayer => selectedByPlayer;

        /// <summary>
        /// Gets whether the coin toss has been completed.
        /// </summary>
        public bool IsComplete => isCoinTossComplete;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // Don't auto-perform coin toss - wait for GameManager to trigger it
            // This prevents duplicate execution warnings
        }

        /// <summary>
        /// Sets the player's selection (heads or tails). Must be called before PerformCoinToss().
        /// </summary>
        /// <param name="selectHeads">True for heads, false for tails</param>
        /// <param name="selectingPlayer">Which player is making the selection (Player 1 or Player 2)</param>
        public void SetPlayerSelection(bool selectHeads, FateSide selectingPlayer)
        {
            if (isCoinTossComplete)
            {
                Debug.LogWarning("[CoinTossManager] Cannot change selection - coin toss already performed.");
                return;
            }

            playerSelection = selectHeads;
            selectedByPlayer = selectingPlayer;
            string selectionString = selectHeads ? "Heads" : "Tails";
            string playerString = selectingPlayer == FateSide.P1 ? "Player 1" : "Player 2";
            Debug.Log($"[CoinTossManager] {playerString} selected: {selectionString}");
        }

        /// <summary>
        /// Performs a random coin toss and determines the starting player based on player selection.
        /// If the result matches the player's selection, that player goes first; otherwise, the other player goes first.
        /// </summary>
        /// <returns>The starting player side (Player 1 or Player 2)</returns>
        public FateSide PerformCoinToss()
        {
            if (isCoinTossComplete)
            {
                Debug.LogWarning("[CoinTossManager] Coin toss already performed. Returning existing result.");
                return coinTossResult.Value;
            }

            if (!playerSelection.HasValue)
            {
                Debug.LogWarning("[CoinTossManager] No player selection made. Defaulting to random result.");
                // Fallback: random selection if no player selection was made
                bool isHeads = UnityEngine.Random.Range(0, 2) == 0;
                coinTossResult = isHeads ? FateSide.P1 : FateSide.P2;
                isCoinTossComplete = true;
                OnCoinTossComplete?.Invoke(coinTossResult.Value);
                return coinTossResult.Value;
            }

            // Random coin flip: 0 = Heads, 1 = Tails
            bool flipResultIsHeads = UnityEngine.Random.Range(0, 2) == 0;
            bool playerSelectedHeads = playerSelection.Value;

            // Determine starting player:
            // If flip result matches player's selection, that player goes first
            // Otherwise, the other player goes first
            FateSide startingPlayer;
            if (flipResultIsHeads == playerSelectedHeads)
            {
                // Result matches selection - selecting player goes first
                startingPlayer = selectedByPlayer.Value;
            }
            else
            {
                // Result doesn't match selection - other player goes first
                startingPlayer = selectedByPlayer.Value == FateSide.P1 ? FateSide.P2 : FateSide.P1;
            }

            coinTossResult = startingPlayer;
            isCoinTossComplete = true;

            string flipResultString = flipResultIsHeads ? "Heads" : "Tails";
            string selectionString = playerSelectedHeads ? "Heads" : "Tails";
            string playerString = selectedByPlayer.Value == FateSide.P1 ? "Player 1" : "Player 2";
            string startingPlayerString = startingPlayer == FateSide.P1 ? "Player 1" : "Player 2";
            
            Debug.Log($"[CoinTossManager] Coin flip result: {flipResultString}. {playerString} selected {selectionString}. " +
                $"Result matches selection: {flipResultIsHeads == playerSelectedHeads}. {startingPlayerString} goes first.");
            Debug.Log($"[CoinTossManager] coinTossResult set to: {coinTossResult.Value} ({(coinTossResult.Value == FateSide.P1 ? "Player 1" : "Player 2")})");

            // Fire event for UI and other systems
            OnCoinTossComplete?.Invoke(coinTossResult.Value);
            Debug.Log($"[CoinTossManager] OnCoinTossComplete event fired with: {coinTossResult.Value} ({(coinTossResult.Value == FateSide.P1 ? "Player 1" : "Player 2")})");

            return coinTossResult.Value;
        }

        /// <summary>
        /// Gets the coin flip result (heads or tails, not the starting player).
        /// </summary>
        /// <returns>True if heads, false if tails. Returns null if coin hasn't been flipped yet.</returns>
        public bool? GetFlipResult()
        {
            if (!isCoinTossComplete || !playerSelection.HasValue || !selectedByPlayer.HasValue)
            {
                return null;
            }

            // Determine if the flip was heads or tails based on:
            // - What the player selected
            // - Whether the selecting player goes first (which tells us if selection matched flip)
            // If selection matches flip, selecting player goes first
            // If selection doesn't match flip, other player goes first
            
            bool playerSelectedHeads = playerSelection.Value;
            bool selectingPlayerGoesFirst = (coinTossResult.Value == selectedByPlayer.Value);
            
            // If player selected heads and goes first → flip was heads
            // If player selected heads and doesn't go first → flip was tails
            // If player selected tails and goes first → flip was tails
            // If player selected tails and doesn't go first → flip was heads
            // Simplified: flipResult = (playerSelectedHeads == selectingPlayerGoesFirst)
            return playerSelectedHeads == selectingPlayerGoesFirst;
        }

        /// <summary>
        /// Resets the coin toss state for a new game (rematch).
        /// </summary>
        public void ResetCoinToss()
        {
            coinTossResult = null;
            isCoinTossComplete = false;
            playerSelection = null;
            selectedByPlayer = null;
            Debug.Log("[CoinTossManager] Coin toss reset for new game");
        }

        /// <summary>
        /// Gets the starting player side. Returns Player 1 if coin toss has not been performed.
        /// </summary>
        /// <returns>The starting player side</returns>
        public FateSide GetStartingPlayer()
        {
            if (coinTossResult.HasValue)
            {
                Debug.Log($"[CoinTossManager] GetStartingPlayer() returning: {coinTossResult.Value} ({(coinTossResult.Value == FateSide.P1 ? "Player 1" : "Player 2")})");
                return coinTossResult.Value;
            }

            // Default to Player 1 if not yet tossed
            Debug.LogWarning("[CoinTossManager] Coin toss not yet performed. Defaulting to Player 1.");
            return FateSide.P1;
        }

        /// <summary>
        /// Forces a specific result (for testing/debugging purposes).
        /// </summary>
        /// <param name="startingSide">The side to force as the starting player</param>
        public void SetForcedResult(FateSide startingSide)
        {
            coinTossResult = startingSide;
            isCoinTossComplete = true;
            Debug.Log($"[CoinTossManager] Forced coin toss result: {startingSide}");
            OnCoinTossComplete?.Invoke(startingSide);
        }
    }
}

