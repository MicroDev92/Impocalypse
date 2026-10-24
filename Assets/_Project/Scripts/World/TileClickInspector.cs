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
        // Mouse OR touch. Gating on Mouse.current alone meant tiles were simply dead
        // on a touch-only device: Mouse.current is null there, so this returned before
        // doing anything. Buttons still worked, because Unity UI routes touch through
        // the EventSystem - which is why the tablet looked fine until you tapped the board.
        Vector2 screenPos;
        var mouse = Mouse.current;
        var touch = Touchscreen.current;

        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            screenPos = mouse.position.ReadValue();
        }
        else if (touch != null && touch.primaryTouch.press.wasPressedThisFrame)
        {
            screenPos = touch.primaryTouch.position.ReadValue();
        }
        else return;

        // Ignore taps that land on UI (sidebars, buttons, cards).
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        if (_camera == null) _camera = Camera.main;
        if (_camera == null || _gridService == null) return;

        Vector3 world = _camera.ScreenToWorldPoint(screenPos);
        var gridPos = new Vector2Int(Mathf.RoundToInt(world.x), Mathf.RoundToInt(world.y));

        // GetTileByPosition only returns playable tiles (border/coordinate tiles are excluded),
        // so a click on the frame or off the board simply does nothing.
        var tile = _gridService.GetTileByPosition(gridPos);
        if (tile == null || string.IsNullOrEmpty(tile.coord)) return;

        _eventBus?.Publish(new TileHighlightedEvent(tile.coord, true));
    }
}
