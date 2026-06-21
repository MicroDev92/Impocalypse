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
/// Left sidebar panel showing current turn info and player score cards.
/// Each player card displays: name, score, territory count, and per-resource counts.
///
/// Hierarchy expected (created by UISetupMenu):
///   PlayerSidebar
///   ├── CurrentTurnPanel
///   │   ├── TurnLabel         ("Current Turn")
///   │   ├── PlayerIcon        (Image)
///   │   ├── PlayerNameText    ("Player Name")
///   │   └── TurnStatusText    ("Your turn!" / "Waiting...")
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
    [Header("Current Turn")]
    [SerializeField] private GameObject currentTurnPanel;
    [SerializeField] private Image turnPlayerIcon;
    [SerializeField] private TMP_Text turnPlayerName;
    [SerializeField] private TMP_Text turnStatusText;

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
        UpdateCurrentTurn();
        RefreshAllStats();
    }

    private void OnRoundStarted(RoundStartedEvent evt)
    {
        UpdateCurrentTurn();
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

        UpdateCurrentTurn();
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

        cardUI.Initialize(player, playerColor, theme);
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

    // ==========================================
    // CURRENT TURN
    // ==========================================

    private void UpdateCurrentTurn()
    {
        if (currentTurnPanel == null) return;

        var playerService = ServiceLocator.GetService<IPlayerService>();
        var gameManager = ServiceLocator.GetService<IGameManager>();
        var localPlayer = playerService?.GetLocalPlayer();

        if (localPlayer == null)
        {
            currentTurnPanel.SetActive(false);
            return;
        }

        currentTurnPanel.SetActive(true);

        if (turnPlayerName != null)
            turnPlayerName.text = localPlayer.Nickname;

        if (turnStatusText != null)
        {
            var phase = gameManager?.CurrentPhase ?? GamePhase.Lobby;
            turnStatusText.text = phase switch
            {
                GamePhase.Draft => "Pick your cards",
                GamePhase.Build => "Place your cities",
                GamePhase.Scoring => "Scoring...",
                GamePhase.GameOver => "Game Over",
                _ => "Waiting..."
            };
        }

        // Tint the icon with player color
        if (turnPlayerIcon != null && colorPalette != null && !string.IsNullOrEmpty(localPlayer.ImpColor))
        {
            turnPlayerIcon.color = colorPalette.GetPrimaryColor(localPlayer.ImpColor);
        }
    }
}
