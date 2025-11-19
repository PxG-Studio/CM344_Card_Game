# Card Game Architecture Documentation

## System Overview

This card game follows a modular, event-driven architecture designed for scalability and maintainability.

## Core Patterns

### 1. Singleton Pattern
- **GameManager**: Central game state controller
- Ensures single source of truth for game state
- Thread-safe with DontDestroyOnLoad

### 2. ScriptableObject Pattern
- **CardData**: Data-driven card design
- Allows designers to create cards without code
- Memory efficient (shared references)

### 3. Event System
- Decoupled communication between systems
- Uses C# Actions/Events
- Examples:
  - `OnCardDrawn`, `OnCardPlayed`
  - `OnHealthChanged`, `OnManaChanged`
  - `OnGameStateChanged`

### 4. Object Pooling (Future)
- Card UI can be pooled for performance
- Particle effects pooling
- Audio source pooling

## Data Flow

### Card Play Flow
```
1. Player drags CardUI
2. CardUI.OnCardPlayed event fires
3. HandUI.HandleCardUIPlayed validates mana
4. Player.SpendMana() called
5. DeckManager.PlayCard() removes from hand
6. CardEffectExecutor.ExecuteCard() applies effects
7. Card moved to discard pile
8. HandUI.RemoveCardFromHand() updates visuals
9. HandUI.ArrangeCards() re-layouts hand
```

### Turn Flow
```
1. GameManager.ChangeState(PlayerTurn)
2. Player.OnTurnStart() → RefreshMana()
3. DeckManager draws cards (via event)
4. Player plays cards
5. Player clicks "End Turn"
6. GameManager.EndPlayerTurn()
7. GameManager.ChangeState(EnemyTurn)
8. Enemy.ExecuteIntent() performs action
9. Enemy.DetermineNextIntent() for next turn
10. GameManager.ChangeState(PlayerTurn) → loop
```

## Component Relationships

```
GameManager (Singleton)
├─> DeckManager
│   ├─> Deck (DrawPile)
│   ├─> Deck (Hand)
│   └─> Deck (DiscardPile)
├─> Player (Entity)
│   └─> EntityUI
├─> Enemy (Entity)
│   └─> EntityUI
└─> GameUI
    ├─> HandUI
    │   └─> CardUI (multiple)
    ├─> Menu Panel
    ├─> Game Panel
    ├─> Victory Panel
    └─> Defeat Panel
```

## State Machine

### Game States
```
Menu ──StartGame──> Preparing ──Initialize──> PlayerTurn
                                                   ↓
                                              EndPlayerTurn
                                                   ↓
Defeat <──PlayerDies── EnemyTurn <───────────────┘
  ↑                      │
  │                 EnemyAction
  │                      │
  └───AllEnemiesDie──> Victory
```

## Entity System

### Base Entity
- Health management with events
- Mana system
- Shield (temporary armor)
- Status effects (poison, burn, stun)
- Event-driven updates

### Player
- Subscribes to turn events
- Mana refresh on turn start
- Card-based actions

### Enemy
- Intent system (preview next action)
- Simple AI behavior
- Automatic action execution

## Card System Details

### Card Data (ScriptableObject)
```
CardData
├─ Metadata (name, description, artwork)
├─ Stats (manaCost, attack, defense, heal)
├─ Type (Attack, Defense, Spell, etc.)
├─ Rarity (Common → Legendary)
└─ Effects[] (multiple effects per card)
```

### Card Instance (Runtime)
```
Card
├─ CardData reference
├─ InstanceID (unique per instance)
├─ Current stats (can be modified)
├─ IsPlayable flag
└─ IsExhausted flag
```

### Deck Management
- **Draw Pile**: Cards to be drawn
- **Hand**: Currently available cards
- **Discard Pile**: Played/discarded cards
- **Auto-reshuffle**: Discard → Draw when empty

## UI Architecture

### CardUI
- Visual representation of Card
- Drag-and-drop interaction
- Hover animations
- Play area detection
- Interactable state

### HandUI
- Card layout in arc formation
- Automatic spacing and rotation
- Card addition/removal
- Effect execution

### EntityUI
- Health bar with text
- Mana bar with text
- Shield indicator
- Enemy intent display

### GameUI
- Panel management
- Turn indicator
- Deck/discard counters
- Button handling

## Extension Points

### Adding New Card Types
1. Add enum to `CardType`
2. Implement logic in `CardEffectExecutor.ExecuteCard()`
3. Create CardData assets

### Adding New Effects
1. Add enum to `EffectType`
2. Implement in `CardEffectExecutor.ExecuteEffect()`
3. Handle in entity classes if needed

### Adding New Enemy Types
1. Inherit from `Enemy` class
2. Override `DetermineNextIntent()` for custom AI
3. Override `ExecuteIntent()` for special actions

### Adding Status Effects
1. Add field to `Entity` base class
2. Implement in `ApplyStatusEffects()`
3. Add UI indicator in `EntityUI`

## Performance Considerations

### Current Optimizations
- Events instead of Update() polling
- ReadOnly list wrappers prevent modifications
- Minimal allocations in hot paths

### Future Optimizations
- Object pooling for CardUI
- Async card animations
- Sprite atlasing
- Asset bundles for cards

## Testing Strategy

### Unit Tests (Future)
- Card stat calculations
- Deck shuffle randomness
- Effect execution
- Damage calculations

### Integration Tests (Future)
- Turn flow
- Card play sequence
- Win/lose conditions
- Discard pile reshuffle

## Debugging Tools

### Current
- Debug.Log statements throughout
- Inspector serialization of key data

### Recommended Additions
- Console commands (draw card, deal damage)
- Cheat panel (set health, add cards)
- Event log viewer
- State history

## Best Practices

### Code Style
- Use namespaces for organization
- XML documentation on public APIs
- Serialized fields for Unity Inspector
- Events for cross-system communication

### Unity Specifics
- ScriptableObjects for data
- Don't use Find() in Update()
- Cache component references
- Use events over SendMessage()

### Performance
- Avoid boxing in hot paths
- Use object pooling for frequently created objects
- Cache frequently accessed components
- Use coroutines for sequential actions

## Common Pitfalls to Avoid

1. **Don't**: Use string for card IDs → **Do**: Use InstanceID
2. **Don't**: Modify CardData at runtime → **Do**: Use Card instance
3. **Don't**: Use FindObjectOfType() every frame → **Do**: Cache references
4. **Don't**: Directly manipulate UI from game logic → **Do**: Use events
5. **Don't**: Hard-code card effects → **Do**: Use ScriptableObject data

