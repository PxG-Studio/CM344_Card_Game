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
                return null;
            }
            
            if (prefab == null)
            {
                return null;
            }
            
            if (parent == null)
            {
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
                Object.Destroy(cardUI.gameObject);
                return null;
            }
            
            
            return cardUI;
        }
        
        /// <summary>
        /// Creates a board card (2D world-space card with CardMover) from a card.
        /// Used when placing a UI card onto the board.
        /// </summary>
        /// <param name="card">The card data</param>
        /// <param name="prefab">The board card prefab (should have CardMoverP1 or CardMoverP2 component)</param>
        /// <param name="position">World position to place the card</param>
        /// <returns>Initialized board card GameObject</returns>
        public static GameObject CreateBoardCard(NewCard card, GameObject prefab, Vector3 position)
        {
            if (card == null || prefab == null)
            {
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
                #endif
            }
            
            // [CardFront] Support both CardMover (P1) and CardMoverP2 (P2)
            // Check for CardMoverP2 first (P2 cards), then CardMover (P1 cards)
            CardMoverP2 cardMoverP2 = boardCard.GetComponent<CardMoverP2>();
            if (cardMoverP2 == null)
            {
                cardMoverP2 = boardCard.GetComponentInChildren<CardMoverP2>();
            }
            
            if (cardMoverP2 != null)
            {
                // P2 card - use CardMoverP2
                cardMoverP2.SetCard(card);
                cardMoverP2.RefreshHomePosition();
            }
            else
            {
                // P1 card - use CardMoverP1
                CardMoverP1 cardMover = boardCard.GetComponent<CardMoverP1>();
                if (cardMover == null)
                {
                    cardMover = boardCard.GetComponentInChildren<CardMoverP1>();
                }
                
                if (cardMover != null)
                {
                    cardMover.SetCard(card);
                    cardMover.RefreshHomePosition();
                }
                else
                {
                    // Neither component found - log warning
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
            
            return boardCard;
        }
    }
}

