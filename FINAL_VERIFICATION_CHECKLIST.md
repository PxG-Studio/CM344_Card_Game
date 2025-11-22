# CardFront Final Verification Checklist

## PRE-PLAY MODE VERIFICATION

### Step 1: Compile Scripts
- [ ] Open Unity Editor
- [ ] Wait for scripts to compile
- [ ] Check console - no compilation errors
- [ ] ✅ **Expected**: No CS0106 errors
- [ ] ✅ **Expected**: No CS0246 errors (namespace errors)
- [ ] ✅ **Expected**: All scripts compile successfully

### Step 2: Run Prefab Validator
- [ ] Go to `Card Game > Validate Card Prefabs`
- [ ] Click "Scan All Card Prefabs"
- [ ] Review validation results
- [ ] Click "Fix All Issues" if any errors found
- [ ] ✅ **Expected**: All prefabs pass validation or issues are fixed

### Step 3: Fix Meta Files (if needed)
- [ ] Go to `Card Game > Fix Orphaned Meta Files`
- [ ] Click "Scan for Orphaned Meta Files"
- [ ] Review orphaned .meta files
- [ ] Delete orphaned .meta files if found
- [ ] ✅ **Expected**: No orphaned .meta files remain

### Step 4: Fix External Editor Path (if needed)
- [ ] Go to `Edit > Preferences > External Tools`
- [ ] Set "External Script Editor" to your preferred editor (VS 2022, VS Code, or Rider)
- [ ] Click "Apply"
- [ ] Test by double-clicking a script file
- [ ] ✅ **Expected**: Script opens in configured editor

### Step 5: Verify Code Files
- [ ] Verify `CardFactory.cs` exists in `Assets/Scripts/Current/`
- [ ] Verify `CardFactory.cs` has `using CardGame.UI;`
- [ ] Verify `NewHandUI.cs` uses `CardFactory.CreateCardUI()`
- [ ] Verify `NewHandOppUI.cs` uses `CardFactory.CreateCardUI()`
- [ ] Verify `NewCardUI.cs` has `IsOpponentCard()` method
- [ ] ✅ **Expected**: All files exist and use CardFactory

---

## RUNTIME VERIFICATION TESTS

### Test 1: DeckManager Draws Cards

**Steps**:
1. Start Play Mode
2. Wait for cards to be drawn
3. Check console logs
4. Check hierarchy for card GameObjects

**Expected Results**:
- ✅ **5 cards** drawn for player
- ✅ **5 cards** drawn for opponent
- ✅ Console shows: "CardFactory: Created and initialized card UI '[CardName]'"
- ✅ Console shows: "NewCardUI.Initialize: Successfully initialized '[CardName]'"
- ✅ Console shows: "NewHandUI.AddCardToHand: Successfully added card '[CardName]'"
- ❌ **Should NOT see**: "Card is null in Start()" warnings
- ❌ **Should NOT see**: "Failed to initialize card UI" errors

**If Failures**:
- Check that CardFactory is being used
- Check that prefab references are assigned in NewHandUI
- Check console for specific error messages

---

### Test 2: NewCardUI Binds Correctly

**Steps**:
1. In Play Mode, check hierarchy
2. Find player cards in hand (under NewHandUI container)
3. Select a player card
4. Check Inspector → NewCardUI component
5. Verify "Card" field
6. Repeat for opponent cards

**Expected Results - Player Cards**:
- ✅ "Card" field shows card instance (not null)
- ✅ Card Data shows card name, stats, description
- ✅ GameObject name matches card name (e.g., "Flame Wanderer")
- ✅ All stat text fields are populated

**Expected Results - Opponent Cards**:
- ✅ Same as player cards
- ✅ Card bound correctly
- ✅ Visuals displayed correctly

**If Failures**:
- Check console for "Card is null" warnings
- Verify CardFactory is being called
- Check that Initialize() is being called before Start()

---

### Test 3: Drag-and-Drop Behavior

**Player Card Test**:
1. Click and hold a player card in hand
2. Drag mouse cursor
3. Release mouse button over board

**Expected Results**:
- ✅ Card follows mouse cursor during drag
- ✅ Card becomes semi-transparent (alpha ~0.8) during drag
- ✅ No "Cannot drag" errors in console
- ✅ No "Card is null" errors during drag
- ✅ Card can be placed on board drop zone
- ✅ Card is removed from hand after placement
- ✅ Board card appears with CardMover component

**Opponent Card Test**:
1. Try to click and drag an opponent card in hand

**Expected Results**:
- ✅ Drag is blocked immediately
- ✅ Console shows: "OnBeginDrag: Cannot drag opponent card '[CardName]'"
- ✅ Card does not move
- ✅ Card does not become semi-transparent
- ✅ No errors occur

**If Failures**:
- Check that EventSystem exists in scene
- Check that CanvasGroup is present on cards
- Verify IsOpponentCard() method is working
- Check console for specific drag errors

---

### Test 4: Missing Script Warnings

**Steps**:
1. Start Play Mode
2. Monitor console throughout entire play session
3. Check for any missing script warnings

**Expected Results**:
- ❌ **Should NOT see**: "The referenced script on this Behaviour (Game Object 'CardBackVisual') is missing!"
- ❌ **Should NOT see**: Any missing script warnings
- ✅ Console shows only expected game logs

**If Failures**:
- Run `Card Game > Validate Card Prefabs` → "Fix All Issues"
- Manually remove missing scripts from prefabs
- Check CardBackVisual GameObject in prefab

---

### Test 5: MCP Unity Server Stability

**Steps**:
1. Start Play Mode
2. Check console for MCP Unity logs
3. Compile scripts (cause domain reload)
4. Monitor server restart behavior

**Expected Results**:
- ✅ "WebSocket server started successfully on localhost:8090" (once on start)
- ✅ Server restarts on domain reload (this is normal)
- ✅ No errors during server restart
- ❌ **Should NOT see**: Multiple servers starting simultaneously
- ❌ **Should NOT see**: Server errors or connection failures

**Note**: Server restart on domain reload is normal and expected behavior.

**If Failures**:
- Check that FixMCPUnityReload.cs is present
- Server restart is normal - not an error

---

### Test 6: HUDSetup Execution

**Steps**:
1. Start Play Mode
2. Check console for HUDSetup logs
3. Verify managers are created
4. Check hierarchy for duplicate managers

**Expected Results**:
- ✅ "HUDSetup: HUD successfully configured!" (appears once)
- ✅ "HUDSetup: Created GameManager" (once)
- ✅ "HUDSetup: Created ScoreManager" (once)
- ✅ "HUDSetup: Created GameEndManager" (once)
- ✅ "HUDSetup: Created FateFlowController" (once)
- ✅ "HUDSetup: Created EventSystem" (once, if missing)
- ❌ **Should NOT see**: "Already setup this frame" messages (indicates duplicate prevention working)
- ❌ **Should NOT see**: Duplicate manager GameObjects in hierarchy

**If Failures**:
- Check that HUDSetup has duplicate prevention
- Verify only one HUDSetup component exists in scene
- Check DefaultExecutionOrder attribute

---

### Test 7: Chain Logic and Turn System

**Steps**:
1. Place multiple cards on board
2. Place cards to create chains
3. End turn
4. Verify opponent turn works

**Expected Results**:
- ✅ Chain captures work correctly
- ✅ Cards flip when captured
- ✅ Scores update correctly
- ✅ Turn switches correctly
- ✅ Opponent can play cards (AI or manual)

**If Failures**:
- Check ScoreManager exists
- Check GameEndManager exists
- Check FateFlowController exists
- Verify turn system is working

---

## FINAL VERIFICATION SUMMARY

### ✅ All Tests Passing

**Checklist**:
- [ ] No compilation errors
- [ ] No missing script warnings
- [ ] Cards initialize correctly
- [ ] Player cards can be dragged
- [ ] Opponent cards cannot be dragged
- [ ] Cards bind to data correctly
- [ ] HUDSetup executes once
- [ ] MCP Unity server stable
- [ ] No meta file errors
- [ ] External editor works

### Expected Console Output (Clean)

**Good Logs** (these are expected):
- "CardFactory: Created and initialized card UI '[CardName]'"
- "NewCardUI.Initialize: Successfully initialized '[CardName]'"
- "HUDSetup: HUD successfully configured!"
- "WebSocket server started successfully on localhost:8090" (once)

**Bad Logs** (these should NOT appear):
- ❌ "Card is null in Start()"
- ❌ "Cannot drag - card is null"
- ❌ "Missing script on CardBackVisual"
- ❌ CS0106 compilation errors
- ❌ CS0246 namespace errors
- ❌ "Already setup this frame" (multiple times)

---

## QUICK DIAGNOSTIC COMMANDS

**Run in Unity Console** (Play Mode):

```csharp
// Check if cards are initialized
FindObjectsOfType<NewCardUI>().ToList().ForEach(c => 
    Debug.Log($"Card: {c.gameObject.name}, HasCard: {c.Card != null}"));

// Check if CardFactory is being used
// (Check NewHandUI.AddCardToHand source code)

// Check for missing scripts
// (Use CardPrefabValidator tool)
```

---

## TROUBLESHOOTING QUICK REFERENCE

### Issue: "Card is null in Start()"
**Fix**: Ensure CardFactory is being used. Check NewHandUI.AddCardToHand().

### Issue: "Cannot drag - card is null"
**Fix**: Card not initialized. Verify Initialize() is called before Start().

### Issue: "Missing script on CardBackVisual"
**Fix**: Run `Card Game > Validate Card Prefabs` → "Fix All Issues".

### Issue: CS0106 Compilation Errors
**Fix**: Check for methods outside class scope. Ensure all methods are inside classes.

### Issue: CS0246 Namespace Errors
**Fix**: Check that all namespaces are correct. Verify using statements.

### Issue: HUDSetup Executes Multiple Times
**Fix**: Verify duplicate prevention is working. Check only one HUDSetup exists in scene.

### Issue: MCP Server Restarts Constantly
**Fix**: This is normal. Server restarts on domain reload are expected.

---

## SUCCESS CRITERIA

✅ **All verification tests pass**
✅ **No errors in console**
✅ **Cards drag and drop correctly**
✅ **Opponent cards cannot be dragged**
✅ **All cards initialize properly**
✅ **HUD setup executes once**
✅ **Prefabs are validated and fixed**

**If all criteria are met, the repair is complete!** 🎉

