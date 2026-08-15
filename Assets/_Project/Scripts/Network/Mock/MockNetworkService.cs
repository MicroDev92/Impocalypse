using System;
using System.Collections.Generic;
using UnityEngine;
using _Project.Scripts.Utility;

/// <summary>
/// Mock implementation of INetworkService for testing.
/// Simulates network behavior without Photon dependency.
/// </summary>
public class MockNetworkService : MonoBehaviour, INetworkService
{
    [Header("Mock Settings")]
    [SerializeField] private bool simulateAsConnected = true;
    [SerializeField] private bool simulateAsMaster = true;
    [SerializeField] private int mockLocalPlayerId = 1;
    [SerializeField] private string mockPlayerName = "MockPlayer";

    // Mock player data
    private readonly Dictionary<int, string> _mockPlayers = new();
    private readonly List<int> _mockPlayerIds = new();

    // Event tracking for tests
    private readonly List<(byte code, object data, int[] targets)> _sentEvents = new();

    // ============================================
    // INetworkService - CONNECTION STATE
    // ============================================

    public bool IsConnected => simulateAsConnected;
    public bool IsMasterClient => simulateAsMaster;
    public int LocalPlayerId => mockLocalPlayerId;
    public string LocalPlayerName => mockPlayerName;
    public int PlayerCount => _mockPlayers.Count;
    public string CurrentRoomName => _mockRoomName;
    public bool IsInRoom => _mockRoomName != null;

    private string _mockRoomName;

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
    public event Action OnRoomLeft;
    public event Action<string> OnRoomJoinFailed;

    // ============================================
    // LIFECYCLE
    // ============================================

    private void Awake()
    {
        // Add default mock player (self)
        AddMockPlayer(mockLocalPlayerId, mockPlayerName);

        ServiceLocator.RegisterService<INetworkService>(this);
        Debug.Log("[MockNetworkService] Registered as INetworkService");
    }

    private void OnDestroy()
    {
        ServiceLocator.UnregisterService<INetworkService>();
    }

    // ============================================
    // INetworkService - PLAYER INFO
    // ============================================

    public IReadOnlyList<int> GetPlayerIds()
    {
        return _mockPlayerIds;
    }

    public string GetPlayerName(int actorNumber)
    {
        return _mockPlayers.TryGetValue(actorNumber, out var name) ? name : null;
    }

    public bool IsPlayerMaster(int actorNumber)
    {
        // In mock, first player is always master
        return _mockPlayerIds.Count > 0 && _mockPlayerIds[0] == actorNumber;
    }

    // ============================================
    // INetworkService - SENDING
    // ============================================

    public void SendToAll(byte eventCode, object data)
    {
        Debug.Log($"[MockNetworkService] SendToAll: event {eventCode}");
        _sentEvents.Add((eventCode, data, null));

        // Simulate receiving own event
        OnEventReceived?.Invoke(eventCode, data, LocalPlayerId);
    }

    public void SendToPlayers(byte eventCode, object data, int[] targetActorIds)
    {
        Debug.Log($"[MockNetworkService] SendToPlayers: event {eventCode} to {string.Join(", ", targetActorIds)}");
        _sentEvents.Add((eventCode, data, targetActorIds));

        // If local player is in targets, simulate receiving
        foreach (var target in targetActorIds)
        {
            if (target == LocalPlayerId)
            {
                OnEventReceived?.Invoke(eventCode, data, LocalPlayerId);
                break;
            }
        }
    }

    public void SendToMaster(byte eventCode, object data)
    {
        Debug.Log($"[MockNetworkService] SendToMaster: event {eventCode}");
        _sentEvents.Add((eventCode, data, new[] { _mockPlayerIds[0] }));

        // If local player is master, simulate receiving
        if (IsMasterClient)
        {
            OnEventReceived?.Invoke(eventCode, data, LocalPlayerId);
        }
    }

    // ============================================
    // INetworkService - CONNECTION MANAGEMENT
    // ============================================

    public void Connect()
    {
        Debug.Log("[MockNetworkService] Connect called (simulated)");
        simulateAsConnected = true;
        OnConnectedToServer?.Invoke();

        // No room join here: connecting and joining are separate steps now, and a mock
        // that still fused them would hide exactly the bug this split exists to prevent.
    }

    public void Disconnect()
    {
        Debug.Log("[MockNetworkService] Disconnect called (simulated)");
        simulateAsConnected = false;
        OnDisconnectedFromServer?.Invoke("Mock disconnect");
    }

    public void LoadScene(string sceneName)
    {
        Debug.Log($"[MockNetworkService] LoadScene: {sceneName} (not loading in mock)");
    }

    public void SetPlayerName(string nickname)
    {
        Debug.Log($"[MockNetworkService] SetPlayerName: {nickname}");
        _mockPlayers[LocalPlayerId] = nickname;
    }

    // ============================================
    // INetworkService - ROOMS
    // ============================================

    public void QuickPlay()
    {
        Debug.Log("[MockNetworkService] QuickPlay (simulated) - always hosts, nothing to match");
        EnterMockRoom(RoomCode.Generate());
    }

    public void CreateRoom(bool isPublic)
    {
        Debug.Log($"[MockNetworkService] CreateRoom isPublic={isPublic} (simulated)");
        EnterMockRoom(RoomCode.Generate());
    }

    public void JoinRoom(string code)
    {
        string normalized = RoomCode.Normalize(code);

        if (!RoomCode.IsValid(normalized))
        {
            Debug.Log($"[MockNetworkService] JoinRoom rejected bad code: '{code}'");
            OnRoomJoinFailed?.Invoke($"Enter a {RoomCode.Length}-letter code");
            return;
        }

        Debug.Log($"[MockNetworkService] JoinRoom: {normalized} (simulated - always succeeds)");
        EnterMockRoom(normalized);
    }

    public void LeaveRoom()
    {
        if (_mockRoomName == null) return;

        Debug.Log("[MockNetworkService] LeaveRoom (simulated)");
        _mockRoomName = null;
        OnRoomLeft?.Invoke();
    }

    public void SetRoomOpen(bool open)
    {
        Debug.Log($"[MockNetworkService] SetRoomOpen: {open} (simulated)");
    }

    private void EnterMockRoom(string code)
    {
        _mockRoomName = code;
        OnRoomJoined?.Invoke();
    }

    private readonly Dictionary<string, object> _roomProperties = new();

    public void SetRoomProperty(string key, object value)
    {
        _roomProperties[key] = value;
        Debug.Log($"[MockNetworkService] Room property set: {key} = {value}");
    }

    public object GetRoomProperty(string key)
    {
        return _roomProperties.TryGetValue(key, out var value) ? value : null;
    }

    // ============================================
    // TEST HELPERS
    // ============================================

    /// <summary>
    /// Adds a mock player to the room.
    /// </summary>
    public void AddMockPlayer(int actorNumber, string nickname)
    {
        if (!_mockPlayers.ContainsKey(actorNumber))
        {
            _mockPlayers[actorNumber] = nickname;
            _mockPlayerIds.Add(actorNumber);
            _mockPlayerIds.Sort();
            OnPlayerJoined?.Invoke(actorNumber, nickname);
        }
    }

    /// <summary>
    /// Removes a mock player from the room.
    /// </summary>
    public void RemoveMockPlayer(int actorNumber)
    {
        if (_mockPlayers.TryGetValue(actorNumber, out var nickname))
        {
            _mockPlayers.Remove(actorNumber);
            _mockPlayerIds.Remove(actorNumber);
            OnPlayerLeft?.Invoke(actorNumber, nickname);
        }
    }

    /// <summary>
    /// Simulates receiving a network event.
    /// </summary>
    public void SimulateEventReceived(byte eventCode, object data, int senderActorNumber)
    {
        OnEventReceived?.Invoke(eventCode, data, senderActorNumber);
    }

    /// <summary>
    /// Gets all events sent since last clear.
    /// </summary>
    public IReadOnlyList<(byte code, object data, int[] targets)> GetSentEvents()
    {
        return _sentEvents;
    }

    /// <summary>
    /// Clears sent event history.
    /// </summary>
    public void ClearSentEvents()
    {
        _sentEvents.Clear();
    }

    /// <summary>
    /// Sets master client status.
    /// </summary>
    public void SetMasterClient(bool isMaster)
    {
        simulateAsMaster = isMaster;
    }

    /// <summary>
    /// Sets connected status.
    /// </summary>
    public void SetConnected(bool isConnected)
    {
        simulateAsConnected = isConnected;
    }
}
