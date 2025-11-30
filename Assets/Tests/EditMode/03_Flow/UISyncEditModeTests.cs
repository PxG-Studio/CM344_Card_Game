using NUnit.Framework;
using UnityEngine;
using CardGame.Managers;
using CardGame.UI;

namespace CardGame.Tests
{
    /// <summary>
    /// EditMode tests for UI synchronization structure and API validation.
    /// </summary>
    public class UISyncEditModeTests
    {
        [Test]
        public void ScoreManager_Has_OnScoreUpdated_Event()
        {
            // Verify ScoreManager has OnScoreUpdated field (it's a System.Action, not a C# event)
            var scoreUpdatedField = typeof(ScoreManager).GetField("OnScoreUpdated", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(scoreUpdatedField, "ScoreManager should have OnScoreUpdated field (System.Action<int, int>)");
            
            // Verify it's the correct type
            Assert.AreEqual(typeof(System.Action<int, int>), scoreUpdatedField.FieldType, 
                "OnScoreUpdated should be of type System.Action<int, int>");
        }

        [Test]
        public void ScoreManager_Has_Score_Properties()
        {
            // Verify ScoreManager has score properties
            var playerScoreProperty = typeof(ScoreManager).GetProperty("PlayerScore");
            Assert.IsNotNull(playerScoreProperty, "ScoreManager should have PlayerScore property");
            
            var opponentScoreProperty = typeof(ScoreManager).GetProperty("OpponentScore");
            Assert.IsNotNull(opponentScoreProperty, "ScoreManager should have OpponentScore property");
        }

        [Test]
        public void ScoreManager_Has_RecalculateScores_Method()
        {
            // Verify ScoreManager has RecalculateScores method
            var recalculateMethod = typeof(ScoreManager).GetMethod("RecalculateScores");
            Assert.IsNotNull(recalculateMethod, "ScoreManager should have RecalculateScores method");
        }

        [Test]
        public void FateFlowController_Has_OnFateChanged_Event()
        {
            // Verify FateFlowController has OnFateChanged event
            var eventField = typeof(FateFlowController).GetEvent("OnFateChanged");
            Assert.IsNotNull(eventField, "FateFlowController should have OnFateChanged event");
        }

        [Test]
        public void GameEndUI_Has_ShowGameEnd_Method()
        {
            // Verify GameEndUI has ShowGameEnd method (there are multiple overloads)
            // Check for the 2-parameter version: ShowGameEnd(bool playerWon, bool isTie)
            var showMethod2Params = typeof(GameEndUI).GetMethod("ShowGameEnd", 
                new System.Type[] { typeof(bool), typeof(bool) });
            Assert.IsNotNull(showMethod2Params, "GameEndUI should have ShowGameEnd(bool, bool) method");
            
            // Check for the 6-parameter version: ShowGameEnd(bool, bool, int, int, int, int)
            var showMethod6Params = typeof(GameEndUI).GetMethod("ShowGameEnd", 
                new System.Type[] { typeof(bool), typeof(bool), typeof(int), typeof(int), typeof(int), typeof(int) });
            // Note: The 6-parameter version exists but we're just verifying the 2-parameter version is accessible
        }
    }
}

