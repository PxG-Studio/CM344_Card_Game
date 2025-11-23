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
        public void CardDropArea1_Has_IsOccupied_Property()
        {
            // Verify IsOccupied property exists
            var isOccupiedProperty = typeof(CardDropArea1).GetProperty("IsOccupied");
            Assert.IsNotNull(isOccupiedProperty, "CardDropArea1 should have IsOccupied property");
            
            // Verify it returns bool
            Assert.AreEqual(typeof(bool), isOccupiedProperty.PropertyType, 
                "IsOccupied property should return bool");
        }

        [Test]
        public void CardDropArea1_Has_GetCardsPlayed_Static_Method()
        {
            // Verify GetCardsPlayed static method exists
            var getCardsPlayedMethod = typeof(CardDropArea1).GetMethod("GetCardsPlayed", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            Assert.IsNotNull(getCardsPlayedMethod, "CardDropArea1 should have GetCardsPlayed static method");
            
            // Verify it returns int
            Assert.AreEqual(typeof(int), getCardsPlayedMethod.ReturnType, 
                "GetCardsPlayed should return int");
        }

        [Test]
        public void CardDropArea1_Has_OnCardDrop_Methods()
        {
            // Verify OnCardDrop method exists (Player 1)
            var onCardDropMethod = typeof(CardDropArea1).GetMethod("OnCardDrop");
            Assert.IsNotNull(onCardDropMethod, "CardDropArea1 should have OnCardDrop method");
            
            // Verify OnCardDropOpp method exists (Player 2)
            var onCardDropOppMethod = typeof(CardDropArea1).GetMethod("OnCardDropOpp");
            Assert.IsNotNull(onCardDropOppMethod, "CardDropArea1 should have OnCardDropOpp method");
        }

        [Test]
        public void CardDropArea1_Implements_ICardDropArea()
        {
            // Verify CardDropArea1 implements ICardDropArea interface
            var iCardDropAreaType = typeof(ICardDropArea);
            Assert.IsTrue(iCardDropAreaType.IsAssignableFrom(typeof(CardDropArea1)), 
                "CardDropArea1 should implement ICardDropArea interface");
        }
    }
}

