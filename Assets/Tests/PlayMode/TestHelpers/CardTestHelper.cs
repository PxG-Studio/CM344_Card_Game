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
        /// Places a card on a drop area using AutomationAttemptDrop
        /// </summary>
        public static bool PlaceCardOnDropArea(CardMover cardMover, CardDropArea1 dropArea, bool bypassTurnCheck = true)
        {
            if (cardMover == null || dropArea == null) return false;
            
            Vector3 dropPosition = dropArea.transform.position;
            return cardMover.AutomationAttemptDrop(dropPosition, bypassTurnCheck);
        }

        /// <summary>
        /// Places an opponent card on a drop area
        /// </summary>
        public static bool PlaceOpponentCardOnDropArea(CardMoverOpp cardMoverOpp, CardDropArea1 dropArea, bool bypassTurnCheck = true)
        {
            if (cardMoverOpp == null || dropArea == null) return false;
            
            // Ensure collider is set before attempting drop (Unity might have called Start() and reset it)
            EnsureColliderSet(cardMoverOpp);
            
            Vector3 dropPosition = dropArea.transform.position;
            return cardMoverOpp.AutomationAttemptDrop(dropPosition, bypassTurnCheck);
        }
        
        /// <summary>
        /// Ensures the collider field is set on CardMoverOpp (Unity's Start() might reset it)
        /// </summary>
        private static void EnsureColliderSet(CardMoverOpp mover)
        {
            if (mover == null) return;
            
            var colField = typeof(CardMoverOpp).GetField("col", 
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
        public static void AddCardToDeckManagerHand(NewDeckManager deckManager, NewCard card)
        {
            if (deckManager == null || card == null) return;
            
            var handField = typeof(NewDeckManager).GetField("hand", 
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
        /// Adds a card to an opponent deck manager's hand using reflection
        /// </summary>
        public static void AddCardToDeckManagerHand(NewDeckManagerOpp deckManager, NewCard card)
        {
            if (deckManager == null || card == null) return;
            
            var handField = typeof(NewDeckManagerOpp).GetField("hand", 
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
            
            // Check isFlipped property (card is captured when isFlipped = true, meaning back is showing)
            var isFlippedProperty = typeof(CardFlipAnimation).GetProperty("isFlipped");
            if (isFlippedProperty != null)
            {
                return (bool)isFlippedProperty.GetValue(flipAnim);
            }
            
            // Fallback: check if back container is active
            var backContainerField = typeof(CardFlipAnimation).GetField("backContainer", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (backContainerField != null)
            {
                GameObject backContainer = backContainerField.GetValue(flipAnim) as GameObject;
                if (backContainer != null)
                {
                    return backContainer.activeSelf;
                }
            }
            
            return false;
        }

        /// <summary>
        /// Gets the adjacent drop area in a specific direction
        /// </summary>
        public static CardDropArea1 GetAdjacentDropArea(CardDropArea1 fromArea, string direction)
        {
            CardDropArea1[] allAreas = Object.FindObjectsOfType<CardDropArea1>();
            Vector3 fromPos = fromArea.transform.position;
            
            float searchDistance = 3.5f; // Slightly larger than adjacentCardDistance
            
            foreach (CardDropArea1 area in allAreas)
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
        /// Creates a CardMover GameObject with a test card
        /// </summary>
        public static CardMover CreateCardMoverWithCard(NewCard card, Vector3 position, bool isPlayerCard = true)
        {
            // Find prefab or create GameObject
            GameObject cardPrefab = GameObject.Find("NewCardPrefab");
            if (cardPrefab == null)
            {
                // Create a minimal card GameObject
                GameObject cardObj = new GameObject($"TestCard_{card.Data.cardName}");
                cardObj.transform.position = position;
                
                // Add Collider2D FIRST (before CardMover so Start() can find it)
                Collider2D col = cardObj.AddComponent<BoxCollider2D>();
                col.isTrigger = true;
                col.enabled = true;
                
                CardMover cardMover = cardObj.AddComponent<CardMover>();
                cardMover.SetCard(card);
                
                // Ensure Start() is called to initialize the col reference
                var startMethod = typeof(CardMover).GetMethod("Start", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (startMethod != null)
                {
                    startMethod.Invoke(cardMover, null);
                }
                
                // Manually set collider reference if Start() didn't work
                var colField = typeof(CardMover).GetField("col", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (colField != null && colField.GetValue(cardMover) == null)
                {
                    colField.SetValue(cardMover, col);
                }
                
                // Add NewCardUI component for capture/flip functionality
                CardGame.UI.NewCardUI cardUI = cardObj.AddComponent<CardGame.UI.NewCardUI>();
                // Ensure Awake() is called before Initialize() to set up containers
                var awakeMethod = typeof(CardGame.UI.NewCardUI).GetMethod("Awake", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (awakeMethod != null)
                {
                    awakeMethod.Invoke(cardUI, null);
                }
                cardUI.Initialize(card);
                
                return cardMover;
            }
            
            // Use prefab if available
            GameObject cardInstance = Object.Instantiate(cardPrefab, position, Quaternion.identity);
            CardMover mover = cardInstance.GetComponent<CardMover>();
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
                var startMethod = typeof(CardMover).GetMethod("Start", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (startMethod != null)
                {
                    startMethod.Invoke(mover, null);
                }
                
                // Manually set collider reference if Start() didn't work
                var colField = typeof(CardMover).GetField("col", 
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
        /// Creates a CardMoverOpp GameObject with a test card
        /// </summary>
        public static CardMoverOpp CreateCardMoverOppWithCard(NewCard card, Vector3 position)
        {
            GameObject cardPrefab = GameObject.Find("NewCardPrefabOpp");
            if (cardPrefab == null)
            {
                GameObject cardObj = new GameObject($"TestCardOpp_{card.Data.cardName}");
                cardObj.transform.position = position;
                
                // Add Collider2D FIRST (before CardMoverOpp so Start() can find it)
                Collider2D col = cardObj.AddComponent<BoxCollider2D>();
                col.isTrigger = true;
                col.enabled = true;
                
                CardMoverOpp cardMover = cardObj.AddComponent<CardMoverOpp>();
                cardMover.SetCard(card);
                
                // Ensure Start() is called to initialize the col reference
                // In EditMode/PlayMode tests, Start() may not be called automatically
                var startMethod = typeof(CardMoverOpp).GetMethod("Start", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (startMethod != null)
                {
                    startMethod.Invoke(cardMover, null);
                }
                
                // ALWAYS manually set collider reference to ensure it's set correctly
                // Unity might call Start() again later, which could reset it to null
                var colField = typeof(CardMoverOpp).GetField("col", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (colField != null)
                {
                    // Force set it to the collider we created
                    colField.SetValue(cardMover, col);
                    
                    // Verify it was set correctly
                    var setCol = colField.GetValue(cardMover) as Collider2D;
                    if (setCol == null)
                    {
                        Debug.LogError($"[CardTestHelper] Failed to set collider on CardMoverOpp for '{card.Data.cardName}'. " +
                            $"Collider exists: {col != null}, Field exists: {colField != null}");
                    }
                }
                
                // Add NewCardUI component for capture/flip functionality
                CardGame.UI.NewCardUI cardUI = cardObj.AddComponent<CardGame.UI.NewCardUI>();
                // Ensure Awake() is called before Initialize() to set up containers
                var awakeMethod = typeof(CardGame.UI.NewCardUI).GetMethod("Awake", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (awakeMethod != null)
                {
                    awakeMethod.Invoke(cardUI, null);
                }
                cardUI.Initialize(card);
                
                return cardMover;
            }
            
            GameObject cardInstance = Object.Instantiate(cardPrefab, position, Quaternion.identity);
            CardMoverOpp mover = cardInstance.GetComponent<CardMoverOpp>();
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
                var startMethod = typeof(CardMoverOpp).GetMethod("Start", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (startMethod != null)
                {
                    startMethod.Invoke(mover, null);
                }
                
                // ALWAYS manually set collider reference to ensure it's set correctly
                // Unity might call Start() again later, which could reset it to null
                var colField = typeof(CardMoverOpp).GetField("col", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (colField != null && prefabCol != null)
                {
                    // Force set it to the collider we found/created
                    colField.SetValue(mover, prefabCol);
                    
                    // Verify it was set correctly
                    var setCol = colField.GetValue(mover) as Collider2D;
                    if (setCol == null)
                    {
                        Debug.LogError($"[CardTestHelper] Failed to set collider on CardMoverOpp prefab instance for '{card.Data.cardName}'. " +
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
                mover = cardInstance.AddComponent<CardMoverOpp>();
                mover.SetCard(card);
                
                Collider2D col = cardInstance.GetComponent<Collider2D>();
                if (col == null)
                {
                    col = cardInstance.AddComponent<BoxCollider2D>();
                    col.isTrigger = true;
                    col.enabled = true;
                }
                
                // Ensure Start() is called
                var startMethod = typeof(CardMoverOpp).GetMethod("Start", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (startMethod != null)
                {
                    startMethod.Invoke(mover, null);
                }
                
                // Manually set collider reference
                var colField = typeof(CardMoverOpp).GetField("col", 
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
            var finalColField = typeof(CardMoverOpp).GetField("col", 
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
            
            // Wait for coin toss to complete
            float elapsed = 0f;
            while (!coinTossManager.IsComplete && elapsed < timeout)
            {
                yield return new WaitForSeconds(0.1f);
                elapsed += 0.1f;
            }
            
            if (!coinTossManager.IsComplete)
            {
                Debug.LogWarning($"[CardTestHelper] Coin toss did not complete within {timeout} seconds. " +
                    $"Game may have auto-started. Proceeding with test anyway.");
                // Don't fail the test - coin toss may have completed but IsComplete flag not set yet
                // or game may be in a different state
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
            
            return isPlayer1 ? scoreManager.PlayerScore : scoreManager.OpponentScore;
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
            CardDropArea1.ResetGameStatistics();
            
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
            CardDropArea1[] tiles = UnityEngine.Object.FindObjectsOfType<CardDropArea1>(true);
            foreach (var tile in tiles)
            {
                tile.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Force-enable all tiles for P2 drop tests (alias for ActivateAllTiles)
        /// </summary>
        public static void ForceEnableAllTiles()
        {
            ActivateAllTiles();
        }
    }
}

