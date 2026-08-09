using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using _Project.Scripts.Utility;
using Core.Events;

namespace Cards
{
    /// <summary>
    /// Handles card selection logic and enforces pick limits.
    /// Decoupled from network and UI concerns.
    /// </summary>
    public class CardSelectionHandler : MonoBehaviour, ICardSelectionListener
    {
        [Header("Configuration")]
        [SerializeField] private int maxPicks = 2;

        [Tooltip("Picks are submitted by pressing Confirm rather than the moment the last card is clicked. Removes the 'did that count?' ambiguity and lets a misclick be undone.")]
        [SerializeField] private bool requireExplicitConfirm = true;

        private readonly List<Card> _selectedCards = new();
        private readonly List<Card> _confirmedCards = new();
        private CardFactory _cardFactory;
        private IEventBus _eventBus;
        private bool _selectionsLocked;

        public IReadOnlyList<Card> SelectedCards => _selectedCards;
        public IReadOnlyList<Card> ConfirmedCards => _confirmedCards;
        public int MaxPicks => maxPicks;
        public int RemainingPicks => maxPicks - _confirmedCards.Count;
        public bool SelectionsLocked => _selectionsLocked;
        public bool HasReachedMaxPicks => _confirmedCards.Count >= maxPicks;

        /// <summary>
        /// Fired when all picks have been made.
        /// </summary>
        public event Action<string[]> OnAllPicksMade;

        private void Awake()
        {
            ServiceLocator.RegisterService<CardSelectionHandler>(this);
        }

        private void Start()
        {
            ServiceLocator.TryGetService(out _cardFactory);
            ServiceLocator.TryGetService(out _eventBus);
        }

        private void OnDestroy()
        {
            ServiceLocator.UnregisterService<CardSelectionHandler>();
        }

        /// <summary>
        /// Sets the maximum number of picks allowed.
        /// </summary>
        public void SetMaxPicks(int max)
        {
            maxPicks = Mathf.Max(1, max);
        }

        /// <summary>
        /// Resets selection state for a new round.
        /// </summary>
        public void ResetSelections()
        {
            _selectedCards.Clear();
            _confirmedCards.Clear();
            _selectionsLocked = false;

            Debug.Log("[CardSelectionHandler] Selections reset");
            PublishSelectionChanged();
        }

        /// <summary>
        /// Called when a card is selected (highlighted).
        /// </summary>
        public void OnCardSelected(Card card)
        {
            if (_selectionsLocked || card == null) return;

            if (!_selectedCards.Contains(card))
            {
                _selectedCards.Add(card);

                _eventBus?.Publish(new CardSelectedEvent(card.coordinate));
                Debug.Log($"[CardSelectionHandler] Card selected: {card.coordinate}");
            }
        }

        /// <summary>
        /// Called when a card is confirmed (locked in).
        /// </summary>
        public bool OnCardConfirmed(Card card)
        {
            if (_selectionsLocked || card == null) return false;

            if (_confirmedCards.Count >= maxPicks)
            {
                Debug.Log("[CardSelectionHandler] Pick refused - quota already full");
                return false;
            }

            if (!_confirmedCards.Contains(card))
            {
                _confirmedCards.Add(card);

                // Publish event
                var networkService = ServiceLocator.GetService<INetworkService>();
                int localPlayerId = networkService?.LocalPlayerId ?? -1;

                _eventBus?.Publish(new CardConfirmedEvent(card.coordinate, localPlayerId));
                Debug.Log($"[CardSelectionHandler] Card confirmed: {card.coordinate} ({_confirmedCards.Count}/{maxPicks})");

                PublishSelectionChanged();

                // Legacy flow: submitting the instant the last card is clicked. Kept
                // behind a flag so the auto-pick path and tests still work.
                if (!requireExplicitConfirm && CanSubmit)
                {
                    SubmitPicks();
                }
            }

            return true;
        }

        /// <summary>Takes a pick back before it has been submitted.</summary>
        public void OnCardUnconfirmed(Card card)
        {
            if (_selectionsLocked || card == null) return;

            if (_confirmedCards.Remove(card))
            {
                _selectedCards.Remove(card);
                Debug.Log($"[CardSelectionHandler] Card unconfirmed: {card.coordinate} ({_confirmedCards.Count}/{maxPicks})");
                PublishSelectionChanged();
            }
        }

        /// <summary>
        /// True when the player has picked their full quota - or every card they
        /// have, when the hand is smaller than the quota (last pass of a round).
        /// </summary>
        public bool CanSubmit
        {
            get
            {
                int availableCards = _cardFactory?.ActiveCards?.Count ?? 0;
                return !_selectionsLocked &&
                       (_confirmedCards.Count >= maxPicks ||
                        (availableCards > 0 && _confirmedCards.Count >= availableCards));
            }
        }

        /// <summary>Commits the current picks. Safe to call from a Confirm button.</summary>
        public void SubmitPicks()
        {
            if (_selectionsLocked || !CanSubmit) return;

            LockSelections();
            NotifyAllPicksMade();
            PublishSelectionChanged();
        }

        private void PublishSelectionChanged()
        {
            RefreshCardAvailability();
            _eventBus?.Publish(new PickSelectionChangedEvent(_confirmedCards.Count, maxPicks, CanSubmit));
        }

        /// <summary>
        /// Dims the cards that cannot be picked right now, so "quota full" is visible
        /// rather than only discoverable by clicking. Picked cards stay bright (they
        /// can be released); dimmed cards stay hoverable so they can still be read.
        /// </summary>
        private void RefreshCardAvailability()
        {
            if (_cardFactory?.ActiveCards == null) return;

            bool quotaFull = _confirmedCards.Count >= maxPicks;
            foreach (var card in _cardFactory.ActiveCards)
            {
                if (card == null) continue;
                card.SetUnavailable(quotaFull && !_confirmedCards.Contains(card));
            }
        }

        /// <summary>
        /// Called when a card is deselected.
        /// </summary>
        public void OnCardDeselected(Card card)
        {
            if (_selectionsLocked || card == null) return;

            _selectedCards.Remove(card);
            Debug.Log($"[CardSelectionHandler] Card deselected: {card.coordinate}");
        }

        /// <summary>
        /// Locks all selections, preventing further changes.
        /// </summary>
        public void LockSelections()
        {
            _selectionsLocked = true;

            // Disable unconfirmed cards
            if (_cardFactory != null)
            {
                foreach (var card in _cardFactory.ActiveCards)
                {
                    if (!_confirmedCards.Contains(card))
                    {
                        card.Disable();
                    }
                }
            }

            Debug.Log("[CardSelectionHandler] Selections locked");
        }

        /// <summary>
        /// Unlocks selections (for undo functionality if needed).
        /// </summary>
        public void UnlockSelections()
        {
            _selectionsLocked = false;

            // Re-enable cards
            if (_cardFactory != null)
            {
                foreach (var card in _cardFactory.ActiveCards)
                {
                    card.Enable();
                }
            }

            Debug.Log("[CardSelectionHandler] Selections unlocked");
        }

        /// <summary>
        /// Gets the coordinates of all confirmed picks (legacy - territory cards only).
        /// </summary>
        public string[] GetConfirmedCoordinates()
        {
            var coords = new string[_confirmedCards.Count];
            for (int i = 0; i < _confirmedCards.Count; i++)
            {
                coords[i] = _confirmedCards[i].coordinate;
            }
            return coords;
        }

        /// <summary>
        /// Gets the serialized CardData strings of all confirmed picks.
        /// </summary>
        public string[] GetConfirmedSerializedCards()
        {
            return _confirmedCards
                .Where(c => c.cardData != null)
                .Select(c => c.cardData.Serialize())
                .ToArray();
        }

        /// <summary>
        /// Gets confirmed cards that are territory type.
        /// </summary>
        public List<Card> GetConfirmedTerritoryCards()
        {
            return _confirmedCards
                .Where(c => c.cardData != null && c.cardData.IsTerritory)
                .ToList();
        }

        /// <summary>
        /// Gets confirmed cards that are city type.
        /// </summary>
        public List<Card> GetConfirmedCityCards()
        {
            return _confirmedCards
                .Where(c => c.cardData != null && c.cardData.IsCity)
                .ToList();
        }

        private void NotifyAllPicksMade()
        {
            var serialized = GetConfirmedSerializedCards();

            _eventBus?.Publish(new LocalPicksSubmittedEvent(serialized));
            OnAllPicksMade?.Invoke(serialized);

            Debug.Log($"[CardSelectionHandler] All picks submitted: {string.Join(", ", serialized)}");
        }
    }
}
