using UnityEngine;
using _Project.Scripts.Utility;
using _Project.Scripts.Visuals;
using Core.Events;
using Config;

namespace Imps
{
    /// <summary>
    /// Handles imp spawning in response to events.
    /// Uses ImpFactory for actual instantiation.
    /// </summary>
    public class ImpSpawner : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private ImpVisuals impVisuals;
        [SerializeField] private PlayerColorPalette colorPalette;

        private ImpFactory _impFactory;
        private IEventBus _eventBus;
        private IGridService _gridService;

        private void Awake()
        {
            ServiceLocator.RegisterService<ImpSpawner>(this);
        }

        private void Start()
        {
            // Get services
            ServiceLocator.TryGetService(out _impFactory);
            ServiceLocator.TryGetService(out _eventBus);
            ServiceLocator.TryGetService(out _gridService);

            // Subscribe to spawn events
            if (_eventBus != null)
            {
                _eventBus.Subscribe<SpawnImpEvent>(OnSpawnImpEvent);
            }

            // Load visuals if not set
            if (impVisuals == null)
            {
                impVisuals = FindAnyObjectByType<ImpVisuals>();
            }
        }

        private void OnDestroy()
        {
            if (_eventBus != null)
            {
                _eventBus.Unsubscribe<SpawnImpEvent>(OnSpawnImpEvent);
            }

            ServiceLocator.UnregisterService<ImpSpawner>();
        }

        private void OnSpawnImpEvent(SpawnImpEvent evt)
        {
            SpawnImp(evt.Coordinate, evt.OwnerActorNumber, evt.Color);
        }

        /// <summary>
        /// Spawns an imp at the specified coordinate.
        /// </summary>
        /// <param name="coordinate">Grid coordinate</param>
        /// <param name="ownerActorNumber">Owner's actor number</param>
        /// <param name="colorId">Color identifier</param>
        /// <returns>Spawned imp or null</returns>
        public Imp SpawnImp(string coordinate, int ownerActorNumber, string colorId)
        {
            if (_impFactory == null)
            {
                Debug.LogError("[ImpSpawner] ImpFactory not available");
                return null;
            }

            // Get tile position
            var tile = _gridService?.GetTileByCoord(coordinate);
            if (tile == null)
            {
                Debug.LogWarning($"[ImpSpawner] Tile not found for coordinate: {coordinate}");
                return null;
            }

            // Get sprite
            Sprite sprite = GetImpSprite(colorId);
            if (sprite == null)
            {
                Debug.LogWarning($"[ImpSpawner] No sprite found for color: {colorId}");
            }

            // Create imp
            var imp = _impFactory.CreateImp(
                coordinate,
                ownerActorNumber,
                colorId,
                sprite,
                tile.transform.position
            );

            if (imp != null)
            {
                // Set tile ownership
                tile.SetOwner(ownerActorNumber);
                _eventBus?.Publish(new TileOwnerChangedEvent(coordinate, ownerActorNumber));

                Debug.Log($"[ImpSpawner] Spawned imp at {coordinate} for player {ownerActorNumber} ({colorId})");

                // Publish event
                _eventBus?.Publish(new ImpSpawnedEvent(coordinate, ownerActorNumber));
            }

            return imp;
        }

        /// <summary>
        /// Spawns imps for the local player at confirmed card coordinates.
        /// Used for local preview during draft.
        /// </summary>
        /// <param name="coordinate">Coordinate to spawn at</param>
        /// <param name="localPlayerId">Local player's actor number</param>
        /// <param name="colorId">Player's color</param>
        public Imp SpawnLocalPreviewImp(string coordinate, int localPlayerId, string colorId)
        {
            return SpawnImp(coordinate, localPlayerId, colorId);
        }

        private Sprite GetImpSprite(string colorId)
        {
            // Try ImpVisuals first (legacy)
            if (impVisuals != null)
            {
                var sprite = impVisuals.GetSprite(colorId);
                if (sprite != null) return sprite;
            }

            // Try PlayerColorPalette
            if (colorPalette != null)
            {
                var sprite = colorPalette.GetImpSprite(colorId);
                if (sprite != null) return sprite;
            }

            return null;
        }

        /// <summary>
        /// Clears all spawned imps.
        /// </summary>
        public void ClearAllImps()
        {
            _impFactory?.ReturnAllImps();
            Debug.Log("[ImpSpawner] Cleared all imps");
        }

        /// <summary>
        /// Gets the number of imps owned by a player.
        /// </summary>
        public int GetImpCount(int ownerActorNumber)
        {
            return _impFactory?.GetImpsByOwner(ownerActorNumber).Count ?? 0;
        }
    }
}
