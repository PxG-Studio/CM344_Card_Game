using UnityEngine;
using UnityEngine.UI;

namespace CardGame.UI
{
    /// <summary>
    /// Simple rotating diamond UI indicator for player turns
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class TurnIndicatorUI : MonoBehaviour
    {
        [Header("Rotation Settings")]
        [SerializeField] private float rotationSpeed = 50f;
        
        [Header("Hover Animation")]
        [SerializeField] private float hoverHeight = 10f;
        [SerializeField] private float hoverSpeed = 2f;
        
        [Header("Colors")]
        [SerializeField] private Color activeColor = new Color(1f, 0.8f, 0f, 1f); // Gold
        [SerializeField] private Color inactiveColor = new Color(0.3f, 0.3f, 0.3f, 0f); // Transparent
        
        private RectTransform rectTransform;
        private Image image;
        private Vector2 startPosition;
        private float hoverOffset;
        private bool isActive = false;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            image = GetComponent<Image>();
            startPosition = rectTransform.anchoredPosition;
            
            // Create a diamond/square texture
            CreateDiamondTexture();
            
            // Start inactive
            SetActive(false);
        }

        private void Update()
        {
            if (!isActive) return;
            
            // Rotate continuously
            rectTransform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
            
            // Hover up and down
            hoverOffset = Mathf.Sin(Time.time * hoverSpeed) * hoverHeight;
            rectTransform.anchoredPosition = startPosition + new Vector2(0, hoverOffset);
        }

        public void SetActive(bool active)
        {
            isActive = active;
            gameObject.SetActive(active);
            
            if (image != null)
            {
                image.color = active ? activeColor : inactiveColor;
            }
        }

        private void CreateDiamondTexture()
        {
            // Create a simple texture for the diamond shape
            int size = 64;
            Texture2D texture = new Texture2D(size, size);
            Color[] pixels = new Color[size * size];
            
            // Fill with transparent
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.clear;
            }
            
            // Draw a diamond (rotated square)
            int center = size / 2;
            int radius = size / 2 - 2;
            
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int dx = Mathf.Abs(x - center);
                    int dy = Mathf.Abs(y - center);
                    
                    // Diamond shape: |x - center| + |y - center| <= radius
                    if (dx + dy <= radius)
                    {
                        pixels[y * size + x] = Color.white;
                    }
                }
            }
            
            texture.SetPixels(pixels);
            texture.Apply();
            texture.filterMode = FilterMode.Bilinear;
            
            // Create sprite from texture
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f),
                100f
            );
            
            if (image != null)
            {
                image.sprite = sprite;
            }
            
            Debug.Log($"TurnIndicatorUI: Created diamond sprite for {gameObject.name}");
        }
    }
}

