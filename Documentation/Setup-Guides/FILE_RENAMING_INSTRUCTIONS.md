# File Renaming Instructions for P1/P2 Compliance

## CRITICAL: Manual File Renaming Required

The following files need to be renamed to match their class names for 100% P1/P2 compliance.

## Files to Rename

### P2 Files (Currently in "Opposition Scripts" folder)

1. **CardMoverOpp.cs** → **CardMoverP2.cs**
   - Location: `Assets/Scripts/Current/Opposition Scripts/CardMoverOpp.cs`
   - Class inside: `CardMoverP2`
   - Also rename: `CardMoverOpp.cs.meta` → `CardMoverP2.cs.meta`

2. **NewDeckManagerOpp.cs** → **NewDeckManagerP2.cs**
   - Location: `Assets/Scripts/Current/Opposition Scripts/NewDeckManagerOpp.cs`
   - Class inside: `NewDeckManagerP2`
   - Also rename: `NewDeckManagerOpp.cs.meta` → `NewDeckManagerP2.cs.meta`

3. **NewHandOppUI.cs** → **NewHandP2UI.cs**
   - Location: `Assets/Scripts/Current/Opposition Scripts/NewHandOppUI.cs`
   - Class inside: `NewHandP2UI`
   - Also rename: `NewHandOppUI.cs.meta` → `NewHandP2UI.cs.meta`

4. **NewCardSystemOpposition.cs** → **NewCardSystemP2.cs**
   - Location: `Assets/Scripts/Current/Opposition Scripts/NewCardSystemOpposition.cs`
   - Class inside: `NewCardSystemP2`
   - Also rename: `NewCardSystemOpposition.cs.meta` → `NewCardSystemP2.cs.meta`

## Recommended Method: Unity Editor

**BEST APPROACH**: Rename files directly in Unity Editor:
1. Open Unity Editor
2. Navigate to `Assets/Scripts/Current/Opposition Scripts/` folder
3. Right-click each file → Rename
4. Unity will automatically update the `.meta` file

This method:
- ✅ Preserves all Unity references
- ✅ Updates meta files automatically
- ✅ Maintains serialized references
- ✅ Prevents broken references

## Alternative: PowerShell Script

If you prefer command-line, run this PowerShell script:

```powershell
# Navigate to the project root
cd "E:\Github\Projects\CM344_Card_Game"

# Rename CardMoverOpp.cs
Rename-Item -Path "Assets\Scripts\Current\Opposition Scripts\CardMoverOpp.cs" -NewName "CardMoverP2.cs"
Rename-Item -Path "Assets\Scripts\Current\Opposition Scripts\CardMoverOpp.cs.meta" -NewName "CardMoverP2.cs.meta"

# Rename NewDeckManagerOpp.cs
Rename-Item -Path "Assets\Scripts\Current\Opposition Scripts\NewDeckManagerOpp.cs" -NewName "NewDeckManagerP2.cs"
Rename-Item -Path "Assets\Scripts\Current\Opposition Scripts\NewDeckManagerOpp.cs.meta" -NewName "NewDeckManagerP2.cs.meta"

# Rename NewHandOppUI.cs
Rename-Item -Path "Assets\Scripts\Current\Opposition Scripts\NewHandOppUI.cs" -NewName "NewHandP2UI.cs"
Rename-Item -Path "Assets\Scripts\Current\Opposition Scripts\NewHandOppUI.cs.meta" -NewName "NewHandP2UI.cs.meta"

# Rename NewCardSystemOpposition.cs
Rename-Item -Path "Assets\Scripts\Current\Opposition Scripts\NewCardSystemOpposition.cs" -NewName "NewCardSystemP2.cs"
Rename-Item -Path "Assets\Scripts\Current\Opposition Scripts\NewCardSystemOpposition.cs.meta" -NewName "NewCardSystemP2.cs.meta"
```

**NOTE**: After renaming via command-line, Unity may need to reimport the files.

## After Renaming

1. **Verify in Unity**: Open Unity Editor and check that:
   - No errors appear in Console
   - All scripts compile successfully
   - References to these classes still work

2. **Run Tests**: Execute test suite to ensure no broken references

3. **Update Documentation**: Any documentation referencing old file names

## Verification Checklist

- [ ] CardMoverOpp.cs → CardMoverP2.cs
- [ ] NewDeckManagerOpp.cs → NewDeckManagerP2.cs
- [ ] NewHandOppUI.cs → NewHandP2UI.cs
- [ ] NewCardSystemOpposition.cs → NewCardSystemP2.cs
- [ ] All .meta files renamed
- [ ] Unity compiles without errors
- [ ] Tests pass

## Current Status

- ✅ Class names: All renamed to P2
- ✅ Code references: All updated
- ❌ File names: Still need manual renaming (THIS FILE)
- ✅ Meta files: Will be auto-updated by Unity

---

**Last Updated**: 2025-11-23
**Status**: Ready for manual file renaming

