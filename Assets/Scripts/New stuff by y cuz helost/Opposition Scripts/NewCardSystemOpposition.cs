using UnityEngine;
using CardGame.Managers;
using CardGame.UI;

namespace CardGame.Testing
{
    /// <summary>
    /// Test script to easily initialize and test the NewCard system
    /// Attach this to a GameObject in your scene to test the card system
    /// </summary>
    public class NewCardSystemOpposition : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private NewDeckManagerOpp deckManager;
        [SerializeField] private NewHandOppUI handUI;
        
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
                deckManager = FindObjectOfType<NewDeckManagerOpp>();
            
            if (handUI == null)
                handUI = FindObjectOfType<NewHandOppUI>();
            
            if (autoInitializeOnStart && deckManager != null)
            {
                InitializeDeck();
                
                if (autoDrawCardsOnStart)
                {
                    // Small delay to ensure everything is set up
                    Invoke(nameof(DrawInitialCards), 0.1f);
                }
            }
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
            Debug.Log("Hand cleared!");
        }
        
        // Debug GUI (only in editor)
        private void OnGUI()
        {
             if (!showDebugButtons || !Application.isPlaying) return;

                // Original input coordinates (left-based)
            float x = 10;
            float y = 10;
            float w = 200;
            float h = 300;

                // Convert to right-side positioning
            float flippedX = Screen.width - w - x;

            GUILayout.BeginArea(new Rect(flippedX, y, w, h));
            GUILayout.Box("NewCard System Test Opp");
            
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
    }
}

