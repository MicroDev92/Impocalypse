using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using _Project.Scripts.Utility;
using Cards;

/// <summary>
/// Represents a single card in the draft phase.
/// Handles user interaction and visual state.
/// Supports both territory cards (coordinate + resource) and city cards (strength).
/// </summary>
public class Card : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private Button button;
    [SerializeField] private Image background;
    [SerializeField] private Image resourceIcon;
    [SerializeField] private TextMeshProUGUI resourceLabel;

    [Header("Resource Sprites")]
    [SerializeField] private Sprite woodResourceSprite;
    [SerializeField] private Sprite carrotResourceSprite;
    [SerializeField] private Sprite fishResourceSprite;

    [Header("Visual Settings")]
    [SerializeField] private Color normalColor = new Color(0.85f, 0.80f, 0.70f, 1f);
    [SerializeField] private Color highlightColor = new Color(1.0f, 0.85f, 0.40f, 1f);
    [SerializeField] private Color confirmedColor = new Color(0.45f, 0.78f, 0.45f, 1f);
    [SerializeField] private Color disabledColor = new Color(0.40f, 0.38f, 0.36f, 0.7f);
    [SerializeField] private Color cityCardColor = new Color(0.70f, 0.75f, 0.90f, 1f);

    [Header("Hover Settings")]
    [SerializeField] private float hoverScale = 1.2f;
    [SerializeField] private float hoverLift = 20f;
    [SerializeField] private float hoverLerpSpeed = 12f;

    public string coordinate;
    public CardData cardData;

    private bool _isHighlighted;
    private bool _isConfirmed;
    private bool _isDisabled;
    private float _currentScale = 1f;
    private float _targetScale = 1f;
    
    private Vector2 _restPosition;
    private Quaternion _restRotation = Quaternion.identity;
    private bool _hasReceivedInitialPosition;
    private int _restSiblingIndex = -1;
    private RectTransform _rectTransform;
    private TileData _linkedTile;
    private ICardSelectionListener _selectionListener;
    private Outline _outline;

    public bool IsHighlighted => _isHighlighted;
    public bool IsConfirmed => _isConfirmed;
    public bool IsDisabled => _isDisabled;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnCardClicked);
        }

        _outline = GetComponent<Outline>();
    }

    private void Start()
    {
        if (label == null || button == null)
        {
            Debug.LogError("[Card] Missing label or button reference");
            return;
        }

        if (!string.IsNullOrEmpty(coordinate))
        {
            label.text = coordinate;
            LinkToTile();
        }
    }

    private void Update()
    {
        if (!_hasReceivedInitialPosition) return;

        // Smoothly lerp scale
        if (Mathf.Abs(_currentScale - _targetScale) > 0.001f)
        {
            _currentScale = Mathf.Lerp(_currentScale, _targetScale, Time.deltaTime * hoverLerpSpeed);
            transform.localScale = Vector3.one * _currentScale;
        }

        // Calculate target position based on rest position + hover lift
        float lift = (_currentScale - 1f) / (hoverScale - 1f) * hoverLift;
        Vector2 targetPos = _restPosition + new Vector2(0, lift);
        
        _rectTransform.anchoredPosition = Vector2.Lerp(_rectTransform.anchoredPosition, targetPos, Time.deltaTime * hoverLerpSpeed);
        _rectTransform.localRotation = Quaternion.Lerp(_rectTransform.localRotation, _restRotation, Time.deltaTime * hoverLerpSpeed);
    }

    public void SetLayoutPosition(Vector2 position, Quaternion rotation)
    {
        _restPosition = position;
        _restRotation = rotation;

        if (!_hasReceivedInitialPosition)
        {
            _hasReceivedInitialPosition = true;
            _rectTransform.anchoredPosition = position;
            _rectTransform.localRotation = rotation;
        }
    }

    /// <summary>
    /// Records the card's resting layer in the fan's draw order, so it can be
    /// restored after a hover temporarily brings it to the front.
    /// </summary>
    public void SetRestSiblingIndex(int index) => _restSiblingIndex = index;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!_isDisabled)
        {
            _targetScale = hoverScale;
            _linkedTile?.Highlight(true);
            
            // Move to front of sibling list so it's not covered
            transform.SetAsLastSibling();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _targetScale = 1f;

        // Restore the card's resting layer so hovering doesn't scramble the fan's
        // overlap order (a hovered card pops to the front, then drops back on exit).
        if (_restSiblingIndex >= 0)
            transform.SetSiblingIndex(_restSiblingIndex);

        // Only turn off highlight if the card isn't currently selected/highlighted in the selection sense
        if (!_isHighlighted)
        {
            _linkedTile?.Highlight(false);
        }
    }

    /// <summary>
    /// Initializes with a coordinate string (legacy path).
    /// </summary>
    public void Initialize(string coord, ICardSelectionListener listener = null)
    {
        coordinate = coord;
        cardData = CardData.Territory(coord);
        _selectionListener = listener;
        ResetState();

        if (label != null) label.text = coord;
        if (button != null) button.interactable = true;

        gameObject.SetActive(true);
        UpdateVisuals();
        LinkToTile();
    }

    /// <summary>
    /// Initializes with CardData (new path supporting territory and city cards).
    /// </summary>
    public void Initialize(CardData data, ICardSelectionListener listener = null)
    {
        cardData = data;
        coordinate = data.IsTerritory ? data.Coordinate : null;
        _selectionListener = listener;
        ResetState();

        if (label != null)
        {
            if (data.IsTerritory)
                label.text = data.Coordinate;
            else if (data.IsCity)
                label.text = $"City\nStr {(int)data.Strength}";
            else if (data.IsFarm)
                label.text = data.Farm == FarmKind.Market ? "Market" : $"{data.Farm}\n{data.Resource}";
            else
                label.text = data.ToString();
        }

        if (button != null) button.interactable = true;

        gameObject.SetActive(true);
        UpdateVisuals();

        if (data.IsTerritory)
            LinkToTile();
        else if (data.IsCity)
            UpdateCityDisplay();
        else
            HideResourceDisplay();
    }

    private void ResetState()
    {
        _isHighlighted = false;
        _isConfirmed = false;
        _isDisabled = false;
        _currentScale = 1f;
        _targetScale = 1f;
        transform.localScale = Vector3.one;
        _linkedTile = null;
    }

    public void Reset()
    {
        coordinate = "";
        cardData = null;
        _selectionListener = null;
        _linkedTile = null;
        ResetState();

        if (label != null) label.text = "";
        if (button != null) button.interactable = true;

        if (resourceIcon != null)
        {
            resourceIcon.sprite = null;
            resourceIcon.gameObject.SetActive(false);
        }

        if (resourceLabel != null)
        {
            resourceLabel.text = "";
            resourceLabel.gameObject.SetActive(false);
        }

        SetHighlight(false);
        UpdateVisuals();
    }

    private void OnCardClicked()
    {
        if (_isDisabled) return;

        if (!_isHighlighted)
        {
            _isHighlighted = true;
            SetHighlight(true);
            UpdateVisuals();
            _selectionListener?.OnCardSelected(this);

            // Animation feedback
            if (ServiceLocator.TryGetService<CardAnimator>(out var animator))
                animator.PlaySelectFeedback(this);
        }
        else if (!_isConfirmed)
        {
            _isConfirmed = true;
            SetHighlight(false);
            UpdateVisuals();
            _selectionListener?.OnCardConfirmed(this);

            // Animation feedback
            if (ServiceLocator.TryGetService<CardAnimator>(out var animator))
                animator.PlayConfirmFeedback(this);
        }
    }

    public void Deselect()
    {
        if (_isConfirmed) return;

        _isHighlighted = false;
        SetHighlight(false);
        UpdateVisuals();
        _selectionListener?.OnCardDeselected(this);
    }

    public void Disable()
    {
        _isDisabled = true;
        _targetScale = 1f;
        if (button != null) button.interactable = false;

        SetHighlight(false);
        UpdateVisuals();

        // Animation feedback
        if (ServiceLocator.TryGetService<CardAnimator>(out var animator))
            animator.PlayDisableFeedback(this);
    }

    public void Enable()
    {
        _isDisabled = false;
        if (button != null) button.interactable = true;
        UpdateVisuals();
    }

    private void SetHighlight(bool state)
    {
        _linkedTile?.Highlight(state);
    }

    private void LinkToTile()
    {
        if (string.IsNullOrEmpty(coordinate)) return;

        var gridService = ServiceLocator.GetService<IGridService>();
        _linkedTile = gridService?.GetTileByCoord(coordinate);

        UpdateResourceDisplay();
    }

    private void UpdateResourceDisplay()
    {
        if (_linkedTile == null)
        {
            HideResourceDisplay();
            return;
        }

        Sprite sprite = null;
        string resourceName = "";

        switch (_linkedTile.type)
        {
            case TileData.TileType.Wood:
                sprite = woodResourceSprite;
                resourceName = "Wood";
                break;
            case TileData.TileType.Carrot:
                sprite = carrotResourceSprite;
                resourceName = "Carrot";
                break;
            case TileData.TileType.Water:
            case TileData.TileType.Fish:
                sprite = fishResourceSprite;
                resourceName = "Fish";
                break;
            case TileData.TileType.Plains:
                resourceName = "Plains";
                break;
            case TileData.TileType.Mountain:
                resourceName = "Mountain";
                break;
            case TileData.TileType.Grass:
            default:
                HideResourceDisplay();
                return;
        }

        if (resourceIcon != null)
        {
            resourceIcon.gameObject.SetActive(sprite != null);
            resourceIcon.sprite = sprite;
        }

        if (resourceLabel != null)
        {
            resourceLabel.gameObject.SetActive(!string.IsNullOrEmpty(resourceName));
            resourceLabel.text = resourceName;
        }
    }

    private void UpdateCityDisplay()
    {
        HideResourceDisplay();

        if (resourceLabel != null)
        {
            resourceLabel.gameObject.SetActive(true);
            int str = cardData != null ? (int)cardData.Strength : 0;
            string towers = new string('I', str);
            resourceLabel.text = towers;
        }
    }

    private void HideResourceDisplay()
    {
        if (resourceIcon != null) resourceIcon.gameObject.SetActive(false);
        if (resourceLabel != null) resourceLabel.gameObject.SetActive(false);
    }

    private void UpdateVisuals()
    {
        if (background == null) return;

        if (_isDisabled)
        {
            background.color = disabledColor;
        }
        else if (_isConfirmed)
        {
            background.color = confirmedColor;
        }
        else if (_isHighlighted)
        {
            background.color = highlightColor;
        }
        else if (cardData != null && cardData.IsCity)
        {
            background.color = cityCardColor;
        }
        else
        {
            background.color = normalColor;
        }
    }

    public void SetSelectionListener(ICardSelectionListener listener)
    {
        _selectionListener = listener;
    }
}
