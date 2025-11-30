# Chain Capture and Scoring System - Setup Guide

## Overview
This guide explains how to set up the chain capture and scoring system in Unity after the code has been implemented.

## New Files Created
1. **ScoreManager.cs** - `Assets/Scripts/Managers/ScoreManager.cs`
2. **GameEndManager.cs** - `Assets/Scripts/Managers/GameEndManager.cs`

## Modified Files
1. **CardDropArea1.cs** - Enhanced with chain capture, scoring, and board tracking
2. **GameManager.cs** - Added reset calls for new managers

## Unity Scene Setup

### Step 1: Add ScoreManager to Scene
1. Create a new empty GameObject in your scene
2. Name it "ScoreManager"
3. Add the `ScoreManager` component to it
4. The ScoreManager uses singleton pattern and will auto-register

### Step 2: Add GameEndManager to Scene
1. Create a new empty GameObject in your scene
2. Name it "GameEndManager"
3. Add the `GameEndManager` component to it
4. Configure settings (optional):
   - **Delay Before Game End**: 0.5 seconds (default)
   - **Max Wait Time For Chains**: 10 seconds (default)

### Step 3: Verify CardDropArea1
The CardDropArea1 component will automatically find ScoreManager and GameEndManager using `FindObjectOfType`. Ensure:
- CardDropArea1 components exist in your scene
- They have references to NewDeckManager and NewDeckManagerOpp (or they'll auto-find)

### Step 4: Verify GameManager
GameManager should already exist in your scene. It will automatically:
- Reset ScoreManager when starting a new game
- Reset GameEndManager when starting a new game
- Handle game state transitions for Victory/Defeat

## How It Works

### Chain Capture Flow
1. Player places a card on the board
2. Card checks adjacent cards for battles
3. When a card is captured, it immediately checks for additional captures
4. Chain continues until no more captures are possible
5. Cards played this turn cannot be captured (same-turn protection)

### Scoring Flow
1. When a card is captured, ScoreManager.AddScore() is called
2. Score increments for the capturing player
3. ScoreManager fires OnScoreChanged event for UI updates (optional)
4. At game end, scores are recalculated from all captured cards on the board

### End Game Flow
1. After each card is placed, board occupancy is checked
2. When the last unoccupied space is filled, game end is triggered
3. GameEndManager waits for all chain captures to complete
4. Final scores are calculated
5. Winner is determined (higher score wins)
6. Game state changes to Victory or Defeat

## Testing Checklist

**Quick Setup Verification:**
- Add `ChainCaptureTester` component to a GameObject in your scene for automated verification
- See `CHAIN_CAPTURE_TESTING_GUIDE.md` for detailed testing instructions

**Manual Testing Checklist:**
- [ ] ScoreManager GameObject exists in scene
- [ ] GameEndManager GameObject exists in scene
- [ ] CardDropArea1 can find ScoreManager (check console for warnings)
- [ ] CardDropArea1 can find GameEndManager (check console for warnings)
- [ ] Test chain capture: Place a card that captures another, verify it can capture adjacent cards
- [ ] Test same-turn protection: Place a card, verify it cannot be captured immediately
- [ ] Test scoring: Capture a card, verify score increases
- [ ] Test board occupancy: Fill the board, verify game end triggers
- [ ] Test game end: Wait for chains to complete, verify winner is determined

**For detailed step-by-step testing instructions, see:** `CHAIN_CAPTURE_TESTING_GUIDE.md`

## Debug Settings

CardDropArea1 has a `debugBattles` flag in the Inspector that enables detailed logging:
- Set to `true` to see battle detection logs
- Set to `false` to reduce console spam

## Known Limitations

1. **Board Detection**: Board occupancy uses distance checks (0.5 units) to determine if a card occupies a space. Ensure your CardDropArea positions match card positions when dropped.

2. **Animation Timing**: Chain captures wait 1.1 seconds for flip animations. If you change CardFlipAnimation duration, you may need to adjust the wait time in CardDropArea1.

3. **Multiple CardDropArea1 Instances**: Each instance independently tracks chain captures. If you have multiple CardDropArea1 components, they all track the same global state (cards on board).

## Troubleshooting

### Scores not updating
- Check if ScoreManager exists in scene
- Check console for "ScoreManager not found" warnings
- Verify FlipCardGameObject is being called with correct capture color

### Game end not triggering
- Check if GameEndManager exists in scene
- Verify CheckBoardOccupancy is being called
- Check console logs for "Board is full" message
- Ensure CardDropArea1 components are properly positioned

### Chain captures not working
- Verify cards are being captured (check capture color)
- Check if cards were played this turn (same-turn protection)
- Enable debugBattles to see detailed logs
- Verify CheckChainCapture is being called after flip completes

### Infinite loops in chain captures
- The system uses `cardsInCurrentChain` to prevent infinite loops
- If you see excessive chain captures, check that cards are being removed from the tracking set

## Future Enhancements

Potential improvements:
1. UI display for scores (listen to ScoreManager.OnScoreChanged)
2. Visual indicator when chain capture is in progress
3. End game screen showing final scores
4. Animation timing configuration per-card instead of hardcoded
5. Board size configuration instead of auto-detection
