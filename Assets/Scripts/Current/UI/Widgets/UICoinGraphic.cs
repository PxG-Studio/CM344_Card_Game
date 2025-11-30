using UnityEngine;
using UnityEngine.UI;

namespace CardGame.UI.Widgets
{
    /// <summary>
    /// Simple procedural coin graphic rendered on the UI canvas to give the coin toss visual more depth.
    /// Draws a circular mesh with a metallic rim and inner gradient.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasRenderer))]
    public class UICoinGraphic : MaskableGraphic
    {
        [Range(12, 128)]
        [SerializeField] private int segments = 48;
        
        [Header("Coin Colors")]
        [SerializeField] private Color rimColor = new Color(1f, 0.8f, 0.2f, 1f);
        [SerializeField] private Color innerColor = new Color(1f, 0.95f, 0.7f, 1f);
        [SerializeField] private Color shadowColor = new Color(0.8f, 0.5f, 0.1f, 1f);
        [SerializeField, Range(0f, 0.5f)] private float rimThickness = 0.12f;
        [SerializeField, Range(0f, 0.3f)] private float bevelDepth = 0.08f;
        
        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
        }
        
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rect = GetPixelAdjustedRect();
            float radius = Mathf.Min(rect.width, rect.height) * 0.5f;
            Vector2 center = rect.center;
            
            int rimSegments = Mathf.Max(segments, 12);
            float innerRadius = radius * (1f - rimThickness);
            float bevelRadius = innerRadius * (1f - bevelDepth);
            
            // Draw rim ring
            AddRing(vh, center, radius, innerRadius, rimSegments, rimColor, shadowColor);
            // Draw bevel ring
            AddRing(vh, center, innerRadius, bevelRadius, rimSegments, Color.Lerp(rimColor, innerColor, 0.5f), innerColor);
            // Draw inner disc
            AddDisc(vh, center, bevelRadius, rimSegments, innerColor);
        }
        
        private void AddRing(VertexHelper vh, Vector2 center, float outerR, float innerR, int segments, Color outerColor, Color innerColor)
        {
            int startIndex = vh.currentVertCount;
            float angleStep = 360f / segments;
            
            for (int i = 0; i <= segments; i++)
            {
                float angle = Mathf.Deg2Rad * (i * angleStep);
                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                
                UIVertex outerVert = UIVertex.simpleVert;
                outerVert.color = outerColor;
                outerVert.position = center + dir * outerR;
                vh.AddVert(outerVert);
                
                UIVertex innerVert = UIVertex.simpleVert;
                innerVert.color = innerColor;
                innerVert.position = center + dir * innerR;
                vh.AddVert(innerVert);
            }
            
            for (int i = 0; i < segments; i++)
            {
                vh.AddTriangle(startIndex + i * 2, startIndex + i * 2 + 1, startIndex + i * 2 + 3);
                vh.AddTriangle(startIndex + i * 2, startIndex + i * 2 + 3, startIndex + i * 2 + 2);
            }
        }
        
        private void AddDisc(VertexHelper vh, Vector2 center, float radius, int segments, Color color)
        {
            int startIndex = vh.currentVertCount;
            UIVertex centerVert = UIVertex.simpleVert;
            centerVert.color = color;
            centerVert.position = center;
            vh.AddVert(centerVert);
            
            float angleStep = 360f / segments;
            for (int i = 0; i <= segments; i++)
            {
                float angle = Mathf.Deg2Rad * (i * angleStep);
                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                UIVertex vert = UIVertex.simpleVert;
                vert.color = color;
                vert.position = center + dir * radius;
                vh.AddVert(vert);
            }
            
            for (int i = 1; i <= segments; i++)
            {
                vh.AddTriangle(startIndex, startIndex + i, startIndex + i + 1);
            }
        }
        
#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            SetVerticesDirty();
        }
#endif
    }
}

