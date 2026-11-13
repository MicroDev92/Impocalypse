# ArtGen — code-drawn art for Impocalypse

The terrain tiles are drawn by code, not painted. Change a colour or a shape in
`tiles.js`, regenerate, and all 13 tiles update together and stay consistent.

## Run it

```bash
python server.py 8090
```

Then open <http://localhost:8090/index.html?save>. The page draws every tile,
shows review sheets, and saves PNGs to `out/` (ignored by git). Without `?save`
it only previews.

Copy the tiles you want into the project:

```bash
cp out/ashlands_*.png out/styx_*.png out/grove_*.png out/field_*.png out/peaks_*.png ../../Assets/Art/Tiles/
```

Unity keeps the import settings (PPU 1024, bilinear, mipmaps) in the existing
`.meta` files, and `GridGenerator` already points at these names.

## Files

| file | what it is |
|---|---|
| `lib.js` | drawing primitives: seeded random, wobbly blobs, tapered strokes, the tile plate |
| `tiles.js` | one painter per terrain, plus `TILESET` listing every variant and its seed |
| `main.js` | renders the set, builds the review sheets, uploads |
| `server.py` | serves this folder and writes uploads to `out/`; local only |

## Judge tiles at the size players see them

A 1024px tile is drawn at about 61px on desktop and 22px on a phone. The page
therefore also renders a mock 10x10 board at both sizes, with the same rotation
and mirroring the game applies. Look at `_board_61_x2.png` and
`_board_22_x4.png` before the full-size tiles — a tile that looks rich at 1024
can be a flat smudge at 61.

## Rules the painters follow

- Ashlands and Styx are rotated in 90 degree steps at runtime (Ashlands is also
  mirrored), so they carry no directional light and nothing with an "up".
- Grove and Peaks are mirrored only. Field is never transformed.
- Features are thick and few: one rendered pixel is ~17 source pixels.
- Variants are seeded. To get a different layout change the seed in `TILESET`;
  to add a variant add a line there and to the matching list on `GridGenerator`.

## UI icons

`icons.js` draws the 14 sidebar icons (resources, strength, wealth, infamy,
tile, domain, pact, treasure) on 256px transparent canvases; they are saved as
`out/icon_*.png`. Copy them to `Assets/Art/UI/Icons/`, then run
**GameObject > UI > Impocalypse - Restyle Left Sidebar** in Unity, which imports
them as sprites and refills `Resources/UIIconSet`. Judge them on
`_sheet_icons_28px_x4.png`: they are shown at about 32px.

## Panels and creatures

- `panels.js` draws the 9-sliced chrome (`panel_bg`, `panel_inset`, `button_bg`,
  `topbar_bg`) at 2x. Copy them to `Assets/Art/UI/Panels/` and run
  **GameObject > UI > Impocalypse - Apply Panel Frames**, which sets the slice
  borders and PPU and points the scene's inner panels at them.
- `creatures.js` draws the four player creatures to the anchoring in
  `ASSET_BRIEF.md`. Copy `imp_*.png` to `Assets/Art/Imps/Drawn/`.
  **Impocalypse > Creatures > Use Drawn Set / Use Generated Set** swaps which
  set the player palette hands out.
- Any `imp_<colour>.png` placed in `out/ext/` is added to `_sheet_creatures.png`
  as a second row, for side-by-side comparison at tile size.
