using System.Collections.Generic;
using _Project.Scripts.Utility;
using Core;
using Core.Events;
using Photon.Pun;
using Photon.Realtime;
using StateMachine;
using StateMachine.States;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Main game manager that coordinates game state and player interactions.
/// Implements IGameManager for loose coupling and better testability.
/// </summary>
public class GameManager : MonoBehaviourPunCallbacks, IGameManager
{
    // IGameManager Properties
    public int CurrentRound { get; private set; } = 1;
    public GamePhase CurrentPhase { get; private set; } = GamePhase.Lobby;
    public int TotalPlayers => ServiceLocator.GetService<INetworkService>()?.PlayerCount ?? 1;
    public int CardsPerPlayer => Rules?.GetCardsPerPlayer(TotalPlayers) ?? 4;
    public bool IsClockwise => Rules?.IsClockwise(CurrentRound) ?? false;
    public int MaxRounds { get; } = 4;
    public GameRules Rules { get; private set; }

    private readonly Dictionary<int, int> _cumulativeScores = new();

    private void Awake()
    {
        ServiceLocator.RegisterService<IGameManager>(this);

        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        PhotonNetwork.AddCallbackTarget(this);

        if (!Rules)
            Rules = Resources.Load<GameRules>("DevRules");
    }

    private void OnDestroy()
    {
        ServiceLocator.UnregisterService<IGameManager>();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Publish scene-loaded events so UI controllers can react
        if (ServiceLocator.TryGetService<IEventBus>(out var eventBus))
        {
            if (scene.name == "GameRoom")
            {
                // GameRoomUI handles its own setup via EventBus
                eventBus.Publish(new ShowStatusEvent(
                    ServiceLocator.GetService<INetworkService>()?.IsMasterClient == true
                        ? "You are the host"
                        : "Waiting for host to start..."));
            }
            else if (scene.name == "GameScene" && CurrentPhase == GamePhase.Lobby)
            {
                // If we load into the game scene from the lobby, start the draft!
                CurrentPhase = GamePhase.Draft;
                CurrentRound = 1;
                Debug.Log("[GameManager] Game scene loaded, advancing to Draft Phase.");
            }
        }
    }

    // IGameManager Implementation
    public void AdvancePhase()
    {
        var stateMachine = ServiceLocator.GetService<GameStateMachine>();

        if (CurrentPhase == GamePhase.Draft)
        {
            CurrentPhase = GamePhase.Build;
            stateMachine?.ChangeState<BuildState>();
        }
        else if (CurrentPhase == GamePhase.Build)
        {
            CurrentPhase = GamePhase.Scoring;
            stateMachine?.ChangeState<ScoringState>();
        }
    }

    public bool IsGameOver()
    {
        return CurrentRound >= MaxRounds && CurrentPhase == GamePhase.Build;
    }

    public void StartNewRound()
    {
        CurrentRound++;
        CurrentPhase = GamePhase.Draft;

        var stateMachine = ServiceLocator.GetService<GameStateMachine>();
        stateMachine?.ChangeState<DraftState>();
    }

    public void EndGame()
    {
        CurrentPhase = GamePhase.GameOver;
        Debug.Log("[GameManager] Game ended!");

        var stateMachine = ServiceLocator.GetService<GameStateMachine>();
        stateMachine?.ChangeState<GameOverState>();
    }

    public void AddRoundScores(Dictionary<int, int> roundScores)
    {
        foreach (var kvp in roundScores)
        {
            if (_cumulativeScores.ContainsKey(kvp.Key))
                _cumulativeScores[kvp.Key] += kvp.Value;
            else
                _cumulativeScores[kvp.Key] = kvp.Value;

            Debug.Log($"[GameManager] Player {kvp.Key} cumulative score: {_cumulativeScores[kvp.Key]}");
        }
    }

    public Dictionary<int, int> GetCumulativeScores()
    {
        return new Dictionary<int, int>(_cumulativeScores);
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"Player joined: {newPlayer.NickName}");
        if (ServiceLocator.TryGetService<IEventBus>(out var eventBus))
        {
            eventBus.Publish(new PlayerJoinedEvent
            {
                ActorNumber = newPlayer.ActorNumber,
                PlayerName = newPlayer.NickName,
                IsLocal = newPlayer.IsLocal
            });
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"Player left: {otherPlayer.NickName}");
        if (ServiceLocator.TryGetService<IEventBus>(out var eventBus))
        {
            eventBus.Publish(new PlayerLeftEvent
            {
                ActorNumber = otherPlayer.ActorNumber,
                PlayerName = otherPlayer.NickName
            });
        }
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        Debug.Log($"Master client switched to: {newMasterClient.NickName}");
        if (ServiceLocator.TryGetService<IEventBus>(out var eventBus))
        {
            eventBus.Publish(new ShowStatusEvent(
                newMasterClient.IsLocal
                    ? "You are now the host"
                    : $"{newMasterClient.NickName} is now the host"));
        }
    }
}
