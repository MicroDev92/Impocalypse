using System;
using System.Collections.Generic;
using Core;
using Core.Events;
using UnityEngine;
using UnityEngine.SceneManagement;
using _Project.Scripts.Utility;

namespace UI
{
    /// <summary>
    /// "Light up these tiles, dim the rest." One mechanism behind every board
    /// question the sidebar can answer: whose land is this, where is their
    /// bloodroot, which tiles make up this domain, what does this pact count.
    ///
    /// Rules:
    ///  - One focus at a time. Asking for another replaces it; asking for the
    ///    same one again turns it off.
    ///  - Purely local. Nothing is sent to other players.
    ///  - The focus is a QUESTION, not a snapshot: it holds a function that
    ///    returns the tiles, and re-asks it whenever the board changes, so a
    ///    focused territory grows as tiles are claimed.
    ///  - It ends by itself on a phase change, and when the player taps a dimmed
    ///    tile or off the board (see TileClickInspector).
    ///
    /// Whoever started the focus identifies it with a key (any string). UI that
    /// wants to show an "active" state listens to <see cref="Changed"/> and asks
    /// <see cref="IsActive"/>.
    /// </summary>
    public static class BoardFocus
    {
        private static string _key;
        private static Func<IEnumerable<TileData>> _tiles;
        private static IEventBus _bus;

        /// <summary>Raised whenever the focus starts, changes or ends.</summary>
        public static event Action Changed;

        public static bool HasFocus => _key != null;
        public static bool IsActive(string key) => _key != null && _key == key;

        /// <summary>Starts this focus, or ends it if it is the one already showing.</summary>
        public static void Toggle(string key, Func<IEnumerable<TileData>> tiles)
        {
            if (IsActive(key)) Clear();
            else Show(key, tiles);
        }

        public static void Show(string key, Func<IEnumerable<TileData>> tiles)
        {
            if (string.IsNullOrEmpty(key) || tiles == null) return;

            EnsureSubscribed();
            _key = key;
            _tiles = tiles;
            Apply();
            Changed?.Invoke();
        }

        public static void Clear()
        {
            if (_key == null) return;

            _key = null;
            _tiles = null;
            Apply();
            Changed?.Invoke();
        }

        /// <summary>Dims every playable tile that is not in the focused set (or none, with no focus).</summary>
        private static void Apply()
        {
            if (!ServiceLocator.TryGetService<IGridService>(out var grid) || grid == null) return;

            HashSet<TileData> lit = null;
            if (_tiles != null)
            {
                lit = new HashSet<TileData>();
                var tiles = _tiles();
                if (tiles != null)
                    foreach (var tile in tiles)
                        if (tile != null) lit.Add(tile);
            }

            foreach (var tile in grid.GetAllTiles())
            {
                if (tile == null) continue;
                tile.SetDimmed(lit != null && !lit.Contains(tile));
            }
        }

        private static void EnsureSubscribed()
        {
            if (_bus != null) return;
            if (!ServiceLocator.TryGetService(out _bus) || _bus == null) return;

            _bus.Subscribe<GamePhaseChangedEvent>(OnPhaseChanged);
            _bus.Subscribe<TileOwnerChangedEvent>(OnBoardChanged);
            _bus.Subscribe<CityPlacedEvent>(OnCityPlaced);
        }

        private static void OnPhaseChanged(GamePhaseChangedEvent evt) => Clear();
        private static void OnBoardChanged(TileOwnerChangedEvent evt) { if (HasFocus) Apply(); }
        private static void OnCityPlaced(CityPlacedEvent evt) { if (HasFocus) Apply(); }

        // Statics outlive a scene (and, with domain reload off, a play session): start clean.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _key = null;
            _tiles = null;
            _bus = null;
            Changed = null;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // The tiles and the event bus of the previous scene are gone.
            _key = null;
            _tiles = null;
            _bus = null;
            Changed?.Invoke();
        }
    }
}
