# Quick DOTween Installation Guide

## ✅ Pre-requisites Complete
- Scripting define symbol `DOTWEEN_AVAILABLE` is already added ✅
- Code is ready with conditional compilation ✅

## Step-by-Step Installation (5 minutes)

### Option A: Unity Asset Store (Easiest)

1. **Open Unity Editor** with your project (`CM344_Card_Game`)

2. **Open Asset Store:**
   - Menu: `Window` → `Asset Store` (or press `Ctrl+9` / `Cmd+9`)

3. **Search for DOTween:**
   - In the search bar, type: `DOTween`
   - Look for **"DOTween (HOTween v2)"** by Demigiant
   - The FREE version should be available

4. **Import:**
   - Click the **"Add to My Assets"** button (if not already added)
   - Click **"Open in Unity"** button (if already in assets)
   - Click **"Import"** button in the Package Manager window
   - Select **ALL** items in the import dialog
   - Click **"Import"**

5. **Setup DOTween:**
   - After import, a **DOTween Utility Panel** should appear automatically
   - If not: Menu → `Tools` → `Demigiant` → `DOTween Utility Panel`
   - Click **"Setup DOTween..."** button
   - In the setup window:
     - ✅ Check **"Default UI"** (for UI animations)
     - ✅ Check **"TextMeshPro"** (for our TextMeshPro animations)
     - ✅ Check **"Core"** (already checked)
   - Click **"Apply"**
   - Click **"Close"**

6. **Verify Installation:**
   - Check: `Assets/Plugins/Demigiant/DOTween/` folder exists
   - Check: Your scripts compile without errors
   - Test: Run the game - animations should be smooth!

### Option B: Manual Download (If Asset Store doesn't work)

1. **Download DOTween:**
   - Visit: https://dotween.demigiant.com/download.php
   - Download **DOTween Free** version (.unitypackage file)

2. **Import in Unity:**
   - Menu: `Assets` → `Import Package` → `Custom Package...`
   - Select the downloaded `.unitypackage` file
   - Click **"Import"**

3. **Follow Step 5 and 6** from Option A above

## ✅ Done!

Once installed, your animations in:
- CardFrontlineUI (flip-clock counter)
- PlayerPanelUI (blurp animations)
- DeltaMarkerPopup (popup animations)

...will automatically use smooth DOTween animations instead of coroutines!

## Troubleshooting

**If DOTween Utility Panel doesn't appear:**
- Check: `Assets/Plugins/Demigiant/DOTween/DOTweenUtilityPanel.cs` exists
- Try: Restart Unity Editor

**If scripts don't compile:**
- Check: `DOTWEEN_AVAILABLE` is in Project Settings → Player → Scripting Define Symbols
- Check: `Assets/Plugins/Demigiant/DOTween/DOTween.cs` exists

**If animations still use coroutines:**
- Check: DOTween is properly installed (see verification steps)
- Our code has fallbacks - animations will still work!

