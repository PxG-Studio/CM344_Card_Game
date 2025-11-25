using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using CardGame.Managers;
using CardGame.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Tests.PlayMode.CoinToss
{
    public class CoinTossInteractionPlayModeTests
    {
        private readonly List<GameObject> _cleanup = new();

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            if (EventSystem.current == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                _cleanup.Add(es);
            }

            var managerGO = new GameObject("CoinTossManager");
            managerGO.AddComponent<CoinTossManager>();
            _cleanup.Add(managerGO);

            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _cleanup)
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
            }
            _cleanup.Clear();
        }

        [UnityTest]
        public IEnumerator HeadsSelectionRegistersThroughCoinTossUI()
        {
            var ui = BuildCoinTossUI(out var headsButton, out var tailsButton, out var selectionPanel);

            yield return null; // Allow Unity lifecycle (Awake/Start) to run

            ui.StartCoinToss();
            yield return null;

            Assert.True(headsButton.interactable, "Heads button should be interactable after StartCoinToss.");
            Assert.False(CoinTossManager.Instance.HasSelection, "No selection should exist before clicking.");

            headsButton.onClick.Invoke();
            yield return null;

            Assert.True(CoinTossManager.Instance.HasSelection, "Clicking the heads button should register a selection.");
            Assert.IsFalse(selectionPanel.activeSelf, "Selection panel should hide once a choice is made.");
        }

        private CoinTossUI BuildCoinTossUI(out Button headsButton, out Button tailsButton, out GameObject selectionPanel)
        {
            var panel = new GameObject("CoinTossPanel");
            panel.AddComponent<Canvas>();
            _cleanup.Add(panel);

            var coinTossUI = panel.AddComponent<CoinTossUI>();

            // Shared helpers
            var headsGO = CreateButton("HeadsButton", "HEADS");
            var tailsGO = CreateButton("TailsButton", "TAILS");
            selectionPanel = new GameObject("ButtonsContainer");
            selectionPanel.transform.SetParent(panel.transform, false);
            headsGO.transform.SetParent(selectionPanel.transform, false);
            tailsGO.transform.SetParent(selectionPanel.transform, false);

            var promptGO = CreateTMP("SelectionPrompt");
            promptGO.transform.SetParent(panel.transform, false);
            var resultGO = CreateTMP("ResultText");
            resultGO.transform.SetParent(panel.transform, false);
            var coinImageGO = CreateImage("CoinImage");
            coinImageGO.transform.SetParent(panel.transform, false);
            var hoverContainerGO = new GameObject("HoverContainer", typeof(RectTransform));
            hoverContainerGO.transform.SetParent(panel.transform, false);
            var continueButtonGO = CreateButton("ContinueButton", "Continue");
            continueButtonGO.transform.SetParent(panel.transform, false);

            headsButton = headsGO.GetComponent<Button>();
            tailsButton = tailsGO.GetComponent<Button>();

            AssignPrivateField(coinTossUI, "coinTossPanel", panel);
            AssignPrivateField(coinTossUI, "headsButton", headsButton);
            AssignPrivateField(coinTossUI, "tailsButton", tailsButton);
            AssignPrivateField(coinTossUI, "selectionPanel", selectionPanel);
            AssignPrivateField(coinTossUI, "selectionPromptText", promptGO.GetComponent<TextMeshProUGUI>());
            AssignPrivateField(coinTossUI, "resultText", resultGO.GetComponent<TextMeshProUGUI>());
            AssignPrivateField(coinTossUI, "coinImage", coinImageGO.GetComponent<Image>());
            AssignPrivateField(coinTossUI, "headsLabel", headsGO.GetComponentInChildren<TextMeshProUGUI>());
            AssignPrivateField(coinTossUI, "tailsLabel", tailsGO.GetComponentInChildren<TextMeshProUGUI>());
            AssignPrivateField(coinTossUI, "hoverContainer", hoverContainerGO.GetComponent<RectTransform>());
            AssignPrivateField(coinTossUI, "continueButton", continueButtonGO.GetComponent<Button>());

            return coinTossUI;
        }

        private GameObject CreateButton(string name, string label)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            var textGO = CreateTMP($"{name}_Text");
            textGO.transform.SetParent(go.transform, false);
            textGO.GetComponent<TextMeshProUGUI>().text = label;
            _cleanup.Add(go);
            return go;
        }

        private GameObject CreateTMP(string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = "";
            tmp.fontSize = 24;
            tmp.alignment = TextAlignmentOptions.Center;
            _cleanup.Add(go);
            return go;
        }

        private GameObject CreateImage(string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            _cleanup.Add(go);
            return go;
        }

        private static void AssignPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, $"Unable to find field '{fieldName}' via reflection.");
            field.SetValue(target, value);
        }
    }
}

