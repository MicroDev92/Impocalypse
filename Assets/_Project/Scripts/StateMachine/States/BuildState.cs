using UnityEngine;
using _Project.Scripts.Utility;
using Core;
using Core.Events;
using Construction;
using Draft.Services;

namespace StateMachine.States
{
    /// <summary>
    /// State for the build/construction phase.
    /// Players place city cards on owned tiles.
    /// If no city cards exist, auto-advances after a brief pause.
    /// </summary>
    public class BuildState : GameStateBase
    {
        public override string StateName => "Build";

        private IEventBus _eventBus;
        private IGameManager _gameManager;
        private IConstructionService _constructionService;
        private ConstructionUIHandler _constructionUI;
        private bool _advanced;
        private float _autoAdvanceTimer;
        private bool _useAutoAdvance;

        public override void Enter()
        {
            Debug.Log("[BuildState] Entering build state");

            ServiceLocator.TryGetService(out _eventBus);
            _gameManager = ServiceLocator.GetService<IGameManager>();
            ServiceLocator.TryGetService(out _constructionService);
            _constructionUI = ServiceLocator.GetService<ConstructionUIHandler>();

            _advanced = false;
            _autoAdvanceTimer = 0f;
            _useAutoAdvance = false;

            int round = _gameManager?.CurrentRound ?? 1;
            int maxRounds = _gameManager?.MaxRounds ?? 4;

            _eventBus?.Publish(new UpdateHUDEvent(round, GamePhase.Build, "CONSTRUCTION PHASE"));

            // Start construction
            if (_constructionService != null)
            {
                _constructionService.OnAllPlayersDone += OnConstructionComplete;
                _constructionService.StartConstruction();

                // Show construction UI with local player's city cards
                var networkService = ServiceLocator.GetService<INetworkService>();
                int localPlayer = networkService?.LocalPlayerId ?? -1;
                var cityCards = _constructionService.GetAvailableCityCards(localPlayer);

                if (_constructionUI != null)
                {
                    _constructionUI.Show(cityCards);
                }

                if (cityCards.Count == 0 && _constructionUI == null)
                {
                    // No UI handler and no cards — auto-advance after brief delay
                    _useAutoAdvance = true;
                    _autoAdvanceTimer = 0f;
                }
            }
            else
            {
                // No construction service — auto-advance (legacy behavior)
                _useAutoAdvance = true;
                _autoAdvanceTimer = 0f;

                string status = round >= maxRounds
                    ? $"Round {round}/{maxRounds} complete! Final scoring..."
                    : $"Round {round}/{maxRounds} complete! Next round starting...";
                _eventBus?.Publish(new UpdateHUDEvent(round, GamePhase.Build, status));
            }
        }

        public override void Exit()
        {
            Debug.Log("[BuildState] Exiting build state");

            if (_constructionService != null)
            {
                _constructionService.OnAllPlayersDone -= OnConstructionComplete;
            }

            if (_constructionUI != null)
            {
                _constructionUI.Hide();
            }
        }

        public override void Update()
        {
            if (_advanced) return;

            if (_useAutoAdvance)
            {
                _autoAdvanceTimer += Time.deltaTime;
                if (_autoAdvanceTimer >= 2.0f)
                {
                    _advanced = true;
                    EndBuildPhase();
                }
            }
            // No local fallback timeout here: phase end is master-authoritative
            // (ConstructionService's build timer or the everyone-done broadcast).
            // A per-client timeout advanced clients independently and desynced them.
        }

        private void OnConstructionComplete()
        {
            if (_advanced) return;
            _advanced = true;
            EndBuildPhase();
        }

        private void EndBuildPhase()
        {
            _gameManager?.AdvancePhase();
        }
    }
}
