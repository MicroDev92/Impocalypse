# Impocalypse — Asset Generation Brief

For generating the art pack with an image model. Written against the actual
import settings and grid maths in the project, so what comes back drops in
without rework.

Companion to `ROADMAP.md`. Style target is the UI mockup (the hand-drawn
cartoon sheet with the four imps and the readable sidebars).

---

## How to drive the generator

1. **Attach the mockup as a reference image on every request.** It carries the
   style more reliably than any adjective. Say "match the style of the attached
   reference" rather than re-describing it.
2. **Generate the four creatures in one image, as a sheet**, then cut them
   apart. Asking for them one at a time produces four slightly different
   styles — different outline weights, different eye sizes — and they are meant
   to be told apart at a glance *as a set*.
3. **Terrain likewise**: all five in one image. Terrain is judged against its
   neighbours, not on its own.
4. Iterate on the sheet until the set is coherent, **then** cut and export. Do
   not start exporting individual files early.

## Rules that apply to everything

- **PNG, transparent background.** No checkerboard, no white box, no padding
  colour.
- **No baked shadows.** The game draws imp shadows at runtime
  (`Imp.SetAirborneOffset`); a painted one would double up.
- **No text, no numbers, no labels.** All text is TextMeshPro at runtime so it
  stays translatable and sharp.
- **No borders or frames** around individual assets.
- Flat fills, bold readable silhouette, consistent outline weight across the set.
- Square canvas unless stated otherwise.

---

## 1. Terrain tiles — 5 needed

The board is a 10×10 grid and **tiles are placed one world unit apart**
(`GridGenerator`: `new Vector3(x, y, 0)`). A terrain tile must therefore be
exactly one unit square, **full bleed, no transparent margin** — the artwork
runs edge to edge or you get seams and overlaps.

> This is currently wrong in the project and worth not repeating: `wood` and
> `carrot` render at 1.18 units (overlapping their neighbours by 18%) and
> `mountain` at 0.81 (leaving a gap). Only `grass` and `water` are correct.

| terrain | in-game name | tiles on board | notes |
|---|---|---|---|
| Plains | Ashlands | 40 | the default ground; must stay visually quiet, it is 40% of the board |
| Water | The Styx | 16 | produces Souls |
| Wood | Gloomwood Grove | 16 | produces Charwood |
| Carrot | Bloodroot Field | 16 | produces Bloodroot |
| Mountain | Brimstone Peaks | 12 | produces nothing |

- **Canvas: 1024 × 1024 px, square, full bleed.** (512 is plenty if build size
  matters — it still supersamples ~4× at normal zoom.)
- Import: PPU set equal to the pixel width, so the tile is exactly 1.00 × 1.00
  world units. I set this on import.
- **Ashlands is the background of the whole board.** Keep it low-contrast and
  low-detail. Anything busy here fights every creature and building drawn on top.
- Tiles are drawn adjacent to identical tiles constantly, so avoid a strong
  centred motif that creates an obvious repeating pattern.

## 2. Player creatures — 4 needed

Four players, and they must be distinguishable **by silhouette, not only by
colour** — players reported they could not tell whose imp was whose.

| slot | concept | colour |
|---|---|---|
| 1 | red troublemaker | warm red |
| 2 | teal sleepyhead | teal |
| 3 | yellow maniac | yellow |
| 4 | purple wannabe royalty | purple |

- **Canvas: 1024 × 1024 px**, creature centred, small transparent margin.
- Creature occupies roughly the lower ¾ of the canvas, **feet near the bottom
  edge** — the runtime shadow anchors under the feet.
- Rendered on a 1-unit tile at roughly **0.8 units**, so detail below about
  2% of the canvas will not survive. Big heads, bold horns, clear outline.
- **Give each a different head/horn silhouette.** If the four are the same
  shape in four colours, the set has failed its one job.
- Also needed: **a matching portrait per creature** for the sidebar (bust,
  front-facing, higher detail, same 1024 canvas). The mockup's "YOU" panel uses
  these. These may be cropped from the full creature if the style allows.

## 3. Luxury resource icons — 3 needed

The only icons missing. `ui_icons.png` already has `charwood`, `bloodroot`,
`souls`, `territory`, `citadel`, `infamy`.

| icon | what it is |
|---|---|
| Demonsilk | luxury from a Special Farm on Gloomwood Grove |
| Soulwine | luxury from a Special Farm on Bloodroot Field |
| Ectoplasm | luxury from a Special Farm on The Styx |

- **Canvas: 512 × 512 px**, object centred, transparent margin.
- Must read at **24–32 px** in the sidebar. Single object, one clear silhouette,
  no scene, no background.
- Match the existing six icons in weight and treatment — generate alongside a
  copy of one of them as reference.

## 4. Card art — 2 slots, optional

`CardArtSet.pact` and `CardArtSet.provisions` are empty (`fileID: 0`), which is
why those cards show bare parchment. Everything else (citadel, farm, bazaar,
warcamp, hellgate) already has art.

- **Canvas: 512 × 512 px**, transparent.
- Reads at roughly 58 × 58 units on the card, inside the measured parchment
  safe area — single clear object.
- Pact: a sealed scroll / contract. Provisions: a supply crate or sack.

---

## Palette

Eyeballed from the mockup, so treat as a starting point — the attached
reference image is the real source of truth.

| role | approx |
|---|---|
| deep background | `#140C0A` |
| parchment | `#F2DFAE` |
| accent orange | `#E8862A` |
| muted text | `#8A6652` |
| player red | `#E04A2F` |
| player teal | `#5FC9B8` |
| player yellow | `#F2B53C` |
| player purple | `#8B6CC9` |

Player colours must stay distinct **at small size and against every terrain**,
including when a tile is tinted with the owning player's colour.

---

## What happens on import

I handle all of this; it is listed so the deliverable is clear.

- Filter mode Bilinear, mipmaps on (point-sampling a 10× downscale is what made
  the tiles look harsh — see commit `f4691df`).
- PPU per the sizes above so terrain lands on exactly 1.00 world units.
- Pivots, sprite slicing, and wiring into `CardArtSet`, the imp sprite lookup
  (`GetImpSprite(colorId)`) and the tile sprite table.

## Acceptance checks

Before a set is considered done:

1. Four creatures side by side at 80 px — can you name which is which without
   colour? (Desaturate and check.)
2. Five terrains in a 10×10 patchwork — does Ashlands recede, or does it fight?
3. Each icon at 24 px — still readable?
4. Every PNG genuinely transparent, no white fringe on the outline.
