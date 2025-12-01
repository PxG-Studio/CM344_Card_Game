using NUnit.Framework;
using UnityEngine;
using CardGame.Managers;
using CardGame.Core;

namespace CardGame.Tests
{
    /// <summary>
    /// EditMode tests for logic error detection - validates method signatures and return types.
    /// These tests ensure logic methods have correct signatures to prevent logic errors.
    /// </summary>
    public class LogicErrorDetectionEditModeTests
    {
        [Test]
        public void ScoreManager_AddScore_Has_Correct_Signature()
        {
            // Verify ScoreManager.AddScore has correct signature
            var addScoreMethod = typeof(ScoreManager).GetMethod("AddScore",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(addScoreMethod, "ScoreManager should have AddScore method");
            
            var parameters = addScoreMethod.GetParameters();
            Assert.AreEqual(1, parameters.Length, "AddScore should have 1 parameter");
            Assert.AreEqual(typeof(bool), parameters[0].ParameterType,
                "AddScore parameter should be bool (isPlayer)");
        }

        [Test]
        public void ScoreManager_GetScore_Has_Correct_Signature()
        {
            // Verify ScoreManager has GetScore method with correct signature
            var getScoreMethod = typeof(ScoreManager).GetMethod("GetScore",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance,
                null,
                new System.Type[] { typeof(bool) },
                null);
            Assert.IsNotNull(getScoreMethod, "ScoreManager should have GetScore(bool) method");
            Assert.AreEqual(typeof(int), getScoreMethod.ReturnType,
                "GetScore should return int");
            
            // Also verify PlayerScore and OpponentScore properties exist
            var playerScoreProperty = typeof(ScoreManager).GetProperty("PlayerScore");
            var opponentScoreProperty = typeof(ScoreManager).GetProperty("OpponentScore");
            Assert.IsNotNull(playerScoreProperty, "ScoreManager should have PlayerScore property");
            Assert.IsNotNull(opponentScoreProperty, "ScoreManager should have OpponentScore property");
        }

        [Test]
        public void GameManager_ChangeState_Has_Correct_Signature()
        {
            // Verify GameManager.ChangeState has correct signature
            var changeStateMethod = typeof(GameManager).GetMethod("ChangeState",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(changeStateMethod, "GameManager should have ChangeState method");
            
            var parameters = changeStateMethod.GetParameters();
            Assert.AreEqual(1, parameters.Length, "ChangeState should have 1 parameter");
            Assert.AreEqual(typeof(GameState), parameters[0].ParameterType,
                "ChangeState parameter should be GameState");
        }

        [Test]
        public void GameManager_CurrentState_Has_Correct_Type()
        {
            // Verify GameManager.CurrentState has correct type
            var currentStateProperty = typeof(GameManager).GetProperty("CurrentState");
            Assert.IsNotNull(currentStateProperty, "GameManager should have CurrentState property");
            Assert.AreEqual(typeof(GameState), currentStateProperty.PropertyType,
                "CurrentState should be of type GameState");
        }

        [Test]
        public void FateFlowController_SetFate_Has_Correct_Signature()
        {
            // Verify FateFlowController.SetFate has correct signature
            var setFateMethod = typeof(FateFlowController).GetMethod("SetFate",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(setFateMethod, "FateFlowController should have SetFate method");
            
            var parameters = setFateMethod.GetParameters();
            Assert.AreEqual(1, parameters.Length, "SetFate should have 1 parameter");
            Assert.AreEqual(typeof(FateSide), parameters[0].ParameterType,
                "SetFate parameter should be FateSide");
        }

        [Test]
        public void FateFlowController_CurrentFate_Has_Correct_Type()
        {
            // Verify FateFlowController.CurrentFate has correct type
            var currentFateProperty = typeof(FateFlowController).GetProperty("CurrentFate");
            Assert.IsNotNull(currentFateProperty, "FateFlowController should have CurrentFate property");
            Assert.AreEqual(typeof(FateSide), currentFateProperty.PropertyType,
                "CurrentFate should be of type FateSide");
        }

        [Test]
        public void NewCard_CurrentStats_Are_Integers()
        {
            // Verify NewCard stat properties return integers
            var topStatProperty = typeof(NewCard).GetProperty("CurrentTopStat");
            var rightStatProperty = typeof(NewCard).GetProperty("CurrentRightStat");
            var downStatProperty = typeof(NewCard).GetProperty("CurrentDownStat");
            var leftStatProperty = typeof(NewCard).GetProperty("CurrentLeftStat");
            
            Assert.AreEqual(typeof(int), topStatProperty.PropertyType,
                "CurrentTopStat should return int");
            Assert.AreEqual(typeof(int), rightStatProperty.PropertyType,
                "CurrentRightStat should return int");
            Assert.AreEqual(typeof(int), downStatProperty.PropertyType,
                "CurrentDownStat should return int");
            Assert.AreEqual(typeof(int), leftStatProperty.PropertyType,
                "CurrentLeftStat should return int");
        }

        [Test]
        public void GameEndManager_CheckGameEnd_Has_Correct_Signature()
        {
            // Verify GameEndManager.CheckGameEnd has correct signature
            var checkGameEndMethod = typeof(GameEndManager).GetMethod("CheckGameEnd",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(checkGameEndMethod, "GameEndManager should have CheckGameEnd method");
            Assert.AreEqual(typeof(void), checkGameEndMethod.ReturnType,
                "CheckGameEnd should return void");
        }
    }
}

