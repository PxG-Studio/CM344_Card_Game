using NUnit.Framework;
using UnityEngine;
using CardGame.Managers;
using CardGame.Core;

namespace CardGame.Tests
{
    /// <summary>
    /// EditMode tests for game state management APIs.
    /// Validates that game state management has the necessary methods and properties.
    /// </summary>
    public class GameStateAndFlowEditModeTests
    {
        [Test]
        public void GameManager_Has_State_Management()
        {
            // Verify GameManager has state management
            var currentStateProperty = typeof(GameManager).GetProperty("CurrentState");
            var changeStateMethod = typeof(GameManager).GetMethod("ChangeState",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            
            Assert.IsNotNull(currentStateProperty, "GameManager should have CurrentState property");
            Assert.IsNotNull(changeStateMethod, "GameManager should have ChangeState method");
            Assert.AreEqual(typeof(GameState), currentStateProperty.PropertyType,
                "CurrentState should be of type GameState");
        }

        [Test]
        public void GameManager_Has_State_Events()
        {
            // Verify GameManager has state change events
            var onGameStateChangedField = typeof(GameManager).GetField("OnGameStateChanged",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(onGameStateChangedField, "GameManager should have OnGameStateChanged event");
        }

        [Test]
        public void GameManager_Has_Game_Flow_Methods()
        {
            // Verify GameManager has game flow methods
            var startGameMethod = typeof(GameManager).GetMethod("StartGame",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var prepareGameMethod = typeof(GameManager).GetMethod("PrepareGame", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var resetGameStateMethod = typeof(GameManager).GetMethod("ResetGameState",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            
            Assert.IsNotNull(startGameMethod, "GameManager should have StartGame method");
            Assert.IsNotNull(prepareGameMethod, "GameManager should have PrepareGame method (private)");
            Assert.IsNotNull(resetGameStateMethod, "GameManager should have ResetGameState method");
        }

        [Test]
        public void FateFlowController_Has_Turn_Management()
        {
            // Verify FateFlowController has turn management
            var currentFateProperty = typeof(FateFlowController).GetProperty("CurrentFate");
            var setFateMethod = typeof(FateFlowController).GetMethod("SetFate",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var advanceFateFlowMethod = typeof(FateFlowController).GetMethod("AdvanceFateFlow",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            
            Assert.IsNotNull(currentFateProperty, "FateFlowController should have CurrentFate property");
            Assert.IsNotNull(setFateMethod, "FateFlowController should have SetFate method");
            Assert.IsNotNull(advanceFateFlowMethod, "FateFlowController should have AdvanceFateFlow method");
            Assert.AreEqual(typeof(FateSide), currentFateProperty.PropertyType,
                "CurrentFate should be of type FateSide");
        }

        [Test]
        public void FateFlowController_Has_Turn_Events()
        {
            // Verify FateFlowController has turn change events
            var onFateChangedEvent = typeof(FateFlowController).GetEvent("OnFateChanged");
            Assert.IsNotNull(onFateChangedEvent, "FateFlowController should have OnFateChanged event");
        }

        [Test]
        public void GameState_Enum_Is_Defined()
        {
            // Verify GameState enum exists and has expected values
            Assert.IsTrue(System.Enum.IsDefined(typeof(GameState), GameState.Menu),
                "GameState.Menu should be defined");
            Assert.IsTrue(System.Enum.IsDefined(typeof(GameState), GameState.Preparing),
                "GameState.Preparing should be defined");
            Assert.IsTrue(System.Enum.IsDefined(typeof(GameState), GameState.PlayerTurn),
                "GameState.PlayerTurn should be defined");
            Assert.IsTrue(System.Enum.IsDefined(typeof(GameState), GameState.EnemyTurn),
                "GameState.EnemyTurn should be defined");
        }

        [Test]
        public void FateSide_Enum_Is_Defined()
        {
            // Verify FateSide enum exists and has expected values
            Assert.IsTrue(System.Enum.IsDefined(typeof(FateSide), FateSide.P1),
                "FateSide.P1 should be defined");
            Assert.IsTrue(System.Enum.IsDefined(typeof(FateSide), FateSide.P2),
                "FateSide.P2 should be defined");
        }

        [Test]
        public void GameEndManager_Has_End_Condition_Checks()
        {
            // Verify GameEndManager has end condition checking
            var checkGameEndMethod = typeof(GameEndManager).GetMethod("CheckGameEnd",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(checkGameEndMethod, "GameEndManager should have CheckGameEnd method");
        }

        [Test]
        public void CoinTossManager_Has_Toss_Methods()
        {
            // Verify CoinTossManager has toss methods
            var performCoinTossMethod = typeof(CoinTossManager).GetMethod("PerformCoinToss",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(performCoinTossMethod, "CoinTossManager should have PerformCoinToss method");
        }

        [Test]
        public void CoinTossManager_Has_Result_Properties()
        {
            // Verify CoinTossManager has result properties
            var isCompleteProperty = typeof(CoinTossManager).GetProperty("IsComplete");
            var resultProperty = typeof(CoinTossManager).GetProperty("Result");
            var getStartingPlayerMethod = typeof(CoinTossManager).GetMethod("GetStartingPlayer");
            
            Assert.IsNotNull(isCompleteProperty, "CoinTossManager should have IsComplete property");
            Assert.IsNotNull(resultProperty, "CoinTossManager should have Result property");
            Assert.IsNotNull(getStartingPlayerMethod, "CoinTossManager should have GetStartingPlayer method");
        }
    }
}





