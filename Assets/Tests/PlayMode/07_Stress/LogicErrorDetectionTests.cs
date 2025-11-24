using System.Collections;
using System.Reflection;
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
    /// Tests specifically designed to catch LOGIC ERRORS in game systems.
    /// These tests validate that game logic produces correct results.
    /// </summary>
    public class LogicErrorDetectionTests
    {
        private const string SCENE_NAME = "BattleScreenMultiplayer";

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // CRITICAL: Clear singleton instances from previous tests
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
        public IEnumerator LogicError_CaptureCalculation_AttackerHigher_ShouldCapture()
        {
            // LOGIC TEST: When attacker stat > defender stat, capture MUST occur
            yield return CardTestHelper.WaitForCoinTossToComplete();
            yield return new WaitForSeconds(1.0f);
            
            CardDropArea[] dropAreas = Object.FindObjectsOfType<CardDropArea>();
            Assert.IsTrue(dropAreas.Length >= 2, "Need at least 2 drop areas");
            
            CardDropArea attackerArea = dropAreas[0];
            CardDropArea defenderArea = CardTestHelper.GetAdjacentDropArea(attackerArea, "right") ?? dropAreas[1];
            
            // Create cards with known stats: Attacker right=7, Defender left=3
            NewCard attackerCard = CardTestHelper.CreateTestCard(3, 7, 3, 3, "StrongAttacker");
            NewCard defenderCard = CardTestHelper.CreateTestCard(3, 3, 3, 3, "WeakDefender");
            
            // Add cards to deck manager hands
            NewDeckManagerP1 playerDeck = Object.FindObjectOfType<NewDeckManagerP1>();
            NewDeckManagerP2 opponentDeck = Object.FindObjectOfType<NewDeckManagerP2>();
            if (playerDeck != null)
            {
                CardTestHelper.AddCardToDeckManagerHand(playerDeck, attackerCard);
            }
            if (opponentDeck != null)
            {
                CardTestHelper.AddCardToDeckManagerHand(opponentDeck, defenderCard);
            }
            
            // Verify stats are correct
            Assert.AreEqual(7, attackerCard.CurrentRightStat, "Attacker right stat should be 7");
            Assert.AreEqual(3, defenderCard.CurrentLeftStat, "Defender left stat should be 3");
            Assert.Greater(attackerCard.CurrentRightStat, defenderCard.CurrentLeftStat, 
                "Attacker should be higher (7 > 3)");
            
            FateFlowController fateController = FateFlowController.Instance;
            if (fateController != null)
            {
                fateController.SetFate(FateSide.Player);
            }
            yield return null;
            
            // Place cards
            CardMoverP1 attackerMover = CardTestHelper.CreateCardMoverWithCard(attackerCard, attackerArea.transform.position, true);
            CardTestHelper.PlaceP1CardOnDropArea(attackerMover, attackerArea, true);
            yield return new WaitForSeconds(0.5f);
            
            CardMoverP2 defenderMover = CardTestHelper.CreateCardMoverP2WithCard(defenderCard, defenderArea.transform.position);
            CardTestHelper.PlaceP2CardOnDropArea(defenderMover, defenderArea, true);
            yield return CardTestHelper.WaitForCaptureAnimations(3f);
            
            // LOGIC ASSERTION: Capture MUST occur when attacker > defender
            bool defenderCaptured = CardTestHelper.IsCardCaptured(defenderMover.gameObject);
            Assert.IsTrue(defenderCaptured, 
                $"LOGIC ERROR: Defender should be captured when attacker stat ({attackerCard.CurrentRightStat}) > defender stat ({defenderCard.CurrentLeftStat}). " +
                $"This is a logic error if capture did not occur.");
        }

        [UnityTest]
        public IEnumerator LogicError_CaptureCalculation_DefenderHigher_ShouldNotCapture()
        {
            // LOGIC TEST: When defender stat > attacker stat, capture MUST NOT occur
            yield return CardTestHelper.WaitForCoinTossToComplete();
            yield return new WaitForSeconds(1.0f);
            
            CardDropArea[] dropAreas = Object.FindObjectsOfType<CardDropArea>();
            Assert.IsTrue(dropAreas.Length >= 2, "Need at least 2 drop areas");
            
            CardDropArea attackerArea = dropAreas[0];
            CardDropArea defenderArea = CardTestHelper.GetAdjacentDropArea(attackerArea, "right") ?? dropAreas[1];
            
            // Create cards: Attacker right=2, Defender left=8
            // CreateTestCard parameters: (top, right, down, left)
            NewCard attackerCard = CardTestHelper.CreateTestCard(3, 2, 3, 3, "WeakAttacker"); // right=2
            NewCard defenderCard = CardTestHelper.CreateTestCard(3, 3, 3, 8, "StrongDefender"); // left=8
            
            // Add cards to deck manager hands
            NewDeckManagerP1 playerDeck = Object.FindObjectOfType<NewDeckManagerP1>();
            NewDeckManagerP2 opponentDeck = Object.FindObjectOfType<NewDeckManagerP2>();
            if (playerDeck != null)
            {
                CardTestHelper.AddCardToDeckManagerHand(playerDeck, attackerCard);
            }
            if (opponentDeck != null)
            {
                CardTestHelper.AddCardToDeckManagerHand(opponentDeck, defenderCard);
            }
            
            Assert.AreEqual(2, attackerCard.CurrentRightStat, "Attacker right stat should be 2");
            Assert.AreEqual(8, defenderCard.CurrentLeftStat, "Defender left stat should be 8");
            Assert.Less(attackerCard.CurrentRightStat, defenderCard.CurrentLeftStat, 
                "Defender should be higher (8 > 2)");
            
            FateFlowController fateController = FateFlowController.Instance;
            if (fateController != null)
            {
                fateController.SetFate(FateSide.Player);
            }
            yield return null;
            
            CardMoverP1 attackerMover = CardTestHelper.CreateCardMoverWithCard(attackerCard, attackerArea.transform.position, true);
            CardTestHelper.PlaceP1CardOnDropArea(attackerMover, attackerArea, true);
            yield return new WaitForSeconds(0.5f);
            
            // Ensure defender is placed exactly adjacent to attacker (to the right, for horizontal battle)
            Vector3 attackerPosition = attackerMover.transform.position;
            Vector3 defenderPosition = attackerPosition + Vector3.right * 1.5f; // 1.5 units to the right for strict adjacency
            
            CardMoverP2 defenderMover = CardTestHelper.CreateCardMoverP2WithCard(defenderCard, defenderPosition);
            
            // Find the closest drop area to the defender position, or use the originally selected one
            CardDropArea closestDefenderArea = defenderArea;
            float minDistance = Vector3.Distance(defenderPosition, defenderArea.transform.position);
            foreach (var area in dropAreas)
            {
                float dist = Vector3.Distance(defenderPosition, area.transform.position);
                if (dist < minDistance && dist < 2.0f) // Within reasonable range
                {
                    minDistance = dist;
                    closestDefenderArea = area;
                }
            }
            
            CardTestHelper.PlaceP2CardOnDropArea(defenderMover, closestDefenderArea, true);
            
            // Manually adjust defender position if needed to ensure strict adjacency
            float actualDistance = Vector3.Distance(attackerMover.transform.position, defenderMover.transform.position);
            const float strictAdjacencyTolerance = 1.6f;
            if (actualDistance > strictAdjacencyTolerance)
            {
                Debug.Log($"[LogicError_CaptureCalculation_DefenderHigher_ShouldNotCapture] Adjusting defender position. Distance: {actualDistance:F3}, Target: {defenderPosition}");
                defenderMover.transform.position = attackerPosition + Vector3.right * 1.5f;
                yield return new WaitForEndOfFrame();
            }
            
            // Always manually trigger battle check for the defender (Player 2) to ensure battle logic runs
            // This ensures the battle check happens with the final card positions
                // Note: CheckCardBattles is for P1 cards, CheckCardBattlesP2 is for P2 cards
                MethodInfo checkBattlesP2Method = typeof(CardDropArea).GetMethod("CheckCardBattlesP2", BindingFlags.NonPublic | BindingFlags.Instance);
                
                if (checkBattlesP2Method != null && closestDefenderArea != null)
                {
                    Debug.Log($"[LogicError_CaptureCalculation_DefenderHigher_ShouldNotCapture] Manually triggering CheckCardBattlesP2 for defender (P2). " +
                        $"Defender left stat: {defenderCard.CurrentLeftStat}, Attacker right stat: {attackerCard.CurrentRightStat}. " +
                        $"Expected: Defender wins (8 > 2), so attacker should be flipped, NOT defender.");
                    checkBattlesP2Method.Invoke(closestDefenderArea, new object[] { defenderMover, defenderCard });
                yield return new WaitForEndOfFrame();
            }
            
            // Wait for any capture animations to complete
            yield return CardTestHelper.WaitForCaptureAnimations(5f);
            
            // Verify cards are actually adjacent
            float finalDistance = Vector3.Distance(attackerMover.transform.position, defenderMover.transform.position);
            Debug.Log($"[LogicError_CaptureCalculation_DefenderHigher_ShouldNotCapture] Final distance between cards: {finalDistance:F3}");
            
            // Check which cards were captured for debugging
            bool attackerCaptured = CardTestHelper.IsCardCaptured(attackerMover.gameObject);
            bool defenderCaptured = CardTestHelper.IsCardCaptured(defenderMover.gameObject);
            
            Debug.Log($"[LogicError_CaptureCalculation_DefenderHigher_ShouldNotCapture] Capture status - Attacker captured: {attackerCaptured}, Defender captured: {defenderCaptured}");
            Debug.Log($"[LogicError_CaptureCalculation_DefenderHigher_ShouldNotCapture] Expected: Defender wins (8 > 2), so Attacker should be captured=True, Defender should be captured=False");
            
            // LOGIC ASSERTION: Capture MUST NOT occur for defender when defender > attacker
            // The attacker's right (2) faces the defender's left (8), so defender wins
            // Therefore, the defender should NOT be captured (attacker should be captured instead)
            Assert.IsFalse(defenderCaptured, 
                $"LOGIC ERROR: Defender should NOT be captured when attacker stat ({attackerCard.CurrentRightStat}) < defender stat ({defenderCard.CurrentLeftStat}). " +
                $"Distance between cards: {finalDistance:F3}. Attacker captured: {attackerCaptured}, Defender captured: {defenderCaptured}. " +
                $"This is a logic error - the defender has higher stat so should win, not be captured.");
        }

        [UnityTest]
        public IEnumerator LogicError_ScoreCalculation_ShouldIncreaseOnCapture()
        {
            // LOGIC TEST: Score MUST increase when a capture occurs
            yield return CardTestHelper.WaitForCoinTossToComplete();
            yield return new WaitForSeconds(1.0f);
            
            ScoreManager scoreManager = ScoreManager.Instance;
            Assert.IsNotNull(scoreManager, "ScoreManager should exist");
            
            int initialPlayerScore = scoreManager.P1Score;
            
            CardDropArea[] dropAreas = Object.FindObjectsOfType<CardDropArea>();
            Assert.IsTrue(dropAreas.Length >= 2, "Need at least 2 drop areas");
            
            CardDropArea attackerArea = dropAreas[0];
            CardDropArea defenderArea = CardTestHelper.GetAdjacentDropArea(attackerArea, "right") ?? dropAreas[1];
            
            NewCard attackerCard = CardTestHelper.CreateTestCard(3, 6, 3, 3, "Attacker");
            NewCard defenderCard = CardTestHelper.CreateTestCard(3, 2, 3, 3, "Defender");
            
            // Add cards to deck manager hands
            NewDeckManagerP1 playerDeck = Object.FindObjectOfType<NewDeckManagerP1>();
            NewDeckManagerP2 opponentDeck = Object.FindObjectOfType<NewDeckManagerP2>();
            if (playerDeck != null)
            {
                CardTestHelper.AddCardToDeckManagerHand(playerDeck, attackerCard);
            }
            if (opponentDeck != null)
            {
                CardTestHelper.AddCardToDeckManagerHand(opponentDeck, defenderCard);
            }
            
            FateFlowController fateController = FateFlowController.Instance;
            if (fateController != null)
            {
                fateController.SetFate(FateSide.Player);
            }
            yield return null;
            
            // Place cards to trigger capture
            CardMoverP1 attackerMover = CardTestHelper.CreateCardMoverWithCard(attackerCard, attackerArea.transform.position, true);
            CardTestHelper.PlaceP1CardOnDropArea(attackerMover, attackerArea, true);
            yield return new WaitForSeconds(0.5f);
            
            CardMoverP2 defenderMover = CardTestHelper.CreateCardMoverP2WithCard(defenderCard, defenderArea.transform.position);
            CardTestHelper.PlaceP2CardOnDropArea(defenderMover, defenderArea, true);
            yield return CardTestHelper.WaitForCaptureAnimations(3f);
            
            // Verify capture occurred
            bool defenderCaptured = CardTestHelper.IsCardCaptured(defenderMover.gameObject);
            Assert.IsTrue(defenderCaptured, "Defender should be captured for score test");
            
            // LOGIC ASSERTION: Score MUST increase after capture
            int newPlayerScore = scoreManager.P1Score;
            Assert.Greater(newPlayerScore, initialPlayerScore, 
                $"LOGIC ERROR: Player score should increase after capturing opponent card. " +
                $"Was: {initialPlayerScore}, Now: {newPlayerScore}. " +
                $"This is a logic error if score did not increase.");
        }

        [UnityTest]
        public IEnumerator LogicError_TurnSwitching_ShouldAlternateCorrectly()
        {
            // LOGIC TEST: Turn switching MUST alternate between Player 1 and Player 2
            yield return CardTestHelper.WaitForCoinTossToComplete();
            yield return new WaitForSeconds(1.0f);
            
            FateFlowController fateController = FateFlowController.Instance;
            Assert.IsNotNull(fateController, "FateFlowController should exist");
            
            // Get initial turn
            FateSide initialTurn = fateController.CurrentFate;
            FateSide expectedNextTurn = initialTurn == FateSide.Player ? FateSide.P2 : FateSide.Player;
            
            // Switch turn
            fateController.AdvanceFateFlow();
            yield return null;
            
            FateSide actualNextTurn = fateController.CurrentFate;
            
            // LOGIC ASSERTION: Turn MUST alternate
            Assert.AreEqual(expectedNextTurn, actualNextTurn, 
                $"LOGIC ERROR: Turn should alternate from {initialTurn} to {expectedNextTurn}, but got {actualNextTurn}. " +
                $"This is a logic error if turn does not alternate correctly.");
            
            // Switch again - should return to initial
            fateController.AdvanceFateFlow();
            yield return null;
            
            FateSide shouldBeInitial = fateController.CurrentFate;
            Assert.AreEqual(initialTurn, shouldBeInitial, 
                $"LOGIC ERROR: Turn should alternate back to {initialTurn} after two switches, but got {shouldBeInitial}.");
        }

        [UnityTest]
        public IEnumerator LogicError_CanAct_ShouldOnlyAllowCurrentPlayer()
        {
            // LOGIC TEST: CanAct MUST only return true for the current player
            yield return CardTestHelper.WaitForCoinTossToComplete();
            yield return new WaitForSeconds(1.0f);
            
            FateFlowController fateController = FateFlowController.Instance;
            Assert.IsNotNull(fateController, "FateFlowController should exist");
            
            // Set to Player 1's turn
            fateController.SetFate(FateSide.Player);
            yield return null;
            
            // LOGIC ASSERTION: Only Player 1 should be able to act
            bool player1CanAct = fateController.CanAct(FateSide.Player);
            bool player2CanAct = fateController.CanAct(FateSide.P2);
            
            Assert.IsTrue(player1CanAct, 
                "LOGIC ERROR: Player 1 should be able to act during Player 1's turn");
            Assert.IsFalse(player2CanAct, 
                "LOGIC ERROR: Player 2 should NOT be able to act during Player 1's turn");
            
            // Switch to Player 2's turn
            fateController.SetFate(FateSide.P2);
            yield return null;
            
            // LOGIC ASSERTION: Only Player 2 should be able to act
            player1CanAct = fateController.CanAct(FateSide.Player);
            player2CanAct = fateController.CanAct(FateSide.P2);
            
            Assert.IsFalse(player1CanAct, 
                "LOGIC ERROR: Player 1 should NOT be able to act during Player 2's turn");
            Assert.IsTrue(player2CanAct, 
                "LOGIC ERROR: Player 2 should be able to act during Player 2's turn");
        }

        [UnityTest]
        public IEnumerator LogicError_IsOccupied_ShouldReflectActualCardPresence()
        {
            // LOGIC TEST: IsOccupied MUST accurately reflect whether a card is on the drop area
            yield return CardTestHelper.WaitForCoinTossToComplete();
            yield return new WaitForSeconds(1.0f);
            
            CardDropArea[] dropAreas = Object.FindObjectsOfType<CardDropArea>();
            Assert.IsTrue(dropAreas.Length > 0, "Need at least one drop area");
            
            CardDropArea testArea = dropAreas[0];
            
            // Initially should be unoccupied
            Assert.IsFalse(testArea.IsOccupied, 
                "LOGIC ERROR: Empty drop area should report IsOccupied = false");
            
            // Place a card
            NewCard testCard = CardTestHelper.CreateTestCard(3, 3, 3, 3, "TestCard");
            
            FateFlowController fateController = FateFlowController.Instance;
            if (fateController != null)
            {
                fateController.SetFate(FateSide.Player);
            }
            yield return null;
            
            CardMoverP1 CardMoverP1 = CardTestHelper.CreateCardMoverWithCard(testCard, testArea.transform.position, true);
            bool placed = CardTestHelper.PlaceP1CardOnDropArea(CardMoverP1, testArea, false);
            Assert.IsTrue(placed, "Card should be placed");
            yield return new WaitForSeconds(0.5f);
            
            // LOGIC ASSERTION: IsOccupied MUST be true after placement
            Assert.IsTrue(testArea.IsOccupied, 
                $"LOGIC ERROR: Drop area should report IsOccupied = true after card placement. " +
                $"This is a logic error if IsOccupied does not reflect actual card presence.");
        }

        [UnityTest]
        public IEnumerator LogicError_EqualStats_ShouldNotCapture()
        {
            // LOGIC TEST: When stats are equal, capture MUST NOT occur
            yield return CardTestHelper.WaitForCoinTossToComplete();
            yield return new WaitForSeconds(1.0f);
            
            CardDropArea[] dropAreas = Object.FindObjectsOfType<CardDropArea>();
            Assert.IsTrue(dropAreas.Length >= 2, "Need at least 2 drop areas");
            
            CardDropArea attackerArea = dropAreas[0];
            CardDropArea defenderArea = CardTestHelper.GetAdjacentDropArea(attackerArea, "right") ?? dropAreas[1];
            
            // Create cards with EQUAL stats: Both right/left = 5
            NewCard attackerCard = CardTestHelper.CreateTestCard(5, 5, 5, 5, "EqualAttacker");
            NewCard defenderCard = CardTestHelper.CreateTestCard(5, 5, 5, 5, "EqualDefender");
            
            Assert.AreEqual(attackerCard.CurrentRightStat, defenderCard.CurrentLeftStat, 
                "Stats should be equal for this test");
            
            FateFlowController fateController = FateFlowController.Instance;
            if (fateController != null)
            {
                fateController.SetFate(FateSide.Player);
            }
            yield return null;
            
            CardMoverP1 attackerMover = CardTestHelper.CreateCardMoverWithCard(attackerCard, attackerArea.transform.position, true);
            CardTestHelper.PlaceP1CardOnDropArea(attackerMover, attackerArea, true);
            yield return new WaitForSeconds(0.5f);
            
            CardMoverP2 defenderMover = CardTestHelper.CreateCardMoverP2WithCard(defenderCard, defenderArea.transform.position);
            CardTestHelper.PlaceP2CardOnDropArea(defenderMover, defenderArea, true);
            yield return CardTestHelper.WaitForCaptureAnimations(3f);
            
            // LOGIC ASSERTION: Capture MUST NOT occur when stats are equal
            bool defenderCaptured = CardTestHelper.IsCardCaptured(defenderMover.gameObject);
            Assert.IsFalse(defenderCaptured, 
                $"LOGIC ERROR: Defender should NOT be captured when stats are equal ({attackerCard.CurrentRightStat} == {defenderCard.CurrentLeftStat}). " +
                $"This is a logic error if capture occurred with equal stats.");
        }
    }
}

