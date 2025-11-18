using System.Collections;
using UnityEngine;

namespace CardGame.UI
{
    /// <summary>
    /// Handles card flip animation using dual fade system:
    /// - CanvasGroup.alpha for UI elements (TextMeshProUGUI, Image)
    /// - color.a for SpriteRenderer elements (preserves RGB values)
    /// </summary>
    public class CardFlipAnimation : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float flipDuration = 0.5f;
        [SerializeField] private GameObject frontContainer;
        [SerializeField] private GameObject backContainer;
        
        private CanvasGroup frontCanvasGroup;
        private CanvasGroup backCanvasGroup;
        private SpriteRenderer[] frontSprites;
        private SpriteRenderer[] backSprites;
        private Coroutine currentFlipCoroutine;
        
        public bool isFlipped { get; private set; }
        public bool isAnimating => currentFlipCoroutine != null;
        
        /// <summary>
        /// Set container references (called from NewCardUI if not set in Inspector)
        /// </summary>
        public void SetContainers(GameObject front, GameObject back)
        {
            frontContainer = front;
            backContainer = back;
        }
        
        private void Awake()
        {
            ValidateSetup();
        }
        
        private void OnDestroy()
        {
            // Critical: Stop coroutines to prevent errors
            if (currentFlipCoroutine != null)
            {
                StopCoroutine(currentFlipCoroutine);
            }
            StopAllCoroutines();
        }
        
        public bool ValidateSetup()
        {
            bool isValid = true;
            
            // Front container setup
            if (frontContainer == null)
            {
                Debug.LogError("CardFlipAnimation: frontContainer is not assigned!", this);
                isValid = false;
            }
            else
            {
                frontCanvasGroup = frontContainer.GetComponent<CanvasGroup>();
                if (frontCanvasGroup == null)
                {
                    frontCanvasGroup = frontContainer.AddComponent<CanvasGroup>();
                    Debug.Log("CardFlipAnimation: Added CanvasGroup to frontContainer", this);
                }
                // Cache SpriteRenderer array (performance optimization)
                frontSprites = frontContainer.GetComponentsInChildren<SpriteRenderer>();
            }
            
            // Back container setup
            if (backContainer == null)
            {
                Debug.LogError("CardFlipAnimation: backContainer is not assigned!", this);
                isValid = false;
            }
            else
            {
                backCanvasGroup = backContainer.GetComponent<CanvasGroup>();
                if (backCanvasGroup == null)
                {
                    backCanvasGroup = backContainer.AddComponent<CanvasGroup>();
                    Debug.Log("CardFlipAnimation: Added CanvasGroup to backContainer", this);
                }
                // Cache SpriteRenderer array (performance optimization)
                backSprites = backContainer.GetComponentsInChildren<SpriteRenderer>();
            }
            
            return isValid;
        }
        
        private IEnumerator FadeContainerCoroutine(GameObject container, CanvasGroup canvasGroup, 
            SpriteRenderer[] sprites, float startAlpha, float targetAlpha, float duration)
        {
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                float t = elapsed / duration;
                float currentAlpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                
                // Fade UI elements via CanvasGroup
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = currentAlpha;
                }
                
                // Fade SpriteRenderer elements via color.a (preserves RGB)
                if (sprites != null)
                {
                    foreach (SpriteRenderer sr in sprites)
                    {
                        if (sr != null)
                        {
                            Color color = sr.color; // Get current color (preserves RGB)
                            color.a = currentAlpha; // Only modify alpha
                            sr.color = color;
                        }
                    }
                }
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // Ensure final state (no floating point errors)
            if (canvasGroup != null)
            {
                canvasGroup.alpha = targetAlpha;
            }
            if (sprites != null)
            {
                foreach (SpriteRenderer sr in sprites)
                {
                    if (sr != null)
                    {
                        Color color = sr.color;
                        color.a = targetAlpha;
                        sr.color = color;
                    }
                }
            }
        }
        
        public void FlipToFront()
        {
            if (isAnimating) return; // Prevent overlapping animations
            if (currentFlipCoroutine != null)
            {
                StopCoroutine(currentFlipCoroutine);
            }
            currentFlipCoroutine = StartCoroutine(FlipToFrontCoroutine());
        }
        
        private IEnumerator FlipToFrontCoroutine()
        {
            // Fade back out
            yield return FadeContainerCoroutine(backContainer, backCanvasGroup, backSprites, 1f, 0f, flipDuration);
            // Fade front in
            yield return FadeContainerCoroutine(frontContainer, frontCanvasGroup, frontSprites, 0f, 1f, flipDuration);
            
            isFlipped = true;
            currentFlipCoroutine = null;
        }
        
        public void FlipToBack()
        {
            if (isAnimating) return;
            if (currentFlipCoroutine != null)
            {
                StopCoroutine(currentFlipCoroutine);
            }
            currentFlipCoroutine = StartCoroutine(FlipToBackCoroutine());
        }
        
        private IEnumerator FlipToBackCoroutine()
        {
            // Fade front out
            yield return FadeContainerCoroutine(frontContainer, frontCanvasGroup, frontSprites, 1f, 0f, flipDuration);
            // Fade back in
            yield return FadeContainerCoroutine(backContainer, backCanvasGroup, backSprites, 0f, 1f, flipDuration);
            
            isFlipped = false;
            currentFlipCoroutine = null;
        }
        
        public void FlipToggle()
        {
            if (isFlipped)
            {
                FlipToBack();
            }
            else
            {
                FlipToFront();
            }
        }
        
        public void SetFlippedState(bool showFront, bool instant = false)
        {
            // Stop any running animation
            if (currentFlipCoroutine != null)
            {
                StopCoroutine(currentFlipCoroutine);
                currentFlipCoroutine = null;
            }
            
            float targetAlpha = showFront ? 1f : 0f;
            
            // Set front container
            if (frontCanvasGroup != null) frontCanvasGroup.alpha = showFront ? 1f : 0f;
            if (frontSprites != null)
            {
                foreach (SpriteRenderer sr in frontSprites)
                {
                    if (sr != null)
                    {
                        Color color = sr.color;
                        color.a = showFront ? 1f : 0f;
                        sr.color = color;
                    }
                }
            }
            
            // Set back container
            if (backCanvasGroup != null) backCanvasGroup.alpha = showFront ? 0f : 1f;
            if (backSprites != null)
            {
                foreach (SpriteRenderer sr in backSprites)
                {
                    if (sr != null)
                    {
                        Color color = sr.color;
                        color.a = showFront ? 0f : 1f;
                        sr.color = color;
                    }
                }
            }
            
            isFlipped = showFront;
        }
    }
}

