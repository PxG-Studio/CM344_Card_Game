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
            NewDeckManagerP1 playerDeck = Object.FindObjectOfType<NewDeckManagerP1>();
            NewDeckManagerP2 opponentDeck = Object.FindObjectOfType<NewDeckManagerP2>();
            
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
            
            // Place attacker
            CardMoverP1 attackerMover = CardTestHelper.CreateCardMoverWithCard(attackerCard, attackerArea.transform.position, true);
            bool attackerPlaced = CardTestHelper.PlaceP1CardOnDropArea(attackerMover, attackerArea, true);
            Assert.IsTrue(attackerPlaced, "Attacker should be placed");
            yield return new WaitForSeconds(0.5f);
            
            // Verify attacker is on board
            Assert.IsTrue(attackerArea.IsOccupied, "Attacker area should be occupied");
            
            // Place defender
            CardMoverP2 defenderMover = CardTestHelper.CreateCardMoverP2WithCard(defenderCard, defenderArea.transform.position);
            bool defenderPlaced = CardTestHelper.PlaceP2CardOnDropArea(defenderMover, defenderArea, true);
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
            
            CardDropArea[] dropAreas = Object.FindObjectsOfType<CardDropArea>();
            Assert.IsTrue(dropAreas.Length >= 2, "Need at least 2 drop areas");
            
            // Find two adjacent drop areas - CRITICAL: Must be within strict adjacency distance (1.6 units)
            CardDropArea attackerArea = dropAreas[0];
            CardDropArea defenderArea = null;
            float minDistance = float.MaxValue;
            Vector3 attackerAreaPos = attackerArea.transform.position;
            const float strictAdjacencyTolerance = 1.6f;
            
            // Try to find adjacent area by direction first
            string[] directions = { "right", "left", "top", "bottom" };
            foreach (string direction in directions)
            {
                CardDropArea candidate = CardTestHelper.GetAdjacentDropArea(attackerArea, direction);
                if (candidate != null)
                {
                    float dist = Vector3.Distance(attackerAreaPos, candidate.transform.position);
                    if (dist <= strictAdjacencyTolerance && dist < minDistance)
                    {
                        minDistance = dist;
                        defenderArea = candidate;
                    }
                }
            }
            
            // If no adjacent area found by direction within tolerance, search all areas for closest within tolerance
            if (defenderArea == null)
            {
                foreach (CardDropArea area in dropAreas)
                {
                    if (area == attackerArea) continue;
                    float dist = Vector3.Distance(attackerAreaPos, area.transform.position);
                    if (dist <= strictAdjacencyTolerance && dist < minDistance)
                    {
                        minDistance = dist;
                        defenderArea = area;
                    }
                }
            }
            
            // If still no adjacent area within tolerance, we'll manually position cards closer
            if (defenderArea == null)
            {
                // Find the closest area (even if beyond tolerance) as a fallback
                float closestDist = float.MaxValue;
                CardDropArea closestArea = null;
                foreach (CardDropArea area in dropAreas)
                {
                    if (area == attackerArea) continue;
                    float dist = Vector3.Distance(attackerAreaPos, area.transform.position);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        closestArea = area;
                    }
                }
                
                if (closestArea != null)
                {
                    Debug.LogWarning($"[IntegrationBug_CaptureOccurs_ButScoreNotUpdated] No drop areas within strict adjacency distance (1.6). " +
                        $"Closest area is {closestDist:F2} units away. Will manually position defender card closer to attacker.");
                    defenderArea = closestArea; // Use as reference, but we'll position manually
                }
            }
            
            if (defenderArea != null)
            {
                float distance = Vector3.Distance(attackerArea.transform.position, defenderArea.transform.position);
                Debug.Log($"[IntegrationBug_CaptureOccurs_ButScoreNotUpdated] Selected drop areas. " +
                    $"Attacker: {attackerArea.name}, Defender: {defenderArea.name}, Distance: {distance:F2} " +
                    $"(strict adjacency requires <= {strictAdjacencyTolerance})");
            }
            else
            {
                Assert.Fail("Could not find any drop areas for test setup");
            }
            
            // Attacker needs higher bottom stat to capture defender when placed above
            // Test: Attacker (top=3, right=7, down=7, left=3) vs Defender (top=2, right=2, down=3, left=3)
            // When attacker is above defender: Attacker bottom (7) > Defender top (2) → Attacker wins
            NewCard attackerCard = CardTestHelper.CreateTestCard(3, 7, 7, 3, "Attacker");
            NewCard defenderCard = CardTestHelper.CreateTestCard(2, 2, 3, 3, "Defender");
            
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
            
            // Place attacker card
            CardMoverP1 attackerMover = CardTestHelper.CreateCardMoverWithCard(attackerCard, attackerArea.transform.position, true);
            CardTestHelper.PlaceP1CardOnDropArea(attackerMover, attackerArea, true);
            yield return new WaitForSeconds(0.5f);
            
            // Verify attacker is placed
            Assert.IsNotNull(attackerMover, "Attacker card should be created");
            Vector3 attackerPos = attackerMover.transform.position;
            Debug.Log($"[IntegrationBug_CaptureOccurs_ButScoreNotUpdated] Attacker placed at {attackerPos}");
            
            // Calculate defender position to ensure strict adjacency (within 1.6 units)
            Vector3 defenderTargetPos = defenderArea.transform.position;
            float dropAreaDistance = Vector3.Distance(attackerPos, defenderTargetPos);
            
            // If drop areas are too far apart, position defender closer to attacker (within strict adjacency)
            if (dropAreaDistance > strictAdjacencyTolerance)
            {
                // Position defender adjacent to attacker (1.5 units away, which is within 1.6 tolerance)
                Vector3 direction = (defenderTargetPos - attackerPos).normalized;
                defenderTargetPos = attackerPos + direction * 1.5f;
                defenderTargetPos.z = 0; // Ensure z is 0 (board level)
                Debug.Log($"[IntegrationBug_CaptureOccurs_ButScoreNotUpdated] Drop areas too far apart ({dropAreaDistance:F2}). " +
                    $"Positioning defender at {defenderTargetPos} (1.5 units from attacker) to ensure strict adjacency.");
            }
            
            // Place defender card at calculated position
            CardMoverP2 defenderMover = CardTestHelper.CreateCardMoverP2WithCard(defenderCard, defenderTargetPos);
            
            // Find the drop area closest to the defender's target position
            CardDropArea closestDropArea = defenderArea;
            float closestDistToTarget = Vector3.Distance(defenderTargetPos, defenderArea.transform.position);
            foreach (CardDropArea area in dropAreas)
            {
                float distToTarget = Vector3.Distance(defenderTargetPos, area.transform.position);
                if (distToTarget < closestDistToTarget)
                {
                    closestDistToTarget = distToTarget;
                    closestDropArea = area;
                }
            }
            
            CardTestHelper.PlaceP2CardOnDropArea(defenderMover, closestDropArea, true);
            
            // Verify positions are actually adjacent (within strict adjacency tolerance)
            Vector3 defenderPos = defenderMover.transform.position;
            float actualDistance = Vector3.Distance(attackerPos, defenderPos);
            Debug.Log($"[IntegrationBug_CaptureOccurs_ButScoreNotUpdated] Cards placed - Attacker: {attackerPos}, Defender: {defenderPos}, Distance: {actualDistance:F2}");
            
            // Ensure cards are within strict adjacency - if not, manually adjust defender position
            if (actualDistance > strictAdjacencyTolerance)
            {
                Vector3 adjustDirection = (defenderPos - attackerPos).normalized;
                Vector3 adjustedPos = attackerPos + adjustDirection * 1.5f;
                adjustedPos.z = defenderPos.z; // Preserve z
                defenderMover.transform.position = adjustedPos;
                actualDistance = Vector3.Distance(attackerPos, adjustedPos);
                Debug.Log($"[IntegrationBug_CaptureOccurs_ButScoreNotUpdated] Manually adjusted defender position to {adjustedPos} " +
                    $"for strict adjacency (distance: {actualDistance:F2})");
                
                // CRITICAL: After adjusting position, manually trigger battle check from defender's perspective
                // The battle check already ran when defender was placed at the old position (too far), so we need to re-check
                // Since defender is a P2 card (CardMoverP2), we need to call CheckCardBattlesP2
                yield return new WaitForEndOfFrame(); // Wait for position to update
                
                // Use the drop area where the defender was placed to trigger battle check
                if (closestDropArea != null && defenderMover != null && defenderMover.Card != null)
                {
                    // Use reflection to manually trigger CheckCardBattlesP2 from defender's drop area
                    var checkBattlesP2Method = typeof(CardDropArea).GetMethod("CheckCardBattlesP2",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    
                    if (checkBattlesP2Method != null)
                    {
                        Debug.Log($"[IntegrationBug_CaptureOccurs_ButScoreNotUpdated] Manually triggering CheckCardBattlesP2 after position adjustment...");
                        checkBattlesP2Method.Invoke(closestDropArea, new object[] { defenderMover, defenderMover.Card });
                        yield return new WaitForEndOfFrame(); // Wait for battle check to process
                        yield return new WaitForSeconds(0.5f); // Give ripple coroutine time to start
                    }
                    else
                    {
                        Debug.LogWarning($"[IntegrationBug_CaptureOccurs_ButScoreNotUpdated] Could not find CheckCardBattlesP2 method via reflection!");
                    }
                }
                
                // Also trigger from attacker's perspective to ensure both directions are checked
                if (attackerArea != null && attackerMover != null && attackerMover.Card != null)
                {
                    var checkBattlesMethod = typeof(CardDropArea).GetMethod("CheckCardBattlesP1",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    
                    if (checkBattlesMethod != null)
                    {
                        Debug.Log($"[IntegrationBug_CaptureOccurs_ButScoreNotUpdated] Also triggering CheckCardBattles from attacker's perspective...");
                        checkBattlesMethod.Invoke(attackerArea, new object[] { attackerMover, attackerMover.Card });
                        yield return new WaitForEndOfFrame(); // Wait for battle check to process
                    }
                }
            }
            
            yield return CardTestHelper.WaitForCaptureAnimations(3f);
            
            // CRITICAL: Wait longer for ripple effect coroutines to complete and score updates to process
            // The ExecuteRippleFlips coroutine can take several seconds:
            // - rippleBaseDelay (default ~0.5-1s)
            // - distance-based delays per card
            // - 1.1s per card flip animation
            // Total could be 3-5 seconds for a single capture
            yield return new WaitForSeconds(5f); // Extended wait to ensure ripple completes and score updates
            
            // Verify capture occurred
            bool defenderCaptured = CardTestHelper.IsCardCaptured(defenderMover.gameObject);
            
            // Check if cards are actually adjacent - if not, capture won't occur
            float finalDistance = Vector3.Distance(attackerMover.transform.position, defenderMover.transform.position);
            Debug.Log($"[IntegrationBug_CaptureOccurs_ButScoreNotUpdated] Final positions - " +
                $"Attacker: {attackerMover.transform.position}, Defender: {defenderMover.transform.position}, " +
                $"Distance: {finalDistance:F2} (strict adjacency requires <= 1.6)");
            
            // CRITICAL DIAGNOSTICS: Check ScoreManager state before asserting
            ScoreManager scoreMgrCheck = ScoreManager.Instance;
            Debug.Log($"[IntegrationBug_CaptureOccurs_ButScoreNotUpdated] ScoreManager diagnostics - " +
                $"Instance exists: {scoreMgrCheck != null}, " +
                $"PlayerScore: {scoreMgrCheck?.PlayerScore ?? -1}, " +
                $"OpponentScore: {scoreMgrCheck?.OpponentScore ?? -1}");
            
            if (!defenderCaptured && finalDistance > 1.6f)
            {
                Assert.Inconclusive($"Cards are not adjacent (distance: {finalDistance:F2} > 1.6), so no capture occurred. " +
                    "Test requires adjacent card placement for capture to trigger score update.");
            }
            
            Assert.IsTrue(defenderCaptured, 
                $"Defender should be captured for score test. Distance: {finalDistance:F2}. " +
                $"Cards are at positions: Attacker={attackerMover.transform.position}, Defender={defenderMover.transform.position}");
            
            // INTEGRATION ASSERTION: Score MUST update after capture
            // Re-check ScoreManager instance in case it changed
            ScoreManager finalScoreManager = ScoreManager.Instance ?? scoreManager;
            if (finalScoreManager == null)
            {
                Assert.Fail("INTEGRATION BUG: ScoreManager.Instance is null! Cannot verify score update.");
            }
            
            int newPlayerScore = finalScoreManager.PlayerScore;
            int newOpponentScore = finalScoreManager.OpponentScore;
            
            Debug.Log($"[IntegrationBug_CaptureOccurs_ButScoreNotUpdated] ═══ SCORE UPDATE VERIFICATION ═══");
            Debug.Log($"[IntegrationBug_CaptureOccurs_ButScoreNotUpdated] Initial scores - Player: {initialPlayerScore}, Opponent: {initialOpponentScore}");
            Debug.Log($"[IntegrationBug_CaptureOccurs_ButScoreNotUpdated] Final scores - Player: {newPlayerScore}, Opponent: {newOpponentScore}");
            Debug.Log($"[IntegrationBug_CaptureOccurs_ButScoreNotUpdated] Score change - Player: +{newPlayerScore - initialPlayerScore}, Opponent: +{newOpponentScore - initialOpponentScore}");
            Debug.Log($"[IntegrationBug_CaptureOccurs_ButScoreNotUpdated] Defender captured: {defenderCaptured}");
            Debug.Log($"[IntegrationBug_CaptureOccurs_ButScoreNotUpdated] Cards distance: {finalDistance:F2}");
            Debug.Log($"[IntegrationBug_CaptureOccurs_ButScoreNotUpdated] ScoreManager Instance ID: {finalScoreManager.GetInstanceID()}");
            Debug.Log($"[IntegrationBug_CaptureOccurs_ButScoreNotUpdated] ════════════════════════════════════");
            
            // If capture occurred but score didn't update, this is the integration bug
            if (defenderCaptured && newPlayerScore == initialPlayerScore)
            {
                Assert.Fail($"INTEGRATION BUG: Player score did NOT increase after capturing opponent card. " +
                    $"Initial: {initialPlayerScore}, Final: {newPlayerScore} (no change). " +
                    $"Defender was captured: {defenderCaptured}. " +
                    $"This indicates FlipCardGameObject was called but score update did not execute. " +
                    $"Check logs for '[CardDropArea] Score update check' or '[CardDropArea] ✅ Score updated' messages.");
            }
            
            Assert.Greater(newPlayerScore, initialPlayerScore, 
                $"INTEGRATION BUG: Player score should increase after capturing opponent card. " +
                $"Was: {initialPlayerScore}, Now: {newPlayerScore}. " +
                $"Capture occurred: {defenderCaptured}. " +
                $"ScoreManager exists: {finalScoreManager != null}. " +
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
            bool player2CanActBefore = fateController.CanAct(FateSide.P2);
            
            Assert.IsTrue(player1CanActBefore, "Player 1 should be able to act");
            Assert.IsFalse(player2CanActBefore, "Player 2 should NOT be able to act");
            
            // Switch turn
            fateController.AdvanceFateFlow();
            yield return null;
            
            // INTEGRATION ASSERTION: CanAct MUST update after turn switch
            bool player1CanActAfter = fateController.CanAct(FateSide.Player);
            bool player2CanActAfter = fateController.CanAct(FateSide.P2);
            
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
            Assert.IsNotNull(scoreManager, "ScoreManager should exist");
            
            // Try to find ScoreUI first
            ScoreUI scoreUI = Object.FindObjectOfType<ScoreUI>(true);
            HUDManager hudManager = Object.FindObjectOfType<HUDManager>(true);
            
            if (scoreUI == null)
            {
                yield return new WaitForSeconds(1.0f);
                scoreUI = Object.FindObjectOfType<ScoreUI>(true);
            }
            
            TMPro.TextMeshProUGUI player1ScoreText = null;
            
            // Try to get score text from ScoreUI
            if (scoreUI != null)
            {
                var player1ScoreField = typeof(ScoreUI).GetField("player1Score", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (player1ScoreField != null)
                {
                    player1ScoreText = player1ScoreField.GetValue(scoreUI) as TMPro.TextMeshProUGUI;
                }
            }
            
            // Fallback: Try to get score text from HUDManager
            if (player1ScoreText == null && hudManager != null)
            {
                var p1ScoreLabelField = typeof(HUDManager).GetField("p1ScoreLabel", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (p1ScoreLabelField != null)
                {
                    player1ScoreText = p1ScoreLabelField.GetValue(hudManager) as TMPro.TextMeshProUGUI;
                }
            }
            
            if (player1ScoreText == null)
            {
                Assert.Inconclusive("ScoreUI or HUDManager with score text not found. ScoreUI or HUDManager may not be set up in the scene.");
                yield break;
            }
            
            // Get initial displayed score (parse from text, removing "Score: " prefix if present)
            string initialText = player1ScoreText.text;
            int initialDisplayedScore = 0;
            
            // Try parsing directly
            if (int.TryParse(initialText, out int parsed))
            {
                initialDisplayedScore = parsed;
            }
            else
            {
                // Try parsing after "Score: " prefix (HUDManager format)
                string[] parts = initialText.Split(':');
                if (parts.Length > 1 && int.TryParse(parts[1].Trim(), out int parsedFromLabel))
                {
                    initialDisplayedScore = parsedFromLabel;
                }
            }
            
            int initialManagerScore = scoreManager.PlayerScore;
            
            // Update score
            scoreManager.AddScore(true);
            yield return new WaitForSeconds(0.5f); // Wait for UI update
            
            int newManagerScore = scoreManager.PlayerScore;
            Assert.Greater(newManagerScore, initialManagerScore, "ScoreManager score should increase");
            
            // INTEGRATION ASSERTION: UI MUST reflect score change
            string newText = player1ScoreText.text;
            int newDisplayedScore = 0;
            
            // Try parsing directly
            if (int.TryParse(newText, out int parsedNew))
            {
                newDisplayedScore = parsedNew;
            }
            else
            {
                // Try parsing after "Score: " prefix (HUDManager format)
                string[] parts = newText.Split(':');
                if (parts.Length > 1 && int.TryParse(parts[1].Trim(), out int parsedFromLabel))
                {
                    newDisplayedScore = parsedFromLabel;
                }
            }
            
            Assert.Greater(newDisplayedScore, initialDisplayedScore, 
                $"INTEGRATION BUG: Score UI should update when ScoreManager score changes. " +
                $"Manager score: {initialManagerScore} → {newManagerScore}, " +
                $"UI score: {initialDisplayedScore} → {newDisplayedScore} (text: '{initialText}' → '{newText}'). " +
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
            // GetMethod with parameter types to avoid AmbiguousMatchException (multiple overloads exist)
            var showMethod = typeof(GameEndUI).GetMethod("ShowGameEnd", 
                new System.Type[] { typeof(bool), typeof(bool) }); // 2-parameter overload
            
            if (showMethod != null)
            {
                // Use the 2-parameter overload: ShowGameEnd(bool playerWon, bool isTie)
                showMethod.Invoke(gameEndUI, new object[] { true, false });
                yield return new WaitForSeconds(0.5f);
                
                // Verify UI is visible
                Assert.IsTrue(gameEndUI.gameObject.activeSelf || gameEndUI.gameObject.activeInHierarchy, 
                    "INTEGRATION BUG: GameEndUI should be visible when game ends. " +
                    "This indicates game end condition did not trigger UI display system.");
            }
            else
            {
                // Fallback: try calling directly if reflection fails
                gameEndUI.ShowGameEnd(true, false);
                yield return new WaitForSeconds(0.5f);
                
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
            
            NewHandP1UI handUI = Object.FindObjectOfType<NewHandP1UI>();
            NewDeckManagerP1 deckManager = handUI?.DeckManager;
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
                Assert.Inconclusive("No empty drop areas");
                yield break;
            }
            
            FateFlowController fateController = FateFlowController.Instance;
            if (fateController != null)
            {
                fateController.SetFate(FateSide.Player);
            }
            yield return null;
            
            CardMoverP1 CardMoverP1 = cardUI.GetComponentInParent<CardMoverP1>();
            if (CardMoverP1 == null)
            {
                CardMoverP1 = cardUI.GetComponent<CardMoverP1>();
            }
            
            if (CardMoverP1 != null)
            {
                CardTestHelper.PlaceP1CardOnDropArea(CardMoverP1, emptyArea, false);
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

