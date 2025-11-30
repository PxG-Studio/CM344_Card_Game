# Delta Marker System

A complete Unity subsystem for displaying animated territory influence delta popups (+1/-1) during card gameplay.

## Quick Start

1. **Create Config Asset**: Right-click → `Create > Card Game > Delta Marker Config`
2. **Create Prefab**: Follow `DELTA_MARKER_SETUP.md` instructions
3. **Add Emitter**: Create empty GameObject, add `DeltaMarkerEmitter` component, assign config & prefab
4. **Done!** The system is automatically integrated into card capture logic.

## Files

- `DeltaMarkerConfig.cs` - ScriptableObject configuration
- `DeltaMarkerPopup.cs` - Individual popup animation component
- `DeltaMarkerEmitter.cs` - Spawner component
- `DeltaMarkerSystem.cs` - Static entry point API
- `DELTA_MARKER_SETUP.md` - Detailed setup guide

## Usage

```csharp
// Show +1 at transform position (conquer)
DeltaMarkerSystem.ShowDelta(+1, cardTransform);

// Show -1 at world position (raze)
DeltaMarkerSystem.ShowDeltaAtPosition(-1, worldPos);

// Show +1 at UI position
DeltaMarkerSystem.ShowDeltaAtUI(+1, screenPos);
```

## Integration Points

- ✅ `CardDropArea.cs` - Shows +1 when cards are captured
- 🔄 Ready for additional integration points as needed

## Features

- ✅ Gold/Yellow for Conquer (+1)
- ✅ Red/Orange for Raze (-1)
- ✅ Smooth animation (coroutine-based, DOTween optional)
- ✅ Screen-space and world-space support
- ✅ Multiplayer-ready (P1/P2 parity)
- ✅ Efficient singleton pattern
- ✅ Auto-cleanup after animation

## Requirements

- Unity 2019.4 or later
- TextMeshPro (TMP)
- DOTween (optional, for enhanced animations)

## Status

✅ **Complete and Ready** - All core functionality implemented and integrated.

