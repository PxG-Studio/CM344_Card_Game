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
            
            // Get or add CardMover component
            CardMover cardMover = boardCard.GetComponent<CardMover>();
            if (cardMover == null)
            {
                cardMover = boardCard.GetComponentInChildren<CardMover>();
            }
            
            if (cardMover != null)
            {
                cardMover.SetCard(card);
                cardMover.RefreshHomePosition();
            }
            else
            {
                Debug.LogWarning($"CardFactory.CreateBoardCard: Board card prefab '{prefab.name}' has no CardMover component. Card may not be draggable.");
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

