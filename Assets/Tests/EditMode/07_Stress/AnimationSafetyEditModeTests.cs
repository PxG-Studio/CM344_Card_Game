using NUnit.Framework;
using UnityEngine;
using CardGame.UI;

namespace CardGame.Tests
{
    /// <summary>
    /// EditMode tests for animation safety - validates animation component structure.
    /// These tests ensure animation components have proper cleanup and state management.
    /// </summary>
    public class AnimationSafetyEditModeTests
    {
        [Test]
        public void CardFlipAnimation_Has_State_Properties()
        {
            // Verify CardFlipAnimation has state tracking properties
            var isFlippedProperty = typeof(CardFlipAnimation).GetProperty("isFlipped");
            var isAnimatingProperty = typeof(CardFlipAnimation).GetProperty("isAnimating");
            
            Assert.IsNotNull(isFlippedProperty, "CardFlipAnimation should have isFlipped property");
            Assert.IsNotNull(isAnimatingProperty, "CardFlipAnimation should have isAnimating property");
            Assert.AreEqual(typeof(bool), isFlippedProperty.PropertyType,
                "isFlipped should be bool");
            Assert.AreEqual(typeof(bool), isAnimatingProperty.PropertyType,
                "isAnimating should be bool");
        }

        [Test]
        public void CardFlipAnimation_Has_Setup_Validation()
        {
            // Verify CardFlipAnimation has setup validation
            var isSetupValidMethod = typeof(CardFlipAnimation).GetMethod("IsSetupValid",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(isSetupValidMethod, "CardFlipAnimation should have IsSetupValid method");
            Assert.AreEqual(typeof(bool), isSetupValidMethod.ReturnType,
                "IsSetupValid should return bool");
        }

        [Test]
        public void CardFlipAnimation_Has_Container_Setup()
        {
            // Verify CardFlipAnimation has container setup methods
            var setContainersMethod = typeof(CardFlipAnimation).GetMethod("SetContainers",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(setContainersMethod, "CardFlipAnimation should have SetContainers method");
        }

        [Test]
        public void CardFlipAnimation_Has_Cleanup_Methods()
        {
            // Verify CardFlipAnimation can stop animations
            // StopCoroutine is inherited from MonoBehaviour and has multiple overloads
            // Check for the Coroutine parameter version (most commonly used)
            var stopCoroutineMethod = typeof(CardFlipAnimation).GetMethod("StopCoroutine",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance,
                null,
                new System.Type[] { typeof(Coroutine) },
                null);
            // StopCoroutine is inherited from MonoBehaviour, so we check if it exists
            Assert.IsNotNull(stopCoroutineMethod, "CardFlipAnimation should inherit StopCoroutine(Coroutine) from MonoBehaviour");
            
            // Also verify OnDestroy exists for cleanup
            var onDestroyMethod = typeof(CardFlipAnimation).GetMethod("OnDestroy",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(onDestroyMethod, "CardFlipAnimation should have OnDestroy method for cleanup");
        }

        [Test]
        public void NewCardUI_Has_Flip_Animation_Reference()
        {
            // Verify NewCardUI has flip animation reference
            var flipAnimationField = typeof(NewCardUI).GetField("flipAnimation",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(flipAnimationField, "NewCardUI should have flipAnimation field");
            Assert.AreEqual(typeof(CardFlipAnimation), flipAnimationField.FieldType,
                "flipAnimation should be of type CardFlipAnimation");
        }

        [Test]
        public void NewCardUI_Has_Container_Setup()
        {
            // Verify NewCardUI has container setup
            var frontContainerField = typeof(NewCardUI).GetField("frontContainer",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var backContainerField = typeof(NewCardUI).GetField("backContainer",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            Assert.IsNotNull(frontContainerField, "NewCardUI should have frontContainer field");
            Assert.IsNotNull(backContainerField, "NewCardUI should have backContainer field");
        }

        [Test]
        public void CardFlipAnimation_Has_Capture_Methods()
        {
            // Verify CardFlipAnimation has capture methods
            // There are two overloads: CaptureCard(Color) and CaptureCard(Color, FlipDirection)
            // Check for the single-parameter version
            var captureCardMethod = typeof(CardFlipAnimation).GetMethod("CaptureCard",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance,
                null,
                new System.Type[] { typeof(Color) },
                null);
            Assert.IsNotNull(captureCardMethod, "CardFlipAnimation should have CaptureCard(Color) method");
            
            // Check for the two-parameter version
            var captureCardWithDirectionMethod = typeof(CardFlipAnimation).GetMethod("CaptureCard",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance,
                null,
                new System.Type[] { typeof(Color), typeof(CardGame.UI.FlipDirection) },
                null);
            Assert.IsNotNull(captureCardWithDirectionMethod, "CardFlipAnimation should have CaptureCard(Color, FlipDirection) method");
        }

        [Test]
        public void CardFlipAnimation_Has_State_Management()
        {
            // Verify CardFlipAnimation has state management methods
            // There are two overloads: SetFlippedState(bool, bool) and SetFlippedState(bool, bool, Color?)
            // Check for the two-parameter version
            var setFlippedStateMethod = typeof(CardFlipAnimation).GetMethod("SetFlippedState",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance,
                null,
                new System.Type[] { typeof(bool), typeof(bool) },
                null);
            Assert.IsNotNull(setFlippedStateMethod, "CardFlipAnimation should have SetFlippedState(bool, bool) method");
            
            // Check for the three-parameter version
            var setFlippedStateWithColorMethod = typeof(CardFlipAnimation).GetMethod("SetFlippedState",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance,
                null,
                new System.Type[] { typeof(bool), typeof(bool), typeof(Color?) },
                null);
            Assert.IsNotNull(setFlippedStateWithColorMethod, "CardFlipAnimation should have SetFlippedState(bool, bool, Color?) method");
        }
    }
}

