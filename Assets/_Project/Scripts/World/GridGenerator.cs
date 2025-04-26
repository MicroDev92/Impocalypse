using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GridGenerator : MonoBehaviour
{
    public static GridGenerator Instance { get; private set; }
    
    public GameObject tilePrefab;
    public Sprite grassSprite;
    public Sprite waterSprite;
    
    [Header("Coordinate Sprites")]
    public Sprite[] letterSprites;     // A–J in order
    public Sprite[] numberSprites;     // 1–10 in order
    public Sprite emptyBorderSprite;
    
    public GameObject impPrefab;
    public List<TileData> allTiles = new(); // populated as tiles spawn

    
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
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
        var reversedLetters = letterSprites.Reverse().ToArray();
        allTiles.Clear(); // clear in case we're regenerating the board

        for (int x = 0; x < 12; x++)
        {
            for (int y = 0; y < 12; y++)
            {
                Vector3 position = new Vector3(x, y, 0);
                GameObject tile = Instantiate(tilePrefab, position, Quaternion.identity, transform);
                TileData tileData = tile.GetComponent<TileData>();

                // 🔹 Add the tile to the global list
                allTiles.Add(tileData);

                // 🔹 Coordinate label tiles
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
                    // 🔹 Playable tile (grass or water)
                    TileData.TileType tileType = Random.value < 0.5f ? TileData.TileType.Grass : TileData.TileType.Water;
                    Sprite tileSprite = tileType == TileData.TileType.Grass ? grassSprite : waterSprite;
                    string coord = $"{(char)('A' + (10 - (y - 1)) - 1)}{x}";
                    tileData.Init(tileType, coord, new Vector2Int(x, y), tileSprite);
                }
            }
        }
    }
    
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

}
