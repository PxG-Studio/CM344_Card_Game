# Delta Marker System Setup Guide

This guide explains how to set up the Delta Marker System for displaying territory influence delta popups (+1/-1) in your Unity card game.

## Overview

The Delta Marker System displays animated popups when territory influence changes:
- **+1 (Gold/Yellow)** = Conquer event (gaining territory)
- **-1 (Red/Orange)** = Raze event (losing territory)

## Setup Steps

### 1. Create ScriptableObject Configuration

1. In Unity, right-click in your Project window (e.g., `Assets/Data/` or `Assets/ScriptableObjects/`)
2. Select `Create > Card Game > Delta Marker Config`
3. Name it `DeltaMarkerConfig`
4. Configure the asset:
   - **Conquer Color**: Gold/Yellow (default: RGB 255, 215, 0)
   - **Raze Color**: Red/Orange (default: RGB 255, 102, 0)
   - **Float Distance**: 100 (distance popup floats upward)
   - **Duration**: 1.5 seconds (total animation time)
   - **Scale Punch Amount**: 1.3 (30% larger during punch)
   - **Float Curve**: EaseOut curve (adjust as needed)
   - **Delta Font**: Assign a bold TextMeshPro font (optional)
   - **Font Size**: 60 (adjust as needed)

### 2. Create Delta Marker Prefab

#### Option A: Create Prefab Manually

1. **Create Canvas GameObject** (if needed):
   - Right-click in Hierarchy → `UI > Canvas`
   - Name it `DeltaMarkerCanvas`
   - Set Render Mode to `Screen Space - Overlay` or `Screen Space - Camera`
   - Set Canvas Scaler to `Scale With Screen Size` (Reference Resolution: 1920x1080)

2. **Create Popup GameObject**:
   - Right-click on `DeltaMarkerCanvas` → `UI > Text - TextMeshPro`
   - Name it `DeltaMarkerPopup`
   - Configure the TextMeshPro component:
     - Font Size: 60
     - Font Style: Bold
     - Alignment: Center (both horizontal and vertical)
     - Color: White (will be overridden by script)
     - Text: "+1" (placeholder)

3. **Add Components**:
   - Add `CanvasGroup` component
   - Add `DeltaMarkerPopup` script component

4. **Set Anchors**:
   - Select the RectTransform
   - Set Anchor Preset to center-middle
   - Set Pivot to (0.5, 0.5)
   - Set Size: Width 200, Height 100

5. **Create Prefab**:
   - Drag `DeltaMarkerPopup` from Hierarchy to Project window
   - Save as `Assets/PreFabs/UI/DeltaMarkerPopup.prefab`
   - Delete the instance from Hierarchy (prefab is saved)

#### Option B: Use Prefab Variant

If you already have a similar UI prefab, create a variant and modify it.

### 3. Create Delta Marker Emitter GameObject

1. **Create GameObject**:
   - Right-click in Hierarchy → `Create Empty`
   - Name it `DeltaMarkerEmitter`

2. **Add Component**:
   - Add `DeltaMarkerEmitter` script component

3. **Configure Component**:
   - **Config**: Assign the `DeltaMarkerConfig` ScriptableObject created in step 1
   - **Delta Marker Prefab**: Assign the `DeltaMarkerPopup.prefab` created in step 2
   - **Parent Canvas**: Assign `HUDOverlayCanvas` (or leave null to auto-find)
   - **Use Screen Space**: Checked (true)

4. **Position**: The emitter's position doesn't matter (it spawns popups at specified locations)

### 4. Integration

The system is already integrated into `CardDropArea.cs`. When a card is captured, it automatically shows a +1 delta marker.

#### Manual Usage

You can also call the system manually from any script:

```csharp
using CardGame.UI;

// Show +1 at a transform position
DeltaMarkerSystem.ShowDelta(+1, cardTransform);

// Show -1 at a world position
DeltaMarkerSystem.ShowDeltaAtPosition(-1, worldPosition);

// Show +1 at UI position
DeltaMarkerSystem.ShowDeltaAtUI(+1, screenPosition);
```

### 5. Optional: DOTween Support

If you have DOTween installed in your project:

1. Add `DOTWEEN_AVAILABLE` to your Scripting Define Symbols:
   - Edit → Project Settings → Player → Other Settings → Scripting Define Symbols
   - Add `DOTWEEN_AVAILABLE` (comma-separated if other symbols exist)

2. The system will automatically use DOTween for smoother animations if available, otherwise it falls back to coroutines.

## File Structure

```
Assets/
├── Scripts/
│   └── Current/
│       └── UI/
│           └── DeltaMarker/
│               ├── DeltaMarkerConfig.cs       (ScriptableObject)
│               ├── DeltaMarkerPopup.cs        (Animation component)
│               ├── DeltaMarkerEmitter.cs      (Spawner component)
│               └── DeltaMarkerSystem.cs       (Static entry point)
├── Data/ (or ScriptableObjects/)
│   └── DeltaMarkerConfig.asset                (Configuration asset)
└── PreFabs/
    └── UI/
        └── DeltaMarkerPopup.prefab            (Popup prefab)
```

## Multiplayer Support

The system is multiplayer-ready:
- Works with both P1 and P2 card captures
- Automatically adapts to world-space or screen-space rendering
- No special naming required (P1/P2 parity handled automatically)

## Troubleshooting

### Popups not appearing:
1. Check that `DeltaMarkerEmitter` exists in the scene
2. Verify `DeltaMarkerConfig` is assigned
3. Verify `DeltaMarkerPopup.prefab` is assigned
4. Check console for error messages

### Popups appearing in wrong position:
1. Ensure `Parent Canvas` is assigned or auto-found
2. Check camera assignment (for world-space conversion)
3. Verify `Use Screen Space` setting matches your canvas setup

### Animation not working:
1. Ensure `DeltaMarkerPopup` component is attached to prefab
2. Check `CanvasGroup` component exists
3. Verify `TextMeshProUGUI` component exists (named `DeltaText` or on root)
4. Check console for animation errors

## Testing

You can test the popup in the editor:

1. Select the `DeltaMarkerPopup` prefab in Project
2. Click "Open Prefab" to edit
3. Select the root GameObject
4. In Inspector, find `DeltaMarkerPopup` component
5. Assign a `DeltaMarkerConfig` asset
6. Right-click the component → `Test Popup (+1)` or `Test Popup (-1)`

## Performance Notes

- Popups auto-destroy after animation completes
- Uses efficient singleton pattern (no per-frame searches)
- Minimal overhead per popup (just instantiation + animation)
- Safe to call frequently during gameplay

