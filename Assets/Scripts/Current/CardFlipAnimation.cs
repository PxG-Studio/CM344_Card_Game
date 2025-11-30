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
        
        // Track capture state and color
        private Color lastCaptureColor = Color.clear;
        public bool WasCaptured => lastCaptureColor != Color.clear;
        public Color LastCaptureColor => lastCaptureColor;
        
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
        /// Ensures the card back sprite renderer stays white so the CardBack_Default image shows properly
        /// </summary>
        private void EnsureBackSpriteIsWhite()
        {
            if (backContainer == null) return;
            
            // Find the card back sprite renderer
            SpriteRenderer backSpriteRenderer = backContainer.GetComponentInChildren<SpriteRenderer>();
            if (backSpriteRenderer != null && (backSpriteRenderer.gameObject.name == "CardBackSprite" || 
                                               backSpriteRenderer.gameObject.name.Contains("CardBack")))
            {
                // Keep the back sprite white so the CardBack_Default image shows properly
                backSpriteRenderer.color = new Color(1f, 1f, 1f, 1f);
            }
        }
        
        /// <summary>
        /// Hides card background, border, and all text elements when showing the back
        /// </summary>
        private void HideCardFrontElements()
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
                    SpriteRenderer bgSR = cardBackground as SpriteRenderer;
                    UnityEngine.UI.Image bgImg = cardBackground as UnityEngine.UI.Image;
                    
                    if (bgSR != null) bgSR.enabled = false;
                    if (bgImg != null) bgImg.enabled = false;
                }
            }
            
            // Get borderOverlayRenderer using reflection
            var borderOverlayField = typeof(NewCardUI).GetField("borderOverlayRenderer",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (borderOverlayField != null)
            {
                var borderOverlay = borderOverlayField.GetValue(cardUI);
                if (borderOverlay != null)
                {
                    SpriteRenderer borderSR = borderOverlay as SpriteRenderer;
                    if (borderSR != null) borderSR.enabled = false;
                }
            }
            
            // Hide all text elements
            HideTextElements(cardUI, true);
        }
        
        /// <summary>
        /// Hides or shows all text elements on the card
        /// </summary>
        private void HideTextElements(NewCardUI cardUI, bool hide)
        {
            if (cardUI == null) return;
            
            // Check if this is a board card or being dragged
            bool isOnBoard = cardUI.GetComponent<CardMoverP1>() != null || cardUI.GetComponent<CardMoverP2>() != null;
            bool isBeingDragged = false;
            
            // Check if card is being dragged using reflection (isDragging is private)
            var isDraggingField = typeof(NewCardUI).GetField("isDragging",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (isDraggingField != null)
            {
                isBeingDragged = (bool)isDraggingField.GetValue(cardUI);
            }
            
            // Get all text fields using reflection
            string[] textFieldNames = {
                "cardNameText",
                "descriptionText",
                "topStatText",
                "rightStatText",
                "downStatText",
                "leftStatText",
                "cardTypeText"
            };
            
            foreach (string fieldName in textFieldNames)
            {
                var textField = typeof(NewCardUI).GetField(fieldName,
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (textField != null)
                {
                    var textComponent = textField.GetValue(cardUI);
                    if (textComponent != null)
                    {
                        TMPro.TextMeshProUGUI tmpText = textComponent as TMPro.TextMeshProUGUI;
                        if (tmpText != null)
                        {
                            // Stat text should always be visible for board cards or cards being dragged
                            if (fieldName.Contains("Stat") && (isOnBoard || isBeingDragged || !hide))
                            {
                                tmpText.enabled = true; // Force stat text to be visible
                                tmpText.alpha = 1f;
                                tmpText.gameObject.SetActive(true);
                            }
                            else
                            {
                                tmpText.enabled = !hide;
                            }
                        }
                    }
                }
            }
            
            // Also hide any other TextMeshProUGUI elements that might be children
            if (frontContainer != null)
            {
                TMPro.TextMeshProUGUI[] allTexts = frontContainer.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
                foreach (var text in allTexts)
                {
                    if (text != null)
                    {
                        bool isStatText = text.name.Contains("Stat") || text.name.Contains("Top") || text.name.Contains("Right") || text.name.Contains("Down") || text.name.Contains("Left");
                        
                        // Stat text should always be visible for board cards or cards being dragged
                        if (isStatText && (isOnBoard || isBeingDragged || !hide))
                        {
                            // Always keep stat text visible for board cards or during drag
                            text.enabled = true;
                            text.alpha = 1f;
                            text.gameObject.SetActive(true);
                        }
                        else
                        {
                            text.enabled = !hide;
                        }
                    }
                }
            }
            
            // Hide text elements at root level too (in case they're not in frontContainer)
            TMPro.TextMeshProUGUI[] rootTexts = cardUI.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
            foreach (var text in rootTexts)
            {
                if (text != null && !text.transform.IsChildOf(backContainer != null ? backContainer.transform : null))
                {
                    bool isStatText = text.name.Contains("Stat") || text.name.Contains("Top") || text.name.Contains("Right") || text.name.Contains("Down") || text.name.Contains("Left");
                    
                    // Stat text should always be visible for board cards or cards being dragged
                    if (isStatText && (isOnBoard || isBeingDragged || !hide))
                    {
                        text.enabled = true; // Force stat text to be visible
                        text.alpha = 1f;
                        text.gameObject.SetActive(true);
                    }
                    else
                    {
                        text.enabled = !hide;
                    }
                }
            }
        }
        
        /// <summary>
        /// Shows card background, border, and all text elements when showing the front
        /// </summary>
        private void ShowCardFrontElements()
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
                    SpriteRenderer bgSR = cardBackground as SpriteRenderer;
                    UnityEngine.UI.Image bgImg = cardBackground as UnityEngine.UI.Image;
                    
                    if (bgSR != null) bgSR.enabled = true;
                    if (bgImg != null) bgImg.enabled = true;
                }
            }
            
            // Get borderOverlayRenderer using reflection
            var borderOverlayField = typeof(NewCardUI).GetField("borderOverlayRenderer",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (borderOverlayField != null)
            {
                var borderOverlay = borderOverlayField.GetValue(cardUI);
                if (borderOverlay != null)
                {
                    SpriteRenderer borderSR = borderOverlay as SpriteRenderer;
                    if (borderSR != null) borderSR.enabled = true;
                }
            }
            
            // Show all text elements
            HideTextElements(cardUI, false);
        }
        
        /// <summary>
        /// Scales the back sprite to match the card background size
        /// This ensures the back sprite is properly sized during the flip animation
        /// </summary>
        private void ScaleBackSprite()
        {
            if (backContainer == null || cardUI == null) return;
            
            // Find the back sprite renderer
            SpriteRenderer backSpriteRenderer = backContainer.GetComponentInChildren<SpriteRenderer>();
            if (backSpriteRenderer == null || backSpriteRenderer.sprite == null) return;
            
            // Get cardBackground from NewCardUI using reflection
            var cardBackgroundField = typeof(NewCardUI).GetField("cardBackground",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (cardBackgroundField == null) return;
            
            var cardBackground = cardBackgroundField.GetValue(cardUI);
            if (cardBackground == null) return;
            
            SpriteRenderer bgSR = cardBackground as SpriteRenderer;
            UnityEngine.UI.Image bgImg = cardBackground as UnityEngine.UI.Image;
            
            if (bgSR == null && bgImg == null) return;
            
            // Get sprite from either SpriteRenderer or Image
            Sprite bgSprite = null;
            if (bgSR != null && bgSR.sprite != null)
            {
                bgSprite = bgSR.sprite;
            }
            else if (bgImg != null && bgImg.sprite != null)
            {
                bgSprite = bgImg.sprite;
            }
            
            if (bgSprite == null) return;
            
            // Get the cardBackground's actual visual size
            // Need to account for all scales in the hierarchy (frontContainer + cardBackground)
            Transform bgTransform = bgSR != null ? bgSR.transform : bgImg.transform;
            Vector3 bgScale = bgTransform.localScale;
            Vector3 frontContainerScale = Vector3.one;
            
            // If cardBackground is inside frontContainer, account for frontContainer's scale too
            if (frontContainer != null && bgTransform.IsChildOf(frontContainer.transform))
            {
                frontContainerScale = frontContainer.transform.localScale;
            }
            
            Vector2 bgSpriteSize = bgSprite.bounds.size;
            // Multiply by all scales in hierarchy
            Vector2 bgActualSize = new Vector2(
                bgSpriteSize.x * bgScale.x * frontContainerScale.x,
                bgSpriteSize.y * bgScale.y * frontContainerScale.y
            );
            
            Vector2 backSize = backSpriteRenderer.sprite.bounds.size;
            if (backSize.x > 0.0001f && backSize.y > 0.0001f)
            {
                // Calculate scale to match the cardBackground's actual visual size
                float scaleX = bgActualSize.x / backSize.x;
                float scaleY = bgActualSize.y / backSize.y;
                float uniform = Mathf.Min(scaleX, scaleY);
                backSpriteRenderer.transform.localScale = new Vector3(uniform, uniform, 1f);
            }
            else
            {
                backSpriteRenderer.transform.localScale = Vector3.one;
            }
            
            // Ensure the sprite renderer is visible after scaling
            backSpriteRenderer.enabled = true;
            backSpriteRenderer.color = new Color(1f, 1f, 1f, 1f); // Ensure full opacity
            if (backSpriteRenderer.gameObject != null)
            {
                backSpriteRenderer.gameObject.SetActive(true);
            }
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
            // BUT exclude the card back sprite renderer - it should always stay white to show the CardBack_Default image
            if (backSprites != null)
            {
                foreach (SpriteRenderer sr in backSprites)
                {
                    if (sr != null)
                    {
                        // Skip the card back sprite - it should always remain white
                        // The card back sprite is named "CardBackSprite" and should show the CardBack_Default image
                        if (sr.gameObject.name == "CardBackSprite" || sr.gameObject.name.Contains("CardBack"))
                        {
                            // Keep the back sprite white so the CardBack_Default image shows properly
                            sr.color = new Color(1f, 1f, 1f, 1f);
                            continue;
                        }
                        
                        // Preserve alpha but set RGB to gray for other sprites
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
            // Start with back visible (card is flipped to back)
            if (frontContainer != null) frontContainer.SetActive(false);
            if (backContainer != null) 
            {
                backContainer.SetActive(true);
                ScaleBackSprite(); // Scale back sprite when it becomes visible
            }
            
            while (elapsed < flipDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / flipDuration;
                float easedT = flipEasing.Evaluate(t);
                
                // Rotate from 180 (back) to 0 (front)
                float currentRotationY = Mathf.Lerp(180f, 0f, easedT);
                transform.localRotation = Quaternion.Euler(0, currentRotationY, 0);
                
                // At midpoint (90 degrees), swap containers and restore original colors
                // This ensures only one container is visible at a time
                if (t >= 0.5f && frontContainer != null && !frontContainer.activeSelf)
                {
                    // Hide back before showing front to prevent overlap
                    if (backContainer != null) backContainer.SetActive(false);
                    frontContainer.SetActive(true);
                    // Restore original colors when flipping back to front
                    RestoreOriginalColors(originalColor);
                    // Show card background and border when showing front
                    ShowCardFrontElements();
                }
                
                yield return null;
            }
            
            // Ensure final state
            transform.localRotation = Quaternion.Euler(0, 0, 0);
            if (frontContainer != null) frontContainer.SetActive(true);
            if (backContainer != null) backContainer.SetActive(false);
            RestoreOriginalColors(originalColor); // Ensure colors are restored
            // Note: Back sprite scaling not needed here since back is hidden
            
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
            // Start with front visible (card is face up)
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
                // This ensures only one container is visible at a time
                if (t >= 0.5f && backContainer != null && !backContainer.activeSelf)
                {
                    // Hide front before showing back to prevent overlap
                    if (frontContainer != null) frontContainer.SetActive(false);
                    backContainer.SetActive(true);
                    ScaleBackSprite(); // Scale back sprite when it becomes visible
                    ApplyCapturedColor(capturedColor);
                    // Ensure back sprite stays white after applying captured color
                    EnsureBackSpriteIsWhite();
                    // Hide card background and border when showing back
                    HideCardFrontElements();
                }
                
                yield return null;
            }
            
            // Ensure final state
            transform.localRotation = Quaternion.Euler(0, 180, 0);
            if (frontContainer != null) frontContainer.SetActive(false);
            if (backContainer != null) 
            {
                backContainer.SetActive(true);
                ScaleBackSprite(); // Ensure back sprite is properly scaled
            }
            ApplyCapturedColor(capturedColor); // Ensure color is applied
            // Ensure back sprite stays white after applying captured color
            EnsureBackSpriteIsWhite();
            // Hide card background and border when showing back
            HideCardFrontElements();
            
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
            // Store capture color
            lastCaptureColor = captureColor;
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
                // This ensures only one container is visible at a time
                if (t >= 0.5f && backContainer != null && !backContainer.activeSelf)
                {
                    // Hide front before showing back to prevent overlap
                    if (frontContainer != null) frontContainer.SetActive(false);
                    backContainer.SetActive(true);
                    ScaleBackSprite(); // Scale back sprite when it becomes visible
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
                // This ensures only one container is visible at a time
                if (t >= 0.5f && frontContainer != null && !frontContainer.activeSelf)
                {
                    // Hide back before showing front to prevent overlap
                    if (backContainer != null) backContainer.SetActive(false);
                    frontContainer.SetActive(true);
                    // Show card background and border when showing front
                    ShowCardFrontElements();
                }

                yield return null;
            }

            // Ensure final state
            transform.localRotation = Quaternion.Euler(0, 0, 0);
            if (frontContainer != null) frontContainer.SetActive(true);
            if (backContainer != null) backContainer.SetActive(false);
            // Show card background and border when showing front
            ShowCardFrontElements();
            
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
                NewDeckManagerP1 playerDeckManager = FindObjectOfType<NewDeckManagerP1>();
                if (playerDeckManager != null && playerDeckManager.Hand.Contains(cardUI.Card))
                {
                    return true; // Card is in player's hand
                }
                
                // Check if it's a CardMover (P1) vs CardMoverP2 (P2)
                //CardMover cardMover = GetComponent<CardMover>();
               
                //if (cardMover != null)
                if (frontContainer.CompareTag("p1"))
                {
                    return true; // P1 card
                }
                
                //CardMoverP2 cardMoverP2 = GetComponent<CardMoverP2>();
                //if (cardMoverP2 != null)
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
                ShowCardFrontElements(); // Show card background and border
            }
            else
            {
                transform.localRotation = Quaternion.Euler(0, 180, 0);
                if (frontContainer != null) frontContainer.SetActive(false);
                if (backContainer != null) 
                {
                    backContainer.SetActive(true);
                    ScaleBackSprite(); // Scale back sprite when showing back
                }
                ApplyCapturedColor(capturedColor);
                EnsureBackSpriteIsWhite(); // Ensure back sprite is white
                HideCardFrontElements(); // Hide card background and border
            }
            
            isFlipped = showFront;
        }
    }
}

