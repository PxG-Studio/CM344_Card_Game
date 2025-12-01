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
        public void CardDropArea_Has_DeckManager_Integration()
        {
            // Verify CardDropArea can access deck managers
            var deckManagerP1Field = typeof(CardDropArea).GetField("deckManagerP1",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var deckManagerP2Field = typeof(CardDropArea).GetField("deckManagerP2",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            Assert.IsNotNull(deckManagerP1Field, "CardDropArea should have deckManagerP1 field");
            Assert.IsNotNull(deckManagerP2Field, "CardDropArea should have deckManagerP2 field");
        }

        [Test]
        public void NewHandUI_Has_DeckManager_Integration()
        {
            // Verify NewHandP1UI can access deck manager
            var deckManagerProperty = typeof(NewHandP1UI).GetProperty("DeckManager");
            Assert.IsNotNull(deckManagerProperty, "NewHandP1UI should have DeckManager property");
            Assert.AreEqual(typeof(NewDeckManagerP1), deckManagerProperty.PropertyType,
                "NewHandP1UI.DeckManager should be of type NewDeckManager");
        }

        [Test]
        public void NewHandP2UI_Has_DeckManager_Integration()
        {
            // Verify NewHandP2UI can access P2 deck manager
            var deckManagerProperty = typeof(NewHandP2UI).GetProperty("DeckManager");
            Assert.IsNotNull(deckManagerProperty, "NewHandP2UI should have DeckManager property");
            Assert.AreEqual(typeof(NewDeckManagerP2), deckManagerProperty.PropertyType,
                "NewHandP2UI.DeckManager should be of type NewDeckManagerP2");
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
        public void CardDropArea_Has_ScoreManager_Integration()
        {
            // Verify CardDropArea can access ScoreManager
            var scoreManagerField = typeof(CardDropArea).GetField("scoreManager",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(scoreManagerField, "CardDropArea should have scoreManager field");
        }

        [Test]
        public void CardDropArea_Has_GameEndManager_Integration()
        {
            // Verify CardDropArea can access GameEndManager
            var gameEndManagerField = typeof(CardDropArea).GetField("gameEndManager",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(gameEndManagerField, "CardDropArea should have gameEndManager field");
        }
    }
}

