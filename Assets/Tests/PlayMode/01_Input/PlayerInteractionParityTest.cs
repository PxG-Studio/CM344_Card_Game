using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using CardGame.Core;

namespace CardGame.Tests
{
    /// <summary>
    /// Comparison runner that traces Player 1 and Player 2 input paths and reports exact divergences.
    /// </summary>
    public class PlayerInteractionParityTest
    {
        /// <summary>
        /// Input trace data structure
        /// </summary>
        public class InputTrace
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

        /// <summary>
        /// Traces the complete input path for a GameObject
        /// </summary>
        public InputTrace TraceInputPath(GameObject obj)
        {
            InputTrace trace = new InputTrace();
            
            // Camera
            Camera camera = Camera.main;
            if (camera == null)
            {
                Camera[] cameras = Object.FindObjectsOfType<Camera>();
                if (cameras.Length > 0)
                {
                    camera = cameras[0];
                }
            }
            trace.cameraName = camera != null ? camera.name : "NULL";
            
            // Layer
            trace.layer = obj.layer;
            trace.layerName = LayerMask.LayerToName(obj.layer);
            
            // Canvas
            Canvas canvas = obj.GetComponentInParent<Canvas>();
            trace.canvasName = canvas != null ? canvas.name : "NULL";
            if (canvas != null)
            {
                trace.sortingLayerName = canvas.sortingLayerName;
                trace.sortingOrder = canvas.sortingOrder;
            }
            else
            {
                Renderer renderer = obj.GetComponent<Renderer>();
                if (renderer != null)
                {
                    trace.sortingLayerName = renderer.sortingLayerName;
                    trace.sortingOrder = renderer.sortingOrder;
                }
            }
            
            // Raycast Target
            Graphic graphic = obj.GetComponent<Graphic>();
            trace.raycastTarget = graphic != null && graphic.raycastTarget;
            
            // Collider
            Collider2D collider = obj.GetComponent<Collider2D>();
            trace.hasCollider = collider != null;
            trace.colliderType = collider != null ? collider.GetType().Name : "NULL";
            
            // Canvas Group
            CanvasGroup canvasGroup = obj.GetComponent<CanvasGroup>();
            trace.canvasGroupInteractable = canvasGroup != null && canvasGroup.interactable;
            trace.canvasGroupBlocksRaycasts = canvasGroup != null && canvasGroup.blocksRaycasts;
            
            // EventSystem Module
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem != null)
            {
                var modules = eventSystem.GetComponents<BaseInputModule>();
                if (modules.Length > 0)
                {
                    trace.eventSystemModule = modules[0].GetType().Name;
                }
            }
            
            // Positions
            trace.worldPosition = obj.transform.position;
            if (camera != null)
            {
                trace.screenPosition = camera.WorldToScreenPoint(obj.transform.position);
            }
            
            return trace;
        }

        /// <summary>
        /// Compares two input traces and returns a list of differences
        /// </summary>
        public List<string> CompareTraces(InputTrace p1Trace, InputTrace p2Trace)
        {
            List<string> differences = new List<string>();
            
            if (p1Trace.cameraName != p2Trace.cameraName)
            {
                differences.Add($"Camera mismatch: P1 uses '{p1Trace.cameraName}', P2 uses '{p2Trace.cameraName}'");
            }
            
            if (p1Trace.layer != p2Trace.layer)
            {
                differences.Add($"Layer mismatch: P1 is '{p1Trace.layerName}' ({p1Trace.layer}), P2 is '{p2Trace.layerName}' ({p2Trace.layer})");
            }
            
            if (p1Trace.canvasName != p2Trace.canvasName)
            {
                differences.Add($"Canvas mismatch: P1 uses '{p1Trace.canvasName}', P2 uses '{p2Trace.canvasName}'");
            }
            
            if (p1Trace.sortingLayerName != p2Trace.sortingLayerName)
            {
                differences.Add($"Sorting Layer mismatch: P1 is '{p1Trace.sortingLayerName}', P2 is '{p2Trace.sortingLayerName}'");
            }
            
            if (p1Trace.sortingOrder != p2Trace.sortingOrder)
            {
                differences.Add($"Sorting Order mismatch: P1 is {p1Trace.sortingOrder}, P2 is {p2Trace.sortingOrder}");
            }
            
            if (p1Trace.raycastTarget != p2Trace.raycastTarget)
            {
                differences.Add($"Raycast Target mismatch: P1 is {p1Trace.raycastTarget}, P2 is {p2Trace.raycastTarget}");
            }
            
            if (p1Trace.hasCollider != p2Trace.hasCollider)
            {
                differences.Add($"Collider mismatch: P1 has {(p1Trace.hasCollider ? p1Trace.colliderType : "none")}, P2 has {(p2Trace.hasCollider ? p2Trace.colliderType : "none")}");
            }
            
            if (p1Trace.canvasGroupInteractable != p2Trace.canvasGroupInteractable)
            {
                differences.Add($"CanvasGroup Interactable mismatch: P1 is {p1Trace.canvasGroupInteractable}, P2 is {p2Trace.canvasGroupInteractable}");
            }
            
            if (p1Trace.canvasGroupBlocksRaycasts != p2Trace.canvasGroupBlocksRaycasts)
            {
                differences.Add($"CanvasGroup BlocksRaycasts mismatch: P1 is {p1Trace.canvasGroupBlocksRaycasts}, P2 is {p2Trace.canvasGroupBlocksRaycasts}");
            }
            
            if (p1Trace.eventSystemModule != p2Trace.eventSystemModule)
            {
                differences.Add($"EventSystem Module mismatch: P1 uses '{p1Trace.eventSystemModule}', P2 uses '{p2Trace.eventSystemModule}'");
            }
            
            return differences;
        }
    }
}

