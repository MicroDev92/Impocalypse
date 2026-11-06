using System.Collections.Generic;
using UnityEngine;

namespace Cards
{
    /// <summary>
    /// Builds a shuffled deck of territory and city cards for the game.
    /// Territory: one card per playable tile (100 tiles).
    /// City: 12x Str1 + 6x Str2 + 3x Str3 = 21 city cards.
    /// Total: 188 cards (100 territory + 88 non-territory).
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

            // Parchments: secret end-game scoring, revealed only at the end.
            //
            // The COUNT matters as much as the contents. Hand size is derived from the
            // deck (deckSize / players / rounds, floored to a multiple of picksPerRound),
            // so a short deck silently deals short hands: with the old greybox set of 8
            // the deck came to 159, and four-player games were dealt 8 cards instead of
            // the 10 the config asks for. At 37 - Bunny Kingdom's parchment count, the
            // only category where this deck was short of it - the deck reaches 188 and
            // the formula lands exactly on 10 / 12 / 12 for 4 / 3 / 2 players.
            //
            // PerFief and LargestFief deliberately pull against each other: one rewards
            // spreading into many domains, the other pouring everything into one.

            // Flat points. Never dead, whatever the board ends up looking like, so they
            // set a floor under a hand that drafts badly.
            foreach (int amount in new[] { 2, 2, 3, 3, 4, 5 })
                deck.Add(CardData.Parchment(ParchmentRule.Treasure, amount));

            // Common resources. 16 tiles of each terrain exist, so a player typically
            // finishes with a handful of each; amount 3 is the top end at ~12 points.
            foreach (var common in new[] { ResourceType.Wood, ResourceType.Carrot, ResourceType.Fish })
                foreach (int amount in new[] { 1, 1, 2, 2, 3 })
                    deck.Add(CardData.Parchment(ParchmentRule.PerResource, amount, common));

            // Luxuries. Exactly one Special Farm exists per luxury resource, so these
            // score either 0 or Amount and nothing in between - a gamble, priced for it.
            deck.Add(CardData.Parchment(ParchmentRule.PerResource, 4, ResourceType.Special_A));
            deck.Add(CardData.Parchment(ParchmentRule.PerResource, 4, ResourceType.Special_B));
            deck.Add(CardData.Parchment(ParchmentRule.PerResource, 4, ResourceType.Special_C));

            // Many domains...
            foreach (int amount in new[] { 1, 1, 2, 2, 2, 3 })
                deck.Add(CardData.Parchment(ParchmentRule.PerFief, amount));

            // ...against one big one. Kept cheaper per tile: a large domain is already
            // worth a lot through Strength x Wealth before any parchment touches it.
            foreach (int amount in new[] { 1, 1, 1, 2 })
                deck.Add(CardData.Parchment(ParchmentRule.LargestFief, amount));

            // Citadels have to be drafted AND built, so they stay scarce.
            foreach (int amount in new[] { 2, 2, 3 })
                deck.Add(CardData.Parchment(ParchmentRule.PerCity, amount));

            Shuffle(deck);

            Debug.Log($"[DeckBuilder] Built deck: {coords.Length} territory + 21 city + 18 farm + 6 camp + 3 skytower + 3 provisions + 37 parchment = {deck.Count} total");
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
