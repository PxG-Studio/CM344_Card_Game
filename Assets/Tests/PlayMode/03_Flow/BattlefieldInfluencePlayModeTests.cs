using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using TMPro;
using CardGame.UI;

namespace CardGame.Tests
{
    /// <summary>
    /// PlayMode tests for the Battle Front Influence bar (CardFrontlineUI).
    /// These tests verify that the bar is created by HUDSetup in the
    /// BattleScreenMultiplayer scene and that basic update behaviour works.
    /// </summary>
    public class BattlefieldInfluencePlayModeTests
    {
        private const string SCENE_NAME = "BattleScreenMultiplayer";

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            CardTestHelper.ClearSingletonInstances();
            yield return null;

            var asyncLoad = SceneManager.LoadSceneAsync(SCENE_NAME, LoadSceneMode.Single);
            asyncLoad.allowSceneActivation = true;

            float timeout = 10f;
            float elapsed = 0f;
            while (!asyncLoad.isDone && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (!asyncLoad.isDone)
            {
                Assert.Fail($"Scene '{SCENE_NAME}' failed to load within {timeout} seconds");
            }

            // Allow HUDSetup to finish building HUD + CardFrontlineUI
            yield return new WaitForSeconds(1.0f);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return null;
            CardTestHelper.ClearSingletonInstances();
            yield return null;
        }

        [UnityTest]
        public IEnumerator CardFrontlineBar_Is_Created_And_Wired()
        {
            yield return new WaitForSeconds(0.5f);

            GameObject bar = GameObject.Find("CardFrontlineBar");
            Assert.IsNotNull(bar, "CardFrontlineBar should exist after HUDSetup runs.");

            var ui = bar.GetComponent<CardFrontlineUI>();
            Assert.IsNotNull(ui, "CardFrontlineUI component should be attached to CardFrontlineBar.");

            GameObject hudCanvas = GameObject.Find("HUDOverlayCanvas");
            Assert.IsNotNull(hudCanvas, "HUDOverlayCanvas should exist in scene.");
            Assert.IsTrue(bar.transform.IsChildOf(hudCanvas.transform),
                "CardFrontlineBar should be parented under HUDOverlayCanvas.");

            // Verify the title label text matches UX spec
            var titleField = typeof(CardFrontlineUI)
                .GetField("titleLabel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var titleLabel = titleField?.GetValue(ui) as TextMeshProUGUI;
            Assert.IsNotNull(titleLabel, "CardFrontlineUI.titleLabel should be assigned.");
            Assert.AreEqual("Battle Front Influence", titleLabel.text,
                "Battlefield influence bar title should read 'Battle Front Influence'.");
        }

        [UnityTest]
        public IEnumerator CardFrontlineUI_UpdateFrontline_Paints_Segments_For_P1()
        {
            yield return new WaitForSeconds(0.5f);

            var ui = Object.FindObjectOfType<CardFrontlineUI>();
            Assert.IsNotNull(ui, "CardFrontlineUI should exist in BattleScreenMultiplayer.");

            // Force initial Start() logic (segments + colours) to have run
            yield return new WaitForEndOfFrame();

            // Use reflection to get private fields
            var type = typeof(CardFrontlineUI);
            var segmentsField = type.GetField("segments",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var emptyColorField = type.GetField("emptySegmentColor",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var p1ColorField = type.GetField("p1SegmentColor",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var segments = segmentsField?.GetValue(ui) as Image[];
            Assert.IsNotNull(segments, "CardFrontlineUI.segments should be initialised in Start().");
            Assert.GreaterOrEqual(segments.Length, 4, "There should be at least 4 segments for a 4x4 board.");

            var emptyColor = (Color)emptyColorField.GetValue(ui);
            var p1Color = (Color)p1ColorField.GetValue(ui);

            // Act: simulate P1 controlling 3 tiles, P2 0, remaining 13.
            ui.UpdateFrontline(3, 0, 13);

            // Give the bar a short moment to process animations
            yield return new WaitForSeconds(0.1f);

            // First three segments should be P1 colour, others should remain empty
            for (int i = 0; i < segments.Length; i++)
            {
                var seg = segments[i];
                Assert.IsNotNull(seg, $"Segment {i} should not be null.");
                if (i < 3)
                {
                    Assert.AreEqual(p1Color, seg.color,
                        $"Segment {i} should reflect P1 control colour after UpdateFrontline.");
                }
                else
                {
                    Assert.AreEqual(emptyColor, seg.color,
                        $"Segment {i} should remain empty when only first three tiles are controlled by P1.");
                }
            }
        }
    }
}


