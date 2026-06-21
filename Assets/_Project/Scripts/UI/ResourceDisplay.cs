using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using _Project.Scripts.Utility;

/// <summary>
/// Displays the local player's resource amounts in the UI.
/// All layout (anchors, HLG, background) is configured in the Editor.
/// This script only instantiates resource item prefabs and updates text values.
/// </summary>
public class ResourceDisplay : MonoBehaviour
{
    [Header("Resource Display")]
    [SerializeField] private RectTransform resourceContainer;
    [SerializeField] private GameObject resourceItemPrefab;

    [Header("Resource Icons")]
    [SerializeField] private Sprite woodIcon;
    [SerializeField] private Sprite carrotIcon;
    [SerializeField] private Sprite fishIcon;

    private IResourceService _resourceService;
    private INetworkService _networkService;
    private readonly Dictionary<TileData.TileType, GameObject> _resourceItems = new();

    private void Start()
    {
        _resourceService = ServiceLocator.GetService<IResourceService>();
        _networkService = ServiceLocator.GetService<INetworkService>();

        if (_resourceService != null)
        {
            _resourceService.OnResourceChanged += OnResourceChanged;
            InitializeResourceDisplay();
        }
    }

    private void OnDestroy()
    {
        if (_resourceService != null)
        {
            _resourceService.OnResourceChanged -= OnResourceChanged;
        }
    }

    private void InitializeResourceDisplay()
    {
        if (resourceContainer == null || resourceItemPrefab == null)
        {
            Debug.LogWarning("[ResourceDisplay] Missing container or prefab reference - skipping UI init");
            return;
        }

        CreateResourceItem(TileData.TileType.Wood, woodIcon);
        CreateResourceItem(TileData.TileType.Carrot, carrotIcon);
        CreateResourceItem(TileData.TileType.Fish, fishIcon);
    }

    private void CreateResourceItem(TileData.TileType resourceType, Sprite icon)
    {
        GameObject item = Instantiate(resourceItemPrefab, resourceContainer);

        Image iconImage = item.transform.Find("Icon")?.GetComponent<Image>();
        if (iconImage != null)
        {
            iconImage.sprite = icon;
        }

        TMP_Text amountText = item.transform.Find("Amount")?.GetComponent<TMP_Text>();
        if (amountText != null)
        {
            amountText.text = "0";
        }

        _resourceItems[resourceType] = item;
    }

    private void OnResourceChanged(int actorNumber, TileData.TileType resourceType, int newAmount)
    {
        int localId = _networkService?.LocalPlayerId ?? -1;
        if (actorNumber == localId)
        {
            UpdateResourceDisplay(resourceType, newAmount);
        }
    }

    private void UpdateResourceDisplay(TileData.TileType resourceType, int amount)
    {
        if (_resourceItems.TryGetValue(resourceType, out GameObject item))
        {
            TMP_Text amountText = item.transform.Find("Amount")?.GetComponent<TMP_Text>();
            if (amountText != null)
                amountText.text = amount.ToString();
        }
    }
}
