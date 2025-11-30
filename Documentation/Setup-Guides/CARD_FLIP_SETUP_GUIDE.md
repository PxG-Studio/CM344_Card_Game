# Card Flip Animation Setup Guide

This guide will help you set up your `NewCardPrefab` to support the card flip animation system.

## Current Status

✅ **Code is ready**: All scripts support flip animation  
⚠️ **Prefab needs setup**: The prefab structure needs to be updated in Unity Editor

## Required Prefab Structure

Your `NewCardPrefab` needs this hierarchy:

```
NewCardPrefab (Root GameObject)
├── NewCardUI (Component)
├── CardFlipAnimation (Component - auto-added if containers exist)
├── CardMover (Component - for 2D board interaction)
│
├── FrontContainer (GameObject) ← NEW - Contains all front face elements
│   ├── CanvasGroup (Component - auto-added)
│   ├── CardBackground (SpriteRenderer or Image)
│   ├── Artwork (SpriteRenderer or Image)
│   ├── CardNameText (TextMeshProUGUI)
│   ├── DescriptionText (TextMeshProUGUI)
│   ├── TopStatText (TextMeshProUGUI)
│   ├── RightStatText (TextMeshProUGUI)
│   ├── DownStatText (TextMeshProUGUI)
│   ├── LeftStatText (TextMeshProUGUI)
│   ├── CardTypeText (TextMeshProUGUI)
│   └── CardTypeIcon (SpriteRenderer or Image)
│
└── BackContainer (GameObject) ← NEW - Contains card back
    ├── CanvasGroup (Component - auto-added)
    └── CardBackSprite (SpriteRenderer or Image)
```

## Step-by-Step Setup Instructions

### Step 1: Open Prefab in Prefab Mode

1. In Unity, navigate to `Assets/PreFabs/NewCardPrefab.prefab`
2. Double-click to open in Prefab Mode (or click "Open Prefab" button)

### Step 2: Create FrontContainer

1. Right-click on `NewCardPrefab` (root) → `Create Empty`
2. Rename to `FrontContainer`
3. **Move all existing card elements into FrontContainer:**
   - Drag `CardBackground` (SpriteRenderer/Image) into `FrontContainer`
   - Drag `Artwork` (SpriteRenderer/Image) into `FrontContainer`
   - Drag `CardNameText` (TextMeshProUGUI) into `FrontContainer`
   - Drag `DescriptionText` (TextMeshProUGUI) into `FrontContainer`
   - Drag `TopStatText` (TextMeshProUGUI) into `FrontContainer`
   - Drag `RightStatText` (TextMeshProUGUI) into `FrontContainer`
   - Drag `DownStatText` (TextMeshProUGUI) into `FrontContainer`
   - Drag `LeftStatText` (TextMeshProUGUI) into `FrontContainer`
   - Drag `CardTypeText` (TextMeshProUGUI) into `FrontContainer`
   - Drag `CardTypeIcon` (SpriteRenderer/Image) into `FrontContainer`
   - **Move any other visual elements** that should be on the front face

### Step 3: Create BackContainer

1. Right-click on `NewCardPrefab` (root) → `Create Empty`
2. Rename to `BackContainer`
3. **Create card back visual:**
   - Right-click `BackContainer` → `2D Object > Sprite` (if using SpriteRenderer)
   - OR Right-click `BackContainer` → `UI > Image` (if using UI Image)
   - Rename to `CardBackSprite`
   - Assign a sprite/texture to the `Sprite` or `Image` component
   - Position and scale it to cover the card area

### Step 4: Configure NewCardUI Component

1. Select `NewCardPrefab` (root) in hierarchy
2. In Inspector, find the `NewCardUI` component
3. **Assign Flip Animation References:**
   - `Front Container`: Drag `FrontContainer` from hierarchy
   - `Back Container`: Drag `BackContainer` from hierarchy
   - `Back Sprite Renderer`: Drag `CardBackSprite`'s SpriteRenderer component (if using SpriteRenderer)
   - `Back Image`: Drag `CardBackSprite`'s Image component (if using UI Image)
   - `Default Card Back Sprite`: Assign a default sprite asset (optional, used if card data doesn't have one)

4. **Configure Flip Settings:**
   - `Start Face Down`: ✅ true (cards start face-down)
   - `Auto Flip On Reveal`: ✅ true (cards automatically flip when drawn)
   - `Reveal Delay`: 0.2 (seconds between card reveals for staggered effect)
   - `Allow Click To Flip`: false (or true if you want click-to-flip)

### Step 5: Reassign UI References (Important!)

After moving elements into `FrontContainer`, you need to **reassign references** in `NewCardUI`:

1. In `NewCardUI` component Inspector:
   - `Card Background`: Drag `CardBackground` from `FrontContainer`
   - `Artwork`: Drag `Artwork` from `FrontContainer`
   - `Card Name Text`: Drag `CardNameText` from `FrontContainer`
   - `Description Text`: Drag `DescriptionText` from `FrontContainer`
   - `Top Stat Text`: Drag `TopStatText` from `FrontContainer`
   - `Right Stat Text`: Drag `RightStatText` from `FrontContainer`
   - `Down Stat Text`: Drag `DownStatText` from `FrontContainer`
   - `Left Stat Text`: Drag `LeftStatText` from `FrontContainer`
   - `Card Type Text`: Drag `CardTypeText` from `FrontContainer`
   - `Card Type Icon`: Drag `CardTypeIcon` from `FrontContainer`

### Step 6: Save Prefab

1. Click "Open Prefab" button again (or press Esc) to exit Prefab Mode
2. Unity will ask to save changes - click **Save**

### Step 7: Verify Setup

1. Open your scene (e.g., `BattleScreenMultiplayer`)
2. Press Play
3. Cards should:
   - Start face-down (showing back)
   - Automatically flip to front when drawn (with staggered delay)
   - Show no errors in Console

## Troubleshooting

### Cards Don't Flip

- ✅ Check Console for errors
- ✅ Verify `FrontContainer` and `BackContainer` are assigned in `NewCardUI`
- ✅ Verify `BackContainer` has a visual element (SpriteRenderer or Image)
- ✅ Check that `Start Face Down` and `Auto Flip On Reveal` are enabled

### Cards Show Blank/Invisible

- ✅ Check that `FrontContainer` contains all visual elements
- ✅ Verify all UI references in `NewCardUI` are reassigned after moving to `FrontContainer`
- ✅ Check that `BackContainer` has a sprite/image assigned

### Flip Animation Errors

- ✅ Ensure `CardFlipAnimation` component exists (auto-added if containers are set)
- ✅ Check that both containers have `CanvasGroup` components (auto-added)
- ✅ Verify no conflicting CanvasGroup on root GameObject

## Optional: Per-Card Back Sprites

You can assign different back sprites per card:

1. In your `NewCardData` ScriptableObject assets
2. Assign a sprite to the `Card Back Sprite` field
3. If not assigned, the `Default Card Back Sprite` from `NewCardUI` will be used

## Current Card Data Status

All `NewCardData` assets already have the `cardBackSprite` field available. You can:
- Leave it empty (uses default)
- Assign a unique sprite per card type
- Assign a sprite per individual card

## Next Steps

After setup:
1. Test the flip animation in Play mode
2. Adjust `revealDelay` in `NewHandUI` if you want different stagger timing
3. Customize flip duration and easing in `CardFlipAnimation` component if desired

