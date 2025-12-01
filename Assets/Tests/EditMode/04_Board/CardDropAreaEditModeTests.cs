using NUnit.Framework;
using UnityEngine;
using CardGame.Core;
using System.Reflection;

namespace CardGame.Tests
{
    /// <summary>
    /// EditMode tests for CardDropArea - validates structure, API, and static methods.
    /// Tests properties, methods, and static statistics.
    /// </summary>
    public class CardDropAreaEditModeTests
    {
        [Test]
        public void CardDropArea_Has_GetOccupyingCard_Method()
        {
            var getOccupyingCardMethod = typeof(CardDropArea).GetMethod("GetOccupyingCard",
                BindingFlags.Public | BindingFlags.Instance);
            
            Assert.IsNotNull(getOccupyingCardMethod, "CardDropArea should have GetOccupyingCard method");
            Assert.AreEqual(typeof(GameObject), getOccupyingCardMethod.ReturnType,
                "GetOccupyingCard should return GameObject");
        }

        [Test]
        public void CardDropArea_Has_ResetForNewGame_Method()
        {
            var resetMethod = typeof(CardDropArea).GetMethod("ResetForNewGame",
                BindingFlags.Public | BindingFlags.Instance);
            
            Assert.IsNotNull(resetMethod, "CardDropArea should have ResetForNewGame method");
            
            // Verify method signature (no parameters)
            var parameters = resetMethod.GetParameters();
            Assert.AreEqual(0, parameters.Length, "ResetForNewGame should take no parameters");
        }

        [Test]
        public void CardDropArea_Has_ResetGameStatistics_Static_Method()
        {
            var resetStatsMethod = typeof(CardDropArea).GetMethod("ResetGameStatistics",
                BindingFlags.Public | BindingFlags.Static);
            
            Assert.IsNotNull(resetStatsMethod, "CardDropArea should have ResetGameStatistics static method");
            
            // Verify method signature (no parameters)
            var parameters = resetStatsMethod.GetParameters();
            Assert.AreEqual(0, parameters.Length, "ResetGameStatistics should take no parameters");
        }

        [Test]
        public void CardDropArea_Has_GetCardsPlayed_Static_Method()
        {
            var getCardsMethod = typeof(CardDropArea).GetMethod("GetCardsPlayed",
                BindingFlags.Public | BindingFlags.Static);
            
            Assert.IsNotNull(getCardsMethod, "CardDropArea should have GetCardsPlayed static method");
            Assert.AreEqual(typeof(int), getCardsMethod.ReturnType,
                "GetCardsPlayed should return int");
        }

        [Test]
        public void CardDropArea_Static_Methods_Can_Be_Called()
        {
            // Verify static methods can be called without instance
            try
            {
                int cardsPlayed = CardDropArea.GetCardsPlayed();
                CardDropArea.ResetGameStatistics();
                Assert.IsTrue(true, "CardDropArea static methods should be callable");
            }
            catch (System.Exception ex)
            {
                Assert.Fail($"CardDropArea static methods should work. Error: {ex.Message}");
            }
        }

        [Test]
        public void CardDropArea_Has_IsOccupied_Property()
        {
            var isOccupiedProperty = typeof(CardDropArea).GetProperty("IsOccupied");
            
            Assert.IsNotNull(isOccupiedProperty, "CardDropArea should have IsOccupied property");
            Assert.AreEqual(typeof(bool), isOccupiedProperty.PropertyType,
                "IsOccupied should return bool");
        }

        [Test]
        public void CardDropArea_Can_Be_Created()
        {
            GameObject go = new GameObject("TestCardDropArea");
            CardDropArea area = go.AddComponent<CardDropArea>();
            
            Assert.IsNotNull(area, "CardDropArea component should be creatable");
            
            // Verify properties are accessible
            bool isOccupied = area.IsOccupied;
            GameObject occupyingCard = area.GetOccupyingCard();
            
            Assert.IsInstanceOf<bool>(isOccupied, "IsOccupied should be accessible");
            // occupyingCard may be null, which is fine
            
            Object.DestroyImmediate(go);
        }
    }
}

