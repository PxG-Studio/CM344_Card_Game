using UnityEngine;

namespace CardGame.UI
{
    /// <summary>
    /// Provides subtle animated effects for tiles based on ownership.
    /// Orange tiles get fire/flame effects, green tiles get grass/earth effects.
    /// </summary>
    public class TileAnimationEffect : MonoBehaviour
    {
        [Header("Effect Settings")]
        [SerializeField] private SpriteRenderer effectRenderer;
        [SerializeField] private SpriteRenderer secondaryEffectRenderer; // For layered effects
        [SerializeField] private float animationSpeed = 1.5f;
        [SerializeField] private float pulseIntensity = 0.15f;
        [SerializeField] private float baseAlpha = 0.25f; // More subtle overlay
        [SerializeField] private float secondaryAlpha = 0.15f; // More subtle secondary layer
        
        private Color baseColor;
        private float timeOffset;
        private float secondaryTimeOffset;
        private bool isActive = false;
        private bool isFireEffect = false;
        private bool isGrassEffect = false;
        
        private void Awake()
        {
            // DON'T auto-create child objects - this can break board layout
            // Effect renderers must be manually assigned in Inspector if animation is desired
            // If not assigned, the effect will simply not animate (tiles will still change color)
            
            // Only initialize if renderers are already assigned
            if (effectRenderer == null && secondaryEffectRenderer == null)
            {
                // No renderers assigned - disable this component to avoid Update() overhead
                enabled = false;
                return;
            }
            
            // Initialize time offsets for varied animation
            timeOffset = Random.Range(0f, Mathf.PI * 2f);
            secondaryTimeOffset = Random.Range(0f, Mathf.PI * 2f);
            
            // Ensure renderers start disabled
            if (effectRenderer != null)
            {
                effectRenderer.enabled = false;
            }
            if (secondaryEffectRenderer != null)
            {
                secondaryEffectRenderer.enabled = false;
            }
        }
        
        private void Update()
        {
            if (!isActive)
            {
                return;
            }
            
            float time = Time.time * animationSpeed;
            
            // Primary effect animation
            if (effectRenderer != null && effectRenderer.enabled)
            {
                float pulse = Mathf.Sin(time + timeOffset) * pulseIntensity;
                float alpha = baseAlpha + pulse;
                alpha = Mathf.Clamp01(alpha);
                
                Color animatedColor = baseColor;
                animatedColor.a = alpha;
                effectRenderer.color = animatedColor;
                
                // Fire effect: pulsing scale and color variation
                if (isFireEffect)
                {
                    float scalePulse = 1f + (Mathf.Sin(time * 1.5f + timeOffset) * 0.08f);
                    effectRenderer.transform.localScale = Vector3.one * scalePulse;
                    
                    // Add slight color variation (more red/yellow flicker)
                    float colorVariation = Mathf.Sin(time * 2f + timeOffset) * 0.1f;
                    animatedColor.r = Mathf.Clamp01(baseColor.r + colorVariation);
                    animatedColor.g = Mathf.Clamp01(baseColor.g + colorVariation * 0.5f);
                    animatedColor.a = alpha;
                    effectRenderer.color = animatedColor;
                }
                // Grass effect: gentle swaying motion
                else if (isGrassEffect)
                {
                    float swayX = Mathf.Sin(time * 0.8f + timeOffset) * 0.03f;
                    float swayY = Mathf.Sin(time * 1.2f + timeOffset) * 0.02f;
                    effectRenderer.transform.localPosition = new Vector3(swayX, swayY, 0f);
                    
                    // Subtle color variation (more green/earth tones)
                    float colorVariation = Mathf.Sin(time * 1.5f + timeOffset) * 0.08f;
                    animatedColor.g = Mathf.Clamp01(baseColor.g + colorVariation);
                    animatedColor.b = Mathf.Clamp01(baseColor.b + colorVariation * 0.3f);
                    animatedColor.a = alpha;
                    effectRenderer.color = animatedColor;
                }
            }
            
            // Secondary effect animation (for layered depth)
            if (secondaryEffectRenderer != null && secondaryEffectRenderer.enabled)
            {
                float secondaryPulse = Mathf.Sin(time * 0.7f + secondaryTimeOffset) * (pulseIntensity * 0.6f);
                float secondaryAlphaValue = secondaryAlpha + secondaryPulse;
                secondaryAlphaValue = Mathf.Clamp01(secondaryAlphaValue);
                
                Color secondaryColor = baseColor;
                secondaryColor.a = secondaryAlphaValue;
                secondaryEffectRenderer.color = secondaryColor;
                
                // Fire: secondary layer with different timing
                if (isFireEffect)
                {
                    float secondaryScale = 1f + (Mathf.Sin(time * 1.2f + secondaryTimeOffset) * 0.05f);
                    secondaryEffectRenderer.transform.localScale = Vector3.one * secondaryScale;
                }
                // Grass: secondary layer with offset position
                else if (isGrassEffect)
                {
                    float secondarySwayX = Mathf.Sin(time * 0.6f + secondaryTimeOffset) * 0.02f;
                    float secondarySwayY = Mathf.Sin(time * 1.0f + secondaryTimeOffset) * 0.015f;
                    secondaryEffectRenderer.transform.localPosition = new Vector3(secondarySwayX, secondarySwayY, 0f);
                }
            }
        }
        
        /// <summary>
        /// Activates the effect with the specified color (orange for P1, green for P2)
        /// </summary>
        public void ActivateEffect(Color color)
        {
            baseColor = color;
            isActive = true;
            
            // Determine effect type based on color
            isFireEffect = (color.r > 0.8f && color.g < 0.6f); // Orange/fire
            isGrassEffect = (color.g > 0.7f && color.r < 0.3f); // Green/grass
            
            if (effectRenderer != null)
            {
                effectRenderer.enabled = true;
                effectRenderer.color = new Color(color.r, color.g, color.b, baseAlpha);
                effectRenderer.transform.localScale = Vector3.one;
                effectRenderer.transform.localPosition = Vector3.zero;
            }
            
            if (secondaryEffectRenderer != null)
            {
                secondaryEffectRenderer.enabled = true;
                secondaryEffectRenderer.color = new Color(color.r, color.g, color.b, secondaryAlpha);
                secondaryEffectRenderer.transform.localScale = Vector3.one;
                secondaryEffectRenderer.transform.localPosition = Vector3.zero;
            }
        }
        
        /// <summary>
        /// Deactivates the effect (returns tile to neutral state)
        /// </summary>
        public void DeactivateEffect()
        {
            isActive = false;
            isFireEffect = false;
            isGrassEffect = false;
            
            if (effectRenderer != null)
            {
                effectRenderer.enabled = false;
            }
            if (secondaryEffectRenderer != null)
            {
                secondaryEffectRenderer.enabled = false;
            }
        }
    }
}

