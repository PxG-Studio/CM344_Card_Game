# Assembly Definition Setup

## Issue
Test assembly couldn't reference game scripts because they were in the default assembly.

## Solution
Created assembly definition for game scripts and updated test assembly to reference it.

## Files Created/Modified

1. **`Assets/Scripts/CM344.CardGame.asmdef`** (NEW)
   - Assembly definition for all game scripts
   - Includes all scripts in `Assets/Scripts/` and subdirectories
   - References: `Unity.TextMeshPro`

2. **`Assets/Tests/CM344.CardGame.Tests.asmdef`** (MODIFIED)
   - Added reference: `"CM344.CardGame"`
   - Now references the main game assembly

## If Errors Persist

1. **Unity needs to recognize new assembly definition**
   - Wait a few seconds for Unity to detect the new `.asmdef` file
   - Unity will automatically compile the new assembly

2. **Check Unity Console**
   - Look for assembly definition errors
   - Verify `CM344.CardGame` assembly compiled successfully

3. **Force Unity Refresh** (if needed)
   - In Unity Editor: `Assets > Refresh` (or `Ctrl+R`)
   - Or close and reopen Unity

4. **Verify Assembly Definition Location**
   - Main assembly: `Assets/Scripts/CM344.CardGame.asmdef`
   - Test assembly: `Assets/Tests/CM344.CardGame.Tests.asmdef`

5. **Check Assembly References**
   - Open test assembly definition in Unity Inspector
   - Verify `CM344.CardGame` appears in References list
   - If not, Unity hasn't detected the main assembly yet

## Assembly Structure

```
CM344.CardGame (Main Game Assembly)
├── All scripts in Assets/Scripts/
├── References: Unity.TextMeshPro
└── Auto-referenced: true

CM344.CardGame.Tests (Test Assembly)
├── All scripts in Assets/Tests/
├── References:
│   ├── CM344.CardGame (main game assembly)
│   ├── UnityEngine.TestRunner
│   ├── UnityEditor.TestRunner
│   └── Unity.TextMeshPro
└── Precompiled: nunit.framework.dll
```

## Verification

Once Unity recognizes the assembly definition:
- ✅ Test scripts should compile without errors
- ✅ `CardGame.UI`, `CardGame.Managers`, `CardGame.Core` namespaces should be accessible
- ✅ Test Runner should show all tests

## Troubleshooting

If compilation still fails after Unity refresh:

1. **Check Assembly Definition Inspector**
   - Select `Assets/Scripts/CM344.CardGame.asmdef`
   - Verify it shows the correct scripts
   - Check for any compilation errors

2. **Check Test Assembly Inspector**
   - Select `Assets/Tests/CM344.CardGame.Tests.asmdef`
   - Verify `CM344.CardGame` is in the References list
   - If missing, Unity hasn't compiled the main assembly yet

3. **Manual Assembly Compilation**
   - Try modifying and saving a script in `Assets/Scripts/`
   - This should trigger Unity to recompile assemblies

4. **Restart Unity** (last resort)
   - Close Unity completely
   - Reopen the project
   - Wait for Unity to compile assemblies

## Notes

- Assembly definitions help isolate code and improve compilation speed
- The main game assembly (`CM344.CardGame`) is auto-referenced, meaning other assemblies can use it
- The test assembly explicitly references the main assembly to access game code
- Unity automatically manages assembly compilation order based on references

