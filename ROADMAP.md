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

---

## Milestone A — Finish the Art Pass

- [ ] **A1. UI theme slice** ← **NEXT SESSION**
  - Panels (sidebars, top bar, pacts panel, game-over screen) themed
  - Buttons (Done, construction list, Play Again / Main Menu)
  - Real icons for the placeholder squares (resources, stats)
  - The global-font verdict: does Luckiest Guy take over all UI?
- [ ] **A2. Juice slice** *(can slide later — even after B4)*
  - SFX + music
  - Animations: imp hop-to-corner, marker pop-in, card deal, harvest feedback
  - Hellgate adaptive placement (centered when alone on a tile)

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
