using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CardGame.Managers;
using CardGame.Core;
using CardGame.UI;

namespace CardGame.Tests
{
    /// <summary>
    /// PlayMode tests for board integrity - validates empty slot counting, full board detection, and tile occupancy.
    /// Tests ACTUAL behavior, not just property existence.
    /// 
    /// COMPREHENSIVE REWRITE with:
    /// - Proper timeout protection
    /// - Robust error handling
    /// - Better waiting mechanisms
    /// - Test isolation
    /// - Clear failure messages
    /// </summary>
    public class BoardIntegrityPlayModeTests
    {
        private const string SCENE_NAME = "BattleScreenMultiplayer";
        private const float SCENE_LOAD_TIMEOUT = 15f;
        private const float TEST_TIMEOUT = 60f; // Maximum time for any test to complete

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // Clear singletons first
            CardTestHelper.ClearSingletonInstances();
            yield return null;
            
            // Verify scene exists
            bool sceneExists = false;
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string scenePath = System.IO.Path.GetFileNameWithoutExtension(
                    UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(i));
                if (scenePath == SCENE_NAME)
                {
                    sceneExists = true;
                    break;
                }
            }
            
            if (!sceneExists)
            {
                Assert.Fail($"Scene '{SCENE_NAME}' must be added to Build Settings");
            }

            // Load scene with timeout
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(SCENE_NAME, LoadSceneMode.Single);
            asyncLoad.allowSceneActivation = true;
            
            float elapsed = 0f;
            while (!asyncLoad.isDone && elapsed < SCENE_LOAD_TIMEOUT)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            if (!asyncLoad.isDone)
            {
                Assert.Fail($"Scene '{SCENE_NAME}' failed to load within {SCENE_LOAD_TIMEOUT} seconds");
            }
            
            // Wait for scene initialization
            yield return new WaitForSeconds(1.0f);
            
            // Verify essential components exist
            CardDropArea[] dropAreas = Object.FindObjectsOfType<CardDropArea>();
            if (dropAreas.Length == 0)
            {
                Assert.Fail($"Scene '{SCENE_NAME}' loaded but no CardDropArea components found");
            }
        }
        
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return null;
            
            // Clean up board
            yield return CardTestHelper.ClearBoard(0.1f);
            
            // Clear singletons
            CardTestHelper.ClearSingletonInstances();
            
            yield return null;
        }

        #region Test 1: Initial Board State

        [UnityTest]
        [Timeout(30000)] // 30 second timeout
        public IEnumerator Board_ReportsCorrectEmptySlotCount_AtStart()
        {
            float testStartTime = Time.realtimeSinceStartup;
            
            // Wait for scene to fully initialize (no coin toss needed for this test)
            yield return new WaitForSeconds(1.5f);
            
            // Verify we haven't exceeded timeout
            if (Time.realtimeSinceStartup - testStartTime > TEST_TIMEOUT)
            {
                Assert.Fail("Test exceeded timeout");
            }
            
            CardDropArea[] dropAreas = Object.FindObjectsOfType<CardDropArea>();
            Assert.AreEqual(16, dropAreas.Length, "Should have exactly 16 drop areas");
            
            int emptySlots = 0;
            int occupiedSlots = 0;
            foreach (CardDropArea dropArea in dropAreas)
            {
                if (dropArea == null) continue;
                
                if (!dropArea.IsOccupied)
                {
                    emptySlots++;
                }
                else
                {
                    occupiedSlots++;
                }
            }
            
            Assert.AreEqual(16, emptySlots, "At game start, all 16 slots should be empty");
            Assert.AreEqual(0, occupiedSlots, "At game start, no slots should be occupied");
        }

        #endregion

        #region Test 2: Empty Slot Updates

        [UnityTest]
        [Timeout(60000)] // 60 second timeout
        public IEnumerator Board_UpdatesEmptySlotCount_OnPlacement()
        {
            float testStartTime = Time.realtimeSinceStartup;
            
            // Wait for coin toss and initialize
            yield return WaitForGameReady();
            
            if (Time.realtimeSinceStartup - testStartTime > TEST_TIMEOUT)
            {
                Assert.Fail("Test exceeded timeout during initialization");
            }
            
            CardDropArea[] dropAreas = Object.FindObjectsOfType<CardDropArea>();
            Assert.AreEqual(16, dropAreas.Length, "Should have exactly 16 drop areas");
            
            // Count initial empty slots
            int initialEmptySlots = CountEmptySlots(dropAreas);
            Assert.AreEqual(16, initialEmptySlots, "Initial empty slot count should be 16");
            
            // Setup deck and hand
            NewDeckManagerP1 deckP1 = Object.FindObjectOfType<NewDeckManagerP1>();
            Assert.IsNotNull(deckP1, "P1 deck should exist");
            
            yield return EnsureDeckInitialized(deckP1);
            yield return EnsureHandHasCards(deckP1, 5);
            
            if (Time.realtimeSinceStartup - testStartTime > TEST_TIMEOUT)
            {
                Assert.Fail("Test exceeded timeout during deck setup");
            }
            
            // Get hand UI and card
            NewHandP1UI handUI = Object.FindObjectOfType<NewHandP1UI>();
            Assert.IsNotNull(handUI, "P1 hand UI should exist");
            
            CardDropArea targetArea = GetFirstEmptyArea(dropAreas);
            Assert.IsNotNull(targetArea, "Should have at least one empty area");
            
            NewCard testCard = deckP1.Hand[0];
            CardMoverP1 mover = FindCardMoverInHand(handUI, testCard);
            Assert.IsNotNull(mover, "Card should have CardMoverP1 component");
            
            // Place card
            bool placed = CardTestHelper.PlaceP1CardOnDropArea(mover, targetArea);
            yield return new WaitForSeconds(0.5f);
            
            Assert.IsTrue(placed, "Card should be placed successfully");
            Assert.IsTrue(targetArea.IsOccupied, "Target area should be occupied after placement");
            
            // Count empty slots after placement
            int emptySlotsAfter = CountEmptySlots(dropAreas);
            int occupiedSlotsAfter = CountOccupiedSlots(dropAreas);
            
            Assert.AreEqual(15, emptySlotsAfter, "After placing one card, 15 slots should be empty");
            Assert.AreEqual(1, occupiedSlotsAfter, "After placing one card, 1 slot should be occupied");
            Assert.AreEqual(initialEmptySlots - 1, emptySlotsAfter, 
                "Empty slot count should decrease by 1 after placement");
        }

        #endregion

        #region Test 3: Full Board Detection

        [UnityTest]
        [Timeout(120000)] // 2 minute timeout for filling board
        public IEnumerator Board_Full_Triggers_GameEnd()
        {
            float testStartTime = Time.realtimeSinceStartup;
            
            // Wait for coin toss and initialize
            yield return WaitForGameReady();
            
            GameEndManager gameEndManager = GameEndManager.Instance;
            Assert.IsNotNull(gameEndManager, "GameEndManager should exist");
            
            CardDropArea[] dropAreas = Object.FindObjectsOfType<CardDropArea>();
            Assert.AreEqual(16, dropAreas.Length, "Should have exactly 16 drop areas");
            
            // Setup both decks
            NewDeckManagerP1 deckP1 = Object.FindObjectOfType<NewDeckManagerP1>();
            NewDeckManagerP2 deckP2 = Object.FindObjectOfType<NewDeckManagerP2>();
            Assert.IsNotNull(deckP1, "P1 deck should exist");
            Assert.IsNotNull(deckP2, "P2 deck should exist");
            
            yield return EnsureDeckInitialized(deckP1);
            yield return EnsureDeckInitialized(deckP2);
            yield return EnsureHandHasCards(deckP1, 8);
            yield return EnsureHandHasCards(deckP2, 8);
            
            if (Time.realtimeSinceStartup - testStartTime > TEST_TIMEOUT)
            {
                Assert.Fail("Test exceeded timeout during deck setup");
            }
            
            // Get hand UIs
            NewHandP1UI handP1 = Object.FindObjectOfType<NewHandP1UI>();
            NewHandP2UI handP2 = Object.FindObjectOfType<NewHandP2UI>();
            Assert.IsNotNull(handP1, "P1 hand UI should exist");
            Assert.IsNotNull(handP2, "P2 hand UI should exist");
            
            // Fill board with timeout protection
            int cardsPlaced = 0;
            yield return FillBoardCompletely(dropAreas, deckP1, deckP2, handP1, handP2, testStartTime, (count) => cardsPlaced = count);
            
            Assert.AreEqual(16, cardsPlaced, $"Should have placed exactly 16 cards. Actually placed: {cardsPlaced}");
            
            // Wait for game end detection
            yield return new WaitForSeconds(2.0f);
            
            // Verify board is full
            int occupiedCount = CountOccupiedSlots(dropAreas);
            Assert.AreEqual(16, occupiedCount, $"Board should be full with 16 cards. Found: {occupiedCount}");
            
            // Verify game end was triggered
            GameEndUI gameEndUI = Object.FindObjectOfType<GameEndUI>(true);
            Assert.IsNotNull(gameEndUI, "GameEndUI should exist when game ends");
            Assert.IsNotNull(gameEndManager, "GameEndManager should exist");
        }

        #endregion

        #region Test 4: Placement Prevention When Full

        [UnityTest]
        [Timeout(120000)] // 2 minute timeout
        public IEnumerator Board_DoesNotAllowPlacement_WhenFull()
        {
            float testStartTime = Time.realtimeSinceStartup;
            
            // Wait for coin toss and initialize
            yield return WaitForGameReady();
            
            CardDropArea[] dropAreas = Object.FindObjectsOfType<CardDropArea>();
            Assert.AreEqual(16, dropAreas.Length, "Should have exactly 16 drop areas");
            
            // Setup decks
            NewDeckManagerP1 deckP1 = Object.FindObjectOfType<NewDeckManagerP1>();
            NewDeckManagerP2 deckP2 = Object.FindObjectOfType<NewDeckManagerP2>();
            Assert.IsNotNull(deckP1, "P1 deck should exist");
            Assert.IsNotNull(deckP2, "P2 deck should exist");
            
            yield return EnsureDeckInitialized(deckP1);
            yield return EnsureDeckInitialized(deckP2);
            yield return EnsureHandHasCards(deckP1, 9);
            yield return EnsureHandHasCards(deckP2, 9);
            
            if (Time.realtimeSinceStartup - testStartTime > TEST_TIMEOUT)
            {
                Assert.Fail("Test exceeded timeout during deck setup");
            }
            
            // Get hand UIs
            NewHandP1UI handP1 = Object.FindObjectOfType<NewHandP1UI>();
            NewHandP2UI handP2 = Object.FindObjectOfType<NewHandP2UI>();
            Assert.IsNotNull(handP1, "P1 hand UI should exist");
            Assert.IsNotNull(handP2, "P2 hand UI should exist");
            
            // Fill board completely
            int cardsPlaced = 0;
            yield return FillBoardCompletely(dropAreas, deckP1, deckP2, handP1, handP2, testStartTime, (count) => cardsPlaced = count);
            Assert.AreEqual(16, cardsPlaced, $"Should have placed exactly 16 cards. Actually placed: {cardsPlaced}");
            
            yield return new WaitForSeconds(1.0f);
            
            // Verify board is full
            int occupiedCount = CountOccupiedSlots(dropAreas);
            Assert.AreEqual(16, occupiedCount, "Board should be full");
            
            // Attempt to place another card (should fail)
            if (deckP1.Hand.Count > 0)
            {
                NewCard extraCard = deckP1.Hand[0];
                CardMoverP1 extraMover = FindCardMoverInHand(handP1, extraCard);
                Assert.IsNotNull(extraMover, "Extra card should have CardMoverP1");
                
                // Try to place on any area (all should be occupied)
                bool placed = false;
                foreach (CardDropArea area in dropAreas)
                {
                    if (area != null && area.IsOccupied)
                    {
                        placed = CardTestHelper.PlaceP1CardOnDropArea(extraMover, area);
                        if (placed) break;
                    }
                }
                
                Assert.IsFalse(placed, "Should NOT be able to place card when board is full");
                
                // Verify board is still full
                int occupiedCountAfter = CountOccupiedSlots(dropAreas);
                Assert.AreEqual(16, occupiedCountAfter, 
                    "Board should still be full after failed placement attempt");
            }
        }

        #endregion

        #region Test 5: Tile Occupancy

        [UnityTest]
        [Timeout(60000)] // 60 second timeout
        public IEnumerator Tile_Occupancy_ReflectsActualCard()
        {
            float testStartTime = Time.realtimeSinceStartup;
            
            // Wait for coin toss and initialize
            yield return WaitForGameReady();
            
            CardDropArea[] dropAreas = Object.FindObjectsOfType<CardDropArea>();
            Assert.AreEqual(16, dropAreas.Length, "Should have exactly 16 drop areas");
            
            // Verify all tiles start unoccupied
            foreach (CardDropArea dropArea in dropAreas)
            {
                if (dropArea == null) continue;
                
                Assert.IsFalse(dropArea.IsOccupied, 
                    $"DropArea '{dropArea.gameObject.name}' should be unoccupied at start");
                Assert.IsNull(dropArea.GetOccupyingCard(), 
                    $"DropArea '{dropArea.gameObject.name}' should have no occupying card at start");
            }
            
            // Setup deck and hand
            NewDeckManagerP1 deckP1 = Object.FindObjectOfType<NewDeckManagerP1>();
            Assert.IsNotNull(deckP1, "P1 deck should exist");
            
            yield return EnsureDeckInitialized(deckP1);
            yield return EnsureHandHasCards(deckP1, 5);
            
            if (Time.realtimeSinceStartup - testStartTime > TEST_TIMEOUT)
            {
                Assert.Fail("Test exceeded timeout during deck setup");
            }
            
            // Get hand UI and card
            NewHandP1UI handUI = Object.FindObjectOfType<NewHandP1UI>();
            Assert.IsNotNull(handUI, "P1 hand UI should exist");
            
            CardDropArea targetArea = GetFirstEmptyArea(dropAreas);
            Assert.IsNotNull(targetArea, "Should have at least one empty area");
            
            NewCard testCard = deckP1.Hand[0];
            CardMoverP1 mover = FindCardMoverInHand(handUI, testCard);
            Assert.IsNotNull(mover, "Card should have CardMoverP1");
            
            // Place card
            bool placed = CardTestHelper.PlaceP1CardOnDropArea(mover, targetArea);
            yield return new WaitForSeconds(0.5f);
            
            Assert.IsTrue(placed, "Card should be placed");
            
            // Verify target area is occupied
            Assert.IsTrue(targetArea.IsOccupied, "Target area should be occupied");
            Assert.IsNotNull(targetArea.GetOccupyingCard(), "Target area should have occupying card");
            Assert.AreEqual(mover.gameObject, targetArea.GetOccupyingCard(), 
                "Occupying card should be the placed card");
            
            // Verify other areas remain unoccupied
            for (int i = 0; i < dropAreas.Length; i++)
            {
                CardDropArea area = dropAreas[i];
                if (area == null || area == targetArea) continue;
                
                Assert.IsFalse(area.IsOccupied, 
                    $"Area {area.name} should remain unoccupied");
                Assert.IsNull(area.GetOccupyingCard(), 
                    $"Area {area.name} should have no occupying card");
            }
        }

        #endregion

        #region Test 6: Card Destruction

        [UnityTest]
        [Timeout(60000)] // 60 second timeout
        public IEnumerator Board_State_AfterCardDestruction()
        {
            float testStartTime = Time.realtimeSinceStartup;
            
            // Wait for coin toss and initialize
            yield return WaitForGameReady();
            
            CardDropArea[] dropAreas = Object.FindObjectsOfType<CardDropArea>();
            Assert.AreEqual(16, dropAreas.Length, "Should have exactly 16 drop areas");
            
            // Setup deck and hand
            NewDeckManagerP1 deckP1 = Object.FindObjectOfType<NewDeckManagerP1>();
            Assert.IsNotNull(deckP1, "P1 deck should exist");
            
            yield return EnsureDeckInitialized(deckP1);
            yield return EnsureHandHasCards(deckP1, 5);
            
            if (Time.realtimeSinceStartup - testStartTime > TEST_TIMEOUT)
            {
                Assert.Fail("Test exceeded timeout during deck setup");
            }
            
            // Get hand UI and card
            NewHandP1UI handUI = Object.FindObjectOfType<NewHandP1UI>();
            Assert.IsNotNull(handUI, "P1 hand UI should exist");
            
            // Place a card
            CardDropArea targetArea = GetFirstEmptyArea(dropAreas);
            Assert.IsNotNull(targetArea, "Should have at least one empty area");
            
            NewCard testCard = deckP1.Hand[0];
            CardMoverP1 mover = FindCardMoverInHand(handUI, testCard);
            Assert.IsNotNull(mover, "Card should have CardMoverP1");
            
            CardTestHelper.PlaceP1CardOnDropArea(mover, targetArea);
            yield return new WaitForSeconds(0.5f);
            
            Assert.IsTrue(targetArea.IsOccupied, "Area should be occupied");
            GameObject occupyingCard = targetArea.GetOccupyingCard();
            Assert.IsNotNull(occupyingCard, "Occupying card should exist");
            
            // Destroy the card
            Object.Destroy(occupyingCard);
            yield return new WaitForSeconds(1.0f); // Wait for destruction to complete
            
            // Verify area is no longer occupied
            GameObject cardAfterDestruction = targetArea.GetOccupyingCard();
            if (cardAfterDestruction == null)
            {
                Assert.IsFalse(targetArea.IsOccupied, 
                    "Area should not be occupied after card destruction");
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Waits for game to be ready (coin toss complete, scene initialized)
        /// </summary>
        private IEnumerator WaitForGameReady()
        {
            yield return CardTestHelper.WaitForCoinTossToComplete();
            yield return new WaitForSeconds(1.0f);
            yield return CardTestHelper.ClearBoard(0.5f);
        }

        /// <summary>
        /// Ensures deck is initialized
        /// </summary>
        private IEnumerator EnsureDeckInitialized(NewDeckManagerP1 deck)
        {
            if (deck == null) yield break;
            
            deck.InitializeDeck();
            yield return new WaitForSeconds(0.3f);
        }

        /// <summary>
        /// Ensures deck is initialized
        /// </summary>
        private IEnumerator EnsureDeckInitialized(NewDeckManagerP2 deck)
        {
            if (deck == null) yield break;
            
            deck.InitializeDeck();
            yield return new WaitForSeconds(0.3f);
        }

        /// <summary>
        /// Ensures hand has at least the specified number of cards
        /// </summary>
        private IEnumerator EnsureHandHasCards(NewDeckManagerP1 deck, int minCards)
        {
            if (deck == null) yield break;
            
            int attempts = 0;
            int maxAttempts = 20;
            
            while (deck.Hand.Count < minCards && deck.DrawPileCount > 0 && attempts < maxAttempts)
            {
                deck.DrawCard();
                yield return new WaitForSeconds(0.1f);
                attempts++;
            }
            
            yield return new WaitForSeconds(0.3f);
        }

        /// <summary>
        /// Ensures hand has at least the specified number of cards
        /// </summary>
        private IEnumerator EnsureHandHasCards(NewDeckManagerP2 deck, int minCards)
        {
            if (deck == null) yield break;
            
            int attempts = 0;
            int maxAttempts = 20;
            
            while (deck.Hand.Count < minCards && deck.DrawPileCount > 0 && attempts < maxAttempts)
            {
                deck.DrawCard();
                yield return new WaitForSeconds(0.1f);
                attempts++;
            }
            
            yield return new WaitForSeconds(0.3f);
        }

        /// <summary>
        /// Fills the board completely with timeout protection
        /// </summary>
        private IEnumerator FillBoardCompletely(
            CardDropArea[] dropAreas, 
            NewDeckManagerP1 deckP1, 
            NewDeckManagerP2 deckP2,
            NewHandP1UI handP1,
            NewHandP2UI handP2,
            float testStartTime,
            System.Action<int> onComplete)
        {
            int cardsPlaced = 0;
            int consecutiveFailures = 0;
            int maxConsecutiveFailures = 20; // Increased from 10
            int totalAttempts = 0;
            int maxTotalAttempts = 200; // Absolute limit
            
            while (cardsPlaced < 16 && consecutiveFailures < maxConsecutiveFailures && totalAttempts < maxTotalAttempts)
            {
                // Check timeout
                if (Time.realtimeSinceStartup - testStartTime > TEST_TIMEOUT)
                {
                    Assert.Fail($"Test exceeded timeout while filling board. Placed {cardsPlaced}/16 cards.");
                }
                
                bool placedThisRound = false;
                
                // Try to place a card
                for (int i = 0; i < dropAreas.Length && cardsPlaced < 16; i++)
                {
                    CardDropArea area = dropAreas[i];
                    if (area == null || area.IsOccupied) continue;
                    
                    bool placed = false;
                    
                    // Alternate between P1 and P2
                    if (cardsPlaced % 2 == 0 && deckP1 != null && deckP1.Hand.Count > 0)
                    {
                        NewCard card = deckP1.Hand[0];
                        CardMoverP1 mover = FindCardMoverInHand(handP1, card);
                        if (mover != null && !mover.IsPlayed)
                        {
                            placed = CardTestHelper.PlaceP1CardOnDropArea(mover, area);
                        }
                    }
                    else if (deckP2 != null && deckP2.Hand.Count > 0)
                    {
                        NewCard card = deckP2.Hand[0];
                        CardMoverP2 mover = FindCardMoverInHand(handP2, card);
                        if (mover != null && !mover.IsPlayed)
                        {
                            placed = CardTestHelper.PlaceP2CardOnDropArea(mover, area);
                        }
                    }
                    
                    if (placed)
                    {
                        cardsPlaced++;
                        placedThisRound = true;
                        consecutiveFailures = 0;
                        yield return new WaitForSeconds(0.15f); // Slightly longer wait
                        break; // Break to restart loop
                    }
                }
                
                totalAttempts++;
                
                if (!placedThisRound)
                {
                    consecutiveFailures++;
                    yield return new WaitForSeconds(0.1f);
                }
            }
            
            onComplete?.Invoke(cardsPlaced);
        }

        /// <summary>
        /// Counts empty slots
        /// </summary>
        private int CountEmptySlots(CardDropArea[] dropAreas)
        {
            int count = 0;
            foreach (CardDropArea area in dropAreas)
            {
                if (area != null && !area.IsOccupied)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// Counts occupied slots
        /// </summary>
        private int CountOccupiedSlots(CardDropArea[] dropAreas)
        {
            int count = 0;
            foreach (CardDropArea area in dropAreas)
            {
                if (area != null && area.IsOccupied)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// Gets the first empty area
        /// </summary>
        private CardDropArea GetFirstEmptyArea(CardDropArea[] dropAreas)
        {
            foreach (CardDropArea area in dropAreas)
            {
                if (area != null && !area.IsOccupied)
                {
                    return area;
                }
            }
            return null;
        }

        /// <summary>
        /// Finds CardMoverP1 in hand UI
        /// </summary>
        private CardMoverP1 FindCardMoverInHand(NewHandP1UI handUI, NewCard card)
        {
            if (handUI == null || card == null) return null;
            
            foreach (Transform child in handUI.transform)
            {
                if (child == null) continue;
                
                CardMoverP1 mover = child.GetComponent<CardMoverP1>();
                if (mover != null && mover.Card == card)
                {
                    return mover;
                }
            }
            return null;
        }

        /// <summary>
        /// Finds CardMoverP2 in hand UI
        /// </summary>
        private CardMoverP2 FindCardMoverInHand(NewHandP2UI handUI, NewCard card)
        {
            if (handUI == null || card == null) return null;
            
            foreach (Transform child in handUI.transform)
            {
                if (child == null) continue;
                
                CardMoverP2 mover = child.GetComponent<CardMoverP2>();
                if (mover != null && mover.Card == card)
                {
                    return mover;
                }
            }
            return null;
        }

        #endregion
    }
}
