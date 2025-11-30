# BattleScreenMultiplayer Test Suite Guide

## Quick Start

1. **Open Test Runner**: `Window > General > Test Runner` (or `Ctrl+R` / `Cmd+R`)
2. **Select Test Mode**: Click "Edit Mode" or "Play Mode" tab
3. **Run Tests**: Click "Run All" or select specific tests and click "Run Selected"

## Test Files

### EditMode Tests (No Play Mode Required)

1. **`BattleScreenMultiplayerSceneSetupTests.cs`**
   - Validates scene structure and component presence
   - Tests: Scene loading, HUDOverlayCanvas, DropAreas, HandUIs, DeckManagers, etc.

2. **`ManagerSetupTests.cs`**
   - Validates manager singletons and HUDSetup functionality
   - Tests: GameManager, CoinTossManager, FateFlowController, CoinTossUI creation

### PlayMode Tests (Requires Play Mode)

1. **`BattleScreenMultiplayerPlayModeTests.cs`**
   - Tests game initialization and coin toss flow
   - Tests: GameManager state, CoinTossManager, CoinTossUI activation, game flow

2. **`CardSystemPlayModeTests.cs`**
   - Tests card systems for both players
   - Tests: Deck initialization, HandUI creation, DropArea validation, card drawing

3. **`HUDAndUIPlayModeTests.cs`**
   - Tests HUD and UI systems
   - Tests: HUDManager, panels, ScoreUI, CoinTossPanel creation/activation, EventSystem

4. **`GameStateAndFlowTests.cs`**
   - Tests game state management and transitions
   - Tests: State transitions, coin toss during Preparing, FateFlowController, events

## Running Specific Test Categories

### To Test Only Scene Setup
```
EditMode > BattleScreenMultiplayerSceneSetupTests > Run Selected
```

### To Test Only Coin Toss System
```
PlayMode > BattleScreenMultiplayerPlayModeTests > CoinToss_* > Run Selected
```

### To Test Only Card Systems
```
PlayMode > CardSystemPlayModeTests > Run Selected
```

### To Test Only HUD/UI
```
PlayMode > HUDAndUIPlayModeTests > Run Selected
```

## Test Coverage Summary

### ✅ Fully Tested
- Scene structure and component presence
- Manager singleton initialization
- CoinTossUI creation and setup
- DropArea existence and components
- HandUI and DeckManager existence
- HUDOverlayCanvas structure

### 🔄 Partially Tested
- Coin toss activation flow (structural validation)
- Card drawing (flow validation)
- Game state transitions (basic validation)

### ❌ Not Yet Tested
- Card drag/drop interactions (detailed)
- Card placement and capture mechanics
- Score calculation
- Game end conditions
- Turn flow timing
- Animation completion

## Interpreting Test Results

### ✅ Passing Tests
- Green checkmark: Test passed
- All assertions validated successfully

### ❌ Failing Tests
- Red X: Test failed
- Check console for error details
- Common causes:
  - Scene not loaded correctly
  - Component missing or not initialized
  - Timing issues (PlayMode tests may need longer waits)

### ⚠️ Skipped Tests
- Yellow icon: Test skipped
- Usually due to `[Ignore]` attribute or preconditions not met

## Common Test Failures and Solutions

### "Scene not found" or "Scene not loaded"
**Solution**: 
- Ensure `BattleScreenMultiplayer` scene exists in `Assets/Scenes/`
- Verify scene is added to Build Settings

### "CoinTossUI not found"
**Solution**:
- Ensure HUDSetup runs successfully
- Check console for HUDSetup logs
- Verify HUDOverlayCanvas exists in scene

### "GameManager.Instance is null"
**Solution**:
- Ensure GameManager is created by HUDSetup
- Check if DontDestroyOnLoad GameObject exists
- Verify no duplicate GameManagers

### "GameObject not found" (PlayMode tests)
**Solution**:
- Increase wait times in `SetUp()` methods
- Verify GameObject exists in scene
- Check if GameObject is inactive (some tests search inactive objects)

### "Assembly definition errors"
**Solution**:
- Verify `CM344.CardGame.Tests.asmdef` is configured correctly
- Ensure Test Framework package is installed
- Check assembly references match project structure

## Using Tests for Debugging

### Step 1: Run All Tests
- Identify which systems are failing
- Check test categories (EditMode vs PlayMode)

### Step 2: Run Failing Test Category
- Narrow down to specific system
- Check console for detailed error messages

### Step 3: Fix Issues
- Address test failures one by one
- Re-run tests after each fix
- Verify fixes don't break other tests

### Step 4: Re-run Full Suite
- Ensure all tests pass
- Verify no regressions introduced

## Test Maintenance

### Adding New Tests
1. Create test method with `[Test]` or `[UnityTest]` attribute
2. Follow naming convention: `[System]_[Action]_[ExpectedResult]`
3. Add descriptive assertions with error messages
4. Update this guide if adding new test categories

### Updating Existing Tests
- If system behavior changes, update corresponding tests
- Update assertions to match new behavior
- Add tests for new features

### Test Organization
- Group related tests in same file
- Use descriptive test class names
- Add XML comments explaining test purpose

## Best Practices

1. **Run tests before committing**: Ensure all tests pass
2. **Run tests after major changes**: Verify no regressions
3. **Add tests for new features**: Maintain coverage
4. **Keep tests fast**: Use appropriate wait times
5. **Keep tests independent**: Each test should be able to run alone
6. **Use descriptive names**: Make test purpose clear
7. **Add comments**: Explain complex test logic

## Integration with CI/CD

### Automated Testing
- Tests can be run in automated build pipelines
- EditMode tests run faster and don't require Play mode
- PlayMode tests require Play mode and take longer

### Test Reports
- Unity Test Runner generates test results
- Export results for CI/CD integration
- Track test coverage over time

## Future Enhancements

### Planned Test Additions
1. **Card Interaction Tests**
   - Detailed drag/drop validation
   - Card placement mechanics
   - Capture and chain logic

2. **Score System Tests**
   - Score calculation validation
   - Score UI updates
   - Score comparison for game end

3. **Turn Flow Tests**
   - Turn transitions
   - Turn indicator updates
   - Turn-based card drawing

4. **Game End Tests**
   - Victory conditions
   - Defeat conditions
   - Game end UI display

5. **Performance Tests**
   - Frame rate validation
   - Memory usage checks
   - Load time measurements

## Support

For issues with tests:
1. Check console for error messages
2. Review test code comments
3. Verify scene setup matches test expectations
4. Check Unity Test Runner documentation
5. Review this guide for common solutions

