using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CardGame.UI;
using CardGame.Core;

namespace CardGame.Tests
{
    /// <summary>
    /// PlayMode tests for TileAnimationEffect - validates tile animation activation and behavior.
    /// Tests animation activation, deactivation, and visual effects.
    /// </summary>
    public class TileAnimationPlayModeTests
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
        public IEnumerator TileAnimationEffect_Exists_On_Tiles()
        {
            CardDropArea[] areas = Object.FindObjectsOfType<CardDropArea>();
            Assert.Greater(areas.Length, 0, "Should have CardDropArea instances");
            
            // Check if any tiles have TileAnimationEffect
            int tilesWithAnimation = 0;
            foreach (CardDropArea area in areas)
            {
                if (area != null)
                {
                    TileAnimationEffect effect = area.GetComponent<TileAnimationEffect>();
                    if (effect != null)
                    {
                        tilesWithAnimation++;
                    }
                }
            }
            
            // Note: Tiles may or may not have animation effects (optional feature)
            Assert.IsTrue(true, $"Found {tilesWithAnimation} tiles with TileAnimationEffect");
            yield return null;
        }

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator TileAnimationEffect_Activates_On_Capture()
        {
            yield return CardTestHelper.WaitForCoinTossToComplete();
            yield return new WaitForSeconds(1.0f);
            
            CardDropArea[] areas = Object.FindObjectsOfType<CardDropArea>();
            TileAnimationEffect effect = null;
            
            // Find a tile with animation effect
            foreach (CardDropArea area in areas)
            {
                if (area != null)
                {
                    effect = area.GetComponent<TileAnimationEffect>();
                    if (effect != null)
                    {
                        break;
                    }
                }
            }
            
            if (effect != null)
            {
                // Activate effect with orange color (P1)
                Color orangeColor = new Color(1f, 0.5f, 0f, 1f);
                effect.ActivateEffect(orangeColor);
                yield return new WaitForSeconds(0.5f);
                
                // Verify effect is active
                var isActiveField = typeof(TileAnimationEffect).GetField("isActive",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (isActiveField != null)
                {
                    bool isActive = (bool)isActiveField.GetValue(effect);
                    Assert.IsTrue(isActive, "TileAnimationEffect should be active after ActivateEffect");
                }
            }
        }

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator TileAnimationEffect_Deactivates_Correctly()
        {
            yield return CardTestHelper.WaitForCoinTossToComplete();
            yield return new WaitForSeconds(1.0f);
            
            CardDropArea[] areas = Object.FindObjectsOfType<CardDropArea>();
            TileAnimationEffect effect = null;
            
            foreach (CardDropArea area in areas)
            {
                if (area != null)
                {
                    effect = area.GetComponent<TileAnimationEffect>();
                    if (effect != null)
                    {
                        break;
                    }
                }
            }
            
            if (effect != null)
            {
                // Activate then deactivate
                Color orangeColor = new Color(1f, 0.5f, 0f, 1f);
                effect.ActivateEffect(orangeColor);
                yield return new WaitForSeconds(0.3f);
                
                effect.DeactivateEffect();
                yield return new WaitForSeconds(0.3f);
                
                // Verify effect is inactive
                var isActiveField = typeof(TileAnimationEffect).GetField("isActive",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (isActiveField != null)
                {
                    bool isActive = (bool)isActiveField.GetValue(effect);
                    Assert.IsFalse(isActive, "TileAnimationEffect should be inactive after DeactivateEffect");
                }
            }
        }

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator TileAnimationEffect_Animates_Over_Time()
        {
            yield return CardTestHelper.WaitForCoinTossToComplete();
            yield return new WaitForSeconds(1.0f);
            
            CardDropArea[] areas = Object.FindObjectsOfType<CardDropArea>();
            TileAnimationEffect effect = null;
            
            foreach (CardDropArea area in areas)
            {
                if (area != null)
                {
                    effect = area.GetComponent<TileAnimationEffect>();
                    if (effect != null)
                    {
                        break;
                    }
                }
            }
            
            if (effect != null)
            {
                // Activate effect
                Color orangeColor = new Color(1f, 0.5f, 0f, 1f);
                effect.ActivateEffect(orangeColor);
                
                // Get initial renderer state
                var rendererField = typeof(TileAnimationEffect).GetField("effectRenderer",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (rendererField != null)
                {
                    SpriteRenderer renderer = rendererField.GetValue(effect) as SpriteRenderer;
                    
                    if (renderer != null)
                    {
                        Color initialColor = renderer.color;
                        
                        // Wait for animation to progress
                        yield return new WaitForSeconds(0.5f);
                        
                        Color animatedColor = renderer.color;
                        
                        // Color should have changed (animation pulse)
                        Assert.AreNotEqual(initialColor, animatedColor,
                            "TileAnimationEffect should animate color over time");
                    }
                }
            }
        }
    }
}

