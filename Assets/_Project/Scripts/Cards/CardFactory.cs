using System.Collections.Generic;
using UnityEngine;
using _Project.Scripts.Utility;
using Core.Pooling;

namespace Cards
{
    /// <summary>
    /// Factory for creating and managing card instances.
    /// Uses object pooling to reduce GC pressure.
    /// </summary>
    public class CardFactory : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private GameObject cardPrefab;
        [SerializeField] private Transform cardContainer;
        [SerializeField] private int poolInitialSize = 20;

        private ObjectPool<Card> _cardPool;
        private readonly List<Card> _activeCards = new();

        public IReadOnlyList<Card> ActiveCards => _activeCards;

        private void Awake()
        {
            ServiceLocator.RegisterService<CardFactory>(this);
            InitializePool();
        }

        private void OnDestroy()
        {
            ServiceLocator.UnregisterService<CardFactory>();
        }

        private void InitializePool()
        {
            if (cardPrefab == null)
            {
                Debug.LogError("[CardFactory] Card prefab not assigned!");
                return;
            }

            _cardPool = ObjectPool<Card>.FromPrefab(
                cardPrefab.GetComponent<Card>(),
                cardContainer,
                poolInitialSize
            );
        }

        /// <summary>
        /// Creates cards for the given coordinates.
        /// </summary>
        /// <param name="coordinates">Array of coordinate strings</param>
        /// <param name="listener">Optional selection listener</param>
        /// <returns>List of created cards</returns>
        public List<Card> CreateCards(string[] coordinates, ICardSelectionListener listener = null)
        {
            if (coordinates == null || coordinates.Length == 0)
            {
                Debug.LogWarning("[CardFactory] No coordinates provided");
                return new List<Card>();
            }

            var createdCards = new List<Card>();

            foreach (var coord in coordinates)
            {
                var card = CreateCard(coord, listener);
                if (card != null)
                {
                    createdCards.Add(card);
                }
            }

            // Refresh the card panel layout so cards are properly sized
            if (cardContainer != null)
            {
                RefreshContainerLayout();
            }

            Debug.Log($"[CardFactory] Created {createdCards.Count} cards");
            return createdCards;
        }

        /// <summary>
        /// Creates cards from CardData array (new deck-based path).
        /// </summary>
        public List<Card> CreateCards(CardData[] cardDatas, ICardSelectionListener listener = null)
        {
            if (cardDatas == null || cardDatas.Length == 0)
            {
                Debug.LogWarning("[CardFactory] No card data provided");
                return new List<Card>();
            }

            var createdCards = new List<Card>();

            foreach (var data in cardDatas)
            {
                var card = CreateCard(data, listener);
                if (card != null) createdCards.Add(card);
            }

            if (cardContainer != null)
            {
                RefreshContainerLayout();
            }

            Debug.Log($"[CardFactory] Created {createdCards.Count} cards from CardData");
            return createdCards;
        }

        /// <summary>
        /// Creates a single card from CardData.
        /// </summary>
        public Card CreateCard(CardData data, ICardSelectionListener listener = null)
        {
            if (_cardPool == null)
            {
                Debug.LogError("[CardFactory] Pool not initialized!");
                return null;
            }

            var card = _cardPool.Get();
            if (card == null)
            {
                Debug.LogError("[CardFactory] Failed to get card from pool");
                return null;
            }

            card.Initialize(data, listener);
            card.transform.SetParent(cardContainer, false);
            _activeCards.Add(card);

            return card;
        }

        /// <summary>
        /// Creates a single card.
        /// </summary>
        /// <param name="coordinate">Coordinate string</param>
        /// <param name="listener">Optional selection listener</param>
        /// <returns>Created card or null</returns>
        public Card CreateCard(string coordinate, ICardSelectionListener listener = null)
        {
            if (_cardPool == null)
            {
                Debug.LogError("[CardFactory] Pool not initialized!");
                return null;
            }

            var card = _cardPool.Get();
            if (card == null)
            {
                Debug.LogError("[CardFactory] Failed to get card from pool");
                return null;
            }

            card.Initialize(coordinate, listener);
            card.transform.SetParent(cardContainer, false);
            _activeCards.Add(card);

            return card;
        }

        /// <summary>
        /// Returns a card to the pool.
        /// </summary>
        /// <param name="card">Card to return</param>
        public void ReturnCard(Card card)
        {
            if (card == null) return;

            _activeCards.Remove(card);
            card.Reset();
            _cardPool?.Return(card);
        }

        /// <summary>
        /// Returns all active cards to the pool.
        /// </summary>
        public void ReturnAllCards()
        {
            foreach (var card in _activeCards.ToArray())
            {
                card.Reset();
                _cardPool?.Return(card);
            }
            _activeCards.Clear();

            Debug.Log("[CardFactory] Returned all cards to pool");
        }

        /// <summary>
        /// Clears all cards (destroys rather than pooling).
        /// Use ReturnAllCards() when possible.
        /// </summary>
        public void ClearAllCards()
        {
            foreach (var card in _activeCards)
            {
                if (card != null)
                {
                    Destroy(card.gameObject);
                }
            }
            _activeCards.Clear();
        }

        /// <summary>
        /// Sets the card container (useful for UI setup).
        /// </summary>
        public void SetCardContainer(Transform container)
        {
            cardContainer = container;
        }

        /// <summary>
        /// Refreshes whichever layout component is on the card container
        /// (supports both CardPanelLayout and CardFanLayout).
        /// </summary>
        private void RefreshContainerLayout()
        {
            if (cardContainer == null) return;

            var fanLayout = cardContainer.GetComponent<CardFanLayout>();
            if (fanLayout != null)
            {
                fanLayout.RefreshCardSizes();
                return;
            }

            var panelLayout = cardContainer.GetComponent<CardPanelLayout>();
            panelLayout?.RefreshCardSizes();
        }
    }
}
