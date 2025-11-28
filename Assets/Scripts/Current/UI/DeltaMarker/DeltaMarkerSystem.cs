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
            
            Debug.Log($"[DeltaMarkerSystem] ShowDelta called: delta={deltaValue}, target={targetTransform.name}, position={targetTransform.position}");
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
            
            Debug.Log($"[DeltaMarkerSystem] Spawning delta marker: delta={deltaValue}, position={worldPosition}, emitter={emitter.name}");
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
        /// Shows an alert marker (custom text, default "!") at a transform's position.
        /// </summary>
        public static void ShowAlert(Transform targetTransform, string text = "!")
        {
            if (targetTransform == null)
            {
                Debug.LogWarning("[DeltaMarkerSystem] Target transform is null! Cannot show alert marker.");
                return;
            }
            
            DeltaMarkerEmitter emitter = GetEmitter();
            if (emitter == null)
            {
                Debug.LogWarning("[DeltaMarkerSystem] DeltaMarkerEmitter not found in scene! Alert marker will not be shown.");
                return;
            }
            
            emitter.SpawnAlert(text, targetTransform.position);
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
            cachedEmitter = Object.FindObjectOfType<DeltaMarkerEmitter>();
            
            if (cachedEmitter == null)
            {
                Debug.LogWarning("[DeltaMarkerSystem] No DeltaMarkerEmitter found in scene. Creating runtime emitter.");
                GameObject emitterObj = new GameObject("DeltaMarkerEmitter_Runtime");
                cachedEmitter = emitterObj.AddComponent<DeltaMarkerEmitter>();
            }
            
            hasSearchedForEmitter = true;
            
            if (cachedEmitter != null)
            {
                cachedEmitter.EnsureReady();
                // Debug.Log($"[DeltaMarkerSystem] Using DeltaMarkerEmitter '{cachedEmitter.gameObject.name}'"); // Reduced verbosity
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

