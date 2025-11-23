using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CardGame.Tests
{
    /// <summary>
    /// Deep instrumentation mode for debugging Player 2 card interaction issues.
    /// Provides comprehensive logging for raycasts, cameras, EventSystem, sorting layers, etc.
    /// </summary>
    public class CardFrontDebugInstrumentation : MonoBehaviour
    {
        private bool isInstrumenting = false;
        private Dictionary<GameObject, List<Vector3>> positionHistory = new Dictionary<GameObject, List<Vector3>>();

        public void EnableInstrumentation(bool enable)
        {
            isInstrumenting = enable;
            if (enable)
            {
                Debug.Log("[CardFrontDebugInstrumentation] Deep instrumentation mode ENABLED");
            }
            else
            {
                Debug.Log("[CardFrontDebugInstrumentation] Deep instrumentation mode DISABLED");
            }
        }

        public void LogRaycastResults(List<RaycastResult> results, string context)
        {
            if (!isInstrumenting) return;
            
            Debug.Log($"[CardFrontDebugInstrumentation] {context} - Raycast Results: {results.Count} hits");
            for (int i = 0; i < results.Count; i++)
            {
                RaycastResult result = results[i];
                Debug.Log($"  [{i}] GameObject: {result.gameObject.name}, " +
                         $"Layer: {LayerMask.LayerToName(result.gameObject.layer)}, " +
                         $"Distance: {result.distance}, " +
                         $"ScreenPos: {result.screenPosition}, " +
                         $"WorldPos: {result.worldPosition}, " +
                         $"Module: {result.module?.GetType().Name}");
            }
        }

        public void LogCameraInfo(Camera mainCamera, Camera[] allCameras, string context)
        {
            if (!isInstrumenting) return;
            
            Debug.Log($"[CardFrontDebugInstrumentation] {context} - Camera Info:");
            Debug.Log($"  Main Camera: {(mainCamera != null ? mainCamera.name : "NULL")}");
            Debug.Log($"  Total Cameras: {allCameras.Length}");
            
            foreach (Camera cam in allCameras)
            {
                Debug.Log($"  Camera '{cam.name}': " +
                         $"Enabled: {cam.enabled}, " +
                         $"CullingMask: {cam.cullingMask}, " +
                         $"Depth: {cam.depth}, " +
                         $"Tag: {cam.tag}, " +
                         $"IsMain: {cam == mainCamera}");
            }
        }

        public void LogEventSystemInfo(EventSystem eventSystem, string context)
        {
            if (!isInstrumenting) return;
            
            Debug.Log($"[CardFrontDebugInstrumentation] {context} - EventSystem Info:");
            Debug.Log($"  EventSystem: {(eventSystem != null ? eventSystem.name : "NULL")}");
            
            if (eventSystem != null)
            {
                Debug.Log($"  Enabled: {eventSystem.enabled}");
                Debug.Log($"  Current Selected: {(eventSystem.currentSelectedGameObject != null ? eventSystem.currentSelectedGameObject.name : "NULL")}");
                
                var modules = eventSystem.GetComponents<BaseInputModule>();
                Debug.Log($"  Input Modules: {modules.Length}");
                foreach (var module in modules)
                {
                    Debug.Log($"    Module: {module.GetType().Name}, Enabled: {module.enabled}");
                }
            }
        }

        public void LogEventSystemModules(EventSystem eventSystem, string context)
        {
            if (!isInstrumenting) return;
            
            LogEventSystemInfo(eventSystem, context);
        }

        public void LogSortingLayer(GameObject obj, string context)
        {
            if (!isInstrumenting) return;
            
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                Debug.Log($"[CardFrontDebugInstrumentation] {context} - Sorting Layer:");
                Debug.Log($"  GameObject: {obj.name}");
                Debug.Log($"  Sorting Layer: {renderer.sortingLayerName} (ID: {renderer.sortingLayerID})");
                Debug.Log($"  Sorting Order: {renderer.sortingOrder}");
            }
            else
            {
                Canvas canvas = obj.GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    Debug.Log($"[CardFrontDebugInstrumentation] {context} - Canvas Sorting:");
                    Debug.Log($"  GameObject: {obj.name}");
                    Debug.Log($"  Canvas Sorting Layer: {canvas.sortingLayerName} (ID: {canvas.sortingLayerID})");
                    Debug.Log($"  Canvas Sorting Order: {canvas.sortingOrder}");
                    Debug.Log($"  Override Sorting: {canvas.overrideSorting}");
                }
            }
        }

        public void LogCanvasInfo(Canvas canvas, string context)
        {
            if (!isInstrumenting) return;
            
            Debug.Log($"[CardFrontDebugInstrumentation] {context} - Canvas Info:");
            Debug.Log($"  Canvas: {canvas.name}");
            Debug.Log($"  Render Mode: {canvas.renderMode}");
            Debug.Log($"  Sorting Layer: {canvas.sortingLayerName} (ID: {canvas.sortingLayerID})");
            Debug.Log($"  Sorting Order: {canvas.sortingOrder}");
            Debug.Log($"  Override Sorting: {canvas.overrideSorting}");
            Debug.Log($"  Pixel Perfect: {canvas.pixelPerfect}");
            
            GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster != null)
            {
                Debug.Log($"  GraphicRaycaster: Enabled={raycaster.enabled}, BlockingObjects={raycaster.blockingObjects}, BlockingMask={raycaster.blockingMask}");
            }
            else
            {
                Debug.LogWarning($"  GraphicRaycaster: MISSING!");
            }
            
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                Debug.Log($"  CanvasScaler: Enabled={scaler.enabled}, ScaleMode={scaler.uiScaleMode}");
            }
        }

        public void LogHoverState(GameObject obj, string context)
        {
            if (!isInstrumenting) return;
            
            Debug.Log($"[CardFrontDebugInstrumentation] {context} - Hover State:");
            Debug.Log($"  GameObject: {obj.name}");
            Debug.Log($"  Layer: {LayerMask.LayerToName(obj.layer)}");
            Debug.Log($"  Active: {obj.activeSelf}, ActiveInHierarchy: {obj.activeInHierarchy}");
            
            CanvasGroup canvasGroup = obj.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                Debug.Log($"  CanvasGroup: Interactable={canvasGroup.interactable}, BlocksRaycasts={canvasGroup.blocksRaycasts}, Alpha={canvasGroup.alpha}");
            }
            
            Graphic graphic = obj.GetComponent<Graphic>();
            if (graphic != null)
            {
                Debug.Log($"  Graphic: RaycastTarget={graphic.raycastTarget}, Color={graphic.color}");
            }
        }

        public void LogPositionHistory(GameObject obj, List<Vector3> positions, string context)
        {
            if (!isInstrumenting) return;
            
            Debug.Log($"[CardFrontDebugInstrumentation] {context} - Position History:");
            Debug.Log($"  GameObject: {obj.name}");
            Debug.Log($"  Positions: {positions.Count}");
            for (int i = 0; i < positions.Count; i++)
            {
                Debug.Log($"    [{i}] {positions[i]}");
            }
        }

        public void LogOffsetInfo(GameObject obj, Vector3 initialOffset, Vector3 newOffset, string context)
        {
            if (!isInstrumenting) return;
            
            Debug.Log($"[CardFrontDebugInstrumentation] {context} - Offset Info:");
            Debug.Log($"  GameObject: {obj.name}");
            Debug.Log($"  Initial Offset: {initialOffset}");
            Debug.Log($"  New Offset: {newOffset}");
            Debug.Log($"  Offset Difference: {Vector3.Distance(initialOffset, newOffset)}");
        }

        public void LogDropAttempt(GameObject card, GameObject dropArea, bool success, string context)
        {
            if (!isInstrumenting) return;
            
            Debug.Log($"[CardFrontDebugInstrumentation] {context} - Drop Attempt:");
            Debug.Log($"  Card: {card.name}");
            Debug.Log($"  Drop Area: {(dropArea != null ? dropArea.name : "NULL")}");
            Debug.Log($"  Success: {success}");
            Debug.Log($"  Card Position: {card.transform.position}");
            if (dropArea != null)
            {
                Debug.Log($"  Drop Area Position: {dropArea.transform.position}");
                Debug.Log($"  Distance: {Vector3.Distance(card.transform.position, dropArea.transform.position)}");
            }
        }

        public void LogLayerComparison(GameObject p1Obj, GameObject p2Obj, string context)
        {
            if (!isInstrumenting) return;
            
            Debug.Log($"[CardFrontDebugInstrumentation] {context} - Layer Comparison:");
            Debug.Log($"  Player 1: {p1Obj.name}, Layer: {LayerMask.LayerToName(p1Obj.layer)} ({p1Obj.layer})");
            Debug.Log($"  Player 2: {p2Obj.name}, Layer: {LayerMask.LayerToName(p2Obj.layer)} ({p2Obj.layer})");
            Debug.Log($"  Match: {p1Obj.layer == p2Obj.layer}");
        }

        // InputTrace struct for parity comparison (duplicated from PlayerInteractionParityTest to avoid assembly dependency)
        public struct InputTrace
        {
            public string cameraName;
            public string layerName;
            public int layer;
            public string canvasName;
            public string sortingLayerName;
            public int sortingOrder;
            public bool raycastTarget;
            public bool hasCollider;
            public string colliderType;
            public bool canvasGroupInteractable;
            public bool canvasGroupBlocksRaycasts;
            public string eventSystemModule;
            public Vector3 worldPosition;
            public Vector3 screenPosition;
        }

        public void LogInputParityComparison(InputTrace p1Trace, InputTrace p2Trace, string context)
        {
            if (!isInstrumenting) return;
            
            Debug.Log($"[CardFrontDebugInstrumentation] {context} - Input Parity Comparison:");
            Debug.Log($"  Player 1 Trace:");
            Debug.Log($"    Camera: {p1Trace.cameraName}");
            Debug.Log($"    Layer: {p1Trace.layerName}");
            Debug.Log($"    Canvas: {p1Trace.canvasName}");
            Debug.Log($"    Sorting Layer: {p1Trace.sortingLayerName}");
            Debug.Log($"    Raycast Target: {p1Trace.raycastTarget}");
            Debug.Log($"    Collider: {p1Trace.hasCollider}");
            
            Debug.Log($"  Player 2 Trace:");
            Debug.Log($"    Camera: {p2Trace.cameraName}");
            Debug.Log($"    Layer: {p2Trace.layerName}");
            Debug.Log($"    Canvas: {p2Trace.canvasName}");
            Debug.Log($"    Sorting Layer: {p2Trace.sortingLayerName}");
            Debug.Log($"    Raycast Target: {p2Trace.raycastTarget}");
            Debug.Log($"    Collider: {p2Trace.hasCollider}");
        }
    }
}

