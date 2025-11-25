using UnityEngine;

namespace CardGame.UI
{
    /// <summary>
    /// Static entry point for the Delta Marker System.
    /// Provides convenient methods to show delta popups anywhere in the game.
    /// Uses a lazy-loaded singleton pattern for efficient access.
    /// </summary>
    public static class DeltaMarkerSystem
    {
        private static DeltaMarkerEmitter cachedEmitter;
        private static bool hasSearchedForEmitter = false;
        
        /// <summary>
        /// Shows a delta marker at the specified transform's position.
        /// Automatically determines conquer (+1) or raze (-1) based on sign.
        /// </summary>
        /// <param name="deltaValue">Territory influence delta (positive = conquer, negative = raze)</param>
        /// <param name="targetTransform">Transform to position the popup at</param>
        public static void ShowDelta(int deltaValue, Transform targetTransform)
        {
            if (targetTransform == null)
            {
                Debug.LogWarning("[DeltaMarkerSystem] Target transform is null! Cannot show delta marker.");
                return;
            }
            
            ShowDeltaAtPosition(deltaValue, targetTransform.position);
        }
        
        /// <summary>
        /// Shows a delta marker at the specified world position.
        /// Automatically determines conquer (+1) or raze (-1) based on sign.
        /// </summary>
        /// <param name="deltaValue">Territory influence delta (positive = conquer, negative = raze)</param>
        /// <param name="worldPosition">World position to spawn the popup at</param>
        public static void ShowDeltaAtPosition(int deltaValue, Vector3 worldPosition)
        {
            DeltaMarkerEmitter emitter = GetEmitter();
            if (emitter == null)
            {
                Debug.LogWarning("[DeltaMarkerSystem] DeltaMarkerEmitter not found in scene! Delta marker will not be shown. " +
                    "Please add a DeltaMarkerEmitter component to a GameObject in your scene.");
                return;
            }
            
            emitter.SpawnDelta(deltaValue, worldPosition);
        }
        
        /// <summary>
        /// Shows a delta marker at the specified UI/screen position.
        /// </summary>
        /// <param name="deltaValue">Territory influence delta (positive = conquer, negative = raze)</param>
        /// <param name="uiPosition">Screen/UI position to spawn at</param>
        public static void ShowDeltaAtUI(int deltaValue, Vector3 uiPosition)
        {
            DeltaMarkerEmitter emitter = GetEmitter();
            if (emitter == null)
            {
                Debug.LogWarning("[DeltaMarkerSystem] DeltaMarkerEmitter not found in scene! Delta marker will not be shown.");
                return;
            }
            
            emitter.SpawnDeltaAtUI(deltaValue, uiPosition);
        }
        
        /// <summary>
        /// Gets the DeltaMarkerEmitter instance using lazy-loaded singleton pattern.
        /// Caches the result to avoid repeated FindObjectOfType calls.
        /// </summary>
        /// <returns>The DeltaMarkerEmitter instance, or null if not found</returns>
        private static DeltaMarkerEmitter GetEmitter()
        {
            // Return cached instance if valid
            if (cachedEmitter != null)
            {
                return cachedEmitter;
            }
            
            // Only search once per scene
            if (hasSearchedForEmitter)
            {
                return null;
            }
            
            // Search for emitter
            hasSearchedForEmitter = true;
            cachedEmitter = Object.FindObjectOfType<DeltaMarkerEmitter>();
            
            if (cachedEmitter == null)
            {
                Debug.LogWarning("[DeltaMarkerSystem] No DeltaMarkerEmitter found in scene. " +
                    "Please add a DeltaMarkerEmitter component to a GameObject and assign the config and prefab.");
            }
            else
            {
                Debug.Log($"[DeltaMarkerSystem] Found DeltaMarkerEmitter on '{cachedEmitter.gameObject.name}'");
            }
            
            return cachedEmitter;
        }
        
        /// <summary>
        /// Clears the cached emitter reference.
        /// Call this when the scene changes or emitter is destroyed.
        /// </summary>
        public static void ClearCache()
        {
            cachedEmitter = null;
            hasSearchedForEmitter = false;
        }
    }
}

