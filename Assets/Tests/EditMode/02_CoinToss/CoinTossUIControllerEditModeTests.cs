using NUnit.Framework;
using UnityEngine;
using CardGame.UI;
using System.Reflection;

namespace CardGame.Tests
{
    /// <summary>
    /// EditMode tests for CoinTossUIController - validates structure and API.
    /// Tests component structure, methods, and dependency injection.
    /// </summary>
    public class CoinTossUIControllerEditModeTests
    {
        [Test]
        public void CoinTossUIController_Has_Required_Fields()
        {
            // Verify CoinTossUIController has required fields
            var rootCanvasGroupField = typeof(CoinTossUIController).GetField("rootCanvasGroup",
                BindingFlags.Public | BindingFlags.Instance);
            var rootPanelField = typeof(CoinTossUIController).GetField("rootPanel",
                BindingFlags.Public | BindingFlags.Instance);
            var coinImageField = typeof(CoinTossUIController).GetField("coinImage",
                BindingFlags.Public | BindingFlags.Instance);
            
            Assert.IsNotNull(rootCanvasGroupField, "CoinTossUIController should have rootCanvasGroup field");
            Assert.IsNotNull(rootPanelField, "CoinTossUIController should have rootPanel field");
            Assert.IsNotNull(coinImageField, "CoinTossUIController should have coinImage field");
        }

        [Test]
        public void CoinTossUIController_Has_InjectDependencies_Method()
        {
            var injectMethod = typeof(CoinTossUIController).GetMethod("InjectDependencies",
                BindingFlags.Public | BindingFlags.Instance);
            
            Assert.IsNotNull(injectMethod, "CoinTossUIController should have InjectDependencies method");
            
            // Verify method has parameters
            var parameters = injectMethod.GetParameters();
            Assert.Greater(parameters.Length, 0,
                "InjectDependencies should take parameters");
        }

        [Test]
        public void CoinTossUIController_Has_StartCoinToss_Method()
        {
            var startMethod = typeof(CoinTossUIController).GetMethod("StartCoinToss",
                BindingFlags.Public | BindingFlags.Instance);
            
            Assert.IsNotNull(startMethod, "CoinTossUIController should have StartCoinToss method");
            
            // Verify method signature (no parameters)
            var parameters = startMethod.GetParameters();
            Assert.AreEqual(0, parameters.Length, "StartCoinToss should take no parameters");
        }

        [Test]
        public void CoinTossUIController_Can_Be_Created()
        {
            GameObject go = new GameObject("TestCoinTossUIController");
            CoinTossUIController controller = go.AddComponent<CoinTossUIController>();
            
            Assert.IsNotNull(controller, "CoinTossUIController component should be creatable");
            
            Object.DestroyImmediate(go);
        }
    }
}

