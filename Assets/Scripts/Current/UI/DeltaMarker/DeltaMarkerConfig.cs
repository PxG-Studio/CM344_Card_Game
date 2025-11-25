using UnityEngine;
using TMPro;

namespace CardGame.UI
{
    /// <summary>
    /// ScriptableObject configuration for Delta Marker popups.
    /// Controls colors, animation parameters, and visual style for territory influence delta indicators.
    /// </summary>
    [CreateAssetMenu(fileName = "DeltaMarkerConfig", menuName = "Card Game/Delta Marker Config", order = 100)]
    public class DeltaMarkerConfig : ScriptableObject
    {
        [Header("Colors")]
        [Tooltip("Color for Conquer events (+1 territory influence). Default: Gold/Yellow")]
        [SerializeField] private Color conquerColor = new Color(1f, 0.843f, 0f, 1f); // Gold
        
        [Tooltip("Color for Raze events (-1 territory influence). Default: Red/Orange")]
        [SerializeField] private Color razeColor = new Color(1f, 0.4f, 0f, 1f); // Red-orange
        
        [Header("Animation Settings")]
        [Tooltip("Distance the popup floats upward (in world units or screen pixels)")]
        [SerializeField] private float floatDistance = 100f;
        
        [Tooltip("Total duration of the animation sequence (in seconds)")]
        [SerializeField] private float duration = 1.5f;
        
        [Tooltip("Amount of scale punch effect (1.0 = no punch, 1.3 = 30% larger)")]
        [SerializeField] private float scalePunchAmount = 1.3f;
        
        [Tooltip("Curve controlling the upward float motion over time")]
        [SerializeField] private AnimationCurve floatCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 1f));
        
        [Header("Visual Style")]
        [Tooltip("Font asset to use for the delta text (bold recommended)")]
        [SerializeField] private TMP_FontAsset deltaFont;
        
        [Tooltip("Font size for the delta text")]
        [SerializeField] private int fontSize = 60;
        
        // Public getters
        public Color ConquerColor => conquerColor;
        public Color RazeColor => razeColor;
        public float FloatDistance => floatDistance;
        public float Duration => duration;
        public float ScalePunchAmount => scalePunchAmount;
        public AnimationCurve FloatCurve => floatCurve;
        public TMP_FontAsset DeltaFont => deltaFont;
        public int FontSize => fontSize;
        
        private void OnValidate()
        {
            // Validation
            if (floatDistance < 0f)
            {
                Debug.LogWarning($"[DeltaMarkerConfig] floatDistance should be positive. Clamping to 0.");
                floatDistance = 0f;
            }
            
            if (duration <= 0f)
            {
                Debug.LogWarning($"[DeltaMarkerConfig] duration must be positive. Setting to 1.0.");
                duration = 1f;
            }
            
            if (scalePunchAmount < 1f)
            {
                Debug.LogWarning($"[DeltaMarkerConfig] scalePunchAmount should be >= 1.0. Setting to 1.0.");
                scalePunchAmount = 1f;
            }
            
            if (fontSize < 10)
            {
                Debug.LogWarning($"[DeltaMarkerConfig] fontSize should be at least 10. Setting to 60.");
                fontSize = 60;
            }
        }
    }
}

