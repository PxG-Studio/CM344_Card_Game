using System;
using UnityEngine;

namespace CardGame.Managers
{
    public enum FateSide
    {
        P1 = 0,
        P2 = 1      // Renamed from legacy naming for P1/P2 consistency
    }

    /// <summary>
    /// Central controller for the Fatebound turn flow. Tracks whose Fate Window is active and raises updates.
    /// </summary>
    public class FateFlowController : MonoBehaviour
    {
        public static FateFlowController Instance { get; private set; }

        [SerializeField] private FateSide startingSide = FateSide.P1;

        public FateSide CurrentFate { get; private set; }

        public event Action<FateSide> OnFateChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Don't set starting side here - wait for coin toss result
            // Coin toss will call SetFate() to determine starting player
            CurrentFate = startingSide; // Default value, will be overridden by coin toss
        }

        private void Start()
        {
            // Only invoke if coin toss hasn't been performed yet
            // If coin toss has been performed, it will set the fate and invoke the event
            if (CoinTossManager.Instance != null && CoinTossManager.Instance.IsComplete)
            {
                // Coin toss already performed, use its result
                FateSide coinTossResult = CoinTossManager.Instance.GetStartingPlayer();
                Debug.Log($"[FateFlowController] Start() - Coin toss already complete. Setting CurrentFate from {CurrentFate} ({(CurrentFate == FateSide.P1 ? "Player 1" : "Player 2")}) to {coinTossResult} ({(coinTossResult == FateSide.P1 ? "Player 1" : "Player 2")})");
                CurrentFate = coinTossResult;
            }
            else
            {
                Debug.Log($"[FateFlowController] Start() - Coin toss not yet complete. CurrentFate remains at default: {CurrentFate} ({(CurrentFate == FateSide.P1 ? "Player 1" : "Player 2")})");
            }
            
            OnFateChanged?.Invoke(CurrentFate);
        }

        public bool CanAct(FateSide side) => CurrentFate == side;

        public void SetFate(FateSide side)
        {
            if (CurrentFate == side)
            {
                Debug.Log($"[FateFlowController] SetFate called with {side} ({(side == FateSide.P1 ? "Player 1" : "Player 2")}), but CurrentFate is already {CurrentFate}. No change needed.");
                return;
            }

            FateSide previousFate = CurrentFate;
            CurrentFate = side;
            Debug.Log($"[FateFlowController] SetFate: Changed from {previousFate} ({(previousFate == FateSide.P1 ? "Player 1" : "Player 2")}) to {CurrentFate} ({(CurrentFate == FateSide.P1 ? "Player 1" : "Player 2")})");
            OnFateChanged?.Invoke(CurrentFate);
        }

        public void AdvanceFateFlow()
        {
            FateSide next = CurrentFate == FateSide.P1 ? FateSide.P2 : FateSide.P1;
            SetFate(next);
        }
    }
}

