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
    [Tooltip("Optional second Ashlands look - plains pick randomly between the two for variety.")]
    public Sprite plainsVariantSprite;
    public Sprite waterSprite;
    
    [Header("Terrain Sprites")]
    public Sprite mountainSprite;

    [Header("Resource Sprites")]
    public Sprite woodSprite;
    public Sprite carrotSprite;
    public Sprite fishSprite;
    
    [Header("Coordinate Sprites (Board Frame source)")]
    [Tooltip("The runtime grid no longer draws the frame - these feed the static BoardFrame prefab. Regenerate it via Tools > Impocalypse > Generate Board Frame after changing them.")]
    public Sprite[] letterSprites;     // A–J in order
    public Sprite[] numberSprites;     // 1–10 in order
    public Sprite emptyBorderSprite;
    public Sprite coordTileBackground; // Stone background for coordinate tiles
    public Sprite cornerSkullSprite;   // Skull decoration for corners
    
    [Header("Prefabs")]
    public GameObject impPrefab;
    
    [Header("Grid Settings")]
    public Vector2Int gridDimensions = new Vector2Int(12, 12);

    [Tooltip("Visual fill of each cell. 1 = seamless edge-to-edge tiles; slightly below 1 (e.g. 0.97) leaves a deliberate hairline gap for a board-game look.")]
    [Range(0.9f, 1f)] public float tileVisualFill = 1f;

    [Tooltip("Randomly rotate full-bleed terrain tiles (plains/lava/forest/mountain) in 90-degree steps to hide texture repetition. Uses the seeded random stream, so all clients get identical rotations.")]
    public bool randomizeTerrainRotation = true;

    [Header("Terrain Generation")]
    [Tooltip("How strongly terrain clumps into regions (rivers, groves, ranges). 1 = pure random; ~3 = gentle regions; 8 = big continents. Seeded, so all clients generate identical maps.")]
    [Range(1f, 8f)] public float clusterStrength = 3f;

    // Base terrain distribution: Ashlands 40 / Styx 16 / Grove 16 / Field 16 / Peaks 12.
    private static readonly TileData.TileType[] TerrainTypes =
    {
        TileData.TileType.Plains,
        TileData.TileType.Water,
        TileData.TileType.Wood,
        TileData.TileType.Carrot,
        TileData.TileType.Mountain
    };
    private static readonly float[] TerrainBaseWeights = { 40f, 16f, 16f, 16f, 12f };

    // Fixed "tile bag": exact tile counts per terrain, derived from the weights.
    // Clustering decides WHERE terrain goes, never HOW MUCH - this prevents the
    // dominant terrain from snowballing via the neighbor boost.
    private int[] _terrainQuota;

    // Companion rule: Styx and Peaks may never appear as a lonely single tile.
    // When one is placed with no same-type neighbor, a future adjacent cell is
    // reserved for its partner. Grove/Field/Ashlands may be singletons.
    private readonly Dictionary<Vector2Int, TileData.TileType> _forcedTerrain = new();

    private void InitTerrainQuota()
    {
        int playable = (gridDimensions.x - 2) * (gridDimensions.y - 2);

        float sum = 0f;
        foreach (var w in TerrainBaseWeights) sum += w;

        _terrainQuota = new int[TerrainTypes.Length];
        int assigned = 0;
        for (int i = 0; i < TerrainTypes.Length; i++)
        {
            _terrainQuota[i] = Mathf.FloorToInt(TerrainBaseWeights[i] / sum * playable);
            assigned += _terrainQuota[i];
        }
        _terrainQuota[0] += playable - assigned; // rounding remainder goes to Ashlands
    }
    
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

        // Clear existing data
        _allTiles.Clear();
        _coordToTile.Clear();
        _positionToTile.Clear();

        // Fill the fixed tile bag for this board (see InitTerrainQuota).
        InitTerrainQuota();
        _forcedTerrain.Clear();
        
        // Clear existing child objects
        foreach (Transform child in transform)
        {
            DestroyImmediate(child.gameObject);
        }

        // Playable cells only (1..dim-2). The coordinate frame is the static
        // BoardFrame prefab in the scene (Tools > Impocalypse > Generate Board Frame) -
        // pure decoration has no business in the gameplay tile pipeline.
        for (int x = 1; x <= gridDimensions.x - 2; x++)
        {
            for (int y = 1; y <= gridDimensions.y - 2; y++)
            {
                Vector3 position = new Vector3(x, y, 0);
                GameObject tile = Instantiate(tilePrefab, position, Quaternion.identity, transform);
                TileData tileData = tile.GetComponent<TileData>();

                _allTiles.Add(tileData);

                // Weighted terrain roll with neighbor clustering bias, so terrain
                // forms soft regions (the Styx winds, groves clump, peaks range).
                // A cell reserved as a companion (see PickTerrain) skips the roll.
                TileData.TileType tileType;
                var cell = new Vector2Int(x, y);
                if (_forcedTerrain.TryGetValue(cell, out var forcedType))
                {
                    tileType = forcedType; // quota already reserved at force time
                    _forcedTerrain.Remove(cell);
                }
                else
                {
                    tileType = PickTerrain(x, y);
                }
                Sprite tileSprite = SpriteFor(tileType);

                string coord = $"{(char)('A' + (gridDimensions.y - 2 - (y - 1)) - 1)}{x}";
                tileData.Init(tileType, coord, new Vector2Int(x, y), tileSprite);

                _coordToTile[coord] = tileData;
                _positionToTile[new Vector2Int(x, y)] = tileData;

                // Normalize to exactly one grid cell so the board is seamless
                // regardless of sprite dimensions.
                tileData.FitToCell(tileVisualFill);

                // Break the wallpaper effect where identical full-bleed textures sit
                // adjacent. Uses the seeded random stream, so all clients match.
                if (randomizeTerrainRotation)
                    ApplyTerrainVariation(tileData);
            }
        }

        Debug.Log($"Grid generated with {_allTiles.Count} playable tiles");
    }
    
    /// <summary>
    /// Weighted terrain pick with clustering: each already-generated orthogonal
    /// neighbor (left, below - generation order guarantees they exist first)
    /// multiplies its own terrain's weight by clusterStrength. Terrain tends to
    /// continue itself without ever being guaranteed to.
    /// </summary>
    private TileData.TileType PickTerrain(int x, int y)
    {
        // Remaining quota is the base weight: a terrain that's used up its share
        // has weight 0, and a depleting terrain gets naturally rarer.
        var weights = new float[TerrainTypes.Length];
        for (int i = 0; i < weights.Length; i++)
            weights[i] = _terrainQuota[i];

        BoostForNeighbor(weights, new Vector2Int(x - 1, y));
        BoostForNeighbor(weights, new Vector2Int(x, y - 1));

        // Re-roll loop: a companion-required terrain that can't satisfy the rule
        // here gets its weight zeroed and the roll repeats with the rest.
        for (int attempt = 0; attempt < TerrainTypes.Length; attempt++)
        {
            float total = 0f;
            foreach (var w in weights) total += w;
            if (total <= 0f) break;

            int picked = -1;
            float roll = Random.Range(0f, total);
            for (int i = 0; i < weights.Length; i++)
            {
                roll -= weights[i];
                if (roll <= 0f && weights[i] > 0f) { picked = i; break; }
            }
            if (picked < 0) break;

            var type = TerrainTypes[picked];

            if (RequiresCompanion(type) && !HasSameNeighbor(x, y, type))
            {
                // Needs a partner: reserve a future adjacent cell for the same type.
                if (_terrainQuota[picked] >= 2 && TryReserveCompanion(x, y, type))
                {
                    _terrainQuota[picked] -= 2; // this tile + the reserved partner
                    return type;
                }

                weights[picked] = 0f; // rule can't be satisfied here - re-roll
                continue;
            }

            _terrainQuota[picked]--;
            return type;
        }

        // Fallback: prefer terrains without the companion rule, then anything left.
        for (int i = 0; i < TerrainTypes.Length; i++)
            if (_terrainQuota[i] > 0 && !RequiresCompanion(TerrainTypes[i]))
            { _terrainQuota[i]--; return TerrainTypes[i]; }
        for (int i = 0; i < TerrainTypes.Length; i++)
            if (_terrainQuota[i] > 0)
            { _terrainQuota[i]--; return TerrainTypes[i]; }
        return TileData.TileType.Plains;
    }

    /// <summary>Styx and Peaks may never be a lonely single tile.</summary>
    private static bool RequiresCompanion(TileData.TileType t) =>
        t == TileData.TileType.Water || t == TileData.TileType.Mountain;

    private bool HasSameNeighbor(int x, int y, TileData.TileType type)
    {
        return (_positionToTile.TryGetValue(new Vector2Int(x - 1, y), out var left) && left.type == type)
            || (_positionToTile.TryGetValue(new Vector2Int(x, y - 1), out var below) && below.type == type);
    }

    /// <summary>
    /// Reserves a not-yet-generated adjacent cell for the same terrain, guaranteeing
    /// the companion rule. Generation order is x-outer/y-inner, so (x, y+1) and
    /// (x+1, y) are always in the future.
    /// </summary>
    private bool TryReserveCompanion(int x, int y, TileData.TileType type)
    {
        var a = new Vector2Int(x, y + 1);
        var b = new Vector2Int(x + 1, y);
        if (Random.value < 0.5f) (a, b) = (b, a); // vary partner direction

        foreach (var cell in new[] { a, b })
        {
            if (IsPlayableCell(cell) && !_forcedTerrain.ContainsKey(cell))
            {
                _forcedTerrain[cell] = type;
                return true;
            }
        }
        return false;
    }

    private bool IsPlayableCell(Vector2Int p) =>
        p.x >= 1 && p.x <= gridDimensions.x - 2 &&
        p.y >= 1 && p.y <= gridDimensions.y - 2;

    private void BoostForNeighbor(float[] weights, Vector2Int pos)
    {
        if (!_positionToTile.TryGetValue(pos, out var neighbor)) return;

        for (int i = 0; i < TerrainTypes.Length; i++)
        {
            if (TerrainTypes[i] == neighbor.type)
            {
                weights[i] *= clusterStrength;
                return;
            }
        }
    }

    private Sprite SpriteFor(TileData.TileType type) => type switch
    {
        TileData.TileType.Water => waterSprite,
        TileData.TileType.Wood => woodSprite,
        TileData.TileType.Carrot => carrotSprite,
        TileData.TileType.Mountain => mountainSprite != null ? mountainSprite : grassSprite,
        // Ashlands: pick between the two plains looks for variety
        _ => (plainsVariantSprite != null && Random.value < 0.5f) ? plainsVariantSprite : grassSprite
    };

    /// <summary>
    /// Visual variety for full-bleed terrain. Non-directional textures (plains, lava)
    /// rotate in 90-degree steps; directional art (tree faces, mountain peaks) only
    /// mirrors so "up" stays up. Rimmed tiles (fish pond, fenced crops) stay untouched -
    /// they read as discrete objects on purpose.
    /// </summary>
    private static void ApplyTerrainVariation(TileData tile)
    {
        switch (tile.type)
        {
            case TileData.TileType.Plains:
            case TileData.TileType.Water:
                tile.transform.rotation = Quaternion.Euler(0f, 0f, 90f * Random.Range(0, 4));
                break;

            case TileData.TileType.Wood:
            case TileData.TileType.Mountain:
                if (Random.value < 0.5f)
                {
                    var s = tile.transform.localScale;
                    s.x = -s.x;
                    tile.transform.localScale = s;
                }
                break;
        }
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
        // Souls: the Styx (Water) is the soul-bearing terrain. Legacy Fish requests
        // map to it so existing callers (HUD counters, sidebar) keep working.
        if (resourceType == TileData.TileType.Fish || resourceType == TileData.TileType.Water)
            return _allTiles.Where(t => t.type == TileData.TileType.Water).ToList();

        if (resourceType != TileData.TileType.Wood &&
            resourceType != TileData.TileType.Carrot)
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
