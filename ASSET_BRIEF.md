# Impocalypse — Asset Generation Brief

For generating the art pack with an image model. Written against the actual
import settings, grid maths and camera in the project.

Companion to `ROADMAP.md`. Style target is the UI mockup (the hand-drawn
cartoon sheet with the four imps and the readable sidebars).

> **Expect rework.** Generated art needs cropping, resizing, transparency
> cleanup and in-scene verification. This brief exists to make the *measurements*
> right so the iteration is about style, not about sizes.

---

## Reference images to attach

Matching these from a description does not work. Attach the files:

| file | why |
|---|---|
| the UI mockup | the style target |
| `Assets/Art/UI/Icons/ui_icons.png` | the six existing resource/UI icons |
| `Assets/Art/UI/Buildings/building_markers.png` | existing building art |
| `Assets/Art/UI/card_frame.png` | existing card frame |
| `Assets/Art/Imps/Imp.png` | the imp being replaced |
| `Assets/Art/Tiles/grass.png`, `water.png` | the only two correctly-sized tiles |

**These two requests pull against each other, and that tension is the decision.**
Attaching the existing art tells the generator to match it — but the existing art
was drawn for the current look, not the mockup's, so matching it may be aiming at
the wrong target. Both "use the existing icons as reference" and "the existing
icons may clash with the new style" are true, and they cannot both be acted on.

Settle it before generating, not halfway through: either the new pack is
constrained to match the current icons and buildings, or those are regenerated
too (+6 icons, +6 building markers). What does not work is assuming old and new
will sit together because they are both "the project's art".

## Rules that apply to everything

- **No text, numbers or labels.** All text is TextMeshPro at runtime.
- **No external ground or contact shadows.** The game draws imp shadows at
  runtime (`Imp.SetAirborneOffset`). Internal two-tone shading for volume is
  fine and wanted; a shadow *cast onto the ground* is not.
- **No borders or frames** around individual assets.
- Flat fills, bold readable silhouette, consistent outline weight across a set.

**Transparency differs by asset type:**

| type | background |
|---|---|
| Terrain tiles | **fully opaque, edge to edge** — no transparency anywhere |
| Creatures, icons, card art | **transparent PNG**, no white fringe on the outline |

## Working method

Generate each *set* as one sheet to establish consistent design — four
creatures requested separately come back in four different outline weights.
But **do not expect a generated sheet to have clean cells or usable 1024px
crops**; the sheet settles the design, then export each asset deliberately at
the sizes below.

Validate before committing to the whole pack: **one Ashlands tile and all four
board creatures first**, tested in the real scene. That proves the one
relationship that matters most — characters legible against terrain — before
twelve more assets depend on it.

---

## 1. Terrain tiles — 5 needed

Tiles are placed one world unit apart (`GridGenerator`: `new Vector3(x, y, 0)`),
so a tile must be exactly **1.00 × 1.00 world units, full bleed, fully opaque**.

> Currently wrong in the project and worth not repeating: `wood` and `carrot`
> render at 1.18 units (overlapping neighbours by 18%), `mountain` at 0.81
> (leaving a gap). Only `grass` and `water` are correct.

| terrain | in-game name | tiles on board | produces |
|---|---|---|---|
| Plains | Ashlands | 40 | — |
| Water | The Styx | 16 | Souls |
| Wood | Gloomwood Grove | 16 | Charwood |
| Carrot | Bloodroot Field | 16 | Bloodroot |
| Mountain | Brimstone Peaks | 12 | — |

- **Canvas: 1024 × 1024 px square.** (512 is ample; it still supersamples ~8× at
  the sizes below.)
- **Edges must be treated to meet themselves.** Terrain appears in blocks of the
  same type constantly, so the four edges need consistent, neutral treatment —
  no motif running off one edge that won't meet its neighbour. Test each terrain
  as a repeated **3 × 3 block of itself**, not only in a mixed board.
- **Ashlands is 40% of the board** and the ground everything is drawn on. Keep it
  quiet and low-contrast; anything busy fights every creature and building.
- Terrains must stay distinguishable **by value and texture, not hue alone** —
  see Ownership tint below.

## 2. Player creatures — 4 needed

Four players, distinguishable **by silhouette, not only colour** — the playtest
finding was that players could not tell whose imp was whose.

| slot | concept | colour |
|---|---|---|
| 1 | red troublemaker | warm red |
| 2 | teal sleepyhead | teal |
| 3 | yellow maniac | yellow |
| 4 | purple wannabe royalty | purple |

**Exact anchoring** (all four must share this, or they will sit at different
heights and their shadows will detach):

- Canvas **1024 × 1024 px**, transparent.
- The creature's **visible artwork occupies the middle 80% horizontally** and
  spans from **8% above the bottom edge to no higher than 4% below the top**.
- **Feet sit on a shared baseline at 8% up from the bottom edge.** The runtime
  shadow is drawn at `-0.38` world units from the sprite centre; with the import
  below, that line lands on that baseline.
- Import pivot: **centre (0.5, 0.5)**. I set this.

**Rendered size — this is the constraint that matters.** The board camera sits
at orthographic size 8.82, so 17.65 world units fill the viewport height. A
creature is 0.8 units tall and is scaled to 0.9 at spawn:

| viewport height | 1 tile | **creature** |
|---|---|---|
| 1080 (desktop) | 61 px | **47 px** |
| 768 (tablet landscape) | 44 px | **34 px** |
| ~390 (phone landscape) | 22 px | **17 px** |

Fully zoomed out on a phone a creature is **about 17 pixels tall**. Design the
silhouette for that and let the detail be a bonus when zoomed in. Anything below
roughly 5% of the canvas will not survive.

Also needed: **a matching portrait per creature** for the sidebar (bust,
front-facing, more detail, same 1024 canvas, transparent).

## 3. Animation scope

**Static sprites only, for this pack.** The game applies movement procedurally:
a drop-in with overshoot, settle and tilt (`ImpSpawner.DropIn`), and a shadow
that spreads and fades with height.

This supports bobbing, bouncing, squash-and-stretch and tilting. It does **not**
support blinking, changing expressions or independently moving limbs — those
need frames, and none are requested here.

If you want a blink later, the cheapest addition is **one extra eyes-closed
variant per creature**, same anchoring, swapped on a timer. Decide before
generating; retrofitting a blink frame to a finished creature rarely matches.

## 4. Luxury resource icons — 3 needed

The only icons missing. `ui_icons.png` already has `charwood`, `bloodroot`,
`souls`, `territory`, `citadel`, `infamy`.

| icon | source |
|---|---|
| Demonsilk | Special Farm on Gloomwood Grove |
| Soulwine | Special Farm on Bloodroot Field |
| Ectoplasm | Special Farm on The Styx |

- **Canvas: 512 × 512 px**, transparent, object centred.
- Must read at **24–32 px**. Single object, one silhouette, no scene.
- See the compatibility warning above: if the new style diverges from the
  existing six, generate all nine rather than mixing.

## 5. Card art — 2 slots, optional

`CardArtSet.pact` and `CardArtSet.provisions` are empty (`fileID: 0`), which is
why those cards show bare parchment. Citadel, farm, bazaar, warcamp and hellgate
already have art.

- **Canvas: 512 × 512 px**, transparent. Renders ~58 units square on the card.
- Pact: a sealed scroll / contract. Provisions: a supply crate or sack.

---

## Ownership tint — read before choosing colours

Four players must be told apart on the board. The decision, so colours can be
chosen against it:

- **Creatures are never tinted.** `Imp` never writes `spriteRenderer.color`, and
  each player gets their own sprite via `ImpVisuals.GetSprite(colorId)`. Artwork
  colours are preserved exactly as drawn.
- **Tiles carry ownership**, via a saturated coloured **edge** plus a light
  colour **wash** over the tile (target ~15%, tuned in engine). The edge is what
  survives at 22 px; the wash is what makes regions readable at a glance.

Consequences for the art:

1. Terrain must remain identifiable **under a light wash of any player colour**,
   so terrains need to differ in **value and texture**, not only hue. Two
   terrains that differ only by hue will collapse into each other when tinted.
2. Player colours must stay distinct from each other **and** from the terrain
   palette, so an owned Styx tile does not read as an owned Bloodroot Field.

### What this section cannot show yet

A populated screenshot at minimum zoom — ownership tint applied, a creature
sharing a tile with a building, correct layering — would be a better acceptance
artifact than any table here, and it was reasonably asked for.

It does not exist, because **tile tinting is not implemented yet** (playtest
finding #6). Capturing the board as it stands would show untinted tiles and
prove nothing about the case that matters. Rather than ship a misleading
reference, the measured sizes above answer the underlying question: how large is
a creature when the board is fully zoomed out, which is 17 px on a phone.

A real capture replaces this once tinting lands, and the tint percentage here
(~15%) is a starting value to be tuned against that capture — not a fixed number.

## Filenames and destinations

| asset | file | destination |
|---|---|---|
| Terrain ×5 | `ashlands.png`, `styx.png`, `grove.png`, `field.png`, `peaks.png` | `Assets/Art/Tiles/` |
| Creatures ×4 | `imp_red.png`, `imp_teal.png`, `imp_yellow.png`, `imp_purple.png` | `Assets/Art/Imps/` |
| Portraits ×4 | `portrait_red.png`, … | `Assets/Art/Imps/` |
| Luxury icons ×3 | `icon_demonsilk.png`, `icon_soulwine.png`, `icon_ectoplasm.png` | `Assets/Art/UI/Icons/` |
| Card art ×2 | `card_pact.png`, `card_provisions.png` | `Assets/Art/UI/` |

Note the existing `colorId` values are `red`, `green`, `blue`, `purple`, while
the concept set is red / teal / yellow / purple. I will remap the ids; the
filenames above are the ones to deliver.

## Palette

Eyeballed from the mockup — the attached reference image is the real source.

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

## What happens on import

Handled on this side; listed so the deliverable is unambiguous.

- **World sprites** (terrain, creatures): Bilinear, mipmaps **on** — point
  sampling a large downscale is what made tiles look harsh (`f4691df`). PPU set
  so terrain is exactly 1.00 world units.
- **UI sprites** (icons, card art, portraits): mipmaps **off** — UI is drawn at
  roughly fixed size and mipmaps only soften it. Packed into a UI atlas with
  padding to avoid bleed.
- Pivots, slicing, and wiring into `CardArtSet`, `ImpVisuals` / the
  `GetImpSprite(colorId)` lookup, and the tile sprite table.

## Acceptance checks

1. Four creatures **desaturated** at 17 px — can you still tell which is which?
   If not, the silhouettes have failed their one job.
2. Each terrain as a **3 × 3 block of itself** — do the edges meet without a
   visible grid?
3. Five terrains mixed on a board — does Ashlands recede, or fight?
4. Each terrain **under a 15% wash of each player colour** — still identifiable?
5. Each icon at 24 px — readable?
6. Every transparent PNG genuinely transparent, no white fringe.
