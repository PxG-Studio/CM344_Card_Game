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
            
            CardDropArea1[] dropAreas = Object.FindObjectsOfType<CardDropArea1>();
            Assert.IsTrue(dropAreas.Length >= 2, "Need at least 2 drop areas");
            
            CardDropArea1 attackerArea = dropAreas[0];
            CardDropArea1 defenderArea = CardTestHelper.GetAdjacentDropArea(attackerArea, "right") ?? dropAreas[1];
            
            // Create cards with known stats: Attacker right=7, Defender left=3
            NewCard attackerCard = CardTestHelper.CreateTestCard(3, 7, 3, 3, "StrongAttacker");
            NewCard defenderCard = CardTestHelper.CreateTestCard(3, 3, 3, 3, "WeakDefender");
            
            // Add cards to deck manager hands
            NewDeckManager playerDeck = Object.FindObjectOfType<NewDeckManager>();
            NewDeckManagerOpp opponentDeck = Object.FindObjectOfType<NewDeckManagerOpp>();
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
            CardMover attackerMover = CardTestHelper.CreateCardMoverWithCard(attackerCard, attackerArea.transform.position, true);
            CardTestHelper.PlaceCardOnDropArea(attackerMover, attackerArea, true);
            yield return new WaitForSeconds(0.5f);
            
            CardMoverOpp defenderMover = CardTestHelper.CreateCardMoverOppWithCard(defenderCard, defenderArea.transform.position);
            CardTestHelper.PlaceOpponentCardOnDropArea(defenderMover, defenderArea, true);
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
            
            CardDropArea1[] dropAreas = Object.FindObjectsOfType<CardDropArea1>();
            Assert.IsTrue(dropAreas.Length >= 2, "Need at least 2 drop areas");
            
            CardDropArea1 attackerArea = dropAreas[0];
            CardDropArea1 defenderArea = CardTestHelper.GetAdjacentDropArea(attackerArea, "right") ?? dropAreas[1];
            
            // Create cards: Attacker right=2, Defender left=8
            NewCard attackerCard = CardTestHelper.CreateTestCard(3, 2, 3, 3, "WeakAttacker");
            NewCard defenderCard = CardTestHelper.CreateTestCard(3, 8, 3, 3, "StrongDefender");
            
            // Add cards to deck manager hands
            NewDeckManager playerDeck = Object.FindObjectOfType<NewDeckManager>();
            NewDeckManagerOpp opponentDeck = Object.FindObjectOfType<NewDeckManagerOpp>();
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
            
            CardMover attackerMover = CardTestHelper.CreateCardMoverWithCard(attackerCard, attackerArea.transform.position, true);
            CardTestHelper.PlaceCardOnDropArea(attackerMover, attackerArea, true);
            yield return new WaitForSeconds(0.5f);
            
            CardMoverOpp defenderMover = CardTestHelper.CreateCardMoverOppWithCard(defenderCard, defenderArea.transform.position);
            CardTestHelper.PlaceOpponentCardOnDropArea(defenderMover, defenderArea, true);
            yield return CardTestHelper.WaitForCaptureAnimations(3f);
            
            // LOGIC ASSERTION: Capture MUST NOT occur when defender > attacker
            bool defenderCaptured = CardTestHelper.IsCardCaptured(defenderMover.gameObject);
            Assert.IsFalse(defenderCaptured, 
                $"LOGIC ERROR: Defender should NOT be captured when attacker stat ({attackerCard.CurrentRightStat}) < defender stat ({defenderCard.CurrentLeftStat}). " +
                $"This is a logic error if capture occurred.");
        }

        [UnityTest]
        public IEnumerator LogicError_ScoreCalculation_ShouldIncreaseOnCapture()
        {
            // LOGIC TEST: Score MUST increase when a capture occurs
            yield return CardTestHelper.WaitForCoinTossToComplete();
            yield return new WaitForSeconds(1.0f);
            
            ScoreManager scoreManager = ScoreManager.Instance;
            Assert.IsNotNull(scoreManager, "ScoreManager should exist");
            
            int initialPlayerScore = scoreManager.PlayerScore;
            
            CardDropArea1[] dropAreas = Object.FindObjectsOfType<CardDropArea1>();
            Assert.IsTrue(dropAreas.Length >= 2, "Need at least 2 drop areas");
            
            CardDropArea1 attackerArea = dropAreas[0];
            CardDropArea1 defenderArea = CardTestHelper.GetAdjacentDropArea(attackerArea, "right") ?? dropAreas[1];
            
            NewCard attackerCard = CardTestHelper.CreateTestCard(3, 6, 3, 3, "Attacker");
            NewCard defenderCard = CardTestHelper.CreateTestCard(3, 2, 3, 3, "Defender");
            
            // Add cards to deck manager hands
            NewDeckManager playerDeck = Object.FindObjectOfType<NewDeckManager>();
            NewDeckManagerOpp opponentDeck = Object.FindObjectOfType<NewDeckManagerOpp>();
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
            CardMover attackerMover = CardTestHelper.CreateCardMoverWithCard(attackerCard, attackerArea.transform.position, true);
            CardTestHelper.PlaceCardOnDropArea(attackerMover, attackerArea, true);
            yield return new WaitForSeconds(0.5f);
            
            CardMoverOpp defenderMover = CardTestHelper.CreateCardMoverOppWithCard(defenderCard, defenderArea.transform.position);
            CardTestHelper.PlaceOpponentCardOnDropArea(defenderMover, defenderArea, true);
            yield return CardTestHelper.WaitForCaptureAnimations(3f);
            
            // Verify capture occurred
            bool defenderCaptured = CardTestHelper.IsCardCaptured(defenderMover.gameObject);
            Assert.IsTrue(defenderCaptured, "Defender should be captured for score test");
            
            // LOGIC ASSERTION: Score MUST increase after capture
            int newPlayerScore = scoreManager.PlayerScore;
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
            FateSide expectedNextTurn = initialTurn == FateSide.Player ? FateSide.Opponent : FateSide.Player;
            
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
            bool player2CanAct = fateController.CanAct(FateSide.Opponent);
            
            Assert.IsTrue(player1CanAct, 
                "LOGIC ERROR: Player 1 should be able to act during Player 1's turn");
            Assert.IsFalse(player2CanAct, 
                "LOGIC ERROR: Player 2 should NOT be able to act during Player 1's turn");
            
            // Switch to Player 2's turn
            fateController.SetFate(FateSide.Opponent);
            yield return null;
            
            // LOGIC ASSERTION: Only Player 2 should be able to act
            player1CanAct = fateController.CanAct(FateSide.Player);
            player2CanAct = fateController.CanAct(FateSide.Opponent);
            
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
            
            CardDropArea1[] dropAreas = Object.FindObjectsOfType<CardDropArea1>();
            Assert.IsTrue(dropAreas.Length > 0, "Need at least one drop area");
            
            CardDropArea1 testArea = dropAreas[0];
            
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
            
            CardMover cardMover = CardTestHelper.CreateCardMoverWithCard(testCard, testArea.transform.position, true);
            bool placed = CardTestHelper.PlaceCardOnDropArea(cardMover, testArea, false);
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
            
            CardDropArea1[] dropAreas = Object.FindObjectsOfType<CardDropArea1>();
            Assert.IsTrue(dropAreas.Length >= 2, "Need at least 2 drop areas");
            
            CardDropArea1 attackerArea = dropAreas[0];
            CardDropArea1 defenderArea = CardTestHelper.GetAdjacentDropArea(attackerArea, "right") ?? dropAreas[1];
            
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
            
            CardMover attackerMover = CardTestHelper.CreateCardMoverWithCard(attackerCard, attackerArea.transform.position, true);
            CardTestHelper.PlaceCardOnDropArea(attackerMover, attackerArea, true);
            yield return new WaitForSeconds(0.5f);
            
            CardMoverOpp defenderMover = CardTestHelper.CreateCardMoverOppWithCard(defenderCard, defenderArea.transform.position);
            CardTestHelper.PlaceOpponentCardOnDropArea(defenderMover, defenderArea, true);
            yield return CardTestHelper.WaitForCaptureAnimations(3f);
            
            // LOGIC ASSERTION: Capture MUST NOT occur when stats are equal
            bool defenderCaptured = CardTestHelper.IsCardCaptured(defenderMover.gameObject);
            Assert.IsFalse(defenderCaptured, 
                $"LOGIC ERROR: Defender should NOT be captured when stats are equal ({attackerCard.CurrentRightStat} == {defenderCard.CurrentLeftStat}). " +
                $"This is a logic error if capture occurred with equal stats.");
        }
    }
}

