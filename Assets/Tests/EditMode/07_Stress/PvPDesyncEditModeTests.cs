using NUnit.Framework;
using UnityEngine;
using CardGame.Managers;
using CardGame.Core;

namespace CardGame.Tests
{
    /// <summary>
    /// EditMode tests for PvP desync safety structure and API validation.
    /// </summary>
    public class PvPDesyncEditModeTests
    {
        [Test]
        public void NewDeckManager_Has_Independent_Hand_Property()
        {
            // Verify NewDeckManagerP1 has Hand property
            var handProperty = typeof(NewDeckManagerP1).GetProperty("Hand");
            Assert.IsNotNull(handProperty, "NewDeckManagerP1 should have Hand property");
            
            // Verify it returns IReadOnlyList<NewCard>
            Assert.AreEqual(typeof(System.Collections.Generic.IReadOnlyList<NewCard>), handProperty.PropertyType, 
                "Hand property should return IReadOnlyList<NewCard>");
        }

        [Test]
        public void NewDeckManagerP2_Has_Independent_Hand_Property()
        {
            // Verify NewDeckManagerP2 has Hand property
            var handProperty = typeof(NewDeckManagerP2).GetProperty("Hand");
            Assert.IsNotNull(handProperty, "NewDeckManagerP2 should have Hand property");
            
            // Verify it returns IReadOnlyList<NewCard>
            Assert.AreEqual(typeof(System.Collections.Generic.IReadOnlyList<NewCard>), handProperty.PropertyType, 
                "Hand property should return IReadOnlyList<NewCard>");
        }

        [Test]
        public void NewDeckManager_And_NewDeckManagerP2_Are_Different_Types()
        {
            // Verify they are different types (independent implementations)
            Assert.AreNotEqual(typeof(NewDeckManagerP1), typeof(NewDeckManagerP2), 
                "NewDeckManagerP1 and NewDeckManagerP2 should be different types");
        }

        [Test]
        public void FateFlowController_Has_CanAct_Method()
        {
            // Verify FateFlowController has CanAct method
            var canActMethod = typeof(FateFlowController).GetMethod("CanAct");
            Assert.IsNotNull(canActMethod, "FateFlowController should have CanAct method");
            
            // Verify it takes FateSide parameter
            var parameters = canActMethod.GetParameters();
            Assert.AreEqual(1, parameters.Length, "CanAct should take one parameter");
            Assert.AreEqual(typeof(FateSide), parameters[0].ParameterType, 
                "CanAct parameter should be FateSide");
        }
    }
}

