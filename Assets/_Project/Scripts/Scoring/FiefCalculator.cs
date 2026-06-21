using System.Collections.Generic;
using System.Linq;
using Cards;
using UnityEngine;

namespace Scoring
{
    /// <summary>
    /// Represents a connected group of owned tiles (a fief).
    /// </summary>
    public class Fief
    {
        public List<TileData> Tiles { get; } = new();
        public int Strength { get; set; }
        public int Wealth { get; set; }
        public int Score => Strength * Wealth;
    }

    /// <summary>
    /// Calculates fiefs (connected territory groups) and their scores.
    /// Score = Strength (sum of city tower values) x Wealth (count of unique resource types).
    /// </summary>
    public static class FiefCalculator
    {
        /// <summary>
        /// Finds all fiefs for a player and calculates their scores.
        /// </summary>
        public static List<Fief> CalculateFiefs(int actorNumber, IGridService gridService)
        {
            var fiefs = new List<Fief>();
            var ownedTiles = gridService.GetOwnedTiles(actorNumber);
            var visited = new HashSet<string>();

            foreach (var tile in ownedTiles)
            {
                if (visited.Contains(tile.coord)) continue;

                // Flood-fill to find connected group
                var fief = new Fief();
                var queue = new Queue<TileData>();
                queue.Enqueue(tile);
                visited.Add(tile.coord);

                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    fief.Tiles.Add(current);

                    var neighbors = gridService.GetOrthogonalNeighbors(current.coord);
                    foreach (var neighbor in neighbors)
                    {
                        if (neighbor.IsOwnedBy(actorNumber) && !visited.Contains(neighbor.coord))
                        {
                            visited.Add(neighbor.coord);
                            queue.Enqueue(neighbor);
                        }
                    }
                }

                // Calculate Strength: sum of city building values in this fief
                fief.Strength = fief.Tiles
                    .Where(t => t.HasBuilding)
                    .Sum(t => (int)t.BuildingStrength);

                // Calculate Wealth: count of unique resource types
                var uniqueResources = new HashSet<TileData.TileType>();
                foreach (var t in fief.Tiles)
                {
                    if (t.type == TileData.TileType.Wood) uniqueResources.Add(TileData.TileType.Wood);
                    else if (t.type == TileData.TileType.Carrot) uniqueResources.Add(TileData.TileType.Carrot);
                    else if (t.type == TileData.TileType.Fish) uniqueResources.Add(TileData.TileType.Fish);
                }
                fief.Wealth = uniqueResources.Count;

                fiefs.Add(fief);
            }

            return fiefs;
        }

        /// <summary>
        /// Calculates total score across all fiefs for a player.
        /// </summary>
        public static int CalculateTotalScore(int actorNumber, IGridService gridService)
        {
            var fiefs = CalculateFiefs(actorNumber, gridService);
            return fiefs.Sum(f => f.Score);
        }
    }
}
