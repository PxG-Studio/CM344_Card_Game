using UnityEngine;

namespace CardGame.UI
{
    /// <summary>
    /// Component responsible for spawning delta marker popups at specified positions.
    /// Handles world-space to screen-space conversion and prefab instantiation.
    /// </summary>
    public class DeltaMarkerEmitter : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("ScriptableObject configuration for delta markers")]
        [SerializeField] private DeltaMarkerConfig config;
        
        [Header("Prefab Reference")]
        [Tooltip("Prefab to instantiate for each delta popup")]
        [SerializeField] private GameObject deltaMarkerPrefab;
        
        [Header("Canvas Settings")]
        [Tooltip("Parent canvas to spawn popups under. If null, will auto-find HUDOverlayCanvas or GameplayCanvas")]
        [SerializeField] private Canvas parentCanvas;
        
        [Tooltip("Whether to use screen space overlay (true) or world space (false)")]
        [SerializeField] private bool useScreenSpace = true;
        
        private Camera mainCamera;
        
        private void Awake()
        {
            // Find main camera
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = FindObjectOfType<Camera>();
            }
            
            // Auto-find parent canvas if not assigned
            if (parentCanvas == null)
            {
                GameObject hudCanvas = GameObject.Find("HUDOverlayCanvas");
                if (hudCanvas != null)
                {
                    parentCanvas = hudCanvas.GetComponent<Canvas>();
                }
                
                if (parentCanvas == null)
                {
                    GameObject gameplayCanvas = GameObject.Find("GameplayCanvas");
                    if (gameplayCanvas != null)
                    {
                        parentCanvas = gameplayCanvas.GetComponent<Canvas>();
                    }
                }
                
                if (parentCanvas == null)
                {
                    // Find any Canvas with ScreenSpaceOverlay render mode
                    Canvas[] allCanvases = FindObjectsOfType<Canvas>();
                    foreach (Canvas canvas in allCanvases)
                    {
                        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                        {
                            parentCanvas = canvas;
                            break;
                        }
                    }
                }
            }
            
            // Validate configuration
            if (config == null)
            {
                Debug.LogWarning("[DeltaMarkerEmitter] No DeltaMarkerConfig assigned! Delta markers will not display correctly.");
            }
            
            if (deltaMarkerPrefab == null)
            {
                Debug.LogWarning("[DeltaMarkerEmitter] No deltaMarkerPrefab assigned! Cannot spawn delta markers.");
            }
        }
        
        /// <summary>
        /// Spawns a delta marker popup at the specified world position.
        /// Automatically determines conquer (+1) or raze (-1) based on sign.
        /// </summary>
        /// <param name="value">Delta value (positive = conquer, negative = raze)</param>
        /// <param name="worldPosition">World position to spawn the popup at</param>
        public void SpawnDelta(int value, Vector3 worldPosition)
        {
            if (config == null || deltaMarkerPrefab == null)
            {
                Debug.LogWarning("[DeltaMarkerEmitter] Cannot spawn delta marker - config or prefab is missing!");
                return;
            }
            
            // Determine color based on sign
            Color popupColor = value >= 0 ? config.ConquerColor : config.RazeColor;
            
            // Convert world position to screen/UI space
            Vector3 spawnPosition;
            Transform parentTransform = null;
            
            if (useScreenSpace && parentCanvas != null)
            {
                // Screen space overlay
                if (mainCamera != null)
                {
                    Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPosition);
                    spawnPosition = screenPos;
                    parentTransform = parentCanvas.transform;
                }
                else
                {
                    Debug.LogWarning("[DeltaMarkerEmitter] No camera found! Using world position directly.");
                    spawnPosition = worldPosition;
                }
            }
            else
            {
                // World space
                spawnPosition = worldPosition;
                parentTransform = transform; // Use emitter as parent if no canvas
            }
            
            // Instantiate prefab
            GameObject popupInstance = Instantiate(deltaMarkerPrefab, parentTransform);
            
            // Set position
            if (useScreenSpace && parentCanvas != null && mainCamera != null)
            {
                // For screen space, use RectTransform
                RectTransform rectTransform = popupInstance.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.position = spawnPosition;
                }
                else
                {
                    popupInstance.transform.position = spawnPosition;
                }
            }
            else
            {
                popupInstance.transform.position = spawnPosition;
            }
            
            // Initialize the popup
            DeltaMarkerPopup popup = popupInstance.GetComponent<DeltaMarkerPopup>();
            if (popup == null)
            {
                popup = popupInstance.GetComponentInChildren<DeltaMarkerPopup>();
            }
            
            if (popup != null)
            {
                popup.Initialize(value, popupColor, config);
            }
            else
            {
                Debug.LogError("[DeltaMarkerEmitter] DeltaMarkerPopup component not found on prefab! Ensure the prefab has DeltaMarkerPopup attached.");
                Destroy(popupInstance);
            }
        }
        
        /// <summary>
        /// Spawns a delta marker popup at a UI position (screen space).
        /// </summary>
        /// <param name="value">Delta value (positive = conquer, negative = raze)</param>
        /// <param name="uiPosition">Screen/UI position to spawn at</param>
        public void SpawnDeltaAtUI(int value, Vector3 uiPosition)
        {
            if (config == null || deltaMarkerPrefab == null)
            {
                Debug.LogWarning("[DeltaMarkerEmitter] Cannot spawn delta marker - config or prefab is missing!");
                return;
            }
            
            // Determine color based on sign
            Color popupColor = value >= 0 ? config.ConquerColor : config.RazeColor;
            
            // Ensure we have a canvas parent
            Transform parentTransform = parentCanvas != null ? parentCanvas.transform : transform;
            
            // Instantiate prefab
            GameObject popupInstance = Instantiate(deltaMarkerPrefab, parentTransform);
            
            // Set UI position
            RectTransform rectTransform = popupInstance.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.position = uiPosition;
            }
            else
            {
                popupInstance.transform.position = uiPosition;
            }
            
            // Initialize the popup
            DeltaMarkerPopup popup = popupInstance.GetComponent<DeltaMarkerPopup>();
            if (popup == null)
            {
                popup = popupInstance.GetComponentInChildren<DeltaMarkerPopup>();
            }
            
            if (popup != null)
            {
                popup.Initialize(value, popupColor, config);
            }
            else
            {
                Debug.LogError("[DeltaMarkerEmitter] DeltaMarkerPopup component not found on prefab!");
                Destroy(popupInstance);
            }
        }
    }
}

