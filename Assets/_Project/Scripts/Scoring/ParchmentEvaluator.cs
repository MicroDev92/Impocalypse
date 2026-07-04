using System.Linq;
using Cards;

namespace Scoring
{
    /// <summary>
    /// Scores a single Parchment card against a player's end-game board.
    /// Self-contained: only needs the grid service (no cross-player comparison yet).
    /// </summary>
    public static class ParchmentEvaluator
    {
        public static int Evaluate(CardData parchment, int actorNumber, IGridService grid)
        {
            if (parchment == null || !parchment.IsParchment || grid == null) return 0;

            switch (parchment.Rule)
            {
                case ParchmentRule.Treasure:
                    return parchment.Amount;

                case ParchmentRule.PerResource:
                    return parchment.Amount * CountResourceTiles(actorNumber, parchment.Resource, grid);

                case ParchmentRule.PerFief:
                    return parchment.Amount * FiefCalculator.CalculateFiefs(actorNumber, grid).Count;

                case ParchmentRule.LargestFief:
                {
                    var fiefs = FiefCalculator.CalculateFiefs(actorNumber, grid);
                    int largest = fiefs.Count == 0 ? 0 : fiefs.Max(f => f.Tiles.Count);
                    return parchment.Amount * largest;
                }

                case ParchmentRule.PerCity:
                    return parchment.Amount *
                           grid.GetOwnedTiles(actorNumber).Count(t => t.Building == BuildingKind.City);

                default:
                    return 0;
            }
        }

        private static int CountResourceTiles(int actorNumber, ResourceType resource, IGridService grid)
        {
            if (resource == ResourceType.None) return 0;
            return grid.GetOwnedTiles(actorNumber)
                       .Count(t => t.BaseResource == resource || t.AddedResource == resource);
        }
    }
}
