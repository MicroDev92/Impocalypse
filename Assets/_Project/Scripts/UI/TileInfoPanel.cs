using UnityEngine;
using UnityEngine.UI;
using TMPro;
using _Project.Scripts.Utility;
using Config;
using Core.Events;
using Players;

/// <summary>
/// Right sidebar panel that shows details about a selected tile.
/// Listens for tile clicks (TileHighlightedEvent) and displays:
///   terrain type, coordinate, owner name, city strength, resource type.
///
/// Hierarchy expected:
///   TileInfoPanel
///   ├── HeaderText           ("Territory Info")
///   ├── TilePreviewPanel
///   │   ├── TileImage        (Image — terrain sprite)
///   │   └── TileTypeName     (TMP_Text — "Mountain", "Wood", etc.)
///   ├── StatsContainer
///   │   ├── CoordRow         (label + value)
///   │   ├── OwnerRow         (label + value)
///   │   ├── ResourceRow      (label + value)
///   │   └── CityRow          (label + value)
///   └── EmptyStateText       ("Click a tile to inspect")
/// </summary>
public class TileInfoPanel : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Panel title. Switches between tile and card wording so the header never contradicts what's shown.")]
    [SerializeField] private TMP_Text headerText;
    [SerializeField] private GameObject contentPanel;
    [SerializeField] private GameObject emptyStatePanel;
    [SerializeField] private TMP_Text emptyStateText;

    [Header("Tile Preview")]
    [SerializeField] private Image tilePreviewImage;
    [SerializeField] private TMP_Text tileTypeName;

    [Header("Stat Values")]
    [SerializeField] private TMP_Text coordValue;
    [SerializeField] private TMP_Text ownerValue;
    [SerializeField] private TMP_Text resourceValue;
    [SerializeField] private TMP_Text cityValue;
    [SerializeField] private GameObject cityRow;
    [Tooltip("Container holding the tile stat rows. Hidden while inspecting a card, whose stats don't apply.")]
    [SerializeField] private GameObject statsContainer;

    [Header("Description")]
    [Tooltip("Free text under the stats: what a card does, or what claiming a tile means. This is where the rules explanation lives - cards in hand are too small for it.")]
    [SerializeField] private TMP_Text descriptionText;

    [Header("Configuration")]
    [SerializeField] private GameTheme theme;
    [SerializeField] private PlayerColorPalette colorPalette;

    private IEventBus _eventBus;
    private string _selectedCoord;

    private void Start()
    {
        if (ServiceLocator.TryGetService<IEventBus>(out _eventBus))
        {
            _eventBus.Subscribe<TileHighlightedEvent>(OnTileHighlighted);
            _eventBus.Subscribe<TileOwnerChangedEvent>(OnTileOwnerChanged);
            _eventBus.Subscribe<CityPlacedEvent>(OnCityPlaced);
            _eventBus.Subscribe<CardInspectedEvent>(OnCardInspected);
            _eventBus.Subscribe<GamePhaseChangedEvent>(OnPhaseChanged);
        }

        ShowEmptyState();
    }

    private void OnDestroy()
    {
        if (_eventBus != null)
        {
            _eventBus.Unsubscribe<TileHighlightedEvent>(OnTileHighlighted);
            _eventBus.Unsubscribe<TileOwnerChangedEvent>(OnTileOwnerChanged);
            _eventBus.Unsubscribe<CityPlacedEvent>(OnCityPlaced);
            _eventBus.Unsubscribe<CardInspectedEvent>(OnCardInspected);
            _eventBus.Unsubscribe<GamePhaseChangedEvent>(OnPhaseChanged);
        }
    }

    private void OnTileHighlighted(TileHighlightedEvent evt)
    {
        if (evt.IsHighlighted)
        {
            ShowTileInfo(evt.Coordinate);
        }
    }

    private void OnTileOwnerChanged(TileOwnerChangedEvent evt)
    {
        if (evt.Coordinate == _selectedCoord)
            ShowTileInfo(_selectedCoord);
    }

    private void OnCityPlaced(CityPlacedEvent evt)
    {
        if (evt.Coordinate == _selectedCoord)
            ShowTileInfo(_selectedCoord);
    }

    /// <summary>
    /// Shows info for a specific tile coordinate.
    /// Can also be called externally (e.g. from a tile click handler).
    /// </summary>
    public void ShowTileInfo(string coord)
    {
        var gridService = ServiceLocator.GetService<IGridService>();
        if (gridService == null || string.IsNullOrEmpty(coord))
        {
            ShowEmptyState();
            return;
        }

        var tile = gridService.GetTileByCoord(coord);
        if (tile == null)
        {
            ShowEmptyState();
            return;
        }

        _selectedCoord = coord;
        ShowContent();

        // Coming back from a card inspection - restore the header and stat rows.
        if (headerText != null) headerText.text = "Territory Info";
        if (statsContainer != null) statsContainer.SetActive(true);

        if (descriptionText != null)
        {
            descriptionText.text = tile.IsOwned
                ? "Tiles you own join up into Domains, which score Strength x Wealth at the end of each round."
                : "Claiming this tile adds it to your Domain and scores at the end of the round.";
            descriptionText.gameObject.SetActive(true);
        }

        // Type name
        if (tileTypeName != null)
            tileTypeName.text = GetTileTypeName(tile.type);

        // Preview image — grab sprite from the tile's SpriteRenderer
        if (tilePreviewImage != null)
        {
            var sr = tile.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                tilePreviewImage.sprite = sr.sprite;
                tilePreviewImage.color = Color.white;
                tilePreviewImage.enabled = true;
            }
            else
            {
                tilePreviewImage.enabled = false;
            }
        }

        // Coordinate
        if (coordValue != null)
            coordValue.text = coord;

        // Owner
        if (ownerValue != null)
        {
            if (tile.IsOwned)
            {
                var playerService = ServiceLocator.GetService<IPlayerService>();
                var player = playerService?.GetPlayer(tile.OwnerActorNumber);
                ownerValue.text = player?.Nickname ?? $"Player {tile.OwnerActorNumber}";

                if (colorPalette != null && player != null && !string.IsNullOrEmpty(player.ImpColor))
                    ownerValue.color = colorPalette.GetPrimaryColor(player.ImpColor);
                else if (theme != null)
                    ownerValue.color = theme.textPrimary;
            }
            else
            {
                ownerValue.text = "Unclaimed";
                if (theme != null) ownerValue.color = theme.textSecondary;
            }
        }

        // Resource (base terrain resource via the lexicon; None for barren terrain)
        if (resourceValue != null)
        {
            var baseRes = tile.BaseResource;
            resourceValue.text = baseRes != Cards.ResourceType.None
                ? GameLexicon.Resource(baseRes)
                : "None";
        }

        // Building (city / farm / market)
        if (cityRow != null)
        {
            bool hasBuilding = tile.HasBuilding;
            cityRow.SetActive(hasBuilding);

            if (hasBuilding && cityValue != null)
            {
                cityValue.text = tile.Building switch
                {
                    BuildingKind.City => $"{GameLexicon.CityName} (Str {(int)tile.BuildingStrength})",
                    BuildingKind.Market => GameLexicon.MarketName,
                    BuildingKind.SpecialFarm => $"Luxury Farm ({GameLexicon.Resource(tile.AddedResource)})",
                    BuildingKind.CommonFarm => $"Farm ({GameLexicon.Resource(tile.AddedResource)})",
                    BuildingKind.Camp => $"{GameLexicon.CampName} (provisional)",
                    _ => "—"
                };
            }
        }
    }

    private void OnCardInspected(CardInspectedEvent evt) => ShowCardInfo(evt.Card, evt.Art);

    /// <summary>
    /// A card's rules are only meaningful for the hand it came from. Without this the
    /// last card hovered during the draft stayed in the panel right through
    /// Construction, confidently describing something the player no longer holds -
    /// worse than showing nothing.
    /// </summary>
    private void OnPhaseChanged(GamePhaseChangedEvent evt) => ClearSelection();

    /// <summary>
    /// Shows a hand card's name, art and full rules. Tile stat rows are hidden -
    /// owner/resource/building don't apply to a card in hand.
    /// </summary>
    public void ShowCardInfo(Cards.CardData card, Sprite art)
    {
        if (card == null) return;

        _selectedCoord = null;
        ShowContent();

        if (headerText != null) headerText.text = "Card Info";
        if (statsContainer != null) statsContainer.SetActive(false);
        if (cityRow != null) cityRow.SetActive(false);

        if (tileTypeName != null)
            tileTypeName.text = GameLexicon.CardName(card);

        if (tilePreviewImage != null)
        {
            tilePreviewImage.sprite = art;
            tilePreviewImage.color = Color.white;
            tilePreviewImage.preserveAspect = true;
            tilePreviewImage.enabled = art != null;
        }

        if (descriptionText != null)
        {
            descriptionText.text = GameLexicon.CardDescription(card);
            descriptionText.gameObject.SetActive(true);
        }
    }

    public void ClearSelection()
    {
        _selectedCoord = null;
        ShowEmptyState();
    }

    private void ShowEmptyState()
    {
        if (contentPanel != null) contentPanel.SetActive(false);
        if (emptyStatePanel != null) emptyStatePanel.SetActive(true);
        if (emptyStateText != null) emptyStateText.text = "Click a tile or hover a card to inspect";
    }

    private void ShowContent()
    {
        if (contentPanel != null) contentPanel.SetActive(true);
        if (emptyStatePanel != null) emptyStatePanel.SetActive(false);
    }

    private static string GetTileTypeName(TileData.TileType type)
    {
        return GameLexicon.Terrain(type);
    }
}
