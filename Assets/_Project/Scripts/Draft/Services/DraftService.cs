using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using _Project.Scripts.Utility;
using Cards;
using Config;
using Core.Events;
using Players;

namespace Draft.Services
{
    /// <summary>
    /// Service that manages the draft phase.
    /// Supports deck-based dealing with territory + city cards.
    /// Territory picks trigger imp spawns; city picks are stored for construction phase.
    /// </summary>
    public class DraftService : MonoBehaviour, IDraftService
    {
        [Header("Configuration")]
        [SerializeField] private DraftConfiguration draftConfig;
        [SerializeField] private GameConfiguration gameConfig;

        private INetworkService _networkService;
        private IEventBus _eventBus;
        private IGridService _gridService;
        private CardFactory _cardFactory;
        private CardSelectionHandler _selectionHandler;

        // Deck (persists across rounds within a game)
        private List<CardData> _deck = new();
        private int _deckPosition;

        // Per-player hands (current round)
        private readonly Dictionary<int, List<CardData>> _playerHands = new();
        private readonly Dictionary<int, string[]> _pendingPicks = new();

        // City cards collected during draft (per player, persists across rounds)
        private readonly Dictionary<int, List<CardData>> _playerCityCards = new();

        // Parchment cards collected during draft (secret hand, scored at game end)
        private readonly Dictionary<int, List<CardData>> _playerParchments = new();

        private int _currentRound = 1;
        private bool _hasSubmittedPicks;
        private List<int> _cachedPlayerIds = new();

        // IDraftService Properties
        public int CurrentRound => _currentRound;
        public bool IsClockwise => draftConfig?.IsClockwise(_currentRound) ?? true;
        public int CardsPerPlayer => CalculateCardsPerPlayer();
        public int PicksPerRound => draftConfig?.PicksPerRound ?? 2;
        public bool HasSubmittedPicks => _hasSubmittedPicks;
        public bool AllPicksSubmitted => _pendingPicks.Count >= TotalPlayers;

        private int TotalPlayers => _networkService?.PlayerCount ?? 1;
        private bool IsTestMode => gameConfig?.TestMode ?? false;
        private bool IsMasterClient => _networkService?.IsMasterClient ?? false;
        private bool UseDeckDealing => draftConfig?.UseDeckDealing ?? true;

        // Events
        public event Action<string[]> OnCardsReceived;
        public event Action<Dictionary<int, string[]>> OnAllPicksSubmitted;
        public event Action<bool> OnHandsPassed;
        public event Action OnDraftEnded;

        private void Awake()
        {
            ServiceLocator.RegisterService<IDraftService>(this);
        }

        private void Start()
        {
            ServiceLocator.TryGetService(out _networkService);
            ServiceLocator.TryGetService(out _eventBus);
            ServiceLocator.TryGetService(out _gridService);
            ServiceLocator.TryGetService(out _cardFactory);
            ServiceLocator.TryGetService(out _selectionHandler);

            if (_networkService != null)
            {
                _networkService.OnEventReceived += HandleNetworkEvent;
            }

            if (_selectionHandler != null)
            {
                _selectionHandler.OnAllPicksMade += OnLocalPicksMade;
            }

            if (draftConfig == null)
                draftConfig = Resources.Load<DraftConfiguration>("DraftConfiguration");
            if (gameConfig == null)
                gameConfig = Resources.Load<GameConfiguration>("GameConfiguration");
        }

        private void OnDestroy()
        {
            if (_networkService != null)
                _networkService.OnEventReceived -= HandleNetworkEvent;

            if (_selectionHandler != null)
                _selectionHandler.OnAllPicksMade -= OnLocalPicksMade;

            ServiceLocator.UnregisterService<IDraftService>();
        }

        private int CalculateCardsPerPlayer()
        {
            if (!UseDeckDealing)
                return draftConfig?.GetCardsPerPlayer(TotalPlayers, IsTestMode) ?? 4;

            if (_deck.Count == 0)
                return draftConfig?.GetCardsPerPlayer(TotalPlayers, IsTestMode) ?? 4;

            int maxRounds = gameConfig?.MaxRounds ?? 4;
            return draftConfig.GetCardsPerPlayerFromDeck(_deck.Count, TotalPlayers, maxRounds);
        }

        public void StartDraft()
        {
            _currentRound = 1;
            _hasSubmittedPicks = false;
            _pendingPicks.Clear();
            _playerHands.Clear();
            _playerCityCards.Clear();
            _playerParchments.Clear();

            // Build deck once per game
            if (UseDeckDealing && IsMasterClient && _gridService != null)
            {
                _deck = DeckBuilder.BuildDeck(_gridService);
                _deckPosition = 0;
            }

            Debug.Log("[DraftService] Draft started");

            if (IsMasterClient)
            {
                var playerService = ServiceLocator.GetService<IPlayerService>();
                playerService?.AssignPlayerIdentities();

                DealCards();
            }
        }

        /// <summary>
        /// Starts a new game round using the existing deck.
        /// Does NOT rebuild the deck or reset city cards.
        /// </summary>
        public void StartNewGameRound()
        {
            _currentRound = 1;
            _hasSubmittedPicks = false;
            _pendingPicks.Clear();
            _playerHands.Clear();
            // Note: _deck, _deckPosition, and _playerCityCards persist

            Debug.Log($"[DraftService] New game round started (deck position: {_deckPosition}/{_deck.Count})");

            if (IsMasterClient)
            {
                DealCards();
            }
        }

        public void DealCards()
        {
            if (!IsMasterClient)
            {
                Debug.LogWarning("[DraftService] Only master client can deal cards");
                return;
            }

            _cachedPlayerIds.Clear();
            _cachedPlayerIds.AddRange(_networkService.GetPlayerIds());
            _cachedPlayerIds.Sort();

            if (UseDeckDealing)
            {
                DealFromDeck();
            }
            else
            {
                DealRandomCoords();
            }
        }

        private void DealFromDeck()
        {
            int cardsPerPlayer = CardsPerPlayer;
            int totalNeeded = cardsPerPlayer * _cachedPlayerIds.Count;
            int available = _deck.Count - _deckPosition;

            if (available < totalNeeded)
            {
                // Deal what's available evenly
                cardsPerPlayer = available / _cachedPlayerIds.Count;
                if (cardsPerPlayer == 0)
                {
                    Debug.LogWarning("[DraftService] No cards left in deck");
                    EndDraft();
                    return;
                }
            }

            for (int i = 0; i < _cachedPlayerIds.Count; i++)
            {
                int actorNumber = _cachedPlayerIds[i];
                var hand = _deck.GetRange(_deckPosition + i * cardsPerPlayer, cardsPerPlayer);
                _playerHands[actorNumber] = new List<CardData>(hand);

                // Send serialized cards to player
                var serialized = hand.Select(c => c.Serialize()).ToArray();
                _networkService.SendToPlayers(
                    NetworkEventCodes.DealCards,
                    serialized,
                    new[] { actorNumber }
                );

                Debug.Log($"[DraftService] Dealt {hand.Count} cards to player {actorNumber} (deck-based)");
            }

            _deckPosition += cardsPerPlayer * _cachedPlayerIds.Count;
        }

        private void DealRandomCoords()
        {
            if (_gridService == null)
            {
                Debug.LogError("[DraftService] Grid service not available");
                return;
            }

            int totalCards = CardsPerPlayer * _cachedPlayerIds.Count;
            var allCoords = _gridService.GetRandomCoords(totalCards);

            if (allCoords == null || allCoords.Length < totalCards)
            {
                Debug.LogError($"[DraftService] Not enough coordinates! Got {allCoords?.Length ?? 0}, need {totalCards}");
                return;
            }

            for (int i = 0; i < _cachedPlayerIds.Count; i++)
            {
                int actorNumber = _cachedPlayerIds[i];
                var coords = allCoords.Skip(i * CardsPerPlayer).Take(CardsPerPlayer).ToArray();
                var hand = coords.Select(c => CardData.Territory(c)).ToList();
                _playerHands[actorNumber] = hand;

                var serialized = hand.Select(c => c.Serialize()).ToArray();
                _networkService.SendToPlayers(
                    NetworkEventCodes.DealCards,
                    serialized,
                    new[] { actorNumber }
                );

                Debug.Log($"[DraftService] Dealt {coords.Length} cards to player {actorNumber}");
            }
        }

        public void SubmitPicks(string[] serializedPicks)
        {
            if (_hasSubmittedPicks)
            {
                Debug.LogWarning("[DraftService] Already submitted picks");
                return;
            }

            _hasSubmittedPicks = true;

            _networkService?.SendToMaster(NetworkEventCodes.SubmitPicks, serializedPicks);

            Debug.Log($"[DraftService] Submitted picks: {string.Join(", ", serializedPicks)}");

            _eventBus?.Publish(new ShowStatusEvent("Waiting for other players..."));
            _eventBus?.Publish(new LocalPicksSubmittedEvent(serializedPicks));
        }

        public void PassHands()
        {
            if (!IsMasterClient)
            {
                Debug.LogWarning("[DraftService] Only master client can pass hands");
                return;
            }

            var newHands = new Dictionary<int, List<CardData>>();

            for (int i = 0; i < _cachedPlayerIds.Count; i++)
            {
                int fromIndex = IsClockwise
                    ? (i - 1 + _cachedPlayerIds.Count) % _cachedPlayerIds.Count
                    : (i + 1) % _cachedPlayerIds.Count;

                int toActor = _cachedPlayerIds[i];
                int fromActor = _cachedPlayerIds[fromIndex];

                newHands[toActor] = _playerHands.TryGetValue(fromActor, out var hand)
                    ? new List<CardData>(hand)
                    : new List<CardData>();
            }

            _playerHands.Clear();
            foreach (var kvp in newHands)
            {
                _playerHands[kvp.Key] = kvp.Value;
            }

            // Send new hands to each player
            foreach (var kvp in _playerHands)
            {
                var serialized = kvp.Value.Select(c => c.Serialize()).ToArray();
                _networkService.SendToPlayers(
                    NetworkEventCodes.DealCards,
                    serialized,
                    new[] { kvp.Key }
                );
            }

            Debug.Log($"[DraftService] Passed hands {(IsClockwise ? "clockwise" : "counter-clockwise")}");

            _eventBus?.Publish(new HandsPassedEvent(IsClockwise));
            OnHandsPassed?.Invoke(IsClockwise);
        }

        public void EndDraft()
        {
            Debug.Log("[DraftService] Draft ended - advancing to Build phase");

            var gameManager = ServiceLocator.GetService<IGameManager>();
            gameManager?.AdvancePhase();

            OnDraftEnded?.Invoke();
        }

        public IReadOnlyList<CardData> GetPlayerCityCards(int actorNumber)
        {
            if (_playerCityCards.TryGetValue(actorNumber, out var cards))
                return cards.AsReadOnly();
            return new List<CardData>().AsReadOnly();
        }

        public IReadOnlyList<CardData> GetPlayerParchments(int actorNumber)
        {
            if (_playerParchments.TryGetValue(actorNumber, out var cards))
                return cards.AsReadOnly();
            return new List<CardData>().AsReadOnly();
        }

        public void ConsumeCityCard(int actorNumber, CardData cityCard)
        {
            if (cityCard == null) return;
            if (!_playerCityCards.TryGetValue(actorNumber, out var cards) || cards.Count == 0)
                return;

            // Exact match first (local placement shares the same card object);
            // fall back to any city of the same strength (network re-creates the card).
            var match = cards.FirstOrDefault(c => c.Serialize() == cityCard.Serialize())
                        ?? cards.FirstOrDefault(c => c.Strength == cityCard.Strength);
            if (match != null)
            {
                cards.Remove(match);
                Debug.Log($"[DraftService] Consumed Str{(int)cityCard.Strength} city for player {actorNumber}; {cards.Count} left in stock");
            }
        }

        private void HandleNetworkEvent(byte eventCode, object data, int senderActorId)
        {
            switch (eventCode)
            {
                case NetworkEventCodes.DealCards:
                    HandleDealCardsReceived(data);
                    break;

                case NetworkEventCodes.SubmitPicks:
                    if (IsMasterClient)
                    {
                        HandlePicksReceived(data, senderActorId);
                    }
                    break;

                case NetworkEventCodes.SpawnImps:
                    HandleSpawnImpsReceived(data);
                    break;

                case NetworkEventCodes.AssignPlayerIdentities:
                    HandleAssignPlayerIdentities(data);
                    break;
            }
        }

        private void HandleDealCardsReceived(object data)
        {
            string[] serializedCards = null;

            if (data is string[] strArray)
            {
                serializedCards = strArray;
            }
            else if (data is object[] rawData)
            {
                serializedCards = rawData.Cast<string>().ToArray();
            }

            if (serializedCards == null)
            {
                Debug.LogError($"[DraftService] Invalid deal cards data type: {data?.GetType().Name ?? "null"}");
                return;
            }

            Debug.Log($"[DraftService] Received {serializedCards.Length} cards");

            // Reset for new round
            _hasSubmittedPicks = false;
            _selectionHandler?.ResetSelections();

            // Parse card data
            var cardDatas = serializedCards
                .Select(CardData.Deserialize)
                .Where(c => c != null)
                .ToArray();

            // Create card UI
            _cardFactory?.ReturnAllCards();
            _cardFactory?.CreateCards(cardDatas, _selectionHandler);

            // Publish event
            _eventBus?.Publish(new CardsDealtEvent(serializedCards));
            OnCardsReceived?.Invoke(serializedCards);
        }

        private void HandlePicksReceived(object data, int senderActorId)
        {
            string[] serializedPicks = null;

            if (data is string[] strArray)
            {
                serializedPicks = strArray;
            }
            else if (data is object[] rawData)
            {
                serializedPicks = rawData.Cast<string>().ToArray();
            }

            if (serializedPicks == null)
            {
                Debug.LogError($"[DraftService] Invalid picks data type: {data?.GetType().Name ?? "null"}");
                return;
            }

            _pendingPicks[senderActorId] = serializedPicks;

            Debug.Log($"[DraftService] Received picks from player {senderActorId}: {string.Join(", ", serializedPicks)}");

            // Parse picks and separate territory from city
            foreach (var serialized in serializedPicks)
            {
                var card = CardData.Deserialize(serialized);
                if (card == null) continue;

                if (card.IsCity || card.IsFarm || card.IsCamp)
                {
                    // Store buildable card (city/farm/camp) for the construction phase
                    if (!_playerCityCards.ContainsKey(senderActorId))
                        _playerCityCards[senderActorId] = new List<CardData>();
                    _playerCityCards[senderActorId].Add(card);
                }
                else if (card.IsParchment)
                {
                    // Keep parchments in a secret hand; scored at game end
                    if (!_playerParchments.ContainsKey(senderActorId))
                        _playerParchments[senderActorId] = new List<CardData>();
                    _playerParchments[senderActorId].Add(card);
                }
            }

            // Remove picked cards from hand
            if (_playerHands.TryGetValue(senderActorId, out var hand))
            {
                var pickSet = new HashSet<string>(serializedPicks);
                hand.RemoveAll(c => pickSet.Contains(c.Serialize()));
            }

            // Check if all picks are in
            if (_pendingPicks.Count >= TotalPlayers)
            {
                ProcessAllPicks();
            }
        }

        private void ProcessAllPicks()
        {
            Debug.Log("[DraftService] All picks received, processing...");

            // Broadcast imp spawns for territory picks only
            var spawnData = new List<object[]>();
            foreach (var kvp in _pendingPicks)
            {
                foreach (var serialized in kvp.Value)
                {
                    var card = CardData.Deserialize(serialized);
                    if (card != null && card.IsTerritory)
                    {
                        spawnData.Add(new object[] { card.Coordinate, kvp.Key });
                    }
                }
            }

            if (spawnData.Count > 0)
            {
                _networkService?.SendToAll(NetworkEventCodes.SpawnImps, spawnData.ToArray());
            }

            // Publish event
            var picksCopy = new Dictionary<int, string[]>(_pendingPicks);
            _eventBus?.Publish(new AllPicksSubmittedEvent(picksCopy));
            OnAllPicksSubmitted?.Invoke(picksCopy);

            // Clear for next round
            _pendingPicks.Clear();

            // Check if hands are empty (draft complete) or pass hands
            bool handsEmpty = _playerHands.Values.All(h => h.Count == 0);
            if (handsEmpty)
            {
                EndDraft();
            }
            else
            {
                // Internal draft pass counter — NOT the game round
                _currentRound++;
                Debug.Log($"[DraftService] Draft pass {_currentRound}, passing hands...");
                PassHands();
            }
        }

        private void HandleSpawnImpsReceived(object data)
        {
            if (data is not object[] rawData)
            {
                Debug.LogError($"[DraftService] Invalid spawn imps data type: {data?.GetType().Name ?? "null"}");
                return;
            }

            foreach (var item in rawData)
            {
                if (item is not object[] entry || entry.Length < 2) continue;

                string coord = (string)entry[0];
                int actor = (int)entry[1];

                string color = "red";
                var playerService = ServiceLocator.GetService<IPlayerService>();
                if (playerService != null)
                {
                    var player = playerService.GetPlayer(actor);
                    if (player != null && !string.IsNullOrEmpty(player.ImpColor))
                        color = player.ImpColor;
                }

                _eventBus?.Publish(new SpawnImpEvent(coord, actor, color));
            }

            Debug.Log($"[DraftService] Processed spawn imps: {rawData.Length} entries");
        }

        private void HandleAssignPlayerIdentities(object data)
        {
            if (data is not object[] rawData)
            {
                Debug.LogError($"[DraftService] Invalid identity data type: {data?.GetType().Name ?? "null"}");
                return;
            }

            var playerService = ServiceLocator.GetService<IPlayerService>();

            foreach (var item in rawData)
            {
                if (item is not object[] entry || entry.Length < 3) continue;

                int actor = (int)entry[0];
                string nick = (string)entry[1];
                string color = (string)entry[2];

                playerService?.HandleIdentityAssignment(actor, color, 0);
                Debug.Log($"[DraftService] Player identity: {nick} -> {color}");
            }
        }

        private void OnLocalPicksMade(string[] serializedPicks)
        {
            SubmitPicks(serializedPicks);
        }
    }
}
