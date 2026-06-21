using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using _Project.Scripts.Utility;
using Core.Events;
using Config;

namespace Players
{
    /// <summary>
    /// Service implementation for managing player data and identities.
    /// Bridges network player info with game-specific player data.
    /// </summary>
    public class PlayerService : MonoBehaviour, IPlayerService
    {
        [Header("Configuration")]
        [SerializeField] private PlayerColorPalette colorPalette;

        private readonly Dictionary<int, PlayerData> _players = new();
        private readonly List<PlayerData> _playerList = new();
        private INetworkService _networkService;
        private IEventBus _eventBus;
        private int _localPlayerId = -1;

        public int PlayerCount => _players.Count;

        public event Action<PlayerData> OnPlayerJoined;
        public event Action<PlayerData> OnPlayerLeft;
        public event Action OnIdentitiesAssigned;

        private void Awake()
        {
            ServiceLocator.RegisterService<IPlayerService>(this);
        }

        private void Start()
        {
            if (!ServiceLocator.TryGetService(out _networkService))
            {
                Debug.LogWarning("[PlayerService] INetworkService not found - running in offline mode");
                return;
            }

            ServiceLocator.TryGetService(out _eventBus);

            _localPlayerId = _networkService.LocalPlayerId;

            _networkService.OnPlayerJoined += HandleNetworkPlayerJoined;
            _networkService.OnPlayerLeft += HandleNetworkPlayerLeft;

            RefreshPlayers();
        }

        private void OnDestroy()
        {
            if (_networkService != null)
            {
                _networkService.OnPlayerJoined -= HandleNetworkPlayerJoined;
                _networkService.OnPlayerLeft -= HandleNetworkPlayerLeft;
            }

            ServiceLocator.UnregisterService<IPlayerService>();
        }

        public PlayerData GetPlayer(int actorNumber)
        {
            return _players.TryGetValue(actorNumber, out var player) ? player : null;
        }

        public PlayerData GetLocalPlayer()
        {
            return GetPlayer(_localPlayerId);
        }

        public IReadOnlyList<PlayerData> GetAllPlayers()
        {
            return _playerList;
        }

        public IReadOnlyList<PlayerData> GetPlayersInSlotOrder()
        {
            return _playerList.OrderBy(p => p.ActorNumber).ToList();
        }

        public PlayerData GetPlayerByColor(string colorId)
        {
            return _playerList.FirstOrDefault(p =>
                p.ImpColor != null && p.ImpColor.Equals(colorId, StringComparison.OrdinalIgnoreCase));
        }

        public void RefreshPlayers()
        {
            if (_networkService == null || !_networkService.IsConnected)
            {
                return;
            }

            var networkPlayerIds = _networkService.GetPlayerIds();
            var existingIds = new HashSet<int>(_players.Keys);

            foreach (int actorId in networkPlayerIds)
            {
                if (!_players.ContainsKey(actorId))
                {
                    AddPlayer(actorId);
                }
                existingIds.Remove(actorId);
            }

            foreach (int actorId in existingIds)
            {
                RemovePlayer(actorId);
            }

            Debug.Log($"[PlayerService] Refreshed players. Count: {_players.Count}");
        }

        public void AssignPlayerIdentities()
        {
            if (_networkService == null || !_networkService.IsMasterClient)
            {
                Debug.LogWarning("[PlayerService] Only master client can assign identities");
                return;
            }

            Debug.Log("[PlayerService] Assigning player identities...");

            var colorIds = colorPalette != null
                ? colorPalette.GetAllColorIds()
                : new[] { "red", "green", "blue", "purple" };

            var sortedPlayers = _playerList.OrderBy(p => p.ActorNumber).ToList();

            for (int i = 0; i < sortedPlayers.Count && i < colorIds.Length; i++)
            {
                var player = sortedPlayers[i];
                string colorId = colorIds[i];

                player.ImpColor = colorId;

                Debug.Log($"[PlayerService] Assigned {player.Nickname} -> {colorId}");

                BroadcastIdentityAssignment(player.ActorNumber, player.Nickname, colorId);
            }

            _eventBus?.Publish(new PlayerIdentitiesAssignedEvent { PlayerCount = sortedPlayers.Count });
            OnIdentitiesAssigned?.Invoke();
        }

        private void BroadcastIdentityAssignment(int actorNumber, string nickname, string colorId)
        {
            var data = new object[] { actorNumber, nickname, colorId };
            _networkService.SendToAll(NetworkEventCodes.AssignPlayerIdentities, new object[] { data });
        }

        public void HandleIdentityAssignment(int actorNumber, string colorId, int slotIndex)
        {
            var player = GetPlayer(actorNumber);
            if (player == null)
            {
                Debug.LogWarning($"[PlayerService] Cannot assign identity - player {actorNumber} not found");
                return;
            }

            player.ImpColor = colorId;
            Debug.Log($"[PlayerService] Applied identity: {player.Nickname} -> {colorId}");
        }

        private void HandleNetworkPlayerJoined(int actorNumber, string nickname)
        {
            if (!_players.ContainsKey(actorNumber))
            {
                AddPlayer(actorNumber);
            }
        }

        private void HandleNetworkPlayerLeft(int actorNumber, string nickname)
        {
            if (_players.ContainsKey(actorNumber))
            {
                RemovePlayer(actorNumber);
            }
        }

        private void AddPlayer(int actorNumber)
        {
            string displayName = _networkService?.GetPlayerName(actorNumber) ?? $"Player {actorNumber}";

            var playerData = new PlayerData
            {
                ActorNumber = actorNumber,
                Nickname = displayName,
                ImpColor = ""
            };

            _players[actorNumber] = playerData;
            _playerList.Add(playerData);

            Debug.Log($"[PlayerService] Player added: {displayName} (Actor {actorNumber})");

            OnPlayerJoined?.Invoke(playerData);

            _eventBus?.Publish(new PlayerJoinedEvent
            {
                ActorNumber = actorNumber,
                PlayerName = displayName,
                IsLocal = actorNumber == _localPlayerId
            });
        }

        private void RemovePlayer(int actorNumber)
        {
            if (_players.TryGetValue(actorNumber, out var player))
            {
                _players.Remove(actorNumber);
                _playerList.Remove(player);

                Debug.Log($"[PlayerService] Player removed: {player.Nickname}");

                OnPlayerLeft?.Invoke(player);

                _eventBus?.Publish(new PlayerLeftEvent
                {
                    ActorNumber = actorNumber,
                    PlayerName = player.Nickname
                });
            }
        }
    }
}
