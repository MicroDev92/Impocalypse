using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [Header("Grid Settings")]
    public int gridWidth = 10;
    public int gridHeight = 10;
    public float tileSize = 1f;
    public Vector2 gridOffset = Vector2.zero;

    [Header("Zoom Settings")]
    public float zoomSpeedTouch = 0.01f;
    public float zoomSpeedMouse = 5f;
    public float minZoom = 3f;
    public float maxZoom = 12f;

    [Header("Pan Settings")]
    public float panSpeed = 0.01f;

    private Camera cam;
    private Vector3 lastPanPosition;
    private int panFingerId;
    private bool isPanning;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void Start()
    {
        FitGridToScreen();
    }

    private void Update()
    {
        HandleZoom();
        HandlePan();
    }

    private void FitGridToScreen()
    {
        float aspect = (float)Screen.width / Screen.height;

        float gridWorldWidth = gridWidth * tileSize;
        float gridWorldHeight = gridHeight * tileSize;

        float requiredHeight = Mathf.Max(gridWorldHeight, gridWorldWidth / aspect);
        cam.orthographicSize = Mathf.Clamp(requiredHeight / 2f, minZoom, maxZoom);

        float centerX = (gridWidth * tileSize) / 2f - tileSize / 2f + gridOffset.x;
        float centerY = (gridHeight * tileSize) / 2f - tileSize / 2f + gridOffset.y;
        cam.transform.position = new Vector3(centerX, centerY, -10f);
    }

    private void HandleZoom()
    {
        // Mouse scroll
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            cam.orthographicSize -= scroll * zoomSpeedMouse;
        }

        // Touch pinch
        if (Input.touchCount == 2)
        {
            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);

            float prevDistance = (t0.position - t0.deltaPosition - (t1.position - t1.deltaPosition)).magnitude;
            float currentDistance = (t0.position - t1.position).magnitude;
            float delta = prevDistance - currentDistance;

            cam.orthographicSize += delta * zoomSpeedTouch;
        }

        cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
    }

    private void HandlePan()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        if (Input.GetMouseButtonDown(0))
        {
            lastPanPosition = cam.ScreenToWorldPoint(Input.mousePosition);
        }
        else if (Input.GetMouseButton(0))
        {
            Vector3 currentPanPosition = cam.ScreenToWorldPoint(Input.mousePosition);
            Vector3 difference = lastPanPosition - currentPanPosition;
            cam.transform.position += difference;
        }
#elif UNITY_ANDROID || UNITY_IOS
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                isPanning = true;
                panFingerId = touch.fingerId;
                lastPanPosition = cam.ScreenToWorldPoint(touch.position);
            }
            else if (touch.phase == TouchPhase.Moved && touch.fingerId == panFingerId && isPanning)
            {
                Vector3 currentPanPosition = cam.ScreenToWorldPoint(touch.position);
                Vector3 difference = lastPanPosition - currentPanPosition;
                cam.transform.position += difference;
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                isPanning = false;
            }
        }
#endif
    }
}
