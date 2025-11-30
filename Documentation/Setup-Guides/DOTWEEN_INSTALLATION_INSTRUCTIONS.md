# DOTween Free Installation Instructions

## Current Status
✅ Scripting define symbol `DOTWEEN_AVAILABLE` has been added to ProjectSettings
✅ Code is ready with conditional compilation (#if DOTWEEN_AVAILABLE)
✅ Fallback coroutine animations will work if DOTween is not installed

## Installation Methods

### Method 1: Unity Asset Store (Recommended - Easiest)
1. Open Unity Editor with your project
2. Go to **Window → Asset Store**
3. Search for "DOTween (HOTween v2)" or "DOTween Free"
4. Click "Add to My Assets" (if free) or "Import" (if already in assets)
5. Once imported, Unity will prompt you to run setup
6. **Tools → Demigiant → DOTween Utility Panel**
7. Click "Setup DOTween..." button
8. Select modules: **Default UI**, **TextMeshPro** (required for our UI animations)
9. Click "Apply"

### Method 2: GitHub Download + Manual Import
1. Download DOTween from: https://github.com/Demigiant/dotween/releases
   - Look for the latest release with a `.unitypackage` file
   - OR download the repository as ZIP and extract
2. In Unity Editor: **Assets → Import Package → Custom Package...**
3. Navigate to the downloaded `.unitypackage` file
4. Import all files
5. **Tools → Demigiant → DOTween Utility Panel**
6. Click "Setup DOTween..." button
7. Select modules needed and click "Apply"

### Method 3: Via Package Manager (Git URL) - If Available
If DOTween has a Git URL package:
1. **Window → Package Manager**
2. Click the **+** button (top left)
3. Select **Add package from git URL...**
4. Enter: `https://github.com/Demigiant/dotween.git?path=/DOTween`
   - Note: This may not work for free version, use Method 1 or 2 instead

## After Installation

### Verification Steps:
1. Check that `Assets/Plugins/Demigiant/DOTween/` folder exists
2. Verify `DOTWEEN_AVAILABLE` is in Project Settings:
   - **Edit → Project Settings → Player → Other Settings → Scripting Define Symbols**
   - Should see: `DOTWEEN_AVAILABLE`
3. Test compilation:
   - Scripts should compile without errors
   - `using DG.Tweening;` should work

### If Installation Fails:
- Our code has fallback coroutine animations
- Animations will still work, just not as smooth
- DOTween can be added later without breaking functionality

## Files That Use DOTween (with fallbacks):
- `Assets/Scripts/Current/UI/CardFrontlineUI.cs`
- `Assets/Scripts/Current/UI/PlayerPanelUI.cs`
- `Assets/Scripts/Current/UI/DeltaMarker/DeltaMarkerPopup.cs`

All have `#if DOTWEEN_AVAILABLE` conditionals and coroutine fallbacks.

