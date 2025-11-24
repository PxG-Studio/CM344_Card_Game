using NUnit.Framework;
using UnityEngine;
using CardGame.Managers;
using CardGame.UI;
using CardGame.Core;
using CardGame.Factories;

namespace CardGame.Tests
{
    /// <summary>
    /// EditMode tests for card system structure and APIs.
    /// Validates that card systems have the necessary methods and properties.
    /// </summary>
    public class CardSystemEditModeTests
    {
        [Test]
        public void NewDeckManager_Has_Deck_Management_Methods()
        {
            // Verify NewDeckManagerP1 has deck management methods
            var initializeDeckMethod = typeof(NewDeckManagerP1).GetMethod("InitializeDeck",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var drawCardMethod = typeof(NewDeckManagerP1).GetMethod("DrawCard",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var shuffleDeckMethod = typeof(NewDeckManagerP1).GetMethod("ShuffleDeck",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            
            Assert.IsNotNull(initializeDeckMethod, "NewDeckManagerP1 should have InitializeDeck method");
            Assert.IsNotNull(drawCardMethod, "NewDeckManagerP1 should have DrawCard method");
            Assert.IsNotNull(shuffleDeckMethod, "NewDeckManagerP1 should have ShuffleDeck method");
        }

        [Test]
        public void NewDeckManager_Has_Hand_Property()
        {
            // Verify NewDeckManagerP1 has Hand property
            var handProperty = typeof(NewDeckManagerP1).GetProperty("Hand");
            Assert.IsNotNull(handProperty, "NewDeckManagerP1 should have Hand property");
        }

        [Test]
        public void NewDeckManagerP2_Has_Deck_Management_Methods()
        {
            // Verify NewDeckManagerP2 has deck management methods
            var initializeDeckMethod = typeof(NewDeckManagerP2).GetMethod("InitializeDeck",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var drawCardMethod = typeof(NewDeckManagerP2).GetMethod("DrawCard",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var shuffleDeckMethod = typeof(NewDeckManagerP2).GetMethod("ShuffleDeck",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            
            Assert.IsNotNull(initializeDeckMethod, "NewDeckManagerP2 should have InitializeDeck method");
            Assert.IsNotNull(drawCardMethod, "NewDeckManagerP2 should have DrawCard method");
            Assert.IsNotNull(shuffleDeckMethod, "NewDeckManagerP2 should have ShuffleDeck method");
        }

        [Test]
        public void NewDeckManagerP2_Has_Hand_Property()
        {
            // Verify NewDeckManagerP2 has Hand property
            var handProperty = typeof(NewDeckManagerP2).GetProperty("Hand");
            Assert.IsNotNull(handProperty, "NewDeckManagerP2 should have Hand property");
        }

        [Test]
        public void CardMover_Has_Card_Reference()
        {
            // Verify CardMoverP1 has Card property
            var cardProperty = typeof(CardMoverP1).GetProperty("Card");
            Assert.IsNotNull(cardProperty, "CardMoverP1 should have Card property");
            Assert.AreEqual(typeof(NewCard), cardProperty.PropertyType,
                "CardMoverP1.Card should be of type NewCard");
        }

        [Test]
        public void CardMoverP2_Has_Card_Reference()
        {
            // Verify CardMoverP2 has Card property
            var cardProperty = typeof(CardMoverP2).GetProperty("Card");
            Assert.IsNotNull(cardProperty, "CardMoverP2 should have Card property");
            Assert.AreEqual(typeof(NewCard), cardProperty.PropertyType,
                "CardMoverP2.Card should be of type NewCard");
        }

        [Test]
        public void NewCardUI_Has_Card_Reference()
        {
            // Verify NewCardUI has Card property
            var cardProperty = typeof(NewCardUI).GetProperty("Card");
            Assert.IsNotNull(cardProperty, "NewCardUI should have Card property");
            Assert.AreEqual(typeof(NewCard), cardProperty.PropertyType,
                "NewCardUI.Card should be of type NewCard");
        }

        [Test]
        public void CardFactory_Has_Card_Creation_Methods()
        {
            // Verify CardFactory has card creation methods
            var createCardUIMethod = typeof(CardFactory).GetMethod("CreateCardUI",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            Assert.IsNotNull(createCardUIMethod, "CardFactory should have CreateCardUI method");
        }

        [Test]
        public void NewCard_Has_Data_Property()
        {
            // Verify NewCard has Data property
            var dataProperty = typeof(NewCard).GetProperty("Data");
            Assert.IsNotNull(dataProperty, "NewCard should have Data property");
        }

        [Test]
        public void NewCard_Has_Directional_Stats()
        {
            // Verify NewCard has directional stat properties
            var topStatProperty = typeof(NewCard).GetProperty("CurrentTopStat");
            var rightStatProperty = typeof(NewCard).GetProperty("CurrentRightStat");
            var downStatProperty = typeof(NewCard).GetProperty("CurrentDownStat");
            var leftStatProperty = typeof(NewCard).GetProperty("CurrentLeftStat");
            
            Assert.IsNotNull(topStatProperty, "NewCard should have CurrentTopStat property");
            Assert.IsNotNull(rightStatProperty, "NewCard should have CurrentRightStat property");
            Assert.IsNotNull(downStatProperty, "NewCard should have CurrentDownStat property");
            Assert.IsNotNull(leftStatProperty, "NewCard should have CurrentLeftStat property");
        }
    }
}

