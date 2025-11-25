using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace CardGame.UI
{
    /// <summary>
    /// UI component for displaying Card Frontline counter with flip-clock animation
    /// and tug-of-war bar showing P1/P2 control.
    /// </summary>
    public class CardFrontlineUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI titleLabel;
        [SerializeField] private TextMeshProUGUI remainingLabel;
        [SerializeField] private Image p1Fill;
        [SerializeField] private Image p2Fill;
        [SerializeField] private RectTransform midDivider;
        
        [Header("Text Settings")]
        [SerializeField] private string titleText = "Battle Front Influence";
        [SerializeField] private string remainingFormat = "Open Fields: {0}";
        
        [Header("Animation Settings")]
        public float barLerpSpeed = 0.25f;
        public float flipDuration = 0.35f;
        public AnimationCurve flipCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        
        private int lastRemainingFields = -1;
        private Coroutine flipCoroutine;
        
        private void Start()
        {
            if (titleLabel != null)
            {
                titleLabel.text = titleText;
            }
            
            UpdateRemainingFields(16); // Default for 4x4 board
        }
        
        /// <summary>
        /// Called when any tile/card is placed or captured.
        /// Updates both the remaining fields counter and the tug-of-war bar.
        /// </summary>
        public void UpdateFrontline(int p1Control, int p2Control, int remainingFields)
        {
            UpdateRemainingFields(remainingFields);
            UpdateFrontlineBar(p1Control, p2Control);
        }
        
        private void UpdateRemainingFields(int remaining)
        {
            if (remaining == lastRemainingFields) return;
            
            string nextText = string.Format(remainingFormat, remaining);
            lastRemainingFields = remaining;
            
            // Stop any existing flip animation
            if (flipCoroutine != null)
            {
                StopCoroutine(flipCoroutine);
            }
            
            // Start flip-clock animation
            flipCoroutine = StartCoroutine(FlipClockAnimation(nextText));
        }
        
        private IEnumerator FlipClockAnimation(string nextText)
        {
            if (remainingLabel == null)
            {
                yield break;
            }
            
            Transform labelTransform = remainingLabel.transform;
            Vector3 originalRotation = labelTransform.localEulerAngles;
            
            // Rotate to 90 degrees (flip away)
            float elapsed = 0f;
            while (elapsed < flipDuration * 0.5f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (flipDuration * 0.5f);
                float curveValue = flipCurve.Evaluate(t);
                
                float xRotation = Mathf.Lerp(0f, 90f, curveValue);
                labelTransform.localEulerAngles = new Vector3(xRotation, originalRotation.y, originalRotation.z);
                yield return null;
            }
            
            // Update text while hidden
            remainingLabel.text = nextText;
            labelTransform.localEulerAngles = new Vector3(90f, originalRotation.y, originalRotation.z);
            
            // Rotate back to 0 degrees (flip in)
            elapsed = 0f;
            while (elapsed < flipDuration * 0.5f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (flipDuration * 0.5f);
                float curveValue = flipCurve.Evaluate(t);
                
                float xRotation = Mathf.Lerp(90f, 0f, curveValue);
                labelTransform.localEulerAngles = new Vector3(xRotation, originalRotation.y, originalRotation.z);
                yield return null;
            }
            
            // Ensure final rotation is exact
            labelTransform.localEulerAngles = originalRotation;
            flipCoroutine = null;
        }
        
        private void UpdateFrontlineBar(int p1, int p2)
        {
            float total = Mathf.Max(1, p1 + p2);
            float p1Ratio = p1 / total;
            float p2Ratio = p2 / total;
            
            // Start coroutine to animate fill amounts
            StartCoroutine(AnimateBarFill(p1Ratio, p2Ratio, p1Ratio));
        }
        
        private IEnumerator AnimateBarFill(float targetP1Ratio, float targetP2Ratio, float targetDividerPos)
        {
            float startP1Fill = p1Fill.fillAmount;
            float startP2Fill = p2Fill.fillAmount;
            float startDividerPos = midDivider.anchorMin.x;
            
            float elapsed = 0f;
            while (elapsed < barLerpSpeed)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / barLerpSpeed;
                
                // Smooth lerp
                p1Fill.fillAmount = Mathf.Lerp(startP1Fill, targetP1Ratio, t);
                p2Fill.fillAmount = Mathf.Lerp(startP2Fill, targetP2Ratio, t);
                
                // Move divider
                float dividerPos = Mathf.Lerp(startDividerPos, targetDividerPos, t);
                midDivider.anchorMin = new Vector2(dividerPos, 0f);
                midDivider.anchorMax = new Vector2(dividerPos, 1f);
                
                yield return null;
            }
            
            // Ensure final values are exact
            p1Fill.fillAmount = targetP1Ratio;
            p2Fill.fillAmount = targetP2Ratio;
            midDivider.anchorMin = new Vector2(targetDividerPos, 0f);
            midDivider.anchorMax = new Vector2(targetDividerPos, 1f);
            
            // Pulse divider
            StartCoroutine(PulseDivider());
        }
        
        private IEnumerator PulseDivider()
        {
            Vector3 originalScale = midDivider.localScale;
            Vector3 pulseScale = originalScale * 1.15f;
            
            float elapsed = 0f;
            float pulseDuration = 0.3f;
            
            // Scale up
            while (elapsed < pulseDuration * 0.5f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (pulseDuration * 0.5f);
                midDivider.localScale = Vector3.Lerp(originalScale, pulseScale, t);
                yield return null;
            }
            
            // Scale down
            elapsed = 0f;
            while (elapsed < pulseDuration * 0.5f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (pulseDuration * 0.5f);
                midDivider.localScale = Vector3.Lerp(pulseScale, originalScale, t);
                yield return null;
            }
            
            midDivider.localScale = originalScale;
        }
    }
}

