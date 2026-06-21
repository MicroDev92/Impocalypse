using System.Collections.Generic;
using Scoring;
using UnityEngine;

/// <summary>
/// Interface for resource management services.
/// Tracks per-player resource collection.
/// </summary>
public interface IResourceService
{
    // Events (actorNumber, resourceType, newAmount)
    event System.Action<int, TileData.TileType, int> OnResourceChanged;
    event System.Action<int, TileData.TileType, int> OnResourceCollected;

    /// <summary>
    /// Collects a resource from a tile for a specific player.
    /// </summary>
    bool CollectResource(TileData tile, int actorNumber);

    /// <summary>
    /// Checks if a tile type is a resource tile.
    /// </summary>
    bool IsResourceTile(TileData.TileType tileType);

    /// <summary>
    /// Gets a player's amount of a resource.
    /// </summary>
    int GetResourceAmount(int actorNumber, TileData.TileType resourceType);

    /// <summary>
    /// Gets the display name of a resource.
    /// </summary>
    string GetResourceName(TileData.TileType resourceType);

    /// <summary>
    /// Gets the icon for a resource.
    /// </summary>
    Sprite GetResourceIcon(TileData.TileType resourceType);

    /// <summary>
    /// Gets all resource amounts for a player.
    /// </summary>
    Dictionary<TileData.TileType, int> GetPlayerResources(int actorNumber);

    /// <summary>
    /// Gets total score for a player (sum of all resources).
    /// </summary>
    int GetPlayerScore(int actorNumber);

    /// <summary>
    /// Gets all player scores.
    /// </summary>
    Dictionary<int, int> GetAllPlayerScores();

    /// <summary>
    /// Resets all resources.
    /// </summary>
    void ResetAll();

    /// <summary>
    /// Gets fief breakdown for a player (for scoring display).
    /// </summary>
    List<Fief> GetPlayerFiefs(int actorNumber);
}
