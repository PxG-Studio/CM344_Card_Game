# Card Flip Animation - Prefab Setup Guide

## Overview
This guide explains how to set up the `NewCardPrefab` to support card flip animations. The flip system uses a dual fade approach that works with both UI elements (TextMeshProUGUI, Image) and 2D elements (SpriteRenderer).

## Required Prefab Structure

```
NewCardPrefab (root GameObject)
├─ NewCardUI (component)
├─ CardFlipAnimation (component, auto-added at runtime)
├─ CardMover (optional, for board cards)
├─ CanvasGroup (optional, for SetInteractable - doesn't control alpha during flip)
├─ FrontContainer (GameObject) - REQUIRED
│  ├─ CanvasGroup (component, alpha = 1, interactable = true)
│  ├─ cardBackground (SpriteRenderer) - will fade via color.a
│  ├─ artwork (SpriteRenderer) - will fade via color.a
│  ├─ cardTypeIcon (SpriteRenderer) - will fade via color.a
│  ├─ cardNameText (TextMeshProUGUI) - will fade via CanvasGroup
│  ├─ descriptionText (TextMeshProUGUI) - will fade via CanvasGroup
│  ├─ topStatText (TextMeshProUGUI) - will fade via CanvasGroup
│  ├─ rightStatText (TextMeshProUGUI) - will fade via CanvasGroup
│  ├─ downStatText (TextMeshProUGUI) - will fade via CanvasGroup
│  ├─ leftStatText (TextMeshProUGUI) - will fade via CanvasGroup
│  └─ cardTypeText (TextMeshProUGUI) - will fade via CanvasGroup
└─ BackContainer (GameObject) - REQUIRED
   ├─ CanvasGroup (component, alpha = 0 initially, interactable = false)
   └─ backSpriteRenderer (SpriteRenderer) OR backImage (Image)
      └─ Assign card back sprite here
      └─ Will fade via color.a (if SpriteRenderer) or CanvasGroup (if Image)
```

## Step-by-Step Setup Instructions

### 1. Open Prefab in Prefab Mode
- Navigate to `Assets/PreFabs/NewCardPrefab.prefab`
- Double-click to open in Prefab mode

### 2. Create FrontContainer
1. Right-click on `NewCardPrefab` root → Create Empty
2. Rename to `FrontContainer`
3. Add Component → Canvas Group
4. Set CanvasGroup properties:
   - Alpha: 1
   - Interactable: ✓ (checked)
   - Blocks Raycasts: ✓ (checked)

### 3. Move Existing Card Elements to FrontContainer
1. Select all existing card elements:
   - `cardBackground` (SpriteRenderer)
   - `artwork` (SpriteRenderer)
   - `cardTypeIcon` (SpriteRenderer)
   - `cardNameText` (TextMeshProUGUI)
   - `descriptionText` (TextMeshProUGUI)
   - `topStatText` (TextMeshProUGUI)
   - `rightStatText` (TextMeshProUGUI)
   - `downStatText` (TextMeshProUGUI)
   - `leftStatText` (TextMeshProUGUI)
   - `cardTypeText` (TextMeshProUGUI)
2. Drag all selected elements into `FrontContainer` in the Hierarchy
3. **Note**: Unity will preserve Inspector references automatically

### 4. Create BackContainer
1. Right-click on `NewCardPrefab` root → Create Empty
2. Rename to `BackContainer`
3. Add Component → Canvas Group
4. Set CanvasGroup properties:
   - Alpha: 0 (initially hidden)
   - Interactable: ✗ (unchecked)
   - Blocks Raycasts: ✗ (unchecked)

### 5. Add Card Back Visual
**Option A: Using SpriteRenderer (for 2D cards)**
1. Right-click `BackContainer` → 2D Object → Sprite
2. Rename to `backSpriteRenderer`
3. Assign your card back sprite to the Sprite field

**Option B: Using Image (for UI cards)**
1. Right-click `BackContainer` → UI → Image
2. Rename to `backImage`
3. Assign your card back sprite to the Source Image field

### 6. Assign References in NewCardUI Component
1. Select `NewCardPrefab` root
2. In the Inspector, find the `NewCardUI` component
3. Under "Flip Animation" section:
   - **Front Container**: Drag `FrontContainer` from Hierarchy
   - **Back Container**: Drag `BackContainer` from Hierarchy
   - **Back Sprite Renderer**: Drag `backSpriteRenderer` (if using SpriteRenderer)
   - **Back Image**: Drag `backImage` (if using Image)
   - **Default Card Back Sprite**: (Optional) Assign a shared default sprite

### 7. Configure Flip Settings
In the `NewCardUI` component, under "Flip Settings":
- **Start Face Down**: ✓ (checked) - Cards start showing back
- **Auto Flip On Reveal**: ✓ (checked) - Cards automatically flip when drawn
- **Reveal Delay**: 0.2 (seconds) - Delay before auto-flip
- **Allow Click To Flip**: ✗ (unchecked by default) - Enable for manual flip

### 8. Save Prefab
- Click "Open Prefab" button to exit Prefab mode
- Unity will automatically save changes

## Migration Guide for Existing Prefabs

If you have an existing `NewCardPrefab` that's already in use:

1. **Backup First**: Duplicate the prefab as a backup
2. Follow steps 1-8 above
3. **Verify References**: After moving elements, check that all Inspector references in `NewCardUI` are still valid
4. **Test in Scene**: Create a test scene and verify:
   - Cards start face-down
   - Cards flip to face-up on reveal
   - Both SpriteRenderer and TextMeshProUGUI elements fade correctly
   - `ArrangeCards()` still works correctly

## Important Notes

### Root CanvasGroup
- The root GameObject may have a `CanvasGroup` component (used by `SetInteractable()`)
- This CanvasGroup **does NOT** control alpha during flip animations
- Only the container CanvasGroups control fade during flip
- Root CanvasGroup only controls interactability

### Component Separation
- **UI Elements** (TextMeshProUGUI, Image): Fade via `CanvasGroup.alpha`
- **2D Elements** (SpriteRenderer): Fade via `color.a` (RGB values preserved)

### Performance
- SpriteRenderer arrays are cached at runtime (no performance impact)
- CanvasGroup components are cached (no GetComponent calls during animation)
- Fade animations are very lightweight

## Troubleshooting

### Cards Don't Flip
- Check that `FrontContainer` and `BackContainer` are assigned in `NewCardUI` Inspector
- Check Console for error messages from `ValidateSetup()`
- Verify `autoFlipOnReveal` is enabled

### Only Text Fades, Sprites Don't
- This means SpriteRenderer fade isn't working
- Check that SpriteRenderer components are children of `FrontContainer`
- Verify `CardFlipAnimation` component exists on root

### Cards Jump During Flip
- This shouldn't happen (flip doesn't affect transforms)
- If it does, check that `ArrangeCards()` isn't being called during flip
- Verify no other scripts are modifying card position during flip

### Root CanvasGroup Conflicts
- Root CanvasGroup should only control `interactable`, not `alpha`
- `SetInteractable()` has been updated to avoid conflicts
- If issues persist, ensure root CanvasGroup alpha is only set when not animating

## Testing Checklist

After setup, verify:
- [ ] Cards start face-down (back visible, front invisible)
- [ ] Cards flip to face-up on reveal (smooth fade)
- [ ] Multiple cards flip with stagger (0.1s intervals)
- [ ] `ArrangeCards()` still works (no position jumps)
- [ ] Both SpriteRenderer and TextMeshProUGUI fade correctly
- [ ] Card maintains flip state when moved to board (CardMover)
- [ ] Click-to-flip works (if enabled)
- [ ] No console errors or warnings

