using UnityEngine;
using _Project.Scripts.Utility;
using Core.Events;

namespace StateMachine.States
{
    /// <summary>
    /// State for the lobby/waiting room phase.
    /// Players wait for host to start the game.
    /// </summary>
    public class LobbyState : GameStateBase
    {
        public override string StateName => "Lobby";

        private INetworkService _networkService;
        private IEventBus _eventBus;

        public override void Enter()
        {
            Debug.Log("[LobbyState] Entering lobby state");

            _networkService = ServiceLocator.GetService<INetworkService>();
            ServiceLocator.TryGetService(out _eventBus);

            // Subscribe to player events
            if (_networkService != null)
            {
                _networkService.OnPlayerJoined += OnPlayerJoined;
                _networkService.OnPlayerLeft += OnPlayerLeft;
            }

            // Update UI
            _eventBus?.Publish(new ShowStatusEvent(
                _networkService?.IsMasterClient == true
                    ? "You are the host. Press Start when ready."
                    : "Waiting for host to start..."));
        }

        public override void Exit()
        {
            Debug.Log("[LobbyState] Exiting lobby state");

            if (_networkService != null)
            {
                _networkService.OnPlayerJoined -= OnPlayerJoined;
                _networkService.OnPlayerLeft -= OnPlayerLeft;
            }
        }

        private void OnPlayerJoined(int actorNumber, string nickname)
        {
            Debug.Log($"[LobbyState] Player joined: {nickname}");
            _eventBus?.Publish(new PlayerJoinedEvent
            {
                ActorNumber = actorNumber,
                PlayerName = nickname,
                IsLocal = false
            });
        }

        private void OnPlayerLeft(int actorNumber, string nickname)
        {
            Debug.Log($"[LobbyState] Player left: {nickname}");
            _eventBus?.Publish(new PlayerLeftEvent
            {
                ActorNumber = actorNumber,
                PlayerName = nickname
            });
        }

        /// <summary>
        /// Called when host starts the game.
        /// </summary>
        public void StartGame()
        {
            if (_networkService?.IsMasterClient != true)
            {
                Debug.LogWarning("[LobbyState] Only host can start the game");
                return;
            }

            var stateMachine = ServiceLocator.GetService<GameStateMachine>();
            stateMachine?.ChangeState<DraftState>();
        }
    }
}
