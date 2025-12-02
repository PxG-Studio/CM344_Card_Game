using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CardGame.Core;
using CardGame.UI;
using CardGame.Managers;
using NewCardData;

namespace CardGame.Tests
{
    /// <summary>
    /// Helper utilities for card testing - creates test cards, places them, validates captures, etc.
    /// </summary>
    public static class CardTestHelper
    {
        /// <summary>
        /// Creates a test card with specified directional stats
        /// </summary>
        public static NewCard CreateTestCard(int top, int right, int down, int left, string cardName = "TestCard")
        {
            // Create a ScriptableObject instance for testing
            NewCardData.NewCardData cardData = ScriptableObject.CreateInstance<NewCardData.NewCardData>();
            
            // Set public fields directly (they are public in NewCardData)
            cardData.TopStat = top;
            cardData.RightStat = right;
            cardData.DownStat = down;
            cardData.LeftStat = left;
            cardData.cardName = cardName;
            cardData.cardType = NewCardData.CardType.Flame; // Default type
            
            NewCard card = new NewCard(cardData);
            return card;
        }

        /// <summary>
        /// Places a P1 card on a drop area using AutomationAttemptDrop
        /// </summary>
        public static bool PlaceP1CardOnDropArea(CardMoverP1 cardMoverP1, CardDropArea dropArea, bool bypassTurnCheck = true)
        {
            if (cardMoverP1 == null || dropArea == null) return false;
            
            Vector3 dropPosition = dropArea.transform.position;
            return cardMoverP1.AutomationAttemptDrop(dropPosition, bypassTurnCheck);
        }

        /// <summary>
        /// Places a P2 card on a drop area
        /// </summary>
        public static bool PlaceP2CardOnDropArea(CardMoverP2 cardMoverP2, CardDropArea dropArea, bool bypassTurnCheck = true)
        {
            if (cardMoverP2 == null || dropArea == null) return false;
            
            // Ensure collider is set before attempting drop (Unity might have called Start() and reset it)
            EnsureColliderSet(cardMoverP2);
            
            Vector3 dropPosition = dropArea.transform.position;
            return cardMoverP2.AutomationAttemptDrop(dropPosition, bypassTurnCheck);
        }
        
        /// <summary>
        /// Ensures the collider field is set on CardMoverP2 (Unity's Start() might reset it)
        /// </summary>
        private static void EnsureColliderSet(CardMoverP2 mover)
        {
            if (mover == null) return;
            
            var colField = typeof(CardMoverP2).GetField("col", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (colField == null) return;
            
            // Check if collider field is null
            var currentCol = colField.GetValue(mover) as Collider2D;
            if (currentCol == null)
            {
                // Try to find existing collider
                Collider2D foundCol = mover.GetComponent<Collider2D>();
                if (foundCol == null)
                {
                    foundCol = mover.GetComponentInChildren<Collider2D>();
                }
                
                // If no collider exists, add one
                if (foundCol == null)
                {
                    foundCol = mover.gameObject.AddComponent<BoxCollider2D>();
                    foundCol.isTrigger = true;
                    foundCol.enabled = true;
                }
                
                // Set the field
                if (foundCol != null)
                {
                    colField.SetValue(mover, foundCol);
                }
            }
        }

        /// <summary>
        /// Adds a card to a deck manager's hand using reflection
        /// </summary>
        public static void AddCardToDeckManagerHand(NewDeckManagerP1 deckManager, NewCard card)
        {
            if (deckManager == null || card == null) return;
            
            var handField = typeof(NewDeckManagerP1).GetField("hand", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (handField != null)
            {
                var hand = handField.GetValue(deckManager);
                var addCardMethod = hand.GetType().GetMethod("AddCard");
                if (addCardMethod != null)
                {
                    addCardMethod.Invoke(hand, new object[] { card });
                    // Trigger OnCardDrawn event so hand UI updates
                    deckManager.OnCardDrawn?.Invoke(card);
                }
            }
        }
        
        /// <summary>
        /// Adds a card to a P2 deck manager's hand using reflection
        /// </summary>
        public static void AddCardToDeckManagerHand(NewDeckManagerP2 deckManager, NewCard card)
        {
            if (deckManager == null || card == null) return;
            
            var handField = typeof(NewDeckManagerP2).GetField("hand", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (handField != null)
            {
                var hand = handField.GetValue(deckManager);
                var addCardMethod = hand.GetType().GetMethod("AddCard");
                if (addCardMethod != null)
                {
                    addCardMethod.Invoke(hand, new object[] { card });
                    // Trigger OnCardDrawn event so hand UI updates
                    deckManager.OnCardDrawn?.Invoke(card);
                }
            }
        }

        /// <summary>
        /// Checks if a card is captured (flipped to back)
        /// </summary>
        public static bool IsCardCaptured(GameObject cardObject)
        {
            if (cardObject == null) return false;
            
            CardFlipAnimation flipAnim = cardObject.GetComponentInChildren<CardFlipAnimation>();
            if (flipAnim == null)
            {
                // Try to get from parent or self
                flipAnim = cardObject.GetComponent<CardFlipAnimation>();
            }
            if (flipAnim == null) return false;
            
            // Note: WasCaptured and LastCaptureColor properties removed in develop-1 revert
            // Use isFlipped and check for capture color via NewCardUI instead
            // isFlipped can be true for cards that are face-down but not captured (e.g., P2 cards initially)
            // So we need to check the card's capture state via NewCardUI or other means
            
            // Final fallback: check if back container is active AND card has a capture color
            // This is less reliable but better than just checking isFlipped
            // CRITICAL: We must check lastCaptureColor here too, because P2 cards start with back showing
            // but haven't been captured yet
            var backContainerField = typeof(CardFlipAnimation).GetField("backContainer", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (backContainerField != null)
            {
                GameObject backContainer = backContainerField.GetValue(flipAnim) as GameObject;
                if (backContainer != null && backContainer.activeSelf)
                {
                    // Check if there's actually a capture color set (lastCaptureColor != clear)
                    // P2 cards start with back showing but haven't been captured, so we need this check
                    var lastCaptureColorField = typeof(CardFlipAnimation).GetField("lastCaptureColor", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (lastCaptureColorField != null)
                    {
                        Color internalLastCaptureColor = (Color)lastCaptureColorField.GetValue(flipAnim);
                        // Only consider it captured if back is showing AND there's a capture color
                        return internalLastCaptureColor != Color.clear;
                    }
                    // If we can't check lastCaptureColor, don't assume captured (P2 cards start with back showing)
                    return false;
                }
            }
            
            return false;
        }

        /// <summary>
        /// Gets the adjacent drop area in a specific direction
        /// </summary>
        public static CardDropArea GetAdjacentDropArea(CardDropArea fromArea, string direction)
        {
            CardDropArea[] allAreas = Object.FindObjectsOfType<CardDropArea>();
            Vector3 fromPos = fromArea.transform.position;
            
            float searchDistance = 3.5f; // Slightly larger than adjacentCardDistance
            
            foreach (CardDropArea area in allAreas)
            {
                if (area == fromArea) continue;
                
                Vector3 delta = area.transform.position - fromPos;
                float distance = Vector3.Distance(fromPos, area.transform.position);
                
                if (distance > searchDistance) continue;
                
                switch (direction.ToLower())
                {
                    case "right":
                        if (delta.x > 0.1f && Mathf.Abs(delta.y) < 0.5f) return area;
                        break;
                    case "left":
                        if (delta.x < -0.1f && Mathf.Abs(delta.y) < 0.5f) return area;
                        break;
                    case "top":
                    case "up":
                        if (delta.y > 0.1f && Mathf.Abs(delta.x) < 0.5f) return area;
                        break;
                    case "down":
                    case "bottom":
                        if (delta.y < -0.1f && Mathf.Abs(delta.x) < 0.5f) return area;
                        break;
                }
            }
            
            return null;
        }

        /// <summary>
        /// Creates a CardMoverP1 GameObject with a test card
        /// </summary>
        public static CardMoverP1 CreateCardMoverWithCard(NewCard card, Vector3 position, bool isPlayerCard = true)
        {
            // Find prefab or create GameObject
            GameObject cardPrefab = GameObject.Find("NewCardPrefab");
            if (cardPrefab == null)
            {
                // Create a minimal card GameObject
                GameObject cardObj = new GameObject($"TestCard_{card.Data.cardName}");
                cardObj.transform.position = position;
                
                // Add Collider2D FIRST (before CardMoverP1 so Start() can find it)
                Collider2D col = cardObj.AddComponent<BoxCollider2D>();
                col.isTrigger = true;
                col.enabled = true;
                
                CardMoverP1 cardMoverP1 = cardObj.AddComponent<CardMoverP1>();
                cardMoverP1.SetCard(card);
                
                // Ensure Start() is called to initialize the col reference
                var startMethod = typeof(CardMoverP1).GetMethod("Start", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (startMethod != null)
                {
                    startMethod.Invoke(cardMoverP1, null);
                }
                
                // Manually set collider reference if Start() didn't work
                var colField = typeof(CardMoverP1).GetField("col", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (colField != null && colField.GetValue(cardMoverP1) == null)
                {
                    colField.SetValue(cardMoverP1, col);
                }
                
                // Add NewCardUI component for capture/flip functionality
                CardGame.UI.NewCardUI cardUI = cardObj.AddComponent<CardGame.UI.NewCardUI>();
                
                // Create front and back containers for flip animation (required for CardFlipAnimation)
                GameObject frontContainer = new GameObject("FrontContainer");
                frontContainer.transform.SetParent(cardObj.transform);
                frontContainer.SetActive(true);
                
                GameObject backContainer = new GameObject("BackContainer");
                backContainer.transform.SetParent(cardObj.transform);
                backContainer.SetActive(false);
                
                // Set containers using reflection before Awake() is called
                var frontContainerField = typeof(CardGame.UI.NewCardUI).GetField("frontContainer",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var backContainerField = typeof(CardGame.UI.NewCardUI).GetField("backContainer",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (frontContainerField != null)
                {
                    frontContainerField.SetValue(cardUI, frontContainer);
                }
                if (backContainerField != null)
                {
                    backContainerField.SetValue(cardUI, backContainer);
                }
                
                // Ensure Awake() is called before Initialize() to set up containers and CardFlipAnimation
                var awakeMethod = typeof(CardGame.UI.NewCardUI).GetMethod("Awake", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (awakeMethod != null)
                {
                    awakeMethod.Invoke(cardUI, null);
                }
                cardUI.Initialize(card);
                
                return cardMoverP1;
            }
            
            // Use prefab if available
            GameObject cardInstance = Object.Instantiate(cardPrefab, position, Quaternion.identity);
            CardMoverP1 mover = cardInstance.GetComponent<CardMoverP1>();
            if (mover != null)
            {
                mover.SetCard(card);
                
                // Ensure collider exists on prefab instance
                Collider2D prefabCol = cardInstance.GetComponent<Collider2D>();
                if (prefabCol == null)
                {
                    prefabCol = cardInstance.GetComponentInChildren<Collider2D>();
                }
                if (prefabCol == null)
                {
                    // Add collider if missing
                    prefabCol = cardInstance.AddComponent<BoxCollider2D>();
                    prefabCol.isTrigger = true;
                    prefabCol.enabled = true;
                }
                
                // Ensure Start() is called
                var startMethod = typeof(CardMoverP1).GetMethod("Start", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (startMethod != null)
                {
                    startMethod.Invoke(mover, null);
                }
                
                // Manually set collider reference if Start() didn't work
                var colField = typeof(CardMoverP1).GetField("col", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (colField != null && colField.GetValue(mover) == null && prefabCol != null)
                {
                    colField.SetValue(mover, prefabCol);
                }
                
                // Ensure NewCardUI is initialized with card data
                CardGame.UI.NewCardUI cardUI = cardInstance.GetComponent<CardGame.UI.NewCardUI>();
                if (cardUI != null)
                {
                    // Ensure Awake() has been called (if not already)
                    var awakeMethod = typeof(CardGame.UI.NewCardUI).GetMethod("Awake", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (awakeMethod != null && !cardInstance.activeInHierarchy)
                    {
                        // If GameObject is inactive, Awake() won't be called automatically
                        awakeMethod.Invoke(cardUI, null);
                    }
                    cardUI.Initialize(card);
                }
                else
                {
                    // Add NewCardUI if missing
                    cardUI = cardInstance.AddComponent<CardGame.UI.NewCardUI>();
                    var awakeMethod = typeof(CardGame.UI.NewCardUI).GetMethod("Awake", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (awakeMethod != null)
                    {
                        awakeMethod.Invoke(cardUI, null);
                    }
                    cardUI.Initialize(card);
                }
            }
            
            return mover;
        }

        /// <summary>
        /// Creates a CardMoverP2 GameObject with a test card
        /// </summary>
        public static CardMoverP2 CreateCardMoverP2WithCard(NewCard card, Vector3 position)
        {
            GameObject cardPrefab = GameObject.Find("NewCardPrefabOpp");
            if (cardPrefab == null)
            {
                GameObject cardObj = new GameObject($"TestCardOpp_{card.Data.cardName}");
                cardObj.transform.position = position;
                
                // Add Collider2D FIRST (before CardMoverP2 so Start() can find it)
                Collider2D col = cardObj.AddComponent<BoxCollider2D>();
                col.isTrigger = true;
                col.enabled = true;
                
                CardMoverP2 cardMoverP2 = cardObj.AddComponent<CardMoverP2>();
                cardMoverP2.SetCard(card);
                
                // Ensure Start() is called to initialize the col reference
                // In EditMode/PlayMode tests, Start() may not be called automatically
                var startMethod = typeof(CardMoverP2).GetMethod("Start", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (startMethod != null)
                {
                    startMethod.Invoke(cardMoverP2, null);
                }
                
                // ALWAYS manually set collider reference to ensure it's set correctly
                // Unity might call Start() again later, which could reset it to null
                var colField = typeof(CardMoverP2).GetField("col", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (colField != null)
                {
                    // Force set it to the collider we created
                    colField.SetValue(cardMoverP2, col);
                    
                    // Verify it was set correctly
                    var setCol = colField.GetValue(cardMoverP2) as Collider2D;
                    if (setCol == null)
                    {
                        Debug.LogError($"[CardTestHelper] Failed to set collider on CardMoverP2 for '{card.Data.cardName}'. " +
                            $"Collider exists: {col != null}, Field exists: {colField != null}");
                    }
                }
                
                // Add NewCardUI component for capture/flip functionality
                CardGame.UI.NewCardUI cardUI = cardObj.AddComponent<CardGame.UI.NewCardUI>();
                
                // Create front and back containers for flip animation (required for CardFlipAnimation)
                GameObject frontContainer = new GameObject("FrontContainer");
                frontContainer.transform.SetParent(cardObj.transform);
                frontContainer.SetActive(true);
                
                GameObject backContainer = new GameObject("BackContainer");
                backContainer.transform.SetParent(cardObj.transform);
                backContainer.SetActive(false);
                
                // Set containers using reflection before Awake() is called
                var frontContainerField = typeof(CardGame.UI.NewCardUI).GetField("frontContainer",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var backContainerField = typeof(CardGame.UI.NewCardUI).GetField("backContainer",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (frontContainerField != null)
                {
                    frontContainerField.SetValue(cardUI, frontContainer);
                }
                if (backContainerField != null)
                {
                    backContainerField.SetValue(cardUI, backContainer);
                }
                
                // Ensure Awake() is called before Initialize() to set up containers and CardFlipAnimation
                var awakeMethod = typeof(CardGame.UI.NewCardUI).GetMethod("Awake", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (awakeMethod != null)
                {
                    awakeMethod.Invoke(cardUI, null);
                }
                cardUI.Initialize(card);
                
                return cardMoverP2;
            }
            
            GameObject cardInstance = Object.Instantiate(cardPrefab, position, Quaternion.identity);
            CardMoverP2 mover = cardInstance.GetComponent<CardMoverP2>();
            if (mover != null)
            {
                mover.SetCard(card);
                
                // Ensure collider exists on prefab instance
                Collider2D prefabCol = cardInstance.GetComponent<Collider2D>();
                if (prefabCol == null)
                {
                    prefabCol = cardInstance.GetComponentInChildren<Collider2D>();
                }
                if (prefabCol == null)
                {
                    // Add collider if missing
                    prefabCol = cardInstance.AddComponent<BoxCollider2D>();
                    prefabCol.isTrigger = true;
                    prefabCol.enabled = true;
                }
                
                // Ensure Start() is called
                var startMethod = typeof(CardMoverP2).GetMethod("Start", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (startMethod != null)
                {
                    startMethod.Invoke(mover, null);
                }
                
                // ALWAYS manually set collider reference to ensure it's set correctly
                // Unity might call Start() again later, which could reset it to null
                var colField = typeof(CardMoverP2).GetField("col", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (colField != null && prefabCol != null)
                {
                    // Force set it to the collider we found/created
                    colField.SetValue(mover, prefabCol);
                    
                    // Verify it was set correctly
                    var setCol = colField.GetValue(mover) as Collider2D;
                    if (setCol == null)
                    {
                        Debug.LogError($"[CardTestHelper] Failed to set collider on CardMoverP2 prefab instance for '{card.Data.cardName}'. " +
                            $"Collider exists: {prefabCol != null}, Field exists: {colField != null}");
                    }
                }
                
                // Ensure NewCardUI is initialized with card data
                CardGame.UI.NewCardUI cardUI = cardInstance.GetComponent<CardGame.UI.NewCardUI>();
                if (cardUI != null)
                {
                    cardUI.Initialize(card);
                }
            }
            else
            {
                // If mover is null, create a new one with collider
                mover = cardInstance.AddComponent<CardMoverP2>();
                mover.SetCard(card);
                
                Collider2D col = cardInstance.GetComponent<Collider2D>();
                if (col == null)
                {
                    col = cardInstance.AddComponent<BoxCollider2D>();
                    col.isTrigger = true;
                    col.enabled = true;
                }
                
                // Ensure Start() is called
                var startMethod = typeof(CardMoverP2).GetMethod("Start", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (startMethod != null)
                {
                    startMethod.Invoke(mover, null);
                }
                
                // Manually set collider reference
                var colField = typeof(CardMoverP2).GetField("col", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (colField != null && colField.GetValue(mover) == null && col != null)
                {
                    colField.SetValue(mover, col);
                }
                
                // Ensure NewCardUI is initialized with card data
                CardGame.UI.NewCardUI cardUI = cardInstance.GetComponent<CardGame.UI.NewCardUI>();
                if (cardUI != null)
                {
                    cardUI.Initialize(card);
                }
                else
                {
                    // Add NewCardUI if missing
                    cardUI = cardInstance.AddComponent<CardGame.UI.NewCardUI>();
                    cardUI.Initialize(card);
                }
            }
            
            // Final verification: ensure collider is set
            var finalColField = typeof(CardMoverP2).GetField("col", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (finalColField != null && finalColField.GetValue(mover) == null)
            {
                Collider2D finalCol = mover.GetComponent<Collider2D>();
                if (finalCol == null)
                {
                    finalCol = mover.GetComponentInChildren<Collider2D>();
                }
                if (finalCol == null)
                {
                    finalCol = mover.gameObject.AddComponent<BoxCollider2D>();
                    finalCol.isTrigger = true;
                    finalCol.enabled = true;
                }
                finalColField.SetValue(mover, finalCol);
            }
            
            // Ensure NewCardUI is initialized with card data (for capture/flip functionality)
            CardGame.UI.NewCardUI finalCardUI = mover.GetComponent<CardGame.UI.NewCardUI>();
            if (finalCardUI == null)
            {
                finalCardUI = mover.gameObject.AddComponent<CardGame.UI.NewCardUI>();
                // Ensure Awake() is called before Initialize() to set up containers
                var awakeMethod = typeof(CardGame.UI.NewCardUI).GetMethod("Awake", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (awakeMethod != null)
                {
                    awakeMethod.Invoke(finalCardUI, null);
                }
            }
            else if (!mover.gameObject.activeInHierarchy)
            {
                // If GameObject is inactive, Awake() won't be called automatically
                var awakeMethod = typeof(CardGame.UI.NewCardUI).GetMethod("Awake", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (awakeMethod != null)
                {
                    awakeMethod.Invoke(finalCardUI, null);
                }
            }
            finalCardUI.Initialize(card);
            
            return mover;
        }

        /// <summary>
        /// Waits for coin toss to complete with improved timeout and error handling
        /// Automatically makes a selection if none exists (for test environments)
        /// </summary>
        public static IEnumerator WaitForCoinTossToComplete(float timeout = 15f)
        {
            // Wait for CoinTossManager to be initialized
            float initWait = 0f;
            while (CoinTossManager.Instance == null && initWait < 5f)
            {
                yield return new WaitForSeconds(0.1f);
                initWait += 0.1f;
            }
            
            CoinTossManager coinTossManager = CoinTossManager.Instance;
            if (coinTossManager == null)
            {
                Debug.LogWarning("[CardTestHelper] CoinTossManager.Instance is null - coin toss may not be available");
                yield break;
            }
            
            // If already complete, return immediately
            if (coinTossManager.IsComplete)
            {
                yield break;
            }
            
            // CRITICAL: If no selection has been made, automatically make one for testing
            // This ensures the coin toss can proceed in test environments where there's no actual player input
            if (!coinTossManager.HasSelection)
            {
                Debug.Log("[CardTestHelper] No coin toss selection made. Automatically selecting 'Heads' for Player 1 (test environment).");
                coinTossManager.SetPlayerSelection(true, FateSide.Player); // Select heads for Player 1
                
                // Also trigger the selection in CoinTossUI if it exists
                CoinTossUI coinTossUI = Object.FindObjectOfType<CoinTossUI>(true);
                if (coinTossUI != null)
                {
                    // Get OnSelectionMade method via reflection to trigger selection flow
                    var onSelectionMadeMethod = typeof(CoinTossUI).GetMethod("OnSelectionMade", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    
                    if (onSelectionMadeMethod != null)
                    {
                        // Trigger selection (this activates coin image and starts animation)
                        onSelectionMadeMethod.Invoke(coinTossUI, new object[] { true }); // Select heads
                        Debug.Log("[CardTestHelper] Triggered coin toss selection via CoinTossUI.OnSelectionMade()");
                    }
                    else
                    {
                        // Fallback: manually start animation if selection method not accessible
                        coinTossUI.StartCoinTossAnimation();
                        Debug.Log("[CardTestHelper] Could not find OnSelectionMade method. Called StartCoinTossAnimation() directly.");
                    }
                }
                else
                {
                    // If no UI exists, perform coin toss directly
                    Debug.Log("[CardTestHelper] CoinTossUI not found. Performing coin toss directly via CoinTossManager.");
                    coinTossManager.PerformCoinToss();
                }
                
                yield return new WaitForEndOfFrame();
                yield return null;
            }
            
            // If we have a selection but coin toss isn't complete, wait a short time for animation to start
            // then force completion if it doesn't complete quickly
            if (coinTossManager.HasSelection && !coinTossManager.IsComplete)
            {
                // Wait a short time for animation to potentially complete
                float shortWait = 0f;
                float shortTimeout = 2f; // Give animation 2 seconds to complete
                while (!coinTossManager.IsComplete && shortWait < shortTimeout)
                {
                    yield return new WaitForSeconds(0.1f);
                    shortWait += 0.1f;
                }
                
                // If still not complete after short wait, force it
                if (!coinTossManager.IsComplete)
                {
                    Debug.Log("[CardTestHelper] Coin toss animation not completing. Performing coin toss directly to force completion...");
                    coinTossManager.PerformCoinToss();
                    yield return new WaitForSeconds(0.1f);
                }
            }
            
            // Wait for coin toss to complete (with full timeout as fallback)
            float elapsed = 0f;
            while (!coinTossManager.IsComplete && elapsed < timeout)
            {
                yield return new WaitForSeconds(0.1f);
                elapsed += 0.1f;
            }
            
            if (!coinTossManager.IsComplete)
            {
                Debug.LogWarning($"[CardTestHelper] Coin toss did not complete within {timeout} seconds. " +
                    $"HasSelection: {coinTossManager.HasSelection}, IsComplete: {coinTossManager.IsComplete}. " +
                    $"Attempting to force completion...");
                
                // Final attempt: If still not complete, try to force it
                if (coinTossManager.HasSelection && !coinTossManager.IsComplete)
                {
                    Debug.Log("[CardTestHelper] Selection exists but coin toss not complete. Performing coin toss directly...");
                    coinTossManager.PerformCoinToss();
                    yield return new WaitForSeconds(0.5f);
                }
                else if (!coinTossManager.HasSelection)
                {
                    // If no selection, make one and perform toss
                    Debug.Log("[CardTestHelper] No selection exists. Making selection and performing coin toss...");
                    coinTossManager.SetPlayerSelection(true, FateSide.Player);
                    coinTossManager.PerformCoinToss();
                    yield return new WaitForSeconds(0.5f);
                }
                
                if (!coinTossManager.IsComplete)
                {
                    Debug.LogWarning($"[CardTestHelper] Coin toss still not complete after force attempt. Proceeding with test anyway.");
                }
            }
        }

        /// <summary>
        /// Waits for capture animations to complete
        /// </summary>
        public static IEnumerator WaitForCaptureAnimations(float maxWaitTime = 5f)
        {
            // Wait for any active chain captures
            GameEndManager gameEndManager = GameEndManager.Instance;
            if (gameEndManager != null)
            {
                // Check if chains are in progress
                var chainsInProgressField = typeof(GameEndManager).GetField("chainsInProgress", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (chainsInProgressField != null)
                {
                    float elapsed = 0f;
                    while (elapsed < maxWaitTime)
                    {
                        bool inProgress = (bool)chainsInProgressField.GetValue(gameEndManager);
                        if (!inProgress) break;
                        
                        yield return new WaitForSeconds(0.1f);
                        elapsed += 0.1f;
                    }
                }
            }
            
            // Additional wait for flip animations (each flip takes ~1 second)
            yield return new WaitForSeconds(1.5f);
        }

        /// <summary>
        /// Gets the current score for a player
        /// </summary>
        public static int GetPlayerScore(bool isPlayer1)
        {
            ScoreManager scoreManager = ScoreManager.Instance;
            if (scoreManager == null) return 0;
            
            return isPlayer1 ? scoreManager.P1Score : scoreManager.P2Score;
        }

        /// <summary>
        /// Clears all singleton instances (for test isolation)
        /// CRITICAL: Call this before loading a scene to prevent DontDestroyOnLoad objects from interfering
        /// </summary>
        public static void ClearSingletonInstances()
        {
            // Destroy all singleton GameObjects
            // This ensures clean state between tests
            GameManager[] gameManagers = Object.FindObjectsOfType<GameManager>(true);
            foreach (GameManager gm in gameManagers)
            {
                if (gm != null) Object.DestroyImmediate(gm.gameObject);
            }
            
            ScoreManager[] scoreManagers = Object.FindObjectsOfType<ScoreManager>(true);
            foreach (ScoreManager sm in scoreManagers)
            {
                if (sm != null) Object.DestroyImmediate(sm.gameObject);
            }
            
            GameEndManager[] gameEndManagers = Object.FindObjectsOfType<GameEndManager>(true);
            foreach (GameEndManager gem in gameEndManagers)
            {
                if (gem != null) Object.DestroyImmediate(gem.gameObject);
            }
            
            FateFlowController[] fateControllers = Object.FindObjectsOfType<FateFlowController>(true);
            foreach (FateFlowController ffc in fateControllers)
            {
                if (ffc != null) Object.DestroyImmediate(ffc.gameObject);
            }
            
            CoinTossManager[] coinTossManagers = Object.FindObjectsOfType<CoinTossManager>(true);
            foreach (CoinTossManager ctm in coinTossManagers)
            {
                if (ctm != null) Object.DestroyImmediate(ctm.gameObject);
            }
            
            GameStatsTracker[] gameStatsTrackers = Object.FindObjectsOfType<GameStatsTracker>(true);
            foreach (GameStatsTracker gst in gameStatsTrackers)
            {
                if (gst != null) Object.DestroyImmediate(gst.gameObject);
            }
            
            // Clear static Instance fields via reflection
            ClearStaticInstance<GameManager>();
            ClearStaticInstance<ScoreManager>();
            ClearStaticInstance<GameEndManager>();
            ClearStaticInstance<FateFlowController>();
            ClearStaticInstance<CoinTossManager>();
            ClearStaticInstance<GameStatsTracker>();
        }
        
        /// <summary>
        /// Clears a static Instance field via reflection
        /// </summary>
        private static void ClearStaticInstance<T>() where T : MonoBehaviour
        {
            try
            {
                var instanceProperty = typeof(T).GetProperty("Instance", 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (instanceProperty != null && instanceProperty.CanWrite)
                {
                    instanceProperty.SetValue(null, null);
                }
                else
                {
                    // Try to find backing field
                    var backingField = typeof(T).GetField("Instance", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                    if (backingField == null)
                    {
                        backingField = typeof(T).GetField("<Instance>k__BackingField", 
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                    }
                    if (backingField != null)
                    {
                        backingField.SetValue(null, null);
                    }
                }
            }
            catch (System.Exception)
            {
                // Ignore reflection errors - instance might not exist
            }
        }

        /// <summary>
        /// Resets the game state for testing
        /// </summary>
        public static void ResetGameState()
        {
            // Reset coin toss
            CoinTossManager coinTossManager = CoinTossManager.Instance;
            if (coinTossManager != null)
            {
                coinTossManager.ResetCoinToss();
            }
            
            // Reset scores
            ScoreManager scoreManager = ScoreManager.Instance;
            if (scoreManager != null)
            {
                scoreManager.ResetScores();
            }
            
            // Reset game statistics
            CardDropArea.ResetGameStatistics();
            
            // Reset game state
            GameManager gameManager = GameManager.Instance;
            if (gameManager != null)
            {
                gameManager.ResetGameState();
            }
        }

        /// <summary>
        /// Initializes the scene and waits for all required systems to be ready
        /// </summary>
        public static IEnumerator InitializeScene(string sceneName = "BattleScreenMultiplayer")
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
            yield return null;
            yield return null; // Allow canvases to initialize
            
            // Wait for all required managers
            yield return new WaitUntil(() => CoinTossManager.Instance != null);
            yield return new WaitUntil(() => GameManager.Instance != null);
            yield return new WaitUntil(() => UnityEngine.Object.FindObjectOfType<HUDManager>() != null);
            
            // Additional frame for full initialization
            yield return null;
        }

        /// <summary>
        /// Activates all tile drop areas for testing (ensures they're active for P2 tests)
        /// </summary>
        public static void ActivateAllTiles()
        {
            CardDropArea[] tiles = UnityEngine.Object.FindObjectsOfType<CardDropArea>(true);
            foreach (var tile in tiles)
            {
                tile.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Force-enable all tiles for P2 drop tests (alias for ActivateAllTiles)
        /// </summary>
        /// <summary>
        /// Clears all cards from the board (cards at z ≈ 0), preserving cards in hands (z = 90)
        /// Also clears occupyingCard references and cardsPlayedThisTurn lists
        /// </summary>
        public static IEnumerator ClearBoard(float waitAfterClear = 0.5f)
        {
            Debug.Log($"[CardTestHelper] Starting board clearing...");
            
            CardDropArea[] dropAreas = Object.FindObjectsOfType<CardDropArea>();
            
            // Clear all CardDropArea occupyingCard references first
            foreach (CardDropArea area in dropAreas)
            {
                if (area != null)
                {
                    // Use reflection to clear the occupyingCard field
                    var occupyingCardField = typeof(CardDropArea).GetField("occupyingCard", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (occupyingCardField != null)
                    {
                        occupyingCardField.SetValue(area, null);
                    }
                    
                    // Also clear cardsPlayedThisTurn
                    var cardsPlayedField = typeof(CardDropArea).GetField("cardsPlayedThisTurn", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (cardsPlayedField != null)
                    {
                        var cardsPlayedList = cardsPlayedField.GetValue(area) as System.Collections.Generic.List<GameObject>;
                        if (cardsPlayedList != null)
                        {
                            cardsPlayedList.Clear();
                        }
                    }
                }
            }
            
            // Now find and destroy all cards on board (z ≈ 0)
            CardMoverP1[] allCardMovers = Object.FindObjectsOfType<CardMoverP1>();
            CardMoverP2[] allCardMoverP2s = Object.FindObjectsOfType<CardMoverP2>();
            
            int cardsOnBoard = 0;
            int cardsInHands = 0;
            List<GameObject> cardsToDestroy = new List<GameObject>();
            
            // Collect cards on board to destroy
            foreach (CardMoverP1 existingMover in allCardMovers)
            {
                if (existingMover != null && existingMover.gameObject != null)
                {
                    float zPos = existingMover.transform.position.z;
                    if (Mathf.Abs(zPos) < 1f) // Cards on board have z ≈ 0
                    {
                        cardsOnBoard++;
                        cardsToDestroy.Add(existingMover.gameObject);
                    }
                    else
                    {
                        cardsInHands++;
                    }
                }
            }
            
            foreach (CardMoverP2 existingMoverP2 in allCardMoverP2s)
            {
                if (existingMoverP2 != null && existingMoverP2.gameObject != null)
                {
                    float zPos = existingMoverP2.transform.position.z;
                    if (Mathf.Abs(zPos) < 1f) // Cards on board have z ≈ 0
                    {
                        cardsOnBoard++;
                        cardsToDestroy.Add(existingMoverP2.gameObject);
                    }
                    else
                    {
                        cardsInHands++;
                    }
                }
            }
            
            // Destroy all cards on board
            foreach (GameObject cardObj in cardsToDestroy)
            {
                if (cardObj != null)
                {
                    Object.DestroyImmediate(cardObj);
                }
            }
            
            Debug.Log($"[CardTestHelper] Clearing board - Destroyed {cardsOnBoard} cards on board, {cardsInHands} cards in hands (preserved)");
            
            yield return new WaitForSeconds(waitAfterClear); // Allow time for cleanup
            
            // Verify board is clear (only check cards at z ≈ 0)
            CardMoverP1[] remainingAll = Object.FindObjectsOfType<CardMoverP1>();
            CardMoverP2[] remainingAllOpp = Object.FindObjectsOfType<CardMoverP2>();
            
            int remainingOnBoard = 0;
            List<string> remainingCardNames = new List<string>();
            foreach (CardMoverP1 mover in remainingAll)
            {
                if (mover != null && Mathf.Abs(mover.transform.position.z) < 1f)
                {
                    remainingOnBoard++;
                    remainingCardNames.Add($"{mover.gameObject.name} at {mover.transform.position}");
                }
            }
            foreach (CardMoverP2 moverP2 in remainingAllOpp)
            {
                if (moverP2 != null && Mathf.Abs(moverP2.transform.position.z) < 1f)
                {
                    remainingOnBoard++;
                    remainingCardNames.Add($"{moverP2.gameObject.name} at {moverP2.transform.position}");
                }
            }
            
            Debug.Log($"[CardTestHelper] Board cleared - {remainingOnBoard} cards remaining on board (should be 0)");
            if (remainingOnBoard > 0)
            {
                Debug.LogWarning($"[CardTestHelper] Remaining cards: {string.Join(", ", remainingCardNames)}");
            }
        }
        
        public static void ForceEnableAllTiles()
        {
            ActivateAllTiles();
        }
    }
}

