using System;
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
        public Button headsButton;
        public Button tailsButton;
        public TextMeshProUGUI promptText;

        private Action<bool> onSelectionCallback;
#if DOTWEEN_AVAILABLE
        private Tween introFadeTween;
        private Tween introMoveTween;
        private Tween coinBounceTween;
#endif

        private void Start()
        {
            if (headsButton != null)
            {
                headsButton.onClick.AddListener(() => Select(true));
            }

            if (tailsButton != null)
            {
                tailsButton.onClick.AddListener(() => Select(false));
            }

            PlayIntro();
        }

        private void OnDestroy()
        {
#if DOTWEEN_AVAILABLE
            introFadeTween?.Kill();
            introMoveTween?.Kill();
            coinBounceTween?.Kill();
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
                rootPanel.anchoredPosition = new Vector2(0f, -40f);
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
#endif
        }

        private void Select(bool isHeads)
        {
            if (headsButton != null)
            {
                headsButton.interactable = false;
            }

            if (tailsButton != null)
            {
                tailsButton.interactable = false;
            }

            onSelectionCallback?.Invoke(isHeads);
        }

        /// <summary>
        /// Injects the display name and callback that should be invoked when the player makes a choice.
        /// </summary>
        public void Setup(string playerName, Action<bool> callback)
        {
            onSelectionCallback = callback;

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
        /// Exposed so HUDSetup can wire the references if a prefab isn't used.
        /// </summary>
        public void InjectDependencies(CanvasGroup canvasGroup, RectTransform panel, Image image,
            Button heads, Button tails, TextMeshProUGUI prompt)
        {
            rootCanvasGroup = canvasGroup;
            rootPanel = panel;
            coinImage = image;
            headsButton = heads;
            tailsButton = tails;
            promptText = prompt;
        }
    }
}

