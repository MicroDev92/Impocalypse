using UnityEngine;
using _Project.Scripts.Utility;
using Core;
using Core.Events;
using Draft.Services;

namespace StateMachine.States
{
    /// <summary>
    /// State for the draft phase.
    /// Triggers DraftService to deal cards and handle picks.
    /// </summary>
    public class DraftState : GameStateBase
    {
        public override string StateName => "Draft";

        private IEventBus _eventBus;
        private IGameManager _gameManager;

        public override void Enter()
        {
            Debug.Log("[DraftState] Entering draft state");

            ServiceLocator.TryGetService(out _eventBus);
            _gameManager = ServiceLocator.GetService<IGameManager>();

            int round = _gameManager?.CurrentRound ?? 1;
            _eventBus?.Publish(new UpdateHUDEvent(round, GamePhase.Draft, "Pick 2 cards"));

            var draftService = ServiceLocator.GetService<IDraftService>();
            if (round == 1)
            {
                // First round: build deck and start fresh
                draftService?.StartDraft();
            }
            else
            {
                // Subsequent rounds: deal from existing deck
                draftService?.StartNewGameRound();
            }
        }

        public override void Exit()
        {
            Debug.Log("[DraftState] Exiting draft state");
        }
    }
}
