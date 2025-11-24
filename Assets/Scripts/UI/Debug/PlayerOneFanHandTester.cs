using UnityEngine;
using CardGame.Managers;
using CardGame.UI;

namespace CardGame.UI.Debugging
{
    /// <summary>
    /// Simple harness to quickly validate the P1 fan-hand UI in play mode.
    /// Attach this to a scene object (ideally P1HandContainer) and it will
    /// initialize the deck and draw a configurable number of cards on start.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Testing/Player One Fan Hand Tester")]
    public class PlayerOneFanHandTester : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private NewDeckManagerP1 deckManager;
        [SerializeField] private PlayerHandController handController;

        [Header("Test Settings")]
        [SerializeField] private bool initializeDeckOnStart = true;
        [SerializeField] private bool drawCardsOnStart = true;
        [SerializeField, Min(1)] private int cardsToDraw = 5;

        private void Awake()
        {
            AutoAssignReferences();
        }

        private void Start()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (drawCardsOnStart)
            {
                RunTest();
            }
        }

        [ContextMenu("Run Fan Hand Test")]
        public void RunTest()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[PlayerOneFanHandTester] Enter Play Mode to run the test.");
                return;
            }

            if (deckManager == null)
            {
                Debug.LogError("[PlayerOneFanHandTester] Deck manager reference missing.");
                return;
            }

            if (initializeDeckOnStart)
            {
                deckManager.InitializeDeck();
            }

            deckManager.DrawCards(Mathf.Max(1, cardsToDraw));
            Debug.Log($"[PlayerOneFanHandTester] Drew {cardsToDraw} cards for Player 1.");
        }

        private void AutoAssignReferences()
        {
            if (deckManager == null)
            {
                deckManager = FindObjectOfType<NewDeckManagerP1>();
            }

            if (handController == null)
            {
                handController = GetComponent<PlayerHandController>() ?? FindObjectOfType<PlayerHandController>();
            }
        }
    }
}

