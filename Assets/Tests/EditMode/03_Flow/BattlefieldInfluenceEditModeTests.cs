using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CardGame.UI;

namespace CardGame.Tests
{
    /// <summary>
    /// EditMode tests for the Battle Front Influence bar (CardFrontlineUI).
    /// These tests validate structure, wiring, and core API surface so that
    /// PlayMode tests and production code can rely on a stable contract.
    /// </summary>
    public class BattlefieldInfluenceEditModeTests
    {
        [Test]
        public void CardFrontlineUI_Has_Required_Fields_And_Methods()
        {
            var uiType = typeof(CardFrontlineUI);

            // Core UI references
            Assert.IsNotNull(uiType.GetField("titleLabel",
                    BindingFlags.NonPublic | BindingFlags.Instance),
                "CardFrontlineUI should have private field 'titleLabel'.");
            Assert.IsNotNull(uiType.GetField("remainingLabel",
                    BindingFlags.NonPublic | BindingFlags.Instance),
                "CardFrontlineUI should have private field 'remainingLabel'.");
            Assert.IsNotNull(uiType.GetField("p1Fill",
                    BindingFlags.NonPublic | BindingFlags.Instance),
                "CardFrontlineUI should have private field 'p1Fill'.");
            Assert.IsNotNull(uiType.GetField("p2Fill",
                    BindingFlags.NonPublic | BindingFlags.Instance),
                "CardFrontlineUI should have private field 'p2Fill'.");
            Assert.IsNotNull(uiType.GetField("midDivider",
                    BindingFlags.NonPublic | BindingFlags.Instance),
                "CardFrontlineUI should have private field 'midDivider'.");

            // Triangle markers are important for HUDSetup wiring even if hidden at runtime
            Assert.IsNotNull(uiType.GetField("triangleTop",
                    BindingFlags.NonPublic | BindingFlags.Instance),
                "CardFrontlineUI should have private field 'triangleTop'.");
            Assert.IsNotNull(uiType.GetField("triangleBottom",
                    BindingFlags.NonPublic | BindingFlags.Instance),
                "CardFrontlineUI should have private field 'triangleBottom'.");

            // Segment backing fields
            Assert.IsNotNull(uiType.GetField("segmentCount",
                    BindingFlags.NonPublic | BindingFlags.Instance),
                "CardFrontlineUI should have private field 'segmentCount'.");
            Assert.IsNotNull(uiType.GetField("segments",
                    BindingFlags.NonPublic | BindingFlags.Instance),
                "CardFrontlineUI should have private field 'segments' for per-tile blocks.");

            // Public API used by game / tests
            var updateFrontlineMethod = uiType.GetMethod("UpdateFrontline",
                BindingFlags.Public | BindingFlags.Instance);
            Assert.IsNotNull(updateFrontlineMethod,
                "CardFrontlineUI should expose UpdateFrontline(int,int,int) as public API.");
        }

        [Test]
        public void HUDSetup_Creates_CardFrontlineBar_With_Title_And_Component()
        {
            // Arrange a minimal HUD environment: HUDOverlayCanvas and Drop Areas
            var hudCanvas = new GameObject("HUDOverlayCanvas");
            hudCanvas.layer = 5;
            hudCanvas.AddComponent<Canvas>();
            hudCanvas.AddComponent<CanvasScaler>();
            hudCanvas.AddComponent<GraphicRaycaster>();

            var dropAreas = new GameObject("Drop Areas");
            var boardRect = dropAreas.AddComponent<RectTransform>();
            boardRect.sizeDelta = new Vector2(800f, 600f);

            var hudSetupGO = new GameObject("HUDSetup_TestRunner");
            var hudSetup = hudSetupGO.AddComponent<HUDSetup>();

            // Act: invoke the private SetupCardFrontlineUI via reflection
            var setupMethod = typeof(HUDSetup).GetMethod("SetupCardFrontlineUI",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(setupMethod, "HUDSetup should have private SetupCardFrontlineUI method.");

            setupMethod.Invoke(hudSetup, new object[] { hudCanvas.transform });

            // Assert: CardFrontlineBar exists and is parented under HUDOverlayCanvas
            var frontlineBar = GameObject.Find("CardFrontlineBar");
            Assert.IsNotNull(frontlineBar, "CardFrontlineBar should be created by HUDSetup.");
            Assert.AreEqual(hudCanvas.transform, frontlineBar.transform.parent,
                "CardFrontlineBar should be a child of HUDOverlayCanvas.");

            // CardFrontlineUI component must be present
            var frontlineUI = frontlineBar.GetComponent<CardFrontlineUI>();
            Assert.IsNotNull(frontlineUI, "CardFrontlineUI component should be attached to CardFrontlineBar.");

            // Title label text should match Battle Front Influence spec
            var titleField = typeof(CardFrontlineUI).GetField("titleLabel",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var titleLabel = titleField?.GetValue(frontlineUI) as TextMeshProUGUI;
            Assert.IsNotNull(titleLabel, "CardFrontlineUI.titleLabel should be wired by HUDSetup.");
            Assert.AreEqual("Battle Front Influence", titleLabel.text,
                "CardFrontlineUI title text should be 'Battle Front Influence'.");

            // Counter label is present but starts fully transparent with empty text
            var remainingField = typeof(CardFrontlineUI).GetField("remainingLabel",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var remainingLabel = remainingField?.GetValue(frontlineUI) as TextMeshProUGUI;
            Assert.IsNotNull(remainingLabel, "CardFrontlineUI.remainingLabel should be wired by HUDSetup.");
        }
    }
}


