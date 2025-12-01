using UnityEngine;

/// <summary>
/// Simple driver to oscillate the leftFill width so you can visually
/// test the InfluenceLaggingMarkers behaviour in isolation.
/// Attach this to any GameObject in a test scene and assign the
/// same RectTransform used as leftFill on InfluenceLaggingMarkers.
/// </summary>
public class InfluenceTestDriver : MonoBehaviour
{
    public RectTransform leftFill;
    public float maxWidth = 640f;
    public float testSpeed = 2f;

    private void Update()
    {
        if (leftFill == null) return;

        float t = (Mathf.Sin(Time.time * testSpeed) + 1f) / 2f;
        float newLeftWidth = Mathf.Lerp(50f, maxWidth - 50f, t);

        leftFill.sizeDelta = new Vector2(newLeftWidth, leftFill.sizeDelta.y);
    }
}


