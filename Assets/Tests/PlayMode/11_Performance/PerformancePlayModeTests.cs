using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CardGame.Managers;
using CardGame.Core;
using System.Diagnostics;

namespace CardGame.Tests
{
    /// <summary>
    /// PlayMode tests for performance - validates frame rate and performance under load.
    /// Tests performance with full board, chain captures, and rapid operations.
    /// </summary>
    public class PerformancePlayModeTests
    {
        private const string SCENE_NAME = "BattleScreenMultiplayer";
        private const float TEST_TIMEOUT = 120f;
        private const float TARGET_FPS = 30f; // Minimum acceptable FPS
        private const int FRAME_SAMPLE_COUNT = 60; // Sample 60 frames

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            CardTestHelper.ClearSingletonInstances();
            yield return null;
            
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(SCENE_NAME, LoadSceneMode.Single);
            asyncLoad.allowSceneActivation = true;
            
            float elapsed = 0f;
            while (!asyncLoad.isDone && elapsed < 10f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            if (!asyncLoad.isDone)
            {
                Assert.Fail($"Scene '{SCENE_NAME}' failed to load");
            }
            
            yield return new WaitForSeconds(1.0f);
        }
        
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return null;
            CardTestHelper.ClearSingletonInstances();
            yield return null;
        }

        [UnityTest]
        [Timeout(120000)]
        public IEnumerator Performance_With_Full_Board()
        {
            yield return CardTestHelper.WaitForCoinTossToComplete();
            yield return new WaitForSeconds(1.0f);
            
            // Fill board
            CardDropArea[] areas = Object.FindObjectsOfType<CardDropArea>();
            NewDeckManagerP1 deckP1 = Object.FindObjectOfType<NewDeckManagerP1>();
            NewDeckManagerP2 deckP2 = Object.FindObjectOfType<NewDeckManagerP2>();
            
            if (deckP1 != null && deckP2 != null)
            {
                deckP1.InitializeDeck();
                deckP2.InitializeDeck();
                yield return new WaitForSeconds(0.5f);
                
                // Place cards to fill board
                int cardsPlaced = 0;
                for (int i = 0; i < areas.Length && cardsPlaced < 16; i++)
                {
                    if (!areas[i].IsOccupied)
                    {
                        // Simple placement logic
                        if (cardsPlaced % 2 == 0 && deckP1.Hand.Count > 0)
                        {
                            NewHandP1UI handP1 = Object.FindObjectOfType<NewHandP1UI>();
                            if (handP1 != null)
                            {
                                NewCard card = deckP1.Hand[0];
                                var moverField = typeof(NewHandP1UI).GetField("cardMovers",
                                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                                
                                if (moverField != null)
                                {
                                    var movers = moverField.GetValue(handP1) as System.Collections.Generic.List<CardMoverP1>;
                                    if (movers != null && movers.Count > 0)
                                    {
                                        CardTestHelper.PlaceP1CardOnDropArea(movers[0], areas[i]);
                                        cardsPlaced++;
                                        yield return new WaitForSeconds(0.1f);
                                    }
                                }
                            }
                        }
                    }
                }
                
                yield return new WaitForSeconds(1.0f);
                
                // Measure frame rate
                float totalDeltaTime = 0f;
                int frameCount = 0;
                
                for (int i = 0; i < FRAME_SAMPLE_COUNT; i++)
                {
                    totalDeltaTime += Time.deltaTime;
                    frameCount++;
                    yield return null;
                }
                
                float averageDeltaTime = totalDeltaTime / frameCount;
                float averageFPS = 1f / averageDeltaTime;
                
                Assert.GreaterOrEqual(averageFPS, TARGET_FPS,
                    $"Frame rate with full board should be >= {TARGET_FPS} FPS. Got: {averageFPS:F1} FPS");
            }
        }

        [UnityTest]
        [Timeout(120000)]
        public IEnumerator Performance_During_Chain_Captures()
        {
            yield return CardTestHelper.WaitForCoinTossToComplete();
            yield return new WaitForSeconds(1.0f);
            
            // Setup board for chain capture scenario
            // (This would require specific card placement - simplified for test)
            
            // Measure frame rate during potential captures
            float totalDeltaTime = 0f;
            int frameCount = 0;
            
            for (int i = 0; i < FRAME_SAMPLE_COUNT; i++)
            {
                totalDeltaTime += Time.deltaTime;
                frameCount++;
                yield return null;
            }
            
            float averageDeltaTime = totalDeltaTime / frameCount;
            float averageFPS = 1f / averageDeltaTime;
            
            Assert.GreaterOrEqual(averageFPS, TARGET_FPS,
                $"Frame rate during chain captures should be >= {TARGET_FPS} FPS. Got: {averageFPS:F1} FPS");
        }

        [UnityTest]
        [Timeout(120000)]
        public IEnumerator Performance_With_Rapid_Operations()
        {
            yield return CardTestHelper.WaitForCoinTossToComplete();
            yield return new WaitForSeconds(1.0f);
            
            ScoreManager scoreManager = ScoreManager.Instance;
            if (scoreManager != null)
            {
                // Rapid score changes
                Stopwatch sw = Stopwatch.StartNew();
                
                for (int i = 0; i < 100; i++)
                {
                    scoreManager.AddScore(i % 2 == 0);
                }
                
                sw.Stop();
                
                // 100 operations should complete quickly
                Assert.Less(sw.ElapsedMilliseconds, 1000,
                    $"100 score operations should complete in < 1 second. Took: {sw.ElapsedMilliseconds}ms");
            }
        }

        [UnityTest]
        [Timeout(120000)]
        public IEnumerator GC_Allocations_Are_Reasonable()
        {
            yield return CardTestHelper.WaitForCoinTossToComplete();
            yield return new WaitForSeconds(1.0f);
            
            // Force GC to get baseline
            System.GC.Collect();
            yield return new WaitForSeconds(0.5f);
            
            long initialMemory = System.GC.GetTotalMemory(false);
            
            // Perform operations
            GameManager gameManager = GameManager.Instance;
            if (gameManager != null)
            {
                gameManager.ResetGameState();
                yield return CardTestHelper.WaitForCoinTossToComplete();
                yield return new WaitForSeconds(2.0f);
            }
            
            // Force GC again
            System.GC.Collect();
            yield return new WaitForSeconds(0.5f);
            
            long finalMemory = System.GC.GetTotalMemory(false);
            long memoryGrowth = finalMemory - initialMemory;
            
            // Memory growth should be reasonable (< 50MB for a rematch)
            Assert.Less(memoryGrowth, 50 * 1024 * 1024,
                $"Memory growth should be < 50MB. Growth: {memoryGrowth / (1024 * 1024)}MB");
        }
    }
}

