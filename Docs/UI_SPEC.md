# Impocalypse — Current UI Spec

Measured from the running game, not from the editor inspector or memory. Companion
to `ASSET_BRIEF.md`; this is the "what exists today" half, for comparing a mockup
against reality.

Screenshots alongside this file:
- `current_ui_populated.png` — populated board, ownership tint live, 1920×1080
- `ownership_tint_fullboard.png` — four owners at full-board zoom

---

## Reference viewport

**1920 × 1080 is the design target.** Everything below is in those units.

| | |
|---|---|
| Canvas | `UICanvas_v2`, Screen Space Overlay |
| Scale mode | `ScaleWithScreenSize` |
| Reference resolution | **1920 × 1080** |
| Match width/height | **0.5** (blends both axes) |
| Reference pixels per unit | 100 |

Match 0.5 means the UI scales by a blend of width and height, so a narrower
viewport shrinks everything rather than reflowing it. That is why the phone
result is "the desktop layout, smaller" rather than a different arrangement.

## Board camera

| | |
|---|---|
| Projection | Orthographic, size **8.8235** |
| Viewport rect | full screen (0,0,1,1) |
| Visible world height | **17.65 units** |
| Grid cell | **1.00 world unit** (`GridGenerator`: `new Vector3(x, y, 0)`) |

Pixels per world unit = `viewportHeight / 17.65`:

| viewport height | 1 tile | creature (0.79u) |
|---|---|---|
| 1080 | 61.2 px | 48.5 px |
| 768 | 43.5 px | 34.5 px |
| ~390 (phone landscape) | 22.1 px | 17.5 px |

## Layout

Sizes and anchors as measured at runtime.

| element | size | anchored pos | anchors |
|---|---|---|---|
| TopBar | 1920 × 50 | (0, 0) | top, full width |
| → LeftGroup | 600 × 50 | (7, 0) | x 0 → 0.32 |
| → StatusText | 768 × 50 | (0, 0) | x 0.32 → 0.72 |
| → ResourceGroup | 524 × 50 | (−7, 0) | x 0.72 → 1 |
| LeftSidebar | **380 × 880** | (0, 50) | left, full height |
| → PlayerListContainer | 340 × 90 | (190, −65) | top-left |
| → PactsPanel | 340 × 150 | (190, −274) | top-left |
| → DomainsPanel | 340 × 572 | (190, −860) | top-left |
| RightSidebar | **380 × 880** | (0, 50) | right, full height |
| HandContainer | **1140 × 0** | (0, 130) | bottom-centre |
| → Card | **130 × 190** | fanned | centre |
| → → Content | **71 × 95** | (0, 0) | centre |
| PickPrompt | 240 × 56 | (−40, 80) | bottom-right |
| → ConfirmButton | 220 × 64 | (0, 0) | centre |
| RecenterButton | 200 × 64 | (40, 50) | bottom-left |
| PhaseAnnouncementBanner | 1920 × 150 | (0, 0) | centred vertically |

Board drawing area between the sidebars: **1920 − 380 − 380 = 1160**.

### Card hand

| | |
|---|---|
| Card | 130 × 190 |
| Content (parchment safe area) | **71 × 95**, measured from `card_frame.png` |
| `maxFanWidth` | 1140 |
| `cardGap` | 14 |
| `minCardScale` | 0.85 |
| Max hand | 12 cards (2–3 players); 10 at 4 players |

At 12 cards the fan clamps at `minCardScale` and cards **overlap**: step is
`(1140 − 110.5) / 11 = 93.6 px`, so each card shows ~110 of its 130 units and
the rest sits under its neighbour. Card content is sized to stay inside that
visible strip.

## Fonts — read this before matching typography

**The project uses two fonts, and one of them is unintentional.**

| font | used by |
|---|---|
| `LuckiestGuy SDF` | RoundText, PhaseText, card Labels, resource amounts, PlayerNameText |
| `LiberationSans SDF` *(Unity's default)* | PactsHeader, PactsList, DomainsHeader, DomainsEmpty, DraftInfoText, OnBoardLabel, ScoreText, EmptyStateText, AnnouncementText, button Labels, Count |

Roughly half the UI — including **both sidebar panels and the whole right
sidebar** — is still on Unity's fallback font. That is almost certainly not a
design decision, and it is a plausible part of playtest finding #5, "right
sidebar hard to read".

> An earlier version of `ASSET_BRIEF.md` claimed LuckiestGuy "is already the
> mockup's lettering". That was unverified: the mockup is a generated image, so
> its lettering is whatever the model drew. What is true is that LuckiestGuy is
> the font *the game* uses for its headings. Whether it matches the mockup is an
> open question — and whichever way it goes, the LiberationSans half needs
> deciding too.

Sizes in use: 12, 13, 14, 15, 16, 17, 18, 60 (announcement). Card labels
auto-size 13–28.

## Colours in use

| role | hex | where |
|---|---|---|
| Accent orange | `#F28C26` | PhaseText, headers, resource amounts |
| Muted text | `#B28C73` | secondary labels, empty states |
| Light text | `#FFEBD1` / `#FFF2E6` | player name, button labels |
| Card label | `#323232` | dark text on parchment |
| Announcement | `#FFCC33` | phase banner |

## Ownership visuals (current)

- **Tile wash** — owner colour at **15% alpha**, child renderer, sorting order 3.
- **Domain outline** — `FiefVisualizer` draws each connected group with
  LineRenderers in the owner's colour, width **0.10 world units** (6.1 px at
  1080, 2.2 px on a phone), sorting order 40, refreshed on
  `TileOwnerChangedEvent`.
- **Creature** — never tinted; each player has their own sprite.

All three resolve from the same `PlayerColorPalette` entry via
`IPlayerService.GetPlayerColor`, so they cannot disagree.

> In `current_ui_populated.png` only one owner shows a colour; the other two
> render white. That is a solo-test artifact — those actor numbers are not
> registered players, so they hit the deliberate "unknown identity" fallback.

## Known divergences from the mockup

Worth settling explicitly rather than discovering during implementation:

1. **Two fonts**, as above.
2. **Mockup aspect vs phone.** The reference is 1920×1080 (16:9). A phone in
   landscape is nearer 2.2:1, and with `match 0.5` the UI shrinks rather than
   reflows — so the desktop composition and a readable phone layout cannot be
   the same arrangement. Same artwork, colours, typography and panel styling can
   carry across; the panel and card arrangement cannot.
3. **Right sidebar is currently empty-state only** — it shows "Click a tile or
   hover a card to inspect" until something is selected. The mockup shows a
   populated card preview, which is a content change as well as a restyle.
4. **Coordinate labels are 20 sprites**, not text. See `ASSET_BRIEF.md`.
