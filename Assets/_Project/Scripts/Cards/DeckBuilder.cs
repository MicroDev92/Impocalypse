using System.Collections.Generic;
using UnityEngine;

namespace Cards
{
    /// <summary>
    /// Builds a shuffled deck of territory and city cards for the game.
    /// Territory: one card per playable tile (100 tiles).
    /// City: 12x Str1 + 6x Str2 + 3x Str3 = 21 city cards.
    /// Total: ~121 cards.
    /// </summary>
    public static class DeckBuilder
    {
        public static List<CardData> BuildDeck(IGridService gridService)
        {
            var deck = new List<CardData>();

            // Territory cards: one per playable tile
            var coords = gridService.GetAllPlayableCoords();
            foreach (var coord in coords)
            {
                deck.Add(CardData.Territory(coord));
            }

            // City cards
            for (int i = 0; i < 12; i++)
                deck.Add(CardData.City(CityStrength.Strength1));
            for (int i = 0; i < 6; i++)
                deck.Add(CardData.City(CityStrength.Strength2));
            for (int i = 0; i < 3; i++)
                deck.Add(CardData.City(CityStrength.Strength3));

            Shuffle(deck);

            Debug.Log($"[DeckBuilder] Built deck: {coords.Length} territory + 21 city = {deck.Count} total");
            return deck;
        }

        /// <summary>
        /// Fisher-Yates shuffle.
        /// </summary>
        private static void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
