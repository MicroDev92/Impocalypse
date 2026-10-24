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
  - [x] **Phase legend** — **CLOSED, not needed.** Playtest 1 was the test this
        was waiting on: nobody was confused about phases. What testers actually
        lost track of was *what they had drafted* — a different problem, now C-e.
  - [x] *Polish tail* — **answered by playtest 1**, and both halves moved to
        Milestone C rather than staying conditional here: construction reusing
        the draft's affordances is C-h, richer inspector is C-e.
  - [x] **Inspector shows cards too** — hovering a card fills the right panel
        with its name, art and full rules. Cards in hand are too small to carry
        an explanation, so this is where "what does a Hellgate do?" is answered;
        it also gives the mostly-empty right sidebar a job during the draft.
  - [x] **Richer tile inspector** — moved to C-e, where the playtest evidence for
        it lives. Not open work here.
  - [x] **Domains list** — live in the left sidebar, showing the real maths
        ("9 tiles · Str 0 × Wealth 3 = 0"), so a domain scoring nothing for
        want of a citadel is obvious while you play.

> **Mockup discipline:** reference mockups are cheap, implementations are not.
> Take the *decisions* (flat row, thumbnails, pick counter, legend); don't
> chase pixel-perfect panel ornamentation.

## ✅ Milestone B — Playtest-Ready Plumbing (complete)

> **DONE 2026-10-20, bar the optional B2.4.** The game is live on an itch.io
> draft page and playable in a browser by anyone with the secret URL. It has a
> lobby, three ways in, it survives a disconnect, and it has been played start
> to finish over a network twice.
>
> Milestone C opened early on purpose, not by drift: playtest 1 surfaced two bugs
> (C-a, C-b) that let a timer take a player's decision away, and shipping a
> friend playtest with those in would have wasted the playtest. Those plus
> C-c/C-d/C-f/C-h are fixed; **C-e and C-g deliberately wait for playtest 2**.
>
> **The open question is no longer technical.** It is whether the loop is fun,
> and only a 3–4 player session answers it — every game so far has been 2-player,
> which is the count where a Bunny-Kingdom-style draft is weakest. That same
> session decides the design debate in Milestone F's notes: whether the game
> needs mechanics (corruption, souls-as-currency) or just presentation (D3/D6).
> Do not build either until it has been played.

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
- [x] **B2. Minimal lobby** — done bar the optional B2.4. Replaced the hardcoded
      LocalTestRoom.
      *Biggest remaining engineering.*

  **Shape:** `Launcher` becomes the main menu; **`GameRoom` survives** as the
  waiting room. It already does its job and its join-refresh bug was just fixed
  — the scene split matches the actual states, so don't merge them.

  - [x] **B2.1 Split connect from join** — `523c6e7`. `OnConnectedToMaster` used to
        auto-joins `LocalTestRoom`, which is the whole problem. Connect on
        launch; join only on player action. Adds `CreateRoom` / `JoinRoom` /
        `LeaveRoom` / `SetRoomOpen` to `INetworkService` (+ MockNetworkService).
        Room-code generator with collision retry via `OnCreateRoomFailed`.
        Everyone loads GameRoom on join, not just the master.
  - [x] **B2.2 Main menu UI** — `1c5978a`, layout tuned in `86e1414`/`520fb5f`.
        Verified with all five checks: connect, bad code refused, Quick Play pair,
        Create-then-Join by code, and a started game refusing a late joiner with
        "That game has already started" — which proved the closed-room guard
        properly, since a code gets an explicit refusal where Quick Play could
        only wander off into a new room.
        Built by an editor command like the other UI
        (`Editor/GameSceneUISetup.cs` pattern). Three ways in:
        - **Quick Play** — `JoinRandomRoom`, create one on failure. The best
          playtest tool available: "everyone click Quick Play at 8pm" beats
          coordinating codes. Doubles as the one-click editor path, which
          solo testing needs constantly.
        - **Create Game** — private room, code shown to read out.
        - **Join Game** — code entry.

        **Public vs private is one flag:** Quick Play rooms are
        `IsVisible = true` (findable by matchmaking); code rooms are
        `IsVisible = false`. Every room gets a code either way, so a Quick Play
        host can still invite by code. Composes with B2.3: rooms closed on Start
        are skipped by random matchmaking for free.

        **Name field lives HERE, not in Options.** It persists to the existing
        `nickname` PlayerPrefs key. Buried in a settings screen nobody touches
        it, and a playtest of four `Player_1234`s defeats the point of B2.
  - [x] **B2.3 GameRoom updates** — `512b7e6`, Leave button verified in the scene.
        Show the room code big (the host reads it
        out); Leave button back to the menu; **master sets `IsOpen = false` on
        Start**, which kills the whole class of "late joiner dragged into a game
        in progress with no state".
  - [ ] **B2.4 Fresh seed on Play Again** *(optional — ship without it if
        it fights back)*. A new room gets a new seed for free, since the seed is
        baked at creation. Only replaying inside the same room repeats the map.
        Fix: master sets `mapSeed`, waits for `OnRoomPropertiesUpdate`, THEN
        calls LoadScene. Ordering is guaranteed because PUN implements
        `AutomaticallySyncScene` as a room property too, so clients receive the
        seed before the scene switch.

  > **Options screen: deliberately deferred, reserve the button's slot.**
  > Nothing real goes in it yet — volume has no audio until D6, and
  > resolution/fullscreen are marginal on desktop and ignored on WebGL. It earns
  > its place the moment sound lands. The name field is NOT a reason to build it
  > (see B2.2).
  >
  > **Room codes: 4 characters, alphabet excluding I, O, 0 and 1.** Codes get
  > read aloud over Discord. Normalise input to uppercase — Photon room names
  > are case-sensitive, players won't be.
  >
  > **Don't set room properties after creation and then load a scene.** The seed
  > desync in the first 2-player playtest was exactly this race. Bake at
  > creation, or confirm via callback before loading (B2.4).
- [x] ~~**B3a. Remote smoke test**~~ — build sent to a friend, ran
      out-of-the-box on his machine; 1 full round played over the internet
      with territories, scores and board state in sync on both ends.
      Found + fixed: lobby list didn't refresh for players already waiting.
- [x] **B3b. Full remote regression** — **PASSED, 2026-08-15.** Two complete
      4-round 2-player games over the network (editor + standalone on a second
      machine). Scoring, parchments and winner determination all ran and agreed.
      This was the project's biggest unknown: before it, only round 1 had ever
      run remotely. It is now the best-tested path in the game rather than the
      worst. The seven issues it surfaced are C-a…C-h — all on top of a game
      that plays start to finish.
- [x] **B4.** `ca9b494`. **"Which one am I?" — marked the local player, then deleted
      CurrentTurnPanel.** The local card is suffixed "(you)"; the panel is gone from
      both builders. **One step left in Unity:** the live GameScene still has the
      old panel object, so delete `CurrentTurnPanel` under `LeftSidebar` by hand
      (or re-run the sidebar generator) and stretch `PlayerListContainer` to fill.
      Before B5, not after: four friends with similar names
      need to find themselves on screen, and right now nothing in the player
      list says which card is yours.

      The panel is mislabelled rather than merely redundant. `UpdateCurrentTurn`
      (`PlayerSidebar.cs:215`) calls `GetLocalPlayer()`, so it always shows YOU
      and never points at another player — it cannot, because a simultaneous
      draft has no turns to indicate. Its other content is covered twice over:
      `PickPromptUI` says "Pick 2 cards — Selected 1/2" with the Confirm button
      during Draft, and the phase banner already reads DRAFT PHASE /
      CONSTRUCTION.

      **Order matters: add the marking first, delete second.** `PlayerCardUI` has
      no local-player marking of any kind — the colour bars separate players from
      each other but not you from them. Delete the panel first and there is
      briefly no way to identify yourself at all. A "(you)" suffix or a brighter
      card frame is enough.

      Touches: `PlayerCardUI.cs`, `PlayerSidebar.cs` (the `currentTurnPanel`
      field and `UpdateCurrentTurn`), and `GameSceneUISetup.cs` in **two** places
      — the panel is built at both `:118` and `:385`.

      **Scope guard:** the minimal version only. Restyling the sidebar around the
      ~90px this frees up is Milestone D — that drift is exactly what parking
      visual work was meant to prevent.
- [x] **B5. WebGL build → itch.io** — **DONE 2026-10-20. The game is playable
      in a browser by anyone with the link.** 🎉

  Settings (`9aa53e5`): `FixedRegion: eu` so every build type lands in the same
  region — a release build picks region by ping while a dev build pins to the dev
  region, so without this a release build and the editor could sit in different
  regions and never see each other's rooms, with no error anywhere. Plus
  Decompression Fallback, so the build loads regardless of how itch serves Brotli.

  Build: release, not development (`9076f6d`) — **38.5 MB vs 155.9 MB** for the
  dev build, and no watermark.

  **Custom WebGL template** (`347e7c4`), because neither stock one works in an
  itch embed: Default resizes correctly but ships a fullscreen button that does
  nothing inside an iframe, and Minimal has clean chrome but hardcodes the canvas
  in pixels, so fullscreen left the game at 960x600 surrounded by black.
  `Assets/WebGLTemplates/Impocalypse` is Minimal with a viewport-filling canvas.

  Verified before it was assumed: WebGLSupport installed, all three scenes in
  Build Settings with Launcher first, no threading anywhere in project code, and
  PUN2 forcing `WebSocketSecure` on WebGL at `LoadBalancingClient.cs:839`/`:1236`
  — so the `Protocol: 0` (UDP) setting is overridden at connect time.

  **Packaging gotcha worth keeping:** zip with `tar`, not PowerShell 5.1's
  `Compress-Archive`, which writes Windows backslash separators. itch unzips on
  Linux, so those land as flat files literally named `Build\...`, `index.html`
  finds nothing, and you get a blank page indistinguishable from a broken game.

## Milestone C — Hardening & Balance (playtest-driven) ← **YOU ARE HERE**

> **Next action: send the itch link to 3 friends and watch a 4-player game.**
> Everything else here is downstream of what that session shows.

### Found in playtest 1 (2 players, 2 full games, 2026-08-15)

Two complete 4-round games over the network. Scoring, parchments and winner
determination all worked. Everything below is on top of a working game.

**The picks cluster — a player's decision can be taken from them. Fix first.**

- [x] **C-a.** `726fc26`. **Pick timer discarded your selection and picked at random.** Select two
      cards, let the timer run out, and `AutoPickForMissingPlayers`
      (`DraftService.cs:314`) submits two *random* cards instead of the two you
      chose. Not a bug in that method: selection is client-side, so the master
      genuinely does not know what you highlighted — it only knows what was
      submitted. Fix belongs on the client: auto-submit the current selection
      shortly before the master's deadline. Late submissions are already handled
      (`DraftService.cs:678`), so beating the deadline by a second is safe.
- [x] **C-b.** `726fc26`, fixed structurally — needs confirming in playtest 2 that
      it never recurs. **Confirm sometimes unavailable on the standalone build.**
      Intermittent, so it needs a repro before a fix. Two candidates: the button
      is driven by `PickSelectionChangedEvent`, and a `CardsDealtEvent` arriving
      after a selection resets it to non-interactable (`PickPromptUI.Refresh`);
      or `_selection` fails its ServiceLocator lookup. Instrument both before
      changing anything. Compounds C-a: no confirm means the timer robs you.

**Draft feedback**

- [x] **C-c.** `c806f7d`. **Cards stayed interactive after picks were confirmed.** Covers both
      "I can unselect after confirming" and "hovering other cards shrinks them".
      The *model* is already correct — `SubmitPicks` calls `LockSelections` and
      `OnCardUnconfirmed` honours it, which is why the right cards still get
      played. It is the view that ignores `SelectionsLocked`. One root cause,
      both symptoms.
- [x] **C-d.** `c806f7d`. **Board highlight survived the timer.** Hover a territory card, let
      the timer elapse; the board highlight stays lit after random picks land.
      Draft-phase-end needs to clear hover state.
- [ ] **C-e. Drafted cards are invisible until they matter.** A drafted building
      cannot be seen until the Build phase and a pact not until it reaches the
      pacts panel, so players lose track of what they took. Provisions do show in
      the top bar but testers did not notice it — wants a highlight *in addition
      to* the bar. Feature, not a defect.

- [x] **C-h.** `c806f7d`. **Construction phase: no card hover, and a sidebar showing stale
      draft info.** Two causes, and they suggest one rule that also settles C-c
      and C-d.

      *No hover:* `Card.OnPointerEnter` (`Card.cs:232`) is wrapped in
      `if (!_isDisabled)`, so a disabled card publishes no `CardInspectedEvent`,
      gets no scale and lights no tile. This directly contradicts the tooltip on
      the field above it (`Card.cs:34`): "Dimmed but still hoverable - you still
      need to read them to decide whether to swap a pick." The intent was right;
      the code never matched it. Hits the draft too — once your quota is full,
      the remaining cards become unreadable.

      *Stale sidebar:* `TileInfoPanel` subscribes to tile and card events but not
      to `GamePhaseChangedEvent`, so the last `CardInspectedEvent` of the draft
      simply stays on screen through Construction. Actively misinforming rather
      than merely empty.

      **The rule to apply across all of these:** *reading a card is always
      allowed; changing your picks is allowed only before you confirm.* Hover,
      inspector and tile highlight are information and should never be gated on
      pick state — selection and the pick affordance are the only things that
      lock. That resolves C-c (cards must stop being selectable after confirm,
      but stay readable), C-d (hover-derived state needs tearing down at phase
      end) and both halves of C-h without any of them contradicting each other.

- [ ] **C-i. First and last hand cards slide under the sidebars.** Seen in the
      deployed build, 2026-10-20. Caused by the sidebar widening in `df8ca70`:
      `CardFanLayout.maxFanWidth` is **1350**, which fitted the old 280px
      sidebars (clear span 1360) and does not fit the new 380px ones (clear span
      **1160**). Overruns ~95px each side, which is what the screenshot shows.

      One serialised value, not a layout problem: set `maxFanWidth` to ~1120 on
      the CardFanLayout component (1160 minus a little margin). Worth also
      deriving it from the sidebar width rather than hardcoding, so the next
      sidebar change does not silently reintroduce this.

**Results screen**

- [x] **C-f.** `7e041d3`. **GameOver score panels overlapped.** Both players' entries rendered on top
      of each other (see playtest screenshot: "Andjela" legible through
      "Aleksa"). Suspect `ScoreContainer`'s VerticalLayoutGroup runs with
      `childControlHeight = false` (`GameSceneUISetup.cs:905`) while entries grow
      with their fief detail lines, so tall entries overlap the next.
- [ ] **C-g. Breakdown reads as if scoring is broken — it is not.** The panel
      shows the **final round's** fief breakdown next to a **cumulative** total,
      unlabelled: "Domain 1 = 24, Domain 2 = 1" beside "58 Infamy". Verified
      correct — `ScoringState.CalculateScores` scores the whole board every
      round and `GameManager.AddRoundScores` accumulates, so a round-1 citadel
      does score in all four rounds. Only the labelling misleads.

- [ ] Fix what friends find (this list writes itself)
- [ ] **Camp priority resolution** (lowest number wins simultaneous claims —
      currently first-come at master). **Promoted by Milestone F**: first-come
      at master means the host wins ties, since their claim carries no network
      latency. A wrinkle between friends; a fairness defect in a ranked game.
      Cheaper to fix now than to invalidate results over later.
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
- [ ] **D7. GameRoom restyled** — the lobby is still the plain panels-and-list it
      started as. It works, and its join-refresh bug is fixed, but it does not
      read as the same product as the menu or the board.

      Deliberately last, and the lowest-value item here: it is the screen players
      look at for twenty seconds while waiting to start. Doing it after D1–D6
      also means it inherits a settled visual language rather than inventing one
      the board work then contradicts.

      The room code and Leave button are **not** part of this — they are B2.3,
      functional and already in place by then. This is the panels, spacing and
      colour around them.

      The main menu got styled on the way in only because it did not exist yet;
      a generator has to place elements somewhere, so it placed them themed.
      That was not a precedent for restyling screens that already work.

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

- [ ] **Revisit `FixedRegion`** — B5 pins it to `eu`, and it is a project-wide
      Photon setting rather than a per-platform one, so the Steam build inherits
      it. Fine for a friend group; it puts the whole world on EU servers for a
      release. Latency barely matters at 45-second timers, but the underlying
      constraint does: **Photon rooms are per-region, so a room code only works
      inside one region.** Let clients auto-pick their best region and two
      friends on different continents type the same code and never find each
      other — the same silent failure the pin was added to prevent, at a larger
      scale. Options: keep a global pin; let players choose a region in the
      (still unbuilt) Options screen and show it beside the code; or encode the
      region into the code itself. Decide here, not by accident.
- [ ] Steamworks account ($100 one-time)
- [ ] Store page: capsule art, screenshots, trailer/GIFs (art pass feeds this)
- [ ] Wishlist window (a few weeks minimum before launch)
- [ ] Optional: free demo or Steam Playtest for a wider beta
- [ ] **Launch 1.0** 🔥

---

## Milestone F — Competitive

*Post-1.0 and deliberately so. Nothing here should pull work out of 1.0 — the
game has to be good before it is ranked, and a ladder on an unbalanced game just
measures who found the strongest opening first.*

**The head start.** Three properties the game already has, for free, from
decisions already made:

- The map is deterministic from a seed every client holds.
- Every board-visible action is broadcast to everyone (`CityPlaced`,
  `SpawnImps`, `AssignPlayerIdentities`), and `RevealParchments` sends all
  scoring cards to all players at the end.
- Therefore **any client can independently recompute the final score.**

That is most of a competitive integrity story already. Verification is "recompute
and compare", not a rewrite, and a match is reconstructible from its seed plus
its event stream — so replays and dispute resolution are cheap here in a way they
usually are not.

**What is actually missing** is an authority that is not a player, and identity.

- [ ] **F1. Server-authoritative scoring.** The prerequisite for all of it.
      Scoring is master-authoritative today: correct for friends on Discord,
      fine through 1.0, because a modified host can only win one casual game.
      A *recorded* score changes that — a host who fabricates one poisons
      everybody else's honest results too. Not fixable by changing netcode
      vendor (Fusion and NGO trust the host in host mode as well); it needs
      authority off the players' machines. **Costs money and devops forever** —
      that is the real price of a competitive mode, and it should be a decision
      rather than something discovered later.
- [ ] **F2. Identity** — Steamworks (SteamID). **Do not build an account
      system:** Milestone E already puts the game on a platform that supplies
      identity and a Leaderboards API.
- [ ] **F3. Ranked ladder + leaderboards.** After F1, never before. A leaderboard
      shipped on host-authoritative scoring is one nobody should trust, and
      withdrawing it after people have competed on it is worse than never
      having had it.
- [ ] **F4. Match records / replays.** Cheap given the head start above: seed +
      event stream is a full match. Doubles as anti-cheat evidence and as the
      thing that makes balance arguments settleable.
- [ ] **F5. Tournaments.** Sits on top of F1–F4. Furthest out.

> **What this ambition changes BEFORE 1.0 — one item, and it is already on the
> list.** Milestone C's *camp priority resolution* (lowest number wins
> simultaneous claims; currently first-come at master) stops being a wrinkle and
> becomes a fairness defect: first-come-at-master means **the host wins ties**,
> because their claim has no network latency. Tolerable between friends,
> disqualifying in a ranked game. Competitive intent promotes that from "nice to
> have" to "required", and it is far cheaper to fix now than to invalidate a
> season of results over later.
>
> Nothing else moves. Balance work (Milestone C) matters more for a competitive
> game than a casual one, but it was already there and playtests drive it either
> way.

## Parking Lot (post-1.0, or "when it hurts")

- Bots / single-player mode
- **Chat / emotes** — raised 2026-10-24, never discussed before that. Recording
  the reasoning so it is not re-derived.

  **The roadmap already assumes Discord**, in two places: room codes are "read
  aloud over Discord", and the trust model is "correct for friends on Discord".
  So the design has quietly assumed a voice channel alongside the game. Chat is
  what is needed when there is not one.

  **Chat matters at the edges, not during play.** Lobby (waiting) and the end
  screen (gg) yes; the 45s draft and 90s build no - typing against a pick timer
  is hostile. That is convenient, because the edges are where nothing else is
  competing for attention.

  **Emotes beat chat for this game.** A handful of one-click reactions - taunt,
  nice one, hurry up - because they work during a timed draft where typing
  cannot, carry no moderation surface if the itch link spreads, fit a
  hell-themed area-control game better than conversation, and are nearly free:
  an emote is one more byte event code on the existing SendToAll plumbing. No
  Photon Chat product, no extra CCU. Real chat costs a text input, a message
  list, scrollback, join history and the moderation question.

  **Decide from the playtest, not from a chair.** Four people playing answer it
  for free: if they are on Discord and never miss it, the whole feature is saved;
  if they reach for something, the moment and the kind tell you which feature it
  actually is - a lobby "ready?" is not the same thing as trash-talking a pact.
- **Mobile port** — investigated 2026-10-20 on a real phone. The original note
  here ("touch pass + UI density; architecture already ports") was right, and a
  fresh reading that called it a design change was wrong. Recording why.

  **The touch interaction already works.** `Card` implements `IPointerEnterHandler`
  but clicks go through a `Button`, and on touch those fire at different moments:

  | gesture | result |
  |---|---|
  | touch down | `OnPointerEnter` — inspector fills, tile highlights |
  | hold | stays shown |
  | release on the card | `Button.onClick` — picks it |
  | slide off, then release | `OnPointerExit`, no matching up — **no pick** |

  So hold-to-read, release-to-pick, slide-away-to-cancel all work already. The
  assumption that touch cannot read a card before committing to it is false; it
  falls out of Unity's event model rather than from design, but it holds. This
  also satisfies the C-c/C-h rule (reading always allowed, picking gated) without
  needing separate gestures.

  **What is actually left is density, not design:**
  - Discoverability — nobody knows to hold. One hint line on touch devices.
  - Text: 14px against a 1920 reference renders about 4px on a phone.
  - Touch targets: bottom-row cards land near 30px against a ~44px minimum.
  - Sidebars take 760 of 1920 and are fixed width; want narrowing or collapsing.
  - **Pinch zoom is already written** in `Utility/CameraController.cs:247`, which
    the scene does NOT use — `ModernCameraController` has single-touch pan but no
    pinch. Port those ~15 lines rather than writing them.
  - Landscape only, and move the CanvasScaler match from 0.5 toward 1.0 so UI
    scales with the short axis.

  Estimate: one or two sessions, no rework of the interaction model.

  **Sequencing:** still after the 4-player desktop playtest, which is what says
  whether any polish is worth buying. But mobile widens the playtest pool a lot —
  a link works on a phone in a way it does not on a laptop nobody has open.
- Persistent identity / leaderboards / tournaments → **promoted to Milestone F.**
- True auto-tiling art (merged lakes/ranges with edge-matched variants)
- Lair sprite usage (burrow on owned tiles / capital mechanic / menu art)
- Sky-tower strict-BK rule (gate occupies building slot) — house rule stands
- Cosmetic enum renames (TileType.Water→Styx etc.) — display names already
  handled by GameLexicon; internal ids stay until a quiet moment
