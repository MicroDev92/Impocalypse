using System.Collections.Generic;
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
                    instructionText.text = "No city cards to place. Click Done.";
                if (doneButton != null)
                {
                    doneButton.gameObject.SetActive(true);
                    doneButton.interactable = true;
                }
                return;
            }

            if (instructionText != null)
                instructionText.text = "Select a city card, then click an owned tile to place it.";

            foreach (var card in cityCards)
            {
                CreateCityCardButton(card);
            }

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

        private void CreateCityCardButton(CardData card)
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
                label.text = $"City Str {(int)card.Strength}";
            }
            
            // If it has a Card script, disable it so it doesn't fight our UI logic
            var cardScript = buttonObj.GetComponent<Card>();
            if (cardScript != null) cardScript.enabled = false;

            _cityCardButtons.Add(buttonObj);
        }

        private void OnCityCardSelected(CardData card, GameObject buttonObj)
        {
            _selectedCityCard = card;

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
                instructionText.text = $"Selected City Str {(int)card.Strength}. Click an owned tile to place.";

            HighlightValidTiles(card);
        }

        private void HighlightValidTiles(CardData cityCard)
        {
            ClearHighlights();

            int localPlayer = _networkService?.LocalPlayerId ?? -1;
            if (localPlayer < 0) return;

            var ownedTiles = _gridService?.GetOwnedTiles(localPlayer);
            if (ownedTiles == null) return;

            foreach (var tile in ownedTiles)
            {
                if (tile.CanPlaceCity(cityCard.Strength))
                {
                    tile.Highlight(true);
                    _highlightedTiles.Add(tile);
                }
            }

            Debug.Log($"[ConstructionUI] Highlighted {_highlightedTiles.Count} valid tiles for Str {(int)cityCard.Strength}");
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

            bool placed = _constructionService.TryPlaceCity(localPlayer, _selectedCityCard, tile.coord);
            if (placed)
            {
                Debug.Log($"[ConstructionUI] Placed city at {tile.coord}");

                _selectedCityCard = null;
                ClearHighlights();

                var remaining = _constructionService.GetAvailableCityCards(localPlayer);
                ClearCityCardButtons();

                if (remaining.Count > 0)
                {
                    foreach (var card in remaining)
                    {
                        CreateCityCardButton(card);
                    }
                    if (instructionText != null)
                        instructionText.text = "City placed! Select another card or click Done.";
                }
                else
                {
                    if (instructionText != null)
                        instructionText.text = "All city cards placed!";
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
