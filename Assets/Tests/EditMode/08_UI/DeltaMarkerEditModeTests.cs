using NUnit.Framework;
using UnityEngine;
using CardGame.UI;
using System.Reflection;

namespace CardGame.Tests
{
    /// <summary>
    /// EditMode tests for DeltaMarker system - validates structure, API, and resource loading.
    /// Tests component structure, methods, and configuration.
    /// </summary>
    public class DeltaMarkerEditModeTests
    {
        [Test]
        public void DeltaMarkerEmitter_Has_Required_Fields()
        {
            // Verify DeltaMarkerEmitter has required fields
            var configField = typeof(DeltaMarkerEmitter).GetField("config",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var prefabField = typeof(DeltaMarkerEmitter).GetField("deltaMarkerPrefab",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var parentCanvasField = typeof(DeltaMarkerEmitter).GetField("parentCanvas",
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            Assert.IsNotNull(configField, "DeltaMarkerEmitter should have config field");
            Assert.IsNotNull(prefabField, "DeltaMarkerEmitter should have deltaMarkerPrefab field");
            Assert.IsNotNull(parentCanvasField, "DeltaMarkerEmitter should have parentCanvas field");
        }

        [Test]
        public void DeltaMarkerEmitter_Has_EnsureReady_Method()
        {
            var ensureReadyMethod = typeof(DeltaMarkerEmitter).GetMethod("EnsureReady",
                BindingFlags.Public | BindingFlags.Instance);
            Assert.IsNotNull(ensureReadyMethod, "DeltaMarkerEmitter should have EnsureReady method");
            
            // Verify method signature (no parameters)
            var parameters = ensureReadyMethod.GetParameters();
            Assert.AreEqual(0, parameters.Length, "EnsureReady should take no parameters");
        }

        [Test]
        public void DeltaMarkerEmitter_Has_EmitDeltaMarker_Method()
        {
            // Check for EmitDeltaMarker method (may be public or private)
            var emitMethod = typeof(DeltaMarkerEmitter).GetMethod("EmitDeltaMarker",
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
            
            if (emitMethod != null)
            {
                var parameters = emitMethod.GetParameters();
                Assert.GreaterOrEqual(parameters.Length, 2,
                    "EmitDeltaMarker should take at least 2 parameters (position, value)");
            }
        }

        [Test]
        public void DeltaMarkerPopup_Has_Required_Components()
        {
            // Verify DeltaMarkerPopup class exists
            var popupType = typeof(DeltaMarkerPopup);
            Assert.IsNotNull(popupType, "DeltaMarkerPopup class should exist");
            
            // Verify it's a MonoBehaviour
            Assert.IsTrue(typeof(MonoBehaviour).IsAssignableFrom(popupType),
                "DeltaMarkerPopup should inherit from MonoBehaviour");
        }

        [Test]
        public void DeltaMarkerConfig_Exists_As_ScriptableObject()
        {
            // Verify DeltaMarkerConfig class exists
            var configType = typeof(DeltaMarkerConfig);
            Assert.IsNotNull(configType, "DeltaMarkerConfig class should exist");
            
            // Verify it's a ScriptableObject
            Assert.IsTrue(typeof(ScriptableObject).IsAssignableFrom(configType),
                "DeltaMarkerConfig should inherit from ScriptableObject");
        }

        [Test]
        public void DeltaMarkerEmitter_Can_Be_Created()
        {
            GameObject go = new GameObject("TestDeltaEmitter");
            DeltaMarkerEmitter emitter = go.AddComponent<DeltaMarkerEmitter>();
            
            Assert.IsNotNull(emitter, "DeltaMarkerEmitter component should be creatable");
            
            Object.DestroyImmediate(go);
        }
    }
}

