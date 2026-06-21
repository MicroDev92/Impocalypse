using UnityEngine;
using _Project.Scripts.Utility;

/// <summary>
/// Component on resource tiles. Resource collection is now handled
/// automatically by ResourceManager listening to ImpSpawnedEvent.
/// This component can still be used for manual collection if needed.
/// </summary>
public class ResourceCollector : MonoBehaviour
{
    [Header("Visual Feedback")]
    public GameObject collectionEffect;
    public AudioClip collectionSound;

    private TileData _tileData;

    private void Start()
    {
        _tileData = GetComponent<TileData>();
    }

    /// <summary>
    /// Gets the resource type of this tile.
    /// </summary>
    public TileData.TileType? GetResourceType()
    {
        if (_tileData == null) return null;

        var resourceService = ServiceLocator.GetService<IResourceService>();
        if (resourceService != null && resourceService.IsResourceTile(_tileData.type))
        {
            return _tileData.type;
        }

        return null;
    }
}
