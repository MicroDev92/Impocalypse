# Project Overview 
- Game Title: Impocalypse
- High-Level Concept: A modern digital board game based on Bunny Kingdom where players draft cards to claim tiles, build cities, and manage resources (Wood, Carrot, Fish) on a 12x12 grid.
- Players: Local/Network Multiplayer.
- Inspiration / Reference Games: Bunny Kingdom, Catan Universe, Hearthstone (for hand feel).
- Tone / Art Direction: Stylized, demonic-fantasy board game.
- Target Platform: Standalone Windows.
- Render Pipeline: Built-in.

# Game Mechanics 
## Core Gameplay Loop
Players go through Draft, Construction, and Scoring phases. In the Draft phase, they pick cards corresponding to grid coordinates. In the Construction phase, they place cities or imps on claimed tiles.
## Controls and Input Methods
- Mouse: Left click to select cards/tiles, Middle click to pan, Scroll to zoom.
- Touch: Pinch to zoom, Swipe to pan.
- Hover: Highlighting board tiles when hovering over cards in hand.

# UI
- **Top HUD:** Horizontal bar at the top showing Round (e.g., 1/4), current Phase, and player resource counts.
- **Card Fan:** A handheld arc of cards at the bottom center that expands and rotates on hover.
- **Contextual Tooltip:** A non-intrusive world-space label showing tile coordinates (A1, B5) and current owner when hovering over the board.
- **Phase Banners:** Large animated text announcements for phase transitions.

# Key Asset & Context
- `ModernCameraController`: Correctly offsets the viewport for UI panels.
- `CardFanLayout`: Component for arc-based card positioning.
- `GridGenerator`: Dynamic 12x12 tile generator.
- `SelectionManager`: New script to bridge Card hover events to Tile highlights.

# Implementation Steps
1. **Environment & Editor Juice [World]**
   - Create a "Tabletop" background plane at Z=5.
   - Add `[ContextMenu("Generate Grid Editor")]` to `GridGenerator.cs` to allow visual layout tuning without pressing Play.
   - **Dependency:** None.

2. **Responsive UI Framework [UI]**
   - Create a Main `Canvas` (Scale with Screen Size).
   - Add `TopHUD` (Anchored Top-Center) and `HandContainer` (Anchored Bottom-Center).
   - Configure `ModernCameraController` margins to match these panels.
   - **Dependency:** Step 1.

3. **Modern Card Hand [UI/Mechanics]**
   - Create a `CardHand` prefab using `CardFanLayout`.
   - Update `CardFactory` to parent new cards to the `HandContainer`.
   - Implement "Card Focus" (card scale/lift on hover).
   - **Dependency:** Step 2.

4. **Diegetic Feedback [Mechanics]**
   - Implement `SelectionManager` to subscribe to `ICardHover` events.
   - Wire `TileData.Highlight(bool)` to trigger when the corresponding card is hovered.
   - **Dependency:** Step 3.

5. **Phase & Score HUD [UI]**
   - Re-implement `DraftHUD` using the new `TopHUD` layout.
   - Create resource widgets (Carrot, Wood, Fish) with icons from `ResourceManager`.
   - **Dependency:** Step 2.

# Verification & Testing
- **Visual Check:** Verify the 12x12 grid is fully visible and not clipped by UI when zooming out.
- **Interaction Check:** Hover over "A1" card in hand and confirm the "A1" tile on the board glows.
- **Layout Check:** Resize the Game Window (16:9 to 21:9) and ensure the top bar and card fan remain anchored correctly.
