using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using _Project.Scripts.Utility;
using Cards;
using Core.Events;
using Draft.Services;

namespace Construction
{
    /// <summary>
    /// Interface for the construction phase service.
    /// </summary>
    public interface IConstructionService
    {
        event Action OnAllPlayersDone;
        IReadOnlyList<CardData> GetAvailableCityCards(int actorNumber);
        bool TryPlaceCity(int actorNumber, CardData cityCard, string coordinate);
        void MarkPlayerDone(int actorNumber);
        bool IsPlayerDone(int actorNumber);
        void StartConstruction();
    }

    /// <summary>
    /// Manages the construction phase where players place city cards on owned tiles.
    /// </summary>
    public class ConstructionService : MonoBehaviour, IConstructionService
    {
        private INetworkService _networkService;
        private IEventBus _eventBus;
        private IGridService _gridService;
        private IDraftService _draftService;

        // Per-player available city cards (decreases as they are placed)
        private readonly Dictionary<int, List<CardData>> _availableCityCards = new();
        private readonly HashSet<int> _donePlayers = new();
        private List<int> _playerIds = new();

        private bool IsMasterClient => _networkService?.IsMasterClient ?? false;

        public event Action OnAllPlayersDone;

        private void Awake()
        {
            ServiceLocator.RegisterService<IConstructionService>(this);
        }

        private void Start()
        {
            ServiceLocator.TryGetService(out _networkService);
            ServiceLocator.TryGetService(out _eventBus);
            ServiceLocator.TryGetService(out _gridService);
            ServiceLocator.TryGetService(out _draftService);

            if (_networkService != null)
            {
                _networkService.OnEventReceived += HandleNetworkEvent;
            }
        }

        private void OnDestroy()
        {
            if (_networkService != null)
            {
                _networkService.OnEventReceived -= HandleNetworkEvent;
            }

            ServiceLocator.UnregisterService<IConstructionService>();
        }

        public void StartConstruction()
        {
            _donePlayers.Clear();
            _availableCityCards.Clear();

            _playerIds = _networkService?.GetPlayerIds()?.ToList() ?? new List<int>();

            // Load city cards from draft service
            foreach (var playerId in _playerIds)
            {
                var cityCards = _draftService?.GetPlayerCityCards(playerId);
                if (cityCards != null && cityCards.Count > 0)
                {
                    _availableCityCards[playerId] = new List<CardData>(cityCards);
                    Debug.Log($"[ConstructionService] Player {playerId} has {cityCards.Count} city cards");
                }
                else
                {
                    // Player has no city cards, auto-mark as done
                    _donePlayers.Add(playerId);
                    Debug.Log($"[ConstructionService] Player {playerId} has no city cards, auto-done");
                }
            }

            // If all players have no city cards, end immediately
            if (_donePlayers.Count >= _playerIds.Count)
            {
                Debug.Log("[ConstructionService] No city cards to place, ending construction");
                EndConstruction();
            }
        }

        public IReadOnlyList<CardData> GetAvailableCityCards(int actorNumber)
        {
            if (_availableCityCards.TryGetValue(actorNumber, out var cards))
                return cards.AsReadOnly();
            return new List<CardData>().AsReadOnly();
        }

        public bool TryPlaceCity(int actorNumber, CardData cityCard, string coordinate)
        {
            if (cityCard == null || !cityCard.IsCity || string.IsNullOrEmpty(coordinate))
                return false;

            var tile = _gridService?.GetTileByCoord(coordinate);
            if (tile == null) return false;

            // Validate placement
            if (!tile.IsOwnedBy(actorNumber))
            {
                Debug.LogWarning($"[ConstructionService] Tile {coordinate} not owned by player {actorNumber}");
                return false;
            }

            if (!tile.CanPlaceCity(cityCard.Strength))
            {
                Debug.LogWarning($"[ConstructionService] Cannot place {cityCard.Strength} city on {coordinate}");
                return false;
            }

            // Place the city
            tile.PlaceCity(cityCard.Strength);

            // Remove card from available
            if (_availableCityCards.TryGetValue(actorNumber, out var cards))
            {
                var toRemove = cards.FirstOrDefault(c => c.Serialize() == cityCard.Serialize());
                if (toRemove != null) cards.Remove(toRemove);
            }

            _eventBus?.Publish(new CityPlacedEvent(coordinate, (int)cityCard.Strength, actorNumber));

            Debug.Log($"[ConstructionService] Player {actorNumber} placed Str{(int)cityCard.Strength} city at {coordinate}");

            // If master, broadcast to all
            if (IsMasterClient)
            {
                _networkService?.SendToAll(NetworkEventCodes.CityPlaced,
                    new object[] { coordinate, (int)cityCard.Strength, actorNumber });
            }
            else
            {
                // Send request to master
                _networkService?.SendToMaster(NetworkEventCodes.PlaceCity,
                    new object[] { coordinate, (int)cityCard.Strength });
            }

            return true;
        }

        public void MarkPlayerDone(int actorNumber)
        {
            _donePlayers.Add(actorNumber);
            Debug.Log($"[ConstructionService] Player {actorNumber} done constructing ({_donePlayers.Count}/{_playerIds.Count})");

            if (IsMasterClient)
            {
                CheckAllDone();
            }
            else
            {
                _networkService?.SendToMaster(NetworkEventCodes.ConstructionDone, new object[] { actorNumber });
            }
        }

        public bool IsPlayerDone(int actorNumber)
        {
            return _donePlayers.Contains(actorNumber);
        }

        private void CheckAllDone()
        {
            if (_donePlayers.Count >= _playerIds.Count)
            {
                EndConstruction();
            }
        }

        private void EndConstruction()
        {
            Debug.Log("[ConstructionService] Construction phase ended");

            if (IsMasterClient)
            {
                _networkService?.SendToAll(NetworkEventCodes.ConstructionPhaseEnd, new object[] { });
            }

            _eventBus?.Publish(new ConstructionPhaseEndedEvent());
            OnAllPlayersDone?.Invoke();
        }

        private void HandleNetworkEvent(byte eventCode, object data, int senderActorId)
        {
            switch (eventCode)
            {
                case NetworkEventCodes.PlaceCity:
                    if (IsMasterClient)
                        HandlePlaceCityRequest(data, senderActorId);
                    break;

                case NetworkEventCodes.CityPlaced:
                    HandleCityPlacedBroadcast(data);
                    break;

                case NetworkEventCodes.ConstructionDone:
                    if (IsMasterClient)
                        HandleConstructionDone(data, senderActorId);
                    break;

                case NetworkEventCodes.ConstructionPhaseEnd:
                    HandleConstructionPhaseEnd();
                    break;
            }
        }

        private void HandlePlaceCityRequest(object data, int senderActorId)
        {
            if (data is not object[] rawData || rawData.Length < 2) return;

            string coord = (string)rawData[0];
            int strength = (int)rawData[1];

            var cityCard = CardData.City((CityStrength)strength);
            TryPlaceCity(senderActorId, cityCard, coord);
        }

        private void HandleCityPlacedBroadcast(object data)
        {
            if (data is not object[] rawData || rawData.Length < 3) return;

            string coord = (string)rawData[0];
            int strength = (int)rawData[1];
            int actorNumber = (int)rawData[2];

            // Apply locally if not already applied (non-master clients)
            var tile = _gridService?.GetTileByCoord(coord);
            if (tile != null && !tile.HasBuilding)
            {
                tile.PlaceCity((CityStrength)strength);
                _eventBus?.Publish(new CityPlacedEvent(coord, strength, actorNumber));
            }
        }

        private void HandleConstructionDone(object data, int senderActorId)
        {
            _donePlayers.Add(senderActorId);
            CheckAllDone();
        }

        private void HandleConstructionPhaseEnd()
        {
            _eventBus?.Publish(new ConstructionPhaseEndedEvent());
            OnAllPlayersDone?.Invoke();
        }
    }
}
