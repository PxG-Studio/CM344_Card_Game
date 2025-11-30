# DOTween Setup - Next Steps

## ✅ DOTween Imported Successfully!

You're seeing the setup popup, which means DOTween is installed. Now you need to configure it.

## Immediate Next Steps:

1. **Click the "Open DOTween Utility Panel" button** in the popup window
   - OR manually: Menu → `Tools` → `Demigiant` → `DOTween Utility Panel`

2. **In the DOTween Utility Panel:**
   - Click the **"Setup DOTween..."** button
   - A setup window will appear

3. **In the Setup Window:**
   - ✅ **Check "Default UI"** - Required for UI animations (Image, RectTransform, etc.)
   - ✅ **Check "TextMeshPro"** - Required for TextMeshPro animations
   - ✅ **"Core"** should already be checked
   - ❌ You can uncheck other modules if you don't need them (Audio, Sprite, etc.)

4. **Click "Apply"** button

5. **Close the setup window**

## ✅ Verification:

After setup, you should see:
- No errors in the Unity Console
- Scripts compile successfully
- `Assets/Plugins/Demigiant/DOTween/` folder contains runtime scripts
- Your animations will now use smooth DOTween instead of coroutines!

## What This Enables:

- **CardFrontlineUI**: Flip-clock counter animations
- **PlayerPanelUI**: Blurp fade/pulse animations  
- **DeltaMarkerPopup**: Smooth popup animations

All these will automatically upgrade from coroutines to DOTween animations now!

