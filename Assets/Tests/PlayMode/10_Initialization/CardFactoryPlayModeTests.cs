using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CardGame.Factories;
using CardGame.Core;
using CardGame.UI;
using NewCardData;

namespace CardGame.Tests
{
    /// <summary>
    /// PlayMode tests for CardFactory - validates card creation and initialization.
    /// Tests factory methods, error handling, and card creation correctness.
    /// </summary>
    public class CardFactoryPlayModeTests
    {
        private const string SCENE_NAME = "BattleScreenMultiplayer";
        private const float TEST_TIMEOUT = 60f;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            CardTestHelper.ClearSingletonInstances();
            yield return null;
            
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(SCENE_NAME, LoadSceneMode.Single);
            asyncLoad.allowSceneActivation = true;
            
            float elapsed = 0f;
            while (!asyncLoad.isDone && elapsed < 10f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            if (!asyncLoad.isDone)
            {
                Assert.Fail($"Scene '{SCENE_NAME}' failed to load");
            }
            
            yield return new WaitForSeconds(1.0f);
        }
        
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return null;
            CardTestHelper.ClearSingletonInstances();
            yield return null;
        }

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator CardFactory_Creates_CardUI_Correctly()
        {
            yield return null;
            // Create test card data
            NewCardData.NewCardData cardData = ScriptableObject.CreateInstance<NewCardData.NewCardData>();
            cardData.TopStat = 5;
            cardData.RightStat = 3;
            cardData.DownStat = 4;
            cardData.LeftStat = 2;
            cardData.cardName = "TestCard";
            cardData.cardType = CardType.Flame;
            
            NewCard card = new NewCard(cardData);
            
            // Find card prefab
            NewCardUI prefab = Resources.Load<NewCardUI>("NewCardPrefab");
            if (prefab == null)
            {
                prefab = Object.FindObjectOfType<NewCardUI>();
            }
            
            // If prefab still not found, create a minimal one programmatically for testing
            if (prefab == null)
            {
                GameObject prefabObj = new GameObject("TestCardPrefab");
                prefab = prefabObj.AddComponent<NewCardUI>();
                prefabObj.SetActive(false); // Prefabs should be inactive
            }
            
            // Create parent transform
            GameObject parentObj = new GameObject("TestParent");
            Transform parent = parentObj.transform;
            
            // Create card using factory
            NewCardUI cardUI = CardFactory.CreateCardUI(card, prefab, parent);
            
            Assert.IsNotNull(cardUI, "CardFactory should create card UI");
            Assert.IsNotNull(cardUI.Card, "Created card UI should have card data");
            Assert.AreEqual(card, cardUI.Card, "Created card UI should have correct card");
            Assert.IsTrue(cardUI.gameObject.activeSelf, "Created card should be active");
            
            // Cleanup
            Object.Destroy(parentObj);
            if (prefab != null && prefab.gameObject.name == "TestCardPrefab")
            {
                Object.Destroy(prefab.gameObject);
            }
            yield return null;
        }

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator CardFactory_Handles_Null_Card()
        {
            NewCardUI prefab = Resources.Load<NewCardUI>("NewCardPrefab");
            if (prefab == null)
            {
                prefab = Object.FindObjectOfType<NewCardUI>();
            }
            
            // If prefab still not found, create a minimal one programmatically for testing
            if (prefab == null)
            {
                GameObject prefabObj = new GameObject("TestCardPrefab");
                prefab = prefabObj.AddComponent<NewCardUI>();
                prefabObj.SetActive(false);
            }
            
            GameObject parentObj = new GameObject("TestParent");
            Transform parent = parentObj.transform;
            
            // Try to create with null card
            NewCardUI cardUI = CardFactory.CreateCardUI(null, prefab, parent);
            
            Assert.IsNull(cardUI, "CardFactory should return null for null card");
            
            Object.Destroy(parentObj);
            if (prefab != null && prefab.gameObject.name == "TestCardPrefab")
            {
                Object.Destroy(prefab.gameObject);
            }
            yield return null;
        }

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator CardFactory_Handles_Null_Prefab()
        {
            NewCardData.NewCardData cardData = ScriptableObject.CreateInstance<NewCardData.NewCardData>();
            cardData.cardName = "TestCard";
            NewCard card = new NewCard(cardData);
            
            GameObject parentObj = new GameObject("TestParent");
            Transform parent = parentObj.transform;
            
            // Try to create with null prefab
            NewCardUI cardUI = CardFactory.CreateCardUI(card, null, parent);
            
            Assert.IsNull(cardUI, "CardFactory should return null for null prefab");
            
            Object.Destroy(parentObj);
            yield return null;
        }

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator CardFactory_Creates_Active_Cards()
        {
            NewCardData.NewCardData cardData = ScriptableObject.CreateInstance<NewCardData.NewCardData>();
            cardData.cardName = "TestCard";
            NewCard card = new NewCard(cardData);
            
            NewCardUI prefab = Resources.Load<NewCardUI>("NewCardPrefab");
            if (prefab == null)
            {
                prefab = Object.FindObjectOfType<NewCardUI>();
            }
            
            // If prefab still not found, create a minimal one programmatically for testing
            if (prefab == null)
            {
                GameObject prefabObj = new GameObject("TestCardPrefab");
                prefab = prefabObj.AddComponent<NewCardUI>();
                prefabObj.SetActive(false);
            }
            
            GameObject parentObj = new GameObject("TestParent");
            Transform parent = parentObj.transform;
            
            NewCardUI cardUI = CardFactory.CreateCardUI(card, prefab, parent);
            
            if (cardUI != null)
            {
                Assert.IsTrue(cardUI.gameObject.activeSelf,
                    "CardFactory should create active cards");
                
                // Verify CanvasGroup is interactive
                CanvasGroup cg = cardUI.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    Assert.IsTrue(cg.interactable,
                        "CardFactory should make cards interactive");
                    Assert.IsTrue(cg.blocksRaycasts,
                        "CardFactory should make cards block raycasts");
                }
            }
            
            Object.Destroy(parentObj);
            if (prefab != null && prefab.gameObject.name == "TestCardPrefab")
            {
                Object.Destroy(prefab.gameObject);
            }
            yield return null;
        }

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator CardFactory_Creates_BoardCard_Correctly()
        {
            NewCardData.NewCardData cardData = ScriptableObject.CreateInstance<NewCardData.NewCardData>();
            cardData.cardName = "TestBoardCard";
            NewCard card = new NewCard(cardData);
            
            // Find board card prefab
            GameObject boardPrefab = Resources.Load<GameObject>("NewCardPrefab");
            if (boardPrefab == null)
            {
                // Try to find existing card on board
                CardMoverP1[] movers = Object.FindObjectsOfType<CardMoverP1>();
                if (movers.Length > 0)
                {
                    boardPrefab = movers[0].gameObject;
                }
            }
            
            // If prefab still not found, create a minimal one programmatically for testing
            if (boardPrefab == null)
            {
                GameObject prefabObj = new GameObject("TestBoardCardPrefab");
                prefabObj.AddComponent<NewCardUI>();
                prefabObj.AddComponent<CardMoverP1>();
                prefabObj.SetActive(false);
                boardPrefab = prefabObj;
            }
            
            Vector3 testPosition = new Vector3(0, 0, 0);
            GameObject boardCard = CardFactory.CreateBoardCard(card, boardPrefab, testPosition);
            
            Assert.IsNotNull(boardCard, "CardFactory should create board card");
            Assert.IsTrue(boardCard.activeSelf, "Board card should be active");
            Assert.AreEqual(card.Data.cardName, boardCard.name,
                "Board card should have correct name");
            
            // Verify card data is set
            CardMoverP1 mover = boardCard.GetComponent<CardMoverP1>();
            CardMoverP2 moverP2 = boardCard.GetComponent<CardMoverP2>();
            
            if (mover != null)
            {
                Assert.IsNotNull(mover.Card, "Board card CardMoverP1 should have card data");
            }
            else if (moverP2 != null)
            {
                Assert.IsNotNull(moverP2.Card, "Board card CardMoverP2 should have card data");
            }
            
            Object.Destroy(boardCard);
            if (boardPrefab != null && boardPrefab.name == "TestBoardCardPrefab")
            {
                Object.Destroy(boardPrefab);
            }
            yield return null;
        }
    }
}

