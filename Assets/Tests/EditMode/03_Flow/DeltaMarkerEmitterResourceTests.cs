using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using CardGame.UI;

namespace Tests.EditMode.Flow
{
    public class DeltaMarkerEmitterResourceTests
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
        public void EnsureReady_LoadsConfigAndPrefabFromResources()
        {
            var hudCanvas = new GameObject("HUDOverlayCanvas");
            hudCanvas.AddComponent<Canvas>();
            _createdObjects.Add(hudCanvas);

            var emitterGO = new GameObject("DeltaEmitter");
            var emitter = emitterGO.AddComponent<DeltaMarkerEmitter>();
            _createdObjects.Add(emitterGO);

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
    }
}

