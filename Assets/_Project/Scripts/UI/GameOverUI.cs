using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using _Project.Scripts.Utility;
using Core;
using Core.Events;
using Scoring;
using StateMachine;
using StateMachine.States;

/// <summary>
/// UI controller for the game over panel.
/// All layout is configured in the Editor — this script shows/hides the panel,
/// populates score entries, and handles button clicks.
/// </summary>
public class GameOverUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMP_Text winnerText;
    [SerializeField] private Transform scoreContainer;
    [SerializeField] private GameObject scoreEntryPrefab;
    [SerializeField] private GameObject fiefDetailPrefab;
    [SerializeField] private Button playAgainButton;
    [SerializeField] private Button returnToMenuButton;

    private IEventBus _eventBus;
    private readonly List<GameObject> _entries = new();

    private void Start()
    {
        ServiceLocator.TryGetService(out _eventBus);

        if (_eventBus != null)
        {
            _eventBus.Subscribe<GameEndedEvent>(OnGameEnded);
            _eventBus.Subscribe<GamePhaseChangedEvent>(OnPhaseChanged);
        }

        if (playAgainButton != null)
            playAgainButton.onClick.AddListener(OnPlayAgain);

        if (returnToMenuButton != null)
            returnToMenuButton.onClick.AddListener(OnReturnToMenu);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_eventBus != null)
        {
            _eventBus.Unsubscribe<GameEndedEvent>(OnGameEnded);
            _eventBus.Unsubscribe<GamePhaseChangedEvent>(OnPhaseChanged);
        }
    }

    private void OnPhaseChanged(GamePhaseChangedEvent evt)
    {
        if (evt.NewPhase == GamePhase.GameOver)
        {
            // Panel will be shown when GameEndedEvent arrives with scores
        }
        else
        {
            Hide();
        }
    }

    private void OnGameEnded(GameEndedEvent evt)
    {
        Show(evt.PlayerScores);
    }

    private void Show(Dictionary<int, int> scores)
    {
        if (gameOverPanel == null) return;

        ClearEntries();
        gameOverPanel.SetActive(true);

        var networkService = ServiceLocator.GetService<INetworkService>();
        var gridService = ServiceLocator.GetService<IGridService>();

        if (scores == null || scores.Count == 0) return;

        // Determine winner
        var sorted = scores.OrderByDescending(s => s.Value).ToList();
        string winnerName = networkService?.GetPlayerName(sorted[0].Key) ?? $"Player {sorted[0].Key}";

        if (winnerText != null)
            winnerText.text = $"{winnerName} Wins! ({sorted[0].Value} pts)";

        // Create score entries
        foreach (var kvp in sorted)
        {
            string playerName = networkService?.GetPlayerName(kvp.Key) ?? $"Player {kvp.Key}";

            List<Fief> fiefs = null;
            if (gridService != null)
            {
                fiefs = FiefCalculator.CalculateFiefs(kvp.Key, gridService);
            }

            CreateEntry(playerName, kvp.Value, fiefs);
        }
    }

    private void Hide()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
        ClearEntries();
    }

    private void CreateEntry(string playerName, int totalScore, List<Fief> fiefs)
    {
        if (scoreContainer == null || scoreEntryPrefab == null) return;

        var entryObj = Instantiate(scoreEntryPrefab, scoreContainer);
        var entry = entryObj.GetComponent<ScoreEntry>();

        if (entry != null)
        {
            entry.SetData(playerName, totalScore, totalScore, fiefs, fiefDetailPrefab);
        }

        _entries.Add(entryObj);
    }

    private void ClearEntries()
    {
        foreach (var entry in _entries)
        {
            if (entry != null) Destroy(entry);
        }
        _entries.Clear();
    }

    private void OnPlayAgain()
    {
        var stateMachine = ServiceLocator.GetService<GameStateMachine>();
        var gameOverState = stateMachine?.GetState<GameOverState>();
        gameOverState?.PlayAgain();
    }

    private void OnReturnToMenu()
    {
        var stateMachine = ServiceLocator.GetService<GameStateMachine>();
        var gameOverState = stateMachine?.GetState<GameOverState>();
        gameOverState?.ReturnToMenu();
    }
}
