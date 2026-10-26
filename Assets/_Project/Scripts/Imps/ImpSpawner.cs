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

        [Header("Building Etiquette")]
        [Tooltip("When a building occupies the imp's tile, the imp steps aside: tucked into this corner offset, at reduced scale, drawn in front of the building marker.")]
        [SerializeField] private Vector2 buildingTuckOffset = new Vector2(-0.28f, -0.26f);
        [Range(0.3f, 1f)]
        [SerializeField] private float buildingTuckScale = 0.55f;
        [Tooltip("Sorting order for a tucked imp - should beat the building marker's (default 100) so the imp stands in front of its new home.")]
        [SerializeField] private int tuckedSortingOrder = 101;

        [Header("Arrival")]
        [Tooltip("How far above its tile an imp starts before dropping in. Provisions claims tiles the player never clicked, so motion is the only thing that says WHERE it happened.")]
        [SerializeField] private float dropHeight = 2.2f;
        [Tooltip("Seconds for the drop. Long enough to follow, short enough not to delay play.")]
        [SerializeField] private float dropDuration = 0.35f;
        [Tooltip("Extra delay per imp when several land in the same frame, so a batch reads as a cascade rather than a blink. 0 disables staggering.")]
        [SerializeField] private float dropStagger = 0.07f;
        [Tooltip("How far the imp sinks past its tile before settling back.")]
        [SerializeField] private float overshootDepth = 0.12f;
        [Tooltip("Max sideways offset an imp starts from, so a batch does not fall down identical vertical lines.")]
        [SerializeField] private float driftRange = 0.18f;
        [Tooltip("Max tilt in degrees during the fall, righting itself on landing. Rotation rather than squash on purpose: scale is shared state owned by the building-tuck logic.")]
        [SerializeField] private float tiltDegrees = 14f;

        private int _lastArrivalFrame = -1;
        private int _arrivalsThisFrame;

        private ImpFactory _impFactory;
        private IEventBus _eventBus;
        private IGridService _gridService;

        // Original scale/sorting per tucked imp, so eviction/pooling restores them.
        private readonly System.Collections.Generic.Dictionary<Imp, (Vector3 scale, int order)> _tucked = new();

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
                _eventBus.Subscribe<CityPlacedEvent>(OnCityPlacedEvent);
                _eventBus.Subscribe<TileOwnerChangedEvent>(OnTileOwnerChangedEvent);
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
                _eventBus.Unsubscribe<CityPlacedEvent>(OnCityPlacedEvent);
                _eventBus.Unsubscribe<TileOwnerChangedEvent>(OnTileOwnerChangedEvent);
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

            // Claim collision handling (the tile's Territory card can arrive after a Camp
            // already claimed it - camp claims are provisional and get evicted).
            if (tile.IsOwned)
            {
                if (tile.IsOwnedBy(ownerActorNumber))
                {
                    // Same player: the Territory card upgrades the camp to a permanent claim,
                    // freeing the building slot for real construction.
                    if (tile.IsCampClaim) tile.ClearBuilding();
                    return _impFactory?.GetImpAtCoordinate(coordinate);
                }

                if (!tile.IsCampClaim)
                {
                    Debug.LogWarning($"[ImpSpawner] {coordinate} already owned by player {tile.OwnerActorNumber} - claim ignored");
                    return null;
                }

                // Evict the camper: remove their imp and camp, break any sky link.
                // Restore pose first - pooled imps must never carry a tucked scale.
                var oldImp = _impFactory?.GetImpAtCoordinate(coordinate);
                if (oldImp != null)
                {
                    RestorePose(oldImp, tile.transform.position);
                    _impFactory.ReturnImp(oldImp);
                }

                if (tile.HasSkyLink)
                {
                    _gridService.GetTileByCoord(tile.SkyLinkCoord)?.ClearSkyLink();
                    tile.ClearSkyLink();
                }
                tile.ClearBuilding();

                Debug.Log($"[ImpSpawner] Player {ownerActorNumber} evicted a camp at {coordinate}");
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

                // Camp flow: the tile already has its building when the imp arrives.
                SyncImpPose(coordinate);

                // Animate last: SyncImpPose has just written the landing position.
                AnimateArrival(imp);
            }

            return imp;
        }

        // ==========================================
        // BUILDING ETIQUETTE
        // The building is the tile's dominant visual; its imp steps aside.
        // ==========================================

        private void OnCityPlacedEvent(CityPlacedEvent evt) => SyncImpPose(evt.Coordinate);
        private void OnTileOwnerChangedEvent(TileOwnerChangedEvent evt) => SyncImpPose(evt.Coordinate);

        private void SyncImpPose(string coordinate)
        {
            var tile = _gridService?.GetTileByCoord(coordinate);
            var imp = _impFactory?.GetImpAtCoordinate(coordinate);
            if (tile == null || imp == null) return;

            if (tile.HasBuilding)
                TuckPose(imp, tile.transform.position);
            else
                RestorePose(imp, tile.transform.position);
        }

        // ==========================================
        // ARRIVAL
        // ==========================================

        /// <summary>
        /// Drops the imp in from above its tile.
        ///
        /// Provisions resolves at pick time and claims tiles the player never clicked,
        /// so the only signal was a counter in the top bar - missable even by someone
        /// who knows it exists. A counter cannot say WHERE; motion can.
        /// </summary>
        private void AnimateArrival(Imp imp)
        {
            if (imp == null || dropDuration <= 0f || dropHeight <= 0f) return;

            if (Time.frameCount != _lastArrivalFrame)
            {
                _lastArrivalFrame = Time.frameCount;
                _arrivalsThisFrame = 0;
            }

            StartCoroutine(DropIn(imp, dropStagger * _arrivalsThisFrame));
            _arrivalsThisFrame++;
        }

        private System.Collections.IEnumerator DropIn(Imp imp, float delay)
        {
            var tr = imp.transform;
            Vector3 landing = tr.position;

            // A little lateral offset and tilt, so imps do not all fall down the same
            // dead-straight line. Purely visual and local-only, so Random is safe.
            float drift = Random.Range(-driftRange, driftRange);
            float tilt = Random.Range(-tiltDegrees, tiltDegrees);
            Vector3 start = landing + Vector3.up * dropHeight + Vector3.right * drift;

            // Overshoot slightly, then settle back. Landing dead on the mark reads as a
            // transform finishing; overshooting reads as something with weight.
            Vector3 overshoot = landing + Vector3.down * overshootDepth;

            Quaternion startRot = Quaternion.Euler(0f, 0f, tilt);
            Quaternion landRot = Quaternion.Euler(0f, 0f, -tilt * 0.25f);
            Quaternion restRot = tr.localRotation;

            tr.position = start;
            tr.localRotation = startRot;
            imp.SetAirborneOffset(start - landing);

            float waited = 0f;
            while (waited < delay)
            {
                if (!StillAirborne(imp)) yield break;
                waited += Time.deltaTime;
                yield return null;
            }

            float fall = dropDuration * 0.78f;
            float settle = Mathf.Max(0.04f, dropDuration - fall);

            float t = 0f;
            while (t < fall)
            {
                // Imps are pooled, so one can be returned mid-drop (camp eviction, round
                // end). Bail rather than writing a pose onto an imp that is now somebody
                // else on another tile.
                if (!StillAirborne(imp)) yield break;

                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / fall);
                // Accelerate downward: a falling imp reads as arriving, one that eases
                // out reads as sliding.
                tr.position = Vector3.LerpUnclamped(start, overshoot, k * k);
                tr.localRotation = Quaternion.Slerp(startRot, landRot, k);
                imp.SetAirborneOffset(tr.position - landing);
                yield return null;
            }

            t = 0f;
            while (t < settle)
            {
                if (!StillAirborne(imp)) yield break;

                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / settle);
                // Ease out of the recoil so it decays rather than snapping.
                float e = 1f - (1f - k) * (1f - k);
                tr.position = Vector3.LerpUnclamped(overshoot, landing, e);
                tr.localRotation = Quaternion.Slerp(landRot, restRot, e);
                imp.SetAirborneOffset(tr.position - landing);
                yield return null;
            }

            if (!StillAirborne(imp)) yield break;
            tr.position = landing;
            tr.localRotation = restRot;
            imp.SetAirborneOffset(Vector3.zero);
        }

        private static bool StillAirborne(Imp imp) => imp != null && imp.gameObject.activeInHierarchy;

        private void TuckPose(Imp imp, Vector3 tileCenter)
        {
            if (!_tucked.ContainsKey(imp))
            {
                var sr = imp.GetComponentInChildren<SpriteRenderer>();
                _tucked[imp] = (imp.transform.localScale, sr != null ? sr.sortingOrder : 10);

                if (sr != null) sr.sortingOrder = tuckedSortingOrder;
                imp.transform.localScale *= buildingTuckScale;
            }

            imp.transform.position = tileCenter + (Vector3)buildingTuckOffset;
        }

        private void RestorePose(Imp imp, Vector3 tileCenter)
        {
            if (_tucked.TryGetValue(imp, out var original))
            {
                imp.transform.localScale = original.scale;
                var sr = imp.GetComponentInChildren<SpriteRenderer>();
                if (sr != null) sr.sortingOrder = original.order;
                _tucked.Remove(imp);
            }

            imp.transform.position = tileCenter;
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
            // Restore every tucked imp before pooling - reused imps must start clean.
            foreach (var kvp in _tucked)
            {
                if (kvp.Key == null) continue;
                kvp.Key.transform.localScale = kvp.Value.scale;
                var sr = kvp.Key.GetComponentInChildren<SpriteRenderer>();
                if (sr != null) sr.sortingOrder = kvp.Value.order;
            }
            _tucked.Clear();

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
