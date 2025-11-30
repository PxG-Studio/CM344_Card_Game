# Fix CardBackVisual Missing Script Warning - Instructions

## ✅ Tool Fixed and Ready

The Editor tool has been updated to properly fix missing script references on CardBackVisual prefabs.

## 🎯 How to Fix the Warning

### Method 1: Use the Editor Menu (Recommended)

1. **Stop Play Mode** (if running) - The tool requires Editor mode
2. In Unity Editor, go to: **`CardFront > Fix Prefabs > Clean Missing Scripts on CardBackVisual Prefabs`**
3. A dialog will appear showing results:
   - If fixed: "Fixed X missing script reference(s) on CardBackVisual in prefab assets."
   - If none found: "No missing script references found on CardBackVisual in prefab assets."
4. Click "OK" to dismiss the dialog
5. The warning should be gone on next scene load/play

### Method 2: Use CardPrefabValidator

1. **Stop Play Mode** (if running)
2. In Unity Editor, go to: **`CardFront > Validate Card Prefabs`**
3. Click **"Fix All Issues"** button
4. This will also fix missing scripts on CardBackVisual (and other issues)

## 🔍 What the Tool Does

The tool:
1. Searches all prefabs in `Assets/PreFabs` folder
2. Recursively finds `CardBackVisual` GameObjects in each prefab
3. Checks for missing script references
4. Removes missing scripts using `GameObjectUtility.RemoveMonoBehavioursWithMissingScript()`
5. Saves prefabs back to disk
6. Logs results to Console

## 📝 Console Logs to Expect

When the tool runs successfully, you'll see logs like:
```
[FixCardBackVisualPrefabs] Searching 2 prefab(s) in Assets/PreFabs...
[FixCardBackVisualPrefabs] Found 1 missing script(s) on CardBackVisual in Assets/PreFabs/NewCardPrefab.prefab
[FixCardBackVisualPrefabs] ✓ Fixed 1 missing script(s) on CardBackVisual in prefab: Assets/PreFabs/NewCardPrefab.prefab
[FixCardBackVisualPrefabs] Checked 2 prefab(s), fixed 1 missing script reference(s)
[FixCardBackVisualPrefabs] Successfully fixed 1 missing script reference(s) on CardBackVisual prefabs
```

## ⚠️ Note

- **The warning is cosmetic** - cards work fine even with the warning
- **The warning appears at scene load** - it will disappear after fixing the prefabs
- **You may need to reload the scene** after fixing to see the warning disappear

## ✅ Verification

After running the tool:
1. Check Console - should see fix logs
2. Reload the scene or restart Unity Editor
3. Check Console - "The referenced script on this Behaviour (Game Object 'CardBackVisual') is missing!" warning should be gone

---

**Tool Location**: `Assets/Editor/FixCardBackVisualPrefabs.cs`  
**Menu Path**: `CardFront > Fix Prefabs > Clean Missing Scripts on CardBackVisual Prefabs`

