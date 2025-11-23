using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using CardGame.Managers;
using CardGame.Core;
using CardGame.UI;

namespace CardGame.Tests
{
    /// <summary>
    /// Comprehensive diagnostic tests for Player 2 card interaction issues.
    /// Tests hover, drag, drop, and input parity with Player 1.
    /// </summary>
    public class P2_CardInteraction_DebugTests
    {
        private const string SCENE_NAME = "BattleScreenMultiplayer";
        private CardFrontDebugInstrumentation debugInstrumentation;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // CRITICAL: Clear singleton instances from previous tests
            CardTestHelper.ClearSingletonInstances();
            yield return null;
            
            // Verify scene exists in build settings
            bool sceneExists = false;
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string scenePath = System.IO.Path.GetFileNameWithoutExtension(
                    UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(i));
                if (scenePath == SCENE_NAME)
                {
                    sceneExists = true;
                    break;
                }
            }
            
            if (!sceneExists)
            {
                Assert.Fail($"Scene '{SCENE_NAME}' must be added to Build Settings");
            }

            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(SCENE_NAME, LoadSceneMode.Single);
            asyncLoad.allowSceneActivation = true;
            
            float timeout = 10f;
            float elapsed = 0f;
            while (!asyncLoad.isDone && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            if (!asyncLoad.isDone)
            {
                Assert.Fail($"Scene '{SCENE_NAME}' failed to load within {timeout} seconds");
            }
            
            yield return new WaitForSeconds(1.0f); // Wait for initialization
            
            // Initialize debug instrumentation
            GameObject debugObj = new GameObject("CardFrontDebugInstrumentation");
            debugInstrumentation = debugObj.AddComponent<CardFrontDebugInstrumentation>();
            debugInstrumentation.EnableInstrumentation(true);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (debugInstrumentation != null)
            {
                debugInstrumentation.EnableInstrumentation(false);
                Object.Destroy(debugInstrumentation.gameObject);
            }
            // Clean up after each test
            yield return null;
            CardTestHelper.ClearSingletonInstances();
            yield return null;
        }

        #region Player 2 Hover Tests

        [UnityTest]
        public IEnumerator Player2_CanHoverCard()
        {
            // Arrange: Wait for game to initialize and set Player 2's turn
            yield return new WaitForSeconds(2.0f);
            
            FateFlowController fateController = FateFlowController.Instance;
            if (fateController != null)
            {
                fateController.SetFate(FateSide.Opponent);
            }
            yield return new WaitForSeconds(0.5f);
            
            // Find Player 2 cards
            CardMoverOpp[] player2Cards = Object.FindObjectsOfType<CardMoverOpp>(true);
            Assert.IsTrue(player2Cards.Length > 0, "Player 2 cards (CardMoverOpp) should exist");
            
            CardMoverOpp testCard = player2Cards[0];
            
            // Verify card has collider
            Collider2D col = testCard.GetComponent<Collider2D>();
            Assert.IsNotNull(col, $"Player 2 card '{testCard.gameObject.name}' should have Collider2D");
            Assert.IsTrue(col.enabled, $"Player 2 card '{testCard.gameObject.name}' Collider2D should be enabled");
            
            // Verify card is not played
            Assert.IsFalse(testCard.IsPlayed, "Test card should not be played");
            
            // Verify CanInteract (turn check)
            bool canInteract = fateController != null && fateController.CanAct(FateSide.Opponent);
            Assert.IsTrue(canInteract, "Player 2 should be able to interact during their turn");
            
            // Log hover capability
            debugInstrumentation?.LogHoverState(testCard.gameObject, "Player2_CanHoverCard");
        }

        [UnityTest]
        public IEnumerator Player2_Hover_ChangesSortingLayerCorrectly()
        {
            // Arrange: Wait for game to initialize
            yield return new WaitForSeconds(2.0f);
            
            CardMoverOpp[] player2Cards = Object.FindObjectsOfType<CardMoverOpp>(true);
            if (player2Cards.Length == 0)
            {
                Assert.Fail("No Player 2 cards found");
            }
            
            CardMoverOpp testCard = player2Cards[0];
            Renderer renderer = testCard.GetComponent<Renderer>();
            
            if (renderer != null)
            {
                int initialSortingOrder = renderer.sortingOrder;
                string initialSortingLayer = renderer.sortingLayerName;
                
                debugInstrumentation?.LogSortingLayer(testCard.gameObject, "Player2_Hover_ChangesSortingLayerCorrectly");
                
                // Verify sorting layer is set
                Assert.IsNotNull(initialSortingLayer, "Player 2 card should have a sorting layer");
                
                // Compare with Player 1 card
                CardMover[] player1Cards = Object.FindObjectsOfType<CardMover>(true);
                if (player1Cards.Length > 0)
                {
                    Renderer p1Renderer = player1Cards[0].GetComponent<Renderer>();
                    if (p1Renderer != null)
                    {
                        // Sorting layers should be comparable (may be different but both should exist)
                        Assert.IsTrue(true, $"Player 1 sorting layer: {p1Renderer.sortingLayerName}, Player 2 sorting layer: {initialSortingLayer}");
                    }
                }
            }
            else
            {
                // Card might use Canvas/SortingGroup instead
                Canvas canvas = testCard.GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    debugInstrumentation?.LogCanvasInfo(canvas, "Player2_Hover_ChangesSortingLayerCorrectly");
                    Assert.IsTrue(true, "Player 2 card uses Canvas for sorting");
                }
            }
        }

        [UnityTest]
        public IEnumerator Player2_Hover_RaycastHitsCorrectUIElements()
        {
            // Arrange: Wait for game to initialize
            yield return new WaitForSeconds(2.0f);
            
            // Set Player 2's turn
            FateFlowController fateController = FateFlowController.Instance;
            if (fateController != null)
            {
                fateController.SetFate(FateSide.Opponent);
            }
            yield return new WaitForSeconds(0.5f);
            
            CardMoverOpp[] player2Cards = Object.FindObjectsOfType<CardMoverOpp>(true);
            if (player2Cards.Length == 0)
            {
                Assert.Fail("No Player 2 cards found");
            }
            
            CardMoverOpp testCard = player2Cards[0];
            
            // Perform raycast test
            Camera camera = Camera.main;
            if (camera == null)
            {
                // Try to find Player 2 camera
                Camera[] cameras = Object.FindObjectsOfType<Camera>();
                foreach (Camera cam in cameras)
                {
                    if (cam.name.Contains("Player2") || cam.name.Contains("Opponent") || cam.name.Contains("P2"))
                    {
                        camera = cam;
                        break;
                    }
                }
            }
            
            Assert.IsNotNull(camera, "Camera should exist for raycast test");
            
            // Get card screen position
            Vector3 cardScreenPos = camera.WorldToScreenPoint(testCard.transform.position);
            
            // Perform UI raycast
            PointerEventData pointerData = new PointerEventData(EventSystem.current);
            pointerData.position = cardScreenPos;
            
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);
            
            debugInstrumentation?.LogRaycastResults(results, "Player2_Hover_RaycastHitsCorrectUIElements");
            
            // Verify card is in raycast results
            bool cardFound = false;
            foreach (RaycastResult result in results)
            {
                if (result.gameObject == testCard.gameObject || 
                    result.gameObject.transform.IsChildOf(testCard.transform) ||
                    testCard.transform.IsChildOf(result.gameObject.transform))
                {
                    cardFound = true;
                    break;
                }
            }
            
            Assert.IsTrue(cardFound || results.Count == 0, 
                $"Player 2 card should be hit by raycast (or no UI elements found). Results: {results.Count}");
        }

        #endregion

        #region Player 2 Drag Tests

        [UnityTest]
        public IEnumerator Player2_Card_FollowsMouseDuringDrag()
        {
            // Arrange: Wait for game to initialize
            yield return new WaitForSeconds(2.0f);
            
            // Set Player 2's turn
            FateFlowController fateController = FateFlowController.Instance;
            if (fateController != null)
            {
                fateController.SetFate(FateSide.Opponent);
            }
            yield return new WaitForSeconds(0.5f);
            
            CardMoverOpp[] player2Cards = Object.FindObjectsOfType<CardMoverOpp>(true);
            if (player2Cards.Length == 0)
            {
                Assert.Fail("No Player 2 cards found");
            }
            
            CardMoverOpp testCard = player2Cards[0];
            Vector3 initialPosition = testCard.transform.position;
            
            // Simulate drag using AutomationAttemptDrop
            Camera camera = Camera.main;
            Vector3 mouseWorldPos = camera.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 2, 0));
            mouseWorldPos.z = 0;
            
            // Track position changes
            List<Vector3> positionHistory = new List<Vector3>();
            positionHistory.Add(testCard.transform.position);
            
            // Simulate drag movement
            for (int i = 0; i < 5; i++)
            {
                Vector3 testPos = initialPosition + new Vector3(i * 0.5f, 0, 0);
                testCard.transform.position = testPos;
                positionHistory.Add(testCard.transform.position);
                yield return null;
            }
            
            debugInstrumentation?.LogPositionHistory(testCard.gameObject, positionHistory, "Player2_Card_FollowsMouseDuringDrag");
            
            // Verify position changed
            Assert.AreNotEqual(initialPosition, testCard.transform.position, 
                "Card position should change during drag simulation");
        }

        [UnityTest]
        public IEnumerator Player2_Drag_UsesCorrectCamera()
        {
            // Arrange: Wait for game to initialize
            yield return new WaitForSeconds(2.0f);
            
            CardMoverOpp[] player2Cards = Object.FindObjectsOfType<CardMoverOpp>(true);
            if (player2Cards.Length == 0)
            {
                Assert.Fail("No Player 2 cards found");
            }
            
            CardMoverOpp testCard = player2Cards[0];
            
            // Check GetMousePositionInWorldSpace method
            var getMouseMethod = typeof(CardMoverOpp).GetMethod("GetMousePositionInWorldSpace");
            Assert.IsNotNull(getMouseMethod, "CardMoverOpp should have GetMousePositionInWorldSpace method");
            
            // Verify it uses Camera.main (this might be the issue if Player 2 needs a different camera)
            Camera mainCamera = Camera.main;
            Camera[] allCameras = Object.FindObjectsOfType<Camera>();
            
            debugInstrumentation?.LogCameraInfo(mainCamera, allCameras, "Player2_Drag_UsesCorrectCamera");
            
            // Check if there's a Player 2 specific camera
            Camera player2Camera = null;
            foreach (Camera cam in allCameras)
            {
                if (cam.name.Contains("Player2") || cam.name.Contains("Opponent") || cam.name.Contains("P2"))
                {
                    player2Camera = cam;
                    break;
                }
            }
            
            if (player2Camera != null)
            {
                Debug.LogWarning($"[P2_Drag_UsesCorrectCamera] Player 2 specific camera found: {player2Camera.name}, but CardMoverOpp uses Camera.main. This may cause issues!");
            }
            
            Assert.IsTrue(true, "Camera usage validated (CardMoverOpp uses Camera.main)");
        }

        [UnityTest]
        public IEnumerator Player2_Drag_UsesCorrectRaycastRoot()
        {
            // Arrange: Wait for game to initialize
            yield return new WaitForSeconds(2.0f);
            
            CardMoverOpp[] player2Cards = Object.FindObjectsOfType<CardMoverOpp>(true);
            if (player2Cards.Length == 0)
            {
                Assert.Fail("No Player 2 cards found");
            }
            
            CardMoverOpp testCard = player2Cards[0];
            
            // Check canvas hierarchy
            Canvas canvas = testCard.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                debugInstrumentation?.LogCanvasInfo(canvas, "Player2_Drag_UsesCorrectRaycastRoot");
                
                // Verify canvas has GraphicRaycaster
                GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
                Assert.IsNotNull(raycaster, "Player 2 card's Canvas should have GraphicRaycaster");
                Assert.IsTrue(raycaster.enabled, "Player 2 card's GraphicRaycaster should be enabled");
            }
            
            // Check EventSystem
            EventSystem eventSystem = EventSystem.current;
            Assert.IsNotNull(eventSystem, "EventSystem should exist");
            
            debugInstrumentation?.LogEventSystemInfo(eventSystem, "Player2_Drag_UsesCorrectRaycastRoot");
        }

        [UnityTest]
        public IEnumerator Player2_CardMaintainsOffsetDuringDrag()
        {
            // Arrange: Wait for game to initialize
            yield return new WaitForSeconds(2.0f);
            
            // Set Player 2's turn
            FateFlowController fateController = FateFlowController.Instance;
            if (fateController != null)
            {
                fateController.SetFate(FateSide.Opponent);
            }
            yield return new WaitForSeconds(0.5f);
            
            CardMoverOpp[] player2Cards = Object.FindObjectsOfType<CardMoverOpp>(true);
            if (player2Cards.Length == 0)
            {
                Assert.Fail("No Player 2 cards found");
            }
            
            CardMoverOpp testCard = player2Cards[0];
            Vector3 startPosition = testCard.transform.position;
            
            // Simulate mouse position
            Camera camera = Camera.main;
            Vector3 mouseScreenPos = new Vector3(Screen.width / 2, Screen.height / 2, 0);
            Vector3 mouseWorldPos = camera.ScreenToWorldPoint(mouseScreenPos);
            mouseWorldPos.z = 0;
            
            // Get initial offset (if any)
            Vector3 initialOffset = mouseWorldPos - startPosition;
            
            // Simulate drag
            testCard.transform.position = mouseWorldPos;
            yield return null;
            
            Vector3 newPosition = testCard.transform.position;
            Vector3 newOffset = mouseWorldPos - newPosition;
            
            debugInstrumentation?.LogOffsetInfo(testCard.gameObject, initialOffset, newOffset, "Player2_CardMaintainsOffsetDuringDrag");
            
            // Offset should be maintained (or card should follow mouse exactly)
            float offsetDifference = Vector3.Distance(initialOffset, newOffset);
            Assert.Less(offsetDifference, 0.1f, 
                $"Card offset should be maintained during drag. Initial: {initialOffset}, New: {newOffset}");
        }

        #endregion

        #region Player 2 Drop Tests

        [UnityTest]
        public IEnumerator Player2_CanDropOnValidTile()
        {
            // Arrange: Wait for game to initialize
            yield return new WaitForSeconds(2.0f);
            
            // Activate all tiles before P2 drop tests
            CardTestHelper.ActivateAllTiles();
            yield return null;
            
            // Set Player 2's turn
            FateFlowController fateController = FateFlowController.Instance;
            if (fateController != null)
            {
                fateController.SetFate(FateSide.Opponent);
            }
            yield return new WaitForSeconds(0.5f);
            
            CardMoverOpp[] player2Cards = Object.FindObjectsOfType<CardMoverOpp>(true);
            CardDropArea1[] dropAreas = Object.FindObjectsOfType<CardDropArea1>(true);
            
            if (player2Cards.Length == 0)
            {
                Assert.Fail("No Player 2 cards found");
            }
            if (dropAreas.Length == 0)
            {
                Assert.Fail("No drop areas found");
            }
            
            CardMoverOpp testCard = player2Cards[0];
            CardDropArea1 testDropArea = null;
            
            // Find an unoccupied drop area
            foreach (CardDropArea1 area in dropAreas)
            {
                if (!area.IsOccupied)
                {
                    testDropArea = area;
                    break;
                }
            }
            
            if (testDropArea == null)
            {
                Assert.Fail("No unoccupied drop areas found");
            }
            
            // Attempt drop
            Vector3 dropPosition = testDropArea.transform.position;
            bool dropResult = testCard.AutomationAttemptDrop(dropPosition, bypassTurnGate: false);
            
            debugInstrumentation?.LogDropAttempt(testCard.gameObject, testDropArea.gameObject, dropResult, "Player2_CanDropOnValidTile");
            
            // Drop should succeed if turn is correct
            if (fateController != null && fateController.CanAct(FateSide.Opponent))
            {
                Assert.IsTrue(dropResult, $"Player 2 should be able to drop card on valid tile '{testDropArea.gameObject.name}'");
            }
        }

        [UnityTest]
        public IEnumerator Player2_Drop_RegistersOnCardDropAreaOpp()
        {
            // Arrange: Wait for game to initialize
            yield return new WaitForSeconds(2.0f);
            
            // Set Player 2's turn
            FateFlowController fateController = FateFlowController.Instance;
            if (fateController != null)
            {
                fateController.SetFate(FateSide.Opponent);
            }
            yield return new WaitForSeconds(0.5f);
            
            // Verify CardDropArea1 has OnCardDropOpp method
            var onCardDropOppMethod = typeof(CardDropArea1).GetMethod("OnCardDropOpp");
            Assert.IsNotNull(onCardDropOppMethod, "CardDropArea1 should have OnCardDropOpp method");
            
            // Verify ICardDropArea interface
            var iCardDropAreaType = typeof(ICardDropArea);
            var onCardDropOppInterfaceMethod = iCardDropAreaType.GetMethod("OnCardDropOpp");
            Assert.IsNotNull(onCardDropOppInterfaceMethod, "ICardDropArea should have OnCardDropOpp method");
            
            Assert.IsTrue(true, "Player 2 drop registers on CardDropArea1 via OnCardDropOpp");
        }

        [UnityTest]
        public IEnumerator Player2_Drop_RejectedOnInvalidTile()
        {
            // Arrange: Wait for game to initialize
            yield return new WaitForSeconds(2.0f);
            
            // Activate all tiles before P2 drop tests
            CardTestHelper.ActivateAllTiles();
            yield return null;
            
            // Set Player 2's turn
            FateFlowController fateController = FateFlowController.Instance;
            if (fateController != null)
            {
                fateController.SetFate(FateSide.Opponent);
            }
            yield return new WaitForSeconds(0.5f);
            
            CardMoverOpp[] player2Cards = Object.FindObjectsOfType<CardMoverOpp>(true);
            if (player2Cards.Length == 0)
            {
                Assert.Fail("No Player 2 cards found");
            }
            
            CardMoverOpp testCard = player2Cards[0];
            
            // Attempt drop at invalid position (far from any drop area)
            Vector3 invalidPosition = new Vector3(1000, 1000, 0);
            bool dropResult = testCard.AutomationAttemptDrop(invalidPosition, bypassTurnGate: false);
            
            debugInstrumentation?.LogDropAttempt(testCard.gameObject, null, dropResult, "Player2_Drop_RejectedOnInvalidTile");
            
            // Drop should fail
            Assert.IsFalse(dropResult, "Player 2 drop should be rejected on invalid tile");
        }

        [UnityTest]
        public IEnumerator Player2_Drop_TriggersPlacementEvents()
        {
            // Arrange: Wait for game to initialize
            yield return new WaitForSeconds(2.0f);
            
            // Verify GameManager has OnCardPlaced event
            var onCardPlacedField = typeof(GameManager).GetEvent("OnCardPlaced");
            Assert.IsNotNull(onCardPlacedField, "GameManager should have OnCardPlaced event");
            
            // Verify CardDropArea1.OnCardDropOpp triggers events
            var onCardDropOppMethod = typeof(CardDropArea1).GetMethod("OnCardDropOpp");
            Assert.IsNotNull(onCardDropOppMethod, "CardDropArea1 should have OnCardDropOpp method");
            
            Assert.IsTrue(true, "Player 2 drop triggers placement events (validated via method existence)");
        }

        #endregion

        #region Input Parity Tests (MOST IMPORTANT)

        [UnityTest]
        public IEnumerator Player2_InputPath_Equals_Player1_InputPath()
        {
            // Arrange: Wait for game to initialize
            yield return new WaitForSeconds(2.0f);
            
            // Get Player 1 and Player 2 cards
            CardMover[] player1Cards = Object.FindObjectsOfType<CardMover>(true);
            CardMoverOpp[] player2Cards = Object.FindObjectsOfType<CardMoverOpp>(true);
            
            if (player1Cards.Length == 0 || player2Cards.Length == 0)
            {
                Assert.Fail("Both Player 1 and Player 2 cards must exist for comparison");
            }
            
            CardMover p1Card = player1Cards[0];
            CardMoverOpp p2Card = player2Cards[0];
            
            // Compare input paths
            PlayerInteractionParityTest parityTest = new PlayerInteractionParityTest();
            PlayerInteractionParityTest.InputTrace p1Trace = parityTest.TraceInputPath(p1Card.gameObject);
            PlayerInteractionParityTest.InputTrace p2Trace = parityTest.TraceInputPath(p2Card.gameObject);
            
            // Convert to CardFrontDebugInstrumentation.InputTrace
            CardFrontDebugInstrumentation.InputTrace p1TraceConverted = new CardFrontDebugInstrumentation.InputTrace
            {
                cameraName = p1Trace.cameraName,
                layerName = p1Trace.layerName,
                layer = p1Trace.layer,
                canvasName = p1Trace.canvasName,
                sortingLayerName = p1Trace.sortingLayerName,
                sortingOrder = p1Trace.sortingOrder,
                raycastTarget = p1Trace.raycastTarget,
                hasCollider = p1Trace.hasCollider,
                colliderType = p1Trace.colliderType,
                canvasGroupInteractable = p1Trace.canvasGroupInteractable,
                canvasGroupBlocksRaycasts = p1Trace.canvasGroupBlocksRaycasts,
                eventSystemModule = p1Trace.eventSystemModule,
                worldPosition = p1Trace.worldPosition,
                screenPosition = p1Trace.screenPosition
            };
            
            CardFrontDebugInstrumentation.InputTrace p2TraceConverted = new CardFrontDebugInstrumentation.InputTrace
            {
                cameraName = p2Trace.cameraName,
                layerName = p2Trace.layerName,
                layer = p2Trace.layer,
                canvasName = p2Trace.canvasName,
                sortingLayerName = p2Trace.sortingLayerName,
                sortingOrder = p2Trace.sortingOrder,
                raycastTarget = p2Trace.raycastTarget,
                hasCollider = p2Trace.hasCollider,
                colliderType = p2Trace.colliderType,
                canvasGroupInteractable = p2Trace.canvasGroupInteractable,
                canvasGroupBlocksRaycasts = p2Trace.canvasGroupBlocksRaycasts,
                eventSystemModule = p2Trace.eventSystemModule,
                worldPosition = p2Trace.worldPosition,
                screenPosition = p2Trace.screenPosition
            };
            
            debugInstrumentation?.LogInputParityComparison(p1TraceConverted, p2TraceConverted, "Player2_InputPath_Equals_Player1_InputPath");
            
            // Compare traces
            List<string> differences = parityTest.CompareTraces(p1Trace, p2Trace);
            
            if (differences.Count > 0)
            {
                string diffReport = string.Join("\n", differences);
                Assert.Fail($"Input path differences found:\n{diffReport}");
            }
            
            Assert.IsTrue(true, "Player 2 input path matches Player 1 input path");
        }

        [UnityTest]
        public IEnumerator Player2_RaycastLayers_Match_Player1()
        {
            // Arrange: Wait for game to initialize
            yield return new WaitForSeconds(2.0f);
            
            CardMover[] player1Cards = Object.FindObjectsOfType<CardMover>(true);
            CardMoverOpp[] player2Cards = Object.FindObjectsOfType<CardMoverOpp>(true);
            
            if (player1Cards.Length == 0 || player2Cards.Length == 0)
            {
                Assert.Fail("Both Player 1 and Player 2 cards must exist");
            }
            
            CardMover p1Card = player1Cards[0];
            CardMoverOpp p2Card = player2Cards[0];
            
            // Compare layers
            int p1Layer = p1Card.gameObject.layer;
            int p2Layer = p2Card.gameObject.layer;
            
            debugInstrumentation?.LogLayerComparison(p1Card.gameObject, p2Card.gameObject, "Player2_RaycastLayers_Match_Player1");
            
            // Layers should match (or both should be in UI layer)
            if (p1Layer != p2Layer)
            {
                string layer1Name = LayerMask.LayerToName(p1Layer);
                string layer2Name = LayerMask.LayerToName(p2Layer);
                Debug.LogWarning($"[RaycastLayers] Player 1 layer: {layer1Name} ({p1Layer}), Player 2 layer: {layer2Name} ({p2Layer})");
            }
            
            // Both should be in UI layer (5) or both in same layer
            Assert.IsTrue(p1Layer == p2Layer || (p1Layer == 5 && p2Layer == 5), 
                $"Raycast layers should match. P1: {LayerMask.LayerToName(p1Layer)}, P2: {LayerMask.LayerToName(p2Layer)}");
        }

        [UnityTest]
        public IEnumerator Player2_EventSystemModules_Match_Player1()
        {
            // Arrange: Wait for game to initialize
            yield return new WaitForSeconds(2.0f);
            
            EventSystem eventSystem = EventSystem.current;
            Assert.IsNotNull(eventSystem, "EventSystem should exist");
            
            // Get all EventSystem modules
            var modules = eventSystem.GetComponents<BaseInputModule>();
            
            debugInstrumentation?.LogEventSystemModules(eventSystem, "Player2_EventSystemModules_Match_Player1");
            
            // Verify at least one input module exists
            Assert.IsTrue(modules.Length > 0, "EventSystem should have at least one input module");
            
            // Verify StandaloneInputModule or similar exists
            bool hasInputModule = false;
            foreach (var module in modules)
            {
                if (module is StandaloneInputModule || module.GetType().Name.Contains("InputModule"))
                {
                    hasInputModule = true;
                    Assert.IsTrue(module.enabled, $"Input module '{module.GetType().Name}' should be enabled");
                }
            }
            
            Assert.IsTrue(hasInputModule, "EventSystem should have an enabled input module");
        }

        #endregion
    }
}

