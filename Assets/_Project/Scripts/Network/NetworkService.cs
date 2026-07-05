using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections.Generic;
using System.Linq;
using _Project.Scripts.Utility;
using ExitGames.Client.Photon;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Photon-based implementation of INetworkService.
/// Encapsulates all Photon-specific code behind a clean interface.
/// </summary>
public class NetworkService : MonoBehaviourPunCallbacks, INetworkService, IOnEventCallback
{
    [Header("Connection Settings")]
    [SerializeField] private string gameVersion = "1.0";
    [SerializeField] private string defaultRoomName = "LocalTestRoom";
    [SerializeField] private byte maxPlayersPerRoom = 4;
    [SerializeField] private bool autoConnect = true;
    [SerializeField] private string lobbySceneName = "GameRoom";

    // Cached player list for interface
    private List<int> _cachedPlayerIds = new();

    // ============================================
    // SINGLETON & LIFECYCLE
    // ============================================

    private static NetworkService _instance;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            ServiceLocator.RegisterService<INetworkService>(this);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        if (autoConnect)
        {
            Connect();
        }
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            ServiceLocator.UnregisterService<INetworkService>();
            _instance = null;
        }
    }

    // ============================================
    // INetworkService - CONNECTION STATE
    // ============================================

    public bool IsConnected => PhotonNetwork.IsConnected;
    public bool IsMasterClient => PhotonNetwork.IsMasterClient;
    public int LocalPlayerId => PhotonNetwork.LocalPlayer?.ActorNumber ?? -1;
    public string LocalPlayerName => PhotonNetwork.LocalPlayer?.NickName ?? string.Empty;
    public int PlayerCount => PhotonNetwork.CurrentRoom?.PlayerCount ?? 0;
    public string CurrentRoomName => PhotonNetwork.CurrentRoom?.Name;

    // ============================================
    // INetworkService - PLAYER INFO
    // ============================================

    public IReadOnlyList<int> GetPlayerIds()
    {
        _cachedPlayerIds.Clear();

        if (PhotonNetwork.CurrentRoom != null)
        {
            foreach (var player in PhotonNetwork.PlayerList)
            {
                _cachedPlayerIds.Add(player.ActorNumber);
            }
        }

        return _cachedPlayerIds;
    }

    public string GetPlayerName(int actorNumber)
    {
        if (PhotonNetwork.CurrentRoom == null) return null;

        var player = PhotonNetwork.CurrentRoom.GetPlayer(actorNumber);
        return player?.NickName;
    }

    public bool IsPlayerMaster(int actorNumber)
    {
        if (PhotonNetwork.CurrentRoom == null) return false;

        var player = PhotonNetwork.CurrentRoom.GetPlayer(actorNumber);
        return player != null && player.IsMasterClient;
    }

    // ============================================
    // INetworkService - EVENTS
    // ============================================

    public event Action OnConnectedToServer;
    public event Action<string> OnDisconnectedFromServer;
    public event Action<int, string> OnPlayerJoined;
    public event Action<int, string> OnPlayerLeft;
    public event Action<int, string> OnMasterClientChanged;
    public event Action<byte, object, int> OnEventReceived;
    public event Action OnRoomJoined;
    public event Action<string> OnRoomJoinFailed;

    // ============================================
    // INetworkService - SENDING
    // ============================================

    public void SendToAll(byte eventCode, object data)
    {
        if (!IsConnected)
        {
            Debug.LogWarning("[NetworkService] Cannot send event: not connected");
            return;
        }

        try
        {
            PhotonNetwork.RaiseEvent(eventCode, data,
                new RaiseEventOptions { Receivers = ReceiverGroup.All },
                SendOptions.SendReliable);

            Debug.Log($"[NetworkService] Sent event {eventCode} to all");
        }
        catch (Exception e)
        {
            Debug.LogError($"[NetworkService] Failed to send event {eventCode}: {e.Message}");
        }
    }

    public void SendToPlayers(byte eventCode, object data, int[] targetActorIds)
    {
        if (!IsConnected)
        {
            Debug.LogWarning("[NetworkService] Cannot send event: not connected");
            return;
        }

        if (targetActorIds == null || targetActorIds.Length == 0)
        {
            Debug.LogWarning("[NetworkService] Cannot send event: no target actors");
            return;
        }

        try
        {
            PhotonNetwork.RaiseEvent(eventCode, data,
                new RaiseEventOptions { TargetActors = targetActorIds },
                SendOptions.SendReliable);

            Debug.Log($"[NetworkService] Sent event {eventCode} to actors: {string.Join(", ", targetActorIds)}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[NetworkService] Failed to send event {eventCode}: {e.Message}");
        }
    }

    public void SendToMaster(byte eventCode, object data)
    {
        if (!IsConnected)
        {
            Debug.LogWarning("[NetworkService] Cannot send event: not connected");
            return;
        }

        try
        {
            PhotonNetwork.RaiseEvent(eventCode, data,
                new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient },
                SendOptions.SendReliable);

            Debug.Log($"[NetworkService] Sent event {eventCode} to master");
        }
        catch (Exception e)
        {
            Debug.LogError($"[NetworkService] Failed to send event {eventCode}: {e.Message}");
        }
    }

    // ============================================
    // INetworkService - CONNECTION MANAGEMENT
    // ============================================

    public void Connect()
    {
        if (IsConnected)
        {
            Debug.Log("[NetworkService] Already connected");
            return;
        }

        PhotonNetwork.AutomaticallySyncScene = true;

        if (string.IsNullOrEmpty(PhotonNetwork.NickName))
        {
            PhotonNetwork.NickName = "Player_" + UnityEngine.Random.Range(1000, 9999);
        }

        PhotonNetwork.GameVersion = gameVersion;
        PhotonNetwork.ConnectUsingSettings();

        Debug.Log("[NetworkService] Connecting to Photon...");
    }

    public void Disconnect()
    {
        if (IsConnected)
        {
            PhotonNetwork.Disconnect();
            Debug.Log("[NetworkService] Disconnecting from Photon");
        }
    }

    public void LoadScene(string sceneName)
    {
        if (!IsConnected)
        {
            Debug.LogWarning("[NetworkService] Cannot load scene: not connected");
            return;
        }

        if (!IsMasterClient)
        {
            Debug.LogWarning("[NetworkService] Only master client can load scenes");
            return;
        }

        try
        {
            PhotonNetwork.LoadLevel(sceneName);
            Debug.Log($"[NetworkService] Loading scene: {sceneName}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[NetworkService] Failed to load scene {sceneName}: {e.Message}");
        }
    }

    public void SetPlayerName(string nickname)
    {
        PhotonNetwork.NickName = nickname;
        Debug.Log($"[NetworkService] Set player name to: {nickname}");
    }

    public void SetRoomProperty(string key, object value)
    {
        if (PhotonNetwork.CurrentRoom == null)
        {
            Debug.LogWarning($"[NetworkService] Cannot set room property '{key}' - not in a room");
            return;
        }

        PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable { { key, value } });
        Debug.Log($"[NetworkService] Room property set: {key} = {value}");
    }

    public object GetRoomProperty(string key)
    {
        var props = PhotonNetwork.CurrentRoom?.CustomProperties;
        if (props != null && props.TryGetValue(key, out var value))
            return value;
        return null;
    }

    // ============================================
    // PHOTON CALLBACKS
    // ============================================

    public override void OnConnectedToMaster()
    {
        Debug.Log("[NetworkService] Connected to Master Server");
        OnConnectedToServer?.Invoke();

        // Auto-join default room. The map seed is baked in at CREATION time: room
        // properties set here are delivered to every client as part of joining, so
        // they are guaranteed present before any scene loads. (Setting them later via
        // SetCustomProperties races the scene switch - learned the hard way.)
        PhotonNetwork.JoinOrCreateRoom(defaultRoomName, new RoomOptions
        {
            MaxPlayers = maxPlayersPerRoom,
            IsOpen = true,
            IsVisible = true,
            CustomRoomProperties = new Hashtable
            {
                { "mapSeed", UnityEngine.Random.Range(int.MinValue, int.MaxValue) }
            }
        }, TypedLobby.Default);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"[NetworkService] Joined room: {PhotonNetwork.CurrentRoom.Name}");

        foreach (var player in PhotonNetwork.PlayerList)
        {
            Debug.Log($"[NetworkService] Player in room: {player.NickName} (Actor: {player.ActorNumber})");
        }

        OnRoomJoined?.Invoke();

        // Load lobby scene if configured
        if (!string.IsNullOrEmpty(lobbySceneName) && IsMasterClient)
        {
            SceneManager.LoadScene(lobbySceneName);
        }
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"[NetworkService] Failed to join room: {message} (Code: {returnCode})");
        OnRoomJoinFailed?.Invoke(message);
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"[NetworkService] Failed to create room: {message} (Code: {returnCode})");
        OnRoomJoinFailed?.Invoke(message);
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"[NetworkService] Player joined: {newPlayer.NickName} (Actor: {newPlayer.ActorNumber})");
        OnPlayerJoined?.Invoke(newPlayer.ActorNumber, newPlayer.NickName);
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"[NetworkService] Player left: {otherPlayer.NickName} (Actor: {otherPlayer.ActorNumber})");
        OnPlayerLeft?.Invoke(otherPlayer.ActorNumber, otherPlayer.NickName);
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        Debug.Log($"[NetworkService] Master client switched to: {newMasterClient.NickName}");
        OnMasterClientChanged?.Invoke(newMasterClient.ActorNumber, newMasterClient.NickName);
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.Log($"[NetworkService] Disconnected: {cause}");
        OnDisconnectedFromServer?.Invoke(cause.ToString());
    }

    // ============================================
    // PHOTON EVENT CALLBACK
    // ============================================

    public void OnEvent(EventData photonEvent)
    {
        Debug.Log($"[NetworkService] Received event {photonEvent.Code} from actor {photonEvent.Sender}");
        OnEventReceived?.Invoke(photonEvent.Code, photonEvent.CustomData, photonEvent.Sender);
    }
}
