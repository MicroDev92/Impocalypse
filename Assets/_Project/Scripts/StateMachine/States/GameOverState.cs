using System.Linq;
using UnityEngine;
using _Project.Scripts.Utility;
using Core.Events;

namespace StateMachine.States
{
    /// <summary>
    /// State for the game over screen.
    /// Displays final results via the HUD status text.
    /// </summary>
    public class GameOverState : GameStateBase
    {
        public override string StateName => "GameOver";

        private IEventBus _eventBus;

        /// <summary>
        /// Non-null when the game was abandoned (someone disconnected) instead of
        /// finishing. Set by DisconnectWatcher *before* the state change, because
        /// Enter() announces the result immediately and must not name a winner
        /// for a game nobody won.
        /// </summary>
        public string EndReason { get; set; }

        public override void Enter()
        {
            Debug.Log("[GameOverState] Entering game over state");

            ServiceLocator.TryGetService(out _eventBus);

            DisplayResults();
        }

        public override void Exit()
        {
            Debug.Log("[GameOverState] Exiting game over state");

            // The state instance is registered once and reused, so a stale reason
            // would mislabel the next game's ending.
            EndReason = null;
        }

        private void DisplayResults()
        {
            if (!string.IsNullOrEmpty(EndReason))
            {
                _eventBus?.Publish(new ShowStatusEvent($"Game ended — {EndReason}"));
                Debug.Log($"[GameOverState] Game abandoned: {EndReason}");
                return;
            }

            var scoringState = ServiceLocator.GetService<GameStateMachine>()?.GetState<ScoringState>();
            var scores = scoringState?.GetScores();
            var networkService = ServiceLocator.GetService<INetworkService>();

            if (scores != null && scores.Count > 0 && networkService != null)
            {
                // Build score summary
                var sorted = scores.OrderByDescending(s => s.Value).ToList();
                string winnerName = networkService.GetPlayerName(sorted[0].Key) ?? $"Player {sorted[0].Key}";

                string summary = $"GAME OVER! Winner: {winnerName} ({sorted[0].Value} {GameLexicon.ScoreName})\n";
                foreach (var kvp in sorted)
                {
                    string name = networkService.GetPlayerName(kvp.Key) ?? $"Player {kvp.Key}";
                    summary += $"\n{name}: {kvp.Value} {GameLexicon.ScoreName}";
                }

                _eventBus?.Publish(new ShowStatusEvent(summary));
                Debug.Log($"[GameOverState] {summary}");
            }
            else
            {
                _eventBus?.Publish(new ShowStatusEvent("Game Over!"));
            }
        }

        public void PlayAgain()
        {
            var stateMachine = ServiceLocator.GetService<GameStateMachine>();
            stateMachine?.ChangeState<LobbyState>();
        }

        public void ReturnToMenu()
        {
            var networkService = ServiceLocator.GetService<INetworkService>();

            // Leave the ROOM, not the server. Disconnect() dropped the Photon
            // connection, and NetworkService is DontDestroyOnLoad so its Start() -
            // which is the only thing that calls Connect() - never ran again. The menu
            // gates its buttons on IsConnected, so it came back permanently dead.
            networkService?.LeaveRoom();
            UnityEngine.SceneManagement.SceneManager.LoadScene("Launcher");
        }
    }
}
