using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using _Project.Scripts.Utility;
using Core.Events;

namespace World
{
    [RequireComponent(typeof(CinemachineCamera))]
    public class ModernCameraController : MonoBehaviour
    {
        [Header("Input References")]
        [SerializeField] private InputActionReference panAction;
        [SerializeField] private InputActionReference zoomAction;

        [Header("Grid Fit Settings")]
        public Vector2Int gridDimensions = new Vector2Int(12, 12);
        public float tileSize = 1f;
        
        [Header("UI Margins (Screen Percentage 0-1)")]
        [Range(0f, 0.3f)] public float headerHeight = 0.07f;
        [Range(0f, 0.3f)] public float cardPanelHeight = 0.15f;
        [Range(0f, 0.4f)] public float resourcePanelWidth = 0.0f;
        [Tooltip("Equal space reserved on BOTH the left and right edges for the two sidebars.")]
        [Range(0f, 0.4f)] public float sideMargin = 0.16f;
        public float gridPadding = 0.03f;

        [Header("Zoom Settings")]
        public float zoomSensitivity = 0.05f;
        public float minZoom = 4f;
        public float maxZoom = 10f;

        private CinemachineCamera _cmCamera;
        private IEventBus _eventBus;
        private Vector2 _lastScreenSize;

        private void Awake()
        {
            _cmCamera = GetComponent<CinemachineCamera>();
        }

        private void OnEnable()
        {
            if (panAction != null) panAction.action.Enable();
            if (zoomAction != null) zoomAction.action.Enable();

            if (panAction != null) panAction.action.performed += OnPanPerformed;
            if (zoomAction != null) zoomAction.action.performed += OnZoomPerformed;

            if (ServiceLocator.TryGetService<IEventBus>(out _eventBus))
            {
                _eventBus.Subscribe<GamePhaseChangedEvent>(OnPhaseChanged);
            }
        }

        private void OnDisable()
        {
            if (panAction != null) panAction.action.performed -= OnPanPerformed;
            if (zoomAction != null) zoomAction.action.performed -= OnZoomPerformed;
            
            _eventBus?.Unsubscribe<GamePhaseChangedEvent>(OnPhaseChanged);
        }

        private void Update()
        {
            if (Screen.width != _lastScreenSize.x || Screen.height != _lastScreenSize.y)
            {
                _lastScreenSize = new Vector2(Screen.width, Screen.height);
                FitGridToScreen();
            }
        }

        private void OnPhaseChanged(GamePhaseChangedEvent evt) => FitGridToScreen();

        private void OnPanPerformed(InputAction.CallbackContext context)
        {
            if (Mouse.current?.middleButton.isPressed == true || (Touchscreen.current != null && Touchscreen.current.touches.Count == 1))
            {
                Vector2 delta = context.ReadValue<Vector2>();
                // Invert delta for "grab and pull" feel
                Vector3 worldDelta = new Vector3(-delta.x, -delta.y, 0);
                
                float viewportHeight = GetActiveCamera()?.pixelHeight ?? Screen.height;
                float sensitivity = _cmCamera.Lens.OrthographicSize / viewportHeight * 2f;
                transform.position += worldDelta * sensitivity;
            }
        }

        private void OnZoomPerformed(InputAction.CallbackContext context)
        {
            float scroll = context.ReadValue<Vector2>().y;
            if (Mathf.Abs(scroll) < 0.01f) return;

            var lens = _cmCamera.Lens;
            lens.OrthographicSize = Mathf.Clamp(lens.OrthographicSize - (scroll * zoomSensitivity), minZoom, maxZoom);
            _cmCamera.Lens = lens;
        }

        [ContextMenu("Fit Grid To Screen")]
        public void FitGridToScreen()
        {
            if (_cmCamera == null) _cmCamera = GetComponent<CinemachineCamera>();

            Camera cam = GetActiveCamera();
            float aspect = cam != null ? cam.aspect : (float)Screen.width / Screen.height;

            float boardWorldWidth = gridDimensions.x * tileSize;
            float boardWorldHeight = gridDimensions.y * tileSize;

            float availableWidthRatio = 1f - resourcePanelWidth - (sideMargin * 2f) - (gridPadding * 2f);
            float availableHeightRatio = 1f - headerHeight - cardPanelHeight - (gridPadding * 2f);

            float orthoSizeForHeight = (boardWorldHeight / 2f) / availableHeightRatio;
            float orthoSizeForWidth = (boardWorldWidth / 2f) / (availableWidthRatio * aspect);

            var lens = _cmCamera.Lens;
            lens.OrthographicSize = Mathf.Max(orthoSizeForHeight, orthoSizeForWidth);
            lens.ModeOverride = LensSettings.OverrideModes.Orthographic;
            _cmCamera.Lens = lens;

            float gridCenterX = (gridDimensions.x - 1) * tileSize / 2f;
            float gridCenterY = (gridDimensions.y - 1) * tileSize / 2f;
            
            float horizontalOffset = (0.5f - (1f - resourcePanelWidth) / 2f) * (lens.OrthographicSize * 2f * aspect);
            float verticalOffset = (0.5f - (cardPanelHeight + availableHeightRatio / 2f + gridPadding)) * (lens.OrthographicSize * 2f);

            transform.position = new Vector3(gridCenterX + horizontalOffset, gridCenterY + verticalOffset, -10f);
        }

        private Camera GetActiveCamera()
        {
            var brain = Object.FindFirstObjectByType<CinemachineBrain>();
            if (brain != null && brain.OutputCamera != null) return brain.OutputCamera;
            return Camera.main;
        }
    }
}
