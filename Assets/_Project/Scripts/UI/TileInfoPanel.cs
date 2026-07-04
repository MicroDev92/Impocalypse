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

        // Resource
        if (resourceValue != null)
        {
            resourceValue.text = tile.type switch
            {
                TileData.TileType.Wood => "Wood",
                TileData.TileType.Carrot => "Carrot",
                TileData.TileType.Fish => "Fish",
                TileData.TileType.Mountain => "None (Mountain)",
                TileData.TileType.Plains => "None (Plains)",
                _ => "None"
            };
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
                    BuildingKind.City => $"City (Str {(int)tile.BuildingStrength})",
                    BuildingKind.Market => "Market",
                    BuildingKind.SpecialFarm => $"Luxury Farm ({tile.AddedResource})",
                    BuildingKind.CommonFarm => $"Farm ({tile.AddedResource})",
                    _ => "—"
                };
            }
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
        if (emptyStateText != null) emptyStateText.text = "Click a tile to inspect";
    }

    private void ShowContent()
    {
        if (contentPanel != null) contentPanel.SetActive(true);
        if (emptyStatePanel != null) emptyStatePanel.SetActive(false);
    }

    private static string GetTileTypeName(TileData.TileType type)
    {
        return type switch
        {
            TileData.TileType.Grass => "Grassland",
            TileData.TileType.Water => "Water",
            TileData.TileType.Wood => "Forest",
            TileData.TileType.Carrot => "Farmland",
            TileData.TileType.Fish => "Fishing Waters",
            TileData.TileType.Plains => "Plains",
            TileData.TileType.Mountain => "Mountain",
            TileData.TileType.PresetVillage => "Village",
            _ => type.ToString()
        };
    }
}
