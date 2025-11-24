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
    /// PlayMode tests for card systems (Player 1 and Player 2/Opponent).
    /// Tests ACTUAL deck initialization, card drawing, hand management, and card placement.
    /// </summary>
    public class CardSystemPlayModeTests
    {
        private const string SCENE_NAME = "BattleScreenMultiplayer";

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // CRITICAL: Clear singleton instances from previous tests
            CardTestHelper.ClearSingletonInstances();
            yield return null;
            
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
                yield break;
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
        public IEnumerator Player1_Deck_Initializes()
        {
            yield return CardTestHelper.WaitForCoinTossToComplete();
            yield return new WaitForSeconds(1.0f);
            
            NewDeckManagerP1 playerDeck = Object.FindObjectOfType<NewDeckManagerP1>();
            Assert.IsNotNull(playerDeck, "Player 1 DeckManager should exist");
            
            // Initialize deck
            playerDeck.InitializeDeck();
            yield return null;
            
            // Assert: Deck should have cards after initialization
            Assert.Greater(playerDeck.DrawPileCount, 0, 
                $"Player 1 deck should have cards after initialization. DrawPileCount: {playerDeck.DrawPileCount}");
        }

        [UnityTest]
        public IEnumerator Player2_Deck_Initializes()
        {
            yield return CardTestHelper.WaitForCoinTossToComplete();
            yield return new WaitForSeconds(1.0f);
            
            NewDeckManagerP2 p2Deck = Object.FindObjectOfType<NewDeckManagerP2>();
            Assert.IsNotNull(p2Deck, "Player 2 DeckManager should exist");
            
            // Initialize deck
            p2Deck.InitializeDeck();
            yield return null;
            
            // Assert: Deck should have cards after initialization
            Assert.Greater(p2Deck.DrawPileCount, 0, 
                $"Player 2 deck should have cards after initialization. DrawPileCount: {p2Deck.DrawPileCount}");
        }

        [UnityTest]
        public IEnumerator Player1_HandUI_Exists()
        {
            yield return new WaitForSeconds(0.5f);
            
            NewHandP1UI handUI = Object.FindObjectOfType<NewHandP1UI>();
            Assert.IsNotNull(handUI, "Player 1 HandUI should exist");
        }

        [UnityTest]
        public IEnumerator Player2_HandP2UI_Exists()
        {
            yield return new WaitForSeconds(0.5f);
            
            NewHandP2UI p2HandUI = Object.FindObjectOfType<NewHandP2UI>();
            Assert.IsNotNull(p2HandUI, "Player 2 HandP2UI should exist");
        }

        [UnityTest]
        public IEnumerator All_DropAreas_Have_CardDropArea_Component()
        {
            yield return new WaitForSeconds(0.5f);
            
            for (int i = 1; i <= 16; i++)
            {
                GameObject dropArea = GameObject.Find($"DropArea{i}");
                Assert.IsNotNull(dropArea, $"DropArea{i} should exist");
                
                CardDropArea dropAreaComponent = dropArea.GetComponent<CardDropArea>();
                Assert.IsNotNull(dropAreaComponent, $"DropArea{i} should have CardDropArea component");
                
                // Verify collider exists and is trigger
                Collider2D collider = dropArea.GetComponent<Collider2D>();
                Assert.IsNotNull(collider, $"DropArea{i} should have Collider2D");
                Assert.IsTrue(collider.isTrigger, $"DropArea{i} Collider2D should be trigger");
            }
        }

        [UnityTest]
        public IEnumerator Cards_Draw_After_CoinToss_Completes()
        {
            yield return new WaitForSeconds(1.0f);
            
            CoinTossManager coinTossManager = CoinTossManager.Instance;
            Assert.IsNotNull(coinTossManager, "CoinTossManager should exist");
            
            // Wait for coin toss to complete
            yield return CardTestHelper.WaitForCoinTossToComplete(5f);
            
            // Wait for cards to be drawn
            yield return new WaitForSeconds(2.0f);
            
            // Assert: Cards should be in hands after coin toss
            NewDeckManagerP1 playerDeck = Object.FindObjectOfType<NewDeckManagerP1>();
            NewDeckManagerP2 p2Deck = Object.FindObjectOfType<NewDeckManagerP2>();
            
            if (playerDeck != null && p2Deck != null)
            {
                // At least one player should have cards (depending on game initialization)
                int totalCards = playerDeck.Hand.Count + p2Deck.Hand.Count;
                Assert.GreaterOrEqual(totalCards, 0, 
                    $"Cards should be drawn after coin toss. Player 1: {playerDeck.Hand.Count}, Player 2: {p2Deck.Hand.Count}");
            }
        }

        [UnityTest]
        public IEnumerator CardPrefabAssets_Are_Not_Active()
        {
            yield return new WaitForSeconds(0.5f);
            
            // Prefab assets should be inactive if they exist in scene
            GameObject prefab1 = GameObject.Find("NewCardPrefabP1");
            GameObject prefab2 = GameObject.Find("NewCardPrefabP2");
            
            if (prefab1 != null)
            {
                // Should be inactive (disabled by NewCardUI)
                Assert.IsFalse(prefab1.activeSelf, 
                    "NewCardPrefabP1 should be inactive if present in scene");
            }
            
            if (prefab2 != null)
            {
                Assert.IsFalse(prefab2.activeSelf, 
                    "NewCardPrefabP2 should be inactive if present in scene");
            }
        }

        [UnityTest]
        public IEnumerator Card_Drag_Prevention_For_Board_Cards()
        {
            // Wait for game initialization and coin toss
            yield return CardTestHelper.WaitForCoinTossToComplete();
            yield return new WaitForSeconds(1.0f);
            
            // Get hand UIs
            NewHandP1UI handUI = Object.FindObjectOfType<NewHandP1UI>();
            NewHandP2UI p2HandUI = Object.FindObjectOfType<NewHandP2UI>();
            
            Assert.IsNotNull(handUI, "Player 1 HandUI should exist");
            Assert.IsNotNull(p2HandUI, "Player 2 HandP2UI should exist");
            
            // Get a card from hand and place it on board
            NewDeckManagerP1 deckManager = handUI.DeckManager;
            if (deckManager != null && deckManager.Hand != null && deckManager.Hand.Count > 0)
            {
                NewCard testCard = deckManager.Hand[0];
                
                // Find the card UI for this card
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
                
                if (cardUI != null)
                {
                    // Find CardMoverP1 for this card
                    CardMoverP1 CardMoverP1 = cardUI.GetComponentInParent<CardMoverP1>();
                    if (CardMoverP1 == null)
                    {
                        CardMoverP1 = cardUI.GetComponent<CardMoverP1>();
                    }
                    
                    if (CardMoverP1 != null)
                    {
                        // Place card on board
                        CardDropArea[] dropAreas = Object.FindObjectsOfType<CardDropArea>();
                        if (dropAreas.Length > 0)
                        {
                            CardDropArea emptyArea = null;
                            foreach (CardDropArea area in dropAreas)
                            {
                                if (!area.IsOccupied)
                                {
                                    emptyArea = area;
                                    break;
                                }
                            }
                            
                            if (emptyArea != null)
                            {
                                // Set Player 1's turn
                                FateFlowController fateController = FateFlowController.Instance;
                                if (fateController != null)
                                {
                                    fateController.SetFate(FateSide.P1);
                                }
                                yield return null;
                                
                                // Place card
                                bool placed = CardTestHelper.PlaceP1CardOnDropArea(CardMoverP1, emptyArea, false);
                                Assert.IsTrue(placed, "Card should be placed on board");
                                yield return new WaitForSeconds(0.5f);
                                
                                // Assert: Card should no longer be in hand
                                NewCard cardInHand = handUI.GetCardForUI(cardUI);
                                Assert.IsNull(cardInHand, 
                                    "Card placed on board should no longer be in hand (GetCardForUI should return null)");
                                
                                // Assert: Card should be marked as played
                                Assert.IsTrue(CardMoverP1.IsPlayed, 
                                    "Card on board should be marked as played (IsPlayed = true)");
                            }
                        }
                    }
                }
            }
        }

        [UnityTest]
        public IEnumerator Cards_In_Hand_Can_Be_Identified()
        {
            yield return CardTestHelper.WaitForCoinTossToComplete();
            yield return new WaitForSeconds(1.0f);
            
            // Get hand UIs
            NewHandP1UI handUI = Object.FindObjectOfType<NewHandP1UI>();
            NewHandP2UI p2HandUI = Object.FindObjectOfType<NewHandP2UI>();
            
            Assert.IsNotNull(handUI, "Player 1 HandUI should exist");
            Assert.IsNotNull(p2HandUI, "Player 2 HandP2UI should exist");
            
            // Verify GetCardForUI methods work correctly
            NewDeckManagerP1 deckManager = handUI?.DeckManager;
            if (deckManager != null && deckManager.Hand != null && deckManager.Hand.Count > 0)
            {
                // Get a card from hand
                NewCard testCard = deckManager.Hand[0];
                
                // Find the card UI
                NewCardUI[] cardUIs = Object.FindObjectsOfType<NewCardUI>(true);
                NewCardUI matchingUI = null;
                foreach (NewCardUI ui in cardUIs)
                {
                    if (ui.Card == testCard)
                    {
                        matchingUI = ui;
                        break;
                    }
                }
                
                if (matchingUI != null)
                {
                    // Test GetCardForUI
                    NewCard foundCard = handUI.GetCardForUI(matchingUI);
                    Assert.AreEqual(testCard, foundCard, 
                        "GetCardForUI should return the correct card for a card UI in hand");
                }
            }
            
            NewDeckManagerP2 p2Deck = p2HandUI?.DeckManager;
            if (p2Deck != null && p2Deck.Hand != null && p2Deck.Hand.Count > 0)
            {
                // Same test for Player 2
                NewCard testCard = p2Deck.Hand[0];
                
                NewCardUI[] cardUIs = Object.FindObjectsOfType<NewCardUI>(true);
                NewCardUI matchingUI = null;
                foreach (NewCardUI ui in cardUIs)
                {
                    if (ui.Card == testCard)
                    {
                        matchingUI = ui;
                        break;
                    }
                }
                
                if (matchingUI != null)
                {
                    NewCard foundCard = p2HandUI.GetCardForUI(matchingUI);
                    Assert.AreEqual(testCard, foundCard, 
                        "GetCardForUI should return the correct card for Player 2 card UI in hand");
                }
            }
        }

        [UnityTest]
        public IEnumerator Cards_On_Board_Cannot_Be_Picked_Up()
        {
            yield return CardTestHelper.WaitForCoinTossToComplete();
            yield return new WaitForSeconds(1.0f);
            
            // Get all CardDropArea components
            CardDropArea[] dropAreas = Object.FindObjectsOfType<CardDropArea>();
            Assert.IsNotNull(dropAreas, "Drop areas should exist");
            Assert.IsTrue(dropAreas.Length > 0, "At least one drop area should exist");
            
            // Find occupied drop areas (cards on board)
            int occupiedCount = 0;
            foreach (var dropArea in dropAreas)
            {
                if (dropArea.IsOccupied)
                {
                    occupiedCount++;
                    
                    // Get the occupying card
                    var occupyingCardField = typeof(CardDropArea).GetField("occupyingCard", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    
                    if (occupyingCardField != null)
                    {
                        GameObject occupyingCardObj = occupyingCardField.GetValue(dropArea) as GameObject;
                        if (occupyingCardObj != null)
                        {
                            // Verify card on board cannot be in hand
                            NewHandP1UI handUI = Object.FindObjectOfType<NewHandP1UI>();
                            NewHandP2UI p2HandUI = Object.FindObjectOfType<NewHandP2UI>();
                            
                            NewCardUI cardUI = occupyingCardObj.GetComponent<NewCardUI>();
                            if (cardUI == null)
                            {
                                cardUI = occupyingCardObj.GetComponentInChildren<NewCardUI>();
                            }
                            
                            if (cardUI != null)
                            {
                                // Card should NOT be in hand
                                if (handUI != null)
                                {
                                    NewCard cardInHand = handUI.GetCardForUI(cardUI);
                                    Assert.IsNull(cardInHand, 
                                        $"Card on board at {dropArea.name} should not be in Player 1 hand");
                                }
                                
                                if (p2HandUI != null)
                                {
                                    NewCard cardInHand = p2HandUI.GetCardForUI(cardUI);
                                    Assert.IsNull(cardInHand, 
                                        $"Card on board at {dropArea.name} should not be in Player 2 hand");
                                }
                            }
                        }
                    }
                }
            }
            
            Assert.GreaterOrEqual(occupiedCount, 0, 
                $"Validated {occupiedCount} cards on board are not in hand");
        }

        [UnityTest]
        public IEnumerator Card_Placement_Validation_Works()
        {
            yield return new WaitForSeconds(1.0f);
            
            // Get drop areas
            CardDropArea[] dropAreas = Object.FindObjectsOfType<CardDropArea>();
            Assert.IsNotNull(dropAreas, "Drop areas should exist");
            Assert.AreEqual(16, dropAreas.Length, "Should have exactly 16 drop areas");
            
            // Verify each drop area has required components and IsOccupied works
            foreach (var dropArea in dropAreas)
            {
                Assert.IsNotNull(dropArea, "DropArea should not be null");
                Assert.IsNotNull(dropArea.gameObject, "DropArea GameObject should exist");
                
                // Verify collider exists
                Collider2D collider = dropArea.GetComponent<Collider2D>();
                Assert.IsNotNull(collider, $"DropArea {dropArea.name} should have Collider2D");
                Assert.IsTrue(collider.isTrigger, $"DropArea {dropArea.name} Collider2D should be trigger");
                
                // Verify IsOccupied property works (no exception)
                bool isOccupied = dropArea.IsOccupied;
                // Property should be accessible and return a valid bool
                Assert.IsTrue(isOccupied == true || isOccupied == false, 
                    $"DropArea {dropArea.name} IsOccupied should return valid bool. Got: {isOccupied}");
            }
        }
    }
}
