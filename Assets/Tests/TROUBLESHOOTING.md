# Test Runner Troubleshooting Guide

## Issue: PlayMode Tests Hang / "Test Runner - (busy for 20s)..."

### Most Common Cause: Scene Not in Build Settings

**Solution:**
1. Open Unity Editor
2. Go to `File > Build Settings`
3. Click `Add Open Scenes` button (or manually add `BattleScreenMultiplayer` scene)
4. Ensure `BattleScreenMultiplayer` appears in the "Scenes In Build" list
5. Close Build Settings window
6. Try running tests again

### Other Common Issues

#### Issue: Tests Time Out After 10 Seconds
**What it means:** The scene is taking too long to load or initialize.

**Possible causes:**
- HUDSetup is taking too long
- Scene has errors preventing load
- DontDestroyOnLoad objects causing conflicts

**Solutions:**
1. Check Unity Console for errors during scene load
2. Increase timeout in test SetUp methods if needed (currently 10 seconds)
3. Check if HUDSetup is hanging - look for console logs

#### Issue: Scene Loads But Tests Fail
**What it means:** Scene loaded but components aren't initialized.

**Solutions:**
1. Increase wait time in `SetUp()` methods
2. Check console for initialization errors
3. Verify HUDSetup runs successfully

#### Issue: "Scene not found in Build Settings"
**What it means:** Test can't find the scene.

**Solutions:**
1. Add scene to Build Settings (see above)
2. Verify scene name matches exactly: `BattleScreenMultiplayer`
3. Check scene file exists: `Assets/Scenes/BattleScreenMultiplayer.unity`

## Quick Fix Checklist

- [ ] Scene added to Build Settings
- [ ] No compilation errors
- [ ] Console shows no errors during scene load
- [ ] HUDSetup completes (check console logs)
- [ ] Tests timeout if scene doesn't load (after 10 seconds)

## Manual Verification

Before running tests, manually verify:
1. Open `BattleScreenMultiplayer` scene
2. Press Play
3. Scene should load and initialize without errors
4. If it doesn't, fix scene issues first before running tests

## If Tests Still Hang

1. **Cancel Test Run** (click Cancel button in Test Runner)
2. **Check Console** for specific errors
3. **Try running one test at a time** instead of "Run All"
4. **Check HUDSetup logs** - it should complete within 1-2 seconds
5. **Verify no infinite loops** in initialization code

## Test Timeout Settings

Current timeouts:
- Scene loading: **10 seconds**
- Initialization wait: **0.2-1.0 seconds** (varies by test file)

If your scene takes longer to load, you can increase timeouts in the test files:
- Look for `float timeout = 10f;` in `[UnitySetUp]` methods
- Increase as needed (e.g., `float timeout = 30f;`)

