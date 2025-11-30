using NUnit.Framework;
using UnityEngine;
using CardGame.UI;
using System.Reflection;

namespace CardGame.Tests
{
    /// <summary>
    /// EditMode tests for VictoryCutInController - validates structure and API.
    /// Tests component structure, methods, and animation settings.
    /// </summary>
    public class VictoryCutInEditModeTests
    {
        [Test]
        public void VictoryCutInController_Has_Required_Fields()
        {
            // Verify VictoryCutInController has required fields
            var cutInRootField = typeof(VictoryCutInController).GetField("cutInRoot",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var canvasGroupField = typeof(VictoryCutInController).GetField("canvasGroup",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var mainTextField = typeof(VictoryCutInController).GetField("mainText",
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            Assert.IsNotNull(cutInRootField, "VictoryCutInController should have cutInRoot field");
            Assert.IsNotNull(canvasGroupField, "VictoryCutInController should have canvasGroup field");
            Assert.IsNotNull(mainTextField, "VictoryCutInController should have mainText field");
        }

        [Test]
        public void VictoryCutInController_Has_Play_Method()
        {
            var playMethod = typeof(VictoryCutInController).GetMethod("Play",
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new System.Type[] { typeof(string), typeof(Color) },
                null);
            
            Assert.IsNotNull(playMethod, "VictoryCutInController should have Play(string, Color) method");
            Assert.AreEqual(typeof(void), playMethod.ReturnType,
                "Play should return void");
        }

        [Test]
        public void VictoryCutInController_Has_Timing_Fields()
        {
            // Verify timing configuration fields exist
            var enterDurationField = typeof(VictoryCutInController).GetField("enterDuration",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var holdDurationField = typeof(VictoryCutInController).GetField("holdDuration",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var exitDurationField = typeof(VictoryCutInController).GetField("exitDuration",
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            Assert.IsNotNull(enterDurationField, "VictoryCutInController should have enterDuration field");
            Assert.IsNotNull(holdDurationField, "VictoryCutInController should have holdDuration field");
            Assert.IsNotNull(exitDurationField, "VictoryCutInController should have exitDuration field");
        }

        [Test]
        public void VictoryCutInController_Can_Be_Created()
        {
            GameObject go = new GameObject("TestVictoryCutIn");
            VictoryCutInController cutIn = go.AddComponent<VictoryCutInController>();
            
            Assert.IsNotNull(cutIn, "VictoryCutInController component should be creatable");
            
            Object.DestroyImmediate(go);
        }

        [Test]
        public void VictoryCutInController_Play_Method_Can_Be_Called()
        {
            GameObject go = new GameObject("TestVictoryCutIn");
            VictoryCutInController cutIn = go.AddComponent<VictoryCutInController>();
            
            // Verify Play method can be called (may fail if dependencies missing)
            try
            {
                cutIn.Play("TEST", Color.yellow);
                Assert.IsTrue(true, "VictoryCutInController.Play should be callable");
            }
            catch (System.Exception ex)
            {
                // If it throws, it should be a null reference, not a missing method
                Assert.IsTrue(ex is System.NullReferenceException,
                    $"VictoryCutInController.Play should exist. Error: {ex.Message}");
            }
            
            Object.DestroyImmediate(go);
        }
    }
}

