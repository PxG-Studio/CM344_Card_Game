using NUnit.Framework;
using UnityEngine;
using CardGame.Core;

namespace CardGame.Tests
{
    /// <summary>
    /// EditMode tests for board integrity structure and API validation.
    /// </summary>
    public class BoardIntegrityEditModeTests
    {
        [Test]
        public void CardDropArea_Has_IsOccupied_Property()
        {
            // Verify IsOccupied property exists
            var isOccupiedProperty = typeof(CardDropArea).GetProperty("IsOccupied");
            Assert.IsNotNull(isOccupiedProperty, "CardDropArea should have IsOccupied property");
            
            // Verify it returns bool
            Assert.AreEqual(typeof(bool), isOccupiedProperty.PropertyType, 
                "IsOccupied property should return bool");
        }

        [Test]
        public void CardDropArea_Has_GetCardsPlayed_Static_Method()
        {
            // Verify GetCardsPlayed static method exists
            var getCardsPlayedMethod = typeof(CardDropArea).GetMethod("GetCardsPlayed", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            Assert.IsNotNull(getCardsPlayedMethod, "CardDropArea should have GetCardsPlayed static method");
            
            // Verify it returns int
            Assert.AreEqual(typeof(int), getCardsPlayedMethod.ReturnType, 
                "GetCardsPlayed should return int");
        }

        [Test]
        public void CardDropArea_Has_OnCardDrop_Methods()
        {
            // Verify OnCardDrop method exists (Player 1)
            var onCardDropMethod = typeof(CardDropArea).GetMethod("OnCardDrop");
            Assert.IsNotNull(onCardDropMethod, "CardDropArea should have OnCardDrop method");
            
            // Verify OnCardDropP2 method exists (Player 2)
            var onCardDropOppMethod = typeof(CardDropArea).GetMethod("OnCardDropP2");
            Assert.IsNotNull(onCardDropOppMethod, "CardDropArea should have OnCardDropP2 method");
        }

        [Test]
        public void CardDropArea_Implements_ICardDropArea()
        {
            // Verify CardDropArea implements ICardDropArea interface
            var iCardDropAreaType = typeof(ICardDropArea);
            Assert.IsTrue(iCardDropAreaType.IsAssignableFrom(typeof(CardDropArea)), 
                "CardDropArea should implement ICardDropArea interface");
        }
    }
}

