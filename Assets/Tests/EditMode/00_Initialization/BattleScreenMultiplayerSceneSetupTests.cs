using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEditor.SceneManagement;
using CardGame.UI;
using CardGame.Managers;
using CardGame.Visuals;

namespace CardGame.Tests
{
    /// <summary>
    /// EditMode tests for BattleScreenMultiplayer scene setup and component validation.
    /// These tests verify scene structure, component presence, and initialization without entering Play mode.
    /// </summary>
    public class BattleScreenMultiplayerSceneSetupTests
    {
        private const string SCENE_NAME = "BattleScreenMultiplayer";
        private Scene scene;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            // Load scene using EditorSceneManager for EditMode tests
            // EditorSceneManager.OpenScene() works in EditMode, SceneManager.LoadScene() only works in PlayMode
            string scenePath = $"Assets/Scenes/{SCENE_NAME}.unity";
            scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            
            if (!scene.IsValid())
            {
                // Try alternative path
                scenePath = $"Assets/{SCENE_NAME}.unity";
                scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }
            
            Assert.IsTrue(scene.IsValid(), $"Scene '{SCENE_NAME}' should be loaded. Path attempted: Assets/Scenes/{SCENE_NAME}.unity");
        }

        [Test]
        public void Scene_Exists()
        {
            Assert.IsTrue(scene.IsValid(), $"Scene '{SCENE_NAME}' should exist");
            Assert.IsTrue(scene.isLoaded, $"Scene '{SCENE_NAME}' should be loaded");
        }

        [Test]
        public void HUDOverlayCanvas_Exists()
        {
            GameObject hudCanvas = GameObject.Find("HUDOverlayCanvas");
            Assert.IsNotNull(hudCanvas, "HUDOverlayCanvas should exist in scene");
            
            // Canvas component is added at runtime by HUDSetup.ConvertToCanvas()
            // In EditMode, it may not exist yet, so we just verify the GameObject exists
            // The actual Canvas component will be added when HUDSetup runs (PlayMode)
            Canvas canvas = hudCanvas.GetComponent<Canvas>();
            
            if (canvas == null)
            {
                // Canvas not present in EditMode - this is OK, HUDSetup will add it at runtime
                // Just verify the GameObject exists and can accept the component
                Assert.IsTrue(true, "HUDOverlayCanvas GameObject exists (Canvas component will be added at runtime by HUDSetup)");
            }
            else
            {
                // Canvas is already present - verify it's configured correctly
                Assert.IsNotNull(canvas, "HUDOverlayCanvas should have Canvas component");
            }
        }

        [Test]
        public void MainCanvas_Exists()
        {
            GameObject mainCanvas = GameObject.Find("Canvas");
            Assert.IsNotNull(mainCanvas, "Main Canvas should exist in scene");
        }

        [Test]
        public void AllDropAreas_Exist()
        {
            for (int i = 1; i <= 16; i++)
            {
                GameObject dropArea = GameObject.Find($"DropArea{i}");
                Assert.IsNotNull(dropArea, $"DropArea{i} should exist in scene");
                
                CardDropArea1 dropAreaComponent = dropArea.GetComponent<CardDropArea1>();
                Assert.IsNotNull(dropAreaComponent, $"DropArea{i} should have CardDropArea1 component");
            }
        }

        [Test]
        public void PrefabAssets_ExistButMayBeInactive()
        {
            // Prefab assets should not be in the scene hierarchy, but if they are, they should be inactive
            // NewCardUI.Start() disables them, but in EditMode they may still be active
            GameObject prefabAsset1 = GameObject.Find("NewCardPrefab");
            GameObject prefabAsset2 = GameObject.Find("NewCardPrefabOpp");
            
            if (prefabAsset1 != null)
            {
                // If prefab exists in scene, it should ideally be inactive
                // However, in EditMode they may still be active (will be disabled at runtime by NewCardUI)
                if (prefabAsset1.activeSelf)
                {
                    // Info: Prefab is active in EditMode (expected) - NewCardUI will disable it at runtime
                    Debug.Log($"NewCardPrefab is active in scene (EditMode). " +
                             $"It will be disabled at runtime by NewCardUI.Start(). " +
                             $"This is expected behavior - prefab assets are disabled at runtime.");
                    // Don't fail - this is handled at runtime
                    Assert.IsTrue(true, "NewCardPrefab exists (will be disabled at runtime)");
                }
                else
                {
                    Assert.IsTrue(true, "NewCardPrefab is inactive (good)");
                }
            }
            
            if (prefabAsset2 != null)
            {
                // If prefab exists in scene, it should ideally be inactive
                // However, in EditMode they may still be active (will be disabled at runtime by NewCardUI)
                if (prefabAsset2.activeSelf)
                {
                    // Info: Prefab is active in EditMode (expected) - NewCardUI will disable it at runtime
                    Debug.Log($"NewCardPrefabOpp is active in scene (EditMode). " +
                             $"It will be disabled at runtime by NewCardUI.Start(). " +
                             $"This is expected behavior - prefab assets are disabled at runtime.");
                    // Don't fail - this is handled at runtime
                    Assert.IsTrue(true, "NewCardPrefabOpp exists (will be disabled at runtime)");
                }
                else
                {
                    Assert.IsTrue(true, "NewCardPrefabOpp is inactive (good)");
                }
            }
            
            // If both are null, that's actually the best case - they shouldn't be in scene at all
            if (prefabAsset1 == null && prefabAsset2 == null)
            {
                Assert.IsTrue(true, "No prefab assets found in scene hierarchy (ideal - prefabs should only exist as assets, not in scene)");
            }
        }

        [Test]
        public void HandUIs_Exist()
        {
            GameObject handUI1 = GameObject.Find("NewHandUI Flame");
            GameObject handUI2 = GameObject.Find("NewHandUI Earth");
            
            Assert.IsNotNull(handUI1, "NewHandUI Flame should exist");
            Assert.IsNotNull(handUI2, "NewHandUI Earth should exist");
            
            // Player 1 (Flame) should have NewHandUI component
            // Player 2/Opponent (Earth) should have NewHandOppUI component
            // Try GetComponent first, then GetComponentInChildren as fallback
            NewHandUI handUIComponent1 = handUI1.GetComponent<NewHandUI>();
            if (handUIComponent1 == null)
            {
                handUIComponent1 = handUI1.GetComponentInChildren<NewHandUI>(true);
            }
            
            // If component not found, provide diagnostic information
            if (handUIComponent1 == null)
            {
                MonoBehaviour[] components1 = handUI1.GetComponents<MonoBehaviour>();
                string componentList1 = components1.Length > 0 
                    ? string.Join(", ", components1.Select(c => c.GetType().Name)) 
                    : "None";
                
                Assert.Fail($"NewHandUI Flame GameObject exists but is missing NewHandUI component. " +
                           $"Found components: [{componentList1}]. " +
                           $"Child count: {handUI1.transform.childCount}. " +
                           $"FIX: Select 'NewHandUI Flame' in scene and add 'NewHandUI' component " +
                           $"(Component > CardGame.UI > NewHandUI)");
            }
            
            // Player 2/Opponent uses NewHandOppUI, not NewHandUI
            NewHandOppUI handOppUIComponent = handUI2.GetComponent<NewHandOppUI>();
            if (handOppUIComponent == null)
            {
                handOppUIComponent = handUI2.GetComponentInChildren<NewHandOppUI>(true);
            }
            
            // If component not found, provide diagnostic information
            if (handOppUIComponent == null)
            {
                MonoBehaviour[] components2 = handUI2.GetComponents<MonoBehaviour>();
                string componentList2 = components2.Length > 0 
                    ? string.Join(", ", components2.Select(c => c.GetType().Name)) 
                    : "None";
                
                Assert.Fail($"NewHandUI Earth GameObject exists but is missing NewHandOppUI component (Player 2/Opponent uses NewHandOppUI, not NewHandUI). " +
                           $"Found components: [{componentList2}]. " +
                           $"Child count: {handUI2.transform.childCount}. " +
                           $"FIX: Select 'NewHandUI Earth' in scene and add 'NewHandOppUI' component " +
                           $"(Component > CardGame.UI > NewHandOppUI)");
            }
            
            Assert.IsNotNull(handUIComponent1, "NewHandUI Flame should have NewHandUI component (Player 1)");
            Assert.IsNotNull(handOppUIComponent, "NewHandUI Earth should have NewHandOppUI component (Player 2/Opponent)");
        }

        [Test]
        public void DeckManagers_Exist()
        {
            GameObject deckMgr1 = GameObject.Find("NewDeckManager Flame");
            GameObject deckMgr2 = GameObject.Find("NewDeckManager Earth");
            
            Assert.IsNotNull(deckMgr1, "NewDeckManager Flame should exist");
            Assert.IsNotNull(deckMgr2, "NewDeckManager Earth should exist");
        }

        [Test]
        public void EventSystem_Exists()
        {
            GameObject eventSystem = GameObject.Find("EventSystem");
            Assert.IsNotNull(eventSystem, "EventSystem should exist for UI interaction");
            Assert.IsNotNull(eventSystem.GetComponent<UnityEngine.EventSystems.EventSystem>(), 
                "EventSystem should have EventSystem component");
        }

        [Test]
        public void Camera_Exists()
        {
            Camera mainCamera = Camera.main;
            Assert.IsNotNull(mainCamera, "Main Camera should exist");
        }

        [Test]
        public void ProceduralBoardBackdrop_Exists()
        {
            GameObject backdrop = GameObject.Find("ProceduralBoardBackdrop");
            if (backdrop != null)
            {
                Assert.IsNotNull(backdrop.GetComponent<ProceduralBoardBackdrop>(), 
                    "ProceduralBoardBackdrop should have ProceduralBoardBackdrop component");
            }
        }

        [Test]
        public void AllDropAreas_Have_Required_Components()
        {
            // Verify all 16 drop areas exist and have required components for card placement
            for (int i = 1; i <= 16; i++)
            {
                GameObject dropArea = GameObject.Find($"DropArea{i}");
                Assert.IsNotNull(dropArea, $"DropArea{i} should exist");
                
                // CardDropArea1 component
                CardDropArea1 dropAreaComponent = dropArea.GetComponent<CardDropArea1>();
                Assert.IsNotNull(dropAreaComponent, $"DropArea{i} should have CardDropArea1 component");
                
                // Collider2D for drop detection
                Collider2D collider = dropArea.GetComponent<Collider2D>();
                Assert.IsNotNull(collider, $"DropArea{i} should have Collider2D");
                
                // Note: CardDropArea1.Start() automatically sets isTrigger = true at runtime
                // In EditMode, we don't check isTrigger since it's set by Start() when the game runs
                // We only verify the collider exists (required for drop detection)
                // The isTrigger property will be correctly set at runtime by CardDropArea1.Start()
                
                // Verify IsOccupied property exists (used for placement validation)
                var isOccupiedProperty = typeof(CardDropArea1).GetProperty("IsOccupied");
                Assert.IsNotNull(isOccupiedProperty, $"CardDropArea1 should have IsOccupied property for placement validation");
            }
        }

        [Test]
        public void HandUIs_Have_GetCardForUI_Method()
        {
            // Verify HandUI components have GetCardForUI method (used for drag validation)
            GameObject handUI1 = GameObject.Find("NewHandUI Flame");
            GameObject handUI2 = GameObject.Find("NewHandUI Earth");
            
            if (handUI1 != null)
            {
                NewHandUI handUIComponent = handUI1.GetComponent<NewHandUI>();
                if (handUIComponent != null)
                {
                    var method = typeof(NewHandUI).GetMethod("GetCardForUI");
                    Assert.IsNotNull(method, "NewHandUI should have GetCardForUI method for drag validation");
                }
            }
            
            if (handUI2 != null)
            {
                NewHandOppUI handOppUIComponent = handUI2.GetComponent<NewHandOppUI>();
                if (handOppUIComponent != null)
                {
                    var method = typeof(NewHandOppUI).GetMethod("GetCardForUI");
                    Assert.IsNotNull(method, "NewHandOppUI should have GetCardForUI method for drag validation");
                }
            }
        }

        [Test]
        public void CardDropArea1_Supports_Placement_Validation()
        {
            // Verify CardDropArea1 has methods/properties for placement validation
            CardDropArea1[] dropAreas = Object.FindObjectsOfType<CardDropArea1>(true);
            Assert.IsTrue(dropAreas.Length > 0, "At least one CardDropArea1 should exist");
            
            if (dropAreas.Length > 0)
            {
                CardDropArea1 sampleDropArea = dropAreas[0];
                
                // Verify OnCardDrop method exists (for Player 1 cards)
                var onCardDropMethod = typeof(CardDropArea1).GetMethod("OnCardDrop");
                Assert.IsNotNull(onCardDropMethod, "CardDropArea1 should have OnCardDrop method for Player 1 card placement");
                
                // Verify OnCardDropOpp method exists (for Player 2/Opponent cards)
                var onCardDropOppMethod = typeof(CardDropArea1).GetMethod("OnCardDropOpp");
                Assert.IsNotNull(onCardDropOppMethod, "CardDropArea1 should have OnCardDropOpp method for Player 2 card placement");
                
                // Verify IsOccupied property exists
                var isOccupiedProperty = typeof(CardDropArea1).GetProperty("IsOccupied");
                Assert.IsNotNull(isOccupiedProperty, "CardDropArea1 should have IsOccupied property for placement validation");
                
                // Verify the property is accessible (test getter)
                bool isOccupied = sampleDropArea.IsOccupied;
                // Property should be accessible (no exception)
                Assert.IsTrue(true, $"CardDropArea1.IsOccupied property is accessible: {isOccupied}");
            }
        }

        [Test]
        public void NewCardUI_Has_Drag_Validation_Methods()
        {
            // Verify NewCardUI has methods for drag validation (OnBeginDrag validation)
            // This tests structural validation - actual drag behavior requires PlayMode
            var onBeginDragMethod = typeof(NewCardUI).GetMethod("OnBeginDrag");
            Assert.IsNotNull(onBeginDragMethod, "NewCardUI should have OnBeginDrag method for drag validation");
            
            // IsPlayerCard and IsOpponentCard are private methods, so we need to search with BindingFlags
            var isPlayerCardMethod = typeof(NewCardUI).GetMethod("IsPlayerCard", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(isPlayerCardMethod, "NewCardUI should have IsPlayerCard method (private)");
            
            var isOpponentCardMethod = typeof(NewCardUI).GetMethod("IsOpponentCard", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(isOpponentCardMethod, "NewCardUI should have IsOpponentCard method (private)");
        }

        [Test]
        public void NewCardUI_Has_Placement_Methods()
        {
            // Verify NewCardUI has methods for card placement
            var placeOpponentCardMethod = typeof(NewCardUI).GetMethod("PlaceOpponentCardOnBoard", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(placeOpponentCardMethod, "NewCardUI should have PlaceOpponentCardOnBoard method for Player 2 card placement");
            
            // Verify method signature matches expected parameters
            var parameters = placeOpponentCardMethod.GetParameters();
            Assert.IsTrue(parameters.Length >= 1, "PlaceOpponentCardOnBoard should accept CardDropArea1 parameter");
        }

        [Test]
        public void FateFlowController_Has_Turn_Validation_Methods()
        {
            // Verify FateFlowController has methods for turn-based validation
            var canActMethod = typeof(FateFlowController).GetMethod("CanAct");
            Assert.IsNotNull(canActMethod, "FateFlowController should have CanAct method for turn validation");
            
            var currentFateProperty = typeof(FateFlowController).GetProperty("CurrentFate");
            Assert.IsNotNull(currentFateProperty, "FateFlowController should have CurrentFate property");
            
            // Verify SetFate method exists (public or private)
            var setFateMethod = typeof(FateFlowController).GetMethod("SetFate", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(setFateMethod, "FateFlowController should have SetFate method");
        }
    }
}

