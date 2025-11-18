using System.Collections;
using UnityEngine;
using CardGame.Managers;

namespace CardGame.UI
{
    /// <summary>
    /// Visual indicator showing whose turn it is with a glowing arrow
    /// Moves to left (player) or right (opponent) side of the board
    /// </summary>
    public class TurnIndicator : MonoBehaviour
    {
        [Header("Arrow References")]
        [SerializeField] private GameObject arrowObject; // The arrow GameObject (SpriteRenderer or Image)
        [SerializeField] private SpriteRenderer arrowSpriteRenderer; // For 2D arrow
        [SerializeField] private UnityEngine.UI.Image arrowImage; // For UI arrow

        [Header("Position Settings")]
        [SerializeField] private Transform boardCenter; // Center of the game board
        [SerializeField] private float leftOffset = -5f; // Distance from board center for player side (left)
        [SerializeField] private float rightOffset = 5f; // Distance from board center for opponent side (right)
        [SerializeField] private float verticalOffset = 0f; // Vertical offset from board center

        [Header("Animation Settings")]
        [SerializeField] private float moveSpeed = 2f; // Speed of position transition
        [SerializeField] private float glowIntensity = 1.5f; // Intensity of the glow effect
        [SerializeField] private float glowPulseSpeed = 2f; // Speed of pulsing glow animation
        [SerializeField] private float glowPulseMin = 0.8f; // Minimum glow intensity
        [SerializeField] private float glowPulseMax = 1.5f; // Maximum glow intensity

        [Header("Colors")]
        [SerializeField] private Color playerColor = new Color(1f, 0.5f, 0f, 1f); // Orange for player
        [SerializeField] private Color opponentColor = new Color(0f, 0.8f, 0f, 1f); // Green for opponent

        [Header("Glow Effect")]
        [SerializeField] private bool useGlowEffect = true;
        [SerializeField] private Material glowMaterial; // Optional: Custom material for glow effect
        // Note: Light2D requires Universal Render Pipeline package - make it optional
        [SerializeField] private Component glowLight; // Optional: 2D light component for glow (Light2D if URP is installed)

        private Vector3 targetPosition;
        private Color currentColor;
        private Color targetColor;
        private bool isAnimating = false;
        private Coroutine moveCoroutine;
        private Coroutine glowCoroutine;

        private void Awake()
        {
            // Auto-detect arrow components if not assigned
            if (arrowObject == null)
            {
                arrowObject = gameObject;
            }

            if (arrowSpriteRenderer == null)
            {
                arrowSpriteRenderer = GetComponent<SpriteRenderer>();
                if (arrowSpriteRenderer == null)
                {
                    arrowSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
                }
            }

            if (arrowImage == null)
            {
                arrowImage = GetComponent<UnityEngine.UI.Image>();
                if (arrowImage == null)
                {
                    arrowImage = GetComponentInChildren<UnityEngine.UI.Image>();
                }
            }

            // Auto-find board center if not assigned (look for common board objects)
            if (boardCenter == null)
            {
                GameObject board = GameObject.Find("Board") ?? GameObject.Find("GameBoard") ?? GameObject.Find("CardBoard");
                if (board != null)
                {
                    boardCenter = board.transform;
                }
                else
                {
                    // Default to world origin
                    boardCenter = new GameObject("BoardCenter").transform;
                    boardCenter.position = Vector3.zero;
                }
            }

            // Initialize position
            if (boardCenter != null)
            {
                targetPosition = new Vector3(
                    boardCenter.position.x + leftOffset,
                    boardCenter.position.y + verticalOffset,
                    transform.position.z
                );
                transform.position = targetPosition;
            }

            // Initialize color
            currentColor = playerColor;
            targetColor = playerColor;
            ApplyColor(currentColor);
        }

        private void Start()
        {
            // Subscribe to game state changes
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
                // Set initial state
                HandleGameStateChanged(GameManager.Instance.CurrentState);
            }
            else
            {
                Debug.LogWarning("TurnIndicator: GameManager.Instance is null! Turn indicator will not update.");
            }

            // Start glow animation
            if (useGlowEffect)
            {
                StartGlowAnimation();
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
            }

            // Stop coroutines
            if (moveCoroutine != null)
            {
                StopCoroutine(moveCoroutine);
            }
            if (glowCoroutine != null)
            {
                StopCoroutine(glowCoroutine);
            }
        }

        private void HandleGameStateChanged(GameState newState)
        {
            switch (newState)
            {
                case GameState.PlayerTurn:
                    SetTurnIndicator(true); // Player turn = left side, orange
                    break;
                case GameState.EnemyTurn:
                    SetTurnIndicator(false); // Opponent turn = right side, green
                    break;
                default:
                    // Hide or keep current state for other game states
                    break;
            }
        }

        /// <summary>
        /// Sets the turn indicator position and color
        /// </summary>
        /// <param name="isPlayerTurn">True for player turn (left, orange), false for opponent turn (right, green)</param>
        public void SetTurnIndicator(bool isPlayerTurn)
        {
            if (boardCenter == null) return;

            // Calculate target position
            float horizontalOffset = isPlayerTurn ? leftOffset : rightOffset;
            targetPosition = new Vector3(
                boardCenter.position.x + horizontalOffset,
                boardCenter.position.y + verticalOffset,
                transform.position.z
            );

            // Set target color
            targetColor = isPlayerTurn ? playerColor : opponentColor;

            // Start smooth transition
            if (moveCoroutine != null)
            {
                StopCoroutine(moveCoroutine);
            }
            moveCoroutine = StartCoroutine(MoveToPositionCoroutine());

            // Update arrow direction (point towards board center)
            if (isPlayerTurn)
            {
                // Point right (towards board)
                transform.rotation = Quaternion.Euler(0, 0, 0);
            }
            else
            {
                // Point left (towards board)
                transform.rotation = Quaternion.Euler(0, 0, 180);
            }
        }

        private IEnumerator MoveToPositionCoroutine()
        {
            isAnimating = true;
            Vector3 startPosition = transform.position;
            Color startColor = currentColor;
            float elapsed = 0f;
            float duration = Vector3.Distance(startPosition, targetPosition) / moveSpeed;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                // Smooth easing
                t = Mathf.SmoothStep(0f, 1f, t);

                // Interpolate position
                transform.position = Vector3.Lerp(startPosition, targetPosition, t);

                // Interpolate color
                currentColor = Color.Lerp(startColor, targetColor, t);
                ApplyColor(currentColor);

                yield return null;
            }

            // Ensure final values
            transform.position = targetPosition;
            currentColor = targetColor;
            ApplyColor(currentColor);
            isAnimating = false;
            moveCoroutine = null;
        }

        private void StartGlowAnimation()
        {
            if (glowCoroutine != null)
            {
                StopCoroutine(glowCoroutine);
            }
            glowCoroutine = StartCoroutine(GlowPulseCoroutine());
        }

        private IEnumerator GlowPulseCoroutine()
        {
            while (true)
            {
                float pulse = Mathf.Lerp(glowPulseMin, glowPulseMax, (Mathf.Sin(Time.time * glowPulseSpeed) + 1f) * 0.5f);
                Color pulsedColor = currentColor * pulse;
                ApplyColor(pulsedColor);

                // Update 2D light if available (using reflection to avoid URP dependency)
                if (glowLight != null)
                {
                    var intensityProperty = glowLight.GetType().GetProperty("intensity");
                    var colorProperty = glowLight.GetType().GetProperty("color");
                    if (intensityProperty != null)
                    {
                        intensityProperty.SetValue(glowLight, pulse * glowIntensity);
                    }
                    if (colorProperty != null)
                    {
                        colorProperty.SetValue(glowLight, currentColor);
                    }
                }

                yield return null;
            }
        }

        private void ApplyColor(Color color)
        {
            // Apply to SpriteRenderer
            if (arrowSpriteRenderer != null)
            {
                arrowSpriteRenderer.color = color;
                if (glowMaterial != null)
                {
                    arrowSpriteRenderer.material = glowMaterial;
                }
            }

            // Apply to Image
            if (arrowImage != null)
            {
                arrowImage.color = color;
                if (glowMaterial != null)
                {
                    arrowImage.material = glowMaterial;
                }
            }
        }

        /// <summary>
        /// Manually set the board center reference
        /// </summary>
        public void SetBoardCenter(Transform center)
        {
            boardCenter = center;
            // Update position immediately if we have a current turn state
            if (GameManager.Instance != null)
            {
                HandleGameStateChanged(GameManager.Instance.CurrentState);
            }
        }

        /// <summary>
        /// Show or hide the indicator
        /// </summary>
        public void SetVisible(bool visible)
        {
            if (arrowObject != null)
            {
                arrowObject.SetActive(visible);
            }
            else
            {
                gameObject.SetActive(visible);
            }
        }
    }
}

