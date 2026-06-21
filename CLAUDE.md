# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Impocalypse is a multiplayer grid-based card drafting game built with Unity and Photon PUN2. Players draft cards representing grid coordinates, then spawn "imps" on those tiles to collect resources.

## Build & Run

This is a Unity project. Open in Unity Editor (2022.3+ LTS recommended):
- Open Unity Hub → Add project from disk
- Play mode: Press Play in the Unity Editor
- Build: File → Build Settings → Select platform → Build

No command-line build system. All testing is done via Unity's Play mode.

## Architecture

### Service Locator Pattern

The codebase uses a **Service Locator** for dependency injection. All major systems register themselves as services in `Awake()` and are accessed via `ServiceLocator.GetService<T>()`.

```csharp
// Standard access
var gridService = ServiceLocator.GetService<IGridService>();

// Safe access with null check
if (ServiceLocator.TryGetService<IEventBus>(out var eventBus))
{
    eventBus.Subscribe<CardSelectedEvent>(OnCardSelected);
}

// Required service (throws if not found)
var required = ServiceLocator.GetRequiredService<INetworkService>();
```

### Core Systems

| System | Interface | Implementation | Purpose |
|--------|-----------|----------------|---------|
| Event Bus | `IEventBus` | `EventBus` | Pub/sub for decoupled communication |
| Network | `INetworkService` | `NetworkService` | Photon abstraction (no Photon types exposed) |
| Game Manager | `IGameManager` | `GameManager` | Round/phase control |
| Grid | `IGridService` | `GridGenerator` | 12x12 tile grid (10x10 playable) |
| Resources | `IResourceService` | `ResourceManager` | Resource tracking (data-driven) |
| Draft | `IDraftService` | `DraftService` | Card dealing, picking, hand passing |
| Players | `IPlayerService` | `PlayerService` | Player data management |
| Draft HUD | `IDraftHUD` | `DraftHUD` | UI abstraction |

### Event-Driven Architecture

The `EventBus` enables loose coupling between systems:

```csharp
// Subscribe
_eventBus.Subscribe<CardConfirmedEvent>(OnCardConfirmed);

// Publish
_eventBus.Publish(new CardConfirmedEvent(coordinate, playerId));

// Unsubscribe in OnDestroy
_eventBus.Unsubscribe<CardConfirmedEvent>(OnCardConfirmed);
```

**Key Events** (defined in `Core/Events/GameEvents.cs`):
- Network: `ConnectedToServerEvent`, `PlayerJoinedEvent`, `PlayerLeftEvent`
- Game: `GamePhaseChangedEvent`, `RoundStartedEvent`, `GameEndedEvent`
- Draft: `CardsDealtEvent`, `CardSelectedEvent`, `CardConfirmedEvent`, `AllPicksSubmittedEvent`
- Imps: `SpawnImpEvent`, `ImpSpawnedEvent`
- Resources: `ResourceCollectedEvent`, `ResourceChangedEvent`

### State Machine

Game phases managed by `GameStateMachine`:

```
LobbyState → DraftState → BuildState → ScoringState → GameOverState
                ↓              ↑
                └──────────────┘ (repeat for 4 rounds)
```

States are in `StateMachine/States/` and implement `IGameState`.

### Multiplayer (Master-Client Model)

Photon PUN2 with full abstraction behind `INetworkService`:
- **Master client** controls: game orchestration, card dealing, round progression, hand rotation
- **All clients** send picks to master, receive spawns/state from master
- Network events defined in `NetworkEventCodes.cs` (NOT NetworkEvents.cs)

```csharp
// Sending events
_networkService.SendToAll(NetworkEventCodes.DealCards, data);
_networkService.SendToMaster(NetworkEventCodes.SubmitPicks, picks);

// Receiving events via INetworkService.OnEventReceived or EventBus
```

### Key Folders

```
Assets/_Project/Scripts/
├── Cards/              # Card system (CardFactory, CardSelectionHandler)
├── Config/             # ScriptableObjects (GameConfiguration, DraftConfiguration)
├── Core/
│   ├── Bootstrap/      # GameBootstrapper (initialization)
│   ├── Events/         # EventBus, GameEvents
│   ├── Pooling/        # Object pooling system
│   └── GamePhase.cs    # Phase enum
├── Data/               # ResourceManager, IResourceService
├── Draft/
│   ├── Handlers/       # DraftNetworkHandler
│   └── Services/       # DraftService
├── Game/               # GameOrchestrator
├── Imps/               # Imp, ImpFactory, ImpSpawner
├── Network/            # NetworkService, NetworkEventCodes, Mock/
├── Players/            # PlayerService, PlayerData
├── StateMachine/       # GameStateMachine, States/
├── UI/                 # DraftHUD, Abstractions/IDraftHUD
├── Utility/            # ServiceLocator, Interfaces/
├── Visuals/            # ImpVisuals
└── World/              # GridGenerator, TileData
```

### Object Pooling

Cards and Imps use object pooling to reduce GC:

```csharp
// Create pool from prefab
var pool = ObjectPool<Card>.FromPrefab(cardPrefab, container, initialSize);

// Get/Return
var card = pool.Get();
pool.Return(card);
```

### Configuration

Game settings via ScriptableObjects in `Config/`:
- `GameConfiguration` - Max rounds, grid size, player limits
- `DraftConfiguration` - Cards per player, picks per round, pass direction
- `ResourceDefinition` - Individual resource types (data-driven)
- `PlayerColorPalette` - Player colors and imp sprites

## Conventions

- Services self-register in `Awake()`, unregister in `OnDestroy()`
- Always null-check service access: `service?.Method()`
- Persistent objects use `DontDestroyOnLoad`
- Use `using Core;` for `GamePhase` enum
- Use `using Core.Events;` for event types
- Prefer EventBus over direct references for cross-system communication
- Prefer interfaces over concrete types for service access

## Coordinate System

Grid uses letter-row, number-column coordinates: `A1` through `J10` (playable 10x10 area within 12x12 grid including border labels).
