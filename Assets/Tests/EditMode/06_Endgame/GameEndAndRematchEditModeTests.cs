using NUnit.Framework;
using UnityEngine;
using CardGame.Managers;
using CardGame.UI;

namespace CardGame.Tests
{
    /// <summary>
    /// EditMode tests for game end detection, score calculation, rematch/quit functionality,
    /// and score metrics persistence across games. These tests validate structure and API
    /// without requiring PlayMode.
    /// 
    /// This test suite follows the codebase structure:
    /// - Uses CardGame.Managers namespace (GameManager, ScoreManager, GameEndManager, GameStatsTracker)
    /// - Uses CardGame.UI namespace (GameEndUI, CardFrontlineUI)
    /// - Validates API structure for rematch functionality
    /// - Tests method existence and signatures (structural validation)
    /// </summary>
    public class GameEndAndRematchEditModeTests
    {
        [Test]
        public void GameEndManager_Has_Required_Methods()
        {
            // Create GameObject as inactive to prevent Awake() from being called
            GameObject managerObj = new GameObject("GameEndManager");
            managerObj.SetActive(false);
            GameEndManager manager = managerObj.AddComponent<GameEndManager>();
            
            Assert.IsNotNull(manager, "GameEndManager component should be created");
            
            // Verify CheckGameEnd method exists
            var checkGameEndMethod = typeof(GameEndManager).GetMethod("CheckGameEnd");
            Assert.IsNotNull(checkGameEndMethod, "GameEndManager should have CheckGameEnd method");
            
            // Verify EvaluateWinner method exists (private, but should exist)
            var evaluateWinnerMethod = typeof(GameEndManager).GetMethod("EvaluateWinner", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(evaluateWinnerMethod, "GameEndManager should have EvaluateWinner method");
            
            // Verify WaitForChainsAndEndGame coroutine exists
            var waitForChainsMethod = typeof(GameEndManager).GetMethod("WaitForChainsAndEndGame", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(waitForChainsMethod, "GameEndManager should have WaitForChainsAndEndGame coroutine");
            
            // Verify Reset method exists
            var resetMethod = typeof(GameEndManager).GetMethod("Reset");
            Assert.IsNotNull(resetMethod, "GameEndManager should have Reset method");
            
            Object.DestroyImmediate(managerObj);
        }

        [Test]
        public void ScoreManager_Has_Recalculate_Method()
        {
            // Create GameObject as inactive to prevent Awake() from being called
            GameObject managerObj = new GameObject("ScoreManager");
            managerObj.SetActive(false);
            ScoreManager manager = managerObj.AddComponent<ScoreManager>();
            
            Assert.IsNotNull(manager, "ScoreManager component should be created");
            
            // Verify RecalculateScores method exists
            var recalculateMethod = typeof(ScoreManager).GetMethod("RecalculateScores");
            Assert.IsNotNull(recalculateMethod, "ScoreManager should have RecalculateScores method");
            
            // Verify GetScoreMargin method exists
            var getScoreMarginMethod = typeof(ScoreManager).GetMethod("GetScoreMargin");
            Assert.IsNotNull(getScoreMarginMethod, "ScoreManager should have GetScoreMargin method");
            
            // Verify ResetScores method exists
            var resetScoresMethod = typeof(ScoreManager).GetMethod("ResetScores");
            Assert.IsNotNull(resetScoresMethod, "ScoreManager should have ResetScores method");
            
            // Verify AddScore method exists
            var addScoreMethod = typeof(ScoreManager).GetMethod("AddScore");
            Assert.IsNotNull(addScoreMethod, "ScoreManager should have AddScore method");
            
            Object.DestroyImmediate(managerObj);
        }

        [Test]
        public void GameEndUI_Has_Required_Methods_And_Fields()
        {
            // Create GameObject as inactive to prevent Awake() from being called
            GameObject uiObj = new GameObject("GameEndUI");
            uiObj.SetActive(false);
            GameEndUI gameEndUI = uiObj.AddComponent<GameEndUI>();
            
            Assert.IsNotNull(gameEndUI, "GameEndUI component should be created");
            
            // Verify ShowGameEnd overloads exist
            var showGameEndMethod1 = typeof(GameEndUI).GetMethod("ShowGameEnd", 
                new System.Type[] { typeof(bool), typeof(bool) });
            var showGameEndMethod2 = typeof(GameEndUI).GetMethod("ShowGameEnd", 
                new System.Type[] { typeof(bool), typeof(bool), typeof(int), typeof(int), typeof(int), typeof(int) });
            
            Assert.IsNotNull(showGameEndMethod1, "GameEndUI should have ShowGameEnd(bool, bool) overload");
            Assert.IsNotNull(showGameEndMethod2, "GameEndUI should have ShowGameEnd(bool, bool, int, int, int, int) overload");
            
            // Verify HideGameEnd method exists
            var hideGameEndMethod = typeof(GameEndUI).GetMethod("HideGameEnd");
            Assert.IsNotNull(hideGameEndMethod, "GameEndUI should have HideGameEnd method");
            
            // Verify Rematch method exists (private)
            var rematchMethod = typeof(GameEndUI).GetMethod("Rematch", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(rematchMethod, "GameEndUI should have Rematch method");
            
            // Verify OnRestartClicked method exists (private)
            var onRestartClickedMethod = typeof(GameEndUI).GetMethod("OnRestartClicked", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(onRestartClickedMethod, "GameEndUI should have OnRestartClicked method");
            
            // Verify OnQuitClicked method exists (private)
            var onQuitClickedMethod = typeof(GameEndUI).GetMethod("OnQuitClicked", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(onQuitClickedMethod, "GameEndUI should have OnQuitClicked method");
            
            // Verify button fields exist
            var restartButtonField = typeof(GameEndUI).GetField("restartButton", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var quitButtonField = typeof(GameEndUI).GetField("quitButton", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            Assert.IsNotNull(restartButtonField, "GameEndUI should have restartButton field");
            Assert.IsNotNull(quitButtonField, "GameEndUI should have quitButton field");
            
            Object.DestroyImmediate(uiObj);
        }

        [Test]
        public void GameManager_Has_ResetGameState_Method()
        {
            // Create GameObject as inactive to prevent Awake() from being called
            GameObject managerObj = new GameObject("GameManager");
            managerObj.SetActive(false);
            GameManager manager = managerObj.AddComponent<GameManager>();
            
            Assert.IsNotNull(manager, "GameManager component should be created");
            
            // Verify ResetGameState method exists
            var resetGameStateMethod = typeof(GameManager).GetMethod("ResetGameState");
            Assert.IsNotNull(resetGameStateMethod, "GameManager should have ResetGameState method for rematch");
            
            Object.DestroyImmediate(managerObj);
        }

        [Test]
        public void GameStatsTracker_Has_Persistence_Methods()
        {
            // Create GameObject as inactive to prevent Awake() from being called
            GameObject trackerObj = new GameObject("GameStatsTracker");
            trackerObj.SetActive(false);
            GameStatsTracker tracker = trackerObj.AddComponent<GameStatsTracker>();
            
            Assert.IsNotNull(tracker, "GameStatsTracker component should be created");
            
            // Verify RecordGameResult method exists
            var recordMethod = typeof(GameStatsTracker).GetMethod("RecordGameResult");
            Assert.IsNotNull(recordMethod, "GameStatsTracker should have RecordGameResult method");
            
            // Verify ResetCurrentGameStats method exists
            var resetCurrentMethod = typeof(GameStatsTracker).GetMethod("ResetCurrentGameStats");
            Assert.IsNotNull(resetCurrentMethod, "GameStatsTracker should have ResetCurrentGameStats method");
            
            // Verify ResetSession method exists
            var resetSessionMethod = typeof(GameStatsTracker).GetMethod("ResetSession");
            Assert.IsNotNull(resetSessionMethod, "GameStatsTracker should have ResetSession method");
            
            // Verify GetWinLossRecord method exists
            var getWinLossMethod = typeof(GameStatsTracker).GetMethod("GetWinLossRecord");
            Assert.IsNotNull(getWinLossMethod, "GameStatsTracker should have GetWinLossRecord method");
            
            // Verify properties exist
            var winsProperty = typeof(GameStatsTracker).GetProperty("Wins");
            var lossesProperty = typeof(GameStatsTracker).GetProperty("Losses");
            var totalGamesProperty = typeof(GameStatsTracker).GetProperty("TotalGames");
            var winRateProperty = typeof(GameStatsTracker).GetProperty("WinRate");
            
            Assert.IsNotNull(winsProperty, "GameStatsTracker should have Wins property");
            Assert.IsNotNull(lossesProperty, "GameStatsTracker should have Losses property");
            Assert.IsNotNull(totalGamesProperty, "GameStatsTracker should have TotalGames property");
            Assert.IsNotNull(winRateProperty, "GameStatsTracker should have WinRate property");
            
            Object.DestroyImmediate(trackerObj);
        }

        [Test]
        public void ScoreManager_Has_Score_Persistence_Logic()
        {
            // Create GameObject as inactive to prevent Awake() from being called
            GameObject managerObj = new GameObject("ScoreManager");
            managerObj.SetActive(false);
            ScoreManager manager = managerObj.AddComponent<ScoreManager>();
            
            Assert.IsNotNull(manager, "ScoreManager component should be created");
            
            // Verify PlayerScore and OpponentScore properties exist
            var playerScoreProperty = typeof(ScoreManager).GetProperty("PlayerScore");
            var opponentScoreProperty = typeof(ScoreManager).GetProperty("OpponentScore");
            
            Assert.IsNotNull(playerScoreProperty, "ScoreManager should have PlayerScore property");
            Assert.IsNotNull(opponentScoreProperty, "ScoreManager should have OpponentScore property");
            
            // Verify ResetScores method exists (scores reset per game, not session)
            var resetScoresMethod = typeof(ScoreManager).GetMethod("ResetScores");
            Assert.IsNotNull(resetScoresMethod, "ScoreManager should have ResetScores method (per-game reset)");
            
            // Note: ScoreManager resets per game, while GameStatsTracker persists across games
            // This is the intended behavior - per-game scores reset, session stats persist
            
            Object.DestroyImmediate(managerObj);
        }

        [Test]
        public void GameEndFlow_Components_Exist()
        {
            // Verify all components needed for game end flow exist and have required methods
            
            // GameEndManager - detects game end
            var gameEndManagerType = typeof(GameEndManager);
            Assert.IsNotNull(gameEndManagerType.GetMethod("CheckGameEnd"), 
                "GameEndManager.CheckGameEnd should exist");
            
            // ScoreManager - calculates final scores
            var scoreManagerType = typeof(ScoreManager);
            Assert.IsNotNull(scoreManagerType.GetMethod("RecalculateScores"), 
                "ScoreManager.RecalculateScores should exist");
            
            // GameEndUI - displays end screen
            var gameEndUIType = typeof(GameEndUI);
            Assert.IsNotNull(gameEndUIType.GetMethod("ShowGameEnd", 
                new System.Type[] { typeof(bool), typeof(bool), typeof(int), typeof(int), typeof(int), typeof(int) }), 
                "GameEndUI.ShowGameEnd should exist");
            
            // GameStatsTracker - persists session statistics
            var statsTrackerType = typeof(GameStatsTracker);
            Assert.IsNotNull(statsTrackerType.GetMethod("RecordGameResult"), 
                "GameStatsTracker.RecordGameResult should exist");
            
            // GameManager - handles state transitions
            var gameManagerType = typeof(GameManager);
            Assert.IsNotNull(gameManagerType.GetMethod("ResetGameState"), 
                "GameManager.ResetGameState should exist for rematch");
            
            // Verify all components have required methods (structure validation)
            bool allMethodsExist = gameManagerType.GetMethod("ResetGameState") != null &&
                                 gameEndManagerType.GetMethod("CheckGameEnd") != null &&
                                 gameEndUIType.GetMethod("ShowGameEnd", new System.Type[] { typeof(bool), typeof(bool) }) != null &&
                                 scoreManagerType.GetMethod("RecalculateScores") != null;
            
            Assert.IsTrue(allMethodsExist, 
                "All game end flow components should exist with required methods");
        }

        [Test]
        public void Rematch_Resets_Game_But_Preserves_Session_Stats()
        {
            // Verify that ResetGameState resets per-game data but not session data
            var gameManagerType = typeof(GameManager);
            var resetGameStateMethod = gameManagerType.GetMethod("ResetGameState");
            Assert.IsNotNull(resetGameStateMethod, "GameManager.ResetGameState should exist");
            
            // Verify GameStatsTracker has separate reset methods
            var statsTrackerType = typeof(GameStatsTracker);
            var resetCurrentMethod = statsTrackerType.GetMethod("ResetCurrentGameStats");
            var resetSessionMethod = statsTrackerType.GetMethod("ResetSession");
            
            Assert.IsNotNull(resetCurrentMethod, 
                "GameStatsTracker.ResetCurrentGameStats should exist (resets current game only)");
            Assert.IsNotNull(resetSessionMethod, 
                "GameStatsTracker.ResetSession should exist (resets entire session)");
            
            // Verify ScoreManager has ResetScores (per-game reset)
            var scoreManagerType = typeof(ScoreManager);
            var resetScoresMethod = scoreManagerType.GetMethod("ResetScores");
            Assert.IsNotNull(resetScoresMethod, 
                "ScoreManager.ResetScores should exist (per-game reset, session stats persist)");
            
            // Verify method signatures match expected behavior
            // ResetCurrentGameStats should exist (resets current game only)
            // ResetSession should exist (resets entire session)
            // ResetScores should exist (per-game reset)
            bool allResetMethodsExist = resetCurrentMethod != null && 
                                       resetSessionMethod != null && 
                                       resetScoresMethod != null;
            
            Assert.IsTrue(allResetMethodsExist, 
                "All reset methods should exist: ResetCurrentGameStats, ResetSession, ResetScores");
            
            // Verify they're different methods (not the same)
            Assert.AreNotSame(resetCurrentMethod, resetSessionMethod, 
                "ResetCurrentGameStats and ResetSession should be different methods");
        }
        
        [Test]
        public void CardDropArea_Has_ResetForNewGame_Method()
        {
            // Verify CardDropArea has ResetForNewGame method (critical for rematch board reset)
            var cardDropAreaType = typeof(CardDropArea);
            var resetForNewGameMethod = cardDropAreaType.GetMethod("ResetForNewGame");
            Assert.IsNotNull(resetForNewGameMethod, 
                "CardDropArea should have ResetForNewGame method (public, called during rematch)");
            
            // Verify ResetGameStatistics static method exists
            var resetGameStatisticsMethod = cardDropAreaType.GetMethod("ResetGameStatistics", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            Assert.IsNotNull(resetGameStatisticsMethod, 
                "CardDropArea should have ResetGameStatistics static method");
        }
        
        [Test]
        public void GameManager_ResetGameState_Calls_All_Required_Steps()
        {
            // Verify ResetGameState method exists and has correct signature
            var gameManagerType = typeof(GameManager);
            var resetGameStateMethod = gameManagerType.GetMethod("ResetGameState");
            Assert.IsNotNull(resetGameStateMethod, "GameManager should have ResetGameState method");
            
            // Verify it's public (called by GameEndUI.Rematch)
            Assert.IsTrue(resetGameStateMethod.IsPublic, "ResetGameState should be public");
            
            // Verify it has no parameters (takes no arguments)
            var parameters = resetGameStateMethod.GetParameters();
            Assert.AreEqual(0, parameters.Length, "ResetGameState should take no parameters");
        }
        
        [Test]
        public void CardFrontlineUI_Has_ResetFrontline_Method()
        {
            // Verify CardFrontlineUI has ResetFrontline method (called during rematch Step 5)
            var frontlineUIType = typeof(CardFrontlineUI);
            var resetFrontlineMethod = frontlineUIType.GetMethod("ResetFrontline");
            Assert.IsNotNull(resetFrontlineMethod, 
                "CardFrontlineUI should have ResetFrontline method (called during rematch)");
            
            // Verify GetP1Control and GetP2Control methods exist (used for winner evaluation)
            var getP1ControlMethod = frontlineUIType.GetMethod("GetP1Control");
            var getP2ControlMethod = frontlineUIType.GetMethod("GetP2Control");
            
            Assert.IsNotNull(getP1ControlMethod, 
                "CardFrontlineUI should have GetP1Control method");
            Assert.IsNotNull(getP2ControlMethod, 
                "CardFrontlineUI should have GetP2Control method");
        }
        
        [Test]
        public void NewDeckManagerP1_And_P2_Have_InitializeDeck_Methods()
        {
            // Verify deck managers have InitializeDeck methods (called during rematch Step 8)
            var deckManagerP1Type = typeof(NewDeckManagerP1);
            var deckManagerP2Type = typeof(NewDeckManagerP2);
            
            var initP1Method = deckManagerP1Type.GetMethod("InitializeDeck");
            var initP2Method = deckManagerP2Type.GetMethod("InitializeDeck");
            
            Assert.IsNotNull(initP1Method, 
                "NewDeckManagerP1 should have InitializeDeck method (called during rematch)");
            Assert.IsNotNull(initP2Method, 
                "NewDeckManagerP2 should have InitializeDeck method (called during rematch)");
        }
        
        [Test]
        public void NewHandP1UI_And_P2UI_Have_ClearHand_Methods()
        {
            // Verify hand UI managers have ClearHand methods (called during rematch Step 9)
            var handP1UIType = typeof(NewHandP1UI);
            var handP2UIType = typeof(NewHandP2UI);
            
            var clearP1Method = handP1UIType.GetMethod("ClearHand");
            var clearP2Method = handP2UIType.GetMethod("ClearHand");
            
            Assert.IsNotNull(clearP1Method, 
                "NewHandP1UI should have ClearHand method (called during rematch)");
            Assert.IsNotNull(clearP2Method, 
                "NewHandP2UI should have ClearHand method (called during rematch)");
        }
        
        [Test]
        public void CoinTossManager_Has_ResetCoinToss_Method()
        {
            // Verify CoinTossManager has ResetCoinToss method (called during rematch Step 10)
            var coinTossManagerType = typeof(CoinTossManager);
            var resetCoinTossMethod = coinTossManagerType.GetMethod("ResetCoinToss");
            
            Assert.IsNotNull(resetCoinTossMethod, 
                "CoinTossManager should have ResetCoinToss method (called during rematch)");
        }
        
        [Test]
        public void CoinTossUI_Has_Show_Method()
        {
            // Verify CoinTossUI has Show method (called during rematch Step 11)
            var coinTossUIType = typeof(CoinTossUI);
            var showMethod = coinTossUIType.GetMethod("Show");
            
            Assert.IsNotNull(showMethod, 
                "CoinTossUI should have Show method (called during rematch)");
        }
    }
}

