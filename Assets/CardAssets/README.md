# Card Game Assets - Asset Package for HashSlingingSlasher

This directory contains all the visual assets needed for the card game UI and components.

## Directory Structure

### 📦 CardFrames/
Card frame templates and borders that form the base structure of each card.

### 🎨 CardBackgrounds/
Background textures, patterns, and gradients for card designs.

### 🛡️ FactionIndicators/
Icons and symbols representing different factions in the game.

### ⚡ ElementalModifiers/
Elemental type indicators (Fire, Water, Earth, Air, etc.) for cards and effects.

### ➡️ DirectionalArrows/
Arrow graphics for indicating direction of attacks, movements, or effects.

### 🖼️ UI/
General UI components including buttons, panels, badges, and other interface elements.

### 🔤 Fonts/
Typography assets for card text, UI labels, and other text elements.

## Asset Integration Guidelines

1. **Import Process**
   - Place assets in the appropriate subfolder
   - Ensure proper naming conventions (descriptive, lowercase, underscores for spaces)
   - Set appropriate import settings in Unity (sprite/texture type, compression, etc.)

2. **Naming Conventions**
   - Use descriptive names: `card_frame_common.png`, `faction_icon_fire.png`
   - Include state suffixes where applicable: `_normal`, `_hover`, `_pressed`, `_disabled`
   - Use consistent prefixes for related assets

3. **Resolution Standards**
   - Card Assets: 512x768 or higher (2:3 aspect ratio)
   - Icons: 128x128 or 256x256
   - UI Elements: Varies by component
   - Use powers of 2 where possible for optimal performance

4. **File Formats**
   - Primary: PNG with transparency
   - Source files: PSD/AI (keep originals for editing)
   - Fonts: TTF or OTF

## Quick Start

Each subdirectory contains its own README.md with specific requirements, guidelines, and asset specifications. Please review the README in each folder before adding assets.

## Notes for HashSlingingSlasher

This asset structure is ready for you to populate with your card game visual assets. Each folder has detailed specifications to ensure consistency and proper integration with the Unity project.

Happy designing! 🎮✨

