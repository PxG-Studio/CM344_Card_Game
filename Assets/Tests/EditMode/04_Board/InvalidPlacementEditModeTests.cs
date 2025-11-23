using NUnit.Framework;
using UnityEngine;
using CardGame.Core;

namespace CardGame.Tests
{
    /// <summary>
    /// EditMode tests for invalid placement recovery structure and API validation.
    /// </summary>
    public class InvalidPlacementEditModeTests
    {
        [Test]
        public void CardMover_Has_ReturnToStartPosition_Method()
        {
            // Verify CardMover has ReturnToStartPosition method
            var returnMethod = typeof(CardMover).GetMethod("ReturnToStartPosition");
            Assert.IsNotNull(returnMethod, "CardMover should have ReturnToStartPosition method");
        }

        [Test]
        public void CardMover_Has_RefreshHomePosition_Method()
        {
            // Verify CardMover has RefreshHomePosition method
            var refreshMethod = typeof(CardMover).GetMethod("RefreshHomePosition");
            Assert.IsNotNull(refreshMethod, "CardMover should have RefreshHomePosition method");
        }

        [Test]
        public void CardMoverOpp_Has_ReturnToStartPosition_Method()
        {
            // Verify CardMoverOpp has ReturnToStartPosition method
            var returnMethod = typeof(CardMoverOpp).GetMethod("ReturnToStartPosition");
            Assert.IsNotNull(returnMethod, "CardMoverOpp should have ReturnToStartPosition method");
        }

        [Test]
        public void CardMoverOpp_Has_RefreshHomePosition_Method()
        {
            // Verify CardMoverOpp has RefreshHomePosition method
            var refreshMethod = typeof(CardMoverOpp).GetMethod("RefreshHomePosition");
            Assert.IsNotNull(refreshMethod, "CardMoverOpp should have RefreshHomePosition method");
        }
    }
}

