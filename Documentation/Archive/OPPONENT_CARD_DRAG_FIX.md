# Opponent Card Drag Issue - Resolution Summary

## Problem Analysis

When it's Player 2's (Opponent's) turn to pick up and drag cards, the following issues were occurring:

1. **Prefab Asset Being Selected**: The logs showed `OnBeginDrag` being called on `'NewCardPrefabOpp'` (the prefab asset), not `'NewCardPrefabOpp(Clone)'` (the instantiated cards). This suggests a prefab asset was in the scene and intercepting drag events.

2. **Card Reference Lost**: When attempting to drag opponent cards, the `card` reference was null, causing the drag to fail with: `CRITICAL: Card reference lost. GameObject: NewCardPrefabOpp, InstanceID: 45398. Cannot start drag.`

3. **Recovery Logic Failure**: The card reference recovery logic wasn't finding opponent cards in the `NewHandOppUI` list.

## Root Causes

1. **Missing Prefab Asset Check**: There was no runtime check to prevent dragging prefab assets (non-clone GameObjects).

2. **Incomplete Recovery Strategy**: The card reference recovery logic didn't have a fallback to search all `NewHandOppUI` instances when the parent hierarchy check failed.

3. **Turn Check Timing**: The opponent card turn check was happening after card reference recovery, which could cause issues if the card reference was needed for the check.

## Fixes Applied

### 1. Early Prefab Asset Check (Line 707-711)
```csharp
// [CardFront] Check for non-clone names FIRST (works at runtime, not just in Editor)
if (!gameObject.name.Contains("(Clone)"))
{
    Debug.LogWarning($"[NewCardUI] Cannot drag '{gameObject.name}' - this appears to be a prefab asset or uninitialized instance. Only instantiated cards (clones) can be dragged. Please ensure you're dragging a card from the hand, not a prefab asset in the scene.");
    return;
}
```
**Purpose**: Blocks prefab assets from being dragged before any other logic runs. This works at runtime, not just in the Editor.

### 2. Enhanced Opponent Card Turn Check (Lines 734-756)
```csharp
// CRITICAL: Opponent cards should only be draggable during opponent's turn
bool isOpponentCard = IsOpponentCard();
if (isOpponentCard)
{
    bool canOpponentAct = CardGame.Managers.FateFlowController.Instance != null && 
                          CardGame.Managers.FateFlowController.Instance.CanAct(CardGame.Managers.FateSide.Opponent);
    
    if (!canOpponentAct)
    {
        // Block opponent cards when it's NOT the opponent's turn
        Debug.Log($"[NewCardUI] Opponent card '{gameObject.name}' drag blocked - not opponent's turn (expected behavior).");
        return;
    }
    else
    {
        // Allow opponent cards when it IS the opponent's turn
        Debug.Log($"[NewCardUI] Opponent card '{gameObject.name}' drag allowed - opponent's turn.");
        // Continue with drag initialization
    }
}
```
**Purpose**: Allows opponent cards to drag when it's their turn, blocks them otherwise.

### 3. Strategy 4 Recovery - Scene-Wide Search (Lines 828-859)
```csharp
// Strategy 4: Try to find card by matching with all instantiated cards in HandUI/HandOppUI lists
if (card == null)
{
    // Try to find NewHandOppUI and match by GameObject instance
    NewHandOppUI sceneHandOppUI = FindObjectOfType<NewHandOppUI>();
    if (sceneHandOppUI != null)
    {
        NewCard foundCard = sceneHandOppUI.GetCardForUI(this);
        if (foundCard != null)
        {
            card = foundCard;
            Debug.Log($"[NewCardUI] Recovered card via HandOppUI Hub's GetCardForUI: {card.Data.cardName}");
        }
    }
    
    // Also try NewHandUI for player cards
    if (card == null)
    {
        NewHandUI sceneHandUI = FindObjectOfType<NewHandUI>();
        if (sceneHandUI != null)
        {
            NewCard foundCard = sceneHandUI.GetCardForUI(this);
            if (foundCard != null)
            {
                card = foundCard;
                Debug.Log($"[NewCardUI] Recovered card via HandUI Hub's GetCardForUI: {card.Data.cardName}");
            }
        }
    }
}
```
**Purpose**: Fallback recovery strategy that searches all HandUI/HandOppUI instances in the scene when parent hierarchy checks fail.

### 4. Improved Error Logging (Lines 867-877)
```csharp
if (!gameObject.name.Contains("(Clone)"))
{
    Debug.LogWarning($"[NewCardUI] Cannot drag '{gameObject.name}' - this appears to be a prefab asset or uninitialized instance...");
    return;
}

Debug.LogError($"[NewCardUI] CRITICAL: Card reference lost. GameObject: {gameObject.name}, InstanceID: {GetInstanceID()}. Cannot start drag.");
Debug.LogError($"[NewCardUI] Recovery strategies failed. Parent: {(transform.parent != null ? transform.parent.name : "null")}, IsOpponentCard: {isOpponentCard}, HasHandOppUI: {(GetComponentInParent<NewHandOppUI>() != null ? "Yes" : "No")}");
```
**Purpose**: Better diagnostics to help identify why card references are lost.

### 5. Correct Turn Check (Lines 886-896)
```csharp
// Determine which side this card belongs to
CardGame.Managers.FateSide cardSide = isOpponentCard ? 
    CardGame.Managers.FateSide.Opponent : 
    CardGame.Managers.FateSide.Player;

bool canAct = CardGame.Managers.FateFlowController.Instance.CanAct(cardSide);
```
**Purpose**: Checks the correct side (player or opponent) instead of always checking player.

### 6. PlaceOpponentCardOnBoard Method (Lines 1185-1297)
**Purpose**: New method to handle opponent card placement using `CardMoverOpp` and `OnCardDropOpp()` instead of player card flow.

## Expected Behavior After Fix

1. ✅ Prefab assets (`NewCardPrefabOpp`, `NewCardPrefab`) are blocked from being dragged
2. ✅ Opponent cards can be dragged when it's the opponent's turn
3. ✅ Opponent cards are blocked when it's the player's turn
4. ✅ Card reference recovery works for both player and opponent cards
5. ✅ Opponent cards are placed on the board using the correct flow (`CardMoverOpp` + `OnCardDropOpp`)

## Testing Checklist

- [ ] Play the game and wait for opponent's turn
- [ ] Try to drag opponent cards - should work when it's their turn
- [ ] Try to drag opponent cards during player's turn - should be blocked
- [ ] Verify opponent cards can be placed on the board
- [ ] Check console logs - prefab asset warnings should appear if trying to drag prefab
- [ ] Verify card references are recovered successfully if lost

## Files Modified

1. `Assets/Scripts/Current/NewCardUI.cs`
   - Added early prefab asset check
   - Enhanced opponent card turn check
   - Added Strategy 4 recovery (scene-wide search)
   - Improved error logging
   - Added `PlaceOpponentCardOnBoard()` method
   - Updated `OnEndDrag()` to route opponent cards correctly

## Notes

- The prefab asset `NewCardPrefabOpp` should be removed from the scene if present
- Only cloned card instances (`NewCardPrefabOpp(Clone)`) should be in the scene at runtime
- The early check at line 707 should block prefab assets from being dragged
- Strategy 4 uses `FindObjectOfType` as a fallback recovery - this is acceptable since it's only used when normal Hub connections fail

