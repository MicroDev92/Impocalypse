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

**Settled: the mockup is the style authority, not the existing art.**

These two requests looked mutually exclusive — "use the existing icons as
reference" versus "the existing icons may clash with the new style" — but they
separate cleanly once you split *subject* from *style*:

- Existing art says **what an object is**: which shape means Charwood, what a
  Warcamp looks like, what the card frame has to accommodate.
- The mockup says **how it should look**: outline weight, palette, shading.

So: **everything visible is regenerated to the mockup.** No existing art ships
in the final look. The existing files travel with this brief as subject
reference only — what a Warcamp is, which shape means Charwood — and are
replaced, not matched.

The one exception is the **font**: `LuckiestGuy` is already the mockup's
lettering and stays.

See *Full inventory* below for what that actually amounts to.

## Full inventory

Replacing everything rather than patching means **37 assets**, not the 18 the
first draft implied. Worth seeing before committing:

| group | count | detail |
|---|---|---|
| Terrain | 5 | Ashlands, Styx, Grove, Field, Peaks |
| Creatures | 4 | board sprites |
| Portraits | 4 | sidebar busts |
| Resource / UI icons | 9 | 6 replacing `ui_icons.png` + 3 new luxuries |
| Building markers | 6 | citadel, farm, bazaar, warcamp, hellgate, lair |
| Card art | 2 | pact, provisions (currently empty slots) |
| Card frame | 1 | 9-sliced |
| Panels & chrome | 4 | panel, button, coordinate slab, corner skull |
| Background | 2 | tileable texture, glow |
| **Total** | **37** | |

**Sequence it.** Do not generate 37 assets before any of them has been seen in
the scene. First batch is **one Ashlands tile and the four creatures** — that
proves the one relationship everything else depends on, characters legible
against terrain at 17 px. The rest follows once that is agreed.

### The 20 coordinate labels are not on that list, deliberately

The A–J and 1–10 labels around the board are currently **20 separate PNGs**
(`TileData.InitWithLabel` assigns `labelRenderer.sprite`). Regenerating them as
art would mean 20 more assets that must stay consistent with each other and
with the font — the most tedious possible use of an image model, and they would
still be fixed-resolution.

They should become **TextMeshPro text in LuckiestGuy** instead: one font already
in the project and already matching the mockup, crisp at any zoom, 20 fewer
assets. That is a code change on this side, not a generation task.

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
heights and their shadows will detach). Every number below is in the same frame
of reference — an earlier revision mixed canvas-relative and world-space figures
and did not compose:

- Canvas **1024 × 1024 px**, transparent.
- **The full canvas maps to exactly 1.00 × 1.00 world units** (imported at
  PPU 1024, whole canvas as the sprite rect, no trimming). So canvas percentages
  and world units are the same scale: 1% of canvas = 0.01 units.
- Import pivot **centre (0.5, 0.5)**, so the canvas spans −0.50 … +0.50 units.
- **Feet sit on a shared baseline 8% up from the bottom edge** = −0.42 units
  before the spawn scale.
- **Visible artwork spans 8% → 96% vertically** (0.88 of the canvas) and the
  **middle 80% horizontally**.

That makes the runtime shadow line up without tuning:

```
feet            -0.42 × 0.9 spawn scale = -0.378 world units
Imp.shadowOffsetY                       = -0.380 world units
mismatch                                   0.002 units  (0.04 px on a phone)
```

**Rendered size — the constraint that matters.** Board camera is at orthographic
size 8.82, so 17.65 world units fill the viewport height. Creature art is
0.88 canvas × 0.9 spawn scale = **0.792 world units**:

| viewport height | 1 tile | **creature** |
|---|---|---|
| 1080 (desktop) | 61.2 px | **48.5 px** |
| 768 (tablet landscape) | 43.5 px | **34.5 px** |
| ~390 (phone landscape) | 22.1 px | **17.5 px** |

Fully zoomed out on a phone a creature is **about 17 px tall**. Design the
silhouette for that and let detail be a bonus when zoomed in; anything below
roughly 5% of the canvas will not survive.

> The *existing* `Imp.png` does not follow this: it is a 703 × 746 sprite rect at
> PPU 864 (0.81 × 0.86 units), trimmed rather than full-canvas. The spec above
> supersedes it — do not measure the old imp to infer the new sizing.

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
