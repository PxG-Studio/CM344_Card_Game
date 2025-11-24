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
            // Verify CardMoverP1 has ReturnToStartPosition method
            var returnMethod = typeof(CardMoverP1).GetMethod("ReturnToStartPosition");
            Assert.IsNotNull(returnMethod, "CardMoverP1 should have ReturnToStartPosition method");
        }

        [Test]
        public void CardMover_Has_RefreshHomePosition_Method()
        {
            // Verify CardMoverP1 has RefreshHomePosition method
            var refreshMethod = typeof(CardMoverP1).GetMethod("RefreshHomePosition");
            Assert.IsNotNull(refreshMethod, "CardMoverP1 should have RefreshHomePosition method");
        }

        [Test]
        public void CardMoverP2_Has_ReturnToStartPosition_Method()
        {
            // Verify CardMoverP2 has ReturnToStartPosition method
            var returnMethod = typeof(CardMoverP2).GetMethod("ReturnToStartPosition");
            Assert.IsNotNull(returnMethod, "CardMoverP2 should have ReturnToStartPosition method");
        }

        [Test]
        public void CardMoverP2_Has_RefreshHomePosition_Method()
        {
            // Verify CardMoverP2 has RefreshHomePosition method
            var refreshMethod = typeof(CardMoverP2).GetMethod("RefreshHomePosition");
            Assert.IsNotNull(refreshMethod, "CardMoverP2 should have RefreshHomePosition method");
        }
    }
}

