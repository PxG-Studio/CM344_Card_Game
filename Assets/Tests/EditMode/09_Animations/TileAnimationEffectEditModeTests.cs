using NUnit.Framework;
using UnityEngine;
using CardGame.UI;
using System.Reflection;

namespace CardGame.Tests
{
    /// <summary>
    /// EditMode tests for TileAnimationEffect - validates structure and API.
    /// Tests component structure, methods, and animation settings.
    /// </summary>
    public class TileAnimationEffectEditModeTests
    {
        [Test]
        public void TileAnimationEffect_Has_Required_Fields()
        {
            // Verify TileAnimationEffect has required fields
            var effectRendererField = typeof(TileAnimationEffect).GetField("effectRenderer",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var secondaryEffectRendererField = typeof(TileAnimationEffect).GetField("secondaryEffectRenderer",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var animationSpeedField = typeof(TileAnimationEffect).GetField("animationSpeed",
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            Assert.IsNotNull(effectRendererField, "TileAnimationEffect should have effectRenderer field");
            Assert.IsNotNull(secondaryEffectRendererField, "TileAnimationEffect should have secondaryEffectRenderer field");
            Assert.IsNotNull(animationSpeedField, "TileAnimationEffect should have animationSpeed field");
        }

        [Test]
        public void TileAnimationEffect_Has_ActivateEffect_Method()
        {
            var activateMethod = typeof(TileAnimationEffect).GetMethod("ActivateEffect",
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new System.Type[] { typeof(Color) },
                null);
            
            Assert.IsNotNull(activateMethod, "TileAnimationEffect should have ActivateEffect(Color) method");
            Assert.AreEqual(typeof(void), activateMethod.ReturnType,
                "ActivateEffect should return void");
        }

        [Test]
        public void TileAnimationEffect_Has_DeactivateEffect_Method()
        {
            var deactivateMethod = typeof(TileAnimationEffect).GetMethod("DeactivateEffect",
                BindingFlags.Public | BindingFlags.Instance);
            
            Assert.IsNotNull(deactivateMethod, "TileAnimationEffect should have DeactivateEffect method");
            
            // Verify method signature (no parameters)
            var parameters = deactivateMethod.GetParameters();
            Assert.AreEqual(0, parameters.Length, "DeactivateEffect should take no parameters");
            Assert.AreEqual(typeof(void), deactivateMethod.ReturnType,
                "DeactivateEffect should return void");
        }

        [Test]
        public void TileAnimationEffect_Can_Be_Created()
        {
            GameObject go = new GameObject("TestTileAnimation");
            TileAnimationEffect effect = go.AddComponent<TileAnimationEffect>();
            
            Assert.IsNotNull(effect, "TileAnimationEffect component should be creatable");
            
            Object.DestroyImmediate(go);
        }

        [Test]
        public void TileAnimationEffect_Methods_Can_Be_Called()
        {
            GameObject go = new GameObject("TestTileAnimation");
            TileAnimationEffect effect = go.AddComponent<TileAnimationEffect>();
            
            // Verify methods can be called
            try
            {
                effect.ActivateEffect(Color.red);
                effect.DeactivateEffect();
                Assert.IsTrue(true, "TileAnimationEffect methods should be callable");
            }
            catch (System.Exception ex)
            {
                Assert.Fail($"TileAnimationEffect methods should not throw. Error: {ex.Message}");
            }
            
            Object.DestroyImmediate(go);
        }
    }
}

