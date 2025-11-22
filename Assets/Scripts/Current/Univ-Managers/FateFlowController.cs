using System;
using UnityEngine;

namespace CardGame.Managers
{
    public enum FateSide
    {
        Player,
        Opponent
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

            CurrentFate = startingSide;
        }

        private void Start()
        {
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
            FateSide next = CurrentFate == FateSide.Player ? FateSide.Opponent : FateSide.Player;
            SetFate(next);
        }
    }
}

