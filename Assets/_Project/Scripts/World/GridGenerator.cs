using System.Collections.Generic;
using System.Linq;
using _Project.Scripts.Utility;
using UnityEngine;

/// <summary>
/// Grid management service that handles tile generation and coordinate management.
/// Implements IGridService for loose coupling and better testability.
/// </summary>
public class GridGenerator : MonoBehaviour, IGridService
{
    [Header("Grid Configuration")]
    public GameObject tilePrefab;
    public Sprite grassSprite;
    public Sprite waterSprite;
    
    [Header("Terrain Sprites")]
    public Sprite mountainSprite;

    [Header("Resource Sprites")]
    public Sprite woodSprite;
    public Sprite carrotSprite;
    public Sprite fishSprite;
    
    [Header("Coordinate Sprites")]
    public Sprite[] letterSprites;     // A–J in order
    public Sprite[] numberSprites;     // 1–10 in order
    public Sprite emptyBorderSprite;
    public Sprite coordTileBackground; // Stone background for coordinate tiles
    public Sprite cornerSkullSprite;   // Skull decoration for corners
    
    [Header("Prefabs")]
    public GameObject impPrefab;
    
    [Header("Grid Settings")]
    public Vector2Int gridDimensions = new Vector2Int(12, 12);
    
    // Private collections for optimized lookups
    private readonly Dictionary<string, TileData> _coordToTile = new();
    private readonly Dictionary<Vector2Int, TileData> _positionToTile = new();
    private readonly List<TileData> _allTiles = new();
    
    // Properties for IGridService interface
    public List<TileData> GetAllTiles() => new List<TileData>(_allTiles);
    
    private void Awake()
    {
        // Register this service with the ServiceLocator
        ServiceLocator.RegisterService<IGridService>(this);
    }
    
    private void OnDestroy()
    {
        // Unregister service when destroyed
        ServiceLocator.UnregisterService<IGridService>();
    }


    void Start()
    {
        GenerateGrid();
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;

        // Draw 12x12 grid tile bounds
        for (int x = 0; x < 12; x++)
        {
            for (int y = 0; y < 12; y++)
            {
                Vector3 pos = new Vector3(x, y, 0);
                Gizmos.DrawWireCube(pos, Vector3.one);
            }
        }
    }


    void GenerateGrid()
    {
        // CRITICAL for multiplayer: terrain is rolled with Random, so all clients must
        // seed identically or every player generates a different world. The host writes
        // a shared seed into the room before loading the scene.
        var network = ServiceLocator.GetService<INetworkService>();
        object seedProp = network?.GetRoomProperty("mapSeed");
        int seed = seedProp is int s ? s : UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        UnityEngine.Random.InitState(seed);
        Debug.Log($"[GridGenerator] Generating grid with seed {seed} (shared: {seedProp != null})");

        var reversedLetters = letterSprites.Reverse().ToArray();
        
        // Clear existing data
        _allTiles.Clear();
        _coordToTile.Clear();
        _positionToTile.Clear();
        
        // Clear existing child objects
        foreach (Transform child in transform)
        {
            DestroyImmediate(child.gameObject);
        }

        for (int x = 0; x < gridDimensions.x; x++)
        {
            for (int y = 0; y < gridDimensions.y; y++)
            {
                Vector3 position = new Vector3(x, y, 0);
                GameObject tile = Instantiate(tilePrefab, position, Quaternion.identity, transform);
                TileData tileData = tile.GetComponent<TileData>();

                // Add the tile to collections
                _allTiles.Add(tileData);

                // Coordinate label tiles
                if ((x == 0 || x == gridDimensions.x - 1) && (y >= 1 && y <= gridDimensions.y - 2))
                {
                    Sprite letter = reversedLetters[y - 1];
                    Sprite background = coordTileBackground != null ? coordTileBackground : letter;
                    tileData.InitWithLabel(TileData.TileType.Coordinate, $"Letter_{y}", new Vector2Int(x, y), background, letter);
                }
                else if ((y == 0 || y == gridDimensions.y - 1) && (x >= 1 && x <= gridDimensions.x - 2))
                {
                    Sprite number = numberSprites[x - 1];
                    Sprite background = coordTileBackground != null ? coordTileBackground : number;
                    tileData.InitWithLabel(TileData.TileType.Coordinate, $"Number_{x}", new Vector2Int(x, y), background, number);
                }
                else if ((x == 0 || x == gridDimensions.x - 1) && (y == 0 || y == gridDimensions.y - 1))
                {
                    Sprite background = coordTileBackground != null ? coordTileBackground : emptyBorderSprite;
                    Sprite skull = cornerSkullSprite;
                    tileData.InitWithLabel(TileData.TileType.EmptyCorner, "Corner", new Vector2Int(x, y), background, skull);
                }
                else
                {
                    // Playable tile terrain distribution:
                    // Plains 30%, Water 20%, Wood 15%, Carrot 15%, Fish 10%, Mountain 10%
                    float randomValue = Random.value;
                    TileData.TileType tileType;
                    Sprite tileSprite;

                    if (randomValue < 0.30f)
                    {
                        tileType = TileData.TileType.Plains;
                        tileSprite = grassSprite;
                    }
                    else if (randomValue < 0.50f)
                    {
                        tileType = TileData.TileType.Water;
                        tileSprite = waterSprite;
                    }
                    else if (randomValue < 0.65f)
                    {
                        tileType = TileData.TileType.Wood;
                        tileSprite = woodSprite;
                    }
                    else if (randomValue < 0.80f)
                    {
                        tileType = TileData.TileType.Carrot;
                        tileSprite = carrotSprite;
                    }
                    else if (randomValue < 0.90f)
                    {
                        tileType = TileData.TileType.Fish;
                        tileSprite = fishSprite;
                    }
                    else
                    {
                        tileType = TileData.TileType.Mountain;
                        tileSprite = mountainSprite != null ? mountainSprite : grassSprite;
                    }
                    
                    string coord = $"{(char)('A' + (gridDimensions.y - 2 - (y - 1)) - 1)}{x}";
                    tileData.Init(tileType, coord, new Vector2Int(x, y), tileSprite);
                    
                    // Add to lookup dictionaries
                    _coordToTile[coord] = tileData;
                    _positionToTile[new Vector2Int(x, y)] = tileData;
                }
            }
        }
        
        Debug.Log($"Grid generated with {_allTiles.Count} tiles, {_coordToTile.Count} playable tiles");
    }
    
    // IGridService Implementation
    public TileData GetTileByCoord(string coord)
    {
        return _coordToTile.TryGetValue(coord, out var tile) ? tile : null;
    }
    
    public TileData GetTileByPosition(Vector2Int position)
    {
        return _positionToTile.TryGetValue(position, out var tile) ? tile : null;
    }
    
    public string[] GetRandomCoords(int count)
    {
        return _coordToTile.Values
            .Where(t => IsPlayableTileType(t.type))
            .OrderBy(x => Random.value)
            .Take(count)
            .Select(t => t.coord)
            .ToArray();
    }

    /// <summary>
    /// Gets all playable tile coordinates (for deck building).
    /// </summary>
    public string[] GetAllPlayableCoords()
    {
        return _coordToTile.Values
            .Where(t => IsPlayableTileType(t.type))
            .Select(t => t.coord)
            .ToArray();
    }

    private static bool IsPlayableTileType(TileData.TileType tileType)
    {
        return tileType == TileData.TileType.Plains ||
               tileType == TileData.TileType.Water ||
               tileType == TileData.TileType.Wood ||
               tileType == TileData.TileType.Carrot ||
               tileType == TileData.TileType.Fish ||
               tileType == TileData.TileType.Mountain ||
               tileType == TileData.TileType.Grass;
    }
    
    public List<TileData> GetTilesByType(TileData.TileType tileType)
    {
        return _allTiles.Where(t => t.type == tileType).ToList();
    }
    
    public List<TileData> GetResourceTiles()
    {
        return _allTiles.Where(t => t.IsResourceTile).ToList();
    }
    
    public List<TileData> GetTilesByResourceType(TileData.TileType resourceType)
    {
        if (resourceType != TileData.TileType.Wood && 
            resourceType != TileData.TileType.Carrot && 
            resourceType != TileData.TileType.Fish)
        {
            Debug.LogWarning($"Requested resource type {resourceType} is not a valid resource");
            return new List<TileData>();
        }
        
        return _allTiles.Where(t => t.type == resourceType).ToList();
    }
    
    public List<TileData> GetTilesInRadius(Vector2Int center, float radius)
    {
        var result = new List<TileData>();
        int radiusInt = Mathf.CeilToInt(radius);
        
        for (int x = center.x - radiusInt; x <= center.x + radiusInt; x++)
        {
            for (int y = center.y - radiusInt; y <= center.y + radiusInt; y++)
            {
                var pos = new Vector2Int(x, y);
                if (Vector2Int.Distance(center, pos) <= radius)
                {
                    var tile = GetTileByPosition(pos);
                    if (tile != null)
                    {
                        result.Add(tile);
                    }
                }
            }
        }
        
        return result;
    }
    
    public bool IsValidCoordinate(string coord)
    {
        return _coordToTile.ContainsKey(coord);
    }
    
    public Vector2Int GetGridDimensions()
    {
        return gridDimensions;
    }
    
    public void RegenerateGrid()
    {
        GenerateGrid();
    }

    [ContextMenu("Generate Grid Editor")]
    private void GenerateGridEditor()
    {
        GenerateGrid();
    }

    public List<TileData> GetOrthogonalNeighbors(string coord)
    {
        var result = new List<TileData>();
        var tile = GetTileByCoord(coord);
        if (tile == null) return result;

        var pos = tile.gridPosition;
        Vector2Int[] offsets = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        foreach (var offset in offsets)
        {
            var neighborPos = pos + offset;
            // Skip border tiles (row/col 0 and 11)
            if (neighborPos.x < 1 || neighborPos.x > gridDimensions.x - 2 ||
                neighborPos.y < 1 || neighborPos.y > gridDimensions.y - 2)
                continue;

            var neighbor = GetTileByPosition(neighborPos);
            if (neighbor != null)
            {
                result.Add(neighbor);
            }
        }

        return result;
    }

    public List<TileData> GetOwnedTiles(int actorNumber)
    {
        return _coordToTile.Values
            .Where(t => t.IsOwnedBy(actorNumber))
            .ToList();
    }
}
