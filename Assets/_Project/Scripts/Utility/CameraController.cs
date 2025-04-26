using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
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
    public Vector2 gridOffset = Vector2.zero;

    [Header("Zoom Settings")]
    public float zoomSpeedTouch = 0.01f;
    public float zoomSpeedMouse = 0.05f;
    public float minZoom = 3f;
    public float maxZoom = 14f;

    private Camera cam;
    private Vector3 lastPanPosition;
    private bool isPanning = false;
    
    private int _lastScreenWidth;
    private int _lastScreenHeight;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void Start()
    {
        FitGridToScreen();
        Debug.Log("Delayed ortho size applied: " + cam.orthographicSize);
    }
    
    private void Update()
    {
        HandleZoom();
        HandlePan();
        
        if (Screen.width != _lastScreenWidth || Screen.height != _lastScreenHeight)
        {
            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;

            FitGridToScreen(); // Recalculate camera on resolution/orientation change
        }

    }
    
    private void OnDrawGizmos()
    {
        if (cam == null) cam = GetComponent<Camera>();

        // Current camera bounds (yellow)
        float camHeight = cam.orthographicSize * 2f;
        float camWidth = camHeight * cam.aspect;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(cam.transform.position, new Vector3(camWidth, camHeight, 0));

        // Suggested fixed bounds (green preview)
        float aspect = (float)Screen.width / Screen.height;
        float boardWorldWidth = gridWidth * tileSize;
        float boardWorldHeight = gridHeight * tileSize;

        float halfBoardHeight = boardWorldHeight / 2f;
        float halfBoardWidth = boardWorldWidth / 2f;

        float suggestedOrthoSize = Mathf.Max(halfBoardHeight, halfBoardWidth / aspect);
        float previewHeight = suggestedOrthoSize * 2f;
        float previewWidth = previewHeight * aspect;

        float centerX = (gridWidth - 1) * tileSize / 2f + gridOffset.x;
        float centerY = (gridHeight - 1) * tileSize / 2f + gridOffset.y;
        Vector3 previewCenter = new Vector3(centerX, centerY, -10f);

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(previewCenter, new Vector3(previewWidth, previewHeight, 0));
    }



    private void FitGridToScreen()
    {
        float aspect = (float)Screen.width / Screen.height;

        float boardWorldWidth = gridWidth * tileSize;
        float boardWorldHeight = gridHeight * tileSize;

        float halfBoardHeight = boardWorldHeight / 2f;
        float halfBoardWidth = boardWorldWidth / 2f;

        // ✅ Proper size that fits the entire board, even on tall/narrow screens
        float requiredOrthoSize = Mathf.Max(halfBoardHeight, halfBoardWidth / aspect);

        // ✅ This ensures no tiles get clipped due to rounding
        cam.orthographicSize = requiredOrthoSize + 0.01f;

        // ✅ Center the camera over the entire grid
        float centerX = (gridWidth - 1) * tileSize / 2f + gridOffset.x;
        float centerY = (gridHeight - 1) * tileSize / 2f + gridOffset.y;
        cam.transform.position = new Vector3(centerX, centerY, -10f);
    }




    
    private void HandleZoom()
    {
        // Mouse Scroll
        if (Mouse.current != null)
        {
            float scroll = Mouse.current.scroll.ReadValue().y;
            cam.orthographicSize -= scroll * zoomSpeedMouse;
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
            }
        }

        cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
    }

    private void HandlePan()
    {
        Vector3 panDelta = Vector3.zero;

        // Mouse drag
        if (Mouse.current != null)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                lastPanPosition = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
                isPanning = true;
            }
            else if (Mouse.current.leftButton.isPressed && isPanning)
            {
                Vector3 currentPos = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
                panDelta = lastPanPosition - currentPos;
                cam.transform.position += panDelta;
            }
            else if (Mouse.current.leftButton.wasReleasedThisFrame)
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
                Vector3 worldPos = cam.ScreenToWorldPoint(screenPos);

                if (!isPanning)
                {
                    lastPanPosition = worldPos;
                    isPanning = true;
                }
                else
                {
                    panDelta = lastPanPosition - worldPos;
                    cam.transform.position += panDelta;
                    lastPanPosition = worldPos;
                }
            }

            if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Ended ||
                touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Canceled)
            {
                isPanning = false;
            }
        }
    }
}
