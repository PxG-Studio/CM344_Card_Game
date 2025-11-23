# BattleScreenMultiplayer Scene Analysis and Verification Plan

## Architecture Principles (Network Science Model)

This analysis follows CardFront's network-science architecture based on Watts-Strogatz small-world networks and Barabási preferential attachment models.

### Network Classification

**Hubs (Managers - Critical, Must Stay Stable)**:
- GameManager
- FateFlowController (TurnController equivalent)
- ScoreManager
- GameEndManager
- GameStatsTracker
- CoinTossManager (if implemented)
- MCPUnityServer

**Clusters (Local Groups - Should Stay Isolated)**:
- Card prefabs (NewCardPrefab, NewCardPrefabOpp)
- Board tiles (CardDropArea1 instances)
- UI panels (P1Panel, P2Panel, GameEndUI)
- Player hand containers (NewHandUI, NewHandOppUI)
- Animation systems (CardFlipAnimation)

**Shortcuts (Events/Channels - Use Sparingly)**:
- ScriptableObject event channels
- C# events (OnFateChanged, OnCardDrawn, OnScoreUpdated)
- UnityEvent callbacks
- MCP Unity messages

### Architectural Rules
1. **Hub Safety**: Hubs must stay minimal - large hubs need helper classes
2. **Cluster Isolation**: Clusters should not use FindObjectOfType or reference global managers directly
3. **Shortcut Control**: Events must be explicit, logged, and prevent double-firing
4. **No Hidden Dependencies**: No quiet singletons or static references
5. **Prefab Integrity**: Never break prefab bindings - create wrappers instead

### Network Science Debugging Approach
When fixing issues, classify as:
- **Cluster Bug**: Fix locally, don't touch hubs
- **Shortcut Bug**: Check event propagation, logging, null safety
- **Hub Bug**: Minimal changes, review carefully, test thoroughly

## Phase 1: Scene Structure Analysis

### 1.1 Load and Inspect Scene
- Load `Assets/Scenes/BattleScreenMultiplayer.unity`
- Document scene hierarchy structure
- Identify all root GameObjects and their purposes
- Map component dependencies and references
- **Classify each component as Hub, Cluster, or Shortcut**

### 1.2 Verify Core GameObjects (Hub Analysis)
- **HUDOverlayCanvas**: Verify exists with HUDSetup component (Cluster boundary)
- **Drop Areas**: Verify 16 CardDropArea1 components exist (4x4 grid - Cluster)
- **Managers (Hubs)**: Verify GameManager, ScoreManager, GameEndManager, GameStatsTracker exist
  - Check for hub-bloat (too many responsibilities)
  - Verify hub stability (minimal, focused responsibilities)
- **FateFlowController (Hub)**: Verify exists and is initialized
  - Check that it's the single source of truth for turn flow
  - Verify it uses events (shortcuts) properly
- **EventSystem**: Verify exists for UI interactions (Infrastructure)

### 1.3 Verify Card System GameObjects (Cluster Analysis)
- **Player 1 System (Cluster)**: 
  - NewDeckManager GameObject (Cluster hub)
  - NewHandUI GameObject (hand container - Cluster)
  - NewCardSystemTester (if present - Testing)
- **Player 2 System (Cluster)**:
  - NewDeckManagerOpp GameObject (Player 2's deck manager - Cluster hub)
  - NewHandOppUI GameObject (Player 2's hand container - Cluster)
  - NewCardSystemOpposition (if present - Player 2's test system)
- **Verify cluster isolation**: Check that clusters don't directly reference global managers

**Note**: In the codebase, "Opponent" terminology refers to Player 2 in the PvP context. Each player has their own independent interaction system (separate clusters).

## Phase 2: Prefab Asset Cleanup

### 2.1 Remove Prefab Assets from Scene
- Use MCP Unity tools to find and remove prefab assets in scene hierarchy
- Target: `NewCardPrefab` and `NewCardPrefabOpp` (without "(Clone)" suffix)
- These should only exist as prefab assets in `Assets/PreFabs/`, not in scene
- Verify removal using `RemovePrefabAssetsFromScene` tool logic
- **Network Classification**: Prefab assets are Cluster templates - must maintain integrity

### 2.2 Verify Prefab Assets Status
- Check `Assets/PreFabs/NewCardPrefab.prefab` is active (root GameObject)
- Check `Assets/PreFabs/NewCardPrefabOpp.prefab` is active (root GameObject)
- Run `EnsurePrefabAssetsActive` tool if needed
- **Verify**: No serialized field changes that break prefab bindings

## Phase 3: Component Setup Verification

### 3.1 HUD System Verification (Cluster Boundary)
- Verify HUDSetup component on HUDOverlayCanvas executes properly
- Check HUDManager component exists and all references are wired:
  - P1Panel and P2Panel exist (UI Clusters)
  - All text labels (score, hand/deck, player labels)
  - Turn indicators for both players
  - Tiles remaining label
  - Deck manager references (should use events, not direct references)
- **Verify**: HUDManager doesn't directly reference hubs - uses events

### 3.2 Card System Component Verification (Cluster Verification)
- **NewDeckManager** (Player 1 - Cluster hub): Verify starting deck configured
- **NewDeckManagerOpp** (Player 2 - Cluster hub): Verify starting deck configured
- **NewHandUI** (Player 1 - Cluster): Verify cardPrefab reference set to NewCardPrefab
- **NewHandOppUI** (Player 2 - Cluster): Verify cardPrefab reference set to NewCardPrefabOpp
- **CardFactory** (Cluster utility): Verify prefab references correct for both players
- **Verify**: Clusters don't use FindObjectOfType for hub references

### 3.3 Board System Verification (Cluster Verification)
- All 16 CardDropArea1 components have:
  - BoxCollider2D with isTrigger = true
  - Proper position in 4x4 grid
  - Unique drop area names (DropArea1 through DropArea16)
- CardDropArea1 has all manager references (should use events):
  - ScoreManager (Hub - via events)
  - GameEndManager (Hub - via events)
  - FateFlowController (Hub - via events)
- **Verify**: CardDropArea1 communicates with hubs via events only, not direct references

### 3.4 Coin Toss System Verification (New Feature - Hub Verification)
- Verify CoinTossManager component exists (needs to be created - Hub)
- Verify coin toss UI exists in scene:
  - Coin toss panel/overlay (Cluster)
  - Heads/Tails visual representation
  - Result display text
  - Animation/visual effect for coin flip
- Verify integration with FateFlowController:
  - Coin toss result determines starting side (Player 1 or Player 2)
  - FateFlowController.SetFate() called with result (Hub-to-Hub via event)
- **Verify**: CoinTossManager (Hub) stays minimal - only coin toss logic

## Phase 4: System Integration Verification (Network Science Analysis)

### 4.1 Initialization Order Verification (Hub Dependencies)
**Verify hub initialization order and stability**:
- HUDSetup (Awake, execution order -100) - Cluster boundary setup
- GameManager (Hub - Start) - Must initialize before all other hubs
- Coin Toss System (Hub - if implemented) - runs before deck initialization
- FateFlowController (Hub - Awake) - initialized but waiting for coin toss result
- Deck Managers (Cluster hubs - InitializeDeck) - after coin toss
- Hand UIs (Cluster - Awake)
- Card Systems (Cluster - Start)

**Verify**: No circular hub dependencies, hubs initialize before clusters depend on them

### 4.2 Reference Chain Verification (Network Path Analysis)
**Map communication paths and verify shortcut usage**:

**Hub-to-Hub Communication** (Must use events):
- Coin Toss System (Hub) → FateFlowController (Hub) - sets starting side via SetFate()
- Verify: Uses events, not direct references

**Hub-to-Cluster Communication** (Should use events):
- FateFlowController (Hub) → HUDManager (Cluster) - updates turn indicators
- Verify: Uses OnFateChanged event

**Cluster Internal Communication** (Can be direct):
- Deck Managers (Cluster hub) → Hand UIs (Cluster) - card drawn events
- Hand UIs (Cluster) → CardFactory (Cluster utility) - card creation
- CardFactory (Cluster utility) → NewCardUI prefabs (Cluster) - instantiation
- Cards (Cluster) → CardMover/CardMoverOpp (Cluster component) - drag components

**Cluster-to-Hub Communication** (Must use events):
- Cards (Cluster) → CardDropArea1 (Cluster boundary) - drop zones
- CardDropArea1 (Cluster boundary) → Managers (Hubs) - score, game end
- Verify: Uses event channels or events, not direct references

**Check for violations**:
- Clusters directly referencing hubs (violation)
- Hidden dependencies through static/singleton (violation)
- Unlogged shortcuts (violation)

### 4.3 Event System Verification (Shortcut Infrastructure)
**Verify shortcut infrastructure and safety**:
- EventSystem exists in scene (Unity infrastructure)
- GraphicRaycaster on Canvas (UI infrastructure)
- UI cards can receive pointer events (UI input shortcuts)
- Coin toss UI can receive click events (if implemented)
- **Verify all shortcuts are logged**: Check for structured logging on events
- **Verify no double-firing**: Check event subscription/unsubscription patterns
- **Verify null listener safety**: Check event invocation safety

### 4.4 Network Architecture Compliance Check
**Verify adherence to network science principles**:

**Hub Bloat Check**:
- [ ] GameManager responsibilities are minimal
- [ ] FateFlowController only handles turn flow
- [ ] ScoreManager only handles scoring
- [ ] No hub has >10 direct responsibilities

**Cluster Isolation Check**:
- [ ] Card prefabs don't use FindObjectOfType
- [ ] Hand UIs don't directly reference global managers
- [ ] Board tiles communicate via events only
- [ ] Clusters use interfaces or event channels for hub communication

**Shortcut Safety Check**:
- [ ] All events have structured logging
- [ ] No events fire without null checks
- [ ] Event subscriptions are properly cleaned up
- [ ] No circular event dependencies

**Prefab Integrity Check**:
- [ ] No serialized field renames that break prefabs
- [ ] Event references maintained in prefabs
- [ ] Component references preserved

## Phase 5: Game Flow Testing

### 5.1 Coin Toss System Testing (New Feature)
**Requirement**: Visual coin toss to decide who goes first (heads/tails for each player interaction)

- Coin toss UI appears at game start
- Coin toss animation plays (spinning/flipping coin)
- Heads/Tails result is displayed clearly
- Result determines starting player:
  - Heads = Player 1 starts
  - Tails = Player 2 starts (or vice versa - needs definition)
- FateFlowController receives result and sets starting side (Hub-to-Hub via event)
- Coin toss UI disappears after result
- Game proceeds with winner of coin toss as active player
- **Network Classification**: CoinTossManager (Hub) → FateFlowController (Hub) via event

### 5.2 Initial Game State
- Coin toss determines starting player
- Deck initialization (10 cards per player)
- Initial hand draw (5 cards each)
- Cards appear in hand UI (Cluster)
- Turn indicator shows winner of coin toss as active (Cluster receives Hub event)

### 5.3 Card Interaction Testing (Player 1 vs Player 2 PvP)
**Note**: Player 1 vs Player 2 PvP - each player has their own interaction system. No separate "Opponent" entity exists. Both players use the same window/interface but each has independent controls.

- Starting player (determined by coin toss) card dragging works (their own interaction system - Cluster)
- Starting player card drops on valid drop area (Cluster boundary - CardDropArea1)
- Starting player card placement on board
- Turn advances to other player (Hub - FateFlowController.AdvanceFateFlow())
- Other player card dragging works (their own interaction system - Cluster)
- Other player card placement on board
- Turn advances back to starting player (Hub - FateFlowController.AdvanceFateFlow())
- Verify turn indicators update correctly for both players (Cluster receives Hub event)
- **Verify**: Cluster-to-Hub communication uses events, not direct references

### 5.4 Score and Battle System
- Card battles trigger correctly
- Score updates when captures occur (Cluster → Hub via event)
- Board occupancy tracking works
- Tiles remaining updates correctly
- Score display shows Player 1 and Player 2 scores (not "Player" vs "Opponent")
- **Verify**: ScoreManager (Hub) receives updates via events only

### 5.5 Game End Condition
- Game end triggers when 10 cards played (5 per player)
- GameEndUI displays correctly with Player 1 vs Player 2 terminology
- Score calculation works (Hub - GameEndManager)
- Statistics display correctly
- **Verify**: GameEndManager (Hub) triggers via events

## Phase 6: Coin Toss Feature Implementation (If Not Present)

### 6.1 Create CoinTossManager Component (New Hub)
**Architecture**: This is a Hub (Manager) - must stay minimal and stable

- Create new script: `Assets/Scripts/Current/Univ-Managers/CoinTossManager.cs`
- Singleton pattern (like other managers)
- **Network Classification**: Hub (Manager)
- **Responsibilities** (Must stay minimal):
  - `PerformCoinToss()` - random heads/tails result
  - `GetResult()` - returns starting player (Player 1 or Player 2)
  - Event (Shortcut): `OnCoinTossComplete(FateSide startingSide)`
- **Integration Points**:
  - Communicates with FateFlowController (Hub-to-Hub via event)
  - Communicates with CoinTossUI (Hub-to-Cluster via event)
- **Logging**: All coin toss operations must be logged: `Log.Info("[CoinTossManager] Coin toss performed: {result}")`

### 6.2 Create Coin Toss UI (New Cluster)
**Architecture**: This is a Cluster (UI) - should be isolated

- Create UI panel in HUDOverlayCanvas or separate Canvas
- **Network Classification**: Cluster (UI Panel)
- Visual elements:
  - Coin sprite/image that can be flipped/rotated
  - "Heads" and "Tails" labels/indicators
  - Result display text
  - Animation component for coin flip effect
- **Isolation Rules**:
  - Should NOT directly reference CoinTossManager (Hub)
  - Should communicate via events only
  - Should subscribe to CoinTossManager.OnCoinTossComplete event
- Integrate with CoinTossManager through event subscription

### 6.3 Integrate with FateFlowController (Hub-to-Hub Communication)
**Architecture**: Hub-to-Hub communication - must use events

- CoinTossManager.OnCoinTossComplete event (Shortcut)
- Subscribe in GameManager or appropriate initialization point
- Call FateFlowController.SetFate() with result
- **Verify**: FateFlowController doesn't hardcode starting side - uses result from coin toss
- **Logging**: `Log.Info("[CoinTossManager] Setting starting fate to: {startingSide}")`
- **Verify**: Event is logged and null-safe

### 6.4 Update GameManager Initialization (Hub Modification)
**Architecture**: Hub change - must be minimal and carefully reviewed

- Call coin toss before deck initialization
- Wait for coin toss result before starting game
- Update StartFirstTurn() to use coin toss result
- **Verify**: No new responsibilities added to GameManager (hub-bloat check)
- **Verify**: Uses events, not direct references to CoinTossManager
- **Logging**: All initialization steps must be logged

### 6.5 Network Science Compliance for Coin Toss
**Verify during implementation**:
- [ ] CoinTossManager (Hub) stays minimal - only coin toss logic
- [ ] CoinTossUI (Cluster) doesn't reference hubs directly
- [ ] Hub-to-Hub communication uses events
- [ ] All shortcuts (events) are logged
- [ ] No hidden dependencies created
- [ ] Prefab integrity maintained (if coin toss UI is prefabbed)

## Phase 7: Console Log Analysis

### 7.1 Expected Logs (Normal Operation)
**Verify structured logging follows network science principles**:

- MCP Unity server started
- HUDSetup initialization messages
- Coin toss result (if implemented): `[CoinTossManager] Coin toss performed: {result}`
- Deck initialization messages
- Card creation messages
- Turn advancement messages (Player 1 ↔ Player 2): `[FateFlowController] Fate changed to: {side}`
- Game end check messages
- **Verify**: All hub operations are logged with `[HubName]` prefix
- **Verify**: All shortcut (event) operations are logged

### 7.2 Warning/Error Resolution
- **Prefab Asset Warnings**: Should be resolved after Phase 2
- **Missing Reference Warnings**: Verify and fix component references (prefab integrity)
- **Null Reference Exceptions**: Trace and fix missing dependencies (hidden dependency violation)
- **Event System Warnings**: Ensure EventSystem and GraphicRaycaster exist (shortcut infrastructure)
- **Coin Toss Warnings**: Verify CoinTossManager integration if implemented (hub verification)
- **Hub Bloat Warnings**: Check if managers have too many responsibilities
- **Cluster Isolation Violations**: Verify clusters don't directly reference hubs

## Phase 8: Final Verification Checklist

### 8.1 Scene Cleanup Verification
- [ ] No prefab assets in scene hierarchy
- [ ] All prefab instances are "(Clone)" variants
- [ ] Prefab assets in `Assets/PreFabs/` are active
- [ ] Prefab integrity maintained (no broken serialized references)

### 8.2 Component Verification
- [ ] All managers (Hubs) exist and initialized
- [ ] All UI components (Clusters) wired correctly
- [ ] All card systems (Clusters) functional
- [ ] Board drop areas (Cluster boundaries) configured
- [ ] Coin toss system (Hub + Cluster) functional (if implemented)

### 8.3 System Integration (Network Architecture Compliance)
- [ ] Initialization order correct (Hubs before Clusters)
- [ ] Event system working (Shortcuts functional)
- [ ] Card dragging/dropping functional for both players (Cluster isolation maintained)
- [ ] Turn system working (Player 1 ↔ Player 2) - FateFlowController (Hub) controls flow
- [ ] Score system working (Player 1 vs Player 2) - ScoreManager (Hub) via events
- [ ] Coin toss determines starting player (if implemented) - CoinTossManager (Hub) → FateFlowController (Hub)
- [ ] Game end triggers correctly - GameEndManager (Hub) via events
- [ ] No hub-bloat detected (managers stay minimal)
- [ ] Cluster isolation maintained (no direct hub references from clusters)
- [ ] All shortcuts (events) are logged and null-safe
- [ ] No hidden dependencies created

### 8.4 Play Mode Testing
- [ ] Scene loads without errors
- [ ] Coin toss executes (if implemented)
- [ ] Starting player determined correctly
- [ ] Cards draw correctly for both players
- [ ] Cards can be dragged and placed by both players
- [ ] Turn indicators update correctly
- [ ] Score updates correctly (Player 1 vs Player 2)
- [ ] Game ends correctly

## Implementation Notes

### Tools and Methods
- Use MCP Unity tools to query scene structure
- Use MCP Unity tools to remove prefab assets
- Use MCP Unity tools to verify component setup
- Create detailed logging for each verification step
- Document any issues found and resolution steps

### Architecture Compliance
- **All fixes must follow network science principles**: Classify as Hub, Cluster, or Shortcut change
- **Hub changes require extra caution**: Small changes, minimal impact
- **Cluster changes should stay isolated**: No global dependencies
- **Shortcut changes must be explicit**: Logged and documented
- **Prefab integrity is critical**: Never break serialized references

### Game-Specific Notes
- **PvP Clarification**: Player 1 vs Player 2 - each player has independent interaction systems. Codebase uses "Opponent" terminology but in PvP context refers to Player 2.
- **Coin Toss**: New feature requirement - visual coin toss to decide starting player before game begins.
- **Turn Flow**: FateFlowController (Hub) owns turn flow - all turn changes must go through it
- **Board Placement**: CardDropArea1 (Cluster boundary) validates placement - cards verify through events only

### Network Science Debugging Approach
When fixing issues, classify as:
1. **Cluster Bug**: Fix locally, don't touch hubs
2. **Shortcut Bug**: Check event propagation, logging, null safety
3. **Hub Bug**: Minimal changes, review carefully, test thoroughly

### MCP Unity Safety
- All MCP connections must be async
- Never block main thread
- Route through adapter layer
- Log cleanly
- Never directly mutate game state

## Files to Examine

1. `Assets/Scenes/BattleScreenMultiplayer.unity` - Scene file
2. `Assets/Scripts/Current/UI/HUDSetup.cs` - HUD initialization (Cluster boundary)
3. `Assets/Scripts/Current/UI/HUDManager.cs` - HUD management (Cluster)
4. `Assets/Scripts/Current/NewCardUI.cs` - Card UI component (Cluster)
5. `Assets/Scripts/Current/Univ-Managers/FateFlowController.cs` - Turn flow controller (Hub)
6. `Assets/Scripts/Current/Univ-Managers/GameManager.cs` - Game state manager (Hub)
7. `Assets/Editor/RemovePrefabAssetsFromScene.cs` - Prefab cleanup tool
8. `Assets/Editor/EnsurePrefabAssetsActive.cs` - Prefab activation tool

## New Files to Create (If Coin Toss Not Implemented)

1. `Assets/Scripts/Current/Univ-Managers/CoinTossManager.cs` - Coin toss logic (Hub)
2. `Assets/Scripts/Current/UI/CoinTossUI.cs` - Coin toss UI controller (Cluster)
3. Coin toss UI prefab (if needed - Cluster template)
