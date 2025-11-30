# Chain Capture System - Testing Guide

This guide provides step-by-step instructions for testing each item in the testing checklist.

## Prerequisites

1. **Add ChainCaptureTester to Scene** (Optional but recommended):
   - Create an empty GameObject named "ChainCaptureTester"
   - Add the `ChainCaptureTester` component
   - This will help verify setup and provide testing utilities

2. **Ensure Unity has finished compiling** - Check bottom-right corner for "Compiling..." status

## Test Checklist

### ✅ Test 1: ScoreManager GameObject exists in scene

**Steps:**
1. In Unity Hierarchy, search for "ScoreManager"
2. If not found, create it:
   - Right-click in Hierarchy > Create Empty
   - Name it "ScoreManager"
   - Add Component > ScoreManager (or search for "CardGame.Managers.ScoreManager")
3. Verify: Inspector shows ScoreManager component with no errors

**Expected Result:** ScoreManager GameObject exists with ScoreManager component attached

**Code Verification:** `ScoreManager.Instance != null` should return true

---

### ✅ Test 2: GameEndManager GameObject exists in scene

**Steps:**
1. In Unity Hierarchy, search for "GameEndManager"
2. If not found, create it:
   - Right-click in Hierarchy > Create Empty
   - Name it "GameEndManager"
   - Add Component > GameEndManager (or search for "CardGame.Managers.GameEndManager")
3. Verify: Inspector shows GameEndManager component with no errors

**Expected Result:** GameEndManager GameObject exists with GameEndManager component attached

**Code Verification:** `GameEndManager.Instance != null` should return true

---

### ✅ Test 3: CardDropArea1 can find ScoreManager

**Steps:**
1. Play the scene
2. Check Unity Console for warnings
3. Look for: "CardDropArea1: ScoreManager not found! Scoring will not work."

**Expected Result:** 
- If ScoreManager exists: No warning should appear
- If ScoreManager missing: Warning appears in console

**Code Location:** `CardDropArea1.cs` line 78-82

**Verification:** 
- If no warning appears, CardDropArea1 successfully found ScoreManager
- If warning appears, add ScoreManager GameObject to scene

---

### ✅ Test 4: CardDropArea1 can find GameEndManager

**Steps:**
1. Play the scene
2. Check Unity Console for warnings
3. Look for: "CardDropArea1: GameEndManager not found! Game end detection will not work."

**Expected Result:**
- If GameEndManager exists: No warning should appear
- If GameEndManager missing: Warning appears in console

**Code Location:** `CardDropArea1.cs` line 88-92

**Verification:**
- If no warning appears, CardDropArea1 successfully found GameEndManager
- If warning appears, add GameEndManager GameObject to scene

---

### ✅ Test 5: Test Chain Capture

**Setup:**
1. Place cards on the board so that:
   - Card A can capture Card B (adjacent, higher stat)
   - Card B can capture Card C (adjacent, higher stat)
2. Enable `debugBattles` on CardDropArea1 for detailed logs

**Steps:**
1. Place Card A adjacent to Card B (Card A should capture Card B)
2. Watch the console for chain capture messages
3. After Card B is captured, verify it checks for additional captures

**Expected Result:**
- Card A captures Card B
- Card B (now captured) immediately checks adjacent cards
- If Card B can capture Card C, it should capture it
- Console shows: "Chain capture triggered! [CardName] can capture X adjacent cards"

**Code Verification:**
- `CheckChainCapture()` is called after each flip completes
- `ExecuteChainCaptureRipple()` processes chain captures
- Console logs show chain capture activity

**What to Look For:**
- Cards flip in sequence (ripple effect)
- After each flip, newly captured card checks for more captures
- Chain continues until no more captures possible

---

### ✅ Test 6: Test Same-Turn Protection

**Setup:**
1. Place a card on the board (Card A)
2. Place an opponent card adjacent to Card A (Card B) that could capture Card A

**Steps:**
1. Place Card A on the board
2. Immediately place Card B adjacent to Card A (same turn)
3. Card B should NOT be able to capture Card A

**Expected Result:**
- Card A is placed
- Card B is placed (same turn)
- Card B cannot capture Card A (same-turn protection)
- Console shows: "CheckChainCapture: [CardName] was played this turn, cannot be captured"

**Code Verification:**
- `cardsPlayedThisTurn` HashSet tracks cards played this turn
- `CheckChainCapture()` checks this set before allowing captures
- Cards are removed from set at turn start/end

**What to Look For:**
- Cards placed on the same turn cannot capture each other
- After turn ends, cards can be captured normally

---

### ✅ Test 7: Test Scoring

**Setup:**
1. Ensure ScoreManager exists in scene
2. Enable console logging

**Steps:**
1. Place a card that captures an opponent card
2. Watch console for score messages
3. Check ScoreManager in Inspector (if using ChainCaptureTester)

**Expected Result:**
- Console shows: "Player score: 1" (or "Opponent score: 1")
- ScoreManager.PlayerScore or OpponentScore increments
- ScoreManager fires OnScoreChanged event

**Code Verification:**
- `FlipCardGameObject()` calls `scoreManager.AddScore()`
- Score increments for the capturing player
- Console logs score updates

**What to Look For:**
- Each capture increments the appropriate score
- Scores update in real-time as cards are captured
- Player score for orange captures, Opponent score for green captures

---

### ✅ Test 8: Test Board Occupancy

**Setup:**
1. Count total CardDropArea1 components in scene (this is your board size)
2. Enable `debugBattles` for detailed logs

**Steps:**
1. Place cards one by one on the board
2. Watch console for: "Board occupancy: X/Y spaces filled"
3. When you place the last card (filling the board), watch for game end trigger

**Expected Result:**
- Console shows board occupancy after each card placement
- When board is full: "Board is full! Last card has been placed."
- GameEndManager.CheckGameEnd() is called

**Code Verification:**
- `CheckBoardOccupancy()` is called after each card drop
- Counts occupied spaces vs total CardDropArea1 instances
- Triggers game end when `occupiedSpaces >= totalSpaces`

**What to Look For:**
- Occupancy count increases with each card placed
- Game end triggers when last space is filled
- Console shows "Board is full!" message

---

### ✅ Test 9: Test Game End

**Setup:**
1. Fill the board completely
2. Ensure chain captures can occur (cards that can capture others)
3. Watch console for game end messages

**Steps:**
1. Place the last card to fill the board
2. Wait for any chain captures to complete
3. Watch for winner determination

**Expected Result:**
- "Board is full! Checking for game end..." appears
- GameEndManager waits for chains to complete
- "Final Scores - Player: X, Opponent: Y" appears
- "Player wins!" or "Opponent wins!" appears
- Game state changes to Victory or Defeat

**Code Verification:**
- `GameEndManager.CheckGameEnd()` is called when board is full
- `WaitForChainsAndEndGame()` waits for chains to complete
- `EvaluateWinner()` compares final scores
- `GameManager.ChangeState()` transitions to Victory/Defeat

**What to Look For:**
- Game waits for all chain captures to finish
- Final scores are recalculated from all captured cards
- Winner is determined correctly
- Game state changes appropriately

---

## Using ChainCaptureTester

The `ChainCaptureTester` script provides automated verification:

1. **Add to Scene:**
   - Create GameObject with ChainCaptureTester component
   - Set "Run Tests On Start" to true for automatic testing

2. **Context Menu Options:**
   - Right-click component > "Display Current Scores"
   - Right-click component > "Reset Scores"
   - Right-click component > "Recalculate Scores"

3. **Inspector Display:**
   - Shows current player/opponent scores
   - Shows setup verification status

---

## Troubleshooting Test Failures

### ScoreManager/GameEndManager not found
- **Solution:** Create GameObjects with these components in the scene
- **Verify:** Check that scripts compiled successfully (no red errors in Console)

### Chain captures not working
- **Check:** `useRippleEffect` is enabled on CardDropArea1
- **Check:** Cards are actually being captured (check capture colors)
- **Check:** `debugBattles` is enabled for detailed logs
- **Verify:** Cards are adjacent (orthogonal neighbors only)

### Scores not updating
- **Check:** ScoreManager exists in scene
- **Check:** Console for "ScoreManager not found" warnings
- **Verify:** Cards are actually being captured (not just placed)

### Game end not triggering
- **Check:** GameEndManager exists in scene
- **Check:** Board occupancy is being calculated correctly
- **Verify:** All CardDropArea1 positions match card positions when dropped
- **Check:** Console for "Board is full!" message

---

## Expected Console Output

When all tests pass, you should see:

```
✅ Test 1 PASSED: ScoreManager found in scene
✅ Test 2 PASSED: GameEndManager found in scene
✅ Test 3 PASSED: Found X CardDropArea1 component(s)
✅ Test 4 PASSED: GameManager found in scene
✅ All critical components found! System is ready for testing.
```

During gameplay:
```
Player score: 1
Chain capture triggered! [CardName] can capture X adjacent cards
Board occupancy: 15/16 spaces filled
Board is full! Last card has been placed.
Board is full! Checking for game end...
Final Scores - Player: 8, Opponent: 7
Player wins!
```
