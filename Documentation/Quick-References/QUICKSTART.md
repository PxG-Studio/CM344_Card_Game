# Quick Start Guide

## Getting Started with Your Card Game

### Step 1: Open Unity Editor
1. Open the project in Unity (2021.3 or later recommended)
2. Wait for scripts to compile
3. Open the `SampleScene` in `Assets/Scenes/`

### Step 2: Set Up the Scene

#### Create GameManager
1. Create empty GameObject: `GameObject > Create Empty`
2. Rename to "GameManager"
3. Add component: `GameManager` script
4. Configure settings:
   - Max Hand Size: 7
   - Cards Drawn Per Turn: 3
   - Starting Hand Size: 5

#### Create DeckManager
1. Create empty GameObject: `GameObject > Create Empty`
2. Rename to "DeckManager"
3. Add component: `DeckManager` script
4. Leave Starting Deck empty for now (we'll add cards later)

#### Create Player
1. Create empty GameObject: `GameObject > Create Empty`
2. Rename to "Player"
3. Add component: `Player` script
4. Configure:
   - Max Health: 100
   - Max Mana: 3

#### Create Enemy
1. Create empty GameObject: `GameObject > Create Empty`
2. Rename to "Enemy"
3. Add component: `Enemy` script
4. Configure:
   - Entity Name: "Goblin"
   - Max Health: 50
   - Max Mana: 0

### Step 3: Set Up UI

#### Create Canvas
1. `GameObject > UI > Canvas`
2. Set Canvas Scaler to "Scale With Screen Size"
3. Reference Resolution: 1920x1080

#### Create HandUI
1. Under Canvas, create empty GameObject: "HandUI"
2. Add component: `HandUI` script
3. Create CardPrefab (see below)
4. Set CardContainer to HandUI itself

#### Create CardPrefab
1. `GameObject > UI > Image` (under Canvas for now)
2. Rename to "CardPrefab"
3. Add component: `CardUI` script
4. Add child UI elements:
   - Background (Image)
   - Artwork (Image)
   - CardName (TextMeshPro)
   - Description (TextMeshPro)
   - ManaCost (TextMeshPro)
   - AttackValue (TextMeshPro)
   - DefenseValue (TextMeshPro)
5. Assign references in CardUI component
6. Drag to Project to create prefab
7. Delete from scene
8. Assign prefab to HandUI

#### Create GameUI
1. Under Canvas, create empty GameObject: "GameUI"
2. Add component: `GameUI` script
3. Create panels:
   - **MenuPanel**: Panel with "Start Game" button
   - **GamePanel**: Panel with turn info, deck counts, "End Turn" button
   - **VictoryPanel**: Panel with "You Win!" text
   - **DefeatPanel**: Panel with "You Lose!" text
4. Assign all references in GameUI component

#### Create EntityUI for Player
1. Under Canvas, create empty GameObject: "PlayerUI"
2. Add component: `EntityUI` script
3. Add child elements:
   - Health Bar (Slider)
   - Health Text (TextMeshPro)
   - Mana Bar (Slider)
   - Mana Text (TextMeshPro)
   - Shield Text (TextMeshPro)
4. Assign Target Entity to Player
5. Assign all UI references

#### Create EntityUI for Enemy
1. Duplicate PlayerUI → "EnemyUI"
2. Add Intent Panel with:
   - Intent Icon (Image)
   - Intent Value (TextMeshPro)
3. Assign Target Entity to Enemy
4. Assign all UI references including intent elements

### Step 4: Create Your First Cards

1. In Project, navigate to `Assets/Data/Cards/`
2. Right-click > `Create > Card Game > Card Data`
3. Name it "Strike"
4. Configure:
   - Card Name: "Strike"
   - Description: "Deal 6 damage"
   - Mana Cost: 1
   - Card Type: Attack
   - Attack Value: 6
   - Card Color: Red

5. Create more cards:
   - **Defend**: Defense type, 5 defense, 1 mana, Blue
   - **Heal**: Heal type, 8 heal value, 1 mana, Green
   - **Fireball**: Attack type, 12 damage, 2 mana, Orange

6. Assign cards to DeckManager's Starting Deck:
   - Add 5x Strike
   - Add 4x Defend
   - Add 1x Fireball

### Step 5: Test the Game

1. Press Play in Unity
2. Click "Start Game" button
3. Cards should appear in your hand
4. Drag cards upward to play them
5. Click "End Turn" to end your turn
6. Enemy should execute their action
7. Your turn starts again

### Troubleshooting

#### Cards Not Appearing
- Check DeckManager has cards in Starting Deck
- Check HandUI has CardPrefab assigned
- Check Console for errors

#### Can't Play Cards
- Check Player has enough mana
- Check CardUI is interactable
- Check drag threshold and play area detection

#### UI Not Showing
- Check Canvas is set to Screen Space - Overlay
- Check GameUI references are assigned
- Check panels are active in hierarchy

#### Game State Not Changing
- Check GameManager is in scene
- Check GameUI is listening to events
- Check Console for state change logs

### Next Steps

1. **Add Card Artwork**: Import sprites and assign to cards
2. **Polish UI**: Style cards, add backgrounds, animations
3. **Add More Cards**: Create interesting card combinations
4. **Improve Enemy AI**: Customize enemy behaviors
5. **Add Visual Effects**: Particle systems for damage/heal
6. **Add Sound**: Audio for card play, damage, etc.

## Keyboard Shortcuts (Optional - Add Later)

- `Space`: End Turn
- `E`: Draw Card (debug)
- `R`: Restart Game

## Common Card Recipes

### Damage Over Time Card
```
Card Type: Debuff
Effects:
  - Effect Type: Poison
  - Effect Value: 3
  - Effect Target: Enemy
```

### Draw Card Effect
```
Card Type: Spell
Effects:
  - Effect Type: DrawCard
  - Effect Value: 2
  - Effect Target: Self
```

### AOE Damage
```
Card Type: Spell
Effects:
  - Effect Type: Damage
  - Effect Value: 4
  - Effect Target: AllEnemies
```

### Multi-Effect Card
```
Card Type: Spell
Attack Value: 3
Effects:
  - Effect Type: Shield
    Effect Value: 3
    Effect Target: Self
  - Effect Type: DrawCard
    Effect Value: 1
    Effect Target: Self
```

## Tips

- Start with simple cards and test thoroughly
- Balance mana costs carefully
- Give feedback for every action (VFX, SFX, text)
- Playtest often to find fun combinations
- Use Debug.Log() to track game flow
- Save prefabs and ScriptableObjects frequently

Enjoy building your card game! 🎴

