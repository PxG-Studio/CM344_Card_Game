# Score Calculation Update - Spaces Controlled

## Issue Identified

The score calculation was counting **captured cards** on the board, but it should count **spaces controlled** by each player out of 16 total spaces.

## Fix Applied

### ✅ Updated ScoreManager.RecalculateScores()

**File**: `Assets/Scripts/Current/Univ-Managers/ScoreManager.cs`

**Changed Logic**:
- **OLD**: Counted captured cards (CardMover/CardMoverOpp with capture color)
- **NEW**: Counts spaces controlled by checking all 16 CardDropArea1 instances

**New Implementation**:
1. Find all `CardDropArea1` instances in the scene (should be 16 total)
2. For each `CardDropArea1`, check if it's occupied (`IsOccupied`)
3. Get the `occupyingCard` GameObject
4. Determine who controls the space based on the card's capture color/owner:
   - **Player controls** if card has orange capture color OR is a CardMover (not captured)
   - **Opponent controls** if card has green capture color OR is a CardMoverOpp (not captured)
5. Count spaces: Player score = spaces controlled by player, Opponent score = spaces controlled by opponent
6. Empty spaces are not counted for either player

### Score Calculation Logic

```csharp
// Find all 16 CardDropArea1 instances
CardDropArea1[] allDropAreas = FindObjectsOfType<CardDropArea1>();

foreach (CardDropArea1 dropArea in allDropAreas)
{
    if (!dropArea.IsOccupied) continue; // Empty space
    
    GameObject occupyingCard = GetOccupyingCard(dropArea);
    bool isPlayerControlled = IsPlayerCard(occupyingCard);
    
    if (isPlayerControlled)
        playerScore++; // Player controls this space
    else
        opponentScore++; // Opponent controls this space
}
```

### Who Controls a Space?

A space is controlled by whoever owns the card on that space:

1. **Captured Cards**: 
   - Orange border = Player controls
   - Green border = Opponent controls

2. **Uncaptured Cards**:
   - `CardMover` component = Player controls (original owner)
   - `CardMoverOpp` component = Opponent controls (original owner)

3. **Empty Spaces**: No points for either player

## Expected Behavior

### Score Calculation
- ✅ **Player Score**: Number of spaces (out of 16) controlled by player
- ✅ **Opponent Score**: Number of spaces (out of 16) controlled by opponent
- ✅ **Total**: Player score + Opponent score = occupied spaces (should be 10 when all cards played)
- ✅ **Empty Spaces**: 16 - (Player + Opponent) = 6 empty spaces when all cards played

### Example Scores
- **Player: 8, Opponent: 2** → Player controls 8/16 spaces, wins
- **Player: 5, Opponent: 5** → Tie (both control 5 spaces each)
- **Player: 10, Opponent: 0** → Dominant victory (player controls all 10 played cards)

### Winner Determination
- ✅ **Higher score wins** (controls more spaces)
- ✅ **Tie** if scores are equal
- ✅ Winner is determined by **scores from P1Panel and P2Panel**

## Logging

Enhanced logging shows:
- Number of CardDropArea1 instances found
- Player spaces controlled (X/16)
- Opponent spaces controlled (Y/16)
- Empty spaces (Z/16)
- Total: X + Y + Z = 16

Example log:
```
[ScoreManager] Found 16 CardDropArea1 instances. Calculating scores based on spaces controlled...
[ScoreManager] Recalculated scores based on 16 spaces: Player controls 8/16, Opponent controls 2/16, Empty: 6/16
```

## Testing

### Play a Complete Game:
1. Start Play Mode
2. Play all 5 player cards
3. Opponent plays all 5 opponent cards
4. After all cards are played, `RecalculateScores()` is called
5. Check Console logs for:
   - `[ScoreManager] Found 16 CardDropArea1 instances...`
   - `[ScoreManager] Recalculated scores based on 16 spaces: Player controls X/16, Opponent controls Y/16...`
6. Verify scores match actual board control:
   - Count orange-bordered cards → should match Player score
   - Count green-bordered cards → should match Opponent score
   - Total should be 10 (cards played) + 6 empty = 16 total

### Expected Results:
- Player score + Opponent score = 10 (when all cards played)
- Empty spaces = 6 (when all cards played)
- Winner determined by higher score (more spaces controlled)

---

*Fix Date: Score Calculation Updated to Count Spaces Controlled*
*Status: Ready for Testing*

