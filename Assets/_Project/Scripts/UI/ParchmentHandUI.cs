using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using _Project.Scripts.Utility;
using Cards;
using Core.Events;
using Draft.Services;

namespace UI
{
    /// <summary>
    /// Shows the local player's secret parchment hand ("pacts") as icon rows:
    /// what the pact counts, then what each one is worth. Identical pacts collapse
    /// into one row with a multiplier, and the panel grows with the list.
    /// Refreshes whenever picks are submitted or the phase changes.
    /// </summary>
    public class ParchmentHandUI : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Shown only while the player holds no pacts.")]
        [SerializeField] private TMP_Text listText;
        [Tooltip("Vertical layout the pact rows are added to.")]
        [SerializeField] private Transform rowContainer;

        [Header("Layout")]
        [Tooltip("Panel height with no rows; each row adds its own height on top.")]
        [SerializeField] private float baseHeight = 56f;
        [SerializeField] private float minHeight = 96f;
        [SerializeField] private float rowSpacing = 4f;

        private readonly List<GameObject> _rows = new();

        private IEventBus _eventBus;

        private void Start()
        {
            if (ServiceLocator.TryGetService(out _eventBus))
            {
                _eventBus.Subscribe<LocalPicksSubmittedEvent>(OnPicksSubmitted);
                _eventBus.Subscribe<GamePhaseChangedEvent>(OnPhaseChanged);
                _eventBus.Subscribe<CardsGrantedEvent>(OnCardsGranted);
            }
            Refresh();
        }

        private void OnDestroy()
        {
            if (_eventBus != null)
            {
                _eventBus.Unsubscribe<LocalPicksSubmittedEvent>(OnPicksSubmitted);
                _eventBus.Unsubscribe<GamePhaseChangedEvent>(OnPhaseChanged);
                _eventBus.Unsubscribe<CardsGrantedEvent>(OnCardsGranted);
            }
        }

        private void OnPicksSubmitted(LocalPicksSubmittedEvent _) => Refresh();
        private void OnPhaseChanged(GamePhaseChangedEvent _) => Refresh();
        private void OnCardsGranted(CardsGrantedEvent _) => Refresh();

        private void Refresh()
        {
            var draft = ServiceLocator.GetService<IDraftService>();
            var network = ServiceLocator.GetService<INetworkService>();
            if (draft == null || network == null) return;

            var pacts = draft.GetPlayerParchments(network.LocalPlayerId);

            foreach (var row in _rows)
                if (row != null) Destroy(row);
            _rows.Clear();

            if (listText != null)
            {
                listText.text = "none yet";
                listText.gameObject.SetActive(pacts.Count == 0);
            }

            // Old scenes without a row container keep the text list.
            if (rowContainer == null)
            {
                if (listText != null && pacts.Count > 0)
                {
                    listText.gameObject.SetActive(true);
                    listText.text = string.Join("\n", pacts.Select(p => "• " + Describe(p)));
                }
                return;
            }

            var groups = pacts.GroupBy(p => (p.Rule, p.Amount, p.Resource))
                              .OrderBy(g => g.Key.Rule)
                              .ThenByDescending(g => g.Key.Amount)
                              .ToList();
            foreach (var group in groups)
                _rows.Add(BuildRow(group.First(), group.Count()));

            if (TryGetComponent<LayoutElement>(out var element))
            {
                float height = baseHeight + groups.Count * (IconRow.Height + rowSpacing);
                // Preferred, not minimum: when the sidebar is short of room the panel
                // is given less than this and its rows scroll.
                element.minHeight = minHeight;
                element.preferredHeight = Mathf.Max(minHeight, height);
            }
        }

        /// <summary>
        /// "[what it counts] +N [infamy] each". The leading icon is the thing the
        /// player should go and collect, so it is the first thing the eye lands on.
        /// </summary>
        private GameObject BuildRow(CardData pact, int copies)
        {
            var icons = UIIconSet.Instance;
            var row = IconRow.Create(rowContainer);

            if (copies > 1)
                IconRow.Number(row, $"{copies}x", IconRow.Soft, 18f);

            if (icons == null)
            {
                IconRow.Word(row, Describe(pact));
                return row.gameObject;
            }

            Sprite counted = pact.Rule switch
            {
                ParchmentRule.Treasure => icons.treasure,
                ParchmentRule.PerResource => icons.Resource(pact.Resource),
                ParchmentRule.PerFief => icons.domain,
                ParchmentRule.LargestFief => icons.tile,
                ParchmentRule.PerCity => icons.strength,
                _ => icons.pact
            };

            IconRow.Icon(row, counted);
            IconRow.Number(row, $"+{pact.Amount}", IconRow.Bright);
            IconRow.Icon(row, icons.infamy);

            switch (pact.Rule)
            {
                case ParchmentRule.Treasure:
                    break;
                case ParchmentRule.LargestFief:
                    IconRow.Word(row, "each, biggest");
                    IconRow.Icon(row, icons.domain, 24f);
                    break;
                default:
                    IconRow.Word(row, "each");
                    break;
            }
            return row.gameObject;
        }

        private static string Describe(CardData p) => p.Rule switch
        {
            ParchmentRule.Treasure => $"Treasure: +{p.Amount}",
            ParchmentRule.PerResource => $"+{p.Amount} per {GameLexicon.Resource(p.Resource)}",
            ParchmentRule.PerFief => $"+{p.Amount} per domain",
            ParchmentRule.LargestFief => $"+{p.Amount}/tile in biggest domain",
            ParchmentRule.PerCity => $"+{p.Amount} per citadel",
            _ => p.ToString()
        };
    }
}
