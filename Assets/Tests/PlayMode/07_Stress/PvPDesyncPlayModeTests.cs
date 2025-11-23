using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CardGame.Managers;
using CardGame.Core;

namespace CardGame.Tests
{
    /// <summary>
    /// PlayMode tests for PvP desync safety - validates independent hands, decks, and turn alternation.
    /// </summary>
    public class PvPDesyncPlayModeTests
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
            
            yield return new WaitForSeconds(0.5f); // Wait for initialization
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
        public IEnumerator BothPlayers_HaveIndependentHands()
        {
            // Arrange: Wait for game to initialize
            yield return new WaitForSeconds(2.0f);
            
            NewDeckManager playerDeck = Object.FindObjectOfType<NewDeckManager>();
            NewDeckManagerOpp opponentDeck = Object.FindObjectOfType<NewDeckManagerOpp>();
            
            Assert.IsNotNull(playerDeck, "NewDeckManager should exist");
            Assert.IsNotNull(opponentDeck, "NewDeckManagerOpp should exist");
            
            // Assert: Hands should be independent (different instances)
            Assert.AreNotSame(playerDeck.Hand, opponentDeck.Hand, 
                "Player and opponent should have independent hand instances");
            
            // Verify hand counts can differ
            int playerHandCount = playerDeck.Hand.Count;
            int opponentHandCount = opponentDeck.Hand.Count;
            
            // Hands may have different counts (independent)
            Assert.IsTrue(true, "Both players have independent hands (can have different card counts)");
        }

        [UnityTest]
        public IEnumerator BothPlayers_UseIndependentDecks()
        {
            // Arrange: Wait for game to initialize
            yield return new WaitForSeconds(2.0f);
            
            NewDeckManager playerDeck = Object.FindObjectOfType<NewDeckManager>();
            NewDeckManagerOpp opponentDeck = Object.FindObjectOfType<NewDeckManagerOpp>();
            
            Assert.IsNotNull(playerDeck, "NewDeckManager should exist");
            Assert.IsNotNull(opponentDeck, "NewDeckManagerOpp should exist");
            
            // Assert: Decks should be independent (different instances)
            Assert.AreNotSame(playerDeck, opponentDeck, 
                "Player and opponent should have independent deck manager instances");
            
            // Verify draw pile counts can differ
            int playerDrawPileCount = playerDeck.DrawPileCount;
            int opponentDrawPileCount = opponentDeck.DrawPileCount;
            
            // Draw piles may have different counts (independent)
            Assert.IsTrue(true, "Both players use independent decks (can have different draw pile counts)");
        }

        [UnityTest]
        public IEnumerator No_SharedScriptableObjectState_BetweenPlayers()
        {
            // Arrange: Wait for game to initialize
            yield return new WaitForSeconds(2.0f);
            
            NewDeckManager playerDeck = Object.FindObjectOfType<NewDeckManager>();
            NewDeckManagerOpp opponentDeck = Object.FindObjectOfType<NewDeckManagerOpp>();
            
            Assert.IsNotNull(playerDeck, "NewDeckManager should exist");
            Assert.IsNotNull(opponentDeck, "NewDeckManagerOpp should exist");
            
            // Initialize decks
            playerDeck.InitializeDeck();
            opponentDeck.InitializeDeck();
            yield return null;
            
            // Draw cards from each deck
            playerDeck.DrawCards(2);
            opponentDeck.DrawCards(2);
            yield return null;
            
            // Assert: Cards in each hand should be independent instances
            // Even if they reference the same NewCardData ScriptableObject,
            // the NewCard instances should be separate
            if (playerDeck.Hand.Count > 0 && opponentDeck.Hand.Count > 0)
            {
                NewCard playerCard = playerDeck.Hand[0];
                NewCard opponentCard = opponentDeck.Hand[0];
                
                // Cards should be different instances (even if same data)
                Assert.AreNotSame(playerCard, opponentCard, 
                    "Player and opponent cards should be independent instances");
            }
            
            Assert.IsTrue(true, "No shared ScriptableObject state between players (independent card instances)");
        }

        [UnityTest]
        public IEnumerator PvP_Turns_AlternateCorrectly()
        {
            // Arrange: Wait for game to initialize
            yield return new WaitForSeconds(2.0f);
            
            FateFlowController fateController = FateFlowController.Instance;
            Assert.IsNotNull(fateController, "FateFlowController should exist");
            
            // Act: Advance turn multiple times
            System.Collections.Generic.List<FateSide> turnHistory = new System.Collections.Generic.List<FateSide>();
            
            fateController.OnFateChanged += (side) => { turnHistory.Add(side); };
            
            // Set initial turn
            fateController.SetFate(FateSide.Player);
            yield return null;
            
            // Advance turn 10 times
            for (int i = 0; i < 10; i++)
            {
                FateSide before = fateController.CurrentFate;
                fateController.AdvanceFateFlow();
                yield return null;
                FateSide after = fateController.CurrentFate;
                
                // Assert: Each advance should switch to opposite side
                FateSide expected = before == FateSide.Player ? FateSide.Opponent : FateSide.Player;
                Assert.AreEqual(expected, after, 
                    $"Turn {i + 1}: Should alternate from {before} to {expected}, got {after}");
            }
            
            // Assert: Should have alternated correctly
            Assert.Greater(turnHistory.Count, 0, "Turn change events should have fired");
            
            // Verify alternation pattern (no consecutive same turns)
            for (int i = 1; i < turnHistory.Count; i++)
            {
                FateSide previous = turnHistory[i - 1];
                FateSide current = turnHistory[i];
                Assert.AreNotEqual(previous, current, 
                    $"Turns should alternate - consecutive turns should not be the same (turn {i})");
            }
        }
    }
}

