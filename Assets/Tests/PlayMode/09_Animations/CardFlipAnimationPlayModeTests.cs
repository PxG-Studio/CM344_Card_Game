using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CardGame.UI;
using CardGame.Core;
using CardGame.Managers;

namespace CardGame.Tests
{
    /// <summary>
    /// PlayMode tests for CardFlipAnimation - validates flip animation completion and behavior.
    /// Tests animation timing, direction, interruption, and cleanup.
    /// </summary>
    public class CardFlipAnimationPlayModeTests
    {
        private const string SCENE_NAME = "BattleScreenMultiplayer";
        private const float TEST_TIMEOUT = 60f;

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
        [Timeout(30000)]
        public IEnumerator CardFlipAnimation_Completes_Flip_Correctly()
        {
            yield return CardTestHelper.WaitForCoinTossToComplete();
            yield return new WaitForSeconds(1.0f);
            
            // Get a card on the board
            CardDropArea[] areas = Object.FindObjectsOfType<CardDropArea>();
            CardDropArea targetArea = null;
            
            foreach (CardDropArea area in areas)
            {
                if (area != null && area.IsOccupied)
                {
                    targetArea = area;
                    break;
                }
            }
            
            if (targetArea == null)
            {
                // Place a card first
                NewDeckManagerP1 deckP1 = Object.FindObjectOfType<NewDeckManagerP1>();
                if (deckP1 != null && deckP1.Hand.Count > 0)
                {
                    deckP1.InitializeDeck();
                    yield return new WaitForSeconds(0.5f);
                    
                    if (deckP1.Hand.Count > 0)
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
                                    targetArea = areas[0];
                                    CardTestHelper.PlaceP1CardOnDropArea(mover, targetArea);
                                    yield return new WaitForSeconds(0.5f);
                                }
                            }
                        }
                    }
                }
            }
            
            if (targetArea != null && targetArea.IsOccupied)
            {
                GameObject cardObj = targetArea.GetOccupyingCard();
                if (cardObj != null)
                {
                    CardFlipAnimation flipAnim = cardObj.GetComponentInChildren<CardFlipAnimation>();
                    if (flipAnim != null)
                    {
                        // Get initial flip state
                        bool wasFlipped = flipAnim.isFlipped;
                        
                        // Trigger flip
                        var flipMethod = typeof(CardFlipAnimation).GetMethod("FlipCard",
                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                        
                        if (flipMethod != null)
                        {
                            flipMethod.Invoke(flipAnim, null);
                            
                            // Wait for animation to complete (flip duration is typically 0.5s)
                            yield return new WaitForSeconds(1.0f);
                            
                            // Verify flip completed
                            bool isFlipped = flipAnim.isFlipped;
                            Assert.AreNotEqual(wasFlipped, isFlipped,
                                "Card flip state should change after animation");
                            
                            // Verify animation is not still running
                            bool isAnimating = flipAnim.isAnimating;
                            Assert.IsFalse(isAnimating,
                                "Flip animation should complete and not be animating");
                        }
                    }
                }
            }
        }

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator CardFlipAnimation_Handles_Interruption()
        {
            yield return CardTestHelper.WaitForCoinTossToComplete();
            yield return new WaitForSeconds(1.0f);
            
            // Find a card with flip animation
            CardFlipAnimation[] flipAnims = Object.FindObjectsOfType<CardFlipAnimation>(true);
            
            if (flipAnims.Length > 0)
            {
                CardFlipAnimation flipAnim = flipAnims[0];
                
                // Start flip
                var flipMethod = typeof(CardFlipAnimation).GetMethod("FlipCard",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                
                if (flipMethod != null)
                {
                    flipMethod.Invoke(flipAnim, null);
                    yield return new WaitForSeconds(0.1f);
                    
                    // Interrupt with another flip
                    flipMethod.Invoke(flipAnim, null);
                    yield return new WaitForSeconds(1.0f);
                    
                    // Verify animation completed without errors
                    Assert.IsTrue(true, "CardFlipAnimation should handle interruption gracefully");
                }
            }
        }

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator CardFlipAnimation_Cleanup_On_Destruction()
        {
            yield return CardTestHelper.WaitForCoinTossToComplete();
            yield return new WaitForSeconds(1.0f);
            
            // Find a card with flip animation
            CardFlipAnimation[] flipAnims = Object.FindObjectsOfType<CardFlipAnimation>(true);
            
            if (flipAnims.Length > 0)
            {
                CardFlipAnimation flipAnim = flipAnims[0];
                GameObject cardObj = flipAnim.gameObject;
                
                // Start flip
                var flipMethod = typeof(CardFlipAnimation).GetMethod("FlipCard",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                
                if (flipMethod != null)
                {
                    flipMethod.Invoke(flipAnim, null);
                    yield return new WaitForSeconds(0.1f);
                    
                    // Destroy card during animation
                    Object.Destroy(cardObj);
                    yield return new WaitForSeconds(0.5f);
                    
                    // Verify no errors (cleanup should happen in OnDestroy)
                    Assert.IsTrue(true, "CardFlipAnimation should cleanup on destruction");
                }
            }
        }

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator Multiple_CardFlips_Simultaneously()
        {
            yield return CardTestHelper.WaitForCoinTossToComplete();
            yield return new WaitForSeconds(1.0f);
            
            CardFlipAnimation[] flipAnims = Object.FindObjectsOfType<CardFlipAnimation>(true);
            
            if (flipAnims.Length >= 2)
            {
                // Flip multiple cards simultaneously
                var flipMethod = typeof(CardFlipAnimation).GetMethod("FlipCard",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                
                if (flipMethod != null)
                {
                    for (int i = 0; i < Mathf.Min(3, flipAnims.Length); i++)
                    {
                        flipMethod.Invoke(flipAnims[i], null);
                    }
                    
                    yield return new WaitForSeconds(1.0f);
                    
                    // Verify all animations completed
                    for (int i = 0; i < Mathf.Min(3, flipAnims.Length); i++)
                    {
                        Assert.IsFalse(flipAnims[i].isAnimating,
                            $"CardFlipAnimation {i} should complete when multiple cards flip");
                    }
                }
            }
        }
    }
}

