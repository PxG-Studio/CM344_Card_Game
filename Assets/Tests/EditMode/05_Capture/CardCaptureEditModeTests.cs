using NUnit.Framework;
using UnityEngine;
using CardGame.Core;

namespace CardGame.Tests
{
    /// <summary>
    /// EditMode tests for card capture logic structure and API validation.
    /// </summary>
    public class CardCaptureEditModeTests
    {
        [Test]
        public void CardDropArea1_Has_Capture_Methods()
        {
            // Verify CardDropArea1 has battle checking methods
            var checkBattlesMethod = typeof(CardDropArea1).GetMethod("CheckCardBattles", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(checkBattlesMethod, "CardDropArea1 should have CheckCardBattles method");
            
            var checkChainCaptureMethod = typeof(CardDropArea1).GetMethod("CheckChainCapture", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(checkChainCaptureMethod, "CardDropArea1 should have CheckChainCapture method");
            
            var executeRippleFlipsMethod = typeof(CardDropArea1).GetMethod("ExecuteRippleFlips", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(executeRippleFlipsMethod, "CardDropArea1 should have ExecuteRippleFlips method");
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

