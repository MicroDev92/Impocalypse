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
            StartPickTimer();
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

            StartPickTimer();
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

        // ==========================================
        // PICK TIMER (master-authoritative)
        // ==========================================

        private float _pickTimer;
        private bool _pickTimerActive;

        private void Update()
        {
            TickLocalAutoSubmit();

            if (!_pickTimerActive || !IsMasterClient) return;

            _pickTimer -= Time.deltaTime;
            if (_pickTimer > 0f) return;

            _pickTimerActive = false;
            AutoPickForMissingPlayers();
        }

        // ==========================================
        // LOCAL AUTO-SUBMIT (runs on every client)
        // ==========================================

        /// <summary>
        /// How far ahead of the master's deadline each client commits its own selection.
        /// Wide enough to absorb latency and a slow frame, short enough that it only
        /// fires when the player really has run out of time.
        /// </summary>
        private const float LocalAutoSubmitMargin = 2f;

        private float _localSubmitTimer;
        private bool _localSubmitTimerActive;

        private void StartLocalSubmitTimer()
        {
            _localSubmitTimer = Mathf.Max(1f, GameTiming.DraftPickSeconds - LocalAutoSubmitMargin);
            _localSubmitTimerActive = true;
        }

        private void StopLocalSubmitTimer() => _localSubmitTimerActive = false;

        /// <summary>
        /// Runs on every client, master included. <see cref="AutoPickForMissingPlayers"/>
        /// stays as the backstop for players who are genuinely absent; this makes sure a
        /// player who IS here and has chosen gets their own cards submitted instead of
        /// random ones. The master cannot do this for them - selection is client-side,
        /// so the master only ever learns what was submitted, never what was highlighted.
        /// </summary>
        private void TickLocalAutoSubmit()
        {
            if (!_localSubmitTimerActive) return;

            if (_hasSubmittedPicks)
            {
                _localSubmitTimerActive = false;
                return;
            }

            _localSubmitTimer -= Time.deltaTime;
            if (_localSubmitTimer > 0f) return;

            _localSubmitTimerActive = false;

            Debug.Log("[DraftService] Pick timer nearly up - committing this player's own selection");
            _selectionHandler?.ForceSubmitCurrentSelection();
        }

        private void StartPickTimer()
        {
            if (!IsMasterClient) return;
            _pickTimer = GameTiming.DraftPickSeconds;
            _pickTimerActive = true;
        }

        private void StopPickTimer() => _pickTimerActive = false;

        /// <summary>
        /// Pick timer expired: submit random picks on behalf of every player who
        /// hasn't picked yet. Runs through HandlePicksReceived - the same
        /// authoritative path as a real submission (provisions, grants, spawns).
        /// </summary>
        private void AutoPickForMissingPlayers()
        {
            var missing = _cachedPlayerIds.Where(id => !_pendingPicks.ContainsKey(id)).ToList();
            foreach (var actorId in missing)
            {
                if (!_playerHands.TryGetValue(actorId, out var hand) || hand.Count == 0) continue;

                int count = Mathf.Min(PicksPerRound, hand.Count);
                var picks = hand.OrderBy(_ => UnityEngine.Random.value)
                                .Take(count)
                                .Select(c => c.Serialize())
                                .ToArray();

                Debug.Log($"[DraftService] Pick timer expired - auto-picking {count} random cards for player {actorId}");
                HandlePicksReceived(picks, actorId);
            }
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

            StartPickTimer(); // fresh 45s for the new hands
        }

        public void EndDraft()
        {
            Debug.Log("[DraftService] Draft ended - advancing to Build phase");
            StopPickTimer();

            // Runs on every client, so this is where the local deadline dies too -
            // otherwise it could still fire for a player the master already auto-picked
            // for, submitting into a draft that is over.
            StopLocalSubmitTimer();

            // Draft end is decided master-side (ProcessAllPicks) - tell everyone else.
            if (IsMasterClient)
                _networkService?.SendToAll(NetworkEventCodes.DraftPhaseEnd, new object[0]);

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

        /// <summary>
        /// Resolves a picked Provisions card at draft time: draws 2 cards and plays them.
        /// Territories join the effective pick list (so ProcessAllPicks broadcasts their
        /// imp spawns), buildables/parchments go to the player's pools, and a drawn
        /// Provisions chains (draws 2 more).
        /// </summary>
        private void ResolveProvisionsPick(int actorNumber, List<string> effectivePicks, List<CardData> granted)
        {
            var queue = new Queue<CardData>(DrawFromDeck(2));
            var drawnNames = new List<string>();

            while (queue.Count > 0)
            {
                var drawn = queue.Dequeue();
                drawnNames.Add(drawn.ToString());

                if (drawn.IsTerritory)
                {
                    effectivePicks.Add(drawn.Serialize());
                }
                else if (drawn.IsProvisions)
                {
                    foreach (var chained in DrawFromDeck(2))
                        queue.Enqueue(chained);
                }
                else
                {
                    GrantPick(actorNumber, drawn);
                    granted.Add(drawn);
                }
            }

            Debug.Log($"[DraftService] Provisions (player {actorNumber}) drew: {string.Join(", ", drawnNames)}");

            if (drawnNames.Count > 0 && actorNumber == (_networkService?.LocalPlayerId ?? -1))
                _eventBus?.Publish(new ShowStatusEvent($"Provisions drew: {string.Join(", ", drawnNames)}"));
        }

        /// <summary>Draws up to count cards from the top of the deck (master only).</summary>
        private IReadOnlyList<CardData> DrawFromDeck(int count)
        {
            var result = new List<CardData>();
            if (!IsMasterClient) return result;

            while (result.Count < count && _deckPosition < _deck.Count)
                result.Add(_deck[_deckPosition++]);

            Debug.Log($"[DraftService] Drew {result.Count} cards from deck (position {_deckPosition}/{_deck.Count})");
            return result;
        }

        /// <summary>
        /// Grants a card to a player as if they had picked it in the draft (used by
        /// Provisions). Buildables go to the construction stock, parchments to the
        /// secret hand. Territories are NOT handled here - the caller claims them.
        /// </summary>
        private void GrantPick(int actorNumber, CardData card)
        {
            if (card == null) return;

            if (card.IsCity || card.IsFarm || card.IsCamp || card.IsSkyTower || card.IsProvisions)
            {
                if (!_playerCityCards.ContainsKey(actorNumber))
                    _playerCityCards[actorNumber] = new List<CardData>();
                _playerCityCards[actorNumber].Add(card);
            }
            else if (card.IsParchment)
            {
                if (!_playerParchments.ContainsKey(actorNumber))
                    _playerParchments[actorNumber] = new List<CardData>();
                _playerParchments[actorNumber].Add(card);
            }
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

                case NetworkEventCodes.GrantCards:
                    HandleGrantCardsReceived(data);
                    break;

                case NetworkEventCodes.DraftPhaseEnd:
                    if (!IsMasterClient) EndDraft();
                    break;

                case NetworkEventCodes.RevealParchments:
                    HandleRevealParchments(data);
                    break;
            }
        }

        /// <summary>
        /// Non-master client receives the buildables/parchments it drafted (including
        /// Provisions draws) - the master routed them authoritatively; mirror locally.
        /// </summary>
        private void HandleGrantCardsReceived(object data)
        {
            string[] serialized = data as string[];
            if (serialized == null && data is object[] raw)
                serialized = raw.Cast<string>().ToArray();
            if (serialized == null) return;

            int localId = _networkService?.LocalPlayerId ?? -1;
            var names = new List<string>();

            foreach (var s in serialized)
            {
                var card = CardData.Deserialize(s);
                if (card == null) continue;
                GrantPick(localId, card);
                names.Add(card.ToString());
            }

            Debug.Log($"[DraftService] Granted cards received: {string.Join(", ", names)}");

            // The store has changed, so anything showing what the player OWNS needs
            // to hear about it. Without this a drafted Pact sat invisible until some
            // unrelated refresh fired - usually a later hand.
            _eventBus?.Publish(new CardsGrantedEvent(localId));
            if (names.Count > 0)
                _eventBus?.Publish(new ShowStatusEvent($"Gained: {string.Join(", ", names)}"));
        }

        /// <summary>
        /// Master broadcasts every player's secret parchments so all clients can compute
        /// identical final scores. Call when final-round scoring begins.
        /// </summary>
        public void BroadcastParchmentsForReveal()
        {
            if (!IsMasterClient) return;

            var payload = _playerParchments
                .Select(kvp => (object)new object[]
                    { kvp.Key, kvp.Value.Select(c => c.Serialize()).ToArray() })
                .ToArray();

            _networkService?.SendToAll(NetworkEventCodes.RevealParchments, payload);
            Debug.Log($"[DraftService] Broadcast parchments for {payload.Length} players");
        }

        private void HandleRevealParchments(object data)
        {
            if (IsMasterClient) return; // master already holds the authoritative copy
            if (data is not object[] rows) return;

            foreach (var row in rows)
            {
                if (row is not object[] entry || entry.Length < 2) continue;

                int actor = (int)entry[0];
                var list = new List<CardData>();

                if (entry[1] is string[] arr)
                {
                    foreach (var s in arr)
                    {
                        var c = CardData.Deserialize(s);
                        if (c != null) list.Add(c);
                    }
                }
                else if (entry[1] is object[] rawList)
                {
                    foreach (var o in rawList)
                    {
                        var c = CardData.Deserialize(o as string);
                        if (c != null) list.Add(c);
                    }
                }

                _playerParchments[actor] = list;
            }

            Debug.Log("[DraftService] Parchments revealed for final scoring");
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

            // This client now holds a hand, so its own deadline starts here. Close enough
            // to when the master started its timer - the gap is one network hop against a
            // two-second margin.
            StartLocalSubmitTimer();

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

            Debug.Log($"[DraftService] Received picks from player {senderActorId}: {string.Join(", ", serializedPicks)}");

            // A submission can arrive after the pick timer already auto-picked for
            // this player (or be a duplicate) - the first accepted submission wins.
            if (_pendingPicks.ContainsKey(senderActorId))
            {
                Debug.LogWarning($"[DraftService] Ignoring duplicate/late picks from player {senderActorId}");
                return;
            }

            // Integrity: only accept picks that exist in the player's current hand
            // (stale picks from a previous hand would corrupt the round).
            if (_playerHands.TryGetValue(senderActorId, out var currentHand))
            {
                var handSet = new HashSet<string>(currentHand.Select(c => c.Serialize()));
                serializedPicks = serializedPicks.Where(handSet.Contains).ToArray();
                if (serializedPicks.Length == 0)
                {
                    Debug.LogWarning($"[DraftService] Ignoring picks from player {senderActorId} - none match their current hand");
                    return;
                }
            }

            // Provisions resolves at pick time and can inject extra territory claims,
            // so the effective pick list may grow beyond what was submitted.
            var effectivePicks = new List<string>(serializedPicks);

            // Everything routed to the sender's pools here happens on the MASTER only -
            // remote players must be told what they now own (GrantCards below).
            var granted = new List<CardData>();

            foreach (var serialized in serializedPicks)
            {
                var card = CardData.Deserialize(serialized);
                if (card == null) continue;

                if (card.IsProvisions)
                {
                    // Resolve NOW (draft rules): draw 2 and play them immediately.
                    ResolveProvisionsPick(senderActorId, effectivePicks, granted);
                }
                else if (card.IsCity || card.IsFarm || card.IsCamp || card.IsSkyTower)
                {
                    // Store buildable card (city/farm/camp/skytower) for the construction phase
                    if (!_playerCityCards.ContainsKey(senderActorId))
                        _playerCityCards[senderActorId] = new List<CardData>();
                    _playerCityCards[senderActorId].Add(card);
                    granted.Add(card);
                }
                else if (card.IsParchment)
                {
                    // Keep parchments in a secret hand; scored at game end
                    if (!_playerParchments.ContainsKey(senderActorId))
                        _playerParchments[senderActorId] = new List<CardData>();
                    _playerParchments[senderActorId].Add(card);
                    granted.Add(card);
                }
            }

            _pendingPicks[senderActorId] = effectivePicks.ToArray();

            // The master routes its own grants locally and deliberately skips the
            // GrantCards send below, so the publish in HandleGrantCardsReceived never
            // runs for the host. Without this the host never sees its own Pacts.
            if (granted.Count > 0 && senderActorId == (_networkService?.LocalPlayerId ?? -1))
                _eventBus?.Publish(new CardsGrantedEvent(senderActorId));

            // Sync a remote player's granted cards to their own client (the master keeps
            // its authoritative copy; the local player already routed nothing client-side).
            if (granted.Count > 0 && senderActorId != (_networkService?.LocalPlayerId ?? -1))
            {
                _networkService?.SendToPlayers(
                    NetworkEventCodes.GrantCards,
                    granted.Select(c => c.Serialize()).ToArray(),
                    new[] { senderActorId });
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
            StopPickTimer();

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
