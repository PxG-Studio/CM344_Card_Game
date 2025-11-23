using NUnit.Framework;
using UnityEngine;
using CardGame.Managers;
using CardGame.Core;

namespace CardGame.Tests
{
    /// <summary>
    /// EditMode tests for deck and hand structure and API validation.
    /// </summary>
    public class DeckAndHandEditModeTests
    {
        [Test]
        public void NewDeckManager_Has_Required_Methods()
        {
            // Verify NewDeckManager has required methods
            var initializeMethod = typeof(NewDeckManager).GetMethod("InitializeDeck");
            Assert.IsNotNull(initializeMethod, "NewDeckManager should have InitializeDeck method");
            
            var shuffleMethod = typeof(NewDeckManager).GetMethod("ShuffleDeck");
            Assert.IsNotNull(shuffleMethod, "NewDeckManager should have ShuffleDeck method");
            
            var drawCardMethod = typeof(NewDeckManager).GetMethod("DrawCard");
            Assert.IsNotNull(drawCardMethod, "NewDeckManager should have DrawCard method");
            
            var drawCardsMethod = typeof(NewDeckManager).GetMethod("DrawCards");
            Assert.IsNotNull(drawCardsMethod, "NewDeckManager should have DrawCards method");
            
            var playCardMethod = typeof(NewDeckManager).GetMethod("PlayCard");
            Assert.IsNotNull(playCardMethod, "NewDeckManager should have PlayCard method");
        }

        [Test]
        public void NewDeckManager_Has_Hand_Property()
        {
            // Verify Hand property exists
            var handProperty = typeof(NewDeckManager).GetProperty("Hand");
            Assert.IsNotNull(handProperty, "NewDeckManager should have Hand property");
            
            // Verify it returns IReadOnlyList<NewCard>
            Assert.AreEqual(typeof(System.Collections.Generic.IReadOnlyList<NewCard>), handProperty.PropertyType, 
                "Hand property should return IReadOnlyList<NewCard>");
        }

        [Test]
        public void NewDeckManager_Has_DrawPileCount_Property()
        {
            // Verify DrawPileCount property exists
            var drawPileCountProperty = typeof(NewDeckManager).GetProperty("DrawPileCount");
            Assert.IsNotNull(drawPileCountProperty, "NewDeckManager should have DrawPileCount property");
        }
    }
}

