using UnityEngine;

public class GridGenerator : MonoBehaviour
{
    public GameObject tilePrefab;
    public Sprite grassSprite;
    public Sprite waterSprite;
    public GameObject labelPrefab;
    public float labelOffset = 0.5f;


    private char[] rowLabels = "ABCDEFGHIJ".ToCharArray();
    private const int width = 10;
    private const int height = 10;
    private const float spacing = 1f;

    void Start()
    {
        GenerateGrid();
        CreateCoordinateLabels();
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
    
    private void CreateCoordinateLabels()
    {
        char[] rowLabels = "JIHGFEDCBA".ToCharArray();
        float margin = 0.5f; // distance away from edge of board

        for (int x = 0; x < width; x++)
        {
            string colLabel = (x + 1).ToString();

            // X position is the center of the tile: x
            // Y positions are just above and below the grid without offset
            Vector3 topPos = new Vector3(x, height + margin, 0);
            Vector3 bottomPos = new Vector3(x, -1f - margin, 0);

            CreateLabel(colLabel, topPos);
            CreateLabel(colLabel, bottomPos);
        }

        for (int y = 0; y < height; y++)
        {
            string rowLabel = rowLabels[y].ToString();

            // Y is center of row tile
            // X is just to the left and right of grid
            Vector3 leftPos = new Vector3(-1f - margin, y, 0);
            Vector3 rightPos = new Vector3(width + margin, y, 0);

            CreateLabel(rowLabel, leftPos);
            CreateLabel(rowLabel, rightPos);
        }
    }



    private void CreateLabel(string text, Vector3 position)
    {
        GameObject labelObj = Instantiate(labelPrefab, position, Quaternion.identity, transform);
        var label = labelObj.GetComponent<TMPro.TextMeshPro>();

        label.text = text;
        label.alignment = TMPro.TextAlignmentOptions.Center;
        label.fontSize = 1.5f;
    }



}
