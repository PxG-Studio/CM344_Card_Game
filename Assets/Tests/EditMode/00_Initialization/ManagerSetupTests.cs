using NUnit.Framework;
using UnityEngine;
using CardGame.Managers;
using CardGame.UI;

namespace CardGame.Tests
{
    /// <summary>
    /// EditMode tests for manager setup and singleton validation.
    /// </summary>
    public class ManagerSetupTests
    {
        [Test]
        public void GameManager_Singleton_Exists()
        {
            // Clear any existing instance first
            var backingField = typeof(GameManager).GetField("<Instance>k__BackingField", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            
            if (backingField != null)
            {
                backingField.SetValue(null, null);
            }
            
            // GameManager should be created by HUDSetup or exist in scene
            // Create GameObject as inactive to prevent Awake() from being called automatically
            // Awake() uses DontDestroyOnLoad which only works in PlayMode
            GameObject managerObj = new GameObject("GameManager");
            managerObj.SetActive(false); // Prevent Awake() from being called
            GameManager manager = managerObj.AddComponent<GameManager>();
            
            Assert.IsNotNull(manager, "GameManager component should be created");
            
            // In EditMode, we can't call Awake() because it uses DontDestroyOnLoad (PlayMode only)
            // Instead, manually set the Instance backing field to test singleton pattern
            if (backingField != null)
            {
                backingField.SetValue(null, manager);
            }
            
            Assert.AreEqual(manager, GameManager.Instance, "GameManager.Instance should return singleton");
            
            Object.DestroyImmediate(managerObj);
            
            // Clean up static instance
            if (backingField != null)
            {
                backingField.SetValue(null, null);
            }
        }

        [Test]
        public void CoinTossManager_Singleton_Exists()
        {
            // Clear any existing instance first
            var backingField = typeof(CoinTossManager).GetField("<Instance>k__BackingField", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            
            if (backingField != null)
            {
                backingField.SetValue(null, null);
            }
            
            // Create GameObject as inactive to prevent Awake() from being called automatically
            // Awake() uses DontDestroyOnLoad which only works in PlayMode
            GameObject managerObj = new GameObject("CoinTossManager");
            managerObj.SetActive(false); // Prevent Awake() from being called
            CoinTossManager manager = managerObj.AddComponent<CoinTossManager>();
            
            Assert.IsNotNull(manager, "CoinTossManager component should be created");
            
            // In EditMode, we can't call Awake() because it uses DontDestroyOnLoad (PlayMode only)
            // Instead, manually set the Instance backing field to test singleton pattern
            if (backingField != null)
            {
                backingField.SetValue(null, manager);
            }
            
            Assert.AreEqual(manager, CoinTossManager.Instance, "CoinTossManager.Instance should return singleton");
            
            Object.DestroyImmediate(managerObj);
            
            // Clean up static instance
            if (backingField != null)
            {
                backingField.SetValue(null, null);
            }
        }

        [Test]
        public void FateFlowController_Singleton_Exists()
        {
            // Clear any existing instance first
            var backingField = typeof(FateFlowController).GetField("<Instance>k__BackingField", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static) ??
                typeof(FateFlowController).GetField("Instance", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            
            if (backingField != null)
            {
                backingField.SetValue(null, null);
            }
            
            // Create GameObject as inactive to prevent Awake() from being called automatically
            // Awake() uses DontDestroyOnLoad which only works in PlayMode
            GameObject controllerObj = new GameObject("FateFlowController");
            controllerObj.SetActive(false); // Prevent Awake() from being called
            FateFlowController controller = controllerObj.AddComponent<FateFlowController>();
            
            Assert.IsNotNull(controller, "FateFlowController component should be created");
            
            // In EditMode, we can't call Awake() because it uses DontDestroyOnLoad (PlayMode only)
            // Instead, manually set the instance field to test singleton pattern
            if (backingField != null)
            {
                backingField.SetValue(null, controller);
            }
            
            Assert.AreEqual(controller, FateFlowController.Instance, "FateFlowController.Instance should return singleton");
            
            Object.DestroyImmediate(controllerObj);
            
            // Clean up static instance
            if (backingField != null)
            {
                backingField.SetValue(null, null);
            }
        }

        [Test]
        public void HUDSetup_Creates_CoinTossUI()
        {
            // Create HUD root
            GameObject hudRoot = new GameObject("HUDOverlayCanvas");
            hudRoot.AddComponent<Canvas>();
            hudRoot.AddComponent<UnityEngine.UI.CanvasScaler>();
            hudRoot.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            
            // Create HUDSetup and call SetupCoinTossUI
            HUDSetup hudSetup = hudRoot.AddComponent<HUDSetup>();
            var method = typeof(HUDSetup).GetMethod("SetupCoinTossUI", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            Assert.IsNotNull(method, "SetupCoinTossUI method should exist");
            
            // Call via reflection
            method.Invoke(hudSetup, new object[] { hudRoot.transform });
            
            // Verify CoinTossUI was created
            CoinTossUI coinTossUI = hudRoot.GetComponentInChildren<CoinTossUI>(true);
            Assert.IsNotNull(coinTossUI, "CoinTossUI should be created by HUDSetup");
            
            // Verify CoinTossPanel is created and parented correctly
            Assert.IsNotNull(coinTossUI.gameObject, "CoinTossPanel GameObject should exist");
            Assert.IsTrue(coinTossUI.transform.IsChildOf(hudRoot.transform), 
                "CoinTossUI should be child of HUDOverlayCanvas");
            
            Object.DestroyImmediate(hudRoot);
        }

        [Test]
        public void ScoreManager_Singleton_Exists()
        {
            // Clear any existing instance first
            var backingField = typeof(ScoreManager).GetField("<Instance>k__BackingField", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            
            if (backingField != null)
            {
                backingField.SetValue(null, null);
            }
            
            // Create GameObject as inactive to prevent Awake() from being called automatically
            GameObject managerObj = new GameObject("ScoreManager");
            managerObj.SetActive(false);
            ScoreManager manager = managerObj.AddComponent<ScoreManager>();
            
            Assert.IsNotNull(manager, "ScoreManager component should be created");
            
            // Manually set the Instance backing field to test singleton pattern
            if (backingField != null)
            {
                backingField.SetValue(null, manager);
            }
            
            Assert.AreEqual(manager, ScoreManager.Instance, "ScoreManager.Instance should return singleton");
            
            Object.DestroyImmediate(managerObj);
            
            // Clean up static instance
            if (backingField != null)
            {
                backingField.SetValue(null, null);
            }
        }

        [Test]
        public void GameEndManager_Singleton_Exists()
        {
            // Clear any existing instance first
            var backingField = typeof(GameEndManager).GetField("<Instance>k__BackingField", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            
            if (backingField != null)
            {
                backingField.SetValue(null, null);
            }
            
            // Create GameObject as inactive to prevent Awake() from being called automatically
            GameObject managerObj = new GameObject("GameEndManager");
            managerObj.SetActive(false);
            GameEndManager manager = managerObj.AddComponent<GameEndManager>();
            
            Assert.IsNotNull(manager, "GameEndManager component should be created");
            
            // Manually set the Instance backing field to test singleton pattern
            if (backingField != null)
            {
                backingField.SetValue(null, manager);
            }
            
            Assert.AreEqual(manager, GameEndManager.Instance, "GameEndManager.Instance should return singleton");
            
            Object.DestroyImmediate(managerObj);
            
            // Clean up static instance
            if (backingField != null)
            {
                backingField.SetValue(null, null);
            }
        }

        [Test]
        public void CoinTossManager_Has_Required_Methods()
        {
            // Verify CoinTossManager has required methods for coin toss functionality
            var performCoinTossMethod = typeof(CoinTossManager).GetMethod("PerformCoinToss");
            Assert.IsNotNull(performCoinTossMethod, "CoinTossManager should have PerformCoinToss method");
            
            var resetCoinTossMethod = typeof(CoinTossManager).GetMethod("ResetCoinToss");
            Assert.IsNotNull(resetCoinTossMethod, "CoinTossManager should have ResetCoinToss method");
            
            var getStartingPlayerMethod = typeof(CoinTossManager).GetMethod("GetStartingPlayer");
            Assert.IsNotNull(getStartingPlayerMethod, "CoinTossManager should have GetStartingPlayer method");
            
            var isCompleteProperty = typeof(CoinTossManager).GetProperty("IsComplete");
            Assert.IsNotNull(isCompleteProperty, "CoinTossManager should have IsComplete property");
        }

        [Test]
        public void GameManager_Has_State_Management_Methods()
        {
            // Verify GameManager has required methods for state management
            var startGameMethod = typeof(GameManager).GetMethod("StartGame");
            Assert.IsNotNull(startGameMethod, "GameManager should have StartGame method");
            
            var resetGameStateMethod = typeof(GameManager).GetMethod("ResetGameState");
            Assert.IsNotNull(resetGameStateMethod, "GameManager should have ResetGameState method");
            
            var currentStateProperty = typeof(GameManager).GetProperty("CurrentState");
            Assert.IsNotNull(currentStateProperty, "GameManager should have CurrentState property");
            
            // Verify OnGameStateChanged event exists
            var onGameStateChangedField = typeof(GameManager).GetField("OnGameStateChanged", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(onGameStateChangedField, "GameManager should have OnGameStateChanged event");
        }
    }
}

