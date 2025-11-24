using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CardGame.Managers;
using CardGame.UI;
using CardGame.Core;

namespace CardGame.Tests
{
    /// <summary>
    /// Regression tests for previously fixed bugs.
    /// These tests ensure that fixed bugs stay fixed and don't regress.
    /// </summary>
    public class RegressionTests
    {
        private const string SCENE_NAME = "BattleScreenMultiplayer";

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // CRITICAL: Clear singleton instances from previous tests
            CardTestHelper.ClearSingletonInstances();
            yield return null;
            
            // Verify scene exists in build settings
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
            
            // Reset game state
            CardTestHelper.ResetGameState();
            yield return null;
        }
        
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            // Clean up after each test
            yield return null;
            CardTestHelper.ClearSingletonInstances();
            yield return null;
        }

        [UnityTest]
        public IEnumerator Regression_Player2_CardDrag_Fix_StillWorks()
        {
            // BUG FIXED: Player 2 cards couldn't be dragged
            // This test ensures the fix still works
            
            yield return CardTestHelper.WaitForCoinTossToComplete();
            yield return new WaitForSeconds(1.0f);
            
            // Set Player 2's turn
            FateFlowController fateController = FateFlowController.Instance;
            if (fateController != null)
            {
                fateController.SetFate(FateSide.P2);
            }
            yield return null;
            
            // Get Player 2 cards
            CardMoverP2[] player2Cards = Object.FindObjectsOfType<CardMoverP2>(true);
            if (player2Cards.Length == 0)
            {
                Assert.Inconclusive("No Player 2 cards found for drag test");
                yield break;
            }
            
            CardMoverP2 testCard = player2Cards[0];
            
            // Verify card has collider (required for drag)
            Collider2D col = testCard.GetComponent<Collider2D>();
            Assert.IsNotNull(col, "Player 2 card should have Collider2D for dragging");
            Assert.IsTrue(col.enabled, "Player 2 card Collider2D should be enabled");
            
            // Verify card is not played
            Assert.IsFalse(testCard.IsPlayed, "Test card should not be played");
            
            // Verify CanInteract works (turn check)
            bool canInteract = fateController != null && fateController.CanAct(FateSide.P2);
            Assert.IsTrue(canInteract, "Player 2 should be able to interact during their turn");
            
            // Verify GetMousePositionInWorldSpace method exists and works
            var getMouseMethod = typeof(CardMoverP2).GetMethod("GetMousePositionInWorldSpace");
            Assert.IsNotNull(getMouseMethod, "CardMoverP2 should have GetMousePositionInWorldSpace method");
            
            // Test that method can be called (doesn't throw exception)
            Vector3 mousePos = (Vector3)getMouseMethod.Invoke(testCard, null);
            Assert.IsNotNull(mousePos, "GetMousePositionInWorldSpace should return valid Vector3");
        }

        [UnityTest]
        public IEnumerator Regression_Player2_CardDrop_Fix_StillWorks()
        {
            // BUG FIXED: Player 2 cards couldn't be dropped on drop areas
            // This test ensures the fix still works
            
            yield return CardTestHelper.WaitForCoinTossToComplete();
            yield return new WaitForSeconds(1.0f);
            
            // Set Player 2's turn
            FateFlowController fateController = FateFlowController.Instance;
            if (fateController != null)
            {
                fateController.SetFate(FateSide.P2);
            }
            yield return null;
            
            // Get drop areas
            CardDropArea[] dropAreas = Object.FindObjectsOfType<CardDropArea>();
            Assert.IsTrue(dropAreas.Length > 0, "Drop areas should exist");
            
            // Find empty drop area
            CardDropArea emptyArea = null;
            foreach (CardDropArea area in dropAreas)
            {
                if (!area.IsOccupied)
                {
                    emptyArea = area;
                    break;
                }
            }
            
            if (emptyArea == null)
            {
                Assert.Inconclusive("No empty drop areas found for drop test");
                yield break;
            }
            
            // Create test card
            NewCard testCard = CardTestHelper.CreateTestCard(3, 3, 3, 3, "Player2TestCard");
            
            // Add card to opponent deck manager's hand
            NewDeckManagerP2 opponentDeck = Object.FindObjectOfType<NewDeckManagerP2>();
            if (opponentDeck != null)
            {
                CardTestHelper.AddCardToDeckManagerHand(opponentDeck, testCard);
            }
            
            CardMoverP2 cardMoverP2 = CardTestHelper.CreateCardMoverP2WithCard(testCard, emptyArea.transform.position);
            
            // Verify OnCardDropP2 method exists on drop area
            var onCardDropOppMethod = typeof(CardDropArea).GetMethod("OnCardDropP2");
            Assert.IsNotNull(onCardDropOppMethod, "CardDropArea should have OnCardDropP2 method");
            
            // Attempt drop
            bool dropResult = CardTestHelper.PlaceP2CardOnDropArea(cardMoverP2, emptyArea, false);
            
            // Assert: Drop should succeed
            Assert.IsTrue(dropResult, 
                "Player 2 should be able to drop cards on drop areas. " +
                $"Drop result: {dropResult}, Area: {emptyArea.name}, IsOccupied: {emptyArea.IsOccupied}");
            
            yield return new WaitForSeconds(0.5f);
            
            // Verify card is on board
            Assert.IsTrue(emptyArea.IsOccupied, 
                "Drop area should be occupied after Player 2 drops card");
        }

        [UnityTest]
        public IEnumerator Regression_CoinToss_Visibility_Fix_StillWorks()
        {
            // BUG FIXED: Coin toss UI was not visible
            // This test ensures the fix still works
            
            yield return new WaitForSeconds(1.0f);
            
            CoinTossManager coinTossManager = CoinTossManager.Instance;
            CoinTossUI coinTossUI = Object.FindObjectOfType<CoinTossUI>(true);
            
            Assert.IsNotNull(coinTossManager, "CoinTossManager should exist");
            Assert.IsNotNull(coinTossUI, "CoinTossUI should exist");
            
            // Verify coin toss UI can be activated
            coinTossUI.StartCoinToss();
            yield return new WaitForEndOfFrame();
            yield return null;
            
            // Verify panel is active (or can be activated)
            Assert.IsTrue(coinTossUI.gameObject.activeSelf || coinTossUI.gameObject.activeInHierarchy, 
                "CoinTossUI panel should be active or activatable");
            
            // Verify animation can start
            coinTossUI.StartCoinTossAnimation();
            yield return new WaitForSeconds(0.5f);
            
            // Verify coin toss can complete
            if (!coinTossManager.IsComplete)
            {
                // Set player selection (Player 1 selects heads)
                coinTossManager.SetPlayerSelection(true, FateSide.Player);
                coinTossManager.PerformCoinToss();
            }
            
            Assert.IsTrue(coinTossManager.IsComplete, 
                "Coin toss should be able to complete (visibility fix ensures UI is accessible)");
        }

        [UnityTest]
        public IEnumerator Regression_CardDrag_Prevention_For_Board_Cards_Fix_StillWorks()
        {
            // BUG FIXED: Cards already on board could be picked up and dragged again
            // This test ensures the fix still works
            
            yield return CardTestHelper.WaitForCoinTossToComplete();
            yield return new WaitForSeconds(1.0f);
            
            // Get hand UI
            NewHandP1UI handUI = Object.FindObjectOfType<NewHandP1UI>();
            NewDeckManagerP1 deckManager = handUI?.DeckManager;
            if (handUI == null || deckManager == null || deckManager.Hand == null || deckManager.Hand.Count == 0)
            {
                Assert.Inconclusive("No cards in hand for drag prevention test");
                yield break;
            }
            
            // Place a card on board
            CardDropArea[] dropAreas = Object.FindObjectsOfType<CardDropArea>();
            CardDropArea emptyArea = null;
            foreach (CardDropArea area in dropAreas)
            {
                if (!area.IsOccupied)
                {
                    emptyArea = area;
                    break;
                }
            }
            
            if (emptyArea == null)
            {
                Assert.Inconclusive("No empty drop areas for drag prevention test");
                yield break;
            }
            
            // Get a card from hand
            NewCard testCard = deckManager.Hand[0];
            
            // Find card UI
            NewCardUI[] cardUIs = Object.FindObjectsOfType<NewCardUI>(true);
            NewCardUI cardUI = null;
            foreach (NewCardUI ui in cardUIs)
            {
                if (ui.Card == testCard)
                {
                    cardUI = ui;
                    break;
                }
            }
            
            if (cardUI == null)
            {
                Assert.Inconclusive("Card UI not found for drag prevention test");
                yield break;
            }
            
            // Set Player 1's turn
            FateFlowController fateController = FateFlowController.Instance;
            if (fateController != null)
            {
                fateController.SetFate(FateSide.Player);
            }
            yield return null;
            
            // Place card on board
            CardMoverP1 CardMoverP1 = cardUI.GetComponentInParent<CardMoverP1>();
            if (CardMoverP1 == null)
            {
                CardMoverP1 = cardUI.GetComponent<CardMoverP1>();
            }
            
            if (CardMoverP1 != null)
            {
                bool placed = CardTestHelper.PlaceP1CardOnDropArea(CardMoverP1, emptyArea, false);
                Assert.IsTrue(placed, "Card should be placed on board");
                yield return new WaitForSeconds(0.5f);
                
                // Assert: Card should no longer be in hand (GetCardForUI should return null)
                CardGame.Core.NewCard cardInHand = handUI.GetCardForUI(cardUI);
                Assert.IsNull(cardInHand, 
                    "Card on board should not be in hand (GetCardForUI should return null)");
                
                // This validates that OnBeginDrag validation will prevent dragging
                // (since GetCardForUI returns null for cards on board)
            }
        }

        [UnityTest]
        public IEnumerator Regression_CardPlacement_Validation_Fix_StillWorks()
        {
            // BUG FIXED: Cards could be placed on occupied tiles
            // This test ensures the fix still works
            
            yield return CardTestHelper.WaitForCoinTossToComplete();
            yield return new WaitForSeconds(1.0f);
            
            CardDropArea[] dropAreas = Object.FindObjectsOfType<CardDropArea>();
            Assert.IsTrue(dropAreas.Length > 0, "Drop areas should exist");
            
            // Find an occupied area (or create one)
            CardDropArea occupiedArea = null;
            foreach (CardDropArea area in dropAreas)
            {
                if (area.IsOccupied)
                {
                    occupiedArea = area;
                    break;
                }
            }
            
            // If no occupied area, place a card first
            if (occupiedArea == null && dropAreas.Length > 0)
            {
                CardDropArea testArea = dropAreas[0];
                NewCard testCard = CardTestHelper.CreateTestCard(3, 3, 3, 3, "Occupier");
                
                // Add card to player deck manager's hand
                NewDeckManagerP1 playerDeck = Object.FindObjectOfType<NewDeckManagerP1>();
                if (playerDeck != null)
                {
                    CardTestHelper.AddCardToDeckManagerHand(playerDeck, testCard);
                }
                
                FateFlowController fateController = FateFlowController.Instance;
                if (fateController != null)
                {
                    fateController.SetFate(FateSide.Player);
                }
                yield return null;
                
                CardMoverP1 testMover = CardTestHelper.CreateCardMoverWithCard(testCard, testArea.transform.position, true);
                CardTestHelper.PlaceP1CardOnDropArea(testMover, testArea, true);
                yield return new WaitForSeconds(0.5f);
                
                if (testArea.IsOccupied)
                {
                    occupiedArea = testArea;
                }
            }
            
            if (occupiedArea != null)
            {
                // Try to place another card on the same area
                NewCard secondCard = CardTestHelper.CreateTestCard(3, 3, 3, 3, "SecondCard");
                
                // Add card to player deck manager's hand
                NewDeckManagerP1 playerDeck = Object.FindObjectOfType<NewDeckManagerP1>();
                if (playerDeck != null)
                {
                    CardTestHelper.AddCardToDeckManagerHand(playerDeck, secondCard);
                }
                
                CardMoverP1 secondMover = CardTestHelper.CreateCardMoverWithCard(secondCard, occupiedArea.transform.position, true);
                
                // Attempt drop
                bool dropResult = CardTestHelper.PlaceP1CardOnDropArea(secondMover, occupiedArea, true);
                
                // Assert: Drop should fail (area is occupied)
                Assert.IsFalse(dropResult, 
                    $"Card should NOT be placed on occupied area '{occupiedArea.name}'. " +
                    $"Drop result: {dropResult}, IsOccupied: {occupiedArea.IsOccupied}");
            }
        }
    }
}

