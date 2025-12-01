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
            // Verify NewDeckManagerP1 has required methods
            var initializeMethod = typeof(NewDeckManagerP1).GetMethod("InitializeDeck");
            Assert.IsNotNull(initializeMethod, "NewDeckManagerP1 should have InitializeDeck method");
            
            var shuffleMethod = typeof(NewDeckManagerP1).GetMethod("ShuffleDeck");
            Assert.IsNotNull(shuffleMethod, "NewDeckManagerP1 should have ShuffleDeck method");
            
            var drawCardMethod = typeof(NewDeckManagerP1).GetMethod("DrawCard");
            Assert.IsNotNull(drawCardMethod, "NewDeckManagerP1 should have DrawCard method");
            
            var drawCardsMethod = typeof(NewDeckManagerP1).GetMethod("DrawCards");
            Assert.IsNotNull(drawCardsMethod, "NewDeckManagerP1 should have DrawCards method");
            
            var playCardMethod = typeof(NewDeckManagerP1).GetMethod("PlayCard");
            Assert.IsNotNull(playCardMethod, "NewDeckManagerP1 should have PlayCard method");
        }

        [Test]
        public void NewDeckManager_Has_Hand_Property()
        {
            // Verify Hand property exists
            var handProperty = typeof(NewDeckManagerP1).GetProperty("Hand");
            Assert.IsNotNull(handProperty, "NewDeckManagerP1 should have Hand property");
            
            // Verify it returns IReadOnlyList<NewCard>
            Assert.AreEqual(typeof(System.Collections.Generic.IReadOnlyList<NewCard>), handProperty.PropertyType, 
                "Hand property should return IReadOnlyList<NewCard>");
        }

        [Test]
        public void NewDeckManager_Has_DrawPileCount_Property()
        {
            // Verify DrawPileCount property exists
            var drawPileCountProperty = typeof(NewDeckManagerP1).GetProperty("DrawPileCount");
            Assert.IsNotNull(drawPileCountProperty, "NewDeckManagerP1 should have DrawPileCount property");
        }
    }
}

