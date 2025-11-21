using UnityEngine;

namespace CardGame.UI
{
    /// <summary>
    /// Sets a custom cursor sprite for the game with optional spinning animation.
    /// Uses a diamond/pointer sprite as the mouse cursor.
    /// </summary>
    public class CustomCursor : MonoBehaviour
    {
        [Header("Cursor Settings")]
        [SerializeField] private Sprite cursorSprite;
        [SerializeField] private Vector2 hotSpot = Vector2.zero; // Click point (0,0 = top-left, center = sprite center)
        [SerializeField] private CursorMode cursorMode = CursorMode.Auto;
        
        [Header("Animation Settings")]
        [SerializeField] private bool enableRotation = true;
        [SerializeField] private float rotationSpeed = 120f; // Degrees per second (matches turn indicator)
        [SerializeField] private float updateInterval = 0.05f; // Update cursor texture every X seconds
        [SerializeField] private bool reverseRotation = false; // Match turn indicator rotation direction
        
        [Header("Size Settings")]
        [SerializeField] private float cursorScale = 0.3f; // Scale factor for cursor size (0.3 = 30% size, smaller for cursor)
        
        [Header("Color Settings")]
        [SerializeField] private bool tintGreen = true; // Tint cursor green
        [SerializeField] private Color greenTint = new Color(0f, 1f, 0f, 1f); // Bright green
        
        [Header("Pointer Shape")]
        [SerializeField] private bool useGeneratedTriangle = true; // If true, generates a simple 2D triangle (recommended - works reliably)
        [SerializeField] private int triangleSize = 32; // Size of the generated triangle cursor (32x32 pixels)
        
        // Legacy fields (kept for backward compatibility, but useGeneratedTriangle is recommended)
        [SerializeField] private bool usePointerShape = false; // Legacy: uses triangle from diamond sprite (less reliable)
        [SerializeField] private bool autoDetectBestShape = false; // Legacy: auto-detect shape
        
        [Header("Auto-Find Settings")]
        [SerializeField] private bool autoFindCursorSprite = true;
        [SerializeField] private string[] cursorGameObjectNames = { 
            "CustomCursor", 
            "GameCursor", 
            "Cursor", 
            "Pointer", 
            "InteractivePointer",
            "UICursor",
            "Deck Slot" // Fallback to old name
        };
        
        private Texture2D cursorTexture;
        private float lastUpdateTime = 0f;
        private float currentRotation = 0f;
        private Sprite cachedTriangleSprite = null; // Cache the triangle sprite to avoid recreating it every frame
        private Sprite cachedSourceSprite = null; // Track which source sprite was used for the cached triangle
        
        private void Start()
        {
            // CRITICAL: Ensure this GameObject can NEVER interfere with mouse input
            EnsureZeroInteractionCapability();
            
            // Small delay to ensure HUDSetup has finished setting the sprite via reflection
            StartCoroutine(DelayedCursorSetup());
        }
        
        /// <summary>
        /// Ensures the CursorManager GameObject has zero interaction capability.
        /// This prevents any edge cases where the cursor manager might interfere with input.
        /// </summary>
        private void EnsureZeroInteractionCapability()
        {
            GameObject managerObj = gameObject;
            
            // Edge Case Guard 1: Remove any colliders (shouldn't have any, but safeguard)
            Collider2D col2D = managerObj.GetComponent<Collider2D>();
            if (col2D != null)
            {
                Destroy(col2D);
                Debug.LogWarning("CustomCursor: Removed unexpected Collider2D from CursorManager");
            }
            
            Collider col3D = managerObj.GetComponent<Collider>();
            if (col3D != null)
            {
                Destroy(col3D);
                Debug.LogWarning("CustomCursor: Removed unexpected Collider from CursorManager");
            }
            
            // Edge Case Guard 2: Remove any renderers (shouldn't have any, but safeguard)
            Renderer renderer = managerObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                Destroy(renderer);
                Debug.LogWarning("CustomCursor: Removed unexpected Renderer from CursorManager");
            }
            
            // Edge Case Guard 3: Ensure on Ignore Raycast layer
            int ignoreLayer = LayerMask.NameToLayer("Ignore Raycast");
            if (ignoreLayer >= 0 && managerObj.layer != ignoreLayer)
            {
                managerObj.layer = ignoreLayer;
                Debug.Log("CustomCursor: Set CursorManager to 'Ignore Raycast' layer");
            }
            
            // Edge Case Guard 4: Disable any UI components that could block raycasts
            UnityEngine.UI.Graphic[] graphics = managerObj.GetComponentsInChildren<UnityEngine.UI.Graphic>(true);
            foreach (var graphic in graphics)
            {
                if (graphic is UnityEngine.UI.Image img) img.raycastTarget = false;
                else if (graphic is UnityEngine.UI.Text txt) txt.raycastTarget = false;
                else if (graphic is UnityEngine.UI.RawImage rawImg) rawImg.raycastTarget = false;
            }
            
            // Edge Case Guard 5: Disable any CanvasGroups
            CanvasGroup[] canvasGroups = managerObj.GetComponentsInChildren<CanvasGroup>(true);
            foreach (var cg in canvasGroups)
            {
                cg.blocksRaycasts = false;
                cg.interactable = false;
            }
            
            // Edge Case Guard 6: Ensure this script doesn't implement input interfaces
            // (It doesn't, but we verify it in case someone adds them)
            // This is more of a documentation/verification step
        }
        
        private System.Collections.IEnumerator DelayedCursorSetup()
        {
            // Wait one frame to ensure HUDSetup has finished
            yield return null;
            
            // Auto-find cursor sprite if enabled and not assigned
            // Note: If sprite was set via reflection (by HUDSetup), it will already be assigned
            if (autoFindCursorSprite && cursorSprite == null)
            {
                FindCursorSprite();
            }
            
            // Only set cursor if we have a sprite
            if (cursorSprite != null)
            {
                Debug.Log($"CustomCursor: Setting cursor with sprite '{cursorSprite.name}' (size: {cursorSprite.rect.width}x{cursorSprite.rect.height})");
                SetCustomCursor();
            }
            else
            {
                Debug.LogWarning("CustomCursor: No cursor sprite available. Custom cursor will not be set.");
                Debug.LogWarning("CustomCursor: Check that HUDSetup found the cursor GameObject and sprite.");
            }
        }
        
        private void Update()
        {
            // Update cursor texture with rotation if enabled
            if (enableRotation && cursorSprite != null && Time.time - lastUpdateTime >= updateInterval)
            {
                // Apply rotation direction (reverse if needed to rotate by its side)
                float rotationDelta = rotationSpeed * updateInterval;
                if (reverseRotation)
                {
                    rotationDelta = -rotationDelta;
                }
                
                currentRotation += rotationDelta;
                if (currentRotation >= 360f)
                {
                    currentRotation -= 360f;
                }
                else if (currentRotation < 0f)
                {
                    currentRotation += 360f;
                }
                
                UpdateCursorTexture();
                lastUpdateTime = Time.time;
            }
        }
        
        /// <summary>
        /// Attempts to find the cursor sprite from a GameObject in the scene.
        /// Note: The GameObject should already be disabled by HUDSetup to prevent input interference.
        /// This method also ensures the found GameObject is fully disabled to prevent any input interference.
        /// </summary>
        private void FindCursorSprite()
        {
            foreach (string name in cursorGameObjectNames)
            {
                GameObject cursorObj = GameObject.Find(name);
                if (cursorObj != null)
                {
                    // Try SpriteRenderer first (for 2D sprites)
                    SpriteRenderer sr = cursorObj.GetComponent<SpriteRenderer>();
                    if (sr != null && sr.sprite != null)
                    {
                        cursorSprite = sr.sprite;
                        Debug.Log($"CustomCursor: Auto-found cursor sprite from '{name}' GameObject (SpriteRenderer)");
                        
                        // CRITICAL: Ensure this GameObject can NEVER interfere with input
                        DisableInputOnGameObject(cursorObj);
                        
                        // Calculate center hot spot if not set
                        if (hotSpot == Vector2.zero)
                        {
                            // For a diamond, use the center as hot spot
                            hotSpot = new Vector2(cursorSprite.rect.width / 2f, cursorSprite.rect.height / 2f);
                        }
                        
                        return;
                    }
                    
                    // Try Image component (for UI sprites)
                    UnityEngine.UI.Image img = cursorObj.GetComponent<UnityEngine.UI.Image>();
                    if (img != null && img.sprite != null)
                    {
                        cursorSprite = img.sprite;
                        Debug.Log($"CustomCursor: Auto-found cursor sprite from '{name}' GameObject (Image)");
                        
                        // CRITICAL: Ensure this GameObject can NEVER interfere with input
                        DisableInputOnGameObject(cursorObj);
                        
                        // Calculate center hot spot if not set
                        if (hotSpot == Vector2.zero)
                        {
                            hotSpot = new Vector2(cursorSprite.rect.width / 2f, cursorSprite.rect.height / 2f);
                        }
                        
                        return;
                    }
                }
            }
            
            Debug.LogWarning("CustomCursor: Could not auto-find cursor sprite. Please assign manually in Inspector.");
        }
        
        /// <summary>
        /// Sets the custom cursor from the sprite.
        /// </summary>
        public void SetCustomCursor()
        {
            if (cursorSprite == null)
            {
                Debug.LogWarning("CustomCursor: No cursor sprite assigned!");
                return;
            }
            
            // Calculate hot spot if not set
            Vector2 finalHotSpot = hotSpot;
            if (hotSpot == Vector2.zero)
            {
                // For pointer shape, use top point (the pointed end)
                // For diamond, use center
                if (usePointerShape)
                {
                    finalHotSpot = new Vector2(cursorSprite.rect.width / 2f, cursorSprite.rect.height);
                }
                else
                {
                    finalHotSpot = new Vector2(cursorSprite.rect.width / 2f, cursorSprite.rect.height / 2f);
                }
            }
            
            UpdateCursorTexture();
            
            // Verify texture was created
            if (cursorTexture == null)
            {
                Debug.LogError("CustomCursor: Failed to create cursor texture! Cursor will not be set.");
                return;
            }
            
            // Set the cursor
            Cursor.SetCursor(cursorTexture, finalHotSpot, cursorMode);
            
            Debug.Log($"CustomCursor: Cursor set successfully! Sprite: {cursorSprite.name}, Hot spot: {finalHotSpot}, Texture size: {cursorTexture.width}x{cursorTexture.height}");
        }
        
        /// <summary>
        /// Updates the cursor texture, applying rotation if enabled.
        /// </summary>
        private void UpdateCursorTexture()
        {
            Sprite spriteToUse = null;
            
            // Use generated triangle if enabled (recommended - works reliably)
            if (useGeneratedTriangle)
            {
                // Generate a simple 2D triangle from scratch
                spriteToUse = GenerateSimpleTriangle();
            }
            else if (cursorSprite != null)
            {
                spriteToUse = cursorSprite;
                
                // Apply pointer shape (triangle) if enabled
                if (usePointerShape || (autoDetectBestShape && ShouldUseTriangle()))
                {
                    // Cache the triangle sprite to avoid recreating it every frame
                    if (cachedTriangleSprite == null || cachedSourceSprite != cursorSprite)
                    {
                        cachedTriangleSprite = CreateTriangleFromDiamond(cursorSprite);
                        cachedSourceSprite = cursorSprite;
                    }
                    
                    if (cachedTriangleSprite != null)
                    {
                        spriteToUse = cachedTriangleSprite;
                    }
                    // If triangle creation fails, fall back to original diamond
                }
                else
                {
                    // Clear cache if triangle is not being used
                    if (cachedTriangleSprite != null)
                    {
                        Destroy(cachedTriangleSprite);
                        cachedTriangleSprite = null;
                        cachedSourceSprite = null;
                    }
                }
            }
            
            if (spriteToUse == null)
            {
                Debug.LogWarning("CustomCursor: No sprite available for cursor!");
                return;
            }
            
            // Convert sprite to texture with optional rotation
            cursorTexture = SpriteToTexture2D(spriteToUse, enableRotation ? currentRotation : 0f);
            
            // Calculate hot spot (accounting for rotation and scaling)
            Vector2 finalHotSpot = hotSpot;
            if (hotSpot == Vector2.zero)
            {
                // Apply scale to hot spot
                float scaledWidth = spriteToUse.rect.width * cursorScale;
                float scaledHeight = spriteToUse.rect.height * cursorScale;
                
                // For triangle pointer, use top point as hot spot (the pointed end)
                // This is the click point, like a traditional mouse cursor
                finalHotSpot = new Vector2(scaledWidth / 2f, scaledHeight);
            }
            else
            {
                // Apply scale to manual hot spot
                finalHotSpot *= cursorScale;
            }
            
            // Update the cursor
            Cursor.SetCursor(cursorTexture, finalHotSpot, cursorMode);
        }
        
        /// <summary>
        /// Generates a diamond cursor from scratch (matching turn indicator visual style).
        /// Uses the same diamond shape as TurnIndicatorUI but in green for the cursor.
        /// </summary>
        private Sprite GenerateSimpleTriangle()
        {
            int size = triangleSize;
            Texture2D diamondTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            
            // Fill with transparent
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.clear;
            }
            
            // Create diamond shape (same as TurnIndicatorUI.CreateDiamondTexture)
            // Diamond shape: rotated square, same visual style as turn indicator
            int center = size / 2;
            int radius = size / 2 - 2;
            
            Color diamondColor = tintGreen ? greenTint : Color.white;
            
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int dx = Mathf.Abs(x - center);
                    int dy = Mathf.Abs(y - center);
                    
                    // Diamond shape: |x - center| + |y - center| <= radius
                    if (dx + dy <= radius)
                    {
                        pixels[y * size + x] = diamondColor;
                    }
                }
            }
            
            diamondTexture.SetPixels(pixels);
            diamondTexture.Apply();
            diamondTexture.filterMode = FilterMode.Bilinear;
            
            // Create sprite with pivot at top center (the pointed end - where hot spot will be)
            // For a diamond, we use the top point as the hot spot
            Sprite diamondSprite = Sprite.Create(
                diamondTexture,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 1f), // Pivot at top center (pointed end for cursor)
                100f
            );
            
            return diamondSprite;
        }
        
        /// <summary>
        /// Determines if triangle fallback should be used based on sprite characteristics.
        /// </summary>
        private bool ShouldUseTriangle()
        {
            // Auto-detect: if sprite is very large or complex, use triangle
            if (cursorSprite != null)
            {
                float area = cursorSprite.rect.width * cursorSprite.rect.height;
                // If sprite is larger than 64x64, triangle might work better
                if (area > 4096f) // 64x64
                {
                    return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// Creates a pointer sprite (triangle) from a diamond sprite.
        /// Creates an upward-pointing triangle from the top portion of the diamond.
        /// The hot spot will be at the top point (the pointed end), like a traditional mouse cursor.
        /// </summary>
        private Sprite CreateTriangleFromDiamond(Sprite diamondSprite)
        {
            if (diamondSprite == null) return null;
            
            // Get the sprite's texture
            Texture2D sourceTexture = diamondSprite.texture;
            Rect spriteRect = diamondSprite.textureRect;
            
            int width = (int)spriteRect.width;
            int height = (int)spriteRect.height;
            
            // Get original pixels - handle non-readable textures
            Color[] originalPixels = null;
            try
            {
                originalPixels = sourceTexture.GetPixels(
                    (int)spriteRect.x,
                    (int)spriteRect.y,
                    width,
                    height
                );
            }
            catch
            {
                // Texture is not readable, use RenderTexture method
                originalPixels = ExtractSpriteRegionViaRenderTexture(diamondSprite);
                if (originalPixels == null)
                {
                    Debug.LogError("CustomCursor: Failed to read sprite pixels for pointer creation.");
                    return null;
                }
            }
            
            // Create pointer (triangle pointing upward from top portion of diamond)
            Texture2D triangleTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] trianglePixels = new Color[width * height];
            
            int centerX = width / 2;
            int halfHeight = height / 2;
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    
                    // Create upward-pointing triangle from top half of diamond
                    // Triangle shape: narrow at top (point), wide at bottom
                    // Formula: |x - centerX| <= (halfHeight - y) * (width / halfHeight) * 0.5f
                    if (y <= halfHeight)
                    {
                        float distanceFromCenter = Mathf.Abs(x - centerX);
                        float maxDistance = (halfHeight - y) * (width / (float)halfHeight) * 0.5f;
                        
                        if (distanceFromCenter <= maxDistance)
                        {
                            // Use original pixel from top half of diamond
                            Color originalColor = originalPixels[y * width + x];
                            if (tintGreen && originalColor.a > 0.01f) // Only tint non-transparent pixels
                            {
                                // Preserve alpha, apply green tint
                                trianglePixels[index] = new Color(greenTint.r, greenTint.g, greenTint.b, originalColor.a);
                            }
                            else
                            {
                                trianglePixels[index] = originalColor;
                            }
                        }
                        else
                        {
                            trianglePixels[index] = Color.clear;
                        }
                    }
                    else
                    {
                        // Bottom half is transparent
                        trianglePixels[index] = Color.clear;
                    }
                }
            }
            
            triangleTexture.SetPixels(trianglePixels);
            triangleTexture.Apply();
            
            // Create sprite from texture with pivot at top center (the pointed end - where the hot spot will be)
            // Vector2(0.5f, 1f) means: 50% horizontally (center), 100% vertically (top)
            Sprite triangleSprite = Sprite.Create(
                triangleTexture,
                new Rect(0, 0, width, height),
                new Vector2(0.5f, 1f), // Pivot at top center (the pointed end of the pointer)
                100f
            );
            
            // Only log once when pointer is first created (not every frame)
            Debug.Log($"CustomCursor: Created pointer shape from diamond sprite (cached for reuse)");
            return triangleSprite;
        }
        
        /// <summary>
        /// Converts a Sprite to Texture2D for cursor use, with optional rotation and scaling.
        /// Handles non-readable textures by rendering to a RenderTexture.
        /// </summary>
        private Texture2D SpriteToTexture2D(Sprite sprite, float rotationDegrees = 0f)
        {
            if (sprite == null) return null;
            
            // Get the sprite's rect
            Rect spriteRect = sprite.textureRect;
            int originalWidth = (int)spriteRect.width;
            int originalHeight = (int)spriteRect.height;
            
            // Apply scale
            int width = Mathf.RoundToInt(originalWidth * cursorScale);
            int height = Mathf.RoundToInt(originalHeight * cursorScale);
            
            // Ensure minimum size and reasonable maximum (cursors should be small)
            if (width < 1) width = 1;
            if (height < 1) height = 1;
            if (width > 128) width = 128; // Max cursor size
            if (height > 128) height = 128;
            
            Color[] originalPixels = null;
            
            // Try to read pixels directly (works if texture is readable)
            try
            {
                Texture2D sourceTexture = sprite.texture;
                Color[] fullPixels = sourceTexture.GetPixels(
                    (int)spriteRect.x,
                    (int)spriteRect.y,
                    originalWidth,
                    originalHeight
                );
                
                // Scale down pixels if needed
                if (cursorScale != 1f)
                {
                    originalPixels = ScalePixels(fullPixels, originalWidth, originalHeight, width, height);
                }
                else
                {
                    originalPixels = fullPixels;
                }
            }
            catch
            {
                // Texture is not readable, use RenderTexture method
                originalPixels = ReadSpritePixelsViaRenderTexture(sprite);
                if (originalPixels == null)
                {
                    Debug.LogError("CustomCursor: Failed to read sprite pixels. Texture may not be readable.");
                    return null;
                }
                
                // Scale down if needed
                if (cursorScale != 1f && originalPixels != null)
                {
                    originalPixels = ScalePixels(originalPixels, originalWidth, originalHeight, width, height);
                }
            }
            
            // Apply green tint if enabled
            Color[] processedPixels = originalPixels;
            if (tintGreen)
            {
                processedPixels = TintPixelsGreen(originalPixels);
            }
            
            // Create a new readable texture with RGBA32 format (required by Cursor.SetCursor)
            Texture2D readableTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            
            if (Mathf.Approximately(rotationDegrees, 0f))
            {
                // No rotation, just copy pixels
                readableTexture.SetPixels(processedPixels);
            }
            else
            {
                // Apply rotation
                Color[] rotatedPixels = RotatePixels(processedPixels, width, height, rotationDegrees);
                readableTexture.SetPixels(rotatedPixels);
            }
            
            readableTexture.Apply();
            
            return readableTexture;
        }
        
        /// <summary>
        /// Scales pixel array from source size to destination size using bilinear interpolation.
        /// </summary>
        private Color[] ScalePixels(Color[] sourcePixels, int sourceWidth, int sourceHeight, int destWidth, int destHeight)
        {
            Color[] scaledPixels = new Color[destWidth * destHeight];
            
            float scaleX = (float)sourceWidth / destWidth;
            float scaleY = (float)sourceHeight / destHeight;
            
            for (int y = 0; y < destHeight; y++)
            {
                for (int x = 0; x < destWidth; x++)
                {
                    // Calculate source coordinates
                    float srcX = x * scaleX;
                    float srcY = y * scaleY;
                    
                    // Bilinear interpolation
                    int x1 = Mathf.FloorToInt(srcX);
                    int y1 = Mathf.FloorToInt(srcY);
                    int x2 = Mathf.Min(x1 + 1, sourceWidth - 1);
                    int y2 = Mathf.Min(y1 + 1, sourceHeight - 1);
                    
                    float fx = srcX - x1;
                    float fy = srcY - y1;
                    
                    // Get four corner pixels
                    Color c11 = sourcePixels[y1 * sourceWidth + x1];
                    Color c21 = sourcePixels[y1 * sourceWidth + x2];
                    Color c12 = sourcePixels[y2 * sourceWidth + x1];
                    Color c22 = sourcePixels[y2 * sourceWidth + x2];
                    
                    // Interpolate
                    Color c1 = Color.Lerp(c11, c21, fx);
                    Color c2 = Color.Lerp(c12, c22, fx);
                    Color final = Color.Lerp(c1, c2, fy);
                    
                    scaledPixels[y * destWidth + x] = final;
                }
            }
            
            return scaledPixels;
        }
        
        /// <summary>
        /// Reads sprite pixels using RenderTexture when the source texture is not readable.
        /// Renders the sprite region to a RenderTexture and extracts pixels.
        /// </summary>
        private Color[] ReadSpritePixelsViaRenderTexture(Sprite sprite)
        {
            if (sprite == null) return null;
            
            return ExtractSpriteRegionViaRenderTexture(sprite);
        }
        
        /// <summary>
        /// Extracts sprite region pixels using RenderTexture with proper UV coordinates.
        /// This works even when the source texture is not readable.
        /// Uses Graphics.Blit with a material that properly handles UV coordinates.
        /// </summary>
        private Color[] ExtractSpriteRegionViaRenderTexture(Sprite sprite)
        {
            if (sprite == null) return null;
            
            Rect spriteRect = sprite.textureRect;
            int width = (int)spriteRect.width;
            int height = (int)spriteRect.height;
            Texture2D sourceTexture = sprite.texture;
            
            // Create a RenderTexture matching the SOURCE texture size (not sprite size)
            // We need to blit the whole texture first, then extract the sprite region
            RenderTexture tempRT = RenderTexture.GetTemporary(sourceTexture.width, sourceTexture.height, 0, RenderTextureFormat.ARGB32);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = tempRT;
            
            // Clear with transparency
            GL.Clear(true, true, Color.clear);
            
            // Use Graphics.Blit to copy from source texture
            // Since we need a specific region, we'll use Graphics.DrawTexture with proper UV mapping
            // Calculate normalized UV coordinates
            float texWidth = sourceTexture.width;
            float texHeight = sourceTexture.height;
            
            // Create source rect in normalized UV coordinates (0-1 range)
            Rect sourceUVRect = new Rect(
                spriteRect.x / texWidth,                    // x normalized
                1f - (spriteRect.y + spriteRect.height) / texHeight,  // y normalized (flipped)
                spriteRect.width / texWidth,                // width normalized
                spriteRect.height / texHeight               // height normalized
            );
            
            // Use Graphics.Blit to copy the whole texture first
            // Then extract the sprite region from the result
            Graphics.Blit(sourceTexture, tempRT);
            
            // Read the entire texture from RenderTexture (must be RGBA32 for cursor use)
            Texture2D fullReadableTexture = new Texture2D(sourceTexture.width, sourceTexture.height, TextureFormat.RGBA32, false);
            fullReadableTexture.ReadPixels(new Rect(0, 0, sourceTexture.width, sourceTexture.height), 0, 0);
            fullReadableTexture.Apply();
            
            // Extract just the sprite region
            int startX = (int)spriteRect.x;
            int startY = (int)spriteRect.y;
            Color[] fullPixels = fullReadableTexture.GetPixels();
            Color[] pixels = new Color[width * height];
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int sourceIndex = (startY + y) * sourceTexture.width + (startX + x);
                    int destIndex = y * width + x;
                    if (sourceIndex >= 0 && sourceIndex < fullPixels.Length)
                    {
                        // Preserve color (including alpha) from source
                        pixels[destIndex] = fullPixels[sourceIndex];
                    }
                    else
                    {
                        pixels[destIndex] = Color.clear;
                    }
                }
            }
            
            // Clean up full texture
            Destroy(fullReadableTexture);
            
            // Clean up
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(tempRT);
            
            return pixels;
        }
        
        /// <summary>
        /// Tints pixels green while preserving alpha channel.
        /// </summary>
        private Color[] TintPixelsGreen(Color[] pixels)
        {
            Color[] tintedPixels = new Color[pixels.Length];
            for (int i = 0; i < pixels.Length; i++)
            {
                Color originalColor = pixels[i];
                // Only tint non-transparent pixels
                if (originalColor.a > 0.01f)
                {
                    // Preserve alpha, apply green tint
                    tintedPixels[i] = new Color(greenTint.r, greenTint.g, greenTint.b, originalColor.a);
                }
                else
                {
                    // Keep transparent pixels transparent
                    tintedPixels[i] = Color.clear;
                }
            }
            return tintedPixels;
        }
        
        /// <summary>
        /// Rotates pixel array by the specified degrees around the center point (like the turn indicator).
        /// This creates a simple 2D rotation, spinning the triangle around its center.
        /// </summary>
        private Color[] RotatePixels(Color[] pixels, int width, int height, float degrees)
        {
            Color[] rotated = new Color[pixels.Length];
            float radians = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);
            
            // Pivot point: center of the texture (like the turn indicator rotates around its center)
            float pivotX = width / 2f;
            float pivotY = height / 2f;
            
            // Clear the rotated array first
            for (int i = 0; i < rotated.Length; i++)
            {
                rotated[i] = Color.clear;
            }
            
            // Rotate around the center point (simple 2D rotation, like the turn indicator)
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Translate to center-relative coordinates
                    float dx = x - pivotX;
                    float dy = y - pivotY;
                    
                    // Apply 2D rotation transformation (rotate around center)
                    float rotatedX = dx * cos - dy * sin;
                    float rotatedY = dx * sin + dy * cos;
                    
                    // Translate back to absolute coordinates
                    int sourceX = Mathf.RoundToInt(rotatedX + pivotX);
                    int sourceY = Mathf.RoundToInt(rotatedY + pivotY);
                    
                    // Sample from source pixel (with bounds checking)
                    if (sourceX >= 0 && sourceX < width && sourceY >= 0 && sourceY < height)
                    {
                        // Use nearest-neighbor for simplicity (cursor is small)
                        rotated[y * width + x] = pixels[sourceY * width + sourceX];
                    }
                    // Else leave as transparent (already cleared above)
                }
            }
            
            return rotated;
        }
        
        /// <summary>
        /// Switch between pointer (triangle) and diamond cursor (for testing).
        /// </summary>
        public void TogglePointerShape()
        {
            usePointerShape = !usePointerShape;
            SetCustomCursor();
            Debug.Log($"CustomCursor: Switched to {(usePointerShape ? "pointer" : "diamond")} mode");
        }
        
        /// <summary>
        /// Enable or disable pointer shape mode.
        /// </summary>
        public void SetPointerMode(bool usePointer)
        {
            usePointerShape = usePointer;
            SetCustomCursor();
            Debug.Log($"CustomCursor: Set to {(usePointer ? "pointer" : "diamond")} mode");
        }
        
        /// <summary>
        /// Reset cursor to default system cursor.
        /// </summary>
        public void ResetCursor()
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            if (cursorTexture != null)
            {
                Destroy(cursorTexture);
                cursorTexture = null;
            }
        }
        
        /// <summary>
        /// Disables all input-related components on a GameObject to prevent interference with mouse input.
        /// </summary>
        private void DisableInputOnGameObject(GameObject obj)
        {
            if (obj == null) return;
            
            // Disable all colliders
            Collider2D[] colliders2D = obj.GetComponentsInChildren<Collider2D>(true);
            foreach (var col in colliders2D) col.enabled = false;
            
            Collider[] colliders3D = obj.GetComponentsInChildren<Collider>(true);
            foreach (var col in colliders3D) col.enabled = false;
            
            // Disable all UI raycast targets
            UnityEngine.UI.Graphic[] graphics = obj.GetComponentsInChildren<UnityEngine.UI.Graphic>(true);
            foreach (var graphic in graphics)
            {
                if (graphic is UnityEngine.UI.Image img) img.raycastTarget = false;
                else if (graphic is UnityEngine.UI.Text txt) txt.raycastTarget = false;
                else if (graphic is UnityEngine.UI.RawImage rawImg) rawImg.raycastTarget = false;
            }
            
            // Disable all CanvasGroups
            CanvasGroup[] canvasGroups = obj.GetComponentsInChildren<CanvasGroup>(true);
            foreach (var cg in canvasGroups)
            {
                cg.blocksRaycasts = false;
                cg.interactable = false;
            }
            
            // Move to Ignore Raycast layer
            int ignoreLayer = LayerMask.NameToLayer("Ignore Raycast");
            if (ignoreLayer >= 0)
            {
                SetLayerRecursive(obj, ignoreLayer);
            }
            
            // Finally, deactivate the GameObject entirely
            obj.SetActive(false);
        }
        
        /// <summary>
        /// Sets the layer recursively on a GameObject and all its children.
        /// </summary>
        private void SetLayerRecursive(GameObject obj, int layer)
        {
            if (obj == null) return;
            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursive(child.gameObject, layer);
            }
        }
        
        private void OnDestroy()
        {
            // Clean up texture
            if (cursorTexture != null)
            {
                Destroy(cursorTexture);
            }
            
            // Clean up cached triangle sprite
            if (cachedTriangleSprite != null)
            {
                Destroy(cachedTriangleSprite);
            }
        }
        
        private void OnApplicationFocus(bool hasFocus)
        {
            // Re-apply cursor when application regains focus
            if (hasFocus && cursorSprite != null)
            {
                SetCustomCursor();
            }
        }
    }
}

