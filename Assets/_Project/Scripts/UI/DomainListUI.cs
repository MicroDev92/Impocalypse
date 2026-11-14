using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
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
        [SerializeField] private TMP_Text headerText;
        [Tooltip("Shown while the player owns no tiles yet.")]
        [SerializeField] private TMP_Text emptyText;

        [Header("Layout")]
        [Tooltip("Panel height with no rows; each row adds its own height on top. The sidebar may give the panel less than it asks for, in which case the list scrolls.")]
        [SerializeField] private float baseHeight = 56f;
        [SerializeField] private float minHeight = 120f;
        [SerializeField] private float rowSpacing = 4f;

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
                    ? $"Your {GameLexicon.FiefName}s: {total}"
                    : $"Your {GameLexicon.FiefName}s";

            if (emptyText != null)
                emptyText.gameObject.SetActive(domains.Count == 0);

            // Ask for the height the rows need. Every domain gets its own row: when
            // the sidebar cannot give that much, the list scrolls instead of being
            // summarised or spilling out of the panel.
            if (TryGetComponent<LayoutElement>(out var element))
            {
                float height = baseHeight + domains.Count * (IconRow.Height + rowSpacing);
                element.minHeight = minHeight;
                element.preferredHeight = Mathf.Max(minHeight, height);
            }

            if (domains.Count == 0 || listContainer == null) return;

            var icons = UIIconSet.Instance;
            foreach (var domain in domains)
                AddRow(domain, icons);
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

        /// <summary>
        /// "[tiles] 5   [strength] 2 x [wealth] 3 = [infamy] 6" - the same maths as
        /// the sentence above, in icons. A domain worth nothing keeps its row but
        /// greys its total, so the missing citadel or resource is the thing noticed.
        /// </summary>
        private void AddRow(Fief domain, UIIconSet icons)
        {
            var row = IconRow.Create(listContainer);
            _rows.Add(row.gameObject);

            if (icons == null)
            {
                IconRow.Word(row, DescribeDomain(domain));
                return;
            }

            IconRow.Icon(row, icons.tile);
            IconRow.Number(row, domain.Tiles.Count.ToString(), IconRow.Bright);
            IconRow.Word(row, "  ");
            IconRow.Icon(row, icons.strength);
            IconRow.Number(row, domain.Strength.ToString(), IconRow.Bright);
            IconRow.Number(row, "x", IconRow.Soft, 20f);
            IconRow.Icon(row, icons.wealth);
            IconRow.Number(row, domain.Wealth.ToString(), IconRow.Bright);
            IconRow.Number(row, "=", IconRow.Soft, 22f);
            IconRow.Icon(row, icons.infamy);
            IconRow.Number(row, domain.Score.ToString(), domain.Score > 0 ? IconRow.Accent : IconRow.Soft);
        }

        private void ClearRows()
        {
            foreach (var row in _rows)
                if (row != null) Destroy(row);
            _rows.Clear();
        }
    }
}
