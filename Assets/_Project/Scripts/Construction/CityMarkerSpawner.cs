using System.Collections.Generic;
using TMPro;
using UnityEngine;
using _Project.Scripts.Utility;
using Core.Events;

/// <summary>
/// Places a city marker on a tile when a city is built, so the player can see what
/// they constructed. Logic only - the marker's appearance lives entirely in the
/// assigned prefab (assign a sprite/art there and swap it freely without touching code).
///
/// Mirrors ImpSpawner's event-driven pattern. If the prefab has a TMP_Text anywhere in
/// its children, the city's strength is written into it.
/// </summary>
public class CityMarkerSpawner : MonoBehaviour
{
    [Header("Marker")]
    [Tooltip("Prefab with a SpriteRenderer (city art) and optionally a TMP_Text for strength.")]
    [SerializeField] private GameObject markerPrefab;

    [Tooltip("Local offset from the tile centre so the marker doesn't sit on top of the imp.")]
    [SerializeField] private Vector2 tileOffset = new Vector2(0f, 0.15f);

    [Tooltip("Sorting order applied to the marker's sprites so they draw above tiles and imps.")]
    [SerializeField] private int sortingOrder = 100;

    private IEventBus _eventBus;
    private IGridService _gridService;
    private readonly Dictionary<string, GameObject> _markers = new();

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

    /// <summary>Removes the marker if the tile's building was razed (e.g. camp eviction).</summary>
    private void OnTileOwnerChanged(TileOwnerChangedEvent evt)
    {
        if (!_markers.TryGetValue(evt.Coordinate, out var marker) || marker == null) return;

        var tile = _gridService?.GetTileByCoord(evt.Coordinate);
        if (tile != null && !tile.HasBuilding)
        {
            Destroy(marker);
            _markers.Remove(evt.Coordinate);
        }
    }

    private void OnCityPlaced(CityPlacedEvent evt)
    {
        if (markerPrefab == null)
        {
            Debug.LogWarning("[CityMarkerSpawner] No marker prefab assigned - nothing to show.");
            return;
        }

        var tile = _gridService?.GetTileByCoord(evt.Coordinate);
        if (tile == null) return;

        // One marker per tile; replace any existing one (e.g. a network re-broadcast).
        if (_markers.TryGetValue(evt.Coordinate, out var existing) && existing != null)
            Destroy(existing);

        var marker = Instantiate(
            markerPrefab,
            tile.transform.position + (Vector3)tileOffset,
            Quaternion.identity);
        marker.name = $"CityMarker_{evt.Coordinate}_Str{evt.Strength}";

        // Write the strength number if the prefab provides a label.
        var label = marker.GetComponentInChildren<TMP_Text>();
        if (label != null) label.text = evt.Strength.ToString();

        // Ensure the marker renders above the board.
        foreach (var sr in marker.GetComponentsInChildren<SpriteRenderer>())
            sr.sortingOrder = sortingOrder;

        _markers[evt.Coordinate] = marker;
    }
}
