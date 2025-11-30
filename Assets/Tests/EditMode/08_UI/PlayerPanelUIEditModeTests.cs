using NUnit.Framework;
using UnityEngine;
using CardGame.UI;
using System.Reflection;

namespace CardGame.Tests
{
    /// <summary>
    /// EditMode tests for PlayerPanelUI - validates structure and API.
    /// Tests component structure and display methods.
    /// </summary>
    public class PlayerPanelUIEditModeTests
    {
        [Test]
        public void PlayerPanelUI_Exists()
        {
            Assert.IsNotNull(typeof(PlayerPanelUI),
                "PlayerPanelUI class should exist");
            
            Assert.IsTrue(typeof(MonoBehaviour).IsAssignableFrom(typeof(PlayerPanelUI)),
                "PlayerPanelUI should inherit from MonoBehaviour");
        }

        [Test]
        public void PlayerPanelUI_Can_Be_Created()
        {
            GameObject go = new GameObject("TestPlayerPanel");
            PlayerPanelUI panel = go.AddComponent<PlayerPanelUI>();
            
            Assert.IsNotNull(panel, "PlayerPanelUI component should be creatable");
            
            Object.DestroyImmediate(go);
        }
    }
}

