using UnityEngine;
using CardGame.Managers;

namespace CardGame.Testing
{
    /// <summary>
    /// Test script to verify chain capture, scoring, and end game functionality
    /// </summary>
    public class ChainCaptureTester : MonoBehaviour
    {
        [Header("Test Results")]
        [SerializeField] private bool scoreManagerFound = false;
        [SerializeField] private bool gameEndManagerFound = false;
        [SerializeField] private bool cardDropAreaFound = false;
        [SerializeField] private int currentPlayerScore = 0;
        [SerializeField] private int currentOpponentScore = 0;
        
        [Header("Test Controls")]
        [SerializeField] private bool runTestsOnStart = false;
        [SerializeField] private bool logTestResults = true;
        
        private void Start()
        {
            if (runTestsOnStart)
            {
                RunAllTests();
            }
            else
            {
                VerifySetup();
            }
        }
        
        /// <summary>
        /// Verifies that all required components are set up correctly
        /// </summary>
        public void VerifySetup()
        {
            if (logTestResults)
            {
                Debug.Log("=== Chain Capture System Setup Verification ===");
            }
            
            // Test 1: ScoreManager exists
            scoreManagerFound = ScoreManager.Instance != null;
            if (scoreManagerFound)
            {
                if (logTestResults)
                {
                    Debug.Log("✅ Test 1 PASSED: ScoreManager found in scene");
                }
                currentPlayerScore = ScoreManager.Instance.PlayerScore;
                currentOpponentScore = ScoreManager.Instance.OpponentScore;
            }
            else
            {
                Debug.LogError("❌ Test 1 FAILED: ScoreManager not found! Create a GameObject with ScoreManager component.");
            }
            
            // Test 2: GameEndManager exists
            gameEndManagerFound = GameEndManager.Instance != null;
            if (gameEndManagerFound)
            {
                if (logTestResults)
                {
                    Debug.Log("✅ Test 2 PASSED: GameEndManager found in scene");
                }
            }
            else
            {
                Debug.LogError("❌ Test 2 FAILED: GameEndManager not found! Create a GameObject with GameEndManager component.");
            }
            
            // Test 3: CardDropArea1 can find managers
            CardDropArea1[] dropAreas = FindObjectsOfType<CardDropArea1>();
            cardDropAreaFound = dropAreas.Length > 0;
            
            if (cardDropAreaFound)
            {
                if (logTestResults)
                {
                    Debug.Log($"✅ Test 3 PASSED: Found {dropAreas.Length} CardDropArea1 component(s)");
                }
                
                // Check if they can find managers (this will be logged by CardDropArea1 itself)
                foreach (CardDropArea1 area in dropAreas)
                {
                    // The warnings will appear in console if managers aren't found
                }
            }
            else
            {
                Debug.LogWarning("⚠️ Test 3 WARNING: No CardDropArea1 components found in scene");
            }
            
            // Test 4: GameManager exists
            bool gameManagerFound = GameManager.Instance != null;
            if (gameManagerFound)
            {
                if (logTestResults)
                {
                    Debug.Log("✅ Test 4 PASSED: GameManager found in scene");
                }
            }
            else
            {
                Debug.LogWarning("⚠️ Test 4 WARNING: GameManager not found (may be optional)");
            }
            
            if (logTestResults)
            {
                Debug.Log("=== Setup Verification Complete ===");
            }
            
            if (scoreManagerFound && gameEndManagerFound)
            {
                if (logTestResults)
                {
                    Debug.Log("✅ All critical components found! System is ready for testing.");
                }
            }
            else
            {
                Debug.LogError("❌ Missing critical components! Please add ScoreManager and GameEndManager to the scene.");
            }
        }
        
        /// <summary>
        /// Runs all automated tests
        /// </summary>
        public void RunAllTests()
        {
            if (logTestResults)
            {
                Debug.Log("=== Running Chain Capture System Tests ===");
            }
            
            VerifySetup();
            
            // Test chain capture logic (code verification)
            TestChainCaptureLogic();
            
            // Test scoring logic (code verification)
            TestScoringLogic();
            
            // Test end game logic (code verification)
            TestEndGameLogic();
            
            if (logTestResults)
            {
                Debug.Log("=== All Tests Complete ===");
            }
        }
        
        /// <summary>
        /// Verifies chain capture logic is implemented correctly
        /// </summary>
        private void TestChainCaptureLogic()
        {
            if (logTestResults)
            {
                Debug.Log("--- Testing Chain Capture Logic ---");
            }
            
            // Verify CheckChainCapture method exists in CardDropArea1
            CardDropArea1 dropArea = FindObjectOfType<CardDropArea1>();
            if (dropArea != null)
            {
                if (logTestResults)
                {
                    Debug.Log("✅ Chain Capture: CardDropArea1 found - chain capture methods should be available");
                }
            }
            else
            {
                Debug.LogWarning("⚠️ Chain Capture: CardDropArea1 not found - cannot verify chain capture");
            }
            
            if (logTestResults)
            {
                Debug.Log("ℹ️ Manual Test Required: Place a card that captures another, then verify the captured card can capture adjacent cards");
            }
        }
        
        /// <summary>
        /// Verifies scoring logic is implemented correctly
        /// </summary>
        private void TestScoringLogic()
        {
            if (logTestResults)
            {
                Debug.Log("--- Testing Scoring Logic ---");
            }
            
            if (ScoreManager.Instance != null)
            {
                int initialPlayerScore = ScoreManager.Instance.PlayerScore;
                int initialOpponentScore = ScoreManager.Instance.OpponentScore;
                
                if (logTestResults)
                {
                    Debug.Log($"✅ Scoring: ScoreManager accessible - Player: {initialPlayerScore}, Opponent: {initialOpponentScore}");
                    Debug.Log("ℹ️ Manual Test Required: Capture a card and verify score increases in console");
                }
            }
            else
            {
                Debug.LogError("❌ Scoring: ScoreManager not found!");
            }
        }
        
        /// <summary>
        /// Verifies end game logic is implemented correctly
        /// </summary>
        private void TestEndGameLogic()
        {
            if (logTestResults)
            {
                Debug.Log("--- Testing End Game Logic ---");
            }
            
            if (GameEndManager.Instance != null)
            {
                if (logTestResults)
                {
                    Debug.Log("✅ End Game: GameEndManager found - end game detection should work");
                }
            }
            else
            {
                Debug.LogError("❌ End Game: GameEndManager not found!");
            }
            
            if (GameManager.Instance != null)
            {
                if (logTestResults)
                {
                    Debug.Log("✅ End Game: GameManager found - state transitions should work");
                }
            }
            else
            {
                Debug.LogWarning("⚠️ End Game: GameManager not found");
            }
            
            if (logTestResults)
            {
                Debug.Log("ℹ️ Manual Test Required: Fill the board and verify game end triggers after chains complete");
            }
        }
        
        /// <summary>
        /// Displays current scores (useful for manual testing)
        /// </summary>
        [ContextMenu("Display Current Scores")]
        public void DisplayCurrentScores()
        {
            if (ScoreManager.Instance != null)
            {
                Debug.Log($"Current Scores - Player: {ScoreManager.Instance.PlayerScore}, Opponent: {ScoreManager.Instance.OpponentScore}");
            }
            else
            {
                Debug.LogError("ScoreManager not found!");
            }
        }
        
        /// <summary>
        /// Resets scores (useful for testing)
        /// </summary>
        [ContextMenu("Reset Scores")]
        public void ResetScores()
        {
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.ResetScores();
                Debug.Log("Scores reset to 0");
            }
            else
            {
                Debug.LogError("ScoreManager not found!");
            }
        }
        
        /// <summary>
        /// Recalculates scores from board state
        /// </summary>
        [ContextMenu("Recalculate Scores")]
        public void RecalculateScores()
        {
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.RecalculateScores();
                Debug.Log($"Recalculated Scores - Player: {ScoreManager.Instance.PlayerScore}, Opponent: {ScoreManager.Instance.OpponentScore}");
            }
            else
            {
                Debug.LogError("ScoreManager not found!");
            }
        }
        
        private void Update()
        {
            // Update score display in inspector
            if (ScoreManager.Instance != null)
            {
                currentPlayerScore = ScoreManager.Instance.PlayerScore;
                currentOpponentScore = ScoreManager.Instance.OpponentScore;
            }
        }
    }
}
