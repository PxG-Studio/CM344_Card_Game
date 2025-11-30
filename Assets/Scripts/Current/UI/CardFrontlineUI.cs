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
        
        [Header("Lagging Marker Settings (currently disabled)")]
        [Tooltip("Top gold triangle that used to lag behind the pivot line (now unused/hidden).")]
        [SerializeField] private RectTransform triangleTop;
        [Tooltip("Bottom gold triangle that used to lag behind the pivot line (now unused/hidden).")]
        [SerializeField] private RectTransform triangleBottom;
        
        [Header("Text Settings")]
        [SerializeField] private string titleText = "Battle Front Influence";
        [SerializeField] private string remainingFormat = "{0}";
        
        [Header("Animation Settings")]
        public float barLerpSpeed = 0.25f;
        public float flipDuration = 0.35f;
        public AnimationCurve flipCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        
        [Header("Delta Marker Settings")]
        [Tooltip("Show floating +/- delta markers whenever frontline influence changes.")]
        [SerializeField] private bool showDeltaMarkers = true;
        [Tooltip("Optional delay before showing the delta marker so it lines up with bar movement.")]
        [SerializeField] private float deltaMarkerDelay = 0.08f;
        
        [Header("Segment Settings")]
        [Tooltip("Number of discrete blocks used to visualise control across the board (4x4 = 16).")]
        [SerializeField] private int segmentCount = 16;
        
        private int lastRemainingFields = -1;
        private Coroutine flipCoroutine;
        private float lastPivotAnchorX = -1f;
        private int lastP1Control = 0;
        private int lastP2Control = 0;
        private int currentP1Control = 0;
        private int currentP2Control = 0;
        private bool hasFrontlineHistory = false;
        private bool hasAnyControl = false;
        
        /// <summary>
        /// Gets the current P1 control count from the Battle Front Influence bar.
        /// </summary>
        public int GetP1Control() => currentP1Control;
        
        /// <summary>
        /// Gets the current P2 control count from the Battle Front Influence bar.
        /// </summary>
        public int GetP2Control() => currentP2Control;
        
        // Discrete segments that represent each tile on the 4x4 board.
        private Image[] segments;
        private Color emptySegmentColor;
        private Color p1SegmentColor = new Color(1f, 0.5f, 0f, 1f); // Default orange
        private Color p2SegmentColor = new Color(0f, 0.8f, 0f, 1f); // Default green
        
        /// <summary>
        /// Gets the P1 color used in the Battle Front Influence bar (orange).
        /// Returns default orange if not yet initialized from UI.
        /// </summary>
        public Color P1Color => (p1SegmentColor != Color.clear && p1SegmentColor.a > 0f) 
            ? p1SegmentColor 
            : new Color(1f, 0.5f, 0f, 1f); // Fallback orange
        
        /// <summary>
        /// Gets the P2 color used in the Battle Front Influence bar (green).
        /// Returns default green if not yet initialized from UI.
        /// </summary>
        public Color P2Color => (p2SegmentColor != Color.clear && p2SegmentColor.a > 0f) 
            ? p2SegmentColor 
            : new Color(0f, 0.8f, 0f, 1f); // Fallback green
        
        private void Start()
        {
            if (titleLabel != null)
            {
                titleLabel.text = titleText;
            }
            
            UpdateRemainingFields(16); // Default for 4x4 board
            
            // Capture colours from the continuous fills, then hide them; the bar
            // will now be rendered using discrete segments instead.
            if (p1Fill != null)
            {
                p1SegmentColor = p1Fill.color;
                p1Fill.fillAmount = 0f;
                p1Fill.gameObject.SetActive(false);
            }
            if (p2Fill != null)
            {
                p2SegmentColor = p2Fill.color;
                p2Fill.fillAmount = 0f;
                p2Fill.gameObject.SetActive(false);
            }
            if (midDivider != null)
            {
                // Keep the divider confined to the bar region (bottom half of this
                // widget) so it doesn't poke up into the title text.
                midDivider.anchorMin = new Vector2(0.5f, 0f);
                midDivider.anchorMax = new Vector2(0.5f, 0.5f);
            }
            
            BuildSegments();

            // TEMP: hide gold triangle markers entirely so the label area above the bar
            // remains clean (no arrow under "Battle Front Influence").
            if (triangleTop != null)
            {
                triangleTop.gameObject.SetActive(false);
            }
            if (triangleBottom != null)
            {
                triangleBottom.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Resets the frontline UI to its initial empty state (used on rematch).
        /// </summary>
        public void ResetFrontline()
        {
            hasFrontlineHistory = false;
            hasAnyControl = false;
            lastP1Control = 0;
            lastP2Control = 0;
            currentP1Control = 0;
            currentP2Control = 0;
            lastRemainingFields = -1;

            // Clear segments to empty colour.
            if (segments != null)
            {
                foreach (var seg in segments)
                {
                    if (seg != null)
                    {
                        seg.color = emptySegmentColor;
                    }
                }
            }

            // Reset divider to centre.
            if (midDivider != null)
            {
                midDivider.anchorMin = new Vector2(0.5f, 0f);
                midDivider.anchorMax = new Vector2(0.5f, 0.5f);
            }

            // Reset remaining fields display back to full board.
            UpdateFrontline(0, 0, 16);
        }
        
        /// <summary>
        /// Called when any tile/card is placed or captured.
        /// Updates both the remaining fields counter and the tug-of-war bar.
        /// </summary>
        public void UpdateFrontline(int p1Control, int p2Control, int remainingFields)
        {
            // Store current control values for external access
            currentP1Control = p1Control;
            currentP2Control = p2Control;
            
            UpdateRemainingFields(remainingFields);
            
            // Show pretty delta markers for net morale change before updating the bar.
            if (showDeltaMarkers)
            {
                HandleDeltaMarkers(p1Control, p2Control);
            }
            
            UpdateFrontlineBar(p1Control, p2Control);
        }
        
        /// <summary>
        /// Computes the net frontline delta since the last update and spawns a
        /// floating +/- marker at the bar's pivot using DeltaMarkerSystem.
        /// Positive = swing toward Player 1, Negative = swing toward Player 2.
        /// </summary>
        private void HandleDeltaMarkers(int p1Control, int p2Control)
        {
            // First call just seeds history so we don't flash a bogus delta at start.
            if (!hasFrontlineHistory)
            {
                lastP1Control = p1Control;
                lastP2Control = p2Control;
                hasFrontlineHistory = true;
                return;
            }
            
            int prevP1Control = lastP1Control;
            int prevP2Control = lastP2Control;
            
            int deltaP1 = p1Control - prevP1Control;
            int deltaP2 = p2Control - prevP2Control;
            
            lastP1Control = p1Control;
            lastP2Control = p2Control;
            
            if (deltaP1 == 0 && deltaP2 == 0)
            {
                return;
            }
            
            // Derive how many visual segments correspond to each side before and
            // after the change so we can place the blurp on the specific tick
            // that was just added or removed.
            int totalTilesInt = Mathf.Max(1, p1Control + p2Control + Mathf.Max(0, lastRemainingFields));
            float totalTiles = totalTilesInt;
            int totalSegments = segments != null && segments.Length > 0 ? segments.Length : segmentCount;
            
            int prevP1Blocks = Mathf.Clamp(Mathf.RoundToInt((prevP1Control / totalTiles) * totalSegments), 0, totalSegments);
            int prevP2Blocks = Mathf.Clamp(Mathf.RoundToInt((prevP2Control / totalTiles) * totalSegments), 0, totalSegments);
            int p1Blocks     = Mathf.Clamp(Mathf.RoundToInt((p1Control     / totalTiles) * totalSegments), 0, totalSegments);
            int p2Blocks     = Mathf.Clamp(Mathf.RoundToInt((p2Control     / totalTiles) * totalSegments), 0, totalSegments);
            
            // Player 1: segments fill from the LEFT (indices 0 .. p1Blocks-1).
            if (deltaP1 != 0)
            {
                int targetIndex;
                if (deltaP1 > 0)
                {
                    // Newly gained tick is the last filled block on the left side.
                    targetIndex = Mathf.Clamp(p1Blocks - 1, 0, totalSegments - 1);
                }
                else
                {
                    // Lost tick was previously the last filled block.
                    targetIndex = Mathf.Clamp(prevP1Blocks - 1, 0, totalSegments - 1);
                }
                
                Transform anchor = (segments != null && targetIndex >= 0 && targetIndex < segments.Length)
                    ? segments[targetIndex].transform
                    : (midDivider != null ? midDivider.transform : transform);
                
                StartCoroutine(ShowFrontlineDelta(anchor, deltaP1));
            }
            
            // Player 2: segments fill from the RIGHT (indices totalSegments-1 .. totalSegments-p2Blocks).
            if (deltaP2 != 0)
            {
                int targetIndex;
                if (deltaP2 > 0)
                {
                    // Newly gained tick is the last filled block from the right side.
                    targetIndex = Mathf.Clamp(totalSegments - p2Blocks, 0, totalSegments - 1);
                }
                else
                {
                    // Lost tick was previously the last filled block on the right.
                    targetIndex = Mathf.Clamp(totalSegments - prevP2Blocks, 0, totalSegments - 1);
                }
                
                Transform anchor = (segments != null && targetIndex >= 0 && targetIndex < segments.Length)
                    ? segments[targetIndex].transform
                    : (midDivider != null ? midDivider.transform : transform);
                
                // Negative value indicates swing toward P2 for consistency with
                // existing delta semantics.
                StartCoroutine(ShowFrontlineDelta(anchor, -deltaP2));
            }
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
            // Automatic UI update - no log needed
            // Special case: at game start, no tiles are controlled by either side.
            // The bar should appear completely empty (white background only),
            // with the pivot centered.
            if (p1 == 0 && p2 == 0)
            {
                hasAnyControl = false;
                if (p1Fill != null) p1Fill.gameObject.SetActive(false);
                if (p2Fill != null) p2Fill.gameObject.SetActive(false);
                
                float emptyPivot = 0.5f;
                StartCoroutine(AnimateBarFill(0f, 0f, emptyPivot));
                AnimateLaggingMarkers(emptyPivot);
                return;
            }
            
            // From the first non-zero control onward, flag that at least one
            // side has influence (used by delta markers / future polish).
            if (!hasAnyControl)
            {
                hasAnyControl = true;
            }
            
            // Total tiles on the board (captured + remaining) – for a 4x4 grid this
            // should stabilise at 16 as the game progresses.
            int totalTilesInt = Mathf.Max(1, p1 + p2 + Mathf.Max(0, lastRemainingFields));
            float totalTiles = totalTilesInt;
            
            // Each side fills from its outer edge toward the centre in direct
            // proportion to how many tiles that side controls across the whole board.
            // One tile on a 4x4 board = 1/16 of that side's half, not the full half.
            float p1FillAmount = Mathf.Clamp01(p1 / totalTiles); // 0..1 overall
            float p2FillAmount = Mathf.Clamp01(p2 / totalTiles); // 0..1 overall
            
            // Update discrete segments instead of continuous fills.
            UpdateSegments(p1FillAmount, p2FillAmount);
            
            // Move the white divider and its gold triangles to reflect which side
            // currently leads. Advantage is in [-1,1]; pivot maps this into [0,1].
            float advantage = 0f;
            int controlled = p1 + p2;
            if (controlled > 0)
            {
                advantage = Mathf.Clamp((float)(p1 - p2) / controlled, -1f, 1f);
            }
            float pivot = Mathf.Clamp01(0.5f + advantage * 0.5f);
            AnimateLaggingMarkers(pivot);
        }

        /// <summary>
        /// Computes the current local X position of the pivot (midDivider) in its parent space.
        /// Uses the divider's anchoredPosition, which reflects its current anchor/position.
        /// </summary>
        private float GetPivotLocalX()
        {
            if (midDivider == null) return 0f;
            return midDivider.anchoredPosition.x;
        }
        
        /// <summary>
        /// Creates a row of segment images that span the bar. Each segment
        /// represents a single tile on the board (4x4 = 16 segments).
        /// </summary>
        private void BuildSegments()
        {
            if (segmentCount <= 0)
            {
                segmentCount = 16;
            }
            
            // Derive an empty colour from the background, or fall back to a dark tint.
            emptySegmentColor = new Color(0.10f, 0.15f, 0.22f, 1f);
            
            if (segments != null && segments.Length == segmentCount)
            {
                return; // already built
            }
            
            segments = new Image[segmentCount];
            
            RectTransform barRect = GetComponent<RectTransform>();
            if (barRect == null)
            {
                return;
            }
            
            // Container that stretches to the full bar area.
            GameObject containerObj = new GameObject("Segments");
            RectTransform container = containerObj.AddComponent<RectTransform>();
            container.SetParent(transform, false);
            container.anchorMin = new Vector2(0f, 0f);
            container.anchorMax = new Vector2(1f, 0.5f);
            container.pivot = new Vector2(0.5f, 0.5f);
            container.sizeDelta = Vector2.zero;
            container.anchoredPosition = Vector2.zero;
            
            for (int i = 0; i < segmentCount; i++)
            {
                GameObject segObj = new GameObject($"Segment_{i}");
                RectTransform segRect = segObj.AddComponent<RectTransform>();
                segRect.SetParent(container, false);
                
                float minX = (float)i / segmentCount;
                float maxX = (float)(i + 1) / segmentCount;
                segRect.anchorMin = new Vector2(minX, 0f);
                segRect.anchorMax = new Vector2(maxX, 1f);
                segRect.pivot = new Vector2(0.5f, 0.5f);
                segRect.sizeDelta = Vector2.zero;
                segRect.anchoredPosition = Vector2.zero;
                
                Image segImage = segObj.AddComponent<Image>();
                segImage.color = emptySegmentColor;
                segments[i] = segImage;
            }
        }
        
        /// <summary>
        /// Colours segments based on the fraction of the board controlled by each
        /// side. For a 4x4, 1 tile ≈ 1/16th of the bar.
        /// </summary>
        private void UpdateSegments(float p1Fraction, float p2Fraction)
        {
            if (segments == null || segments.Length == 0)
            {
                return;
            }
            
            int total = segments.Length;
            int p1Blocks = Mathf.Clamp(Mathf.RoundToInt(p1Fraction * total), 0, total);
            int p2Blocks = Mathf.Clamp(Mathf.RoundToInt(p2Fraction * total), 0, total);
            
            // Clear all first.
            for (int i = 0; i < total; i++)
            {
                segments[i].color = emptySegmentColor;
            }
            
            // Fill from the left for player 1.
            for (int i = 0; i < p1Blocks; i++)
            {
                segments[i].color = p1SegmentColor;
            }
            
            // Fill from the right for player 2.
            for (int i = 0; i < p2Blocks; i++)
            {
                int index = total - 1 - i;
                if (index >= 0 && index < total)
                {
                    segments[index].color = p2SegmentColor;
                }
            }
        }
        
        /// <summary>
        /// Spawns a floating delta marker at the frontline bar pivot, slightly
        /// delayed so it lines up with the bar animation.
        /// </summary>
        private IEnumerator ShowFrontlineDelta(Transform anchor, int deltaValue)
        {
            if (anchor == null)
            {
                yield break;
            }
            
            if (deltaMarkerDelay > 0f)
            {
                yield return new WaitForSeconds(deltaMarkerDelay);
            }

            // Prefer spawning in UI/screen space so the marker appears exactly over
            // the frontline bar, regardless of world camera setup.
            RectTransform rect = anchor as RectTransform;
            Vector3 uiPos;
            if (rect != null)
            {
                // For Screen Space - Overlay canvases, a null camera is correct.
                uiPos = RectTransformUtility.WorldToScreenPoint(null, rect.position);
            }
            else
            {
                uiPos = RectTransformUtility.WorldToScreenPoint(null, anchor.position);
            }

            // Delta marker shown - no log needed (automatic UI update)
            DeltaMarkerSystem.ShowDeltaAtUI(deltaValue, uiPos);
        }
        
        /// <summary>
        /// Animates the lagging gold triangle markers to follow the pivot line with a slight delay
        /// and a springy "snap" using DOTween. This produces the desired Dynasty Warriors-style
        /// morale swing effect where the pivot moves first and the markers catch up.
        /// </summary>
        /// <param name="targetPivotAnchorX">Target pivot position as an anchor ratio (0..1).</param>
        private void AnimateLaggingMarkers(float targetPivotAnchorX)
        {
            if (midDivider == null || (triangleTop == null && triangleBottom == null))
            {
                return;
            }
            
            // Only react when the pivot actually changes meaningfully
            if (Mathf.Approximately(lastPivotAnchorX, targetPivotAnchorX))
            {
                return;
            }
            
            lastPivotAnchorX = targetPivotAnchorX;
            
            // Smoothly move the pivot's anchors (divider) using DOTween instead of manual lerp.
            // This keeps the existing behaviour (barLerpSpeed) but adds a bit more polish.
            float startMinX = midDivider.anchorMin.x;
            float startMaxX = midDivider.anchorMax.x;

            // Instantly move the divider pivot to the target anchor position.
            // We keep the original width by preserving the delta between min and max.
            float width = startMaxX - startMinX;
            midDivider.anchorMin = new Vector2(targetPivotAnchorX, 0f);
            midDivider.anchorMax = new Vector2(targetPivotAnchorX + width, 0.5f);
            
            // Compute the actual local X position for the triangles to move toward.
            float targetLocalX = GetPivotLocalX();
            
            // Move triangles to align with the new pivot (no tween dependency).
            if (triangleTop != null)
            {
                Vector2 topPos = triangleTop.anchoredPosition;
                triangleTop.anchoredPosition = new Vector2(targetLocalX, topPos.y);
            }
            
            if (triangleBottom != null)
            {
                Vector2 bottomPos = triangleBottom.anchoredPosition;
                triangleBottom.anchoredPosition = new Vector2(targetLocalX, bottomPos.y);
            }
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
                midDivider.anchorMax = new Vector2(dividerPos, 0.5f);
                
                yield return null;
            }
            
            // Ensure final values are exact
            p1Fill.fillAmount = targetP1Ratio;
            p2Fill.fillAmount = targetP2Ratio;
            midDivider.anchorMin = new Vector2(targetDividerPos, 0f);
            midDivider.anchorMax = new Vector2(targetDividerPos, 0.5f);
            
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

