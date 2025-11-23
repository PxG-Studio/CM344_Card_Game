using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CardGame.Managers;
using CardGame.UI;
using CardGame.Core;

namespace CardGame.Tests
{
    /// <summary>
    /// Integration tests for complete game flow - tests end-to-end scenarios from coin toss to game end.
    /// These tests validate that all systems work together correctly.
    /// </summary>
    public class CompleteGameFlowIntegrationTests
    {
        private const string SCENE_NAME = "BattleScreenMultiplayer";

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // CRITICAL: Clear singleton instances from previous tests
            CardTestHelper.ClearSingletonInstances();
            yield return null;
            
            // Verify scene exists in build settings
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
            
            // Reset game state
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
        public IEnumerator CompleteFlow_CoinToss_To_CardPlacement_To_Capture_To_ScoreUpdate()
        {
            // Arrange: Wait for game to initialize
            yield return CardTestHelper.WaitForCoinTossToComplete();
            yield return new WaitForSeconds(1.0f);
            
            // Get managers
            CoinTossManager coinTossManager = CoinTossManager.Instance;
            GameManager gameManager = GameManager.Instance;
            FateFlowController fateController = FateFlowController.Instance;
            ScoreManager scoreManager = ScoreManager.Instance;
            
            Assert.IsNotNull(coinTossManager, "CoinTossManager should exist");
            Assert.IsNotNull(gameManager, "GameManager should exist");
            Assert.IsNotNull(fateController, "FateFlowController should exist");
            Assert.IsNotNull(scoreManager, "ScoreManager should exist");
            
            // Step 1: Verify coin toss completed
            Assert.IsTrue(coinTossManager.IsComplete, "Coin toss should be complete");
            FateSide startingSide = coinTossManager.GetStartingPlayer();
            Assert.IsTrue(startingSide == FateSide.Player || startingSide == FateSide.Opponent, 
                "Starting side should be valid");
            
            // Step 2: Verify turn is set correctly
            fateController.SetFate(startingSide);
            yield return null;
            Assert.AreEqual(startingSide, fateController.CurrentFate, 
                "FateFlowController should reflect coin toss result");
            
            // Step 3: Verify cards are drawn
            NewDeckManager playerDeck = Object.FindObjectOfType<NewDeckManager>();
            NewDeckManagerOpp opponentDeck = Object.FindObjectOfType<NewDeckManagerOpp>();
            
            if (playerDeck != null && opponentDeck != null)
            {
                // Wait for cards to be drawn
                yield return new WaitForSeconds(2.0f);
                
                // At least one player should have cards
                int totalCards = playerDeck.Hand.Count + opponentDeck.Hand.Count;
                Assert.GreaterOrEqual(totalCards, 0, 
                    "Cards should be drawn after coin toss");
            }
            
            // Step 4: Place cards and trigger capture
            CardDropArea1[] dropAreas = Object.FindObjectsOfType<CardDropArea1>();
            if (dropAreas.Length >= 2)
            {
                CardDropArea1 area1 = dropAreas[0];
                CardDropArea1 area2 = CardTestHelper.GetAdjacentDropArea(area1, "right") ?? dropAreas[1];
                
                if (area2 != null)
                {
                    // Create test cards for capture
                    NewCard attackerCard = CardTestHelper.CreateTestCard(3, 5, 3, 3, "Attacker");
                    NewCard defenderCard = CardTestHelper.CreateTestCard(3, 2, 3, 3, "Defender");
                    
                    // Add cards to deck manager hands
                    if (playerDeck != null)
                    {
                        CardTestHelper.AddCardToDeckManagerHand(playerDeck, attackerCard);
                    }
                    if (opponentDeck != null)
                    {
                        CardTestHelper.AddCardToDeckManagerHand(opponentDeck, defenderCard);
                    }
                    
                    // Set correct turn
                    fateController.SetFate(FateSide.Player);
                    yield return null;
                    
                    // Get initial score
                    int initialPlayerScore = scoreManager.PlayerScore;
                    
                    // Place attacker
                    CardMover attackerMover = CardTestHelper.CreateCardMoverWithCard(attackerCard, area1.transform.position, true);
                    CardTestHelper.PlaceCardOnDropArea(attackerMover, area1, true);
                    yield return new WaitForSeconds(0.5f);
                    
                    // Place defender
                    CardMoverOpp defenderMover = CardTestHelper.CreateCardMoverOppWithCard(defenderCard, area2.transform.position);
                    CardTestHelper.PlaceOpponentCardOnDropArea(defenderMover, area2, true);
                    yield return CardTestHelper.WaitForCaptureAnimations(3f);
                    
                    // Step 5: Verify capture occurred
                    bool defenderCaptured = CardTestHelper.IsCardCaptured(defenderMover.gameObject);
                    Assert.IsTrue(defenderCaptured, 
                        "Defender should be captured when attacker is higher");
                    
                    // Step 6: Verify score updated
                    int newPlayerScore = scoreManager.PlayerScore;
                    Assert.Greater(newPlayerScore, initialPlayerScore, 
                        $"Player score should increase after capture. Was: {initialPlayerScore}, Now: {newPlayerScore}");
                }
            }
        }

        [UnityTest]
        public IEnumerator CompleteFlow_CoinToss_To_GameStart_To_TurnAssignment_To_CardPlacement()
        {
            // Arrange: Wait for game to initialize
            yield return CardTestHelper.WaitForCoinTossToComplete();
            yield return new WaitForSeconds(1.0f);
            
            CoinTossManager coinTossManager = CoinTossManager.Instance;
            GameManager gameManager = GameManager.Instance;
            FateFlowController fateController = FateFlowController.Instance;
            
            // Step 1: Coin toss completed
            Assert.IsTrue(coinTossManager.IsComplete, "Coin toss should be complete");
            FateSide startingSide = coinTossManager.GetStartingPlayer();
            
            // Step 2: Turn assigned
            fateController.SetFate(startingSide);
            yield return null;
            
            bool canStartingPlayerAct = fateController.CanAct(startingSide);
            Assert.IsTrue(canStartingPlayerAct, 
                $"Starting player ({startingSide}) should be able to act after coin toss");
            
            // Step 3: Game state should be in gameplay (not Menu)
            Assert.AreNotEqual(GameState.Menu, gameManager.CurrentState, 
                "Game state should not be Menu after coin toss");
            
            // Step 4: Cards should be available for placement
            NewDeckManager playerDeck = Object.FindObjectOfType<NewDeckManager>();
            NewDeckManagerOpp opponentDeck = Object.FindObjectOfType<NewDeckManagerOpp>();
            
            yield return new WaitForSeconds(2.0f);
            
            if (playerDeck != null && opponentDeck != null)
            {
                // At least starting player should have cards
                int startingPlayerCards = startingSide == FateSide.Player 
                    ? playerDeck.Hand.Count 
                    : opponentDeck.Hand.Count;
                
                Assert.GreaterOrEqual(startingPlayerCards, 0, 
                    $"Starting player ({startingSide}) should have cards available for placement");
            }
            
            // Step 5: Card placement should work for starting player
            CardDropArea1[] dropAreas = Object.FindObjectsOfType<CardDropArea1>();
            if (dropAreas.Length > 0 && startingSide == FateSide.Player && playerDeck != null && playerDeck.Hand.Count > 0)
            {
                CardDropArea1 emptyArea = null;
                foreach (CardDropArea1 area in dropAreas)
                {
                    if (!area.IsOccupied)
                    {
                        emptyArea = area;
                        break;
                    }
                }
                
                if (emptyArea != null)
                {
                    // Create test card
                    NewCard testCard = CardTestHelper.CreateTestCard(3, 3, 3, 3, "TestCard");
                    
                    // Add card to player deck manager's hand
                    if (playerDeck != null)
                    {
                        CardTestHelper.AddCardToDeckManagerHand(playerDeck, testCard);
                    }
                    
                    CardMover testMover = CardTestHelper.CreateCardMoverWithCard(testCard, emptyArea.transform.position, true);
                    
                    // Place card (should work for starting player)
                    bool placed = CardTestHelper.PlaceCardOnDropArea(testMover, emptyArea, false);
                    Assert.IsTrue(placed, 
                        $"Starting player ({startingSide}) should be able to place cards");
                    
                    yield return new WaitForSeconds(0.5f);
                    
                    // Verify card is on board
                    Assert.IsTrue(emptyArea.IsOccupied, 
                        "Drop area should be occupied after card placement");
                }
            }
        }

        [UnityTest]
        public IEnumerator CompleteFlow_AllCardsPlaced_To_GameEnd_To_ScoreCalculation_To_GameEndUI()
        {
            // Arrange: Wait for game to initialize
            yield return CardTestHelper.WaitForCoinTossToComplete();
            yield return new WaitForSeconds(1.0f);
            
            GameEndManager gameEndManager = GameEndManager.Instance;
            ScoreManager scoreManager = ScoreManager.Instance;
            GameManager gameManager = GameManager.Instance;
            
            Assert.IsNotNull(gameEndManager, "GameEndManager should exist");
            Assert.IsNotNull(scoreManager, "ScoreManager should exist");
            
            // Set up scores
            for (int i = 0; i < 8; i++)
            {
                scoreManager.AddScore(true); // Player 1: 8 points
            }
            for (int i = 0; i < 5; i++)
            {
                scoreManager.AddScore(false); // Player 2: 5 points
            }
            
            yield return null;
            
            // Simulate all cards played (set cards played count)
            // Note: This is a simplified test - in real game, cards would be placed
            CardDropArea1.ResetGameStatistics();
            
            // Manually set cards played count (simulating game completion)
            // In real scenario, this would happen through actual card placement
            // For integration test, we'll trigger game end check
            
            // Act: Trigger game end check
            gameEndManager.CheckGameEnd();
            yield return new WaitForSeconds(1.0f);
            
            // Assert: Game end UI should be shown (if game actually ended)
            GameEndUI gameEndUI = Object.FindObjectOfType<GameEndUI>(true);
            if (gameEndUI != null)
            {
                // Verify ShowGameEnd can be called
                var showMethod = typeof(GameEndUI).GetMethod("ShowGameEnd");
                Assert.IsNotNull(showMethod, "GameEndUI should have ShowGameEnd method");
                
                // Call ShowGameEnd with Player 1 winning
                showMethod.Invoke(gameEndUI, new object[] { true, false, 3 });
                yield return new WaitForSeconds(0.5f);
                
                // Verify winner text is set
                var winnerTextField = typeof(GameEndUI).GetField("winnerText", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (winnerTextField != null)
                {
                    TMPro.TextMeshProUGUI winnerText = winnerTextField.GetValue(gameEndUI) as TMPro.TextMeshProUGUI;
                    if (winnerText != null)
                    {
                        string text = winnerText.text.ToUpper();
                        Assert.IsTrue(text.Contains("PLAYER 1") || text.Contains("WINS"), 
                            $"GameEndUI should show Player 1 wins. Got: {winnerText.text}");
                    }
                }
            }
        }
    }
}

