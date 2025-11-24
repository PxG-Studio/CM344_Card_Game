# BattleScreenMultiplayer Test Suite

## Overview

This test suite provides comprehensive testing for the `BattleScreenMultiplayer` scene using Unity's Test Runner. The tests are organized into **EditMode** and **PlayMode** categories to validate scene setup, component initialization, runtime behavior, and game flow.

## Test Structure

```
Assets/Tests/
├── CM344.CardGame.Tests.asmdef       # Assembly definition for tests
├── EditMode/                          # Tests that run in Edit mode
│   ├── BattleScreenMultiplayerSceneSetupTests.cs
│   ├── ManagerSetupTests.cs
│   └── GameEndAndRematchEditModeTests.cs
└── PlayMode/                          # Tests that run in Play mode
    ├── BattleScreenMultiplayerPlayModeTests.cs
    ├── CardSystemPlayModeTests.cs
    ├── HUDAndUIPlayModeTests.cs
    ├── GameStateAndFlowTests.cs
    └── GameEndAndRematchPlayModeTests.cs
```

## Running Tests

### In Unity Editor

1. **Open Test Runner Window**:
   - Menu: `Window > General > Test Runner`
   - Or press `Ctrl+R` (Windows) / `Cmd+R` (Mac)

2. **Select Test Mode**:
   - **EditMode**: Click "Edit Mode" tab (runs immediately without Play mode)
   - **PlayMode**: Click "Play Mode" tab (requires Play mode)

3. **Run Tests**:
   - **All Tests**: Click "Run All" button
   - **Specific Test**: Click the checkbox next to a test, then click "Run Selected"
   - **Filter**: Use the search bar to filter tests by name

### Test Results

- ✅ **Green**: Test passed
- ❌ **Red**: Test failed (check console for details)
- ⚠️ **Yellow**: Test skipped or ignored
- ⏱️ **Duration**: Time taken for each test

## Test Categories

### EditMode Tests

These tests run in Edit mode (no Play mode required) and validate scene structure and component setup:

#### `BattleScreenMultiplayerSceneSetupTests`
- ✅ Scene exists and loads correctly
- ✅ HUDOverlayCanvas exists with required components
- ✅ Main Canvas exists
- ✅ All 16 DropAreas exist with CardDropArea1 components
- ✅ Prefab assets are not active in scene (if present)
- ✅ HandUIs exist for both players
- ✅ DeckManagers exist for both players
- ✅ EventSystem exists for UI interaction
- ✅ Camera exists
- ✅ ProceduralBoardBackdrop exists (if present)
- ✅ All DropAreas have required components (CardDropArea1, Collider2D, IsOccupied property)
- ✅ HandUIs have GetCardForUI method (for drag validation)
- ✅ CardDropArea1 supports placement validation (OnCardDrop, OnCardDropP2, IsOccupied)
- ✅ NewCardUI has drag validation methods (OnBeginDrag, IsP1Card, IsP2Card)
- ✅ NewCardUI has placement methods (PlaceP2CardOnBoard)
- ✅ FateFlowController has turn validation methods (CanAct, CurrentFate, SetFate)

#### `ManagerSetupTests`
- ✅ GameManager singleton exists
- ✅ CoinTossManager singleton exists
- ✅ FateFlowController singleton exists
- ✅ HUDSetup creates CoinTossUI correctly
- ✅ ScoreManager singleton exists
- ✅ GameEndManager singleton exists
- ✅ CoinTossManager has required methods (PerformCoinToss, ResetCoinToss, GetStartingPlayer, IsComplete)
- ✅ GameManager has state management methods (StartGame, ResetGameState, CurrentState, OnGameStateChanged)

#### `GameEndAndRematchEditModeTests`
- ✅ GameEndManager has required methods (CheckGameEnd, EvaluateWinner, WaitForChainsAndEndGame, Reset)
- ✅ ScoreManager has RecalculateScores method for final score calculation
- ✅ GameEndUI has required methods (ShowGameEnd, HideGameEnd, Rematch, OnRestartClicked, OnQuitClicked)
- ✅ GameManager has ResetGameState method for rematch
- ✅ GameStatsTracker has persistence methods (RecordGameResult, ResetCurrentGameStats, ResetSession, GetWinLossRecord)
- ✅ ScoreManager has score persistence logic (scores reset per game, not session)
- ✅ Game end flow components exist with required methods
- ✅ Rematch resets game but preserves session stats

### PlayMode Tests

These tests run in Play mode and validate runtime behavior:

#### `BattleScreenMultiplayerPlayModeTests`
- ✅ GameManager initializes correctly (starts in Menu state)
- ✅ CoinTossManager exists and is initialized
- ✅ CoinTossUI is created by HUDSetup
- ✅ Coin toss activation flow works
- ✅ Coin toss completes and sets starting player
- ✅ FateFlowController updates after coin toss
- ✅ Game state transitions correctly
- ✅ HUDOverlayCanvas is active

#### `CardSystemPlayModeTests`
- ✅ Player 1 deck initializes
- ✅ Player 2 deck initializes
- ✅ Player 1 HandUI exists
- ✅ Player 2 HandP2UI exists
- ✅ All DropAreas have CardDropArea1 components with Collider2D triggers
- ✅ Cards draw after coin toss completes
- ✅ Prefab assets are inactive (if present)
- ✅ Card placement validation works
- ✅ Card drag prevention for board cards (validates hand check in OnBeginDrag)
- ✅ Cards in hand can be identified via Hub connections
- ✅ Cards on board cannot be picked up (validates they're not in hand hierarchy)
- ✅ Card placement validation checks occupation status

#### `HUDAndUIPlayModeTests`
- ✅ HUDManager exists and is wired
- ✅ P1Panel and P2Panel exist
- ✅ ScoreUI exists
- ✅ TilesRemainingLabel exists with TextMeshProUGUI
- ✅ CoinTossPanel is created and parented correctly
- ✅ CoinTossPanel activates when game starts
- ✅ GameEndUI exists
- ✅ Turn indicators exist
- ✅ HUDOverlayCanvas has required components (Canvas, CanvasScaler, GraphicRaycaster)
- ✅ EventSystem exists and is active
- ✅ CoinTossUI components are wired correctly

#### `GameStateAndFlowTests`
- ✅ GameManager starts in Menu state
- ✅ StartGame transitions to Preparing state
- ✅ Coin toss occurs during Preparing state
- ✅ FateFlowController tracks turns
- ✅ Game state events fire correctly
- ✅ ResetGameState clears game state
- ✅ Turn-based card interaction restrictions (validates CanAct for drag validation)

#### `GameEndAndRematchPlayModeTests`
- ✅ GameEndManager detects when all cards are placed (CheckGameEnd method)
- ✅ ScoreManager recalculates scores on game end (RecalculateScores called)
- ✅ GameEndManager triggers GameEndUI when all cards placed
- ✅ GameEndUI has rematch and quit buttons (OnRestartClicked, OnQuitClicked)
- ✅ Rematch resets game but keeps session stats (GameStatsTracker persists, ScoreManager resets)
- ✅ Score calculation occurs on game end (RecalculateScores in WaitForChainsAndEndGame)
- ✅ GameStatsTracker persists across games (wins/losses/total games continue)
- ✅ GameEndUI displays correct winner and statistics (ShowGameEnd with statistics)
- ✅ Game end flow complete sequence (CheckGameEnd → RecalculateScores → EvaluateWinner → ShowGameEnd)

## Common Issues and Solutions

### Issue: Tests Fail with "Scene not found"
**Solution**: Ensure `BattleScreenMultiplayer` scene exists in `Assets/Scenes/` and is added to Build Settings.

### Issue: PlayMode tests fail with "GameObject not found"
**Solution**: Tests wait for initialization, but if they still fail, increase wait times in `SetUp()` methods.

### Issue: CoinTossUI tests fail
**Solution**: Ensure HUDSetup runs successfully and creates CoinTossUI. Check console for HUDSetup logs.

### Issue: Assembly definition errors
**Solution**: Ensure `CM344.CardGame.Tests.asmdef` references required assemblies:
- `UnityEngine.TestRunner`
- `UnityEditor.TestRunner`
- `Unity.TextMeshPro`
- `nunit.framework.dll` (precompiled reference)

### Issue: Prefab asset tests fail
**Solution**: Prefab assets in scene should be removed using `RemovePrefabAssetsFromScene` editor tool, or they should be inactive.

## Adding New Tests

### EditMode Test Example

```csharp
[Test]
public void MyComponent_Exists()
{
    GameObject obj = GameObject.Find("MyComponent");
    Assert.IsNotNull(obj, "MyComponent should exist");
}
```

### PlayMode Test Example

```csharp
[UnityTest]
public IEnumerator MySystem_Works_Correctly()
{
    yield return new WaitForSeconds(1.0f);
    
    MySystem system = Object.FindObjectOfType<MySystem>();
    Assert.IsNotNull(system, "MySystem should exist");
    
    // Test behavior...
    yield return new WaitForSeconds(0.5f);
    
    Assert.IsTrue(system.IsWorking, "MySystem should be working");
}
```

## Test Coverage

### ✅ Covered Systems
- Scene setup and structure
- Manager initialization (GameManager, CoinTossManager, FateFlowController, ScoreManager, GameEndManager, GameStatsTracker)
- HUD and UI creation (HUDSetup, CoinTossUI, GameEndUI)
- Card systems (Player 1 and Player 2 decks, hands)
- Drop areas (all 16 areas with components)
- Game state management
- Coin toss flow
- Game end detection (when all cards placed)
- Score calculation and recalculation
- Game end UI display with rematch/quit
- Session statistics persistence across games

### 🔄 Partial Coverage
- Card drag/drop interactions (structural and validation logic coverage)
- Turn management (validation of CanAct and turn restrictions)
- Card placement (validates structure and occupation checks)
- Card pickup prevention (validates hand hierarchy checks)

### ❌ Not Yet Covered
- Detailed card interactions (placement, capture, chains)
- Turn flow and timing
- Card effects execution
- Animation timing and completion

## Future Test Additions

### Recommended Additional Tests
1. **Card Interaction Tests**
   - Card placement on drop areas
   - Card capture mechanics
   - Chain capture validation
   - Card stat comparisons

2. **Turn Flow Tests**
   - Turn transitions (Player 1 → Player 2)
   - Turn indicator updates
   - Turn-based card drawing

3. **Score System Tests**
   - ✅ Score calculation on game end (RecalculateScores)
   - ✅ Score updates in UI (events)
   - ✅ Score comparison for game end (evaluated in EvaluateWinner)
   - 🔄 Score updates during gameplay (captures)

4. **Game End Tests**
   - ✅ Game end detection (when all cards placed)
   - ✅ Victory/defeat conditions (evaluated by GameEndManager)
   - ✅ Game end UI display (ShowGameEnd with statistics)
   - ✅ Rematch functionality (ResetGameState)
   - ✅ Quit functionality (OnQuitClicked)

5. **Integration Tests**
   - ✅ Complete game end flow (CheckGameEnd → RecalculateScores → EvaluateWinner → ShowGameEnd)
   - ✅ Rematch flow (ResetGameState preserves session stats, resets per-game data)
   - ✅ Score metrics persistence (GameStatsTracker persists, ScoreManager resets)
   - 🔄 Complete game flow (start → coin toss → turns → end) - partially covered
   - 🔄 Card placement → capture → score → state change - partially covered

## Notes

- **Timing**: PlayMode tests use `yield return new WaitForSeconds()` to allow initialization. Adjust wait times if tests fail due to timing.
- **Dependencies**: Tests assume HUDSetup runs automatically on scene load. If not, tests may need adjustment.
- **Cleanup**: Tests use `SetUp()` and `TearDown()` methods for initialization and cleanup. Ensure proper cleanup to avoid test interference.

## Contributing

When adding new tests:
1. Follow naming convention: `[System]_[Action]_[ExpectedResult]`
2. Add appropriate assertions with descriptive messages
3. Use `[OneTimeSetUp]` for expensive setup operations
4. Use `[SetUp]` / `[TearDown]` for per-test setup/cleanup
5. Document any assumptions or dependencies in test comments
6. Update this README if adding new test categories

