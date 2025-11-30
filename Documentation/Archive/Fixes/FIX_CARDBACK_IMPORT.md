# Fix Card Back Sprite Import Error

## Problem:
"Unknown error occurred while loading 'Assets/Sprite/CardBack_Default.png'"

## Solution:

The sprite file exists and is valid, but Unity needs to reimport it. Here's how to fix:

### Option 1: Manual Reimport (Quickest)
1. In Unity Editor, go to **Project window**
2. Navigate to `Assets/Sprite/`
3. Find `CardBack_Default.png`
4. **Right-click** on the file
5. Select **"Reimport"**
6. Wait for Unity to finish importing

### Option 2: Regenerate the Sprite
1. In Unity Editor: **Tools** → **CardGame** → **Generate Card Back Sprite**
2. Choose 512x512 or 1024x1024
3. The generator will now properly configure import settings

### Option 3: Check Import Settings
1. Select `CardBack_Default.png` in Project window
2. In Inspector, verify:
   - **Texture Type**: Sprite (2D and UI)
   - **Pixels Per Unit**: 100
   - **Filter Mode**: Point (no filter)
   - **Compression**: None
3. Click **"Apply"** if you made changes

## Why This Happens:

When sprites are generated programmatically, Unity sometimes needs a manual refresh to properly import them. The file is valid, but Unity's asset database needs to process it.

## After Fixing:

The card back should now load correctly and appear on face-down cards!

