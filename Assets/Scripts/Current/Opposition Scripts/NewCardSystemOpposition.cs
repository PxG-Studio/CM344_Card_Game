using UnityEngine;
using CardGame.Managers;
using CardGame.UI;

namespace CardGame.Testing
{
    /// <summary>
    /// Test script to easily initialize and test the NewCard system for P2
    /// Attach this to a GameObject in your scene to test the card system
    /// </summary>
    public class NewCardSystemP2 : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private NewDeckManagerP2 deckManager;
        [SerializeField] private NewHandP2UI handUI;
        
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
                deckManager = FindObjectOfType<NewDeckManagerP2>();
            
            if (handUI == null)
                handUI = FindObjectOfType<NewHandP2UI>();
            
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
                Debug.LogWarning("[NewCardSystemP2] CoinTossManager not found after wait. Proceeding with card draw anyway.");
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
                    Debug.LogWarning("[NewCardSystemP2] CoinTossManager became null during wait. Proceeding with card draw.");
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
            CardMoverP2[] boardCards = FindObjectsOfType<CardMoverP2>();
            foreach (CardMoverP2 card in boardCards)
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

                // Original input coordinates (left-based)
            float x = 10;
            float y = 10;
            float w = 200;
            float h = 300;

                // Convert to right-side positioning
            float flippedX = Screen.width - w - x;

            GUILayout.BeginArea(new Rect(flippedX, y, w, h));
            GUILayout.Box("NewCard System Test P2");
            
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
                
                // Create right-aligned style for labels
                GUIStyle rightAlignedStyle = new GUIStyle(GUI.skin.label);
                rightAlignedStyle.alignment = TextAnchor.MiddleRight;
                
                GUILayout.Label($"Draw Pile: {deckManager.DrawPileCount}", rightAlignedStyle);
                GUILayout.Label($"Hand: {deckManager.Hand.Count}", rightAlignedStyle);
                GUILayout.Label($"Played: {deckManager.DiscardPileCount}", rightAlignedStyle);
            }
            
            GUILayout.EndArea();
        }
        #endif
    }
}

