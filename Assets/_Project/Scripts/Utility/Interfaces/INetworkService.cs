using System;
using System.Collections.Generic;

/// <summary>
/// Interface for network service abstraction.
/// Hides Photon implementation details behind a clean API.
/// All parameters use primitives or simple types - no Photon types exposed.
/// </summary>
public interface INetworkService
{
    // ============================================
    // CONNECTION STATE
    // ============================================

    /// <summary>
    /// Whether the client is connected to the network.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Whether this client is the master/host client.
    /// </summary>
    bool IsMasterClient { get; }

    /// <summary>
    /// Local player's actor number (unique ID in room).
    /// </summary>
    int LocalPlayerId { get; }

    /// <summary>
    /// Local player's nickname.
    /// </summary>
    string LocalPlayerName { get; }

    /// <summary>
    /// Number of players currently in the room.
    /// </summary>
    int PlayerCount { get; }

    /// <summary>
    /// Current room name, or null if not in a room.
    /// </summary>
    string CurrentRoomName { get; }

    // ============================================
    // PLAYER INFO
    // ============================================

    /// <summary>
    /// Gets all player actor numbers in the room.
    /// </summary>
    IReadOnlyList<int> GetPlayerIds();

    /// <summary>
    /// Gets a player's nickname by actor number.
    /// </summary>
    /// <param name="actorNumber">Player's actor number</param>
    /// <returns>Nickname or null if not found</returns>
    string GetPlayerName(int actorNumber);

    /// <summary>
    /// Checks if a player is the master client.
    /// </summary>
    /// <param name="actorNumber">Player's actor number</param>
    /// <returns>True if player is master client</returns>
    bool IsPlayerMaster(int actorNumber);

    // ============================================
    // EVENTS - SENDING
    // ============================================

    /// <summary>
    /// Sends data to all players in the room.
    /// </summary>
    /// <param name="eventCode">Event code</param>
    /// <param name="data">Serializable data</param>
    void SendToAll(byte eventCode, object data);

    /// <summary>
    /// Sends data to specific players.
    /// </summary>
    /// <param name="eventCode">Event code</param>
    /// <param name="data">Serializable data</param>
    /// <param name="targetActorIds">Target player actor numbers</param>
    void SendToPlayers(byte eventCode, object data, int[] targetActorIds);

    /// <summary>
    /// Sends data to the master client.
    /// </summary>
    /// <param name="eventCode">Event code</param>
    /// <param name="data">Serializable data</param>
    void SendToMaster(byte eventCode, object data);

    // ============================================
    // CONNECTION MANAGEMENT
    // ============================================

    /// <summary>
    /// Connects to the network.
    /// </summary>
    void Connect();

    /// <summary>
    /// Disconnects from the network.
    /// </summary>
    void Disconnect();

    /// <summary>
    /// Loads a scene for all players (master client only).
    /// </summary>
    /// <param name="sceneName">Scene name to load</param>
    void LoadScene(string sceneName);

    /// <summary>
    /// Sets the local player's nickname.
    /// </summary>
    /// <param name="nickname">New nickname</param>
    void SetPlayerName(string nickname);

    // ============================================
    // EVENTS - CALLBACKS
    // ============================================

    /// <summary>
    /// Fired when connected to the server.
    /// </summary>
    event Action OnConnectedToServer;

    /// <summary>
    /// Fired when disconnected from the server.
    /// </summary>
    event Action<string> OnDisconnectedFromServer;

    /// <summary>
    /// Fired when a player joins the room.
    /// Parameters: actorNumber, nickname
    /// </summary>
    event Action<int, string> OnPlayerJoined;

    /// <summary>
    /// Fired when a player leaves the room.
    /// Parameters: actorNumber, nickname
    /// </summary>
    event Action<int, string> OnPlayerLeft;

    /// <summary>
    /// Fired when the master client changes.
    /// Parameters: new master actorNumber, new master nickname
    /// </summary>
    event Action<int, string> OnMasterClientChanged;

    /// <summary>
    /// Fired when a network event is received.
    /// Parameters: eventCode, data, senderActorNumber
    /// </summary>
    event Action<byte, object, int> OnEventReceived;

    /// <summary>
    /// Fired when joined a room.
    /// </summary>
    event Action OnRoomJoined;

    /// <summary>
    /// Fired when failed to join a room.
    /// Parameters: error message
    /// </summary>
    event Action<string> OnRoomJoinFailed;
}
