using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Tests.EditMode.Flow
{
    public class HUDBackgroundSetupEditModeTests
    {
        private readonly List<GameObject> _createdObjects = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _createdObjects)
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
            }
            _createdObjects.Clear();
        }

        [Test]
        public void SetupBoardBackdrop_CreatesFullScreenImageOnHudCanvas()
        {
            var hudCanvas = CreateNamedGO("HUDOverlayCanvas");
            hudCanvas.AddComponent<Canvas>();
            var dropAreas = CreateNamedGO("Drop Areas");
            CreateDropAreaChild(dropAreas.transform, "SlotA", new Vector3(-2, 0, 0));
            CreateDropAreaChild(dropAreas.transform, "SlotB", new Vector3(2, 0, 0));

            var hudSetup = CreateHUDSetup();

            InvokeSetupBackdrop(hudSetup);

            var backdrop = GameObject.Find("BattlegroundBackdrop");
            Assert.IsNotNull(backdrop, "Backdrop object should exist after setup.");
            Assert.AreEqual("HUDOverlayCanvas", backdrop.transform.parent.name);

            var image = backdrop.GetComponent<Image>();
            Assert.IsNotNull(image);
            Assert.False(image.raycastTarget, "Backdrop image must not block UI input.");

            var rect = backdrop.GetComponent<RectTransform>();
            Assert.AreEqual(Vector2.zero, rect.anchorMin);
            Assert.AreEqual(Vector2.one, rect.anchorMax);
            Assert.AreEqual(Vector2.zero, rect.offsetMin);
            Assert.AreEqual(Vector2.zero, rect.offsetMax);
        }

        [Test]
        public void SetupBoardBackdrop_RemovesLegacyWorldSpaceBackdrops()
        {
            var hudCanvas = CreateNamedGO("HUDOverlayCanvas");
            hudCanvas.AddComponent<Canvas>();
            var dropAreas = CreateNamedGO("Drop Areas");
            var legacy = new GameObject("BattlegroundSprite");
            legacy.transform.SetParent(dropAreas.transform, false);
            _createdObjects.Add(legacy);

            var hudSetup = CreateHUDSetup();
            InvokeSetupBackdrop(hudSetup);

            Assert.IsNull(dropAreas.transform.Find("BattlegroundSprite"),
                "Legacy world-space backdrop should be removed.");
            Assert.IsNull(dropAreas.transform.Find("BattlegroundBackdrop"),
                "Drop Areas should no longer host the BattlegroundBackdrop after setup.");
        }

        private HUDSetup CreateHUDSetup()
        {
            var go = CreateNamedGO("HUDSetup_TestRunner");
            return go.AddComponent<HUDSetup>();
        }

        private GameObject CreateNamedGO(string name)
        {
            var go = new GameObject(name);
            _createdObjects.Add(go);
            return go;
        }

        private void CreateDropAreaChild(Transform parent, string name, Vector3 position)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            child.transform.localPosition = position;
            _createdObjects.Add(child);
        }

        private void InvokeSetupBackdrop(HUDSetup hudSetup)
        {
            var method = typeof(HUDSetup)
                .GetMethod("SetupBoardBackdrop", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method, "Unable to find SetupBoardBackdrop via reflection.");
            method.Invoke(hudSetup, null);
        }
    }
}

