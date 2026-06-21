using UnityEngine;

namespace Config
{
    /// <summary>
    /// Draft phase configuration ScriptableObject.
    /// Contains settings for card dealing and picking.
    /// </summary>
    [CreateAssetMenu(fileName = "DraftConfiguration", menuName = "Impocalypse/Draft Configuration")]
    public class DraftConfiguration : ScriptableObject
    {
        [Header("Card Counts by Player Count")]
        [Tooltip("Cards per player with 2 players")]
        [SerializeField] private int cardsPerPlayer_2Players = 12;

        [Tooltip("Cards per player with 3 players")]
        [SerializeField] private int cardsPerPlayer_3Players = 12;

        [Tooltip("Cards per player with 4 players")]
        [SerializeField] private int cardsPerPlayer_4Players = 10;

        [Tooltip("Cards per player in test mode")]
        [SerializeField] private int cardsPerPlayer_Test = 4;

        [Header("Pick Settings")]
        [Tooltip("Number of cards each player picks per round")]
        [Range(1, 5)]
        [SerializeField] private int picksPerRound = 2;

        [Header("Hand Passing")]
        [Tooltip("If true, odd rounds pass clockwise, even rounds counter-clockwise")]
        [SerializeField] private bool alternatePassDirection = true;

        [Tooltip("If alternatePassDirection is false, always pass in this direction")]
        [SerializeField] private bool defaultClockwise = true;

        [Header("Deck Mode")]
        [Tooltip("Use deck-based dealing with territory + city cards")]
        [SerializeField] private bool useDeckDealing = true;

        // Properties
        public int PicksPerRound => picksPerRound;
        public bool UseDeckDealing => useDeckDealing;
        public bool AlternatePassDirection => alternatePassDirection;
        public bool DefaultClockwise => defaultClockwise;

        /// <summary>
        /// Gets the number of cards per player based on player count.
        /// </summary>
        /// <param name="playerCount">Number of players in the game</param>
        /// <param name="testMode">Whether test mode is enabled</param>
        /// <returns>Number of cards to deal to each player</returns>
        public int GetCardsPerPlayer(int playerCount, bool testMode = false)
        {
            if (testMode)
            {
                return cardsPerPlayer_Test;
            }

            return playerCount switch
            {
                2 => cardsPerPlayer_2Players,
                3 => cardsPerPlayer_3Players,
                4 => cardsPerPlayer_4Players,
                _ => cardsPerPlayer_4Players
            };
        }

        /// <summary>
        /// Determines pass direction for a given round.
        /// </summary>
        /// <param name="roundNumber">Current round number (1-based)</param>
        /// <returns>True if clockwise, false if counter-clockwise</returns>
        public bool IsClockwise(int roundNumber)
        {
            if (!alternatePassDirection)
            {
                return defaultClockwise;
            }

            // Odd rounds = clockwise, even rounds = counter-clockwise
            return roundNumber % 2 != 0;
        }

        /// <summary>
        /// Calculates cards per player from deck size for deck-based dealing.
        /// deck.Count / playerCount / maxRounds, capped to a reasonable maximum.
        /// </summary>
        public int GetCardsPerPlayerFromDeck(int deckSize, int playerCount, int maxRounds)
        {
            if (playerCount <= 0 || maxRounds <= 0) return picksPerRound;
            int calculated = deckSize / playerCount / maxRounds;
            // Cap at 12 cards max per hand
            calculated = Mathf.Min(calculated, 12);
            // Round down to nearest multiple of picksPerRound so hands always empty cleanly
            calculated = (calculated / picksPerRound) * picksPerRound;
            return Mathf.Max(calculated, picksPerRound);
        }

        /// <summary>
        /// Validates configuration values.
        /// </summary>
        private void OnValidate()
        {
            cardsPerPlayer_2Players = Mathf.Max(picksPerRound, cardsPerPlayer_2Players);
            cardsPerPlayer_3Players = Mathf.Max(picksPerRound, cardsPerPlayer_3Players);
            cardsPerPlayer_4Players = Mathf.Max(picksPerRound, cardsPerPlayer_4Players);
            cardsPerPlayer_Test = Mathf.Max(picksPerRound, cardsPerPlayer_Test);
        }
    }
}
