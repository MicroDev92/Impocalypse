using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using _Project.Scripts.Utility;
using Core.Events;
using Scoring;

namespace UI
{
    /// <summary>
    /// Live list of the local player's Domains in the left sidebar.
    ///
    /// The game's whole scoring model is Strength x Wealth per connected group of
    /// tiles, but until now that was invisible until the round ended - players
    /// claimed tiles with no feedback on what they were building. This shows the
    /// domains forming in real time, which is the core feedback loop.
    ///
    /// Deliberately a GROWING list of real domains, never fixed "empty" slots:
    /// domains are emergent from connectivity, so a fixed slot count would
    /// misinform the player about how the game works.
    /// </summary>
    public class DomainListUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Transform listContainer;
        [Tooltip("Row prefab with a TMP_Text (FiefDetail.prefab works).")]
        [SerializeField] private GameObject rowPrefab;
        [SerializeField] private TMP_Text headerText;
        [Tooltip("Shown while the player owns no tiles yet.")]
        [SerializeField] private TMP_Text emptyText;

        [Header("Display")]
        [Tooltip("Longest list shown; beyond this the smallest domains are summarised on one line.")]
        [SerializeField] private int maxRows = 5;

        private IEventBus _eventBus;
        private readonly List<GameObject> _rows = new();

        private void Start()
        {
            if (ServiceLocator.TryGetService<IEventBus>(out _eventBus))
            {
                _eventBus.Subscribe<TileOwnerChangedEvent>(OnBoardChanged);
                _eventBus.Subscribe<CityPlacedEvent>(OnCityPlaced);
                _eventBus.Subscribe<GamePhaseChangedEvent>(OnPhaseChanged);
                _eventBus.Subscribe<RoundStartedEvent>(OnRoundStarted);
            }

            Refresh();
        }

        private void OnDestroy()
        {
            if (_eventBus == null) return;
            _eventBus.Unsubscribe<TileOwnerChangedEvent>(OnBoardChanged);
            _eventBus.Unsubscribe<CityPlacedEvent>(OnCityPlaced);
            _eventBus.Unsubscribe<GamePhaseChangedEvent>(OnPhaseChanged);
            _eventBus.Unsubscribe<RoundStartedEvent>(OnRoundStarted);
        }

        private void OnBoardChanged(TileOwnerChangedEvent evt) => Refresh();
        private void OnCityPlaced(CityPlacedEvent evt) => Refresh();
        private void OnPhaseChanged(GamePhaseChangedEvent evt) => Refresh();
        private void OnRoundStarted(RoundStartedEvent evt) => Refresh();

        public void Refresh()
        {
            var grid = ServiceLocator.GetService<IGridService>();
            var network = ServiceLocator.GetService<INetworkService>();
            if (grid == null || network == null) return;

            var domains = FiefCalculator.CalculateFiefs(network.LocalPlayerId, grid)
                                        .OrderByDescending(f => f.Score)
                                        .ThenByDescending(f => f.Tiles.Count)
                                        .ToList();

            ClearRows();

            int total = domains.Sum(d => d.Score);
            if (headerText != null)
                headerText.text = domains.Count > 0
                    ? $"Your {GameLexicon.FiefName}s — {total} {GameLexicon.ScoreName}"
                    : $"Your {GameLexicon.FiefName}s";

            if (emptyText != null)
                emptyText.gameObject.SetActive(domains.Count == 0);

            if (domains.Count == 0 || listContainer == null || rowPrefab == null) return;

            int shown = Mathf.Min(domains.Count, maxRows);
            for (int i = 0; i < shown; i++)
                AddRow(DescribeDomain(domains[i]));

            // Summarise the tail rather than letting the list push the sidebar over.
            if (domains.Count > shown)
            {
                var rest = domains.Skip(shown).ToList();
                AddRow($"+{rest.Count} smaller ({rest.Sum(d => d.Score)} {GameLexicon.ScoreName})");
            }
        }

        /// <summary>
        /// One line per domain, showing the actual scoring maths so the player can
        /// see WHY a domain is worth what it is - a big domain with no citadel
        /// scores zero, and that should be obvious at a glance.
        /// </summary>
        private static string DescribeDomain(Fief domain)
        {
            string tiles = domain.Tiles.Count == 1 ? "1 tile" : $"{domain.Tiles.Count} tiles";
            return $"{tiles} · Str {domain.Strength} × Wealth {domain.Wealth} = {domain.Score}";
        }

        private void AddRow(string text)
        {
            var row = Instantiate(rowPrefab, listContainer);
            row.SetActive(true);

            var label = row.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = text;

            _rows.Add(row);
        }

        private void ClearRows()
        {
            foreach (var row in _rows)
                if (row != null) Destroy(row);
            _rows.Clear();
        }
    }
}
