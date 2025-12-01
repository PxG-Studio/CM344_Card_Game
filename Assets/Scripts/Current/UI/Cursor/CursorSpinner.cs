using UnityEngine;
using UnityEngine.UI;

#if DOTWEEN_AVAILABLE
using DG.Tweening;
#endif

namespace CardGame.UI.CursorSystem
{
    /// <summary>
    /// Handles the animated cursor visual: follows the mouse, spins, and adds hover/pulse motion.
    /// </summary>
    public class CursorSpinner : MonoBehaviour
    {
        [Header("Motion Settings")]
        [SerializeField] private float rotationSpeed = 120f;
        [SerializeField] private float hoverDistance = 10f;
        [SerializeField] private float hoverSpeed = 2.5f;
        [SerializeField] private float pulseAmount = 0.08f;
        [SerializeField] private float pulseSpeed = 1.35f;
        [SerializeField] private bool hideSystemCursor = true;
        
        private RectTransform rootRect;
        private RectTransform visualRect;
        private CanvasGroup visualGroup;
        private Image visualImage;
        private float hoverTimer;
        private bool initialized;

#if DOTWEEN_AVAILABLE
        private Sequence hoverSequence;
        private Tween pulseTween;
#endif

        /// <summary>
        /// Injects runtime references after the cursor visual is created.
        /// </summary>
        public void Initialize(RectTransform visual, Image image, float cursorSize, Color tint)
        {
            rootRect = GetComponent<RectTransform>();
            visualRect = visual;
            visualImage = image;
            
            if (visualRect == null || visualImage == null)
            {
                return;
            }
            
            visualRect.sizeDelta = new Vector2(cursorSize, cursorSize);
            visualRect.pivot = new Vector2(0.5f, 0.8f); // Tip of the triangle stays under the mouse
            visualRect.anchoredPosition = Vector2.zero;
            
            visualImage.raycastTarget = false;
            visualImage.preserveAspect = true;
            visualImage.color = tint;
            
            visualGroup = visualRect.GetComponent<CanvasGroup>();
            if (visualGroup == null)
            {
                visualGroup = visualRect.gameObject.AddComponent<CanvasGroup>();
            }
            visualGroup.alpha = 1f;
            
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = Vector2.zero;
            
            hoverTimer = Random.Range(0f, Mathf.PI * 2f);
            initialized = true;
        }

        /// <summary>
        /// Applies runtime animation parameters (can be called multiple times).
        /// </summary>
        public void ApplySettings(float rotation, float hoverDist, float hoverSpd, float pulseAmt, float pulseSpd, bool hideCursor)
        {
            rotationSpeed = rotation;
            hoverDistance = hoverDist;
            hoverSpeed = hoverSpd;
            pulseAmount = pulseAmt;
            pulseSpeed = pulseSpd;
            hideSystemCursor = hideCursor;
#if DOTWEEN_AVAILABLE
            SetupDotweenAnimation();
#endif
        }

        private void OnEnable()
        {
            if (hideSystemCursor)
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Confined;
            }
        }

        private void OnDisable()
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
#if DOTWEEN_AVAILABLE
            hoverSequence?.Kill();
            pulseTween?.Kill();
#endif
        }

        private void LateUpdate()
        {
            if (!initialized || visualRect == null)
            {
                return;
            }
            
            FollowMouse();
            ApplyRotation();
#if DOTWEEN_AVAILABLE
            // DOTween handles hover + pulse when present
#else
            ApplyHover();
            ApplyPulse();
#endif
        }

        private void FollowMouse()
        {
            rootRect.position = Input.mousePosition;
        }

        private void ApplyRotation()
        {
            visualRect.Rotate(0f, 0f, -rotationSpeed * Time.unscaledDeltaTime);
        }

        private void ApplyHover()
        {
            hoverTimer += Time.unscaledDeltaTime * hoverSpeed;
            float offset = Mathf.Sin(hoverTimer) * hoverDistance;
            visualRect.anchoredPosition = new Vector2(0f, offset);
        }

        private void ApplyPulse()
        {
            float scale = 1f + Mathf.Sin(hoverTimer * pulseSpeed) * pulseAmount;
            visualRect.localScale = Vector3.one * scale;
        }

#if DOTWEEN_AVAILABLE
        private void SetupDotweenAnimation()
        {
            if (!initialized || visualRect == null) return;
            
            hoverSequence?.Kill();
            pulseTween?.Kill();
            
            hoverSequence = DOTween.Sequence();
            hoverSequence.Append(visualRect.DOAnchorPosY(hoverDistance, 0.4f).SetEase(Ease.InOutSine));
            hoverSequence.Append(visualRect.DOAnchorPosY(-hoverDistance, 0.4f).SetEase(Ease.InOutSine));
            hoverSequence.SetLoops(-1, LoopType.Yoyo);
            
            pulseTween = visualRect
                .DOScale(Vector3.one * (1f + pulseAmount), 0.35f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }
#endif
    }
}

