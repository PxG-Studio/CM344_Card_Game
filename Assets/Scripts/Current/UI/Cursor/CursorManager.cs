using UnityEngine;
using UnityEngine.UI;

namespace CardGame.UI.CursorSystem
{
    /// <summary>
    /// Bootstraps the spinning triangle cursor inspired by the turn indicator visual.
    /// Creates a dedicated overlay canvas and keeps the cursor alive across scenes.
    /// </summary>
    public class CursorManager : MonoBehaviour
    {
        public static CursorManager Instance { get; private set; }
        
        [Header("Visual Settings")]
        [SerializeField] private float cursorSize = 72f;
        [SerializeField] private Color cursorColor = new Color(1f, 0.78f, 0.08f, 1f);
        [SerializeField] private bool addShadow = true;
        [SerializeField] private Color shadowColor = new Color(0f, 0f, 0f, 0.35f);
        [SerializeField] private Vector2 shadowOffset = new Vector2(4f, -6f);
        
        [Header("Animation Settings")]
        [SerializeField] private float rotationSpeed = 120f;
        [SerializeField] private float hoverDistance = 10f;
        [SerializeField] private float hoverSpeed = 2.4f;
        [SerializeField] private float pulseAmount = 0.08f;
        [SerializeField] private float pulseSpeed = 1.15f;
        [SerializeField] private bool hideSystemCursor = true;
        
        private Canvas cursorCanvas;
        private CursorSpinner spinner;
        private Sprite cachedTriangleSprite;
        
        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildCursor();
        }
        
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
            
            if (cachedTriangleSprite != null)
            {
                Destroy(cachedTriangleSprite.texture);
                Destroy(cachedTriangleSprite);
                cachedTriangleSprite = null;
            }
        }
        
        /// <summary>
        /// Ensures the cursor visual is rebuilt (e.g., after resolution change).
        /// </summary>
        public void RefreshCursor()
        {
            if (cursorCanvas == null)
            {
                BuildCursor();
                return;
            }
            
            if (spinner == null)
            {
                BuildCursorVisual(cursorCanvas.transform);
            }
        }
        
        private void BuildCursor()
        {
            cursorCanvas = CreateCursorCanvas();
            BuildCursorVisual(cursorCanvas.transform);
        }
        
        private Canvas CreateCursorCanvas()
        {
            GameObject canvasGO = new GameObject("TriangleCursorCanvas");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;
            
            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            GraphicRaycaster raycaster = canvasGO.AddComponent<GraphicRaycaster>();
            raycaster.enabled = false;
            
            DontDestroyOnLoad(canvasGO);
            return canvas;
        }
        
        private void BuildCursorVisual(Transform parent)
        {
            // Clear previous visual if it exists
            foreach (Transform child in parent)
            {
                Destroy(child.gameObject);
            }
            
            GameObject cursorRoot = new GameObject("TriangleCursor");
            cursorRoot.transform.SetParent(parent, false);
            RectTransform rootRect = cursorRoot.AddComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = Vector2.zero;
            
            GameObject visualGO = new GameObject("Visual");
            visualGO.transform.SetParent(cursorRoot.transform, false);
            RectTransform visualRect = visualGO.AddComponent<RectTransform>();
            visualRect.sizeDelta = new Vector2(cursorSize, cursorSize);
            visualRect.pivot = new Vector2(0.5f, 0.8f);
            visualRect.anchorMin = new Vector2(0.5f, 0f);
            visualRect.anchorMax = new Vector2(0.5f, 0f);
            
            Image visualImage = visualGO.AddComponent<Image>();
            visualImage.sprite = GetTriangleSprite();
            visualImage.color = cursorColor;
            visualImage.raycastTarget = false;
            visualImage.preserveAspect = true;
            
            if (addShadow)
            {
                CreateShadowLayer(visualRect, visualImage.sprite);
            }
            
            spinner = cursorRoot.AddComponent<CursorSpinner>();
            spinner.Initialize(
                visualRect,
                visualImage,
                cursorSize,
                cursorColor
            );
            spinner.ApplySettings(rotationSpeed, hoverDistance, hoverSpeed, pulseAmount, pulseSpeed, hideSystemCursor);
        }
        
        private void CreateShadowLayer(RectTransform visualRect, Sprite sprite)
        {
            GameObject shadowGO = new GameObject("Shadow");
            shadowGO.transform.SetParent(visualRect, false);
            RectTransform shadowRect = shadowGO.AddComponent<RectTransform>();
            shadowRect.sizeDelta = visualRect.sizeDelta;
            shadowRect.pivot = visualRect.pivot;
            shadowRect.anchorMin = visualRect.anchorMin;
            shadowRect.anchorMax = visualRect.anchorMax;
            shadowRect.anchoredPosition = shadowOffset;
            
            Image shadowImage = shadowGO.AddComponent<Image>();
            shadowImage.sprite = sprite;
            shadowImage.color = shadowColor;
            shadowImage.raycastTarget = false;
            shadowImage.preserveAspect = true;
            shadowGO.transform.SetAsFirstSibling();
        }
        
        private Sprite GetTriangleSprite()
        {
            if (cachedTriangleSprite != null)
            {
                return cachedTriangleSprite;
            }
            
            int size = Mathf.Clamp(Mathf.RoundToInt(cursorSize * 2f), 64, 512);
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            
            Color[] pixels = new Color[size * size];
            int centerX = size / 2;
            
            for (int y = 0; y < size; y++)
            {
                float normalizedY = y / (float)(size - 1); // 0 bottom, 1 top
                float apexFalloff = 1f - normalizedY;
                float halfWidth = apexFalloff * centerX;
                
                // Subtle shading between highlight and base color
                Color shade = Color.Lerp(cursorColor * 0.9f, cursorColor * 1.1f, 1f - apexFalloff);
                shade.a = cursorColor.a;
                
                for (int x = 0; x < size; x++)
                {
                    if (Mathf.Abs(x - centerX) <= halfWidth)
                    {
                        pixels[y * size + x] = shade;
                    }
                    else
                    {
                        pixels[y * size + x] = Color.clear;
                    }
                }
            }
            
            texture.SetPixels(pixels);
            texture.Apply();
            
            cachedTriangleSprite = Sprite.Create(
                texture,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.82f),
                256f
            );
            
            cachedTriangleSprite.name = "RuntimeTriangleCursor";
            return cachedTriangleSprite;
        }
    }
}

