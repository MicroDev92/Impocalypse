using UnityEngine;

public class GridGenerator : MonoBehaviour
{
    public GameObject tilePrefab;
    public Sprite grassSprite;
    public Sprite waterSprite;

    private char[] rowLabels = "ABCDEFGHIJ".ToCharArray();
    private const int width = 10;
    private const int height = 10;
    private const float spacing = 1f;

    void Start()
    {
        GenerateGrid();
    }

    void GenerateGrid()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var pos = new Vector3(x * spacing, y * spacing, 0f);
                var tile = Instantiate(tilePrefab, pos, Quaternion.identity, transform);

                var tileType = Random.value < 0.5f ? TileData.TileType.Grass : TileData.TileType.Water;
                var chosenSprite = tileType == TileData.TileType.Grass ? grassSprite : waterSprite;
                var coord = $"{rowLabels[y]}{x + 1}";

                tile.GetComponent<TileData>().Init(tileType, coord, new Vector2Int(x, y), chosenSprite);
            }
        }
    }
}
