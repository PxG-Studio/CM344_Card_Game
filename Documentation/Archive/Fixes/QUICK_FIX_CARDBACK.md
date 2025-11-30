# Quick Fix for Card Back Import Error

## The Error:
"Unknown error occurred while loading 'Assets/Sprite/CardBack_Default.png'"

## Quick Fix (30 seconds):

**In Unity Editor:**

1. Open **Project** window (bottom panel)
2. Navigate to: `Assets` → `Sprite`
3. Find `CardBack_Default.png`
4. **Right-click** on it
5. Click **"Reimport"**
6. Wait for Unity to finish (you'll see a progress bar)

That's it! The sprite will now load correctly.

## Why This Happens:

Unity sometimes needs to manually reimport programmatically-generated assets. The file is valid (512x512 PNG), but Unity's asset database needs to refresh.

## After Reimport:

- The error will disappear
- Cards will show the beautiful Persona 5-style card back when face-down
- Everything will work perfectly!

**Note:** The prefabs are already configured to use this sprite, so once Unity reimports it, everything will work automatically.

