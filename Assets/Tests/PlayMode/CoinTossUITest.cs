using System.Collections;
using CardGame.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace CardGame.Tests.PlayMode
{
    public class CoinTossUITest
    {
        [UnityTest]
        public IEnumerator UI_Layout_IsCenteredAndButtonsVisible()
        {
            EnsureEventSystem();

            var canvasRoot = new GameObject("TestCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasRoot.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

            GameObject panel = CoinTossUITestFactory.CreatePanel();
            panel.transform.SetParent(canvasRoot.transform, false);

            var controller = panel.GetComponent<CoinTossUIController>();
            Assert.IsNotNull(controller);
            Assert.IsNotNull(controller.headsButton);
            Assert.IsNotNull(controller.tailsButton);
            Assert.IsTrue(controller.headsButton.gameObject.activeSelf);
            Assert.IsTrue(controller.tailsButton.gameObject.activeSelf);

            yield return null;
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current == null)
            {
                var eventSystemObj = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                Object.DontDestroyOnLoad(eventSystemObj);
            }
        }
    }

    internal static class CoinTossUITestFactory
    {
        internal static GameObject CreatePanel()
        {
            var panel = new GameObject("CoinTossPanel_Test",
                typeof(RectTransform),
                typeof(CanvasGroup),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(CoinTossUIController));

            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(650f, 550f);

            var controller = panel.GetComponent<CoinTossUIController>();
            var panelCanvasGroup = panel.GetComponent<CanvasGroup>();

            var root = new GameObject("PanelRoot",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            root.transform.SetParent(panel.transform, false);
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = new Vector2(650f, 550f);

            // Coin image
            var coinGO = new GameObject("CoinImage",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            coinGO.transform.SetParent(root.transform, false);
            var coinRect = coinGO.GetComponent<RectTransform>();
            coinRect.anchorMin = coinRect.anchorMax = new Vector2(0.5f, 0.65f);
            coinRect.pivot = new Vector2(0.5f, 0.5f);
            coinRect.sizeDelta = new Vector2(180f, 180f);

            // Prompt text
            var promptGO = new GameObject("PromptText",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            promptGO.transform.SetParent(root.transform, false);
            var promptRect = promptGO.GetComponent<RectTransform>();
            promptRect.anchorMin = new Vector2(0.5f, 0.35f);
            promptRect.anchorMax = new Vector2(0.5f, 0.35f);
            promptRect.pivot = new Vector2(0.5f, 0.5f);
            promptRect.sizeDelta = new Vector2(600f, 60f);
            var promptTMP = promptGO.GetComponent<TextMeshProUGUI>();
            promptTMP.text = "Player 1: Select Heads or Tails";
            promptTMP.enableAutoSizing = true;
            promptTMP.fontSizeMin = 24f;
            promptTMP.fontSizeMax = 48f;
            promptTMP.alignment = TextAlignmentOptions.Center;

            // Buttons container
            var buttonsContainer = new GameObject("ButtonsContainer",
                typeof(RectTransform),
                typeof(HorizontalLayoutGroup),
                typeof(ContentSizeFitter));
            buttonsContainer.transform.SetParent(root.transform, false);
            var buttonsRect = buttonsContainer.GetComponent<RectTransform>();
            buttonsRect.anchorMin = new Vector2(0.5f, 0.15f);
            buttonsRect.anchorMax = new Vector2(0.5f, 0.15f);
            buttonsRect.pivot = new Vector2(0.5f, 0.5f);
            buttonsRect.sizeDelta = new Vector2(520f, 90f);

            var hGroup = buttonsContainer.GetComponent<HorizontalLayoutGroup>();
            hGroup.spacing = 40f;
            hGroup.childAlignment = TextAnchor.MiddleCenter;
            hGroup.childControlWidth = false;
            hGroup.childControlHeight = false;
            var fitter = buttonsContainer.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            var headsButton = CreateButton(buttonsContainer.transform, "HEADS");
            var tailsButton = CreateButton(buttonsContainer.transform, "TAILS");

            controller.InjectDependencies(panelCanvasGroup, rootRect,
                coinGO.GetComponent<Image>(),
                headsButton, tailsButton, promptTMP);
            controller.Setup("Player 1", _ => { });

            return panel;
        }

        private static Button CreateButton(Transform parent, string label)
        {
            var buttonGO = new GameObject($"{label}Button",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button),
                typeof(LayoutElement));
            buttonGO.transform.SetParent(parent, false);
            var rect = buttonGO.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(220f, 70f);

            var layout = buttonGO.GetComponent<LayoutElement>();
            layout.preferredWidth = 220f;
            layout.preferredHeight = 70f;

            var textGO = new GameObject("Label",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            textGO.transform.SetParent(buttonGO.transform, false);
            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            var tmp = textGO.GetComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 18f;
            tmp.fontSizeMax = 32f;

            return buttonGO.GetComponent<Button>();
        }
    }
}

