using System.Collections.Generic;
using UnityEngine;
using CardGame.Managers;

namespace CardGame.Visuals
{
    /// <summary>
    /// Generates a stylised board background with depth, grid lines and subtle shading.
    /// Attach this to a GameObject that lives under the Drop Areas root.
    /// </summary>
    [ExecuteAlways]
    public class ProceduralBoardBackdrop : MonoBehaviour
    {
        [Header("Layout")]
        [SerializeField] private float padding = 0.35f;
        [SerializeField] private int gridColumns = 4;
        [SerializeField] private int gridRows = 4;
        [SerializeField] private int pixelsPerUnit = 380;

        [Header("Colours")]
        [SerializeField] private Color baseColor = new Color(0.027f, 0.400f, 0.341f);
        [SerializeField] private Color highlightColor = new Color(0.192f, 0.694f, 0.580f);
        [SerializeField] private Color ridgeColor = new Color(0.035f, 0.275f, 0.243f);
        [SerializeField] private Color gridLineColor = new Color(0.623f, 0.866f, 0.783f, 0.94f);
        [SerializeField] private Color borderColor = new Color(0.118f, 0.423f, 0.365f, 1f);
        [SerializeField] private Color reflectionColor = new Color(0.741f, 0.949f, 0.890f, 0.4f);

        [Header("Depth")]
        [SerializeField] private Vector2 shadowOffset = new Vector2(0.16f, -0.22f);
        [SerializeField] private Color shadowColor = new Color(0f, 0f, 0f, 0.35f);
        [SerializeField] private int sortingOrder = -100;

        private SpriteRenderer boardRenderer;
        private SpriteRenderer shadowRenderer;
        private Texture2D generatedTexture;
        private Sprite generatedSprite;

        private void Awake()
        {
            EnsureRenderers();
        }

        private void OnEnable()
        {
            RefreshNow();
        }

        private void OnDestroy()
        {
            DisposeGeneratedAssets();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (this != null)
                    {
                        RefreshNow();
                    }
                };
            }
        }
#endif

        public void RefreshNow()
        {
            EnsureRenderers();

            List<CardDropArea1> dropAreas = FindDropAreas();
            if (dropAreas.Count == 0)
            {
                return;
            }

            Bounds bounds = new Bounds(dropAreas[0].transform.localPosition, Vector3.zero);
            for (int i = 1; i < dropAreas.Count; i++)
            {
                bounds.Encapsulate(dropAreas[i].transform.localPosition);
            }

            Vector2 boardSize = new Vector2(bounds.size.x + padding * 2f, bounds.size.y + padding * 2f);

            transform.localPosition = bounds.center;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;

            int targetWidth = Mathf.Max(64, Mathf.RoundToInt(boardSize.x * pixelsPerUnit));
            int targetHeight = Mathf.Max(64, Mathf.RoundToInt(boardSize.y * pixelsPerUnit));

            GenerateTexture(targetWidth, targetHeight);

            float ppu = targetWidth / boardSize.x;
            ApplySprite(targetWidth, targetHeight, ppu);

            shadowRenderer.transform.localPosition = new Vector3(shadowOffset.x, shadowOffset.y, 0f);
            shadowRenderer.transform.localScale = Vector3.one;
            shadowRenderer.sortingOrder = sortingOrder - 1;
        }

        private void EnsureRenderers()
        {
            if (boardRenderer == null)
            {
                boardRenderer = GetComponent<SpriteRenderer>();
                if (boardRenderer == null)
                {
                    boardRenderer = gameObject.AddComponent<SpriteRenderer>();
                }
                boardRenderer.sortingOrder = sortingOrder;
                boardRenderer.color = Color.white;
            }

            if (shadowRenderer == null)
            {
                Transform shadowTransform = transform.Find("Shadow");
                if (shadowTransform == null)
                {
                    shadowTransform = new GameObject("Shadow").transform;
                    shadowTransform.SetParent(transform, false);
                }
                shadowRenderer = shadowTransform.GetComponent<SpriteRenderer>();
                if (shadowRenderer == null)
                {
                    shadowRenderer = shadowTransform.gameObject.AddComponent<SpriteRenderer>();
                }
                shadowRenderer.color = shadowColor;
            }
        }

        private List<CardDropArea1> FindDropAreas()
        {
            List<CardDropArea1> results = new List<CardDropArea1>();
            Transform root = transform.parent != null ? transform.parent : transform;
            var areas = root.GetComponentsInChildren<CardDropArea1>(true);
            if (areas != null && areas.Length > 0)
            {
                results.AddRange(areas);
            }
            return results;
        }

        private void GenerateTexture(int width, int height)
        {
            if (generatedTexture == null || generatedTexture.width != width || generatedTexture.height != height)
            {
                DisposeGeneratedAssets();
                generatedTexture = new Texture2D(width, height, TextureFormat.RGBA32, false, true)
                {
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear,
                    name = "ProceduralBoardTexture"
                };
            }

            Color[] pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                float ny = y / (float)(height - 1);
                for (int x = 0; x < width; x++)
                {
                    float nx = x / (float)(width - 1);
                    Color c = BaseGradient(nx, ny);
                    c = ApplyMountainShape(c, nx, ny);
                    c = ApplyReflection(c, nx, ny);
                    pixels[y * width + x] = c;
                }
            }

            DrawBorder(pixels, width, height);
            DrawGrid(pixels, width, height);

            generatedTexture.SetPixels(pixels);
            generatedTexture.Apply();
        }

        private Color BaseGradient(float nx, float ny)
        {
            float vertical = Mathf.Pow(1f - ny, 0.6f);
            Color c = Color.Lerp(baseColor, highlightColor, vertical * 0.65f);

            float diagonal = Mathf.Clamp01((nx + (1f - ny)) * 0.5f);
            c = Color.Lerp(c, baseColor * 0.8f, diagonal * 0.1f);
            return c;
        }

        private Color ApplyMountainShape(Color current, float nx, float ny)
        {
            float ridge = 0.42f + 0.22f * Mathf.Sin((nx - 0.2f) * 2.6f);
            float valley = 0.25f + 0.15f * Mathf.Sin((nx + 0.6f) * 5.1f);

            float ridgeMask = Mathf.Clamp01(ridge - ny);
            float valleyMask = Mathf.Clamp01(valley - ny);

            if (ridgeMask > 0f)
            {
                current = Color.Lerp(current, ridgeColor, ridgeMask * 0.45f);
            }

            if (valleyMask > 0f)
            {
                current = Color.Lerp(current, ridgeColor * 0.9f, valleyMask * 0.25f);
            }

            return current;
        }

        private Color ApplyReflection(Color current, float nx, float ny)
        {
            float reflection = Mathf.Clamp01(1f - (nx * 0.65f + ny * 1.1f));
            if (reflection > 0f)
            {
                current = Color.Lerp(current, reflectionColor, reflection * 0.35f);
            }
            return current;
        }

        private void DrawBorder(Color[] pixels, int width, int height)
        {
            int borderThickness = Mathf.Max(2, Mathf.RoundToInt(Mathf.Min(width, height) * 0.02f));
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (x < borderThickness || y < borderThickness || x >= width - borderThickness || y >= height - borderThickness)
                    {
                        int index = y * width + x;
                        pixels[index] = Color.Lerp(pixels[index], borderColor, 0.65f);
                    }
                }
            }
        }

        private void DrawGrid(Color[] pixels, int width, int height)
        {
            int margin = Mathf.RoundToInt(Mathf.Min(width, height) * 0.08f);
            int thickness = Mathf.Max(1, Mathf.RoundToInt(Mathf.Min(width, height) * 0.008f));

            int usableWidth = width - margin * 2;
            int usableHeight = height - margin * 2;

            for (int col = 0; col <= gridColumns; col++)
            {
                int x = margin + Mathf.RoundToInt(usableWidth * col / (float)gridColumns);
                DrawLine(pixels, width, height, x, margin, usableHeight, thickness, true);
            }

            for (int row = 0; row <= gridRows; row++)
            {
                int y = margin + Mathf.RoundToInt(usableHeight * row / (float)gridRows);
                DrawLine(pixels, width, height, y, margin, usableWidth, thickness, false);
            }
        }

        private void DrawLine(Color[] pixels, int textureWidth, int textureHeight, int start, int margin, int length, int thickness, bool vertical)
        {
            for (int offset = -thickness / 2; offset <= thickness / 2; offset++)
            {
                for (int i = 0; i < length; i++)
                {
                    int x = vertical ? start + offset : margin + i;
                    int y = vertical ? margin + i : start + offset;

                    if (x < 0 || x >= textureWidth || y < 0 || y >= textureHeight)
                        continue;

                    int index = y * textureWidth + x;
                    if (index >= 0 && index < pixels.Length)
                    {
                        pixels[index] = Color.Lerp(pixels[index], gridLineColor, 0.85f);
                    }
                }
            }
        }

        private void ApplySprite(int width, int height, float ppu)
        {
            if (generatedSprite != null)
            {
                DestroySprite(generatedSprite);
            }

            generatedSprite = Sprite.Create(generatedTexture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), ppu);
            generatedSprite.name = "ProceduralBoardSprite";

            boardRenderer.sprite = generatedSprite;
            shadowRenderer.sprite = generatedSprite;
        }

        private void DisposeGeneratedAssets()
        {
            if (generatedSprite != null)
            {
                DestroySprite(generatedSprite);
                generatedSprite = null;
            }

            if (generatedTexture != null)
            {
                DestroyTexture(generatedTexture);
                generatedTexture = null;
            }
        }

        private void DestroySprite(Sprite sprite)
        {
            if (Application.isPlaying)
            {
                Destroy(sprite);
            }
#if UNITY_EDITOR
            else
            {
                DestroyImmediate(sprite);
            }
#endif
        }

        private void DestroyTexture(Texture2D texture)
        {
            if (Application.isPlaying)
            {
                Destroy(texture);
            }
#if UNITY_EDITOR
            else
            {
                DestroyImmediate(texture);
            }
#endif
        }
    }
}

