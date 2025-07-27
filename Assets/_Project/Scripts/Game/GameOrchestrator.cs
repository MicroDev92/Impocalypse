using System.Collections.Generic;
using System.Linq;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using _Project.Scripts.Data;

public class GameOrchestrator : MonoBehaviour, IOnEventCallback
{
    private Dictionary<int, List<string>> pendingPicks = new();
    private Dictionary<int, List<string>> playerHands = new();
    private int totalPlayers;

    private void Start()
    {
        totalPlayers = PhotonNetwork.CurrentRoom.PlayerCount;
        PhotonNetwork.AddCallbackTarget(this);

        if (PhotonNetwork.IsMasterClient)
        {
            AssignPlayerIdentities();
            DealInitialCards();
        }
    }

    private void OnDestroy()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    public void OnEvent(EventData photonEvent)
    {
        switch (photonEvent.Code)
        {
            case NetworkEvents.SubmitPicks:
                HandleSubmitPicks(photonEvent);
                break;
        }
    }

    private void HandleSubmitPicks(EventData photonEvent)
    {
        var coords = ((object[])photonEvent.CustomData).Cast<string>().ToArray();
        int actorNumber = photonEvent.Sender;

        if (!pendingPicks.ContainsKey(actorNumber))
            pendingPicks[actorNumber] = new List<string>();

        pendingPicks[actorNumber].AddRange(coords);
        Debug.Log($"Received picks from player {actorNumber}");

        if (pendingPicks.Count == totalPlayers)
        {
            Debug.Log("All players submitted picks. Broadcasting imp spawns...");

            List<object> serialized = new();
            foreach (var kvp in pendingPicks)
            {
                foreach (var coord in kvp.Value)
                {
                    serialized.Add(new object[] { coord, kvp.Key });
                }
            }

            PhotonNetwork.RaiseEvent(NetworkEvents.SpawnImps, serialized.ToArray(),
                new RaiseEventOptions { Receivers = ReceiverGroup.All },
                SendOptions.SendReliable);

            // Remove picked cards from hands
            foreach (var kvp in pendingPicks)
            {
                if (playerHands.ContainsKey(kvp.Key))
                {
                    playerHands[kvp.Key].RemoveAll(c => kvp.Value.Contains(c));
                }
            }

            pendingPicks.Clear();
            GameManager.Instance.currentRound++;
            
            FindFirstObjectByType<DraftHUD>()?.SetRound(GameManager.Instance.currentRound);
            FindFirstObjectByType<DraftHUD>()?.SetPhase(GamePhase.Draft); // for the new turn


            PassHandsToNextPlayer();
        }
    }

    private void PassHandsToNextPlayer()
    {
        var playerList = PhotonNetwork.PlayerList.OrderBy(p => p.ActorNumber).ToArray();
        Dictionary<int, List<string>> newHands = new();

        for (int i = 0; i < playerList.Length; i++)
        {
            int fromIndex = GameManager.Instance.IsClockwise ? (i - 1 + playerList.Length) % playerList.Length
                                                             : (i + 1) % playerList.Length;
            int toActor = playerList[i].ActorNumber;
            int fromActor = playerList[fromIndex].ActorNumber;

            newHands[toActor] = playerHands.ContainsKey(fromActor) ? new List<string>(playerHands[fromActor]) : new List<string>();
        }

        playerHands = newHands;

        foreach (var kvp in playerHands)
        {
            PhotonNetwork.RaiseEvent(NetworkEvents.DealCards, kvp.Value.ToArray(),
                new RaiseEventOptions { TargetActors = new int[] { kvp.Key } },
                new SendOptions { Reliability = true });
        }
    }

    private void AssignPlayerIdentities()
    {
        var players = PhotonNetwork.CurrentRoom.Players.Values.OrderBy(p => p.ActorNumber).ToArray();
        string[] impColors = new[] { "red", "green", "blue", "purple" };

        List<PlayerData> playerDataList = new();
        for (int i = 0; i < players.Length; i++)
        {
            playerDataList.Add(new PlayerData
            {
                ActorNumber = players[i].ActorNumber,
                Nickname = players[i].NickName,
                ImpColor = impColors[i % impColors.Length]
            });
        }

        object[] identityArray = playerDataList.Select(p => new object[]
        {
            p.ActorNumber,
            p.Nickname,
            p.ImpColor
        }).ToArray<object>();

        PhotonNetwork.RaiseEvent(NetworkEvents.AssignPlayerIdentities, identityArray,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            SendOptions.SendReliable);
    }

    private void DealInitialCards()
    {
        int count = GameManager.Instance.CardsPerPlayer;
        var players = PhotonNetwork.CurrentRoom.Players.Values.ToList();
        var allCoords = GridGenerator.Instance.GetRandomCoords(count * players.Count);

        for (int i = 0; i < players.Count; i++)
        {
            var coords = allCoords.Skip(i * count).Take(count).ToList();
            playerHands[players[i].ActorNumber] = coords;

            PhotonNetwork.RaiseEvent(NetworkEvents.DealCards, coords.ToArray(),
                new RaiseEventOptions { TargetActors = new int[] { players[i].ActorNumber } },
                new SendOptions { Reliability = true });
        }
    }
}
