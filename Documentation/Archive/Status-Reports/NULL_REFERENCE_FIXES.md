# NullReferenceException Fixes

## Issues Resolved

### 1. CoinTossManager NullReferenceException in NewCardSystemTester and NewCardSystemOpposition

**Problem:**
- `NullReferenceException` at line 59 in `WaitForCoinTossThenDrawCards()` coroutines
- `CoinTossManager.Instance` could become null between the initial check and the `IsComplete` property access
- Race condition where the instance might be destroyed or not yet initialized

**Solution:**
- Updated both `NewCardSystemTester.cs` and `NewCardSystemOpposition.cs` to:
  - Store `CoinTossManager.Instance` in a local variable after waiting for initialization
  - Add proper null checks before accessing `IsComplete`
  - Re-check the instance in the wait loop in case it gets destroyed
  - Increase timeout from 10s to 15s to account for the new coin toss selection step
  - Add graceful fallback if CoinTossManager is never found

**Files Modified:**
- `Assets/Scripts/Current/Player 1 scripts/NewCardSystemTester.cs`
- `Assets/Scripts/Current/Opposition Scripts/NewCardSystemOpposition.cs`

### 2. CardMoverOpp Missing Collider2D Errors

**Problem:**
- `NullReferenceException` when `AttemptDrop()` is called on cards without colliders
- Error: `[CardMoverOpp] AttemptDrop failed - no Collider2D on 'Earth Historian'`
- Cards created in tests or at runtime might not have colliders properly initialized

**Solution:**
- Added `EnsureCollider()` method to `CardMoverOpp` that:
  - Checks for existing collider on the GameObject
  - Checks for collider in children
  - Creates a `BoxCollider2D` if none exists (for test scenarios)
  - Ensures collider is properly configured (isTrigger = true, enabled = true)
- Added `EnsureCollider()` calls in:
  - `Start()` method (replaces direct `GetComponent<Collider2D>()`)
  - `OnMouseDown()` method (before using collider)
  - `AttemptDrop()` method (before using collider)
  - `AutomationAttemptDrop()` method (before attempting drop)

**Files Modified:**
- `Assets/Scripts/Current/Opposition Scripts/CardMoverOpp.cs`

## Testing Recommendations

1. Run PlayMode tests to verify:
   - `P2_CardInteraction_DebugTests.Player2_CanDropOnValidTile()` - should no longer show collider errors
   - `P2_CardInteraction_DebugTests.Player2_Drop_RejectedOnInvalidTile()` - should no longer show collider errors
   - Coin toss flow tests should complete without NullReferenceExceptions

2. Verify in Unity Editor:
   - Cards spawn correctly with colliders
   - Coin toss selection UI appears and works correctly
   - Card drawing happens after coin toss completes

## Notes

- The `EnsureCollider()` method is defensive and will create a collider if one doesn't exist, which is useful for test scenarios but should not be necessary in production if cards are properly set up
- The coin toss wait logic now accounts for the new selection step, which may take additional time
- All fixes maintain backward compatibility with existing code

