using NUnit.Framework;
using UnityEngine;
using CardGame.Managers;
using CardGame.UI;
using TMPro;

namespace CardGame.Tests
{
    /// <summary>
    /// EditMode tests for UI update validation - validates UI update methods and event subscriptions exist.
    /// These tests ensure UI components have the necessary methods to update when game state changes.
    /// </summary>
    public class UIUpdateValidationEditModeTests
    {
        [Test]
        public void ScoreUI_Has_Update_Methods()
        {
            // Verify ScoreUI has methods to update score display
            var updateScoreDisplayMethod = typeof(ScoreUI).GetMethod("UpdateScoreDisplay",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance,
                null,
                new System.Type[] { typeof(int), typeof(int) },
                null);
            Assert.IsNotNull(updateScoreDisplayMethod, "ScoreUI should have UpdateScoreDisplay(int, int) method");
            
            // Also check for SetScores method
            var setScoresMethod = typeof(ScoreUI).GetMethod("SetScores",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance,
                null,
                new System.Type[] { typeof(int), typeof(int) },
                null);
            Assert.IsNotNull(setScoresMethod, "ScoreUI should have SetScores(int, int) method");
        }

        [Test]
        public void TurnIndicatorUI_Has_Update_Methods()
        {
            // Verify TurnIndicatorUI has methods to update turn display
            var setActiveMethod = typeof(TurnIndicatorUI).GetMethod("SetActive",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance,
                null,
                new System.Type[] { typeof(bool) },
                null);
            Assert.IsNotNull(setActiveMethod, "TurnIndicatorUI should have SetActive(bool) method to control turn indicator");
        }

        [Test]
        public void NewHandUI_Has_Update_Methods()
        {
            // Verify NewHandP1UI has methods to update hand display
            var addCardToHandMethod = typeof(NewHandP1UI).GetMethod("AddCardToHand",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var removeCardFromHandMethod = typeof(NewHandP1UI).GetMethod("RemoveCardFromHand",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            
            Assert.IsNotNull(addCardToHandMethod, "NewHandP1UI should have AddCardToHand method");
            Assert.IsNotNull(removeCardFromHandMethod, "NewHandP1UI should have RemoveCardFromHand method");
        }

        [Test]
        public void NewHandP2UI_Has_Update_Methods()
        {
            // Verify NewHandP2UI has methods to update hand display
            var addCardToHandMethod = typeof(NewHandP2UI).GetMethod("AddCardToHand",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var removeCardFromHandMethod = typeof(NewHandP2UI).GetMethod("RemoveCardFromHand",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            
            Assert.IsNotNull(addCardToHandMethod, "NewHandP2UI should have AddCardToHand method");
            Assert.IsNotNull(removeCardFromHandMethod, "NewHandP2UI should have RemoveCardFromHand method");
        }

        [Test]
        public void GameEndUI_Has_Display_Methods()
        {
            // Verify GameEndUI has methods to display game end (specify parameter types to avoid ambiguity)
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
        public void ScoreManager_Subscribes_To_Events()
        {
            // Verify ScoreManager can subscribe to score update events
            var onScoreUpdatedField = typeof(ScoreManager).GetField("OnScoreUpdated",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(onScoreUpdatedField, "ScoreManager should have OnScoreUpdated event for UI subscription");
        }

        [Test]
        public void GameManager_Subscribes_To_State_Events()
        {
            // Verify GameManager has state change events for UI subscription
            var onGameStateChangedField = typeof(GameManager).GetField("OnGameStateChanged",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(onGameStateChangedField, "GameManager should have OnGameStateChanged event for UI subscription");
        }

        [Test]
        public void FateFlowController_Has_Turn_Events()
        {
            // Verify FateFlowController has turn change events (use GetEvent for events)
            var onFateChangedEvent = typeof(FateFlowController).GetEvent("OnFateChanged",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(onFateChangedEvent, "FateFlowController should have OnFateChanged event for UI subscription");
        }
    }
}

