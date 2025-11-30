# Coin Toss Selection System Update

## Overview

Updated the coin toss system to allow players to select heads or tails before the coin is flipped. The starting player is determined by whether the flip result matches the player's selection.

## How It Works

1. **Player Selection**: A player (typically Player 1) selects either Heads or Tails
2. **Coin Flip**: The coin is randomly flipped (heads or tails)
3. **Starting Player Determination**:
   - If the flip result **matches** the player's selection → That player goes first
   - If the flip result **doesn't match** the player's selection → The other player goes first

## Code Changes

### CoinTossManager.cs
- Added `playerSelection` field to track heads/tails selection
- Added `selectedByPlayer` field to track which player made the selection
- Added `SetPlayerSelection(bool selectHeads, FateSide selectingPlayer)` method
- Updated `PerformCoinToss()` to determine starting player based on selection match
- Added `GetFlipResult()` method to get the actual flip result (heads/tails)
- Added `HasSelection` property to check if selection has been made
- Updated `ResetCoinToss()` to clear selection state

### CoinTossUI.cs
- Added `headsButton` and `tailsButton` serialized fields for selection buttons
- Added `selectionPanel` and `selectionPromptText` for selection UI
- Added `waitingForSelection` state tracking
- Added `OnSelectionMade(bool selectHeads)` method to handle button clicks
- Updated `StartCoinTossAnimation()` to check for selection before starting animation
- Updated result display to show both flip result and starting player
- Updated `Show()` method to reset selection UI

### Test Files Updated
- `BattleScreenMultiplayerPlayModeTests.cs` - Added selection before PerformCoinToss()
- `CoinTossFlowPlayModeTests.cs` - Added selection before PerformCoinToss() in multiple tests
- `RegressionTests.cs` - Added selection before PerformCoinToss()

## UI Requirements

The CoinTossUI prefab needs to have:
- **headsButton**: Button for selecting heads
- **tailsButton**: Button for selecting tails
- **selectionPanel**: GameObject containing the selection UI (optional, can be the main panel)
- **selectionPromptText**: TextMeshProUGUI prompting player to select (optional)

These should be assigned in the Unity Inspector on the CoinTossUI component.

## Test Updates

All tests that call `PerformCoinToss()` now:
1. First call `SetPlayerSelection(true/false, FateSide.Player)` to set the selection
2. Then call `PerformCoinToss()` to flip and determine starting player

Example:
```csharp
coinTossManager.SetPlayerSelection(true, FateSide.Player); // Player 1 selects heads
FateSide startingPlayer = coinTossManager.PerformCoinToss();
```

## Backward Compatibility

The system includes a fallback: if `PerformCoinToss()` is called without a selection, it will:
- Log a warning
- Use a random result (maintains old behavior for compatibility)
- This ensures existing code doesn't break, but new code should use selection

## Next Steps

1. **Assign UI Elements in Unity**: 
   - Open the CoinTossUI prefab or scene object
   - Assign the `headsButton` and `tailsButton` fields in the Inspector
   - Optionally assign `selectionPanel` and `selectionPromptText`

2. **Test in Unity**:
   - Run the game and verify the selection buttons appear
   - Verify clicking a button triggers the coin flip animation
   - Verify the starting player is determined correctly

3. **Run Tests**:
   - All tests should now pass with the updated coin toss logic
   - Tests now properly set player selection before flipping

## Notes

- Currently, Player 1 always makes the selection (hardcoded in `OnSelectionMade`)
- For true multiplayer, this could be extended to allow either player to select
- The selection UI is shown before the coin animation starts
- The coin animation only starts after a selection is made

