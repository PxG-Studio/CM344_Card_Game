# Test Organization - Rematch & Game End Tests

## Namespace Structure

All rematch and game end tests are now properly organized using namespaces that match the folder structure:

### PlayMode Tests
- **Namespace**: `Tests.PlayMode.Endgame`
- **Location**: `Assets/Tests/PlayMode/06_Endgame/`
- **Test Classes**:
  - `RematchBoardResetTests` - Comprehensive 22-test suite for rematch board reset
  - `GameEndAndRematchPlayModeTests` - Game end detection and UI tests

### EditMode Tests
- **Namespace**: `Tests.EditMode.Endgame`
- **Location**: `Assets/Tests/EditMode/06_Endgame/`
- **Test Classes**:
  - `GameEndAndRematchEditModeTests` - Structural/API validation tests (13 tests)

## Test Runner Hierarchy

The tests will appear in Unity Test Runner with this hierarchy:

```
Tests
├── PlayMode
│   └── Endgame
│       ├── RematchBoardResetTests
│       │   ├── Test 1: Visual State - Tile Colors
│       │   ├── Test 2: Board State - Card Destruction
│       │   ├── Test 3: Manager State - All 12 Reset Steps
│       │   ├── Test 4: Edge Cases & Error Conditions
│       │   └── Test 5: Full Integration Tests
│       └── GameEndAndRematchPlayModeTests
│           ├── GameEndManager_Detects_When_All_Cards_Placed
│           ├── ScoreManager_Recalculates_Scores_On_Game_End
│           ├── GameEndManager_Triggers_GameEnd_UI_When_All_Cards_Placed
│           ├── GameEndUI_Has_Rematch_And_Quit_Buttons
│           ├── Rematch_Resets_Game_But_Keeps_Session_Stats
│           ├── Score_Calculation_Occurs_On_Game_End
│           ├── GameStatsTracker_Persists_Across_Games
│           ├── GameEndUI_Displays_Correct_Winner_And_Statistics
│           └── Game_End_Flow_Complete_Sequence
└── EditMode
    └── Endgame
        └── GameEndAndRematchEditModeTests
            ├── GameEndManager_Has_Required_Methods
            ├── ScoreManager_Has_Recalculate_Method
            ├── GameEndUI_Has_Required_Methods_And_Fields
            ├── GameManager_Has_ResetGameState_Method
            ├── GameStatsTracker_Has_Persistence_Methods
            ├── ScoreManager_Has_Score_Persistence_Logic
            ├── GameEndFlow_Components_Exist
            ├── Rematch_Resets_Game_But_Preserves_Session_Stats
            ├── CardDropArea_Has_ResetForNewGame_Method
            ├── GameManager_ResetGameState_Calls_All_Required_Steps
            ├── CardFrontlineUI_Has_ResetFrontline_Method
            ├── NewDeckManagerP1_And_P2_Have_InitializeDeck_Methods
            ├── NewHandP1UI_And_P2UI_Have_ClearHand_Methods
            ├── CoinTossManager_Has_ResetCoinToss_Method
            └── CoinTossUI_Has_Show_Method
```

## Dependencies

### CardTestHelper Access
- PlayMode tests use `using CardGame.Tests;` to access `CardTestHelper`
- `CardTestHelper` is in `CardGame.Tests` namespace
- All helper methods are accessible via this import

### Codebase Namespaces Used
- `CardGame.Managers` - GameManager, ScoreManager, GameEndManager, etc.
- `CardGame.UI` - GameEndUI, CardFrontlineUI, NewHandP1UI, NewHandP2UI, etc.
- `CardGame.Core` - NewCard
- `CardGame.Tests` - CardTestHelper

## Test Organization Benefits

1. **Clear Hierarchy**: Tests are organized by mode (PlayMode/EditMode) and category (Endgame)
2. **Easy Navigation**: Test Runner shows clear folder-like structure
3. **Consistent Naming**: Follows existing test patterns in the codebase
4. **Proper Grouping**: Related tests are grouped together logically

## Running Tests

1. Open Unity Test Runner (Window → General → Test Runner)
2. Select **PlayMode** or **EditMode** tab
3. Navigate to **Tests → PlayMode/EditMode → Endgame**
4. Expand test classes to see individual tests
5. Run all tests or select specific tests to run

