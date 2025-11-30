# Handoff Document for Yakob

## Project: CM344 Card Game - NewCard System Implementation

**Date:** Current Session  
**From:** Development Team  
**To:** Yakob

---

## Overview

This document summarizes the implementation of a new card system with directional stats (Top, Right, Down, Left) that works alongside the existing card system. The system includes full UI integration, deck management, and board placement functionality.

---

## What Was Created

### 1. New Card Data System

#### `NewCardData.cs` (ScriptableObject)
- **Location:** `Assets/Scripts/New stuff by y cuz helost/`
- **Namespace:** `NewCardData`
- **Purpose:** ScriptableObject for defining cards with directional stats
- **Key Features:**
  - Directional stats: `TopStat`, `RightStat`, `DownStat`, `LeftStat`
  - Card types: Flame, Wind, Earth, Lightning
  - Effect types: Cyclone, Burn, Bloom, VoltSwitch
  - CreateAssetMenu: "Card Game/Directional Card Data"
- **Usage:** Right-click in Project → Create → Card Game → Directional Card Data

#### `NewCard.cs` (Runtime Wrapper)
- **Location:** `Assets/Scripts/New stuff by y cuz helost/`
- **Namespace:** `CardGame.Core`
- **Purpose:** Runtime instance of a NewCardData
- **Key Features:**
  - Wraps `NewCardData.NewCardData`
  - Runtime modifiable stats (CurrentTopStat, etc.)
  - InstanceID tracking
  - Stat modification methods

#### `NewDeck.cs` (Deck Manager)
- **Location:** `Assets/Scripts/New stuff by y cuz helost/`
- **Purpose:** Manages collection of NewCard instances
- **Features:** Add, remove, shuffle, draw functionality

---

### 2. Deck Management System

#### `NewDeckManager.cs` (MonoBehaviour)
- **Location:** `Assets/Scripts/New stuff by y cuz helost/`
- **Namespace:** `CardGame.Managers`
- **Purpose:** Manages deck, hand, and discard pile
- **Key Features:**
  - `startingDeck` list (assign NewCardData assets in Inspector)
  - `InitializeDeck()` - loads cards from startingDeck
  - `DrawCards(int count)` - draws cards from deck
  - `PlayCard(NewCard card)` - plays card and moves to discard
  - Events: `OnCardDrawn`, `OnCardPlayed`, `OnCardDiscarded`
  - Auto-reshuffle from discard pile

#### Setup:
1. Create GameObject → Add Component → NewDeckManager
2. Assign NewCardData assets to "Starting Deck" list in Inspector
3. Call `InitializeDeck()` then `DrawCards(5)` to test

---

### 3. UI System

#### `NewCardUI.cs` (Card Display)
- **Location:** `Assets/Scripts/New stuff by y cuz helost/`
- **Namespace:** `CardGame.UI`
- **Purpose:** Visual representation of NewCard
- **Key Features:**
  - Displays all card data (name, description, stats, artwork, color)
  - Drag and drop interaction
  - Hover effects (scale up, move up)
  - Play area detection
  - Implements Unity event interfaces (IPointerEnter, IDragHandler, etc.)

#### `NewHandUI.cs` (Hand Layout Manager)
- **Location:** `Assets/Scripts/New stuff by y cuz helost/`
- **Namespace:** `CardGame.UI`
- **Purpose:** Manages visual hand layout
- **Key Features:**
  - Subscribes to NewDeckManager events
  - Instantiates card prefabs when cards are drawn
  - Arc/fan layout for cards
  - Auto-arrangement on add/remove
  - **Important:** Doesn't destroy CardMover cards when played (keeps them on board)

#### Prefab Setup Required:
- Create NewCardUI prefab with UI elements:
  - Card Background (Image)
  - Artwork (Image)
  - CardNameText (TextMeshPro)
  - DescriptionText (TextMeshPro)
  - TopStatText, RightStatText, DownStatText, LeftStatText (TextMeshPro)
  - CardTypeText (TextMeshPro, optional)
- Assign all references in NewCardUI component Inspector

---

### 4. 2D Physics Card System (Board Cards)

#### `CardMover.cs` (2D Card Dragging)
- **Location:** `Assets/Scripts/New stuff by y cuz helost/`
- **Purpose:** Handles 2D physics-based card dragging
- **Key Features:**
  - Uses `OnMouseDown`, `OnMouseDrag`, `OnMouseUp`
  - Works with 2D Colliders (not UI)
  - Detects drop zones via `Physics2D.OverlapPoint`
  - **Auto-finds NewCard reference:**
    - Looks for NewCardUI component (same GameObject, children, or parent)
    - Falls back to name matching with cards in hand
    - Uses only card in hand if there's just one
  - Stores NewCard reference for playing

#### `CardDropArea1.cs` (Drop Zone)
- **Location:** `Assets/Scripts/New stuff by y cuz helost/`
- **Purpose:** Handles cards dropped on board slots
- **Key Features:**
  - Implements `ICardDropArea` interface
  - Auto-finds NewDeckManager
  - When card dropped:
    1. Snaps card to slot position
    2. Gets NewCard from CardMover
    3. Checks if card is in hand
    4. Calls `deckManager.PlayCard(card)`
  - **Settings:**
    - `playCardOnDrop` - toggle to play card automatically
    - `snapCardToPosition` - toggle to snap card to slot

#### `ICardDropArea.cs` (Interface)
- **Location:** `Assets/Scripts/New stuff by y cuz helost/`
- **Purpose:** Interface for drop zones
- **Method:** `void OnCardDrop(CardMover card)`

---

### 5. Testing Helper

#### `NewCardSystemTester.cs`
- **Location:** `Assets/Scripts/New stuff by y cuz helost/`
- **Namespace:** `CardGame.Testing`
- **Purpose:** Easy testing of NewCard system
- **Features:**
  - Auto-initialize on start
  - Auto-draw cards on start
  - Debug GUI buttons (editor only)
  - Manual testing methods

---

## Key Integration Points

### How Cards Flow Through the System

```
1. Create NewCardData assets (ScriptableObjects)
   ↓
2. Assign to NewDeckManager.startingDeck
   ↓
3. Call NewDeckManager.InitializeDeck()
   → Creates NewCard instances from NewCardData
   ↓
4. Call NewDeckManager.DrawCards(5)
   → Fires OnCardDrawn events
   ↓
5. NewHandUI receives event
   → Instantiates NewCardUI prefab
   → Calls cardUI.Initialize(card)
   ↓
6. NewCardUI.UpdateVisuals()
   → Displays all data from NewCardData
   ↓
7. User interacts:
   - Option A: Drag UI card → NewCardUI.PlayCard() → NewHandUI → NewDeckManager.PlayCard()
   - Option B: Drag 2D card → CardMover → CardDropArea1 → NewDeckManager.PlayCard()
```

---

## Important Fixes Made

### 1. Card Reference Finding
- **Problem:** CardMover couldn't find NewCard reference
- **Solution:** Added automatic finding via:
  - NewCardUI component search
  - Name matching with hand cards
  - Fallback to only card in hand

### 2. Card Destruction Issue
- **Problem:** Cards disappeared when played on board
- **Solution:** Updated `NewHandUI.RemoveCardFromHand()` to check for CardMover component
  - If card has CardMover → keep GameObject (board card)
  - If card is pure UI → destroy GameObject (hand card)

### 3. Namespace Conflicts
- **Problem:** `NewCardData` namespace conflicted with class name
- **Solution:** Use fully qualified names: `NewCardData.NewCardData`

### 4. Missing Using Statements
- **Problem:** `IReadOnlyList.Contains()` not found
- **Solution:** Added `using System.Linq;` to CardDropArea1.cs

---

## Current System Status

### ✅ Working:
- NewCardData ScriptableObject creation
- NewCard runtime wrapper
- NewDeckManager deck/hand/discard management
- NewCardUI display (when prefab is set up)
- NewHandUI hand layout
- CardMover 2D dragging
- CardDropArea1 drop zone with PlayCard integration
- Automatic card reference finding
- Cards stay on board when played (CardMover cards)

### ⚠️ Needs Setup:
- NewCardUI prefab creation (UI elements and references)
- Scene setup (NewDeckManager, NewHandUI GameObjects)
- NewCardData assets creation
- Card prefab setup (if using 2D CardMover system)

---

## Files Created/Modified

### New Files:
1. `NewCardData.cs` - ScriptableObject
2. `NewCard.cs` - Runtime wrapper
3. `NewDeck.cs` - Deck manager
4. `NewDeckManager.cs` - MonoBehaviour deck manager
5. `NewCardUI.cs` - UI display component
6. `NewHandUI.cs` - Hand layout manager
7. `NewCardSystemTester.cs` - Testing helper
8. `CardMover.cs` - 2D card dragging (updated)
9. `CardDropArea1.cs` - Drop zone (updated)
10. `ICardDropArea.cs` - Interface (existing, used by system)

### Modified Files:
- `CardMover.cs` - Added NewCard reference finding
- `CardDropArea1.cs` - Added PlayCard integration
- `NewHandUI.cs` - Prevented destruction of board cards

---

## How to Use the System

### For UI Cards (NewCardUI):
1. Create NewCardData assets
2. Assign to NewDeckManager.startingDeck
3. Create NewCardUI prefab with all UI elements
4. Assign prefab to NewHandUI.cardPrefab
5. Call `InitializeDeck()` then `DrawCards(5)`

### For 2D Board Cards (CardMover):
1. Create card GameObjects with CardMover component
2. CardMover will auto-find NewCard reference
3. Create CardDropArea1 GameObjects for drop zones
4. Assign NewDeckManager to CardDropArea1 (or let it auto-find)
5. Cards can be dragged and dropped on slots
6. Cards will be played automatically when dropped

---

## Known Issues / Future Work

1. **Card Reference Assignment:**
   - Currently relies on auto-finding or name matching
   - **Better solution:** Set card reference when instantiating cards
   - Consider adding a method to link CardMover to NewCard at creation time

2. **Two Card Systems:**
   - UI system (NewCardUI) and 2D system (CardMover) are separate
   - Consider unifying or creating a bridge between them

3. **Card Effects:**
   - `ApplyCardEffects()` in NewHandUI is placeholder
   - Needs implementation for directional stats gameplay

4. **Slot Scaling:**
   - Previous implementation for slot scaling was removed
   - Can be re-added if needed

---

## Testing Checklist

- [ ] NewCardData assets can be created
- [ ] NewDeckManager initializes deck correctly
- [ ] Cards can be drawn and appear in hand
- [ ] NewCardUI prefab displays all data correctly
- [ ] Cards can be dragged and played (UI system)
- [ ] CardMover cards can be dragged
- [ ] Cards can be dropped on CardDropArea1
- [ ] Cards are played when dropped
- [ ] Cards stay on board after being played
- [ ] Cards are removed from hand when played

---

## Git Status

All changes have been committed and pushed to:
- `main` branch
- `develop` branch  
- `prototype` branch
- `develop-2` branch (newly created)

---

## Contact / Questions

If you have questions about:
- **NewCard system setup** → Check this document
- **CardMover integration** → See CardDropArea1.cs and CardMover.cs
- **UI prefab setup** → See NewCardUI.cs comments
- **Deck management** → See NewDeckManager.cs

---

## Quick Reference

### Key Classes:
- `NewCardData` - ScriptableObject (card definition)
- `NewCard` - Runtime card instance
- `NewDeckManager` - Deck/hand management
- `NewCardUI` - UI card display
- `NewHandUI` - Hand layout
- `CardMover` - 2D card dragging
- `CardDropArea1` - Board drop zone

### Key Methods:
- `NewDeckManager.InitializeDeck()` - Load deck
- `NewDeckManager.DrawCards(int)` - Draw cards
- `NewDeckManager.PlayCard(NewCard)` - Play card
- `NewCardUI.Initialize(NewCard)` - Setup card display
- `CardMover.SetCard(NewCard)` - Manually set card reference
- `CardDropArea1.OnCardDrop(CardMover)` - Handle card drop

---

**End of Handoff Document**

