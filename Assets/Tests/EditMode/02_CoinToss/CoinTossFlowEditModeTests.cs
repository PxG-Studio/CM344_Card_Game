using NUnit.Framework;
using UnityEngine;
using CardGame.Managers;
using CardGame.UI;

namespace CardGame.Tests
{
    /// <summary>
    /// EditMode tests for Coin Toss Flow structure and API validation.
    /// Tests component existence, method availability, and structure without runtime behavior.
    /// </summary>
    public class CoinTossFlowEditModeTests
    {
        [Test]
        public void CoinTossManager_Has_Required_Methods()
        {
            // Verify CoinTossManager has required methods
            var performMethod = typeof(CoinTossManager).GetMethod("PerformCoinToss");
            Assert.IsNotNull(performMethod, "CoinTossManager should have PerformCoinToss method");
            
            var resetMethod = typeof(CoinTossManager).GetMethod("ResetCoinToss");
            Assert.IsNotNull(resetMethod, "CoinTossManager should have ResetCoinToss method");
            
            var getStartingPlayerMethod = typeof(CoinTossManager).GetMethod("GetStartingPlayer");
            Assert.IsNotNull(getStartingPlayerMethod, "CoinTossManager should have GetStartingPlayer method");
            
            var setForcedResultMethod = typeof(CoinTossManager).GetMethod("SetForcedResult");
            Assert.IsNotNull(setForcedResultMethod, "CoinTossManager should have SetForcedResult method");
        }

        [Test]
        public void CoinTossManager_Has_Required_Properties()
        {
            // Verify CoinTossManager has required properties
            var resultProperty = typeof(CoinTossManager).GetProperty("Result");
            Assert.IsNotNull(resultProperty, "CoinTossManager should have Result property");
            
            var isCompleteProperty = typeof(CoinTossManager).GetProperty("IsComplete");
            Assert.IsNotNull(isCompleteProperty, "CoinTossManager should have IsComplete property");
        }

        [Test]
        public void CoinTossManager_Has_OnCoinTossComplete_Event()
        {
            // Verify OnCoinTossComplete event exists
            var eventField = typeof(CoinTossManager).GetEvent("OnCoinTossComplete");
            Assert.IsNotNull(eventField, "CoinTossManager should have OnCoinTossComplete event");
        }

        [Test]
        public void CoinTossUI_Has_Required_Methods()
        {
            // Verify CoinTossUI has required methods
            var startCoinTossMethod = typeof(CoinTossUI).GetMethod("StartCoinToss");
            Assert.IsNotNull(startCoinTossMethod, "CoinTossUI should have StartCoinToss method");
            
            var startAnimationMethod = typeof(CoinTossUI).GetMethod("StartCoinTossAnimation");
            Assert.IsNotNull(startAnimationMethod, "CoinTossUI should have StartCoinTossAnimation method");
            
            var showMethod = typeof(CoinTossUI).GetMethod("Show");
            Assert.IsNotNull(showMethod, "CoinTossUI should have Show method");
            
            var hideMethod = typeof(CoinTossUI).GetMethod("Hide");
            Assert.IsNotNull(hideMethod, "CoinTossUI should have Hide method");
        }

        [Test]
        public void CoinTossUI_Has_Required_UI_Fields()
        {
            // Verify CoinTossUI has required UI field references
            var coinTossPanelField = typeof(CoinTossUI).GetField("coinTossPanel", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(coinTossPanelField, "CoinTossUI should have coinTossPanel field");
            
            var resultTextField = typeof(CoinTossUI).GetField("resultText", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(resultTextField, "CoinTossUI should have resultText field");
            
            var headsLabelField = typeof(CoinTossUI).GetField("headsLabel", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(headsLabelField, "CoinTossUI should have headsLabel field");
            
            var tailsLabelField = typeof(CoinTossUI).GetField("tailsLabel", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(tailsLabelField, "CoinTossUI should have tailsLabel field");
            
            var coinImageField = typeof(CoinTossUI).GetField("coinImage", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(coinImageField, "CoinTossUI should have coinImage field");
            
            var continueButtonField = typeof(CoinTossUI).GetField("continueButton", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(continueButtonField, "CoinTossUI should have continueButton field");
        }
    }
}

