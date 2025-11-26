using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using CardGame.Managers;
using CardGame.UI;
using CardGame.Tests;
using CardGame.Core;

namespace CardGame.Tests.Endgame
{
    /// <summary>
    /// Deterministic end-to-end test:
    /// Coin toss → cards on board → capture → score recalculation.
    /// Uses explicit stats and adjacency so there is no randomness in the capture outcome.
    /// </summary>
    public class CompleteGameFlow_CaptureScoreTests
    {
        /// <summary>
        /// Deterministic capture that must increase P1's score after a manual score recompute.
        /// </summary>
        [UnityTest]
        public IEnumerator CompleteFlow_Capture_UpdatesScore()
        {
            Debug.Log("=== TEST START: Deterministic Capture → Score Update ===");

            // Load the battle scene and wait for managers/UI to be ready
            yield return TestSceneInitializer.LoadBattleScene();

            // Ensure coin toss completes so the game is in a stable state
            yield return CardTestHelper.WaitForCoinTossToComplete();

            GameManager gameManager = GameManager.Instance;
            ScoreManager scoreManager = ScoreManager.Instance;
            FateFlowController fateController = FateFlowController.Instance;
            NewDeckManagerP1 playerDeck = Object.FindObjectOfType<NewDeckManagerP1>();
            NewDeckManagerP2 opponentDeck = Object.FindObjectOfType<NewDeckManagerP2>();

            Assert.IsNotNull(gameManager, "[TEST] GameManager instance is required.");
            Assert.IsNotNull(scoreManager, "[TEST] ScoreManager instance is required.");
            Assert.IsNotNull(fateController, "[TEST] FateFlowController instance is required.");
            Assert.IsNotNull(playerDeck, "[TEST] NewDeckManagerP1 is required.");
            Assert.IsNotNull(opponentDeck, "[TEST] NewDeckManagerP2 is required.");

            int initialP1Score = scoreManager.P1Score;
            Debug.Log($"[TEST] Initial P1 score: {initialP1Score}");

            // ------------------------------------------------------------------
            // Create deterministic cards:
            // Attacker: strong on all sides
            // Defender: weak on all sides
            // ------------------------------------------------------------------
            NewCard attackerCard = CardTestHelper.CreateTestCard(9, 9, 9, 9, "TestAttacker");
            NewCard defenderCard = CardTestHelper.CreateTestCard(1, 1, 1, 1, "TestDefender");

            Debug.Log("[TEST] Created attacker and defender cards with deterministic stats.");

            // Ensure decks know about these cards so CardDropArea will accept plays
            CardTestHelper.AddCardToDeckManagerHand(playerDeck, attackerCard);
            CardTestHelper.AddCardToDeckManagerHand(opponentDeck, defenderCard);

            // ------------------------------------------------------------------
            // Find two adjacent drop areas
            // ------------------------------------------------------------------
            CardDropArea[] dropAreas = Object.FindObjectsOfType<CardDropArea>();
            Assert.IsTrue(dropAreas.Length >= 2, "[TEST] Not enough CardDropArea instances in scene.");

            CardDropArea areaA = dropAreas[0];
            CardDropArea areaB = CardTestHelper.GetAdjacentDropArea(areaA, "right");

            if (areaB == null)
            {
                areaB = CardTestHelper.GetAdjacentDropArea(areaA, "left");
            }
            if (areaB == null)
            {
                areaB = CardTestHelper.GetAdjacentDropArea(areaA, "top");
            }
            if (areaB == null)
            {
                areaB = CardTestHelper.GetAdjacentDropArea(areaA, "bottom");
            }

            Assert.IsNotNull(areaB, "[TEST] Failed to find an adjacent drop area for deterministic capture.");

            Debug.Log($"[TEST] Using DropArea A: {areaA.name} at {areaA.transform.position}, " +
                      $"DropArea B: {areaB.name} at {areaB.transform.position}");

            // ------------------------------------------------------------------
            // Create card movers using shared helpers (handles colliders & UI)
            // ------------------------------------------------------------------
            CardMoverP2 defenderMover = CardTestHelper.CreateCardMoverP2WithCard(defenderCard, areaB.transform.position);
            Assert.IsNotNull(defenderMover, "[TEST] Failed to create defender mover.");

            CardMoverP1 attackerMover = CardTestHelper.CreateCardMoverWithCard(attackerCard, areaA.transform.position, true);
            Assert.IsNotNull(attackerMover, "[TEST] Failed to create attacker mover.");

            // ------------------------------------------------------------------
            // Place defender first (P2), then attacker (P1) to trigger capture.
            // We explicitly set Fate so CardDropArea.CanCardAct(...) allows each play.
            // ------------------------------------------------------------------
            Debug.Log("[TEST] Dropping P2 defender card on area B...");
            fateController.SetFate(FateSide.P2);
            yield return null;
            bool defenderPlaced = CardTestHelper.PlaceP2CardOnDropArea(defenderMover, areaB, true);
            Assert.IsTrue(defenderPlaced, "[TEST] Failed to place defender card on board.");

            yield return new WaitForSeconds(0.25f);

            Debug.Log("[TEST] Dropping P1 attacker card on area A (should capture defender)...");
            fateController.SetFate(FateSide.Player);
            yield return null;
            bool attackerPlaced = CardTestHelper.PlaceP1CardOnDropArea(attackerMover, areaA, true);
            Assert.IsTrue(attackerPlaced, "[TEST] Failed to place attacker card on board.");

            // Wait for any capture animations and chain logic
            yield return CardTestHelper.WaitForCaptureAnimations(3f);
            yield return new WaitForSeconds(1f);

            // ------------------------------------------------------------------
            // Verify capture occurred (board-centric: who controls the defender tile?)
            // ------------------------------------------------------------------
            GameObject defenderGOOnTile = areaB.GetOccupyingCard();
            Assert.IsNotNull(defenderGOOnTile,
                "[TEST] Defender tile (areaB) should be occupied after capture sequence.");

            NewCardUI defenderUI = defenderGOOnTile.GetComponentInChildren<NewCardUI>() ??
                                   defenderGOOnTile.GetComponent<NewCardUI>();
            Assert.IsNotNull(defenderUI,
                "[TEST] Occupying card on defender tile must have a NewCardUI component.");
            Assert.IsNotNull(defenderUI.Card,
                "[TEST] Defender tile card must have a NewCard reference.");
            Assert.AreEqual("TestDefender", defenderUI.Card.Data.cardName,
                "[TEST] Defender tile should still be logically showing the TestDefender card.");

            // Determine ownership via capture border color on NewCardUI.
            // This mirrors what FlipCardGameObject does when a card is captured.
            Color playerCaptureColor = defenderUI.PlayerCapturedColor;

            // Access the private cardBackground field to read the rendered border color.
            var cardBackgroundField = typeof(NewCardUI).GetField("cardBackground",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(cardBackgroundField,
                "[TEST] NewCardUI.cardBackground field should exist for capture color checks.");

            object cardBackground = cardBackgroundField.GetValue(defenderUI);
            Assert.IsNotNull(cardBackground,
                "[TEST] Defender NewCardUI should have a cardBackground object for color.");

            Color borderColor = Color.white;
            if (cardBackground is SpriteRenderer bgSR)
            {
                borderColor = bgSR.color;
            }
            else if (cardBackground is UnityEngine.UI.Image bgImg)
            {
                borderColor = bgImg.color;
            }

            float colorTolerance = 0.1f;
            bool defenderNowP1Owned =
                Mathf.Abs(borderColor.r - playerCaptureColor.r) < colorTolerance &&
                Mathf.Abs(borderColor.g - playerCaptureColor.g) < colorTolerance &&
                Mathf.Abs(borderColor.b - playerCaptureColor.b) < colorTolerance;

            float finalDistance = Vector3.Distance(attackerMover.transform.position, defenderMover.transform.position);

            Debug.Log($"[TEST] Capture check (board state): defenderNowP1Owned={defenderNowP1Owned}, " +
                      $"distance={finalDistance:F2}, attackerPos={attackerMover.transform.position}, " +
                      $"defenderPos={defenderMover.transform.position}, " +
                      $"occupyingCard={defenderGOOnTile.name}, " +
                      $"borderColor={borderColor}, playerCaptureColor={playerCaptureColor}");

            Assert.IsTrue(defenderNowP1Owned,
                "[TEST] Defender card should be captured by the stronger attacker (defender tile must now be owned by P1).");

            // ------------------------------------------------------------------
            // Manually recalculate scores (end-game style) and assert increase
            // ------------------------------------------------------------------
            scoreManager.RecalculateScores();
            int newP1Score = scoreManager.P1Score;

            Debug.Log($"[TEST] P1 score after capture and RecalculateScores(): {newP1Score}");

            Assert.Greater(newP1Score, initialP1Score,
                $"[TEST] Player 1 score should increase after capture. Before: {initialP1Score}, After: {newP1Score}");

            Debug.Log("=== TEST PASSED: Deterministic Capture → Score Update ===");
        }
    }
}


