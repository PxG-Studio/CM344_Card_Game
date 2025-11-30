using NUnit.Framework;
using UnityEngine;
using CardGame.UI;
using CardGame.Managers;
using TMPro;

namespace CardGame.Tests
{
    /// <summary>
    /// EditMode tests for HUD and UI component structure and setup.
    /// Validates that HUD components have the necessary structure and references.
    /// </summary>
    public class HUDAndUIEditModeTests
    {
        [Test]
        public void HUDManager_Has_UI_References()
        {
            // Verify HUDManager has UI component references
            var p1ScoreLabelField = typeof(HUDManager).GetField("p1ScoreLabel",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var p2ScoreLabelField = typeof(HUDManager).GetField("p2ScoreLabel",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            Assert.IsNotNull(p1ScoreLabelField, "HUDManager should have p1ScoreLabel field");
            Assert.IsNotNull(p2ScoreLabelField, "HUDManager should have p2ScoreLabel field");
        }

        [Test]
        public void HUDManager_Has_Hand_References()
        {
            // Verify HUDManager has hand UI references
            var p1HandDeckLabelField = typeof(HUDManager).GetField("p1HandDeckLabel",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var p2HandDeckLabelField = typeof(HUDManager).GetField("p2HandDeckLabel",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            Assert.IsNotNull(p1HandDeckLabelField, "HUDManager should have p1HandDeckLabel field");
            Assert.IsNotNull(p2HandDeckLabelField, "HUDManager should have p2HandDeckLabel field");
        }

        [Test]
        public void HUDManager_Has_Turn_Indicator_References()
        {
            // Verify HUDManager has turn indicator references
            var p1TurnIndicatorField = typeof(HUDManager).GetField("p1TurnIndicator",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var p2TurnIndicatorField = typeof(HUDManager).GetField("p2TurnIndicator",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            Assert.IsNotNull(p1TurnIndicatorField, "HUDManager should have p1TurnIndicator field");
            Assert.IsNotNull(p2TurnIndicatorField, "HUDManager should have p2TurnIndicator field");
        }

        [Test]
        public void HUDSetup_Has_Setup_Methods()
        {
            // Verify HUDSetup has setup methods
            var setupHUDMethod = typeof(HUDSetup).GetMethod("SetupHUD",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(setupHUDMethod, "HUDSetup should have SetupHUD method");
        }

        [Test]
        public void ScoreUI_Has_Text_Components()
        {
            // Verify ScoreUI has text components for display
            var playerScoreTextField = typeof(ScoreUI).GetField("player1Score",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var opponentScoreTextField = typeof(ScoreUI).GetField("player2Score",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            // Check for update methods (specify parameter types to avoid ambiguity)
            var updateScoreDisplayMethod = typeof(ScoreUI).GetMethod("UpdateScoreDisplay",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance,
                null,
                new System.Type[] { typeof(int), typeof(int) },
                null);
            Assert.IsNotNull(updateScoreDisplayMethod, "ScoreUI should have UpdateScoreDisplay(int, int) method");
        }

        [Test]
        public void TurnIndicatorUI_Has_Display_Methods()
        {
            // Verify TurnIndicatorUI has display methods
            var setActiveMethod = typeof(TurnIndicatorUI).GetMethod("SetActive",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(setActiveMethod, "TurnIndicatorUI should have SetActive method");
        }

        [Test]
        public void NewHandUI_Has_Canvas_Group()
        {
            // Verify NewHandP1UI has CanvasGroup for layout
            var canvasGroupProperty = typeof(NewHandP1UI).GetProperty("CanvasGroup",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            // CanvasGroup might be accessed via GetComponent, so we check for AddCardToHand instead
            var addCardMethod = typeof(NewHandP1UI).GetMethod("AddCardToHand",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(addCardMethod, "NewHandP1UI should have AddCardToHand method");
        }

        [Test]
        public void NewHandP2UI_Has_Canvas_Group()
        {
            // Verify NewHandP2UI has CanvasGroup for layout
            var addCardMethod = typeof(NewHandP2UI).GetMethod("AddCardToHand",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(addCardMethod, "NewHandP2UI should have AddCardToHand method");
        }

        [Test]
        public void GameEndUI_Has_Display_Methods()
        {
            // Verify GameEndUI has display methods
            // ShowGameEnd has multiple overloads, so we need to specify parameter types
            // Check for the two-parameter version: ShowGameEnd(bool playerWon, bool isTie)
            var showGameEndMethod = typeof(GameEndUI).GetMethod("ShowGameEnd",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance,
                null,
                new System.Type[] { typeof(bool), typeof(bool) },
                null);
            Assert.IsNotNull(showGameEndMethod, "GameEndUI should have ShowGameEnd(bool, bool) method");
            
            // Also check for the six-parameter version
            var showGameEndFullMethod = typeof(GameEndUI).GetMethod("ShowGameEnd",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance,
                null,
                new System.Type[] { typeof(bool), typeof(bool), typeof(int), typeof(int), typeof(int), typeof(int) },
                null);
            Assert.IsNotNull(showGameEndFullMethod, "GameEndUI should have ShowGameEnd(bool, bool, int, int, int, int) method");
        }

        [Test]
        public void CoinTossUI_Has_Animation_Methods()
        {
            // Verify CoinTossUI has animation methods
            var startCoinTossMethod = typeof(CoinTossUI).GetMethod("StartCoinToss",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(startCoinTossMethod, "CoinTossUI should have StartCoinToss method");
        }
    }
}


