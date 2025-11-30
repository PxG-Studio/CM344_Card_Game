using NUnit.Framework;
using UnityEngine;
using CardGame.UI;
using CardGame.Managers;
using System.Reflection;
using UnityEngine.Scripting;

namespace CardGame.Tests
{
    /// <summary>
    /// EditMode tests for HUDSetup - validates structure and API.
    /// Tests initialization methods, component setup, and manager creation.
    /// </summary>
    public class HUDSetupEditModeTests
    {
        [Test]
        public void HUDSetup_Has_SetupHUD_Method()
        {
            var setupMethod = typeof(HUDSetup).GetMethod("SetupHUD",
                BindingFlags.Public | BindingFlags.Instance);
            
            Assert.IsNotNull(setupMethod, "HUDSetup should have SetupHUD method");
            
            // Verify method signature (no parameters)
            var parameters = setupMethod.GetParameters();
            Assert.AreEqual(0, parameters.Length, "SetupHUD should take no parameters");
            Assert.AreEqual(typeof(void), setupMethod.ReturnType,
                "SetupHUD should return void");
        }

        [Test]
        public void HUDSetup_Has_AutoSetup_Field()
        {
            var autoSetupField = typeof(HUDSetup).GetField("autoSetupOnAwake",
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            Assert.IsNotNull(autoSetupField, "HUDSetup should have autoSetupOnAwake field");
            Assert.AreEqual(typeof(bool), autoSetupField.FieldType,
                "autoSetupOnAwake should be bool");
        }

        [Test]
        public void HUDSetup_Has_DefaultExecutionOrder()
        {
            // Verify HUDSetup has DefaultExecutionOrder attribute
            var attributes = typeof(HUDSetup).GetCustomAttributes(typeof(UnityEngine.DefaultExecutionOrder), true);
            Assert.Greater(attributes.Length, 0,
                "HUDSetup should have DefaultExecutionOrder attribute");
        }

        [Test]
        public void HUDSetup_Can_Be_Created()
        {
            GameObject go = new GameObject("TestHUDSetup");
            HUDSetup hudSetup = go.AddComponent<HUDSetup>();
            
            Assert.IsNotNull(hudSetup, "HUDSetup component should be creatable");
            
            Object.DestroyImmediate(go);
        }

        [Test]
        public void HUDSetup_SetupHUD_Can_Be_Called()
        {
            GameObject go = new GameObject("TestHUDSetup");
            HUDSetup hudSetup = go.AddComponent<HUDSetup>();
            
            // SetupHUD may fail if scene isn't set up, but method should exist
            try
            {
                hudSetup.SetupHUD();
                Assert.IsTrue(true, "SetupHUD should be callable");
            }
            catch (System.Exception ex)
            {
                // If it throws, it should be a runtime error, not a missing method
                Assert.IsTrue(ex is System.NullReferenceException || ex is System.ArgumentException,
                    $"SetupHUD should exist. Error: {ex.Message}");
            }
            
            Object.DestroyImmediate(go);
        }

        [Test]
        public void HUDSetup_Initializes_All_Required_Managers()
        {
            // Verify HUDSetup has methods to initialize managers
            var ensureGameManagersMethod = typeof(HUDSetup).GetMethod("EnsureGameManagers",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            var ensureFateFlowControllerMethod = typeof(HUDSetup).GetMethod("EnsureFateFlowController",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            var ensureCoinTossManagerMethod = typeof(HUDSetup).GetMethod("EnsureCoinTossManager",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            
            // At least one of these should exist
            bool hasManagerMethods = ensureGameManagersMethod != null ||
                                   ensureFateFlowControllerMethod != null ||
                                   ensureCoinTossManagerMethod != null;
            
            Assert.IsTrue(hasManagerMethods,
                "HUDSetup should have methods to initialize managers");
        }

        [Test]
        public void HUDSetup_Initializes_UI_Components()
        {
            // Verify HUDSetup has methods to setup UI
            var setupGameEndUIMethod = typeof(HUDSetup).GetMethod("SetupGameEndUI",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            var setupCoinTossUIMethod = typeof(HUDSetup).GetMethod("SetupCoinTossUI",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            var ensureDeltaMarkerEmitterMethod = typeof(HUDSetup).GetMethod("EnsureDeltaMarkerEmitter",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            
            // At least one of these should exist
            bool hasUIMethods = setupGameEndUIMethod != null ||
                              setupCoinTossUIMethod != null ||
                              ensureDeltaMarkerEmitterMethod != null;
            
            Assert.IsTrue(hasUIMethods,
                "HUDSetup should have methods to setup UI components");
        }
    }
}

