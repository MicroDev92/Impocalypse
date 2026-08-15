using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using _Project.Scripts.Utility;
using Config;
using Core;
using Core.Events;
using Players;

/// <summary>
/// Left sidebar panel showing the player score cards.
/// Each player card displays: name, score, territory count, and per-resource counts.
/// The local player's card is suffixed "(you)".
///
/// There used to be a CurrentTurnPanel above the list. It was removed: it read
/// "Current Turn" but always showed the LOCAL player, because a simultaneous draft
/// has no turns to indicate. Its instruction line duplicated PickPromptUI and the
/// phase banner, and its only unique job - telling you which player you were - now
/// belongs to the list itself.
///
/// Hierarchy expected (created by UISetupMenu):
///   PlayerSidebar
///   └── PlayerListContainer   (VerticalLayoutGroup)
///       └── [PlayerCard prefabs instantiated at runtime]
///
/// PlayerCard hierarchy (created per player):
///   PlayerCard
///   ├── LeftBorder            (Image, colored by player)
///   ├── Content
///   │   ├── HeaderRow
///   │   │   ├── PlayerName    (TMP_Text)
///   │   │   └── ScoreText     (TMP_Text)
///   │   └── StatsRow
///   │       ├── TerritoryStat (icon + count)
///   │       ├── WoodStat      (icon + count)
///   │       ├── CarrotStat    (icon + count)
///   │       └── FishStat      (icon + count)
/// </summary>
public class PlayerSidebar : MonoBehaviour
{
    [Header("Player List")]
    [SerializeField] private Transform playerListContainer;
    [SerializeField] private GameObject playerCardPrefab;

    [Header("Configuration")]
    [SerializeField] private GameTheme theme;
    [SerializeField] private PlayerColorPalette colorPalette;

    private IEventBus _eventBus;
    private readonly Dictionary<int, PlayerCardUI> _playerCards = new();

    private void Start()
    {
        if (ServiceLocator.TryGetService<IEventBus>(out _eventBus))
        {
            _eventBus.Subscribe<PlayerIdentitiesAssignedEvent>(OnIdentitiesAssigned);
            _eventBus.Subscribe<GamePhaseChangedEvent>(OnPhaseChanged);
            _eventBus.Subscribe<RoundStartedEvent>(OnRoundStarted);
            _eventBus.Subscribe<ImpSpawnedEvent>(OnImpSpawned);
            _eventBus.Subscribe<ResourceCollectedEvent>(OnResourceCollected);
            _eventBus.Subscribe<CityPlacedEvent>(OnCityPlaced);
        }

        // Delay initial build to let services register
        Invoke(nameof(RebuildPlayerList), 0.1f);
    }

    private void OnDestroy()
    {
        if (_eventBus != null)
        {
            _eventBus.Unsubscribe<PlayerIdentitiesAssignedEvent>(OnIdentitiesAssigned);
            _eventBus.Unsubscribe<GamePhaseChangedEvent>(OnPhaseChanged);
            _eventBus.Unsubscribe<RoundStartedEvent>(OnRoundStarted);
            _eventBus.Unsubscribe<ImpSpawnedEvent>(OnImpSpawned);
            _eventBus.Unsubscribe<ResourceCollectedEvent>(OnResourceCollected);
            _eventBus.Unsubscribe<CityPlacedEvent>(OnCityPlaced);
        }
    }

    private void OnIdentitiesAssigned(PlayerIdentitiesAssignedEvent evt)
    {
        RebuildPlayerList();
    }

    private void OnPhaseChanged(GamePhaseChangedEvent evt)
    {
        RefreshAllStats();
    }

    /// <summary>
    /// Nothing to do per round any more now the turn panel is gone; the stats refresh
    /// on their own events. Kept subscribed so a round-scoped need has a home.
    /// </summary>
    private void OnRoundStarted(RoundStartedEvent evt)
    {
    }

    private void OnImpSpawned(ImpSpawnedEvent evt)
    {
        // Territory count changed — refresh the affected player
        RefreshPlayerStats(evt.OwnerActorNumber);
    }

    private void OnResourceCollected(ResourceCollectedEvent evt)
    {
        // Resource counts changed — refresh all (event doesn't carry player ID)
        RefreshAllStats();
    }

    private void OnCityPlaced(CityPlacedEvent evt)
    {
        RefreshPlayerStats(evt.ActorNumber);
    }

    // ==========================================
    // BUILD PLAYER LIST
    // ==========================================

    private void RebuildPlayerList()
    {
        ClearPlayerCards();

        var playerService = ServiceLocator.GetService<IPlayerService>();
        if (playerService == null) return;

        var players = playerService.GetPlayersInSlotOrder();
        foreach (var player in players)
        {
            CreatePlayerCard(player);
        }
    }

    private void CreatePlayerCard(PlayerData player)
    {
        if (playerListContainer == null || playerCardPrefab == null) return;

        var cardObj = Instantiate(playerCardPrefab, playerListContainer);
        var cardUI = cardObj.GetComponent<PlayerCardUI>();

        if (cardUI == null)
        {
            Debug.LogWarning("[PlayerSidebar] PlayerCard prefab missing PlayerCardUI component");
            Destroy(cardObj);
            return;
        }

        Color playerColor = Color.white;
        if (colorPalette != null && !string.IsNullOrEmpty(player.ImpColor))
        {
            playerColor = colorPalette.GetPrimaryColor(player.ImpColor);
        }

        var network = ServiceLocator.GetService<INetworkService>();
        bool isLocal = network != null && player.ActorNumber == network.LocalPlayerId;

        cardUI.Initialize(player, playerColor, theme, isLocal);
        _playerCards[player.ActorNumber] = cardUI;

        RefreshPlayerStats(player.ActorNumber);
    }

    private void ClearPlayerCards()
    {
        foreach (var kvp in _playerCards)
        {
            if (kvp.Value != null)
                Destroy(kvp.Value.gameObject);
        }
        _playerCards.Clear();
    }

    // ==========================================
    // UPDATE STATS
    // ==========================================

    private void RefreshAllStats()
    {
        foreach (var kvp in _playerCards)
        {
            RefreshPlayerStats(kvp.Key);
        }
    }

    private void RefreshPlayerStats(int actorNumber)
    {
        if (!_playerCards.TryGetValue(actorNumber, out var cardUI)) return;

        var gridService = ServiceLocator.GetService<IGridService>();
        var resourceService = ServiceLocator.GetService<IResourceService>();
        var gameManager = ServiceLocator.GetService<IGameManager>();

        int territories = gridService?.GetOwnedTiles(actorNumber)?.Count ?? 0;

        int wood = resourceService?.GetResourceAmount(actorNumber, TileData.TileType.Wood) ?? 0;
        int carrot = resourceService?.GetResourceAmount(actorNumber, TileData.TileType.Carrot) ?? 0;
        int fish = resourceService?.GetResourceAmount(actorNumber, TileData.TileType.Fish) ?? 0;

        int score = 0;
        var cumulativeScores = gameManager?.GetCumulativeScores();
        if (cumulativeScores != null && cumulativeScores.TryGetValue(actorNumber, out int cs))
            score = cs;
        else if (resourceService != null)
            score = resourceService.GetPlayerScore(actorNumber);

        cardUI.UpdateStats(score, territories, wood, carrot, fish);
    }

}
