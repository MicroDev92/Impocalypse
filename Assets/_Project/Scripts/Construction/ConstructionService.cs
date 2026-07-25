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
        bool TryPlaceSkyTower(int actorNumber, CardData card, string coordA, string coordB);
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

            // If all players have no city cards, end immediately. MASTER ONLY: non-master
            // clients can't see other players' stocks (they'd auto-done everyone and end
            // the phase early, desyncing the game). The broadcast ends it for them.
            if (IsMasterClient && _donePlayers.Count >= _playerIds.Count)
            {
                Debug.Log("[ConstructionService] No city cards to place, ending construction");
                EndConstruction();
            }
            else if (IsMasterClient)
            {
                // Master-authoritative build timer: expiry ends the phase for
                // everyone through the same broadcast path as everyone-done.
                _buildTimer = GameTiming.BuildSeconds;
                _buildTimerActive = true;
            }
        }

        // ==========================================
        // BUILD TIMER (master-authoritative)
        // ==========================================

        private float _buildTimer;
        private bool _buildTimerActive;

        private void Update()
        {
            if (!_buildTimerActive || !IsMasterClient) return;

            _buildTimer -= Time.deltaTime;
            if (_buildTimer > 0f) return;

            _buildTimerActive = false;
            Debug.Log("[ConstructionService] Build timer expired - ending construction");
            EndConstruction();
        }

        public IReadOnlyList<CardData> GetAvailableCityCards(int actorNumber)
        {
            if (_availableCityCards.TryGetValue(actorNumber, out var cards))
                return cards.AsReadOnly();
            return new List<CardData>().AsReadOnly();
        }

        public bool TryPlaceCity(int actorNumber, CardData card, string coordinate)
        {
            if (card == null || string.IsNullOrEmpty(coordinate)) return false;
            if (!card.IsCity && !card.IsFarm && !card.IsCamp) return false;

            var tile = _gridService?.GetTileByCoord(coordinate);
            if (tile == null) return false;

            // Camps claim EMPTY territory, so they branch before the ownership check.
            if (card.IsCamp)
            {
                if (tile.IsOwned)
                {
                    Debug.LogWarning($"[ConstructionService] Cannot camp on {coordinate} - already claimed");
                    return false;
                }

                // Claim via the imp-spawn pipeline (sets owner, tint, fief recalculation).
                string color = "red";
                var playerService = ServiceLocator.GetService<Players.IPlayerService>();
                var player = playerService?.GetPlayer(actorNumber);
                if (player != null && !string.IsNullOrEmpty(player.ImpColor)) color = player.ImpColor;

                // The camp occupies the building slot (nothing else can be built here)
                // and the claim is provisional: the real Territory card evicts it.
                // PlaceCamp BEFORE the spawn event: its handlers sync building markers
                // synchronously and must see the camp already on the tile.
                tile.PlaceCamp();
                _eventBus?.Publish(new SpawnImpEvent(coordinate, actorNumber, color));

                ConsumeBuildingCard(actorNumber, card);
                Debug.Log($"[ConstructionService] Player {actorNumber} camped {coordinate}");
                SendPlacement(card, actorNumber, coordinate);
                return true;
            }

            if (!tile.IsOwnedBy(actorNumber))
            {
                Debug.LogWarning($"[ConstructionService] Tile {coordinate} not owned by player {actorNumber}");
                return false;
            }

            // Validate + place by building kind
            if (card.IsCity)
            {
                if (!tile.CanPlaceCity(card.Strength))
                {
                    Debug.LogWarning($"[ConstructionService] Cannot place {card.Strength} city on {coordinate}");
                    return false;
                }
                tile.PlaceCity(card.Strength);
            }
            else if (card.Farm == FarmKind.Special)
            {
                if (!tile.CanPlaceSpecialFarm(card.AllowedTerrain))
                {
                    Debug.LogWarning($"[ConstructionService] Cannot place luxury farm on {coordinate} (needs {card.AllowedTerrain})");
                    return false;
                }
                tile.PlaceFarm(card.Resource, special: true);
            }
            else if (card.Farm == FarmKind.Market)
            {
                if (!tile.CanPlaceCommonResource())
                {
                    Debug.LogWarning($"[ConstructionService] Cannot place market on {coordinate} (needs a resource-less owned tile)");
                    return false;
                }
                tile.PlaceMarket();
            }
            else // Common farm
            {
                if (!tile.CanPlaceCommonResource())
                {
                    Debug.LogWarning($"[ConstructionService] Cannot place common farm on {coordinate} (needs a resource-less owned tile)");
                    return false;
                }
                tile.PlaceFarm(card.Resource, special: false);
            }

            ConsumeBuildingCard(actorNumber, card);

            Debug.Log($"[ConstructionService] Player {actorNumber} placed {card} at {coordinate}");

            // Local feedback + repaint events
            if (card.IsCity)
                _eventBus?.Publish(new CityPlacedEvent(coordinate, (int)card.Strength, actorNumber));
            else
                _eventBus?.Publish(new TileOwnerChangedEvent(coordinate, actorNumber)); // farm/market repaint

            SendPlacement(card, actorNumber, coordinate);
            return true;
        }

        /// <summary>
        /// Syncs a placement: master broadcasts the applied placement to everyone;
        /// non-master sends a request the master will validate, apply, and re-broadcast.
        /// </summary>
        private void SendPlacement(CardData card, int actorNumber, string coordA, string coordB = "")
        {
            if (IsMasterClient)
                _networkService?.SendToAll(NetworkEventCodes.CityPlaced,
                    new object[] { card.Serialize(), actorNumber, coordA, coordB ?? "" });
            else
                _networkService?.SendToMaster(NetworkEventCodes.PlaceCity,
                    new object[] { card.Serialize(), coordA, coordB ?? "" });
        }

        /// <summary>
        /// Links two of the player's tiles with a sky tower pair so they count as adjacent
        /// (merging their fiefs). Both must be owned by the player and not already linked.
        /// </summary>
        public bool TryPlaceSkyTower(int actorNumber, CardData card, string coordA, string coordB)
        {
            if (card == null || !card.IsSkyTower) return false;
            if (string.IsNullOrEmpty(coordA) || string.IsNullOrEmpty(coordB) || coordA == coordB) return false;

            var a = _gridService?.GetTileByCoord(coordA);
            var b = _gridService?.GetTileByCoord(coordB);
            if (a == null || b == null) return false;

            if (!a.IsOwnedBy(actorNumber) || !b.IsOwnedBy(actorNumber) || a.HasSkyLink || b.HasSkyLink)
            {
                Debug.LogWarning($"[ConstructionService] Invalid sky tower endpoints {coordA}/{coordB}");
                return false;
            }

            a.PlaceSkyLink(coordB);
            b.PlaceSkyLink(coordA);
            ConsumeBuildingCard(actorNumber, card);

            // Repaint fief outlines (the two fiefs just merged).
            _eventBus?.Publish(new TileOwnerChangedEvent(coordA, actorNumber));

            Debug.Log($"[ConstructionService] Player {actorNumber} sky-linked {coordA} <-> {coordB}");
            SendPlacement(card, actorNumber, coordA, coordB);
            return true;
        }

        /// <summary>Removes a placed card from the round's stock and the draft's persistent stock.</summary>
        private void ConsumeBuildingCard(int actorNumber, CardData card)
        {
            if (_availableCityCards.TryGetValue(actorNumber, out var cards))
            {
                var toRemove = cards.FirstOrDefault(c => c.Serialize() == card.Serialize());
                if (toRemove != null) cards.Remove(toRemove);
            }
            _draftService?.ConsumeCityCard(actorNumber, card);
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
            _buildTimerActive = false;
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

        /// <summary>Master receives a remote player's placement request, validates + applies + re-broadcasts.</summary>
        private void HandlePlaceCityRequest(object data, int senderActorId)
        {
            if (data is not object[] rawData || rawData.Length < 2) return;

            var card = CardData.Deserialize(rawData[0] as string);
            if (card == null) return;

            string coordA = rawData[1] as string;
            string coordB = rawData.Length > 2 ? rawData[2] as string : "";

            if (card.IsSkyTower)
                TryPlaceSkyTower(senderActorId, card, coordA, coordB);
            else
                TryPlaceCity(senderActorId, card, coordA);
        }

        /// <summary>
        /// Applies a placement broadcast locally. Idempotent: the placing client (and the
        /// master) already applied it, so an already-built/owned/linked tile means "done".
        /// </summary>
        private void HandleCityPlacedBroadcast(object data)
        {
            if (data is not object[] rawData || rawData.Length < 3) return;

            var card = CardData.Deserialize(rawData[0] as string);
            if (card == null) return;

            int actorNumber = (int)rawData[1];
            string coordA = rawData[2] as string;
            string coordB = rawData.Length > 3 ? rawData[3] as string : "";

            var tile = _gridService?.GetTileByCoord(coordA);
            if (tile == null) return;

            if (card.IsSkyTower)
            {
                if (tile.HasSkyLink || string.IsNullOrEmpty(coordB)) return;
                var other = _gridService.GetTileByCoord(coordB);
                if (other == null || other.HasSkyLink) return;

                tile.PlaceSkyLink(coordB);
                other.PlaceSkyLink(coordA);
                _eventBus?.Publish(new TileOwnerChangedEvent(coordA, actorNumber));
            }
            else if (card.IsCamp)
            {
                if (tile.IsOwned) return;

                string color = "red";
                var player = ServiceLocator.GetService<Players.IPlayerService>()?.GetPlayer(actorNumber);
                if (player != null && !string.IsNullOrEmpty(player.ImpColor)) color = player.ImpColor;

                // PlaceCamp before the spawn event - marker sync runs synchronously.
                tile.PlaceCamp();
                _eventBus?.Publish(new SpawnImpEvent(coordA, actorNumber, color));
            }
            else if (card.IsCity)
            {
                if (tile.HasBuilding) return;
                tile.PlaceCity(card.Strength);
                _eventBus?.Publish(new CityPlacedEvent(coordA, (int)card.Strength, actorNumber));
            }
            else if (card.IsFarm)
            {
                if (tile.HasBuilding) return;
                if (card.Farm == FarmKind.Market) tile.PlaceMarket();
                else tile.PlaceFarm(card.Resource, card.Farm == FarmKind.Special);
                _eventBus?.Publish(new TileOwnerChangedEvent(coordA, actorNumber));
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
