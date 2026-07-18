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

            // Farm family
            // Common Farms: add a basic resource to a resource-LESS tile
            for (int i = 0; i < 4; i++) deck.Add(CardData.CommonFarm(ResourceType.Wood));
            for (int i = 0; i < 4; i++) deck.Add(CardData.CommonFarm(ResourceType.Carrot));
            for (int i = 0; i < 4; i++) deck.Add(CardData.CommonFarm(ResourceType.Fish));
            // Special Farms: add a luxury resource on a matching RESOURCE tile
            deck.Add(CardData.SpecialFarm(ResourceType.Special_A, TileData.TileType.Wood));
            deck.Add(CardData.SpecialFarm(ResourceType.Special_B, TileData.TileType.Carrot));
            deck.Add(CardData.SpecialFarm(ResourceType.Special_C, TileData.TileType.Water)); // Ectoplasm farm sits on the Styx
            // Markets: wildcard common (player's choice at scoring)
            for (int i = 0; i < 3; i++) deck.Add(CardData.Market());

            // Camps: claim any empty territory. Priority 1 (highest) to 6 (lowest)
            // breaks ties when several players camp in the same build phase.
            for (int p = 1; p <= 6; p++) deck.Add(CardData.Camp(p));

            // Sky Towers: link two territories in different fiefs as adjacent
            for (int i = 0; i < 3; i++) deck.Add(CardData.SkyTower());

            // Provisions: draw 2 cards from the deck and play them immediately
            for (int i = 0; i < 3; i++) deck.Add(CardData.Provisions());

            // Parchments (secret end-game scoring; greybox set)
            deck.Add(CardData.Parchment(ParchmentRule.Treasure, 5));
            deck.Add(CardData.Parchment(ParchmentRule.Treasure, 3));
            deck.Add(CardData.Parchment(ParchmentRule.PerResource, 1, ResourceType.Wood));
            deck.Add(CardData.Parchment(ParchmentRule.PerResource, 1, ResourceType.Carrot));
            deck.Add(CardData.Parchment(ParchmentRule.PerResource, 1, ResourceType.Fish));
            deck.Add(CardData.Parchment(ParchmentRule.PerFief, 2));
            deck.Add(CardData.Parchment(ParchmentRule.LargestFief, 1));
            deck.Add(CardData.Parchment(ParchmentRule.PerCity, 2));

            Shuffle(deck);

            Debug.Log($"[DeckBuilder] Built deck: {coords.Length} territory + 21 city + 18 farm + 6 camp + 3 skytower + 3 provisions + 8 parchment = {deck.Count} total");
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
