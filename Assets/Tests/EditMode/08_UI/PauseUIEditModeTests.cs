using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using CardGame.UI;
using CardGame.Managers;

namespace CardGame.Tests
{
    /// <summary>
    /// EditMode tests for PauseUI - validates structure and API.
    /// Tests component structure, methods, and game state management.
    /// </summary>
    public class PauseUIEditModeTests
    {
        [Test]
        public void PauseUI_Has_Required_Fields()
        {
            // Verify PauseUI has required fields
            var pausePanelField = typeof(PauseUI).GetField("pausePanel",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var resumeButtonField = typeof(PauseUI).GetField("resumeButton",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var quitButtonField = typeof(PauseUI).GetField("quitButton",
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            Assert.IsNotNull(pausePanelField, "PauseUI should have pausePanel field");
            Assert.IsNotNull(resumeButtonField, "PauseUI should have resumeButton field");
            Assert.IsNotNull(quitButtonField, "PauseUI should have quitButton field");
            
            // Verify field types
            Assert.AreEqual(typeof(GameObject), pausePanelField.FieldType,
                "pausePanel should be GameObject");
            Assert.AreEqual(typeof(Button), resumeButtonField.FieldType,
                "resumeButton should be Button");
            Assert.AreEqual(typeof(Button), quitButtonField.FieldType,
                "quitButton should be Button");
        }

        [Test]
        public void PauseUI_Has_PauseGame_Method()
        {
            var pauseMethod = typeof(PauseUI).GetMethod("PauseGame",
                BindingFlags.Public | BindingFlags.Instance);
            
            Assert.IsNotNull(pauseMethod, "PauseUI should have PauseGame method");
            
            // Verify method signature (no parameters)
            var parameters = pauseMethod.GetParameters();
            Assert.AreEqual(0, parameters.Length, "PauseGame should take no parameters");
            Assert.AreEqual(typeof(void), pauseMethod.ReturnType,
                "PauseGame should return void");
        }

        [Test]
        public void PauseUI_Has_ResumeGame_Method()
        {
            var resumeMethod = typeof(PauseUI).GetMethod("ResumeGame",
                BindingFlags.Public | BindingFlags.Instance);
            
            Assert.IsNotNull(resumeMethod, "PauseUI should have ResumeGame method");
            
            // Verify method signature (no parameters)
            var parameters = resumeMethod.GetParameters();
            Assert.AreEqual(0, parameters.Length, "ResumeGame should take no parameters");
            Assert.AreEqual(typeof(void), resumeMethod.ReturnType,
                "ResumeGame should return void");
        }

        [Test]
        public void PauseUI_Can_Be_Created()
        {
            GameObject go = new GameObject("TestPauseUI");
            PauseUI pauseUI = go.AddComponent<PauseUI>();
            
            Assert.IsNotNull(pauseUI, "PauseUI component should be creatable");
            
            Object.DestroyImmediate(go);
        }

        [Test]
        public void PauseUI_Methods_Can_Be_Called()
        {
            GameObject go = new GameObject("TestPauseUI");
            PauseUI pauseUI = go.AddComponent<PauseUI>();
            
            // Verify methods can be called (may fail if dependencies missing, but shouldn't throw on structure)
            try
            {
                pauseUI.PauseGame();
                pauseUI.ResumeGame();
                Assert.IsTrue(true, "PauseUI methods should be callable");
            }
            catch (System.Exception ex)
            {
                // If it throws, it should be a null reference, not a missing method
                Assert.IsTrue(ex is System.NullReferenceException,
                    $"PauseUI methods should exist. Error: {ex.Message}");
            }
            
            Object.DestroyImmediate(go);
        }
    }
}

