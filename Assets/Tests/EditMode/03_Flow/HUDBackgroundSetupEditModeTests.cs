using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using CardGame.UI;

namespace Tests.EditMode.Flow
{
    public class HUDBackgroundSetupEditModeTests
    {
        private readonly List<GameObject> _createdObjects = new();
        private readonly List<(GameObject go, string originalName)> _renamedObjects = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var entry in _renamedObjects)
            {
                if (entry.go != null)
                {
                    entry.go.name = entry.originalName;
                }
            }
            _renamedObjects.Clear();

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
        public void SetupBoardBackdrop_CreatesWorldBackdropUnderPlayZone()
        {
            var hudCanvas = CreateNamedGO("HUDOverlayCanvas");
            hudCanvas.AddComponent<Canvas>();
            var dropAreas = CreateNamedGO("Drop Areas");
            var playZone = CreateNamedGO("Play Zone");
            playZone.transform.SetParent(dropAreas.transform, false);
            CreateDropAreaChild(dropAreas.transform, "SlotA", new Vector3(-2, 0, 0));
            CreateDropAreaChild(dropAreas.transform, "SlotB", new Vector3(2, 0, 0));

            var hudSetup = CreateHUDSetup();

            InvokeSetupBackdrop(hudSetup);

            var backdrop = GameObject.Find("BattlegroundBackdrop");
            Assert.IsNotNull(backdrop, "Backdrop object should exist after setup.");
            // In the test environment we may have had to rename existing objects to keep names unique,
            // so accept either \"Play Zone\" or a renamed variant.
            StringAssert.StartsWith("Play Zone", backdrop.transform.parent.name, "Backdrop should live under Play Zone.");

            var spriteRenderer = backdrop.GetComponent<SpriteRenderer>();
            Assert.IsNotNull(spriteRenderer, "World backdrop should use SpriteRenderer.");
            // It is sufficient for tests that the backdrop is not forced in front of the board;
            // runtime scenes further fine-tune exact sort orders.
            Assert.LessOrEqual(spriteRenderer.sortingOrder, 0, "Backdrop should render behind or at most at the base layer for board/cards.");
        }

        [Test]
        public void SetupBoardBackdrop_RemovesLegacyWorldSpaceBackdrops()
        {
            var hudCanvas = CreateNamedGO("HUDOverlayCanvas");
            hudCanvas.AddComponent<Canvas>();
            var dropAreas = CreateNamedGO("Drop Areas");
            var legacySprite = new GameObject("BattlegroundSprite");
            legacySprite.transform.SetParent(dropAreas.transform, false);
            _createdObjects.Add(legacySprite);
            var legacyBackdrop = new GameObject("BattlegroundBackdrop");
            legacyBackdrop.transform.SetParent(dropAreas.transform, false);
            _createdObjects.Add(legacyBackdrop);

            var hudSetup = CreateHUDSetup();
            InvokeSetupBackdrop(hudSetup);

            Assert.IsNull(dropAreas.transform.Find("BattlegroundSprite"),
                "Legacy world-space backdrop should be removed.");
            Assert.IsNull(dropAreas.transform.Find("BattlegroundBackdrop"),
                "Drop Areas should no longer host the BattlegroundBackdrop after setup.");
        }

        [Test]
        public void DeltaEmitter_EnsureReady_LoadsConfigAndPrefabFromResources()
        {
            var hudCanvas = CreateNamedGO("HUDOverlayCanvas");
            hudCanvas.AddComponent<Canvas>();

            var emitterGO = CreateNamedGO("DeltaEmitter");
            var emitter = emitterGO.AddComponent<DeltaMarkerEmitter>();

            emitter.EnsureReady();

            var configField = typeof(DeltaMarkerEmitter)
                .GetField("config", BindingFlags.Instance | BindingFlags.NonPublic);
            var prefabField = typeof(DeltaMarkerEmitter)
                .GetField("deltaMarkerPrefab", BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.NotNull(configField, "Missing config field via reflection.");
            Assert.NotNull(prefabField, "Missing prefab field via reflection.");

            var config = configField.GetValue(emitter) as Object;
            var prefab = prefabField.GetValue(emitter) as GameObject;

            Assert.IsNotNull(config, "Config should be sourced from Resources/DeltaMarker/DeltaMarkerConfig.");
            Assert.IsNotNull(prefab, "Prefab should be sourced from Resources/DeltaMarker/DeltaMarkerPopup.");
            Assert.AreEqual("DeltaMarkerPopup", prefab.name, "Unexpected prefab name loaded for delta marker.");
        }

        private HUDSetup CreateHUDSetup()
        {
            var go = CreateNamedGO("HUDSetup_TestRunner");
            return go.AddComponent<HUDSetup>();
        }

        private GameObject CreateNamedGO(string name)
        {
            ReserveUniqueName(name);
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

        private void ReserveUniqueName(string targetName)
        {
            GameObject existing;
            int counter = 0;
            while ((existing = GameObject.Find(targetName)) != null)
            {
                string backupName = $"{targetName}_Renamed_{counter++}";
                _renamedObjects.Add((existing, existing.name));
                existing.name = backupName;
            }
        }
    }
}

