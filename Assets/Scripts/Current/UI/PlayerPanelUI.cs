using UnityEngine;
using TMPro;
using System.Collections;

namespace CardGame.UI
{
    /// <summary>
    /// UI component for player panels with Field Control display and dynamic blurp messages.
    /// </summary>
    public class PlayerPanelUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI fieldControlLabel;
        [SerializeField] private TextMeshProUGUI blurpLabel;
        
        [Header("Animation Settings")]
        public float blurpFadeInDuration = 0.25f;
        public float blurpPulseDuration = 0.3f;
        public float blurpFadeOutDuration = 1.2f;
        public AnimationCurve blurpFadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        
        private Coroutine blurpCoroutine;
        private CanvasGroup blurpCanvasGroup;
        
        private void Awake()
        {
            // Ensure blurp has a CanvasGroup for fade effects
            if (blurpLabel != null)
            {
                blurpCanvasGroup = blurpLabel.GetComponent<CanvasGroup>();
                if (blurpCanvasGroup == null)
                {
                    blurpCanvasGroup = blurpLabel.gameObject.AddComponent<CanvasGroup>();
                }
                
                // Start hidden
                blurpCanvasGroup.alpha = 0f;
                blurpCanvasGroup.blocksRaycasts = false;
                blurpCanvasGroup.interactable = false;
            }
        }
        
        /// <summary>
        /// Updates the Field Control display value.
        /// </summary>
        public void SetFieldControl(int value)
        {
            if (fieldControlLabel != null)
            {
                fieldControlLabel.text = $"Field Control: {value}";
            }
        }
        
        /// <summary>
        /// Triggers a blurp message with fade in, pulse, and fade out animation.
        /// </summary>
        public void TriggerBlurp(string message)
        {
            if (blurpLabel == null || blurpCanvasGroup == null) return;
            
            // Stop any existing blurp animation
            if (blurpCoroutine != null)
            {
                StopCoroutine(blurpCoroutine);
            }
            
            blurpLabel.text = message;
            blurpCoroutine = StartCoroutine(BlurpAnimation());
        }
        
        /// <summary>
        /// Clears the blurp text immediately.
        /// </summary>
        public void ClearBlurp()
        {
            if (blurpCanvasGroup != null)
            {
                blurpCanvasGroup.alpha = 0f;
            }
            if (blurpLabel != null)
            {
                blurpLabel.text = "";
            }
            if (blurpCoroutine != null)
            {
                StopCoroutine(blurpCoroutine);
                blurpCoroutine = null;
            }
        }
        
        private IEnumerator BlurpAnimation()
        {
            // Fade in
            float elapsed = 0f;
            while (elapsed < blurpFadeInDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / blurpFadeInDuration;
                float curveValue = blurpFadeCurve.Evaluate(t);
                blurpCanvasGroup.alpha = curveValue;
                yield return null;
            }
            blurpCanvasGroup.alpha = 1f;
            
            // Scale pulse (0.6x → 1.0x)
            Transform blurpTransform = blurpLabel.transform;
            Vector3 originalScale = Vector3.one;
            Vector3 pulseScale = originalScale * 0.6f;
            
            elapsed = 0f;
            while (elapsed < blurpPulseDuration * 0.5f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (blurpPulseDuration * 0.5f);
                blurpTransform.localScale = Vector3.Lerp(pulseScale, originalScale, t * t); // Ease out
                yield return null;
            }
            blurpTransform.localScale = originalScale;
            
            // Fade out
            elapsed = 0f;
            while (elapsed < blurpFadeOutDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / blurpFadeOutDuration;
                float curveValue = 1f - blurpFadeCurve.Evaluate(t);
                blurpCanvasGroup.alpha = curveValue;
                yield return null;
            }
            blurpCanvasGroup.alpha = 0f;
            blurpLabel.text = "";
            blurpCoroutine = null;
        }
    }
}

