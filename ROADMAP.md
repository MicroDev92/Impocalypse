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

## ✅ Milestone A — Art Pass Foundations (complete)

*The board's remaining visual work moved to Milestone D, deliberately — see
there for why.*

- [x] **A1. UI theme slice**
  - Panel / button / icon art, 9-sliced; `UITheme` central lookup with graceful
    fallback; generator commands apply it automatically
  - Display font on headings (≥16px), clean font for dense text
  - Dark background, camera framing widened, board enlarged
  - *Residual (judge during a real multiplayer session, not solo):* sidebar
    width + frame thickness, whether the stone frames earn their space

- [x] **A2. GameScene UX iteration**
  *Layout and clarity, not decoration. Ordered by value-per-effort.*
  - [x] **Hand: fan → flat row.** Evenly spaced, auto-shrinks to fit, bottom
        aligned, strict draw order. Retired the burying/clipping bug class.
  - [x] **Card thumbnails** — territory cards draw their own tile's sprite;
        others use a shared `CardArtSet` (hand + construction buttons).
        Labels are captions only now; rules moved to the inspector.
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

> **Mockup discipline:** reference mockups are cheap, implementations are not.
> Take the *decisions* (flat row, thumbnails, pick counter, legend); don't
> chase pixel-perfect panel ornamentation.

## Milestone B — Playtest-Ready Plumbing ← **YOU ARE HERE**

> Nothing visual happens until this milestone is done. The board's remaining
> art (Milestone D) is real work with real value — it is deliberately parked,
> not forgotten.

- [x] **B1. Disconnect handling** — `4568eea`, verified both directions with two
      instances. `DisconnectWatcher` ends the game naming who dropped; the
      results panel shows the reason instead of a winner and hides Play Again.
      Found on the way: the bug was worse than a missing message — phase
      timers are master-authoritative, so a **master** dropping mid-phase left
      nobody to end the phase and the game hung permanently. Sends no network
      message by design: Photon tells every remaining client directly, which is
      what makes the master-drop case work at all.
- [ ] **B2. Minimal lobby** ← **NEXT** — main menu scene; Create Game (4-letter
      room code) / Join Game (enter code); player names; fresh map seed per game.
      Replaces the hardcoded LocalTestRoom. *Biggest remaining engineering.*
- [x] ~~**B3a. Remote smoke test**~~ — build sent to a friend, ran
      out-of-the-box on his machine; 1 full round played over the internet
      with territories, scores and board state in sync on both ends.
      Found + fixed: lobby list didn't refresh for players already waiting.
- [ ] **B3b. Full remote regression** — complete 4-round game with a remote
      player, same winner + score on both screens. **Start arranging this
      first**: it's blocked on a friend's calendar, not on code, so it should
      be in flight while B1 is being written. Only round 1 has ever run over
      the network — scoring, pacts and winner determination are the least
      tested code in the project and the most expensive to find broken late.
- [ ] **B4. WebGL build → itch.io unlisted page** → **🎉 FRIEND PLAYTESTS**

## Milestone C — Hardening & Balance (playtest-driven)

- [ ] Fix what friends find (this list writes itself)
- [ ] Camp priority resolution (lowest number wins simultaneous claims —
      currently first-come at master)
- [ ] Balance tuning: Cluster Strength, timer durations, terrain quotas
- [ ] 3–4 player games verified
- [ ] Photon capacity check (free tier = 20 CCU; fine through here)

## Milestone D — Board Depth & Life

*Was Milestone A3/A4. Moved here on purpose. Two reasons, and the second is
the better one: art can't fix a desync in round 3, and playtesters will tell
you what the board actually needs. If three people say they couldn't tell
whose imp was whose, owner rings stop being decoration and become a fix —
art aimed at observed confusion beats art aimed at taste.*

*The board reads flatter than the UI around it. The cause is objects floating
on paint, not the tile art — so the fix is depth and layering, never more
texture detail (the board already carries coordinates, terrain, resources,
ownership, highlights and buildings; more detail makes it harder to read).*

**Cheap tier — no tileset changes, little or no new art:**

- [ ] **D1. Object shadows + owner rings** — a soft ellipse under every imp
      and building marker, plus a player-coloured ring under imps. Biggest
      single gain. Sizes derive from each object's own sprite bounds (same
      art-proofing rule as `TileData.FitToCell`), and both sprites can be
      generated in code, so this can start with no art at all.
- [ ] **D2. Scatter props + board lighting** — bones, skulls, rocks, dead
      trees strewn on tiles (seeded, non-interactive, they get shadows too),
      plus a vignette and a single global grade. A large part of why reference
      mockups look "alive" — mockup boards are never bare, and one consistent
      light unifies mismatched elements better than any single sprite.

> **Judge here.** Screenshot the board. Whether the expensive tier is worth
> it should be decided against a real screenshot, not a prediction.

**Expensive tier — real art work, decide with playtest evidence:**

- [ ] **D3. Terrain variants** — 2 variants per terrain on the seeded stream.
      Machinery already exists (Ashlands variant B + seeded rotation/mirroring);
      this extends it and kills visible repetition.
- [ ] **D4. Imps restyled** — current sprites are legacy assets in a different
      art language entirely; the most-repeated object on screen and the worst
      mismatch. Chunky cartoon devils matching the tile register, ¾ view, thick
      outlines, one per player colour.
- [ ] **D5. Buildings restyled** — same silhouettes (they read well at board
      scale, already verified), but brighter and simpler to match the tiles.
      Currently painted like miniatures next to cartoon ground.
- [ ] **D6. Juice** — SFX + music; animations (imp hop-to-corner, marker
      pop-in, card deal, harvest feedback); hellgate adaptive placement
      (centred when alone on a tile).

> **The ground stays flat, top-down, and full-bleed. Settled — don't reopen.**
>
> Explored and rejected, in order: a lip on every tile (bands the board into a
> staircase); lips only on region edges (still looked wrong); true isometric.
> Terrain must FUSE — the Styx is meant to read as one uniform lake or river,
> and anything that gives tiles their own silhouette fights that.
>
> Isometric is out on gameplay, not cost: cards *are* coordinates, a player
> reads "Ashlands · C7" and must find C7 in about a second, and a square grid
> with row/column labels beats a diamond lattice at that decisively.
>
> Depth comes from what sits ON the board — shadows, props, lighting, and
> style-matched objects — never from reshaping the ground.

## Milestone E — Steam

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
