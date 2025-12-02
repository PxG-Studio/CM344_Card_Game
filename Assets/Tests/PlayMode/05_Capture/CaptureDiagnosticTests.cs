using System.Collections;
using System.Collections.Generic;
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
    /// Diagnostic tests to trace why captures aren't completing in ChainCapture_ResolvesInDeterministicOrder
    /// </summary>
    public class CaptureDiagnosticTests
    {
        private const string SCENE_NAME = "BattleScreenMultiplayer";

        [UnitySetUp]
        public IEnumerator SetUp()
        {
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
            }
            
            yield return new WaitForSeconds(0.5f);
            CardTestHelper.ResetGameState();
            yield return null;
        }
        
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return null;
            CardTestHelper.ClearSingletonInstances();
            yield return null;
        }

        [UnityTest]
        public IEnumerator Diagnostic_WhyAreCardsNotCaptured()
        {
            yield return CardTestHelper.WaitForCoinTossToComplete();
            yield return new WaitForSeconds(1.0f);
            yield return CardTestHelper.ClearBoard(0.5f);

            CardDropArea[] dropAreas = Object.FindObjectsOfType<CardDropArea>();
            Assert.IsTrue(dropAreas.Length >= 3, "Need at least 3 drop areas");

            // Find 3 adjacent areas
            CardDropArea area1 = dropAreas[0];
            CardDropArea area2 = null;
            CardDropArea area3 = null;
            
            float adjacentDistance = 3.0f;
            Vector3 area1Pos = area1.transform.position;
            
            foreach (CardDropArea area in dropAreas)
            {
                if (area == area1) continue;
                float dist = Vector3.Distance(area1Pos, area.transform.position);
                if (dist <= adjacentDistance && area2 == null)
                {
                    area2 = area;
                }
                else if (dist <= adjacentDistance && area3 == null && area != area2)
                {
                    area3 = area;
                }
            }
            
            Assert.IsNotNull(area2, "Could not find area2 adjacent to area1");
            Assert.IsNotNull(area3, "Could not find area3 adjacent to area1");

            // Create test cards
            NewCard chainStarter = CardTestHelper.CreateTestCard(5, 5, 5, 5, "ChainStarter");
            NewCard weakCard1 = CardTestHelper.CreateTestCard(2, 2, 2, 2, "Weak1");
            NewCard weakCard2 = CardTestHelper.CreateTestCard(2, 2, 2, 2, "Weak2");

            // Get deck managers
            NewDeckManagerP2 p2Deck = Object.FindObjectOfType<NewDeckManagerP2>();
            NewDeckManagerP1 playerDeck = Object.FindObjectOfType<NewDeckManagerP1>();
            Assert.IsNotNull(p2Deck, "P2 deck should exist");
            Assert.IsNotNull(playerDeck, "Player deck should exist");

            // Add cards to hands
            CardTestHelper.AddCardToDeckManagerHand(p2Deck, weakCard1);
            CardTestHelper.AddCardToDeckManagerHand(p2Deck, weakCard2);
            CardTestHelper.AddCardToDeckManagerHand(playerDeck, chainStarter);

            FateFlowController fateController = FateFlowController.Instance;
            if (fateController != null)
            {
                fateController.SetFate(FateSide.P2);
            }
            yield return null;

            // Place Weak1
            CardMoverP2 weak1Mover = CardTestHelper.CreateCardMoverP2WithCard(weakCard1, area2.transform.position);
            bool weak1Placed = CardTestHelper.PlaceP2CardOnDropArea(weak1Mover, area2, true);
            Assert.IsTrue(weak1Placed, "Weak1 should be placed");
            yield return new WaitForSeconds(0.5f);
            
            Debug.Log($"[Diagnostic] After Weak1 placement: IsOccupied={area2.IsOccupied}, " +
                     $"IsFreshlyPlayed={IsFreshlyPlayed(weak1Mover.gameObject)}");

            // Place Weak2
            if (fateController != null)
            {
                fateController.SetFate(FateSide.P2);
                yield return null;
            }
            
            CardMoverP2 weak2Mover = CardTestHelper.CreateCardMoverP2WithCard(weakCard2, area3.transform.position);
            bool weak2Placed = CardTestHelper.PlaceP2CardOnDropArea(weak2Mover, area3, true);
            Assert.IsTrue(weak2Placed, "Weak2 should be placed");
            yield return new WaitForSeconds(0.5f);
            
            Debug.Log($"[Diagnostic] After Weak2 placement: IsOccupied={area3.IsOccupied}, " +
                     $"IsFreshlyPlayed={IsFreshlyPlayed(weak2Mover.gameObject)}");

            // Switch to Player 1 and place ChainStarter
            if (fateController != null)
            {
                fateController.SetFate(FateSide.Player);
            }
            yield return new WaitForSeconds(0.5f); // Wait for turn to switch
            
            Debug.Log($"[Diagnostic] Before ChainStarter placement: " +
                     $"Weak1 IsFreshlyPlayed={IsFreshlyPlayed(weak1Mover.gameObject)}, " +
                     $"Weak2 IsFreshlyPlayed={IsFreshlyPlayed(weak2Mover.gameObject)}");

            CardMoverP1 starterMover = CardTestHelper.CreateCardMoverWithCard(chainStarter, area1.transform.position, true);
            bool starterPlaced = CardTestHelper.PlaceP1CardOnDropArea(starterMover, area1, true);
            Assert.IsTrue(starterPlaced, "ChainStarter should be placed");
            yield return new WaitForSeconds(0.5f);

            // Wait for battle checks
            yield return new WaitForSeconds(2f);

            // Check capture status
            bool weak1Captured = CardTestHelper.IsCardCaptured(weak1Mover.gameObject);
            bool weak2Captured = CardTestHelper.IsCardCaptured(weak2Mover.gameObject);

            Debug.Log($"[Diagnostic] Final status: Weak1 captured={weak1Captured}, Weak2 captured={weak2Captured}");
            
            // Get CardFlipAnimation components
            CardFlipAnimation flip1 = weak1Mover.gameObject.GetComponentInChildren<CardFlipAnimation>();
            CardFlipAnimation flip2 = weak2Mover.gameObject.GetComponentInChildren<CardFlipAnimation>();
            
            if (flip1 != null)
            {
                // Note: WasCaptured and LastCaptureColor properties removed in develop-1 revert
                Debug.Log($"[Diagnostic] Weak1 CardFlipAnimation: isFlipped={flip1.isFlipped}, isAnimating={flip1.isAnimating}");
            }
            
            if (flip2 != null)
            {
                // Note: WasCaptured and LastCaptureColor properties removed in develop-1 revert
                Debug.Log($"[Diagnostic] Weak2 CardFlipAnimation: isFlipped={flip2.isFlipped}, isAnimating={flip2.isAnimating}");
            }

            // This test is diagnostic - we're not asserting, just logging
            yield return new WaitForSeconds(1f);
        }

        private bool IsFreshlyPlayed(GameObject cardObject)
        {
            CardDropArea dropArea = Object.FindObjectOfType<CardDropArea>();
            if (dropArea == null) return false;
            
            var cardsPlayedField = typeof(CardDropArea).GetField("cardsPlayedThisTurn",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (cardsPlayedField == null) return false;
            
            var cardsPlayed = cardsPlayedField.GetValue(dropArea) as HashSet<GameObject>;
            if (cardsPlayed == null) return false;
            
            return cardsPlayed.Contains(cardObject);
        }
    }
}

