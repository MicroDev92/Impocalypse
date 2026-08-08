using Cards;
using UnityEngine;

/// <summary>What kind of building occupies a tile (at most one per tile).</summary>
public enum BuildingKind { None, City, CommonFarm, SpecialFarm, Market, Camp }

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

    // Building (at most one per tile: a City, a Farm, or a Market)
    private BuildingKind _building = BuildingKind.None;
    private CityStrength _cityStrength = CityStrength.None;
    private ResourceType _addedResource = ResourceType.None;

    // Properties
    public int OwnerActorNumber => _ownerActorNumber;
    public bool IsOwned => _ownerActorNumber >= 0;
    public bool HasBuilding => _building != BuildingKind.None;
    public BuildingKind Building => _building;
    public CityStrength BuildingStrength => _building == BuildingKind.City ? _cityStrength : CityStrength.None;

    /// <summary>The resource a Farm/Market on this tile adds (None for a city or empty tile).</summary>
    public ResourceType AddedResource => _addedResource;
    /// <summary>True if a Market (wildcard common) sits on this tile.</summary>
    public bool ProducesWildcard => _building == BuildingKind.Market;
    /// <summary>The resource this tile's terrain produces on its own (None for non-resource terrain).</summary>
    public ResourceType BaseResource => ResourceTypes.FromTerrain(type);

    /// <summary>
    /// The terrain art currently drawn on this tile. Territory cards use it for
    /// their thumbnail, so a card can never show different terrain than the board.
    /// </summary>
    public Sprite TerrainSprite
    {
        get
        {
            var sr = backgroundRenderer != null ? backgroundRenderer : GetComponent<SpriteRenderer>();
            return sr != null ? sr.sprite : null;
        }
    }

    /// <summary>Coordinate of the tile this one is sky-tower-linked to (null if none).
    /// Linked tiles count as adjacent for fief connectivity.</summary>
    public string SkyLinkCoord { get; private set; }
    public bool HasSkyLink => !string.IsNullOrEmpty(SkyLinkCoord);

    /// <summary>A sky tower endpoint needs an owned tile without an existing link.</summary>
    public bool CanPlaceSkyTower() => IsOwned && !HasSkyLink;

    public void PlaceSkyLink(string otherCoord) => SkyLinkCoord = otherCoord;
    public void ClearSkyLink() => SkyLinkCoord = null;

    /// <summary>
    /// True if this tile is held by a Camp. The camp occupies the building slot (so
    /// nothing else can be built here), and the claim is provisional: playing the
    /// tile's actual Territory card evicts the camper.
    /// </summary>
    public bool IsCampClaim => _building == BuildingKind.Camp;

    /// <summary>Places a camp - it fills the building slot, blocking other buildings.</summary>
    public void PlaceCamp() => _building = BuildingKind.Camp;

    /// <summary>Removes the building (camp removal on eviction/upgrade).</summary>
    public void ClearBuilding()
    {
        _building = BuildingKind.None;
        _cityStrength = CityStrength.None;
        _addedResource = ResourceType.None;
    }

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

    /// <summary>A Common Farm or Market goes on an owned, building-free, resource-less tile.</summary>
    public bool CanPlaceCommonResource() => IsOwned && !HasBuilding && BaseResource == ResourceType.None;

    /// <summary>A Special Farm goes on an owned, building-free tile of the required terrain.</summary>
    public bool CanPlaceSpecialFarm(TileType requiredTerrain) =>
        IsOwned && !HasBuilding && type == requiredTerrain;

    /// <summary>
    /// Whether this tile type produces a resource for fief wealth calculation.
    /// </summary>
    public bool IsResourceTile =>
        type == TileType.Wood || type == TileType.Carrot || type == TileType.Fish ||
        type == TileType.Water; // the Styx produces Souls

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

    [Tooltip("Glyph (letter/number/skull) size as a fraction of the cell on coordinate tiles.")]
    [SerializeField] private float labelCellFraction = 0.7f;

    /// <summary>
    /// Scales the tile so its background sprite spans exactly cellSize world units,
    /// regardless of the source image's pixel size or PPU. Makes the board seamless
    /// and art-proof: any sprite resolves to a perfect grid cell.
    /// </summary>
    public void FitToCell(float cellSize)
    {
        var sr = backgroundRenderer != null ? backgroundRenderer : GetComponent<SpriteRenderer>();
        if (sr == null || sr.sprite == null) return;

        var size = sr.sprite.bounds.size;
        if (size.x <= 0f || size.y <= 0f) return;

        transform.localScale = new Vector3(cellSize / size.x, cellSize / size.y, 1f);

        // The glyph is a child, so the root normalization above scales it by an
        // arbitrary factor that depends on the background art's pixel size. Undo
        // that: normalize the glyph independently so it always spans
        // labelCellFraction of the cell, no matter what art either sprite uses.
        if (labelRenderer != null && labelRenderer.sprite != null)
        {
            var glyph = labelRenderer.sprite.bounds.size;
            if (glyph.x > 0f && glyph.y > 0f)
            {
                float worldTarget = cellSize * labelCellFraction;
                float k = worldTarget / Mathf.Max(glyph.x, glyph.y);
                labelRenderer.transform.localScale = new Vector3(
                    k * size.x / cellSize,
                    k * size.y / cellSize,
                    1f);
                labelRenderer.sortingOrder = 1; // always above the slab
            }
        }
    }

    [Header("Highlight")]
    [Tooltip("Translucent overlay shown when the tile is highlighted. An overlay BRIGHTENS any terrain, unlike a sprite tint which can only darken - so it contrasts every tile type.")]
    [SerializeField] private Color highlightColor = new Color(1f, 0.95f, 0.45f, 0.5f);

    private SpriteRenderer _highlightOverlay;
    private static Sprite _unitWhiteSprite;

    public void Highlight(bool on)
    {
        if (_highlightOverlay == null)
        {
            if (!on) return;
            CreateHighlightOverlay();
        }
        _highlightOverlay.enabled = on;
    }

    private void CreateHighlightOverlay()
    {
        var go = new GameObject("Highlight");
        go.transform.SetParent(transform, false);

        _highlightOverlay = go.AddComponent<SpriteRenderer>();
        _highlightOverlay.sprite = GetUnitWhiteSprite();
        _highlightOverlay.color = highlightColor;
        _highlightOverlay.sortingOrder = 5; // above tile art (0), below imps (10)
        _highlightOverlay.enabled = false;

        // Match the background sprite's local bounds, so the parent's FitToCell
        // scaling lands the overlay at exactly one grid cell.
        var sr = backgroundRenderer != null ? backgroundRenderer : GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
        {
            var size = sr.sprite.bounds.size;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
        }
    }

    private static Sprite GetUnitWhiteSprite()
    {
        if (_unitWhiteSprite == null)
        {
            var tex = Texture2D.whiteTexture;
            _unitWhiteSprite = Sprite.Create(
                tex, new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f), tex.width);
        }
        return _unitWhiteSprite;
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
        _building = BuildingKind.City;
        _cityStrength = strength;
        // Visual indicator will be handled by the construction system
    }

    /// <summary>Places a farm that adds the given resource (special = true for a luxury farm).</summary>
    public void PlaceFarm(ResourceType resource, bool special)
    {
        _building = special ? BuildingKind.SpecialFarm : BuildingKind.CommonFarm;
        _addedResource = resource;
    }

    /// <summary>Places a Market (wildcard common, resolved during scoring).</summary>
    public void PlaceMarket()
    {
        _building = BuildingKind.Market;
        _addedResource = ResourceType.WildcardCommon;
    }

    /// <summary>
    /// Resets ownership and building state.
    /// </summary>
    public void ResetOwnership()
    {
        _ownerActorNumber = -1;
        _building = BuildingKind.None;
        _cityStrength = CityStrength.None;
        _addedResource = ResourceType.None;
        SkyLinkCoord = null;

        if (_highlightOverlay != null) _highlightOverlay.enabled = false;

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
