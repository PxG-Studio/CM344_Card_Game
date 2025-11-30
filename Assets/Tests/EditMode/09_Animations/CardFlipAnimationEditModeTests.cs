using NUnit.Framework;
using UnityEngine;
using CardGame.UI;
using System.Reflection;

namespace CardGame.Tests
{
    /// <summary>
    /// EditMode tests for CardFlipAnimation - validates structure and API.
    /// Tests component structure, methods, properties, and flip directions.
    /// </summary>
    public class CardFlipAnimationEditModeTests
    {
        [Test]
        public void CardFlipAnimation_Has_Required_Properties()
        {
            // Verify CardFlipAnimation has required properties
            var isFlippedProperty = typeof(CardFlipAnimation).GetProperty("isFlipped");
            var isAnimatingProperty = typeof(CardFlipAnimation).GetProperty("isAnimating");
            
            Assert.IsNotNull(isFlippedProperty, "CardFlipAnimation should have isFlipped property");
            Assert.IsNotNull(isAnimatingProperty, "CardFlipAnimation should have isAnimating property");
            
            // Verify property types
            Assert.AreEqual(typeof(bool), isFlippedProperty.PropertyType,
                "isFlipped should return bool");
            Assert.AreEqual(typeof(bool), isAnimatingProperty.PropertyType,
                "isAnimating should return bool");
        }

        [Test]
        public void CardFlipAnimation_Has_FlipCard_Method()
        {
            // Check for FlipCard method (may have overloads)
            var flipMethod = typeof(CardFlipAnimation).GetMethod("FlipCard",
                BindingFlags.Public | BindingFlags.Instance);
            
            Assert.IsNotNull(flipMethod, "CardFlipAnimation should have FlipCard method");
        }

        [Test]
        public void CardFlipAnimation_Has_StopFlipAnimation_Method()
        {
            var stopMethod = typeof(CardFlipAnimation).GetMethod("StopFlipAnimation",
                BindingFlags.Public | BindingFlags.Instance);
            
            Assert.IsNotNull(stopMethod, "CardFlipAnimation should have StopFlipAnimation method");
        }

        [Test]
        public void CardFlipAnimation_Has_FlipDirection_Enum()
        {
            // Verify FlipDirection enum exists
            var flipDirectionType = typeof(FlipDirection);
            Assert.IsNotNull(flipDirectionType, "FlipDirection enum should exist");
            Assert.IsTrue(flipDirectionType.IsEnum, "FlipDirection should be an enum");
            
            // Verify enum values
            var values = System.Enum.GetValues(typeof(FlipDirection));
            Assert.Greater(values.Length, 0, "FlipDirection should have values");
        }

        [Test]
        public void CardFlipAnimation_Has_WasCaptured_Property()
        {
            var wasCapturedProperty = typeof(CardFlipAnimation).GetProperty("WasCaptured");
            var lastCaptureColorProperty = typeof(CardFlipAnimation).GetProperty("LastCaptureColor");
            
            Assert.IsNotNull(wasCapturedProperty, "CardFlipAnimation should have WasCaptured property");
            Assert.IsNotNull(lastCaptureColorProperty, "CardFlipAnimation should have LastCaptureColor property");
            
            // Verify property types
            Assert.AreEqual(typeof(bool), wasCapturedProperty.PropertyType,
                "WasCaptured should return bool");
            Assert.AreEqual(typeof(Color), lastCaptureColorProperty.PropertyType,
                "LastCaptureColor should return Color");
        }

        [Test]
        public void CardFlipAnimation_Can_Be_Created()
        {
            GameObject go = new GameObject("TestCardFlip");
            CardFlipAnimation flipAnim = go.AddComponent<CardFlipAnimation>();
            
            Assert.IsNotNull(flipAnim, "CardFlipAnimation component should be creatable");
            
            Object.DestroyImmediate(go);
        }

        [Test]
        public void CardFlipAnimation_Properties_Are_Accessible()
        {
            GameObject go = new GameObject("TestCardFlip");
            CardFlipAnimation flipAnim = go.AddComponent<CardFlipAnimation>();
            
            // Verify properties can be accessed
            bool isFlipped = flipAnim.isFlipped;
            bool isAnimating = flipAnim.isAnimating;
            bool wasCaptured = flipAnim.WasCaptured;
            Color captureColor = flipAnim.LastCaptureColor;
            
            // Properties should be accessible (values may be default)
            Assert.IsInstanceOf<bool>(isFlipped, "isFlipped should be accessible");
            Assert.IsInstanceOf<bool>(isAnimating, "isAnimating should be accessible");
            Assert.IsInstanceOf<bool>(wasCaptured, "WasCaptured should be accessible");
            Assert.IsInstanceOf<Color>(captureColor, "LastCaptureColor should be accessible");
            
            Object.DestroyImmediate(go);
        }
    }
}

