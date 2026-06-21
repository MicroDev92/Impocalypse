using System.Collections.Generic;

namespace Core.Events
{
    // Note: GamePhase enum is in Core.GamePhase
    // ============================================
    // NETWORK EVENTS
    // ============================================

    /// <summary>
    /// Fired when connection to server is established.
    /// </summary>
    public struct ConnectedToServerEvent : IGameEvent { }

    /// <summary>
    /// Fired when disconnected from server.
    /// </summary>
    public struct DisconnectedEvent : IGameEvent
    {
        public string Reason { get; }
        public DisconnectedEvent(string reason) => Reason = reason;
    }

    /// <summary>
    /// Fired when a player joins the room.
    /// </summary>
    public struct PlayerJoinedEvent : IGameEvent
    {
        public int ActorNumber { get; set; }
        public string PlayerName { get; set; }
        public bool IsLocal { get; set; }
    }

    /// <summary>
    /// Fired when a player leaves the room.
    /// </summary>
    public struct PlayerLeftEvent : IGameEvent
    {
        public int ActorNumber { get; set; }
        public string PlayerName { get; set; }
    }

    /// <summary>
    /// Fired when player identities (colors/slots) are assigned.
    /// </summary>
    public struct PlayerIdentitiesAssignedEvent : IGameEvent
    {
        public int PlayerCount { get; set; }
    }

    /// <summary>
    /// Fired when a player's ready state changes.
    /// </summary>
    public struct PlayerReadyChangedEvent : IGameEvent
    {
        public int ActorNumber { get; set; }
        public bool IsReady { get; set; }
        public bool AllReady { get; set; }
    }

    // ============================================
    // GAME STATE EVENTS
    // ============================================

    /// <summary>
    /// Fired when the game phase changes.
    /// </summary>
    public struct GamePhaseChangedEvent : IGameEvent
    {
        public GamePhase NewPhase { get; }
        public GamePhase PreviousPhase { get; }

        public GamePhaseChangedEvent(GamePhase newPhase, GamePhase previousPhase)
        {
            NewPhase = newPhase;
            PreviousPhase = previousPhase;
        }
    }

    /// <summary>
    /// Fired when a new round starts.
    /// </summary>
    public struct RoundStartedEvent : IGameEvent
    {
        public int RoundNumber { get; }
        public bool IsClockwise { get; }

        public RoundStartedEvent(int roundNumber, bool isClockwise)
        {
            RoundNumber = roundNumber;
            IsClockwise = isClockwise;
        }
    }

    /// <summary>
    /// Fired when the game ends.
    /// </summary>
    public struct GameEndedEvent : IGameEvent
    {
        public Dictionary<int, int> PlayerScores { get; }

        public GameEndedEvent(Dictionary<int, int> playerScores)
        {
            PlayerScores = playerScores;
        }
    }

    // ============================================
    // DRAFT EVENTS
    // ============================================

    /// <summary>
    /// Fired when cards are dealt to the local player.
    /// Carries serialized CardData strings (e.g., "T:A1", "C:2").
    /// </summary>
    public struct CardsDealtEvent : IGameEvent
    {
        public string[] SerializedCards { get; }

        public CardsDealtEvent(string[] serializedCards)
        {
            SerializedCards = serializedCards;
        }
    }

    /// <summary>
    /// Fired when a card is selected (first click - highlight).
    /// </summary>
    public struct CardSelectedEvent : IGameEvent
    {
        public string Coordinate { get; }

        public CardSelectedEvent(string coordinate)
        {
            Coordinate = coordinate;
        }
    }

    /// <summary>
    /// Fired when a card is confirmed (second click - lock in).
    /// </summary>
    public struct CardConfirmedEvent : IGameEvent
    {
        public string Coordinate { get; }
        public int PlayerId { get; }

        public CardConfirmedEvent(string coordinate, int playerId)
        {
            Coordinate = coordinate;
            PlayerId = playerId;
        }
    }

    /// <summary>
    /// Fired when local player has submitted all picks.
    /// </summary>
    public struct LocalPicksSubmittedEvent : IGameEvent
    {
        public string[] Coordinates { get; }

        public LocalPicksSubmittedEvent(string[] coordinates)
        {
            Coordinates = coordinates;
        }
    }

    /// <summary>
    /// Fired when all players have submitted their picks.
    /// </summary>
    public struct AllPicksSubmittedEvent : IGameEvent
    {
        public Dictionary<int, string[]> PlayerPicks { get; }

        public AllPicksSubmittedEvent(Dictionary<int, string[]> playerPicks)
        {
            PlayerPicks = playerPicks;
        }
    }

    /// <summary>
    /// Fired when hands are passed to next player.
    /// </summary>
    public struct HandsPassedEvent : IGameEvent
    {
        public bool Clockwise { get; }

        public HandsPassedEvent(bool clockwise)
        {
            Clockwise = clockwise;
        }
    }

    // ============================================
    // IMP EVENTS
    // ============================================

    /// <summary>
    /// Fired when an imp should be spawned.
    /// </summary>
    public struct SpawnImpEvent : IGameEvent
    {
        public string Coordinate { get; }
        public int OwnerActorNumber { get; }
        public string Color { get; }

        public SpawnImpEvent(string coordinate, int ownerActorNumber, string color)
        {
            Coordinate = coordinate;
            OwnerActorNumber = ownerActorNumber;
            Color = color;
        }
    }

    /// <summary>
    /// Fired when an imp is spawned on the grid.
    /// </summary>
    public struct ImpSpawnedEvent : IGameEvent
    {
        public string Coordinate { get; }
        public int OwnerActorNumber { get; }

        public ImpSpawnedEvent(string coordinate, int ownerActorNumber)
        {
            Coordinate = coordinate;
            OwnerActorNumber = ownerActorNumber;
        }
    }

    // ============================================
    // RESOURCE EVENTS
    // ============================================

    /// <summary>
    /// Fired when a resource is collected.
    /// </summary>
    public struct ResourceCollectedEvent : IGameEvent
    {
        public string ResourceId { get; }
        public int Amount { get; }
        public int NewTotal { get; }

        public ResourceCollectedEvent(string resourceId, int amount, int newTotal)
        {
            ResourceId = resourceId;
            Amount = amount;
            NewTotal = newTotal;
        }
    }

    /// <summary>
    /// Fired when a resource amount changes.
    /// </summary>
    public struct ResourceChangedEvent : IGameEvent
    {
        public string ResourceId { get; }
        public int OldAmount { get; }
        public int NewAmount { get; }

        public ResourceChangedEvent(string resourceId, int oldAmount, int newAmount)
        {
            ResourceId = resourceId;
            OldAmount = oldAmount;
            NewAmount = newAmount;
        }
    }

    // ============================================
    // UI EVENTS
    // ============================================

    /// <summary>
    /// Fired to request HUD update.
    /// </summary>
    public struct UpdateHUDEvent : IGameEvent
    {
        public int Round { get; }
        public GamePhase Phase { get; }
        public string Status { get; }

        public UpdateHUDEvent(int round, GamePhase phase, string status)
        {
            Round = round;
            Phase = phase;
            Status = status;
        }
    }

    /// <summary>
    /// Fired to show a status message.
    /// </summary>
    public struct ShowStatusEvent : IGameEvent
    {
        public string Message { get; }
        public float Duration { get; }

        public ShowStatusEvent(string message, float duration = 0f)
        {
            Message = message;
            Duration = duration;
        }
    }

    // ============================================
    // GRID EVENTS
    // ============================================

    /// <summary>
    /// Fired when a tile is highlighted.
    /// </summary>
    public struct TileHighlightedEvent : IGameEvent
    {
        public string Coordinate { get; }
        public bool IsHighlighted { get; }

        public TileHighlightedEvent(string coordinate, bool isHighlighted)
        {
            Coordinate = coordinate;
            IsHighlighted = isHighlighted;
        }
    }

    /// <summary>
    /// Fired when the grid is regenerated.
    /// </summary>
    public struct GridRegeneratedEvent : IGameEvent { }

    // ============================================
    // OWNERSHIP EVENTS
    // ============================================

    /// <summary>
    /// Fired when a tile's owner changes.
    /// </summary>
    public struct TileOwnerChangedEvent : IGameEvent
    {
        public string Coordinate { get; }
        public int ActorNumber { get; }

        public TileOwnerChangedEvent(string coordinate, int actorNumber)
        {
            Coordinate = coordinate;
            ActorNumber = actorNumber;
        }
    }

    // ============================================
    // CONSTRUCTION EVENTS
    // ============================================

    /// <summary>
    /// Fired when a city is placed on a tile.
    /// </summary>
    public struct CityPlacedEvent : IGameEvent
    {
        public string Coordinate { get; }
        public int Strength { get; }
        public int ActorNumber { get; }

        public CityPlacedEvent(string coordinate, int strength, int actorNumber)
        {
            Coordinate = coordinate;
            Strength = strength;
            ActorNumber = actorNumber;
        }
    }

    /// <summary>
    /// Fired when the construction phase ends.
    /// </summary>
    public struct ConstructionPhaseEndedEvent : IGameEvent { }

    // ============================================
    // SCORING EVENTS
    // ============================================

    /// <summary>
    /// Fired when a fief is scored.
    /// </summary>
    public struct FiefScoredEvent : IGameEvent
    {
        public int ActorNumber { get; }
        public int FiefSize { get; }
        public int Strength { get; }
        public int Wealth { get; }
        public int Score { get; }

        public FiefScoredEvent(int actorNumber, int fiefSize, int strength, int wealth, int score)
        {
            ActorNumber = actorNumber;
            FiefSize = fiefSize;
            Strength = strength;
            Wealth = wealth;
            Score = score;
        }
    }
}
