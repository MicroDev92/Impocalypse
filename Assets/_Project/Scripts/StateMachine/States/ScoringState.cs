using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using _Project.Scripts.Utility;
using Core;
using Core.Events;
using Draft.Services;
using Scoring;

namespace StateMachine.States
{
    /// <summary>
    /// State for the scoring phase.
    /// Calculates fief-based scores (Strength x Wealth) and transitions.
    /// Shows scores for a few seconds before advancing.
    /// </summary>
    public class ScoringState : GameStateBase
    {
        public override string StateName => "Scoring";

        private IEventBus _eventBus;
        private IGameManager _gameManager;
        private Dictionary<int, int> _playerScores = new();
        private float _timer;
        private bool _transitioned;

        private const float ScoringDisplayTime = 4f;

        public override void Enter()
        {
            Debug.Log("[ScoringState] Entering scoring state");

            ServiceLocator.TryGetService(out _eventBus);
            _gameManager = ServiceLocator.GetService<IGameManager>();

            _timer = 0f;
            _transitioned = false;

            CalculateScores();

            int round = _gameManager?.CurrentRound ?? 1;
            int maxRounds = _gameManager?.MaxRounds ?? 4;
            string status = round >= maxRounds
                ? "Final scoring!"
                : $"Round {round} scoring...";

            _eventBus?.Publish(new UpdateHUDEvent(round, GamePhase.Scoring, status));
        }

        public override void Update()
        {
            if (_transitioned) return;

            _timer += Time.deltaTime;
            if (_timer >= ScoringDisplayTime)
            {
                _transitioned = true;
                Advance();
            }
        }

        public override void Exit()
        {
            Debug.Log("[ScoringState] Exiting scoring state");
        }

        private void CalculateScores()
        {
            Debug.Log("[ScoringState] Calculating fief-based scores...");

            var resourceService = ServiceLocator.GetService<IResourceService>();
            var gridService = ServiceLocator.GetService<IGridService>();
            var networkService = ServiceLocator.GetService<INetworkService>();

            if (resourceService != null)
            {
                _playerScores = resourceService.GetAllPlayerScores();

                // Log detailed fief breakdown
                if (gridService != null && networkService != null)
                {
                    foreach (var playerId in networkService.GetPlayerIds())
                    {
                        var fiefs = FiefCalculator.CalculateFiefs(playerId, gridService);
                        Debug.Log($"[ScoringState] Player {playerId}: {fiefs.Count} fiefs");

                        for (int i = 0; i < fiefs.Count; i++)
                        {
                            var fief = fiefs[i];
                            Debug.Log($"  Fief {i + 1}: {fief.Tiles.Count} tiles, Str={fief.Strength}, Wealth={fief.Wealth}, Score={fief.Score}");

                            _eventBus?.Publish(new FiefScoredEvent(
                                playerId, fief.Tiles.Count, fief.Strength, fief.Wealth, fief.Score));
                        }

                        int total = fiefs.Sum(f => f.Score);
                        Debug.Log($"  Total: {total} points");
                    }
                }
            }

            // Fallback
            if (_playerScores.Count == 0)
            {
                var networkService2 = ServiceLocator.GetService<INetworkService>();
                if (networkService2 != null)
                {
                    foreach (var playerId in networkService2.GetPlayerIds())
                    {
                        _playerScores[playerId] = 0;
                    }
                }
            }

            // Add round scores to cumulative
            _gameManager?.AddRoundScores(_playerScores);

            foreach (var kvp in _playerScores)
            {
                Debug.Log($"[ScoringState] Player {kvp.Key}: {kvp.Value} points this round");
            }
        }

        private void Advance()
        {
            if (_gameManager != null && _gameManager.CurrentRound < _gameManager.MaxRounds)
            {
                Debug.Log("[ScoringState] Non-final round, advancing to next round");
                _gameManager.StartNewRound();
            }
            else
            {
                // Final round — score secret parchments, then game over
                var cumulativeScores = _gameManager?.GetCumulativeScores() ?? _playerScores;
                AddParchmentBonuses(cumulativeScores);
                _eventBus?.Publish(new GameEndedEvent(cumulativeScores));

                var stateMachine = ServiceLocator.GetService<GameStateMachine>();
                stateMachine?.ChangeState<GameOverState>();
            }
        }

        private void AddParchmentBonuses(Dictionary<int, int> scores)
        {
            var draft = ServiceLocator.GetService<IDraftService>();
            var grid = ServiceLocator.GetService<IGridService>();
            var network = ServiceLocator.GetService<INetworkService>();
            if (draft == null || grid == null || network == null) return;

            foreach (var pid in network.GetPlayerIds())
            {
                int bonus = 0;
                foreach (var parchment in draft.GetPlayerParchments(pid))
                    bonus += ParchmentEvaluator.Evaluate(parchment, pid, grid);

                if (bonus != 0)
                {
                    scores[pid] = (scores.TryGetValue(pid, out int s) ? s : 0) + bonus;
                    Debug.Log($"[ScoringState] Player {pid} parchment bonus: +{bonus}");
                }
            }
        }

        public IReadOnlyDictionary<int, int> GetScores()
        {
            return _playerScores;
        }
    }
}
