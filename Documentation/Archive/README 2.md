# Card Game Project - Unity

## Project Structure

This is a modular card game architecture built in Unity. The project follows clean code principles with clear separation of concerns.

### Folder Structure

```
Assets/
├── Scripts/
│   ├── Core/           # Core game logic (Card, Deck)
│   ├── Data/           # ScriptableObjects and data models
│   ├── Managers/       # Game systems (GameManager, DeckManager)
│   ├── UI/             # UI components (CardUI, HandUI, EntityUI, GameUI)
│   ├── Entities/       # Game entities (Player, Enemy, Entity base)
│   └── Utils/          # Utility classes (CardEffectExecutor)
├── Data/
│   └── Cards/          # Card ScriptableObjects will go here
├── Scenes/             # Unity scenes
└── Sprite/             # Sprites and artwork
```

## Architecture Overview

### Core Systems

#### 1. **Card System**
- **CardData** (ScriptableObject): Defines card properties (name, cost, effects, artwork)
- **Card** (Runtime): Instance of a card with modifiable stats
- **Deck**: Collection manager for cards (deck, hand, discard pile)

#### 2. **Game Management**
- **GameManager**: Singleton controlling game flow and turn system
- **DeckManager**: Manages player's deck, hand, and discard pile operations

#### 3. **Entities**
- **Entity** (Base): Abstract class for all combat entities
- **Player**: Player-controlled entity with mana and card actions
- **Enemy**: AI-controlled entity with intent system

#### 4. **UI System**
- **CardUI**: Visual representation of cards with drag-and-drop
- **HandUI**: Manages card layout in player's hand
- **EntityUI**: Displays health, mana, and status for entities
- **GameUI**: Main game interface controller

### Key Features

✅ **Turn-Based System**: Player turn → Enemy turn cycle
✅ **Card Management**: Draw, play, discard with auto-reshuffle
✅ **Combat System**: Damage, healing, shield mechanics
✅ **Status Effects**: Poison, burn, stun
✅ **Enemy AI**: Intent system showing next action
✅ **Mana System**: Resource management for playing cards
✅ **ScriptableObject Data**: Easy card creation via Unity Inspector

## Game Flow

1. **Menu** → Start Game
2. **Preparing** → Initialize deck, shuffle, draw starting hand
3. **Player Turn**:
   - Draw cards
   - Play cards by dragging
   - End turn
4. **Enemy Turn**:
   - Execute enemy intent
   - Show next intent
5. **Victory/Defeat** → Game ends

## Creating Cards

1. Right-click in `Assets/Data/Cards/`
2. Select `Create > Card Game > Card Data`
3. Configure card properties:
   - **Name & Description**: Card identity
   - **Mana Cost**: Resource cost to play
   - **Card Type**: Attack, Defense, Spell, Heal, Buff, Debuff
   - **Stats**: Attack/Defense/Heal values
   - **Effects**: Additional effects with targets
   - **Artwork**: Card art sprite

## Card Types

- **Attack**: Deals damage to enemies
- **Defense**: Grants shield to player
- **Heal**: Restores player health
- **Spell**: Custom effects
- **Buff**: Positive effects on player
- **Debuff**: Negative effects on enemies

## Effect Types

- **Damage**: Direct damage
- **Heal**: Restore health
- **Shield**: Temporary armor
- **DrawCard**: Draw additional cards
- **Poison**: Damage over time (decreases)
- **Burn**: Damage over time (stacks)
- **Stun**: Skip enemy turn

## Next Steps

### To Complete the Game:

1. **Scene Setup**:
   - Create game scene with UI layout
   - Add GameManager GameObject
   - Add DeckManager GameObject
   - Add Player and Enemy GameObjects
   - Set up UI canvas with HandUI, EntityUI, GameUI

2. **Card Creation**:
   - Create card artwork sprites
   - Build starting deck using ScriptableObjects
   - Assign cards to DeckManager

3. **UI Polish**:
   - Design card prefab with TextMeshPro
   - Create health/mana bars
   - Add intent icons for enemies
   - Design menu/victory/defeat screens

4. **Balance & Content**:
   - Design card synergies
   - Create multiple enemy types
   - Add difficulty scaling
   - Implement rewards system

5. **Advanced Features** (Optional):
   - Card rarities and collection
   - Deck building
   - Multiple encounters
   - Save/Load system
   - Sound effects and music
   - Animations and VFX

## Dependencies

- **TextMeshPro**: Required for UI text (included in Unity)
- **Unity UI**: Canvas-based UI system

## Notes

- All scripts use proper namespaces (`CardGame.*`)
- Event-driven architecture for loose coupling
- Ready for extension (new card types, effects, entities)
- ScriptableObject pattern for data-driven design

