using UnityEngine;

public class TileData : MonoBehaviour
{
    public enum TileType
    {
        Grass,
        Water,
        Wood,
        Fish,
        Farm,
        PresetVillage,
        Coordinate,
        EmptyCorner
    }

    public TileType type;
    public string coord;
    
    [Tooltip("Grid position in tile matrix (0–11 world grid)")]
    public Vector2Int gridPosition;

    public void Init(TileType tileType, string coordLabel, Vector2Int position, Sprite sprite)
    {
        type = tileType;
        coord = coordLabel;
        gridPosition = position;
        GetComponent<SpriteRenderer>().sprite = sprite;
        gameObject.name = $"Tile_{coord}_{type}";
    }
    
    public void Highlight(bool on)
    {
        var sr = GetComponent<SpriteRenderer>();
        sr.color = on ? Color.yellow : Color.white;
    }
}
