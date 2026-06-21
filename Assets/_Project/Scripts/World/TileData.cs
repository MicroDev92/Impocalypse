using Cards;
using UnityEngine;

public class TileData : MonoBehaviour
{
    public enum TileType
    {
        Grass,
        Water,
        Wood,
        Carrot,
        Fish,
        Farm,
        PresetVillage,
        Coordinate,
        EmptyCorner,
        Plains,
        Mountain
    }

    public TileType type;
    public string coord;

    [Tooltip("Grid position in tile matrix (0–11 world grid)")]
    public Vector2Int gridPosition;

    [Header("Sprite References")]
    [SerializeField] private SpriteRenderer backgroundRenderer;
    [SerializeField] private SpriteRenderer labelRenderer;

    // Ownership
    private int _ownerActorNumber = -1;
    private Color _originalColor = Color.white;

    // Building
    private CityStrength _buildingStrength = CityStrength.None;

    // Properties
    public int OwnerActorNumber => _ownerActorNumber;
    public bool IsOwned => _ownerActorNumber >= 0;
    public bool HasBuilding => _buildingStrength != CityStrength.None;
    public CityStrength BuildingStrength => _buildingStrength;

    public bool IsOwnedBy(int actorNumber) => _ownerActorNumber == actorNumber;

    /// <summary>
    /// Whether a city of the given strength can be placed on this tile.
    /// Str3 requires Mountain terrain. Tile must be owned and have no existing building.
    /// </summary>
    public bool CanPlaceCity(CityStrength strength)
    {
        if (!IsOwned || HasBuilding) return false;
        if (strength == CityStrength.Strength3 && type != TileType.Mountain) return false;
        return true;
    }

    /// <summary>
    /// Whether this tile type produces a resource for fief wealth calculation.
    /// </summary>
    public bool IsResourceTile =>
        type == TileType.Wood || type == TileType.Carrot || type == TileType.Fish;

    public void Init(TileType tileType, string coordLabel, Vector2Int position, Sprite sprite)
    {
        type = tileType;
        coord = coordLabel;
        gridPosition = position;

        var sr = backgroundRenderer != null ? backgroundRenderer : GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sprite = sprite;
            _originalColor = sr.color;
        }

        gameObject.name = $"Tile_{coord}_{type}";
    }

    public void InitWithLabel(TileType tileType, string coordLabel, Vector2Int position, Sprite backgroundSprite, Sprite labelSprite)
    {
        type = tileType;
        coord = coordLabel;
        gridPosition = position;

        if (backgroundRenderer != null)
        {
            backgroundRenderer.sprite = backgroundSprite;
        }
        else
        {
            GetComponent<SpriteRenderer>().sprite = backgroundSprite;
        }

        if (labelRenderer != null)
        {
            labelRenderer.sprite = labelSprite;
        }

        gameObject.name = $"Tile_{coord}_{type}";
    }

    public void Highlight(bool on)
    {
        var sr = backgroundRenderer != null ? backgroundRenderer : GetComponent<SpriteRenderer>();
        sr.color = on ? Color.yellow : (IsOwned ? GetOwnerTintColor() : _originalColor);
    }

    /// <summary>
    /// Sets the owner of this tile and applies a visual tint.
    /// </summary>
    public void SetOwner(int actorNumber)
    {
        _ownerActorNumber = actorNumber;
        ApplyOwnerTint();
    }

    /// <summary>
    /// Places a city building on this tile.
    /// </summary>
    public void PlaceCity(CityStrength strength)
    {
        _buildingStrength = strength;
        // Visual indicator will be handled by the construction system
    }

    /// <summary>
    /// Resets ownership and building state.
    /// </summary>
    public void ResetOwnership()
    {
        _ownerActorNumber = -1;
        _buildingStrength = CityStrength.None;

        var sr = backgroundRenderer != null ? backgroundRenderer : GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = _originalColor;
    }

    private void ApplyOwnerTint()
    {
        var sr = backgroundRenderer != null ? backgroundRenderer : GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = GetOwnerTintColor();
        }
    }

    private Color GetOwnerTintColor()
    {
        // Tint based on player actor number with semi-transparent overlay
        Color baseColor = _originalColor;
        Color playerColor = GetPlayerColor(_ownerActorNumber);
        return Color.Lerp(baseColor, playerColor, 0.35f);
    }

    private static Color GetPlayerColor(int actorNumber)
    {
        // Default player color palette
        return (actorNumber % 4) switch
        {
            0 => new Color(0.9f, 0.3f, 0.3f), // Red
            1 => new Color(0.3f, 0.5f, 0.9f), // Blue
            2 => new Color(0.3f, 0.8f, 0.3f), // Green
            3 => new Color(0.9f, 0.8f, 0.2f), // Yellow
            _ => Color.white
        };
    }
}
