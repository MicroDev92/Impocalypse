using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using _Project.Scripts.Utility;
using Core.Events;

namespace Scoring
{
    /// <summary>
    /// Draws fief visuals on the board:
    ///  - a coloured outline around each connected group of a player's tiles (so a "fief"
    ///    is actually visible, including lone single-tile fiefs), and
    ///  - a score badge (Strength x Wealth) on each fief that actually scores (> 0).
    ///
    /// Outlines are drawn with LineRenderers (no art asset needed). The score badge is a
    /// prefab you assign (a world-space TMP_Text) - leave it empty to show outlines only.
    /// Recomputes from FiefCalculator whenever ownership or structures change.
    /// </summary>
    public class FiefVisualizer : MonoBehaviour
    {
        [Header("Outline")]
        [SerializeField] private float lineWidth = 0.10f;
        [Tooltip("World size of one tile. Tiles are placed 1 unit apart, so leave at 1.")]
        [SerializeField] private float cellSize = 1f;
        [Tooltip("Pull the outline slightly inside the tile edge so it reads as a highlight.")]
        [SerializeField] private float inset = 0.05f;
        [SerializeField] private int outlineSortingOrder = 40;
        [Tooltip("Dark line drawn under the coloured one, so the outline survives on ground of its own hue (teal on the Styx, red on Bloodroot).")]
        [SerializeField] private Color casingColor = new Color(0.09f, 0.05f, 0.04f, 1f);
        [Tooltip("Casing width as a multiple of the line width.")]
        [SerializeField] private float casingScale = 1.5f;

        [Header("Score badge (optional)")]
        [Tooltip("Prefab with a TMP_Text somewhere in it. Spawned on each fief that scores > 0. Leave empty for outlines only.")]
        [SerializeField] private GameObject scoreBadgePrefab;
        [Tooltip("Z position for badges (negative = closer to the camera, so they draw in front).")]
        [SerializeField] private float badgeZ = -1f;

        private IEventBus _eventBus;
        private IGridService _gridService;
        private INetworkService _networkService;
        private Transform _container;
        private Material _lineMaterial;
        private bool _dirty;

        private static readonly Vector2Int[] Dirs =
            { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        private void Start()
        {
            ServiceLocator.TryGetService(out _eventBus);
            ServiceLocator.TryGetService(out _gridService);
            ServiceLocator.TryGetService(out _networkService);

            _container = new GameObject("FiefVisuals").transform;
            _lineMaterial = new Material(Shader.Find("Sprites/Default"));

            if (_eventBus != null)
            {
                _eventBus.Subscribe<TileOwnerChangedEvent>(OnTileOwnerChanged);
                _eventBus.Subscribe<CityPlacedEvent>(OnCityPlaced);
                _eventBus.Subscribe<GamePhaseChangedEvent>(OnPhaseChanged);
            }

            MarkDirty();
        }

        private void OnDestroy()
        {
            if (_eventBus != null)
            {
                _eventBus.Unsubscribe<TileOwnerChangedEvent>(OnTileOwnerChanged);
                _eventBus.Unsubscribe<CityPlacedEvent>(OnCityPlaced);
                _eventBus.Unsubscribe<GamePhaseChangedEvent>(OnPhaseChanged);
            }

            if (_container != null) Destroy(_container.gameObject);
        }

        private void OnTileOwnerChanged(TileOwnerChangedEvent _) => MarkDirty();
        private void OnCityPlaced(CityPlacedEvent _) => MarkDirty();
        private void OnPhaseChanged(GamePhaseChangedEvent _) => MarkDirty();
        private void MarkDirty() => _dirty = true;

        // Coalesce bursts of events into a single rebuild per frame.
        private void LateUpdate()
        {
            if (!_dirty) return;
            _dirty = false;
            Rebuild();
        }

        private void Rebuild()
        {
            if (_container == null || _gridService == null) return;

            for (int i = _container.childCount - 1; i >= 0; i--)
                Destroy(_container.GetChild(i).gameObject);

            var playerIds = _networkService?.GetPlayerIds();
            if (playerIds == null) return;

            foreach (var pid in playerIds)
            {
                var color = GetPlayerColor(pid);
                foreach (var fief in FiefCalculator.CalculateFiefs(pid, _gridService))
                {
                    DrawOutline(fief, color);
                    DrawSkyLinks(fief, color);
                    if (fief.Score > 0 && scoreBadgePrefab != null)
                        DrawBadge(fief);
                }
            }
        }

        private void DrawOutline(Fief fief, Color color)
        {
            var members = new HashSet<Vector2Int>(fief.Tiles.Select(t => t.gridPosition));
            float h = cellSize * 0.5f - inset;

            foreach (var tile in fief.Tiles)
            {
                Vector3 c = tile.transform.position;
                Vector2Int gp = tile.gridPosition;

                foreach (var d in Dirs)
                {
                    // Only draw an edge where this tile borders a non-fief cell.
                    if (members.Contains(gp + d)) continue;

                    Vector3 a, b;
                    if (d == Vector2Int.up)        { a = c + new Vector3(-h,  h); b = c + new Vector3( h,  h); }
                    else if (d == Vector2Int.down) { a = c + new Vector3(-h, -h); b = c + new Vector3( h, -h); }
                    else if (d == Vector2Int.left) { a = c + new Vector3(-h, -h); b = c + new Vector3(-h,  h); }
                    else                           { a = c + new Vector3( h, -h); b = c + new Vector3( h,  h); }

                    CreateSegment(a, b, color);
                }
            }
        }

        /// <summary>Draws a line between sky-tower-linked tiles (once per pair).</summary>
        private void DrawSkyLinks(Fief fief, Color color)
        {
            foreach (var tile in fief.Tiles)
            {
                if (!tile.HasSkyLink) continue;
                if (string.CompareOrdinal(tile.coord, tile.SkyLinkCoord) >= 0) continue; // draw each pair once

                var other = _gridService.GetTileByCoord(tile.SkyLinkCoord);
                if (other != null)
                    CreateSegment(tile.transform.position, other.transform.position, color);
            }
        }

        private void CreateSegment(Vector3 a, Vector3 b, Color color)
        {
            // Dark casing first, colour on top - the map-maker's way of keeping a line
            // legible on any ground. A player colour alone disappears on terrain of the
            // same hue, and the playtest complaint was exactly "whose is this".
            AddLine("FiefEdgeCasing", a, b, casingColor, lineWidth * casingScale, outlineSortingOrder - 1);
            AddLine("FiefEdge", a, b, color, lineWidth, outlineSortingOrder);
        }

        private void AddLine(string name, Vector3 a, Vector3 b, Color color, float width, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_container, false);

            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.positionCount = 2;
            lr.SetPosition(0, a);
            lr.SetPosition(1, b);
            lr.startWidth = lr.endWidth = width;
            lr.numCapVertices = 2;
            lr.material = _lineMaterial;
            lr.startColor = lr.endColor = color;
            lr.sortingOrder = order;
        }

        private void DrawBadge(Fief fief)
        {
            Vector3 centroid = Vector3.zero;
            foreach (var t in fief.Tiles) centroid += t.transform.position;
            centroid /= fief.Tiles.Count;
            centroid.z = badgeZ;

            var badge = Instantiate(scoreBadgePrefab, centroid, Quaternion.identity, _container);
            var label = badge.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = fief.Score.ToString();
        }

        /// <summary>
        /// The owner's colour, from the identity PlayerService assigned.
        ///
        /// This used to be a private actorNumber % 4 palette - a second copy of the one
        /// TileData carried, and wrong in the same way. Actor 1 resolved to blue while
        /// their imp was red, so a player's own domain was outlined in another player's
        /// colour. Both now resolve through the one service, from the same palette entry
        /// that supplies the imp sprite, so outline, tile tint and imp cannot disagree.
        /// </summary>
        private static Color GetPlayerColor(int actorNumber)
        {
            return ServiceLocator.TryGetService(out Players.IPlayerService players)
                ? players.GetPlayerColor(actorNumber)
                : Color.white;
        }
    }
}
