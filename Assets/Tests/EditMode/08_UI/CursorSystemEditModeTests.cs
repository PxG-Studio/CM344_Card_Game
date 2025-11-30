using NUnit.Framework;
using UnityEngine;
using CardGame.UI;
using System.Reflection;

namespace CardGame.Tests
{
    /// <summary>
    /// EditMode tests for Cursor systems - validates structure and API.
    /// Tests CustomCursor, CursorManager, and CursorSpinner components.
    /// </summary>
    public class CursorSystemEditModeTests
    {
        [Test]
        public void CustomCursor_Exists()
        {
            Assert.IsNotNull(typeof(CustomCursor),
                "CustomCursor class should exist");
            
            Assert.IsTrue(typeof(MonoBehaviour).IsAssignableFrom(typeof(CustomCursor)),
                "CustomCursor should inherit from MonoBehaviour");
        }

        [Test]
        public void CursorManager_Exists()
        {
            Assert.IsNotNull(typeof(CursorManager),
                "CursorManager class should exist");
            
            Assert.IsTrue(typeof(MonoBehaviour).IsAssignableFrom(typeof(CursorManager)),
                "CursorManager should inherit from MonoBehaviour");
        }

        [Test]
        public void CursorSpinner_Exists()
        {
            Assert.IsNotNull(typeof(CursorSpinner),
                "CursorSpinner class should exist");
            
            Assert.IsTrue(typeof(MonoBehaviour).IsAssignableFrom(typeof(CursorSpinner)),
                "CursorSpinner should inherit from MonoBehaviour");
        }

        [Test]
        public void CustomCursor_Can_Be_Created()
        {
            GameObject go = new GameObject("TestCustomCursor");
            CustomCursor cursor = go.AddComponent<CustomCursor>();
            
            Assert.IsNotNull(cursor, "CustomCursor component should be creatable");
            
            Object.DestroyImmediate(go);
        }

        [Test]
        public void CursorManager_Can_Be_Created()
        {
            GameObject go = new GameObject("TestCursorManager");
            CursorManager manager = go.AddComponent<CursorManager>();
            
            Assert.IsNotNull(manager, "CursorManager component should be creatable");
            
            Object.DestroyImmediate(go);
        }

        [Test]
        public void CursorSpinner_Can_Be_Created()
        {
            GameObject go = new GameObject("TestCursorSpinner");
            CursorSpinner spinner = go.AddComponent<CursorSpinner>();
            
            Assert.IsNotNull(spinner, "CursorSpinner component should be creatable");
            
            Object.DestroyImmediate(go);
        }
    }
}

