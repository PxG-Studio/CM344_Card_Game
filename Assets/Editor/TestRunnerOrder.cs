using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

/// <summary>
/// Test Runner Order - Organizes PlayMode tests by execution priority
/// Unity runs tests alphabetically by folder name, so we use numbered prefixes
/// to enforce the correct execution order:
/// 
/// 00_Initialization - Scene setup, managers, prefabs
/// 01_Input - Drag/drop, mouse handling, colliders
/// 02_CoinToss - Coin toss UI, winners, deck setup
/// 03_Flow - Game state, turns, HUD, UI sync
/// 04_Board - Tile rules, placement validation
/// 05_Capture - Capture rules, chain reactions
/// 06_Endgame - Rematch, reset states, end-game UI
/// 07_Stress - Tween cleanup, rapid input, race conditions
/// </summary>
public class TestRunnerOrder : ICallbacks
{
    public void RunStarted(ITestAdaptor testsToRun)
    {
        // Organize groups by execution priority
        // HIGH → LOW
        Debug.Log("[TestRunnerOrder] Test run started. Tests will execute in folder order:");
        Debug.Log("  00_Initialization → 01_Input → 02_CoinToss → 03_Flow → 04_Board → 05_Capture → 06_Endgame → 07_Stress");
    }

    public void RunFinished(ITestResultAdaptor result)
    {
        // ITestResultAdaptor doesn't have TestCount, calculate from child results
        int passCount = 0;
        int failCount = 0;
        int inconclusiveCount = 0;
        
        if (result.HasChildren)
        {
            foreach (var child in result.Children)
            {
                if (child.TestStatus == TestStatus.Passed) passCount++;
                else if (child.TestStatus == TestStatus.Failed) failCount++;
                else inconclusiveCount++;
            }
        }
        else
        {
            if (result.TestStatus == TestStatus.Passed) passCount = 1;
            else if (result.TestStatus == TestStatus.Failed) failCount = 1;
            else inconclusiveCount = 1;
        }
        
        int totalTests = passCount + failCount + inconclusiveCount;
        
        if (result.TestStatus == TestStatus.Passed)
        {
            Debug.Log($"[TestRunnerOrder] All tests completed successfully. Total: {totalTests}, Passed: {passCount}, Failed: {failCount}");
        }
        else
        {
            Debug.LogWarning($"[TestRunnerOrder] Test run completed with failures. Total: {totalTests}, Passed: {passCount}, Failed: {failCount}");
        }
    }

    public void TestStarted(ITestAdaptor test)
    {
        // Optional: Log individual test starts for debugging
        // Debug.Log($"[TestRunnerOrder] Starting: {test.FullName}");
    }

    public void TestFinished(ITestResultAdaptor result)
    {
        if (result.TestStatus == TestStatus.Failed)
        {
            Debug.LogError($"[TestRunnerOrder] Test failed: {result.Test.FullName}\n{result.Message}");
        }
    }
}

