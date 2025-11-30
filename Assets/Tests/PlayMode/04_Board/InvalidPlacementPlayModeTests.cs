using System.Collections;
using System.Linq;
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
    /// PlayMode tests for invalid placement recovery - validates card return to hand on invalid drops.
    /// Tests ACTUAL behavior, not just method existence.
    /// </summary>
    public class InvalidPlacementPlayModeTests
    {
        private const string SCENE_NAME = "BattleScreenMultiplayer";

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            CardTestHelper.ClearSingletonInstances();
            yield return null;
            
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

            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(SCENE_NAME, LoadSceneMode.Single);
            asyncLoad.allowSceneActivation = true;
            
            float timeout = 10f;
            float elapsed = 0f;
            while (!asyncLoad.isDone && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            if (!asyncLoad.isDone)
            {
                Assert.Fail($"Scene '{SCENE_NAME}' failed to load within {timeout} seconds");
            }
            
            yield return new WaitForSeconds(0.5f);
        }
        
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return null;
            CardTestHelper.ClearBoard(0.1f);
            CardTestHelper.ClearSingletonInstances();
            yield return null;
        }

        [UnityTest]
        public IEnumerator Card_ReturnsToHand_OnOccupiedTile()
        {
            yield return CardTestHelper.WaitForCoinTossToComplete();
            yield return new WaitForSeconds(1.0f);
            yield return CardTestHelper.ClearBoard(0.5f);
            
            NewDeckManagerP1 deckP1 = Object.FindObjectOfType<NewDeckManagerP1>();
            Assert.IsNotNull(deckP1, "P1 deck should exist");
            deckP1.InitializeDeck();
            
            if (deckP1.Hand.Count == 0)
            {
                deckP1.DrawCards(5);
                yield return new WaitForSeconds(0.5f);
            }
            
            Assert.Greater(deckP1.Hand.Count, 0, "P1 should have cards in hand");
            
            CardDropArea[] dropAreas = Object.FindObjectsOfType<CardDropArea>();
            Assert.IsTrue(dropAreas.Length > 0, "Drop areas should exist");
            
            CardDropArea targetArea = dropAreas[0];
            NewHandP1UI handUI = Object.FindObjectOfType<NewHandP1UI>();
            Assert.IsNotNull(handUI, "P1 hand UI should exist");
            
            // Get first card and place it
            NewCard firstCard = deckP1.Hand[0];
            CardMoverP1 firstMover = FindCardMoverInHand(handUI, firstCard);
            Assert.IsNotNull(firstMover, "First card should have CardMoverP1 in hand");
            
            Vector3 firstOriginalPosition = firstMover.transform.position;
            bool placed = CardTestHelper.PlaceP1CardOnDropArea(firstMover, targetArea);
            yield return new WaitForSeconds(0.5f);
            
            Assert.IsTrue(placed, "First card should be placed successfully");
            Assert.IsTrue(targetArea.IsOccupied, "Tile should be occupied after placement");
            Assert.AreEqual(firstMover.gameObject, targetArea.GetOccupyingCard(), "First card should occupy the tile");
            
            // Try to place second card on same occupied tile (should fail)
            Assert.Greater(deckP1.Hand.Count, 0, "Should have second card in hand");
            NewCard secondCard = deckP1.Hand[0];
            CardMoverP1 secondMover = FindCardMoverInHand(handUI, secondCard);
            Assert.IsNotNull(secondMover, "Second card should have CardMoverP1 in hand");
            
            Vector3 secondOriginalPosition = secondMover.transform.position;
            bool secondPlaced = CardTestHelper.PlaceP1CardOnDropArea(secondMover, targetArea);
            yield return new WaitForSeconds(0.5f);
            
            // Verify second card was NOT placed
            Assert.IsFalse(secondPlaced, "Second card should NOT be placed on occupied tile");
            Assert.AreNotEqual(secondMover.gameObject, targetArea.GetOccupyingCard(), 
                "Second card should NOT occupy the tile");
            Assert.AreEqual(firstMover.gameObject, targetArea.GetOccupyingCard(), 
                "First card should still occupy the tile");
            
            // Verify second card returned to hand (position should be similar to original)
            float distanceFromOriginal = Vector3.Distance(secondMover.transform.position, secondOriginalPosition);
            Assert.Less(distanceFromOriginal, 1.0f, 
                "Second card should return to hand position after invalid drop");
        }

        [UnityTest]
        public IEnumerator Card_ReturnsToHand_OnWrongTurn()
        {
            yield return CardTestHelper.WaitForCoinTossToComplete();
            yield return new WaitForSeconds(1.0f);
            yield return CardTestHelper.ClearBoard(0.5f);
            
            NewDeckManagerP1 deckP1 = Object.FindObjectOfType<NewDeckManagerP1>();
            Assert.IsNotNull(deckP1, "P1 deck should exist");
            deckP1.InitializeDeck();
            
            if (deckP1.Hand.Count == 0)
            {
                deckP1.DrawCards(5);
                yield return new WaitForSeconds(0.5f);
            }
            
            Assert.Greater(deckP1.Hand.Count, 0, "P1 should have cards in hand");
            
            CardDropArea[] dropAreas = Object.FindObjectsOfType<CardDropArea>();
            Assert.IsTrue(dropAreas.Length > 0, "Drop areas should exist");
            
            CardDropArea targetArea = dropAreas[0];
            Assert.IsFalse(targetArea.IsOccupied, "Target area should be empty");
            
            NewHandP1UI handUI = Object.FindObjectOfType<NewHandP1UI>();
            Assert.IsNotNull(handUI, "P1 hand UI should exist");
            
            NewCard testCard = deckP1.Hand[0];
            CardMoverP1 mover = FindCardMoverInHand(handUI, testCard);
            Assert.IsNotNull(mover, "Card should have CardMoverP1 in hand");
            
            Vector3 originalPosition = mover.transform.position;
            
            // Set turn to P2 (wrong turn for P1 card)
            FateFlowController fateController = FateFlowController.Instance;
            Assert.IsNotNull(fateController, "FateFlowController should exist");
            fateController.SetFate(FateSide.P2);
            yield return new WaitForSeconds(0.2f);
            
            // Attempt to place P1 card on P2's turn (should fail)
            bool placed = CardTestHelper.PlaceP1CardOnDropArea(mover, targetArea, false);
            yield return new WaitForSeconds(0.5f);
            
            // Verify card was NOT placed
            Assert.IsFalse(placed, "P1 card should NOT be placed on P2's turn");
            Assert.IsFalse(targetArea.IsOccupied, "Tile should remain unoccupied");
            
            // Verify card returned to hand
            float distanceFromOriginal = Vector3.Distance(mover.transform.position, originalPosition);
            Assert.Less(distanceFromOriginal, 1.0f, 
                "Card should return to hand position after wrong-turn drop");
        }

        [UnityTest]
        public IEnumerator InvalidDrop_DoesNotOccupyTile()
        {
            yield return CardTestHelper.WaitForCoinTossToComplete();
            yield return new WaitForSeconds(1.0f);
            yield return CardTestHelper.ClearBoard(0.5f);
            
            CardDropArea[] dropAreas = Object.FindObjectsOfType<CardDropArea>();
            Assert.AreEqual(16, dropAreas.Length, "Should have exactly 16 drop areas");
            
            // Verify all tiles start unoccupied
            foreach (CardDropArea area in dropAreas)
            {
                Assert.IsFalse(area.IsOccupied, $"Area {area.name} should be unoccupied at start");
            }
            
            NewDeckManagerP1 deckP1 = Object.FindObjectOfType<NewDeckManagerP1>();
            Assert.IsNotNull(deckP1, "P1 deck should exist");
            deckP1.InitializeDeck();
            
            if (deckP1.Hand.Count == 0)
            {
                deckP1.DrawCards(5);
                yield return new WaitForSeconds(0.5f);
            }
            
            NewHandP1UI handUI = Object.FindObjectOfType<NewHandP1UI>();
            Assert.IsNotNull(handUI, "P1 hand UI should exist");
            
            CardDropArea targetArea = dropAreas[0];
            
            // Place a card successfully
            NewCard firstCard = deckP1.Hand[0];
            CardMoverP1 firstMover = FindCardMoverInHand(handUI, firstCard);
            Assert.IsNotNull(firstMover, "First card should have CardMoverP1");
            
            // Verify card is in hand before placement
            Assert.IsTrue(deckP1.Hand.Contains(firstCard), 
                "First card should be in hand before placement");
            
            bool placed = CardTestHelper.PlaceP1CardOnDropArea(firstMover, targetArea);
            Assert.IsTrue(placed, "Card placement should succeed");
            yield return new WaitForSeconds(0.5f);
            
            // Verify area is occupied after placement
            Assert.IsTrue(targetArea.IsOccupied, "Tile should be occupied after valid placement");
            Assert.AreEqual(firstMover.gameObject, targetArea.GetOccupyingCard(), 
                "First card should be the occupying card");
            
            // Count occupied tiles
            int occupiedCount = 0;
            foreach (CardDropArea area in dropAreas)
            {
                if (area.IsOccupied) occupiedCount++;
            }
            Assert.AreEqual(1, occupiedCount, "Exactly one tile should be occupied");
            
            // Attempt invalid placement on occupied tile
            if (deckP1.Hand.Count > 0)
            {
                NewCard secondCard = deckP1.Hand[0];
                CardMoverP1 secondMover = FindCardMoverInHand(handUI, secondCard);
                Assert.IsNotNull(secondMover, "Second card should have CardMoverP1");
                
                bool secondPlaced = CardTestHelper.PlaceP1CardOnDropArea(secondMover, targetArea);
                yield return new WaitForSeconds(0.5f);
                
                Assert.IsFalse(secondPlaced, "Second card should NOT be placed");
                
                // Verify tile occupancy count didn't change
                int occupiedCountAfter = 0;
                foreach (CardDropArea area in dropAreas)
                {
                    if (area.IsOccupied) occupiedCountAfter++;
                }
                Assert.AreEqual(1, occupiedCountAfter, 
                    "Tile occupancy count should remain 1 after invalid drop");
                Assert.IsTrue(targetArea.IsOccupied, "Target tile should still be occupied by first card");
            }
        }

        [UnityTest]
        public IEnumerator Card_Position_Resets_AfterInvalidDrop()
        {
            yield return CardTestHelper.WaitForCoinTossToComplete();
            yield return new WaitForSeconds(1.0f);
            yield return CardTestHelper.ClearBoard(0.5f);
            
            NewDeckManagerP1 deckP1 = Object.FindObjectOfType<NewDeckManagerP1>();
            Assert.IsNotNull(deckP1, "P1 deck should exist");
            deckP1.InitializeDeck();
            
            if (deckP1.Hand.Count == 0)
            {
                deckP1.DrawCards(5);
                yield return new WaitForSeconds(0.5f);
            }
            
            NewHandP1UI handUI = Object.FindObjectOfType<NewHandP1UI>();
            Assert.IsNotNull(handUI, "P1 hand UI should exist");
            
            CardDropArea[] dropAreas = Object.FindObjectsOfType<CardDropArea>();
            CardDropArea occupiedArea = dropAreas[0];
            
            // Place first card
            NewCard firstCard = deckP1.Hand[0];
            CardMoverP1 firstMover = FindCardMoverInHand(handUI, firstCard);
            Assert.IsNotNull(firstMover, "First card should have CardMoverP1");
            
            CardTestHelper.PlaceP1CardOnDropArea(firstMover, occupiedArea);
            yield return new WaitForSeconds(0.5f);
            
            Assert.IsTrue(occupiedArea.IsOccupied, "Area should be occupied");
            
            // Get second card and record its position
            Assert.Greater(deckP1.Hand.Count, 0, "Should have second card");
            NewCard secondCard = deckP1.Hand[0];
            CardMoverP1 secondMover = FindCardMoverInHand(handUI, secondCard);
            Assert.IsNotNull(secondMover, "Second card should have CardMoverP1");
            
            Vector3 positionBeforeInvalidDrop = secondMover.transform.position;
            Vector3 scaleBeforeInvalidDrop = secondMover.transform.localScale;
            
            // Attempt invalid drop
            bool placed = CardTestHelper.PlaceP1CardOnDropArea(secondMover, occupiedArea);
            yield return new WaitForSeconds(0.5f);
            
            Assert.IsFalse(placed, "Card should NOT be placed");
            
            // Verify position reset (should be close to original)
            Vector3 positionAfterInvalidDrop = secondMover.transform.position;
            float positionDelta = Vector3.Distance(positionBeforeInvalidDrop, positionAfterInvalidDrop);
            Assert.Less(positionDelta, 0.5f, 
                $"Card position should reset after invalid drop. Delta: {positionDelta}");
            
            // Verify scale unchanged (invalid drops don't apply board scale)
            Vector3 scaleAfterInvalidDrop = secondMover.transform.localScale;
            Assert.AreEqual(scaleBeforeInvalidDrop, scaleAfterInvalidDrop, 
                "Card scale should remain unchanged after invalid drop");
        }

        [UnityTest]
        public IEnumerator No_GhostReferences_AfterInvalidDrop()
        {
            yield return CardTestHelper.WaitForCoinTossToComplete();
            yield return new WaitForSeconds(1.0f);
            yield return CardTestHelper.ClearBoard(0.5f);
            
            CardDropArea[] dropAreas = Object.FindObjectsOfType<CardDropArea>();
            Assert.AreEqual(16, dropAreas.Length, "Should have exactly 16 drop areas");
            
            // Verify all areas start with no occupying card
            foreach (CardDropArea area in dropAreas)
            {
                Assert.IsFalse(area.IsOccupied, $"Area {area.name} should be unoccupied");
                Assert.IsNull(area.GetOccupyingCard(), $"Area {area.name} should have no occupying card");
            }
            
            NewDeckManagerP1 deckP1 = Object.FindObjectOfType<NewDeckManagerP1>();
            Assert.IsNotNull(deckP1, "P1 deck should exist");
            deckP1.InitializeDeck();
            
            if (deckP1.Hand.Count == 0)
            {
                deckP1.DrawCards(5);
                yield return new WaitForSeconds(0.5f);
            }
            
            NewHandP1UI handUI = Object.FindObjectOfType<NewHandP1UI>();
            Assert.IsNotNull(handUI, "P1 hand UI should exist");
            
            CardDropArea targetArea = dropAreas[0];
            
            // Place a card successfully
            NewCard firstCard = deckP1.Hand[0];
            CardMoverP1 firstMover = FindCardMoverInHand(handUI, firstCard);
            Assert.IsNotNull(firstMover, "First card should have CardMoverP1");
            
            // Verify card is in hand before placement
            // Hand is IReadOnlyList, so use LINQ Contains
            Assert.IsTrue(deckP1.Hand.Contains(firstCard), 
                "First card should be in hand before placement");
            
            bool placed = CardTestHelper.PlaceP1CardOnDropArea(firstMover, targetArea);
            Assert.IsTrue(placed, "Card placement should succeed");
            yield return new WaitForSeconds(0.5f);
            
            // Verify area is occupied after placement
            Assert.IsTrue(targetArea.IsOccupied, "Area should be occupied after successful placement");
            Assert.AreEqual(firstMover.gameObject, targetArea.GetOccupyingCard(), 
                "First card should be the occupying card");
            
            // Attempt invalid drop
            if (deckP1.Hand.Count > 0)
            {
                NewCard secondCard = deckP1.Hand[0];
                CardMoverP1 secondMover = FindCardMoverInHand(handUI, secondCard);
                Assert.IsNotNull(secondMover, "Second card should have CardMoverP1");
                
                CardTestHelper.PlaceP1CardOnDropArea(secondMover, targetArea);
                yield return new WaitForSeconds(0.5f);
                
                // Verify no ghost reference - first card should still be the only occupying card
                Assert.AreEqual(firstMover.gameObject, targetArea.GetOccupyingCard(), 
                    "First card should still be the only occupying card");
                Assert.AreNotEqual(secondMover.gameObject, targetArea.GetOccupyingCard(), 
                    "Second card should NOT be the occupying card");
                
                // Verify all other areas still have no occupying card
                foreach (CardDropArea area in dropAreas)
                {
                    if (area != targetArea)
                    {
                        Assert.IsFalse(area.IsOccupied, $"Area {area.name} should remain unoccupied");
                        Assert.IsNull(area.GetOccupyingCard(), 
                            $"Area {area.name} should have no occupying card");
                    }
                }
            }
        }

        [UnityTest]
        public IEnumerator Rapid_InvalidDrops_DoNotCorruptState()
        {
            yield return CardTestHelper.WaitForCoinTossToComplete();
            yield return new WaitForSeconds(1.0f);
            yield return CardTestHelper.ClearBoard(0.5f);
            
            NewDeckManagerP1 deckP1 = Object.FindObjectOfType<NewDeckManagerP1>();
            Assert.IsNotNull(deckP1, "P1 deck should exist");
            deckP1.InitializeDeck();
            
            if (deckP1.Hand.Count == 0)
            {
                deckP1.DrawCards(5);
                yield return new WaitForSeconds(0.5f);
            }
            
            NewHandP1UI handUI = Object.FindObjectOfType<NewHandP1UI>();
            Assert.IsNotNull(handUI, "P1 hand UI should exist");
            
            CardDropArea[] dropAreas = Object.FindObjectsOfType<CardDropArea>();
            CardDropArea targetArea = dropAreas[0];
            
            // Place a card
            NewCard firstCard = deckP1.Hand[0];
            CardMoverP1 firstMover = FindCardMoverInHand(handUI, firstCard);
            Assert.IsNotNull(firstMover, "First card should have CardMoverP1");
            
            CardTestHelper.PlaceP1CardOnDropArea(firstMover, targetArea);
            yield return new WaitForSeconds(0.5f);
            
            Assert.IsTrue(targetArea.IsOccupied, "Area should be occupied");
            
            // Perform rapid invalid drops
            int invalidDropCount = 0;
            for (int i = 0; i < Mathf.Min(3, deckP1.Hand.Count); i++)
            {
                NewCard card = deckP1.Hand[i];
                CardMoverP1 mover = FindCardMoverInHand(handUI, card);
                if (mover != null && mover != firstMover)
                {
                    bool placed = CardTestHelper.PlaceP1CardOnDropArea(mover, targetArea);
                    if (!placed) invalidDropCount++;
                    yield return null; // Minimal wait for rapid testing
                }
            }
            
            yield return new WaitForSeconds(0.5f);
            
            // Verify state is still correct
            Assert.IsTrue(targetArea.IsOccupied, "Area should still be occupied");
            Assert.AreEqual(firstMover.gameObject, targetArea.GetOccupyingCard(), 
                "First card should still be the only occupying card");
            
            // Count total occupied areas
            int occupiedCount = 0;
            foreach (CardDropArea area in dropAreas)
            {
                if (area.IsOccupied) occupiedCount++;
            }
            Assert.AreEqual(1, occupiedCount, 
                $"Exactly one area should be occupied after rapid invalid drops. Found: {occupiedCount}");
        }

        // Helper method to find CardMoverP1 in hand
        private CardMoverP1 FindCardMoverInHand(NewHandP1UI handUI, NewCard card)
        {
            if (handUI == null || card == null) return null;
            
            foreach (Transform child in handUI.transform)
            {
                CardMoverP1 mover = child.GetComponent<CardMoverP1>();
                if (mover != null && mover.Card == card)
                {
                    return mover;
                }
            }
            return null;
        }
    }
}
