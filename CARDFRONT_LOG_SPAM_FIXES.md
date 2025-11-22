# CardFront Log Spam Fixes

## Issues Identified and Fixed

### ✅ Issue 1: "Turn tracking cleared" Spam (13+ logs per card drop)

**Problem:**
- Every `CardDropArea1` instance (one per board tile) subscribes to `OnFateChanged`
- When `AdvanceFateFlow()` is called, all instances receive the event
- Each instance logs "Turn tracking cleared" → 13+ duplicate logs

**Fix:**
- Added static `lastClearedFate` tracking in `CardDropArea1`
- `ClearTurnTracking()` now only logs once per fate change
- All instances still clear their tracking, but only one logs

**Code Change:**
```csharp
// [CardFront] Static tracking to prevent duplicate logs
private static FateSide lastClearedFate = (FateSide)(-1);

private void ClearTurnTracking(FateSide currentFate)
{
    cardsPlayedThisTurn.Clear();
    
    // Only log once per fate change, not once per CardDropArea1 instance
    if (debugBattles && lastClearedFate != currentFate)
    {
        lastClearedFate = currentFate;
        Debug.Log($"[CardDropArea1] Turn tracking cleared for {currentFate}...");
    }
}
```

---

### ✅ Issue 2: Opponent Card Drag Warning Spam

**Problem:**
- User attempts to drag opponent cards (expected behavior)
- Each attempt logs a warning → console spam
- This is expected behavior, not an error

**Fix:**
- Changed `Debug.LogWarning()` to `Debug.Log()` for opponent card drag blocks
- Reduced log verbosity (removed "Cannot drag" language, just states it's blocked)

**Code Change:**
```csharp
// Before: Debug.LogWarning(...)
// After: Debug.Log(...)
Debug.Log($"[NewCardUI] Opponent card '{gameObject.name}' drag blocked (expected behavior).");
```

---

### ✅ Issue 3: "Card is null in Start()" Warnings for Uninitialized Prefabs

**Problem:**
- Prefab instances (`NewCardPrefab`, `NewCardPrefabOpp`) placed directly in scene
- These are expected to be uninitialized until `Initialize()` is called
- Warnings spam console on every `Start()`

**Fix:**
- Added check for uninitialized prefab instances
- Only warns if prefab instance is in a hand container (should have been initialized)
- Prefab assets and standalone prefab instances don't warn

**Code Change:**
```csharp
#if UNITY_EDITOR
// Check if this is an uninitialized prefab instance
bool isPrefabInstance = UnityEditor.PrefabUtility.IsPartOfPrefabInstance(gameObject);
if (isPrefabInstance && (gameObject.name == "NewCardPrefab" || gameObject.name == "NewCardPrefabOpp"))
{
    // Only warn if it's in a hand container (should have been initialized)
    bool inHandContainer = parent != null && 
        (parent.GetComponent<CardGame.UI.NewHandUI>() != null || 
         parent.GetComponent<CardGame.UI.NewHandOppUI>() != null);
    
    if (!inHandContainer)
    {
        return; // Don't warn for standalone prefab instances
    }
}
#endif
```

---

## Card Placement Status

### ✅ **Card Placement IS Working**

**Evidence from Logs:**
- `Card Flame Shepard played on board - keeping GameObject`
- `Card Earth Dreamer played from drop area and placed on board`
- `Board occupancy: 1/16 spaces filled` → `2/16 spaces filled`

**Drag Systems:**
1. **`CardMover` / `CardMoverOpp`** (2D Physics-based) - ✅ **WORKING**
   - Uses `OnMouseDown()`, `OnMouseDrag()`, `OnMouseUp()`
   - Detects drops via `CardDropArea1.OnCardDrop()`
   - This is the system currently being used

2. **`NewCardUI`** (UI EventSystem-based) - ⚠️ **Not Being Used**
   - Uses `IBeginDragHandler`, `IDragHandler`, `IEndDragHandler`
   - Requires UI raycasting and EventSystem
   - Player cards aren't triggering `OnBeginDrag` (only opponent cards, which are blocked)

**Recommendation:**
- Card placement via `CardMover` is working correctly
- If you want to use UI drag (`NewCardUI`), investigate why player cards aren't receiving drag events
- For now, the physics-based drag system is sufficient

---

## Summary

**Fixed:**
- ✅ Reduced "Turn tracking cleared" from 13+ logs to 1 log per fate change
- ✅ Reduced opponent card drag warnings (changed to info logs)
- ✅ Reduced uninitialized prefab warnings (only warn when appropriate)

**Status:**
- ✅ Card placement is working via `CardMover` system
- ⚠️ UI drag system (`NewCardUI`) not being used, but not required

**Result:**
- Console logs are now much cleaner
- Only meaningful warnings remain
- Card placement functionality is intact

