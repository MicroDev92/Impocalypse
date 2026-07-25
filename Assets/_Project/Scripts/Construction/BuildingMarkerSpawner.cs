using System.Collections.Generic;
using TMPro;
using UnityEngine;
using _Project.Scripts.Utility;
using Core.Events;

/// <summary>
/// Gives every structure a body on the board: citadels, farms, bazaars, warcamps,
/// and hellgate endpoints. Logic only - appearance is the generic marker prefab
/// plus the sprite slots below, all swappable without touching code.
///
/// Architecture: state reconciliation, not event choreography. On any relevant
/// event the spawner re-syncs markers against the tile's actual Building/SkyLink
/// state, which automatically covers placements, camp evictions, camp-to-citadel
/// upgrades, network re-broadcasts, and the hellgate's far endpoint (which never
/// receives an event of its own).
/// </summary>
public class BuildingMarkerSpawner : MonoBehaviour
{
    [Header("Marker")]
    [Tooltip("Generic marker prefab: a SpriteRenderer and optionally a TMP_Text (used for citadel strength).")]
    [SerializeField] private GameObject markerPrefab;

    [Tooltip("Local offset from the tile centre so the marker doesn't sit on top of the imp.")]
    [SerializeField] private Vector2 tileOffset = new Vector2(0f, 0.15f);

    [Tooltip("Corner offset for hellgate markers, so they coexist with a building marker on the same tile.")]
    [SerializeField] private Vector2 gateOffset = new Vector2(0.3f, -0.3f);

    [Tooltip("Sorting order applied to marker sprites so they draw above tiles and imps.")]
    [SerializeField] private int sortingOrder = 100;

    [Tooltip("Building marker size as a fraction of a grid cell.")]
    [Range(0.2f, 1f)]
    [SerializeField] private float cellFraction = 0.55f;

    [Tooltip("Hellgate marker size as a fraction of a grid cell.")]
    [Range(0.1f, 0.8f)]
    [SerializeField] private float gateCellFraction = 0.38f;

    [Header("Building Sprites")]
    [SerializeField] private Sprite citadelSprite;
    [SerializeField] private Sprite farmSprite;
    [SerializeField] private Sprite bazaarSprite;
    [SerializeField] private Sprite warcampSprite;
    [SerializeField] private Sprite hellgateSprite;

    private IEventBus _eventBus;
    private IGridService _gridService;

    private readonly Dictionary<string, (GameObject go, BuildingKind kind)> _buildingMarkers = new();
    private readonly Dictionary<string, GameObject> _gateMarkers = new();

    private void Start()
    {
        ServiceLocator.TryGetService(out _eventBus);
        ServiceLocator.TryGetService(out _gridService);
        _eventBus?.Subscribe<CityPlacedEvent>(OnCityPlaced);
        _eventBus?.Subscribe<TileOwnerChangedEvent>(OnTileOwnerChanged);
    }

    private void OnDestroy()
    {
        _eventBus?.Unsubscribe<CityPlacedEvent>(OnCityPlaced);
        _eventBus?.Unsubscribe<TileOwnerChangedEvent>(OnTileOwnerChanged);
    }

    private void OnCityPlaced(CityPlacedEvent evt) => SyncTile(evt.Coordinate);
    private void OnTileOwnerChanged(TileOwnerChangedEvent evt) => SyncTile(evt.Coordinate);

    private void SyncTile(string coord)
    {
        var tile = _gridService?.GetTileByCoord(coord);
        if (tile == null) return;

        SyncBuildingMarker(tile);

        // Hellgates: sync this endpoint, its partner (which gets no event of its
        // own), and sweep stale gate markers left by broken links (evictions).
        SyncGateMarker(tile);
        if (tile.HasSkyLink)
        {
            var partner = _gridService.GetTileByCoord(tile.SkyLinkCoord);
            if (partner != null) SyncGateMarker(partner);
        }
        SweepStaleGates();
    }

    // ==========================================
    // BUILDINGS
    // ==========================================

    private void SyncBuildingMarker(TileData tile)
    {
        _buildingMarkers.TryGetValue(tile.coord, out var current);

        if (!tile.HasBuilding)
        {
            if (current.go != null) Destroy(current.go);
            _buildingMarkers.Remove(tile.coord);
            return;
        }

        // Correct marker already up (idempotent for re-broadcasts).
        if (current.go != null && current.kind == tile.Building) return;

        if (current.go != null) Destroy(current.go);

        var sprite = SpriteForBuilding(tile.Building);
        if (markerPrefab == null || sprite == null)
        {
            Debug.LogWarning($"[BuildingMarkerSpawner] Missing prefab or sprite for {tile.Building}.");
            return;
        }

        var marker = SpawnMarker(tile, sprite, tileOffset, cellFraction);
        marker.name = $"Marker_{tile.coord}_{tile.Building}";

        // Strength number is citadel-only; other buildings blank the label.
        var label = marker.GetComponentInChildren<TMP_Text>();
        if (label != null)
            label.text = tile.Building == BuildingKind.City
                ? ((int)tile.BuildingStrength).ToString()
                : string.Empty;

        _buildingMarkers[tile.coord] = (marker, tile.Building);
    }

    private Sprite SpriteForBuilding(BuildingKind kind) => kind switch
    {
        BuildingKind.City => citadelSprite,
        BuildingKind.CommonFarm => farmSprite,
        BuildingKind.SpecialFarm => farmSprite,
        BuildingKind.Market => bazaarSprite,
        BuildingKind.Camp => warcampSprite,
        _ => null
    };

    // ==========================================
    // HELLGATES
    // ==========================================

    private void SyncGateMarker(TileData tile)
    {
        _gateMarkers.TryGetValue(tile.coord, out var current);

        if (!tile.HasSkyLink)
        {
            if (current != null) Destroy(current);
            _gateMarkers.Remove(tile.coord);
            return;
        }

        if (current != null) return; // already shown

        if (markerPrefab == null || hellgateSprite == null) return;

        var marker = SpawnMarker(tile, hellgateSprite, gateOffset, gateCellFraction);
        marker.name = $"Hellgate_{tile.coord}";

        var label = marker.GetComponentInChildren<TMP_Text>();
        if (label != null) label.text = string.Empty;

        _gateMarkers[tile.coord] = marker;
    }

    /// <summary>Removes gate markers whose tile lost its link. We can't know the
    /// partner after a link is already cleared, so reconcile the whole set.</summary>
    private void SweepStaleGates()
    {
        List<string> stale = null;
        foreach (var kvp in _gateMarkers)
        {
            var tile = _gridService.GetTileByCoord(kvp.Key);
            if (tile == null || !tile.HasSkyLink)
                (stale ??= new List<string>()).Add(kvp.Key);
        }
        if (stale == null) return;

        foreach (var coord in stale)
        {
            if (_gateMarkers[coord] != null) Destroy(_gateMarkers[coord]);
            _gateMarkers.Remove(coord);
        }
    }

    // ==========================================
    // SHARED
    // ==========================================

    private GameObject SpawnMarker(TileData tile, Sprite sprite, Vector2 offset, float fraction)
    {
        var marker = Instantiate(
            markerPrefab,
            tile.transform.position + (Vector3)offset,
            Quaternion.identity);

        foreach (var sr in marker.GetComponentsInChildren<SpriteRenderer>())
        {
            sr.sprite = sprite;
            sr.sortingOrder = sortingOrder;
        }

        // Normalize to a fraction of the cell, whatever the art's pixel size
        // (same art-proofing rule as TileData.FitToCell).
        var size = sprite.bounds.size;
        if (size.x > 0f && size.y > 0f)
        {
            float k = fraction / Mathf.Max(size.x, size.y);
            marker.transform.localScale = new Vector3(k, k, 1f);
        }

        return marker;
    }
}
