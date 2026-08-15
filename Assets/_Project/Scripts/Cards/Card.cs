using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using _Project.Scripts.Utility;
using Cards;
using Core.Events;

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
    [Tooltip("Cards you cannot pick right now because the quota is full. Dimmed but still hoverable - you still need to read them to decide whether to swap a pick.")]
    [SerializeField] private Color unavailableColor = new Color(0.45f, 0.42f, 0.40f, 0.75f);

    [Header("Refused Feedback")]
    [SerializeField] private float shakeDistance = 10f;
    [SerializeField] private float shakeDuration = 0.25f;

    [Tooltip("Smallest the card label may auto-shrink to. The hand layout also scales cards down, so this is the real readability floor.")]
    [SerializeField] private float minLabelFontSize = 20f;

    [Header("Card Art")]
    [Tooltip("Large art panel on the card face. Territory cards mirror their tile's terrain; the art set below covers everything else.")]
    [SerializeField] private Image thumbnail;
    [SerializeField] private CardArtSet cardArt = new CardArtSet();

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

    /// <summary>
    /// Scale the card returns to when not hovered. The hand layout sets this so a
    /// large hand can shrink its cards to fit; hover multiplies it rather than
    /// replacing it, so hovering a shrunken card still pops it proportionally.
    /// </summary>
    private float _restScale = 1f;

    private Vector2 _restPosition;
    private Quaternion _restRotation = Quaternion.identity;
    private bool _hasReceivedInitialPosition;
    private bool _isHovered;
    private bool _isUnavailable;
    private Vector2 _shakeOffset;
    private Coroutine _shakeRoutine;
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

        // Card text varies (coordinates vs pact rules), so TMP may shrink it to fit -
        // but never below a readable floor. The old 8pt minimum let long labels
        // collapse to an illegible smudge once the hand layout also scaled the card
        // down; text that can't be read is worse than text that wraps.
        if (label != null)
        {
            label.enableAutoSizing = true;
            label.fontSizeMax = label.fontSize;
            label.fontSizeMin = minLabelFontSize;
            label.textWrappingMode = TextWrappingModes.Normal;
        }
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
            _currentScale = Mathf.Lerp(_currentScale, _targetScale, Time.deltaTime * hoverLerpSpeed);

        // Enforce every frame, not only while lerping: other systems (pooling,
        // visual resets) set localScale back to 1, which used to desync the card's
        // displayed size from _currentScale - hovering then "shrank" the card
        // instead of popping it, and it stayed shrunk.
        transform.localScale = Vector3.one * _currentScale;

        // Calculate target position based on rest position + hover lift
        float hoverProgress = hoverScale > 1f
            ? Mathf.Clamp01((_currentScale / _restScale - 1f) / (hoverScale - 1f))
            : 0f;
        float lift = hoverProgress * hoverLift;
        Vector2 targetPos = _restPosition + new Vector2(0, lift) + _shakeOffset;
        
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
    /// Sets the card's resting scale (the hand layout shrinks cards when the hand
    /// is large). Hover scales relative to this, so the pop stays proportional.
    /// </summary>
    public void SetLayoutScale(float scale)
    {
        _restScale = Mathf.Max(0.05f, scale);

        if (_isHovered)
        {
            _targetScale = _restScale * hoverScale;
            return;
        }

        // Snap rather than lerp: a re-layout (new hand dealt, card picked) should
        // land at the right size immediately, not visibly animate from the old one.
        _targetScale = _restScale;
        _currentScale = _restScale;
        transform.localScale = Vector3.one * _restScale;
    }

    /// <summary>
    /// Records the card's resting layer in the fan's draw order, so it can be
    /// restored after a hover temporarily brings it to the front.
    /// </summary>
    public void SetRestSiblingIndex(int index) => _restSiblingIndex = index;

    /// <summary>
    /// Marks the card as un-pickable because the quota is full. Deliberately NOT
    /// the same as Disable(): the card stays interactive so it can still be hovered
    /// and read in the inspector - you need to read cards you cannot take in order
    /// to decide whether to swap one of your picks.
    /// </summary>
    public void SetUnavailable(bool unavailable)
    {
        if (_isUnavailable == unavailable) return;
        _isUnavailable = unavailable;
        UpdateVisuals();
    }

    /// <summary>Wobble to signal a refused click (the wrong-password idiom).</summary>
    private void Shake()
    {
        if (!gameObject.activeInHierarchy) return;
        if (_shakeRoutine != null) StopCoroutine(_shakeRoutine);
        _shakeRoutine = StartCoroutine(ShakeRoutine());
    }

    private System.Collections.IEnumerator ShakeRoutine()
    {
        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / shakeDuration;
            // Decaying oscillation, so it settles rather than stopping dead.
            float damping = 1f - progress;
            _shakeOffset = new Vector2(
                Mathf.Sin(progress * Mathf.PI * 6f) * shakeDistance * damping, 0f);
            yield return null;
        }

        _shakeOffset = Vector2.zero;
        _shakeRoutine = null;
    }

    /// <summary>
    /// Reading a card is always allowed; only PICKING one is gated, and that is
    /// enforced in OnCardClicked and on the Button itself.
    ///
    /// This whole method used to sit behind <c>if (!_isDisabled)</c>, which is why
    /// Construction had no hover at all, and why the unpickable cards went unreadable
    /// in the draft at exactly the moment you need to read them to decide whether to
    /// swap a pick - contradicting the tooltip on the field that dims them.
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovered = true;
        _targetScale = _restScale * hoverScale;
        _linkedTile?.Highlight(true);

        // Cards are too small to carry their own rules text - hovering sends
        // them to the inspector panel, which has room to explain them.
        if (cardData != null && ServiceLocator.TryGetService<IEventBus>(out var bus))
            bus.Publish(new CardInspectedEvent(cardData, thumbnail != null ? thumbnail.sprite : null));

        // Move to front of sibling list so it's not covered
        transform.SetAsLastSibling();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovered = false;
        _targetScale = _restScale;

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
                label.text = $"Str {(int)data.Strength}"; // art already says "citadel"
            else if (data.IsFarm)
                label.text = FarmCardLabel(data);
            // Labels stay short: the thumbnail identifies the card, so text is a
            // caption rather than a rules explanation (which no longer fits at
            // twelve-cards-across sizes). Pacts are the exception - their rule IS
            // their content and no art can convey it.
            else if (data.IsCamp)
                label.text = $"{GameLexicon.CampName} #{data.Amount}";
            else if (data.IsSkyTower)
                label.text = GameLexicon.SkyTowerName;
            else if (data.IsProvisions)
                label.text = "Provisions";
            else if (data.IsParchment)
                label.text = ParchmentCardLabel(data);
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

        // After LinkToTile, so a territory card can read its tile's terrain art.
        UpdateThumbnail();
    }

    /// <summary>
    /// Chooses the card's art. Territory cards mirror their actual tile sprite,
    /// so card and board can never disagree (and new terrain art needs no card
    /// changes); other card types use the sprites assigned on the prefab.
    /// </summary>
    private void UpdateThumbnail()
    {
        if (thumbnail == null) return;

        // Territory cards mirror their own tile; everything else comes from the
        // shared art set, so hand and construction buttons can't disagree.
        Sprite art = cardData != null && cardData.IsTerritory
            ? (_linkedTile != null ? _linkedTile.TerrainSprite : null)
            : cardArt.SpriteFor(cardData);

        thumbnail.sprite = art;
        thumbnail.preserveAspect = true;
        thumbnail.enabled = art != null;
    }

    /// <summary>Hand-card label for a farm: says what it adds and where it can go.</summary>
    private static string FarmCardLabel(CardData data)
    {
        return data.Farm switch
        {
            FarmKind.Market => GameLexicon.MarketName,
            FarmKind.Special => $"{GameLexicon.Resource(data.Resource)}\non {GameLexicon.TerrainShort(data.AllowedTerrain)}",
            _ => GameLexicon.Resource(data.Resource) // art says "farm"; the inspector says "adds"
        };
    }

    /// <summary>Hand-card label for a parchment: states its rule, resource, and points.</summary>
    private static string ParchmentCardLabel(CardData data)
    {
        return data.Rule switch
        {
            Cards.ParchmentRule.Treasure => $"Treasure\n+{data.Amount}",
            Cards.ParchmentRule.PerResource => $"Pact\n+{data.Amount}/{GameLexicon.Resource(data.Resource)}",
            Cards.ParchmentRule.PerFief => $"Pact\n+{data.Amount}/domain",
            Cards.ParchmentRule.LargestFief => $"Pact\n+{data.Amount}/tile\nbiggest domain",
            Cards.ParchmentRule.PerCity => $"Pact\n+{data.Amount}/citadel",
            _ => "Pact"
        };
    }

    private void ResetState()
    {
        _isHighlighted = false;
        _isConfirmed = false;
        _isDisabled = false;
        _currentScale = 1f;
        _targetScale = 1f;
        transform.localScale = Vector3.one;
        _restScale = 1f;
        _isHovered = false;
        _isUnavailable = false;
        _shakeOffset = Vector2.zero;
        _linkedTile = null;
    }

    public void Reset()
    {
        coordinate = "";
        cardData = null;
        _selectionListener = null;

        // Turn the tile's highlight off BEFORE letting go of the tile. ResetState()
        // nulls _linkedTile, so the SetHighlight below it had nothing left to switch
        // off - which is how a hovered tile stayed lit on the board after the draft
        // timer took the cards away.
        SetHighlight(false);

        _linkedTile = null;
        ResetState();

        if (label != null) label.text = "";
        if (button != null) button.interactable = true;
        if (thumbnail != null) { thumbnail.sprite = null; thumbnail.enabled = false; }

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

        // (Highlight is cleared at the top, while we still hold the tile.)
        UpdateVisuals();
    }

    private void OnCardClicked()
    {
        if (_isDisabled) return;

        // One click picks, one click releases. The old intermediate "selected"
        // state existed only because picks auto-submitted on the second click;
        // with an explicit Confirm button it was a third visual language for
        // nothing. States are now: normal, picked, unavailable.
        if (_isConfirmed)
        {
            // Ask first, repaint second. Flipping our own state before the handler had
            // a say meant a locked selection could still be visually un-picked: the
            // model correctly kept the pick and played it, while the card sat there
            // showing it released.
            if (_selectionListener != null && !_selectionListener.OnCardUnconfirmed(this))
                return;

            _isConfirmed = false;
            _isHighlighted = false;
            SetHighlight(false);
            UpdateVisuals();
        }
        else
        {
            // Ask first: the handler refuses once the quota is full, and a refused
            // card must not paint itself as picked. Shake instead of doing nothing -
            // a silent click reads as a broken game.
            if (_selectionListener != null && !_selectionListener.OnCardConfirmed(this))
            {
                Shake();
                return;
            }

            _isConfirmed = true;
            _isHighlighted = false;
            SetHighlight(false);
            UpdateVisuals();
            _selectionListener?.OnCardSelected(this);

            // Animation feedback
            if (ServiceLocator.TryGetService<CardAnimator>(out var animator))
                animator.PlayConfirmFeedback(this);
        }
    }

    /// <summary>
    /// Paints this card as picked without a click having happened. Used when the pick
    /// timer fills a player's remaining slots for them: those cards really were
    /// submitted, so leaving them looking untouched would have the hand contradict
    /// what was actually sent.
    ///
    /// View only - the handler has already accepted the pick by the time this is called.
    /// </summary>
    public void ShowAsPicked()
    {
        if (_isConfirmed) return;

        _isConfirmed = true;
        _isHighlighted = false;
        SetHighlight(false);
        UpdateVisuals();
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

        // _restScale, not a hardcoded 1: the hand layout shrinks cards to fit, so 1
        // disagreed with OnPointerExit (which resets to _restScale) and the card
        // visibly changed size as the pointer crossed it - the "hovering shrinks the
        // other cards" from playtest 1.
        _targetScale = _isHovered ? _restScale * hoverScale : _restScale;

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

        // The link exists for tile highlighting on hover; the card's own face is
        // thumbnail + coordinate only. The old resource icon/name row is gone -
        // it duplicated what the terrain art already shows and collided with the
        // coordinate in the same strip. Exact resource lives in the tile inspector.
        HideResourceDisplay();
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
                resourceName = GameLexicon.Resource(Cards.ResourceType.Wood);
                break;
            case TileData.TileType.Carrot:
                sprite = carrotResourceSprite;
                resourceName = GameLexicon.Resource(Cards.ResourceType.Carrot);
                break;
            case TileData.TileType.Fish:
            case TileData.TileType.Water:
                // The Styx carries Souls
                sprite = fishResourceSprite;
                resourceName = GameLexicon.Resource(Cards.ResourceType.Fish);
                break;
            case TileData.TileType.Plains:
                resourceName = GameLexicon.TerrainShort(TileData.TileType.Plains);
                break;
            case TileData.TileType.Mountain:
                resourceName = GameLexicon.TerrainShort(TileData.TileType.Mountain);
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
        // Citadel art + "Str N" is the whole card now; the tower-marks row was a
        // third way of saying the same thing and collided with the label.
        HideResourceDisplay();
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
        else if (_isUnavailable)
        {
            background.color = unavailableColor;
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
