using System;
using UnityEngine;

namespace CardGame.Managers
{
    public enum FateSide
    {
        Player = 0, // P1
        P2 = 1      // Renamed from Opponent for P1/P2 consistency
    }

    /// <summary>
    /// Central controller for the Fatebound turn flow. Tracks whose Fate Window is active and raises updates.
    /// </summary>
    public class FateFlowController : MonoBehaviour
    {
        public static FateFlowController Instance { get; private set; }

        [SerializeField] private FateSide startingSide = FateSide.Player;

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
                CurrentFate = CoinTossManager.Instance.GetStartingPlayer();
            }
            
            OnFateChanged?.Invoke(CurrentFate);
        }

        public bool CanAct(FateSide side) => CurrentFate == side;

        public void SetFate(FateSide side)
        {
            if (CurrentFate == side) return;

            CurrentFate = side;
            OnFateChanged?.Invoke(CurrentFate);
        }

        public void AdvanceFateFlow()
        {
            FateSide next = CurrentFate == FateSide.Player ? FateSide.P2 : FateSide.Player;
            SetFate(next);
        }
    }
}

