# How to Prevent Log Truncation (15000 Character Limit)

When test output files exceed 15,000 characters, they get truncated with `---MESSAGE TRUNCATED AT 15000 CHARACTERS---`. Here are several strategies to prevent this:

## Strategy 1: Use Filtered Logging (Recommended)

I've already updated `AreCardsStrictlyAdjacent` to only log when:
- `debugBattles` is enabled, OR
- Distance < 10f (filters out cards in hands at z=90 which are ~90 units away)

This dramatically reduces log volume while preserving test-critical information.

## Strategy 2: Use the Filter Tool

A Unity Editor tool has been created at `Assets/Editor/FilterTestOutput.cs`. 

**To use it:**
1. In Unity Editor, go to menu: `Tools > Filter Test Output`
2. It will filter the latest test output file, keeping only:
   - `[EdgePlacementTest]` logs
   - `[StrictAdjacency]` logs
   - `[CheckCardBattles]` logs
   - Error messages
   - Assert failures
   - Stack traces

The filtered output will be saved as `*_FILTERED.txt` in your Downloads folder.

## Strategy 3: Use grep/pattern matching

Instead of reading entire files, use grep to find specific patterns:

```bash
# Find only test-related logs
grep -i "\[EdgePlacementTest\]\|\[StrictAdjacency\]\|\[CheckCardBattles\]" output.txt

# Find only errors
grep -i "error\|exception\|assert" output.txt

# Find only failures
grep -i "expected.*but was\|failed\|assert" output.txt
```

## Strategy 4: Read Files in Chunks

When files are too large, read them in sections:

```csharp
// Read first 100 lines
read_file target_file offset 1 limit 100

// Read last 100 lines
read_file target_file offset (total_lines - 100) limit 100

// Read around a specific line (e.g., line 2000)
read_file target_file offset 1950 limit 100
```

## Strategy 5: Reduce Logging Verbosity

### Option A: Use Conditional Logging

Add conditions to limit logging:

```csharp
// Only log when debugging or in specific scenarios
if (debugBattles || distance < 10f)
{
    Debug.Log($"Message: {data}");
}
```

### Option B: Use Log Levels

Use different log methods based on importance:

```csharp
Debug.LogError(...);      // Always shows
Debug.LogWarning(...);    // Important but not critical
Debug.Log(...);           // Regular info (can be filtered)
```

### Option C: Disable Debug Logging in Tests

Add a flag to disable verbose logging during tests:

```csharp
private static bool suppressVerboseLogs = true; // Set to false for debugging

if (!suppressVerboseLogs || debugBattles)
{
    Debug.Log("Verbose message");
}
```

## Strategy 6: Summary Logging

Instead of logging every action, log summaries:

```csharp
// Instead of:
foreach (var card in cards)
{
    Debug.Log($"Checking card {card.name}...");
}

// Do:
Debug.Log($"[Summary] Checking {cards.Count} cards...");
// Only log important ones
if (importantCard != null)
{
    Debug.Log($"Important: {importantCard.name}");
}
```

## Strategy 7: Configure Unity Test Runner

In Unity's Test Runner, you can:
1. Filter which tests run (reduces output)
2. Use `[Category]` attributes to group tests
3. Run tests individually instead of all at once

## Strategy 8: Write Test-Specific Log Files

Modify tests to write only relevant information to separate files:

```csharp
[UnityTest]
public IEnumerator EdgePlacement_DoesNotTriggerInvalidComparisons()
{
    var logBuilder = new StringBuilder();
    
    // ... test code ...
    
    logBuilder.AppendLine($"[EdgePlacementTest] Distance: {distance}");
    logBuilder.AppendLine($"[EdgePlacementTest] Result: {result}");
    
    // Write summary to file
    File.WriteAllText("test_summary.txt", logBuilder.ToString());
    
    // Use regular Debug.Log only for failures
    if (testFailed)
    {
        Debug.LogError(logBuilder.ToString());
    }
}
```

## Strategy 9: Use Unity's Console Filtering

Unity Editor's Console has filtering options:
1. Open Console window
2. Use filter dropdowns to show only:
   - Errors
   - Warnings
   - Specific categories (if using custom log tags)

## Recommended Approach

**For this project, I recommend:**
1. ✅ **Already done**: Conditional logging in `AreCardsStrictlyAdjacent` (filters z=90 cards)
2. ✅ **Already done**: Filter tool created at `Assets/Editor/FilterTestOutput.cs`
3. **Use grep**: When viewing logs, use grep to find specific patterns
4. **Read in chunks**: For large files, read sections instead of entire file

## Quick Reference

```bash
# Filter test output for this specific test
grep -E "\[EdgePlacementTest\]|\[StrictAdjacency\]|Expected:|But was:" output.txt

# Count how many lines (to see if truncation occurred)
wc -l output.txt

# View last 50 lines (where failures usually are)
tail -n 50 output.txt

# View first 100 lines (setup)
head -n 100 output.txt
```

