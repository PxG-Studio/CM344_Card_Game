using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;

namespace CardGame.Editor
{
    /// <summary>
    /// Utility to filter test output logs to reduce truncation issues
    /// Filters by patterns like [EdgePlacementTest], [StrictAdjacency], errors, etc.
    /// </summary>
    public static class FilterTestOutput
    {
        [MenuItem("Tools/Filter Test Output")]
        public static void FilterLatestTestOutput()
        {
            string downloadsPath = @"C:\Users\nervcentre\Downloads\CARDGAME\CardCapturePlayModeTests";
            string testName = "EdgePlacement_DoesNotTriggerInvalidComparisons";
            string inputFile = Path.Combine(downloadsPath, $"{testName}.txt");
            
            if (!File.Exists(inputFile))
            {
                Debug.LogError($"Test output file not found: {inputFile}");
                return;
            }
            
            // Read and filter
            string[] lines = File.ReadAllLines(inputFile);
            string[] filtered = lines.Where(line => 
                line.Contains("[EdgePlacementTest]") ||
                line.Contains("[StrictAdjacency]") ||
                line.Contains("[CheckCardBattles]") ||
                line.Contains("Expected:") ||
                line.Contains("But was:") ||
                line.Contains("Assert") ||
                line.Contains("ERROR") ||
                line.Contains("Exception") ||
                Regex.IsMatch(line, @"at\s+\w+\.", RegexOptions.IgnoreCase)
            ).ToArray();
            
            // Write filtered output
            string outputFile = Path.Combine(downloadsPath, $"{testName}_FILTERED.txt");
            File.WriteAllLines(outputFile, filtered);
            
            Debug.Log($"Filtered {lines.Length} lines down to {filtered.Length} lines. Output: {outputFile}");
        }
    }
}

