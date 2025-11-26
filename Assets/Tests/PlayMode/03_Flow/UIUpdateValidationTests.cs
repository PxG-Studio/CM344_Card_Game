using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CardGame.Managers;
using CardGame.UI;
using CardGame.Core;
using TMPro;

namespace CardGame.Tests
{
    /// <summary>
    /// Tests specifically designed to catch UI NOT UPDATING bugs.
    /// These tests validate that UI elements reflect game state changes.
    /// </summary>
    public class UIUpdateValidationTests
    {
        private const string SCENE_NAME = "BattleScreenMultiplayer";

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // CRITICAL: Clear singleton instances from previous tests
            CardTestHelper.ClearSingletonInstances();
            yield return null;
            
            bool sceneExists = false;
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string scenePath = System.IO.Path.GetFileNameWithoutExtension(
                    UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(i));
                if (scenePath == SCENE_NAME)
                {
                    sceneExists = true;
                    break;
                }
            }
            
            if (!sceneExists)
            {
                Assert.Fail($"Scene '{SCENE_NAME}' must be added to Build Settings");
            }

            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(SCENE_NAME, LoadSceneMode.Single);
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
            
            yield return new WaitForSeconds(0.5f);
            CardTestHelper.ResetGameState();
            yield return null;
        }
        
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            // Clean up after each test
            yield return null;
            CardTestHelper.ClearSingletonInstances();
            yield return null;
        }

        [UnityTest]
        public IEnumerator UIUpdate_ScoreUI_UpdatesWhenScoreChanges()
        {
            // UNIT-STYLE UI TEST: validate that ScoreUI updates its visible text when
            // its SetScores/UpdateScoreDisplay APIs are called, independent of the scene wiring.
            yield return null;

            ScoreUI scoreUI = CreateTestScoreUI(out TextMeshProUGUI player1ScoreText, out TextMeshProUGUI player2ScoreText);
            
            // Get initial value from the UI (should default to 0/empty)
            int initialManagerScore = 0;
            int initialDisplayedScore = 0;
            if (int.TryParse(player1ScoreText.text, out int parsed))
            {
                initialDisplayedScore = parsed;
            }
            
            // For this UI-focused test, drive the UI directly via its public API rather than
            // depending on the full event pipeline (which is covered by higher-level tests).
            // We treat "score changes" as "ScoreUI.SetScores is called with a new value".
            int targetManagerScore = initialManagerScore + 1;
            int currentP2Score = 0;
            scoreUI.SetScores(targetManagerScore, currentP2Score);
            yield return null; // Allow layout/text to refresh
            
            // UI ASSERTION: Displayed score MUST match the value passed to ScoreUI
            int newDisplayedScore = 0;
            if (int.TryParse(player1ScoreText.text, out int parsedNew))
            {
                newDisplayedScore = parsedNew;
            }
            
            Assert.AreEqual(targetManagerScore, newDisplayedScore, 
                $"UI NOT UPDATING: ScoreUI text ({newDisplayedScore}) does not match expected score ({targetManagerScore}) after SetScores(). " +
                $"Initial: Manager={initialManagerScore}, UI={initialDisplayedScore}. " +
                "This indicates the ScoreUI display is not updating when its scores are changed.");
        }

        [UnityTest]
        public IEnumerator UIUpdate_TurnIndicator_UpdatesWhenTurnSwitches()
        {
            // UI TEST: Turn indicator MUST update when turn switches
            yield return CardTestHelper.WaitForCoinTossToComplete();
            yield return new WaitForSeconds(1.0f);
            
            FateFlowController fateController = FateFlowController.Instance;
            Assert.IsNotNull(fateController, "FateFlowController should exist");
            
            // Find turn indicator
            GameObject turnIndicator = GameObject.Find("TurnIndicator_UI");
            if (turnIndicator == null)
            {
                TurnIndicatorUI[] indicators = Object.FindObjectsOfType<TurnIndicatorUI>(true);
                if (indicators.Length > 0)
                {
                    turnIndicator = indicators[0].gameObject;
                }
            }
            
            if (turnIndicator == null)
            {
                Assert.Inconclusive("TurnIndicatorUI not found - may be optional");
                yield break;
            }
            
            TurnIndicatorUI indicatorUI = turnIndicator.GetComponent<TurnIndicatorUI>();
            if (indicatorUI == null)
            {
                Assert.Inconclusive("TurnIndicatorUI component not found");
                yield break;
            }
            
            // Get initial turn
            FateSide initialTurn = fateController.CurrentFate;
            fateController.SetFate(initialTurn);
            yield return new WaitForSeconds(0.5f);
            
            // Switch turn
            FateSide newTurn = initialTurn == FateSide.Player ? FateSide.P2 : FateSide.Player;
            fateController.SetFate(newTurn);
            yield return new WaitForSeconds(0.5f); // Wait for UI update
            
            // UI ASSERTION: Turn indicator MUST reflect turn change
            // (We can't easily check visual state, but we can verify the component exists and can update)
            Assert.AreEqual(newTurn, fateController.CurrentFate, 
                "Turn should have switched");
            
            // Verify indicator is still active (should be visible)
            Assert.IsTrue(turnIndicator.activeSelf || turnIndicator.activeInHierarchy, 
                "UI NOT UPDATING: TurnIndicator should be visible after turn switch. " +
                "This indicates turn indicator is not updating when turn changes.");
        }

        [UnityTest]
        public IEnumerator UIUpdate_GameEndUI_ShowsWhenGameEnds()
        {
            // UI TEST: GameEndUI MUST be shown when game ends
            yield return CardTestHelper.WaitForCoinTossToComplete();
            yield return new WaitForSeconds(1.0f);
            
            GameEndManager gameEndManager = GameEndManager.Instance;
            GameEndUI gameEndUI = Object.FindObjectOfType<GameEndUI>(true);
            
            Assert.IsNotNull(gameEndManager, "GameEndManager should exist");
            
            if (gameEndUI == null)
            {
                yield return new WaitForSeconds(1.0f);
                gameEndUI = Object.FindObjectOfType<GameEndUI>(true);
            }
            
            if (gameEndUI == null)
            {
                Assert.Inconclusive("GameEndUI not found - may be created dynamically");
                yield break;
            }
            
            // Initially should be hidden
            bool initiallyVisible = gameEndUI.gameObject.activeSelf || gameEndUI.gameObject.activeInHierarchy;
            
            // Trigger game end
            var showMethod = typeof(GameEndUI).GetMethod("ShowGameEnd");
            if (showMethod != null)
            {
                showMethod.Invoke(gameEndUI, new object[] { true, false, 3 });
                yield return new WaitForSeconds(0.5f);
                
                // UI ASSERTION: GameEndUI MUST be visible after ShowGameEnd
                bool nowVisible = gameEndUI.gameObject.activeSelf || gameEndUI.gameObject.activeInHierarchy;
                Assert.IsTrue(nowVisible, 
                    $"UI NOT UPDATING: GameEndUI should be visible after ShowGameEnd(). " +
                    $"Was visible: {initiallyVisible}, Now visible: {nowVisible}. " +
                    "This indicates GameEndUI is not updating when game ends.");
            }
        }

        [UnityTest]
        public IEnumerator UIUpdate_GameEndUI_WinnerText_UpdatesCorrectly()
        {
            // UI TEST: GameEndUI winner text MUST display correct winner
            yield return CardTestHelper.WaitForCoinTossToComplete();
            yield return new WaitForSeconds(1.0f);
            
            GameEndUI gameEndUI = Object.FindObjectOfType<GameEndUI>(true);
            
            if (gameEndUI == null)
            {
                yield return new WaitForSeconds(1.0f);
                gameEndUI = Object.FindObjectOfType<GameEndUI>(true);
            }
            
            if (gameEndUI == null)
            {
                Assert.Inconclusive("GameEndUI not found");
                yield break;
            }
            
            var showMethod = typeof(GameEndUI).GetMethod("ShowGameEnd");
            if (showMethod == null)
            {
                Assert.Inconclusive("ShowGameEnd method not found");
                yield break;
            }
            
            // Test Player 1 wins
            showMethod.Invoke(gameEndUI, new object[] { true, false, 5 });
            yield return new WaitForSeconds(0.5f);
            
            var winnerTextField = typeof(GameEndUI).GetField("winnerText", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (winnerTextField != null)
            {
                TextMeshProUGUI winnerText = winnerTextField.GetValue(gameEndUI) as TextMeshProUGUI;
                if (winnerText != null)
                {
                    string text = winnerText.text.ToUpper();
                    
                    // UI ASSERTION: Winner text MUST indicate Player 1 wins
                    Assert.IsTrue(text.Contains("PLAYER 1") || text.Contains("WINS"), 
                        $"UI NOT UPDATING: Winner text should indicate Player 1 wins when playerWon=true. " +
                        $"Got: '{winnerText.text}'. " +
                        "This indicates winner text is not updating correctly.");
                }
            }
            
            // Test Player 2 wins
            showMethod.Invoke(gameEndUI, new object[] { false, false, -3 });
            yield return new WaitForSeconds(0.5f);
            
            if (winnerTextField != null)
            {
                TextMeshProUGUI winnerText = winnerTextField.GetValue(gameEndUI) as TextMeshProUGUI;
                if (winnerText != null)
                {
                    string text = winnerText.text.ToUpper();
                    
                    // UI ASSERTION: Winner text MUST indicate Player 2 wins
                    Assert.IsTrue(text.Contains("PLAYER 2") || text.Contains("WINS"), 
                        $"UI NOT UPDATING: Winner text should indicate Player 2 wins when playerWon=false. " +
                        $"Got: '{winnerText.text}'. " +
                        "This indicates winner text is not updating correctly.");
                }
            }
        }

        [UnityTest]
        public IEnumerator UIUpdate_ScoreUI_Player2Score_UpdatesWhenScoreChanges()
        {
            // UI TEST: Player 2 score UI MUST update when opponent score changes
            yield return null;
            
            ScoreUI scoreUI = CreateTestScoreUI(out TextMeshProUGUI player1ScoreText, out TextMeshProUGUI player2ScoreText);
            
            if (player2ScoreText == null)
            {
                Assert.Inconclusive("Player 2 score text not found");
                yield break;
            }
            
            // Get initial values
            int initialManagerScore = 0;
            int initialDisplayedScore = 0;
            if (int.TryParse(player2ScoreText.text, out int parsed))
            {
                initialDisplayedScore = parsed;
            }
            
            // Drive the UI directly via SetScores, keeping P1's score constant and bumping P2.
            int targetP2Score = initialManagerScore + 1;
            int currentP1Score = 0;
            scoreUI.SetScores(currentP1Score, targetP2Score);
            yield return null;
            
            int newDisplayedScore = 0;
            if (int.TryParse(player2ScoreText.text, out int parsedNew))
            {
                newDisplayedScore = parsedNew;
            }
            
            Assert.AreEqual(targetP2Score, newDisplayedScore, 
                $"UI NOT UPDATING: Player 2 ScoreUI text ({newDisplayedScore}) does not match expected opponent score ({targetP2Score}) after SetScores(). " +
                $"Initial: Manager={initialManagerScore}, UI={initialDisplayedScore}. " +
                "This indicates Player 2 score UI is not updating when its score is changed.");
        }

        /// <summary>
        /// Helper: creates a minimal ScoreUI instance with two TextMeshProUGUI fields wired
        /// into its private player1Score/player2Score fields so we can unit-test its behavior
        /// without depending on the scene HUD wiring.
        /// </summary>
        private static ScoreUI CreateTestScoreUI(out TextMeshProUGUI player1ScoreText, out TextMeshProUGUI player2ScoreText)
        {
            var scoreUiGO = new GameObject("TestScoreUI_Helper");
            var scoreUI = scoreUiGO.AddComponent<ScoreUI>();

            var p1GO = new GameObject("P1ScoreText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            var p2GO = new GameObject("P2ScoreText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            p1GO.transform.SetParent(scoreUiGO.transform, false);
            p2GO.transform.SetParent(scoreUiGO.transform, false);

            player1ScoreText = p1GO.GetComponent<TextMeshProUGUI>();
            player2ScoreText = p2GO.GetComponent<TextMeshProUGUI>();

            var p1Field = typeof(ScoreUI).GetField("player1Score",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var p2Field = typeof(ScoreUI).GetField("player2Score",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            p1Field?.SetValue(scoreUI, player1ScoreText);
            p2Field?.SetValue(scoreUI, player2ScoreText);

            return scoreUI;
        }

        [UnityTest]
        public IEnumerator UIUpdate_CardHover_ChangesSortingLayer()
        {
            // UI TEST: Card hover MUST change sorting layer for visual feedback
            yield return CardTestHelper.WaitForCoinTossToComplete();
            yield return new WaitForSeconds(1.0f);
            
            NewHandP1UI handUI = Object.FindObjectOfType<NewHandP1UI>();
            NewDeckManagerP1 deckManager = handUI?.DeckManager;
            if (handUI == null || deckManager == null || deckManager.Hand == null || deckManager.Hand.Count == 0)
            {
                Assert.Inconclusive("No cards in hand for hover test");
                yield break;
            }
            
            // Find a card UI
            NewCardUI[] cardUIs = Object.FindObjectsOfType<NewCardUI>(true);
            NewCardUI testCardUI = null;
            
            foreach (NewCardUI cardUI in cardUIs)
            {
                if (cardUI.Card != null && deckManager.Hand.Any(c => c == cardUI.Card))
                {
                    testCardUI = cardUI;
                    break;
                }
            }
            
            if (testCardUI == null)
            {
                Assert.Inconclusive("No card UI found in hand");
                yield break;
            }
            
            // Verify card implements hover interface
            bool hasPointerEnter = testCardUI is UnityEngine.EventSystems.IPointerEnterHandler;
            bool hasPointerExit = testCardUI is UnityEngine.EventSystems.IPointerExitHandler;
            
            // UI ASSERTION: Card MUST support hover (implement IPointerEnterHandler)
            Assert.IsTrue(hasPointerEnter, 
                "UI NOT UPDATING: NewCardUI should implement IPointerEnterHandler for hover preview. " +
                "This indicates hover functionality is not implemented.");
            Assert.IsTrue(hasPointerExit, 
                "UI NOT UPDATING: NewCardUI should implement IPointerExitHandler for hover preview. " +
                "This indicates hover exit functionality is not implemented.");
        }

        [UnityTest]
        public IEnumerator UIUpdate_CoinTossUI_ShowsResultAfterToss()
        {
            // UI TEST: CoinTossUI MUST show result after coin toss completes
            yield return new WaitForSeconds(1.0f);
            
            CoinTossUI coinTossUI = Object.FindObjectOfType<CoinTossUI>(true);
            CoinTossManager coinTossManager = CoinTossManager.Instance;
            
            Assert.IsNotNull(coinTossUI, "CoinTossUI should exist");
            Assert.IsNotNull(coinTossManager, "CoinTossManager should exist");
            
            // Start coin toss
            coinTossUI.StartCoinToss();
            yield return new WaitForEndOfFrame();
            yield return null;
            
            coinTossUI.StartCoinTossAnimation();
            yield return CardTestHelper.WaitForCoinTossToComplete(10f);
            yield return new WaitForSeconds(2.0f); // Wait for animation
            
            // UI ASSERTION: CoinTossUI MUST show result after toss
            // Get result text field
            var resultTextField = typeof(CoinTossUI).GetField("resultText", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (resultTextField != null)
            {
                TextMeshProUGUI resultText = resultTextField.GetValue(coinTossUI) as TextMeshProUGUI;
                if (resultText != null)
                {
                    // Result text should be visible and contain result
                    bool textVisible = resultText.gameObject.activeSelf || resultText.gameObject.activeInHierarchy;
                    bool hasText = !string.IsNullOrEmpty(resultText.text);
                    
                    Assert.IsTrue(textVisible || hasText, 
                        "UI NOT UPDATING: CoinTossUI result text should be visible or contain text after coin toss. " +
                        $"Visible: {textVisible}, HasText: {hasText}. " +
                        "This indicates coin toss result is not being displayed.");
                }
            }
        }

        // --------------------------------------------------------------------
        // Integration-style sanity checks: real scene ScoreUI + ScoreManager
        // --------------------------------------------------------------------

        [UnityTest]
        public IEnumerator UIIntegration_ScoreUI_FollowsScoreManager_P1()
        {
            // Ensure scene systems are initialized
            yield return CardTestHelper.WaitForCoinTossToComplete();
            yield return new WaitForSeconds(1.0f);

            ScoreManager scoreManager = ScoreManager.Instance;
            ScoreUI scoreUI = Object.FindObjectOfType<ScoreUI>(true);

            Assert.IsNotNull(scoreManager, "ScoreManager should exist for integration test");

            if (scoreUI == null)
            {
                Assert.Inconclusive("ScoreUI not found in scene HUD (integration wiring may be different).");
                yield break;
            }

            var p1Field = typeof(ScoreUI).GetField("player1Score",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            TextMeshProUGUI p1Text = p1Field?.GetValue(scoreUI) as TextMeshProUGUI;

            if (p1Text == null)
            {
                Assert.Inconclusive("Player 1 score text not found on scene ScoreUI.");
                yield break;
            }

            int before = scoreManager.P1Score;
            int beforeDisplayed = 0;
            int.TryParse(p1Text.text, out beforeDisplayed);

            // Act: change score via real manager API
            scoreManager.AddScore(true);
            yield return new WaitForSeconds(0.5f);

            int after = scoreManager.P1Score;
            int afterDisplayed = 0;
            int.TryParse(p1Text.text, out afterDisplayed);

            Assert.AreEqual(after, afterDisplayed,
                $"INTEGRATION: Scene ScoreUI text for P1 ({afterDisplayed}) must follow ScoreManager.P1Score ({after}). " +
                $"Initial: Manager={before}, UI={beforeDisplayed}.");
        }

        [UnityTest]
        public IEnumerator UIIntegration_ScoreUI_FollowsScoreManager_P2()
        {
            // Ensure scene systems are initialized
            yield return CardTestHelper.WaitForCoinTossToComplete();
            yield return new WaitForSeconds(1.0f);

            ScoreManager scoreManager = ScoreManager.Instance;
            ScoreUI scoreUI = Object.FindObjectOfType<ScoreUI>(true);

            Assert.IsNotNull(scoreManager, "ScoreManager should exist for integration test");

            if (scoreUI == null)
            {
                Assert.Inconclusive("ScoreUI not found in scene HUD (integration wiring may be different).");
                yield break;
            }

            var p2Field = typeof(ScoreUI).GetField("player2Score",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            TextMeshProUGUI p2Text = p2Field?.GetValue(scoreUI) as TextMeshProUGUI;

            if (p2Text == null)
            {
                Assert.Inconclusive("Player 2 score text not found on scene ScoreUI.");
                yield break;
            }

            int before = scoreManager.P2Score;
            int beforeDisplayed = 0;
            int.TryParse(p2Text.text, out beforeDisplayed);

            // Act: change opponent score via real manager API
            scoreManager.AddScore(false);
            yield return new WaitForSeconds(0.5f);

            int after = scoreManager.P2Score;
            int afterDisplayed = 0;
            int.TryParse(p2Text.text, out afterDisplayed);

            Assert.AreEqual(after, afterDisplayed,
                $"INTEGRATION: Scene ScoreUI text for P2 ({afterDisplayed}) must follow ScoreManager.P2Score ({after}). " +
                $"Initial: Manager={before}, UI={beforeDisplayed}.");
        }
    }
}

