# 🏗️ Impocalypse Unity Project - Complete Architecture Refactoring Prompt

## 🎯 **Objective**
Refactor the Impocalypse Unity project to implement clean architecture principles, eliminate anti-patterns, and follow Unity best practices. Transform the current singleton-heavy, tightly-coupled codebase into a maintainable and scalable architecture.

## 📋 **Current Issues to Address**

### **Critical Anti-Patterns**
1. **Singleton Anti-Patterns**: Multiple static instances (`GameManager.Instance`, `GridGenerator.Instance`, `DraftHUD.Instance`)
2. **Tight Coupling**: Direct dependencies between components
3. **Performance Issues**: `FindFirstObjectByType` calls in event handlers
4. **Mixed Responsibilities**: Classes doing too many things
5. **Hard-coded Values**: Magic numbers and strings throughout

### **Architecture Problems**
1. **No Dependency Injection**: Direct singleton access everywhere
2. **Poor Maintainability**: Hard to modify and extend
3. **UI Coupling**: Game logic directly manipulating UI
4. **Network Coupling**: Game logic mixed with network code

## 🏛️ **Target Architecture**

### **Clean Architecture Layers**
```
┌─────────────────────────────────────┐
│           Presentation Layer        │
│  (UI Controllers, HUD, Input)       │
├─────────────────────────────────────┤
│         Application Layer           │
│  (Use Cases, Game Orchestration)    │
├─────────────────────────────────────┤
│         Domain Layer                │
│  (Game Logic, Entities, Rules)      │
├─────────────────────────────────────┤
│         Infrastructure Layer        │
│  (Network, Data, External Services) │
└─────────────────────────────────────┘
```

### **Dependency Flow**
- **Dependencies point inward** (UI → Application → Domain ← Infrastructure)
- **No circular dependencies**
- **Interfaces define contracts**
- **Dependency injection manages dependencies**

## 🔧 **Implementation Steps**

### **Phase 1: Create Interfaces and Contracts**

#### **1.1 Domain Interfaces**
Create these interfaces in `Assets/_Project/Scripts/Interfaces/`:

```csharp
// IGameState.cs
public interface IGameState
{
    int CurrentRound { get; }
    GamePhase CurrentPhase { get; }
    int TotalPlayers { get; }
    bool IsClockwise { get; }
    void AdvancePhase();
    void SetRound(int round);
    void SetPhase(GamePhase phase);
}

// IGameRules.cs
public interface IGameRules
{
    int GetCardsPerPlayer(int playerCount);
    bool IsClockwise(int round);
    int MaxRounds { get; }
}

// IPlayerRepository.cs
public interface IPlayerRepository
{
    PlayerData GetPlayer(int actorNumber);
    void AddPlayer(PlayerData player);
    void RemovePlayer(int actorNumber);
    IEnumerable<PlayerData> GetAllPlayers();
    void Clear();
}

// ICardService.cs
public interface ICardService
{
    void DealCards(int playerCount, int cardsPerPlayer);
    void PassHandsToNextPlayer();
    void SubmitPicks(int playerActor, string[] coordinates);
    event System.Action<string[]> OnCardsDealt;
    event System.Action OnHandsPassed;
}

// IImpService.cs
public interface IImpService
{
    void SpawnImp(string coordinate, int ownerActor);
    void SpawnImps(IEnumerable<ImpSpawnData> impData);
    event System.Action<ImpSpawnData> OnImpSpawned;
}
```

#### **1.2 Infrastructure Interfaces**
```csharp
// INetworkService.cs
public interface INetworkService
{
    bool IsMasterClient { get; }
    bool IsConnected { get; }
    int LocalPlayerActor { get; }
    int TotalPlayers { get; }
    void RaiseEvent(byte eventCode, object data, RaiseEventOptions options, SendOptions sendOptions);
    void AddCallbackTarget(object target);
    void RemoveCallbackTarget(object target);
    event System.Action<EventData> OnEventReceived;
}

// IGridService.cs
public interface IGridService
{
    TileData GetTileByCoord(string coord);
    string[] GetRandomCoords(int count);
    void GenerateGrid();
    event System.Action OnGridGenerated;
}
```

#### **1.3 UI Interfaces**
```csharp
// IHUDService.cs
public interface IHUDService
{
    void SetRound(int round);
    void SetPhase(GamePhase phase);
    void SetStatus(string status);
    void UpdateHUD(int round, GamePhase phase, string status);
}

// ICardUIService.cs
public interface ICardUIService
{
    void SpawnCards(string[] coordinates);
    void DisableUnselectedCards();
    void ClearCards();
    event System.Action<Card> OnCardSelected;
    event System.Action<Card> OnCardConfirmed;
}
```

### **Phase 2: Implement Domain Layer**

#### **2.1 Domain Entities**
```csharp
// Assets/_Project/Scripts/Domain/Entities/GameState.cs
public class GameState : IGameState
{
    private int currentRound = 1;
    private GamePhase currentPhase = GamePhase.Draft;
    private readonly IGameRules gameRules;
    private readonly INetworkService networkService;

    public int CurrentRound => currentRound;
    public GamePhase CurrentPhase => currentPhase;
    public int TotalPlayers => networkService.TotalPlayers;
    public bool IsClockwise => gameRules.IsClockwise(currentRound);

    public GameState(IGameRules gameRules, INetworkService networkService)
    {
        this.gameRules = gameRules;
        this.networkService = networkService;
    }

    public void AdvancePhase()
    {
        if (currentPhase == GamePhase.Draft)
        {
            currentPhase = GamePhase.Build;
        }
        else if (currentPhase == GamePhase.Build)
        {
            if (currentRound >= gameRules.MaxRounds)
            {
                // Game over logic
                return;
            }
            currentRound++;
            currentPhase = GamePhase.Draft;
        }
    }

    public void SetRound(int round) => currentRound = round;
    public void SetPhase(GamePhase phase) => currentPhase = phase;
}
```

#### **2.2 Domain Services**
```csharp
// Assets/_Project/Scripts/Domain/Services/CardService.cs
public class CardService : ICardService
{
    private readonly INetworkService networkService;
    private readonly IGridService gridService;
    private Dictionary<int, List<string>> playerHands = new();
    private Dictionary<int, List<string>> pendingPicks = new();

    public event System.Action<string[]> OnCardsDealt;
    public event System.Action OnHandsPassed;

    public CardService(INetworkService networkService, IGridService gridService)
    {
        this.networkService = networkService;
        this.gridService = gridService;
    }

    public void DealCards(int playerCount, int cardsPerPlayer)
    {
        var allCoords = gridService.GetRandomCoords(cardsPerPlayer * playerCount);
        
        for (int i = 0; i < playerCount; i++)
        {
            var coords = allCoords.Skip(i * cardsPerPlayer).Take(cardsPerPlayer).ToList();
            playerHands[i + 1] = coords; // Actor numbers start at 1
        }
        
        OnCardsDealt?.Invoke(allCoords);
    }

    public void PassHandsToNextPlayer()
    {
        var playerList = networkService.GetPlayerList().OrderBy(p => p.ActorNumber).ToArray();
        Dictionary<int, List<string>> newHands = new();

        for (int i = 0; i < playerList.Length; i++)
        {
            int fromIndex = (i - 1 + playerList.Length) % playerList.Length;
            int toActor = playerList[i].ActorNumber;
            int fromActor = playerList[fromIndex].ActorNumber;

            newHands[toActor] = playerHands.ContainsKey(fromActor) 
                ? new List<string>(playerHands[fromActor]) 
                : new List<string>();
        }

        playerHands = newHands;
        OnHandsPassed?.Invoke();
    }

    public void SubmitPicks(int playerActor, string[] coordinates)
    {
        if (!pendingPicks.ContainsKey(playerActor))
            pendingPicks[playerActor] = new List<string>();

        pendingPicks[playerActor].AddRange(coordinates);

        if (pendingPicks.Count == networkService.TotalPlayers)
        {
            ProcessAllPicks();
        }
    }

    private void ProcessAllPicks()
    {
        // Remove picked cards from hands
        foreach (var kvp in pendingPicks)
        {
            if (playerHands.ContainsKey(kvp.Key))
            {
                playerHands[kvp.Key].RemoveAll(c => kvp.Value.Contains(c));
            }
        }

        pendingPicks.Clear();
    }
}

// Assets/_Project/Scripts/Domain/Services/ImpService.cs
public class ImpService : IImpService
{
    private readonly INetworkService networkService;
    private readonly IGridService gridService;

    public event System.Action<ImpSpawnData> OnImpSpawned;

    public ImpService(INetworkService networkService, IGridService gridService)
    {
        this.networkService = networkService;
        this.gridService = gridService;
    }

    public void SpawnImp(string coordinate, int ownerActor)
    {
        var impData = new ImpSpawnData { Coord = coordinate, OwnerActor = ownerActor };
        OnImpSpawned?.Invoke(impData);
    }

    public void SpawnImps(IEnumerable<ImpSpawnData> impData)
    {
        foreach (var data in impData)
        {
            SpawnImp(data.Coord, data.OwnerActor);
        }
    }
}

// Assets/_Project/Scripts/Domain/Services/PlayerRepository.cs
public class PlayerRepository : IPlayerRepository
{
    private Dictionary<int, PlayerData> players = new();

    public PlayerData GetPlayer(int actorNumber)
    {
        return players.TryGetValue(actorNumber, out var player) ? player : null;
    }

    public void AddPlayer(PlayerData player)
    {
        players[player.ActorNumber] = player;
    }

    public void RemovePlayer(int actorNumber)
    {
        players.Remove(actorNumber);
    }

    public IEnumerable<PlayerData> GetAllPlayers()
    {
        return players.Values;
    }

    public void Clear()
    {
        players.Clear();
    }
}
```

### **Phase 3: Implement Infrastructure Layer**

#### **3.1 Network Service**
```csharp
// Assets/_Project/Scripts/Infrastructure/Services/PhotonNetworkService.cs
public class PhotonNetworkService : MonoBehaviour, INetworkService, IOnEventCallback
{
    public bool IsMasterClient => PhotonNetwork.IsMasterClient;
    public bool IsConnected => PhotonNetwork.IsConnected;
    public int LocalPlayerActor => PhotonNetwork.LocalPlayer.ActorNumber;
    public int TotalPlayers => PhotonNetwork.CurrentRoom?.PlayerCount ?? 1;

    public event System.Action<EventData> OnEventReceived;

    private void Start()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }

    private void OnDestroy()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    public void RaiseEvent(byte eventCode, object data, RaiseEventOptions options, SendOptions sendOptions)
    {
        PhotonNetwork.RaiseEvent(eventCode, data, options, sendOptions);
    }

    public void AddCallbackTarget(object target)
    {
        PhotonNetwork.AddCallbackTarget(target);
    }

    public void RemoveCallbackTarget(object target)
    {
        PhotonNetwork.RemoveCallbackTarget(target);
    }

    public void OnEvent(EventData photonEvent)
    {
        OnEventReceived?.Invoke(photonEvent);
    }

    public Player[] GetPlayerList()
    {
        return PhotonNetwork.PlayerList;
    }
}
```

#### **3.2 Grid Service**
```csharp
// Assets/_Project/Scripts/Infrastructure/Services/GridService.cs
public class GridService : MonoBehaviour, IGridService
{
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private Sprite grassSprite;
    [SerializeField] private Sprite waterSprite;
    
    [Header("Coordinate Sprites")]
    [SerializeField] private Sprite[] letterSprites;     // A–J in order
    [SerializeField] private Sprite[] numberSprites;     // 1–10 in order
    [SerializeField] private Sprite emptyBorderSprite;
    
    private List<TileData> allTiles = new();

    public event System.Action OnGridGenerated;

    public TileData GetTileByCoord(string coord)
    {
        return allTiles.FirstOrDefault(t => t.coord == coord);
    }

    public string[] GetRandomCoords(int count)
    {
        return allTiles
            .Where(t => t.type == TileData.TileType.Grass || t.type == TileData.TileType.Water)
            .OrderBy(x => Random.value)
            .Take(count)
            .Select(t => t.coord)
            .ToArray();
    }

    public void GenerateGrid()
    {
        var reversedLetters = letterSprites.Reverse().ToArray();
        allTiles.Clear();

        for (int x = 0; x < 12; x++)
        {
            for (int y = 0; y < 12; y++)
            {
                Vector3 position = new Vector3(x, y, 0);
                GameObject tile = Instantiate(tilePrefab, position, Quaternion.identity, transform);
                TileData tileData = tile.GetComponent<TileData>();

                allTiles.Add(tileData);

                // Coordinate label tiles
                if ((x == 0 || x == 11) && (y >= 1 && y <= 10))
                {
                    Sprite letter = reversedLetters[y - 1];
                    tileData.Init(TileData.TileType.Coordinate, $"Letter_{y}", new Vector2Int(x, y), letter);
                }
                else if ((y == 0 || y == 11) && (x >= 1 && x <= 10))
                {
                    Sprite number = numberSprites[x - 1];
                    tileData.Init(TileData.TileType.Coordinate, $"Number_{x}", new Vector2Int(x, y), number);
                }
                else if ((x == 0 || x == 11) && (y == 0 || y == 11))
                {
                    tileData.Init(TileData.TileType.EmptyCorner, "Corner", new Vector2Int(x, y), emptyBorderSprite);
                }
                else
                {
                    // Playable tile (grass or water)
                    TileData.TileType tileType = Random.value < 0.5f ? TileData.TileType.Grass : TileData.TileType.Water;
                    Sprite tileSprite = tileType == TileData.TileType.Grass ? grassSprite : waterSprite;
                    string coord = $"{(char)('A' + (10 - (y - 1)) - 1)}{x}";
                    tileData.Init(tileType, coord, new Vector2Int(x, y), tileSprite);
                }
            }
        }

        OnGridGenerated?.Invoke();
    }
}
```

### **Phase 4: Implement Application Layer**

#### **4.1 Game Orchestrator (Refactored)**
```csharp
// Assets/_Project/Scripts/Application/GameOrchestrator.cs
public class GameOrchestrator : MonoBehaviour
{
    [Header("Services")]
    [SerializeField] private IGameState gameState;
    [SerializeField] private INetworkService networkService;
    [SerializeField] private ICardService cardService;
    [SerializeField] private IImpService impService;
    [SerializeField] private IHUDService hudService;
    [SerializeField] private IPlayerRepository playerRepository;

    private void Start()
    {
        SetupEventHandlers();
        
        if (networkService.IsMasterClient)
        {
            InitializeGame();
        }
    }

    private void SetupEventHandlers()
    {
        networkService.OnEventReceived += HandleNetworkEvent;
        cardService.OnCardsDealt += HandleCardsDealt;
        cardService.OnHandsPassed += HandleHandsPassed;
        impService.OnImpSpawned += HandleImpSpawned;
    }

    private void HandleNetworkEvent(EventData photonEvent)
    {
        switch (photonEvent.Code)
        {
            case NetworkEvents.SubmitPicks:
                HandleSubmitPicks(photonEvent);
                break;
            case NetworkEvents.AssignPlayerIdentities:
                HandlePlayerIdentities(photonEvent);
                break;
        }
    }

    private void HandleSubmitPicks(EventData photonEvent)
    {
        var coords = ((object[])photonEvent.CustomData).Cast<string>().ToArray();
        int actorNumber = photonEvent.Sender;
        
        cardService.SubmitPicks(actorNumber, coords);
        
        // Update UI
        hudService.SetStatus($"Player {actorNumber} submitted picks");
    }

    private void HandlePlayerIdentities(EventData photonEvent)
    {
        object[] rawData = (object[])photonEvent.CustomData;
        playerRepository.Clear();

        foreach (object[] entry in rawData)
        {
            int actor = (int)entry[0];
            string nick = (string)entry[1];
            string color = (string)entry[2];

            playerRepository.AddPlayer(new PlayerData
            {
                ActorNumber = actor,
                Nickname = nick,
                ImpColor = color
            });
        }
    }

    private void HandleCardsDealt(string[] coordinates)
    {
        // Cards were dealt, update UI if needed
        hudService.SetStatus("Cards dealt - select your picks");
    }

    private void HandleHandsPassed()
    {
        // Hands were passed to next player
        hudService.SetStatus("Hands passed to next player");
    }

    private void HandleImpSpawned(ImpSpawnData impData)
    {
        // Imp was spawned, update visual representation
        var player = playerRepository.GetPlayer(impData.OwnerActor);
        if (player != null)
        {
            // Handle imp spawning visualization
        }
    }

    private void InitializeGame()
    {
        // Initialize game state
        gameState.SetRound(1);
        gameState.SetPhase(GamePhase.Draft);
        
        // Assign player identities
        AssignPlayerIdentities();
        
        // Deal initial cards
        cardService.DealCards(gameState.TotalPlayers, gameState.CardsPerPlayer);
        
        // Update UI
        hudService.UpdateHUD(gameState.CurrentRound, gameState.CurrentPhase, "Pick your cards...");
    }

    private void AssignPlayerIdentities()
    {
        var players = networkService.GetPlayerList().OrderBy(p => p.ActorNumber).ToArray();
        string[] impColors = new[] { "red", "green", "blue", "purple" };

        List<object> identityArray = new();
        for (int i = 0; i < players.Length; i++)
        {
            identityArray.Add(new object[]
            {
                players[i].ActorNumber,
                players[i].NickName,
                impColors[i % impColors.Length]
            });
        }

        networkService.RaiseEvent(NetworkEvents.AssignPlayerIdentities, identityArray.ToArray(),
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            SendOptions.SendReliable);
    }
}
```

### **Phase 5: Implement Presentation Layer**

#### **5.1 HUD Service**
```csharp
// Assets/_Project/Scripts/Presentation/Services/HUDService.cs
public class HUDService : MonoBehaviour, IHUDService
{
    [Header("UI References")]
    [SerializeField] private TMP_Text roundText;
    [SerializeField] private TMP_Text phaseText;
    [SerializeField] private TMP_Text statusText;

    public void SetRound(int round)
    {
        if (roundText != null)
            roundText.text = $"Round {round}";
    }

    public void SetPhase(GamePhase phase)
    {
        if (phaseText != null)
            phaseText.text = $"Phase: {phase.ToString().ToUpper()}";
    }

    public void SetStatus(string status)
    {
        if (statusText != null)
            statusText.text = status;
    }

    public void UpdateHUD(int round, GamePhase phase, string status)
    {
        SetRound(round);
        SetPhase(phase);
        SetStatus(status);
    }
}
```

#### **5.2 Card UI Service**
```csharp
// Assets/_Project/Scripts/Presentation/Services/CardUIService.cs
public class CardUIService : MonoBehaviour, ICardUIService
{
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private Transform cardPanel;

    private List<Card> activeCards = new();
    private List<Card> selectedCards = new();

    public event System.Action<Card> OnCardSelected;
    public event System.Action<Card> OnCardConfirmed;

    private void Start()
    {
        Card.OnCardConfirmed += OnCardConfirmed;
    }

    private void OnDestroy()
    {
        Card.OnCardConfirmed -= OnCardConfirmed;
    }

    public void SpawnCards(string[] coordinates)
    {
        ClearCards();

        foreach (var coord in coordinates)
        {
            GameObject go = Instantiate(cardPrefab, cardPanel);
            Card card = go.GetComponent<Card>();
            if (card != null)
            {
                card.coordinate = coord;
                activeCards.Add(card);
            }
        }
    }

    public void DisableUnselectedCards()
    {
        foreach (var card in activeCards)
        {
            if (!selectedCards.Contains(card))
                card.Disable();
        }
    }

    public void ClearCards()
    {
        foreach (Transform child in cardPanel)
            Destroy(child.gameObject);

        activeCards.Clear();
        selectedCards.Clear();
    }
}
```

### **Phase 6: Dependency Injection Setup**

#### **6.1 Service Locator**
```csharp
// Assets/_Project/Scripts/Infrastructure/ServiceLocator.cs
public class ServiceLocator : MonoBehaviour
{
    [Header("Services")]
    [SerializeField] private PhotonNetworkService networkService;
    [SerializeField] private GridService gridService;
    [SerializeField] private HUDService hudService;
    [SerializeField] private CardUIService cardUIService;

    // Service instances
    private IGameState gameState;
    private IGameRules gameRules;
    private ICardService cardService;
    private IImpService impService;
    private IPlayerRepository playerRepository;

    private void Awake()
    {
        InitializeServices();
        InjectDependencies();
    }

    private void InitializeServices()
    {
        // Load configuration
        gameRules = Resources.Load<GameRules>("DevRules");
        
        // Create service instances
        gameState = new GameState(gameRules, networkService);
        cardService = new CardService(networkService, gridService);
        impService = new ImpService(networkService, gridService);
        playerRepository = new PlayerRepository();
    }

    private void InjectDependencies()
    {
        // Find all components that need services and inject them
        var orchestrator = FindFirstObjectByType<GameOrchestrator>();
        if (orchestrator != null)
        {
            // Use reflection or serialized fields to inject dependencies
            // This is a simplified example - you'd need proper DI container
        }
    }

    // Public accessors for services
    public IGameState GameState => gameState;
    public INetworkService NetworkService => networkService;
    public ICardService CardService => cardService;
    public IImpService ImpService => impService;
    public IHUDService HUDService => hudService;
    public ICardUIService CardUIService => cardUIService;
    public IPlayerRepository PlayerRepository => playerRepository;
}
```

### **Phase 7: Migration and Cleanup**

#### **7.1 Remove Singleton References**
Replace all singleton access with dependency injection:

```csharp
// ❌ OLD: Singleton access
GameManager.Instance.currentRound++;
GridGenerator.Instance.GetTileByCoord(coord);

// ✅ NEW: Dependency injection
[SerializeField] private IGameState gameState;
[SerializeField] private IGridService gridService;

gameState.CurrentRound++;
gridService.GetTileByCoord(coord);
```

#### **7.2 Update Scene Setup**
1. **Remove singleton components** from scenes
2. **Add ServiceLocator** to main scene
3. **Configure dependencies** in inspector
4. **Update prefabs** to use new architecture

#### **7.3 Performance Optimizations**
1. **Cache references** instead of using `FindFirstObjectByType`
2. **Use object pooling** for frequently created objects
3. **Optimize event handling** to reduce garbage collection

## 📋 **Implementation Checklist**

### **Phase 1: Interfaces** ✅
- [ ] Create all domain interfaces
- [ ] Create infrastructure interfaces  
- [ ] Create UI interfaces
- [ ] Define event contracts

### **Phase 2: Domain Layer** ✅
- [ ] Implement GameState entity
- [ ] Implement CardService
- [ ] Implement ImpService
- [ ] Implement PlayerRepository

### **Phase 3: Infrastructure Layer** ✅
- [ ] Implement PhotonNetworkService
- [ ] Implement GridService
- [ ] Move network logic to services
- [ ] Implement data persistence

### **Phase 4: Application Layer** ✅
- [ ] Refactor GameOrchestrator
- [ ] Implement use cases
- [ ] Add event handling
- [ ] Remove business logic from UI

### **Phase 5: Presentation Layer** ✅
- [ ] Implement HUDService
- [ ] Implement CardUIService
- [ ] Remove UI logic from game classes
- [ ] Add proper UI event handling

### **Phase 6: Dependency Injection** ✅
- [ ] Implement ServiceLocator
- [ ] Configure dependencies
- [ ] Update scene setup
- [ ] Test dependency injection

### **Phase 7: Migration** ✅
- [ ] Remove all singleton references
- [ ] Update all components to use DI
- [ ] Optimize performance
- [ ] Add error handling

## 🎯 **Success Criteria**

### **Architecture Goals**
- [ ] **Zero singletons** in game logic
- [ ] **Dependency injection** throughout
- [ ] **Interface-based design** for all major components
- [ ] **Separation of concerns** maintained
- [ ] **Maintainable code** with clear dependencies

### **Performance Goals**
- [ ] **No FindFirstObjectByType** in event handlers
- [ ] **Cached references** for frequently accessed objects
- [ ] **Efficient event handling** with minimal GC pressure
- [ ] **Optimized network** communication

### **Maintainability Goals**
- [ ] **Clear dependencies** and relationships
- [ ] **Consistent patterns** throughout codebase
- [ ] **Documented interfaces** and contracts
- [ ] **Easy to extend** and modify

## 🚨 **Important Notes**

1. **Preserve Functionality**: Ensure all existing game features work exactly the same
2. **Incremental Migration**: Refactor one component at a time to avoid breaking changes
3. **Backup Strategy**: Keep original files until refactoring is complete and tested
4. **Documentation**: Update documentation as you refactor

## 🎮 **Final Architecture Benefits**

- **Maintainable**: Easy to modify and extend
- **Scalable**: Easy to add new features
- **Performant**: Optimized for Unity and networking
- **Clean**: Follows SOLID principles and clean architecture

This refactoring will transform the codebase from a tightly-coupled, singleton-heavy architecture into a clean, maintainable, and scalable system that follows Unity best practices and modern software architecture principles. 