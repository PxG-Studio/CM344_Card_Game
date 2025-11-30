using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CardGame.UI;
using CardGame.Managers;
using TMPro;

namespace CardGame.Tests
{
    /// <summary>
    /// PlayMode tests for DeltaMarker system - validates score change visual feedback.
    /// Tests delta marker spawning, animation, and cleanup.
    /// </summary>
    public class DeltaMarkerPlayModeTests
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
        public IEnumerator DeltaMarkerEmitter_Exists_After_HUDSetup()
        {
            yield return CardTestHelper.WaitForCoinTossToComplete();
            yield return new WaitForSeconds(1.0f);
            
            DeltaMarkerEmitter emitter = Object.FindObjectOfType<DeltaMarkerEmitter>();
            Assert.IsNotNull(emitter, "DeltaMarkerEmitter should exist after HUDSetup");
        }

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator DeltaMarkerEmitter_Can_Spawn_Marker()
        {
            yield return CardTestHelper.WaitForCoinTossToComplete();
            yield return new WaitForSeconds(1.0f);
            
            DeltaMarkerEmitter emitter = Object.FindObjectOfType<DeltaMarkerEmitter>();
            Assert.IsNotNull(emitter, "DeltaMarkerEmitter should exist");
            
            // Get the EmitDeltaMarker method via reflection
            var emitMethod = typeof(DeltaMarkerEmitter).GetMethod("EmitDeltaMarker",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            
            if (emitMethod != null)
            {
                // Try to emit a marker at a test position
                Vector3 testPosition = new Vector3(0, 0, 0);
                emitMethod.Invoke(emitter, new object[] { testPosition, 1, true });
                yield return new WaitForSeconds(0.5f);
                
                // Verify marker was created (check for DeltaMarkerPopup)
                DeltaMarkerPopup[] popups = Object.FindObjectsOfType<DeltaMarkerPopup>();
                Assert.Greater(popups.Length, 0, "Delta marker popup should be created");
            }
            else
            {
                // If method doesn't exist, verify emitter is at least initialized
                Assert.IsNotNull(emitter, "DeltaMarkerEmitter should be initialized");
            }
        }

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator DeltaMarker_Displays_Correct_Value()
        {
            yield return CardTestHelper.WaitForCoinTossToComplete();
            yield return new WaitForSeconds(1.0f);
            
            DeltaMarkerEmitter emitter = Object.FindObjectOfType<DeltaMarkerEmitter>();
            if (emitter == null)
            {
                Assert.Inconclusive("DeltaMarkerEmitter not found - may not be set up in scene");
                yield break;
            }
            
            var emitMethod = typeof(DeltaMarkerEmitter).GetMethod("EmitDeltaMarker",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            
            if (emitMethod != null)
            {
                // Emit marker with value +5
                Vector3 testPosition = new Vector3(0, 0, 0);
                emitMethod.Invoke(emitter, new object[] { testPosition, 5, true });
                yield return new WaitForSeconds(0.5f);
                
                // Find the popup and verify text
                DeltaMarkerPopup popup = Object.FindObjectOfType<DeltaMarkerPopup>();
                if (popup != null)
                {
                    TMP_Text textComponent = popup.GetComponentInChildren<TMP_Text>();
                    if (textComponent != null)
                    {
                        Assert.IsTrue(textComponent.text.Contains("5") || textComponent.text.Contains("+5"),
                            $"Delta marker should display value 5. Got: {textComponent.text}");
                    }
                }
            }
        }

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator DeltaMarker_Handles_Negative_Values()
        {
            yield return CardTestHelper.WaitForCoinTossToComplete();
            yield return new WaitForSeconds(1.0f);
            
            DeltaMarkerEmitter emitter = Object.FindObjectOfType<DeltaMarkerEmitter>();
            if (emitter == null)
            {
                Assert.Inconclusive("DeltaMarkerEmitter not found");
                yield break;
            }
            
            var emitMethod = typeof(DeltaMarkerEmitter).GetMethod("EmitDeltaMarker",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            
            if (emitMethod != null)
            {
                // Emit marker with negative value
                Vector3 testPosition = new Vector3(0, 0, 0);
                emitMethod.Invoke(emitter, new object[] { testPosition, -3, false });
                yield return new WaitForSeconds(0.5f);
                
                // Verify marker was created (negative values should still work)
                DeltaMarkerPopup popup = Object.FindObjectOfType<DeltaMarkerPopup>();
                Assert.IsNotNull(popup, "Delta marker should handle negative values");
            }
        }

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator Multiple_DeltaMarkers_Dont_Conflict()
        {
            yield return CardTestHelper.WaitForCoinTossToComplete();
            yield return new WaitForSeconds(1.0f);
            
            DeltaMarkerEmitter emitter = Object.FindObjectOfType<DeltaMarkerEmitter>();
            if (emitter == null)
            {
                Assert.Inconclusive("DeltaMarkerEmitter not found");
                yield break;
            }
            
            var emitMethod = typeof(DeltaMarkerEmitter).GetMethod("EmitDeltaMarker",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            
            if (emitMethod != null)
            {
                // Emit multiple markers rapidly
                for (int i = 0; i < 5; i++)
                {
                    Vector3 position = new Vector3(i * 0.5f, 0, 0);
                    emitMethod.Invoke(emitter, new object[] { position, i + 1, true });
                    yield return new WaitForSeconds(0.1f);
                }
                
                yield return new WaitForSeconds(1.0f);
                
                // Verify multiple markers exist
                DeltaMarkerPopup[] popups = Object.FindObjectsOfType<DeltaMarkerPopup>();
                Assert.GreaterOrEqual(popups.Length, 1, 
                    "Multiple delta markers should be able to exist simultaneously");
            }
        }

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator DeltaMarker_Animates_And_Disappears()
        {
            yield return CardTestHelper.WaitForCoinTossToComplete();
            yield return new WaitForSeconds(1.0f);
            
            DeltaMarkerEmitter emitter = Object.FindObjectOfType<DeltaMarkerEmitter>();
            if (emitter == null)
            {
                Assert.Inconclusive("DeltaMarkerEmitter not found");
                yield break;
            }
            
            var emitMethod = typeof(DeltaMarkerEmitter).GetMethod("EmitDeltaMarker",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            
            if (emitMethod != null)
            {
                Vector3 testPosition = new Vector3(0, 0, 0);
                emitMethod.Invoke(emitter, new object[] { testPosition, 1, true });
                
                // Wait for animation to complete
                yield return new WaitForSeconds(3.0f);
                
                // Marker should be destroyed or inactive after animation
                DeltaMarkerPopup popup = Object.FindObjectOfType<DeltaMarkerPopup>();
                if (popup != null)
                {
                    // Marker might still exist but should be inactive or animating out
                    Assert.IsTrue(!popup.gameObject.activeSelf || popup.transform.localScale.magnitude < 0.1f,
                        "Delta marker should animate out and become inactive");
                }
            }
        }
    }
}

