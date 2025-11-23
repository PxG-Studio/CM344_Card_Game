using UnityEngine;
using CardGame.Core;
using CardGame.UI;
using CardGame.Managers;
using NewCardData;

namespace CardGame.Factories
{
    /// <summary>
    /// Centralized factory for creating card UI instances.
    /// Ensures consistent initialization order: Instantiate -> Initialize -> Start
    /// </summary>
    public static class CardFactory
    {
        /// <summary>
        /// Creates and initializes a card UI instance.
        /// CRITICAL: Initialize() is called immediately after instantiation, BEFORE Start() runs.
        /// </summary>
        /// <param name="card">The card data to display</param>
        /// <param name="prefab">The card prefab to instantiate</param>
        /// <param name="parent">Parent transform for the card</param>
        /// <param name="revealDelay">Optional reveal delay for flip animation</param>
        /// <returns>Initialized NewCardUI instance with card data bound</returns>
        public static NewCardUI CreateCardUI(NewCard card, NewCardUI prefab, Transform parent, float revealDelay = 0f)
        {
            if (card == null)
            {
                Debug.LogError("CardFactory.CreateCardUI: Cannot create card UI with null card data!");
                return null;
            }
            
            if (prefab == null)
            {
                Debug.LogError("CardFactory.CreateCardUI: Cannot create card UI with null prefab!");
                return null;
            }
            
            if (parent == null)
            {
                Debug.LogError("CardFactory.CreateCardUI: Cannot create card UI with null parent!");
                return null;
            }
            
            // Instantiate the prefab
            NewCardUI cardUI = Object.Instantiate(prefab, parent);
            
            // [CardFront] CRITICAL: Ensure cloned card GameObject is ACTIVE
            // Cloned cards must be active for coroutines and interactions to work
            // Note: Sometimes Unity instantiates inactive even if prefab is active (known Unity quirk)
            if (!cardUI.gameObject.activeSelf)
            {
                cardUI.gameObject.SetActive(true);
                // Only log in Editor mode to reduce runtime warning spam
                #if UNITY_EDITOR
                Debug.Log($"[CardFactory] Activated cloned card '{cardUI.gameObject.name}' (was instantiated inactive). This is normal if prefab was in scene hierarchy.");
                #endif
            }
            
            // [CardFront] CRITICAL: Ensure cloned card CanvasGroup is INTERACTIVE
            // Cards must be interactive to receive drag events and be clickable
            CanvasGroup cg = cardUI.GetComponent<CanvasGroup>();
            if (cg == null)
            {
                cg = cardUI.gameObject.AddComponent<CanvasGroup>();
            }
            cg.interactable = true;
            cg.blocksRaycasts = true;
            
            // Set reveal delay BEFORE Initialize() if needed
            if (revealDelay > 0f && cardUI.autoFlipOnReveal)
            {
                cardUI.revealDelay = revealDelay;
            }
            
            // CRITICAL: Initialize immediately after instantiation, BEFORE Unity calls Start()
            // This ensures card data is bound before any Start() methods run
            cardUI.Initialize(card);
            
            // Verify initialization succeeded
            if (cardUI.Card == null)
            {
                Debug.LogError($"CardFactory.CreateCardUI: Failed to initialize card UI for '{card.Data?.cardName ?? "UNKNOWN"}'. Card reference is null after Initialize().");
                Object.Destroy(cardUI.gameObject);
                return null;
            }
            
            Debug.Log($"CardFactory: Created and initialized card UI '{card.Data.cardName}' (InstanceID: {card.InstanceID})");
            
            return cardUI;
        }
        
        /// <summary>
        /// Creates a board card (2D world-space card with CardMover) from a card.
        /// Used when placing a UI card onto the board.
        /// </summary>
        /// <param name="card">The card data</param>
        /// <param name="prefab">The board card prefab (should have CardMover component)</param>
        /// <param name="position">World position to place the card</param>
        /// <returns>Initialized board card GameObject</returns>
        public static GameObject CreateBoardCard(NewCard card, GameObject prefab, Vector3 position)
        {
            if (card == null || prefab == null)
            {
                Debug.LogError("CardFactory.CreateBoardCard: Cannot create board card with null card or prefab!");
                return null;
            }
            
            GameObject boardCard = Object.Instantiate(prefab, position, Quaternion.identity);
            boardCard.name = card.Data.cardName;
            
            // [CardFront] CRITICAL: Ensure board card is active (for visibility and interactions)
            if (!boardCard.activeSelf)
            {
                boardCard.SetActive(true);
                // Only log in Editor mode to reduce runtime warning spam
                #if UNITY_EDITOR
                Debug.Log($"[CardFactory] Activated board card '{boardCard.name}' (was instantiated inactive). This is normal if prefab was in scene hierarchy.");
                #endif
            }
            
            // [CardFront] Support both CardMover (Player 1) and CardMoverOpp (Player 2)
            // Check for CardMoverOpp first (opponent cards), then CardMover (player cards)
            CardMoverOpp cardMoverOpp = boardCard.GetComponent<CardMoverOpp>();
            if (cardMoverOpp == null)
            {
                cardMoverOpp = boardCard.GetComponentInChildren<CardMoverOpp>();
            }
            
            if (cardMoverOpp != null)
            {
                // Opponent card - use CardMoverOpp (Player 2)
                cardMoverOpp.SetCard(card);
                cardMoverOpp.RefreshHomePosition();
                Debug.Log($"[CardFactory] Created board card '{card.Data.cardName}' with CardMoverOpp (opponent card)");
            }
            else
            {
                // Player card - use CardMover (Player 1)
                CardMover cardMover = boardCard.GetComponent<CardMover>();
                if (cardMover == null)
                {
                    cardMover = boardCard.GetComponentInChildren<CardMover>();
                }
                
                if (cardMover != null)
                {
                    cardMover.SetCard(card);
                    cardMover.RefreshHomePosition();
                    Debug.Log($"[CardFactory] Created board card '{card.Data.cardName}' with CardMover (player card)");
                }
                else
                {
                    // Neither component found - log warning
                    Debug.LogWarning($"[CardFactory] Board card prefab '{prefab.name}' has neither CardMover nor CardMoverOpp component. Card may not be draggable. Please add the appropriate mover component to the prefab.");
                }
            }
            
            // Get or add NewCardUI component for visuals
            NewCardUI cardUI = boardCard.GetComponent<NewCardUI>();
            if (cardUI == null)
            {
                cardUI = boardCard.GetComponentInChildren<NewCardUI>();
            }
            
            if (cardUI != null)
            {
                cardUI.Initialize(card);
            }
            
            Debug.Log($"CardFactory: Created board card '{card.Data.cardName}' at position {position}");
            
            return boardCard;
        }
    }
}

