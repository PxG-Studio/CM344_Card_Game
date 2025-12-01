using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if DOTWEEN_AVAILABLE
using DG.Tweening;
#endif

namespace CardGame.UI
{
    /// <summary>
    /// Lightweight presenter that handles Coin Toss panel layout intro animation
    /// and forwards button selections through a callback supplied by HUD setup.
    /// </summary>
    public class CoinTossUIController : MonoBehaviour
    {
        [Header("UI")]
        public CanvasGroup rootCanvasGroup;
        public RectTransform rootPanel;
        public Image coinImage;
        public TextMeshProUGUI promptText;
#if DOTWEEN_AVAILABLE
        private Tween introFadeTween;
        private Tween introMoveTween;
        private Tween coinBounceTween;
#else
        private Coroutine fallbackPulseRoutine;
#endif

        private void Start()
        {
            PlayIntro();
        }

        private void OnDestroy()
        {
#if DOTWEEN_AVAILABLE
            introFadeTween?.Kill();
            introMoveTween?.Kill();
            coinBounceTween?.Kill();
#else
            if (fallbackPulseRoutine != null)
            {
                StopCoroutine(fallbackPulseRoutine);
                fallbackPulseRoutine = null;
            }
#endif
        }

        private void PlayIntro()
        {
            if (rootCanvasGroup != null)
            {
                rootCanvasGroup.alpha = 0f;
            }

            if (rootPanel != null)
            {
                // Preserve any horizontal offset that layout code (HUDSetup) has applied
                // and only adjust the Y position for the intro slide.
                var current = rootPanel.anchoredPosition;
                rootPanel.anchoredPosition = new Vector2(current.x, -40f);
            }

#if DOTWEEN_AVAILABLE
            if (rootCanvasGroup != null)
            {
                introFadeTween = rootCanvasGroup.DOFade(1f, 0.3f);
            }

            if (rootPanel != null)
            {
                introMoveTween = rootPanel
                    .DOAnchorPosY(0f, 0.35f)
                    .SetEase(Ease.OutQuad);
            }

            if (coinImage != null)
            {
                RectTransform coinRect = coinImage.rectTransform;
                coinRect.localScale = Vector3.one * 1.1f;
                coinBounceTween = coinRect
                    .DOScale(1.2f, 0.6f)
                    .SetEase(Ease.OutSine)
                    .SetLoops(-1, LoopType.Yoyo);
            }
#else
            if (rootCanvasGroup != null)
            {
                rootCanvasGroup.alpha = 1f;
            }

            if (rootPanel != null)
            {
                // Keep whatever X offset was configured and only animate/settle the Y.
                var current = rootPanel.anchoredPosition;
                rootPanel.anchoredPosition = new Vector2(current.x, 0f);
            }

            if (coinImage != null && fallbackPulseRoutine == null)
            {
                fallbackPulseRoutine = StartCoroutine(FallbackCoinPulse());
            }
#endif
        }

        public void Setup(string playerName)
        {
            if (promptText != null)
            {
                promptText.enableAutoSizing = true;
                promptText.fontSizeMax = 48f;
                promptText.fontSizeMin = 24f;
                promptText.text = $"{playerName}: Select Heads or Tails";
                promptText.alignment = TextAlignmentOptions.Center;
            }
        }

        /// <summary>
        /// Starts the coin toss intro animation. Can be called externally to trigger the intro.
        /// </summary>
        public void StartCoinToss()
        {
            PlayIntro();
        }

        /// <summary>
        /// Exposed so HUDSetup can wire the references if a prefab isn't used.
        /// </summary>
        public void InjectDependencies(CanvasGroup canvasGroup, RectTransform panel, Image image,
            TextMeshProUGUI prompt)
        {
            rootCanvasGroup = canvasGroup;
            rootPanel = panel;
            coinImage = image;
            promptText = prompt;
        }

#if !DOTWEEN_AVAILABLE
        private IEnumerator FallbackCoinPulse()
        {
            RectTransform coinRect = coinImage != null ? coinImage.rectTransform : null;
            if (coinRect == null)
            {
                yield break;
            }

            float t = 0f;
            Vector3 baseScale = Vector3.one * 1.1f;
            coinRect.localScale = baseScale;

            while (true)
            {
                t += Time.unscaledDeltaTime;
                float pulse = 0.1f * Mathf.Sin(t * Mathf.PI);
                coinRect.localScale = baseScale * (1f + pulse);
                yield return null;
            }
        }
#endif
    }
}

