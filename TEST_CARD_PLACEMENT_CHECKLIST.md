# Card Placement Testing Checklist

## 🎯 Testing Goal
Test drag-and-drop functionality: drag a player card from hand and place it on the board.

## ✅ Pre-Test Requirements

1. **Unity Editor is open** ✅
2. **Scene is loaded** ✅
3. **Cards are initialized** ✅ (from logs: all cards verify in Start())
4. **CardDropArea1 components exist in scene** - Need to verify

## 🧪 Testing Steps

### Step 1: Enter Play Mode
- Press **Play** button in Unity Editor
- Wait for cards to initialize (should see "Card verified in Start()" logs)

### Step 2: Verify Cards in Hand
- Check bottom of screen - should see 5 player cards (Flame Wanderer, Flame Shepard, Flame Dreamer, Flame Witch, Flame Weaver)
- Cards should be visible in hand UI

### Step 3: Attempt to Drag a Card
- **Click and hold** on a player card (e.g., "Flame Wanderer")
- **Drag** the card toward the board
- **Check Console** - should see:
  ```
  [NewCardUI] OnBeginDrag: Flame Wanderer, allowDrag: True, card bound: True
  [NewCardUI] Starting drag for card: Flame Wanderer
  ```

### Step 4: Drop Card on Board
- **Drag card** over a board tile (CardDropArea1)
- **Release mouse button** to drop
- **Check Console** - should see:
  ```
  [NewCardUI] OnEndDrag: Card 'Flame Wanderer' dropped...
  [NewCardUI] Found CardDropArea1 via UI raycast: [TileName]
  [NewCardUI] PlaceCardOnBoard: All checks passed...
  [NewCardUI] PlaceCardOnBoard: Card 'Flame Wanderer' placement complete!
  ```

### Step 5: Verify Placement
- Card should be placed on the board tile
- Card should be removed from hand
- Card should snap to tile position
- Console should show card played logs

## 🔍 What to Watch For

### ✅ Success Indicators
- `[NewCardUI] OnBeginDrag` appears when dragging starts
- `[NewCardUI] OnEndDrag` appears when releasing
- `[NewCardUI] PlaceCardOnBoard` appears when dropping on tile
- Card visually moves from hand to board
- Card is removed from hand UI

### ⚠️ Potential Issues

#### Issue 1: No OnBeginDrag Log
- **Cause**: Card reference is null
- **Check**: Look for "Card is null" warnings in console
- **Fix**: Card initialization issue

#### Issue 2: No OnEndDrag Log
- **Cause**: `isDragging` was never set to true
- **Check**: OnBeginDrag didn't complete successfully
- **Fix**: Check card reference recovery in OnBeginDrag

#### Issue 3: No PlaceCardOnBoard Log
- **Cause**: CardDropArea1 not found or not in drop position
- **Check**: Ensure CardDropArea1 components exist in scene
- **Fix**: Verify drop areas are positioned correctly

#### Issue 4: Card Doesn't Move
- **Cause**: EventSystem missing or drag handlers not working
- **Check**: Console for EventSystem warnings
- **Fix**: HUDSetup should create EventSystem automatically

## 📋 Console Log Sequence (Expected)

```
[NewCardUI] OnBeginDrag: Flame Wanderer, allowDrag: True, card bound: True
[NewCardUI] Starting drag for card: Flame Wanderer
... (dragging) ...
[NewCardUI] OnEndDrag: Card 'Flame Wanderer' dropped...
[NewCardUI] Found CardDropArea1 via UI raycast: [TileName]
[NewCardUI] PlaceCardOnBoard: All checks passed...
CardFactory: Created board card 'Flame Wanderer' at position...
[NewCardUI] PlaceCardOnBoard: Card 'Flame Wanderer' placement complete!
```

## 🚀 Quick Test

1. **Press Play** in Unity
2. **Drag** any player card from hand
3. **Drop** on any board tile
4. **Check Console** for logs above
5. **Verify** card is placed and removed from hand

---

**Ready to test!** Let me know what logs appear when you try dragging a card.

