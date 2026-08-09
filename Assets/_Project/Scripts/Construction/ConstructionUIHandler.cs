using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using _Project.Scripts.Utility;
using Cards;
using Core.Events;

namespace Construction
{
    /// <summary>
    /// UI handler for the construction phase.
    /// Shows city cards at screen bottom, highlights valid tiles, handles placement.
    /// All layout is configured in the Editor — this script only instantiates
    /// CityCardButton prefabs and manages interaction logic.
    /// </summary>
    public class ConstructionUIHandler : MonoBehaviour
    {
        [Tooltip("Smallest the building row may shrink to when the stock is large. Below this buttons stay this size and the row may overflow.")]
        [Range(0.3f, 1f)]
        [SerializeField] private float minButtonScale = 0.55f;

        [Header("Card Art")]
        [Tooltip("Same art the draft hand uses, so a building looks identical in hand and while constructing.")]
        [SerializeField] private CardArtSet cardArt = new CardArtSet();

        [Header("UI References")]
        [SerializeField] private Transform cityCardContainer;
        [SerializeField] private GameObject cityCardButtonPrefab;
        [SerializeField] private Button doneButton;
        [SerializeField] private TextMeshProUGUI instructionText;

        private IConstructionService _constructionService;
        private IGridService _gridService;
        private INetworkService _networkService;
        private IEventBus _eventBus;

        private CardData _selectedCityCard;
        private string _pendingSkyCoord; // first endpoint of a sky tower (two-click placement)
        private readonly List<GameObject> _cityCardButtons = new();
        private readonly List<TileData> _highlightedTiles = new();
        private bool _isActive;

        private void Awake()
        {
            ServiceLocator.RegisterService<ConstructionUIHandler>(this);
        }

        private void Start()
        {
            ServiceLocator.TryGetService(out _constructionService);
            ServiceLocator.TryGetService(out _gridService);
            ServiceLocator.TryGetService(out _networkService);
            ServiceLocator.TryGetService(out _eventBus);

            if (doneButton != null)
            {
                doneButton.onClick.AddListener(OnDoneClicked);
            }

            Hide();
        }

        private void OnDestroy()
        {
            ClearHighlights();
            ServiceLocator.UnregisterService<ConstructionUIHandler>();
        }

        public void Show(IReadOnlyList<CardData> cityCards)
        {
            Debug.Log($"[ConstructionUI] Showing with {cityCards?.Count ?? 0} cards");
            _isActive = true;
            _selectedCityCard = null;
            SetChildrenActive(true);

            ClearCityCardButtons();
            ClearHighlights();

            if (cityCards == null || cityCards.Count == 0)
            {
                Debug.LogWarning("[ConstructionUI] No city cards provided to Show()");
                if (instructionText != null)
                    instructionText.text = "Nothing to build. Click Done.";
                if (doneButton != null)
                {
                    doneButton.gameObject.SetActive(true);
                    doneButton.interactable = true;
                }
                return;
            }

            if (instructionText != null)
                instructionText.text = "Select a building, then click a highlighted tile to place it.";

            BuildButtons(cityCards);

            if (doneButton != null)
            {
                doneButton.gameObject.SetActive(true);
                doneButton.interactable = true;
            }
        }

        public void Hide()
        {
            _isActive = false;
            _selectedCityCard = null;
            _pendingSkyCoord = null;
            ClearCityCardButtons();
            ClearHighlights();
            SetChildrenActive(false);
        }

        private void SetChildrenActive(bool active)
        {
            foreach (Transform child in transform)
            {
                child.gameObject.SetActive(active);
            }
        }

        /// <summary>
        /// Creates one button per DISTINCT card, stacking duplicates with a ×N count and
        /// sorting by category (Cities, Farms, Markets, Camps, Sky Towers). Bounded distinct
        /// types mean the row can never overflow.
        /// </summary>
        private void BuildButtons(IReadOnlyList<CardData> cards)
        {
            ClearCityCardButtons();
            if (cards == null) return;

            var groups = cards
                .GroupBy(StackKey)
                .OrderBy(g => SortIndex(g.First()))
                .ThenBy(g => (int)g.First().Strength)
                .ThenBy(g => g.First().Amount);

            foreach (var group in groups)
                CreateCityCardButton(group.First(), group.Count());

            // Measure only once every button exists, and only after Unity has laid
            // them out - reading rect sizes before the rebuild returns stale values.
            if (cityCardContainer is RectTransform containerRect)
                LayoutRebuilder.ForceRebuildLayoutImmediate(containerRect);
            FitButtonsToContainer();
        }

        /// <summary>Gameplay identity ignoring unique ids - identical cards share a stack.</summary>
        private static string StackKey(CardData c)
        {
            if (c.IsCity) return $"C{(int)c.Strength}";
            if (c.IsCamp) return $"K{c.Amount}";      // priorities are distinct on purpose
            if (c.IsSkyTower) return "S";
            if (c.IsFarm)
            {
                return c.Farm switch
                {
                    FarmKind.Market => "FM",
                    FarmKind.Special => $"FS{(int)c.Resource}:{(int)c.AllowedTerrain}",
                    _ => $"FC{(int)c.Resource}"
                };
            }
            return c.Serialize();
        }

        private static int SortIndex(CardData c)
        {
            if (c.IsCity) return 0;
            if (c.IsFarm)
                return c.Farm switch { FarmKind.Common => 1, FarmKind.Special => 2, _ => 3 };
            if (c.IsCamp) return 4;
            if (c.IsSkyTower) return 5;
            return 6;
        }

        private void CreateCityCardButton(CardData card, int stackCount = 1)
        {
            if (cityCardContainer == null || cityCardButtonPrefab == null)
            {
                Debug.LogWarning("[ConstructionUI] Missing cityCardContainer or cityCardButtonPrefab reference");
                return;
            }

            var buttonObj = Instantiate(cityCardButtonPrefab, cityCardContainer);
            
            // If it's a 'Card' prefab, it might have a Card script that wipes listeners in Awake.
            // We'll get the button and add our listener, ensuring it's not the first thing in Awake.
            var btn = buttonObj.GetComponent<Button>();
            if (btn != null)
            {
                // Delay slightly or just clear then add to be sure we are the ones in control
                var capturedCard = card;
                var capturedObj = buttonObj;
                btn.onClick.RemoveAllListeners(); 
                btn.onClick.AddListener(() => OnCityCardSelected(capturedCard, capturedObj));
            }

            var label = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.text = stackCount > 1
                    ? $"{BuildingLabel(card)}  ×{stackCount}"
                    : BuildingLabel(card);
            }
            
            // If it has a Card script, disable it so it doesn't fight our UI logic
            var cardScript = buttonObj.GetComponent<Card>();
            if (cardScript != null) cardScript.enabled = false;

            // ...but the disabled Card script also means nothing fills its thumbnail,
            // so set the art here from the shared set. Buildings look the same while
            // constructing as they did in hand.
            ApplyButtonArt(buttonObj, card);

            _cityCardButtons.Add(buttonObj);
        }

        /// <summary>
        /// Shrinks the whole button row so a large building stock stays on screen,
        /// the same auto-fit rule the draft hand uses. A player who drafted ten
        /// buildings could otherwise push the last ones off the edge - and a
        /// building you cannot click is a building you cannot place.
        /// </summary>
        private void FitButtonsToContainer()
        {
            if (cityCardContainer is not RectTransform containerRect) return;

            int count = _cityCardButtons.Count;
            if (count == 0)
            {
                containerRect.localScale = Vector3.one;
                return;
            }

            float available = containerRect.rect.width;
            if (available <= 1f) return; // layout not built yet

            float buttonWidth = 150f;
            var firstRect = _cityCardButtons[0].GetComponent<RectTransform>();
            if (firstRect != null && firstRect.rect.width > 1f) buttonWidth = firstRect.rect.width;

            float spacing = 8f;
            if (cityCardContainer.TryGetComponent<HorizontalLayoutGroup>(out var hlg))
            {
                spacing = hlg.spacing + hlg.padding.left + hlg.padding.right;

                // Centre the row like the draft hand does, rather than letting it
                // pile up against one edge as the buttons shrink.
                hlg.childAlignment = TextAnchor.MiddleCenter;
            }

            float needed = count * buttonWidth + (count - 1) * spacing;
            float scale = needed > available ? available / needed : 1f;
            scale = Mathf.Clamp(scale, minButtonScale, 1f);

            // Scaling collapses toward the pivot, so a non-centred pivot would drag
            // the row sideways as it shrinks. Force a centred pivot without moving
            // the rect: shifting the pivot needs an equal compensating shift.
            if (!Mathf.Approximately(containerRect.pivot.x, 0.5f))
            {
                float deltaPivot = 0.5f - containerRect.pivot.x;
                containerRect.pivot = new Vector2(0.5f, containerRect.pivot.y);
                containerRect.anchoredPosition += new Vector2(deltaPivot * containerRect.rect.width, 0f);
            }

            containerRect.localScale = new Vector3(scale, scale, 1f);
        }

        /// <summary>
        /// Fills the button's "Thumbnail" image with the building's art. Falls back
        /// to hiding it when the art is missing, so a button never shows a white box.
        /// </summary>
        private void ApplyButtonArt(GameObject buttonObj, CardData card)
        {
            Transform thumbTransform = buttonObj.transform.Find("Thumbnail");
            if (thumbTransform == null) return;

            var thumb = thumbTransform.GetComponent<Image>();
            if (thumb == null) return;

            var art = cardArt.SpriteFor(card);
            thumb.sprite = art;
            thumb.preserveAspect = true;
            thumb.enabled = art != null;
        }

        private void OnCityCardSelected(CardData card, GameObject buttonObj)
        {
            _selectedCityCard = card;
            _pendingSkyCoord = null;

            // Reset all button colors
            foreach (var btn in _cityCardButtons)
            {
                var img = btn.GetComponent<Image>();
                if (img != null) img.color = new Color(0.70f, 0.75f, 0.90f, 1f);
            }

            // Highlight selected
            var selectedImg = buttonObj.GetComponent<Image>();
            if (selectedImg != null) selectedImg.color = new Color(1f, 0.85f, 0.40f, 1f);

            if (instructionText != null)
                instructionText.text = $"Selected {BuildingLabel(card)}. Click a highlighted tile to place.";

            HighlightValidTiles(card);
        }

        private void HighlightValidTiles(CardData card)
        {
            ClearHighlights();

            int localPlayer = _networkService?.LocalPlayerId ?? -1;
            if (localPlayer < 0) return;

            // Camps target EMPTY tiles; everything else targets your own tiles.
            IEnumerable<TileData> candidates;
            if (card.IsCamp)
            {
                var coords = _gridService?.GetAllPlayableCoords();
                if (coords == null) return;
                candidates = coords.Select(c => _gridService.GetTileByCoord(c)).Where(t => t != null);
            }
            else
            {
                var owned = _gridService?.GetOwnedTiles(localPlayer);
                if (owned == null) return;
                candidates = owned;
            }

            foreach (var tile in candidates)
            {
                if (CanPlace(tile, card))
                {
                    tile.Highlight(true);
                    _highlightedTiles.Add(tile);
                }
            }

            Debug.Log($"[ConstructionUI] Highlighted {_highlightedTiles.Count} valid tiles for {BuildingLabel(card)}");
        }

        /// <summary>Whether the given building card can be placed on this tile.</summary>
        private static bool CanPlace(TileData tile, CardData card)
        {
            if (card.IsCity) return tile.CanPlaceCity(card.Strength);
            if (card.IsCamp) return !tile.IsOwned;
            if (card.IsSkyTower) return tile.CanPlaceSkyTower();
            if (card.IsFarm)
            {
                if (card.Farm == FarmKind.Special) return tile.CanPlaceSpecialFarm(card.AllowedTerrain);
                return tile.CanPlaceCommonResource(); // common farm or market
            }
            return false;
        }

        /// <summary>Short label describing a building card.</summary>
        private static string BuildingLabel(CardData card)
        {
            if (card.IsCity) return $"{GameLexicon.CityName} Str {(int)card.Strength}";
            if (card.IsCamp) return $"{GameLexicon.CampName} #{card.Amount} (claim empty tile)";
            if (card.IsSkyTower) return $"{GameLexicon.SkyTowerName} (link 2 domains)";
            if (card.IsFarm)
            {
                return card.Farm switch
                {
                    FarmKind.Market => $"{GameLexicon.MarketName} (any resource)",
                    FarmKind.Special => $"Luxury: {GameLexicon.Resource(card.Resource)} (on {GameLexicon.TerrainShort(card.AllowedTerrain)})",
                    _ => $"Farm: +{GameLexicon.Resource(card.Resource)}"
                };
            }
            return "Building";
        }

        private void ClearHighlights()
        {
            foreach (var tile in _highlightedTiles)
            {
                if (tile != null) tile.Highlight(false);
            }
            _highlightedTiles.Clear();
        }

        private void Update()
        {
            if (!_isActive || _selectedCityCard == null) return;

            var mouse = Mouse.current;
            if (mouse == null) return;

            if (mouse.leftButton.wasPressedThisFrame)
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                    return;

                Vector2 screenPos = mouse.position.ReadValue();
                Vector2 worldPos = Camera.main.ScreenToWorldPoint(screenPos);

                var hit = Physics2D.Raycast(worldPos, Vector2.zero);

                TileData tile = null;

                if (hit.collider != null)
                {
                    tile = hit.collider.GetComponent<TileData>();
                }

                if (tile == null)
                {
                    tile = FindTileAtWorldPosition(worldPos);
                }

                if (tile != null)
                {
                    TryPlaceOnTile(tile);
                }
                else
                {
                    Debug.Log($"[ConstructionUI] No tile found at world pos {worldPos}");
                }
            }
        }

        private TileData FindTileAtWorldPosition(Vector2 worldPos)
        {
            int gridX = Mathf.RoundToInt(worldPos.x);
            int gridY = Mathf.RoundToInt(worldPos.y);

            return _gridService?.GetTileByPosition(new Vector2Int(gridX, gridY));
        }

        private void TryPlaceOnTile(TileData tile)
        {
            if (_selectedCityCard == null || _constructionService == null) return;

            int localPlayer = _networkService?.LocalPlayerId ?? -1;
            if (localPlayer < 0) return;

            // Sky towers take two clicks: first endpoint, then the second.
            if (_selectedCityCard.IsSkyTower && _pendingSkyCoord == null)
            {
                if (!tile.IsOwnedBy(localPlayer) || tile.HasSkyLink)
                {
                    Debug.Log($"[ConstructionUI] {tile.coord} is not a valid sky tower endpoint");
                    return;
                }
                _pendingSkyCoord = tile.coord;
                if (instructionText != null)
                    instructionText.text = $"First gate on {tile.coord}. Click a tile in a DIFFERENT domain for the second.";
                return;
            }

            bool placed = _selectedCityCard.IsSkyTower
                ? _constructionService.TryPlaceSkyTower(localPlayer, _selectedCityCard, _pendingSkyCoord, tile.coord)
                : _constructionService.TryPlaceCity(localPlayer, _selectedCityCard, tile.coord);
            if (_selectedCityCard.IsSkyTower && !placed)
            {
                _pendingSkyCoord = null;
                if (instructionText != null)
                    instructionText.text = $"Invalid second endpoint - {GameLexicon.SkyTowerName} reset. Click the first tile again.";
                return;
            }
            _pendingSkyCoord = null;
            if (placed)
            {
                Debug.Log($"[ConstructionUI] Placed city at {tile.coord}");

                _selectedCityCard = null;
                ClearHighlights();

                var remaining = _constructionService.GetAvailableCityCards(localPlayer);
                BuildButtons(remaining);

                if (remaining.Count > 0)
                {
                    if (instructionText != null)
                        instructionText.text = "Placed! Select another card or click Done.";
                }
                else
                {
                    if (instructionText != null)
                        instructionText.text = "Everything built!";
                }
            }
            else
            {
                Debug.Log($"[ConstructionUI] Cannot place city at {tile.coord}");
            }
        }

        private void OnDoneClicked()
        {
            int localPlayer = _networkService?.LocalPlayerId ?? -1;
            if (localPlayer >= 0)
            {
                _constructionService?.MarkPlayerDone(localPlayer);
            }

            if (doneButton != null) doneButton.interactable = false;
            if (instructionText != null) instructionText.text = "Waiting for other players...";

            ClearHighlights();
        }

        private void ClearCityCardButtons()
        {
            foreach (var btn in _cityCardButtons)
            {
                if (btn != null) Destroy(btn);
            }
            _cityCardButtons.Clear();
        }
    }
}
