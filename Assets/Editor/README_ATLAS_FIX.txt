QUICK FIX FOR FONT ATLAS VISIBILITY:

1. If visible in SCENE VIEW:
   - Click Gizmos dropdown (top right of Scene view)
   - Uncheck "Textures" or press Shift+G to toggle Gizmos

2. If visible in GAME VIEW:
   - Enter Play Mode
   - Press Cmd+Shift+H (Mac) or Ctrl+Shift+H (Windows)
   - OR use Tools > Quick Hide Atlas

3. Manual check:
   - In Play Mode, search Hierarchy for "atlas", "tmp", "font"
   - Disable any suspicious GameObjects

4. Scene View Settings:
   - Window > Rendering > Scene View
   - Disable texture previews
