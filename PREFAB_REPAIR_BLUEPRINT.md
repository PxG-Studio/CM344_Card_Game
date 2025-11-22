# CardFront Prefab Repair Blueprint

## PREFAB STRUCTURE GUIDE

### NewCardPrefab - Complete Structure

#### Root GameObject: `NewCardPrefab`

**Required Components:**
1. ✅ **NewCardUI** (MonoBehaviour)
   - Main card UI component
   - Handles card display and interaction
   - MUST exist

2. ✅ **RectTransform** (Unity Component)
   - Required for UI cards
   - Auto-added if missing
   - MUST exist

3. ✅ **CanvasGroup** (Unity Component)
   - Required for drag-and-drop alpha control
   - Created at runtime by `NewCardUI.Awake()` if missing
   - Can be added manually to prefab

**Components to Remove:**
- ❌ Any "Missing Script" components (legacy deleted scripts)
- ❌ Legacy `CardUI` component (old system, if present)
- ❌ Any duplicate card components

**Hierarchy Structure:**
```
NewCardPrefab (root)
├── Components:
│   ├── NewCardUI (required)
│   ├── RectTransform (required)
│   └── CanvasGroup (optional - auto-created if missing)
│
├── FrontContainer (GameObject) - Optional
│   └── Contains: All visible card elements (front face)
│       ├── Artwork (SpriteRenderer) - Optional
│       ├── CardBackground (SpriteRenderer) - Optional
│       ├── CardNameText (TextMeshProUGUI) - Optional
│       ├── DescriptionText (TextMeshProUGUI) - Optional
│       ├── TopStatText (TextMeshProUGUI) - Optional
│       ├── RightStatText (TextMeshProUGUI) - Optional
│       ├── DownStatText (TextMeshProUGUI) - Optional
│       ├── LeftStatText (TextMeshProUGUI) - Optional
│       ├── CardTypeText (TextMeshProUGUI) - Optional
│       └── CardTypeIcon (SpriteRenderer) - Optional
│
└── BackContainer (GameObject) - Optional
    └── Contains: Card back visual (back face)
        └── CardBackVisual (GameObject) - Optional
            └── Components:
                ├── Image (UI cards) OR
                └── SpriteRenderer (2D cards)
                └── NO SCRIPTS (visual only)
```

**Note**: FrontContainer, BackContainer, and CardBackVisual are created at runtime if missing.

---

### NewCardPrefabOpp - Complete Structure

**Same as NewCardPrefab** (opponent variant)

**Differences**:
- Prefab name: `NewCardPrefabOpp`
- Uses opponent-specific visuals (if any)
- Otherwise identical structure

---

### CardBackVisual - Detailed Structure

**Purpose**: Visual representation of card back (face-down state)

**GameObject Structure:**
- **Name**: `CardBackVisual`
- **Parent**: `BackContainer` GameObject
- **Position**: Local (0, 0, 0)
- **Scale**: (1, 1, 1)

**Required Components**:
1. ✅ **Image** (for UI cards) OR **SpriteRenderer** (for 2D cards)
   - Must have one or the other
   - Created at runtime if missing
   - Default color: Dark gray (0.3, 0.3, 0.3, 1.0)

**Components to Remove:**
- ❌ Any "Missing Script" components
- ❌ Any legacy scripts
- ❌ Both Image AND SpriteRenderer (should have only one)

**How It's Created**:
- Created at runtime by `NewCardUI.AutoSetupContainers()` if missing
- Auto-detects if UI card (RectTransform exists) or 2D card (no RectTransform)
- Adds appropriate component automatically

---

### FrontContainer - Detailed Structure

**Purpose**: Container for all visible card elements (front face)

**GameObject Structure:**
- **Name**: `FrontContainer`
- **Parent**: Root GameObject (NewCardPrefab)
- **Position**: Local (0, 0, 0)
- **Scale**: (1, 1, 1)

**Children** (all optional):
- Artwork (SpriteRenderer)
- CardBackground (SpriteRenderer)
- CardNameText (TextMeshProUGUI)
- DescriptionText (TextMeshProUGUI)
- TopStatText (TextMeshProUGUI)
- RightStatText (TextMeshProUGUI)
- DownStatText (TextMeshProUGUI)
- LeftStatText (TextMeshProUGUI)
- CardTypeText (TextMeshProUGUI)
- CardTypeIcon (SpriteRenderer)

**How It's Created**:
- Created at runtime by `NewCardUI.AutoSetupContainers()` if missing
- All existing card elements are moved into FrontContainer automatically

---

### BackContainer - Detailed Structure

**Purpose**: Container for card back visual (back face)

**GameObject Structure:**
- **Name**: `BackContainer`
- **Parent**: Root GameObject (NewCardPrefab)
- **Position**: Local (0, 0, 0)
- **Scale**: (1, 1, 1)

**Children**:
- CardBackVisual (GameObject)

**How It's Created**:
- Created at runtime by `NewCardUI.AutoSetupContainers()` if missing

---

## STEP-BY-STEP PREFAB REPAIR

### Step 1: Remove Missing Scripts

**Using CardPrefabValidator**:
1. Open Unity Editor
2. Go to `Card Game > Validate Card Prefabs`
3. Click "Scan All Card Prefabs"
4. Review validation results
5. Click "Fix All Issues" to auto-remove missing scripts

**Manual Method**:
1. Open `Assets/PreFabs/NewCardPrefab.prefab` in Prefab mode
2. Select root GameObject
3. In Inspector, find any "Missing Script" components
4. Click the three-dots menu on each missing script
5. Select "Remove Component"
6. Repeat for all children (especially CardBackVisual)
7. Save prefab
8. Repeat for `NewCardPrefabOpp.prefab`

### Step 2: Verify Required Components

**For NewCardPrefab**:
1. Open `Assets/PreFabs/NewCardPrefab.prefab` in Prefab mode
2. Select root GameObject
3. Verify these components exist:
   - ✅ `NewCardUI` component
   - ✅ `RectTransform` component
   - ✅ `CanvasGroup` component (optional - auto-created if missing)

4. If missing, add manually:
   - Right-click in Inspector → `Add Component` → `UI` → `Canvas Group`

5. Save prefab

**For NewCardPrefabOpp**:
- Same steps as NewCardPrefab

### Step 3: Fix CardBackVisual

**For Prefab (if CardBackVisual exists)**:
1. Open prefab in Prefab mode
2. Find `CardBackVisual` GameObject (under BackContainer)
3. Select CardBackVisual
4. Remove any "Missing Script" components
5. Verify it has:
   - ✅ `Image` component (for UI cards) OR
   - ✅ `SpriteRenderer` component (for 2D cards)
6. Remove if it has both (should have only one)
7. Save prefab

**Note**: If CardBackVisual doesn't exist in prefab, it will be created at runtime automatically.

### Step 4: Verify Child Structure

**FrontContainer**:
- Optional in prefab (created at runtime if missing)
- If exists, should contain visual elements
- Can leave empty - will be populated at runtime

**BackContainer**:
- Optional in prefab (created at runtime if missing)
- If exists, should contain CardBackVisual
- Can leave empty - will be created at runtime

**Visual Elements**:
- Can exist at root level or in FrontContainer
- Will be moved to FrontContainer automatically at runtime
- Serialized field references will be auto-assigned

### Step 5: Assign Serialized Fields (Optional)

**Note**: All serialized fields can remain null - they will auto-setup at runtime.

**If you want to assign manually**:
1. Open prefab in Prefab mode
2. Select root GameObject
3. In NewCardUI component Inspector:
   - `Card Background`: Drag SpriteRenderer from hierarchy
   - `Artwork`: Drag SpriteRenderer from hierarchy
   - `Card Name Text`: Drag TextMeshProUGUI from hierarchy
   - `Description Text`: Drag TextMeshProUGUI from hierarchy
   - `Top Stat Text`: Drag TextMeshProUGUI from hierarchy
   - `Right Stat Text`: Drag TextMeshProUGUI from hierarchy
   - `Down Stat Text`: Drag TextMeshProUGUI from hierarchy
   - `Left Stat Text`: Drag TextMeshProUGUI from hierarchy
   - `Card Type Text`: Drag TextMeshProUGUI from hierarchy
   - `Card Type Icon`: Drag SpriteRenderer from hierarchy
   - `Flip Animation`: Drag CardFlipAnimation component (optional)
   - `Front Container`: Drag FrontContainer GameObject (optional)
   - `Back Container`: Drag BackContainer GameObject (optional)

4. Save prefab

---

## PREFAB VALIDATION CHECKLIST

### Prefab: NewCardPrefab

- [ ] No missing script components on root GameObject
- [ ] No missing script components on CardBackVisual (if exists)
- [ ] NewCardUI component exists on root
- [ ] RectTransform component exists on root
- [ ] CanvasGroup component exists on root (optional - auto-created)
- [ ] FrontContainer exists or will be created at runtime
- [ ] BackContainer exists or will be created at runtime
- [ ] CardBackVisual has Image OR SpriteRenderer (not both)
- [ ] CardBackVisual has NO scripts attached
- [ ] All visual elements are children of FrontContainer (or root)

### Prefab: NewCardPrefabOpp

- [ ] Same checklist as NewCardPrefab
- [ ] Prefab name is "NewCardPrefabOpp"

---

## RUNTIME AUTO-SETUP

**NewCardUI automatically creates missing elements at runtime**:

1. **CanvasGroup**: Created in `Awake()` if missing
2. **FrontContainer**: Created in `AutoSetupContainers()` if missing
3. **BackContainer**: Created in `AutoSetupContainers()` if missing
4. **CardBackVisual**: Created in `AutoSetupContainers()` if missing
5. **Visual Elements**: Moved to FrontContainer automatically if at root level
6. **Serialized Fields**: Auto-assigned if elements exist in hierarchy

**Result**: Prefabs can be minimal - everything will auto-setup at runtime!

---

## FINAL PREFAB STATE

**Minimum Required**:
- Root GameObject with `NewCardUI` component
- `RectTransform` component (auto-added)
- Visual elements (can be anywhere in hierarchy)

**Everything Else**:
- Optional in prefab
- Auto-created at runtime if missing
- Auto-setup by `NewCardUI.Awake()` and `AutoSetupContainers()`

**Best Practice**:
- Keep prefabs minimal
- Let runtime auto-setup handle containers and missing components
- Only manually assign serialized fields if you want specific assignments

