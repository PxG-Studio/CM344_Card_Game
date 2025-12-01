using NUnit.Framework;
using UnityEngine;
using CardGame.Factories;
using CardGame.Core;
using CardGame.UI;
using NewCardData;
using CardGame.Managers;

namespace CardGame.Tests
{
    /// <summary>
    /// EditMode tests for CardFactory - validates structure and API.
    /// Tests static factory methods, error handling, and card creation logic.
    /// </summary>
    public class CardFactoryEditModeTests
    {
        [Test]
        public void CardFactory_Has_CreateCardUI_Method()
        {
            var createMethod = typeof(CardFactory).GetMethod("CreateCardUI",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            
            Assert.IsNotNull(createMethod, "CardFactory should have CreateCardUI static method");
            
            // Verify method signature
            var parameters = createMethod.GetParameters();
            Assert.GreaterOrEqual(parameters.Length, 3,
                "CreateCardUI should take at least 3 parameters (card, prefab, parent, optional revealDelay)");
            
            // Verify return type
            Assert.AreEqual(typeof(NewCardUI), createMethod.ReturnType,
                "CreateCardUI should return NewCardUI");
        }

        [Test]
        public void CardFactory_Has_CreateBoardCard_Method()
        {
            var createBoardMethod = typeof(CardFactory).GetMethod("CreateBoardCard",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            
            Assert.IsNotNull(createBoardMethod, "CardFactory should have CreateBoardCard static method");
            
            // Verify method signature
            var parameters = createBoardMethod.GetParameters();
            Assert.GreaterOrEqual(parameters.Length, 3,
                "CreateBoardCard should take at least 3 parameters (card, prefab, position)");
            
            // Verify return type
            Assert.AreEqual(typeof(GameObject), createBoardMethod.ReturnType,
                "CreateBoardCard should return GameObject");
        }

        [Test]
        public void CardFactory_CreateCardUI_Handles_Null_Card()
        {
            // Create test prefab and parent
            GameObject prefabObj = new GameObject("TestPrefab");
            NewCardUI prefab = prefabObj.AddComponent<NewCardUI>();
            
            GameObject parentObj = new GameObject("TestParent");
            Transform parent = parentObj.transform;
            
            // Call with null card
            NewCardUI result = CardFactory.CreateCardUI(null, prefab, parent);
            
            Assert.IsNull(result, "CardFactory.CreateCardUI should return null for null card");
            
            Object.DestroyImmediate(prefabObj);
            Object.DestroyImmediate(parentObj);
        }

        [Test]
        public void CardFactory_CreateCardUI_Handles_Null_Prefab()
        {
            // Create test card
            NewCardData.NewCardData cardData = ScriptableObject.CreateInstance<NewCardData.NewCardData>();
            cardData.cardName = "TestCard";
            NewCard card = new NewCard(cardData);
            
            GameObject parentObj = new GameObject("TestParent");
            Transform parent = parentObj.transform;
            
            // Call with null prefab
            NewCardUI result = CardFactory.CreateCardUI(card, null, parent);
            
            Assert.IsNull(result, "CardFactory.CreateCardUI should return null for null prefab");
            
            Object.DestroyImmediate(parentObj);
        }

        [Test]
        public void CardFactory_CreateCardUI_Handles_Null_Parent()
        {
            // Create test card and prefab
            NewCardData.NewCardData cardData = ScriptableObject.CreateInstance<NewCardData.NewCardData>();
            cardData.cardName = "TestCard";
            NewCard card = new NewCard(cardData);
            
            GameObject prefabObj = new GameObject("TestPrefab");
            NewCardUI prefab = prefabObj.AddComponent<NewCardUI>();
            
            // Call with null parent
            NewCardUI result = CardFactory.CreateCardUI(card, prefab, null);
            
            Assert.IsNull(result, "CardFactory.CreateCardUI should return null for null parent");
            
            Object.DestroyImmediate(prefabObj);
        }

        [Test]
        public void CardFactory_CreateBoardCard_Handles_Null_Parameters()
        {
            // Test null card
            GameObject prefab = new GameObject("TestPrefab");
            GameObject result1 = CardFactory.CreateBoardCard(null, prefab, Vector3.zero);
            Assert.IsNull(result1, "CreateBoardCard should return null for null card");
            
            // Test null prefab
            NewCardData.NewCardData cardData = ScriptableObject.CreateInstance<NewCardData.NewCardData>();
            cardData.cardName = "TestCard";
            NewCard card = new NewCard(cardData);
            GameObject result2 = CardFactory.CreateBoardCard(card, null, Vector3.zero);
            Assert.IsNull(result2, "CreateBoardCard should return null for null prefab");
            
            Object.DestroyImmediate(prefab);
        }

        [Test]
        public void CardFactory_Is_Static_Class()
        {
            // Verify CardFactory is static (no instance constructor)
            var constructors = typeof(CardFactory).GetConstructors(
                System.Reflection.BindingFlags.Public | 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
            
            // Static classes have no instance constructors
            Assert.AreEqual(0, constructors.Length,
                "CardFactory should be a static class (no instance constructors)");
        }
    }
}

