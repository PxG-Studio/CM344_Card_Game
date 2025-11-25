using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
#if DOTWEEN_AVAILABLE
using DG.Tweening;
#endif

namespace CardGame.UI
{
    /// <summary>
    /// Individual delta marker popup component that animates a "+1" or "-1" text.
    /// Handles scale punch, upward float, and fade-out animation using DOTween.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class DeltaMarkerPopup : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TextMeshProUGUI deltaText;
        
        private DeltaMarkerConfig config;
        private Coroutine animationCoroutine;
#if DOTWEEN_AVAILABLE
        private Sequence animationSequence;
#endif
        
        private void Awake()
        {
            // Auto-find components if not assigned
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }
            
            if (deltaText == null)
            {
                deltaText = GetComponentInChildren<TextMeshProUGUI>();
                if (deltaText == null)
                {
                    deltaText = GetComponent<TextMeshProUGUI>();
                }
            }
            
            // Ensure CanvasGroup is properly set up
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
        }
        
        /// <summary>
        /// Initializes the popup with a delta value and configuration.
        /// </summary>
        /// <param name="value">The delta value to display (positive = conquer, negative = raze)</param>
        /// <param name="color">Color for the text</param>
        /// <param name="config">Configuration asset containing animation parameters</param>
        public void Initialize(int value, Color color, DeltaMarkerConfig config, string overrideText = null)
        {
            if (config == null)
            {
                Debug.LogError("[DeltaMarkerPopup] Config is null! Cannot initialize popup.");
                return;
            }
            
            this.config = config;
            
            // Set up text
            if (deltaText != null)
            {
                // Format the value with sign
                string sign = value >= 0 ? "+" : "";
                deltaText.text = string.IsNullOrEmpty(overrideText) ? $"{sign}{value}" : overrideText;
                deltaText.color = color;
                deltaText.fontSize = config.FontSize;
                
                // Apply font if specified
                if (config.DeltaFont != null)
                {
                    deltaText.font = config.DeltaFont;
                }
                
                // Make text bold
                deltaText.fontStyle = FontStyles.Bold;
            }
            
            // Reset transform
            transform.localScale = Vector3.zero;
            
            // Start animation
            PlayAnimation();
        }
        
        /// <summary>
        /// Plays the complete animation sequence: scale punch → float upward → fade out.
        /// Uses coroutines (compatible with existing codebase) or DOTween if available.
        /// </summary>
        private void PlayAnimation()
        {
            if (config == null)
            {
                Debug.LogError("[DeltaMarkerPopup] Config is null! Cannot play animation.");
                return;
            }
            
            // Stop any existing animation
            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
            }
            
#if DOTWEEN_AVAILABLE
            // Use DOTween if available
            PlayAnimationWithDOTween();
#else
            // Use coroutines (default)
            animationCoroutine = StartCoroutine(PlayAnimationCoroutine());
#endif
        }
        
#if DOTWEEN_AVAILABLE
        /// <summary>
        /// DOTween-based animation (if DOTween is available).
        /// </summary>
        private void PlayAnimationWithDOTween()
        {
            // Kill any existing sequence
            if (animationSequence != null && animationSequence.IsActive())
            {
                animationSequence.Kill();
            }
            
            // Create new sequence
            animationSequence = DOTween.Sequence();
            
            // 1. Scale punch (appear)
            animationSequence.Append(transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack));
            
            // 2. Scale punch effect
            animationSequence.Append(transform.DOPunchScale(
                Vector3.one * (config.ScalePunchAmount - 1f), 
                0.3f, 
                5, 
                0.5f
            ).SetEase(Ease.OutQuad));
            
            // 3. Float upward while fading out
            float floatDuration = config.Duration - 0.5f; // Reserve time for punch
            if (floatDuration > 0f)
            {
                // Calculate target position (upward)
                Vector3 startPos = transform.localPosition;
                Vector3 endPos = startPos + Vector3.up * config.FloatDistance;
                
                // Move upward with curve
                animationSequence.Append(transform.DOLocalMove(endPos, floatDuration)
                    .SetEase(config.FloatCurve));
                
                // Fade out simultaneously
                if (canvasGroup != null)
                {
                    animationSequence.Join(canvasGroup.DOFade(0f, floatDuration).SetEase(Ease.InQuad));
                }
            }
            else
            {
                // If duration is too short, just fade out
                if (canvasGroup != null)
                {
                    animationSequence.Append(canvasGroup.DOFade(0f, 0.5f));
                }
            }
            
            // 4. Destroy when complete
            animationSequence.OnComplete(() =>
            {
                if (gameObject != null)
                {
                    Destroy(gameObject);
                }
            });
            
            animationSequence.Play();
        }
#endif
        
        /// <summary>
        /// Coroutine-based animation (default, compatible with existing codebase).
        /// </summary>
        private IEnumerator PlayAnimationCoroutine()
        {
            Vector3 startPosition = transform.localPosition;
            Vector3 startScale = Vector3.zero;
            Vector3 targetScale = Vector3.one;
            
            // 1. Scale punch (appear) - 0.2s
            float appearDuration = 0.2f;
            float elapsed = 0f;
            
            while (elapsed < appearDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / appearDuration;
                // OutBack easing approximation
                float easedT = 1f - Mathf.Pow(1f - t, 3f);
                transform.localScale = Vector3.Lerp(startScale, targetScale, easedT);
                yield return null;
            }
            transform.localScale = targetScale;
            
            // 2. Scale punch effect - 0.3s
            float punchDuration = 0.3f;
            float punchAmount = config.ScalePunchAmount - 1f;
            elapsed = 0f;
            Vector3 baseScale = targetScale;
            
            while (elapsed < punchDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / punchDuration;
                // Oscillating punch effect
                float punch = Mathf.Sin(t * Mathf.PI * 5f) * (1f - t) * punchAmount;
                transform.localScale = baseScale * (1f + punch);
                yield return null;
            }
            transform.localScale = baseScale;
            
            // 3. Float upward while fading out
            float floatDuration = config.Duration - 0.5f; // Reserve time for punch
            if (floatDuration > 0f)
            {
                Vector3 endPosition = startPosition + Vector3.up * config.FloatDistance;
                float startAlpha = canvasGroup != null ? canvasGroup.alpha : 1f;
                elapsed = 0f;
                
                while (elapsed < floatDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / floatDuration;
                    
                    // Use animation curve for float motion
                    float curveT = config.FloatCurve != null ? config.FloatCurve.Evaluate(t) : t;
                    transform.localPosition = Vector3.Lerp(startPosition, endPosition, curveT);
                    
                    // Fade out
                    if (canvasGroup != null)
                    {
                        float alpha = Mathf.Lerp(startAlpha, 0f, t * t); // Quadratic fade
                        canvasGroup.alpha = alpha;
                    }
                    
                    yield return null;
                }
                
                transform.localPosition = endPosition;
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 0f;
                }
            }
            
            // Destroy when complete
            if (gameObject != null)
            {
                Destroy(gameObject);
            }
        }
        
        private void OnDestroy()
        {
            // Clean up coroutine
            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
            }
            
#if DOTWEEN_AVAILABLE
            // Clean up DOTween sequence if used
            if (animationSequence != null && animationSequence.IsActive())
            {
                animationSequence.Kill();
            }
#endif
        }
        
        #if UNITY_EDITOR
        /// <summary>
        /// Editor helper to test the popup in the scene view.
        /// </summary>
        [ContextMenu("Test Popup (+1)")]
        private void TestPopupPositive()
        {
            if (config == null)
            {
                Debug.LogWarning("[DeltaMarkerPopup] No config assigned. Create a DeltaMarkerConfig asset first.");
                return;
            }
            
            Initialize(1, config.ConquerColor, config);
        }
        
        [ContextMenu("Test Popup (-1)")]
        private void TestPopupNegative()
        {
            if (config == null)
            {
                Debug.LogWarning("[DeltaMarkerPopup] No config assigned. Create a DeltaMarkerConfig asset first.");
                return;
            }
            
            Initialize(-1, config.RazeColor, config);
        }
        #endif
    }
}

