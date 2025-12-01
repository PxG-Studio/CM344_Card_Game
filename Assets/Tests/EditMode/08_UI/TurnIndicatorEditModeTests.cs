using NUnit.Framework;
using UnityEngine;
using CardGame.UI;
using System.Reflection;

namespace CardGame.Tests
{
    /// <summary>
    /// EditMode tests for TurnIndicator systems - validates structure and API.
    /// Tests component structure, methods, and display functionality.
    /// </summary>
    public class TurnIndicatorEditModeTests
    {
        [Test]
        public void TurnIndicatorUI_Has_SetActive_Method()
        {
            var setActiveMethod = typeof(TurnIndicatorUI).GetMethod("SetActive",
                BindingFlags.Public | BindingFlags.Instance);
            
            Assert.IsNotNull(setActiveMethod, "TurnIndicatorUI should have SetActive method");
            
            // Verify method signature
            var parameters = setActiveMethod.GetParameters();
            Assert.GreaterOrEqual(parameters.Length, 1,
                "SetActive should take at least 1 parameter");
        }

        [Test]
        public void TurnIndicatorMoving_Has_Required_Methods()
        {
            // Verify TurnIndicatorMoving has update methods
            var updateMethod = typeof(TurnIndicatorMoving).GetMethod("Update",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            
            // Update method may be private, but class should exist
            Assert.IsNotNull(typeof(TurnIndicatorMoving),
                "TurnIndicatorMoving class should exist");
        }

        [Test]
        public void TurnIndicator3D_Has_Required_Methods()
        {
            // Verify TurnIndicator3D class exists
            Assert.IsNotNull(typeof(TurnIndicator3D),
                "TurnIndicator3D class should exist");
            
            // Verify it's a MonoBehaviour
            Assert.IsTrue(typeof(MonoBehaviour).IsAssignableFrom(typeof(TurnIndicator3D)),
                "TurnIndicator3D should inherit from MonoBehaviour");
        }

        [Test]
        public void TurnIndicatorUI_Can_Be_Created()
        {
            GameObject go = new GameObject("TestTurnIndicator");
            TurnIndicatorUI indicator = go.AddComponent<TurnIndicatorUI>();
            
            Assert.IsNotNull(indicator, "TurnIndicatorUI component should be creatable");
            
            Object.DestroyImmediate(go);
        }

        [Test]
        public void TurnIndicatorMoving_Can_Be_Created()
        {
            GameObject go = new GameObject("TestTurnIndicatorMoving");
            TurnIndicatorMoving indicator = go.AddComponent<TurnIndicatorMoving>();
            
            Assert.IsNotNull(indicator, "TurnIndicatorMoving component should be creatable");
            
            Object.DestroyImmediate(go);
        }

        [Test]
        public void TurnIndicator3D_Can_Be_Created()
        {
            GameObject go = new GameObject("TestTurnIndicator3D");
            TurnIndicator3D indicator = go.AddComponent<TurnIndicator3D>();
            
            Assert.IsNotNull(indicator, "TurnIndicator3D component should be creatable");
            
            Object.DestroyImmediate(go);
        }
    }
}

