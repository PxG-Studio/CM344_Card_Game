using NUnit.Framework;
using UnityEngine;
using CardGame.Managers;
using CardGame.UI;
using CardGame.Core;

namespace CardGame.Tests
{
    /// <summary>
    /// EditMode tests for integration bug detection - validates API contracts and integration points.
    /// These tests ensure systems can communicate correctly at the API level.
    /// </summary>
    public class IntegrationBugDetectionEditModeTests
    {
        [Test]
        public void GameManager_Has_ScoreManager_Integration()
        {
            // Verify GameManager can access ScoreManager via singleton pattern
            // GameManager uses ScoreManager.Instance rather than a direct field reference
            var instanceProperty = typeof(ScoreManager).GetProperty("Instance",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            Assert.IsNotNull(instanceProperty, "ScoreManager should have Instance property for singleton access");
            Assert.AreEqual(typeof(ScoreManager), instanceProperty.PropertyType,
                "ScoreManager.Instance should be of type ScoreManager");
        }

        [Test]
        public void GameManager_Has_GameEndManager_Integration()
        {
            // Verify GameManager can access GameEndManager via singleton pattern
            // GameManager uses GameEndManager.Instance rather than a direct field reference
            var instanceProperty = typeof(GameEndManager).GetProperty("Instance",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            Assert.IsNotNull(instanceProperty, "GameEndManager should have Instance property for singleton access");
            Assert.AreEqual(typeof(GameEndManager), instanceProperty.PropertyType,
                "GameEndManager.Instance should be of type GameEndManager");
        }

        [Test]
        public void CardDropArea1_Has_DeckManager_Integration()
        {
            // Verify CardDropArea1 can access deck managers
            var deckManagerField = typeof(CardDropArea1).GetField("deckManager",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var deckManagerOppField = typeof(CardDropArea1).GetField("deckManagerOpp",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            Assert.IsNotNull(deckManagerField, "CardDropArea1 should have deckManager field");
            Assert.IsNotNull(deckManagerOppField, "CardDropArea1 should have deckManagerOpp field");
        }

        [Test]
        public void NewHandUI_Has_DeckManager_Integration()
        {
            // Verify NewHandUI can access deck manager
            var deckManagerProperty = typeof(NewHandUI).GetProperty("DeckManager");
            Assert.IsNotNull(deckManagerProperty, "NewHandUI should have DeckManager property");
            Assert.AreEqual(typeof(NewDeckManager), deckManagerProperty.PropertyType,
                "NewHandUI.DeckManager should be of type NewDeckManager");
        }

        [Test]
        public void NewHandOppUI_Has_DeckManager_Integration()
        {
            // Verify NewHandOppUI can access opponent deck manager
            var deckManagerProperty = typeof(NewHandOppUI).GetProperty("DeckManager");
            Assert.IsNotNull(deckManagerProperty, "NewHandOppUI should have DeckManager property");
            Assert.AreEqual(typeof(NewDeckManagerOpp), deckManagerProperty.PropertyType,
                "NewHandOppUI.DeckManager should be of type NewDeckManagerOpp");
        }

        [Test]
        public void ScoreManager_Has_Event_Integration()
        {
            // Verify ScoreManager has score update events
            var onScoreUpdatedField = typeof(ScoreManager).GetField("OnScoreUpdated",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(onScoreUpdatedField, "ScoreManager should have OnScoreUpdated event");
        }

        [Test]
        public void GameManager_Has_State_Change_Event()
        {
            // Verify GameManager has state change events
            var onGameStateChangedField = typeof(GameManager).GetField("OnGameStateChanged",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(onGameStateChangedField, "GameManager should have OnGameStateChanged event");
        }

        [Test]
        public void CardDropArea1_Has_ScoreManager_Integration()
        {
            // Verify CardDropArea1 can access ScoreManager
            var scoreManagerField = typeof(CardDropArea1).GetField("scoreManager",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(scoreManagerField, "CardDropArea1 should have scoreManager field");
        }

        [Test]
        public void CardDropArea1_Has_GameEndManager_Integration()
        {
            // Verify CardDropArea1 can access GameEndManager
            var gameEndManagerField = typeof(CardDropArea1).GetField("gameEndManager",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(gameEndManagerField, "CardDropArea1 should have gameEndManager field");
        }
    }
}

