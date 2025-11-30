using NUnit.Framework;
using UnityEngine;
using System.Reflection;
// ScoreUI is in global namespace

namespace CardGame.Tests
{
    /// <summary>
    /// EditMode tests for ScoreUI - validates structure and API.
    /// Tests component structure, methods, and event subscriptions.
    /// </summary>
    public class ScoreUIEditModeTests
    {
        [Test]
        public void ScoreUI_Has_Required_Fields()
        {
            // Verify ScoreUI has required text fields
            var player1ScoreField = typeof(ScoreUI).GetField("player1Score",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var player2ScoreField = typeof(ScoreUI).GetField("player2Score",
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            Assert.IsNotNull(player1ScoreField, "ScoreUI should have player1Score field");
            Assert.IsNotNull(player2ScoreField, "ScoreUI should have player2Score field");
            
            // Verify field types
            Assert.AreEqual(typeof(TMPro.TMP_Text), player1ScoreField.FieldType,
                "player1Score should be TMP_Text");
            Assert.AreEqual(typeof(TMPro.TMP_Text), player2ScoreField.FieldType,
                "player2Score should be TMP_Text");
        }

        [Test]
        public void ScoreUI_Has_UpdateScoreDisplay_Method()
        {
            var updateMethod = typeof(ScoreUI).GetMethod("UpdateScoreDisplay",
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new System.Type[] { typeof(int), typeof(int) },
                null);
            
            Assert.IsNotNull(updateMethod, "ScoreUI should have UpdateScoreDisplay(int, int) method");
            
            // Verify return type
            Assert.AreEqual(typeof(void), updateMethod.ReturnType,
                "UpdateScoreDisplay should return void");
        }

        [Test]
        public void ScoreUI_Has_SetScores_Method()
        {
            var setScoresMethod = typeof(ScoreUI).GetMethod("SetScores",
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new System.Type[] { typeof(int), typeof(int) },
                null);
            
            Assert.IsNotNull(setScoresMethod, "ScoreUI should have SetScores(int, int) method");
        }

        [Test]
        public void ScoreUI_Can_Be_Created()
        {
            GameObject go = new GameObject("TestScoreUI");
            ScoreUI scoreUI = go.AddComponent<ScoreUI>();
            
            Assert.IsNotNull(scoreUI, "ScoreUI component should be creatable");
            
            Object.DestroyImmediate(go);
        }

        [Test]
        public void ScoreUI_UpdateScoreDisplay_Can_Be_Called()
        {
            GameObject go = new GameObject("TestScoreUI");
            ScoreUI scoreUI = go.AddComponent<ScoreUI>();
            
            // Call UpdateScoreDisplay (should not throw even if fields are null)
            try
            {
                scoreUI.UpdateScoreDisplay(5, 3);
                Assert.IsTrue(true, "UpdateScoreDisplay should be callable");
            }
            catch (System.Exception ex)
            {
                Assert.Fail($"UpdateScoreDisplay should not throw. Error: {ex.Message}");
            }
            
            Object.DestroyImmediate(go);
        }
    }
}

