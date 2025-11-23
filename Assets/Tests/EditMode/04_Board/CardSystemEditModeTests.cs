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
            // Verify NewDeckManager has deck management methods
            var initializeDeckMethod = typeof(NewDeckManager).GetMethod("InitializeDeck",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var drawCardMethod = typeof(NewDeckManager).GetMethod("DrawCard",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var shuffleDeckMethod = typeof(NewDeckManager).GetMethod("ShuffleDeck",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            
            Assert.IsNotNull(initializeDeckMethod, "NewDeckManager should have InitializeDeck method");
            Assert.IsNotNull(drawCardMethod, "NewDeckManager should have DrawCard method");
            Assert.IsNotNull(shuffleDeckMethod, "NewDeckManager should have ShuffleDeck method");
        }

        [Test]
        public void NewDeckManager_Has_Hand_Property()
        {
            // Verify NewDeckManager has Hand property
            var handProperty = typeof(NewDeckManager).GetProperty("Hand");
            Assert.IsNotNull(handProperty, "NewDeckManager should have Hand property");
        }

        [Test]
        public void NewDeckManagerOpp_Has_Deck_Management_Methods()
        {
            // Verify NewDeckManagerOpp has deck management methods
            var initializeDeckMethod = typeof(NewDeckManagerOpp).GetMethod("InitializeDeck",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var drawCardMethod = typeof(NewDeckManagerOpp).GetMethod("DrawCard",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var shuffleDeckMethod = typeof(NewDeckManagerOpp).GetMethod("ShuffleDeck",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            
            Assert.IsNotNull(initializeDeckMethod, "NewDeckManagerOpp should have InitializeDeck method");
            Assert.IsNotNull(drawCardMethod, "NewDeckManagerOpp should have DrawCard method");
            Assert.IsNotNull(shuffleDeckMethod, "NewDeckManagerOpp should have ShuffleDeck method");
        }

        [Test]
        public void NewDeckManagerOpp_Has_Hand_Property()
        {
            // Verify NewDeckManagerOpp has Hand property
            var handProperty = typeof(NewDeckManagerOpp).GetProperty("Hand");
            Assert.IsNotNull(handProperty, "NewDeckManagerOpp should have Hand property");
        }

        [Test]
        public void CardMover_Has_Card_Reference()
        {
            // Verify CardMover has Card property
            var cardProperty = typeof(CardMover).GetProperty("Card");
            Assert.IsNotNull(cardProperty, "CardMover should have Card property");
            Assert.AreEqual(typeof(NewCard), cardProperty.PropertyType,
                "CardMover.Card should be of type NewCard");
        }

        [Test]
        public void CardMoverOpp_Has_Card_Reference()
        {
            // Verify CardMoverOpp has Card property
            var cardProperty = typeof(CardMoverOpp).GetProperty("Card");
            Assert.IsNotNull(cardProperty, "CardMoverOpp should have Card property");
            Assert.AreEqual(typeof(NewCard), cardProperty.PropertyType,
                "CardMoverOpp.Card should be of type NewCard");
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

