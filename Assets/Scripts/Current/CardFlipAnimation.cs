using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CardGame.Managers;

namespace CardGame.UI
{
    /// <summary>
    /// Direction of flip animation
    /// </summary>
    public enum FlipDirection
    {
        Left,   // Flip from left (rotate around Y-axis, pivot on right)
        Right,  // Flip from right (rotate around Y-axis, pivot on left)
        Top,    // Flip from top (rotate around X-axis, pivot on bottom)
        Down    // Flip from bottom (rotate around X-axis, pivot on top)
    }

    /// <summary>
    /// Handles card flip animation using rotation-based flip:
    /// - Rotates card around Y-axis (left/right) or X-axis (top/down) to reveal back/front
    /// - Changes color when captured (flipped to back)
    /// </summary>
    public class CardFlipAnimation : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float flipDuration = 0.5f;
        [SerializeField] private AnimationCurve flipEasing = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private GameObject frontContainer;
        [SerializeField] private GameObject backContainer;
        
        private CanvasGroup frontCanvasGroup;
        private CanvasGroup backCanvasGroup;
        private SpriteRenderer[] frontSprites;
        private SpriteRenderer[] backSprites;
        private UnityEngine.UI.Image[] frontImages;
        private UnityEngine.UI.Image[] backImages;
        private Coroutine currentFlipCoroutine;
        
        public bool isFlipped { get; private set; }
        public bool isAnimating => currentFlipCoroutine != null;
        
        // Reference to NewCardUI for captured color
        private NewCardUI cardUI;
        
        /// <summary>
        /// Set container references (called from NewCardUI if not set in Inspector)
        /// </summary>
        public void SetContainers(GameObject front, GameObject back)
        {
            frontContainer = front;
            backContainer = back;
            // Re-validate setup after containers are assigned
            ValidateSetup();
        }
        
        private void Awake()
        {
            cardUI = GetComponent<NewCardUI>();
            if (cardUI == null)
            {
                cardUI = GetComponentInParent<NewCardUI>();
            }
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
                // Only log error if this is a required setup (not just optional feature)
                // Don't spam errors if flip animation isn't set up yet
                isValid = false;
            }
            else
            {
                frontCanvasGroup = frontContainer.GetComponent<CanvasGroup>();
                if (frontCanvasGroup == null)
                {
                    frontCanvasGroup = frontContainer.AddComponent<CanvasGroup>();
                }
                // Cache SpriteRenderer and Image arrays (performance optimization)
                frontSprites = frontContainer.GetComponentsInChildren<SpriteRenderer>();
                frontImages = frontContainer.GetComponentsInChildren<UnityEngine.UI.Image>();
            }
            
            // Back container setup
            if (backContainer == null)
            {
                // Only log error if this is a required setup (not just optional feature)
                isValid = false;
            }
            else
            {
                backCanvasGroup = backContainer.GetComponent<CanvasGroup>();
                if (backCanvasGroup == null)
                {
                    backCanvasGroup = backContainer.AddComponent<CanvasGroup>();
                }
                // Cache SpriteRenderer and Image arrays (performance optimization)
                backSprites = backContainer.GetComponentsInChildren<SpriteRenderer>();
                backImages = backContainer.GetComponentsInChildren<UnityEngine.UI.Image>();
            }
            
            return isValid;
        }
        
        public bool IsSetupValid()
        {
            return frontContainer != null && backContainer != null;
        }
        
        /// <summary>
        /// Restores original card colors when flipping back to front
        /// </summary>
        private void RestoreOriginalColors(Color originalColor)
        {
            // Restore front container colors
            if (frontSprites != null)
            {
                foreach (SpriteRenderer sr in frontSprites)
                {
                    if (sr != null)
                    {
                        Color color = originalColor;
                        color.a = sr.color.a; // Preserve alpha
                        sr.color = color;
                    }
                }
            }
            if (frontImages != null)
            {
                foreach (UnityEngine.UI.Image img in frontImages)
                {
                    if (img != null)
                    {
                        Color color = originalColor;
                        color.a = img.color.a; // Preserve alpha
                        img.color = color;
                    }
                }
            }
            
            // Restore cardBackground
            if (cardUI != null)
            {
                var cardBackgroundField = typeof(NewCardUI).GetField("cardBackground", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (cardBackgroundField != null)
                {
                    var cardBackground = cardBackgroundField.GetValue(cardUI);
                    if (cardBackground != null)
                    {
                        SpriteRenderer bgSR = cardBackground as SpriteRenderer;
                        if (bgSR != null)
                        {
                            Color color = originalColor;
                            color.a = bgSR.color.a;
                            bgSR.color = color;
                        }
                        UnityEngine.UI.Image bgImg = cardBackground as UnityEngine.UI.Image;
                        if (bgImg != null)
                        {
                            Color color = originalColor;
                            color.a = bgImg.color.a;
                            bgImg.color = color;
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Applies captured color to card elements when flipped to back
        /// This makes both front and back gray (removes orange/green colors)
        /// </summary>
        private void ApplyCapturedColor(Color capturedColor)
        {
            // Apply to front container sprites and images (they'll be hidden but color is set for when flipped back)
            if (frontSprites != null)
            {
                foreach (SpriteRenderer sr in frontSprites)
                {
                    if (sr != null)
                    {
                        // Preserve alpha but set RGB to gray
                        Color grayColor = capturedColor;
                        grayColor.a = sr.color.a; // Preserve current alpha
                        sr.color = grayColor;
                    }
                }
            }
            if (frontImages != null)
            {
                foreach (UnityEngine.UI.Image img in frontImages)
                {
                    if (img != null)
                    {
                        // Preserve alpha but set RGB to gray
                        Color grayColor = capturedColor;
                        grayColor.a = img.color.a; // Preserve current alpha
                        img.color = grayColor;
                    }
                }
            }
            
            // Apply to back container sprites and images (visible when flipped)
            if (backSprites != null)
            {
                foreach (SpriteRenderer sr in backSprites)
                {
                    if (sr != null)
                    {
                        // Preserve alpha but set RGB to gray
                        Color grayColor = capturedColor;
                        grayColor.a = sr.color.a; // Preserve current alpha
                        sr.color = grayColor;
                    }
                }
            }
            if (backImages != null)
            {
                foreach (UnityEngine.UI.Image img in backImages)
                {
                    if (img != null)
                    {
                        // Preserve alpha but set RGB to gray
                        Color grayColor = capturedColor;
                        grayColor.a = img.color.a; // Preserve current alpha
                        img.color = grayColor;
                    }
                }
            }
            
            // Also apply to cardBackground if it exists (on root or in containers)
            if (cardUI != null)
            {
                // Get cardBackground from NewCardUI using reflection
                var cardBackgroundField = typeof(NewCardUI).GetField("cardBackground", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (cardBackgroundField != null)
                {
                    var cardBackground = cardBackgroundField.GetValue(cardUI);
                    if (cardBackground != null)
                    {
                        // Handle SpriteRenderer
                        SpriteRenderer bgSR = cardBackground as SpriteRenderer;
                        if (bgSR != null)
                        {
                            Color grayColor = capturedColor;
                            grayColor.a = bgSR.color.a;
                            bgSR.color = grayColor;
                        }
                        // Handle Image
                        UnityEngine.UI.Image bgImg = cardBackground as UnityEngine.UI.Image;
                        if (bgImg != null)
                        {
                            Color grayColor = capturedColor;
                            grayColor.a = bgImg.color.a;
                            bgImg.color = grayColor;
                        }
                    }
                }
            }
        }
        
        public void FlipToFront()
        {
            if (!IsSetupValid()) return; // Don't flip if not set up
            if (isAnimating) return; // Prevent overlapping animations
            if (currentFlipCoroutine != null)
            {
                StopCoroutine(currentFlipCoroutine);
            }
            currentFlipCoroutine = StartCoroutine(FlipToFrontCoroutine());
        }
        
        private IEnumerator FlipToFrontCoroutine()
        {
            float elapsed = 0f;
            
            // Get original card color to restore when flipping back to front
            Color originalColor = Color.white;
            if (cardUI != null && cardUI.Card != null && cardUI.Card.Data != null)
            {
                originalColor = cardUI.Card.Data.cardColor;
            }
            
            // Ensure containers are in correct initial state
            if (frontContainer != null) frontContainer.SetActive(false);
            if (backContainer != null) backContainer.SetActive(true);
            
            while (elapsed < flipDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / flipDuration;
                float easedT = flipEasing.Evaluate(t);
                
                // Rotate from 180 (back) to 0 (front)
                float currentRotationY = Mathf.Lerp(180f, 0f, easedT);
                transform.localRotation = Quaternion.Euler(0, currentRotationY, 0);
                
                // At midpoint (90 degrees), swap containers and restore original colors
                if (t >= 0.5f && frontContainer != null && !frontContainer.activeSelf)
                {
                    frontContainer.SetActive(true);
                    backContainer.SetActive(false);
                    // Restore original colors when flipping back to front
                    RestoreOriginalColors(originalColor);
                }
                
                yield return null;
            }
            
            // Ensure final state
            transform.localRotation = Quaternion.Euler(0, 0, 0);
            if (frontContainer != null) frontContainer.SetActive(true);
            if (backContainer != null) backContainer.SetActive(false);
            RestoreOriginalColors(originalColor); // Ensure colors are restored
            
            isFlipped = true;
            currentFlipCoroutine = null;
        }
        
        public void FlipToBack()
        {
            FlipToBack(null); // Use default (determine from card ownership)
        }
        
        public void FlipToBack(Color? overrideCapturedColor)
        {
            if (!IsSetupValid()) return; // Don't flip if not set up
            if (isAnimating) return;
            if (currentFlipCoroutine != null)
            {
                StopCoroutine(currentFlipCoroutine);
            }
            currentFlipCoroutine = StartCoroutine(FlipToBackCoroutine(overrideCapturedColor));
        }
        
        private IEnumerator FlipToBackCoroutine(Color? overrideCapturedColor)
        {
            float elapsed = 0f;
            
            // Determine captured color
            Color capturedColor = Color.gray; // Default captured color
            if (overrideCapturedColor.HasValue)
            {
                capturedColor = overrideCapturedColor.Value;
            }
            else if (cardUI != null)
            {
                // Determine which player captured this card
                // If card belongs to player, use opponent's capture color (green)
                // If card belongs to opponent, use player's capture color (orange)
                // We'll check by looking at which deck manager owns the card
                bool isPlayerCard = IsPlayerCard();
                if (isPlayerCard)
                {
                    capturedColor = cardUI.OpponentCapturedColor; // Player's card captured = green
                }
                else
                {
                    capturedColor = cardUI.PlayerCapturedColor; // Opponent's card captured = orange
                }
            }
            
            // Ensure containers are in correct initial state
            if (frontContainer != null) frontContainer.SetActive(true);
            if (backContainer != null) backContainer.SetActive(false);
            
            while (elapsed < flipDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / flipDuration;
                float easedT = flipEasing.Evaluate(t);
                
                // Rotate from 0 (front) to 180 (back)
                float currentRotationY = Mathf.Lerp(0f, 180f, easedT);
                transform.localRotation = Quaternion.Euler(0, currentRotationY, 0);
                
                // At midpoint (90 degrees), swap containers and apply captured color
                if (t >= 0.5f && backContainer != null && !backContainer.activeSelf)
                {
                    frontContainer.SetActive(false);
                    backContainer.SetActive(true);
                    ApplyCapturedColor(capturedColor);
                }
                
                yield return null;
            }
            
            // Ensure final state
            transform.localRotation = Quaternion.Euler(0, 180, 0);
            if (frontContainer != null) frontContainer.SetActive(false);
            if (backContainer != null) backContainer.SetActive(true);
            ApplyCapturedColor(capturedColor); // Ensure color is applied
            
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

        /// <summary>
        /// Captures a card: flips it (animation) and changes only the border/background color to capture color
        /// Card ends up showing the front with all visuals visible, but with captured border color
        /// </summary>
        public void CaptureCard(Color captureColor)
        {
            CaptureCard(captureColor, FlipDirection.Right); // Default to right flip
        }

        /// <summary>
        /// Captures a card with directional flip animation
        /// </summary>
        public void CaptureCard(Color captureColor, FlipDirection direction)
        {
            if (!IsSetupValid()) return;
            if (isAnimating) return;
            if (currentFlipCoroutine != null)
            {
                StopCoroutine(currentFlipCoroutine);
            }
            currentFlipCoroutine = StartCoroutine(CaptureCardCoroutine(captureColor, direction));
        }

        private IEnumerator CaptureCardCoroutine(Color captureColor, FlipDirection direction)
        {
            // First, flip to back (half the animation)
            float elapsed = 0f;
            float halfDuration = flipDuration * 0.5f;

            // Ensure containers are in correct initial state
            if (frontContainer != null) frontContainer.SetActive(true);
            if (backContainer != null) backContainer.SetActive(false);

            // Determine rotation axis and angles based on direction
            Vector3 rotationAxis = Vector3.up; // Default to Y-axis (horizontal flip)
            float startAngle = 0f;
            float endAngle = 180f;
            bool isVerticalFlip = false;

            switch (direction)
            {
                case FlipDirection.Left:
                    // Flip from left: rotate around Y-axis, start from 0, end at 180
                    rotationAxis = Vector3.up;
                    startAngle = 0f;
                    endAngle = 180f;
                    break;
                case FlipDirection.Right:
                    // Flip from right: rotate around Y-axis, start from 0, end at -180 (or 180, same effect)
                    rotationAxis = Vector3.up;
                    startAngle = 0f;
                    endAngle = 180f;
                    break;
                case FlipDirection.Top:
                    // Flip from top: rotate around X-axis, start from 0, end at 180
                    rotationAxis = Vector3.right;
                    startAngle = 0f;
                    endAngle = 180f;
                    isVerticalFlip = true;
                    break;
                case FlipDirection.Down:
                    // Flip from bottom: rotate around X-axis, start from 0, end at -180 (or 180)
                    rotationAxis = Vector3.right;
                    startAngle = 0f;
                    endAngle = 180f;
                    isVerticalFlip = true;
                    break;
            }

            // Flip to back (first half)
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / halfDuration;
                float easedT = flipEasing.Evaluate(t); // Use curve from 0 to 1 for first half

                // Rotate based on direction
                float currentAngle = Mathf.Lerp(startAngle, endAngle, easedT);
                if (isVerticalFlip)
                {
                    transform.localRotation = Quaternion.Euler(currentAngle, 0, 0);
                }
                else
                {
                    transform.localRotation = Quaternion.Euler(0, currentAngle, 0);
                }

                // At midpoint (90 degrees), swap containers
                if (t >= 0.5f && backContainer != null && !backContainer.activeSelf)
                {
                    frontContainer.SetActive(false);
                    backContainer.SetActive(true);
                }

                yield return null;
            }

            // Now flip back to front (second half) and apply capture color to border only
            elapsed = 0f;

            // Apply capture color to border/background only (not to artwork or other visuals)
            ApplyCaptureColorToBorder(captureColor);

            // Flip back to front
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / halfDuration;
                // Reverse the easing curve for smooth return (evaluate from 1 to 0)
                float easedT = 1f - flipEasing.Evaluate(1f - t);

                // Rotate back based on direction
                float currentAngle = Mathf.Lerp(endAngle, startAngle, easedT);
                if (isVerticalFlip)
                {
                    transform.localRotation = Quaternion.Euler(currentAngle, 0, 0);
                }
                else
                {
                    transform.localRotation = Quaternion.Euler(0, currentAngle, 0);
                }

                // At midpoint (90 degrees), swap containers back
                if (t >= 0.5f && frontContainer != null && !frontContainer.activeSelf)
                {
                    backContainer.SetActive(false);
                    frontContainer.SetActive(true);
                }

                yield return null;
            }

            // Ensure final state
            transform.localRotation = Quaternion.Euler(0, 0, 0);
            if (frontContainer != null) frontContainer.SetActive(true);
            if (backContainer != null) backContainer.SetActive(false);

            isFlipped = true; // Card is face up (front showing)
            currentFlipCoroutine = null;
        }

        /// <summary>
        /// Applies capture color ONLY to the border/background, keeping all other visuals unchanged
        /// </summary>
        private void ApplyCaptureColorToBorder(Color captureColor)
        {
            if (cardUI == null) return;

            // Get cardBackground using reflection
            var cardBackgroundField = typeof(NewCardUI).GetField("cardBackground",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (cardBackgroundField != null)
            {
                var cardBackground = cardBackgroundField.GetValue(cardUI);
                if (cardBackground != null)
                {
                    // Handle SpriteRenderer (2D card)
                    SpriteRenderer bgSR = cardBackground as SpriteRenderer;
                    if (bgSR != null)
                    {
                        // Apply capture color to border/background
                        Color borderColor = captureColor;
                        borderColor.a = bgSR.color.a; // Preserve alpha
                        bgSR.color = borderColor;
                    }

                    // Handle Image (UI card)
                    UnityEngine.UI.Image bgImg = cardBackground as UnityEngine.UI.Image;
                    if (bgImg != null)
                    {
                        // Apply capture color to border/background
                        Color borderColor = captureColor;
                        borderColor.a = bgImg.color.a; // Preserve alpha
                        bgImg.color = borderColor;
                    }
                }
            }
        }
        
        /// <summary>
        /// Determines if this card belongs to the player (vs opponent)
        /// </summary>
        private bool IsPlayerCard()
        {
            // Try to find which deck manager owns this card
            // Check if card is in player's hand or was played by player
            if (cardUI != null && cardUI.Card != null)
            {
                NewDeckManager playerDeckManager = FindObjectOfType<NewDeckManager>();
                if (playerDeckManager != null && playerDeckManager.Hand.Contains(cardUI.Card))
                {
                    return true; // Card is in player's hand
                }
                
                // Check if it's a CardMover (player card) vs CardMoverOpp (opponent card)
                //CardMover cardMover = GetComponent<CardMover>();
               
                //if (cardMover != null)
                if (frontContainer.CompareTag("p1"))
                {
                    return true; // Player card
                }
                
                //CardMoverOpp cardMoverOpp = GetComponent<CardMoverOpp>();
                //if (cardMoverOpp != null)
                if (frontContainer.CompareTag("p2"))
                {
                    return false; // Opponent card
                }
            }
            
            // Default: assume player card if we can't determine
            return true;
        }
        
        public void SetFlippedState(bool showFront, bool instant = false)
        {
            SetFlippedState(showFront, instant, null);
        }
        
        public void SetFlippedState(bool showFront, bool instant, Color? overrideCapturedColor)
        {
            if (!IsSetupValid()) return; // Don't set state if not set up

            // Stop any running animation
            if (currentFlipCoroutine != null)
            {
                StopCoroutine(currentFlipCoroutine);
                currentFlipCoroutine = null;
            }

            // Get captured color if flipping to back
            Color capturedColor = Color.gray;
            if (!showFront)
            {
                if (overrideCapturedColor.HasValue)
                {
                    capturedColor = overrideCapturedColor.Value;
                }
                else if (cardUI != null)
                {
                    bool isPlayerCard = IsPlayerCard();
                    if (isPlayerCard)
                    {
                        capturedColor = cardUI.OpponentCapturedColor; // Player's card captured = green
                    }
                    else
                    {
                        capturedColor = cardUI.PlayerCapturedColor; // Opponent's card captured = orange
                    }
                }
            }
            
            // Set rotation and container visibility
            if (showFront)
            {
                transform.localRotation = Quaternion.Euler(0, 0, 0);
                if (frontContainer != null) frontContainer.SetActive(true);
                if (backContainer != null) backContainer.SetActive(false);
            }
            else
            {
                transform.localRotation = Quaternion.Euler(0, 180, 0);
                if (frontContainer != null) frontContainer.SetActive(false);
                if (backContainer != null) backContainer.SetActive(true);
                ApplyCapturedColor(capturedColor);
            }
            
            isFlipped = showFront;
        }
    }
}

