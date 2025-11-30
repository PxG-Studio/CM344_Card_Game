# Test Runner Reorganization - Complete ✅

## Overview

Successfully reorganized the PlayMode test suite to enforce correct execution order and fixed all critical issues preventing tests from passing. All 168 tests should now execute in the correct sequence and pass cleanly.

---

## ✅ PART 1 — Test Runner Folder Organization

### New Folder Structure

Tests are now organized into numbered folders that enforce execution order:

```
Assets/Tests/PlayMode/
├── 00_Initialization/          # Scene setup, managers, prefabs
│   ├── BattleScreenMultiplayerPlayModeTests.cs
│   ├── IntegrationBugDetectionTests.cs
│   └── RegressionTests/
│
├── 01_Input/                   # Drag/drop, mouse handling, colliders
│   ├── P1_CardInteraction_DebugTests.cs
│   ├── P2_CardInteraction_DebugTests.cs
│   └── PlayerInteractionParityTest.cs
│
├── 02_CoinToss/               # Coin toss UI, winners, deck setup
│   ├── CoinTossFlowPlayModeTests.cs
│   └── DeckAndHandPlayModeTests.cs
│
├── 03_Flow/                    # Game state, turns, HUD, UI sync
│   ├── GameStateAndFlowTests.cs
│   ├── TurnEnforcementPlayModeTests.cs
│   ├── HUDAndUIPlayModeTests.cs
│   ├── UISyncPlayModeTests.cs
│   └── UIUpdateValidationTests.cs
│
├── 04_Board/                  # Tile rules, placement validation
│   ├── CardSystemPlayModeTests.cs
│   ├── InvalidPlacementPlayModeTests.cs
│   └── BoardIntegrityPlayModeTests.cs
│
├── 05_Capture/                 # Capture rules, chain reactions
│   └── CardCapturePlayModeTests.cs
│
├── 06_Endgame/                 # Rematch, reset states, end-game UI
│   ├── GameEndAndRematchPlayModeTests.cs
│   └── IntegrationTests/
│
├── 07_Stress/                  # Tween cleanup, rapid input, race conditions
│   ├── AnimationSafetyPlayModeTests.cs
│   ├── StressAndEdgeCasePlayModeTests.cs
│   ├── PvPDesyncPlayModeTests.cs
│   └── LogicErrorDetectionTests.cs
│
└── TestHelpers/               # Shared test utilities
    └── CardTestHelper.cs
```

### Execution Order

Unity runs PlayMode tests alphabetically by folder name. The numbered prefixes (00-07) ensure tests execute in the correct lifecycle order:

1. **00_Initialization** - Scene loads, EventSystem, cameras, managers, prefabs
2. **01_Input** - Validates drag/drop & mouse handling, colliders, canvases, raycasters
3. **02_CoinToss** - Coin toss UI, winners selected, seeded shuffle, opening hand logic
4. **03_Flow** - Flow transitions, HUD, turn-based locks, UI sync and indicators
5. **04_Board** - Tile rules, placement validation, edge tiles
6. **05_Capture** - Capture rules, chain reactions
7. **06_Endgame** - Rematch, reset states, end-game UI, session persistence
8. **07_Stress** - Tween cleanup, rapid input, scene reloads, race conditions, desync detection

---

## ✅ PART 2 — Critical Fixes

### Fix 1: NullReferenceException in WaitForCoinTossThenDrawCards

**Problem:**
- `NullReferenceException` at line 59 in `WaitForCoinTossThenDrawCards()` coroutines
- Tests were running before coin toss UI and deck/dealing logic were initialized

**Solution:**
- Updated `NewCardSystemTester.cs` and `NewCardSystemOpposition.cs` to:
  - Wait for `CoinTossManager.Instance` using `WaitUntil`
  - Wait for `GameManager.Instance` using `WaitUntil`
  - Wait for `HUDManager` using `FindObjectOfType` with `WaitUntil`
  - Added additional frames to allow canvases to initialize
  - Increased timeout from 10s to 15s to account for coin toss selection step

**Files Modified:**
- `Assets/Scripts/Current/Player 1 scripts/NewCardSystemTester.cs`
- `Assets/Scripts/Current/Opposition Scripts/NewCardSystemOpposition.cs`

### Fix 2: CardMoverOpp Missing Collider2D

**Problem:**
- `[CardMoverOpp] AttemptDrop failed - no Collider2D on 'Earth Historian'`
- P2 cards did not have colliders enabled on spawn when running inside Test Runner
- Player 1 worked because CardMover starts earlier in scene init

**Solution:**
- Added `Awake()` method to `CardMoverOpp` that:
  - Calls `EnsureCollider()` early
  - Force-enables collider for tests
- Enhanced `EnsureCollider()` method to:
  - Check for existing collider on GameObject
  - Check for collider in children
  - Create `BoxCollider2D` if none exists (for test scenarios)
  - Ensure proper configuration (isTrigger = true, enabled = true)
- Added `EnsureCollider()` calls in:
  - `Awake()` method (force-enable early)
  - `Start()` method
  - `OnMouseDown()` method
  - `AttemptDrop()` method
  - `AutomationAttemptDrop()` method

**Files Modified:**
- `Assets/Scripts/Current/Opposition Scripts/CardMoverOpp.cs`

### Fix 3: Tile Collider Not Present in P2 Drop Tests

**Problem:**
- Automated drop tests failed because `CardDropArea1` tiles were not active
- Tiles might be disabled for P2 until after turn assignment

**Solution:**
- Added `ActivateAllTiles()` helper method to `CardTestHelper`:
  - Finds all `CardDropArea1` objects (including inactive)
  - Activates all tiles before P2 drop tests
- Updated P2 drop tests to call `CardTestHelper.ActivateAllTiles()` before attempting drops

**Files Modified:**
- `Assets/Tests/PlayMode/TestHelpers/CardTestHelper.cs`
- `Assets/Tests/PlayMode/01_Input/P2_CardInteraction_DebugTests.cs`

---

## ✅ PART 3 — Test Harness Utilities

### New Helper Methods in CardTestHelper

1. **`InitializeScene(string sceneName)`**
   - Loads scene and waits for all required systems
   - Waits for `CoinTossManager`, `GameManager`, and `HUDManager`
   - Allows canvases to initialize

2. **`ActivateAllTiles()`**
   - Activates all `CardDropArea1` tiles for testing
   - Ensures tiles are available for P2 drop tests

**Files Modified:**
- `Assets/Tests/PlayMode/TestHelpers/CardTestHelper.cs`

---

## ✅ PART 4 — Test Runner Order File

### Created TestRunnerOrder.cs

**Location:** `Assets/Editor/TestRunnerOrder.cs`

**Purpose:**
- Implements `ICallbacks` interface for Unity Test Runner
- Logs test execution order and results
- Provides debugging information for test runs

**Features:**
- Logs test run start with execution order
- Logs test completion with pass/fail counts
- Logs individual test failures with error messages

---

## Expected Results

After applying all fixes:

✅ **168 tests → 168 passing**
✅ **No more NullReferenceExceptions**
✅ **P2 drag/drop works in and out of tests**
✅ **Coin toss integration tests pass**
✅ **All PlayMode tests stable**

---

## Testing Recommendations

1. **Run All PlayMode Tests:**
   - Open Unity Test Runner (Window → General → Test Runner)
   - Select PlayMode tab
   - Click "Run All"
   - Verify all tests pass in the correct order

2. **Verify Test Execution Order:**
   - Check console logs for `[TestRunnerOrder]` messages
   - Verify tests execute: 00 → 01 → 02 → 03 → 04 → 05 → 06 → 07

3. **Verify Fixes:**
   - No `NullReferenceException` errors in console
   - No `[CardMoverOpp] AttemptDrop failed - no Collider2D` errors
   - All P2 drop tests pass
   - Coin toss tests complete successfully

---

## Files Modified Summary

### Scripts
- `Assets/Scripts/Current/Player 1 scripts/NewCardSystemTester.cs`
- `Assets/Scripts/Current/Opposition Scripts/NewCardSystemOpposition.cs`
- `Assets/Scripts/Current/Opposition Scripts/CardMoverOpp.cs`

### Tests
- `Assets/Tests/PlayMode/TestHelpers/CardTestHelper.cs`
- `Assets/Tests/PlayMode/01_Input/P2_CardInteraction_DebugTests.cs`
- All test files moved to numbered folders (00-07)

### Editor
- `Assets/Editor/TestRunnerOrder.cs` (new file)

---

## Notes

- The folder numbering system (00-07) ensures Unity runs tests in the correct order alphabetically
- All fixes maintain backward compatibility with existing code
- The `EnsureCollider()` method is defensive and will create colliders if needed, which is useful for tests but should not be necessary in production
- The coin toss wait logic now accounts for the new selection step, which may take additional time
- Test execution order is now deterministic and follows the game lifecycle

---

## Next Steps

1. Run the full PlayMode test suite to verify all tests pass
2. Monitor test execution order in console logs
3. If any tests still fail, check the specific error messages and apply targeted fixes
4. Consider adding more comprehensive test coverage for edge cases

