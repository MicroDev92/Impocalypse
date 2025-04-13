using System.Collections.Generic;
using UnityEngine;
using Photon.Realtime;
using ExitGames.Client.Photon;
using UnityEngine.SceneManagement;

public class NetworkManager : MonoBehaviour, IConnectionCallbacks, IMatchmakingCallbacks, ILobbyCallbacks
{
    public static NetworkManager Instance;
    private LoadBalancingClient _client;

    private string _gameVersion = "1.0";
    private string _region = "eu";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ConnectToPhoton()
    {
        _client = new LoadBalancingClient();
        _client.AppId = "1e6c098d-5fbb-42e7-bebe-67943264ae46"; // Replace with your App ID
        _client.AppVersion = _gameVersion;
        _client.NickName = "Aleksa";
        _client.LoadBalancingPeer.DisconnectTimeout = 100000;
        _client.AddCallbackTarget(this);
        _client.ConnectToRegionMaster(_region);
    }

    private void Update()
    {
        _client?.Service();
    }

    public void OnConnectedToMaster()
    {
        Debug.Log("Connected to Photon Master.");
        _client.OpJoinRandomRoom();
    }

    public void OnJoinRoomFailed(short returnCode, string message)
    {
        throw new System.NotImplementedException();
    }

    public void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("No rooms found. Creating one.");
        _client.OpCreateRoom(new EnterRoomParams
        {
            RoomName = "Room_" + Random.Range(1000, 9999),
            RoomOptions = new RoomOptions { MaxPlayers = 4, IsOpen = true, IsVisible = true }
        });
    }

    public void OnJoinedRoom()
    {
        Debug.Log("Joined Room: " + _client.CurrentRoom.Name);

        // Set the current room in GameManager
        GameManager.Instance?.SetRoom(_client.CurrentRoom, _client.LocalPlayer);

        // Load GameRoom scene (don't call GameManager.OnJoinedRoom directly here)
        SceneManager.LoadScene("GameRoom");
    }


    public void OnConnected() => Debug.Log("Connected to Photon.");
    public void OnDisconnected(DisconnectCause cause) => Debug.LogWarning("Disconnected: " + cause);
    public void OnRegionListReceived(RegionHandler regionHandler)
    {
        throw new System.NotImplementedException();
    }

    public void OnCustomAuthenticationResponse(Dictionary<string, object> data)
    {
        throw new System.NotImplementedException();
    }

    public void OnCustomAuthenticationFailed(string debugMessage)
    {
        throw new System.NotImplementedException();
    }

    public void OnFriendListUpdate(List<FriendInfo> friendList)
    {
        throw new System.NotImplementedException();
    }

    public void OnCreatedRoom() => Debug.Log("Room created.");
    public void OnCreateRoomFailed(short returnCode, string message) => Debug.LogWarning("Create Room Failed: " + message);
    public void OnLeftRoom() => Debug.Log("Left Room");
    public void OnRoomListUpdate(System.Collections.Generic.List<RoomInfo> roomList) { }
    public void OnLobbyStatisticsUpdate(System.Collections.Generic.List<TypedLobbyInfo> lobbyStats) { }
    public void OnJoinedLobby() { }
    public void OnLeftLobby() { }
}
