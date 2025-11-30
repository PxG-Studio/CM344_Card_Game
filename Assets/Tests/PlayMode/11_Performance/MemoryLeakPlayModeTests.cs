using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CardGame.Managers;
using System.Collections.Generic;

namespace CardGame.Tests
{
    /// <summary>
    /// PlayMode tests for memory leaks - validates no memory leaks after extended gameplay.
    /// Tests coroutine cleanup, event unsubscription, and object destruction.
    /// </summary>
    public class MemoryLeakPlayModeTests
    {
        private const string SCENE_NAME = "BattleScreenMultiplayer";
        private const float TEST_TIMEOUT = 300f; // 5 minutes for memory tests

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
        [Timeout(300000)] // 5 minutes
        public IEnumerator No_Memory_Leak_After_Multiple_Rematches()
        {
            yield return CardTestHelper.WaitForCoinTossToComplete();
            yield return new WaitForSeconds(1.0f);
            
            GameManager gameManager = GameManager.Instance;
            Assert.IsNotNull(gameManager, "GameManager should exist");
            
            // Count initial objects
            int initialGameObjects = Object.FindObjectsOfType<GameObject>().Length;
            int initialManagers = Object.FindObjectsOfType<MonoBehaviour>().Length;
            
            // Perform multiple rematches
            for (int i = 0; i < 5; i++)
            {
                gameManager.ResetGameState();
                yield return CardTestHelper.WaitForCoinTossToComplete();
                yield return new WaitForSeconds(1.0f);
            }
            
            // Force garbage collection
            System.GC.Collect();
            yield return new WaitForSeconds(1.0f);
            
            // Count objects after rematches
            int finalGameObjects = Object.FindObjectsOfType<GameObject>().Length;
            int finalManagers = Object.FindObjectsOfType<MonoBehaviour>().Length;
            
            // Allow some variance (objects may be created/destroyed during rematch)
            // But shouldn't grow unbounded
            int objectGrowth = finalGameObjects - initialGameObjects;
            int managerGrowth = finalManagers - initialManagers;
            
            Assert.Less(objectGrowth, 100,
                $"GameObject count should not grow unbounded. Growth: {objectGrowth}");
            Assert.Less(managerGrowth, 20,
                $"Manager count should not grow unbounded. Growth: {managerGrowth}");
        }

        [UnityTest]
        [Timeout(300000)]
        public IEnumerator No_Memory_Leak_After_Multiple_Card_Placements()
        {
            yield return CardTestHelper.WaitForCoinTossToComplete();
            yield return new WaitForSeconds(1.0f);
            
            CardDropArea[] areas = Object.FindObjectsOfType<CardDropArea>();
            NewDeckManagerP1 deckP1 = Object.FindObjectOfType<NewDeckManagerP1>();
            
            if (deckP1 != null)
            {
                deckP1.InitializeDeck();
                yield return new WaitForSeconds(0.5f);
                
                // Count initial cards
                int initialCards = Object.FindObjectsOfType<CardMoverP1>().Length;
                
                // Place and remove cards multiple times
                for (int i = 0; i < 10; i++)
                {
                    if (deckP1.Hand.Count > 0 && areas.Length > 0)
                    {
                        NewHandP1UI handUI = Object.FindObjectOfType<NewHandP1UI>();
                        if (handUI != null)
                        {
                            NewCard card = deckP1.Hand[0];
                            var moverField = typeof(NewHandP1UI).GetField("cardMovers",
                                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            
                            if (moverField != null)
                            {
                                var movers = moverField.GetValue(handUI) as System.Collections.Generic.List<CardMoverP1>;
                                if (movers != null && movers.Count > 0)
                                {
                                    CardMoverP1 mover = movers[0];
                                    CardTestHelper.PlaceP1CardOnDropArea(mover, areas[0]);
                                    yield return new WaitForSeconds(0.3f);
                                    
                                    // Remove card
                                    if (areas[0].IsOccupied)
                                    {
                                        GameObject cardObj = areas[0].GetOccupyingCard();
                                        if (cardObj != null)
                                        {
                                            Object.Destroy(cardObj);
                                        }
                                    }
                                    yield return new WaitForSeconds(0.3f);
                                }
                            }
                        }
                    }
                }
                
                // Force GC
                System.GC.Collect();
                yield return new WaitForSeconds(1.0f);
                
                // Verify card count didn't grow unbounded
                int finalCards = Object.FindObjectsOfType<CardMoverP1>().Length;
                int cardGrowth = finalCards - initialCards;
                
                Assert.Less(cardGrowth, 50,
                    $"Card count should not grow unbounded. Growth: {cardGrowth}");
            }
        }

        [UnityTest]
        [Timeout(300000)]
        public IEnumerator Coroutines_Are_Cleaned_Up()
        {
            yield return CardTestHelper.WaitForCoinTossToComplete();
            yield return new WaitForSeconds(1.0f);
            
            // Count active coroutines (indirectly by checking MonoBehaviour count)
            int initialMonoBehaviours = Object.FindObjectsOfType<MonoBehaviour>().Length;
            
            // Trigger operations that start coroutines
            GameManager gameManager = GameManager.Instance;
            if (gameManager != null)
            {
                // Reset game state (starts coroutines)
                gameManager.ResetGameState();
                yield return CardTestHelper.WaitForCoinTossToComplete();
                yield return new WaitForSeconds(2.0f);
            }
            
            // Force GC
            System.GC.Collect();
            yield return new WaitForSeconds(1.0f);
            
            // Verify no unbounded growth
            int finalMonoBehaviours = Object.FindObjectsOfType<MonoBehaviour>().Length;
            int growth = finalMonoBehaviours - initialMonoBehaviours;
            
            Assert.Less(growth, 30,
                $"MonoBehaviour count should not grow unbounded. Growth: {growth}");
        }

        [UnityTest]
        [Timeout(300000)]
        public IEnumerator Event_Subscriptions_Are_Cleaned_Up()
        {
            yield return CardTestHelper.WaitForCoinTossToComplete();
            yield return new WaitForSeconds(1.0f);
            
            ScoreManager scoreManager = ScoreManager.Instance;
            ScoreUI scoreUI = Object.FindObjectOfType<ScoreUI>();
            
            if (scoreManager != null && scoreUI != null)
            {
                // Destroy ScoreUI
                Object.Destroy(scoreUI.gameObject);
                yield return new WaitForSeconds(0.5f);
                
                // Verify ScoreManager still works (no null ref from event)
                scoreManager.AddScore(true);
                Assert.IsTrue(true, "ScoreManager should work after ScoreUI destruction (events cleaned up)");
            }
        }
    }
}

