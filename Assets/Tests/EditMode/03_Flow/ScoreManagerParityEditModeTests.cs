using NUnit.Framework;
using UnityEngine;
using CardGame.Managers;

namespace CardGame.Tests.EditMode
{
    /// <summary>
    /// EditMode coverage to ensure ScoreManager treats P1 and P2 symmetrically.
    /// </summary>
    public class ScoreManagerParityEditModeTests
    {
        private GameObject scoreManagerGO;
        private ScoreManager scoreManager;

        [SetUp]
        public void SetUp()
        {
            // Ensure no lingering instance interferes with the test domain.
            var existingManagers = Object.FindObjectsOfType<ScoreManager>();
            foreach (var manager in existingManagers)
            {
                if (manager != null)
                {
                    Object.DestroyImmediate(manager.gameObject);
                }
            }

            scoreManagerGO = new GameObject("ScoreManager_Test");
            scoreManager = scoreManagerGO.AddComponent<ScoreManager>();
            scoreManager.ResetScores();
        }

        [TearDown]
        public void TearDown()
        {
            if (scoreManagerGO != null)
            {
                Object.DestroyImmediate(scoreManagerGO);
            }
        }

        [TestCase(true, TestName = "AddScore_Increments_P1")]
        [TestCase(false, TestName = "AddScore_Increments_P2")]
        public void AddScore_IncrementsCorrectSide(bool isPlayerOne)
        {
            int beforeP1 = scoreManager.P1Score;
            int beforeP2 = scoreManager.P2Score;

            scoreManager.AddScore(isPlayerOne);

            if (isPlayerOne)
            {
                Assert.AreEqual(beforeP1 + 1, scoreManager.P1Score, "P1 points should increment when player flag is true.");
                Assert.AreEqual(beforeP2, scoreManager.P2Score, "P2 score should stay unchanged when P1 scores.");
            }
            else
            {
                Assert.AreEqual(beforeP1, scoreManager.P1Score, "P1 score should stay unchanged when P2 scores.");
                Assert.AreEqual(beforeP2 + 1, scoreManager.P2Score, "P2 points should increment when player flag is false.");
            }
        }

        [Test]
        public void ResetScores_ClearsBothSides()
        {
            scoreManager.AddScore(true);
            scoreManager.AddScore(false);
            scoreManager.AddScore(false);

            scoreManager.ResetScores();

            Assert.AreEqual(0, scoreManager.P1Score, "ResetScores should clear P1 points.");
            Assert.AreEqual(0, scoreManager.P2Score, "ResetScores should clear P2 points.");
        }
    }
}

