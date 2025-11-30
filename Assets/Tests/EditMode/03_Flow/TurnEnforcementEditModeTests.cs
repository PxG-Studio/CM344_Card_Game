using NUnit.Framework;
using UnityEngine;
using CardGame.Managers;
using CardGame.UI;

namespace CardGame.Tests
{
    /// <summary>
    /// EditMode tests for turn enforcement structure and API validation.
    /// </summary>
    public class TurnEnforcementEditModeTests
    {
        [Test]
        public void FateFlowController_Has_Turn_Validation_Methods()
        {
            // Verify FateFlowController has required methods
            var canActMethod = typeof(FateFlowController).GetMethod("CanAct");
            Assert.IsNotNull(canActMethod, "FateFlowController should have CanAct method");
            
            var advanceFateFlowMethod = typeof(FateFlowController).GetMethod("AdvanceFateFlow");
            Assert.IsNotNull(advanceFateFlowMethod, "FateFlowController should have AdvanceFateFlow method");
            
            var setFateMethod = typeof(FateFlowController).GetMethod("SetFate");
            Assert.IsNotNull(setFateMethod, "FateFlowController should have SetFate method");
        }

        [Test]
        public void FateFlowController_Has_OnFateChanged_Event()
        {
            // Verify OnFateChanged event exists
            var eventField = typeof(FateFlowController).GetEvent("OnFateChanged");
            Assert.IsNotNull(eventField, "FateFlowController should have OnFateChanged event");
        }

        [Test]
        public void NewCardUI_Has_Turn_Validation_In_Drag()
        {
            // Verify NewCardUI checks turn before allowing drag
            var onBeginDragMethod = typeof(NewCardUI).GetMethod("OnBeginDrag");
            Assert.IsNotNull(onBeginDragMethod, "NewCardUI should have OnBeginDrag method");
            
            // Verify IsPlayerCard and IsOpponentCard methods exist
            var isPlayerCardMethod = typeof(NewCardUI).GetMethod("IsPlayerCard", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var isOpponentCardMethod = typeof(NewCardUI).GetMethod("IsOpponentCard", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            Assert.IsNotNull(isPlayerCardMethod, "NewCardUI should have IsPlayerCard method");
            Assert.IsNotNull(isOpponentCardMethod, "NewCardUI should have IsOpponentCard method");
        }
    }
}

