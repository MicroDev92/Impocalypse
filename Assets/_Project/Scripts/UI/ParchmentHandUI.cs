using System.Text;
using TMPro;
using UnityEngine;
using _Project.Scripts.Utility;
using Cards;
using Core.Events;
using Draft.Services;

namespace UI
{
    /// <summary>
    /// Shows the local player's secret parchment hand ("pacts") as a simple text list.
    /// Refreshes whenever picks are submitted or the phase changes. Layout lives in the
    /// scene/prefab; this script only fills the text.
    /// </summary>
    public class ParchmentHandUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_Text listText;

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
            if (listText == null) return;

            var draft = ServiceLocator.GetService<IDraftService>();
            var network = ServiceLocator.GetService<INetworkService>();
            if (draft == null || network == null) return;

            var pacts = draft.GetPlayerParchments(network.LocalPlayerId);
            if (pacts.Count == 0)
            {
                listText.text = "<i>none yet</i>";
                return;
            }

            var sb = new StringBuilder();
            foreach (var p in pacts)
                sb.AppendLine("• " + Describe(p));
            listText.text = sb.ToString().TrimEnd();
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
