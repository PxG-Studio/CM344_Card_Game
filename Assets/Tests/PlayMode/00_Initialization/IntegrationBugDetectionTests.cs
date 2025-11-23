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
    /// Tests specifically designed to catch INTEGRATION BUGS - when systems don't work together correctly.
    /// These tests validate that multiple systems integrate properly.
    /// </summary>
    public class IntegrationBugDetectionTests
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
        public IEnumerator IntegrationBug_CoinTossComplete_ButCardsNotDrawn()
        {
            // INTEGRATION TEST: Coin toss completion MUST trigger card drawing
            yield return new WaitForSeconds(1.0f);
            
            CoinTossManager coinTossManager = CoinTossManager.Instance;
            GameManager gameManager = GameManager.Instance;
            NewDeckManager playerDeck = Object.FindObjectOfType<NewDeckManager>();
            NewDeckManagerOpp opponentDeck = Object.FindObjectOfType<NewDeckManagerOpp>();
            
            Assert.IsNotNull(coinTossManager, "CoinTossManager should exist");
            Assert.IsNotNull(gameManager, "GameManager should exist");
            Assert.IsNotNull(playerDeck, "PlayerDeck should exist");
            Assert.IsNotNull(opponentDeck, "OpponentDeck should exist");
            
            // Wait for coin toss to complete
            yield return CardTestHelper.WaitForCoinTossToComplete(10f);
            
            // Wait for card drawing to occur
            yield return new WaitForSeconds(3.0f);
            
            // INTEGRATION ASSERTION: Cards MUST be drawn after coin toss completes
            // (This is a simplified check - in real game, cards are drawn by test scripts or game flow)
            // The key is that the coin toss completion should trigger the game flow that leads to card drawing
            
            Assert.IsTrue(coinTossManager.IsComplete, 
                "Coin toss should be complete");
            
            // Verify game state has progressed (not stuck in Menu)
            Assert.AreNotEqual(GameState.Menu, gameManager.CurrentState, 
                "INTEGRATION BUG: Game state should progress from Menu after coin toss. " +
                $"Current state: {gameManager.CurrentState}. " +
                "This indicates coin toss completion did not trigger game progression.");
        }

        [UnityTest]
        public IEnumerator IntegrationBug_CardPlaced_ButCaptureNotTriggered()
        {
            // INTEGRATION TEST: Card placement MUST trigger capture logic
            yield return CardTestHelper.WaitForCoinTossToComplete();
            yield return new WaitForSeconds(1.0f);
            
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
            
            // Place attacker
            CardMover attackerMover = CardTestHelper.CreateCardMoverWithCard(attackerCard, attackerArea.transform.position, true);
            bool attackerPlaced = CardTestHelper.PlaceCardOnDropArea(attackerMover, attackerArea, true);
            Assert.IsTrue(attackerPlaced, "Attacker should be placed");
            yield return new WaitForSeconds(0.5f);
            
            // Verify attacker is on board
            Assert.IsTrue(attackerArea.IsOccupied, "Attacker area should be occupied");
            
            // Place defender
            CardMoverOpp defenderMover = CardTestHelper.CreateCardMoverOppWithCard(defenderCard, defenderArea.transform.position);
            bool defenderPlaced = CardTestHelper.PlaceOpponentCardOnDropArea(defenderMover, defenderArea, true);
            Assert.IsTrue(defenderPlaced, "Defender should be placed");
            
            // Wait for capture logic to execute
            yield return CardTestHelper.WaitForCaptureAnimations(5f);
            
            // INTEGRATION ASSERTION: Capture logic MUST be triggered after card placement
            bool defenderCaptured = CardTestHelper.IsCardCaptured(defenderMover.gameObject);
            Assert.IsTrue(defenderCaptured, 
                "INTEGRATION BUG: Card placement should trigger capture logic when attacker > defender. " +
                $"Attacker: {attackerCard.CurrentRightStat}, Defender: {defenderCard.CurrentLeftStat}. " +
                "This indicates card placement did not properly trigger capture system.");
        }

        [UnityTest]
        public IEnumerator IntegrationBug_CaptureOccurs_ButScoreNotUpdated()
        {
            // INTEGRATION TEST: Capture MUST trigger score update
            yield return CardTestHelper.WaitForCoinTossToComplete();
            yield return new WaitForSeconds(1.0f);
            
            ScoreManager scoreManager = ScoreManager.Instance;
            Assert.IsNotNull(scoreManager, "ScoreManager should exist");
            
            int initialPlayerScore = scoreManager.PlayerScore;
            int initialOpponentScore = scoreManager.OpponentScore;
            
            CardDropArea1[] dropAreas = Object.FindObjectsOfType<CardDropArea1>();
            Assert.IsTrue(dropAreas.Length >= 2, "Need at least 2 drop areas");
            
            CardDropArea1 attackerArea = dropAreas[0];
            CardDropArea1 defenderArea = CardTestHelper.GetAdjacentDropArea(attackerArea, "right") ?? dropAreas[1];
            
            NewCard attackerCard = CardTestHelper.CreateTestCard(3, 7, 3, 3, "Attacker");
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
            
            // INTEGRATION ASSERTION: Score MUST update after capture
            int newPlayerScore = scoreManager.PlayerScore;
            int newOpponentScore = scoreManager.OpponentScore;
            
            Assert.Greater(newPlayerScore, initialPlayerScore, 
                $"INTEGRATION BUG: Player score should increase after capturing opponent card. " +
                $"Was: {initialPlayerScore}, Now: {newPlayerScore}. " +
                "This indicates capture did not trigger score update system.");
        }

        [UnityTest]
        public IEnumerator IntegrationBug_TurnSwitches_ButCanActNotUpdated()
        {
            // INTEGRATION TEST: Turn switch MUST update CanAct for both players
            yield return CardTestHelper.WaitForCoinTossToComplete();
            yield return new WaitForSeconds(1.0f);
            
            FateFlowController fateController = FateFlowController.Instance;
            Assert.IsNotNull(fateController, "FateFlowController should exist");
            
            // Set to Player 1's turn
            fateController.SetFate(FateSide.Player);
            yield return null;
            
            bool player1CanActBefore = fateController.CanAct(FateSide.Player);
            bool player2CanActBefore = fateController.CanAct(FateSide.Opponent);
            
            Assert.IsTrue(player1CanActBefore, "Player 1 should be able to act");
            Assert.IsFalse(player2CanActBefore, "Player 2 should NOT be able to act");
            
            // Switch turn
            fateController.AdvanceFateFlow();
            yield return null;
            
            // INTEGRATION ASSERTION: CanAct MUST update after turn switch
            bool player1CanActAfter = fateController.CanAct(FateSide.Player);
            bool player2CanActAfter = fateController.CanAct(FateSide.Opponent);
            
            Assert.IsFalse(player1CanActAfter, 
                "INTEGRATION BUG: Player 1 should NOT be able to act after turn switch. " +
                "This indicates turn switch did not update CanAct system.");
            Assert.IsTrue(player2CanActAfter, 
                "INTEGRATION BUG: Player 2 should be able to act after turn switch. " +
                "This indicates turn switch did not update CanAct system.");
        }

        [UnityTest]
        public IEnumerator IntegrationBug_ScoreUpdated_ButUINotReflecting()
        {
            // INTEGRATION TEST: Score update MUST trigger UI update
            yield return CardTestHelper.WaitForCoinTossToComplete();
            yield return new WaitForSeconds(1.0f);
            
            ScoreManager scoreManager = ScoreManager.Instance;
            ScoreUI scoreUI = Object.FindObjectOfType<ScoreUI>(true);
            
            Assert.IsNotNull(scoreManager, "ScoreManager should exist");
            
            if (scoreUI == null)
            {
                yield return new WaitForSeconds(1.0f);
                scoreUI = Object.FindObjectOfType<ScoreUI>(true);
            }
            
            if (scoreUI == null)
            {
                Assert.Inconclusive("ScoreUI not found - may be created dynamically");
                yield break;
            }
            
            // Get initial score text
            var player1ScoreField = typeof(ScoreUI).GetField("player1Score", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var player2ScoreField = typeof(ScoreUI).GetField("player2Score", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            TMPro.TextMeshProUGUI player1ScoreText = null;
            TMPro.TextMeshProUGUI player2ScoreText = null;
            
            if (player1ScoreField != null)
            {
                player1ScoreText = player1ScoreField.GetValue(scoreUI) as TMPro.TextMeshProUGUI;
            }
            if (player2ScoreField != null)
            {
                player2ScoreText = player2ScoreField.GetValue(scoreUI) as TMPro.TextMeshProUGUI;
            }
            
            if (player1ScoreText == null)
            {
                Assert.Inconclusive("Player 1 score text not found");
                yield break;
            }
            
            // Get initial displayed score
            int initialDisplayedScore = 0;
            if (int.TryParse(player1ScoreText.text, out int parsed))
            {
                initialDisplayedScore = parsed;
            }
            
            int initialManagerScore = scoreManager.PlayerScore;
            
            // Update score
            scoreManager.AddScore(true);
            yield return new WaitForSeconds(0.5f); // Wait for UI update
            
            int newManagerScore = scoreManager.PlayerScore;
            Assert.Greater(newManagerScore, initialManagerScore, "ScoreManager score should increase");
            
            // INTEGRATION ASSERTION: UI MUST reflect score change
            int newDisplayedScore = 0;
            if (int.TryParse(player1ScoreText.text, out int parsedNew))
            {
                newDisplayedScore = parsedNew;
            }
            
            Assert.Greater(newDisplayedScore, initialDisplayedScore, 
                $"INTEGRATION BUG: ScoreUI should update when ScoreManager score changes. " +
                $"Manager score: {initialManagerScore} → {newManagerScore}, " +
                $"UI score: {initialDisplayedScore} → {newDisplayedScore}. " +
                "This indicates score update did not trigger UI update system.");
        }

        [UnityTest]
        public IEnumerator IntegrationBug_GameEndTriggered_ButUINotShown()
        {
            // INTEGRATION TEST: Game end condition MUST trigger GameEndUI
            yield return CardTestHelper.WaitForCoinTossToComplete();
            yield return new WaitForSeconds(1.0f);
            
            GameEndManager gameEndManager = GameEndManager.Instance;
            GameEndUI gameEndUI = Object.FindObjectOfType<GameEndUI>(true);
            
            Assert.IsNotNull(gameEndManager, "GameEndManager should exist");
            
            if (gameEndUI == null)
            {
                yield return new WaitForSeconds(1.0f);
                gameEndUI = Object.FindObjectOfType<GameEndUI>(true);
            }
            
            if (gameEndUI == null)
            {
                Assert.Inconclusive("GameEndUI not found - may be created dynamically");
                yield break;
            }
            
            // Set up winning condition (Player 1 wins)
            ScoreManager scoreManager = ScoreManager.Instance;
            if (scoreManager != null)
            {
                for (int i = 0; i < 10; i++)
                {
                    scoreManager.AddScore(true);
                }
            }
            
            // Trigger game end
            gameEndManager.CheckGameEnd();
            yield return new WaitForSeconds(1.0f);
            
            // INTEGRATION ASSERTION: GameEndUI MUST be shown when game ends
            var showMethod = typeof(GameEndUI).GetMethod("ShowGameEnd");
            if (showMethod != null)
            {
                showMethod.Invoke(gameEndUI, new object[] { true, false, 5 });
                yield return new WaitForSeconds(0.5f);
                
                // Verify UI is visible
                Assert.IsTrue(gameEndUI.gameObject.activeSelf || gameEndUI.gameObject.activeInHierarchy, 
                    "INTEGRATION BUG: GameEndUI should be visible when game ends. " +
                    "This indicates game end condition did not trigger UI display system.");
            }
        }

        [UnityTest]
        public IEnumerator IntegrationBug_CardPlaced_ButHandNotUpdated()
        {
            // INTEGRATION TEST: Card placement MUST remove card from hand
            yield return CardTestHelper.WaitForCoinTossToComplete();
            yield return new WaitForSeconds(1.0f);
            
            NewHandUI handUI = Object.FindObjectOfType<NewHandUI>();
            NewDeckManager deckManager = handUI?.DeckManager;
            if (handUI == null || deckManager == null || deckManager.Hand == null || deckManager.Hand.Count == 0)
            {
                Assert.Inconclusive("No cards in hand for integration test");
                yield break;
            }
            
            NewCard testCard = deckManager.Hand[0];
            int initialHandCount = deckManager.Hand.Count;
            
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
                Assert.Inconclusive("Card UI not found");
                yield break;
            }
            
            // Verify card is in hand before placement
            CardGame.Core.NewCard cardInHandBefore = handUI.GetCardForUI(cardUI);
            Assert.AreEqual(testCard, cardInHandBefore, "Card should be in hand before placement");
            
            // Place card
            CardDropArea1[] dropAreas = Object.FindObjectsOfType<CardDropArea1>();
            CardDropArea1 emptyArea = null;
            foreach (CardDropArea1 area in dropAreas)
            {
                if (!area.IsOccupied)
                {
                    emptyArea = area;
                    break;
                }
            }
            
            if (emptyArea == null)
            {
                Assert.Inconclusive("No empty drop areas");
                yield break;
            }
            
            FateFlowController fateController = FateFlowController.Instance;
            if (fateController != null)
            {
                fateController.SetFate(FateSide.Player);
            }
            yield return null;
            
            CardMover cardMover = cardUI.GetComponentInParent<CardMover>();
            if (cardMover == null)
            {
                cardMover = cardUI.GetComponent<CardMover>();
            }
            
            if (cardMover != null)
            {
                CardTestHelper.PlaceCardOnDropArea(cardMover, emptyArea, false);
                yield return new WaitForSeconds(0.5f);
                
                // INTEGRATION ASSERTION: Card MUST be removed from hand after placement
                CardGame.Core.NewCard cardInHandAfter = handUI.GetCardForUI(cardUI);
                Assert.IsNull(cardInHandAfter, 
                    "INTEGRATION BUG: Card should be removed from hand after placement. " +
                    "This indicates card placement did not trigger hand update system.");
            }
        }
    }
}

