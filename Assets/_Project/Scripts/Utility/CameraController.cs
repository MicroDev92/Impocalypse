using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using _Project.Scripts.Utility;
using Core.Events;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(Camera))]
[ExecuteAlways]
public class CameraController : MonoBehaviour
{
    [Header("Grid Settings")]
    public int gridWidth = 12;
    public int gridHeight = 12;
    public float tileSize = 1f;

    [Header("Layout Margins (screen percentage 0-1)")]
    [Tooltip("Space at top for header")]
    [Range(0f, 0.3f)]
    public float headerHeight = 0.07f;

    [Tooltip("Space at bottom for card panel")]
    [Range(0f, 0.3f)]
    public float cardPanelHeight = 0.15f;

    [Tooltip("Space on right for resource panel")]
    [Range(0f, 0.4f)]
    public float resourcePanelWidth = 0.0f;

    [Tooltip("Small padding around the grid")]
    [Range(0f, 0.1f)]
    public float gridPadding = 0.03f;

    [Header("Camera Lock")]
    [Tooltip("When true, disables zoom and pan")]
    public bool lockCamera = false;

    [Header("Zoom Settings (when unlocked)")]
    public float zoomSpeedTouch = 0.01f;
    public float zoomSpeedMouse = 0.05f;
    public float minZoom = 4f;
    public float maxZoom = 10f;

    [Header("Smooth Camera")]
    public float smoothSpeed = 5f;

    private Camera cam;
    private Vector3 lastPanPosition;
    private bool isPanning = false;

    private int _lastScreenWidth;
    private int _lastScreenHeight;

    private Vector3 _targetPosition;
    private float _targetOrthoSize;
    private bool _isAnimating;

    private IEventBus _eventBus;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void Start()
    {
        FitGridToScreen();
        _targetPosition = cam.transform.position;
        _targetOrthoSize = cam.orthographicSize;

        if (Application.isPlaying && ServiceLocator.TryGetService<IEventBus>(out _eventBus))
        {
            _eventBus.Subscribe<GamePhaseChangedEvent>(OnPhaseChanged);
        }
    }

    private void OnDestroy()
    {
        if (_eventBus != null)
        {
            _eventBus.Unsubscribe<GamePhaseChangedEvent>(OnPhaseChanged);
        }
    }

    private void OnPhaseChanged(GamePhaseChangedEvent evt)
    {
        ResetView();
    }

    /// <summary>
    /// Resets the camera to the default fitted view with smooth animation.
    /// </summary>
    public void ResetView()
    {
        FitGridToScreen();
        _isAnimating = true;
    }

    private void Update()
    {
        if (!lockCamera)
        {
            HandleZoom();
            HandlePan();
        }

        // Smooth lerp toward targets
        if (_isAnimating)
        {
            cam.transform.position = Vector3.Lerp(cam.transform.position, _targetPosition, Time.deltaTime * smoothSpeed);
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, _targetOrthoSize, Time.deltaTime * smoothSpeed);

            if (Vector3.Distance(cam.transform.position, _targetPosition) < 0.01f &&
                Mathf.Abs(cam.orthographicSize - _targetOrthoSize) < 0.01f)
            {
                cam.transform.position = _targetPosition;
                cam.orthographicSize = _targetOrthoSize;
                _isAnimating = false;
            }
        }

        if (Screen.width != _lastScreenWidth || Screen.height != _lastScreenHeight)
        {
            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;
            FitGridToScreen();
        }
    }

    private void OnDrawGizmos()
    {
        if (cam == null) cam = GetComponent<Camera>();

        float aspect = cam.aspect;
        float camHeight = cam.orthographicSize * 2f;
        float camWidth = camHeight * aspect;
        Vector3 camPos = cam.transform.position;

        // Camera bounds (yellow)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(camPos, new Vector3(camWidth, camHeight, 0));

        // Calculate screen areas in world space
        float left = camPos.x - camWidth / 2f;
        float right = camPos.x + camWidth / 2f;
        float bottom = camPos.y - camHeight / 2f;
        float top = camPos.y + camHeight / 2f;

        // Header area (cyan) - top strip
        float headerH = camHeight * headerHeight;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(new Vector3(camPos.x, top - headerH / 2f, 0), new Vector3(camWidth, headerH, 0));

        // Card panel area (magenta) - bottom strip
        float cardH = camHeight * cardPanelHeight;
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireCube(new Vector3(camPos.x, bottom + cardH / 2f, 0), new Vector3(camWidth, cardH, 0));

        // Resource panel area (red) - right strip (between header and cards)
        float resourceW = camWidth * resourcePanelWidth;
        float middleHeight = camHeight * (1f - headerHeight - cardPanelHeight);
        float middleY = bottom + cardH + middleHeight / 2f;
        if (resourcePanelWidth > 0f)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(new Vector3(right - resourceW / 2f, middleY, 0), new Vector3(resourceW, middleHeight, 0));
        }

        // Grid area (green) - center area
        float gridAreaW = camWidth * (1f - resourcePanelWidth) - camWidth * gridPadding * 2f;
        float gridAreaH = middleHeight - camHeight * gridPadding * 2f;
        float gridAreaX = left + (camWidth * (1f - resourcePanelWidth)) / 2f;
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(new Vector3(gridAreaX, middleY, 0), new Vector3(gridAreaW, gridAreaH, 0));
    }

    private void FitGridToScreen()
    {
        float aspect = (float)Screen.width / Screen.height;

        float boardWorldWidth = gridWidth * tileSize;
        float boardWorldHeight = gridHeight * tileSize;

        // Calculate the available area for the grid (as screen ratios)
        float availableWidthRatio = 1f - resourcePanelWidth - gridPadding * 2f;
        float availableHeightRatio = 1f - headerHeight - cardPanelHeight - gridPadding * 2f;

        // Calculate orthographic size to fit the grid within the available area
        float orthoSizeForHeight = (boardWorldHeight / 2f) / availableHeightRatio;
        float orthoSizeForWidth = (boardWorldWidth / 2f) / (availableWidthRatio * aspect);

        // Use the larger value to ensure the grid fits completely
        float requiredOrthoSize = Mathf.Max(orthoSizeForHeight, orthoSizeForWidth);

        // Calculate camera world dimensions
        float camHeight = requiredOrthoSize * 2f;
        float camWidth = camHeight * aspect;

        // Grid center in world space
        float gridCenterX = (gridWidth - 1) * tileSize / 2f;
        float gridCenterY = (gridHeight - 1) * tileSize / 2f;

        // Calculate where the grid area center should be on screen
        float gridAreaCenterXRatio = (1f - resourcePanelWidth) / 2f;
        float horizontalOffset = (0.5f - gridAreaCenterXRatio) * camWidth;

        float gridAreaCenterYRatio = cardPanelHeight + availableHeightRatio / 2f + gridPadding;
        float verticalOffset = (0.5f - gridAreaCenterYRatio) * camHeight;

        float camX = gridCenterX + horizontalOffset;
        float camY = gridCenterY + verticalOffset;

        var newPos = new Vector3(camX, camY, -10f);

        if (_isAnimating)
        {
            // When animating, set targets (lerp will handle transition)
            _targetPosition = newPos;
            _targetOrthoSize = requiredOrthoSize;
        }
        else
        {
            // Snap immediately (initial load, editor, resolution change)
            cam.orthographicSize = requiredOrthoSize;
            cam.transform.position = newPos;
            _targetPosition = newPos;
            _targetOrthoSize = requiredOrthoSize;
        }
    }

    private void HandleZoom()
    {
        // Mouse Scroll
        if (Mouse.current != null)
        {
            float scroll = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                cam.orthographicSize -= scroll * zoomSpeedMouse;
                _isAnimating = false; // user took manual control
            }
        }

        // Touch Pinch
        if (Touchscreen.current != null && Touchscreen.current.touches.Count >= 2)
        {
            var t0 = Touchscreen.current.touches[0];
            var t1 = Touchscreen.current.touches[1];

            if (t0.isInProgress && t1.isInProgress)
            {
                Vector2 prevT0 = t0.startPosition.ReadValue();
                Vector2 prevT1 = t1.startPosition.ReadValue();
                Vector2 currentT0 = t0.position.ReadValue();
                Vector2 currentT1 = t1.position.ReadValue();

                float prevDist = Vector2.Distance(prevT0, prevT1);
                float currentDist = Vector2.Distance(currentT0, currentT1);
                float delta = prevDist - currentDist;

                cam.orthographicSize += delta * zoomSpeedTouch;
                _isAnimating = false;
            }
        }

        cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
    }

    private void HandlePan()
    {
        // Middle-mouse drag (left click reserved for card selection)
        if (Mouse.current != null)
        {
            if (Mouse.current.middleButton.wasPressedThisFrame)
            {
                // Don't start pan if pointer is over UI
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                    return;

                lastPanPosition = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
                isPanning = true;
                _isAnimating = false;
            }
            else if (Mouse.current.middleButton.isPressed && isPanning)
            {
                Vector3 currentPos = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
                Vector3 panDelta = lastPanPosition - currentPos;
                cam.transform.position += panDelta;
                ClampCameraPosition();
            }
            else if (Mouse.current.middleButton.wasReleasedThisFrame)
            {
                isPanning = false;
            }
        }

        // Touch drag (1 finger)
        if (Touchscreen.current != null && Touchscreen.current.touches.Count == 1)
        {
            var touch = Touchscreen.current.touches[0];

            if (touch.isInProgress)
            {
                Vector2 screenPos = touch.position.ReadValue();

                // Don't start pan if pointer is over UI
                if (!isPanning && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                    return;

                Vector3 worldPos = cam.ScreenToWorldPoint(screenPos);

                if (!isPanning)
                {
                    lastPanPosition = worldPos;
                    isPanning = true;
                    _isAnimating = false;
                }
                else
                {
                    Vector3 panDelta = lastPanPosition - worldPos;
                    cam.transform.position += panDelta;
                    lastPanPosition = worldPos;
                    ClampCameraPosition();
                }
            }

            if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Ended ||
                touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Canceled)
            {
                isPanning = false;
            }
        }
    }

    private void ClampCameraPosition()
    {
        float gridCenterX = (gridWidth - 1) * tileSize / 2f;
        float gridCenterY = (gridHeight - 1) * tileSize / 2f;
        float halfGridW = gridWidth * tileSize / 2f;
        float halfGridH = gridHeight * tileSize / 2f;

        Vector3 pos = cam.transform.position;
        pos.x = Mathf.Clamp(pos.x, gridCenterX - halfGridW, gridCenterX + halfGridW);
        pos.y = Mathf.Clamp(pos.y, gridCenterY - halfGridH, gridCenterY + halfGridH);
        cam.transform.position = pos;
    }
}
