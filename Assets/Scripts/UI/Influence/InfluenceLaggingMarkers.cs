using UnityEngine;

/// <summary>
/// Drives a tug-of-war influence bar with a moving pivot line and two lagging,
/// spinning triangle markers that "chase" influence changes with a dramatic delay.
/// Attach this to the root InfluenceBar GameObject and wire up the RectTransform
/// references in the Inspector.
/// </summary>
public class InfluenceLaggingMarkers : MonoBehaviour
{
    [Header("References")]
    public RectTransform leftFill;      // The left (orange) fill rect whose width reflects P1 influence
    public RectTransform pivotLine;     // The vertical pivot line in the center of the bar
    public RectTransform triangleTop;   // ▼ points downward, positioned above the bar
    public RectTransform triangleBottom;// ▲ points upward, positioned below the bar

    [Header("Settings")]
    public float verticalOffset = 30f;
    public float pivotMoveDuration = 0.25f;
    public float triangleLagDelay = 0.10f;
    public float triangleMoveDuration = 0.40f;
    public float spinDuration = 0.35f;

    private float lastPivotX;

    private void Start()
    {
        if (leftFill != null)
        {
            lastPivotX = leftFill.sizeDelta.x;
        }
    }

    private void Update()
    {
        if (leftFill == null || pivotLine == null)
        {
            return;
        }

        float pivotX = leftFill.sizeDelta.x;

        // Detect influence shift (change in left fill width)
        if (Mathf.Abs(pivotX - lastPivotX) > 0.1f)
        {
            AnimateShift(pivotX);
            lastPivotX = pivotX;
        }
    }

    /// <summary>
    /// Animates the pivot line and triangles to the new influence position.
    /// </summary>
    /// <param name="pivotX">Target X in the local space of the bar (anchoredPosition.x).</param>
        private void AnimateShift(float pivotX)
        {
            // Move the pivot line directly to the new X position
            if (pivotLine != null)
            {
                Vector2 pivotPos = pivotLine.anchoredPosition;
                pivotLine.anchoredPosition = new Vector2(pivotX, pivotPos.y);
            }

            // Triangles follow immediately, matching the new pivot X
            if (triangleTop != null)
            {
                var topPos = triangleTop.anchoredPosition;
                triangleTop.anchoredPosition = new Vector2(
                    pivotX,
                    Mathf.Abs(topPos.y) > 0.01f ? topPos.y : verticalOffset);
            }

            if (triangleBottom != null)
            {
                var bottomPos = triangleBottom.anchoredPosition;
                float defaultY = -Mathf.Abs(verticalOffset);
                triangleBottom.anchoredPosition = new Vector2(
                    pivotX,
                    Mathf.Abs(bottomPos.y) > 0.01f ? bottomPos.y : defaultY);
            }
        }
}


