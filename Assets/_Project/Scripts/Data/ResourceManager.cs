using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using _Project.Scripts.Utility;
using Core.Events;
using Scoring;

/// <summary>
/// Manages per-player resource tracking and fief-based scoring.
/// Resources are tile properties — scoring uses FiefCalculator (Strength x Wealth).
/// Legacy resource collection from ImpSpawnedEvent is kept for backward compatibility.
/// </summary>
public class ResourceManager : MonoBehaviour, IResourceService
{
    [Header("Resource Icons")]
    [SerializeField] private Sprite woodIcon;
    [SerializeField] private Sprite carrotIcon;
    [SerializeField] private Sprite fishIcon;

    // Per-player resources (legacy tracking, kept for resource display)
    private readonly Dictionary<int, Dictionary<TileData.TileType, int>> _playerResources = new();
    private readonly Dictionary<TileData.TileType, string> _resourceNames = new();
    private readonly Dictionary<TileData.TileType, Sprite> _resourceIcons = new();

    private IEventBus _eventBus;
    private IGridService _gridService;

    public event Action<int, TileData.TileType, int> OnResourceChanged;
    public event Action<int, TileData.TileType, int> OnResourceCollected;

    private void Awake()
    {
        ServiceLocator.RegisterService<IResourceService>(this);
        InitializeMetadata();
    }

    private void Start()
    {
        ServiceLocator.TryGetService(out _eventBus);
        ServiceLocator.TryGetService(out _gridService);

        _eventBus?.Subscribe<ImpSpawnedEvent>(OnImpSpawned);
    }

    private void OnDestroy()
    {
        _eventBus?.Unsubscribe<ImpSpawnedEvent>(OnImpSpawned);
        ServiceLocator.UnregisterService<IResourceService>();
    }

    private void InitializeMetadata()
    {
        _resourceNames[TileData.TileType.Wood] = "Wood";
        _resourceNames[TileData.TileType.Carrot] = "Carrot";
        _resourceNames[TileData.TileType.Fish] = "Fish";

        _resourceIcons[TileData.TileType.Wood] = woodIcon;
        _resourceIcons[TileData.TileType.Carrot] = carrotIcon;
        _resourceIcons[TileData.TileType.Fish] = fishIcon;
    }

    private void OnImpSpawned(ImpSpawnedEvent evt)
    {
        var tile = _gridService?.GetTileByCoord(evt.Coordinate);
        if (tile == null) return;

        if (IsResourceTile(tile.type))
        {
            CollectResource(tile, evt.OwnerActorNumber);
        }
    }

    public bool CollectResource(TileData tile, int actorNumber)
    {
        if (tile == null || !IsResourceTile(tile.type))
            return false;

        EnsurePlayerExists(actorNumber);

        var resources = _playerResources[actorNumber];
        resources[tile.type]++;
        int newAmount = resources[tile.type];

        OnResourceCollected?.Invoke(actorNumber, tile.type, newAmount);
        OnResourceChanged?.Invoke(actorNumber, tile.type, newAmount);

        string resourceId = tile.type.ToString().ToLowerInvariant();
        _eventBus?.Publish(new ResourceCollectedEvent(resourceId, 1, newAmount));

        return true;
    }

    public bool IsResourceTile(TileData.TileType tileType)
    {
        return tileType == TileData.TileType.Wood ||
               tileType == TileData.TileType.Carrot ||
               tileType == TileData.TileType.Fish;
    }

    public int GetResourceAmount(int actorNumber, TileData.TileType resourceType)
    {
        if (_playerResources.TryGetValue(actorNumber, out var resources))
            return resources.TryGetValue(resourceType, out int amount) ? amount : 0;
        return 0;
    }

    public string GetResourceName(TileData.TileType resourceType)
    {
        return _resourceNames.TryGetValue(resourceType, out string name) ? name : "Unknown";
    }

    public Sprite GetResourceIcon(TileData.TileType resourceType)
    {
        return _resourceIcons.TryGetValue(resourceType, out Sprite icon) ? icon : null;
    }

    public Dictionary<TileData.TileType, int> GetPlayerResources(int actorNumber)
    {
        if (_playerResources.TryGetValue(actorNumber, out var resources))
            return new Dictionary<TileData.TileType, int>(resources);

        return new Dictionary<TileData.TileType, int>
        {
            { TileData.TileType.Wood, 0 },
            { TileData.TileType.Carrot, 0 },
            { TileData.TileType.Fish, 0 }
        };
    }

    /// <summary>
    /// Gets player score using fief-based calculation (Strength x Wealth).
    /// Falls back to legacy resource sum if grid service unavailable.
    /// </summary>
    public int GetPlayerScore(int actorNumber)
    {
        if (_gridService != null)
        {
            return FiefCalculator.CalculateTotalScore(actorNumber, _gridService);
        }

        // Fallback: legacy resource sum
        if (_playerResources.TryGetValue(actorNumber, out var resources))
            return resources.Values.Sum();
        return 0;
    }

    public Dictionary<int, int> GetAllPlayerScores()
    {
        var scores = new Dictionary<int, int>();

        if (_gridService != null)
        {
            // Get all player IDs from owned tiles
            var networkService = ServiceLocator.GetService<INetworkService>();
            if (networkService != null)
            {
                foreach (var playerId in networkService.GetPlayerIds())
                {
                    scores[playerId] = FiefCalculator.CalculateTotalScore(playerId, _gridService);
                }
            }
        }

        // Fallback / supplement with legacy data
        if (scores.Count == 0)
        {
            foreach (var kvp in _playerResources)
            {
                scores[kvp.Key] = kvp.Value.Values.Sum();
            }
        }

        return scores;
    }

    public List<Fief> GetPlayerFiefs(int actorNumber)
    {
        if (_gridService != null)
        {
            return FiefCalculator.CalculateFiefs(actorNumber, _gridService);
        }
        return new List<Fief>();
    }

    public void ResetAll()
    {
        _playerResources.Clear();
        Debug.Log("[ResourceManager] All resources reset");
    }

    private void EnsurePlayerExists(int actorNumber)
    {
        if (!_playerResources.ContainsKey(actorNumber))
        {
            _playerResources[actorNumber] = new Dictionary<TileData.TileType, int>
            {
                { TileData.TileType.Wood, 0 },
                { TileData.TileType.Carrot, 0 },
                { TileData.TileType.Fish, 0 }
            };
        }
    }
}
