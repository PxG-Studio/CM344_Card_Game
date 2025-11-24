using UnityEngine;

namespace CardGame.UI
{
    /// <summary>
    /// Simple utility that arranges card UI children in a fan layout.
    /// Attach this to the FanLayout GameObject underneath P1HandContainer.
    /// </summary>
    [ExecuteAlways]
    public class FanHandUI : MonoBehaviour
    {
        [Header("Fan Settings")]
        [SerializeField] private float angleSpread = 18f;
        [SerializeField] private float radius = 240f;
        [SerializeField] private float verticalOffset = 60f;
        [SerializeField] private float scaleCenterBoost = 1.12f;

        [Header("Debug")]
        [SerializeField] public bool autoUpdate = true;

        private void LateUpdate()
        {
            if (autoUpdate)
            {
                UpdateFan();
            }
        }

        public void UpdateFan()
        {
            int count = transform.childCount;
            if (count == 0)
            {
                return;
            }

            float middleIndex = (count - 1) / 2f;

            for (int i = 0; i < count; i++)
            {
                RectTransform child = transform.GetChild(i) as RectTransform;
                if (child == null)
                {
                    continue;
                }

                float offset = i - middleIndex;
                float angle = offset * angleSpread;
                float rad = angle * Mathf.Deg2Rad;

                Vector2 anchoredPosition = new Vector2(
                    Mathf.Sin(rad) * radius,
                    Mathf.Cos(rad) * (radius * 0.2f) - radius + verticalOffset
                );

                child.anchoredPosition = anchoredPosition;
                child.localRotation = Quaternion.Euler(0f, 0f, -angle);

                float t = middleIndex <= 0f ? 0f : Mathf.Abs(offset) / middleIndex;
                float scale = Mathf.Lerp(scaleCenterBoost, 0.92f, t);
                child.localScale = Vector3.one * scale;

                child.SetSiblingIndex(i);
            }
        }
    }
}

