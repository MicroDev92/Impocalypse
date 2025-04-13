using UnityEngine;

public class TileData : MonoBehaviour
{
    public enum TileType { Grass, Water }

    public TileType type;
    public string coord;
    public Vector2Int gridPosition;

    public void Init(TileType tileType, string coordLabel, Vector2Int position, Sprite sprite)
    {
        type = tileType;
        coord = coordLabel;
        gridPosition = position;
        GetComponent<SpriteRenderer>().sprite = sprite;
        gameObject.name = $"Tile_{coord}_{type}";
    }
}
