using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using _Project.Scripts.Utility;
using Core.Events;

/// <summary>
/// Detects left-clicks on board tiles and publishes a TileHighlightedEvent, which the
/// TileInfoPanel (right sidebar) listens for to show that tile's details.
///
/// Read-only: it only inspects, never changes game state, so it is safe to run in every
/// phase (and coexists with ConstructionUIHandler's build-phase placement clicks).
/// Uses the grid's integer tile positions, so no colliders are required on tiles.
/// </summary>
public class TileClickInspector : MonoBehaviour
{
    private IEventBus _eventBus;
    private IGridService _gridService;
    private Camera _camera;

    private void Start()
    {
        ServiceLocator.TryGetService(out _eventBus);
        ServiceLocator.TryGetService(out _gridService);
        _camera = Camera.main;
    }

    private void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;

        // Ignore clicks that land on UI (sidebars, buttons, cards).
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        if (_camera == null) _camera = Camera.main;
        if (_camera == null || _gridService == null) return;

        Vector3 world = _camera.ScreenToWorldPoint(mouse.position.ReadValue());
        var gridPos = new Vector2Int(Mathf.RoundToInt(world.x), Mathf.RoundToInt(world.y));

        // GetTileByPosition only returns playable tiles (border/coordinate tiles are excluded),
        // so a click on the frame or off the board simply does nothing.
        var tile = _gridService.GetTileByPosition(gridPos);
        if (tile == null || string.IsNullOrEmpty(tile.coord)) return;

        _eventBus?.Publish(new TileHighlightedEvent(tile.coord, true));
    }
}
