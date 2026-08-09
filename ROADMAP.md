# Impocalypse Roadmap

> **Lost or unfocused? Find the first unchecked box. That's the work.**
> Order inside a milestone is flexible; milestones are roughly sequential.

---

## North Star

**1.0 = a 2–4 player online multiplayer game with the complete ruleset, themed
art, sound, and a stable lobby — released on Steam.**

Explicitly *not* in 1.0: bots/single-player, mobile, extra game modes, ranked
anything. Those live in the Parking Lot until after launch.

---

## ✅ Done (the foundation)

- [x] Complete ruleset: draft, construction, all card types (territories,
      citadels, farms, luxury farms, bazaars, warcamps, hellgates, provisions,
      pacts), fief scoring (Str × Wealth), 4-round game with winner
- [x] Online multiplayer: full state sync, shared map seed, verified 2-player
      playtest (identical boards, identical winner)
- [x] The great renaming: GameLexicon, Souls/Infamy/Styx/Charwood/Bloodroot,
      Citadel/Hellgate/Warcamp/Bazaar, Domains, Pacts
- [x] Art pass — tiles: vibrant cartoon-hell tileset, clustering + companion
      rule terrain generation, coal frame (prefab-ized), highlight overlay
- [x] Art pass — cards: hellish frame, anchored layout, Luckiest Guy font
- [x] Art pass — imps & buildings: markers for every structure, hellgate
      portals on both endpoints, imps step aside for buildings
- [x] Phase timers: draft 45 s/hand (random auto-pick), build 90 s,
      master-authoritative, top-bar countdown
- [x] Art pass — UI theme: 9-sliced panels/buttons, resource + stat icons,
      display font, dark background, larger board

---

## Milestone A — Finish the Art Pass

- [x] **A1. UI theme slice**
  - Panel / button / icon art, 9-sliced; `UITheme` central lookup with graceful
    fallback; generator commands apply it automatically
  - Display font on headings (≥16px), clean font for dense text
  - Dark background, camera framing widened, board enlarged
  - *Residual (judge during a real multiplayer session, not solo):* sidebar
    width + frame thickness, whether the stone frames earn their space

- [ ] **A2. GameScene UX iteration** ← **NEXT**
  *Layout and clarity, not decoration. Ordered by value-per-effort.*
  - [x] **Hand: fan → flat row.** Evenly spaced, auto-shrinks to fit, bottom
        aligned, strict draw order. Retired the burying/clipping bug class.
  - [x] **Card thumbnails** — territory cards draw their own tile's sprite;
        others use a shared `CardArtSet` (hand + construction buttons).
        Labels are captions only now; rules moved to the inspector.
  - [ ] **Domains list** ← **NEXT** (see below)
  - [x] **Pick counter** in the top bar, beside the phase and countdown.
  - [x] **Explicit Confirm Picks** — plus one-click pick/release, dimmed
        unavailable cards, and a shake on refused clicks.
  - [ ] **Phase legend** — OPTIONAL. Hovering a card explains more than a static
        legend would; decide after a playtest whether anyone's actually lost.
  - [ ] *Polish tail (only if a playtest asks for it):* richer tile inspector
        (potential value, connected citadels); construction phase could reuse
        the same dim/shake affordances as the draft.
  - [x] **Inspector shows cards too** — hovering a card fills the right panel
        with its name, art and full rules. Cards in hand are too small to carry
        an explanation, so this is where "what does a Hellgate do?" is answered;
        it also gives the mostly-empty right sidebar a job during the draft.
  - [ ] **Richer tile inspector** — potential value, connected citadels.
  - [x] **Domains list** — live in the left sidebar, showing the real maths
        ("9 tiles · Str 0 × Wealth 3 = 0"), so a domain scoring nothing for
        want of a citadel is obvious while you play.

- [ ] **A3. Board depth pass** ← **NEXT**
  *The board reads flatter than the UI around it. The cause is objects floating
  on paint, not the tile art — so the fix is depth and layering, never more
  texture detail (the board already carries coordinates, terrain, resources,
  ownership, highlights and buildings; more detail makes it harder to read).*

  - [ ] **A3.1 Object shadows + owner rings** — a soft ellipse under every imp
        and building marker, plus a player-coloured disc under imps. Biggest
        single gain, two shared sprites, no tileset changes. The ring also
        answers "whose imp is that?" at a glance.
  - [ ] **A3.2 Bevel + seam overlay** — ONE reusable overlay sprite over every
        cell (light top edge, dark bottom, thin dark border), fitted by the
        existing `FitToCell` pipeline. Turns painted squares into slabs and
        deepens the grid without regenerating any terrain. Keep it subtle —
        too strong and the board reads as bathroom tiling.
  - [ ] **A3.3 Terrain variants** — 2 variants per terrain, picked on the
        seeded stream. Machinery already exists (Ashlands variant B + seeded
        rotation/mirroring); this just extends it and kills visible repetition.
  - [ ] **A3.4 Imps restyled** — current sprites are legacy assets in a
        different art language entirely; they're the most-repeated object on
        screen and the worst mismatch. Chunky cartoon devils matching the tile
        register, ¾ view, thick outlines, one per player colour.
  - [ ] **A3.5 Buildings restyled** — same silhouettes (they read well at board
        scale, already verified), but brighter and simpler to match the tiles.
        Currently painted like miniatures next to cartoon ground.

  > **Rejected deliberately: irregular / chipped per-tile slab masks.**
  > Tiles are full-bleed *by design* so adjacent same terrain fuses — that's why
  > the Styx reads as a river and groves clump, and why clustering and the
  > companion rule exist. Per-tile silhouettes would fragment those regions back
  > into 100 separate stones. This game is about connectivity; the board must
  > read as continuous regions. Favour blending over mosaic.

- [ ] **A4. Juice slice** *(hold until after a playtest)*
  - SFX + music
  - Animations: imp hop-to-corner, marker pop-in, card deal, harvest feedback
  - Hellgate adaptive placement (centered when alone on a tile)

> **Mockup discipline:** reference mockups are cheap, implementations are not.
> Take the *decisions* (flat row, thumbnails, pick counter, legend); don't
> chase pixel-perfect panel ornamentation.

## Milestone B — Playtest-Ready Plumbing

- [ ] **B1. Disconnect handling** — a player dropping mid-game must not
      strand the others (minimum: detect + end gracefully with a message)
- [ ] **B2. Minimal lobby** — main menu scene; Create Game (4-letter room
      code) / Join Game (enter code); player names; fresh map seed per game.
      Replaces the hardcoded LocalTestRoom. *Biggest remaining engineering.*
- [x] ~~**B3a. Remote smoke test**~~ — build sent to a friend, ran
      out-of-the-box on his machine; 1 full round played over the internet
      with territories, scores and board state in sync on both ends.
      Found + fixed: lobby list didn't refresh for players already waiting.
- [ ] **B3b. Full remote regression** — complete 4-round game with a remote
      player, same winner + score on both screens
- [ ] **B4. WebGL build → itch.io unlisted page** → **🎉 FRIEND PLAYTESTS**

## Milestone C — Hardening & Balance (playtest-driven)

- [ ] Fix what friends find (this list writes itself)
- [ ] Camp priority resolution (lowest number wins simultaneous claims —
      currently first-come at master)
- [ ] Balance tuning: Cluster Strength, timer durations, terrain quotas
- [ ] 3–4 player games verified
- [ ] Photon capacity check (free tier = 20 CCU; fine through here)

## Milestone D — Steam

- [ ] Steamworks account ($100 one-time)
- [ ] Store page: capsule art, screenshots, trailer/GIFs (art pass feeds this)
- [ ] Wishlist window (a few weeks minimum before launch)
- [ ] Optional: free demo or Steam Playtest for a wider beta
- [ ] **Launch 1.0** 🔥

---

## Parking Lot (post-1.0, or "when it hurts")

- Bots / single-player mode
- Mobile port (touch pass + UI density; architecture already ports)
- True auto-tiling art (merged lakes/ranges with edge-matched variants)
- Lair sprite usage (burrow on owned tiles / capital mechanic / menu art)
- Sky-tower strict-BK rule (gate occupies building slot) — house rule stands
- Cosmetic enum renames (TileType.Water→Styx etc.) — display names already
  handled by GameLexicon; internal ids stay until a quiet moment
