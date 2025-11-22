using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CardGame.Managers;

namespace CardGame.UI
{
    /// <summary>
    /// Single turn indicator that moves in a smooth figure-eight pattern (like Navi from Zelda)
    /// between P1Panel and P2Panel, making the active turn immediately obvious through motion.
    /// </summary>
    public class TurnIndicatorMoving : MonoBehaviour
    {
        [Header("Target Panels")]
        [SerializeField] private RectTransform p1Panel;
        [SerializeField] private RectTransform p2Panel;
        
        [Header("Animation Settings")]
        [SerializeField] private float transitionDuration = 1.5f; // Time to complete figure-eight transition
        [SerializeField] private float hoverHeight = 15f; // Vertical hover amplitude
        [SerializeField] private float hoverSpeed = 3f; // Hover oscillation speed
        [SerializeField] private AnimationCurve motionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        [Header("Visual Settings")]
        [SerializeField] private float rotationSpeed = 180f; // Rotation speed while moving
        [SerializeField] private float scalePulseSpeed = 2f; // Scale pulsing speed
        [SerializeField] private float scalePulseAmount = 0.2f; // How much to scale (20% larger/smaller)
        
        [Header("Colors")]
        [SerializeField] private Color activeColor = new Color(1f, 0.8f, 0f, 1f); // Gold/Yellow
        [SerializeField] private Color inactiveColor = new Color(0.3f, 0.3f, 0.3f, 0.5f); // Gray/Semi-transparent
        
        // Public method to set panels (called from HUDSetup)
        public void SetPanels(RectTransform p1, RectTransform p2)
        {
            p1Panel = p1;
            p2Panel = p2;
            
            // Ensure rectTransform is initialized (in case SetPanels is called before Awake)
            if (rectTransform == null)
            {
                rectTransform = GetComponent<RectTransform>();
            }
            
            // Update positions if panels are set
            if (p1Panel != null && p2Panel != null && rectTransform != null)
            {
                p1Position = GetPanelCenter(p1Panel);
                p2Position = GetPanelCenter(p2Panel);
                rectTransform.anchoredPosition = p1Position;
                currentTargetPosition = p1Position;
            }
        }
        
        private RectTransform rectTransform;
        private Image image;
        private TextMeshProUGUI textIndicator;
        private Vector2 p1Position;
        private Vector2 p2Position;
        private Vector2 currentTargetPosition;
        private bool isMovingToP2 = false; // Direction of movement
        private float transitionProgress = 0f;
        private bool isTransitioning = false;
        private float hoverOffset = 0f;
        private float baseScale = 1f;
        
        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            image = GetComponent<Image>();
            textIndicator = GetComponent<TextMeshProUGUI>();
            
            // Get base scale
            baseScale = rectTransform.localScale.x;
            
            // Create visual if needed
            if (textIndicator == null && image == null)
            {
                CreateDiamondVisual();
            }
        }
        
        private void Start()
        {
            // Find panels if not assigned
            if (p1Panel == null || p2Panel == null)
            {
                FindPanels();
            }
            
            if (p1Panel != null && p2Panel != null)
            {
                // Get panel positions (center of each panel)
                p1Position = GetPanelCenter(p1Panel);
                p2Position = GetPanelCenter(p2Panel);
                
                // Start at P1 position
                rectTransform.anchoredPosition = p1Position;
                currentTargetPosition = p1Position;
                
                // Subscribe to game state changes
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
                    
                    // Set initial state based on current game state
                    HandleGameStateChanged(GameManager.Instance.CurrentState);
                }
            }
            else
            {
                Debug.LogWarning("TurnIndicatorMoving: Could not find P1Panel or P2Panel! Will retry in Update.");
            }
        }
        
        private void Update()
        {
            // Retry finding panels if not found yet
            if (p1Panel == null || p2Panel == null)
            {
                FindPanels();
                if (p1Panel != null && p2Panel != null)
                {
                    p1Position = GetPanelCenter(p1Panel);
                    p2Position = GetPanelCenter(p2Panel);
                    rectTransform.anchoredPosition = p1Position;
                    currentTargetPosition = p1Position;
                }
                else
                {
                    return; // Still can't find panels
                }
            }
            
            // Update hover animation
            hoverOffset = Mathf.Sin(Time.time * hoverSpeed) * hoverHeight;
            
            // Update transition if moving
            if (isTransitioning)
            {
                transitionProgress += Time.deltaTime / transitionDuration;
                
                if (transitionProgress >= 1f)
                {
                    transitionProgress = 1f;
                    isTransitioning = false;
                }
                
                // Calculate figure-eight path position
                Vector2 position = CalculateFigureEightPosition(transitionProgress);
                rectTransform.anchoredPosition = position + new Vector2(0, hoverOffset);
            }
            else
            {
                // Just hover at current position
                rectTransform.anchoredPosition = currentTargetPosition + new Vector2(0, hoverOffset);
            }
            
            // Rotate continuously
            rectTransform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
            
            // Pulse scale
            float scalePulse = 1f + Mathf.Sin(Time.time * scalePulseSpeed) * scalePulseAmount;
            rectTransform.localScale = Vector3.one * baseScale * scalePulse;
        }
        
        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
            }
        }
        
        /// <summary>
        /// Calculate position along a figure-eight path between P1 and P2 panels.
        /// The path creates a smooth, readable motion similar to Navi from Zelda.
        /// </summary>
        private Vector2 CalculateFigureEightPosition(float t)
        {
            // Clamp t to 0-1
            t = Mathf.Clamp01(t);
            
            // Apply easing curve
            float easedT = motionCurve.Evaluate(t);
            
            // Calculate base linear interpolation
            Vector2 linearPos = Vector2.Lerp(p1Position, p2Position, easedT);
            
            // Calculate direction vector from P1 to P2
            Vector2 direction = (p2Position - p1Position);
            float distance = direction.magnitude;
            
            if (distance < 0.1f)
            {
                // Panels are too close, just return linear position
                return linearPos;
            }
            
            direction.Normalize();
            
            // Perpendicular vector (90-degree rotation counterclockwise)
            Vector2 perpendicular = new Vector2(-direction.y, direction.x);
            
            // Figure-eight pattern: creates two loops
            // Uses sin(2πt) for the figure-eight shape
            // First loop (0 to 0.5): curves in one direction
            // Second loop (0.5 to 1): curves in opposite direction
            float figureEightPhase = easedT * Mathf.PI * 2f; // Full 2π cycle
            float figureEightOffset = Mathf.Sin(figureEightPhase) * 40f; // Amplitude of 40 pixels
            
            // Apply perpendicular offset to create the figure-eight shape
            Vector2 offset = perpendicular * figureEightOffset;
            
            return linearPos + offset;
        }
        
        /// <summary>
        /// Handle game state changes to trigger movement.
        /// </summary>
        private void HandleGameStateChanged(GameState newState)
        {
            switch (newState)
            {
                case GameState.PlayerTurn:
                    MoveToP1();
                    break;
                case GameState.EnemyTurn:
                    MoveToP2();
                    break;
            }
        }
        
        /// <summary>
        /// Move indicator to P1 panel with figure-eight animation.
        /// </summary>
        public void MoveToP1()
        {
            if (p1Panel == null) return;
            
            currentTargetPosition = GetPanelCenter(p1Panel);
            
            // Only animate if we're not already at P1
            if (Vector2.Distance(rectTransform.anchoredPosition, currentTargetPosition) > 1f)
            {
                isMovingToP2 = false;
                isTransitioning = true;
                transitionProgress = isMovingToP2 ? 1f - transitionProgress : 0f; // Reverse if needed
            }
            
            UpdateVisuals(true);
        }
        
        /// <summary>
        /// Move indicator to P2 panel with figure-eight animation.
        /// </summary>
        public void MoveToP2()
        {
            if (p2Panel == null) return;
            
            currentTargetPosition = GetPanelCenter(p2Panel);
            
            // Only animate if we're not already at P2
            if (Vector2.Distance(rectTransform.anchoredPosition, currentTargetPosition) > 1f)
            {
                isMovingToP2 = true;
                isTransitioning = true;
                transitionProgress = isMovingToP2 ? 0f : 1f - transitionProgress; // Reverse if needed
            }
            
            UpdateVisuals(true);
        }
        
        /// <summary>
        /// Get the center position of a panel.
        /// </summary>
        private Vector2 GetPanelCenter(RectTransform panel)
        {
            if (panel == null) return Vector2.zero;
            
            // Get the center of the panel's rect
            Rect rect = panel.rect;
            Vector2 center = panel.anchoredPosition;
            
            // Adjust for pivot if needed
            Vector2 pivotOffset = new Vector2(
                (0.5f - panel.pivot.x) * rect.width,
                (0.5f - panel.pivot.y) * rect.height
            );
            
            return center + pivotOffset;
        }
        
        /// <summary>
        /// Find P1Panel and P2Panel in the scene.
        /// </summary>
        private void FindPanels()
        {
            if (p1Panel == null)
            {
                GameObject p1Obj = GameObject.Find("P1Panel");
                if (p1Obj != null)
                {
                    p1Panel = p1Obj.GetComponent<RectTransform>();
                }
            }
            
            if (p2Panel == null)
            {
                GameObject p2Obj = GameObject.Find("P2Panel");
                if (p2Obj != null)
                {
                    p2Panel = p2Obj.GetComponent<RectTransform>();
                }
            }
        }
        
        /// <summary>
        /// Update visual appearance based on active state.
        /// </summary>
        private void UpdateVisuals(bool isActive)
        {
            if (textIndicator != null)
            {
                textIndicator.color = isActive ? activeColor : inactiveColor;
                textIndicator.enabled = true; // Always visible, just changes color
            }
            else if (image != null)
            {
                image.color = isActive ? activeColor : inactiveColor;
                image.enabled = true;
            }
        }
        
        /// <summary>
        /// Create a diamond visual if no image or text is present.
        /// </summary>
        private void CreateDiamondVisual()
        {
            if (image == null)
            {
                image = gameObject.AddComponent<Image>();
            }
            
            // Create a simple diamond texture
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
            
            image.sprite = sprite;
        }
    }
}

