using System.Collections.Generic;
using UnityEngine;
using TMPro;
using _Project.Scripts.Utility;
using Core.Events;
using Scoring;

namespace UI
{
    /// <summary>
    /// Displays per-player scores after each round with fief breakdown.
    /// Shows as an overlay panel during scoring phase.
    /// All layout is configured in the Editor — this script only instantiates
    /// ScoreEntry prefabs and populates data.
    /// </summary>
    public class ScoreboardDisplay : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject scoreboardPanel;
        [SerializeField] private Transform scoreEntryContainer;
        [SerializeField] private TextMeshProUGUI titleText;

        [Header("Prefabs")]
        [SerializeField] private GameObject scoreEntryPrefab;
        [SerializeField] private GameObject fiefDetailPrefab;

        private IEventBus _eventBus;
        private readonly List<GameObject> _scoreEntries = new();

        private void Start()
        {
            ServiceLocator.TryGetService(out _eventBus);

            _eventBus?.Subscribe<GameEndedEvent>(OnGameEnded);
            _eventBus?.Subscribe<GamePhaseChangedEvent>(OnPhaseChanged);

            if (scoreboardPanel != null)
                scoreboardPanel.SetActive(false);
        }

        private void OnDestroy()
        {
            _eventBus?.Unsubscribe<GameEndedEvent>(OnGameEnded);
            _eventBus?.Unsubscribe<GamePhaseChangedEvent>(OnPhaseChanged);
        }

        private void OnPhaseChanged(GamePhaseChangedEvent evt)
        {
            if (evt.NewPhase == Core.GamePhase.Scoring || evt.NewPhase == Core.GamePhase.GameOver)
            {
                ShowScoreboard();
            }
            else
            {
                HideScoreboard();
            }
        }

        private void OnGameEnded(GameEndedEvent evt)
        {
            ShowFinalScores(evt.PlayerScores);
        }

        private void ShowScoreboard()
        {
            if (scoreboardPanel == null) return;
            scoreboardPanel.SetActive(true);

            ClearEntries();

            var gameManager = ServiceLocator.GetService<IGameManager>();
            var gridService = ServiceLocator.GetService<IGridService>();
            var networkService = ServiceLocator.GetService<INetworkService>();

            if (titleText != null)
            {
                int round = gameManager?.CurrentRound ?? 1;
                int maxRounds = gameManager?.MaxRounds ?? 4;
                titleText.text = round >= maxRounds ? "FINAL SCORES" : $"Round {round} Scores";
            }

            if (networkService == null || gridService == null) return;

            var cumulativeScores = gameManager?.GetCumulativeScores() ?? new Dictionary<int, int>();

            foreach (var playerId in networkService.GetPlayerIds())
            {
                var fiefs = FiefCalculator.CalculateFiefs(playerId, gridService);
                int roundScore = 0;
                foreach (var fief in fiefs) roundScore += fief.Score;

                int cumulative = cumulativeScores.TryGetValue(playerId, out int cs) ? cs : roundScore;
                string playerName = networkService.GetPlayerName(playerId) ?? $"Player {playerId}";

                CreateScoreEntry(playerName, fiefs, roundScore, cumulative);
            }
        }

        private void ShowFinalScores(Dictionary<int, int> scores)
        {
            if (scoreboardPanel == null) return;
            scoreboardPanel.SetActive(true);

            ClearEntries();

            if (titleText != null) titleText.text = "GAME OVER";

            var networkService = ServiceLocator.GetService<INetworkService>();

            foreach (var kvp in scores)
            {
                string playerName = networkService?.GetPlayerName(kvp.Key) ?? $"Player {kvp.Key}";
                CreateScoreEntry(playerName, null, kvp.Value, kvp.Value);
            }
        }

        private void CreateScoreEntry(string playerName, List<Fief> fiefs, int roundScore, int cumulative)
        {
            if (scoreEntryContainer == null || scoreEntryPrefab == null) return;

            var entryObj = Instantiate(scoreEntryPrefab, scoreEntryContainer);
            var entry = entryObj.GetComponent<ScoreEntry>();

            if (entry != null)
            {
                entry.SetData(playerName, roundScore, cumulative, fiefs, fiefDetailPrefab);
            }

            _scoreEntries.Add(entryObj);
        }

        private void HideScoreboard()
        {
            if (scoreboardPanel != null)
                scoreboardPanel.SetActive(false);
            ClearEntries();
        }

        private void ClearEntries()
        {
            foreach (var entry in _scoreEntries)
            {
                if (entry != null) Destroy(entry);
            }
            _scoreEntries.Clear();
        }
    }
}
