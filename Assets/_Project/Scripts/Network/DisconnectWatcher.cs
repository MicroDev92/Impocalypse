using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using _Project.Scripts.Utility;
using Core;
using Core.Events;
using StateMachine;
using StateMachine.States;

/// <summary>
/// Ends the game gracefully when someone drops, instead of leaving the rest of
/// the table staring at a board that will never advance.
///
/// Why this is needed: phase progression is master-authoritative. The draft and
/// build timers only run on the master client, so if the MASTER disconnects
/// mid-phase nobody is left to end the phase and the game hangs forever. A
/// non-master dropping is milder - the master's timer eventually auto-picks for
/// them - but the game would then play on with a ghost who can still win.
///
/// Deliberately needs no network event code: Photon delivers OnPlayerLeftRoom to
/// every remaining client, so each one reaches the same conclusion on its own.
/// That matters because the one client we cannot rely on to broadcast anything
/// is the master - and the master leaving is exactly the case that hangs.
///
/// Policy is the roadmap's B1 minimum: detect, end, say who left. Continuing the
/// game without the dropped player is a bigger question (what happens to their
/// territory? do their domains still score?) and Bunny Kingdom has no answer for
/// it - so it stays out until playtests say it's worth designing.
/// </summary>
public class DisconnectWatcher : MonoBehaviour
{
    private INetworkService _networkService;
    private IEventBus _eventBus;
    private IGameManager _gameManager;

    /// <summary>Creates the watcher if it isn't up yet. Called by GameBootstrapper
    /// so this needs no scene wiring and can't be lost to a prefab edit.</summary>
    public static void EnsureExists()
    {
        if (FindAnyObjectByType<DisconnectWatcher>() != null) return;

        var go = new GameObject("[DisconnectWatcher]");
        go.AddComponent<DisconnectWatcher>();
        DontDestroyOnLoad(go);
    }

    // Coroutine Start: services register during Awake, so wait a frame before
    // resolving them (same reason GameBootstrapper defers its first transition).
    private IEnumerator Start()
    {
        yield return null;

        ServiceLocator.TryGetService(out _eventBus);
        ServiceLocator.TryGetService(out _gameManager);

        if (!ServiceLocator.TryGetService(out _networkService))
        {
            Debug.LogWarning("[DisconnectWatcher] No INetworkService - disconnects will not be handled.");
            yield break;
        }

        _networkService.OnPlayerLeft += HandlePlayerLeft;
        _networkService.OnDisconnectedFromServer += HandleLocalDisconnect;
    }

    private void OnDestroy()
    {
        if (_networkService == null) return;

        _networkService.OnPlayerLeft -= HandlePlayerLeft;
        _networkService.OnDisconnectedFromServer -= HandleLocalDisconnect;
    }

    private void HandlePlayerLeft(int actorNumber, string nickname)
    {
        if (!IsGameLive()) return;

        string who = string.IsNullOrWhiteSpace(nickname) ? $"Player {actorNumber}" : nickname;
        Debug.LogWarning($"[DisconnectWatcher] {who} left mid-game - ending the game.");

        EndGameEarly($"{who} disconnected");
    }

    private void HandleLocalDisconnect(string cause)
    {
        if (!IsGameLive()) return;

        Debug.LogWarning($"[DisconnectWatcher] Lost connection ({cause}) - ending the game.");

        EndGameEarly("You lost connection to the server");
    }

    /// <summary>
    /// Only Draft/Build/Scoring can strand anyone. Lobby drops belong to
    /// GameRoomUI, and a drop after the game is over is just someone closing
    /// the results screen. This guard also makes the handler self-limiting:
    /// once we end the game the phase is GameOver, so a second drop is ignored.
    /// </summary>
    private bool IsGameLive()
    {
        if (_gameManager == null) return false;

        var phase = _gameManager.CurrentPhase;
        return phase == GamePhase.Draft || phase == GamePhase.Build || phase == GamePhase.Scoring;
    }

    private void EndGameEarly(string reason)
    {
        var scores = _gameManager?.GetCumulativeScores() ?? new Dictionary<int, int>();

        // Set the reason before the state change: GameOverState announces itself
        // in Enter(), so it has to know by then that there is no winner to name.
        var stateMachine = ServiceLocator.GetService<GameStateMachine>();
        var gameOverState = stateMachine?.GetState<GameOverState>();
        if (gameOverState != null) gameOverState.EndReason = reason;

        // Same order as ScoringState's normal ending: scores first so the results
        // panel is populated, then the phase change.
        _eventBus?.Publish(new GameEndedEvent(scores, reason));
        _gameManager?.EndGame();
    }
}
