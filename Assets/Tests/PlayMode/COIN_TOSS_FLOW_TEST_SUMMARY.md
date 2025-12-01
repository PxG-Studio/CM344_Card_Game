# Coin Toss Flow Test Suite - Complete Summary

## Overview

This test suite validates the complete Coin Toss → Game Start Flow, including selection window, coin toss mechanics, winner banner, card dealing, and turn assignment.

## Test File Created

**File**: `Assets/Tests/PlayMode/CoinTossFlowPlayModeTests.cs`

## Test Coverage

### SECTION 1 — Selection Window (2 tests)

1. **SelectionWindow_Appears_OnSceneStart**
   - Validates coin toss UI exists
   - Checks for heads/tails labels
   - Verifies CoinTossManager exists
   - **Note**: Current implementation doesn't have selection window with buttons yet

2. **SelectionWindow_TimesOut_AndAutoSelectsIfNoPlayerInput**
   - Tests 10-second timeout behavior
   - Validates auto-selection after timeout
   - **Note**: Current implementation doesn't have timeout mechanism yet

### SECTION 2 — Early Player Selection (2 tests)

3. **CoinToss_Begins_When_Player1_SelectsSide**
   - Simulates Player 1 selecting Heads
   - Validates coin toss starts immediately
   - Checks selection window closes

4. **CoinToss_Begins_When_Player2_SelectsSide**
   - Simulates Player 2 selecting Tails
   - Validates coin toss starts immediately
   - Checks selection window closes

### SECTION 3 — Coin Toss Mechanics (2 tests)

5. **CoinToss_AnimationSequence_PlaysBeforeResult**
   - Validates animation plays before result is shown
   - Checks coin image rotation
   - Verifies animation duration

6. **CoinLands_DeterminesWinnerCorrectly**
   - Tests multiple coin tosses for randomness
   - Validates deterministic results with forced values
   - Ensures results are valid (Player or Opponent)

### SECTION 4 — Winner Banner (2 tests)

7. **GameStartBanner_ShowsCorrectWinner**
   - Tests Player 1 win banner
   - Tests Player 2 win banner
   - Validates result text displays correctly

8. **GameStartBanner_ClosesAfterDuration**
   - Tests banner closes after continue button click
   - **Note**: Current implementation requires button click; new flow should auto-close after 2-3 seconds

### SECTION 5 — Card Dealing (3 tests)

9. **Player1_ReceivesRandomOpeningHand**
   - Validates Player 1 receives cards
   - Checks cards are from ScriptableObject deck
   - Verifies card references are valid

10. **Player2_ReceivesRandomOpeningHand**
    - Validates Player 2 receives cards
    - Checks cards are from ScriptableObject deck
    - Verifies card references are valid

11. **Deck_ShuffleDeterministic_WithSeed**
    - Validates ShuffleDeck() method exists
    - **Note**: Unity's Random doesn't support seeding directly; would need custom RNG for deterministic tests

### SECTION 6 — Turn Assignment (2 tests)

12. **WinnerOfCoinToss_Player1_ReceivesFirstTurn**
    - Forces Player 1 to win coin toss
    - Validates FateFlowController sets Player 1's turn
    - Checks CanAct() works correctly

13. **WinnerOfCoinToss_Player2_ReceivesFirstTurn**
    - Forces Player 2 to win coin toss
    - Validates FateFlowController sets Player 2's turn
    - Checks CanAct() works correctly

### SECTION 7 — Transition Into Gameplay (1 test)

14. **GameTransitions_IntoNormalTurnBasedFlow**
    - Validates coin toss UI hides after transition
    - Checks turn system is active
    - Verifies game state transitions correctly
    - Ensures card placement is active for starting player
    - Validates EventSystem remains stable

## Current Implementation Status

### ✅ Already Implemented
- Coin toss animation (3D spinning)
- Coin toss result determination
- Result text display
- Continue button
- Turn assignment via FateFlowController
- Card dealing system

### ❌ Not Yet Implemented (Required for New Flow)
1. **Selection Window with Heads/Tails Buttons**
   - UI panel with two buttons (Heads/Tails)
   - Both players can click either button
   - First click triggers coin toss immediately

2. **Countdown Timer**
   - 10-second countdown display
   - Auto-selects random side if no input
   - Timer stops when player selects

3. **Game Start Banner**
   - Banner similar to GameEndUI style
   - Shows "Player 1 Starts!" or "Player 2 Starts!"
   - Auto-closes after 2-3 seconds (no button click needed)

4. **Selection Window Timeout**
   - Auto-selects random side after 10 seconds
   - Triggers coin toss automatically

## Implementation Recommendations

### 1. Create Selection Window UI

**New Component**: `CoinTossSelectionUI.cs`

```csharp
public class CoinTossSelectionUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject selectionPanel;
    [SerializeField] private Button headsButton;
    [SerializeField] private Button tailsButton;
    [SerializeField] private TextMeshProUGUI countdownText;
    
    [Header("Settings")]
    [SerializeField] private float selectionTimeout = 10f;
    
    private float timeRemaining;
    private bool selectionMade = false;
    
    public event System.Action<bool> OnSelectionMade; // true = heads, false = tails
    public event System.Action OnTimeout;
    
    // Start countdown and show selection window
    // Handle button clicks
    // Auto-select on timeout
}
```

### 2. Integrate with CoinTossUI

Modify `CoinTossUI.cs` to:
- Show selection window first
- Wait for selection or timeout
- Then start animation
- Show result banner after animation

### 3. Create Game Start Banner

**New Component**: `GameStartBannerUI.cs`

```csharp
public class GameStartBannerUI : MonoBehaviour
{
    [SerializeField] private GameObject bannerPanel;
    [SerializeField] private TextMeshProUGUI winnerText;
    [SerializeField] private float displayDuration = 3f;
    
    public void ShowBanner(FateSide winner)
    {
        // Show banner with winner text
        // Auto-hide after displayDuration
    }
}
```

## Test Execution

### Running Tests

1. Open Unity Test Runner (Window > General > Test Runner)
2. Select **PlayMode** tab
3. Find `CoinTossFlowPlayModeTests` test class
4. Run all tests or individual tests
5. Review console logs for detailed diagnostics

### Expected Results

- **Current Implementation**: Some tests will pass, others will note missing features
- **After New Flow Implementation**: All tests should pass

### Test Dependencies

Tests require:
- `BattleScreenMultiplayer` scene in Build Settings
- CoinTossManager singleton
- CoinTossUI component
- GameManager singleton
- FateFlowController singleton
- NewDeckManager and NewDeckManagerOpp
- Proper scene initialization

## Validation Checklist

After implementing new flow, verify:

- [ ] Selection window appears on scene start
- [ ] Heads/Tails buttons are clickable
- [ ] Countdown timer displays and counts down
- [ ] Selection triggers coin toss immediately
- [ ] Timeout auto-selects after 10 seconds
- [ ] Coin toss animation plays correctly
- [ ] Winner banner appears with correct text
- [ ] Banner auto-closes after 2-3 seconds
- [ ] Both players receive opening hands
- [ ] Winner gets first turn
- [ ] Game transitions to normal gameplay

## Notes

- Tests use reflection to access private fields (headsLabel, tailsLabel, etc.) for validation
- Some tests simulate behavior that doesn't exist yet (selection buttons, timeout)
- Tests are designed to guide implementation of the new flow
- All tests follow Unity Test Runner conventions
- Tests include proper waits for animations and async operations

## Next Steps

1. Implement `CoinTossSelectionUI` component
2. Integrate selection window into coin toss flow
3. Create `GameStartBannerUI` component
4. Update `CoinTossUI` to use new flow
5. Re-run tests to validate implementation
6. Fix any failing tests based on actual behavior

