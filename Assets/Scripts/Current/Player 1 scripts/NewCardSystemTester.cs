using UnityEngine;
using CardGame.Managers;
using CardGame.UI;

namespace CardGame.Testing
{
    /// <summary>
    /// Test script to easily initialize and test the NewCard system
    /// Attach this to a GameObject in your scene to test the card system
    /// </summary>
    public class NewCardSystemP1Tester : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private NewDeckManagerP1 deckManager;
        [SerializeField] private NewHandP1UI handUI;
        
        [Header("Test Settings")]
        [SerializeField] private bool autoInitializeOnStart = true;
        [SerializeField] private bool autoDrawCardsOnStart = true;
        [SerializeField] private int cardsToDraw = 5;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugButtons = true;
        
        private void Start()
        {
            // Auto-find components if not assigned
            if (deckManager == null)
                deckManager = FindObjectOfType<NewDeckManagerP1>();
            
            if (handUI == null)
                handUI = FindObjectOfType<NewHandP1UI>();
            
            if (autoInitializeOnStart && deckManager != null)
            {
                InitializeDeck();
                
                if (autoDrawCardsOnStart)
                {
                    // Wait for coin toss to complete before drawing cards
                    // GameManager will trigger card drawing after coin toss completes
                    StartCoroutine(WaitForCoinTossThenDrawCards());
                }
            }
        }
        
        private System.Collections.IEnumerator WaitForCoinTossThenDrawCards()
        {
            // Wait for all required managers to be initialized
            yield return new WaitUntil(() => CoinTossManager.Instance != null);
            yield return new WaitUntil(() => GameManager.Instance != null);
            yield return new WaitUntil(() => FindObjectOfType<HUDManager>() != null);
            
            // Additional frame to allow canvases to initialize
            yield return null;
            yield return null;
            
            // Get instance with null check - it may become null even after WaitUntil
            CoinTossManager coinTossManager = CoinTossManager.Instance;
            
            if (coinTossManager == null)
            {
                Debug.LogWarning("[NewCardSystemTester] CoinTossManager not found after wait. Proceeding with card draw anyway.");
                yield return new WaitForSeconds(0.5f);
                DrawInitialCards();
                yield break;
            }
            
            // Wait for coin toss to complete
            float waitTime = 0f;
            float maxWaitTime = 15f; // Increased timeout to account for selection step
            
            while (waitTime < maxWaitTime)
            {
                // Re-check instance each iteration in case it was destroyed
                coinTossManager = CoinTossManager.Instance;
                
                if (coinTossManager == null)
                {
                    Debug.LogWarning("[NewCardSystemTester] CoinTossManager became null during wait. Proceeding with card draw.");
                    break;
                }
                
                if (coinTossManager.IsComplete)
                {
                    break;
                }
                
                yield return new WaitForSeconds(0.1f);
                waitTime += 0.1f;
            }
            
            // Additional delay to ensure game is ready
            yield return new WaitForSeconds(0.5f);
            
            // Now draw cards
            DrawInitialCards();
        }
        
        public void InitializeDeck()
        {
            if (deckManager == null)
            {
                Debug.LogError("NewCardSystemTester: DeckManager not found!");
                return;
            }
            
            deckManager.InitializeDeck();
            Debug.Log("Deck initialized!");
        }
        
        public void DrawInitialCards()
        {
            if (deckManager == null)
            {
                Debug.LogError("NewCardSystemTester: DeckManager not found!");
                return;
            }
            
            deckManager.DrawCards(cardsToDraw);
            Debug.Log($"Drew {cardsToDraw} cards!");
        }
        
        public void DrawOneCard()
        {
            if (deckManager == null)
            {
                Debug.LogError("NewCardSystemTester: DeckManager not found!");
                return;
            }
            
            deckManager.DrawCard();
        }
        
        public void ShuffleDeck()
        {
            if (deckManager == null)
            {
                Debug.LogError("NewCardSystemTester: DeckManager not found!");
                return;
            }
            
            deckManager.ShuffleDeck();
        }
        
        public void ClearHand()
        {
            if (handUI == null)
            {
                Debug.LogError("NewCardSystemTester: HandUI not found!");
                return;
            }
            
            handUI.ClearHand();
            
            // Also clear all cards from the board
            CardMoverP1[] boardCards = FindObjectsOfType<CardMoverP1>();
            foreach (CardMoverP1 card in boardCards)
            {
                if (card.IsPlayed)
                {
                    Destroy(card.gameObject);
                }
            }
            
            Debug.Log("Hand and board cleared!");
        }
        
        // [CardFront] Debug GUI - Editor-only, disabled in builds
        #if UNITY_EDITOR
        private void OnGUI()
        {
            // [CardFront] Only show in Editor Play Mode, never in builds
            if (!showDebugButtons || !Application.isPlaying) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 200, 300));
            GUILayout.Box("NewCard System Tester");
            
            if (GUILayout.Button("Initialize Deck"))
            {
                InitializeDeck();
            }
            
            if (GUILayout.Button($"Draw {cardsToDraw} Cards"))
            {
                DrawInitialCards();
            }
            
            if (GUILayout.Button("Draw 1 Card"))
            {
                DrawOneCard();
            }
            
            if (GUILayout.Button("Shuffle Deck"))
            {
                ShuffleDeck();
            }
            
            if (GUILayout.Button("Clear Hand"))
            {
                ClearHand();
            }
            
            if (deckManager != null)
            {
                GUILayout.Space(10);
                GUILayout.Label($"Draw Pile: {deckManager.DrawPileCount}");
                GUILayout.Label($"Hand: {deckManager.Hand.Count}");
                GUILayout.Label($"Played: {deckManager.DiscardPileCount}");
            }
            
            GUILayout.EndArea();
        }
        #endif
    }
}

